using Microsoft.EntityFrameworkCore;
using _3K.Application.Common;
using _3K.Application.Features.GridIslemleri.Queries;
using _3K.Application.Features.GridIslemleri.Validators;
using _3K.Core.Entities;
using _3K.Core.Constants;
using _3K.Core.Enums;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;

namespace _3K.Application.Tests;

/// <summary>
/// Grid Sahası görünümü (bellek içi; SQL kanıtı <c>GridSahaPostgresTests</c>'tedir): "Grid'de bekleyen" kuralı, SQL
/// ikizinin kuralla eşdeğerliği ve PostgreSQL'e bağlantısız çevirisi, mal kabul "Gelen" eki, istek doğrulayıcıları ve
/// mal kabul kayıtlarının ekran eşlemesi. Sorgular durum değiştirmez.
/// </summary>
public sealed class GridSahaTests
{
    internal static CekiSatiri Satir(
        decimal gridGelen,
        decimal gelen,
        GridSevkDurum sevk = GridSevkDurum.SevkEdilmedi,
        decimal? sevkMiktari = null,
        decimal? karsilanan = null,
        bool? erken = null,
        GridDurum durum = GridDurum.TamGeldi,
        UcKDurum ucK = UcKDurum.Bekliyor) => new()
    {
        GridDurumuId = (int)durum,
        GridGelenAdet = gridGelen,
        GelenMiktar = gelen,
        GridSevkDurumuId = (int)sevk,
        GridSevkMiktari = sevkMiktari,
        AktifGridSevkKarsilananMiktari = karsilanan,
        AktifGridSevkPartisiErkenSonuclandirildiMi = erken,
        UcKDurumuId = (int)ucK
    };

    [Fact]
    public void Kural_KismiPartideYoldakiGriddeSayilmaz_TamSevkEdilenBeklemez()
    {
        // 10 geldi, 6 sevk edildi, 3K 2'sini aldı: yolda 4, Grid'de bekleyen 10 − 2 − 4 = 4.
        var kismi = Satir(10, 2, GridSevkDurum.SevkEdildi, 6, 2, false);
        Assert.Equal((4m, 4m, true), (GridSahaBekleyenKurali.YoldakiMiktar(kismi), GridSahaBekleyenKurali.BekleyenMiktar(kismi), GridSahaBekleyenKurali.GriddeBekliyorMu(kismi)));

        // Tamamı yolda ya da 3K'ya ulaşmış: bekleyen yok.
        Assert.False(GridSahaBekleyenKurali.GriddeBekliyorMu(Satir(5, 0, GridSevkDurum.SevkEdildi, 5, 0, false)));
        Assert.False(GridSahaBekleyenKurali.GriddeBekliyorMu(Satir(5, 5, GridSevkDurum.SevkEdildi, 5, 5, false)));

        // Erken sonuçlandırılmış parti yolda değildir: kalan Grid'de bekler.
        Assert.Equal(3m, GridSahaBekleyenKurali.BekleyenMiktar(Satir(5, 2, GridSevkDurum.SevkEdildi, 5, 2, true)));

        // Sayaçsız (legacy) parti: 3K Tam Geldi ve parti kadar teslim aldıysa parti karşılanmış sayılır.
        Assert.Equal(4m, GridSahaBekleyenKurali.BekleyenMiktar(Satir(10, 6, GridSevkDurum.SevkEdildi, 6, null, null, ucK: UcKDurum.TamGeldi)));
        Assert.Equal(0m, GridSahaBekleyenKurali.BekleyenMiktar(Satir(10, 4, GridSevkDurum.SevkEdildi, 6, null, null, ucK: UcKDurum.EksikGeldi)));

        // Eksik Geldi: gelen kadar bekler; gelmemiş/terminal durumlar Grid'de sayılmaz.
        Assert.True(GridSahaBekleyenKurali.GriddeBekliyorMu(Satir(3, 0, durum: GridDurum.EksikGeldi)));
        foreach (var durum in new[] { GridDurum.GridKapandi, GridDurum.Iptal, GridDurum.TrafoSevk, GridDurum.Gelmedi })
            Assert.False(GridSahaBekleyenKurali.GriddeBekliyorMu(Satir(3, 0, durum: durum)));
    }

