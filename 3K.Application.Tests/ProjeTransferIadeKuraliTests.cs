using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Models;

namespace _3K.Application.Tests;

/// <summary>
/// Transfer geri alındığında donör satırın yeniden sevk borcu (U-180). Canlı vaka: PA598-01 ürünü PA598-02'ye
/// verince borç açıldı, transfer geri alınınca borç kapanmadığı için karşılanmış satır "Yeniden Sevk Gerekli" kaldı.
/// </summary>
public sealed class ProjeTransferIadeKuraliTests
{
    private static CekiSatiri Donor(decimal istenen, decimal gelen, decimal borc, decimal? gridSevk = 2m) => new()
    {
        Id = 1,
        IstenenAdet = istenen,
        GelenMiktar = gelen,
        GridDurumuId = (int)GridDurum.TamGeldi,
        GridSevkMiktari = gridSevk,
        GridSevkDurumuId = (int)GridSevkDurum.YenidenSevkGerekli,
        YenidenSevkGerekliAdet = borc
    };

    [Fact]
    public void KalanKapandiysa_TransferinActigiBorcKapanirVeSevkEdildiyeDoner()
    {
        var donor = Donor(istenen: 2m, gelen: 2m, borc: 2m);

        ProjeTransferIadeKurali.DonorYenidenSevkBorcunuDuzelt(donor);

        Assert.Equal(0m, donor.YenidenSevkGerekliAdet);
        Assert.Equal((int)GridSevkDurum.SevkEdildi, donor.GridSevkDurumuId);
    }

    [Fact]
    public void BaskaNedenleKalanVarsa_BorcKalanaInerVeYenidenSevkGerekliKalir()
    {
        // 3 adet geri gönderim borcu + transferin açtığı 1 adet; transfer geri alınınca kalan 3.
        var donor = Donor(istenen: 4m, gelen: 1m, borc: 4m);

        ProjeTransferIadeKurali.DonorYenidenSevkBorcunuDuzelt(donor);

        Assert.Equal(3m, donor.YenidenSevkGerekliAdet);
        Assert.Equal((int)GridSevkDurum.YenidenSevkGerekli, donor.GridSevkDurumuId);
    }

    [Fact]
    public void BorcKalandanAzsa_Degismez()
    {
        var donor = Donor(istenen: 4m, gelen: 1m, borc: 2m);

        ProjeTransferIadeKurali.DonorYenidenSevkBorcunuDuzelt(donor);

        Assert.Equal(2m, donor.YenidenSevkGerekliAdet);
        Assert.Equal((int)GridSevkDurum.YenidenSevkGerekli, donor.GridSevkDurumuId);
    }

    [Fact]
    public void AktifSevkPartisiYoksa_BorcKapanincaBekliyoraDoner()
    {
        var donor = Donor(istenen: 2m, gelen: 2m, borc: 2m, gridSevk: null);

        ProjeTransferIadeKurali.DonorYenidenSevkBorcunuDuzelt(donor);

        Assert.Equal(0m, donor.YenidenSevkGerekliAdet);
        Assert.Equal((int)GridSevkDurum.Bekliyor, donor.GridSevkDurumuId);
    }

    [Fact]
    public void YenidenSevkGerekliDegilse_SevkDurumunaDokunmaz()
    {
        var donor = Donor(istenen: 2m, gelen: 2m, borc: 0m);
        donor.GridSevkDurumuId = (int)GridSevkDurum.SevkEdildi;

        ProjeTransferIadeKurali.DonorYenidenSevkBorcunuDuzelt(donor);

        Assert.Equal(0m, donor.YenidenSevkGerekliAdet);
        Assert.Equal((int)GridSevkDurum.SevkEdildi, donor.GridSevkDurumuId);
    }
}
