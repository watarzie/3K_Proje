using Microsoft.Extensions.Logging.Abstractions;
using _3K.Application.Behaviors;
using _3K.Application.Common;
using _3K.Application.Features.AramaIslemleri;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Repositories;

namespace _3K.Application.Tests;

/// <summary>
/// Üst çubuktaki genel arama: istek gruplardan herhangi birinin okuma izniyle geçer; her grup (projeler, sandıklar,
/// ürünler) kendi izniyle süzülür ve proje tipi veritabanından okunur; ürünler barkodla başlayanlar, sandıklar tam sandık
/// no eşleşmesidir. Gerçek SQL izole PostgreSQL'de doğrulanır.
/// </summary>
public sealed class GenelAramaTests
{
    [Theory]
    [InlineData("pa", true)]
    [InlineData("  FC4 ", true)]
    [InlineData("a", false)]
    [InlineData("   ", false)]
    [InlineData(null, false)]
    public void Dogrulayici_EnAzIkiKarakterIster(string? q, bool gecerli)
    {
        Assert.Equal(gecerli, new GenelAramaQueryValidator().Validate(new GenelAramaQuery { Q = q }).IsValid);
        Assert.False(new GenelAramaQueryValidator().Validate(new GenelAramaQuery { Q = "PA", Limit = 51 }).IsValid);
    }

    [Fact]
    public async Task Yetki_GruplardanBirininOkumaIzniYeter_HicbiriYoksa403()
    {
        var istek = new GenelAramaQuery { Q = "PA" };
        Assert.True(RequestMenuPermissionResolver.HasServerDefinition(istek));
        Assert.Equal(MenuPermissionMatch.Any, ((IRequiresMenuPermissions)istek).PermissionMatch);
        Assert.Contains(MenuPermissionRequirement.Read(YetkiKodlari.Sandik.BarkodUrunArama), istek.RequiredMenuPermissions);
        Assert.Contains(MenuPermissionRequirement.Read(YetkiKodlari.Saha.Sandiklar), istek.RequiredMenuPermissions);

        async Task<bool> GecerMi(OrtakRoller roller)
        {
            var gecti = false;
            var sonuc = await new AuthorizationBehavior<GenelAramaQuery, Result<GenelAramaSonucuDto>>(new OrtakUser(), roller)
                .Handle(istek, () => { gecti = true; return Task.FromResult(Result<GenelAramaSonucuDto>.Success(new())); }, CancellationToken.None);
            if (!gecti) Assert.Equal(403, sonuc.StatusCode);
            return gecti;
        }
        Assert.True(await GecerMi(OrtakRoller.Yalniz(YetkiKodlari.Sandik.BarkodUrunArama)));
        Assert.True(await GecerMi(OrtakRoller.Yalniz(YetkiKodlari.Yedek.Sandiklar)));
        Assert.False(await GecerMi(OrtakRoller.Yalniz(YetkiKodlari.Dashboard, YetkiKodlari.Kullanicilar)));
    }

    /// <summary>
    /// Gerçek SQL: proje no/müşteri içinde arama (tam eşleşen önce), tam sandık no, barkodla başlayan ürünler (çeki ve
    /// elle eklenen birleşik). Saha izni olmayan kullanıcı saha projesini ve sandığını görmez; barkod izni yoksa ürün grubu
    /// boş ve aranamaz döner.
    /// </summary>
    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Postgres_GruplarIzneGoreSuzulur_ProjeSandikVeBarkodEslesir()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            await using (var db = new AppDbContext(options))
            {
                var normal = new Proje { ProjeNo = "PA727-03", Musteri = "GCA Nigeria", ProjeTipiId = (int)ProjeTipi.Normal };
                var normal2 = new Proje { ProjeNo = "PA727", Musteri = "GE", ProjeTipiId = (int)ProjeTipi.Normal };
                var saha = new Proje { ProjeNo = "PA727-03-SAHA", Musteri = "GCA", ProjeTipiId = (int)ProjeTipi.Saha };
                Sandik S(Proje p, string no) => new() { Proje = p, SandikNo = no, Ad = "Sandık " + no, DurumId = (int)SandikDurum.Hazirlaniyor, TipId = 3, DepoLokasyonId = 2 };
                db.Sandiklar.AddRange(S(normal, "12"), S(normal, "1"), S(saha, "12"), S(normal2, " 12 "));
                var ceki = new Ceki { Proje = normal, OrijinalDosyaYolu = "a.xlsx" };
                db.CekiSatirlari.AddRange(
                    new CekiSatiri { Ceki = ceki, SiraNo = 1, BarkodNo = "FC4324045", Aciklama = "Boya", IstenenAdet = 1 },
                    new CekiSatiri { Ceki = ceki, SiraNo = 2, BarkodNo = "fc4324045-01", Aciklama = "Sertleştirici", IstenenAdet = 1 },
                    new CekiSatiri { Ceki = new Ceki { Proje = normal2, OrijinalDosyaYolu = "b.xlsx" }, SiraNo = 1, BarkodNo = "FC4324045", Aciklama = "Boya", IstenenAdet = 1 },
                    new CekiSatiri { Ceki = ceki, SiraNo = 3, BarkodNo = "XFC4324", Aciklama = "Başka", IstenenAdet = 1 });
                await db.SaveChangesAsync();
                db.SandikIcerikleri.Add(new SandikIcerik { Sandik = db.Sandiklar.Local.Single(s => s.Proje == saha), BarkodNo = "FC43-ELLE", Isim = "Elle", Miktar = 1 });
                await db.SaveChangesAsync();
            }

