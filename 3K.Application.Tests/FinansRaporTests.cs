using System.Reflection;
using ClosedXML.Excel;
using _3K.Application.Features.AmbalajIslemleri.Commands;
using _3K.Application.Features.FinansIslemleri.Queries;
using _3K.Application.Features.FinansIslemleri.Validators;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Services.Finans;

namespace _3K.Application.Tests;

/// <summary>
/// Finans işler, gider ve finansal durum raporları: bölümler, para birimi bazında ara/genel toplamlar, ölçü ve sandık
/// cinsi kuralı, avansın maliyete girmemesi, finansal durumun Kontrol Paneli tutarlarıyla aynı olması ve PDF/Excel'in
/// aynı satırlardan üretilmesi. Veri okuma PostgreSQL testindedir (<c>FinansPostgresTests</c>).
/// </summary>
public sealed class FinansRaporTests
{
    private static readonly DateTime Tarih = new(2026, 10, 1);

    private static FinansIsKaydiModel Is(int id, string paraBirimi, decimal net, decimal siparisNet,
        FinansIsTuru tur = FinansIsTuru.OzelIs) => new()
    {
        Id = id, ProjeNo = $"PA-{id}", Musteri = "QA", IsTuru = tur, Kategori = FinansIsKategorisi.OzelIs, IsAdi = $"İş {id}",
        Adet = 1, ToplamM3 = 1m, ParaBirimi = paraBirimi, NetTutar = net, KdvTutari = net * 0.2m, ToplamTutar = net * 1.2m,
        SiparisNetTutar = siparisNet, KalanSiparisNetTutar = net - siparisNet, FinansTarihi = Tarih, Durum = FinansIsDurumu.SiparisBekliyor,
        KaynakAktif = true
    };

    private static List<RaporSatiri> Satirlar(BolumluRapor rapor) => Assert.Single(rapor.Tablolar).Satirlar.ToList();

    private static object? Deger(BolumluRapor rapor, RaporSatiri satir, string sutun)
        => satir.Degerler[rapor.Tablolar[0].Sutunlar.ToList().IndexOf(sutun)];

    [Fact]
    public void IsRaporu_Birlesik_SiparisiAcilanVeAcilmayanBolumleri_AraToplamVeParaBirimiBazindaGenelToplam()
    {
        // 1 tam, 2 kısmi açılmış (açılan bölümde); 3 TRY ve 4 EUR hiç açılmamış.
        var rapor = FinansRaporService.IsRaporu(
            [Is(1, "EUR", 1000m, 1000m), Is(2, "EUR", 500m, 200m), Is(3, "TRY", 300m, 0m), Is(4, "EUR", 100m, 0m)],
            FinansIsRaporuKapsami.Birlesik, "filtre");
        var satirlar = Satirlar(rapor);

        Assert.Equal(29, rapor.Tablolar[0].Sutunlar.Count);
        Assert.All(satirlar.Where(x => x.Tur != RaporSatirTuru.Bolum), x => Assert.Equal(29, x.Degerler.Length));
        Assert.Equal(
            ["Siparişi açılan işler (2 kayıt)", "Siparişi açılmayan işler (2 kayıt)", "Genel toplam"],
            satirlar.Where(x => x.Tur == RaporSatirTuru.Bolum).Select(x => (string)x.Degerler[0]!));

        var acilanVeri = satirlar.Skip(1).TakeWhile(x => x.Tur == RaporSatirTuru.Veri).ToList();
        Assert.Equal([1, 2], acilanVeri.Select(x => (int)x.Degerler[0]!));
        Assert.Equal("Kısmi Açıldı", Deger(rapor, acilanVeri[1], "Sipariş Durumu"));
        Assert.Equal(300m, Deger(rapor, acilanVeri[1], "Kalan Sipariş"));

        var araToplamlar = satirlar.Where(x => x.Tur == RaporSatirTuru.AraToplam).ToList();
        Assert.Equal(3, araToplamlar.Count); // açılan: EUR; açılmayan: EUR + TRY (para birimleri toplanmaz)
        Assert.Equal((1500m, 1200m, 300m), ((decimal)Deger(rapor, araToplamlar[0], "Net")!,
            (decimal)Deger(rapor, araToplamlar[0], "Sipariş Net")!, (decimal)Deger(rapor, araToplamlar[0], "Kalan Sipariş")!));

        var genel = satirlar.Where(x => x.Tur == RaporSatirTuru.GenelToplam).ToList();
        Assert.Equal(["EUR", "TRY"], genel.Select(x => (string)Deger(rapor, x, "Para Birimi")!));
        Assert.Equal(1600m, Deger(rapor, genel[0], "Net"));
        Assert.Equal(1920m, Deger(rapor, genel[0], "Toplam"));
        Assert.Equal(300m, Deger(rapor, genel[1], "Net"));
        Assert.Equal("GENEL TOPLAM (3 kayıt)", Deger(rapor, genel[0], "Proje"));
        Assert.Same(genel[^1], satirlar[^1]);
        // Adet toplanmaz: kalemlerin birimi farklıdır (adet, ay, sefer…).
        Assert.Null(Deger(rapor, genel[0], "Adet"));
    }

