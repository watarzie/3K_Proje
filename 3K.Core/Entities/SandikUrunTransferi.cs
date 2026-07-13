using _3K.Core.Helpers;

namespace _3K.Core.Entities
{
    /// <summary>
    /// Sandıklar arasındaki ürün miktarı hareketlerinin değiştirilemez kayıt defteridir.
    /// Sandık/çeki satırı sonradan silinse bile snapshot alanları hareketi açıklamaya devam eder.
    /// </summary>
    public class SandikUrunTransferi : BaseEntity
    {
        public int ProjeId { get; set; }
        public int? CekiSatiriId { get; set; }
        public int? KaynakSandikId { get; set; }
        public int? HedefSandikId { get; set; }

        /// <summary>
        /// Kaynak içerik silinse bile teşhis amacıyla saklanan kaynak kayıt kimliği.
        /// Bilinçli olarak FK değildir.
        /// </summary>
        public int KaynakSandikIcerikId { get; set; }

        /// <summary>
        /// İstemci tekrar denemelerinde aynı hareketin iki kez uygulanmasını engeller.
        /// </summary>
        public Guid IslemAnahtari { get; set; }

        public decimal Miktar { get; set; }
        public decimal StokKarsilanan { get; set; }
        public decimal ProjeKarsilanan { get; set; }
        public decimal TedarikciKarsilanan { get; set; }
        public string KaynakSandikNo { get; set; } = string.Empty;
        public string HedefSandikNo { get; set; } = string.Empty;
        public string? BarkodNo { get; set; }
        public string UrunAdi { get; set; } = string.Empty;
        public int? BirimId { get; set; }
        public int? KullaniciId { get; set; }
        public string? Aciklama { get; set; }
        public DateTime Tarih { get; set; } = TurkeyTime.Now;

        public virtual Proje Proje { get; set; } = null!;
        public virtual CekiSatiri? CekiSatiri { get; set; }
        public virtual Sandik? KaynakSandik { get; set; }
        public virtual Sandik? HedefSandik { get; set; }
        public virtual LookupBirim? BirimLookup { get; set; }
        public virtual Kullanici? Kullanici { get; set; }
    }
}
