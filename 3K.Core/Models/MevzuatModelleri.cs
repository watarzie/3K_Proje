using _3K.Core.Enums;

namespace _3K.Core.Models;

/// <summary>Kural seti sürümünün özeti.</summary>
public sealed class MevzuatKuralSetiOzeti
{
    public int Id { get; init; }
    public string YetkiAlani { get; init; } = string.Empty;
    public string Ad { get; init; } = string.Empty;
    public int SurumNo { get; init; }
    public DateTime GecerlilikBaslangici { get; init; }
    public DateTime? GecerlilikBitisi { get; init; }
    public MevzuatKuralSetiDurumu Durum { get; init; }
    public DateTime? OnayTarihi { get; init; }
    public int KuralSayisi { get; init; }
}

/// <summary>Kural seti ayrıntısı: kurallar (sırasıyla) ve atıf yapılan kaynaklar.</summary>
public sealed class MevzuatKuralSetiDetayi
{
    public MevzuatKuralSetiOzeti Ozet { get; init; } = new();
    public string? Aciklama { get; init; }
    public IReadOnlyList<MevzuatKuraliModeli> Kurallar { get; init; } = [];
    public IReadOnlyList<MevzuatKaynagiModeli> Kaynaklar { get; init; } = [];
}

public sealed class MevzuatKuraliModeli
{
    public int Id { get; init; }
    public string Kod { get; init; } = string.Empty;
    public MevzuatKuralTipi Tip { get; init; }
    public string ParametrelerJson { get; init; } = "{}";
    public int? KaynakId { get; init; }
    public string? Madde { get; init; }
    public string Aciklama { get; init; } = string.Empty;
    public int Sira { get; init; }
}

public sealed class MevzuatKaynagiModeli
{
    public int Id { get; init; }
    public string Ad { get; init; } = string.Empty;
    public MevzuatKaynakTuru Tur { get; init; }
    public DateTime? ResmiGazeteTarihi { get; init; }
    public string? ResmiGazeteSayisi { get; init; }
    public string? Url { get; init; }
    public DateTime? ErisimTarihi { get; init; }
    public string? Aciklama { get; init; }
}
