using _3K.Core.Enums;
using _3K.Core.Constants;

namespace _3K.Core.Entities
{
    public class OnayBekleyenIslem : BaseEntity
    {
        public string IslemKodu { get; set; } = OnayIslemKodlari.Genel;
        public string IslemAciklamasi { get; set; } = string.Empty;
        public string CommandType { get; set; } = string.Empty;
        public string PayloadJson { get; set; } = string.Empty;
        
        public int TalepEdenKullaniciId { get; set; }
        public virtual Kullanici TalepEdenKullanici { get; set; } = null!;

        public int? OnaylayanKullaniciId { get; set; }
        public virtual Kullanici? OnaylayanKullanici { get; set; }

        public OnayDurumu Durum { get; set; } = OnayDurumu.Bekliyor;

        public DateTime? KararTarihi { get; set; }
        public string? KararAciklamasi { get; set; }

        public OnayCalistirmaDurumu CalistirmaDurumu { get; set; } = OnayCalistirmaDurumu.Bekliyor;
        public DateTime? CalistirmaBaslamaTarihi { get; set; }
        public DateTime? CalistirmaBitisTarihi { get; set; }
        public string? CalistirmaHatasi { get; set; }

        public string? ReferansTipi { get; set; }
        public int? ReferansId { get; set; }
        public int? ProjeId { get; set; }
        public virtual Proje? Proje { get; set; }
        public string? HedefUrl { get; set; }

        // Geriye dönük veritabanı uyumluluğu için korunur. Yeni kod KararAciklamasi alanını kullanır.
        public string? RedAciklamasi { get; set; }
    }
}