    [Fact]
    public void IsRaporu_IptalVeKaynagiPasifIsListelenir_ToplamaGirmez_AyriToplanir()
    {
        var rapor = FinansRaporService.IsRaporu(
            [Is(1, "EUR", 1000m, 0m), Kopya(Is(5, "EUR", 999m, 0m), iptal: true, kaynakAktif: true), Kopya(Is(6, "EUR", 50m, 0m), iptal: false, kaynakAktif: false)],
            FinansIsRaporuKapsami.Birlesik, "filtre");
        var satirlar = Satirlar(rapor);

        var veri = satirlar.Where(x => x.Tur == RaporSatirTuru.Veri).ToList();
        Assert.Equal(["Sipariş Bekliyor", "İptal", "Kaynağı pasif"], veri.Select(x => (string?)Deger(rapor, x, "Durum")));
        var araToplam = Assert.Single(satirlar, x => x.Tur == RaporSatirTuru.AraToplam && ((string)Deger(rapor, x, "Proje")!).StartsWith("Ara toplam"));
        Assert.Equal(1000m, Deger(rapor, araToplam, "Net"));
        var genel = Assert.Single(satirlar, x => x.Tur == RaporSatirTuru.GenelToplam);
        Assert.Equal((1000m, "GENEL TOPLAM (1 kayıt)"), ((decimal)Deger(rapor, genel, "Net")!, (string)Deger(rapor, genel, "Proje")!));
        var disarida = satirlar[^1];
        Assert.Equal(RaporSatirTuru.AraToplam, disarida.Tur);
        Assert.Equal((1049m, "İptal / kaynağı pasif (toplama dahil değil) (2 kayıt)"),
            ((decimal)Deger(rapor, disarida, "Net")!, (string)Deger(rapor, disarida, "Proje")!));
    }

    private static FinansIsKaydiModel Kopya(FinansIsKaydiModel x, bool iptal, bool kaynakAktif) => new()
    {
        Id = x.Id, ProjeNo = x.ProjeNo, Musteri = x.Musteri, IsTuru = x.IsTuru, Kategori = x.Kategori, IsAdi = x.IsAdi,
        Adet = x.Adet, ToplamM3 = x.ToplamM3, ParaBirimi = x.ParaBirimi, NetTutar = x.NetTutar, KdvTutari = x.KdvTutari,
        ToplamTutar = x.ToplamTutar, SiparisNetTutar = x.SiparisNetTutar, KalanSiparisNetTutar = x.KalanSiparisNetTutar,
        FinansTarihi = x.FinansTarihi, Durum = x.Durum, IptalEdildi = iptal, KaynakAktif = kaynakAktif
    };

    [Fact]
    public void Onizleme_SatirSiniriAsilinca_GenelToplamSondaKalir_AraToplamKaymaz()
    {
        var isler = Enumerable.Range(1, 60).Select(i => Is(i, "EUR", 10m, 10m)).Append(Is(100, "EUR", 5m, 0m)).ToList();
        var onizleme = FinansRaporService.Onizleme(FinansRaporService.IsRaporu(isler, FinansIsRaporuKapsami.Birlesik, "filtre"));

        Assert.Equal(61, onizleme.ToplamSatir);
        Assert.Equal(FinansRaporOnizlemeModel.EnFazlaSatir, onizleme.Satirlar.Count);
        Assert.Equal([FinansRaporOnizlemeModel.SatirBolum, FinansRaporOnizlemeModel.SatirGenelToplam], onizleme.SatirTurleri!.TakeLast(2));
        Assert.Equal("Genel toplam", onizleme.Satirlar[^2][0]);
        // Kesilen bölümün ara toplamı başka bölümün altına düşmez.
        Assert.DoesNotContain(FinansRaporOnizlemeModel.SatirAraToplam, onizleme.SatirTurleri!);
        Assert.Equal("605,00", onizleme.Satirlar[^1][Array.IndexOf(FinansRaporService.IsRaporuBasliklari, "Net")]);
    }

    [Theory]
    [InlineData(FinansIsRaporuKapsami.SiparisiAcilan, new[] { 1 }, "Siparişi açılan işler")]
    [InlineData(FinansIsRaporuKapsami.SiparisiAcilmayan, new[] { 2 }, "Siparişi açılmayan işler")]
    public void IsRaporu_AyriKapsam_YalnizOBolumuVeOnunGenelToplaminiVerir(FinansIsRaporuKapsami kapsam, int[] beklenen, string bolum)
    {
        var rapor = FinansRaporService.IsRaporu([Is(1, "EUR", 1000m, 400m), Is(2, "EUR", 250m, 0m)], kapsam, "filtre");
        var satirlar = Satirlar(rapor);

        Assert.EndsWith(bolum, rapor.Baslik);
        Assert.Equal($"{bolum} (1 kayıt)", satirlar[0].Degerler[0]);
        Assert.Equal(beklenen, satirlar.Where(x => x.Tur == RaporSatirTuru.Veri).Select(x => (int)x.Degerler[0]!));
        var genel = Assert.Single(satirlar, x => x.Tur == RaporSatirTuru.GenelToplam);
        Assert.Equal(beklenen[0] == 1 ? 1000m : 250m, Deger(rapor, genel, "Net"));
    }