            async Task<GenelAramaSonucuDto> Ara(string q, OrtakRoller roller)
            {
                await using var db = new AppDbContext(options);
                using var uow = new UnitOfWork(db, NullLogger<UnitOfWork>.Instance);
                var sonuc = await new GenelAramaQueryHandler(uow, new EntityFrameworkReadQueryExecutor(), roller, new OrtakUser())
                    .Handle(new GenelAramaQuery { Q = q }, default);
                Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
                return sonuc.Value!;
            }

            var hepsi = await Ara(" pa727 ", OrtakRoller.TumIzinler());
            // Tam eşleşen proje no önce; müşteri adında da aranır.
            Assert.Equal("PA727", hepsi.Projeler[0].ProjeNo);
            Assert.Equal(3, hepsi.Projeler.Count);
            Assert.Equal(2, hepsi.Projeler.Single(p => p.ProjeNo == "PA727-03").SandikSayisi);
            Assert.Equal(["GCA Nigeria"], (await Ara("nigeria", OrtakRoller.TumIzinler())).Projeler.Select(p => p.Musteri));

            var sandiklar = await Ara("12", OrtakRoller.TumIzinler());
            Assert.Equal(3, sandiklar.Sandiklar.Count);
            Assert.All(sandiklar.Sandiklar, s => Assert.Equal("12", s.SandikNo.Trim()));

            var urunler = await Ara("fc43", OrtakRoller.TumIzinler());
            Assert.Equal([("FC43-ELLE", 1), ("FC4324045", 2), ("FC4324045-01", 1)], urunler.Urunler.Select(u => (u.Barkod, u.ProjeSayisi)));
            Assert.Equal("FC4324045", (await Ara("FC4324045", OrtakRoller.TumIzinler())).Urunler[0].Barkod);

            // Saha izni yok, barkod izni yok: saha projesi/sandığı ve ürünler gelmez.
            var normalKullanici = OrtakRoller.Yalniz(YetkiKodlari.Sandik.Yonetimi);
            var kisitli = await Ara("pa727", normalKullanici);
            Assert.DoesNotContain(kisitli.Projeler, p => p.ProjeTipiId == (int)ProjeTipi.Saha);
            Assert.Equal((true, true, false), (kisitli.ProjeAranabilir, kisitli.SandikAranabilir, kisitli.UrunAranabilir));
            Assert.Empty((await Ara("fc43", normalKullanici)).Urunler);
            Assert.All((await Ara("12", normalKullanici)).Sandiklar, s => Assert.Equal((int)ProjeTipi.Normal, s.ProjeTipiId));

            // Yalnız barkod izni: proje ve sandık grupları aranamaz.
            var barkodcu = await Ara("fc43", OrtakRoller.Yalniz(YetkiKodlari.Sandik.BarkodUrunArama));
            Assert.Equal((false, false, true, 3), (barkodcu.ProjeAranabilir, barkodcu.SandikAranabilir, barkodcu.UrunAranabilir, barkodcu.Urunler.Count));
            Assert.Empty(barkodcu.Projeler);
        });
    }
}
