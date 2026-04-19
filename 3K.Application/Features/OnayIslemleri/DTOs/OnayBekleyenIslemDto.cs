using _3K.Core.Enums;

namespace _3K.Application.Features.OnayIslemleri.DTOs
{
    public class OnayBekleyenIslemDto
    {
        public int Id { get; set; }
        public string IslemAciklamasi { get; set; } = string.Empty;
        public string TalepEdenKisi { get; set; } = string.Empty;
        public DateTime OlusturulmaTarihi { get; set; }
        public OnayDurumu Durum { get; set; }
    }
}
