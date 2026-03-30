using MediatR;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    public class StoktanKarsilaCommand : IRequest<bool>
    {
        public int CekiSatiriId { get; set; }
        public int StokKaydiId { get; set; }
        public int KullaniciId { get; set; }
        public int ProjeId { get; set; }
        public int KarsilananAdet { get; set; }
    }
}
