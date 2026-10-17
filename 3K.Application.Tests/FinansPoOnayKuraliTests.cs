using System.Text.Json;
using _3K.Application.Common;
using _3K.Application.Features.FinansIslemleri.Commands;
using _3K.Application.Features.FinansIslemleri.Validators;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Helpers;
using _3K.Core.Models;
using _3K.Infrastructure.Services.Finans;
using _3K_API.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace _3K.Application.Tests;

/// <summary>
/// PO öncesi iş onayının saf kuralları (veritabanısız): kalem onay durumu, onayın tutar/para birimi değişince düşmesi,
/// kapsam (yalnız proje kategorileri), kural kapalıyken PO serbestliği, PO seçicisinde onay durumuna göre ayrı grup,
/// talep görüntüsü özetinin jsonb gidiş-dönüşünde korunması, doğrulayıcı ve iç onay komutunun sözleşmesi.
/// </summary>
public sealed class FinansPoOnayKuraliTests
{
    private static readonly DateTime Tarih = new(2026, 10, 17);

    [Fact]
    public void Durum_TalepKarariVeOnayAlanlarindanTuretilir()
    {
        var kalem = Kalem(FinansIsTuru.AnaAmbalaj, 1.5m, 100m);
        Assert.Equal(FinansPoOnayDurumu.OnayaSunulmadi, FinansPoOnayKurali.Durum(kalem, null, null));
        Assert.Equal(FinansPoOnayDurumu.OnayBekliyor, FinansPoOnayKurali.Durum(kalem, OnayDurumu.Bekliyor, OnayCalistirmaDurumu.Bekliyor));
        Assert.Equal(FinansPoOnayDurumu.Reddedildi, FinansPoOnayKurali.Durum(kalem, OnayDurumu.Reddedildi, OnayCalistirmaDurumu.Atlandi));
        // Karar verildi, uygulama sürüyor: henüz onaylı değil.
        Assert.Equal(FinansPoOnayDurumu.OnayBekliyor, FinansPoOnayKurali.Durum(kalem, OnayDurumu.Onaylandi, OnayCalistirmaDurumu.Calisiyor));
        // Onaylandı fakat kalem değiştiği için uygulanamadı: yeniden onaya sunulur.
        Assert.Equal(FinansPoOnayDurumu.OnayDustu, FinansPoOnayKurali.Durum(kalem, OnayDurumu.Onaylandi, OnayCalistirmaDurumu.Basarisiz));

        Onayla(kalem);
        Assert.Equal(FinansPoOnayDurumu.Onaylandi, FinansPoOnayKurali.Durum(kalem, OnayDurumu.Onaylandi, OnayCalistirmaDurumu.Basarili));
        Assert.True(FinansPoOnayKurali.PoyaBaglanabilir(kalem, kuralAcik: true));
    }

    [Fact]
    public void Onay_NetTutarVeyaParaBirimiDegisinceDuser_TarihDegisikligiDusurmez()
    {
        var kalem = Onayla(Kalem(FinansIsTuru.AnaAmbalaj, 1.5m, 100m));
        kalem.FinansTarihi = Tarih.AddMonths(1);
        kalem.Aciklama = "Açıklama değişti";
        Assert.True(FinansPoOnayKurali.OnayGecerli(kalem));

        kalem.ToplamM3 = 1.6m; // üretim aktarımı m³'ü büyüttü: 150 → 160
        Assert.Equal(FinansPoOnayDurumu.OnayDustu, FinansPoOnayKurali.Durum(kalem, OnayDurumu.Onaylandi, OnayCalistirmaDurumu.Basarili));
        Assert.False(FinansPoOnayKurali.PoyaBaglanabilir(kalem, kuralAcik: true));
        Assert.True(FinansPoOnayKurali.OnayaGonderilebilir(FinansPoOnayDurumu.OnayDustu));

        var paraBirimi = Onayla(Kalem(FinansIsTuru.AnaAmbalaj, 1.5m, 100m));
        paraBirimi.ParaBirimiSnapshot = "TRY";
        Assert.False(FinansPoOnayKurali.OnayGecerli(paraBirimi));
    }

