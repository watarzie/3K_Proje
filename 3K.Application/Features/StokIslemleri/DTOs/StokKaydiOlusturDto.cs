namespace _3K.Application.Features.StokIslemleri.DTOs
{
    public class StokKaydiOlusturDto
    {
        public string MalzemeKodu { get; set; } = string.Empty;
        public string MalzemeAdi { get; set; } = string.Empty;
        public decimal Miktar { get; set; }
        public int BirimId { get; set; }
        public string? Lokasyon { get; set; }
        public string? KaynakProje { get; set; }
        public string? StokGirisNedeni { get; set; }
    }
}
