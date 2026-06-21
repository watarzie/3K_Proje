namespace _3K.Core.Interfaces
{
    public interface ISahaTamamlamaService
    {
        Task<Dictionary<int, decimal>> GetSevkEdilenTamamlamaMapAsync(
            IEnumerable<int> kaynakCekiSatiriIds,
            CancellationToken cancellationToken = default);

        Task SenkronizeKaynakProjelerAsync(
            IEnumerable<int> kaynakCekiSatiriIds,
            CancellationToken cancellationToken = default);

        Task SenkronizeKaynakProjelerBySahaSandikIdsAsync(
            IEnumerable<int> sahaSandikIds,
            CancellationToken cancellationToken = default);
    }
}