    [Fact]
    public void IsRaporu_OlcuYalnizIlaveSahaYedekVeOzelIste_SandikCinsiVeTalepEdenYazilir()
    {
        FinansIsKaydiModel Olculu(int id, FinansIsTuru tur, AmbalajSandikCinsi? cins, string? tip = null) => new()
        {
            Id = id, ProjeNo = "PA-1", IsTuru = tur, Kategori = FinansIsKategorisi.Saha, ParaBirimi = "EUR", Boy = 1200, En = 800, Yukseklik = 600,
            KaynakAktif = true, SandikCinsi = cins, SandikTipi = tip, TalepEdenKisi = "Kişi A", TalepEdenBolum = "Bölüm B", Aciklama = "Not"
        };
        var isler = new[]
        {
            Olculu(1, FinansIsTuru.IlaveSandik, AmbalajSandikCinsi.Kafes),
            Olculu(2, FinansIsTuru.SahaSandigi, AmbalajSandikCinsi.Diger, "Palet"),
            Olculu(3, FinansIsTuru.YedekSandik, AmbalajSandikCinsi.Diger),
            Olculu(4, FinansIsTuru.OzelIs, null, "Serbest tip"),
            Olculu(5, FinansIsTuru.AnaAmbalaj, AmbalajSandikCinsi.AhsapKapali),
            Olculu(6, FinansIsTuru.Tadilat, null)
        };

        var satirlar = isler.Select(FinansRaporService.IsRaporuSatiri).ToList();
        int S(string ad) => Array.IndexOf(FinansRaporService.IsRaporuBasliklari, ad);

        Assert.All(satirlar.Take(4), x => Assert.Equal("1200 × 800 × 600", x[S("Ölçü (mm)")]));
        Assert.All(satirlar.Skip(4), x => Assert.Null(x[S("Ölçü (mm)")]));
        Assert.Equal(["Kafes Sandık", "Palet", "Diğer", "Serbest tip", "Ahşap Kapalı", null],
            satirlar.Select(x => (string?)x[S("Sandık Cinsi")]));
        Assert.All(satirlar, x => Assert.Equal("Kişi A / Bölüm B", x[S("Talep Eden")]));
        Assert.All(satirlar, x => Assert.Equal("Not", x[S("Açıklama")]));
        // Enum ToString() yazılmaz.
        Assert.Equal("Saha Sandığı", satirlar[1][S("İş Türü")]);
        Assert.Equal("Saha", satirlar[1][S("Kategori")]);
    }

    /// <summary>
    /// Saha/yedek/ilave özel sandığında işi isteyen Ambalaj formunda "Talimat veren" alanına yazılır; eski talep eden alanı
    /// formdan kalktığı için boştur. Finans iş kaydının talep edeni talimat verendir; dolu eski talep eden önceliklidir.
    /// </summary>
    [Fact]
    public void AmbalajAktarimi_TalepEdenBossaTalimatVerenFinansinTalepEdeniOlur()
    {
        var kayit = new AmbalajUretimKaydi
        {
            IsAkisKimligi = Guid.NewGuid(), SandikNo = "1", Tur = AmbalajSandikTuru.Yedek, Adet = 1,
            SandikCinsi = AmbalajSandikCinsi.AhsapKapali, Boy = 1350, En = 800, Yukseklik = 1000,
            UretimTarihi = Tarih, TalimatVeren = "  Hikmet Gültekin "
        };

        var model = AmbalajFinansSenkronizasyonu.ModelOlustur(kayit, null);
        Assert.Equal(("Hikmet Gültekin", (string?)null), (model.TalepEdenKisi, model.TalepEdenBolum));
        Assert.Equal((1350m, 800m, 1000m), (model.Boy, model.En, model.Yukseklik));

        kayit.TalepEdenKisi = "Eski Talep";
        kayit.TalepEdenBolum = "Saha";
        model = AmbalajFinansSenkronizasyonu.ModelOlustur(kayit, null);
        Assert.Equal(("Eski Talep", "Saha"), (model.TalepEdenKisi, model.TalepEdenBolum));

        kayit.TalepEdenKisi = " ";
        kayit.TalimatVeren = null;
        Assert.Null(AmbalajFinansSenkronizasyonu.ModelOlustur(kayit, null).TalepEdenKisi);
    }

