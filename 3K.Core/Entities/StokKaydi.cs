using _3K.Core.Enums;

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
        public int DurumId { get; set; } = (int)StokDurum.Aktif;

        // Navigation Properties
        public virtual LookupStokDurum? DurumLookup { get; set; }
        public virtual ICollection<StokHareketi> StokHareketleri { get; set; } = new List<StokHareketi>();
    }
}
