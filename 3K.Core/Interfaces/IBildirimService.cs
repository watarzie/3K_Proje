using _3K.Core.Enums;

namespace _3K.Core.Interfaces
{
    public interface IBildirimService
    {
        Task AbonelereBildirimGonderAsync(
            BildirimTipi tip,
            string baslik,
            string mesaj,
            string? hedefUrl,
            string referansTipi,
            int referansId,
            int? olusturanKullaniciId,
            CancellationToken cancellationToken = default);
    }
}