    [Fact]
    public void SqlIfadeleri_RastgeleSatirlardaSafKuralaEsittir()
    {
        var bekleyen = GridSorgulari.GriddeBekleyenIfadesi.Compile();
        var yolda = GridSorgulari.YoldakiMiktarIfadesi.Compile();
        var pozitif = GridSorgulari.BekleyenPozitifIfadesi.Compile();
        var gelmis = GridSorgulari.GridGelmisDurumIfadesi.Compile();
        var rastgele = new Random(20261023);
        var durumlar = new[] { GridDurum.Bekliyor, GridDurum.TamGeldi, GridDurum.EksikGeldi, GridDurum.GridKapandi, GridDurum.Iptal, GridDurum.TrafoSevk, GridDurum.Gelmedi };
        var sevkler = new[] { GridSevkDurum.SevkEdildi, GridSevkDurum.Bekliyor, GridSevkDurum.SevkEdilmedi, GridSevkDurum.YenidenSevkGerekli };
        var ucKlar = new[] { UcKDurum.Bekliyor, UcKDurum.TamGeldi, UcKDurum.EksikGeldi, UcKDurum.Gelmedi, UcKDurum.FazlaGeldi };
        var bayraklar = new bool?[] { null, false, true };
        decimal? Miktar() => rastgele.Next(4) == 0 ? null : rastgele.Next(0, 12) + (rastgele.Next(2) == 0 ? 0m : 0.5m);

        for (var i = 0; i < 3000; i++)
        {
            var satir = Satir(
                rastgele.Next(0, 12),
                rastgele.Next(0, 14),
                sevkler[rastgele.Next(sevkler.Length)],
                Miktar(),
                Miktar(),
                bayraklar[rastgele.Next(bayraklar.Length)],
                durumlar[rastgele.Next(durumlar.Length)],
                ucKlar[rastgele.Next(ucKlar.Length)]);
            var aciklama = $"durum {satir.GridDurumuId} sevk {satir.GridSevkDurumuId} ucK {satir.UcKDurumuId} gridGelen {satir.GridGelenAdet} " +
                           $"gelen {satir.GelenMiktar} parti {satir.GridSevkMiktari} karşılanan {satir.AktifGridSevkKarsilananMiktari} erken {satir.AktifGridSevkPartisiErkenSonuclandirildiMi}";

            Assert.True(GridSahaBekleyenKurali.YoldakiMiktar(satir) == yolda(satir), "yolda: " + aciklama);
            Assert.True(GridSahaBekleyenKurali.BekleyenMiktar(satir) == bekleyen(satir), "bekleyen: " + aciklama);
            Assert.True((GridSahaBekleyenKurali.BekleyenMiktar(satir) > 0) == pozitif(satir), "pozitif: " + aciklama);
            Assert.True(GridSahaBekleyenKurali.GridGelmisDurum(satir.GridDurumuId) == gelmis(satir), "gelmiş: " + aciklama);
        }
    }

    [Fact]
    public void BekleyenIle_ProjeksiyonaBekleyenMiktariEkler_PostgreSqlCevirisiBaglantisizUretilir()
    {
        var satir = Satir(10, 2, GridSevkDurum.SevkEdildi, 6, 2, false);
        satir.Id = 42;
        var sonuc = GridSorgulari.BekleyenIle((cs, bekleyen) => new { cs.Id, Bekleyen = bekleyen }).Compile()(satir);
        Assert.Equal((42, 4m), (sonuc.Id, sonuc.Bekleyen));

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=127.0.0.1;Database=grid_saha_ceviri;Username=kullanilmaz;Password=kullanilmaz")
            .Options;
        using var db = new AppDbContext(options);
        var sql = GridSorgulari.SevkKilidiDisinda(db.CekiSatirlari.AsNoTracking())
            .Where(GridSorgulari.GridGelmisDurumIfadesi)
            .Where(GridSorgulari.BekleyenPozitifIfadesi)
            .OrderBy(GridSorgulari.GriddeBekleyenIfadesi)
            .Select(GridSorgulari.BekleyenIle((cs, bekleyen) => new { cs.Id, Bekleyen = bekleyen }))
            .ToQueryString();

        Assert.Contains("CASE", sql);
        Assert.Contains("COALESCE", sql);
        Assert.Contains("\"AktifGridSevkPartisiErkenSonuclandirildiMi\"", sql);
        Assert.Contains("\"SevkiyatDuzeltmeAcikMi\"", sql);
    }

