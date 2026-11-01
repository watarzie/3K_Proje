using _3K.Core.Helpers;

namespace _3K.Core.Entities
{
    /// <summary>
    /// Sevk sonrası düzeltme kaydı: sevkli bir sandık sevkiyat kaydı korunarak düzeltmeye açıldığında yazılır, düzeltme
    /// tamamlanınca kapanır. Açılıştaki sandık içeriği anlık görüntü olarak
    /// saklanır; tamamlanınca "önce → sonra" fark özeti yazılır. Sevkiyat raporu güncel veriden üretildiği için düzeltmenin
    /// neyi değiştirdiği yalnız bu kayıttan okunur. Bir sandıkta aynı anda tek açık kayıt olur.
    /// </summary>
    public class SandikDuzeltmeKaydi : BaseEntity
    {
        public int ProjeId { get; set; }
        /// <summary>Düzeltmedeki sandık; sandık sonradan silinirse boşalır, iz kalır.</summary>
        public int? SandikId { get; set; }
        /// <summary>Sandık numarasının açılıştaki kopyası (sandık silinse de kayıt okunur).</summary>
        public string SandikNo { get; set; } = string.Empty;
        public string Gerekce { get; set; } = string.Empty;
        public int AcanKullaniciId { get; set; }
        public DateTime AcilisTarihi { get; set; } = TurkeyTime.Now;
        /// <summary>Açılıştaki sandık bilgisi ve içeriği (JSON, <c>SandikDuzeltmeGoruntusu</c>).</summary>
        public string AcilisGoruntusu { get; set; } = string.Empty;
        public int? TamamlayanKullaniciId { get; set; }
        public DateTime? TamamlanmaTarihi { get; set; }
        /// <summary>Tamamlanınca yazılan fark özeti (satır satır, kullanıcıya gösterilir).</summary>
        public string? FarkOzeti { get; set; }

        public virtual Proje Proje { get; set; } = null!;
        public virtual Sandik? Sandik { get; set; }
    }
}
