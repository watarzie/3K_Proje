using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Features.SandikIslemleri.Queries
{
    /// <summary>
    /// Normal projelerdeki kalan > 0 olan ürünleri listeler.
    /// Saha/Yedek sandıklara ekleme yapmak için kullanılır.
    /// </summary>
    public class GetEksikUrunlerByProjeQuery : IRequest<Result<List<EksikUrunForSandikDto>>>
    {
        public int ProjeId { get; set; }
    }

    public class EksikUrunForSandikDto
    {
        public int CekiSatiriId { get; set; }
        public int SiraNo { get; set; }
        public string BarkodNo { get; set; } = string.Empty;
        public string Aciklama { get; set; } = string.Empty;
        public string SandikNo { get; set; } = string.Empty;
        public decimal IstenenAdet { get; set; }
        public decimal GelenMiktar { get; set; }
        public decimal KalanMiktar { get; set; }
        public string Birim { get; set; } = string.Empty;
        public string ProjeNo { get; set; } = string.Empty;
    }
}


