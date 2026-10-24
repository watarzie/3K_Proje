using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using _3K.Application.Features.GridIslemleri.Queries;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Repositories;
using _3K.Infrastructure.Services;

namespace _3K.Application.Tests;

/// <summary>
/// Grid Sahası görünümünün PostgreSQL sınırı (izole yerel sunucu, 127.0.0.1:55439): hareket okuyucusu canlı ve arşiv
/// tablolarını UNION ALL ile birleştirip yalnız gerçek mal kabulleri (Tam/Eksik Geldi'ye geçiş, Grid manuel ürün) seçer,
/// tarihe göre sayfalar ve personel özetini SQL'de gruplar; ürünler sorgusunun Grid'de bekleyen ifadesi (CASE/COALESCE),
/// sevk kilidi dışlaması, kapsamları, araması ve son mal kabul bilgisi gerçek SQL'de doğru çalışır. Uygulama
/// veritabanı kullanılmaz.
/// </summary>
public sealed class GridSahaPostgresTests
{
    private static readonly DateTime Gun = new(2026, 10, 20, 10, 0, 0);

    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Postgres_MalKabulKayitlari_CanliVeArsiviBirlestirir_YalnizGercekKabulleriSecer()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            await using var db = new AppDbContext(options);
            db.Kullanicilar.AddRange(Kullanici(70, "Ayşe Grid"), Kullanici(71, "Mehmet Grid"));
            db.Projeler.AddRange(
                new Proje { Id = 500, ProjeNo = "PA-500", Musteri = "M", ProjeTipiId = (int)ProjeTipi.Normal },
                new Proje { Id = 501, ProjeNo = "PA-501", Musteri = "M", ProjeTipiId = (int)ProjeTipi.Saha, DurumId = (int)ProjeDurum.SevkEdildi });
            db.HareketGecmisleri.AddRange(
                // Gelmedi → Tam Geldi: mal kabul.
                Canli(500, IslemTipi.GridDurumGuncellendi, 70, Gun, "CekiSatiri", "1001", "10", "8", "Grid Durum Güncellendi", "Durum: TamGeldi | Gelen: 5"),
                // Tam Geldi'de sevk güncellemesi (8 → 8): mal kabul değil.
                Canli(500, IslemTipi.GridDurumGuncellendi, 70, Gun.AddHours(1), "CekiSatiri", "1001", "8", "8", "Grid Durum Güncellendi", "sevk"),
                // Toplu Tam Geldi: mal kabul.
                Canli(500, IslemTipi.GridDurumGuncellendi, 71, Gun.AddHours(2), "CekiSatiri", "1002", "10", "8", "Grid Toplu TamGeldi", "Toplu | Gelen: 2"),
                // Grid İptal: mal kabul değil.
                Canli(500, IslemTipi.GridDurumGuncellendi, 71, Gun.AddHours(3), "CekiSatiri", "1003", "10", "12", "Grid Toplu Iptal", "iptal"),
                // Grid manuel ürün: mal kabul; 3K'nın manuel ürünü (aynı işlem tipi): değil.
                Canli(500, IslemTipi.ManuelUrunEklendi, 70, Gun.AddHours(4), "CekiSatiri", "1004", null, null, GridHareketSabitleri.ManuelUrunIslemi, "Neden | Gelen: 1"),
                Canli(500, IslemTipi.ManuelUrunEklendi, 70, Gun.AddHours(5), "CekiSatiri", "1005", null, null, "Manuel Ürün Eklendi", "3K"),
                // Toplu sevk: yalnız istenince.
                Canli(500, IslemTipi.GridTopluSevkEdildi, 71, Gun.AddHours(6), "TopluSevk", "1001,1002", "1", "8", "Grid Toplu Sevk", "sevk"),
                // Trafo Sevk ve toplu Grid Kapandı: sahada sonuçlanan ürün, mal kabul (24.10.2026); aynı durumda güncelleme değil.
                Canli(500, IslemTipi.GridDurumGuncellendi, 70, Gun.AddHours(7), "CekiSatiri", "1006", "10", "11", "Grid Durum Güncellendi", "Durum: TrafoSevk | Trafo: 3 | Gelen: 0"),
                Canli(500, IslemTipi.GridDurumGuncellendi, 71, Gun.AddHours(8), "CekiSatiri", "1003", "12", "14", "Grid Toplu GridKapandi", "Toplu GridKapandi — Açıklama yok"),
                Canli(500, IslemTipi.GridDurumGuncellendi, 70, Gun.AddHours(9), "CekiSatiri", "1006", "11", "11", "Grid Durum Güncellendi", "trafo düzeltme"),
                // Aralık dışı mal kabul.
                Canli(500, IslemTipi.GridDurumGuncellendi, 70, new DateTime(2026, 9, 1), "CekiSatiri", "1001", "10", "8", "Grid Durum Güncellendi", "eski"));
            db.HareketGecmisleriArsiv.Add(new HareketGecmisiArsiv
            {
                ProjeId = 501, ProjeNo = "PA-501", ReferansTipi = "CekiSatiri", ReferansId = "1001", Islem = "Grid Durum Güncellendi",
                IslemTipiId = (int)IslemTipi.GridDurumGuncellendi, KullaniciId = 71, KullaniciAdi = "Mehmet Grid", Tarih = Gun.AddMinutes(-30),
                EskiDeger = "8", YeniDeger = "9", Aciklama = "arşiv | Gelen: 4"
            });
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            IGridHareketOkuyucu okuyucu = new GridHareketOkuyucu(db);
            GridHareketFiltresi Filtre(GridHareketTuru tur = GridHareketTuru.MalKabul, int boyut = 10, int? kullanici = null, string? arama = null) =>
                new(new DateTime(2026, 10, 15), new DateTime(2026, 10, 22), tur, kullanici, null, arama, 1, boyut);

