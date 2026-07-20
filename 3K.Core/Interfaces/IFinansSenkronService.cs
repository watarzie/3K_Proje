namespace _3K.Core.Interfaces
{
    public interface IFinansSenkronService
    {
        Task UretimFormuAlindiAsync(int projeId, int? tur, CancellationToken cancellationToken = default);
        Task OzelUretimFormuAlindiAsync(int projeId, int tur, CancellationToken cancellationToken = default);
        Task TumunuSenkronizeEtAsync(CancellationToken cancellationToken = default);
        Task ProjeyiSenkronizeEtAsync(int projeId, CancellationToken cancellationToken = default);
        Task BagimsizSandigiSenkronizeEtAsync(int sandikId, CancellationToken cancellationToken = default);
    }
}