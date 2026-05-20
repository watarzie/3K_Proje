using MediatR;
using _3K.Application.Common;
using _3K.Core.Enums;

namespace _3K.Application.Features.StokIslemleri.Commands
{
    /// <summary>
    /// İş akışı 6: Stoktan eksik karşılama.
    /// </summary>
    public class StokKarsilaCommand : IRequest<Result>, ISecuredRequest
    {

        public int CekiSatiriId { get; set; }
        public int StokKaydiId { get; set; }
        public decimal Miktar { get; set; }
        public int KullaniciId { get; set; }
        public int ProjeId { get; set; }
    }
}

