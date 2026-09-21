using _3K.Core.Enums;

namespace _3K.Core.Models;

public sealed record FinansTarihiDegistirModel(DateTime FinansTarihi, string Aciklama);
public sealed record FinansFiyatlandirmaModel(
    FinansFiyatlandirmaBirimi FiyatlandirmaBirimi, decimal Adet, decimal BirimM3,
    decimal BirimFiyat, string ParaBirimi, decimal KdvOrani, string Aciklama,
    decimal? ManuelNetTutar = null, int? SablonSurumId = null,
    IReadOnlyDictionary<string, string?>? AlanDegerleri = null,
    IReadOnlyList<FinansFiyatBileseniModel>? Bilesenler = null);
public sealed record FinansFiyatBileseniModel(string Ad, FinansFiyatlandirmaBirimi Yontem, decimal Miktar, decimal BirimFiyat);
public sealed record FinansSablonAlanModel(string Kod, string Ad, string VeriTuru, bool Zorunlu);
public sealed record FinansSablonKaydetModel(string Kod, string Ad, bool Aktif, IReadOnlyList<FinansSablonAlanModel> Alanlar);
public sealed record FinansSablonModel(int Id, string Kod, string Ad, bool Aktif, int SurumId, int Surum, IReadOnlyList<FinansSablonAlanModel> Alanlar);
public sealed record FinansKaliciSilModel(string VarlikTuru, int Id, string Surum, bool IkinciOnay, string Aciklama);
public sealed record FinansBagimlilikModel(string VarlikTuru, int Id, string Referans);
public sealed record FinansKaliciSilOnizlemeModel(string VarlikTuru, int Id, string Referans, string Surum, bool Silinebilir,
    IReadOnlyList<FinansBagimlilikModel> Bagimliliklar, IReadOnlyList<string> Engeller);
public sealed record FinansBelgeYukleModel(string HedefTuru, int HedefId, string OrijinalAd, byte[] Icerik);
public sealed record FinansBelgeModel(int Id, string HedefTuru, int HedefId, int Surum, string OrijinalAd, long Boyut,
    string IcerikTuru, string Hash, string Yukleyen, DateTime YuklemeTarihi);
public sealed record FinansBelgeIcerikModel(byte[] Icerik, string DosyaAdi, string IcerikTuru);
public sealed record FinansBekleyenModel(int IsKaydiId, int? SiparisKalemiId, string ProjeNo, string IsAdi,
    string? PoNumarasi, string Asama, DateTime Baslangic, int Gun, string Grup, string ParaBirimi, decimal? KalanNetTutar);
public sealed record FinansGrafikModel(string Grup, string ParaBirimi, decimal? Gelir, decimal? Gider);
public sealed record FinansPanelParaModel(string ParaBirimi, decimal? IsBedeli, decimal? SiparisNetTutar,
    decimal? SiparisBekleyen, decimal? FaturaBekleyen, decimal? Gelir, decimal? Gider, decimal? Fark,
    decimal? TahminiKar, decimal? KarOrani, decimal? YilGelir, decimal? YilGider);
public sealed record FinansPanelModel(DateTime Baslangic, DateTime Bitis, string TarihEkseni,
    IReadOnlyList<FinansPanelParaModel> Tutarlar, IReadOnlyList<FinansGrafikModel> Aylik,
    IReadOnlyList<FinansGrafikModel> IsTurleri, IReadOnlyList<FinansGrafikModel> GiderTurleri,
    int SiparisBekleyen, int KismiSiparis, int SiparisTam, int KismiFatura, int Tamamlanan,
    IReadOnlyList<FinansGrafikModel>? Projeler = null, IReadOnlyList<FinansGrafikModel>? OzelIsTurleri = null);
public sealed record FinansHareketModel(string Tur, int Id, string ProjeNo, string Ad, DateTime IsTarihi,
    DateTime FinansTarihi, DateTime FinansDonemi, string ParaBirimi, decimal? NetTutar, string Durum);
