namespace _3K.Core.Models
{
    public sealed class BildirimListeFiltresi
    {
        public bool? OkunduMu { get; init; }
        public DateTime? BaslangicTarihi { get; init; }
        public DateTime? BitisTarihiHaric { get; init; }
        public int? TipId { get; init; }
        public string? Arama { get; init; }
        public int Sayfa { get; init; }
        public int SayfaBoyutu { get; init; }
    }

    public sealed class BildirimSorguKaydi
    {
        public int Id { get; init; }
        public int TipId { get; init; }
        public string Baslik { get; init; } = string.Empty;
        public string Mesaj { get; init; } = string.Empty;
        public DateTime OlusturulmaTarihi { get; init; }
        public bool OkunduMu { get; init; }
        public DateTime? OkunmaTarihi { get; init; }
        public string? HedefUrl { get; init; }
        public string? ReferansTipi { get; init; }
        public int? ReferansId { get; init; }
        public int? OlusturanKullaniciId { get; init; }
        public string? OlusturanKullaniciAdi { get; init; }
        public int? ProjeId { get; set; }
        public string? ProjeNo { get; set; }
    }

    public sealed class BildirimSayfaliSorguSonucu
    {
        public IReadOnlyList<BildirimSorguKaydi> Bildirimler { get; init; } = [];
        public int ToplamKayit { get; init; }
        public int ToplamOkunmamis { get; init; }
    }
}
