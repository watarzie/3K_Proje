namespace _3K.Application.Features.BildirimIslemleri.DTOs
{
    public class BildirimDto
    {
        public int Id { get; set; }
        public int TipId { get; set; }
        public string Baslik { get; set; } = string.Empty;
        public string Mesaj { get; set; } = string.Empty;
        public string? HedefUrl { get; set; }
        public DateTime OlusturulmaTarihi { get; set; }
        public bool OkunduMu { get; set; }
        public DateTime? OkunmaTarihi { get; set; }
        public string? ReferansTipi { get; set; }
        public int? ReferansId { get; set; }
        public BildirimMetadataDto Metadata { get; set; } = new();
    }

    public class BildirimMetadataDto
    {
        public int? ProjeId { get; set; }
        public string? ProjeNo { get; set; }
        public int? OlusturanKullaniciId { get; set; }
        public string? OlusturanKullaniciAdi { get; set; }
    }

    public class BildirimListeSonucDto
    {
        public List<BildirimDto> Bildirimler { get; set; } = new();
        public int ToplamKayit { get; set; }
        public int ToplamOkunmamis { get; set; }
        public int Sayfa { get; set; }
        public int SayfaBoyutu { get; set; }
        public int ToplamSayfa { get; set; }
    }

    public class BildirimOzetDto
    {
        public int ToplamOkunmamis { get; set; }
        public List<BildirimDto> Bildirimler { get; set; } = new();
    }

    public class BildirimAbonelikAyariDto
    {
        public int KullaniciId { get; set; }
        public string AdSoyad { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public bool CekiYuklendiBildirimi { get; set; }
        public bool CekiRevizyonuBildirimi { get; set; }
    }
}
