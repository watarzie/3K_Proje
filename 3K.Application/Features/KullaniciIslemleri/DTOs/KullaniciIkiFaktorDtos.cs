namespace _3K.Application.Features.KullaniciIslemleri.DTOs
{
    public sealed class KullaniciIkiFaktorZorunluluguDto
    {
        public bool? ZorunluMu { get; set; }
    }

    public sealed class KullaniciIkiFaktorDurumDto
    {
        public int KullaniciId { get; set; }
        public bool IkiFaktorZorunluMu { get; set; }
        public bool IkiFaktorEtkinMi { get; set; }
        public DateTime? IkiFaktorDogrulandiTarihiUtc { get; set; }
    }
}
