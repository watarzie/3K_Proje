using ClosedXML.Excel;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Exceptions;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Data;

namespace _3K.Infrastructure.Services.Cekiler;

/// <summary>Onaylanan revizyonu uygular: satır ekleme, güncelleme ve silme; işlem ve stok hareketlerinin geri alınması; boşalan sandıkların temizlenmesi.</summary>
internal sealed class CekiRevizyonUygulayici(
    AppDbContext context,
    IDurumHesaplaService durumHesaplaService,
    ISahaAktarimSilmeKorumaService sahaAktarimSilmeKorumaService,
    CekiRevizyonSorgulari sorgular,
    ILogger<CekiRevizyonUygulayici> logger)
{
    internal async Task<CekiRevizyonSonuc> CekiRevizyonunuUygulaAsync(
        Stream excelDosya,
        string dosyaAdi,
        int kullaniciId,
        CancellationToken cancellationToken,
        Action<string> dosyaYoluOlusturuldu)
    {
        var dosya = await CekiRevizyonDosyasi.RevizyonDosyasiniOkuAsync(excelDosya, dosyaAdi, cancellationToken);
        var dosyaBytes = dosya.DosyaBytes;
        var import = dosya.Import;
        var revizyonSatirlari = import.Satirlar
            .Where(s => !string.IsNullOrWhiteSpace(s.CheckKodu))
            .ToList();

        if (revizyonSatirlari.Count == 0)
        {
            CekiRevizyonHatalari.RevizyonValidationHatasiFirlat(
                "Revizyon Excel dosyasında CHECK/KONTROL kolonunda A, U veya D işareti bulunamadı.",
                CekiRevizyonSorunKodlari.IsaretliSatirBulunamadi);
        }

        await using var ownedTransaction = context.Database.CurrentTransaction == null
            ? await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;
        string? dosyaYolu = null;

        try
        {
            var baglam = await sorgular.RevizyonBaglaminiYukleAsync(import);
            var proje = baglam.Proje;
            var anaCeki = baglam.AnaCeki;
            var anaSatirlar = baglam.AnaSatirlar;

            var siraGruplari = anaSatirlar
                .GroupBy(s => s.SiraNo)
                .ToDictionary(g => g.Key, g => g.ToList());

            var sandiklar = await context.Sandiklar
                .Where(s => s.ProjeId == proje.Id)
                .ToListAsync();

            var sandikCache = sandiklar
                .GroupBy(s => CekiExcelOkuyucu.NormalizeKoliNo(s.SandikNo), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", proje.Id.ToString(), "Revizyonlar");
            Directory.CreateDirectory(uploadsDir);
            dosyaYolu = Path.Combine(
                uploadsDir,
                $"{TurkeyTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}_{CekiRevizyonDosyasi.CleanFileName(dosyaAdi)}");
            dosyaYoluOlusturuldu(dosyaYolu);
            await File.WriteAllBytesAsync(dosyaYolu, dosyaBytes, cancellationToken);

            var revizyonCeki = new Ceki
            {
                ProjeId = proje.Id,
                OrijinalDosyaYolu = dosyaYolu,
                YuklemeTarihi = TurkeyTime.Now,
                CekiTipiId = (int)CekiTipi.Revizyon,
                KaynakCekiId = anaCeki.Id,
                Aciklama = $"Revizyon dosyası: {dosyaAdi}"
            };

            context.Cekiler.Add(revizyonCeki);
            await context.SaveChangesAsync(cancellationToken);

            var sonuc = new CekiRevizyonSonuc
            {
                ProjeId = proje.Id,
                ProjeNo = proje.ProjeNo,
                AnaCekiId = anaCeki.Id,
                RevizyonCekiId = revizyonCeki.Id
            };

            var silinecekSatirlar = new List<CekiSatiri>();
            var guncellenecekler = new List<(CekiSatiri Satir, CiktiSatirImportBilgisi RevizyonSatiri)>();
            var eklenecekler = new List<CiktiSatirImportBilgisi>();

            foreach (var revizyonSatiri in revizyonSatirlari)
            {
                var kod = revizyonSatiri.CheckKodu;

                if (kod is "A" or "U" && !revizyonSatiri.VeriSatiriMi)
                {
                    CekiRevizyonHatalari.RevizyonValidationHatasiFirlat(
                        $"Revizyon satırı boş: Excel satırı {revizyonSatiri.ExcelSatirNo}, CHECK={kod}.",
                        CekiRevizyonHatalari.RevizyonSorunuOlustur(
                            revizyonSatiri,
                            CekiRevizyonSorunKodlari.BosSatir,
                            "CHECK işaretli satır boş görünüyor. Barkod veya ürün tanımı girilmelidir."));
                }

                if (kod is "A" or "U" && revizyonSatiri.IstenenAdet <= 0)
                {
                    CekiRevizyonHatalari.RevizyonValidationHatasiFirlat(
                        $"Revizyon satırında miktar geçersiz: Excel satırı {revizyonSatiri.ExcelSatirNo}, CHECK={kod}.",
                        CekiRevizyonHatalari.RevizyonSorunuOlustur(
                            revizyonSatiri,
                            CekiRevizyonSorunKodlari.GecersizMiktar,
                            "Miktar sıfır veya negatif olamaz. Revizyon satırındaki miktarı kontrol edin."));
                }

                if (kod == "A")
                {
                    eklenecekler.Add(revizyonSatiri);
                    continue;
                }

                var mevcutSatir = CekiRevizyonKurallari.EslesenAnaSatiriBul(revizyonSatiri, siraGruplari, anaSatirlar);
                if (mevcutSatir == null)
                {
                    CekiRevizyonHatalari.RevizyonValidationHatasiFirlat(
                        $"Revizyon {kod} satırı ana çekide bulunamadı. Excel satırı: {revizyonSatiri.ExcelSatirNo}, Sıra No: {revizyonSatiri.SiraNo}.",
                        CekiRevizyonHatalari.RevizyonSorunuOlustur(
                            revizyonSatiri,
                            CekiRevizyonSorunKodlari.AnaSatirBulunamadi,
                            $"Ana çekide eşleşen satır bulunamadı. Sıra No: {revizyonSatiri.SiraNo}. Barkod, poz ve sandık bilgilerini kontrol edin."));
                }

                if (kod == "U")
                {
                    guncellenecekler.Add((mevcutSatir, revizyonSatiri));
                }
                else if (kod == "D")
                {
                    silinecekSatirlar.Add(mevcutSatir);
                }
            }

            // Onay/ön izleme sonrasında oluşan saha ilişkisini de yeniden doğrula.
            // Hiçbir U satırının hareketi geri alınmadan veya hedef sandığı oluşturulmadan
            // önce bütün kaynak güncellemeleri tek ilişki sorgusuyla kontrol edilir.
            await RevizyonKaynakSahaGuncellemeleriniDogrulaAsync(
                proje.ProjeTipiId,
                guncellenecekler.Select(item => item.Satir).ToList(),
                cancellationToken);

            var etkilenenSandikIds = silinecekSatirlar
                .Concat(guncellenecekler.Select(item => item.Satir))
                .SelectMany(satir => satir.SandikIcerikleri)
                .Select(icerik => icerik.SandikId)
                .Where(sandikId => sandikId > 0)
                .ToHashSet();
            var hedefSandikNolari = eklenecekler
                .Concat(guncellenecekler.Select(item => item.RevizyonSatiri))
                .Select(satir => CekiExcelOkuyucu.NormalizeKoliNo(satir.KoliNo))
                .Where(sandikNo => !string.IsNullOrWhiteSpace(sandikNo))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (silinecekSatirlar.Count > 0)
                sonuc.SilinenSatirSayisi = await RevizyonSatirlariniSilAsync(proje.Id, silinecekSatirlar.DistinctBy(s => s.Id).ToList(), kullaniciId);

            foreach (var item in guncellenecekler)
            {
                if (CekiRevizyonKurallari.CekiSatiriIslemGormusMu(item.Satir))
                    await RevizyonSatiriIslemleriniGeriAlAsync(proje.Id, item.Satir, kullaniciId, $"Revizyon U satırı uygulanmadan önce otomatik geri alındı. Excel satırı: {item.RevizyonSatiri.ExcelSatirNo}");

                await RevizyonSatiriniGuncelleAsync(proje.Id, item.Satir, item.RevizyonSatiri, sandikCache, import.SandikBilgileri, kullaniciId);
                sonuc.GuncellenenSatirSayisi++;
            }

            foreach (var revizyonSatiri in eklenecekler)
            {
                await RevizyonSatiriEkleAsync(proje.Id, anaCeki.Id, revizyonSatiri, sandikCache, import.SandikBilgileri, kullaniciId);
                sonuc.EklenenSatirSayisi++;
            }

            context.HareketGecmisleri.Add(new HareketGecmisi
            {
                ProjeId = proje.Id,
                ReferansTipi = "Ceki",
                ReferansId = anaCeki.Id.ToString(),
                ReferansMetni = $"Çeki Revizyonu - {dosyaAdi}",
                Islem = "Çeki Revizyonu Uygulandı",
                KullaniciId = kullaniciId,
                Tarih = TurkeyTime.Now,
                EskiDeger = anaCeki.Id.ToString(),
                YeniDeger = revizyonCeki.Id.ToString(),
                Aciklama = $"Eklenen: {sonuc.EklenenSatirSayisi}, Güncellenen: {sonuc.GuncellenenSatirSayisi}, Silinen: {sonuc.SilinenSatirSayisi}"
            });

            await context.SaveChangesAsync(cancellationToken);
            foreach (var hedefSandikNo in hedefSandikNolari)
            {
                if (sandikCache.TryGetValue(hedefSandikNo, out var hedefSandik) &&
                    hedefSandik.Id > 0)
                {
                    etkilenenSandikIds.Add(hedefSandik.Id);
                }
            }

            await BosSandiklariTemizleVeDurumlariGuncelleAsync(
                proje.Id,
                etkilenenSandikIds);
            await context.SaveChangesAsync(cancellationToken);
            if (ownedTransaction != null)
                await ownedTransaction.CommitAsync(cancellationToken);

            sonuc.Mesaj = $"{proje.ProjeNo} revizyonu uygulandı. Eklenen: {sonuc.EklenenSatirSayisi}, Güncellenen: {sonuc.GuncellenenSatirSayisi}, Silinen: {sonuc.SilinenSatirSayisi}.";
            return sonuc;
        }
        catch (Exception exception)
        {
            try
            {
                if (ownedTransaction != null)
                    await ownedTransaction.RollbackAsync(CancellationToken.None);
            }
            catch (Exception rollbackException)
            {
                logger.LogError(
                    rollbackException,
                    "Çeki revizyonu transaction geri alma işlemi başarısız oldu. Dosya: {DosyaAdi}",
                    dosyaAdi);
            }

            CekiRevizyonDosyasi.RevizyonDosyasiniGuvenleSil(logger, dosyaYolu);

            if (RevizyonEszamanliDegisiklikMi(exception))
            {
                const string mesaj = "Proje verileri başka bir işlem tarafından değiştirildi. Ön izlemeyi yenileyip tekrar deneyin.";
                throw new CekiRevizyonConflictException(
                    mesaj,
                    new[]
                    {
                        CekiRevizyonHatalari.GenelRevizyonSorunuOlustur(
                            CekiRevizyonSorunKodlari.EszamanliDegisiklik,
                            mesaj,
                            CekiRevizyonSorunKategorileri.DurumCakismasi)
                    },
                    exception);
            }

            throw;
        }
    }

    private static bool RevizyonEszamanliDegisiklikMi(Exception exception)
    {
        if (exception is DbUpdateConcurrencyException or ConcurrencyConflictException)
            return true;

        var postgresException = exception as PostgresException
            ?? (exception as DbUpdateException)?.InnerException as PostgresException;

        return postgresException?.SqlState is
            PostgresErrorCodes.SerializationFailure or
            PostgresErrorCodes.DeadlockDetected;
    }

    internal async Task RevizyonKaynakSahaGuncellemeleriniDogrulaAsync(
        int projeTipiId,
        IReadOnlyCollection<CekiSatiri> guncellenecekSatirlar,
        CancellationToken cancellationToken)
    {
        var normalKaynakSatirIds = CekiRevizyonKurallari.RevizyonNormalKaynakSatirIds(projeTipiId, guncellenecekSatirlar);
        if (normalKaynakSatirIds.Count == 0)
            return;

        var aktifAktarimBagliSatirIds = await sahaAktarimSilmeKorumaService
            .GetAktifAktarimBagliCekiSatiriIdsAsync(normalKaynakSatirIds, cancellationToken);
        var sorunlar = guncellenecekSatirlar
            .Where(s => normalKaynakSatirIds.Contains(s.Id) && aktifAktarimBagliSatirIds.Contains(s.Id))
            .DistinctBy(s => s.Id)
            .Select(s => CekiRevizyonHatalari.RevizyonSorunuOlustur(
                s,
                CekiRevizyonSorunKodlari.AktifSahaAktarimi,
                CekiRevizyonKurallari.RevizyonAktifSahaKaynakGuncellemeMesaji))
            .ToList();

        if (sorunlar.Count > 0)
            throw new CekiRevizyonConflictException(CekiRevizyonKurallari.RevizyonAktifSahaKaynakGuncellemeMesaji, sorunlar);
    }

    internal async Task RevizyonSatiriIslemleriniGeriAlAsync(int projeId, CekiSatiri satir, int kullaniciId, string aciklama)
    {
        var engel = await sorgular.RevizyonOtomatikGeriAlEngelMesajiAsync(satir);
        if (!string.IsNullOrWhiteSpace(engel))
        {
            var kod = engel.StartsWith("Satır sevk edilmiş", StringComparison.Ordinal)
                ? CekiRevizyonSorunKodlari.SevkKilidi
                : CekiRevizyonSorunKodlari.AktifProjeTransferi;
            throw new CekiRevizyonConflictException(
                $"Revizyon satırı otomatik geri alınamadı. Sıra No: {satir.SiraNo}. {engel}",
                new[] { CekiRevizyonHatalari.RevizyonSorunuOlustur(satir, kod, engel) });
        }

        var transferler = await context.ProjeTransferleri
            .Where(t => t.DurumId == (int)ProjeTransferDurum.Aktif &&
                (t.KaynakCekiSatiriId == satir.Id ||
                 (t.HedefCekiSatiriId.HasValue && t.HedefCekiSatiriId.Value == satir.Id)))
            .ToListAsync();

        var gelenTransferler = transferler
            .Where(t => t.HedefCekiSatiriId.HasValue && t.HedefCekiSatiriId.Value == satir.Id)
            .ToList();

        if (gelenTransferler.Any())
        {
            var kaynakIdler = gelenTransferler.Select(t => t.KaynakCekiSatiriId).Distinct().ToList();
            var kaynakSatirlar = await context.CekiSatirlari
                .Where(s => kaynakIdler.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id);

            foreach (var transfer in gelenTransferler)
            {
                if (kaynakSatirlar.TryGetValue(transfer.KaynakCekiSatiriId, out var kaynakSatir))
                {
                    kaynakSatir.ProjeGonderilen = Math.Max(kaynakSatir.ProjeGonderilen - transfer.Miktar, 0);
                    kaynakSatir.DurumId = durumHesaplaService.HesaplaGenelDurum(kaynakSatir.GridDurumuId, kaynakSatir.UcKDurumuId);
                    durumHesaplaService.HesaplaKalanVeDurum(kaynakSatir);
                    ProjeTransferIadeKurali.DonorYenidenSevkBorcunuDuzelt(kaynakSatir);
                    context.CekiSatirlari.Update(kaynakSatir);
                }

                transfer.DurumId = (int)ProjeTransferDurum.GeriAlindi;
                transfer.IptalTarihi = TurkeyTime.Now;
                transfer.IptalAciklama = "Revizyon uygulaması sırasında satır otomatik geri alındı.";
                context.ProjeTransferleri.Update(transfer);
            }
        }

        var stokHareketleri = await context.StokHareketleri
            .Where(h => h.CekiSatiriId == satir.Id)
            .ToListAsync();
        await RevizyonStokHareketleriniGeriAlAsync(stokHareketleri);

        satir.GridDurumuId = (int)GridDurum.Gelmedi;
        satir.GridGelenAdet = 0;
        satir.TrafoSevkAdet = 0;
        satir.GridSevkDurumuId = (int)GridSevkDurum.SevkEdilmedi;
        satir.GridSevkMiktari = 0;
        satir.AktifGridSevkKarsilananMiktari = null;
        satir.AktifGridSevkPartisiErkenSonuclandirildiMi = null;
        satir.YenidenSevkGerekliAdet = 0;
        satir.GridSevkTarihi = null;
        satir.GridAciklama = null;
        satir.GridPersonelId = null;

        satir.UcKDurumuId = (int)UcKDurum.Bekliyor;
        satir.UcKKarsilamaTipiId = (int)UcKDurum.Bekliyor;
        satir.GelenMiktar = 0;
        satir.TeslimTarihi = null;
        satir.KaynakHedefProjeNo = null;
        satir.UcKAciklama = null;
        satir.KarsilananMiktar = 0;
        satir.StokKarsilanan = 0;
        satir.ProjeKarsilanan = 0;
        satir.ProjeGonderilen = 0;
        satir.TedarikciKarsilanan = 0;
        satir.HataliMiktar = 0;
        satir.GeriGonderilenMiktar = 0;
        satir.GeriGonderilmeSebebiId = null;
        satir.KaynakProjeId = null;
        satir.KaliteDurumId = null;
        satir.SurecDurumId = null;
        satir.PaketleyenId = null;
        satir.KontrolEdenId = null;

        satir.DurumId = durumHesaplaService.HesaplaGenelDurum(satir.GridDurumuId, satir.UcKDurumuId);
        durumHesaplaService.HesaplaKalanVeDurum(satir);

        var icerikler = await context.SandikIcerikleri
            .Where(i => i.CekiSatiriId == satir.Id)
            .ToListAsync();

        foreach (var icerik in icerikler)
        {
            icerik.KonulanAdet = 0;
            icerik.EksikAdet = 0;
            icerik.AktifGridSevkKarsilananMiktari = null;
            icerik.StokKarsilanan = 0;
            icerik.ProjeKarsilanan = 0;
            icerik.TedarikciKarsilanan = 0;
            context.SandikIcerikleri.Update(icerik);
        }

        context.CekiSatirlari.Update(satir);
        context.HareketGecmisleri.Add(new HareketGecmisi
        {
            ProjeId = projeId,
            ReferansTipi = "CekiSatiri",
            ReferansId = satir.Id.ToString(),
            ReferansMetni = $"Sıra: {satir.SiraNo} - {satir.Aciklama}",
            Islem = "Revizyon Öncesi Otomatik Geri Al",
            KullaniciId = kullaniciId,
            Tarih = TurkeyTime.Now,
            YeniDeger = "Bekliyor",
            Aciklama = aciklama
        });
    }

    internal async Task RevizyonSatiriEkleAsync(
        int projeId,
        int anaCekiId,
        CiktiSatirImportBilgisi revizyonSatiri,
        Dictionary<string, Sandik> sandikCache,
        Dictionary<string, SandikImportBilgisi> sandikBilgileri,
        int kullaniciId)
    {
        var sandik = await GetOrCreateSandikAsync(projeId, revizyonSatiri.KoliNo, sandikCache, sandikBilgileri);

        var satir = new CekiSatiri
        {
            CekiId = anaCekiId,
            SiraNo = revizyonSatiri.SiraNo,
            BarkodNo = revizyonSatiri.BarkodNo,
            OlcuResmiPozNo = string.IsNullOrWhiteSpace(revizyonSatiri.OlcuResmiPozNo) ? null : revizyonSatiri.OlcuResmiPozNo,
            Aciklama = revizyonSatiri.Aciklama,
            CekideGecenSandikNo = revizyonSatiri.KoliNo,
            FiiliSandikNo = revizyonSatiri.KoliNo,
            IstenenAdet = revizyonSatiri.IstenenAdet,
            BirimId = revizyonSatiri.BirimId,
            Remarks = string.IsNullOrWhiteSpace(revizyonSatiri.Remarks) ? null : revizyonSatiri.Remarks,
            DurumId = (int)UrunDurum.Bekliyor,
            GridDurumuId = (int)GridDurum.Gelmedi
        };

        context.CekiSatirlari.Add(satir);
        context.SandikIcerikleri.Add(new SandikIcerik
        {
            Sandik = sandik,
            CekiSatiri = satir,
            TahsisMiktari = satir.IstenenAdet,
            KonulanAdet = 0,
            EksikAdet = 0
        });

        context.HareketGecmisleri.Add(new HareketGecmisi
        {
            ProjeId = projeId,
            ReferansTipi = "CekiSatiri",
            ReferansMetni = $"Sıra: {satir.SiraNo} - {satir.Aciklama}",
            Islem = "Revizyon Satırı Eklendi",
            KullaniciId = kullaniciId,
            Tarih = TurkeyTime.Now,
            YeniDeger = RevizyonSatirOzeti(revizyonSatiri),
            Aciklama = $"Revizyon A satırı eklendi. Excel satırı: {revizyonSatiri.ExcelSatirNo}"
        });
    }

    internal async Task RevizyonSatiriniGuncelleAsync(
        int projeId,
        CekiSatiri mevcutSatir,
        CiktiSatirImportBilgisi revizyonSatiri,
        Dictionary<string, Sandik> sandikCache,
        Dictionary<string, SandikImportBilgisi> sandikBilgileri,
        int kullaniciId)
    {
        var eskiOzet = CekiSatiriOzeti(mevcutSatir);
        var eskiKoliNo = CekiExcelOkuyucu.NormalizeKoliNo(mevcutSatir.CekideGecenSandikNo);
        var eskiFiiliKoliNo = CekiExcelOkuyucu.NormalizeKoliNo(mevcutSatir.FiiliSandikNo);
        var eskiIstenenAdet = mevcutSatir.IstenenAdet;
        var yeniKoliNo = CekiExcelOkuyucu.NormalizeKoliNo(revizyonSatiri.KoliNo);

        var fiiliPlanlaAyni = string.IsNullOrWhiteSpace(eskiFiiliKoliNo) ||
            string.Equals(eskiFiiliKoliNo, eskiKoliNo, StringComparison.OrdinalIgnoreCase);

        var hedefSandik = await GetOrCreateSandikAsync(projeId, yeniKoliNo, sandikCache, sandikBilgileri);

        mevcutSatir.SiraNo = revizyonSatiri.SiraNo;
        mevcutSatir.BarkodNo = revizyonSatiri.BarkodNo;
        mevcutSatir.OlcuResmiPozNo = string.IsNullOrWhiteSpace(revizyonSatiri.OlcuResmiPozNo) ? null : revizyonSatiri.OlcuResmiPozNo;
        mevcutSatir.Aciklama = revizyonSatiri.Aciklama;
        mevcutSatir.CekideGecenSandikNo = yeniKoliNo;
        if (eskiIstenenAdet != revizyonSatiri.IstenenAdet)
            mevcutSatir.OrijinalIstenenAdet ??= eskiIstenenAdet;
        mevcutSatir.IstenenAdet = revizyonSatiri.IstenenAdet;
        mevcutSatir.BirimId = revizyonSatiri.BirimId;
        mevcutSatir.Remarks = string.IsNullOrWhiteSpace(revizyonSatiri.Remarks) ? null : revizyonSatiri.Remarks;

        if (fiiliPlanlaAyni)
            mevcutSatir.FiiliSandikNo = yeniKoliNo;

        mevcutSatir.DurumId = durumHesaplaService.HesaplaGenelDurum(mevcutSatir.GridDurumuId, mevcutSatir.UcKDurumuId);
        durumHesaplaService.HesaplaKalanVeDurum(mevcutSatir);

        var icerikler = await context.SandikIcerikleri
            .Include(i => i.Sandik)
            .Where(i => i.CekiSatiriId == mevcutSatir.Id)
            .ToListAsync();

        // Taşıma uygunluğunu revizyonun hesaplayacağı eksikten değil,
        // tahsisin güncelleme öncesindeki operasyon izinden belirle.
        var iceriklerTasinabilir = CekiRevizyonKurallari.RevizyonIcerikleriTasinabilirMi(icerikler);
        var tahsisSandigiUyumsuz = CekiRevizyonKurallari.RevizyonTekTamTahsisSandigiUyumsuzMu(
            projeId, eskiKoliNo, eskiFiiliKoliNo, eskiIstenenAdet, yeniKoliNo, icerikler);
        if (tahsisSandigiUyumsuz && (CekiRevizyonKurallari.SandikSevkKilitliMi(hedefSandik) || icerikler.Any(i => CekiRevizyonKurallari.SandikSevkKilitliMi(i.Sandik))))
            throw new CekiRevizyonConflictException(
                "Tahsis düzeltmesinin kaynak veya hedef sandığı sevk kilitli.",
                new[] { CekiRevizyonHatalari.RevizyonSorunuOlustur(mevcutSatir, CekiRevizyonSorunKodlari.SevkKilidi,
                    "Tahsis düzeltmesi için önce ilgili sandığın sevk kilidini açın.") });

        if (icerikler.Count == 1)
        {
            var tekIcerik = icerikler[0];
            if (tekIcerik.TahsisMiktari <= 0 || tekIcerik.TahsisMiktari == eskiIstenenAdet)
            {
                // Revizyon miktarı fiziksel olarak sandığa konulmuş adedin altına inse
                // bile mevcut malzemeyi sessizce yok sayma; tahsis fiziksel alt sınırı korur.
                tekIcerik.TahsisMiktari = Math.Max(revizyonSatiri.IstenenAdet, tekIcerik.KonulanAdet);
                tekIcerik.EksikAdet = Math.Max(tekIcerik.TahsisMiktari - tekIcerik.KonulanAdet, 0);
            }
        }

        if (icerikler.Count == 0)
        {
            context.SandikIcerikleri.Add(new SandikIcerik
            {
                Sandik = hedefSandik,
                CekiSatiriId = mevcutSatir.Id,
                TahsisMiktari = mevcutSatir.IstenenAdet,
                KonulanAdet = 0,
                EksikAdet = 0
            });
        }
        else if ((!string.Equals(eskiKoliNo, yeniKoliNo, StringComparison.OrdinalIgnoreCase) || tahsisSandigiUyumsuz) &&
            fiiliPlanlaAyni &&
            iceriklerTasinabilir)
        {
            foreach (var icerik in icerikler)
                icerik.Sandik = hedefSandik;
        }

        context.CekiSatirlari.Update(mevcutSatir);
        context.HareketGecmisleri.Add(new HareketGecmisi
        {
            ProjeId = projeId,
            ReferansTipi = "CekiSatiri",
            ReferansId = mevcutSatir.Id.ToString(),
            ReferansMetni = $"Sıra: {mevcutSatir.SiraNo} - {mevcutSatir.Aciklama}",
            Islem = "Revizyon Satırı Güncellendi",
            KullaniciId = kullaniciId,
            Tarih = TurkeyTime.Now,
            EskiDeger = eskiOzet,
            YeniDeger = CekiSatiriOzeti(mevcutSatir),
            Aciklama = $"Revizyon U satırı uygulandı. Excel satırı: {revizyonSatiri.ExcelSatirNo}"
        });
    }

    private async Task<int> RevizyonSatirlariniSilAsync(int projeId, List<CekiSatiri> satirlar, int kullaniciId)
    {
        var idler = satirlar.Select(s => s.Id).Distinct().ToList();
        if (idler.Count == 0)
            return 0;

        var aktifAktarimBagliSatirIds = await sahaAktarimSilmeKorumaService
            .GetAktifAktarimBagliCekiSatiriIdsAsync(idler);

        if (aktifAktarimBagliSatirIds.Count > 0)
        {
            var sorunlar = satirlar
                .Where(s => aktifAktarimBagliSatirIds.Contains(s.Id))
                .Select(s => CekiRevizyonHatalari.RevizyonSorunuOlustur(
                    s,
                    CekiRevizyonSorunKodlari.AktifSahaAktarimi,
                    SahaAktarimSilmeKorumaMesajlari.RevizyonCekiSatiriDetay))
                .ToList();
            throw new CekiRevizyonConflictException(
                SahaAktarimSilmeKorumaMesajlari.RevizyonCekiSatiri,
                sorunlar);
        }

        var kilitliIcerikler = await context.SandikIcerikleri
            .AsNoTracking()
            .Include(i => i.Sandik)
            .Where(i => i.CekiSatiriId.HasValue &&
                idler.Contains(i.CekiSatiriId.Value) &&
                i.Sandik.DurumId == (int)SandikDurum.Sevkedildi &&
                !i.Sandik.SevkiyatDuzeltmeAcikMi)
            .ToListAsync();

        if (kilitliIcerikler.Any())
        {
            var kilitliSatirIds = kilitliIcerikler
                .Where(i => i.CekiSatiriId.HasValue)
                .Select(i => i.CekiSatiriId!.Value)
                .ToHashSet();
            var sorunlar = satirlar
                .Where(s => kilitliSatirIds.Contains(s.Id))
                .Select(s => CekiRevizyonHatalari.RevizyonSorunuOlustur(
                    s,
                    CekiRevizyonSorunKodlari.SevkKilidi,
                    "Satır sevk edilmiş sandıkta. Silmek için önce ilgili sandığın sevk kilidini açın."))
                .ToList();
            throw new CekiRevizyonConflictException(
                $"Revizyon D satırlarından {kilitliSatirIds.Count} tanesi sevk edilmiş sandıkta.",
                sorunlar);
        }

        var transferler = await context.ProjeTransferleri
            .Where(t => idler.Contains(t.KaynakCekiSatiriId) ||
                (t.HedefCekiSatiriId.HasValue && idler.Contains(t.HedefCekiSatiriId.Value)))
            .ToListAsync();

        var aktifTransferler = transferler
            .Where(t => t.DurumId == (int)ProjeTransferDurum.Aktif)
            .ToList();

        var disariGidenTransferVar = aktifTransferler.Any(t =>
            idler.Contains(t.KaynakCekiSatiriId) &&
            (!t.HedefCekiSatiriId.HasValue || !idler.Contains(t.HedefCekiSatiriId.Value)));

        if (disariGidenTransferVar)
        {
            var disariGidenKaynakIds = aktifTransferler
                .Where(t =>
                    idler.Contains(t.KaynakCekiSatiriId) &&
                    (!t.HedefCekiSatiriId.HasValue || !idler.Contains(t.HedefCekiSatiriId.Value)))
                .Select(t => t.KaynakCekiSatiriId)
                .ToHashSet();
            var sorunlar = satirlar
                .Where(s => disariGidenKaynakIds.Contains(s.Id))
                .Select(s => CekiRevizyonHatalari.RevizyonSorunuOlustur(
                    s,
                    CekiRevizyonSorunKodlari.AktifProjeTransferi,
                    "Satır başka bir proje/satıra kaynak olarak verilmiş. Önce hedef projedeki karşılama/transfer geri alınmalı."))
                .ToList();
            throw new CekiRevizyonConflictException(
                "Revizyon D satırlarından bazıları aktif proje transferine bağlı.",
                sorunlar);
        }

        var kaynakSatirIdler = aktifTransferler
            .Where(t => t.HedefCekiSatiriId.HasValue &&
                idler.Contains(t.HedefCekiSatiriId.Value) &&
                !idler.Contains(t.KaynakCekiSatiriId))
            .Select(t => t.KaynakCekiSatiriId)
            .Distinct()
            .ToList();

        var kaynakSatirlar = kaynakSatirIdler.Count == 0
            ? new Dictionary<int, CekiSatiri>()
            : await context.CekiSatirlari
                .Where(s => kaynakSatirIdler.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id);

        foreach (var transfer in aktifTransferler.Where(t => t.HedefCekiSatiriId.HasValue && idler.Contains(t.HedefCekiSatiriId.Value)))
        {
            if (kaynakSatirlar.TryGetValue(transfer.KaynakCekiSatiriId, out var kaynakSatir))
            {
                kaynakSatir.ProjeGonderilen = Math.Max(kaynakSatir.ProjeGonderilen - transfer.Miktar, 0);
                kaynakSatir.DurumId = durumHesaplaService.HesaplaGenelDurum(kaynakSatir.GridDurumuId, kaynakSatir.UcKDurumuId);
                durumHesaplaService.HesaplaKalanVeDurum(kaynakSatir);
                ProjeTransferIadeKurali.DonorYenidenSevkBorcunuDuzelt(kaynakSatir);
                context.CekiSatirlari.Update(kaynakSatir);
            }

            transfer.DurumId = (int)ProjeTransferDurum.GeriAlindi;
            transfer.IptalTarihi = TurkeyTime.Now;
            transfer.IptalAciklama = "Revizyon D satırı silindiği için transfer pasife alındı.";
            transfer.HedefCekiSatiriId = null;
            context.ProjeTransferleri.Update(transfer);
        }

        foreach (var transfer in transferler.Where(t => idler.Contains(t.KaynakCekiSatiriId)))
            context.ProjeTransferleri.Remove(transfer);

        var stokHareketleri = await context.StokHareketleri
            .Where(h => idler.Contains(h.CekiSatiriId))
            .ToListAsync();
        await RevizyonStokHareketleriniGeriAlAsync(stokHareketleri);

        var icerikler = await context.SandikIcerikleri
            .Where(i => i.CekiSatiriId.HasValue && idler.Contains(i.CekiSatiriId.Value))
            .ToListAsync();
        context.SandikIcerikleri.RemoveRange(icerikler);

        foreach (var satir in satirlar)
        {
            context.HareketGecmisleri.Add(new HareketGecmisi
            {
                ProjeId = projeId,
                ReferansTipi = "CekiSatiri",
                ReferansId = satir.Id.ToString(),
                ReferansMetni = $"Sıra: {satir.SiraNo} - {satir.Aciklama}",
                Islem = "Revizyon Satırı Silindi",
                KullaniciId = kullaniciId,
                Tarih = TurkeyTime.Now,
                EskiDeger = CekiSatiriOzeti(satir),
                Aciklama = $"Revizyon D satırı silindi. Sıra: {satir.SiraNo}, Barkod: {satir.BarkodNo}, Sandık: {satir.CekideGecenSandikNo}"
            });
            context.CekiSatirlari.Remove(satir);
        }

        await context.SaveChangesAsync();
        return satirlar.Count;
    }

    private async Task RevizyonStokHareketleriniGeriAlAsync(List<StokHareketi> stokHareketleri)
    {
        if (stokHareketleri.Count == 0)
            return;

        var stokEngelleri = await sorgular.RevizyonStokGeriAlEngelleriniBulAsync(stokHareketleri);
        if (stokEngelleri.Count > 0)
        {
            var etkilenenCekiSatiriIds = stokEngelleri
                .SelectMany(e => e.CekiSatiriIds)
                .Distinct()
                .ToList();
            var cekiSatirlari = await context.CekiSatirlari
                .Where(s => etkilenenCekiSatiriIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id);
            var sorunlar = stokEngelleri
                .SelectMany(engel => engel.CekiSatiriIds.Select(id =>
                    cekiSatirlari.TryGetValue(id, out var satir)
                        ? CekiRevizyonHatalari.RevizyonSorunuOlustur(satir, engel.Kod, engel.Mesaj)
                        : CekiRevizyonHatalari.GenelRevizyonSorunuOlustur(
                            engel.Kod,
                            engel.Mesaj,
                            CekiRevizyonSorunKategorileri.DurumCakismasi)))
                .ToList();

            throw new CekiRevizyonConflictException(
                "Revizyon, ilişkili stok hareketleri geri alınamadığı için uygulanamadı.",
                sorunlar);
        }

        var stokIdler = stokHareketleri.Select(h => h.StokKaydiId).Distinct().ToList();
        var stoklar = await context.StokKayitlari
            .Where(s => stokIdler.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id);
        var tumIlgiliStokHareketleri = await context.StokHareketleri
            .Where(h => stokIdler.Contains(h.StokKaydiId))
            .ToListAsync();
        var geriAlinacakHareketIdleri = stokHareketleri.Select(h => h.Id).ToHashSet();

        foreach (var grup in stokHareketleri.GroupBy(h => h.StokKaydiId))
        {
            if (!stoklar.TryGetValue(grup.Key, out var stok))
                continue;

            var stoktanKarsilanan = grup
                .Where(h => h.IslemTipiId == (int)IslemTipi.StoktanKarsilandi)
                .Sum(h => Math.Abs(h.Miktar));

            var fazlaTeslimdenAktarilan = grup
                .Where(h => h.IslemTipiId == (int)IslemTipi.FazlaTeslimStogaAktarildi)
                .Sum(h => Math.Abs(h.Miktar));

            if (fazlaTeslimdenAktarilan > 0)
            {
                var baskaHareketVarMi = tumIlgiliStokHareketleri.Any(h =>
                    h.StokKaydiId == grup.Key &&
                    !geriAlinacakHareketIdleri.Contains(h.Id));

                if (baskaHareketVarMi || stok.Miktar < fazlaTeslimdenAktarilan)
                {
                    throw new CekiRevizyonConflictException(
                        "Revizyon sırasında stok durumu değişti. Ön izlemeyi yenileyip tekrar deneyin.",
                        new[]
                        {
                            CekiRevizyonHatalari.GenelRevizyonSorunuOlustur(
                                CekiRevizyonSorunKodlari.StokHareketiKullanilmis,
                                "Stok durumu ön izleme sonrasında değişti. Ön izlemeyi yenileyip tekrar deneyin.",
                                CekiRevizyonSorunKategorileri.DurumCakismasi)
                        });
                }

                stok.Miktar = Math.Max(stok.Miktar - fazlaTeslimdenAktarilan, 0);
            }

            if (stoktanKarsilanan > 0)
                stok.Miktar += stoktanKarsilanan;

            stok.DurumId = stok.Miktar > 0
                ? (int)StokDurum.Aktif
                : (int)StokDurum.Tukendi;

            context.StokKayitlari.Update(stok);
        }

        context.StokHareketleri.RemoveRange(stokHareketleri);
    }

    private async Task BosSandiklariTemizleVeDurumlariGuncelleAsync(
        int projeId,
        IReadOnlyCollection<int> etkilenenSandikIds)
    {
        if (etkilenenSandikIds.Count == 0)
            return;

        var sandikIds = etkilenenSandikIds.Distinct().ToList();
        var sandiklar = await context.Sandiklar
            .Include(s => s.SandikIcerikleri)
            .ThenInclude(i => i.CekiSatiri)
            .Where(s =>
                s.ProjeId == projeId &&
                sandikIds.Contains(s.Id))
            .ToListAsync();

        var bosSandikIds = sandiklar
            .Where(s =>
                s.DurumId != (int)SandikDurum.Sevkedildi &&
                s.SandikIcerikleri.Count == 0)
            .Select(s => s.Id)
            .ToList();

        var aktifAktarimBagliBosSandikIds = await sahaAktarimSilmeKorumaService
            .GetAktifAktarimBagliSandikIdsAsync(bosSandikIds);

        if (aktifAktarimBagliBosSandikIds.Count > 0)
        {
            throw new ReferentialIntegrityConflictException(
                SahaAktarimSilmeKorumaMesajlari.RevizyonBosSandik);
        }

        foreach (var sandik in sandiklar)
        {
            if (sandik.DurumId == (int)SandikDurum.Sevkedildi)
                continue;

            if (sandik.SandikIcerikleri.Count == 0)
            {
                context.Sandiklar.Remove(sandik);
                continue;
            }

            var cekiIcerikleri = sandik.SandikIcerikleri.Where(i => i.CekiSatiriId.HasValue).ToList();
            var hepsiTamamlandi = cekiIcerikleri.Count > 0 &&
                cekiIcerikleri.All(i => i.CekiSatiri?.DurumId == (int)UrunDurum.Tamamlandi);

            sandik.DurumId = hepsiTamamlandi
                ? (int)SandikDurum.Kapandi
                : (int)SandikDurum.Hazirlaniyor;
        }
    }

    private async Task<Sandik> GetOrCreateSandikAsync(
        int projeId,
        string koliNo,
        Dictionary<string, Sandik> sandikCache,
        Dictionary<string, SandikImportBilgisi> sandikBilgileri)
    {
        var normalizedKoliNo = CekiExcelOkuyucu.NormalizeKoliNo(koliNo);
        if (string.IsNullOrWhiteSpace(normalizedKoliNo))
        {
            CekiRevizyonHatalari.RevizyonValidationHatasiFirlat(
                "Revizyon satırında koli/sandık no boş olamaz.",
                CekiRevizyonSorunKodlari.SandikNoEksik);
        }

        if (sandikCache.TryGetValue(normalizedKoliNo, out var mevcutSandik))
        {
            SandikBilgisiniUygula(mevcutSandik, sandikBilgileri.GetValueOrDefault(normalizedKoliNo));
            return mevcutSandik;
        }

        sandikBilgileri.TryGetValue(normalizedKoliNo, out var bilgi);
        var sandik = new Sandik
        {
            ProjeId = projeId,
            SandikNo = normalizedKoliNo,
            Ad = bilgi?.SandikIsmi,
            AdIngilizce = bilgi?.SandikIsmiIngilizce,
            DurumId = (int)SandikDurum.Hazirlaniyor,
            En = bilgi?.En,
            Boy = bilgi?.Boy,
            Yukseklik = bilgi?.Yukseklik,
            NetKg = bilgi?.NetKg,
            GrossKg = bilgi?.GrossKg
        };

        context.Sandiklar.Add(sandik);
        sandikCache[normalizedKoliNo] = sandik;
        return sandik;
    }

    private static void SandikBilgisiniUygula(Sandik sandik, SandikImportBilgisi? bilgi)
    {
        if (bilgi == null)
            return;

        if (!string.IsNullOrWhiteSpace(bilgi.SandikIsmi))
            sandik.Ad = bilgi.SandikIsmi;
        if (!string.IsNullOrWhiteSpace(bilgi.SandikIsmiIngilizce))
            sandik.AdIngilizce = bilgi.SandikIsmiIngilizce;
        sandik.En = bilgi.En ?? sandik.En;
        sandik.Boy = bilgi.Boy ?? sandik.Boy;
        sandik.Yukseklik = bilgi.Yukseklik ?? sandik.Yukseklik;
        sandik.NetKg = bilgi.NetKg ?? sandik.NetKg;
        sandik.GrossKg = bilgi.GrossKg ?? sandik.GrossKg;
    }

    private static string CekiSatiriOzeti(CekiSatiri satir)
    {
        return $"Sıra:{satir.SiraNo}; Poz:{satir.OlcuResmiPozNo}; Barkod:{satir.BarkodNo}; Tanım:{satir.Aciklama}; Koli:{satir.CekideGecenSandikNo}; Miktar:{satir.IstenenAdet}; Birim:{satir.BirimId}; Açıklama:{satir.Remarks}";
    }

    private static string RevizyonSatirOzeti(CiktiSatirImportBilgisi satir)
    {
        return $"Sıra:{satir.SiraNo}; Poz:{satir.OlcuResmiPozNo}; Barkod:{satir.BarkodNo}; Tanım:{satir.Aciklama}; Koli:{satir.KoliNo}; Miktar:{satir.IstenenAdet}; Birim:{satir.BirimId}; Açıklama:{satir.Remarks}";
    }
}
