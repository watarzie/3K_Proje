using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Features.StokIslemleri.Commands
{
    public class StokKaydiGuncelleCommand : IRequest<Result>
    {
        public int Id { get; set; }
        public string? MalzemeKodu { get; set; }
        public string MalzemeAdi { get; set; } = null!;
        public int Miktar { get; set; }
        public string? Birim { get; set; }
        public string? Lokasyon { get; set; }
        public string KaynakProje { get; set; } = null!;
        public string? StokGirisNedeni { get; set; }
    }
}