    [Theory]
    [InlineData(5, "Durum: TamGeldi | Sevk Durumu: SevkEdilmedi | Gelen: 5")]
    [InlineData(2.25, "Toplu TamGeldi — Açıklama yok | Gelen: 2.25")]
    public void GelenEki_YazilirVeOkunur_EskiKayittaBos(decimal gelen, string beklenen)
    {
        var onEk = beklenen[..beklenen.IndexOf(" | Gelen:", StringComparison.Ordinal)];
        Assert.Equal(beklenen, onEk + GridMalKabulAciklamaKurali.GelenEki(gelen));
        Assert.Equal(gelen, GridMalKabulAciklamaKurali.GelenMiktariniAyristir(beklenen));
        Assert.Equal(3.5m, GridMalKabulAciklamaKurali.GelenMiktariniAyristir("x | Gelen: 3,5"));
        Assert.Null(GridMalKabulAciklamaKurali.GelenMiktariniAyristir("Durum: TamGeldi | Sevk Durumu: SevkEdildi (4 Adet)"));
        Assert.Null(GridMalKabulAciklamaKurali.GelenMiktariniAyristir(null));
        // Ek her zaman sondadır: kullanıcının notundaki aynı kalıp miktar sanılmaz.
        Assert.Equal(gelen, GridMalKabulAciklamaKurali.GelenMiktariniAyristir("Durum: EksikGeldi | Not: | Gelen: 999 yazıldı" + GridMalKabulAciklamaKurali.GelenEki(gelen)));
        Assert.Null(GridMalKabulAciklamaKurali.GelenMiktariniAyristir("Durum: EksikGeldi | Not: | Gelen: 999 yazıldı"));
    }

    [Fact]
    public void Sorgular_GridSahaGorunumuOkumaIzniIster_ProjeTipindenBagimsizdir()
    {
        IRequiresMenuPermission urunler = new GetGridSahaUrunleriQuery();
        IRequiresMenuPermission malKabul = new GetGridMalKabulGecmisiQuery();
        foreach (var istek in new[] { urunler, malKabul })
        {
            Assert.IsAssignableFrom<ISecuredRequest>(istek);
            Assert.Equal(YetkiKodlari.Sandik.GridSahaGorunumu, istek.RequiredMenuKod);
            Assert.Equal(YetkiTipi.R, istek.RequiredYetkiTipi);
            // Proje/sandık bağlamlı izin yoktur: satırların Grid izni ekranda ayrıca sorulur (G-111).
            Assert.IsNotAssignableFrom<IRequiresProjectMenuPermission>(istek);
        }
    }

