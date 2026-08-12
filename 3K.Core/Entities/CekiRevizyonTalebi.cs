namespace _3K.Core.Entities
{
    /// <summary>
    /// Onaya sunulan revizyon dosyasını ve talep anındaki değişmez ön izlemeyi
    /// birlikte saklayan revizyon artifact'ıdır. Onay durumu
    /// <see cref="OnayBekleyenIslem"/> üzerinde yönetilir.
    /// </summary>
    public class CekiRevizyonTalebi : BaseEntity
    {
        public int ProjeId { get; set; }
        public int AnaCekiId { get; set; }
        public int TalepEdenKullaniciId { get; set; }

        public string DosyaAdi { get; set; } = string.Empty;
        public byte[]? DosyaIcerigi { get; set; }
        public string DosyaSha256 { get; set; } = string.Empty;

        public string OnizlemeJson { get; set; } = string.Empty;
        public string OnizlemeHash { get; set; } = string.Empty;
        public int OnizlemeSurumu { get; set; } = 1;

        public int EklenenSatirSayisi { get; set; }
        public int GuncellenenSatirSayisi { get; set; }
        public int SilinenSatirSayisi { get; set; }

        public int? UygulananRevizyonCekiId { get; set; }
        public DateTime? UygulamaTarihi { get; set; }

        public virtual Proje Proje { get; set; } = null!;
        public virtual Ceki AnaCeki { get; set; } = null!;
        public virtual Kullanici TalepEdenKullanici { get; set; } = null!;
        public virtual Ceki? UygulananRevizyonCeki { get; set; }
    }
}
