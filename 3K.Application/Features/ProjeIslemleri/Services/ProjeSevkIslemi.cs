using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.ProjeIslemleri.Services;

/// <summary>Doğrulanmış proje sevki: proje, bütün sandıkları, saha aktarım durumu ve sevk edilecek sandıklar.</summary>
internal sealed class ProjeSevkHazirligi
{
    public required Proje Proje { get; init; }
    public required int EskiDurum { get; init; }
    public required List<Sandik> Sandiklar { get; init; }
    public required KaynakSandikSahaAktarimDurumu SahaDurumu { get; init; }
    public required List<Sandik> SevkEdilecekSandiklar { get; init; }
}

/// <summary>Doğrulama sonucu: hazırlık ya da hata; hata belirli sandıklardan kaynaklanıyorsa onların kimlikleri.</summary>
internal sealed record ProjeSevkKontrolu(ProjeSevkHazirligi? Hazirlik, Result? Hata, IReadOnlyList<int> SorunluSandikIds)
{
    public static ProjeSevkKontrolu Basarisiz(Result hata, IEnumerable<int>? sandikIds = null) =>
        new(null, hata, sandikIds?.ToArray() ?? []);
}

internal sealed record ProjeSevkSonucu(Sevkiyat? Sevkiyat, SevkSeferi? Sefer, int SandikSayisi);

