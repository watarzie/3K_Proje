namespace _3K.Application.Features.SevkiyatIslemleri.DTOs;

/// <summary>Toplu sevkin sonucu: oluşan (veya tekrarlanan isteğin) seferi.</summary>
public sealed class TopluSevkSonucDto
{
    public int SeferId { get; init; }
    public string SeferNo { get; init; } = string.Empty;
    public int ProjeSayisi { get; init; }
    public int SandikSayisi { get; init; }
}

/// <summary>
/// Sefer listesinin satırı. Gerçek seferin anahtarı "S:{id}"; sefere bağlı olmayan eski sevkiyatlar plaka + sevk günü ile
/// "eski kayıt" seferi olarak gruplanır ("E:{plaka}|{yyyy-MM-dd}").
/// </summary>
public sealed class SevkSeferiOzetDto
{
    public string Anahtar { get; init; } = string.Empty;
    public int? SeferId { get; init; }
    public string SeferNo { get; init; } = string.Empty;
    public bool EskiKayit { get; init; }
    public string? AracPlaka { get; init; }
    public string? Sofor { get; init; }
    public string? DorsePlaka { get; init; }
    public DateTime SevkTarihi { get; init; }
    public int ProjeSayisi { get; init; }
    public int SandikSayisi { get; init; }
    public decimal? BrutKg { get; init; }
    public bool DuzeltmesiAcikSandikVar { get; init; }
    public IReadOnlyList<string> ProjeNolari { get; init; } = Array.Empty<string>();
}

public sealed class SevkSeferiDetayDto
{
    public SevkSeferiOzetDto Sefer { get; init; } = new();
    public string? Aciklama { get; init; }
    public string? KullaniciAdSoyad { get; init; }
    public IReadOnlyList<SevkSeferiProjeDto> Projeler { get; init; } = Array.Empty<SevkSeferiProjeDto>();
}

public sealed class SevkSeferiProjeDto
{
    public int ProjeId { get; init; }
    public string ProjeNo { get; init; } = string.Empty;
    public int ProjeTipiId { get; init; }
    public int SevkiyatNo { get; init; }
    public IReadOnlyList<SevkSeferiSandikDto> Sandiklar { get; init; } = Array.Empty<SevkSeferiSandikDto>();
}

public sealed class SevkSeferiSandikDto
{
    public int SandikId { get; init; }
    public string SandikNo { get; init; } = string.Empty;
    public string? SandikAdi { get; init; }
    public decimal? BrutKg { get; init; }
    public bool SevkiyatDuzeltmeAcikMi { get; init; }
}

/// <summary>Sevkiyat istatistikleri: plaka, proje ve ay bazında sefer/sandık sayıları (kaynak: sefer ve sevkiyat kayıtları).</summary>
public sealed class SevkiyatIstatistikDto
{
    public int SeferSayisi { get; init; }
    public int SandikSayisi { get; init; }
    public int ProjeSayisi { get; init; }
    public int BuAySeferSayisi { get; init; }
    public string? EnCokKullanilanPlaka { get; init; }
    public IReadOnlyList<PlakaIstatistikDto> Plakalar { get; init; } = Array.Empty<PlakaIstatistikDto>();
    public IReadOnlyList<ProjeSevkIstatistikDto> Projeler { get; init; } = Array.Empty<ProjeSevkIstatistikDto>();
    public IReadOnlyList<AylikSevkIstatistikDto> Aylik { get; init; } = Array.Empty<AylikSevkIstatistikDto>();
}

public sealed class PlakaIstatistikDto
{
    public string Plaka { get; init; } = string.Empty;
    public int SeferSayisi { get; init; }
    public int SandikSayisi { get; init; }
    public int ProjeSayisi { get; init; }
    public DateTime SonSevk { get; init; }
}

public sealed class ProjeSevkIstatistikDto
{
    public int ProjeId { get; init; }
    public string ProjeNo { get; init; } = string.Empty;
    public int ProjeTipiId { get; init; }
    public int SeferSayisi { get; init; }
    public int SandikSayisi { get; init; }
    public IReadOnlyList<string> SeferNolari { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Plakalar { get; init; } = Array.Empty<string>();
}

public sealed class AylikSevkIstatistikDto
{
    public int Yil { get; init; }
    public int Ay { get; init; }
    public int SeferSayisi { get; init; }
    public int SandikSayisi { get; init; }
}
