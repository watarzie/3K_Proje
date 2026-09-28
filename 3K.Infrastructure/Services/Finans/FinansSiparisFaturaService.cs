using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Exceptions;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;

namespace _3K.Infrastructure.Services.Finans;

/// <summary>Sipariş (PO) ve fatura kayıtları: kapasite kontrollü dağıtım, tutar revizyonu, belge mutabakatı, iptal ve durum güncellemesi.</summary>
internal sealed class FinansSiparisFaturaService(
    AppDbContext context,
    FinansIslemYurutucu islem,
    FinansDenetimKaydi denetim) : IFinansSiparisFaturaService
{
    public async Task<FinansSayfaliSonuc<FinansSiparisModel>> SiparislerAsync(
        FinansListeFiltre filtre,
        CancellationToken cancellationToken)
    {
        var query = SiparisDetayQuery();
        var scopedLines = ScopeOrderLines(context.Set<FinansSiparisKalemi>().AsNoTracking(), filtre);
        query = query.Where(x => x.Kalemler.Any(l => scopedLines.Select(s => s.Id).Contains(l.Id)));
        if (!filtre.IptalEdilenleriDahilEt) query = query.Where(x => !x.IptalEdildi);
        query = ApplyFaturalamaBekleyenFilter(query, filtre.FaturalamaBekleyen);
        if (filtre.Baslangic.HasValue) query = query.Where(x => x.SiparisTarihi >= filtre.Baslangic.Value.Date);
        if (filtre.Bitis.HasValue)
        {
            var end = filtre.Bitis.Value.Date.AddDays(1);
            query = query.Where(x => x.SiparisTarihi < end);
        }
        if (filtre.ProjeId.HasValue) query = query.Where(x => x.Kalemler.Any(y => y.FinansIsKaydi.ProjeId == filtre.ProjeId));
        if (!string.IsNullOrWhiteSpace(filtre.ProjeNo)) query = query.Where(x => x.Kalemler.Any(y => y.FinansIsKaydi.ProjeNo == filtre.ProjeNo));
        if (filtre.IsTuru.HasValue) query = query.Where(x => x.Kalemler.Any(y => y.FinansIsKaydi.IsTuru == filtre.IsTuru));
        if (filtre.Durum.HasValue) query = query.Where(x => x.Kalemler.Any(y => y.FinansIsKaydi.Durum == filtre.Durum));
        if (filtre.SiparisDurumu.HasValue) query = query.Where(x => x.Durum == filtre.SiparisDurumu);
        if (!string.IsNullOrWhiteSpace(filtre.ParaBirimi))
        {
            var currency = filtre.ParaBirimi.Trim().ToUpperInvariant();
            query = query.Where(x => x.Kalemler.Any(y => y.ParaBirimiSnapshot == currency));
        }
        if (!string.IsNullOrWhiteSpace(filtre.PoNumarasi))
        {
            var po = filtre.PoNumarasi.Trim().ToLower();
            query = query.Where(x => x.PoNumarasi.ToLower().Contains(po));
        }
        if (!string.IsNullOrWhiteSpace(filtre.TalepEden))
        {
            var requester = filtre.TalepEden.Trim().ToLower();
            query = query.Where(x => x.Kalemler.Any(y =>
                (y.FinansIsKaydi.TalepEdenKisi != null && y.FinansIsKaydi.TalepEdenKisi.ToLower().Contains(requester)) ||
                (y.FinansIsKaydi.TalepEdenBolum != null && y.FinansIsKaydi.TalepEdenBolum.ToLower().Contains(requester))));
        }
        if (!string.IsNullOrWhiteSpace(filtre.Arama))
        {
            var search = filtre.Arama.Trim().ToLower();
            query = query.Where(x => x.PoNumarasi.ToLower().Contains(search) ||
                                     x.KayitNo.ToLower().Contains(search) ||
                                     x.Kalemler.Any(y => y.FinansIsKaydi.ProjeNo.ToLower().Contains(search)));
        }

        var page = Math.Max(1, filtre.PageNumber);
        var size = Math.Clamp(filtre.PageSize, 1, 250);
        var count = await query.CountAsync(cancellationToken);
        var entities = await query.OrderByDescending(x => x.SiparisTarihi).ThenByDescending(x => x.Id)
            .Skip((page - 1) * size).Take(size).ToListAsync(cancellationToken);
        return new FinansSayfaliSonuc<FinansSiparisModel>
        {
            Items = entities.Select(x => FinansSiparisFaturaEslemeleri.MapSiparis(x, filtre)).ToArray(),
            Toplamlar = await query.Where(x => !x.IptalEdildi).SelectMany(x => x.Kalemler)
                .Where(x => scopedLines.Select(s => s.Id).Contains(x.Id)).GroupBy(x => x.ParaBirimiSnapshot)
                .Select(g => new FinansParaToplamiModel(g.Key, g.Sum(x => x.NetTutarSnapshot), g.Sum(x => x.KdvTutariSnapshot), g.Sum(x => x.ToplamTutarSnapshot))).ToArrayAsync(cancellationToken),
            PageNumber = page,
            PageSize = size,
            TotalCount = count
        };
    }

    internal static IQueryable<FinansSiparis> ApplyFaturalamaBekleyenFilter(
        IQueryable<FinansSiparis> query,
        bool faturalamaBekleyen)
        => !faturalamaBekleyen
            ? query
            : query.Where(x =>
                !x.IptalEdildi &&
                (x.Durum == FinansSiparisDurumu.Acik ||
                 x.Durum == FinansSiparisDurumu.KismiFaturalandi));

    public async Task<FinansSiparisModel?> SiparisGetirAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await SiparisDetayQuery().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity is null ? null : FinansSiparisFaturaEslemeleri.MapSiparis(entity);
    }

    public Task<FinansSiparisModel?> SiparisGuncelleAsync(
        int id,
        FinansSiparisGuncelleModel model,
        CancellationToken cancellationToken)
        => islem.AtomikAsync(() => SiparisGuncelleCoreAsync(id, model, cancellationToken), cancellationToken);

    private async Task<FinansSiparisModel?> SiparisGuncelleCoreAsync(
        int id,
        FinansSiparisGuncelleModel model,
        CancellationToken cancellationToken)
    {
        var entity = await SiparisDetayQuery(true).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return null;
        if (entity.IptalEdildi)
            throw IsKuraliException.Cakisma("İptal edilmiş sipariş güncellenemez.");

        var po = model.PoNumarasi.Trim().ToUpperInvariant();
        if (await context.Set<FinansSiparis>().AnyAsync(x => x.Id != id && x.PoNumarasi == po, cancellationToken))
            throw IsKuraliException.Cakisma("Bu PO numarası daha önce kullanılmış; finansal belge numaraları tekrar kullanılamaz.");

        var auditBefore = FinansDenetimKaydi.AnlikGoruntu(entity);
        entity.PoNumarasi = po;
        entity.SiparisTarihi = model.SiparisTarihi;
        entity.Aciklama = model.Aciklama?.Trim();
        if (model.Kalemler is not null)
        {
            FinansGerekceKurali.Dogrula(model.Gerekce);
            if (model.Kalemler.Count == 0 || model.Kalemler.Select(x => x.IsKaydiId).Distinct().Count() != model.Kalemler.Count)
                throw IsKuraliException.Cakisma("Tekil mevcut PO kalemlerini seçin.");
            foreach (var requested in model.Kalemler)
            {
                var line = entity.Kalemler.SingleOrDefault(x => x.FinansIsKaydiId == requested.IsKaydiId)
                    ?? throw IsKuraliException.Cakisma("Revizyon yalnız bu PO'nun mevcut kalemlerinde yapılabilir.");
                if (!requested.NetTutar.HasValue) throw IsKuraliException.Cakisma("Revizyon net tutarı zorunludur.");
                var work = await context.IsKaydiDetayQuery(true).FirstAsync(x => x.Id == requested.IsKaydiId, cancellationToken);
                var used = work.SiparisKalemleri.Where(x => x.Id != line.Id && !x.FinansSiparis.IptalEdildi).Sum(x => x.NetTutarSnapshot);
                FinansTutarKurallari.Kapasite(requested.NetTutar.Value, used, FinansTutarKurallari.IsNet(work), "PO revizyonu");
                var money = FinansTutarKurallari.Hesapla(1, requested.NetTutar.Value, line.KdvOraniSnapshot);
                var billed = line.FaturaKalemleri.Where(x => !x.FinansFatura.IptalEdildi).ToArray();
                if (billed.Sum(x => x.NetTutarSnapshot) > money.Net || billed.Sum(x => x.KdvTutariSnapshot) > money.Kdv || billed.Sum(x => x.ToplamTutarSnapshot) > money.Toplam)
                    throw IsKuraliException.Cakisma("PO tutarı aktif faturalanan net/KDV/brüt tutarının altına indirilemez.");
                var beforeLine = FinansDenetimKaydi.AnlikGoruntu(line);
                line.NetTutarSnapshot = money.Net; line.KdvTutariSnapshot = money.Kdv; line.ToplamTutarSnapshot = money.Toplam;
                line.TutarBazli = true;
                denetim.DegisiklikleriEkle(nameof(FinansSiparisKalemi), line, beforeLine);
            }
            denetim.Ekle(nameof(FinansSiparis), id, "Tutar Revizyonu", "Gerekçe", null, null, model.Gerekce);
            await context.SaveChangesAsync(cancellationToken);
            RefreshOrderStatus(entity);
            await RefreshWorkStatusesAsync(entity.Kalemler.Select(x => x.FinansIsKaydiId), cancellationToken);
        }
        denetim.DegisiklikleriEkle(nameof(FinansSiparis), entity, auditBefore);
        await context.SaveChangesAsync(cancellationToken);
        return await SiparisGetirAsync(id, cancellationToken);
    }

    public Task<FinansSiparisModel> SiparisOlusturAsync(
        FinansSiparisOlusturModel model,
        CancellationToken cancellationToken)
        => islem.AtomikAsync(() => SiparisOlusturCoreAsync(model, cancellationToken), cancellationToken);

    private async Task<FinansSiparisModel> SiparisOlusturCoreAsync(
        FinansSiparisOlusturModel model,
        CancellationToken cancellationToken)
    {
        var po = model.PoNumarasi.Trim().ToUpperInvariant();
        if (await context.Set<FinansSiparis>().AnyAsync(x => x.PoNumarasi == po, cancellationToken))
            throw IsKuraliException.Cakisma("Bu PO numarası daha önce kullanılmış; finansal belge numaraları tekrar kullanılamaz.");
        var requestedIds = model.Kalemler.Select(x => x.IsKaydiId).Distinct().ToArray();
        if (requestedIds.Length == 0) throw IsKuraliException.Cakisma("Sipariş en az bir iş kalemi içermelidir.");
        if (requestedIds.Length != model.Kalemler.Count)
            throw IsKuraliException.Cakisma("Aynı iş kaydı bir siparişte birden fazla kez dağıtılamaz.");
        var works = await context.IsKaydiDetayQuery(true).Where(x => requestedIds.Contains(x.Id)).ToListAsync(cancellationToken);
        if (works.Count != requestedIds.Length)
            throw IsKuraliException.Cakisma("Sipariş kalemlerinden en az biri bulunamadı.");

        var order = new FinansSiparis
        {
            KayitNo = NewDocumentNo("SIP"),
            PoNumarasi = po,
            SiparisTarihi = model.SiparisTarihi,
            Aciklama = model.Aciklama?.Trim(),
            ParaBirimi = string.IsNullOrWhiteSpace(model.ParaBirimi) ? null : FinansParaBirimiKurali.Normalize(model.ParaBirimi)
        };
        foreach (var requested in model.Kalemler)
        {
            var work = works.Single(x => x.Id == requested.IsKaydiId);
            if (work.IptalEdildi || !work.KaynakAktif)
                throw IsKuraliException.Cakisma($"{work.IsAdi} aktif olmadığı için siparişe eklenemez.");
            var productId = requested.FinansUrunId ?? work.FinansUrunId;
            var pricingUnit = work.FiyatlandirmaBirimiSnapshot;
            var unitPrice = requested.BirimFiyat ?? work.BirimFiyatSnapshot;
            var currency = string.IsNullOrWhiteSpace(requested.ParaBirimi)
                ? work.ParaBirimiSnapshot
                : FinansParaBirimiKurali.Normalize(requested.ParaBirimi);
            var vat = requested.KdvOrani ?? work.KdvOraniSnapshot;
            if (productId != work.FinansUrunId || unitPrice != work.BirimFiyatSnapshot || currency != work.ParaBirimiSnapshot || vat != work.KdvOraniSnapshot)
            {
                throw IsKuraliException.Cakisma("PO işin fiyat snapshot'ını kullanır. Önce ayrı fiyatlandırma işlemiyle iş bedelini düzeltin.");
            }
            if (FinansTutarKurallari.IsNet(work) <= 0)
                throw IsKuraliException.Cakisma($"{work.IsAdi} kalemi için geçerli fiyat bulunamadı.");
            order.ParaBirimi ??= currency;
            if (order.ParaBirimi != currency)
                throw IsKuraliException.Cakisma("Tek PO yalnız aynı para birimindeki işleri içerebilir.");

            var activeLines = work.SiparisKalemleri.Where(x => !x.FinansSiparis.IptalEdildi).ToList();
            if (activeLines.Any(x => x.FiyatlandirmaBirimiSnapshot != pricingUnit))
                throw IsKuraliException.Cakisma("Kısmi siparişte fiyatlandırma birimi değiştirilemez.");
            var distribution = requested.NetTutar.HasValue
                ? new FinansDagitimMiktari(0, 0)
                : FinansMiktarKurallari.DagitimiNormalizeEt(
                pricingUnit,
                requested.Adet,
                requested.M3,
                work.Adet,
                work.ToplamM3,
                activeLines.Sum(x => x.Adet),
                activeLines.Sum(x => x.M3),
                activeLines.Count > 0,
                "Sipariş");
            var pricingQuantity = FinansTutarKurallari.FiyatlandirmaMiktari(pricingUnit, distribution.Adet, distribution.M3);
            var requestedNet = requested.NetTutar ?? FinansTutarKurallari.Hesapla(pricingQuantity, unitPrice, vat).Net;
            FinansTutarKurallari.Kapasite(requestedNet, activeLines.Sum(x => x.NetTutarSnapshot), FinansTutarKurallari.IsNet(work), "Sipariş");
            var money = FinansTutarKurallari.Hesapla(1, requestedNet, vat);
            order.Kalemler.Add(new FinansSiparisKalemi
            {
                FinansIsKaydiId = work.Id,
                FinansIsKaydi = work,
                FinansSiparis = order,
                Adet = distribution.Adet,
                M3 = distribution.M3,
                TutarBazli = requested.NetTutar.HasValue,
                FinansUrunId = productId,
                FiyatlandirmaBirimiSnapshot = pricingUnit,
                BirimFiyatSnapshot = unitPrice,
                ParaBirimiSnapshot = currency,
                KdvOraniSnapshot = vat,
                NetTutarSnapshot = money.Net,
                KdvTutariSnapshot = money.Kdv,
                ToplamTutarSnapshot = money.Toplam
            });
        }

        if (model.BelgeNetTutar.HasValue && model.BelgeNetTutar != order.Kalemler.Sum(x => x.NetTutarSnapshot))
            throw IsKuraliException.Cakisma("PO belge net tutarı satırların net tutar toplamına eşit olmalıdır.");
        context.Set<FinansSiparis>().Add(order);
        await context.SaveChangesAsync(cancellationToken);
        denetim.Ekle(nameof(FinansSiparis), order.Id, "Oluşturma", "*", null, $"PO: {order.PoNumarasi}");
        await RefreshWorkStatusesAsync(requestedIds, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return (await SiparisGetirAsync(order.Id, cancellationToken))!;
    }

    public Task<bool> SiparisIptalAsync(int id, string aciklama, CancellationToken cancellationToken)
        => islem.AtomikAsync(() => SiparisIptalCoreAsync(id, aciklama, cancellationToken), cancellationToken);

    private async Task<bool> SiparisIptalCoreAsync(int id, string aciklama, CancellationToken cancellationToken)
    {
        FinansGerekceKurali.Dogrula(aciklama);
        var entity = await SiparisDetayQuery(true).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return false;
        if (entity.IptalEdildi) return true;
        if (entity.Kalemler.SelectMany(x => x.FaturaKalemleri).Any(x => !x.FinansFatura.IptalEdildi))
            throw IsKuraliException.Cakisma("Aktif faturası bulunan sipariş iptal edilemez. Önce faturaları iptal edin.");
        entity.IptalEdildi = true;
        entity.IptalTarihi = TurkeyTime.Now;
        entity.IptalAciklamasi = aciklama.Trim();
        entity.Durum = FinansSiparisDurumu.IptalEdildi;
        denetim.Ekle(nameof(FinansSiparis), id, "İptal", nameof(entity.IptalEdildi), false, true, aciklama);
        await RefreshWorkStatusesAsync(entity.Kalemler.Select(x => x.FinansIsKaydiId), cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<FinansSayfaliSonuc<FinansFaturaModel>> FaturalarAsync(
        FinansListeFiltre filtre,
        CancellationToken cancellationToken)
    {
        var query = ApplyInvoiceStatusFilter(FaturaDetayQuery(), filtre);
        var scopedLines = ScopeOrderLines(context.Set<FinansSiparisKalemi>().AsNoTracking(), filtre);
        query = query.Where(x => x.Kalemler.Any(l => scopedLines.Select(s => s.Id).Contains(l.FinansSiparisKalemiId)));
        if (!string.IsNullOrWhiteSpace(filtre.FaturaNumarasi)) query = query.Where(x => x.FaturaNumarasi.ToLower().Contains(filtre.FaturaNumarasi.ToLower()));
        if (filtre.Baslangic.HasValue) query = query.Where(x => x.FaturaTarihi >= filtre.Baslangic.Value.Date);
        if (filtre.Bitis.HasValue)
        {
            var end = filtre.Bitis.Value.Date.AddDays(1);
            query = query.Where(x => x.FaturaTarihi < end);
        }
        if (filtre.ProjeId.HasValue) query = query.Where(x => x.Kalemler.Any(y => y.FinansSiparisKalemi.FinansIsKaydi.ProjeId == filtre.ProjeId));
        if (!string.IsNullOrWhiteSpace(filtre.ProjeNo)) query = query.Where(x => x.Kalemler.Any(y => y.FinansSiparisKalemi.FinansIsKaydi.ProjeNo == filtre.ProjeNo));
        if (filtre.IsTuru.HasValue) query = query.Where(x => x.Kalemler.Any(y => y.FinansSiparisKalemi.FinansIsKaydi.IsTuru == filtre.IsTuru));
        if (filtre.Durum.HasValue) query = query.Where(x => x.Kalemler.Any(y => y.FinansSiparisKalemi.FinansIsKaydi.Durum == filtre.Durum));
        if (filtre.SiparisDurumu.HasValue) query = query.Where(x => x.FinansSiparis.Durum == filtre.SiparisDurumu);
        if (!string.IsNullOrWhiteSpace(filtre.ParaBirimi))
        {
            var currency = filtre.ParaBirimi.Trim().ToUpperInvariant();
            query = query.Where(x => x.Kalemler.Any(y => y.FinansSiparisKalemi.ParaBirimiSnapshot == currency));
        }
        if (!string.IsNullOrWhiteSpace(filtre.PoNumarasi))
        {
            var po = filtre.PoNumarasi.Trim().ToLower();
            query = query.Where(x => x.FinansSiparis.PoNumarasi.ToLower().Contains(po));
        }
        if (!string.IsNullOrWhiteSpace(filtre.TalepEden))
        {
            var requester = filtre.TalepEden.Trim().ToLower();
            query = query.Where(x => x.Kalemler.Any(y =>
                (y.FinansSiparisKalemi.FinansIsKaydi.TalepEdenKisi != null && y.FinansSiparisKalemi.FinansIsKaydi.TalepEdenKisi.ToLower().Contains(requester)) ||
                (y.FinansSiparisKalemi.FinansIsKaydi.TalepEdenBolum != null && y.FinansSiparisKalemi.FinansIsKaydi.TalepEdenBolum.ToLower().Contains(requester))));
        }
        if (!string.IsNullOrWhiteSpace(filtre.Arama))
        {
            var search = filtre.Arama.Trim().ToLower();
            query = query.Where(x => x.FaturaNumarasi.ToLower().Contains(search) ||
                                     x.KayitNo.ToLower().Contains(search) ||
                                     x.FinansSiparis.PoNumarasi.ToLower().Contains(search));
        }
        var page = Math.Max(1, filtre.PageNumber);
        var size = Math.Clamp(filtre.PageSize, 1, 250);
        var count = await query.CountAsync(cancellationToken);
        var entities = await query.OrderByDescending(x => x.FaturaTarihi).ThenByDescending(x => x.Id)
            .Skip((page - 1) * size).Take(size).ToListAsync(cancellationToken);
        return new FinansSayfaliSonuc<FinansFaturaModel>
        {
            Items = entities.Select(x => FinansSiparisFaturaEslemeleri.MapFatura(x, filtre)).ToArray(),
            Toplamlar = await FinansSorgulari.BuildInvoiceTotalsQuery(query.Where(x => !x.IptalEdildi).SelectMany(x => x.Kalemler)
                .Where(x => scopedLines.Select(l => l.Id).Contains(x.FinansSiparisKalemiId)))
                .ToArrayAsync(cancellationToken),
            PageNumber = page,
            PageSize = size,
            TotalCount = count
        };
    }

    internal static IQueryable<FinansFatura> ApplyInvoiceStatusFilter(
        IQueryable<FinansFatura> query,
        FinansListeFiltre filtre)
    {
        if (filtre.FaturaDurumu == FinansFaturaDurumu.IptalEdildi)
            return query.Where(x => x.IptalEdildi && x.Durum == FinansFaturaDurumu.IptalEdildi);
        if (filtre.FaturaDurumu == FinansFaturaDurumu.Aktif)
            return query.Where(x => !x.IptalEdildi && x.Durum == FinansFaturaDurumu.Aktif);
        return filtre.IptalEdilenleriDahilEt ? query : query.Where(x => !x.IptalEdildi);
    }

    public async Task<FinansFaturaModel?> FaturaGetirAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await FaturaDetayQuery().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity is null ? null : FinansSiparisFaturaEslemeleri.MapFatura(entity);
    }

    public Task<FinansFaturaModel?> FaturaGuncelleAsync(
        int id,
        FinansFaturaGuncelleModel model,
        CancellationToken cancellationToken)
        => islem.AtomikAsync(() => FaturaGuncelleCoreAsync(id, model, cancellationToken), cancellationToken);

    private async Task<FinansFaturaModel?> FaturaGuncelleCoreAsync(
        int id,
        FinansFaturaGuncelleModel model,
        CancellationToken cancellationToken)
    {
        var entity = await FaturaDetayQuery(true).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return null;
        if (entity.IptalEdildi)
            throw IsKuraliException.Cakisma("İptal edilmiş fatura güncellenemez.");

        var invoiceNo = model.FaturaNumarasi.Trim().ToUpperInvariant();
        if (await context.Set<FinansFatura>().AnyAsync(x => x.Id != id && x.FaturaNumarasi == invoiceNo, cancellationToken))
            throw IsKuraliException.Cakisma("Bu fatura numarası daha önce kullanılmış; finansal belge numaraları tekrar kullanılamaz.");

        var auditBefore = FinansDenetimKaydi.AnlikGoruntu(entity);
        entity.FaturaNumarasi = invoiceNo;
        entity.FaturaTarihi = model.FaturaTarihi;
        entity.Aciklama = model.Aciklama?.Trim();
        if (model.Kalemler is not null)
        {
            FinansGerekceKurali.Dogrula(model.Gerekce);
            if (model.BelgeMutabakatiniKoru) throw IsKuraliException.Cakisma("Kalem revizyonunda eski belge tutarları korunamaz; gerçek belge toplamlarını yeniden girin.");
            if (model.Kalemler.Count == 0 || model.Kalemler.Select(x => x.SiparisKalemiId).Distinct().Count() != model.Kalemler.Count)
                throw IsKuraliException.Cakisma("Tekil mevcut fatura kalemlerini seçin.");
            var order = await SiparisDetayQuery(true).FirstAsync(x => x.Id == entity.FinansSiparisId, cancellationToken);
            if (order.IptalEdildi) throw IsKuraliException.Cakisma("Pasif PO faturası değiştirilemez.");
            foreach (var requested in model.Kalemler)
            {
                var line = entity.Kalemler.SingleOrDefault(x => x.FinansSiparisKalemiId == requested.SiparisKalemiId)
                    ?? throw IsKuraliException.Cakisma("Revizyon yalnız bu faturanın mevcut kalemlerinde yapılabilir.");
                var poLine = order.Kalemler.Single(x => x.Id == line.FinansSiparisKalemiId);
                var others = poLine.FaturaKalemleri.Where(x => x.Id != line.Id && !x.FinansFatura.IptalEdildi).ToArray();
                if (!requested.NetTutar.HasValue) throw IsKuraliException.Cakisma("Revizyon net tutarı zorunludur.");
                var net = requested.NetTutar.Value;
                FinansTutarKurallari.Kapasite(net, others.Sum(x => x.NetTutarSnapshot), poLine.NetTutarSnapshot, "Fatura revizyonu");
                var money = FinansTutarKurallari.FaturaKalemiTutari(net, poLine.KdvOraniSnapshot,
                    poLine.NetTutarSnapshot, poLine.KdvTutariSnapshot, others.Sum(x => x.NetTutarSnapshot), others.Sum(x => x.KdvTutariSnapshot));
                if (money.Kdv < 0 || money.Toplam + others.Sum(x => x.ToplamTutarSnapshot) > poLine.ToplamTutarSnapshot)
                    throw IsKuraliException.Cakisma("Fatura revizyonu PO KDV/brüt bakiyesini aşamaz.");
                var beforeLine = FinansDenetimKaydi.AnlikGoruntu(line);
                line.NetTutarSnapshot = money.Net; line.KdvTutariSnapshot = money.Kdv; line.ToplamTutarSnapshot = money.Toplam; line.TutarBazli = true;
                denetim.DegisiklikleriEkle(nameof(FinansFaturaKalemi), line, beforeLine);
            }
            denetim.Ekle(nameof(FinansFatura), id, "Tutar Revizyonu", "Gerekçe", null, null, model.Gerekce);
            await context.SaveChangesAsync(cancellationToken);
            RefreshOrderStatus(order);
            await RefreshWorkStatusesAsync(order.Kalemler.Select(x => x.FinansIsKaydiId), cancellationToken);
        }
        ApplyInvoiceDocumentReconciliationForUpdate(entity, model);
        denetim.DegisiklikleriEkle(nameof(FinansFatura), entity, auditBefore);
        await context.SaveChangesAsync(cancellationToken);
        return await FaturaGetirAsync(id, cancellationToken);
    }

    public Task<FinansFaturaModel> FaturaOlusturAsync(
        FinansFaturaOlusturModel model,
        CancellationToken cancellationToken)
        => islem.AtomikAsync(() => FaturaOlusturCoreAsync(model, cancellationToken), cancellationToken);

    private async Task<FinansFaturaModel> FaturaOlusturCoreAsync(
        FinansFaturaOlusturModel model,
        CancellationToken cancellationToken)
    {
        var invoiceNo = model.FaturaNumarasi.Trim().ToUpperInvariant();
        if (await context.Set<FinansFatura>().AnyAsync(x => x.FaturaNumarasi == invoiceNo, cancellationToken))
            throw IsKuraliException.Cakisma("Bu fatura numarası daha önce kullanılmış; finansal belge numaraları tekrar kullanılamaz.");
        var order = await SiparisDetayQuery(true).FirstOrDefaultAsync(x => x.Id == model.SiparisId, cancellationToken)
            ?? throw IsKuraliException.Cakisma("Sipariş bulunamadı.");
        if (order.IptalEdildi) throw IsKuraliException.Cakisma("İptal edilmiş sipariş faturalanamaz.");
        if (order.Kalemler.Select(x => x.ParaBirimiSnapshot).Distinct().Count() != 1)
            throw IsKuraliException.Cakisma("Fatura oluşturmak için PO satırlarının para birimleri aynı olmalıdır.");
        var requestedIds = model.Kalemler.Select(x => x.SiparisKalemiId).Distinct().ToArray();
        if (requestedIds.Length != model.Kalemler.Count)
            throw IsKuraliException.Cakisma("Aynı sipariş kalemi faturada birden fazla kez kullanılamaz.");
        if (model.Kalemler.Count == 0) throw IsKuraliException.Cakisma("En az bir fatura kalemi seçilmelidir.");

        var invoice = new FinansFatura
        {
            FinansSiparisId = order.Id,
            KayitNo = NewDocumentNo("FAT"),
            FaturaNumarasi = invoiceNo,
            FaturaTarihi = model.FaturaTarihi,
            Aciklama = model.Aciklama?.Trim()
        };
        foreach (var requested in model.Kalemler)
        {
            var line = order.Kalemler.FirstOrDefault(x => x.Id == requested.SiparisKalemiId)
                ?? throw IsKuraliException.Cakisma("Fatura kalemi siparişe ait değil.");
            var activeInvoiceLines = line.FaturaKalemleri.Where(x => !x.FinansFatura.IptalEdildi).ToList();
            var distribution = requested.NetTutar.HasValue
                ? new FinansDagitimMiktari(0, 0)
                : FinansMiktarKurallari.DagitimiNormalizeEt(
                line.FiyatlandirmaBirimiSnapshot,
                requested.Adet,
                requested.M3,
                line.Adet,
                line.M3,
                activeInvoiceLines.Sum(x => x.Adet),
                activeInvoiceLines.Sum(x => x.M3),
                activeInvoiceLines.Count > 0,
                "Fatura");
            var pricingQuantity = FinansTutarKurallari.FiyatlandirmaMiktari(line.FiyatlandirmaBirimiSnapshot, distribution.Adet, distribution.M3);
            var requestedNet = requested.NetTutar ?? FinansTutarKurallari.Hesapla(pricingQuantity, line.BirimFiyatSnapshot, line.KdvOraniSnapshot).Net;
            FinansTutarKurallari.Kapasite(requestedNet, activeInvoiceLines.Sum(x => x.NetTutarSnapshot), line.NetTutarSnapshot, "Fatura");
            var money = FinansTutarKurallari.FaturaKalemiTutari(requestedNet, line.KdvOraniSnapshot, line.NetTutarSnapshot,
                line.KdvTutariSnapshot, activeInvoiceLines.Sum(x => x.NetTutarSnapshot), activeInvoiceLines.Sum(x => x.KdvTutariSnapshot));
            if (money.Kdv < 0 || money.Toplam + activeInvoiceLines.Sum(x => x.ToplamTutarSnapshot) > line.ToplamTutarSnapshot)
                throw IsKuraliException.Cakisma("Fatura KDV/brüt tutarı PO kaleminin kalanını aşamaz.");
            invoice.Kalemler.Add(new FinansFaturaKalemi
            {
                FinansSiparisKalemiId = line.Id,
                FinansSiparisKalemi = line,
                FinansFatura = invoice,
                TutarBazli = requested.NetTutar.HasValue,
                Adet = distribution.Adet,
                M3 = distribution.M3,
                NetTutarSnapshot = money.Net,
                KdvTutariSnapshot = money.Kdv,
                ToplamTutarSnapshot = money.Toplam
            });
        }

        ApplyInvoiceDocumentReconciliation(
            invoice,
            model.BelgeParaBirimi,
            model.BelgeNetTutar,
            model.BelgeKdvTutari,
            model.BelgeToplamTutar,
            model.MutabakatAciklamasi);

        context.Set<FinansFatura>().Add(invoice);
        await context.SaveChangesAsync(cancellationToken);
        denetim.Ekle(nameof(FinansFatura), invoice.Id, "Oluşturma", "*", null, $"Fatura: {invoice.FaturaNumarasi}");
        RefreshOrderStatus(order);
        await RefreshWorkStatusesAsync(order.Kalemler.Select(x => x.FinansIsKaydiId), cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return (await FaturaDetayQuery().Where(x => x.Id == invoice.Id).Select(x => x).FirstAsync(cancellationToken)) is { } loaded
            ? FinansSiparisFaturaEslemeleri.MapFatura(loaded)
            : throw IsKuraliException.Cakisma("Fatura oluşturuldu ancak okunamadı.");
    }

    public Task<bool> FaturaIptalAsync(int id, string aciklama, CancellationToken cancellationToken)
        => islem.AtomikAsync(() => FaturaIptalCoreAsync(id, aciklama, cancellationToken), cancellationToken);

    private async Task<bool> FaturaIptalCoreAsync(int id, string aciklama, CancellationToken cancellationToken)
    {
        FinansGerekceKurali.Dogrula(aciklama);
        var invoice = await FaturaDetayQuery(true).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (invoice is null) return false;
        if (invoice.IptalEdildi) return true;
        invoice.IptalEdildi = true;
        invoice.IptalTarihi = TurkeyTime.Now;
        invoice.IptalAciklamasi = aciklama.Trim();
        invoice.Durum = FinansFaturaDurumu.IptalEdildi;
        denetim.Ekle(nameof(FinansFatura), id, "İptal", nameof(invoice.IptalEdildi), false, true, aciklama);
        var order = await SiparisDetayQuery(true).FirstAsync(x => x.Id == invoice.FinansSiparisId, cancellationToken);
        RefreshOrderStatus(order);
        await RefreshWorkStatusesAsync(order.Kalemler.Select(x => x.FinansIsKaydiId), cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private void RefreshOrderStatus(FinansSiparis order)
    {
        var oldStatus = order.Durum;
        if (order.IptalEdildi)
        {
            order.Durum = FinansSiparisDurumu.IptalEdildi;
        }
        else
        {
            var invoiceLines = order.Kalemler.SelectMany(x => x.FaturaKalemleri)
                .Where(x => !x.FinansFatura.IptalEdildi).ToList();
            var anyInvoice = order.Kalemler.Any(line =>
            {
                var lines = invoiceLines.Where(x => x.FinansSiparisKalemiId == line.Id).ToList();
                return lines.Sum(x => x.NetTutarSnapshot) > 0;
            });
            var fullyInvoiced = order.Kalemler.Count > 0 && order.Kalemler.All(line =>
            {
                var lines = invoiceLines.Where(x => x.FinansSiparisKalemiId == line.Id).ToList();
                return line.NetTutarSnapshot > 0 && lines.Sum(x => x.NetTutarSnapshot) >= line.NetTutarSnapshot;
            });
            order.Durum = fullyInvoiced
                ? FinansSiparisDurumu.Faturalandi
                : anyInvoice ? FinansSiparisDurumu.KismiFaturalandi : FinansSiparisDurumu.Acik;
        }
        denetim.DegisiklikEkle(nameof(FinansSiparis), order.Id, nameof(order.Durum), oldStatus, order.Durum);
    }

    private async Task RefreshWorkStatusesAsync(IEnumerable<int> workIds, CancellationToken cancellationToken)
    {
        var ids = workIds.Distinct().ToArray();
        if (ids.Length == 0)
            return;
        var works = await context.IsKaydiDetayQuery(true).Where(x => ids.Contains(x.Id)).ToListAsync(cancellationToken);
        foreach (var work in works)
        {
            var oldStatus = work.Durum;
            work.Durum = FinansTutarKurallari.Durum(work);
            denetim.DegisiklikEkle(nameof(FinansIsKaydi), work.Id, nameof(FinansIsKaydi.Durum), oldStatus, work.Durum);
        }
    }

    private IQueryable<FinansSiparis> SiparisDetayQuery(bool tracking = false)
    {
        var query = context.Set<FinansSiparis>()
            .Include(x => x.Kalemler)
                .ThenInclude(x => x.FinansIsKaydi)
            .Include(x => x.Kalemler)
                .ThenInclude(x => x.FinansUrun)
            .Include(x => x.Kalemler)
                .ThenInclude(x => x.FaturaKalemleri)
                    .ThenInclude(x => x.FinansFatura)
            .AsSplitQuery();

        return tracking ? query : query.AsNoTracking();
    }

    private IQueryable<FinansFatura> FaturaDetayQuery(bool tracking = false)
    {
        var query = context.Set<FinansFatura>()
            .Include(x => x.FinansSiparis)
            .Include(x => x.Kalemler)
                .ThenInclude(x => x.FinansSiparisKalemi)
                    .ThenInclude(x => x.FinansIsKaydi)
            .Include(x => x.Kalemler)
                .ThenInclude(x => x.FinansSiparisKalemi)
                    .ThenInclude(x => x.FinansUrun)
            .AsSplitQuery();

        return tracking ? query : query.AsNoTracking();
    }

    internal static void ApplyInvoiceDocumentReconciliationForUpdate(
        FinansFatura invoice,
        FinansFaturaGuncelleModel model)
    {
        if (model.BelgeMutabakatiniKoru)
            return;

        ApplyInvoiceDocumentReconciliation(
            invoice,
            model.BelgeParaBirimi,
            model.BelgeNetTutar,
            model.BelgeKdvTutari,
            model.BelgeToplamTutar,
            model.MutabakatAciklamasi);
    }

    private static void ApplyInvoiceDocumentReconciliation(
        FinansFatura invoice,
        string? documentCurrency,
        decimal? documentNet,
        decimal? documentVat,
        decimal? documentTotal,
        string? reconciliationNote)
    {
        var hasAnyDocumentValue = !string.IsNullOrWhiteSpace(documentCurrency) ||
                                  documentNet.HasValue ||
                                  documentVat.HasValue ||
                                  documentTotal.HasValue;
        if (!hasAnyDocumentValue)
        {
            invoice.BelgeParaBirimiSnapshot = null;
            invoice.BelgeNetTutarSnapshot = null;
            invoice.BelgeKdvTutariSnapshot = null;
            invoice.BelgeToplamTutarSnapshot = null;
            invoice.MutabakatFarkiSnapshot = 0;
            invoice.MutabakatAciklamasi = null;
            return;
        }

        if (string.IsNullOrWhiteSpace(documentCurrency) ||
            !documentNet.HasValue ||
            !documentVat.HasValue ||
            !documentTotal.HasValue)
            throw IsKuraliException.Cakisma("Belge para birimi, net, KDV ve brüt toplam alanları birlikte girilmelidir.");

        if (documentNet.Value < 0 || documentVat.Value < 0 || documentTotal.Value < 0)
            throw IsKuraliException.Cakisma("Belge tutarları negatif olamaz.");
        if (documentNet.Value + documentVat.Value != documentTotal.Value)
            throw IsKuraliException.Cakisma("Belge net + KDV toplamı brüt toplamla eşleşmelidir.");

        var lineCurrencies = invoice.Kalemler
            .Select(x => FinansParaBirimiKurali.Normalize(x.FinansSiparisKalemi.ParaBirimiSnapshot))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (lineCurrencies.Length != 1)
            throw IsKuraliException.Cakisma("Farklı para birimindeki fatura kalemleri için tek belge toplamı girilemez.");

        var normalizedCurrency = FinansParaBirimiKurali.Normalize(documentCurrency);
        if (!string.Equals(lineCurrencies[0], normalizedCurrency, StringComparison.OrdinalIgnoreCase))
            throw IsKuraliException.Cakisma("Belge para birimi fatura kalemlerinin para birimiyle eşleşmelidir.");

        var calculatedTotal = invoice.Kalemler.Sum(x => x.ToplamTutarSnapshot);
        if (documentNet.Value != invoice.Kalemler.Sum(x => x.NetTutarSnapshot) ||
            documentVat.Value != invoice.Kalemler.Sum(x => x.KdvTutariSnapshot) ||
            documentTotal.Value != calculatedTotal)
            throw IsKuraliException.Cakisma("Belge net/KDV/brüt tutarları satır toplamlarıyla eşleşmelidir. Farkı PO kapasitesini aşmadan satırlara dağıtın.");
        var difference = decimal.Round(documentTotal.Value - calculatedTotal, 2, MidpointRounding.AwayFromZero);
        var normalizedNote = reconciliationNote?.Trim();
        if (Math.Abs(difference) > 0.02m && string.IsNullOrWhiteSpace(normalizedNote))
            throw IsKuraliException.Cakisma("Belge ile hesaplanan brüt toplam arasındaki fark için mutabakat açıklaması zorunludur.");

        invoice.BelgeParaBirimiSnapshot = normalizedCurrency;
        invoice.BelgeNetTutarSnapshot = decimal.Round(documentNet.Value, 2, MidpointRounding.AwayFromZero);
        invoice.BelgeKdvTutariSnapshot = decimal.Round(documentVat.Value, 2, MidpointRounding.AwayFromZero);
        invoice.BelgeToplamTutarSnapshot = decimal.Round(documentTotal.Value, 2, MidpointRounding.AwayFromZero);
        invoice.MutabakatFarkiSnapshot = difference;
        invoice.MutabakatAciklamasi = string.IsNullOrWhiteSpace(normalizedNote) ? null : normalizedNote;
    }

    private static IQueryable<FinansSiparisKalemi> ScopeOrderLines(IQueryable<FinansSiparisKalemi> query, FinansListeFiltre filtre)
    {
        if (filtre.ProjeId.HasValue) query = query.Where(x => x.FinansIsKaydi.ProjeId == filtre.ProjeId);
        if (!string.IsNullOrWhiteSpace(filtre.ProjeNo)) query = query.Where(x => x.FinansIsKaydi.ProjeNo == filtre.ProjeNo);
        if (filtre.IsTuru.HasValue) query = query.Where(x => x.FinansIsKaydi.IsTuru == filtre.IsTuru);
        if (filtre.SandikCinsi.HasValue) query = query.Where(x => x.FinansIsKaydi.SandikCinsi == filtre.SandikCinsi);
        if (!string.IsNullOrWhiteSpace(filtre.Firma)) query = query.Where(x => x.FinansIsKaydi.Musteri.ToLower().Contains(filtre.Firma.ToLower()));
        if (!string.IsNullOrWhiteSpace(filtre.ParaBirimi)) query = query.Where(x => x.ParaBirimiSnapshot == filtre.ParaBirimi.Trim().ToUpper());
        if (!string.IsNullOrWhiteSpace(filtre.FaturaNumarasi)) query = query.Where(x => x.FaturaKalemleri.Any(f => f.FinansFatura.FaturaNumarasi.ToLower().Contains(filtre.FaturaNumarasi.ToLower())));
        return query;
    }

    private static string NewDocumentNo(string prefix)
        => $"{prefix}-{TurkeyTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..Math.Min(31, prefix.Length + 1 + 14 + 1 + 32)];
}
