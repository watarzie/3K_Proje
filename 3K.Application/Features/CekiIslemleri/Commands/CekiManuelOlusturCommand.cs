using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.CekiIslemleri.DTOs;

namespace _3K.Application.Features.CekiIslemleri.Commands
{
    public class CekiManuelOlusturCommand : IRequest<Result<CekiYuklemeResultDto>>, ISecuredRequest
    {
        public string[] RequiredRoles => new[] { StatusConstants.KullaniciRol.Admin };
        public string? RequiredMenuKod => "aktif-projeler";

        public string ProjeNo { get; set; } = string.Empty;
        public string? FBNo { get; set; }
        public string? Musteri { get; set; }
        public string? Lokasyon { get; set; }
        public string? Guc { get; set; }
        public string? Gerilim { get; set; }
        public string? ProjeMuduru { get; set; }
        public string? SorumluKisi { get; set; }
        public string? OlcuResmiNo { get; set; }
        public string? NakilOlcuResmiNo { get; set; }
        public string? SonMontajResmiNo { get; set; }
        public DateTime? PlanlananSevkTarihi { get; set; }
        public int ProjeTipiId { get; set; } = 1;
        public int KullaniciId { get; set; }
        public List<ManuelCekiSandikDto> Sandiklar { get; set; } = new();
        public List<ManuelCekiSatiriDto> Satirlar { get; set; } = new();
    }

    public class ManuelCekiSandikDto
    {
        public string SandikNo { get; set; } = string.Empty;
        public string? Ad { get; set; }
        public decimal? En { get; set; }
        public decimal? Boy { get; set; }
        public decimal? Yukseklik { get; set; }
        public decimal? NetKg { get; set; }
        public decimal? GrossKg { get; set; }
    }

    public class ManuelCekiSatiriDto
    {
        public int? SiraNo { get; set; }
        public string? BarkodNo { get; set; }
        public string Aciklama { get; set; } = string.Empty;
        public string SandikNo { get; set; } = string.Empty;
        public decimal IstenenAdet { get; set; }
        public int? BirimId { get; set; }
        public string? Birim { get; set; }
        public string? Remarks { get; set; }
    }
}
