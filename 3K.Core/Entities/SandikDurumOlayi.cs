using _3K.Core.Enums;
using _3K.Core.Helpers;

namespace _3K.Core.Entities
{
    /// <summary>
    /// Sandığın kapanma veya geri açılma olayı: kim, ne zaman, nasıl (elle/toplu/otomatik) ve elle geri açmada hangi
    /// sebeple. Hareket geçmişi sevk edilen projelerde arşive taşındığı için açılış/kapanış istatistiği bu tablodan
    /// okunur. Kayıt değişmez; sandık silinirse bağ boşalır ve iz sandık numarası kopyasıyla kalır, proje silinirse
    /// kayıt da silinir. Olay kuralı <c>SandikDurumOlayKurali</c>'dadır.
    /// </summary>
    public class SandikDurumOlayi : BaseEntity
    {
        public int ProjeId { get; set; }
        public int? SandikId { get; set; }
        /// <summary>Olay anındaki sandık numarasının kopyası (sandık silinse de kayıt okunur).</summary>
        public string SandikNo { get; set; } = string.Empty;
        public SandikDurumOlayTuru Tur { get; set; }
        public SandikDurumOlayKaynagi Kaynak { get; set; }
        /// <summary>Önceki sandık durumu; geçmişten aktarılan olaylarda bilinmeyebilir.</summary>
        public int? EskiDurumId { get; set; }
        public int YeniDurumId { get; set; }
        /// <summary>Elle geri açmada zorunlu sebep (<see cref="LookupSandikAcilmaSebebi"/>); diğer olaylarda boş.</summary>
        public int? SebepId { get; set; }
        public string? Not { get; set; }
        /// <summary>İşlemi yapan kullanıcı; arka plan işlerinde ve kullanıcısı bilinmeyen eski kayıtlarda boş.</summary>
        public int? KullaniciId { get; set; }
        public DateTime Tarih { get; set; } = TurkeyTime.Now;

        public virtual Proje Proje { get; set; } = null!;
        public virtual Sandik? Sandik { get; set; }
        public virtual LookupSandikAcilmaSebebi? Sebep { get; set; }
    }
}