    [Fact]
    public void Kapsam_YalnizUretimKategorileri_OzelVeDuzenliIsOnaysizBaglanir()
    {
        foreach (var isTuru in Enum.GetValues<FinansIsTuru>().Where(x => x != FinansIsTuru.OzelIs))
            Assert.True(FinansPoOnayKurali.KapsamdaMi(isTuru, null), isTuru.ToString());
        Assert.False(FinansPoOnayKurali.KapsamdaMi(FinansIsTuru.OzelIs, null));
        Assert.False(FinansPoOnayKurali.KapsamdaMi(FinansIsTuru.OzelIs, 5));

        var ozel = Kalem(FinansIsTuru.OzelIs, 1m, 50m);
        Assert.Equal(FinansPoOnayDurumu.Gerekmiyor, FinansPoOnayKurali.Durum(ozel, null, null));
        Assert.True(FinansPoOnayKurali.PoyaBaglanabilir(ozel, kuralAcik: true));

        var iptal = Kalem(FinansIsTuru.AnaAmbalaj, 1m, 50m);
        iptal.IptalEdildi = true;
        Assert.Equal(FinansPoOnayDurumu.Gerekmiyor, FinansPoOnayKurali.Durum(iptal, OnayDurumu.Bekliyor, OnayCalistirmaDurumu.Bekliyor));
    }

    [Fact]
    public void KuralKapaliyken_OnaysizKalemPoyaBaglanir_KuralKaydiYoksaAcikSayilir()
    {
        var kalem = Kalem(FinansIsTuru.SarfKereste, 0.5m, 200m);
        Assert.True(FinansPoOnayKurali.PoyaBaglanabilir(kalem, kuralAcik: false));
        Assert.False(FinansPoOnayKurali.PoyaBaglanabilir(kalem, kuralAcik: true));
        Assert.True(FinansPoOnayKurali.KuralAcik(null));
        Assert.False(FinansPoOnayKurali.KuralAcik(false));
    }

    [Fact]
    public void Birlesik_KapsamDisiniSaymaz_BekleyenIsiOnaGetirir()
    {
        Assert.Equal(FinansPoOnayDurumu.Gerekmiyor, FinansPoOnayKurali.Birlesik([FinansPoOnayDurumu.Gerekmiyor]));
        Assert.Equal(FinansPoOnayDurumu.Onaylandi,
            FinansPoOnayKurali.Birlesik([FinansPoOnayDurumu.Onaylandi, FinansPoOnayDurumu.Gerekmiyor, FinansPoOnayDurumu.Onaylandi]));
        Assert.Equal(FinansPoOnayDurumu.OnayBekliyor,
            FinansPoOnayKurali.Birlesik([FinansPoOnayDurumu.Onaylandi, FinansPoOnayDurumu.OnayBekliyor]));
        Assert.Equal(FinansPoOnayDurumu.OnayaSunulmadi,
            FinansPoOnayKurali.Birlesik([FinansPoOnayDurumu.OnayBekliyor, FinansPoOnayDurumu.OnayaSunulmadi]));
        Assert.Equal(FinansPoOnayDurumu.Reddedildi,
            FinansPoOnayKurali.Birlesik([FinansPoOnayDurumu.OnayDustu, FinansPoOnayDurumu.Reddedildi, FinansPoOnayDurumu.Onaylandi]));
    }

    [Fact]
    public void PoSecicisi_KuralAcikkenOnayliVeOnaysizKalemAyriSatirdir_KapaliykenTekSatir()
    {
        var onayli = Onayla(Kalem(FinansIsTuru.AnaAmbalaj, 1m, 100m, id: 1));
        var yeni = Kalem(FinansIsTuru.AnaAmbalaj, 2m, 100m, id: 2);
        var bilgiler = new Dictionary<int, FinansPoOnayBilgisi>
        {
            [1] = new(FinansPoOnayDurumu.Onaylandi, null, Tarih),
            [2] = new(FinansPoOnayDurumu.OnayaSunulmadi, null, null)
        };
        Assert.Equal(FinansPoOnayKurali.GrupAnahtari(onayli), FinansSiparisFaturaService.SiparisGrupAnahtari(onayli));

        var acik = FinansSiparisFaturaService.SiparisGruplariniOlustur([onayli, yeni], bilgiler, poOnayKuralAcik: true);
        Assert.Equal(2, acik.Count);
        var onayliGrup = acik.Single(x => x.IsKaydiIds.SequenceEqual([1]));
        var yeniGrup = acik.Single(x => x.IsKaydiIds.SequenceEqual([2]));
        Assert.Equal((FinansPoOnayDurumu.Onaylandi, true, false), (onayliGrup.PoOnayDurumu, onayliGrup.PoyaBaglanabilir, onayliGrup.PoOnayaGonderilebilir));
        Assert.Equal((FinansPoOnayDurumu.OnayaSunulmadi, false, true), (yeniGrup.PoOnayDurumu, yeniGrup.PoyaBaglanabilir, yeniGrup.PoOnayaGonderilebilir));

        var kapali = Assert.Single(FinansSiparisFaturaService.SiparisGruplariniOlustur([onayli, yeni], bilgiler, poOnayKuralAcik: false));
        Assert.Equal([1, 2], kapali.IsKaydiIds);
        Assert.Equal((FinansPoOnayDurumu.OnayaSunulmadi, true, false), (kapali.PoOnayDurumu, kapali.PoyaBaglanabilir, kapali.PoOnayaGonderilebilir));
    }

