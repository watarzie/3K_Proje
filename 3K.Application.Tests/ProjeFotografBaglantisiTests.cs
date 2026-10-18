using Microsoft.EntityFrameworkCore;
using _3K.Application.Behaviors;
using _3K.Application.Common;
using _3K.Application.Features.ProjeIslemleri.Commands;
using _3K.Application.Features.ProjeIslemleri.DTOs;
using _3K.Application.Features.ProjeIslemleri.Queries;
using _3K.Application.Features.ProjeIslemleri.Validators;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Models;
using _3K.Infrastructure.Data;
using MediatR;

namespace _3K.Application.Tests;

/// <summary>
/// Proje fotoğraf bağlantıları (projede birden çok): yalnız http/https adres kuralı, isteğe bağlı açıklama, tek izin kodu
/// (R görür/açar, W ekler/düzenler/siler), ekleme/düzenleme/silme ile değişiklik geçmişi, aynı adres ve üst sınır
/// retleri, proje listesinde izne bağlı görünürlük ve eski tekil kolonun yeni tabloya taşınması.
/// </summary>
public sealed class ProjeFotografBaglantisiTests
{
    [Theory]
    [InlineData("https://foto.ornek.com/projeler/PA727-03", "https://foto.ornek.com/projeler/PA727-03")]
    [InlineData("  foto.ornek.com/pa727  ", "https://foto.ornek.com/pa727")]
    [InlineData("http://192.168.1.20/galeri?proje=PA727", "http://192.168.1.20/galeri?proje=PA727")]
    public void Kural_HttpVeHttpsAdresiniKabulEder_SemasizAdreseHttpsEkler(string girdi, string beklenen)
    {
        Assert.True(ProjeFotografBaglantisiKurali.Gecerli(girdi));
        Assert.Equal(beklenen, ProjeFotografBaglantisiKurali.Normalize(girdi));
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("JAVASCRIPT://alert(1)")]
    [InlineData("data:text/html,<script>alert(1)</script>")]
    [InlineData("ftp://foto.ornek.com/pa727")]
    [InlineData("file:///C:/fotograflar")]
    [InlineData("https://foto ornek.com/pa727")]
    [InlineData("https://")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Kural_TehlikeliBozukVeyaBosAdresiReddeder(string? girdi) =>
        Assert.False(ProjeFotografBaglantisiKurali.Gecerli(girdi));

    [Fact]
    public void Kural_UzunlukSinirlari_AciklamaKirpilir()
    {
        var sinirda = "https://ornek.com/" + new string('a', ProjeFotografBaglantisiKurali.EnFazlaUzunluk - "https://ornek.com/".Length);
        Assert.True(ProjeFotografBaglantisiKurali.Gecerli(sinirda));
        Assert.False(ProjeFotografBaglantisiKurali.Gecerli(sinirda + "a"));

        Assert.Null(ProjeFotografBaglantisiKurali.AciklamaNormalize("   "));
        Assert.Equal("Paketleme", ProjeFotografBaglantisiKurali.AciklamaNormalize("  Paketleme "));
        Assert.True(ProjeFotografBaglantisiKurali.AciklamaGecerli(null));
        Assert.True(ProjeFotografBaglantisiKurali.AciklamaGecerli(" " + new string('a', ProjeFotografBaglantisiKurali.AciklamaEnFazlaUzunluk) + " "));
        Assert.False(ProjeFotografBaglantisiKurali.AciklamaGecerli(new string('a', ProjeFotografBaglantisiKurali.AciklamaEnFazlaUzunluk + 1)));
    }

    [Fact]
    public void Dogrulayicilar_GecersizAdresiUzunAciklamayiVeKimliksizIstegiReddeder()
    {
        var kaydet = new ProjeFotografBaglantisiKaydetCommandValidator();
        Assert.True(kaydet.Validate(new ProjeFotografBaglantisiKaydetCommand { ProjeId = 1, Adres = "foto.ornek.com/pa727" }).IsValid);
        Assert.True(kaydet.Validate(new ProjeFotografBaglantisiKaydetCommand { ProjeId = 1, BaglantiId = 4, Adres = "https://foto.ornek.com/pa727", Aciklama = "Paketleme" }).IsValid);

        var hatali = kaydet.Validate(new ProjeFotografBaglantisiKaydetCommand { ProjeId = 1, Adres = "javascript:alert(1)" });
        Assert.Contains("web adresi", Assert.Single(hatali.Errors).ErrorMessage);
        Assert.False(kaydet.Validate(new ProjeFotografBaglantisiKaydetCommand { ProjeId = 1, Adres = " " }).IsValid);
        Assert.Contains("Açıklama", Assert.Single(kaydet.Validate(new ProjeFotografBaglantisiKaydetCommand
        {
            ProjeId = 1, Adres = "https://foto.ornek.com/pa727", Aciklama = new string('a', ProjeFotografBaglantisiKurali.AciklamaEnFazlaUzunluk + 1)
        }).Errors).ErrorMessage);
        Assert.False(kaydet.Validate(new ProjeFotografBaglantisiKaydetCommand { ProjeId = 0, Adres = "https://foto.ornek.com/pa727" }).IsValid);
        Assert.False(kaydet.Validate(new ProjeFotografBaglantisiKaydetCommand { ProjeId = 1, BaglantiId = 0, Adres = "https://foto.ornek.com/pa727" }).IsValid);

        var sil = new ProjeFotografBaglantisiSilCommandValidator();
        Assert.True(sil.Validate(new ProjeFotografBaglantisiSilCommand { ProjeId = 1, BaglantiId = 4 }).IsValid);
        Assert.False(sil.Validate(new ProjeFotografBaglantisiSilCommand { ProjeId = 1, BaglantiId = 0 }).IsValid);
        Assert.False(sil.Validate(new ProjeFotografBaglantisiSilCommand { ProjeId = 0, BaglantiId = 4 }).IsValid);
    }

    [Fact]
    public async Task Komutlar_YalnizFotografBaglantisiYazmaIzniyleCalisir_ProjeTipiKoduYetmez()
    {
        var kaydet = new ProjeFotografBaglantisiKaydetCommand { ProjeId = 1, Adres = "https://foto.ornek.com/pa727" };
        var sil = new ProjeFotografBaglantisiSilCommand { ProjeId = 1, BaglantiId = 4 };
        Assert.Equal((YetkiKodlari.Proje.FotografBaglantisi, YetkiTipi.W), (kaydet.RequiredMenuKod, kaydet.RequiredYetkiTipi));
        Assert.Equal((YetkiKodlari.Proje.FotografBaglantisi, YetkiTipi.W), (sil.RequiredMenuKod, sil.RequiredYetkiTipi));
        Assert.True(RequestMenuPermissionResolver.HasServerDefinition(kaydet));
        Assert.True(RequestMenuPermissionResolver.HasServerDefinition(sil));

        Assert.True(await GecerMiAsync(kaydet, OrtakRoller.Yalniz(YetkiKodlari.Proje.FotografBaglantisi)));
        Assert.True(await GecerMiAsync(sil, OrtakRoller.Yalniz(YetkiKodlari.Proje.FotografBaglantisi)));
        // Proje listesi ve sandık yönetimi izinleri bağlantıları düzenlemeyi açmaz.
        Assert.False(await GecerMiAsync(kaydet, OrtakRoller.Haric(YetkiKodlari.Proje.FotografBaglantisi)));
        Assert.False(await GecerMiAsync(sil, OrtakRoller.Haric(YetkiKodlari.Proje.FotografBaglantisi)));
    }

    [Fact]
    public async Task Komutlar_BirdenCokBaglantiEklerDuzenlerSiler_HerDegisiklikGecmiseYazilir()
    {
        var uow = new OrtakMemoryUow();
        uow.Repo<Proje>().Rows.AddRange([new Proje { Id = 10, ProjeNo = "PA727-03" }, new Proje { Id = 11, ProjeNo = "PA727-04" }]);
        var baglantilar = uow.Repo<ProjeFotografBaglantisi>().Rows;
        var hareket = new OrtakHareket();
        var kaydet = new ProjeFotografBaglantisiKaydetCommandHandler(uow, hareket, new OrtakUser());
        var sil = new ProjeFotografBaglantisiSilCommandHandler(uow, hareket, new OrtakUser());

        var ilk = await kaydet.Handle(new() { ProjeId = 10, Adres = " foto.ornek.com/pa727 " }, default);
        var ikinci = await kaydet.Handle(new() { ProjeId = 10, Adres = "https://drive.ornek.com/sevk", Aciklama = " Sevk günü " }, default);

        Assert.True(ilk.IsSuccess);
        Assert.Equal([("https://foto.ornek.com/pa727", (string?)null), ("https://drive.ornek.com/sevk", "Sevk günü")],
            ikinci.Value!.Select(x => (x.Adres, x.Aciklama)));
        Assert.All(baglantilar, x => Assert.Equal(10, x.ProjeId));
        Assert.Equal([("Fotoğraf Bağlantısı Eklendi", "Yok", "https://foto.ornek.com/pa727", 7, 10),
                      ("Fotoğraf Bağlantısı Eklendi", "Yok", "Sevk günü · https://drive.ornek.com/sevk", 7, 10)],
            hareket.Rows.Select(x => (x.Islem, x.EskiDeger, x.YeniDeger, x.KullaniciId, x.ProjeId)));

        // Aynı değerlerle yeniden kaydetmek geçmişe yazılmaz.
        var ilkId = ilk.Value!.Single().Id;
        Assert.True((await kaydet.Handle(new() { ProjeId = 10, BaglantiId = ilkId, Adres = "https://foto.ornek.com/pa727" }, default)).IsSuccess);
        Assert.Equal(2, hareket.Rows.Count);

        var duzenlendi = await kaydet.Handle(new() { ProjeId = 10, BaglantiId = ilkId, Adres = "https://foto.ornek.com/pa727-yeni", Aciklama = "Paketleme" }, default);
        Assert.True(duzenlendi.IsSuccess);
        Assert.Equal(("Paketleme", "https://foto.ornek.com/pa727-yeni"), (duzenlendi.Value![0].Aciklama, duzenlendi.Value[0].Adres));
        Assert.Equal(("Fotoğraf Bağlantısı Güncellendi", "https://foto.ornek.com/pa727", "Paketleme · https://foto.ornek.com/pa727-yeni"),
            (hareket.Rows[^1].Islem, hareket.Rows[^1].EskiDeger, hareket.Rows[^1].YeniDeger));

        var silindi = await sil.Handle(new() { ProjeId = 10, BaglantiId = ilkId }, default);
        Assert.True(silindi.IsSuccess);
        Assert.Equal(["https://drive.ornek.com/sevk"], silindi.Value!.Select(x => x.Adres));
        Assert.Equal(["https://drive.ornek.com/sevk"], baglantilar.Select(x => x.Adres));
        Assert.Equal(("Fotoğraf Bağlantısı Kaldırıldı", "Paketleme · https://foto.ornek.com/pa727-yeni", "Yok"),
            (hareket.Rows[^1].Islem, hareket.Rows[^1].EskiDeger, hareket.Rows[^1].YeniDeger));
    }

    [Fact]
    public async Task Komutlar_AyniAdresiBaskaProjeninBaglantisiniVeUstSiniriReddeder_VeriDegismez()
    {
        var uow = new OrtakMemoryUow();
        uow.Repo<Proje>().Rows.AddRange([new Proje { Id = 10 }, new Proje { Id = 11 }]);
        var baglantilar = uow.Repo<ProjeFotografBaglantisi>().Rows;
        baglantilar.AddRange([
            new ProjeFotografBaglantisi { Id = 1, ProjeId = 10, Adres = "https://foto.ornek.com/a" },
            new ProjeFotografBaglantisi { Id = 2, ProjeId = 10, Adres = "https://foto.ornek.com/b" },
            new ProjeFotografBaglantisi { Id = 3, ProjeId = 11, Adres = "https://foto.ornek.com/baska" }]);
        var hareket = new OrtakHareket();
        var kaydet = new ProjeFotografBaglantisiKaydetCommandHandler(uow, hareket, new OrtakUser());
        var sil = new ProjeFotografBaglantisiSilCommandHandler(uow, hareket, new OrtakUser());

        // Aynı adres ekleme ve başka bağlantıyı aynı adrese çevirme çakışmadır; başka projede aynı adres serbesttir.
        Assert.Equal(409, (await kaydet.Handle(new() { ProjeId = 10, Adres = "foto.ornek.com/a" }, default)).StatusCode);
        Assert.Equal(409, (await kaydet.Handle(new() { ProjeId = 10, BaglantiId = 2, Adres = "https://foto.ornek.com/a" }, default)).StatusCode);
        // Başka projenin bağlantısı bu projeden düzenlenemez/silinmez; olmayan proje ve bağlantı bulunamaz.
        Assert.Equal(404, (await kaydet.Handle(new() { ProjeId = 10, BaglantiId = 3, Adres = "https://foto.ornek.com/c" }, default)).StatusCode);
        Assert.Equal(404, (await sil.Handle(new() { ProjeId = 10, BaglantiId = 3 }, default)).StatusCode);
        Assert.Equal(404, (await sil.Handle(new() { ProjeId = 10, BaglantiId = 99 }, default)).StatusCode);
        Assert.Equal(404, (await kaydet.Handle(new() { ProjeId = 99, Adres = "https://foto.ornek.com/c" }, default)).StatusCode);
        Assert.Equal(["https://foto.ornek.com/a", "https://foto.ornek.com/b", "https://foto.ornek.com/baska"], baglantilar.Select(x => x.Adres));
        Assert.Empty(hareket.Rows);
        Assert.True((await kaydet.Handle(new() { ProjeId = 11, Adres = "https://foto.ornek.com/a" }, default)).IsSuccess);

        for (var i = baglantilar.Count(x => x.ProjeId == 10); i < ProjeFotografBaglantisiKurali.EnFazlaBaglanti; i++)
            Assert.True((await kaydet.Handle(new() { ProjeId = 10, Adres = $"https://foto.ornek.com/{i}" }, default)).IsSuccess);
        var fazla = await kaydet.Handle(new() { ProjeId = 10, Adres = "https://foto.ornek.com/fazla" }, default);
        Assert.Equal(400, fazla.StatusCode);
        Assert.Contains("en fazla 20", fazla.Error!.Message);
        Assert.Equal(ProjeFotografBaglantisiKurali.EnFazlaBaglanti, baglantilar.Count(x => x.ProjeId == 10));
        // Sınırdayken var olan bağlantı düzenlenebilir.
        Assert.True((await kaydet.Handle(new() { ProjeId = 10, BaglantiId = 1, Adres = "https://foto.ornek.com/a2" }, default)).IsSuccess);
    }

    [Fact]
    public async Task Liste_BaglantilariYalnizOkumaIzniOlanaEklenmeSirasiylaGosterir()
    {
        var proje = new Proje
        {
            Id = 10, ProjeNo = "PA727-03", Musteri = "GCA",
            FotografBaglantilari =
            [
                new ProjeFotografBaglantisi { Id = 8, ProjeId = 10, Adres = "https://foto.ornek.com/sevk", Aciklama = "Sevk" },
                new ProjeFotografBaglantisi { Id = 5, ProjeId = 10, Adres = "https://foto.ornek.com/pa727" },
            ]
        };

        async Task<List<ProjeFotografBaglantisiDto>> ListedekiBaglantilar(OrtakRoller roller, int? kullanici = 7)
        {
            var sonuc = await new ProjeListeleQueryHandler(new SahaProjeTamamlanmaRegresyonTests.ProjeRepo(proje),
                    new SahaProjeTamamlanmaRegresyonTests.LookupStub(), new SahaProjeTamamlanmaRegresyonTests.SahaStub(),
                    roller, new OrtakUser(kullanici))
                .Handle(new ProjeListeleQuery(), default);
            return Assert.Single(sonuc.Value!.Items).FotografBaglantilari;
        }

        Assert.Equal([(5, "https://foto.ornek.com/pa727", (string?)null), (8, "https://foto.ornek.com/sevk", "Sevk")],
            (await ListedekiBaglantilar(OrtakRoller.Yalniz(YetkiKodlari.Proje.FotografBaglantisi))).Select(x => (x.Id, x.Adres, x.Aciklama)));
        Assert.Empty(await ListedekiBaglantilar(OrtakRoller.Haric(YetkiKodlari.Proje.FotografBaglantisi)));
        Assert.Empty(await ListedekiBaglantilar(OrtakRoller.TumIzinler(), kullanici: null));
        var roller = OrtakRoller.TumIzinler();
        await ListedekiBaglantilar(roller);
        Assert.Contains((YetkiKodlari.Proje.FotografBaglantisi, YetkiTipi.R), roller.Sorgulananlar);
    }

    /// <summary>
    /// Önceki sürümde 01 betiği tekil kolonu ekler, düğümü (5256) ve yalnız Admin W iznini yazar; 02 düğümü Projeler altına
    /// taşır. 20261018_01 dolu tekil değerleri (kırpılmış, açıklamasız) yeni tabloya taşır, boşları atlar ve kolonu kaldırır.
    /// Üç betik ikinci kez çalıştırılınca hiçbir şey değişmez (01 tablo varken kolonu geri eklemez). EF eşlemesi tabloyla
    /// çalışır; proje silinince bağlantıları da silinir.
    /// </summary>
    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Postgres_Betikler_TekilBaglantiyiTasiyipKolonuKaldirir_TekrarCalisir_ProjeSilinceBaglantilarSilinir()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            await using var db = new AppDbContext(options);
            db.Projeler.AddRange(
                new Proje { ProjeNo = "PA-FOTO", Musteri = "Müşteri" },
                new Proje { ProjeNo = "PA-BOS", Musteri = "Müşteri" },
                new Proje { ProjeNo = "PA-YOK", Musteri = "Müşteri" });
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            int Id(string projeNo) => db.Projeler.AsNoTracking().Single(x => x.ProjeNo == projeNo).Id;

            // Önceki sürüm: çoklu tablo yok; EnsureCreated tabloyu modelden kurduğu için kaldırılır.
            await db.Database.ExecuteSqlRawAsync("DROP TABLE \"ProjeFotografBaglantilari\"");
            var betikler = await Task.WhenAll(new[]
            {
                "20261014_01_Proje_Fotograf_Baglantisi.sql",
                "20261014_02_Proje_Fotograf_Baglantisi_Projeler_Altina.sql",
                "20261018_01_Proje_Fotograf_Baglantilari.sql",
            }.Select(x => File.ReadAllTextAsync(FinansSekmeAgaci.BetikYolu(x))));
            await db.Database.ExecuteSqlRawAsync(betikler[0]);
            await db.Database.ExecuteSqlRawAsync(betikler[1]);
            await db.Database.ExecuteSqlRawAsync(
                "UPDATE \"Projeler\" SET \"FotografBaglantisi\" = CASE \"ProjeNo\" WHEN 'PA-FOTO' THEN ' https://foto.ornek.com/pa-foto ' " +
                "WHEN 'PA-BOS' THEN '  ' END");

            await db.Database.ExecuteSqlRawAsync(betikler[2]);
            var ilkSatirlar = await db.RolYetkileri.AsNoTracking().CountAsync();
            foreach (var betik in betikler)
                await db.Database.ExecuteSqlRawAsync(betik);
            db.ChangeTracker.Clear();

            Assert.Equal(0, await db.Database.SqlQueryRaw<int>(
                "SELECT count(*)::int AS \"Value\" FROM information_schema.columns " +
                "WHERE table_name = 'Projeler' AND column_name = 'FotografBaglantisi'").SingleAsync());
            var tasinan = Assert.Single(await db.Set<ProjeFotografBaglantisi>().AsNoTracking().ToListAsync());
            Assert.Equal((Id("PA-FOTO"), "https://foto.ornek.com/pa-foto", (string?)null), (tasinan.ProjeId, tasinan.Adres, tasinan.Aciklama));
            Assert.Equal(ilkSatirlar, await db.RolYetkileri.AsNoTracking().CountAsync());
            var izin = Assert.Single(await db.RolYetkileri.AsNoTracking().Where(x => x.MenuTanimiId == 5256).ToListAsync());
            Assert.Equal((SistemRolleri.AdminId, (int)YetkiTipi.W), (izin.RolId, izin.YetkiTipiId));

            db.Set<ProjeFotografBaglantisi>().Add(new ProjeFotografBaglantisi
            {
                ProjeId = Id("PA-FOTO"), Adres = "https://drive.ornek.com/sevk", Aciklama = "Sevk günü"
            });
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            Assert.Equal(2, (await db.Projeler.AsNoTracking().Include(x => x.FotografBaglantilari)
                .SingleAsync(x => x.ProjeNo == "PA-FOTO")).FotografBaglantilari.Count);

            await db.Projeler.Where(x => x.ProjeNo == "PA-FOTO").ExecuteDeleteAsync();
            Assert.Empty(await db.Set<ProjeFotografBaglantisi>().AsNoTracking().ToListAsync());
        });
    }

    private static async Task<bool> GecerMiAsync<TIstek>(TIstek komut, OrtakRoller roller)
        where TIstek : IRequest<Result<List<ProjeFotografBaglantisiDto>>>
    {
        var gecti = false;
        var sonuc = await new AuthorizationBehavior<TIstek, Result<List<ProjeFotografBaglantisiDto>>>(new OrtakUser(), roller)
            .Handle(komut, () => { gecti = true; return Task.FromResult(Result<List<ProjeFotografBaglantisiDto>>.Success([])); }, CancellationToken.None);
        if (!gecti) Assert.Equal(403, sonuc.StatusCode);
        return gecti;
    }
}
