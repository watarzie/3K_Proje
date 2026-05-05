using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.StokIslemleri.DTOs;
using _3K.Core.Enums;

namespace _3K.Application.Features.StokIslemleri.Commands
{
    /// <summary>
    /// Stok kaydı oluştur.
    /// Roller: Admin
    /// </summary>
    public class StokKaydiOlusturCommand : IRequest<Result<StokKaydiDto>>, ISecuredRequest
    {
        public string[] RequiredRoles => new[] { StatusConstants.KullaniciRol.Admin };
        public string? RequiredMenuKod => "stok";

        public string MalzemeKodu { get; set; } = string.Empty;
        public string MalzemeAdi { get; set; } = string.Empty;
        public decimal Miktar { get; set; }
        public int BirimId { get; set; }
        public string? Lokasyon { get; set; }
        public string? KaynakProje { get; set; }
        public string? StokGirisNedeni { get; set; }
    }
}
