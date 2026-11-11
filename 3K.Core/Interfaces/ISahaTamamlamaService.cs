using _3K.Core.Models;

namespace _3K.Core.Interfaces
{
    public interface ISahaTamamlamaService
    {
        Task<Dictionary<int, decimal>> GetSevkEdilenTamamlamaMapAsync(
            IEnumerable<int> kaynakCekiSatiriIds,
            CancellationToken cancellationToken = default);

        Task<Dictionary<int, decimal>> GetAktifTamamlamaMapAsync(
            IEnumerable<int> kaynakCekiSatiriIds,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Aktif saha işinin kalan miktarına göre kapanan miktar. İptal/Grid Kapandı
        /// açık işi kapatır; bu değer fiziksel teslim veya sevkiyat miktarı değildir.
        /// </summary>
        Task<Dictionary<int, decimal>> GetAktifIsTamamlamaMapAsync(
            IEnumerable<int> kaynakCekiSatiriIds,
            CancellationToken cancellationToken = default);

        Task<Dictionary<int, decimal>> GetAktifGerceklesenTamamlamaMapAsync(
            IEnumerable<int> kaynakCekiSatiriIds,
            CancellationToken cancellationToken = default);

        Task<Dictionary<int, decimal>> GetSevkEdilenGerceklesenTamamlamaMapAsync(
            IEnumerable<int> kaynakCekiSatiriIds,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// S-27: Aktif aktarımı sahada Grid İptal edilmiş kök satırlar için, gerçekleşen saha sevkine ek olarak
        /// iptalle kapanan miktar (<see cref="SahaIptalSevkKapanisKurali"/>). Saha sandığının sevkini beklemez.
        /// Yalnız normal projenin sevk durumu hesabında kullanılır; fiziksel teslim değildir.
        /// </summary>
        Task<Dictionary<int, decimal>> GetSahaIptalKapananMapAsync(
            IEnumerable<int> kaynakCekiSatiriIds,
            CancellationToken cancellationToken = default);

        Task<HashSet<int>> GetAktifSandikBazliAktarimSatirIdsAsync(
            IEnumerable<int> kaynakCekiSatiriIds,
            CancellationToken cancellationToken = default);

        Task<KaynakSandikSahaAktarimDurumu> GetKaynakSandikSahaAktarimDurumuAsync(
            IEnumerable<int> kaynakSandikIds,
            CancellationToken cancellationToken = default);

        Task<bool> AktifTamamlamaVarMiAsync(
            int kaynakCekiSatiriId,
            CancellationToken cancellationToken = default);

        Task SenkronizeKaynakProjelerAsync(
            IEnumerable<int> kaynakCekiSatiriIds,
            CancellationToken cancellationToken = default);

        Task SenkronizeKaynakProjelerBySahaSandikIdsAsync(
            IEnumerable<int> sahaSandikIds,
            CancellationToken cancellationToken = default);
    }
}
