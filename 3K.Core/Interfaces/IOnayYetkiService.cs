namespace _3K.Core.Interfaces
{
    public interface IOnayYetkiService
    {
        Task<bool> KullaniciIslemOnaylayabilirMiAsync(
            int kullaniciId,
            string? islemKodu,
            int talepEdenKullaniciId,
            CancellationToken ct = default);

        Task<_3K.Core.Models.OnayErisimKapsami> GetErisimKapsamiAsync(
            int kullaniciId,
            CancellationToken ct = default);
    }
}
