using _3K.Application.Features.ProjeIslemleri.Commands;
using _3K.Application.Features.SandikIslemleri.Commands;
using _3K.Application.Features.SandikIslemleri.Validators;
using _3K.Core.Enums;
using _3K.Core.Models;

namespace _3K.Application.Tests;

/// <summary>
/// Sevk sonrası düzeltmenin saf kuralları: "önce → sonra" fark özeti ve sevkiyat kaydı korunarak açmada gerekçe.
/// Kilit, taşıma ve sandık ekleme/silme akışı ProjectLockCorrectionScopeTests ve SevkSonrasiDuzeltmePostgresTests'te.
/// </summary>
public sealed class SevkSonrasiDuzeltmeTests
{
    private static readonly SandikDuzeltmeGoruntusu Once = new()
    {
        SandikNo = "5", Ad = "Konsol", TipId = 1, En = 120, Boy = 80, Yukseklik = 100, NetKg = 50,
        Kalemler =
        [
            new() { SandikIcerikId = 1, CekiSatiriId = 10, BarkodNo = "B-10", Urun = "Konsol", Tahsis = 4, Konulan = 4,
                GridDurumuId = 8, UcKDurumuId = 2 },
            new() { SandikIcerikId = 2, CekiSatiriId = 11, BarkodNo = "B-11", Urun = "Boru", Tahsis = 2, Konulan = 2 }
        ]
    };

    [Fact]
    public void Fark_DegisiklikYoksaTekSatirDoner()
    {
        Assert.Equal([SandikDuzeltmeFarkKurali.DegisiklikYok], Hesapla(Once, Once));
    }

    [Fact]
    public void Fark_EklenenCikarilanVeDegisenUrunleriVeSandikBilgisiniYazar()
    {
        var sonra = Once with
        {
            Yukseklik = 110,
            Kalemler =
            [
                Once.Kalemler[0] with { Konulan = 3, UcKDurumuId = 1 },
                new() { SandikIcerikId = 3, CekiSatiriId = 12, BarkodNo = "B-YENI", Urun = "Unutulan", Tahsis = 2, Konulan = 2 }
            ]
        };

        Assert.Equal(
        [
            "Ölçü: 120×80×100 → 120×80×110",
            "Eklendi: Unutulan (B-YENI) — 2 adet",
            "Çıkarıldı: Boru (B-11) — 2 adet",
            "Konsol (B-10): konulan 4 → 3, 3K u2 → u1"
        ], Hesapla(Once, sonra));
    }

    [Fact]
    public void Fark_AyniSatirinBolunmusTahsisleriTekUrunSayilir()
    {
        var sonra = Once with
        {
            Kalemler =
            [
                Once.Kalemler[0] with { SandikIcerikId = 1, Konulan = 2, Tahsis = 2 },
                Once.Kalemler[0] with { SandikIcerikId = 9, Konulan = 2, Tahsis = 2 },
                Once.Kalemler[1]
            ]
        };

        Assert.Equal([SandikDuzeltmeFarkKurali.DegisiklikYok], Hesapla(Once, sonra));
    }

    [Theory]
    [InlineData(SevkiyatKilitAcmaTipi.SevkiyatKaydiKorunarakAc, null, false)]
    [InlineData(SevkiyatKilitAcmaTipi.SevkiyatKaydiKorunarakAc, "  ", false)]
    [InlineData(SevkiyatKilitAcmaTipi.SevkiyatKaydiKorunarakAc, "Konsol unutuldu", true)]
    [InlineData(SevkiyatKilitAcmaTipi.SevkiyatGeriAlinarakAc, null, true)]
    public void KorunarakAcma_GerekceIster_GeriAlinarakAcmaIstemez(SevkiyatKilitAcmaTipi tip, string? gerekce, bool gecerli)
    {
        var sandik = new SandikKilidiAcCommandValidator().Validate(
            new SandikKilidiAcCommand { ProjeId = 1, SandikId = 2, KilitAcmaTipiId = (int)tip, Aciklama = gerekce });
        var proje = new ProjeKilidiAcCommandValidator().Validate(
            new ProjeKilidiAcCommand { ProjeId = 1, KilitAcmaTipiId = (int)tip, Aciklama = gerekce });

        Assert.Equal(gecerli, sandik.IsValid);
        Assert.Equal(gecerli, proje.IsValid);
    }

    private static IReadOnlyList<string> Hesapla(SandikDuzeltmeGoruntusu once, SandikDuzeltmeGoruntusu sonra) =>
        SandikDuzeltmeFarkKurali.Hesapla(once, sonra, id => $"g{id}", id => $"u{id}", id => $"t{id}");
}
