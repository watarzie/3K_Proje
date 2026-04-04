namespace _3K.Core.Entities
{
    public class StokKaydi : BaseEntity
    {
        public string MalzemeKodu { get; set; } = string.Empty;
        public string MalzemeAdi { get; set; } = string.Empty;
        public int Miktar { get; set; }
        public string Birim { get; set; } = string.Empty;
        public string? Lokasyon { get; set; }
        public string? KaynakProje { get; set; }
        public string? StokGirisNedeni { get; set; }
        public string Durum { get; set; } = "Aktif";

        // Navigation Properties
        public virtual ICollection<StokHareketi> StokHareketleri { get; set; } = new List<StokHareketi>();
    }
}
