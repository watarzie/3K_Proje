using _3K.Core.Entities;
using _3K.Core.Enums;

namespace _3K.Core.Models;

/// <summary>
/// Grid/3K işleminden sonra açıklamanın tek başına düzenlenmesi. Açıklama çeki satırındadır (ürünün bütün sandık
/// parçaları için ortaktır); Grid ekranı Grid açıklamasını, 3K ekranı 3K açıklamasını düzenler. Düzenleme yeni bir
/// Grid/3K işlemi değildir: durum, miktar ve sevk partisi değişmez. Ekrandaki eski değer kayıtlı değerle eşleşmezse
/// başkasının girdiği açıklama fark edilmeden ezilmesin diye kayıt reddedilir.
/// </summary>
public static class UrunAciklamaKurali
{
    public const int EnFazlaUzunluk = 1000;
    public const string UzunlukMesaji = "Açıklama en fazla 1000 karakter olabilir.";
    public const string CakismaMesaji =
        "Açıklama siz düzenlerken başka bir kullanıcı tarafından değiştirildi. Listeyi yenileyip güncel açıklamayı görerek tekrar deneyin.";
    public const string CekiBagiYokMesaji = "Bu satırın çeki bağı olmadığı için açıklaması buradan düzenlenemez.";

    /// <summary>Kenar boşlukları atılır; boş metin açıklama yok (null) sayılır.</summary>
    public static string? Normalize(string? deger) =>
        string.IsNullOrWhiteSpace(deger) ? null : deger.Trim();

    public static bool Gecerli(string? deger) => (Normalize(deger)?.Length ?? 0) <= EnFazlaUzunluk;

    public static bool EkrandakiDegerGuncel(string? kayitli, string? ekrandaki) =>
        string.Equals(Normalize(kayitli), Normalize(ekrandaki), StringComparison.Ordinal);

    public static string? Oku(CekiSatiri satir, UrunAciklamaAlani alan) => alan switch
    {
        UrunAciklamaAlani.Grid => satir.GridAciklama,
        UrunAciklamaAlani.UcK => satir.UcKAciklama,
        _ => throw new ArgumentOutOfRangeException(nameof(alan))
    };

    public static void Yaz(CekiSatiri satir, UrunAciklamaAlani alan, string? deger)
    {
        switch (alan)
        {
            case UrunAciklamaAlani.Grid:
                satir.GridAciklama = deger;
                break;
            case UrunAciklamaAlani.UcK:
                satir.UcKAciklama = deger;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(alan));
        }
    }

    public static string HareketAdi(UrunAciklamaAlani alan) =>
        alan == UrunAciklamaAlani.Grid ? "Grid Açıklaması Güncellendi" : "3K Açıklaması Güncellendi";
}
