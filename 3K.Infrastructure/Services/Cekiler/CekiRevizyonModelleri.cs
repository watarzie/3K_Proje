using _3K.Core.Entities;

namespace _3K.Infrastructure.Services.Cekiler;

internal sealed record RevizyonDosyaBilgisi(
    byte[] DosyaBytes,
    CiktiImportBilgisi Import);

internal sealed record RevizyonBaglami(
    Proje Proje,
    Ceki AnaCeki,
    List<CekiSatiri> AnaSatirlar);

internal sealed record RevizyonStokEngeli(
    string Kod,
    string Mesaj,
    IReadOnlySet<int> CekiSatiriIds);

internal sealed record RevizyonStokKontrolBaglami(
    IReadOnlyDictionary<int, StokKaydi> Stoklar,
    IReadOnlyCollection<StokHareketi> TumStokHareketleri)
{
    public static readonly RevizyonStokKontrolBaglami Bos = new(
        new Dictionary<int, StokKaydi>(),
        Array.Empty<StokHareketi>());
}
