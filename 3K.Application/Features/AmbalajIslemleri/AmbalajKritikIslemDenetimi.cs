using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.AmbalajIslemleri;

/// <summary>
/// Ambalaj komutlarının kayda bağlı kapılarını uygular: ek yazma izinleri, ilk form şartı ve
/// kritik değişiklik gerekçesi. Onaylanan kritik işlemleri kayıt geçmişine yazar.
/// Handler, değişikliği uygulamadan önce aynı transaction içinde çağırır.
/// </summary>
public sealed class AmbalajKritikIslemDenetimi(IUnitOfWork unitOfWork, IRolService roller, ICurrentUserService kullanici)
{
    private const string FinansKaynakTuru = "AMBALAJURETIM";

    /// <summary>Komutun gerekçe alanı boşsa açıklaması gerekçe sayılır.</summary>
    public static string? GerekceSec(params string?[] adaylar) =>
        adaylar.FirstOrDefault(aday => !string.IsNullOrWhiteSpace(aday));

    /// <summary>Hedef kayıtların form ve aktif sipariş bağlantılarını tek seferde yükleyerek değişiklik tanımı üretir.</summary>
    public IReadOnlyList<AmbalajKayitDegisikligi> DegisiklikleriHazirla(
        IReadOnlyCollection<AmbalajUretimKaydi> kayitlar,
        Func<AmbalajKayitDegisikligi, AmbalajKayitDegisikligi> tanimla)
    {
        if (kayitlar.Count == 0)
            return [];
        var ids = kayitlar.Select(k => k.Id).ToList();
        var formluIds = unitOfWork.GetRepository<AmbalajUretimFormuKaydi>().Queryable()
            .Where(x => ids.Contains(x.AmbalajUretimKaydiId))
            .Select(x => x.AmbalajUretimKaydiId)
            .ToHashSet();
        var akisIdleri = kayitlar.Select(k => k.IsAkisKimligi.ToString("D")).ToList();
        var finansaBagliAkislar = unitOfWork.GetRepository<FinansIsKaydi>().Queryable()
            .Where(f => f.KaynakTuru.ToUpper() == FinansKaynakTuru && akisIdleri.Contains(f.KaynakKayitId!) &&
                        f.SiparisKalemleri.Any(s => !s.FinansSiparis.IptalEdildi))
            .Select(f => f.KaynakKayitId!)
            .ToHashSet();
        return kayitlar
            .Select(k => tanimla(new AmbalajKayitDegisikligi(
                k,
                formluIds.Contains(k.Id),
                finansaBagliAkislar.Contains(k.IsAkisKimligi.ToString("D")))))
            .ToList();
    }

    /// <summary>Projenin ambalaja dahil bütün aktif kayıtları tamamlanmışsa proje tamamlanmış sayılır.</summary>
    public bool ProjeTamamlandiMi(int projeId) =>
        AmbalajUretimPolitikasi.ProjeDurumu(unitOfWork.GetRepository<AmbalajUretimKaydi>().Queryable()
            .Where(k => k.ProjeId == projeId).ToList()) == AmbalajUretimDurumu.Tamamlandi;

    public AmbalajIslemGereksinimi Degerlendir(IEnumerable<AmbalajKayitDegisikligi> degisiklikler)
    {
        var gereksinim = new AmbalajIslemGereksinimi();
        foreach (var degisiklik in degisiklikler)
            AmbalajIslemKurallari.Degerlendir(degisiklik, gereksinim);
        return gereksinim;
    }

    /// <summary>
    /// Eksik izin 403, iş kuralı engeli ve ilk form şartı 409, gerekçesiz kritik işlem 400 döner.
    /// Üretim planı ve üretim formu işlemlerinde gerekçe istenmez (A2, D11); işlem yine kritik
    /// sayılıyorsa geçmişe yazılır.
    /// </summary>
    public async Task<Result> KapilariDenetleAsync(
        AmbalajIslemGereksinimi gereksinim,
        string? gerekce,
        CancellationToken cancellationToken,
        bool gerekceZorunlu = true)
    {
        foreach (var kod in gereksinim.Yetkiler)
        {
            if (!await AmbalajYetkilendirmeYardimcisi.YetkiliMiAsync(roller, kullanici, kod, cancellationToken))
                return Result.Failure($"Bu işlem için gerekli yetkiniz bulunmuyor ({kod}).", 403);
        }
        if (gereksinim.Engel is { } engel)
            return Result.Failure(engel, 409);
        if (gereksinim.IlkFormGerekli)
            return Result.Failure("Bu kayıt için önce Üretim Formu Oluştur işlemini kullanınız.", 409);
        if (gerekceZorunlu && gereksinim.Kritik && string.IsNullOrWhiteSpace(gerekce))
            return Result.Failure("Kritik üretim değişikliği için gerekçe gereklidir.", 400);
        return Result.Success();
    }

    /// <summary>Kritik işlem, etkilenen her kayıt için gerekçesiyle birlikte geçmişe yazılır.</summary>
    public async Task KritikIslemiKaydetAsync(
        AmbalajIslemGereksinimi gereksinim,
        IEnumerable<AmbalajUretimKaydi?> etkilenenKayitlar,
        string islemTuru,
        string? gerekce,
        CancellationToken cancellationToken)
    {
        if (!gereksinim.Kritik)
            return;
        var hareketler = unitOfWork.GetRepository<AmbalajUretimHareketi>();
        foreach (var kayit in etkilenenKayitlar.OfType<AmbalajUretimKaydi>().DistinctBy(k => k.Id))
        {
            await hareketler.AddAsync(new AmbalajUretimHareketi
            {
                AmbalajUretimKaydiId = kayit.Id,
                KullaniciId = kullanici.IslemKullaniciId ?? 0,
                Islem = "Kritik üretim işlemi",
                AlanAdi = "YetkiliRevizyon",
                YeniDeger = islemTuru,
                Aciklama = gerekce
            });
        }
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
