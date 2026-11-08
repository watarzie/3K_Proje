using _3K.Application.Features.SandikIslemleri.Commands;
using _3K.Application.Features.SandikIslemleri.Validators;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Models;

namespace _3K.Application.Tests;

/// <summary>
/// Sandık satırının kalıcı "Ürün bilgisi" alanı (seri no, SKT, lot): normalleştirme ve taşıma birleşimi kuralı,
/// yalnız projeye ait ve sevk kilidi olmayan sandıkta düzenleme, eski/yeni değerin hareket geçmişine yazılması,
/// Grid ile 3K aynı alanı düzenlediği için ekrandaki eski değer güncel değilse kaydın reddedilmesi (U-249).
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

    [Theory]
    [InlineData(null, null, true)]
    [InlineData(null, "   ", true)]
    [InlineData("SN-1", " SN-1 ", true)]
    [InlineData("SN-1", null, false)]
    [InlineData(null, "SN-1", false)]
    [InlineData("SN-1", "sn-1", false)]
    public void EkrandakiDegerGuncel_bosluk_farkini_yok_sayar_icerik_farkini_yakalar(string? kayitli, string? ekrandaki, bool beklenen) =>
        Assert.Equal(beklenen, SandikIcerikUrunBilgisiKurali.EkrandakiDegerGuncel(kayitli, ekrandaki));

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

    /// <summary>
    /// 3K "SN-3K" girmişken Grid ekranı bilgiyi boş görüyordu: Grid'in kaydı 3K'nın bilgisini ezmez, 409 döner;
    /// kayıt, kaydetme ve hareket geçmişi değişmez. Güncel değeri gören kullanıcı aynı değişikliği yapabilir.
    /// </summary>
    [Fact]
    public async Task Ekrandaki_eski_deger_degistiyse_409_doner_ve_kayitli_bilgi_ezilmez()
    {
        var k = new Kurgu();
        k.Icerik.UrunBilgisi = "SN-3K";

        var sonuc = await k.GuncelleAsync("SKT 2027-03", onceki: null);

        Assert.Equal(409, sonuc.StatusCode);
        Assert.Equal(SandikIcerikUrunBilgisiKurali.CakismaMesaji, sonuc.Error?.Message);
        Assert.Equal("SN-3K", k.Icerik.UrunBilgisi);
        Assert.Equal(0, k.Uow.SaveCount);
        Assert.Empty(k.Hareket.Rows);

        var guncelGorerek = await k.GuncelleAsync("SN-3K · SKT 2027-03", onceki: " SN-3K ");

        Assert.True(guncelGorerek.IsSuccess, guncelGorerek.Error?.Message);
        Assert.Equal("SN-3K · SKT 2027-03", k.Icerik.UrunBilgisi);
        var hareket = Assert.Single(k.Hareket.Rows);
        Assert.Equal(("SN-3K", "SN-3K · SKT 2027-03"), (hareket.EskiDeger, hareket.YeniDeger));
    }

    /// <summary>Kaydı uygulanmış isteğin tekrarı (çift tıklama, ağ tekrarı) eski "önceki" değeri taşır; çakışma sayılmaz.</summary>
    [Fact]
    public async Task Ayni_degerin_tekrari_cakisma_sayilmaz_ve_hicbir_sey_yazmaz()
    {
        var k = new Kurgu();
        k.Icerik.UrunBilgisi = "SN-2";

        var sonuc = await k.GuncelleAsync("SN-2", onceki: "SN-1");

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal("SN-2", k.Icerik.UrunBilgisi);
        Assert.Equal(0, k.Uow.SaveCount);
        Assert.Empty(k.Hareket.Rows);
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

    // ===== Açıklama: aynı pencere Grid ekranında Grid, 3K ekranında 3K açıklamasını düzenler =====

    [Theory]
    [InlineData(UrunAciklamaAlani.Grid, "Grid Açıklaması Güncellendi")]
    [InlineData(UrunAciklamaAlani.UcK, "3K Açıklaması Güncellendi")]
    public async Task Aciklama_ekranin_alanina_yazilir_diger_aciklamaya_ve_islem_durumuna_dokunmaz(UrunAciklamaAlani alan, string islem)
    {
        var k = new Kurgu();
        k.Satir.GridAciklama = "eski grid";
        k.Satir.UcKAciklama = "eski 3k";
        k.Satir.UcKDurumuId = (int)UcKDurum.TamGeldi;
        k.Satir.GelenMiktar = 2;

        var sonuc = await k.GuncelleAsync(null, null, alan, "  sonradan not  ", alan == UrunAciklamaAlani.Grid ? "eski grid" : "eski 3k");

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(alan == UrunAciklamaAlani.Grid ? "sonradan not" : "eski grid", k.Satir.GridAciklama);
        Assert.Equal(alan == UrunAciklamaAlani.UcK ? "sonradan not" : "eski 3k", k.Satir.UcKAciklama);
        Assert.Equal((int)UcKDurum.TamGeldi, k.Satir.UcKDurumuId);
        Assert.Equal(2, k.Satir.GelenMiktar);
        Assert.Equal(1, k.Uow.SaveCount);
        var hareket = Assert.Single(k.Hareket.Rows);
        Assert.Equal(("CekiSatiri", "23", islem), (hareket.ReferansTipi, hareket.ReferansId, hareket.Islem));
        Assert.Equal((int)IslemTipi.NotEklendi, hareket.IslemTipiId);
        Assert.Equal("sonradan not", hareket.YeniDeger);
    }

    [Fact]
    public async Task Bilgi_ve_aciklama_tek_kayitta_birlikte_yazilir_iki_hareket_dusulur()
    {
        var k = new Kurgu();

        var sonuc = await k.GuncelleAsync("SKT: 03.2027", null, UrunAciklamaAlani.UcK, "3k deneme", null);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal("SKT: 03.2027", k.Icerik.UrunBilgisi);
        Assert.Equal("3k deneme", k.Satir.UcKAciklama);
        Assert.Equal(1, k.Uow.SaveCount);
        Assert.Equal(["Ürün Bilgisi Güncellendi", "3K Açıklaması Güncellendi"], k.Hareket.Rows.Select(h => h.Islem));
    }

    [Fact]
    public async Task Bos_aciklama_siler_ayni_aciklama_hicbir_sey_yazmaz()
    {
        var k = new Kurgu();
        k.Satir.GridAciklama = "not";

        Assert.True((await k.GuncelleAsync(null, null, UrunAciklamaAlani.Grid, " not ", "not")).IsSuccess);
        Assert.Equal(0, k.Uow.SaveCount);
        Assert.Empty(k.Hareket.Rows);

        Assert.True((await k.GuncelleAsync(null, null, UrunAciklamaAlani.Grid, "   ", "not")).IsSuccess);
        Assert.Null(k.Satir.GridAciklama);
        Assert.Single(k.Hareket.Rows);
    }

    /// <summary>Açıklamada çakışma varsa ürün bilgisi de yazılmaz: kayıt hep birlikte uygulanır ya da hiç uygulanmaz.</summary>
    [Fact]
    public async Task Aciklama_ekrandaki_eski_degerden_farkliysa_409_ve_hicbir_alan_yazilmaz()
    {
        var k = new Kurgu();
        k.Satir.UcKAciklama = "başkasının notu";

        var sonuc = await k.GuncelleAsync("SN-1", null, UrunAciklamaAlani.UcK, "benim notum", null);

        Assert.Equal(409, sonuc.StatusCode);
        Assert.Equal(UrunAciklamaKurali.CakismaMesaji, sonuc.Error?.Message);
        Assert.Null(k.Icerik.UrunBilgisi);
        Assert.Equal("başkasının notu", k.Satir.UcKAciklama);
        Assert.Equal(0, k.Uow.SaveCount);
        Assert.Empty(k.Hareket.Rows);
    }

    [Fact]
    public async Task Sevk_kilitli_sandikta_aciklama_da_degistirilemez()
    {
        var k = new Kurgu();
        k.Sandik.DurumId = (int)SandikDurum.Sevkedildi;

        var sonuc = await k.GuncelleAsync(null, null, UrunAciklamaAlani.UcK, "not", null);

        Assert.False(sonuc.IsSuccess);
        Assert.Null(k.Satir.UcKAciklama);
        Assert.Equal(0, k.Uow.SaveCount);
    }

    /// <summary>Sevk sonrası düzeltmede açılan sandıkta (kilit kalkmış) açıklama eklenebilir.</summary>
    [Fact]
    public async Task Sevk_sonrasi_duzeltmedeki_sandikta_aciklama_eklenebilir()
    {
        var k = new Kurgu();
        k.Sandik.DurumId = (int)SandikDurum.Sevkedildi;
        k.Sandik.SevkiyatDuzeltmeAcikMi = true;

        var sonuc = await k.GuncelleAsync(null, null, UrunAciklamaAlani.UcK, "sonradan not", null);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal("sonradan not", k.Satir.UcKAciklama);
    }

    [Fact]
    public async Task Ceki_bagi_olmayan_satirda_aciklama_reddedilir()
    {
        var k = new Kurgu();
        k.Icerik.CekiSatiriId = null;

        var sonuc = await k.GuncelleAsync("SN-1", null, UrunAciklamaAlani.UcK, "not", null);

        Assert.Equal(UrunAciklamaKurali.CekiBagiYokMesaji, sonuc.Error?.Message);
        Assert.Null(k.Icerik.UrunBilgisi);
        Assert.Equal(0, k.Uow.SaveCount);
    }

    [Fact]
    public void Validator_bin_karakteri_asan_aciklamayi_ve_gecersiz_alani_reddeder()
    {
        var validator = new SandikIcerikBilgisiGuncelleCommandValidator();
        var komut = new SandikIcerikBilgisiGuncelleCommand
        {
            ProjeId = 1, SandikId = 2, SandikIcerikId = 3, AciklamaAlani = UrunAciklamaAlani.Grid, Aciklama = new string('x', 1000)
        };
        Assert.True(validator.Validate(komut).IsValid);
        komut.Aciklama = new string('x', 1001);
        Assert.False(validator.Validate(komut).IsValid);
        komut.Aciklama = "not";
        komut.AciklamaAlani = (UrunAciklamaAlani)9;
        Assert.False(validator.Validate(komut).IsValid);
    }

    private sealed class Kurgu
    {
        public Kurgu()
        {
            Sandik = new Sandik { Id = 30, ProjeId = 10, SandikNo = "1", DurumId = (int)SandikDurum.Hazirlaniyor };
            Satir = new CekiSatiri { Id = 23, IstenenAdet = 2 };
            Icerik = new SandikIcerik { Id = 40, SandikId = Sandik.Id, Sandik = Sandik, CekiSatiriId = 23, TahsisMiktari = 2 };
            Uow.Repo<Sandik>().Rows.Add(Sandik);
            Uow.Repo<SandikIcerik>().Rows.Add(Icerik);
            Uow.Repo<CekiSatiri>().Rows.Add(Satir);
        }

        public OrtakMemoryUow Uow { get; } = new();
        public OrtakHareket Hareket { get; } = new();
        public Sandik Sandik { get; }
        public CekiSatiri Satir { get; }
        public SandikIcerik Icerik { get; }

        public Task<_3K.Application.Common.Result> GuncelleAsync(
            string? bilgi, string? onceki, UrunAciklamaAlani alan, string? aciklama, string? oncekiAciklama) =>
            new SandikIcerikBilgisiGuncelleCommandHandler(Uow, Hareket, new OrtakUser())
                .Handle(new()
                {
                    ProjeId = 10, SandikId = Sandik.Id, SandikIcerikId = Icerik.Id,
                    UrunBilgisi = bilgi, OncekiUrunBilgisi = onceki,
                    AciklamaAlani = alan, Aciklama = aciklama, OncekiAciklama = oncekiAciklama
                }, default);

        /// <summary>Önceki değer verilmezse kullanıcı ekranda kayıtlı (güncel) değeri görüyordur.</summary>
        public Task<_3K.Application.Common.Result> GuncelleAsync(string? bilgi) => GuncelleAsync(bilgi, Icerik.UrunBilgisi);

        public Task<_3K.Application.Common.Result> GuncelleAsync(string? bilgi, string? onceki) =>
            new SandikIcerikBilgisiGuncelleCommandHandler(Uow, Hareket, new OrtakUser())
                .Handle(new()
                {
                    ProjeId = 10, SandikId = Sandik.Id, SandikIcerikId = Icerik.Id,
                    UrunBilgisi = bilgi, OncekiUrunBilgisi = onceki
                }, default);
    }
}
