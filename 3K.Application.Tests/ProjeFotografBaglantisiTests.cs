using Microsoft.EntityFrameworkCore;
using _3K.Application.Behaviors;
using _3K.Application.Common;
using _3K.Application.Features.ProjeIslemleri.Commands;
using _3K.Application.Features.ProjeIslemleri.Queries;
using _3K.Application.Features.ProjeIslemleri.Validators;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Models;
using _3K.Infrastructure.Data;

namespace _3K.Application.Tests;

/// <summary>
/// Proje fotoğraf sayfası bağlantısı: yalnız http/https adres kuralı, tek izin kodu (R görür/açar, W düzenler),
/// kaydetme/kaldırma ile değişiklik geçmişi ve proje listesinde izne bağlı görünürlük.
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
    public void Kural_TehlikeliVeyaBozukAdresiReddeder(string girdi) =>
        Assert.False(ProjeFotografBaglantisiKurali.Gecerli(girdi));

    [Fact]
    public void Kural_BosDegerBaglantiyiKaldirir_UzunlukSiniriUygulanir()
    {
        Assert.True(ProjeFotografBaglantisiKurali.Gecerli("   "));
        Assert.Null(ProjeFotografBaglantisiKurali.Normalize("   "));
        Assert.Null(ProjeFotografBaglantisiKurali.Normalize(null));
        var sinirda = "https://ornek.com/" + new string('a', ProjeFotografBaglantisiKurali.EnFazlaUzunluk - "https://ornek.com/".Length);
        Assert.True(ProjeFotografBaglantisiKurali.Gecerli(sinirda));
        Assert.False(ProjeFotografBaglantisiKurali.Gecerli(sinirda + "a"));
    }

    [Fact]
    public void Dogrulayici_GecersizAdresiVeProjesizIstegiReddeder_BosuKaldirmaSayar()
    {
        var dogrulayici = new ProjeFotografBaglantisiGuncelleCommandValidator();
        Assert.True(dogrulayici.Validate(new ProjeFotografBaglantisiGuncelleCommand { ProjeId = 1, Baglanti = null }).IsValid);
        Assert.True(dogrulayici.Validate(new ProjeFotografBaglantisiGuncelleCommand { ProjeId = 1, Baglanti = "foto.ornek.com/pa727" }).IsValid);
        var hatali = dogrulayici.Validate(new ProjeFotografBaglantisiGuncelleCommand { ProjeId = 1, Baglanti = "javascript:alert(1)" });
        Assert.False(hatali.IsValid);
        Assert.Contains("web adresi", Assert.Single(hatali.Errors).ErrorMessage);
        Assert.False(dogrulayici.Validate(new ProjeFotografBaglantisiGuncelleCommand { ProjeId = 0, Baglanti = null }).IsValid);
    }

    [Fact]
    public async Task Komut_YalnizFotografBaglantisiYazmaIzniyleCalisir_ProjeTipiKoduYetmez()
    {
        var komut = new ProjeFotografBaglantisiGuncelleCommand { ProjeId = 1, Baglanti = "https://foto.ornek.com/pa727" };
        Assert.Equal((YetkiKodlari.Proje.FotografBaglantisi, YetkiTipi.W), (komut.RequiredMenuKod, komut.RequiredYetkiTipi));
        Assert.True(RequestMenuPermissionResolver.HasServerDefinition(komut));

        Assert.True(await GecerMiAsync(komut, OrtakRoller.Yalniz(YetkiKodlari.Proje.FotografBaglantisi)));
        // Proje listesi ve sandık yönetimi izinleri bağlantıyı düzenlemeyi açmaz.
        Assert.False(await GecerMiAsync(komut, OrtakRoller.Haric(YetkiKodlari.Proje.FotografBaglantisi)));
    }

    [Fact]
    public async Task Komut_BaglantiyiKaydeder_DegisiklikGecmisineYazar_BosGonderilinceKaldirir()
    {
        var uow = new OrtakMemoryUow();
        uow.Repo<Proje>().Rows.Add(new Proje { Id = 10, ProjeNo = "PA727-03", Musteri = "GCA" });
        var hareket = new OrtakHareket();
        var handler = new ProjeFotografBaglantisiGuncelleCommandHandler(uow, hareket, new OrtakUser());

        var eklendi = await handler.Handle(new() { ProjeId = 10, Baglanti = " foto.ornek.com/pa727 " }, default);

        Assert.True(eklendi.IsSuccess);
        Assert.Equal("https://foto.ornek.com/pa727", eklendi.Value);
        Assert.Equal("https://foto.ornek.com/pa727", uow.Repo<Proje>().Rows.Single().FotografBaglantisi);
        var kayit = Assert.Single(hareket.Rows);
        Assert.Equal(("Fotoğraf Bağlantısı Eklendi", "Yok", "https://foto.ornek.com/pa727", 7, 10),
            (kayit.Islem, kayit.EskiDeger, kayit.YeniDeger, kayit.KullaniciId, kayit.ProjeId));

        // Aynı adres yeniden gönderilirse geçmişe yazılmaz.
        Assert.True((await handler.Handle(new() { ProjeId = 10, Baglanti = "https://foto.ornek.com/pa727" }, default)).IsSuccess);
        Assert.Single(hareket.Rows);

        var degisti = await handler.Handle(new() { ProjeId = 10, Baglanti = "https://foto.ornek.com/pa727-yeni" }, default);
        Assert.True(degisti.IsSuccess);
        Assert.Equal(("Fotoğraf Bağlantısı Güncellendi", "https://foto.ornek.com/pa727"), (hareket.Rows[^1].Islem, hareket.Rows[^1].EskiDeger));

        var kaldirildi = await handler.Handle(new() { ProjeId = 10, Baglanti = "  " }, default);
        Assert.True(kaldirildi.IsSuccess);
        Assert.Null(kaldirildi.Value);
        Assert.Null(uow.Repo<Proje>().Rows.Single().FotografBaglantisi);
        Assert.Equal(("Fotoğraf Bağlantısı Kaldırıldı", "Yok"), (hareket.Rows[^1].Islem, hareket.Rows[^1].YeniDeger));

        Assert.Equal(404, (await handler.Handle(new() { ProjeId = 99, Baglanti = "https://foto.ornek.com/x" }, default)).StatusCode);
    }

    [Fact]
    public async Task Liste_BaglantiyiYalnizOkumaIzniOlanaGosterir()
    {
        var proje = new Proje { Id = 10, ProjeNo = "PA727-03", Musteri = "GCA", FotografBaglantisi = "https://foto.ornek.com/pa727" };

        async Task<string?> ListedekiBaglanti(OrtakRoller roller, int? kullanici = 7)
        {
            var sonuc = await new ProjeListeleQueryHandler(new SahaProjeTamamlanmaRegresyonTests.ProjeRepo(proje),
                    new SahaProjeTamamlanmaRegresyonTests.LookupStub(), new SahaProjeTamamlanmaRegresyonTests.SahaStub(),
                    roller, new OrtakUser(kullanici))
                .Handle(new ProjeListeleQuery(), default);
            return Assert.Single(sonuc.Value!.Items).FotografBaglantisi;
        }

        Assert.Equal("https://foto.ornek.com/pa727", await ListedekiBaglanti(OrtakRoller.Yalniz(YetkiKodlari.Proje.FotografBaglantisi)));
        Assert.Null(await ListedekiBaglanti(OrtakRoller.Haric(YetkiKodlari.Proje.FotografBaglantisi)));
        Assert.Null(await ListedekiBaglanti(OrtakRoller.TumIzinler(), kullanici: null));
        var roller = OrtakRoller.TumIzinler();
        await ListedekiBaglanti(roller);
        Assert.Contains((YetkiKodlari.Proje.FotografBaglantisi, YetkiTipi.R), roller.Sorgulananlar);
    }

    /// <summary>
    /// Betik kolonu varchar(1000) boş olabilir olarak ekler; kök düzeyde rotasız düğümü (5256) ve yalnız Admin W iznini
    /// yazar; ikinci çalıştırmada hiçbir şey değişmez ve EF eşlemesi kolonla çalışır.
    /// </summary>
    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Postgres_Betik_KolonuVeKokDugumuEkler_YalnizAdmineYazmaVerir_TekrarCalisir()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            await using var db = new AppDbContext(options);
            // EnsureCreated kolonu modelden kurar; betiğin kolonu kendisinin eklediği sınanır.
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"Projeler\" DROP COLUMN \"FotografBaglantisi\"");
            var betik = await File.ReadAllTextAsync(FinansSekmeAgaci.BetikYolu("20261014_01_Proje_Fotograf_Baglantisi.sql"));

            await db.Database.ExecuteSqlRawAsync(betik);
            var ilkSatirlar = await db.RolYetkileri.AsNoTracking().CountAsync();
            await db.Database.ExecuteSqlRawAsync(betik);
            db.ChangeTracker.Clear();

            var kolon = await db.Database.SqlQueryRaw<string>(
                "SELECT data_type || ':' || character_maximum_length || ':' || is_nullable AS \"Value\" FROM information_schema.columns " +
                "WHERE table_name = 'Projeler' AND column_name = 'FotografBaglantisi'").SingleAsync();
            Assert.Equal("character varying:1000:YES", kolon);

            var dugum = await db.MenuTanimlari.AsNoTracking().SingleAsync(x => x.Kod == YetkiKodlari.Proje.FotografBaglantisi);
            Assert.Equal((5256, (int?)null, (string?)null, "MENU.PROJE_FOTOGRAF_BAGLANTISI"), (dugum.Id, dugum.ParentId, dugum.Route, dugum.LabelKey));
            var izin = Assert.Single(await db.RolYetkileri.AsNoTracking().Where(x => x.MenuTanimiId == 5256).ToListAsync());
            Assert.Equal((SistemRolleri.AdminId, (int)YetkiTipi.W), (izin.RolId, izin.YetkiTipiId));
            Assert.Equal(ilkSatirlar, await db.RolYetkileri.AsNoTracking().CountAsync());

            db.Projeler.Add(new Proje { ProjeNo = "PA-FOTO", Musteri = "Müşteri", FotografBaglantisi = "https://foto.ornek.com/pa-foto" });
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            Assert.Equal("https://foto.ornek.com/pa-foto",
                (await db.Projeler.AsNoTracking().SingleAsync(x => x.ProjeNo == "PA-FOTO")).FotografBaglantisi);
        });
    }

    private static async Task<bool> GecerMiAsync(ProjeFotografBaglantisiGuncelleCommand komut, OrtakRoller roller)
    {
        var gecti = false;
        var sonuc = await new AuthorizationBehavior<ProjeFotografBaglantisiGuncelleCommand, Result<string?>>(new OrtakUser(), roller)
            .Handle(komut, () => { gecti = true; return Task.FromResult(Result<string?>.Success(null)); }, CancellationToken.None);
        if (!gecti) Assert.Equal(403, sonuc.StatusCode);
        return gecti;
    }
}
