using System.Text.Json.Serialization;
using _3K.Core.Enums;

namespace _3K.Core.Models;

/// <summary>Onaya gönderilecek işler; uygun kalemler grup (proje × iş türü × para birimi) başına bir talebe toplanır.</summary>
public sealed record FinansPoOnayGonderModel(IReadOnlyList<int> IsKaydiIds);

/// <summary>
/// Onaya sunulan grubun değişmez görüntüsü: onaycı bunu görür, onay uygulanırken kalemler bu değerlerle
/// karşılaştırılır. Bütünlüğü <see cref="Helpers.FinansPoOnayOnizlemeButunlugu"/> ile doğrulanır.
/// </summary>
public sealed class FinansPoOnayOnizleme
{
    public int Surum { get; init; }
    public int? ProjeId { get; init; }
    public string ProjeNo { get; init; } = string.Empty;
    public string Musteri { get; init; } = string.Empty;
    public FinansIsTuru IsTuru { get; init; }
    public FinansIsKategorisi Kategori { get; init; }
    public string KategoriAdi { get; init; } = string.Empty;
    public string ParaBirimi { get; init; } = string.Empty;
    public int KalemSayisi { get; init; }
    public decimal NetTutar { get; init; }
    public decimal KdvTutari { get; init; }
    public decimal ToplamTutar { get; init; }
    public decimal ToplamM3 { get; init; }
    public decimal AhsapM3 { get; init; }
    public decimal KontraM3 { get; init; }
    public DateTime IlkFinansTarihi { get; init; }
    public DateTime SonFinansTarihi { get; init; }
    public IReadOnlyList<FinansPoOnayKalemi> Kalemler { get; init; } = Array.Empty<FinansPoOnayKalemi>();
}

/// <summary>Onaya sunulan tek kalemin (iş kaydı) o andaki ölçü, miktar ve tutar değerleri.</summary>
public sealed class FinansPoOnayKalemi
{
    public int IsKaydiId { get; init; }
    public string IsAdi { get; init; } = string.Empty;
    public string? SandikNo { get; init; }
    public string? SandikAdi { get; init; }
    public string? SandikTipi { get; init; }
    public decimal? Boy { get; init; }
    public decimal? En { get; init; }
    public decimal? Yukseklik { get; init; }
    public decimal Adet { get; init; }
    public string Birim { get; init; } = string.Empty;
    public decimal ToplamM3 { get; init; }
    public decimal AhsapM3 { get; init; }
    public decimal KontraM3 { get; init; }
    public FinansFiyatlandirmaBirimi FiyatlandirmaBirimi { get; init; }
    public decimal BirimFiyat { get; init; }
    public decimal? AhsapBirimFiyat { get; init; }
    public decimal KdvOrani { get; init; }
    public decimal NetTutar { get; init; }
    public decimal SiparisNetTutar { get; init; }
    public string ParaBirimi { get; init; } = string.Empty;
    public DateTime UretimTarihi { get; init; }
    public DateTime FinansTarihi { get; init; }
}

/// <summary>
/// Oluşturulan onay talebinin özeti; iç onay komutu bunu taşır. Görüntü özeti yalnız sunucu içindir, HTTP yanıtına
/// yazılmaz.
/// </summary>
public sealed record FinansPoOnayTalebiOzeti(
    int TalepId,
    int? ProjeId,
    string ProjeNo,
    string Ad,
    int KalemSayisi,
    decimal NetTutar,
    string ParaBirimi,
    [property: JsonIgnore] string OnizlemeHash);

/// <summary>
/// "Onaya gönder" sonucu. Kural açıksa talepler Onay Merkezi'ne düşer (202); kapalıysa kalemler hemen onaylanır.
/// Onaylı, onayda, iptal, fiyatsız veya kapsam dışı kalemler atlanır ve sayısı bildirilir.
/// </summary>
public sealed class FinansPoOnayGonderSonucu
{
    public IReadOnlyList<FinansPoOnayTalebiOzeti> Talepler { get; init; } = Array.Empty<FinansPoOnayTalebiOzeti>();
    public int KalemSayisi { get; init; }
    public int AtlananKalemSayisi { get; init; }
    public int OnayaAlinanTalepSayisi { get; init; }
    public int DogrudanOnaylananTalepSayisi { get; init; }
}

/// <summary>Onaylanan talebin kalemlere uygulanma sonucu.</summary>
public sealed record FinansPoOnayUygulamaSonucu(int TalepId, int KalemSayisi, decimal NetTutar, string ParaBirimi);

/// <summary>
/// Onay Merkezi'nde onaycıya gösterilen talep detayı: onaya sunulan görüntü ve her kalemin şu anki durumu.
/// Kalem değiştiyse onay uygulanmaz; onaycı bunu kararından önce görür.
/// </summary>
public sealed class FinansPoOnayDetayModel
{
    public int TalepId { get; init; }
    public FinansPoOnayOnizleme Onizleme { get; init; } = new();
    public IReadOnlyList<FinansPoOnayKalemDurumu> KalemDurumlari { get; init; } = Array.Empty<FinansPoOnayKalemDurumu>();
    public int DegisenKalemSayisi { get; init; }
    public DateTime? UygulamaTarihi { get; init; }
}

/// <summary>Onaya sunulan kalemin şimdiki hâli: değişmişse neyin değiştiği.</summary>
public sealed record FinansPoOnayKalemDurumu(
    int IsKaydiId,
    decimal? GuncelNetTutar,
    string? GuncelParaBirimi,
    bool Degisti,
    string? Degisiklik);
