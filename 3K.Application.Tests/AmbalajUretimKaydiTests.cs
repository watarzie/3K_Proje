using _3K.Core.Entities;
using _3K.Core.Enums;

namespace _3K.Application.Tests;

/// <summary>Ambalaj kaydının karar ve iptal geçişleri entity üzerinde tek yerde uygulanır.</summary>
public sealed class AmbalajUretimKaydiTests
{
    [Fact]
    public void HaricTutmak_UretimSeciminiKaldirir_DahilEtmekSecimeDokunmaz()
    {
        var kayit = new AmbalajUretimKaydi { AmbalajaDahil = true, UretimeAlindi = true };

        kayit.AmbalajKarariniUygula(false);
        Assert.False(kayit.AmbalajaDahil);
        Assert.False(kayit.UretimeAlindi);

        kayit.AmbalajKarariniUygula(true);
        Assert.True(kayit.AmbalajaDahil);
        Assert.False(kayit.UretimeAlindi);
    }

    [Theory]
    [InlineData(null, "Kullanıcı tarafından silindi.")]
    [InlineData("Yanlış girildi", "Yanlış girildi")]
    public void KullaniciIptali_NedeniVeKullaniciyiYazar_SistemTarafindanGeriAlinmaz(string? neden, string beklenenNeden)
    {
        var kayit = new AmbalajUretimKaydi { KaynakKayitId = 5, UretimDurumu = AmbalajUretimDurumu.Uretimde };

        kayit.KullaniciAdinaIptalEt(7, neden);

        Assert.True(kayit.IptalMi);
        Assert.NotNull(kayit.IptalTarihi);
        Assert.Equal(7, kayit.IptalEdenKullaniciId);
        Assert.Equal(beklenenNeden, kayit.IptalNedeni);
        Assert.Null(kayit.IptalOncesiUretimDurumu);
        Assert.False(kayit.SistemIptaliGeriAlinabilirMi());
    }

    [Fact]
    public void SistemIptali_OncekiDurumuSaklar_GeriAlindigindaDurumuYukler()
    {
        var kayit = new AmbalajUretimKaydi { KaynakKayitId = 5, UretimDurumu = AmbalajUretimDurumu.Planlandi };
        Assert.True(kayit.KaynakEksikligindeIptalEdilebilirMi());

        kayit.KaynakEksikligindenIptalEt();

        Assert.True(kayit.IptalMi);
        Assert.Null(kayit.IptalEdenKullaniciId);
        Assert.Equal(AmbalajUretimKaydi.SistemKaynakEksigiNedeni, kayit.IptalNedeni);
        Assert.Equal(AmbalajUretimDurumu.Planlandi, kayit.IptalOncesiUretimDurumu);
        Assert.True(kayit.SistemIptaliGeriAlinabilirMi());

        kayit.SistemIptaliniGeriAl();

        Assert.False(kayit.IptalMi);
        Assert.Null(kayit.IptalTarihi);
        Assert.Null(kayit.IptalNedeni);
        Assert.Null(kayit.IptalOncesiUretimDurumu);
        Assert.Equal(AmbalajUretimDurumu.Planlandi, kayit.UretimDurumu);
    }

    [Theory]
    [InlineData(false, false, AmbalajUretimDurumu.Uretimde)]
    [InlineData(false, true, AmbalajUretimDurumu.Planlandi)]
    [InlineData(true, false, AmbalajUretimDurumu.Planlandi)]
    public void KaynakEksikligi_UretimeGirmisKilitliVeyaIptalKaydiIptalEtmez(
        bool iptalMi, bool kilitliMi, AmbalajUretimDurumu durum)
    {
        var kayit = new AmbalajUretimKaydi
        {
            KaynakKayitId = 5,
            IptalMi = iptalMi,
            KaynakSenkronizasyonuKilitliMi = kilitliMi,
            UretimDurumu = durum
        };

        Assert.False(kayit.KaynakEksikligindeIptalEdilebilirMi());
        Assert.False(new AmbalajUretimKaydi().KaynakEksikligindeIptalEdilebilirMi());
    }
}
