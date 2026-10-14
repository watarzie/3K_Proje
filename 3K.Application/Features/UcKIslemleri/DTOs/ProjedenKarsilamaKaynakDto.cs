namespace _3K.Application.Features.UcKIslemleri.DTOs
{
    /// <summary>
    /// Projeden karşılama için kaynak ürün seçeneği. Yalnız eşleştirme ve miktar kontrolü
    /// için gereken alanları taşır; değerler kaynak projenin Grid satırlarıyla aynıdır.
    /// Çok sandığa dağıtılmış çeki satırı her sandık tahsisi için ayrı satır olarak döner.
    /// </summary>
    public class ProjedenKarsilamaKaynakDto
    {
        public int CekiSatiriId { get; set; }
        public int SiraNo { get; set; }
        public string BarkodNo { get; set; } = string.Empty;
        public string? OlcuResmiPozNo { get; set; }
        public string Aciklama { get; set; } = string.Empty;
        /// <summary>Bu sandık satırına 3K'da gelen miktar.</summary>
        public decimal GelenMiktar { get; set; }
        /// <summary>Bu sandık satırında kullanılabilir net miktar.</summary>
        public decimal NetKullanilabilir { get; set; }
    }
}
