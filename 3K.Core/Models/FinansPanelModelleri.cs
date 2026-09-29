namespace _3K.Core.Models;

public sealed record FinansPanelModel(DateTime Baslangic, DateTime Bitis, string TarihEkseni,
    IReadOnlyList<FinansPanelParaModel> Tutarlar, IReadOnlyList<FinansGrafikModel> Aylik,
    IReadOnlyList<FinansGrafikModel> IsTurleri, IReadOnlyList<FinansGrafikModel> GiderTurleri,
    int SiparisBekleyen, int KismiSiparis, int SiparisTam, int KismiFatura, int Tamamlanan,
    IReadOnlyList<FinansGrafikModel>? Projeler = null, IReadOnlyList<FinansGrafikModel>? OzelIsTurleri = null,
    FinansProjeDurumSayilariModel? ProjeDurumlari = null,
    IReadOnlyList<FinansAlacakOzetiModel>? Alacaklar = null,
    IReadOnlyList<FinansGrafikModel>? BeklenenTahsilat = null,
    IReadOnlyList<FinansYaslandirmaKovasiModel>? Yaslandirma = null,
    int VadeUyariGunu = FinansVadeKurallari.VarsayilanUyariGunu);

/// <summary>
/// Para birimi bazında alacak özeti (D3, F3): ödenmemiş brüt tutar, vadesi geçen ve uyarı penceresinde
/// vadesi gelen tutar ile zamanında ve geç ödenen fatura sayıları. Para birimleri toplanmaz.
/// </summary>
public sealed record FinansAlacakOzetiModel(
    string ParaBirimi,
    decimal? BekleyenTutar,
    int BekleyenAdet,
    decimal? VadesiGecenTutar,
    int VadesiGecenAdet,
    decimal? YaklasanTutar,
    int YaklasanAdet,
    int ZamanindaOdenenAdet,
    int GecOdenenAdet,
    decimal? OdenenTutar);

/// <summary>
/// Yaşlandırma kovası (R1 §5): en az <see cref="MinGun"/> gündür siparişi veya faturası bekleyen iş kalemi
/// sayısı ve para birimi bazında kalan net tutar. Kovalar kümülatiftir (90+ aynı zamanda 15+ içindedir).
/// </summary>
public sealed record FinansYaslandirmaKovasiModel(
    string Asama,
    int MinGun,
    int Adet,
    IReadOnlyList<FinansParaToplamiModel> Tutarlar);

/// <summary>
/// Dönemdeki projelerin (özel işler hariç) PO ve fatura karşılanmasına göre sayıları (F4); proje
/// durumu <see cref="FinansTutarKurallari.BirlesikDurum"/> ile bütün aktif kalemlerinden türetilir.
/// </summary>
public sealed record FinansProjeDurumSayilariModel(
    int ToplamProje,
    int SiparisYok,
    int SiparisKismi,
    int SiparisTam,
    int FaturaYok,
    int FaturaKismi,
    int FaturaTam,
    int Tamamlanan,
    int FiyatBekleyen);

public sealed record FinansPanelParaModel(string ParaBirimi, decimal? IsBedeli, decimal? SiparisNetTutar,
    decimal? SiparisBekleyen, decimal? FaturaBekleyen, decimal? Gelir, decimal? Gider, decimal? Fark,
    decimal? TahminiKar, decimal? KarOrani, decimal? YilGelir, decimal? YilGider);

public sealed record FinansGrafikModel(string Grup, string ParaBirimi, decimal? Gelir, decimal? Gider);

public sealed record FinansHareketModel(string Tur, int Id, string ProjeNo, string Ad, DateTime IsTarihi,
    DateTime FinansTarihi, DateTime FinansDonemi, string ParaBirimi, decimal? NetTutar, string Durum);

public sealed record FinansBekleyenModel(int IsKaydiId, int? SiparisKalemiId, string ProjeNo, string IsAdi,
    string? PoNumarasi, string Asama, DateTime Baslangic, int Gun, string Grup, string ParaBirimi, decimal? KalanNetTutar);

/// <summary>Yaşlandırma aşamaları: sipariş bekleyen (iş kaydından beri) ve fatura bekleyen (PO tarihinden beri).</summary>
public static class FinansBekleyenAsamalari
{
    public const string Siparis = "Sipariş";
    public const string Fatura = "Fatura";
    public static readonly IReadOnlyList<string> Tumu = [Siparis, Fatura];
}

public sealed record FinansDegisiklikModel(
    int Id,
    string VarlikTuru,
    int VarlikId,
    string Islem,
    string AlanAdi,
    string? EskiDeger,
    string? YeniDeger,
    string? Aciklama,
    DateTime Tarih,
    string? Kullanici,
    Guid IslemGrubu = default,
    string? Referans = null);
