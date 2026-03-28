using MediatR;
using _3K.Application.DTOs;

namespace _3K.Application.Features.FBTransferIslemleri.Commands
{
    /// <summary>
    /// İş akışı 5: FB arası malzeme transfer
    /// </summary>
    public class FBTransferOlusturCommand : IRequest<FBTransferResultDto>
    {
        public int CekiSatiriId { get; set; }
        public string AsilFB { get; set; } = string.Empty;
        public string AlinanFB { get; set; } = string.Empty;
        public int Miktar { get; set; }
        public string? Neden { get; set; }
        public string? IadeDurumu { get; set; }
        public string? Aciklama { get; set; }
        public int KullaniciId { get; set; }
        public int ProjeId { get; set; }
    }
}
