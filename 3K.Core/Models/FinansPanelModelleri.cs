using _3K.Core.Enums;

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
    int VadeUyariGunu = FinansVadeKurallari.VarsayilanUyariGunu,
    FinansPanelSayilariModel? Sayilar = null,
    IReadOnlyList<FinansPanelAylikSayiModel>? AylikSayilar = null,
    IReadOnlyList<FinansPanelProjeOzetiModel>? ProjeOzetleri = null,
    IReadOnlyList<FinansPanelKategoriOzetiModel>? KategoriOzetleri = null,
    IReadOnlyList<FinansPanelMunferitIsModel>? MunferitIsler = null,
    int MunferitIsSayisi = 0);

/// <summary>
/// Ana paneldeki münferit iş satırı (elle açılmış, düzenli işe bağlı olmayan özel iş): dönemde finans tarihine göre en
/// yeni kayıtlar. <see cref="NetTutar"/> maske listesindedir; sayı ve metin alanları maskelenmez.
/// </summary>
public sealed record FinansPanelMunferitIsModel(
    int Id,
    DateTime FinansTarihi,
    string ProjeNo,
    string Musteri,
    string IsAdi,
    decimal Adet,
    string Birim,
    decimal? NetTutar,
    string ParaBirimi,
    FinansIsDurumu Durum);

/// <summary>Panel detay dökümünün proje grubu (sıralama sırası): sistem projesinin tipi veya projesiz iş.</summary>
public enum FinansPanelProjeGrubu
{
    Normal = 1,
    Saha = 2,
    Yedek = 3,
    /// <summary>Sistem projesine bağlı olmayan işler (bağımsız/münferit iş, düzenli iş, manuel proje numarası).</summary>
    Bagimsiz = 4
}

/// <summary>
/// Panel detay dökümü, proje × para birimi: dönemdeki iş kalemi sayısı, iş bedeli, PO'ya bağlanan ve faturalanan
/// net, kalanlar, projeye yazılan gider ve birleşik PO/fatura karşılanması. Tutar alan adları maske listesindedir
/// (isBedeli, *NetTutar, gider); yetkisi olmayana null döner. Sayı alanları ve <see cref="ProjeGrubu"/> maskelenmez.
/// Bağımsız grupta satır proje numarası ve müşteriyle ayrılır.
/// </summary>
public sealed record FinansPanelProjeOzetiModel(
    string ProjeNo,
    string Musteri,
    string ParaBirimi,
    int IsKaydi,
    decimal? IsBedeli,
    decimal? SiparisNetTutar,
    decimal? FaturalananNetTutar,
    decimal? KalanSiparisNetTutar,
    decimal? KalanFaturaNetTutar,
    decimal? Gider,
    FinansKarsilanma Siparis,
    FinansKarsilanma Fatura,
    FinansPanelProjeGrubu ProjeGrubu = FinansPanelProjeGrubu.Normal);

/// <summary>Panel detay dökümü, iş kategorisi × para birimi (PO/fatura ekranındaki kategori kırılımıyla aynı).</summary>
public sealed record FinansPanelKategoriOzetiModel(
    FinansIsKategorisi Kategori,
    string ParaBirimi,
    int IsKaydi,
    int Proje,
    decimal? IsBedeli,
    decimal? SiparisNetTutar,
    decimal? FaturalananNetTutar);

/// <summary>
/// Panelin genel durum sayıları (seçili dönem): iş kalemi ve proje sayısı, PO sayısı (sipariş tarihine göre,
/// durum kırılımıyla), fatura sayısı (fatura tarihine göre, ödeme kırılımıyla) ve gider kaydı sayısı. İptaller
/// sayılmaz. Alan adları maske listesindeki parasal adlarla çakışmaz; sayılar yetki projeksiyonundan geçer.
/// </summary>
public sealed record FinansPanelSayilariModel(
    int IsKaydi,
    int Proje,
    int Po,
    int AcikPo,
    int KismiFaturaliPo,
    int KapaliPo,
    int Fatura,
    int OdenenFatura,
    int OdenmemisFatura,
    int VadesiGecenFatura,
    int GiderKaydi);

/// <summary>Ay bazında (yyyy-MM) iş kalemi, PO ve fatura sayısı; dönem grafiği için ilk veri ayından bitişe kesintisizdir.</summary>
public sealed record FinansPanelAylikSayiModel(string Ay, int IsKaydi, int Po, int Fatura);

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
