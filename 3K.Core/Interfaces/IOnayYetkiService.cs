namespace _3K.Core.Interfaces
{
    public interface IOnayYetkiService
    {
        Task<bool> KullaniciIslemOnaylayabilirMiAsync(
            int kullaniciId,
            string? islemKodu,
            int talepEdenKullaniciId,
            CancellationToken ct = default);
    }
}
