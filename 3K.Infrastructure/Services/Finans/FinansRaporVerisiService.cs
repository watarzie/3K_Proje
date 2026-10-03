using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Exceptions;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;

namespace _3K.Infrastructure.Services.Finans;

/// <summary>Rapor dosyalarının ortak veri kümesi: filtreye uyan işler, giderler ve para birimi toplamları.</summary>
internal sealed class FinansRaporVerisiService(AppDbContext context) : IFinansRaporVerisiService
{
    public async Task<FinansRaporModel> RaporVerisiAsync(FinansListeFiltre filtre, CancellationToken cancellationToken)
    {
        if (filtre.Baslangic.HasValue && filtre.Bitis.HasValue) FinansTarihKurallari.AraligiDogrula(filtre.Baslangic.Value, filtre.Bitis.Value);
        if (await FinansSorgulari.ApplyFilter(context.Set<FinansIsKaydi>(), filtre).CountAsync(cancellationToken) > FinansSorgulari.RaporKayitSiniri)
            throw IsKuraliException.Cakisma("Rapor en fazla 20.000 iş içerebilir; filtreyi daraltın.");
        var workEntities = await FinansSorgulari.ApplyFilter(context.IsKaydiDetayQuery(), filtre).OrderBy(x => x.FinansDonemi).ThenBy(x => x.Id).ToListAsync(cancellationToken);
        var works = workEntities.Select(FinansModelEslemeleri.MapIsKaydi).ToArray();
        var expenseQuery = context.Set<FinansGider>().AsNoTracking().Include(x => x.Kategori).Include(x => x.GiderKalemi).Include(x => x.Proje).AsQueryable();
        expenseQuery = FinansSorgulari.ExpenseFilter(expenseQuery, filtre);
        var expenseTotals = await FinansSorgulari.BuildExpenseTotalsQuery(expenseQuery)
            .OrderBy(x => x.ParaBirimi)
            .ToArrayAsync(cancellationToken);
        if (await expenseQuery.CountAsync(cancellationToken) > FinansSorgulari.RaporKayitSiniri)
            throw IsKuraliException.Cakisma("Rapor en fazla 20.000 gider içerebilir; filtreyi daraltın.");
        var expenses = (await expenseQuery
                .OrderBy(x => x.FinansDonemi)
                .ThenBy(x => x.Id)
                .ToListAsync(cancellationToken))
            .Select(FinansModelEslemeleri.MapExpense)
            .ToArray();

        // Fatura başlığını filtreleyip MapFatura ile tüm kalemleri toplamak proje/iş türü/
        // para birimi filtrelerinde ilgisiz kalemleri gelire katardı. Gelir hesabı doğrudan
        // filtreye uyan fatura kalemleri üzerinden yapılır.
        var invoiceLineQuery = ApplyInvoiceLineFilter(
            context.Set<FinansFaturaKalemi>().AsNoTracking(),
            filtre);
        var incomes = await FinansSorgulari.BuildInvoiceTotalsQuery(invoiceLineQuery)
            .OrderBy(x => x.ParaBirimi)
            .ToArrayAsync(cancellationToken);
        var currencies = incomes.Select(x => x.ParaBirimi).Union(expenseTotals.Select(x => x.ParaBirimi)).Order().ToArray();
        var net = currencies.Select(currency =>
        {
            var income = incomes.FirstOrDefault(x => x.ParaBirimi == currency);
            var expense = expenseTotals.FirstOrDefault(x => x.ParaBirimi == currency);
            return new FinansParaToplamiModel(currency,
                (income?.NetTutar ?? 0) - (expense?.NetTutar ?? 0),
                (income?.KdvTutari ?? 0) - (expense?.KdvTutari ?? 0),
                (income?.ToplamTutar ?? 0) - (expense?.ToplamTutar ?? 0));
        }).ToArray();
        return new FinansRaporModel
        {
            Filtre = filtre,
            Isler = works,
            Giderler = expenses,
            GelirToplamlari = incomes,
            GiderToplamlari = expenseTotals,
            NetToplamlari = net
        };
    }

