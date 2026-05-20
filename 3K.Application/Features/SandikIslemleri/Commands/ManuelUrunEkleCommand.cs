using MediatR;
using _3K.Application.Common;
using _3K.Core.Enums;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    /// <summary>
    /// İş akışı 9: Manuel ürün ekleme.
    /// </summary>
    public class ManuelUrunEkleCommand : IRequest<Result>, ISecuredRequest
    {

        public int ProjeId { get; set; }
        public int SandikId { get; set; }
        public string BarkodNo { get; set; } = string.Empty;
        public string Aciklama { get; set; } = string.Empty;
        public decimal IstenenAdet { get; set; }
        public int BirimId { get; set; } = (int)Birim.Adet;
        public string? EklemeNedeni { get; set; }
        public int KullaniciId { get; set; }
    }
}

