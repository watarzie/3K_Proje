using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.NotIslemleri.DTOs;

namespace _3K.Application.Features.NotIslemleri.Queries
{
    /// <summary>
    /// Belirli bir referansa (CekiSatiri, Sandik, Proje) bağlı notları getirir.
    /// Grid ve 3K notları karşılıklı görünür.
    /// </summary>
    public class GetNotlarQuery : IRequest<Result<List<NotDto>>>, ISecuredRequest
    {
        public string[] RequiredRoles => new[]
        {
            StatusConstants.KullaniciRol.Admin,
            StatusConstants.KullaniciRol.Personel3K,
            StatusConstants.KullaniciRol.PersonelGrid
        };

        public string BagliReferansTipi { get; set; } = string.Empty;
        public int BagliReferansId { get; set; }
    }
}