/// <summary>
/// Proje sevkinin ortak motoru: tekil proje sevki ve çok projeli toplu sevk aynı kuralları kullanır. Doğrulama
/// (<see cref="HazirlaAsync"/>) veri değiştirmez; uygulama (<see cref="UygulaAsync"/>) çağıranın transaction'ında çalışır.
/// </summary>
internal sealed class ProjeSevkIslemi(
    IUnitOfWork unitOfWork,
    IHareketService hareketService,
    ISahaTamamlamaService sahaTamamlamaService,
    IReadQueryExecutor readQueries)
{
    public async Task<ProjeSevkKontrolu> HazirlaAsync(int projeId, IReadOnlyCollection<int>? sandikIds, CancellationToken cancellationToken)
    {
        var proje = await unitOfWork.GetRepository<Proje>().GetByIdAsync(projeId);
        if (proje == null)
            return ProjeSevkKontrolu.Basarisiz(Result.Failure("Proje bulunamadı."));

        var eskiDurum = proje.DurumId;
        var sandiklar = (await unitOfWork.GetRepository<Sandik>().FindAsync(s => s.ProjeId == projeId)).ToList();

        var sahaDurumu = proje.ProjeTipiId == (int)ProjeTipi.Normal
            ? await sahaTamamlamaService.GetKaynakSandikSahaAktarimDurumuAsync(sandiklar.Select(s => s.Id), cancellationToken)
            : new KaynakSandikSahaAktarimDurumu();
        var aktifSahaAktarimliSandikIds = sahaDurumu.AktifAktarimaBagliSandikIds;

        if (sandiklar.Count == 0)
            return ProjeSevkKontrolu.Basarisiz(Result.Failure("Projeye ait sandık bulunamadı."));

        if (proje.DurumId == (int)ProjeDurum.SevkEdildi)
            return ProjeSevkKontrolu.Basarisiz(Result.Failure(
                "Proje zaten sevk edilmiş durumda. Düzeltmeye açık sandıklar için Düzeltmeyi Tamamla işlemini kullanın."));

        var secilenSandikIds = sandikIds?
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        List<Sandik> sevkEdilecekSandiklar;
        if (secilenSandikIds is { Count: > 0 })
        {
            var secilenSandiklar = sandiklar.Where(s => secilenSandikIds.Contains(s.Id)).ToList();
            if (secilenSandiklar.Count != secilenSandikIds.Count)
                return ProjeSevkKontrolu.Basarisiz(Result.Failure("Seçilen sandıklardan bazıları bu projeye ait değil."),
                    secilenSandikIds.Except(secilenSandiklar.Select(s => s.Id)));

            var sevkli = secilenSandiklar.Where(s => s.DurumId == (int)SandikDurum.Sevkedildi).Select(s => s.Id).ToList();
            if (sevkli.Count > 0)
                return ProjeSevkKontrolu.Basarisiz(Result.Failure(
                    "Seçilen sandıklardan bazıları zaten sevk edilmiş. Düzeltmeye açık sandıklar için Düzeltmeyi Tamamla işlemini kullanın."), sevkli);

            var sahada = secilenSandiklar.Where(s => aktifSahaAktarimliSandikIds.Contains(s.Id)).Select(s => s.Id).ToList();
            if (sahada.Count > 0)
                return ProjeSevkKontrolu.Basarisiz(Result.Failure(
                    "Seçilen sandıklardan bazıları aktif saha aktarımına bağlı. Bu sandıklar ana projeden yeniden sevk edilemez; gerekirse aktarımı saha projesinden geri alın.", 409), sahada);

            var hazirOlmayan = secilenSandiklar.Where(s => !SandikSevkKilidiHelper.SandikSevkeHazirMi(s)).Select(s => s.Id).ToList();
            if (hazirOlmayan.Count > 0)
                return ProjeSevkKontrolu.Basarisiz(Result.Failure(SandikSevkKilidiHelper.SandikSevkeHazirDegilMesaji, 409), hazirOlmayan);

            sevkEdilecekSandiklar = secilenSandiklar;
        }
        else
        {
            var sevkeHazirOlmayanAdaylar = sandiklar.Where(s =>
                    s.DurumId != (int)SandikDurum.Sevkedildi &&
                    !aktifSahaAktarimliSandikIds.Contains(s.Id) &&
                    !SandikSevkKilidiHelper.SandikSevkeHazirMi(s))
                .Select(s => s.Id)
                .ToList();

            if (sevkeHazirOlmayanAdaylar.Count > 0)
                return ProjeSevkKontrolu.Basarisiz(Result.Failure(SandikSevkKilidiHelper.SandikSevkeHazirDegilMesaji, 409), sevkeHazirOlmayanAdaylar);

            sevkEdilecekSandiklar = sandiklar
                .Where(s =>
                    SandikSevkKilidiHelper.SandikSevkeHazirMi(s) &&
                    !aktifSahaAktarimliSandikIds.Contains(s.Id))
                .ToList();
        }

        if (sevkEdilecekSandiklar.Count == 0)
            return ProjeSevkKontrolu.Basarisiz(Result.Failure("Sevk edilecek sandık bulunamadı."));

        return new ProjeSevkKontrolu(new ProjeSevkHazirligi
        {
            Proje = proje,
            EskiDurum = eskiDurum,
            Sandiklar = sandiklar,
            SahaDurumu = sahaDurumu,
            SevkEdilecekSandiklar = sevkEdilecekSandiklar
        }, null, []);
    }

    /// <summary>
    /// Sandıkları sevk eder, yeni sevk edilen sandıklar için proje sevkiyatı yazar ve proje durumunu günceller.
    /// <paramref name="sefer"/> verilmezse (tekil sevk) yeni sevkiyat tek projelik bir sefere bağlanır.
    /// <paramref name="hareketEki"/> hareket açıklamasının sonuna eklenir (toplu sevkte sefer no ve plaka).
    /// </summary>
    public async Task<ProjeSevkSonucu> UygulaAsync(
        ProjeSevkHazirligi hazirlik,
        DateTime sevkTarihi,
        string? aciklama,
        string? aracPlaka,
        SevkSeferi? sefer,
        int kullaniciId,
        string? hareketEki,
        CancellationToken cancellationToken)
    {
        var projeRepo = unitOfWork.GetRepository<Proje>();
        var sandikRepo = unitOfWork.GetRepository<Sandik>();
        var proje = hazirlik.Proje;
        var sevkEdilecekSandikIdleri = hazirlik.SevkEdilecekSandiklar.Select(s => s.Id).ToList();

        var mevcutSevkiyatBagliSandikIdleri = (await unitOfWork.GetRepository<SevkiyatSandik>().FindAsync(ss =>
                sevkEdilecekSandikIdleri.Contains(ss.SandikId)))
            .Select(ss => ss.SandikId)
            .ToHashSet();

        foreach (var sandik in hazirlik.SevkEdilecekSandiklar)
        {
            sandik.SevkOncesiDurumId ??= sandik.DurumId;
            sandik.DurumId = (int)SandikDurum.Sevkedildi;
            sandik.SevkiyatDuzeltmeAcikMi = false;
            sandikRepo.Update(sandik);
        }

        var yeniSevkiyatSandiklari = hazirlik.SevkEdilecekSandiklar
            .Where(s => !mevcutSevkiyatBagliSandikIdleri.Contains(s.Id))
            .ToList();
        var duzeltmeSonrasiKilitlenenSayisi = hazirlik.SevkEdilecekSandiklar.Count - yeniSevkiyatSandiklari.Count;

        Sevkiyat? sevkiyat = null;
        SevkSeferi? yeniTekilSefer = null;
        if (yeniSevkiyatSandiklari.Count > 0)
        {
            if (sefer is null)
            {
                yeniTekilSefer = YeniSefer(await SeferNoUretAsync(cancellationToken), sevkTarihi, aracPlaka, null, null,
                    aciklama, kullaniciId, null, null);
                await unitOfWork.GetRepository<SevkSeferi>().AddAsync(yeniTekilSefer);
            }
            sevkiyat = await SevkiyatKayitHelper.OlusturAsync(
                unitOfWork, proje.Id, yeniSevkiyatSandiklari, sevkTarihi, aciklama, aracPlaka, kullaniciId, sefer ?? yeniTekilSefer);
        }

        if (proje.ProjeTipiId == (int)ProjeTipi.Normal)
        {
            proje.DurumId = await NormalProjeSevkDurumHesaplayici.HesaplaAsync(
                unitOfWork, readQueries, sahaTamamlamaService,
                proje.Id, proje.DurumId, hazirlik.Sandiklar,
                hazirlik.SahaDurumu.SahaUzerindenSevkEdilenSandikIds,
                cancellationToken);
        }
        else
        {
            proje.DurumId = ProjeSevkDurumHelper.Hesapla(hazirlik.Sandiklar, proje.DurumId);
        }
        proje.GerceklesenSevkTarihi ??= sevkTarihi;
        projeRepo.Update(proje);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (proje.ProjeTipiId == (int)ProjeTipi.Saha)
        {
            await sahaTamamlamaService.SenkronizeKaynakProjelerBySahaSandikIdsAsync(
                sevkEdilecekSandikIdleri,
                cancellationToken);
        }

        var hareketAciklamasi = SevkAciklamasi(
            hazirlik.SevkEdilecekSandiklar.Count, yeniSevkiyatSandiklari.Count, duzeltmeSonrasiKilitlenenSayisi, sevkiyat);
        await hareketService.HareketKaydetAsync(new HareketGecmisi
        {
            ProjeId = proje.Id,
            KullaniciId = kullaniciId,
            ReferansTipi = "Proje",
            ReferansId = proje.Id.ToString(),
            Islem = "Proje Sevk Edildi",
            IslemTipiId = (int)IslemTipi.ProjeSevkEdildi,
            EskiDeger = hazirlik.EskiDurum.ToString(),
            YeniDeger = proje.DurumId.ToString(),
            Aciklama = string.IsNullOrWhiteSpace(hareketEki) ? hareketAciklamasi : $"{hareketAciklamasi} {hareketEki}"
        });

        return new ProjeSevkSonucu(sevkiyat, sefer ?? yeniTekilSefer, hazirlik.SevkEdilecekSandiklar.Count);
    }

    /// <summary>Yeni sefer başlığı; plakalar normalize edilir.</summary>
    public static SevkSeferi YeniSefer(
        string seferNo, DateTime sevkTarihi, string? aracPlaka, string? sofor, string? dorsePlaka, string? aciklama,
        int kullaniciId, Guid? islemAnahtari, string? istekHash) => new()
    {
        SeferNo = seferNo,
        AracPlaka = AracPlakaKurali.Normalize(aracPlaka),
        Sofor = string.IsNullOrWhiteSpace(sofor) ? null : sofor.Trim(),
        DorsePlaka = AracPlakaKurali.Normalize(dorsePlaka),
        Aciklama = string.IsNullOrWhiteSpace(aciklama) ? null : aciklama.Trim(),
        SevkTarihi = sevkTarihi,
        KullaniciId = kullaniciId,
        IslemAnahtari = islemAnahtari,
        IstekHash = istekHash
    };

    /// <summary>
    /// Bu yılın sıradaki sefer numarası (transaction içinde okunur). Eşzamanlı iki sefer aynı numarayı alırsa benzersiz
    /// indeks ikincisini reddeder ve işlem bütünüyle geri alınır.
    /// </summary>
    public async Task<string> SeferNoUretAsync(CancellationToken cancellationToken)
    {
        var yil = TurkeyTime.Now.Year;
        var onek = $"{yil}-";
        var son = await readQueries.ToListAsync(unitOfWork.GetRepository<SevkSeferi>().Queryable()
            .Where(x => x.SeferNo.StartsWith(onek))
            .OrderByDescending(x => x.SeferNo)
            .Select(x => x.SeferNo)
            .Take(1), cancellationToken);
        return SevkSeferiKurali.SonrakiSeferNo(yil, son.FirstOrDefault());
    }

    private static string SevkAciklamasi(int toplamSandik, int yeniSevkSandikSayisi, int duzeltmeSonrasiKilitlenenSayisi, Sevkiyat? sevkiyat)
    {
        if (yeniSevkSandikSayisi > 0 && sevkiyat != null && duzeltmeSonrasiKilitlenenSayisi > 0)
        {
            return $"Proje sevk işlemi yapıldı. {yeniSevkSandikSayisi} sandık {sevkiyat.SevkiyatNo}. sevkiyat ile sevk edildi, {duzeltmeSonrasiKilitlenenSayisi} sandık mevcut sevkiyat kaydı korunarak yeniden kilitlendi.";
        }

        if (yeniSevkSandikSayisi > 0 && sevkiyat != null)
            return $"Proje sevk işlemi yapıldı. {toplamSandik} sandık {sevkiyat.SevkiyatNo}. sevkiyat ile sevk edildi.";

        return $"Sevkiyat düzeltmesi tamamlandı. {duzeltmeSonrasiKilitlenenSayisi} sandık mevcut sevkiyat kaydı korunarak yeniden kilitlendi.";
    }
}
