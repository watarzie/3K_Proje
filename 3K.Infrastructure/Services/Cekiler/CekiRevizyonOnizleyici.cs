using ClosedXML.Excel;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Globalization;

namespace _3K.Infrastructure.Services.Cekiler;

/// <summary>Revizyon dosyasının ana çekiye etkisini ön izler: eklenen, güncellenen ve silinecek satırlar, sandık etkileri, saha aktarım ve stok riskleri.</summary>
internal sealed class CekiRevizyonOnizleyici(
    AppDbContext context,
    ISahaAktarimSilmeKorumaService sahaAktarimSilmeKorumaService,
    CekiRevizyonSorgulari sorgular)
{
    internal async Task<CekiRevizyonOnizlemeSonuc> RevizyonOnizlemeOlusturAsync(
        CiktiImportBilgisi import,
        string dosyaAdi,
        Proje proje,
        Ceki anaCeki,
        List<CekiSatiri> anaSatirlar)
    {
        var revizyonSatirlari = import.Satirlar
            .Where(s => !string.IsNullOrWhiteSpace(s.CheckKodu))
            .ToList();

        if (revizyonSatirlari.Count == 0)
        {
            CekiRevizyonHatalari.RevizyonValidationHatasiFirlat(
                "Revizyon Excel dosyasında CHECK/KONTROL kolonunda A, U veya D işareti bulunamadı.",
                CekiRevizyonSorunKodlari.IsaretliSatirBulunamadi);
        }

        var siraGruplari = anaSatirlar
            .GroupBy(s => s.SiraNo)
            .ToDictionary(g => g.Key, g => g.ToList());

        var sonuc = new CekiRevizyonOnizlemeSonuc
        {
            ProjeId = proje.Id,
            ProjeNo = proje.ProjeNo,
            AnaCekiId = anaCeki.Id,
            DosyaAdi = dosyaAdi,
            ToplamIsaretliSatirSayisi = revizyonSatirlari.Count
        };

        var tahsisDuzeltilecekKaynakSandikNolari = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var tahsisDuzeltilecekSatirlar = new List<CekiRevizyonOnizlemeSatiri>();

        foreach (var revizyonSatiri in revizyonSatirlari)
        {
            var satir = new CekiRevizyonOnizlemeSatiri
            {
                ExcelSatirNo = revizyonSatiri.ExcelSatirNo,
                CheckKodu = revizyonSatiri.CheckKodu,
                IslemTipi = revizyonSatiri.CheckKodu switch
                {
                    "A" => "Eklenecek",
                    "U" => "Güncellenecek",
                    "D" => "Silinecek",
                    _ => "Bilinmiyor"
                },
                YeniSiraNo = revizyonSatiri.SiraNo,
                BarkodNo = revizyonSatiri.BarkodNo,
                PozNo = revizyonSatiri.OlcuResmiPozNo,
                Tanim = revizyonSatiri.Aciklama,
                YeniKoliNo = revizyonSatiri.KoliNo,
                YeniIstenenAdet = revizyonSatiri.IstenenAdet
            };

            if (revizyonSatiri.CheckKodu is "A" or "U" && !revizyonSatiri.VeriSatiriMi)
            {
                RevizyonSatirRiskEkle(
                    satir,
                    "Engel",
                    "CHECK işaretli satır boş görünüyor.",
                    CekiRevizyonSorunKodlari.BosSatir,
                    CekiRevizyonSorunKategorileri.Dogrulama);
                sonuc.Satirlar.Add(satir);
                continue;
            }

            if (revizyonSatiri.CheckKodu is "A" or "U" && revizyonSatiri.IstenenAdet <= 0)
            {
                RevizyonSatirRiskEkle(
                    satir,
                    "Engel",
                    "Miktar sıfır veya negatif olamaz. Revizyon satırındaki miktarı kontrol edin.",
                    CekiRevizyonSorunKodlari.GecersizMiktar,
                    CekiRevizyonSorunKategorileri.Dogrulama);
                sonuc.Satirlar.Add(satir);
                continue;
            }

            if (revizyonSatiri.CheckKodu is "A" or "U" &&
                string.IsNullOrWhiteSpace(CekiExcelOkuyucu.NormalizeKoliNo(revizyonSatiri.KoliNo)))
            {
                RevizyonSatirRiskEkle(
                    satir,
                    "Engel",
                    "Revizyon satırında koli/sandık no boş olamaz.",
                    CekiRevizyonSorunKodlari.SandikNoEksik,
                    CekiRevizyonSorunKategorileri.Dogrulama);
                sonuc.Satirlar.Add(satir);
                continue;
            }

            if (revizyonSatiri.CheckKodu == "A")
            {
                sonuc.EklenenSatirSayisi++;
                satir.Mesaj = "Yeni çeki satırı olarak eklenecek.";

                if (CekiRevizyonKurallari.RevizyonBenzerSatirVarMi(revizyonSatiri, anaSatirlar))
                    RevizyonSatirRiskEkle(satir, "Uyarı", "Ana çekide aynı barkod/poz/sandık ile benzer bir satır var. Yine de CHECK=A olduğu için yeni satır olarak ele alınacak.");

                sonuc.Satirlar.Add(satir);
                continue;
            }

            var mevcutSatir = CekiRevizyonKurallari.EslesenAnaSatiriBul(revizyonSatiri, siraGruplari, anaSatirlar);
            if (mevcutSatir == null)
            {
                RevizyonSatirRiskEkle(
                    satir,
                    "Engel",
                    $"Ana çekide eşleşen satır bulunamadı. Sıra No: {revizyonSatiri.SiraNo}",
                    CekiRevizyonSorunKodlari.AnaSatirBulunamadi,
                    CekiRevizyonSorunKategorileri.Dogrulama);
                sonuc.Satirlar.Add(satir);
                continue;
            }

            RevizyonEskiSatirBilgileriniDoldur(satir, mevcutSatir);

            if (revizyonSatiri.CheckKodu == "U")
            {
                sonuc.GuncellenenSatirSayisi++;
                RevizyonDegisiklikleriDoldur(satir, mevcutSatir, revizyonSatiri);

                if (CekiRevizyonKurallari.RevizyonTekTamTahsisSandigiUyumsuzMu(
                    proje.Id, mevcutSatir.CekideGecenSandikNo, mevcutSatir.FiiliSandikNo,
                    mevcutSatir.IstenenAdet, revizyonSatiri.KoliNo, mevcutSatir.SandikIcerikleri.ToList()))
                {
                    var kaynakSandikNo = CekiExcelOkuyucu.NormalizeKoliNo(mevcutSatir.SandikIcerikleri.Single().Sandik.SandikNo);
                    RevizyonDegisiklikEkle(satir, "Tahsis sandığı", kaynakSandikNo, CekiExcelOkuyucu.NormalizeKoliNo(revizyonSatiri.KoliNo));
                    tahsisDuzeltilecekKaynakSandikNolari.Add(kaynakSandikNo);
                    tahsisDuzeltilecekSatirlar.Add(satir);
                    if (CekiRevizyonKurallari.SandikSevkKilitliMi(mevcutSatir.SandikIcerikleri.Single().Sandik))
                        RevizyonSatirRiskEkle(satir, "Engel", "Tahsis düzeltmesinin kaynak sandığı sevk kilitli. Önce ilgili sandığın sevk kilidini açın.",
                            CekiRevizyonSorunKodlari.SevkKilidi, CekiRevizyonSorunKategorileri.DurumCakismasi);
                }

                if (satir.Degisiklikler.Count == 0)
                    satir.Degisiklikler.Add("Ana veride değişiklik yok.");

                if (satir.IslemGormusMu)
                {
                    satir.GeriAlmaEtkisi = RevizyonGeriAlmaEtkisiOlustur(mevcutSatir);
                    var geriAlEngeli = await sorgular.RevizyonOtomatikGeriAlEngelMesajiAsync(mevcutSatir);
                    if (!string.IsNullOrWhiteSpace(geriAlEngeli))
                    {
                        var engelKodu = geriAlEngeli.StartsWith("Satır sevk edilmiş", StringComparison.Ordinal)
                            ? CekiRevizyonSorunKodlari.SevkKilidi
                            : CekiRevizyonSorunKodlari.AktifProjeTransferi;
                        RevizyonSatirRiskEkle(
                            satir,
                            "Engel",
                            geriAlEngeli,
                            engelKodu,
                            CekiRevizyonSorunKategorileri.DurumCakismasi);
                    }
                    else
                    {
                        RevizyonSatirRiskEkle(satir, "Uyarı", "Satır işlem görmüş. Revizyon uygulanırken önce Grid/3K/stok/proje hareketleri otomatik geri alınacak, sonra ana veri güncellenecek.");
                    }
                }

                if (!string.Equals(CekiExcelOkuyucu.NormalizeKoliNo(mevcutSatir.CekideGecenSandikNo), CekiExcelOkuyucu.NormalizeKoliNo(revizyonSatiri.KoliNo), StringComparison.OrdinalIgnoreCase)
                    && mevcutSatir.SandikIcerikleri.Any()
                    && !CekiRevizyonKurallari.RevizyonIcerikleriTasinabilirMi(mevcutSatir.SandikIcerikleri.ToList()))
                {
                    RevizyonSatirRiskEkle(satir, "Uyarı", "Sandık değişiyor fakat satırda işlem/konum bilgisi var. Planlanan sandık güncellenir, mevcut operasyon izi korunur.");
                }

                if (string.IsNullOrWhiteSpace(satir.Mesaj))
                    satir.Mesaj = "Ana çeki satırı güncellenecek.";
            }
            else if (revizyonSatiri.CheckKodu == "D")
            {
                sonuc.SilinecekSatirSayisi++;
                satir.Mesaj = "Ana çekiden silinecek.";
                satir.GeriAlmaEtkisi = RevizyonGeriAlmaEtkisiOlustur(mevcutSatir);

                if (satir.IslemGormusMu)
                    RevizyonSatirRiskEkle(satir, "Uyarı", "Satır işlem görmüş. Revizyon uygulanırken geri alınabilir hareketler otomatik temizlenecek ve satır silinecek.");

                if (mevcutSatir.SandikIcerikleri.Any(i => CekiRevizyonKurallari.SandikSevkKilitliMi(i.Sandik)))
                {
                    RevizyonSatirRiskEkle(
                        satir,
                        "Engel",
                        "Satır sevk edilmiş sandıkta. Silmek için önce ilgili sandığın sevk kilidini açın.",
                        CekiRevizyonSorunKodlari.SevkKilidi,
                        CekiRevizyonSorunKategorileri.DurumCakismasi);
                }

                if (await sorgular.RevizyonDisariGidenAktifTransferVarMiAsync(mevcutSatir.Id))
                {
                    RevizyonSatirRiskEkle(
                        satir,
                        "Engel",
                        "Satır başka bir proje/satıra kaynak olarak verilmiş. Silmek için önce hedef projedeki karşılama/transfer geri alınmalı.",
                        CekiRevizyonSorunKodlari.AktifProjeTransferi,
                        CekiRevizyonSorunKategorileri.DurumCakismasi);
                }
            }

            sonuc.Satirlar.Add(satir);
        }

        if (tahsisDuzeltilecekSatirlar.Count > 0)
        {
            var hedefNolar = tahsisDuzeltilecekSatirlar
                .Select(s => CekiExcelOkuyucu.NormalizeKoliNo(s.YeniKoliNo).ToUpperInvariant()).Distinct().ToList();
            var kilitliHedefNolar = (await context.Sandiklar.AsNoTracking()
                    .Where(s => s.ProjeId == proje.Id && hedefNolar.Contains(s.SandikNo.ToUpper()) &&
                        s.DurumId == (int)SandikDurum.Sevkedildi && !s.SevkiyatDuzeltmeAcikMi)
                    .Select(s => s.SandikNo).ToListAsync())
                .Select(CekiExcelOkuyucu.NormalizeKoliNo).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var satir in tahsisDuzeltilecekSatirlar.Where(s => kilitliHedefNolar.Contains(CekiExcelOkuyucu.NormalizeKoliNo(s.YeniKoliNo))))
                RevizyonSatirRiskEkle(satir, "Engel", "Tahsis düzeltmesinin hedef sandığı sevk kilitli. Önce ilgili sandığın sevk kilidini açın.",
                    CekiRevizyonSorunKodlari.SevkKilidi, CekiRevizyonSorunKategorileri.DurumCakismasi);
        }

        await RevizyonSahaAktarimRiskleriniEkleAsync(sonuc.Satirlar, proje.ProjeTipiId, anaSatirlar);
        await RevizyonStokGeriAlRiskleriniEkleAsync(sonuc.Satirlar);
        await RevizyonGelenTransferOzetleriniEkleAsync(sonuc.Satirlar);
        sonuc.SandikEtkileri = await RevizyonSandikEtkileriniOlusturAsync(
            proje.Id,
            sonuc.Satirlar,
            import.SandikBilgileri,
            tahsisDuzeltilecekKaynakSandikNolari);

        sonuc.RiskliSatirSayisi = sonuc.Satirlar.Count(s => s.RiskSeviyesi != "Güvenli");
        sonuc.EngelliSatirSayisi = sonuc.Satirlar.Count(s => !s.UygulanabilirMi);
        sonuc.UygulanabilirMi = sonuc.EngelliSatirSayisi == 0;
        sonuc.Mesaj = sonuc.UygulanabilirMi
            ? $"{proje.ProjeNo} revizyonu ön izleme hazır. Eklenen: {sonuc.EklenenSatirSayisi}, Güncellenecek: {sonuc.GuncellenenSatirSayisi}, Silinecek: {sonuc.SilinecekSatirSayisi}."
            : $"{proje.ProjeNo} revizyonunda {sonuc.EngelliSatirSayisi} engelli satır var. Engeller giderilmeden revizyon uygulanamaz.";

        if (sonuc.RiskliSatirSayisi > 0)
            sonuc.Uyarilar.Add($"{sonuc.RiskliSatirSayisi} satırda uyarı/engel var. Uygulamadan önce detayları kontrol edin.");

        return sonuc;
    }

    private async Task<List<CekiRevizyonSandikEtkisi>> RevizyonSandikEtkileriniOlusturAsync(
        int projeId,
        IReadOnlyCollection<CekiRevizyonOnizlemeSatiri> onizlemeSatirlari,
        IReadOnlyDictionary<string, SandikImportBilgisi> sandikBilgileri,
        IEnumerable<string> tahsisDuzeltilecekKaynakSandikNolari)
    {
        var hedefSandikNolari = onizlemeSatirlari
            .Where(satir => satir.CheckKodu is "A" or "U")
            .Select(satir => CekiExcelOkuyucu.NormalizeKoliNo(satir.YeniKoliNo))
            .Where(sandikNo => !string.IsNullOrWhiteSpace(sandikNo))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var etkilenenSandikNolari = hedefSandikNolari
            .Concat(onizlemeSatirlari
                .Where(satir => satir.CheckKodu is "U" or "D")
                .Select(satir => CekiExcelOkuyucu.NormalizeKoliNo(satir.EskiKoliNo)))
            .Concat(tahsisDuzeltilecekKaynakSandikNolari)
            .Where(sandikNo => !string.IsNullOrWhiteSpace(sandikNo))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (etkilenenSandikNolari.Count == 0)
            return new List<CekiRevizyonSandikEtkisi>();

        var hedefSandikSeti = hedefSandikNolari.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var etkilenenSandikAnahtarlari = etkilenenSandikNolari
            .Select(sandikNo => sandikNo.ToUpperInvariant())
            .ToList();
        var mevcutSandiklar = (await context.Sandiklar
                .AsNoTracking()
                .Include(sandik => sandik.SandikIcerikleri)
                .ThenInclude(icerik => icerik.CekiSatiri)
                .Where(sandik =>
                    sandik.ProjeId == projeId &&
                    etkilenenSandikAnahtarlari.Contains(sandik.SandikNo.ToUpper()))
                .ToListAsync())
            .GroupBy(
                sandik => CekiExcelOkuyucu.NormalizeKoliNo(sandik.SandikNo),
                StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                grup => grup.Key,
                grup => grup.First(),
                StringComparer.OrdinalIgnoreCase);

        var etkiler = new List<CekiRevizyonSandikEtkisi>();
        foreach (var sandikNo in etkilenenSandikNolari.OrderBy(no => no, StringComparer.Ordinal))
        {
            var hedefSandikMi = hedefSandikSeti.Contains(sandikNo);
            SandikImportBilgisi? bilgi = null;
            if (hedefSandikMi)
                sandikBilgileri.TryGetValue(sandikNo, out bilgi);

            if (!mevcutSandiklar.TryGetValue(sandikNo, out var mevcutSandik))
            {
                if (!hedefSandikMi)
                    continue;

                etkiler.Add(new CekiRevizyonSandikEtkisi
                {
                    SandikNo = sandikNo,
                    YeniSandikMi = true,
                    DurumYenidenHesaplanacakMi = true,
                    BosKalirsaSilinecekMi = true,
                    YeniAd = CekiRevizyonHatalari.NullIfEmpty(bilgi?.SandikIsmi),
                    YeniAdIngilizce = CekiRevizyonHatalari.NullIfEmpty(bilgi?.SandikIsmiIngilizce),
                    YeniEn = bilgi?.En,
                    YeniBoy = bilgi?.Boy,
                    YeniYukseklik = bilgi?.Yukseklik,
                    YeniNetKg = bilgi?.NetKg,
                    YeniGrossKg = bilgi?.GrossKg
                });
                continue;
            }

            var etki = MevcutSandikEtkisiniOlustur(mevcutSandik, bilgi);
            if (etki != null)
                etkiler.Add(etki);
        }

        return etkiler;
    }

    private static CekiRevizyonSandikEtkisi? MevcutSandikEtkisiniOlustur(
        Sandik mevcutSandik,
        SandikImportBilgisi? bilgi)
    {
        var durumYenidenHesaplanacakMi = mevcutSandik.DurumId != (int)SandikDurum.Sevkedildi;
        var cekiIcerikleri = mevcutSandik.SandikIcerikleri
            .Where(icerik => icerik.CekiSatiriId.HasValue)
            .ToList();
        var etki = new CekiRevizyonSandikEtkisi
        {
            SandikNo = CekiExcelOkuyucu.NormalizeKoliNo(mevcutSandik.SandikNo),
            DurumYenidenHesaplanacakMi = durumYenidenHesaplanacakMi,
            BosKalirsaSilinecekMi = durumYenidenHesaplanacakMi,
            EskiDurumId = mevcutSandik.DurumId,
            MevcutIcerikSayisi = mevcutSandik.SandikIcerikleri.Count,
            MevcutCekiIcerigiSayisi = cekiIcerikleri.Count,
            TamamlanmisCekiIcerigiSayisi = cekiIcerikleri.Count(icerik =>
                icerik.CekiSatiri?.DurumId == (int)UrunDurum.Tamamlandi)
        };
        var degisiklikVar = durumYenidenHesaplanacakMi;

        if (!string.IsNullOrWhiteSpace(bilgi?.SandikIsmi) &&
            !string.Equals(mevcutSandik.Ad, bilgi.SandikIsmi, StringComparison.Ordinal))
        {
            etki.EskiAd = mevcutSandik.Ad;
            etki.YeniAd = bilgi.SandikIsmi;
            degisiklikVar = true;
        }

        if (!string.IsNullOrWhiteSpace(bilgi?.SandikIsmiIngilizce) &&
            !string.Equals(mevcutSandik.AdIngilizce, bilgi.SandikIsmiIngilizce, StringComparison.Ordinal))
        {
            etki.EskiAdIngilizce = mevcutSandik.AdIngilizce;
            etki.YeniAdIngilizce = bilgi.SandikIsmiIngilizce;
            degisiklikVar = true;
        }

        degisiklikVar |= SandikDecimalEtkisiniEkle(
            mevcutSandik.En,
            bilgi?.En,
            (eski, yeni) => (etki.EskiEn, etki.YeniEn) = (eski, yeni));
        degisiklikVar |= SandikDecimalEtkisiniEkle(
            mevcutSandik.Boy,
            bilgi?.Boy,
            (eski, yeni) => (etki.EskiBoy, etki.YeniBoy) = (eski, yeni));
        degisiklikVar |= SandikDecimalEtkisiniEkle(
            mevcutSandik.Yukseklik,
            bilgi?.Yukseklik,
            (eski, yeni) => (etki.EskiYukseklik, etki.YeniYukseklik) = (eski, yeni));
        degisiklikVar |= SandikDecimalEtkisiniEkle(
            mevcutSandik.NetKg,
            bilgi?.NetKg,
            (eski, yeni) => (etki.EskiNetKg, etki.YeniNetKg) = (eski, yeni));
        degisiklikVar |= SandikDecimalEtkisiniEkle(
            mevcutSandik.GrossKg,
            bilgi?.GrossKg,
            (eski, yeni) => (etki.EskiGrossKg, etki.YeniGrossKg) = (eski, yeni));

        return degisiklikVar ? etki : null;
    }

    private static bool SandikDecimalEtkisiniEkle(
        decimal? mevcutDeger,
        decimal? yeniDeger,
        Action<decimal?, decimal?> ata)
    {
        if (!yeniDeger.HasValue || mevcutDeger == yeniDeger)
            return false;

        ata(mevcutDeger, yeniDeger);
        return true;
    }

    internal async Task RevizyonSahaAktarimRiskleriniEkleAsync(
        IReadOnlyCollection<CekiRevizyonOnizlemeSatiri> satirlar,
        int projeTipiId,
        IReadOnlyCollection<CekiSatiri> anaSatirlar)
    {
        var normalKaynakSatirIds = CekiRevizyonKurallari.RevizyonNormalKaynakSatirIds(projeTipiId, anaSatirlar);
        var korunacakSatirlar = satirlar
            .Where(s =>
                s.MevcutCekiSatiriId.HasValue &&
                (s.CheckKodu == "D" ||
                 (s.CheckKodu == "U" && normalKaynakSatirIds.Contains(s.MevcutCekiSatiriId.Value))))
            .ToList();

        if (korunacakSatirlar.Count == 0)
            return;

        var aktifAktarimBagliSatirIds = await sahaAktarimSilmeKorumaService
            .GetAktifAktarimBagliCekiSatiriIdsAsync(
                korunacakSatirlar.Select(s => s.MevcutCekiSatiriId!.Value).Distinct());

        foreach (var satir in korunacakSatirlar.Where(s =>
                     aktifAktarimBagliSatirIds.Contains(s.MevcutCekiSatiriId!.Value)))
        {
            RevizyonSatirRiskEkle(
                satir,
                "Engel",
                satir.CheckKodu == "U"
                    ? CekiRevizyonKurallari.RevizyonAktifSahaKaynakGuncellemeMesaji
                    : SahaAktarimSilmeKorumaMesajlari.RevizyonCekiSatiriDetay,
                CekiRevizyonSorunKodlari.AktifSahaAktarimi,
                CekiRevizyonSorunKategorileri.DurumCakismasi);
        }
    }

    private async Task RevizyonStokGeriAlRiskleriniEkleAsync(
        IReadOnlyCollection<CekiRevizyonOnizlemeSatiri> satirlar)
    {
        var adaylar = satirlar
            .Where(satir =>
                satir.CheckKodu is "U" or "D" &&
                satir.MevcutCekiSatiriId.HasValue)
            .ToList();

        if (adaylar.Count == 0)
            return;

        var cekiSatiriIds = adaylar
            .Select(satir => satir.MevcutCekiSatiriId!.Value)
            .Distinct()
            .ToList();
        var stokHareketleri = await context.StokHareketleri
            .AsNoTracking()
            .Where(hareket => cekiSatiriIds.Contains(hareket.CekiSatiriId))
            .ToListAsync();

        foreach (var satir in adaylar.Where(satir => satir.GeriAlmaEtkisi != null))
        {
            var satirId = satir.MevcutCekiSatiriId!.Value;
            var hareketler = stokHareketleri
                .Where(hareket => hareket.CekiSatiriId == satirId)
                .OrderBy(hareket => hareket.Id)
                .ToList();
            var etki = satir.GeriAlmaEtkisi!;
            etki.StokHareketSayisi = hareketler.Count;
            etki.StoktanKarsilananMiktar = hareketler
                .Where(hareket => hareket.IslemTipiId == (int)IslemTipi.StoktanKarsilandi)
                .Sum(hareket => Math.Abs(hareket.Miktar));
            etki.FazlaTeslimStogaAktarilanMiktar = hareketler
                .Where(hareket => hareket.IslemTipiId == (int)IslemTipi.FazlaTeslimStogaAktarildi)
                .Sum(hareket => Math.Abs(hareket.Miktar));
            etki.DigerStokHareketMiktari = hareketler
                .Where(hareket =>
                    hareket.IslemTipiId != (int)IslemTipi.StoktanKarsilandi &&
                    hareket.IslemTipiId != (int)IslemTipi.FazlaTeslimStogaAktarildi)
                .Sum(hareket => Math.Abs(hareket.Miktar));
            etki.StokHareketleri = hareketler
                .Select(hareket => new CekiRevizyonStokHareketEtkisi
                {
                    StokHareketiId = hareket.Id,
                    StokKaydiId = hareket.StokKaydiId,
                    IslemTipiId = hareket.IslemTipiId,
                    Miktar = hareket.Miktar
                })
                .ToList();
        }

        if (stokHareketleri.Count == 0)
            return;

        var kontrolBaglami = await sorgular.RevizyonStokKontrolBaglaminiYukleAsync(
            stokHareketleri.Select(hareket => hareket.StokKaydiId));

        foreach (var guncellenecekSatir in adaylar.Where(satir => satir.CheckKodu == "U"))
        {
            var satirId = guncellenecekSatir.MevcutCekiSatiriId!.Value;
            var satirHareketleri = stokHareketleri
                .Where(hareket => hareket.CekiSatiriId == satirId)
                .ToList();
            var engeller = CekiRevizyonKurallari.RevizyonStokGeriAlEngelleriniBul(satirHareketleri, kontrolBaglami);
            RevizyonStokEngelleriniOnizlemeyeEkle(
                new[] { guncellenecekSatir },
                engeller);
        }

        var silinecekSatirlar = adaylar
            .Where(satir => satir.CheckKodu == "D")
            .ToList();
        var silinecekSatirIds = silinecekSatirlar
            .Select(satir => satir.MevcutCekiSatiriId!.Value)
            .ToHashSet();
        var silmeHareketleri = stokHareketleri
            .Where(hareket => silinecekSatirIds.Contains(hareket.CekiSatiriId))
            .ToList();
        var silmeEngelleri = CekiRevizyonKurallari.RevizyonStokGeriAlEngelleriniBul(
            silmeHareketleri,
            kontrolBaglami);
        RevizyonStokEngelleriniOnizlemeyeEkle(silinecekSatirlar, silmeEngelleri);
    }

    private async Task RevizyonGelenTransferOzetleriniEkleAsync(
        IReadOnlyCollection<CekiRevizyonOnizlemeSatiri> satirlar)
    {
        var adaylar = satirlar
            .Where(satir =>
                satir.GeriAlmaEtkisi != null &&
                satir.MevcutCekiSatiriId.HasValue)
            .ToList();

        if (adaylar.Count == 0)
            return;

        var cekiSatiriIds = adaylar
            .Select(satir => satir.MevcutCekiSatiriId!.Value)
            .Distinct()
            .ToList();
        var gelenTransferler = await context.ProjeTransferleri
            .AsNoTracking()
            .Where(transfer =>
                transfer.DurumId == (int)ProjeTransferDurum.Aktif &&
                transfer.HedefCekiSatiriId.HasValue &&
                cekiSatiriIds.Contains(transfer.HedefCekiSatiriId.Value))
            .ToListAsync();

        foreach (var satir in adaylar)
        {
            var satirId = satir.MevcutCekiSatiriId!.Value;
            var transferler = gelenTransferler
                .Where(transfer => transfer.HedefCekiSatiriId == satirId)
                .OrderBy(transfer => transfer.Id)
                .ToList();
            var etki = satir.GeriAlmaEtkisi!;
            etki.GelenAktifProjeTransferSayisi = transferler.Count;
            etki.GelenAktifProjeTransferMiktari = transferler.Sum(transfer => transfer.Miktar);
            etki.GelenAktifProjeTransferleri = transferler
                .Select(transfer => new CekiRevizyonGelenTransferEtkisi
                {
                    ProjeTransferiId = transfer.Id,
                    KaynakProjeId = transfer.KaynakProjeId,
                    KaynakCekiSatiriId = transfer.KaynakCekiSatiriId,
                    Miktar = transfer.Miktar
                })
                .ToList();
        }
    }

    private static void RevizyonStokEngelleriniOnizlemeyeEkle(
        IReadOnlyCollection<CekiRevizyonOnizlemeSatiri> satirlar,
        IReadOnlyCollection<RevizyonStokEngeli> engeller)
    {
        foreach (var engel in engeller)
        {
            foreach (var satir in satirlar.Where(satir =>
                         satir.MevcutCekiSatiriId.HasValue &&
                         engel.CekiSatiriIds.Contains(satir.MevcutCekiSatiriId.Value)))
            {
                RevizyonSatirRiskEkle(
                    satir,
                    "Engel",
                    engel.Mesaj,
                    engel.Kod,
                    CekiRevizyonSorunKategorileri.DurumCakismasi);
            }
        }
    }

    private static void RevizyonEskiSatirBilgileriniDoldur(CekiRevizyonOnizlemeSatiri onizlemeSatiri, CekiSatiri mevcutSatir)
    {
        onizlemeSatiri.MevcutCekiSatiriId = mevcutSatir.Id;
        onizlemeSatiri.EskiSiraNo = mevcutSatir.SiraNo;
        onizlemeSatiri.EskiKoliNo = mevcutSatir.CekideGecenSandikNo;
        onizlemeSatiri.EskiIstenenAdet = mevcutSatir.IstenenAdet;
        onizlemeSatiri.IslemGormusMu = CekiRevizyonKurallari.CekiSatiriIslemGormusMu(mevcutSatir);
        onizlemeSatiri.IslemGorenAdet = CekiRevizyonKurallari.CekiSatiriIslemMiktari(mevcutSatir);

        if (string.IsNullOrWhiteSpace(onizlemeSatiri.BarkodNo))
            onizlemeSatiri.BarkodNo = mevcutSatir.BarkodNo;
        if (string.IsNullOrWhiteSpace(onizlemeSatiri.PozNo))
            onizlemeSatiri.PozNo = mevcutSatir.OlcuResmiPozNo;
        if (string.IsNullOrWhiteSpace(onizlemeSatiri.Tanim))
            onizlemeSatiri.Tanim = mevcutSatir.Aciklama;
    }

    private static CekiRevizyonGeriAlmaEtkisi RevizyonGeriAlmaEtkisiOlustur(
        CekiSatiri mevcutSatir)
    {
        var icerikler = mevcutSatir.SandikIcerikleri.ToList();
        return new CekiRevizyonGeriAlmaEtkisi
        {
            GridDurumuId = mevcutSatir.GridDurumuId,
            GridGelenAdet = mevcutSatir.GridGelenAdet,
            TrafoSevkAdet = mevcutSatir.TrafoSevkAdet,
            GridSevkDurumuId = mevcutSatir.GridSevkDurumuId,
            GridSevkMiktari = mevcutSatir.GridSevkMiktari ?? 0,
            YenidenSevkGerekliAdet = mevcutSatir.YenidenSevkGerekliAdet,
            GridSevkTarihi = mevcutSatir.GridSevkTarihi,
            GridAciklama = mevcutSatir.GridAciklama,
            GridPersonelId = mevcutSatir.GridPersonelId,
            UcKDurumuId = mevcutSatir.UcKDurumuId,
            UcKKarsilamaTipiId = mevcutSatir.UcKKarsilamaTipiId,
            GelenMiktar = mevcutSatir.GelenMiktar,
            TeslimTarihi = mevcutSatir.TeslimTarihi,
            KaynakHedefProjeNo = mevcutSatir.KaynakHedefProjeNo,
            UcKAciklama = mevcutSatir.UcKAciklama,
            KarsilananMiktar = mevcutSatir.KarsilananMiktar,
            StokKarsilanan = mevcutSatir.StokKarsilanan,
            ProjeKarsilanan = mevcutSatir.ProjeKarsilanan,
            ProjeGonderilen = mevcutSatir.ProjeGonderilen,
            TedarikciKarsilanan = mevcutSatir.TedarikciKarsilanan,
            HataliMiktar = mevcutSatir.HataliMiktar,
            GeriGonderilenMiktar = mevcutSatir.GeriGonderilenMiktar,
            GeriGonderilmeSebebiId = mevcutSatir.GeriGonderilmeSebebiId,
            KaynakProjeId = mevcutSatir.KaynakProjeId,
            KaliteDurumId = mevcutSatir.KaliteDurumId,
            SurecDurumId = mevcutSatir.SurecDurumId,
            PaketleyenId = mevcutSatir.PaketleyenId,
            KontrolEdenId = mevcutSatir.KontrolEdenId,
            SandikIcerikSayisi = icerikler.Count,
            TahsisMiktari = icerikler.Sum(icerik => icerik.TahsisMiktari),
            KonulanAdet = icerikler.Sum(icerik => icerik.KonulanAdet),
            EksikAdet = icerikler.Sum(icerik => icerik.EksikAdet),
            SandikStokKarsilanan = icerikler.Sum(icerik => icerik.StokKarsilanan),
            SandikProjeKarsilanan = icerikler.Sum(icerik => icerik.ProjeKarsilanan),
            SandikTedarikciKarsilanan = icerikler.Sum(icerik => icerik.TedarikciKarsilanan)
        };
    }

    private static void RevizyonDegisiklikleriDoldur(CekiRevizyonOnizlemeSatiri satir, CekiSatiri mevcutSatir, CiktiSatirImportBilgisi revizyonSatiri)
    {
        RevizyonDegisiklikEkle(satir, "Sıra No", mevcutSatir.SiraNo, revizyonSatiri.SiraNo);
        RevizyonDegisiklikEkle(satir, "Barkod", mevcutSatir.BarkodNo, revizyonSatiri.BarkodNo);
        RevizyonDegisiklikEkle(satir, "Poz No", mevcutSatir.OlcuResmiPozNo, revizyonSatiri.OlcuResmiPozNo);
        RevizyonDegisiklikEkle(satir, "Tanım", mevcutSatir.Aciklama, revizyonSatiri.Aciklama);
        RevizyonDegisiklikEkle(satir, "Sandık", mevcutSatir.CekideGecenSandikNo, revizyonSatiri.KoliNo);
        RevizyonDegisiklikEkle(satir, "Miktar", mevcutSatir.IstenenAdet, revizyonSatiri.IstenenAdet);
        RevizyonDegisiklikEkle(satir, "Birim", mevcutSatir.BirimId, revizyonSatiri.BirimId);
        RevizyonDegisiklikEkle(satir, "Açıklama", mevcutSatir.Remarks, revizyonSatiri.Remarks);
    }

    private static void RevizyonDegisiklikEkle(CekiRevizyonOnizlemeSatiri satir, string alan, int eskiDeger, int yeniDeger)
    {
        if (eskiDeger != yeniDeger)
            satir.Degisiklikler.Add($"{alan}: {eskiDeger.ToString(CultureInfo.InvariantCulture)} → {yeniDeger.ToString(CultureInfo.InvariantCulture)}");
    }

    private static void RevizyonDegisiklikEkle(CekiRevizyonOnizlemeSatiri satir, string alan, decimal eskiDeger, decimal yeniDeger)
    {
        if (eskiDeger != yeniDeger)
            satir.Degisiklikler.Add($"{alan}: {CekiRevizyonKurallari.FormatRevizyonDecimal(eskiDeger)} → {CekiRevizyonKurallari.FormatRevizyonDecimal(yeniDeger)}");
    }

    private static void RevizyonDegisiklikEkle(CekiRevizyonOnizlemeSatiri satir, string alan, string? eskiDeger, string? yeniDeger)
    {
        var eski = (eskiDeger ?? string.Empty).Trim();
        var yeni = (yeniDeger ?? string.Empty).Trim();
        if (!string.Equals(eski, yeni, StringComparison.OrdinalIgnoreCase))
            satir.Degisiklikler.Add($"{alan}: {DisplayValue(eski)} → {DisplayValue(yeni)}");
    }

    private static string DisplayValue(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "-" : value;
    }

    private static void RevizyonSatirRiskEkle(
        CekiRevizyonOnizlemeSatiri satir,
        string riskSeviyesi,
        string mesaj,
        string? kod = null,
        string kategori = CekiRevizyonSorunKategorileri.Dogrulama)
    {
        if (riskSeviyesi == "Engel")
        {
            satir.RiskSeviyesi = "Engel";
            satir.UygulanabilirMi = false;

            if (!satir.Engeller.Contains(mesaj, StringComparer.Ordinal))
                satir.Engeller.Add(mesaj);

            var sorunKodu = string.IsNullOrWhiteSpace(kod)
                ? CekiRevizyonSorunKodlari.GecersizDosya
                : kod;

            if (!satir.Sorunlar.Any(s => s.Kod == sorunKodu && s.Mesaj == mesaj))
            {
                satir.Sorunlar.Add(new CekiRevizyonSorunu
                {
                    Kod = sorunKodu,
                    Mesaj = mesaj,
                    Kategori = kategori,
                    ExcelSatirNo = satir.ExcelSatirNo,
                    CheckKodu = satir.CheckKodu,
                    SiraNo = satir.YeniSiraNo,
                    BarkodNo = CekiRevizyonHatalari.NullIfEmpty(satir.BarkodNo),
                    PozNo = CekiRevizyonHatalari.NullIfEmpty(satir.PozNo),
                    Tanim = CekiRevizyonHatalari.NullIfEmpty(satir.Tanim),
                    SandikNo = CekiRevizyonHatalari.NullIfEmpty(satir.YeniKoliNo ?? satir.EskiKoliNo)
                });
            }
        }
        else if (satir.RiskSeviyesi != "Engel")
        {
            satir.RiskSeviyesi = "Uyarı";
        }

        if (!satir.Uyarilar.Contains(mesaj, StringComparer.Ordinal))
            satir.Uyarilar.Add(mesaj);
        if (string.IsNullOrWhiteSpace(satir.Mesaj))
            satir.Mesaj = mesaj;
    }
}
