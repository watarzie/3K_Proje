using MediatR;
using _3K.Core.Enums;
using _3K.Application.Common;
using _3K.Application.Features.SandikIslemleri.DTOs;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    /// <summary>
    /// Projeye yeni boş sandık ekler — çeki dışı ek sandık ihtiyacı için.
    /// </summary>
    public class SandikEkleCommand : IRequest<Result<SandikDto>>, ISecuredRequest
    {
        public string[] RequiredRoles => new[] { StatusConstants.KullaniciRol.Admin, StatusConstants.KullaniciRol.Personel3K };

        public int ProjeId { get; set; }
        public string SandikNo { get; set; } = string.Empty;
        public int TipId { get; set; } = (int)SandikTipi.AhsapKapali;
        public int DepoLokasyonId { get; set; } = (int)DepoLokasyon.Belirsiz;

        // Fiziksel Özellikler (Saha/Yedek projeleri için)
        public decimal? En { get; set; }
        public decimal? Boy { get; set; }
        public decimal? Yukseklik { get; set; }
        public decimal? NetKg { get; set; }
        public decimal? GrossKg { get; set; }
    }
}
