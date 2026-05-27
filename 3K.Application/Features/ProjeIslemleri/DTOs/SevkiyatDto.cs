namespace _3K.Application.Features.ProjeIslemleri.DTOs
{
    public class SevkiyatDto
    {
        public int Id { get; set; }
        public int SevkiyatNo { get; set; }
        public DateTime SevkTarihi { get; set; }
        public string? Aciklama { get; set; }
        public string? KullaniciAdSoyad { get; set; }
        public int SandikSayisi { get; set; }
        public string KayitTipi { get; set; } = "Sevkiyat";
        public bool IsKilitAcma { get; set; }
        public List<SevkiyatSandikDto> Sandiklar { get; set; } = new();
    }

    public class SevkiyatSandikDto
    {
        public int SandikId { get; set; }
        public string SandikNo { get; set; } = string.Empty;
        public string? SandikAdi { get; set; }
    }
}