    /// <summary>
    /// Aylık raporda İlave, Saha ve Yedek proje satırları Özel İş gibi ölçü ve talep edenle yazılır: satır ölçüye göre
    /// ayrılır (aynı ölçü tek satır), talep edenler birleştirilir. Ana ambalaj proje bazında toplu kalır, ölçüsüzdür.
    /// Satırlar ayrılınca toplam tutar değişmez; PDF ve Excel aynı satırları taşır.
    /// </summary>
    [Fact]
    public void AylikRapor_IlaveSahaYedekSatiriOlcuyeGoreAyrilir_OlcuVeTalepEdenOzelIsGibiYazilir()
    {
        FinansIsKaydi Kayit(int id, FinansIsTuru tur, decimal boy, decimal en, decimal yukseklik, string? talepEden,
            string? bolum = null) => new()
        {
            Id = id, ProjeId = 10, ProjeNo = "PA702-5010", Musteri = "QA", IsTuru = tur, KaynakAktif = true, Adet = 1,
            Birim = "Adet", SandikTipi = "Ahşap Kapalı", SandikCinsi = AmbalajSandikCinsi.AhsapKapali,
            Boy = boy, En = en, Yukseklik = yukseklik, TalepEdenKisi = talepEden, TalepEdenBolum = bolum,
            FiyatlandirmaBirimiSnapshot = FinansFiyatlandirmaBirimi.Adet, BirimFiyatSnapshot = 100m, ParaBirimiSnapshot = "EUR",
            KdvOraniSnapshot = 20m, UretimTarihi = Tarih, FinansTarihi = Tarih, FinansDonemi = Tarih
        };
        var ozelIs = Kayit(8, FinansIsTuru.OzelIs, 1000, 500, 400, "Halit Kaplan");
        ozelIs.ProjeId = null;
        ozelIs.ProjeNo = "BAĞIMSIZ";
        ozelIs.IsAdi = "Özel kasa";
        var kayitlar = new[]
        {
            Kayit(1, FinansIsTuru.YedekSandik, 1800, 800, 800, "Hikmet Gültekin"),
            Kayit(2, FinansIsTuru.YedekSandik, 1800, 800, 800, "hikmet gültekin"),
            Kayit(3, FinansIsTuru.YedekSandik, 2200, 1000, 1100, "Hikmet Gültekin", "Saha"),
            Kayit(4, FinansIsTuru.SahaSandigi, 2000, 800, 800, "Ali Veli"),
            Kayit(5, FinansIsTuru.IlaveSandik, 1200, 600, 500, null),
            Kayit(6, FinansIsTuru.AnaAmbalaj, 1400, 1000, 1375, "Ana"),
            Kayit(7, FinansIsTuru.AnaAmbalaj, 1500, 1000, 1375, null),
            ozelIs
        };

        var satirlar = FinansAylikOzetService.AylikModelleriOlustur(kayitlar, new Dictionary<int, string>());

        var yedek = satirlar.Where(x => x.IsTuru == FinansIsTuru.YedekSandik).OrderBy(x => x.Boy).ToList();
        Assert.Equal(2, yedek.Count);
        Assert.Equal([1, 2], yedek[0].IsKaydiIds);
        Assert.Equal((1800m, 800m, 800m, 2m, "Hikmet Gültekin"),
            (yedek[0].Boy, yedek[0].En, yedek[0].Yukseklik, yedek[0].SandikAdedi, yedek[0].TalepEden));
        Assert.Equal([3], yedek[1].IsKaydiIds);
        Assert.Equal((2200m, "Hikmet Gültekin / Saha"), (yedek[1].Boy, yedek[1].TalepEden));
        var saha = Assert.Single(satirlar, x => x.IsTuru == FinansIsTuru.SahaSandigi);
        Assert.Equal((2000m, 800m, 800m, "Ali Veli"), (saha.Boy, saha.En, saha.Yukseklik, saha.TalepEden));
        var ilave = Assert.Single(satirlar, x => x.IsTuru == FinansIsTuru.IlaveSandik);
        Assert.Equal((1200m, (string?)null), (ilave.Boy, ilave.TalepEden));
        var ana = Assert.Single(satirlar, x => x.IsTuru == FinansIsTuru.AnaAmbalaj);
        Assert.Equal(((decimal?)null, (decimal?)null, (decimal?)null, "Ana"), (ana.Boy, ana.En, ana.Yukseklik, ana.TalepEden));
        var ozel = Assert.Single(satirlar, x => x.IsTuru == FinansIsTuru.OzelIs);
        Assert.Equal((1000m, "Halit Kaplan"), (ozel.Boy, ozel.TalepEden));
        Assert.Equal(kayitlar.Sum(FinansTutarKurallari.IsNet), satirlar.Sum(x => x.NetTutar));

        using var workbook = new XLWorkbook(new MemoryStream(FinansRaporService.AylikExcelOlustur(satirlar, 2026, 10)));
        var sheet = Assert.Single(workbook.Worksheets);
        int Sutun(string ad) => Array.IndexOf(FinansRaporService.AylikExcelBasliklari, ad) + 1;
        Assert.Equal(FinansRaporService.AylikExcelBasliklari,
            Enumerable.Range(1, FinansRaporService.AylikExcelBasliklari.Length).Select(i => sheet.Cell(4, i).GetString()));
        var excelSatirlari = Enumerable.Range(5, satirlar.Count)
            .Select(row => (Is: sheet.Cell(row, Sutun("İş")).GetString(), Boy: sheet.Cell(row, Sutun("Boy (mm)")).GetString(),
                Yukseklik: sheet.Cell(row, Sutun("Yükseklik (mm)")).GetString(), TalepEden: sheet.Cell(row, Sutun("Talep Eden")).GetString()))
            .ToList();
        Assert.Contains(("Yedek Sandık · Ahşap Kapalı", "2200", "1100", "Hikmet Gültekin / Saha"), excelSatirlari);
        Assert.Contains(("Saha Sandığı · Ahşap Kapalı", "2000", "800", "Ali Veli"), excelSatirlari);
        Assert.Contains(("Özel kasa", "1000", "400", "Halit Kaplan"), excelSatirlari);
        // Toplam satırı başlıktan bulunan sütunlara yazılır (Talep Eden sütunu eklenince kaymaz).
        var toplamSatiri = satirlar.Count + 6;
        Assert.Equal("EUR TOPLAM", sheet.Cell(toplamSatiri, Sutun("Para Birimi")).GetString());
        Assert.Equal(satirlar.Sum(x => x.NetTutar), sheet.Cell(toplamSatiri, Sutun("Net")).GetValue<decimal>());

        var pdf = FinansRaporService.AylikPdfOlustur(satirlar, 2026, 10);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
    }

