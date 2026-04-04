using MediatR;
using _3K.Application.Common;
using _3K.Application.DTOs;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    /// <summary>
    /// Projeye yeni boş sandık ekler — çeki dışı ek sandık ihtiyacı için.
    /// </summary>
    public class SandikEkleCommand : IRequest<Result<SandikDto>>, ISecuredRequest
    {
        public string[] RequiredRoles => new[] { "Admin", "Personel3K" };

        public int ProjeId { get; set; }
        public string SandikNo { get; set; } = string.Empty;
        public string Tip { get; set; } = "Proje";
        public string DepoLokasyonu { get; set; } = "Belirsiz";
    }
}
