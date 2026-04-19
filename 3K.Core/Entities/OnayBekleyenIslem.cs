using _3K.Core.Enums;

namespace _3K.Core.Entities
{
    public class OnayBekleyenIslem : BaseEntity
    {
        public string IslemAciklamasi { get; set; } = string.Empty;
        public string CommandType { get; set; } = string.Empty;
        public string PayloadJson { get; set; } = string.Empty;
        
        public int TalepEdenKullaniciId { get; set; }
        public virtual Kullanici TalepEdenKullanici { get; set; } = null!;

        public int? OnaylayanKullaniciId { get; set; }
        public virtual Kullanici? OnaylayanKullanici { get; set; }

        public OnayDurumu Durum { get; set; } = OnayDurumu.Bekliyor;
        public string? RedAciklamasi { get; set; }
    }
}
