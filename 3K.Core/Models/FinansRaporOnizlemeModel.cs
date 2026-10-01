namespace _3K.Core.Models;

/// <summary>Rapor önizlemesi: dosyanın başlığı, sütunları, ilk satırları (biçimlenmiş) ve toplam satır sayısı.</summary>
public sealed record FinansRaporOnizlemeModel(
    string Baslik,
    IReadOnlyList<string> Sutunlar,
    IReadOnlyList<IReadOnlyList<string>> Satirlar,
    int ToplamSatir)
{
    public const int EnFazlaSatir = 50;
}
