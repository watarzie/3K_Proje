namespace _3K.Core.Models;

public sealed record KullaniciYetkiKarari(int MenuTanimiId, bool? IzinVerildi);
public sealed record KullaniciYetkiModel(int MenuTanimiId, string Kod, string Ad,
    int RolYetkiTipiId, int EtkinYetkiTipiId, bool? IzinVerildi, bool Kritik);
public sealed record KullaniciYetkiSonucu(bool Basarili, string? Hata = null, int DurumKodu = 200);

public static class YetkiDegerlendirici
{
    public static int EtkinYetki(int rolYetkisi, bool? kullaniciKarari, int ekIzinSeviyesi)
        => kullaniciKarari switch { false => 1, true => ekIzinSeviyesi, null => rolYetkisi };
}
