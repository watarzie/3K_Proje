namespace _3K.Core.Models;

/// <summary>Grid Sahası hareket okumalarının işlem kümesi; ikisi birlikte seçilebilir.</summary>
[Flags]
public enum GridHareketTuru
{
    /// <summary>
    /// Mal kabul: çeki satırının Grid durumu Tam Geldi, Eksik Geldi, Trafo Sevk ya da Grid Kapandı'ya geçti (tekil veya
    /// toplu; aynı durumun sevk güncellemesi sayılmaz) ya da Grid'e manuel ürün eklendi.
    /// </summary>
    MalKabul = 1,
    /// <summary>Grid'den 3K'ya toplu sevk (seçimdeki gelmemiş satırlar örtük Tam Geldi olur; tek kayıt, çok ürün).</summary>
    TopluSevk = 2
}

/// <summary>Grid hareket kaydı sabitleri: komutların yazdığı ve okumaların aradığı metinler aynı kaynaktan gelir.</summary>
public static class GridHareketSabitleri
{
    /// <summary>Grid manuel ürün ekleme hareketinin işlem metni (3K'nın "Manuel Ürün Eklendi" kaydından ayrılır).</summary>
    public const string ManuelUrunIslemi = "Grid Manuel Ürün Eklendi";
    public const string CekiSatiriReferansi = "CekiSatiri";
    public const string TopluSevkReferansi = "TopluSevk";
    /// <summary>Grid toplu komutlarının işlem metni öneki ("Grid Toplu TamGeldi", "Grid Toplu Sevk").</summary>
    public const string TopluIslemOneki = "Grid Toplu";
}

/// <summary>
/// Grid hareket süzgeci. Canlı (<c>HareketGecmisleri</c>) ve arşiv (<c>HareketGecmisleriArsiv</c>) tabloları aynı
/// süzgeçle okunur; sevk edilmiş projelerin gece arşive taşınan hareketleri de görünür. Bitiş tarihi hariçtir.
/// </summary>
public sealed record GridHareketFiltresi(
    DateTime BaslangicDahil,
    DateTime BitisHaric,
    GridHareketTuru Turler,
    int? KullaniciId,
    int? ProjeId,
    string? Arama,
    int SayfaNo,
    int SayfaBoyutu);

/// <summary>Canlı veya arşiv tablosundan gelen tek hareket; iki tablo aynı biçime indirgenir.</summary>
public sealed class GridHareketKaydi
{
    public int Id { get; init; }
    /// <summary>Kayıt arşiv tablosundan geldiyse true; canlı ve arşiv kimlikleri çakışabilir.</summary>
    public bool ArsivdenMi { get; init; }
    public int ProjeId { get; init; }
    public string ProjeNo { get; init; } = string.Empty;
    public string ReferansTipi { get; init; } = string.Empty;
    public string? ReferansId { get; init; }
    public string? ReferansMetni { get; init; }
    public string Islem { get; init; } = string.Empty;
    public int? IslemTipiId { get; init; }
    public int KullaniciId { get; init; }
    public string KullaniciAdi { get; init; } = string.Empty;
    public DateTime Tarih { get; init; }
    public string? EskiDeger { get; init; }
    public string? YeniDeger { get; init; }
    public string? Aciklama { get; init; }
}

/// <summary>Süzgeçteki bütün hareketlerin (sayfa değil) personel bazında sayısı; personel süzgeci özeti daraltmaz.</summary>
public sealed record GridHareketPersonelOzeti(int KullaniciId, string KullaniciAdi, int IslemSayisi);

/// <summary>Sayfalanmış Grid hareketleri ve süzgecin personel özeti.</summary>
public sealed class GridHareketSayfasi
{
    public IReadOnlyList<GridHareketKaydi> Kayitlar { get; init; } = [];
    public int ToplamKayit { get; init; }
    public IReadOnlyList<GridHareketPersonelOzeti> PersonelOzeti { get; init; } = [];
}

/// <summary>Çeki satırının en son mal kabul kaydı (canlı ya da arşiv): ne zaman, kim.</summary>
public sealed record GridSonMalKabul(int CekiSatiriId, DateTime Tarih, int KullaniciId, string KullaniciAdi);
