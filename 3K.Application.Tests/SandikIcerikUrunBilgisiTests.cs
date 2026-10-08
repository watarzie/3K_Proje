using _3K.Application.Features.SandikIslemleri.Commands;
using _3K.Application.Features.SandikIslemleri.Validators;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Models;

namespace _3K.Application.Tests;

/// <summary>
/// Sandık satırının kalıcı "Ürün bilgisi" alanı (seri no, SKT, lot): normalleştirme ve taşıma birleşimi kuralı,
/// yalnız projeye ait ve sevk kilidi olmayan sandıkta düzenleme, eski/yeni değerin hareket geçmişine yazılması.
/// </summary>
public sealed class SandikIcerikUrunBilgisiTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("   ", null)]
    [InlineData("  SN-1  ", "SN-1")]
    public void Normalize_bos_metni_bilgi_yok_sayar_kenar_boslugunu_atar(string? girdi, string? beklenen) =>
        Assert.Equal(beklenen, SandikIcerikUrunBilgisiKurali.Normalize(girdi));

    [Theory]
    [InlineData(null, "SN-1", "SN-1")]
    [InlineData("SKT 2027", null, "SKT 2027")]
    [InlineData("SN-1", "SN-1", "SN-1")]
    [InlineData("SKT 2027", "SN-1", "SKT 2027 | SN-1")]
    public void Birlestir_iki_bilgiyi_de_korur(string? hedef, string? kaynak, string? beklenen) =>
        Assert.Equal(beklenen, SandikIcerikUrunBilgisiKurali.Birlestir(hedef, kaynak));

    [Fact]
    public void Birlestir_kolon_sinirini_asmaz()
    {
        var sonuc = SandikIcerikUrunBilgisiKurali.Birlestir(new string('a', 900), new string('b', 300));
        Assert.Equal(SandikIcerikUrunBilgisiKurali.EnFazlaUzunluk, sonuc!.Length);
    }

    [Fact]
    public void Validator_bin_karakteri_asan_bilgiyi_reddeder()
    {
        var validator = new SandikIcerikBilgisiGuncelleCommandValidator();
        var komut = new SandikIcerikBilgisiGuncelleCommand { ProjeId = 1, SandikId = 2, SandikIcerikId = 3, UrunBilgisi = new string('x', 1000) };
        Assert.True(validator.Validate(komut).IsValid);
        komut.UrunBilgisi = new string('x', 1001);
        Assert.False(validator.Validate(komut).IsValid);
    }

    [Fact]
    public async Task Guncelleme_bilgiyi_yazar_ve_eski_yeni_degeri_gecmise_isler()
    {
        var k = new Kurgu();
        k.Icerik.UrunBilgisi = "SN-1";

        var sonuc = await k.GuncelleAsync("  SN-1 · SKT 2027-03  ");

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal("SN-1 · SKT 2027-03", k.Icerik.UrunBilgisi);
        var hareket = Assert.Single(k.Hareket.Rows);
        Assert.Equal((int)IslemTipi.NotEklendi, hareket.IslemTipiId);
        Assert.Equal(("SandikIcerik", "40", "SN-1", "SN-1 · SKT 2027-03"),
            (hareket.ReferansTipi, hareket.ReferansId, hareket.EskiDeger, hareket.YeniDeger));
        Assert.Equal(7, hareket.KullaniciId);
    }

    [Fact]
    public async Task Bos_deger_bilgiyi_siler_ayni_deger_hicbir_sey_yazmaz()
    {
        var k = new Kurgu();
        k.Icerik.UrunBilgisi = "SN-1";

        Assert.True((await k.GuncelleAsync(" SN-1 ")).IsSuccess);
        Assert.Equal(0, k.Uow.SaveCount);
        Assert.Empty(k.Hareket.Rows);

        Assert.True((await k.GuncelleAsync("   ")).IsSuccess);
        Assert.Null(k.Icerik.UrunBilgisi);
        Assert.Single(k.Hareket.Rows);
    }

    [Fact]
    public async Task Sevk_kilitli_sandikta_bilgi_degistirilemez()
    {
        var k = new Kurgu();
        k.Sandik.DurumId = (int)SandikDurum.Sevkedildi;

        var sonuc = await k.GuncelleAsync("SN-1");

        Assert.False(sonuc.IsSuccess);
        Assert.Null(k.Icerik.UrunBilgisi);
        Assert.Equal(0, k.Uow.SaveCount);
        Assert.Empty(k.Hareket.Rows);
    }

    [Theory]
    [InlineData(99, 30, 40)]
    [InlineData(10, 31, 40)]
    [InlineData(10, 30, 41)]
    public async Task Baska_projenin_sandigi_veya_satiri_404_doner(int projeId, int sandikId, int icerikId)
    {
        var k = new Kurgu();
        var diger = new Sandik { Id = 31, ProjeId = 10, SandikNo = "2", DurumId = (int)SandikDurum.Hazirlaniyor };
        k.Uow.Repo<Sandik>().Rows.Add(diger);

        var sonuc = await new SandikIcerikBilgisiGuncelleCommandHandler(k.Uow, k.Hareket, new OrtakUser())
            .Handle(new() { ProjeId = projeId, SandikId = sandikId, SandikIcerikId = icerikId, UrunBilgisi = "SN-1" }, default);

        Assert.Equal(404, sonuc.StatusCode);
        Assert.Null(k.Icerik.UrunBilgisi);
        Assert.Equal(0, k.Uow.SaveCount);
    }

    private sealed class Kurgu
    {
        public Kurgu()
        {
            Sandik = new Sandik { Id = 30, ProjeId = 10, SandikNo = "1", DurumId = (int)SandikDurum.Hazirlaniyor };
            Icerik = new SandikIcerik { Id = 40, SandikId = Sandik.Id, Sandik = Sandik, CekiSatiriId = 23, TahsisMiktari = 2 };
            Uow.Repo<Sandik>().Rows.Add(Sandik);
            Uow.Repo<SandikIcerik>().Rows.Add(Icerik);
        }

        public OrtakMemoryUow Uow { get; } = new();
        public OrtakHareket Hareket { get; } = new();
        public Sandik Sandik { get; }
        public SandikIcerik Icerik { get; }

        public Task<_3K.Application.Common.Result> GuncelleAsync(string? bilgi) =>
            new SandikIcerikBilgisiGuncelleCommandHandler(Uow, Hareket, new OrtakUser())
                .Handle(new() { ProjeId = 10, SandikId = Sandik.Id, SandikIcerikId = Icerik.Id, UrunBilgisi = bilgi }, default);
    }
}
