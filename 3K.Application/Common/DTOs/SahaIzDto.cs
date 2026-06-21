namespace _3K.Application.Common.DTOs
{
    public class SahaTamamlamaIzDto
    {
        public int KaynakCekiSatiriId { get; set; }
        public int KaynakProjeId { get; set; }
        public string KaynakProjeNo { get; set; } = string.Empty;
        public string KaynakSandikNo { get; set; } = string.Empty;
        public int KaynakSiraNo { get; set; }
        public string KaynakUrunAdi { get; set; } = string.Empty;
        public int SahaProjeId { get; set; }
        public string SahaProjeNo { get; set; } = string.Empty;
        public int SahaSandikId { get; set; }
        public string SahaSandikNo { get; set; } = string.Empty;
        public int SahaCekiSatiriId { get; set; }
        public decimal Miktar { get; set; }
        public int BirimId { get; set; }
        public string Birim { get; set; } = string.Empty;
        public int DurumId { get; set; }
        public string DurumMetni { get; set; } = string.Empty;
        public bool SevkEdildiMi { get; set; }
        public DateTime? SevkTarihi { get; set; }
    }

    public class SahaKaynakSatirIzDto
    {
        public int CekiSatiriId { get; set; }
        public int KaynakProjeId { get; set; }
        public string KaynakProjeNo { get; set; } = string.Empty;
        public string KaynakSandikNo { get; set; } = string.Empty;
        public int KaynakSiraNo { get; set; }
    }
}
