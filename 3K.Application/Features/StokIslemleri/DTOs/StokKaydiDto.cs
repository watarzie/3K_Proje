namespace _3K.Application.Features.StokIslemleri.DTOs
{
    public class StokKaydiDto
    {
        public int Id { get; set; }
        public string MalzemeKodu { get; set; } = string.Empty;
        public string MalzemeAdi { get; set; } = string.Empty;
        public decimal Miktar { get; set; }
        public int BirimId { get; set; }
        public string BirimMetni { get; set; } = string.Empty;
        public string? Lokasyon { get; set; }
        public string? KaynakProje { get; set; }
        public string? StokGirisNedeni { get; set; }
        public int DurumId { get; set; }
        public string DurumMetni { get; set; } = string.Empty;
    }
}