    internal static IQueryable<FinansFaturaKalemi> ApplyInvoiceLineFilter(
        IQueryable<FinansFaturaKalemi> query,
        FinansListeFiltre filtre)
    {
        if (filtre.FaturaDurumu == FinansFaturaDurumu.IptalEdildi)
            query = query.Where(x =>
                x.FinansFatura.IptalEdildi &&
                x.FinansFatura.Durum == FinansFaturaDurumu.IptalEdildi);
        else
        {
            if (!filtre.IptalEdilenleriDahilEt || filtre.FaturaDurumu == FinansFaturaDurumu.Aktif)
                query = query.Where(x => !x.FinansFatura.IptalEdildi);
            if (filtre.FaturaDurumu == FinansFaturaDurumu.Aktif)
                query = query.Where(x => x.FinansFatura.Durum == FinansFaturaDurumu.Aktif);
        }
        if (filtre.Baslangic.HasValue) query = query.Where(x => x.FinansSiparisKalemi.FinansIsKaydi.FinansTarihi >= filtre.Baslangic.Value.Date);
        if (filtre.Bitis.HasValue)
        {
            var end = filtre.Bitis.Value.Date.AddDays(1);
            query = query.Where(x => x.FinansSiparisKalemi.FinansIsKaydi.FinansTarihi < end);
        }
        if (filtre.ProjeId.HasValue) query = query.Where(x => x.FinansSiparisKalemi.FinansIsKaydi.ProjeId == filtre.ProjeId);
        if (!filtre.IptalEdilenleriDahilEt) query = query.Where(x => !x.FinansSiparisKalemi.FinansIsKaydi.IptalEdildi && x.FinansSiparisKalemi.FinansIsKaydi.KaynakAktif && !x.FinansFatura.FinansSiparis.IptalEdildi);
        if (filtre.SandikCinsi.HasValue) query = query.Where(x => x.FinansSiparisKalemi.FinansIsKaydi.SandikCinsi == filtre.SandikCinsi);
        if (!string.IsNullOrWhiteSpace(filtre.Firma)) query = query.Where(x => x.FinansSiparisKalemi.FinansIsKaydi.Musteri.ToLower().Contains(filtre.Firma.ToLower()));
        if (!string.IsNullOrWhiteSpace(filtre.FaturaNumarasi)) query = query.Where(x => x.FinansFatura.FaturaNumarasi.ToLower().Contains(filtre.FaturaNumarasi.ToLower()));
        if (!string.IsNullOrWhiteSpace(filtre.ProjeNo)) query = query.Where(x => x.FinansSiparisKalemi.FinansIsKaydi.ProjeNo == filtre.ProjeNo.Trim());
        if (filtre.IsTuru.HasValue) query = query.Where(x => x.FinansSiparisKalemi.FinansIsKaydi.IsTuru == filtre.IsTuru);
        if (filtre.Durum.HasValue) query = query.Where(x => x.FinansSiparisKalemi.FinansIsKaydi.Durum == filtre.Durum);
        if (filtre.SiparisDurumu.HasValue) query = query.Where(x => x.FinansFatura.FinansSiparis.Durum == filtre.SiparisDurumu);
        if (!string.IsNullOrWhiteSpace(filtre.ParaBirimi))
        {
            var currency = filtre.ParaBirimi.Trim().ToUpperInvariant();
            query = query.Where(x => x.FinansSiparisKalemi.ParaBirimiSnapshot == currency);
        }
        if (!string.IsNullOrWhiteSpace(filtre.PoNumarasi))
        {
            var po = filtre.PoNumarasi.Trim().ToLower();
            query = query.Where(x => x.FinansFatura.FinansSiparis.PoNumarasi.ToLower().Contains(po));
        }
        if (!string.IsNullOrWhiteSpace(filtre.TalepEden))
        {
            var requester = filtre.TalepEden.Trim().ToLower();
            query = query.Where(x =>
                (x.FinansSiparisKalemi.FinansIsKaydi.TalepEdenKisi != null && x.FinansSiparisKalemi.FinansIsKaydi.TalepEdenKisi.ToLower().Contains(requester)) ||
                (x.FinansSiparisKalemi.FinansIsKaydi.TalepEdenBolum != null && x.FinansSiparisKalemi.FinansIsKaydi.TalepEdenBolum.ToLower().Contains(requester)));
        }
        if (!string.IsNullOrWhiteSpace(filtre.Arama))
        {
            var search = filtre.Arama.Trim().ToLower();
            query = query.Where(x =>
                x.FinansFatura.FaturaNumarasi.ToLower().Contains(search) ||
                x.FinansFatura.FinansSiparis.PoNumarasi.ToLower().Contains(search) ||
                x.FinansSiparisKalemi.FinansIsKaydi.ProjeNo.ToLower().Contains(search));
        }
        return query;
    }
}