    [Fact]
    public void Dogrulayicilar_GecersizKapsamDurumTurVeAraligiReddeder()
    {
        var urunler = new GetGridSahaUrunleriQueryValidator();
        Assert.True(urunler.Validate(new GetGridSahaUrunleriQuery { Kapsam = GridSahaKapsami.Tumu, GridDurumuId = (int)GridDurum.EksikGeldi }).IsValid);
        Assert.False(urunler.Validate(new GetGridSahaUrunleriQuery { Kapsam = "hepsi" }).IsValid);
        Assert.False(urunler.Validate(new GetGridSahaUrunleriQuery { GridDurumuId = (int)GridDurum.GridKapandi }).IsValid);
        Assert.False(urunler.Validate(new GetGridSahaUrunleriQuery { Siralama = "gelis" }).IsValid);
        Assert.False(urunler.Validate(new GetGridSahaUrunleriQuery { PageSize = 101 }).IsValid);

        var malKabul = new GetGridMalKabulGecmisiQueryValidator();
        Assert.True(malKabul.Validate(new GetGridMalKabulGecmisiQuery { Tur = GridMalKabulTuru.Hepsi }).IsValid);
        Assert.False(malKabul.Validate(new GetGridMalKabulGecmisiQuery { Tur = "sifirlama" }).IsValid);
        Assert.False(malKabul.Validate(new GetGridMalKabulGecmisiQuery { Baslangic = new(2026, 10, 2), Bitis = new(2026, 10, 1) }).IsValid);
        Assert.False(malKabul.Validate(new GetGridMalKabulGecmisiQuery { Baslangic = new(2025, 1, 1), Bitis = new(2026, 10, 1) }).IsValid);
        Assert.True(malKabul.Validate(new GetGridMalKabulGecmisiQuery { Baslangic = new(2025, 10, 1), Bitis = new(2026, 10, 2) }).IsValid); // 366 gün
        Assert.False(malKabul.Validate(new GetGridMalKabulGecmisiQuery { Baslangic = new(2025, 10, 1), Bitis = new(2026, 10, 3) }).IsValid); // 367 gün
        // Tek uç verilince öteki uç varsayılandır (bitiş bugün): sınır delinmez.
        Assert.False(malKabul.Validate(new GetGridMalKabulGecmisiQuery { Baslangic = new(2000, 1, 1) }).IsValid);
        Assert.True(malKabul.Validate(new GetGridMalKabulGecmisiQuery { Baslangic = TurkeyTime.Now.Date.AddDays(-10) }).IsValid);
        Assert.True(malKabul.Validate(new GetGridMalKabulGecmisiQuery { Bitis = new(2026, 1, 15) }).IsValid);
        // Taşan tarih 500 değil doğrulama hatasıdır.
        Assert.False(malKabul.Validate(new GetGridMalKabulGecmisiQuery { Bitis = DateTime.MaxValue }).IsValid);
        Assert.False(malKabul.Validate(new GetGridMalKabulGecmisiQuery { Bitis = DateTime.MinValue }).IsValid);
        Assert.Equal((new DateTime(2026, 9, 23), new DateTime(2026, 10, 23)),
            new GetGridMalKabulGecmisiQuery().EtkinAralik(new DateTime(2026, 10, 23, 14, 30, 0)));
        Assert.Equal(GridHareketTuru.MalKabul, GridMalKabulTuru.Cevir(null));
        Assert.Equal(GridHareketTuru.MalKabul | GridHareketTuru.TopluSevk, GridMalKabulTuru.Cevir(GridMalKabulTuru.Hepsi));
    }

