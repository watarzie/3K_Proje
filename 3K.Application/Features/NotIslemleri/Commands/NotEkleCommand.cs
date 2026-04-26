using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Features.NotIslemleri.Commands
{
    /// <summary>
    /// Madde 9: Not ekleme komutu.
    /// Grid veya 3K tarafı, herhangi bir entity'ye (CekiSatiri, Sandik, Proje) not bırakabilir.
    /// </summary>
    public class NotEkleCommand : IRequest<Result>, ISecuredRequest
    {
        public string[] RequiredRoles => new[]
        {
            StatusConstants.KullaniciRol.Admin,
            StatusConstants.KullaniciRol.Personel3K,
            StatusConstants.KullaniciRol.PersonelGrid
        };

        /// <summary>
        /// Bağlı referans tipi: "CekiSatiri", "Sandik", "Proje"
        /// </summary>
        public string BagliReferansTipi { get; set; } = string.Empty;

        /// <summary>
        /// Bağlı entity'nin Id'si.
        /// </summary>
        public int BagliReferansId { get; set; }

        /// <summary>
        /// CekiSatiri'ne doğrudan bağlı ise (opsiyonel FK).
        /// </summary>
        public int? CekiSatiriId { get; set; }

        /// <summary>
        /// Not içeriği (max 2000 karakter).
        /// </summary>
        public string Icerik { get; set; } = string.Empty;

        /// <summary>
        /// İlgili Proje Id (hareket kaydı için).
        /// </summary>
        public int ProjeId { get; set; }
    }
}
