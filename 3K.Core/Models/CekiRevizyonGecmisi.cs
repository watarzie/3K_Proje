namespace _3K.Core.Models;

public sealed class CekiRevizyonGecmisiKaydi
{
    public string Kaynak { get; set; } = "talep";
    public int KayitId { get; set; }
    public int ProjeId { get; set; }
    public int? RevizyonCekiId { get; set; }
    public string DosyaAdi { get; set; } = string.Empty;
    public string? Yukleyen { get; set; }
    public DateTime YuklemeTarihi { get; set; }
    public DateTime? UygulamaTarihi { get; set; }
    public string? Onaylayan { get; set; }
    public DateTime? KararTarihi { get; set; }
    public string Durum { get; set; } = "Bilinmiyor";
    public int? EklenenSatirSayisi { get; set; }
    public int? GuncellenenSatirSayisi { get; set; }
    public int? SilinenSatirSayisi { get; set; }
    public bool DosyaMevcut { get; set; }
    public bool DetayMevcut { get; set; }
    public string? Bilgi { get; set; }
}

public sealed class CekiRevizyonGecmisiSayfa
{
    public List<CekiRevizyonGecmisiKaydi> Items { get; set; } = new();
    public int TotalCount { get; set; }
}

public sealed class CekiRevizyonGecmisiDetayi
{
    public CekiRevizyonGecmisiKaydi Kayit { get; set; } = new();
    public CekiRevizyonOnizlemeSonuc? Onizleme { get; set; }
}

public sealed record CekiRevizyonDosyasi(byte[] Icerik, string DosyaAdi);