    [Fact]
    public void TalepGoruntusu_JsonbGidisDonusundeOzetiKorur_DegerDegisinceOzetDegisir()
    {
        var onizleme = Onizleme(146.76m, 0.293514m);
        var hash = FinansPoOnayOnizlemeButunlugu.HashOlustur(onizleme);
        var ayarlar = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var geriOkunan = JsonSerializer.Deserialize<FinansPoOnayOnizleme>(JsonSerializer.Serialize(onizleme, ayarlar), ayarlar)!;
        Assert.True(FinansPoOnayOnizlemeButunlugu.HashDogrula(geriOkunan, hash));
        // jsonb numeric ölçeği farklı dönebilir (146.760 / 0.2935140); özet değişmez.
        Assert.True(FinansPoOnayOnizlemeButunlugu.HashDogrula(Onizleme(146.760m, 0.2935140m), hash));
        Assert.False(FinansPoOnayOnizlemeButunlugu.HashDogrula(Onizleme(146.77m, 0.293514m), hash));
        Assert.False(FinansPoOnayOnizlemeButunlugu.HashDogrula(onizleme, null));
    }

    [Fact]
    public void PoEngelMesaji_KalemVeDurumuSoyler()
    {
        var kalemler = Enumerable.Range(1, 7)
            .Select(x => (Kalem(FinansIsTuru.AnaAmbalaj, 1m, 10m, id: x), FinansPoOnayDurumu.OnayBekliyor))
            .ToArray();
        var mesaj = FinansPoOnayKurali.PoEngelMesaji(kalemler);
        Assert.Contains("PA727-03 · Normal Ambalaj - 1 (Onay bekliyor)", mesaj);
        Assert.Contains("ve 2 kalem daha", mesaj);
        Assert.Contains("Onaya gönder", mesaj);
    }

    [Fact]
    public void OnayaGonder_DogrulayiciBosVeGecersizSecimiReddeder_SekmeSiparisAcmadir()
    {
        var dogrulayici = new FinansPoOnayGonderCommandValidator();
        Assert.False(dogrulayici.Validate(new FinansPoOnayGonderCommand { Model = new([]) }).IsValid);
        Assert.False(dogrulayici.Validate(new FinansPoOnayGonderCommand { Model = new([1, 0]) }).IsValid);
        Assert.False(dogrulayici.Validate(new FinansPoOnayGonderCommand { Model = new(Enumerable.Range(1, 2001).ToArray()) }).IsValid);
        Assert.True(dogrulayici.Validate(new FinansPoOnayGonderCommand { Model = new([1, 2]) }).IsValid);
        Assert.Equal(FinansSekmeleri.SiparisAcma, new FinansPoOnayGonderCommand { Model = new([1]) }.Sekmeler);
    }

