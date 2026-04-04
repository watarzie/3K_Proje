using MediatR;
using _3K.Application.Common;
using _3K.Application.DTOs;
using _3K.Core.Enums;

namespace _3K.Application.Features.StokIslemleri.Commands
{
    /// <summary>
    /// Stok kaydı oluştur.
    /// Roller: Admin
    /// </summary>
    public class StokKaydiOlusturCommand : IRequest<Result<StokKaydiDto>>, ISecuredRequest
    {
        public string[] RequiredRoles => new[] { nameof(KullaniciRol.Admin) };

        public string MalzemeKodu { get; set; } = string.Empty;
        public string MalzemeAdi { get; set; } = string.Empty;
        public int Miktar { get; set; }
        public string Birim { get; set; } = string.Empty;
        public string? Lokasyon { get; set; }
        public string? KaynakProje { get; set; }
    }
}
