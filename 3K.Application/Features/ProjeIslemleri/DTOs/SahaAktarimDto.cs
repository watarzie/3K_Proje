namespace _3K.Application.Features.ProjeIslemleri.DTOs
{
    public class SahaAktarimDto
    {
        public int SahaCekiSatiriId { get; set; }
        public int SahaProjeId { get; set; }
        public string SahaProjeNo { get; set; } = string.Empty;
        public int? SahaSandikId { get; set; }
        public string SahaSandikNo { get; set; } = string.Empty;
        public int KaynakProjeId { get; set; }
        public string KaynakProjeNo { get; set; } = string.Empty;
        public string KaynakSandikNo { get; set; } = string.Empty;
        public int SiraNo { get; set; }
        public string BarkodNo { get; set; } = string.Empty;
        public string Aciklama { get; set; } = string.Empty;
        public decimal Miktar { get; set; }
        public string Birim { get; set; } = string.Empty;
        public string DurumMetni { get; set; } = string.Empty;
        public bool SevkEdildiMi { get; set; }
        public bool IslemGormusMu { get; set; }
        public bool GeriAlinabilirMi { get; set; }
        public string? GeriAlinamamaNedeni { get; set; }

        /// <summary>Satırın bağlı olduğu aktarım başlığı; aktarımın tamamı bu kimlikle geri alınır.</summary>
        public int? SahaAktarimId { get; set; }
        public DateTime? SahaAktarimTarihi { get; set; }
        public string? SahaAktarimAciklama { get; set; }
        /// <summary>Başlık sahadan sahaya taşımaysa tamamı geri alınmaz; ters yönde yeni taşıma yapılır.</summary>
        public bool SahaAktarimTasimaMi { get; set; }
    }
}