    [Fact]
    public void IcOnayKomutu_AyarlanabilirOnaydir_HttpUcuYoktur_ReferansTalebiGosterir_YanittaOzetYok()
    {
        var komut = new FinansPoOnayUygulaCommand
        {
            TalepId = 12,
            ProjeId = 395,
            ProjeNo = "PA727-03",
            Ozet = "Ana Ambalaj · 3 kalem · 520,30 EUR",
            OnizlemeHash = new string('a', 64)
        };
        Assert.IsAssignableFrom<IConfigurableApproval>(komut);
        Assert.Equal(OnayIslemKodlari.FinansPoIsOnayi, komut.GetApprovalOperationCode());
        Assert.True(OnayIslemKodlari.AyarlanabilirMi(OnayIslemKodlari.FinansPoIsOnayi));
        Assert.Equal("Finans: PO Öncesi İş Onayı", OnayIslemKodlari.DisplayName(OnayIslemKodlari.FinansPoIsOnayi));
        Assert.Equal(new ApprovalReference(OnayReferansTipleri.FinansPoOnayTalebi, 12, 395, "/onay-merkezi"), komut.GetApprovalReference());
        Assert.Equal("PA727-03 · Ana Ambalaj · 3 kalem · 520,30 EUR: PO öncesi iş onayı bekliyor.", komut.GetApprovalDescription());
        Assert.True(MenuAuthorizationExceptions.IsServerInternal(komut));
        // Hiçbir API ucu iç komutu gövdeden almaz; komut yalnız "Onaya gönder" işleyicisinde kurulur.
        var uclar = typeof(FinansSiparisFaturaController).Assembly.GetTypes()
            .Where(x => typeof(ControllerBase).IsAssignableFrom(x))
            .SelectMany(x => x.GetMethods())
            .SelectMany(x => x.GetParameters())
            .Select(x => x.ParameterType);
        Assert.DoesNotContain(typeof(FinansPoOnayUygulaCommand), uclar);
        // Görüntü özeti yalnız sunucu içindir; "Onaya gönder" yanıtına yazılmaz.
        var yanit = JsonSerializer.Serialize(new FinansPoOnayTalebiOzeti(12, 395, "PA727-03", "Ana Ambalaj", 3, 520.30m, "EUR", new string('a', 64)));
        Assert.DoesNotContain("aaaa", yanit);
        Assert.Contains("\"TalepId\":12", yanit);
        Assert.False(typeof(ISecuredRequest).IsAssignableFrom(typeof(FinansPoOnayUygulaCommand)));
        Assert.True(new FinansPoOnayUygulaCommandValidator().Validate(komut).IsValid);
        Assert.False(new FinansPoOnayUygulaCommandValidator().Validate(new FinansPoOnayUygulaCommand { TalepId = 12 }).IsValid);
    }

    private static FinansIsKaydi Kalem(FinansIsTuru isTuru, decimal m3, decimal fiyat, int id = 1) => new()
    {
        Id = id,
        ProjeId = 395,
        ProjeNo = "PA727-03",
        Musteri = "GE Vernova",
        IsTuru = isTuru,
        IsAdi = $"Normal Ambalaj - {id}",
        Adet = 1,
        Birim = "m³",
        BirimM3 = m3,
        ToplamM3 = m3,
        FiyatlandirmaBirimiSnapshot = isTuru == FinansIsTuru.OzelIs ? FinansFiyatlandirmaBirimi.SabitTutar : FinansFiyatlandirmaBirimi.Metrekup,
        BirimFiyatSnapshot = fiyat,
        ParaBirimiSnapshot = "EUR",
        KdvOraniSnapshot = 20,
        UretimTarihi = Tarih,
        FinansTarihi = Tarih,
        FinansDonemi = new DateTime(2026, 10, 1),
        KaynakAktif = true
    };

    private static FinansIsKaydi Onayla(FinansIsKaydi kalem)
    {
        kalem.PoOnayTalebiId = 1;
        kalem.PoOnayNetTutar = FinansTutarKurallari.IsNet(kalem);
        kalem.PoOnayParaBirimi = kalem.ParaBirimiSnapshot;
        kalem.PoOnayTarihi = Tarih;
        return kalem;
    }

    private static FinansPoOnayOnizleme Onizleme(decimal net, decimal m3) => new()
    {
        Surum = FinansPoOnayOnizlemeButunlugu.Surum,
        ProjeId = 395,
        ProjeNo = "PA727-03",
        Musteri = "GE Vernova",
        IsTuru = FinansIsTuru.AnaAmbalaj,
        Kategori = FinansIsKategorisi.AnaAmbalaj,
        KategoriAdi = "Ana Ambalaj",
        ParaBirimi = "EUR",
        KalemSayisi = 1,
        NetTutar = net,
        KdvTutari = 29.35m,
        ToplamTutar = net + 29.35m,
        ToplamM3 = m3,
        AhsapM3 = m3,
        IlkFinansTarihi = Tarih,
        SonFinansTarihi = Tarih,
        Kalemler =
        [
            new FinansPoOnayKalemi
            {
                IsKaydiId = 1, IsAdi = "Normal Ambalaj - 2", SandikNo = "2", Adet = 1, Birim = "m³", ToplamM3 = m3, AhsapM3 = m3,
                FiyatlandirmaBirimi = FinansFiyatlandirmaBirimi.Metrekup, BirimFiyat = 500m, KdvOrani = 20, NetTutar = net,
                ParaBirimi = "EUR", UretimTarihi = Tarih, FinansTarihi = Tarih
            }
        ]
    };
}
