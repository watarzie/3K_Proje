namespace _3K.Core.Models
{
    public class CekiRevizyonOnizlemeSonuc
    {
        public int ProjeId { get; set; }
        public string ProjeNo { get; set; } = string.Empty;
        public int AnaCekiId { get; set; }
        public string DosyaAdi { get; set; } = string.Empty;
        public int ToplamIsaretliSatirSayisi { get; set; }
        public int EklenenSatirSayisi { get; set; }
        public int GuncellenenSatirSayisi { get; set; }
        public int SilinecekSatirSayisi { get; set; }
        public int RiskliSatirSayisi { get; set; }
        public int EngelliSatirSayisi { get; set; }
        public bool UygulanabilirMi { get; set; }
        public string Mesaj { get; set; } = string.Empty;
        public List<string> Uyarilar { get; set; } = new();
        public List<CekiRevizyonOnizlemeSatiri> Satirlar { get; set; } = new();
    }

    public class CekiRevizyonOnizlemeSatiri
    {
        public int ExcelSatirNo { get; set; }
        public string CheckKodu { get; set; } = string.Empty;
        public string IslemTipi { get; set; } = string.Empty;
        public string RiskSeviyesi { get; set; } = "Güvenli";
        public bool UygulanabilirMi { get; set; } = true;
        public string Mesaj { get; set; } = string.Empty;
        public int? MevcutCekiSatiriId { get; set; }
        public int? EskiSiraNo { get; set; }
        public int YeniSiraNo { get; set; }
        public string? BarkodNo { get; set; }
        public string? PozNo { get; set; }
        public string? Tanim { get; set; }
        public string? EskiKoliNo { get; set; }
        public string? YeniKoliNo { get; set; }
        public decimal? EskiIstenenAdet { get; set; }
        public decimal? YeniIstenenAdet { get; set; }
        public bool IslemGormusMu { get; set; }
        public decimal IslemGorenAdet { get; set; }
        public List<string> Degisiklikler { get; set; } = new();
        public List<string> Uyarilar { get; set; } = new();
    }
}
