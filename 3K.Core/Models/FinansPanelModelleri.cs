namespace _3K.Core.Models;

public sealed record FinansPanelModel(DateTime Baslangic, DateTime Bitis, string TarihEkseni,
    IReadOnlyList<FinansPanelParaModel> Tutarlar, IReadOnlyList<FinansGrafikModel> Aylik,
    IReadOnlyList<FinansGrafikModel> IsTurleri, IReadOnlyList<FinansGrafikModel> GiderTurleri,
    int SiparisBekleyen, int KismiSiparis, int SiparisTam, int KismiFatura, int Tamamlanan,
    IReadOnlyList<FinansGrafikModel>? Projeler = null, IReadOnlyList<FinansGrafikModel>? OzelIsTurleri = null);

public sealed record FinansPanelParaModel(string ParaBirimi, decimal? IsBedeli, decimal? SiparisNetTutar,
    decimal? SiparisBekleyen, decimal? FaturaBekleyen, decimal? Gelir, decimal? Gider, decimal? Fark,
    decimal? TahminiKar, decimal? KarOrani, decimal? YilGelir, decimal? YilGider);

public sealed record FinansGrafikModel(string Grup, string ParaBirimi, decimal? Gelir, decimal? Gider);

public sealed record FinansHareketModel(string Tur, int Id, string ProjeNo, string Ad, DateTime IsTarihi,
    DateTime FinansTarihi, DateTime FinansDonemi, string ParaBirimi, decimal? NetTutar, string Durum);

public sealed record FinansBekleyenModel(int IsKaydiId, int? SiparisKalemiId, string ProjeNo, string IsAdi,
    string? PoNumarasi, string Asama, DateTime Baslangic, int Gun, string Grup, string ParaBirimi, decimal? KalanNetTutar);

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