    /// <summary>
    /// Gerçek akış: talimat verenle kaydedilen yedek sandığı Ambalaj aktarımıyla Finans'a girer; İşler raporu satırında ve
    /// aylık raporda Özel İş gibi ölçü ve talep eden yazılır.
    /// </summary>
    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task YedekSandik_AmbalajdanFinansa_IslerVeAylikRapordaOlcuVeTalepEdenYazilir()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            await using var db = new AppDbContext(options);
            var finans = new FinansTestServisleri(db, new Kullanici());
            var kayit = new AmbalajUretimKaydi
            {
                IsAkisKimligi = Guid.NewGuid(), ManuelProjeNo = "PA702-5010", ManuelProjeAdi = "QA", BagimsizKayitMi = true,
                Tur = AmbalajSandikTuru.Yedek, KaynakModul = AmbalajKaynakModulu.Yedek, SandikNo = "4", Ad = "Emniyet Direği",
                Adet = 1, SandikCinsi = AmbalajSandikCinsi.AhsapKapali, Boy = 2200, En = 1000, Yukseklik = 1100,
                TalimatVeren = "Hikmet Gültekin", AmbalajaDahil = true, UretimeAlindi = true,
                UretimDurumu = AmbalajUretimDurumu.Tamamlandi, UretimTarihi = Tarih,
                HesaplananBirimM3 = 2.42m, HesaplananToplamM3 = 2.42m
            };

            await finans.UretimAktarim.UretimKayitlariniAktarAsync([AmbalajFinansSenkronizasyonu.ModelOlustur(kayit, null)], default);
            db.ChangeTracker.Clear();

            var rapor = await finans.RaporVerisi.RaporVerisiAsync(new FinansListeFiltre(), default);
            var satir = FinansRaporService.IsRaporuSatiri(Assert.Single(rapor.Isler, x => x.KaynakBileseni == "NET"));
            int S(string ad) => Array.IndexOf(FinansRaporService.IsRaporuBasliklari, ad);
            Assert.Equal(("2200 × 1000 × 1100", "Hikmet Gültekin"), (satir[S("Ölçü (mm)")], satir[S("Talep Eden")]));

