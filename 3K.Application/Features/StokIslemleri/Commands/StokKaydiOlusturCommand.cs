using MediatR;
using _3K.Application.DTOs;

namespace _3K.Application.Features.StokIslemleri.Commands
{
    public class StokKaydiOlusturCommand : IRequest<StokKaydiDto>
    {
        public string MalzemeKodu { get; set; } = string.Empty;
        public string MalzemeAdi { get; set; } = string.Empty;
        public int Miktar { get; set; }
        public string Birim { get; set; } = string.Empty;
        public string? Lokasyon { get; set; }
        public string? KaynakProje { get; set; }
    }
}
