using MediatR;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    public class UrunIptalCommand : IRequest<bool>
    {
        public int CekiSatiriId { get; set; }
        public int ProjeId { get; set; }
        public int KullaniciId { get; set; }
        public string Neden { get; set; } = string.Empty;
    }
}
