using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.ProjeIslemleri.DTOs;

namespace _3K.Application.Features.ProjeIslemleri.Commands
{
    public class EksiklerdenSahaProjesiOlusturCommand : IRequest<Result<ProjeDto>>, ISecuredRequest, IRequiresMenuPermission
    {
        public string RequiredMenuKod => "sahaya-aktar";

        public int? KaynakProjeId { get; set; }
        public int? HedefSahaProjeId { get; set; }
        public string? ProjeNo { get; set; }
        public string? Musteri { get; set; }
        public string? Lokasyon { get; set; }
        public string? Aciklama { get; set; }
        public List<EksikSahaSandikDto> Sandiklar { get; set; } = new();
    }

    public class EksikSahaSandikDto
    {
        public int? HedefSandikId { get; set; }
        public string SandikNo { get; set; } = string.Empty;
        public string? SandikIsmi { get; set; }
        public decimal? En { get; set; }
        public decimal? Boy { get; set; }
        public decimal? Yukseklik { get; set; }
        public decimal? NetKg { get; set; }
        public decimal? GrossKg { get; set; }
        public List<EksikSahaUrunDto> Urunler { get; set; } = new();
    }

    public class EksikSahaUrunDto
    {
        public int CekiSatiriId { get; set; }
        public int? KaynakProjeId { get; set; }
        public int? KaynakSandikId { get; set; }
        public decimal Miktar { get; set; }
        public string? Aciklama { get; set; }
    }
}
