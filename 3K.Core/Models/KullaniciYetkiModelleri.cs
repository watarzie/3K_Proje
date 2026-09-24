using _3K.Core.Constants;
using _3K.Core.Enums;

namespace _3K.Core.Models;

public sealed record KullaniciYetkiKarari(int MenuTanimiId, bool? IzinVerildi);
public sealed record KullaniciYetkiModel(int MenuTanimiId, string Kod, string Ad,
    int RolYetkiTipiId, int EtkinYetkiTipiId, bool? IzinVerildi, bool Kritik);
public sealed record KullaniciYetkiSonucu(bool Basarili, string? Hata = null, int DurumKodu = 200);

public static class YetkiDegerlendirici
{
    public static int EtkinYetki(int rolYetkisi, bool? kullaniciKarari, int ekIzinSeviyesi)
        => kullaniciKarari switch { false => 1, true => ekIzinSeviyesi, null => rolYetkisi };

    public static int UstSinirliYetki(string menuKodu, int kendiYetkisi, int ustYetkisi)
    {
        var seviye = Math.Min(kendiYetkisi, ustYetkisi);
        var gereken = YetkiKatalogu.Bul(menuKodu)?.GerekenYetki;
        // Yalnız yazma işlemi olan katalog düğümünün salt okuma karşılığı yoktur.
        if (seviye == (int)YetkiTipi.R && gereken == YetkiTipi.W)
            return (int)YetkiTipi.N;
        // Okuma/alan kodlarına eski rollerden W yazılmış olsa da bu kodlar işlem izni vermez.
        if (seviye == (int)YetkiTipi.W && gereken == YetkiTipi.R)
            return (int)YetkiTipi.R;
        return seviye;
    }
}
