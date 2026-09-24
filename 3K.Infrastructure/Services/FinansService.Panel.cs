using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Models;

namespace _3K.Infrastructure.Services;

public sealed partial class FinansService
{
    private const int RaporKayitSiniri = 20000;
    private static void TarihAraliginiDogrula(DateTime start, DateTime end)
    {
        if (start.Year < 2000 || end.Year > 2200 || end.Date < start.Date || (end.Date - start.Date).TotalDays > 3660)
            throw new InvalidOperationException("Geçerli ve en fazla on yıllık tarih aralığı seçin.");
    }

    public Task<FinansSayfaliSonuc<FinansIsKaydiModel>> GenelAramaAsync(FinansListeFiltre filtre, CancellationToken cancellationToken)
        => IsKayitlariAsync(filtre with { Baslangic = null, Bitis = null }, cancellationToken);

    public async Task<FinansSayfaliSonuc<FinansHareketModel>> HareketlerAsync(FinansListeFiltre filtre, string? tur, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(tur) && tur is not ("IsKaydi" or "Siparis" or "Fatura" or "Gider"))
            throw new InvalidOperationException("Hareket türü geçerli değil.");
        var page = Math.Max(1, filtre.PageNumber); var size = Math.Clamp(filtre.PageSize, 1, 250);
        var take = checked(page * size);
        if (take > RaporKayitSiniri) throw new InvalidOperationException("Bu sayfa için filtreyi daraltın; en fazla 20.000 hareket taranabilir.");
        var workQuery = ApplyFilter(_context.Set<FinansIsKaydi>().AsNoTracking(), filtre);
        var expenseQuery = ExpenseFilter(_context.Set<FinansGider>().AsNoTracking(), filtre);
        var linkedWorkIds = ApplyFilter(_context.Set<FinansIsKaydi>().AsNoTracking(), filtre with { Baslangic = null, Bitis = null }).Select(x => x.Id);
        var orderQuery = _context.Set<FinansSiparis>().AsNoTracking().Where(x => x.Kalemler.Any(k => linkedWorkIds.Contains(k.FinansIsKaydiId)));
        var invoiceQuery = _context.Set<FinansFatura>().AsNoTracking().Where(x => x.Kalemler.Any(k => linkedWorkIds.Contains(k.FinansSiparisKalemi.FinansIsKaydiId)));
        if (!filtre.IptalEdilenleriDahilEt) { orderQuery = orderQuery.Where(x => !x.IptalEdildi); invoiceQuery = invoiceQuery.Where(x => !x.IptalEdildi); }
        if (filtre.Baslangic.HasValue) { orderQuery = orderQuery.Where(x => x.SiparisTarihi >= filtre.Baslangic.Value.Date); invoiceQuery = invoiceQuery.Where(x => x.FaturaTarihi >= filtre.Baslangic.Value.Date); }
        if (filtre.Bitis.HasValue) { var end = filtre.Bitis.Value.Date.AddDays(1); orderQuery = orderQuery.Where(x => x.SiparisTarihi < end); invoiceQuery = invoiceQuery.Where(x => x.FaturaTarihi < end); }
        if (!string.IsNullOrWhiteSpace(tur))
        {
            if (tur != "IsKaydi") workQuery = workQuery.Where(x => false);
            if (tur != "Gider") expenseQuery = expenseQuery.Where(x => false);
            if (tur != "Siparis") orderQuery = orderQuery.Where(x => false);
            if (tur != "Fatura") invoiceQuery = invoiceQuery.Where(x => false);
        }
        var count = await workQuery.CountAsync(cancellationToken) + await expenseQuery.CountAsync(cancellationToken) +
            await orderQuery.CountAsync(cancellationToken) + await invoiceQuery.CountAsync(cancellationToken);
        // Sıralama ve sınır domain sorgusunda uygulanır; record constructor üstüne ORDER BY EF tarafından çevrilemez.
        var works = await workQuery.OrderByDescending(x => x.FinansTarihi).ThenByDescending(x => x.Id).Take(take)
            .Select(x => new FinansHareketModel("IsKaydi", x.Id, x.ProjeNo, x.IsAdi, x.UretimTarihi, x.FinansTarihi, x.FinansDonemi,
                x.ParaBirimiSnapshot, x.ManuelNetTutar ?? decimal.Round(x.BirimFiyatSnapshot *
                    (x.FiyatlandirmaBirimiSnapshot == FinansFiyatlandirmaBirimi.Adet ? x.Adet : x.FiyatlandirmaBirimiSnapshot == FinansFiyatlandirmaBirimi.Metrekup ? x.ToplamM3 : 1m), 2), x.Durum.ToString())).ToListAsync(cancellationToken);
        var expenses = await expenseQuery.OrderByDescending(x => x.FinansTarihi).ThenByDescending(x => x.Id).Take(take)
            .Select(x => new FinansHareketModel("Gider", x.Id, x.Proje != null ? x.Proje.ProjeNo : x.ManuelProjeNo ?? "", x.Aciklama,
                x.Tarih, x.FinansTarihi, x.FinansDonemi, x.ParaBirimi, x.Matrah, x.IptalEdildi ? "İptal" : x.AvansMi ? "Avans" : "Gider")).ToListAsync(cancellationToken);
        var orders = await orderQuery.OrderByDescending(x => x.SiparisTarihi).ThenByDescending(x => x.Id).Take(take)
            .Select(x => new FinansHareketModel("Siparis", x.Id, "", x.PoNumarasi, x.SiparisTarihi, x.SiparisTarihi, x.SiparisTarihi,
                x.ParaBirimi ?? "", x.Kalemler.Sum(k => k.NetTutarSnapshot), x.Durum.ToString())).ToListAsync(cancellationToken);
        var invoices = await invoiceQuery.OrderByDescending(x => x.FaturaTarihi).ThenByDescending(x => x.Id).Take(take)
            .Select(x => new FinansHareketModel("Fatura", x.Id, "", x.FaturaNumarasi, x.FaturaTarihi, x.FaturaTarihi, x.FaturaTarihi,
                x.BelgeParaBirimiSnapshot ?? x.FinansSiparis.ParaBirimi ?? "", x.Kalemler.Sum(k => k.NetTutarSnapshot), x.Durum.ToString())).ToListAsync(cancellationToken);
        return new() { Items = works.Concat(expenses).Concat(orders).Concat(invoices).OrderByDescending(x => x.FinansTarihi).ThenBy(x => x.Tur).ThenByDescending(x => x.Id)
            .Skip((page - 1) * size).Take(size).ToArray(), PageNumber = page, PageSize = size, TotalCount = count };
    }

    private static IQueryable<FinansGider> ExpenseFilter(IQueryable<FinansGider> query, FinansListeFiltre filtre)
    {
        if (filtre.GiderKategoriId.HasValue) query = query.Where(x => x.FinansGiderKategoriId == filtre.GiderKategoriId);
        if (!string.IsNullOrWhiteSpace(filtre.Firma))
        {
            var firma = LiteralSearchPattern(filtre.Firma);
            query = query.Where(x => x.FirmaVeyaKisi != null && EF.Functions.ILike(x.FirmaVeyaKisi, firma, "\\"));
        }
        if (!filtre.IptalEdilenleriDahilEt) query = query.Where(x => !x.IptalEdildi);
        if (filtre.Baslangic.HasValue) query = query.Where(x => x.FinansTarihi >= filtre.Baslangic.Value.Date);
        if (filtre.Bitis.HasValue) { var end = filtre.Bitis.Value.Date.AddDays(1); query = query.Where(x => x.FinansTarihi < end); }
        if (filtre.ProjeId.HasValue) query = query.Where(x => x.ProjeId == filtre.ProjeId);
        if (!string.IsNullOrWhiteSpace(filtre.ProjeNo)) query = query.Where(x => x.ManuelProjeNo == filtre.ProjeNo || (x.Proje != null && x.Proje.ProjeNo == filtre.ProjeNo));
        if (!string.IsNullOrWhiteSpace(filtre.ParaBirimi)) query = query.Where(x => x.ParaBirimi == filtre.ParaBirimi);
        if (filtre.IsTuru.HasValue) query = query.Where(x => x.IsTuru == filtre.IsTuru);
        if (!string.IsNullOrWhiteSpace(filtre.Arama))
        {
            var search = LiteralSearchPattern(filtre.Arama);
            query = query.Where(x => EF.Functions.ILike(x.Aciklama, search, "\\") || (x.FirmaVeyaKisi != null && EF.Functions.ILike(x.FirmaVeyaKisi, search, "\\")) ||
                (x.BelgeNo != null && EF.Functions.ILike(x.BelgeNo, search, "\\")) || EF.Functions.ILike(x.Kategori.Ad, search, "\\"));
        }
        return query;
    }

    public async Task<FinansPanelModel> PanelAsync(DateTime baslangic, DateTime bitis, CancellationToken cancellationToken)
    {
        TarihAraliginiDogrula(baslangic, bitis);
        var yearStart = new DateTime(bitis.Year, 1, 1);
        var loadStart = baslangic < yearStart ? baslangic.Date : yearStart;
        var end = bitis.Date.AddDays(1);
        var worksQuery = IsKaydiDetayQuery().Where(x => !x.IptalEdildi && x.KaynakAktif && x.FinansTarihi >= loadStart && x.FinansTarihi < end);
        if (await worksQuery.CountAsync(cancellationToken) > RaporKayitSiniri) throw new InvalidOperationException("Panel aralığında 20.000'den fazla iş var; tarih aralığını daraltın.");
        var allWorks = await worksQuery.ToListAsync(cancellationToken);
        var expenseQuery = _context.Set<FinansGider>().AsNoTracking().Include(x => x.Kategori).Include(x => x.Proje)
            .Where(x => !x.IptalEdildi && !x.AvansMi && x.FinansTarihi >= loadStart && x.FinansTarihi < end);
        if (await expenseQuery.CountAsync(cancellationToken) > RaporKayitSiniri) throw new InvalidOperationException("Panel aralığında 20.000'den fazla gider var; tarih aralığını daraltın.");
        var allExpenses = await expenseQuery.ToListAsync(cancellationToken);
        var works = allWorks.Where(x => x.FinansTarihi >= baslangic.Date).Select(MapIsKaydi).ToArray();
        var expenses = allExpenses.Where(x => x.FinansTarihi >= baslangic.Date).ToArray();
        var currencies = works.Select(x => x.ParaBirimi).Concat(expenses.Select(x => x.ParaBirimi)).Concat(new[] { "TRY", "EUR", "USD" }).Distinct().Order().ToArray();
        var totals = currencies.Select(currency =>
        {
            var w = works.Where(x => x.ParaBirimi == currency).ToArray();
            var income = w.Sum(x => x.FaturalananNetTutar);
            var cost = expenses.Where(x => x.ParaBirimi == currency).Sum(x => x.Matrah);
            var eligible = w.Sum(x => x.NetTutar);
            return new FinansPanelParaModel(currency, eligible, w.Sum(x => x.SiparisNetTutar), w.Sum(x => x.KalanSiparisNetTutar), w.Sum(x => x.KalanFaturaNetTutar),
                income, cost, income - cost, eligible - cost, eligible == 0 ? null : FinansTutarKurallari.Para((eligible - cost) / eligible * 100m),
                allWorks.Where(x => x.FinansTarihi >= yearStart && x.ParaBirimiSnapshot == currency).Sum(FinansTutarKurallari.FaturaNet),
                allExpenses.Where(x => x.FinansTarihi >= yearStart && x.ParaBirimi == currency).Sum(x => x.Matrah));
        }).ToArray();
        var monthly = allWorks.GroupBy(x => new { Month = FirstDayOfMonth(x.FinansTarihi), Currency = x.ParaBirimiSnapshot })
            .Select(x => new FinansGrafikModel(x.Key.Month.ToString("yyyy-MM"), x.Key.Currency, x.Sum(FinansTutarKurallari.FaturaNet), 0m))
            .Concat(allExpenses.GroupBy(x => new { Month = FirstDayOfMonth(x.FinansTarihi), Currency = x.ParaBirimi })
                .Select(x => new FinansGrafikModel(x.Key.Month.ToString("yyyy-MM"), x.Key.Currency, 0m, x.Sum(y => y.Matrah))))
            .GroupBy(x => new { x.Grup, x.ParaBirimi }).Select(x => new FinansGrafikModel(x.Key.Grup, x.Key.ParaBirimi, x.Sum(y => y.Gelir), x.Sum(y => y.Gider))).OrderBy(x => x.Grup).ToArray();
        var types = works.GroupBy(x => new { x.IsTuru, x.ParaBirimi }).Select(x => new FinansGrafikModel(x.Key.IsTuru.ToString(), x.Key.ParaBirimi, x.Sum(y => y.FaturalananNetTutar), 0m)).ToArray();
        var costs = expenses.GroupBy(x => new { Category = x.Kategori.Ad, x.ParaBirimi }).Select(x => new FinansGrafikModel(x.Key.Category, x.Key.ParaBirimi, 0m, x.Sum(y => y.Matrah))).ToArray();
        return new(baslangic.Date, bitis.Date, "İş ve gider finans tarihi; gelir=faturalanan net, tahmini kâr=uygun iş net bedeli−kayıtlı net gider; avans maliyet değildir.",
            totals, monthly, types, costs, works.Count(x => x.SiparisNetTutar == 0), works.Count(x => x.SiparisNetTutar > 0 && x.KalanSiparisNetTutar > 0),
            works.Count(x => x.NetTutar > 0 && x.KalanSiparisNetTutar == 0), works.Count(x => x.FaturalananNetTutar > 0 && x.Durum != FinansIsDurumu.Faturalandi), works.Count(x => x.Durum == FinansIsDurumu.Faturalandi),
            works.GroupBy(x => new { x.ProjeNo, x.ParaBirimi }).Select(x => new FinansGrafikModel(x.Key.ProjeNo, x.Key.ParaBirimi, x.Sum(y => y.FaturalananNetTutar),
                expenses.Where(g => (g.Proje?.ProjeNo ?? g.ManuelProjeNo ?? "") == x.Key.ProjeNo && g.ParaBirimi == x.Key.ParaBirimi).Sum(g => g.Matrah))).ToArray(),
            works.Where(x => x.IsTuru == FinansIsTuru.OzelIs).GroupBy(x => new { x.OzelIsTuru, x.ParaBirimi }).Select(x => new FinansGrafikModel(x.Key.OzelIsTuru ?? "Özel İş", x.Key.ParaBirimi, x.Sum(y => y.FaturalananNetTutar), 0)).ToArray());
    }

    internal static string YasGrubu(int days) => days >= 90 ? "90+" : days >= 60 ? "60–89" : days >= 30 ? "30–59" : days >= 15 ? "15–29" : "0–14";

    public async Task<FinansSayfaliSonuc<FinansBekleyenModel>> YaslandirmaAsync(DateTime referansTarihi, int minimumGun, FinansListeFiltre filtre, CancellationToken cancellationToken)
    {
        if (minimumGun < 0 || minimumGun > 36600) throw new InvalidOperationException("Geçerli bekleme gününü girin.");
        var query = ApplyFilter(IsKaydiDetayQuery(), filtre with { Baslangic = null, Bitis = null, IptalEdilenleriDahilEt = false });
        if (await query.CountAsync(cancellationToken) > RaporKayitSiniri) throw new InvalidOperationException("Yaşlandırma için proje veya iş türü filtresini daraltın (en fazla 20.000 iş).");
        var works = await query.ToListAsync(cancellationToken);
        var items = new List<FinansBekleyenModel>();
        foreach (var work in works)
        {
            var remaining = FinansTutarKurallari.IsNet(work) - FinansTutarKurallari.SiparisNet(work);
            var age = Math.Max(0, (referansTarihi.Date - work.KayitTarihi.Date).Days);
            if (remaining > 0 && age >= minimumGun) items.Add(new(work.Id, null, work.ProjeNo, work.IsAdi, null, "Sipariş", work.KayitTarihi.Date, age, YasGrubu(age), work.ParaBirimiSnapshot, remaining));
            foreach (var line in work.SiparisKalemleri.Where(x => !x.FinansSiparis.IptalEdildi))
            {
                remaining = line.NetTutarSnapshot - FinansTutarKurallari.FaturaNet(line);
                age = Math.Max(0, (referansTarihi.Date - line.FinansSiparis.SiparisTarihi.Date).Days);
                if (remaining > 0 && age >= minimumGun) items.Add(new(work.Id, line.Id, work.ProjeNo, work.IsAdi, line.FinansSiparis.PoNumarasi, "Fatura", line.FinansSiparis.SiparisTarihi.Date, age, YasGrubu(age), line.ParaBirimiSnapshot, remaining));
            }
        }
        var page = Math.Max(1, filtre.PageNumber); var size = Math.Clamp(filtre.PageSize, 1, 250);
        return new() { Items = items.OrderByDescending(x => x.Gun).ThenBy(x => x.IsKaydiId).Skip((page - 1) * size).Take(size).ToArray(),
            Toplamlar = items.GroupBy(x => x.ParaBirimi).Select(x => new FinansParaToplamiModel(x.Key, x.Sum(y => y.KalanNetTutar ?? 0), 0, x.Sum(y => y.KalanNetTutar ?? 0))).ToArray(),
            PageNumber = page, PageSize = size, TotalCount = items.Count };
    }
}