            var malKabul = await okuyucu.GridHareketleriAsync(Filtre());
            Assert.Equal(6, malKabul.ToplamKayit);
            Assert.Equal([(false, "1003"), (false, "1006"), (false, "1004"), (false, "1002"), (false, "1001"), (true, "1001")],
                malKabul.Kayitlar.Select(k => (k.ArsivdenMi, k.ReferansId!)));
            Assert.Equal(("PA-501", "Mehmet Grid"), (malKabul.Kayitlar[5].ProjeNo, malKabul.Kayitlar[5].KullaniciAdi));
            Assert.Equal([(70, "Ayşe Grid", 3), (71, "Mehmet Grid", 3)],
                malKabul.PersonelOzeti.OrderBy(p => p.KullaniciId).Select(p => (p.KullaniciId, p.KullaniciAdi, p.IslemSayisi)));

            var sayfa2 = await okuyucu.GridHareketleriAsync(Filtre(boyut: 3) with { SayfaNo = 2 });
            Assert.Equal([(false, "1002"), (false, "1001"), (true, "1001")], sayfa2.Kayitlar.Select(k => (k.ArsivdenMi, k.ReferansId!)));

            Assert.Equal(["TopluSevk"], (await okuyucu.GridHareketleriAsync(Filtre(GridHareketTuru.TopluSevk))).Kayitlar.Select(k => k.ReferansTipi));
            Assert.Equal(7, (await okuyucu.GridHareketleriAsync(Filtre(GridHareketTuru.MalKabul | GridHareketTuru.TopluSevk))).ToplamKayit);
            var personelSuzulu = await okuyucu.GridHareketleriAsync(Filtre(kullanici: 71));
            Assert.Equal(3, personelSuzulu.ToplamKayit);
            Assert.All(personelSuzulu.Kayitlar, k => Assert.Equal(71, k.KullaniciId));
            // Personel süzgeci özeti daraltmaz: seçim listesi ve özet öteki personeli de göstermeye devam eder.
            Assert.Equal([70, 71], personelSuzulu.PersonelOzeti.Select(p => p.KullaniciId).Order());
            Assert.Equal(1, (await okuyucu.GridHareketleriAsync(Filtre(arama: "pa-501"))).ToplamKayit);
            Assert.Equal(3, (await okuyucu.GridHareketleriAsync(Filtre(arama: "ayşe"))).ToplamKayit);