    [Fact]
    public async Task MalKabulKayitlari_UrunDurumGelenVeTopluSevkEslenir_VarsayilanSon30GunMalKabul()
    {
        var uow = new OrtakMemoryUow();
        uow.Repo<CekiSatiri>().Rows.Add(new CekiSatiri
        {
            Id = 1001, SiraNo = 3, BarkodNo = "B-1001", Aciklama = "Bağlantı Plakası", CekideGecenSandikNo = "2", FiiliSandikNo = "4", GridGelenAdet = 7
        });
        uow.Repo<Proje>().Rows.Add(new Proje { Id = 500, ProjeNo = "PA-500", ProjeTipiId = (int)ProjeTipi.Saha });
        var okuyucu = new SahteOkuyucu(
        [
            Kayit(1, "CekiSatiri", "1001", "Grid Durum Güncellendi", "10", "9", "Durum: EksikGeldi | Gelen: 3"),
            Kayit(2, "CekiSatiri", "1001", "Grid Toplu TamGeldi", "9", "8", "Toplu TamGeldi — x"),
            Kayit(3, "TopluSevk", "1001,1002", "Grid Toplu Sevk", "1", "8", "sevk", arsiv: true),
            Kayit(4, "CekiSatiri", "9999", GridHareketSabitleri.ManuelUrunIslemi, null, null, "Neden: x | Gelen: 2")
        ]);
        var lookup = new Lookup();
        var handler = new GetGridMalKabulGecmisiQueryHandler(okuyucu, uow, SynchronousReadQueryExecutor.Instance, lookup);

        var sonuc = (await handler.Handle(new GetGridMalKabulGecmisiQuery { PageSize = 10 }, CancellationToken.None)).Value!;

        Assert.Equal(GridHareketTuru.MalKabul, okuyucu.SonFiltre!.Turler);
        Assert.Equal((sonuc.Bitis.AddDays(-30), sonuc.Bitis.AddDays(1)), (okuyucu.SonFiltre.BaslangicDahil, okuyucu.SonFiltre.BitisHaric));
        var satirlar = sonuc.Liste.Items;
        Assert.Equal(("Eksik Geldi", "Gelmedi", 3m, 7m, "4", false), (satirlar[0].IslemMetni, satirlar[0].OncekiDurumMetni, satirlar[0].GelenAdet, satirlar[0].GuncelGridGelenAdet, satirlar[0].SandikNo, satirlar[0].TopluMu));
        Assert.Equal("B-1001 - Bağlantı Plakası", satirlar[0].UrunMetni);
        Assert.Equal(((int?)(int)ProjeTipi.Saha, "C-1"), (satirlar[0].ProjeTipiId, satirlar[0].Anahtar));
        Assert.Equal(("Tam Geldi", true, (decimal?)null), (satirlar[1].IslemMetni, satirlar[1].TopluMu, satirlar[1].GelenAdet));
        Assert.Equal(("Toplu sevk", "Toplu sevk (2 ürün)", (int?)2, (int?)null, "A-3"), (satirlar[2].IslemMetni, satirlar[2].UrunMetni, satirlar[2].TopluUrunSayisi, satirlar[2].CekiSatiriId, satirlar[2].Anahtar));
        Assert.Equal(("Manuel ürün", (string?)null, 2m, "Silinmiş ürün"), (satirlar[3].IslemMetni, satirlar[3].OncekiDurumMetni, satirlar[3].GelenAdet, satirlar[3].UrunMetni));

        await handler.Handle(new GetGridMalKabulGecmisiQuery { Tur = GridMalKabulTuru.TopluSevk }, CancellationToken.None);
        Assert.Equal(GridHareketTuru.TopluSevk, okuyucu.SonFiltre!.Turler);
    }

    private static GridHareketKaydi Kayit(int id, string referansTipi, string referansId, string islem, string? eski, string? yeni, string aciklama, bool arsiv = false) => new()
    {
        Id = id, ArsivdenMi = arsiv, ProjeId = 500, ProjeNo = "PA-500", ReferansTipi = referansTipi, ReferansId = referansId,
        Islem = islem, KullaniciId = 70, KullaniciAdi = "Ayşe Grid", Tarih = new DateTime(2026, 10, 20, 10, 0, 0).AddMinutes(-id),
        EskiDeger = eski, YeniDeger = yeni, Aciklama = aciklama
    };

    private sealed class SahteOkuyucu(IReadOnlyList<GridHareketKaydi> kayitlar) : IGridHareketOkuyucu
    {
        public GridHareketFiltresi? SonFiltre { get; private set; }

        public Task<GridHareketSayfasi> GridHareketleriAsync(GridHareketFiltresi filtre, CancellationToken cancellationToken = default)
        {
            SonFiltre = filtre;
            return Task.FromResult(new GridHareketSayfasi
            {
                Kayitlar = kayitlar,
                ToplamKayit = kayitlar.Count,
                PersonelOzeti = [new GridHareketPersonelOzeti(70, "Ayşe Grid", kayitlar.Count)]
            });
        }

        public Task<IReadOnlyDictionary<int, GridSonMalKabul>> SonMalKabulleriAsync(IReadOnlyCollection<int> cekiSatiriIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<int, GridSonMalKabul>>(new Dictionary<int, GridSonMalKabul>());

        public Task<int> MalKabulSayisiAsync(DateTime baslangicDahil, DateTime bitisHaric, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
    }

    private sealed class Lookup : ILookupCacheService
    {
        public string GetDeger<TLookup>(int id) where TLookup : LookupBase => typeof(TLookup) == typeof(LookupGridDurum)
            ? id switch { 8 => "Tam Geldi", 9 => "Eksik Geldi", 10 => "Gelmedi", 1 => "Bekliyor", _ => "?" }
            : "?";
        public Task WarmupAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task RefreshAsync<TLookup>(CancellationToken ct = default) where TLookup : LookupBase => Task.CompletedTask;
    }
}
