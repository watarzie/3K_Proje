namespace _3K.Core.Models
{
    public class ManuelCekiOlusturModel
    {
        public string ProjeNo { get; set; } = string.Empty;
        public string? FBNo { get; set; }
        public string? Musteri { get; set; }
        public string? Lokasyon { get; set; }
        public string? Guc { get; set; }
        public string? Gerilim { get; set; }
        public string? ProjeMuduru { get; set; }
        public string? SorumluKisi { get; set; }
        public string? OlcuResmiNo { get; set; }
        public string? NakilOlcuResmiNo { get; set; }
        public string? SonMontajResmiNo { get; set; }
        public DateTime? PlanlananSevkTarihi { get; set; }
        public int ProjeTipiId { get; set; } = 1;
        public List<ManuelSandikModel> Sandiklar { get; set; } = new();
        public List<ManuelCekiSatiriModel> Satirlar { get; set; } = new();
    }

    public class ManuelSandikModel
    {
        public string SandikNo { get; set; } = string.Empty;
        public string? Ad { get; set; }
        public decimal? En { get; set; }
        public decimal? Boy { get; set; }
        public decimal? Yukseklik { get; set; }
        public decimal? NetKg { get; set; }
        public decimal? GrossKg { get; set; }
    }

    public class ManuelCekiSatiriModel
    {
        public int? SiraNo { get; set; }
        public string? BarkodNo { get; set; }
        public string Aciklama { get; set; } = string.Empty;
        public string SandikNo { get; set; } = string.Empty;
        public decimal IstenenAdet { get; set; }
        public int? BirimId { get; set; }
        public string? Birim { get; set; }
        public string? Remarks { get; set; }
    }
}
