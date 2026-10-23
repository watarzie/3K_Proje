using _3K.Core.Enums;

namespace _3K.Core.Models;

/// <summary>
/// Rapor önizlemesi: dosyanın başlığı, sütunları, ilk satırları (biçimlenmiş) ve kayıt sayısı. Bölümlü raporlarda
/// <see cref="SatirTurleri"/> her satırın türünü verir (<see cref="SatirVeri"/>, <see cref="SatirBolum"/>,
/// <see cref="SatirAraToplam"/>, <see cref="SatirGenelToplam"/>); <see cref="ToplamSatir"/> yalnız veri satırlarını sayar ve
/// satır sınırı aşılsa da genel toplam satırları önizlemenin sonunda gösterilir.
/// </summary>
public sealed record FinansRaporOnizlemeModel(
    string Baslik,
    IReadOnlyList<string> Sutunlar,
    IReadOnlyList<IReadOnlyList<string>> Satirlar,
    int ToplamSatir,
    IReadOnlyList<string>? SatirTurleri = null,
    IReadOnlyList<string>? Tablolar = null)
{
    public const int EnFazlaSatir = 50;
    public const string SatirVeri = "veri";
    public const string SatirBolum = "bolum";
    public const string SatirAraToplam = "ara-toplam";
    public const string SatirGenelToplam = "genel-toplam";
}

/// <summary>Rapora özel seçimler: işler raporunun kapsamı ve gider raporunun gruplaması (diğer raporlarda yok sayılır).</summary>
public sealed record FinansRaporSecenekleri(
    FinansIsRaporuKapsami Kapsam = FinansIsRaporuKapsami.Birlesik,
    FinansGiderRaporuGruplamasi Gruplama = FinansGiderRaporuGruplamasi.Duz);
