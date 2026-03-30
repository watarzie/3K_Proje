using MediatR;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    public class FBDenKarsilaCommand : IRequest<bool>
    {
        public int CekiSatiriId { get; set; }
        public int ProjeId { get; set; }
        public int KullaniciId { get; set; }
        public string AsilFB { get; set; } = string.Empty;
        public string AlinanFB { get; set; } = string.Empty;
        public int KarsilananAdet { get; set; }
        public string? Aciklama { get; set; }
        public string? IadeDurumu { get; set; }
    }
}
