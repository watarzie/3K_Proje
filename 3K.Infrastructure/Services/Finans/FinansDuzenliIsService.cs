using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Exceptions;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;

namespace _3K.Infrastructure.Services.Finans;

/// <summary>Düzenli işler ve referans tarihine kadarki dönem iş kayıtlarının üretimi.</summary>
internal sealed class FinansDuzenliIsService(
    AppDbContext context,
    FinansIslemYurutucu islem,
    FinansDenetimKaydi denetim,
    FinansFiyatBelirleyici fiyatlar) : IFinansDuzenliIsService
{
    public async Task<FinansSayfaliSonuc<FinansDuzenliIsModel>> DuzenliIslerSayfaliAsync(
        bool sadeceAktif,
        string? arama,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = ApplyRecurringFilter(
            context.Set<FinansDuzenliIs>().AsNoTracking(), sadeceAktif, arama);
        var count = await query.CountAsync(cancellationToken);
        var (page, size, skip) = FinansSorgulari.NormalizePagination(pageNumber, pageSize, count, 100);
        var entities = await query
            .OrderBy(x => x.IsAdi)
            .ThenBy(x => x.Id)
            .Skip(skip)
            .Take(size)
            .ToListAsync(cancellationToken);

        return new FinansSayfaliSonuc<FinansDuzenliIsModel>
        {
            Items = entities.Select(MapRecurring).ToArray(),
            PageNumber = page,
            PageSize = size,
            TotalCount = count
        };
    }

    internal static IQueryable<FinansDuzenliIs> ApplyRecurringFilter(
        IQueryable<FinansDuzenliIs> query,
        bool sadeceAktif,
        string? arama)
    {
        if (sadeceAktif)
            query = query.Where(x => x.Aktif);
        if (string.IsNullOrWhiteSpace(arama))
            return query;

        var search = arama.Trim().ToLower();
        return query.Where(x =>
            x.IsAdi.ToLower().Contains(search) ||
            x.Musteri.ToLower().Contains(search) ||
            x.RaporGrubu.ToLower().Contains(search) ||
            (x.ManuelProjeNo != null && x.ManuelProjeNo.ToLower().Contains(search)) ||
            (x.ManuelProjeAdi != null && x.ManuelProjeAdi.ToLower().Contains(search)) ||
            (x.OzelIsTuru != null && x.OzelIsTuru.ToLower().Contains(search)));
    }

    public Task<FinansDuzenliIsModel> DuzenliIsOlusturAsync(
        FinansDuzenliIsKaydetModel model,
        CancellationToken cancellationToken)
        => islem.AtomikAsync(() => DuzenliIsOlusturCoreAsync(model, cancellationToken), cancellationToken);

    private async Task<FinansDuzenliIsModel> DuzenliIsOlusturCoreAsync(
        FinansDuzenliIsKaydetModel model,
        CancellationToken cancellationToken)
    {
        await ValidateRecurringReferencesAsync(model, cancellationToken);
        var entity = new FinansDuzenliIs();
        ApplyRecurring(entity, model);
        context.Set<FinansDuzenliIs>().Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        denetim.Ekle(nameof(FinansDuzenliIs), entity.Id, "Oluşturma", "*", null, "Düzenli iş oluşturuldu");
        await context.SaveChangesAsync(cancellationToken);
        return MapRecurring(entity);
    }

    public Task<FinansDuzenliIsModel?> DuzenliIsGuncelleAsync(
        int id,
        FinansDuzenliIsKaydetModel model,
        CancellationToken cancellationToken)
        => islem.AtomikAsync(() => DuzenliIsGuncelleCoreAsync(id, model, cancellationToken), cancellationToken);

    private async Task<FinansDuzenliIsModel?> DuzenliIsGuncelleCoreAsync(
        int id,
        FinansDuzenliIsKaydetModel model,
        CancellationToken cancellationToken)
    {
        var entity = await context.Set<FinansDuzenliIs>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return null;
        await ValidateRecurringReferencesAsync(model, cancellationToken);
        var auditBefore = FinansDenetimKaydi.AnlikGoruntu(entity);
        ApplyRecurring(entity, model);
        denetim.DegisiklikleriEkle(nameof(FinansDuzenliIs), entity, auditBefore);
        await context.SaveChangesAsync(cancellationToken);
        return MapRecurring(entity);
    }

    /// <summary>
    /// Aktif düzenli işlerin referans tarihine kadarki dönem kayıtlarını üretir. Her şablon kendi
    /// transaction'ında işlenir: iş kuralına takılan şablon (ör. çakışan tarife) atlanıp sonuçta
    /// raporlanır, diğer şablonların üretimini engellemez. Aynı dönem anahtarı iki kez üretilmez.
    /// </summary>
    public async Task<FinansDonemOlusturSonucModel> DuzenliIsDonemiOlusturAsync(DateTime referansTarihi, CancellationToken cancellationToken)
    {
        if (!FinansTarihKurallari.GecerliTarih(referansTarihi)) throw IsKuraliException.Cakisma("Geçerli referans tarihi girin.");
        var templates = await context.Set<FinansDuzenliIs>().AsNoTracking().Include(x => x.Proje).Include(x => x.FinansUrun)
            .Where(x => x.Aktif && x.BaslangicTarihi <= referansTarihi.Date).OrderBy(x => x.Id).ToListAsync(cancellationToken);
        var created = 0;
        var hatalar = new List<string>();
        foreach (var template in templates)
        {
            try
            {
                created += await islem.AtomikAsync(
                    () => DuzenliIsDonemleriniOlusturAsync(template, referansTarihi, cancellationToken), cancellationToken);
            }
            catch (IsKuraliException exception)
            {
                // Geri alınan şablonun izlenen kayıtları sonraki şablonun kaydına karışmasın.
                context.ChangeTracker.Clear();
                hatalar.Add($"#{template.Id} {template.IsAdi}: {exception.Message}");
            }
        }
        return new FinansDonemOlusturSonucModel(templates.Count, created, referansTarihi) { Hatalar = hatalar };
    }

    private async Task<int> DuzenliIsDonemleriniOlusturAsync(
        FinansDuzenliIs template,
        DateTime referansTarihi,
        CancellationToken cancellationToken)
    {
        var created = 0;
        var final = template.BitisTarihi.HasValue && template.BitisTarihi.Value < referansTarihi ? template.BitisTarihi.Value : referansTarihi;
        foreach (var due in DuzenliIsTarihleri(template.BaslangicTarihi, final, template.OlusturmaGunu))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var period = FinansTarihKurallari.AyBasi(due);
            var sourceId = $"{template.Id}:{period:yyyyMM}";
            if (await context.Set<FinansIsKaydi>().AnyAsync(x => x.KaynakTuru == "DuzenliIs" && x.KaynakKayitId == sourceId, cancellationToken) ||
                await context.Set<FinansKaynakBastirma>().AnyAsync(x => x.KaynakTuru == "DuzenliIs" && x.KaynakKayitId == sourceId, cancellationToken))
                continue;
            var product = template.FinansUrun;
            var pricing = product?.FiyatlandirmaBirimi ?? (template.HesaplamaYontemi is FinansHesaplamaYontemi.SabitAylik or FinansHesaplamaYontemi.DegiskenTutar
                ? FinansFiyatlandirmaBirimi.SabitTutar : template.HesaplamaYontemi == FinansHesaplamaYontemi.Metrekup ? FinansFiyatlandirmaBirimi.Metrekup : FinansFiyatlandirmaBirimi.Adet);
            var quantity = FinansMiktarKurallari.DuzenliIsMiktari(pricing, template.Miktar);
            var tariff = product is null ? null : await fiyatlar.GecerliTarifeAsync(product.Id, due, cancellationToken);
            var work = new FinansIsKaydi
            {
                ProjeId = template.ProjeId, ProjeNo = template.Proje?.ProjeNo ?? template.ManuelProjeNo ?? "BAĞIMSIZ",
                Musteri = template.Proje?.Musteri ?? template.ManuelProjeAdi ?? template.Musteri, ManuelProjeMi = !template.ProjeId.HasValue,
                IsTuru = template.IsTuru, IsAdi = template.IsAdi, OzelIsTuru = template.OzelIsTuru, HesaplamaYontemi = template.HesaplamaYontemi,
                RaporGrubu = template.RaporGrubu, Aciklama = template.Aciklama, Adet = quantity.Adet,
                Birim = pricing == FinansFiyatlandirmaBirimi.Metrekup ? "m³" : template.Birim,
                BirimM3 = quantity.M3, ToplamM3 = quantity.M3, FinansUrunId = template.FinansUrunId,
                UretimTarihi = due, FinansTarihi = due, FinansDonemi = period, KayitTarihi = TurkeyTime.Now,
                KaynakTuru = "DuzenliIs", KaynakKayitId = sourceId, KaynakAktif = true, DuzenliIsId = template.Id,
                FiyatlandirmaBirimiSnapshot = pricing, BirimFiyatSnapshot = tariff?.BirimFiyat ?? template.BirimFiyat,
                ParaBirimiSnapshot = tariff?.ParaBirimi ?? template.ParaBirimi, KdvOraniSnapshot = tariff?.KdvOrani ?? template.KdvOrani,
                TarifeYiliSnapshot = tariff?.Yil, TarifeIdSnapshot = tariff?.Id
            };
            context.Add(work);
            await context.SaveChangesAsync(cancellationToken);
            denetim.Ekle(nameof(FinansIsKaydi), work.Id, "Dönem Oluşturma", "*", null, sourceId);
            created++;
        }
        await context.SaveChangesAsync(cancellationToken);
        return created;
    }

    internal static IReadOnlyList<DateTime> DuzenliIsTarihleri(DateTime start, DateTime end, int day)
    {
        if (day is < 1 or > 31 || !FinansTarihKurallari.GecerliTarih(start) || !FinansTarihKurallari.GecerliTarih(end))
            throw IsKuraliException.Cakisma("Düzenli iş başlangıç/bitiş ve gününü kontrol edin.");
        var result = new List<DateTime>();
        for (var month = FinansTarihKurallari.AyBasi(start); month <= FinansTarihKurallari.AyBasi(end); month = month.AddMonths(1))
        {
            var due = month.AddDays(Math.Min(day, DateTime.DaysInMonth(month.Year, month.Month)) - 1);
            if (due < start.Date) due = start.Date;
            if (due <= end.Date) result.Add(due);
        }
        return result;
    }

    private async Task ValidateRecurringReferencesAsync(FinansDuzenliIsKaydetModel model, CancellationToken cancellationToken)
    {
        await context.ResolveProjectAsync(model.ProjeId, model.ManuelProjeNo, model.ManuelProjeAdi, model.Musteri, cancellationToken);
        if (!model.FinansUrunId.HasValue) return;
        if (!await context.Set<FinansUrun>().AnyAsync(x => x.Id == model.FinansUrunId && x.Aktif, cancellationToken))
            throw IsKuraliException.Cakisma("Seçilen finans ürünü bulunamadı veya pasif.");
    }

    private static void ApplyRecurring(FinansDuzenliIs entity, FinansDuzenliIsKaydetModel model)
    {
        entity.ProjeId = model.ProjeId;
        entity.ManuelProjeNo = model.ManuelProjeNo?.Trim();
        entity.ManuelProjeAdi = model.ManuelProjeAdi?.Trim();
        entity.IsAdi = model.IsAdi.Trim();
        entity.IsTuru = model.IsTuru;
        entity.OzelIsTuru = model.OzelIsTuru?.Trim();
        entity.HesaplamaYontemi = model.HesaplamaYontemi;
        entity.RaporGrubu = model.RaporGrubu.Trim();
        entity.Musteri = model.Musteri.Trim();
        entity.Aciklama = model.Aciklama?.Trim();
        entity.TekrarSikligi = FinansTekrarSikligi.Aylik;
        entity.BaslangicTarihi = model.BaslangicTarihi.Date;
        entity.BitisTarihi = model.BitisTarihi?.Date;
        entity.OlusturmaGunu = model.OlusturmaGunu;
        entity.Miktar = model.Miktar;
        entity.Birim = model.Birim.Trim();
        entity.FinansUrunId = model.FinansUrunId;
        entity.BirimFiyat = model.BirimFiyat;
        entity.ParaBirimi = FinansParaBirimiKurali.Normalize(model.ParaBirimi);
        entity.KdvOrani = model.KdvOrani;
        entity.Aktif = model.Aktif;
    }

    private static FinansDuzenliIsModel MapRecurring(FinansDuzenliIs entity) => new()
    {
        Id = entity.Id,
        ProjeId = entity.ProjeId,
        ManuelProjeNo = entity.ManuelProjeNo,
        IsAdi = entity.IsAdi,
        IsTuru = entity.OzelIsTuru ?? entity.IsTuru.ToString(),
        HesaplamaYontemi = entity.HesaplamaYontemi,
        RaporGrubu = entity.RaporGrubu,
        Musteri = entity.Musteri,
        Aciklama = entity.Aciklama,
        TekrarSikligi = "Aylık",
        BaslangicTarihi = entity.BaslangicTarihi,
        BitisTarihi = entity.BitisTarihi,
        OlusturmaGunu = entity.OlusturmaGunu,
        Miktar = entity.Miktar,
        Birim = entity.Birim,
        FinansUrunId = entity.FinansUrunId,
        BirimFiyat = entity.BirimFiyat,
        ParaBirimi = entity.ParaBirimi,
        KdvOrani = entity.KdvOrani,
        Aktif = entity.Aktif
    };
}
