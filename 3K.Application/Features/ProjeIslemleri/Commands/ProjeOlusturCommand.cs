using MediatR;
using _3K.Application.DTOs;

namespace _3K.Application.Features.ProjeIslemleri.Commands
{
    public class ProjeOlusturCommand : IRequest<ProjeDto>
    {
        public string ProjeNo { get; set; } = string.Empty;
        public string Musteri { get; set; } = string.Empty;
        public DateTime? PlanlananSevkTarihi { get; set; }
        public string SorumluKisi { get; set; } = string.Empty;
        public int KullaniciId { get; set; }
    }
}
