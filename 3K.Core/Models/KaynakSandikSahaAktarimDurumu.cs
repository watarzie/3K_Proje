namespace _3K.Core.Models
{
    /// <summary>
    /// Normal proje sandiklarinin sandik-bazli saha aktarimi durumunu tasir.
    /// Fiziksel Sandik.DurumId degerini degistirmeden sevk adaylarini ve
    /// saha uzerinden tamamlanan sevkiyatlari ayirt etmek icin kullanilir.
    /// </summary>
    public sealed class KaynakSandikSahaAktarimDurumu
    {
        public IReadOnlySet<int> AktifAktarimaBagliSandikIds { get; init; } = new HashSet<int>();
        public IReadOnlySet<int> TamamenSahayaAktarilanSandikIds { get; init; } = new HashSet<int>();
        public IReadOnlySet<int> SahaUzerindenSevkEdilenSandikIds { get; init; } = new HashSet<int>();
    }
}
