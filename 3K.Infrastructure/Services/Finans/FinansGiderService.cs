using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;
using _3K.Core.Exceptions;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;

namespace _3K.Infrastructure.Services.Finans;

/// <summary>Gider kayıtları: KDV hariç/dahil tutar hesabı, avans mahsubu kuralları ve iptal.</summary>
internal sealed class FinansGiderService(
    AppDbContext context,
    FinansIslemYurutucu islem,
    FinansDenetimKaydi denetim) : IFinansGiderService
{
    public async Task<FinansSayfaliSonuc<FinansGiderModel>> GiderlerAsync(
        FinansListeFiltre filtre,
        CancellationToken cancellationToken)
    {
        var query = context.Set<FinansGider>()
            .AsNoTracking()
            .Include(x => x.Kategori)
            .Include(x => x.GiderKalemi)
            .Include(x => x.Proje)
            .Include(x => x.FinansSiparis)
            .AsQueryable();
        query = FinansSorgulari.ExpenseFilter(query, filtre);
        var count = await query.CountAsync(cancellationToken);
        var (page, size, skip) = FinansSorgulari.NormalizePagination(
            filtre.PageNumber, filtre.PageSize, count, 250);
        var totals = await query
            .Where(x => !x.IptalEdildi && !x.AvansMi)
            .GroupBy(x => x.ParaBirimi)
            .Select(x => new FinansParaToplamiModel("", 0, 0, 0) { ParaBirimi = x.Key, NetTutar = x.Sum(y => y.Matrah), KdvTutari = x.Sum(y => y.KdvTutari), ToplamTutar = x.Sum(y => y.ToplamTutar) })
            .OrderBy(x => x.ParaBirimi)
            .ToListAsync(cancellationToken);
        var entities = await query.OrderByDescending(x => x.Tarih).ThenByDescending(x => x.Id)
            .Skip(skip).Take(size).ToListAsync(cancellationToken);
        return new FinansSayfaliSonuc<FinansGiderModel>
        {
            Items = entities.Select(FinansModelEslemeleri.MapExpense).ToArray(),
            Toplamlar = totals,
            PageNumber = page,
            PageSize = size,
            TotalCount = count
        };
    }

    public Task<FinansGiderModel> GiderOlusturAsync(FinansGiderKaydetModel model, CancellationToken cancellationToken)
        => islem.AtomikAsync(() => GiderOlusturCoreAsync(model, cancellationToken), cancellationToken);

    private async Task<FinansGiderModel> GiderOlusturCoreAsync(FinansGiderKaydetModel model, CancellationToken cancellationToken)
    {
        await ValidateExpenseReferencesAsync(model, cancellationToken);
        var entity = new FinansGider();
        ApplyExpense(entity, model);
        context.Set<FinansGider>().Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        denetim.Ekle(nameof(FinansGider), entity.Id, "Oluşturma", "*", null, "Gider oluşturuldu");
        await context.SaveChangesAsync(cancellationToken);
        return await GetExpenseAsync(entity.Id, cancellationToken);
    }

    public Task<FinansGiderModel?> GiderGuncelleAsync(int id, FinansGiderKaydetModel model, CancellationToken cancellationToken)
        => islem.AtomikAsync(() => GiderGuncelleCoreAsync(id, model, cancellationToken), cancellationToken);

    private async Task<FinansGiderModel?> GiderGuncelleCoreAsync(int id, FinansGiderKaydetModel model, CancellationToken cancellationToken)
    {
        var entity = await context.Set<FinansGider>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return null;
        if (entity.IptalEdildi) throw IsKuraliException.Cakisma("İptal edilmiş gider güncellenemez.");
        await ValidateExpenseReferencesAsync(model, cancellationToken, entity.FinansSiparisId, entity.ParaBirimi);
        var auditBefore = FinansDenetimKaydi.AnlikGoruntu(entity);
        ApplyExpense(entity, model);
        denetim.DegisiklikleriEkle(nameof(FinansGider), entity, auditBefore);
        await context.SaveChangesAsync(cancellationToken);
        return await GetExpenseAsync(id, cancellationToken);
    }

    public Task<bool> GiderIptalAsync(int id, string aciklama, CancellationToken cancellationToken)
        => islem.AtomikAsync(() => GiderIptalCoreAsync(id, aciklama, cancellationToken), cancellationToken);

    private async Task<bool> GiderIptalCoreAsync(int id, string aciklama, CancellationToken cancellationToken)
    {
        FinansGerekceKurali.Dogrula(aciklama);
        var entity = await context.Set<FinansGider>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return false;
        if (entity.IptalEdildi) return true;
        // Mahsubu yapılmış avans iptal edilirse mahsup giderleri dayanaksız kalır (D8).
        if (entity.AvansMi && await context.Set<FinansGider>()
                .AnyAsync(x => x.MahsupEdilenAvansId == id && !x.IptalEdildi, cancellationToken))
            throw IsKuraliException.Cakisma("Bu avansa bağlı aktif mahsup giderleri var; önce mahsup kayıtlarını iptal edin.");
        entity.IptalEdildi = true;
        entity.IptalTarihi = TurkeyTime.Now;
        entity.IptalAciklamasi = aciklama.Trim();
        denetim.Ekle(nameof(FinansGider), id, "İptal", nameof(entity.IptalEdildi), false, true, aciklama);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task ValidateExpenseReferencesAsync(
        FinansGiderKaydetModel model,
        CancellationToken cancellationToken,
        int? mevcutSiparisId = null,
        string? mevcutParaBirimi = null)
    {
        if (model.Miktar <= 0 || model.BirimFiyat < 0 || model.KdvOrani is < 0 or > 100 || !FinansTarihKurallari.GecerliTarih(model.Tarih) ||
            !FinansTarihKurallari.GecerliTarih(model.FinansDonemi) || (model.FinansTarihi.HasValue && !FinansTarihKurallari.GecerliTarih(model.FinansTarihi.Value)))
            throw IsKuraliException.Cakisma("Gider miktarı, fiyatı, KDV oranı ve tarihlerini kontrol edin.");
        await FinansSorgulari.ParaBirimiSecimiAsync(context, model.ParaBirimi, [mevcutParaBirimi], cancellationToken);
        if (model.MahsupEdilenAvansId.HasValue)
        {
            var advance = await context.Set<FinansGider>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == model.MahsupEdilenAvansId, cancellationToken);
            if (model.AvansMi || advance is null || !advance.AvansMi || advance.IptalEdildi || advance.ParaBirimi != FinansParaBirimiKurali.Normalize(model.ParaBirimi) || advance.FirmaVeyaKisi != model.FirmaVeyaKisi?.Trim())
                throw IsKuraliException.Cakisma("Avans mahsubu aynı kişi/para birimindeki aktif avansa bağlanmalıdır; avansın kendisi maliyete dahil edilmez.");
        }
        if (model.ProjeId.HasValue && !string.IsNullOrWhiteSpace(model.ManuelProjeNo))
            throw IsKuraliException.Cakisma("Sistem projesi ile manuel proje aynı anda seçilemez.");
        if (!await context.Set<FinansGiderKategori>().AnyAsync(x => x.Id == model.KategoriId && x.Aktif, cancellationToken))
            throw IsKuraliException.Cakisma("Gider kategorisi bulunamadı veya pasif.");
        if (model.GiderKalemiId.HasValue && !await context.Set<FinansGiderKalemi>()
                .AnyAsync(x => x.Id == model.GiderKalemiId && x.FinansGiderKategoriId == model.KategoriId && x.Aktif, cancellationToken))
            throw IsKuraliException.Cakisma("Gider kalemi kategoriyle eşleşmiyor veya pasif.");
        if (model.ProjeId.HasValue && !await context.Set<Proje>().AnyAsync(x => x.Id == model.ProjeId, cancellationToken))
            throw IsKuraliException.Cakisma("Proje bulunamadı.");
        // Yeni PO bağı yalnız aktif PO'ya kurulabilir; mevcut bağ PO sonradan iptal edilse de korunur.
        if (model.FinansSiparisId.HasValue && model.FinansSiparisId != mevcutSiparisId &&
            !await context.Set<FinansSiparis>().AnyAsync(x => x.Id == model.FinansSiparisId && !x.IptalEdildi, cancellationToken))
            throw IsKuraliException.Cakisma("İlişkilendirilecek PO bulunamadı veya iptal edilmiş.");
    }

    private static void ApplyExpense(FinansGider entity, FinansGiderKaydetModel model)
    {
        var baseAmount = FinansTutarKurallari.Para(model.Miktar * model.BirimFiyat);
        var amount = model.KdvDahil
            ? FinansTutarKurallari.KdvDahilHesapla(baseAmount, model.KdvOrani)
            : FinansTutarKurallari.Hesapla(model.Miktar, model.BirimFiyat, model.KdvOrani);
        entity.Tarih = model.Tarih;
        entity.FinansTarihi = (model.FinansTarihi ?? (FinansTarihKurallari.AyBasi(model.Tarih) == FinansTarihKurallari.AyBasi(model.FinansDonemi) ? model.Tarih : model.FinansDonemi)).Date;
        entity.FinansDonemi = FinansTarihKurallari.AyBasi(entity.FinansTarihi);
        entity.FinansGiderKategoriId = model.KategoriId;
        entity.FinansGiderKalemiId = model.GiderKalemiId;
        entity.AltKategori = model.AltKategori?.Trim();
        entity.FirmaVeyaKisi = model.FirmaVeyaKisi?.Trim();
        entity.Aciklama = model.Aciklama.Trim();
        entity.Miktar = model.Miktar;
        entity.Birim = model.Birim.Trim();
        entity.BirimFiyat = model.BirimFiyat;
        entity.Tutar = baseAmount;
        entity.ParaBirimi = FinansParaBirimiKurali.Normalize(model.ParaBirimi);
        entity.KdvDahil = model.KdvDahil;
        entity.KdvOrani = model.KdvOrani;
        entity.Matrah = amount.Net;
        entity.KdvTutari = amount.Kdv;
        entity.ToplamTutar = amount.Toplam;
        entity.ProjeId = model.ProjeId;
        entity.ManuelProjeNo = model.ManuelProjeNo?.Trim();
        entity.IsTuru = model.IsTuru;
        entity.BelgeNo = model.BelgeNo?.Trim();
        entity.AvansMi = model.AvansMi;
        entity.MahsupEdilenAvansId = model.MahsupEdilenAvansId;
        entity.FinansSiparisId = model.FinansSiparisId;
    }

    private async Task<FinansGiderModel> GetExpenseAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await context.Set<FinansGider>().AsNoTracking()
            .Include(x => x.Kategori).Include(x => x.GiderKalemi).Include(x => x.Proje).Include(x => x.FinansSiparis)
            .FirstAsync(x => x.Id == id, cancellationToken);
        return FinansModelEslemeleri.MapExpense(entity);
    }
}