            using var workbook = new XLWorkbook(new MemoryStream(await finans.Raporlar.AylikExcelAsync(2026, 10, null, default)));
            var sheet = Assert.Single(workbook.Worksheets);
            int Sutun(string ad) => Array.IndexOf(FinansRaporService.AylikExcelBasliklari, ad) + 1;
            var yedekSatiri = Enumerable.Range(5, 5).Single(row => sheet.Cell(row, Sutun("İş")).GetString().StartsWith("Yedek Sandık"));
            Assert.Equal(("2200", "1000", "1100", "Hikmet Gültekin"),
                (sheet.Cell(yedekSatiri, Sutun("Boy (mm)")).GetString(), sheet.Cell(yedekSatiri, Sutun("En (mm)")).GetString(),
                    sheet.Cell(yedekSatiri, Sutun("Yükseklik (mm)")).GetString(), sheet.Cell(yedekSatiri, Sutun("Talep Eden")).GetString()));
        });
    }

    private sealed class Kullanici : ICurrentUserService { public int? UserId => 7; public bool IsAuthenticated => true; }

    [Fact]
    public void GiderRaporu_KategoriGruplu_AvansVeIptalMaliyetToplaminaGirmez_AvansAyriToplanir()
    {
        FinansGiderModel Gider(int id, string kategori, decimal matrah, bool avans = false, bool iptal = false) => new()
        {
            Id = id, Kategori = kategori, ParaBirimi = "EUR", Matrah = matrah, KdvTutari = matrah * 0.2m,
            ToplamTutar = matrah * 1.2m, AvansMi = avans, IptalEdildi = iptal, Tarih = Tarih, FinansTarihi = Tarih
        };
        var rapor = FinansRaporService.GiderRaporu(
            [Gider(1, "Kereste", 100m), Gider(2, "Kereste", 50m, avans: true), Gider(3, "Nakliye", 70m), Gider(4, "Nakliye", 999m, iptal: true), Gider(5, "", 10m)],
            FinansGiderRaporuGruplamasi.Kategori, "filtre");
        var satirlar = Satirlar(rapor);

        Assert.Equal(
            ["Kategorisiz (1 kayıt)", "Kereste (2 kayıt)", "Nakliye (2 kayıt)", "Genel toplam"],
            satirlar.Where(x => x.Tur == RaporSatirTuru.Bolum).Select(x => (string)x.Degerler[0]!));
        // Bütün giderler listelenir (iptal ve avans durum sütunuyla), toplamlara yalnız maliyet girer.
        Assert.Equal(5, satirlar.Count(x => x.Tur == RaporSatirTuru.Veri));
        Assert.Equal(["İptal"], satirlar.Where(x => x.Tur == RaporSatirTuru.Veri && (int)x.Degerler[0]! == 4).Select(x => (string?)Deger(rapor, x, "Durum")));

        var keresteAra = satirlar.Where(x => x.Tur == RaporSatirTuru.AraToplam).ElementAt(1);
        Assert.Equal(100m, Deger(rapor, keresteAra, "Matrah"));
        var genel = Assert.Single(satirlar, x => x.Tur == RaporSatirTuru.GenelToplam);
        Assert.Equal(180m, Deger(rapor, genel, "Matrah"));
        Assert.Equal("GENEL TOPLAM (avans hariç) (3 kayıt)", Deger(rapor, genel, "Kategori"));
        var avans = satirlar[^1];
        Assert.Equal(RaporSatirTuru.AraToplam, avans.Tur);
        Assert.Equal(50m, Deger(rapor, avans, "Matrah"));
    }

    [Fact]
    public void GiderRaporu_Duz_BolumBasligiYok_GenelToplamVar()
    {
        var rapor = FinansRaporService.GiderRaporu(
            [new FinansGiderModel { Id = 1, Kategori = "Kereste", ParaBirimi = "TRY", Matrah = 10m, ToplamTutar = 12m }],
            FinansGiderRaporuGruplamasi.Duz, "filtre");
        var satirlar = Satirlar(rapor);

        Assert.Equal(RaporSatirTuru.Veri, satirlar[0].Tur);
        Assert.Equal(["Genel toplam"], satirlar.Where(x => x.Tur == RaporSatirTuru.Bolum).Select(x => (string)x.Degerler[0]!));
        Assert.Equal(10m, Deger(rapor, Assert.Single(satirlar, x => x.Tur == RaporSatirTuru.GenelToplam), "Matrah"));
    }

    [Fact]
    public void FinansalDurum_OzetTutarlariKontrolPaneliyleAyni_ParaBirimiFiltresiUygulanir()
    {
        var panel = new FinansPanelModel(new(2026, 3, 1), new(2026, 4, 30), "Eksen.",
            [
                new("EUR", 1000m, 800m, 200m, 300m, 500m, 150m, 350m, 850m, 85m, 0m, 0m),
                new("TRY", 50m, 0m, 50m, 0m, 0m, 5m, -5m, 45m, 90m, 0m, 0m)
            ],
            [new("2026-01", "EUR", 1m, 1m), new("2026-03", "EUR", 200m, 50m), new("2026-04", "EUR", 300m, 100m)],
            [], [], 0, 0, 0, 0, 0,
            Alacaklar: [new("EUR", 120m, 1, 20m, 1, 0m, 0, 0, 0, 480m)],
            ProjeOzetleri: Enumerable.Range(1, 17)
                .Select(i => new FinansPanelProjeOzetiModel($"PA-{i:00}", "QA", "EUR", 1, i * 10m, 0m, 0m, 0m, 0m, 1m, FinansKarsilanma.Yok, FinansKarsilanma.Yok))
                .ToList(),
            KategoriOzetleri: [new(FinansIsKategorisi.OzelIs, "EUR", 2, 1, 1000m, 800m, 500m)]);
        var giderler = new[]
        {
            new FinansGiderModel { Id = 1, Kategori = "Kereste", ParaBirimi = "EUR", Matrah = 150m, FinansTarihi = new(2026, 3, 20) },
            new FinansGiderModel { Id = 2, Kategori = "Kereste", ParaBirimi = "EUR", Matrah = 40m, AvansMi = true, FinansTarihi = new(2026, 3, 21) }
        };
        // Dönem verisi (rapor verisi zaten dönemle süzülür): aylık trend panelin yılbaşı serisinden değil buradan gelir.
        var isler = new[]
        {
            new FinansIsKaydiModel { Id = 1, ParaBirimi = "EUR", FaturalananNetTutar = 200m, FinansTarihi = new(2026, 3, 15), KaynakAktif = true },
            new FinansIsKaydiModel { Id = 2, ParaBirimi = "EUR", FaturalananNetTutar = 300m, FinansTarihi = new(2026, 4, 2), KaynakAktif = true },
            new FinansIsKaydiModel { Id = 3, ParaBirimi = "TRY", FaturalananNetTutar = 9m, FinansTarihi = new(2026, 4, 2), KaynakAktif = true }
        };

        var rapor = FinansRaporService.FinansalDurumRaporu(panel, isler, giderler, "EUR");

        Assert.Equal(["Özet", "Gelir kırılımı", "Gider kırılımı", "Aylık trend", "Proje bazlı"], rapor.Tablolar.Select(x => x.Baslik));
        var ozet = rapor.Tablolar[0];
        Assert.Equal(["Kalem", "EUR"], ozet.Sutunlar);
        decimal Kalem(string ad) => (decimal)ozet.Satirlar.Single(x => (string)x.Degerler[0]! == ad).Degerler[1]!;
        Assert.Equal(1000m, Kalem("İş net bedeli"));
        Assert.Equal(500m, Kalem("Faturalanan net gelir"));
        Assert.Equal(150m, Kalem("Net gider (avans hariç)"));
        Assert.Equal(40m, Kalem("Avans (bilgi; maliyete dahil değil)"));
        Assert.Equal(850m, Kalem("Tahmini kâr (iş net − gider)"));
        Assert.Equal(480m, Kalem("Tahsil edilen (KDV dahil)"));
        Assert.Equal(20m, Kalem("Vadesi geçen alacak (KDV dahil)"));

        // Aylık trend dönem verisinden (avans hariç, seçili para birimi); toplamı özetle aynı. Proje bazlı ilk 15 + "Diğer".
        var aylik = rapor.Tablolar[3].Satirlar.Select(x => ((string)x.Degerler[0]!, (decimal)x.Degerler[2]!, (decimal)x.Degerler[3]!)).ToList();
        Assert.Equal([("2026-03", 200m, 150m), ("2026-04", 300m, 0m)], aylik);
        Assert.Equal((Kalem("Faturalanan net gelir"), Kalem("Net gider (avans hariç)")), (aylik.Sum(x => x.Item2), aylik.Sum(x => x.Item3)));
        var projeler = rapor.Tablolar[4].Satirlar;
        Assert.Equal(16, projeler.Count);
        Assert.Equal("PA-17", projeler[0].Degerler[0]);
        Assert.Equal("Diğer (2 proje)", projeler[^1].Degerler[0]);
        Assert.Equal(30m, projeler[^1].Degerler[3]);
    }

    [Fact]
    public void FinansalDurum_KarOraniYoksaBos_YalnizAlacagiOlanParaBirimiDeSutundur()
    {
        var panel = new FinansPanelModel(new(2026, 1, 1), new(2026, 1, 31), "Eksen.",
            [new("EUR", 0m, 0m, 0m, 0m, 0m, 10m, -10m, -10m, null, 0m, 0m)],
            [], [], [], 0, 0, 0, 0, 0,
            Alacaklar: [new("USD", 70m, 1, 0m, 0, 0m, 0, 0, 0, 0m)]);

        var ozet = FinansRaporService.FinansalDurumRaporu(panel, [], [], null).Tablolar[0];

        Assert.Equal(["Kalem", "EUR", "USD"], ozet.Sutunlar);
        var karOrani = ozet.Satirlar.Single(x => (string)x.Degerler[0]! == "Kâr oranı %");
        Assert.Null(karOrani.Degerler[1]);
        Assert.Equal(70m, ozet.Satirlar.Single(x => (string)x.Degerler[0]! == "Açık alacak (KDV dahil)").Degerler[2]);
    }

    [Fact]
    public void BolumluRapor_ExcelAyniSatirlariTasir_PdfUretilir()
    {
        var rapor = FinansRaporService.IsRaporu([Is(1, "EUR", 1000m, 1000m), Is(2, "TRY", 300m, 0m)], FinansIsRaporuKapsami.Birlesik, "filtre");
        var satirlar = Satirlar(rapor);

        using var workbook = new XLWorkbook(new MemoryStream(FinansRaporService.BolumluRaporExcel(rapor)));
        var sheet = Assert.Single(workbook.Worksheets);
        Assert.Equal(rapor.Tablolar[0].Sutunlar, Enumerable.Range(1, 29).Select(i => sheet.Cell(4, i).GetString()));
        // m³ üç ondalıkla biçimlenir (PDF ve önizlemeyle aynı hassasiyet); ilk veri satırı 6. satırdır (5. satır bölüm başlığı).
        Assert.Equal("#,##0.000", sheet.Cell(6, Array.IndexOf(FinansRaporService.IsRaporuBasliklari, "Toplam m³") + 1).Style.NumberFormat.Format);
        for (var i = 0; i < satirlar.Count; i++)
        {
            var row = 5 + i;
            Assert.Equal(Convert.ToString(satirlar[i].Degerler[satirlar[i].Tur == RaporSatirTuru.Bolum ? 0 : 1]) ?? string.Empty,
                sheet.Cell(row, satirlar[i].Tur == RaporSatirTuru.Bolum ? 1 : 2).GetString());
        }
        var sonSatir = 4 + satirlar.Count;
        Assert.StartsWith("GENEL TOPLAM", sheet.Cell(sonSatir, 2).GetString());
        Assert.True(sheet.Cell(sonSatir, 2).Style.Font.Bold);

        var pdf = FinansRaporService.BolumluRaporPdf(rapor);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
    }

    [Fact]
    public async Task Handler_GecersizKapsamVeGruplama_400_ServisCagrilmaz()
    {
        var servis = DispatchProxy.Create<IFinansRaporService, CagrilmamasiGerekenServis>();
        var handler = new FinansRaporQueryHandlers(servis);

        var isler = await handler.Handle(new FinansIsRaporuDosyaQuery { Kapsam = (FinansIsRaporuKapsami)9 }, CancellationToken.None);
        var gider = await handler.Handle(new FinansGiderRaporuDosyaQuery { Gruplama = (FinansGiderRaporuGruplamasi)9 }, CancellationToken.None);
        var onizleme = await handler.Handle(new FinansRaporOnizlemeQuery { Kapsam = (FinansIsRaporuKapsami)9 }, CancellationToken.None);
        var format = await handler.Handle(new FinansFinansalDurumRaporuDosyaQuery { Format = "docx" }, CancellationToken.None);

        Assert.Equal([400, 400, 400, 400], new[] { isler.StatusCode, gider.StatusCode, onizleme.StatusCode, format.StatusCode });
    }

    [Fact]
    public void FinansalDurum_DonemBossaYilbasindanBugune_TersAralikGecersiz()
    {
        var (baslangic, bitis) = FinansTarihKurallari.FinansalDurumDonemi(null, new DateTime(2026, 4, 30));
        Assert.Equal((new DateTime(2026, 1, 1), new DateTime(2026, 4, 30)), (baslangic, bitis));

        var dosya = new FinansFinansalDurumRaporuDosyaQueryValidator();
        Assert.True(dosya.Validate(new FinansFinansalDurumRaporuDosyaQuery()).IsValid);
        Assert.False(dosya.Validate(new FinansFinansalDurumRaporuDosyaQuery
            { Filtre = new() { Baslangic = new(2026, 5, 1), Bitis = new(2026, 4, 1) } }).IsValid);

        var onizleme = new FinansRaporOnizlemeQueryValidator();
        var ters = new FinansListeFiltre { Baslangic = new(2026, 5, 1), Bitis = new(2026, 4, 1) };
        Assert.False(onizleme.Validate(new FinansRaporOnizlemeQuery { Tur = "finansal-durum", Filtre = ters }).IsValid);
        // Diğer türlerde aralık servis kuralıyla denetlenir; doğrulayıcı yalnız finansal durum için çalışır.
        Assert.True(onizleme.Validate(new FinansRaporOnizlemeQuery { Tur = "is", Filtre = ters }).IsValid);
    }

    [Theory]
    [InlineData(FinansIsRaporuKapsami.Birlesik, "finans-isleri-2")]
    [InlineData(FinansIsRaporuKapsami.SiparisiAcilan, "finans-isleri-siparisi-acilan-")]
    [InlineData(FinansIsRaporuKapsami.SiparisiAcilmayan, "finans-isleri-siparisi-acilmayan-")]
    public async Task Handler_IsRaporuDosyaAdiKapsamiTasir(FinansIsRaporuKapsami kapsam, string onEk)
    {
        var servis = DispatchProxy.Create<IFinansRaporService, SabitDosyaServisi>();
        var sonuc = await new FinansRaporQueryHandlers(servis)
            .Handle(new FinansIsRaporuDosyaQuery { Format = "xlsx", Kapsam = kapsam }, CancellationToken.None);

        Assert.True(sonuc.IsSuccess);
        Assert.StartsWith(onEk, sonuc.Value!.DosyaAdi);
        Assert.EndsWith(".xlsx", sonuc.Value.DosyaAdi);
    }

    public class CagrilmamasiGerekenServis : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
            => throw new InvalidOperationException("Geçersiz istekte servis çağrılmamalı: " + targetMethod!.Name);
    }

    public class SabitDosyaServisi : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
            => Task.FromResult(new byte[] { 1, 2, 3 });
    }
}