            // Satırın son mal kabulü (canlı ve arşiv; sevk güncellemesi ve aralık sayılmaz) ve günlük sayı.
            var son = await okuyucu.SonMalKabulleriAsync([1001, 1002, 1003, 1006, 9999]);
            Assert.Equal([1001, 1002, 1003, 1006], son.Keys.Order());
            Assert.Equal((Gun, "Ayşe Grid"), (son[1001].Tarih, son[1001].KullaniciAdi));
            // Grid Kapandı ve Trafo Sevk de satırın son mal kabulüdür; aynı durumdaki trafo düzeltmesi sayılmaz.
            Assert.Equal((Gun.AddHours(8), "Mehmet Grid"), (son[1003].Tarih, son[1003].KullaniciAdi));
            Assert.Equal(Gun.AddHours(7), son[1006].Tarih);
            Assert.Equal(6, await okuyucu.MalKabulSayisiAsync(Gun.Date, Gun.Date.AddDays(1)));
        });
    }

    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Postgres_UrunlerSorgusu_BekleyenVeTumuKapsami_SevkKilidiAramaSiralamaVeSonMalKabul()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            await using var db = new AppDbContext(options);
            db.Kullanicilar.Add(Kullanici(70, "Ayşe Grid"));
            db.Projeler.AddRange(
                new Proje { Id = 500, ProjeNo = "PA-500", Musteri = "Güney Trafo", ProjeTipiId = (int)ProjeTipi.Normal },
                new Proje { Id = 501, ProjeNo = "PA-501", Musteri = "M", ProjeTipiId = (int)ProjeTipi.Normal, DurumId = (int)ProjeDurum.SevkEdildi });
            db.Cekiler.AddRange(new Ceki { Id = 600, ProjeId = 500 }, new Ceki { Id = 601, ProjeId = 501 });
            db.Sandiklar.AddRange(
                new Sandik { Id = 700, ProjeId = 500, SandikNo = "1", DurumId = (int)SandikDurum.Hazirlaniyor },
                new Sandik { Id = 701, ProjeId = 500, SandikNo = "2", DurumId = (int)SandikDurum.Sevkedildi });
            db.CekiSatirlari.AddRange(
                // Kısmi parti: 10 geldi, 6 sevk, 2 karşılandı → yolda 4, Grid'de 4.
                Satir(1001, "3", "Bağlantı Plakası", gridGelen: 10, gelen: 2, sevk: 6, karsilanan: 2, erken: false),
                // Eksik Geldi 3, sevk yok: Grid'de 3.
                Satir(1002, "1", "Vida Seti", gridGelen: 3, gelen: 0, durum: GridDurum.EksikGeldi),
                // Tamamı 3K'ya ulaşmış Tam Geldi: yalnız "tümü" kapsamında.
                Satir(1003, "1", "Kapak", gridGelen: 5, gelen: 5, sevk: 5, karsilanan: 5, erken: false),
                // Sevk edilmiş sandıkta ve sevk edilmiş projede: hiçbir kapsamda yok.
                Satir(1004, "2", "Sevkli sandık", gridGelen: 5, gelen: 0),
                Satir(1005, "1", "Sevkli proje", gridGelen: 5, gelen: 0, cekiId: 601),
                // Grid Kapandı: sahada sonuçlanmış; yalnız "tümü" kapsamında, bekleyeni 0 (terminal).
                Satir(1006, "1", "Kapandı", gridGelen: 5, gelen: 0, durum: GridDurum.GridKapandi),
                // Trafo Sevk: 3 adet trafoya gitti (Grid gelene girmez), Grid'e gelen 2 adet 3K'ya ulaşmadı: Grid'de 2.
                TrafoSatiri(Satir(1007, "1", "Nem alıcı röle", gridGelen: 2, gelen: 0, durum: GridDurum.TrafoSevk), trafo: 3));
            db.SandikIcerikleri.Add(new SandikIcerik { SandikId = 701, CekiSatiriId = 1004, TahsisMiktari = 5 });
            db.HareketGecmisleri.Add(Canli(500, IslemTipi.GridDurumGuncellendi, 70, Gun, "CekiSatiri", "1001", "10", "8", "Grid Durum Güncellendi", "x | Gelen: 10"));
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            var handler = new GetGridSahaUrunleriQueryHandler(
                new UnitOfWork(db, NullLogger<UnitOfWork>.Instance),
                new EntityFrameworkReadQueryExecutor(),
                new Lookup(),
                new OrtakSaha(),
                new GridHareketOkuyucu(db));
            async Task<_3K.Application.Features.GridIslemleri.DTOs.GridSahaUrunleriDto> Getir(GetGridSahaUrunleriQuery q) =>
                (await handler.Handle(q, CancellationToken.None)).Value!;

            var bekleyen = await Getir(new GetGridSahaUrunleriQuery { PageSize = 50 });
            Assert.Equal([1002, 1007, 1001], bekleyen.Liste.Items.Select(x => x.CekiSatiriId)); // proje, sandık ("1" < "3"), sıra
            Assert.Equal([3m, 2m, 4m], bekleyen.Liste.Items.Select(x => x.GriddeBekleyenMiktar));
            Assert.Equal([0m, 0m, 4m], bekleyen.Liste.Items.Select(x => x.YoldakiMiktar));
            Assert.Equal([0m, 3m, 0m], bekleyen.Liste.Items.Select(x => x.TrafoSevkAdet));
            Assert.Equal((3, 9m, 1, 1), (bekleyen.Ozet.BekleyenSatir, bekleyen.Ozet.BekleyenAdet, bekleyen.Ozet.ProjeSayisi, bekleyen.Ozet.EksikGelenSatir));
            var plaka = bekleyen.Liste.Items[2];
            Assert.Equal((Gun, "Ayşe Grid", "Tam Geldi", "Adet"), (plaka.SonMalKabulTarihi!.Value, plaka.SonMalKabulPersoneli, plaka.GridDurumuMetni, plaka.Birim));
            Assert.Null(bekleyen.Liste.Items[0].SonMalKabulTarihi);

            var tumu = await Getir(new GetGridSahaUrunleriQuery { Kapsam = GridSahaKapsami.Tumu, PageSize = 50 });
            Assert.Equal([1002, 1003, 1006, 1007, 1001], tumu.Liste.Items.Select(x => x.CekiSatiriId));
            Assert.Equal(0m, tumu.Liste.Items.Single(x => x.CekiSatiriId == 1006).GriddeBekleyenMiktar); // Grid Kapandı terminal
            Assert.Equal(3, tumu.Ozet.BekleyenSatir); // özet kapsamdan bağımsız
            Assert.Equal([1006], (await Getir(new GetGridSahaUrunleriQuery { Kapsam = GridSahaKapsami.Tumu, GridDurumuId = (int)GridDurum.GridKapandi })).Liste.Items.Select(x => x.CekiSatiriId));
            Assert.Equal([1007], (await Getir(new GetGridSahaUrunleriQuery { GridDurumuId = (int)GridDurum.TrafoSevk })).Liste.Items.Select(x => x.CekiSatiriId));
            Assert.Empty((await Getir(new GetGridSahaUrunleriQuery { GridDurumuId = (int)GridDurum.GridKapandi })).Liste.Items);

            Assert.Equal([1001], (await Getir(new GetGridSahaUrunleriQuery { Arama = "bağlantı" })).Liste.Items.Select(x => x.CekiSatiriId));
            Assert.Equal(3, (await Getir(new GetGridSahaUrunleriQuery { Arama = "güney" })).Liste.TotalCount);
            Assert.Equal([1002], (await Getir(new GetGridSahaUrunleriQuery { GridDurumuId = (int)GridDurum.EksikGeldi })).Liste.Items.Select(x => x.CekiSatiriId));

            var bekleyeneGore = await Getir(new GetGridSahaUrunleriQuery { Siralama = GridSahaSiralama.Bekleyen, Azalan = true, PageSize = 1 });
            Assert.Equal([1001], bekleyeneGore.Liste.Items.Select(x => x.CekiSatiriId));
            Assert.Equal((3, 3, true), (bekleyeneGore.Liste.TotalCount, bekleyeneGore.Liste.TotalPages, bekleyeneGore.Liste.HasNextPage));
        });
    }

    private static Kullanici Kullanici(int id, string ad) => new()
    {
        Id = id, AdSoyad = ad, BasHarf = ad[..1], Email = $"k{id}@ornek.local", SifreHash = "x", RolId = 3
    };

    private static HareketGecmisi Canli(int projeId, IslemTipi tip, int kullanici, DateTime tarih, string referansTipi, string referansId,
        string? eski, string? yeni, string islem, string aciklama) => new()
    {
        ProjeId = projeId, ReferansTipi = referansTipi, ReferansId = referansId, Islem = islem, IslemTipiId = (int)tip,
        KullaniciId = kullanici, Tarih = tarih, EskiDeger = eski, YeniDeger = yeni, Aciklama = aciklama
    };

    private static CekiSatiri Satir(int id, string sandikNo, string aciklama, decimal gridGelen, decimal gelen, decimal? sevk = null,
        decimal? karsilanan = null, bool? erken = null, GridDurum durum = GridDurum.TamGeldi, int cekiId = 600) => new()
    {
        Id = id, CekiId = cekiId, SiraNo = id, BarkodNo = "B-" + id, Aciklama = aciklama, CekideGecenSandikNo = sandikNo, IstenenAdet = 10,
        BirimId = (int)Birim.Adet, GridDurumuId = (int)durum, GridGelenAdet = gridGelen, GelenMiktar = gelen,
        GridSevkDurumuId = (int)(sevk.HasValue ? GridSevkDurum.SevkEdildi : GridSevkDurum.SevkEdilmedi),
        GridSevkMiktari = sevk, AktifGridSevkKarsilananMiktari = karsilanan, AktifGridSevkPartisiErkenSonuclandirildiMi = erken,
        UcKDurumuId = (int)UcKDurum.Bekliyor, UcKKarsilamaTipiId = (int)UcKDurum.Bekliyor
    };

    private static CekiSatiri TrafoSatiri(CekiSatiri satir, decimal trafo)
    {
        satir.TrafoSevkAdet = trafo;
        return satir;
    }

    private sealed class Lookup : ILookupCacheService
    {
        public string GetDeger<TLookup>(int id) where TLookup : LookupBase =>
            typeof(TLookup) == typeof(LookupGridDurum) && id == (int)GridDurum.TamGeldi ? "Tam Geldi"
            : typeof(TLookup) == typeof(LookupBirim) && id == (int)Birim.Adet ? "Adet"
            : "?";
        public Task WarmupAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task RefreshAsync<TLookup>(CancellationToken ct = default) where TLookup : LookupBase => Task.CompletedTask;
    }
}
