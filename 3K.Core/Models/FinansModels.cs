using _3K.Core.Enums;

namespace _3K.Core.Models
{
    public sealed class FinansSayfaliSonuc<T>
    {
        public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
        /// <summary>
        /// Sayfadaki satırlardan değil, uygulanan filtrenin tamamından hesaplanan
        /// para birimi bazlı toplamlar. Toplam gerektirmeyen listelerde boş döner.
        /// </summary>
        public IReadOnlyList<FinansParaToplamiModel> Toplamlar { get; init; } = Array.Empty<FinansParaToplamiModel>();
        public int PageNumber { get; init; }
        public int PageSize { get; init; }
        public int TotalCount { get; init; }
        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }

    public sealed record FinansListeFiltre(
        int PageNumber = 1,
        int PageSize = 25,
        string? Arama = null,
        int? ProjeId = null,
        string? ProjeNo = null,
        FinansIsTuru? IsTuru = null,
        FinansIsDurumu? Durum = null,
        DateTime? Baslangic = null,
        DateTime? Bitis = null,
        string? ParaBirimi = null,
        bool IptalEdilenleriDahilEt = false,
        string? PoNumarasi = null,
        string? TalepEden = null,
        FinansSiparisDurumu? SiparisDurumu = null,
        FinansFaturaDurumu? FaturaDurumu = null,
        bool FaturaBekleyen = false,
        bool FaturalamaBekleyen = false);

    public sealed class FinansDashboardModel
    {
        public int ToplamIs { get; init; }
        public decimal ToplamSandik { get; init; }
        public decimal ToplamM3 { get; init; }
        public int SiparisBekleyen { get; init; }
        public int SiparisAcik { get; init; }
        public int KismiSiparis { get; init; }
        public int FaturaBekleyen { get; init; }
        public int Faturalanan { get; init; }
        public int BuAyOzelIs { get; init; }
        public int BuAyGiderKaydi { get; init; }
        /// <summary>Referans Finans ekranının kullandığı bu ay toplam gider tutarı.</summary>
        public decimal BuAyGider => BuAyGiderler.Sum(x => x.ToplamTutar);
        public IReadOnlyList<FinansParaToplamiModel> BuAyGiderler { get; init; } = Array.Empty<FinansParaToplamiModel>();
        public IReadOnlyList<FinansParaToplamiModel> Gelirler { get; init; } = Array.Empty<FinansParaToplamiModel>();
        public IReadOnlyList<FinansParaToplamiModel> Giderler { get; init; } = Array.Empty<FinansParaToplamiModel>();
        public IReadOnlyList<FinansParaToplamiModel> Netler { get; init; } = Array.Empty<FinansParaToplamiModel>();
        public IReadOnlyList<FinansParaToplamiModel> SiparisBekleyenTutarlar { get; init; } = Array.Empty<FinansParaToplamiModel>();
        public IReadOnlyList<FinansParaToplamiModel> SiparisAcikTutarlar { get; init; } = Array.Empty<FinansParaToplamiModel>();
        public IReadOnlyList<FinansParaToplamiModel> FaturalananTutarlar { get; init; } = Array.Empty<FinansParaToplamiModel>();
    }

    public sealed record FinansParaToplamiModel(
        string ParaBirimi,
        decimal NetTutar,
        decimal KdvTutari,
        decimal ToplamTutar);

    public sealed record class FinansIsKaydiModel
    {
        public int Id { get; init; }
        public int? ProjeId { get; init; }
        public string ProjeNo { get; init; } = string.Empty;
        public string Musteri { get; init; } = string.Empty;
        public bool ManuelProjeMi { get; init; }
        public string IsAdi { get; init; } = string.Empty;
        public string? OzelIsTuru { get; init; }
        public FinansHesaplamaYontemi? HesaplamaYontemi { get; init; }
        public string? RaporGrubu { get; init; }
        public string? Aciklama { get; init; }
        public string? TalepEdenKisi { get; init; }
        public string? TalepEdenBolum { get; init; }
        public FinansIsTuru IsTuru { get; init; }
        public string? SandikNo { get; init; }
        public string? SandikAdi { get; init; }
        public string? SandikTipi { get; init; }
        public decimal? Boy { get; init; }
        public decimal? En { get; init; }
        public decimal? Yukseklik { get; init; }
        public int? IcSandikSablonId { get; init; }
        public decimal Adet { get; init; }
        public string Birim { get; init; } = string.Empty;
        public decimal BirimM3 { get; init; }
        public decimal ToplamM3 { get; init; }
        public int? FinansUrunId { get; init; }
        public FinansFiyatlandirmaBirimi FiyatlandirmaBirimi { get; init; }
        public decimal BirimFiyat { get; init; }
        public string ParaBirimi { get; init; } = string.Empty;
        public decimal KdvOrani { get; init; }
        public int? TarifeYili { get; init; }
        public decimal NetTutar { get; init; }
        public decimal KdvTutari { get; init; }
        public decimal ToplamTutar { get; init; }
        public DateTime UretimTarihi { get; init; }
        public DateTime FinansDonemi { get; init; }
        public DateTime KayitTarihi { get; init; }
        public FinansIsDurumu Durum { get; init; }
        public decimal SiparisAdedi { get; init; }
        public decimal SiparisM3 { get; init; }
        public decimal SiparisBekleyenAdet { get; init; }
        public decimal SiparisBekleyenM3 { get; init; }
        public decimal FaturalananAdet { get; init; }
        public decimal FaturalananM3 { get; init; }
        public IReadOnlyList<string> PoNumaralari { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> FaturaNumaralari { get; init; } = Array.Empty<string>();
        public string KaynakTuru { get; init; } = string.Empty;
        public string? KaynakKayitId { get; init; }
        public bool KaynakAktif { get; init; }
        public bool IptalEdildi { get; init; }
        public string? IptalAciklamasi { get; init; }
        public DateTime CreatedDate { get; init; }
        public string? CreatedBy { get; init; }
    }

    public sealed class FinansProjeOzetModel
    {
        public int? ProjeId { get; init; }
        public string ProjeNo { get; init; } = string.Empty;
        public string Musteri { get; init; } = string.Empty;
        public int ToplamIsAdedi { get; init; }
        public decimal ToplamSandikAdedi { get; init; }
        public decimal ToplamM3 { get; init; }
        public decimal SiparisAcikM3 { get; init; }
        public decimal SiparisBekleyenM3 { get; init; }
        public decimal FaturalananM3 { get; init; }
        public decimal FaturaBekleyenM3 { get; init; }
        public DateTime? SonUretimeAlmaTarihi { get; init; }
        public string GenelDurum { get; init; } = string.Empty;
        public IReadOnlyList<FinansParaToplamiModel> Tutarlar { get; init; } = Array.Empty<FinansParaToplamiModel>();
        public decimal BirimFiyat { get; init; }
        public string ParaBirimi { get; init; } = "EUR";
        public decimal KdvOrani { get; init; }
        public decimal NetTutar { get; init; }
        public decimal KdvTutari { get; init; }
        public decimal ToplamTutar { get; init; }
        public bool TarifeEksik { get; init; }
        public IReadOnlyList<string> PoNumaralari { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> FaturaNumaralari { get; init; } = Array.Empty<string>();
        public int? FaturaBekleyenSiparisId { get; init; }
    }

    /// <summary>
    /// Finans formlarında proje seçimi için kullanılan, parasal veya operasyonel
    /// özet taşımayan güvenli seçenek modelidir.
    /// </summary>
    public sealed record FinansProjeSecenekModel(
        int ProjeId,
        string ProjeNo,
        string Musteri);

    public sealed record FinansIsKaydiKaydetModel(
        int? ProjeId,
        string? ManuelProjeNo,
        string? ManuelProjeAdi,
        string? Musteri,
        FinansIsTuru IsTuru,
        string IsAdi,
        string? Aciklama,
        string? TalepEdenKisi,
        string? TalepEdenBolum,
        decimal Adet,
        string Birim,
        decimal BirimM3,
        int? FinansUrunId,
        decimal? ManuelBirimFiyat,
        string? ParaBirimi,
        decimal? KdvOrani,
        DateTime UretimTarihi,
        DateTime FinansDonemi,
        string? SandikNo = null,
        string? SandikAdi = null,
        string? SandikTipi = null,
        decimal? Boy = null,
        decimal? En = null,
        decimal? Yukseklik = null,
        int? IcSandikSablonId = null,
        string? OzelIsTuru = null,
        FinansHesaplamaYontemi? HesaplamaYontemi = null,
        string? RaporGrubu = null);

    public sealed record FinansOzelIsKaydetModel(
        string IsTuru,
        string Musteri,
        int? ProjeId,
        string IsAdi,
        string? Aciklama,
        decimal Miktar,
        string Birim,
        DateTime IsTarihi,
        FinansHesaplamaYontemi HesaplamaYontemi,
        string RaporGrubu,
        decimal BirimFiyat,
        string ParaBirimi,
        decimal KdvOrani);

    public sealed record FinansAylikDegerModel(decimal? Miktar = null, decimal? NetBirimFiyat = null);

    /// <summary>
    /// Üretim modülünün finans bağımlılığı olmadan yayınlayacağı idempotent aktarım sözleşmesi.
    /// Aynı KaynakTuru + KaynakKayitId tekrar gönderildiğinde yeni kayıt açılmaz.
    /// </summary>
    public sealed record FinansUretimAktarimModel(
        string KaynakTuru,
        string KaynakKayitId,
        bool KaynakAktif,
        int? ProjeId,
        string ProjeNo,
        string Musteri,
        FinansIsTuru IsTuru,
        string IsAdi,
        decimal Adet,
        decimal BirimM3,
        DateTime UretimTarihi,
        DateTime FinansDonemi,
        string? SandikNo = null,
        string? SandikAdi = null,
        string? SandikTipi = null,
        decimal? Boy = null,
        decimal? En = null,
        decimal? Yukseklik = null,
        int? IcSandikSablonId = null,
        string? Aciklama = null,
        string? TalepEdenKisi = null,
        string? TalepEdenBolum = null);

    public sealed record FinansSenkronizasyonSonucModel(int Olusturulan, int Guncellenen, int Pasiflestirilen);

    /// <summary>
    /// Referans Finans ekranının aylık proje/özel iş kırılımı. Miktar ve para
    /// hesapları backendde decimal olarak gruplanır; önyüz yalnız gösterir.
    /// </summary>
    public sealed record FinansAylikIsModel(
        string KaynakTuru,
        int? OzelIsId,
        int? ProjeId,
        string ProjeBirimAnahtari,
        string Musteri,
        FinansIsTuru IsTuru,
        string IsGrubu,
        string ProjeNo,
        string IsAdi,
        string? SandikTipi,
        decimal? Boy,
        decimal? En,
        decimal? Yukseklik,
        DateTime UretimBaslangic,
        DateTime UretimBitis,
        decimal SandikAdedi,
        decimal Miktar,
        string Birim,
        decimal BirimFiyat,
        decimal KdvOrani,
        decimal NetTutar,
        decimal KdvTutari,
        decimal ToplamTutar,
        string ParaBirimi,
        decimal SiparisMiktari,
        decimal FaturalananMiktar,
        decimal SiparisToplamTutar,
        decimal FaturalananToplamTutar,
        IReadOnlyList<int> IsKaydiIds,
        IReadOnlyList<string> PoNumaralari,
        IReadOnlyList<string> FaturaNumaralari,
        string Durum,
        bool MiktarDuzenlenebilir,
        bool TutarDuzenlenebilir,
        bool IptalEdildi,
        string? IptalAciklamasi);

    /// <summary>
    /// Aylık iş takibinde sayfalama birimi proje veya tek bir özel iştir. Böylece
    /// aynı projenin ana/alt iş satırları farklı sayfalara bölünmez.
    /// </summary>
    public sealed class FinansAylikSayfaliSonuc
    {
        public IReadOnlyList<FinansAylikIsModel> Items { get; init; } = Array.Empty<FinansAylikIsModel>();
        public IReadOnlyList<FinansAylikFinansOzetiModel> FinansOzeti { get; init; } = Array.Empty<FinansAylikFinansOzetiModel>();
        public IReadOnlyList<FinansAylikGrupToplamiModel> GrupToplamlari { get; init; } = Array.Empty<FinansAylikGrupToplamiModel>();
        public int PageNumber { get; init; }
        public int PageSize { get; init; }
        public int TotalCount { get; init; }
        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }

    public sealed record FinansAylikFinansOzetiModel(
        string ParaBirimi,
        decimal Toplam,
        decimal SiparisAcik,
        decimal SiparisBekleyen,
        decimal Faturalanan,
        decimal FaturaBekleyen,
        decimal Gider,
        decimal Net);

    public sealed record FinansAylikGrupToplamiModel(
        string Grup,
        string ParaBirimi,
        decimal NetTutar,
        decimal KdvTutari,
        decimal ToplamTutar);

    public sealed record FinansOzelIsModel(
        int Id,
        string KayitNo,
        string IsTuru,
        string Musteri,
        string IsAdi,
        decimal Miktar,
        string Birim,
        decimal BirimFiyat,
        string ParaBirimi,
        decimal KdvOrani,
        int? FinansKaydiId,
        DateTime IsTarihi,
        int? DuzenliIsId,
        IReadOnlyList<string> PoNumaralari,
        IReadOnlyList<string> FaturaNumaralari,
        int? FaturaBekleyenSiparisId);

    public sealed record FinansSiparisDagitimModel(
        int IsKaydiId,
        decimal Adet,
        decimal M3,
        int? FinansUrunId = null,
        decimal? BirimFiyat = null,
        string? ParaBirimi = null,
        decimal? KdvOrani = null);

    public sealed record FinansSiparisOlusturModel(
        string PoNumarasi,
        DateTime SiparisTarihi,
        string? Aciklama,
        IReadOnlyList<FinansSiparisDagitimModel> Kalemler);

    public sealed record FinansSiparisGuncelleModel(
        string PoNumarasi,
        DateTime SiparisTarihi,
        string? Aciklama);

    public sealed record class FinansSiparisModel
    {
        public int Id { get; init; }
        public string KayitNo { get; init; } = string.Empty;
        public string PoNumarasi { get; init; } = string.Empty;
        public string ProjeNo { get; init; } = string.Empty;
        public string Musteri { get; init; } = string.Empty;
        public IReadOnlyList<string> IsTurleri { get; init; } = Array.Empty<string>();
        public DateTime SiparisTarihi { get; init; }
        public decimal SandikAdedi { get; init; }
        public decimal ToplamM3 { get; init; }
        public decimal FaturalananAdet { get; init; }
        public decimal FaturalananM3 { get; init; }
        public decimal KalanAdet { get; init; }
        public decimal KalanM3 { get; init; }
        public FinansSiparisDurumu Durum { get; init; }
        public string? Aciklama { get; init; }
        public IReadOnlyList<FinansParaToplamiModel> Tutarlar { get; init; } = Array.Empty<FinansParaToplamiModel>();
        public IReadOnlyList<FinansSiparisKalemiModel> Kalemler { get; init; } = Array.Empty<FinansSiparisKalemiModel>();
        public bool IptalEdildi { get; init; }
        public DateTime CreatedDate { get; init; }
        public string? CreatedBy { get; init; }
    }

    public sealed record class FinansSiparisKalemiModel
    {
        public int Id { get; init; }
        public int IsKaydiId { get; init; }
        public string SandikNo { get; init; } = string.Empty;
        public string SandikAdi { get; init; } = string.Empty;
        public FinansIsTuru IsTuru { get; init; }
        public decimal Adet { get; init; }
        public decimal M3 { get; init; }
        public decimal FaturalananAdet { get; init; }
        public decimal FaturalananM3 { get; init; }
        public decimal KalanAdet { get; init; }
        public decimal KalanM3 { get; init; }
        public int? FinansUrunId { get; init; }
        public string UrunKodu { get; init; } = string.Empty;
        public string UrunAdi { get; init; } = string.Empty;
        public FinansFiyatlandirmaBirimi FiyatlandirmaBirimi { get; init; }
        public decimal FiyatlandirmaMiktari { get; init; }
        public decimal BirimFiyat { get; init; }
        public string ParaBirimi { get; init; } = string.Empty;
        public decimal KdvOrani { get; init; }
        public decimal NetTutar { get; init; }
        public decimal KdvTutari { get; init; }
        public decimal ToplamTutar { get; init; }
        public bool FiyatManuelDegistirildi { get; init; }
    }

    public sealed record FinansFaturaKalemiOlusturModel(int SiparisKalemiId, decimal Adet, decimal M3);
    public sealed record FinansFaturaOlusturModel(
        int SiparisId,
        string FaturaNumarasi,
        DateTime FaturaTarihi,
        string? Aciklama,
        IReadOnlyList<FinansFaturaKalemiOlusturModel> Kalemler,
        string? BelgeParaBirimi = null,
        decimal? BelgeNetTutar = null,
        decimal? BelgeKdvTutari = null,
        decimal? BelgeToplamTutar = null,
        string? MutabakatAciklamasi = null);

    public sealed record FinansFaturaGuncelleModel(
        string FaturaNumarasi,
        DateTime FaturaTarihi,
        string? Aciklama,
        string? BelgeParaBirimi = null,
        decimal? BelgeNetTutar = null,
        decimal? BelgeKdvTutari = null,
        decimal? BelgeToplamTutar = null,
        string? MutabakatAciklamasi = null,
        bool BelgeMutabakatiniKoru = false);

    public sealed record class FinansFaturaModel
    {
        public int Id { get; init; }
        public string KayitNo { get; init; } = string.Empty;
        public string FaturaNumarasi { get; init; } = string.Empty;
        public DateTime FaturaTarihi { get; init; }
        public int SiparisId { get; init; }
        public string PoNumarasi { get; init; } = string.Empty;
        public string ProjeNo { get; init; } = string.Empty;
        public IReadOnlyList<string> IsTurleri { get; init; } = Array.Empty<string>();
        public decimal SandikAdedi { get; init; }
        public decimal ToplamM3 { get; init; }
        public FinansFaturaDurumu Durum { get; init; }
        public string? Aciklama { get; init; }
        public IReadOnlyList<FinansParaToplamiModel> Tutarlar { get; init; } = Array.Empty<FinansParaToplamiModel>();
        public string? BelgeParaBirimi { get; init; }
        public decimal? BelgeNetTutar { get; init; }
        public decimal? BelgeKdvTutari { get; init; }
        public decimal? BelgeToplamTutar { get; init; }
        public decimal MutabakatFarki { get; init; }
        public string? MutabakatAciklamasi { get; init; }
        public bool IptalEdildi { get; init; }
        public DateTime CreatedDate { get; init; }
        public string? CreatedBy { get; init; }
    }

    public sealed record FinansDuzenliIsKaydetModel(
        int? ProjeId,
        string? ManuelProjeNo,
        string? ManuelProjeAdi,
        string IsAdi,
        FinansIsTuru IsTuru,
        string Musteri,
        string? Aciklama,
        DateTime BaslangicTarihi,
        DateTime? BitisTarihi,
        int OlusturmaGunu,
        decimal Miktar,
        string Birim,
        int? FinansUrunId,
        decimal BirimFiyat,
        string ParaBirimi,
        decimal KdvOrani,
        bool Aktif,
        string? OzelIsTuru = null,
        FinansHesaplamaYontemi HesaplamaYontemi = FinansHesaplamaYontemi.DegiskenAdet,
        string RaporGrubu = "Özel İş");

    public sealed record FinansDuzenliIsUyumKaydetModel(
        int? ProjeId,
        string IsAdi,
        string IsTuru,
        string Musteri,
        string? Aciklama,
        string TekrarSikligi,
        DateTime BaslangicTarihi,
        DateTime? BitisTarihi,
        int OlusturmaGunu,
        decimal Miktar,
        string Birim,
        decimal BirimFiyat,
        string ParaBirimi,
        decimal KdvOrani,
        bool Aktif,
        FinansHesaplamaYontemi HesaplamaYontemi = FinansHesaplamaYontemi.DegiskenAdet,
        string RaporGrubu = "Özel İş");

    public sealed class FinansDuzenliIsModel
    {
        public int Id { get; init; }
        public int? ProjeId { get; init; }
        public string? ManuelProjeNo { get; init; }
        public string IsAdi { get; init; } = string.Empty;
        public string IsTuru { get; init; } = string.Empty;
        public FinansHesaplamaYontemi HesaplamaYontemi { get; init; }
        public string RaporGrubu { get; init; } = "Özel İş";
        public string Musteri { get; init; } = string.Empty;
        public string? Aciklama { get; init; }
        public string TekrarSikligi { get; init; } = "Aylık";
        public DateTime BaslangicTarihi { get; init; }
        public DateTime? BitisTarihi { get; init; }
        public int OlusturmaGunu { get; init; }
        public decimal Miktar { get; init; }
        public string Birim { get; init; } = string.Empty;
        public int? FinansUrunId { get; init; }
        public decimal BirimFiyat { get; init; }
        public string ParaBirimi { get; init; } = string.Empty;
        public decimal KdvOrani { get; init; }
        public bool Aktif { get; init; }
    }

    public sealed record FinansDonemOlusturSonucModel(int Taranan, int Olusturulan, DateTime ReferansTarihi);

    public sealed record FinansGiderKaydetModel(
        DateTime Tarih,
        DateTime FinansDonemi,
        int KategoriId,
        int? GiderKalemiId,
        string? AltKategori,
        string? FirmaVeyaKisi,
        string Aciklama,
        decimal Miktar,
        string Birim,
        decimal BirimFiyat,
        string ParaBirimi,
        bool KdvDahil,
        decimal KdvOrani,
        int? ProjeId,
        string? ManuelProjeNo,
        FinansIsTuru? IsTuru);

    public sealed record FinansGiderUyumKaydetModel(
        DateTime Tarih,
        int KategoriId,
        string? AltKategori,
        string? FirmaVeyaKisi,
        string Aciklama,
        decimal Tutar,
        string ParaBirimi,
        bool KdvDahil,
        decimal KdvOrani,
        int? ProjeId,
        FinansIsTuru? IsTuru);

    public sealed class FinansGiderModel
    {
        public int Id { get; init; }
        public DateTime Tarih { get; init; }
        public DateTime FinansDonemi { get; init; }
        public int KategoriId { get; init; }
        public string Kategori { get; init; } = string.Empty;
        public int? GiderKalemiId { get; init; }
        public string? GiderKalemi { get; init; }
        public string? AltKategori { get; init; }
        public string? FirmaVeyaKisi { get; init; }
        public string Aciklama { get; init; } = string.Empty;
        public decimal Miktar { get; init; }
        public string Birim { get; init; } = string.Empty;
        public decimal BirimFiyat { get; init; }
        public decimal Tutar { get; init; }
        public string ParaBirimi { get; init; } = string.Empty;
        public bool KdvDahil { get; init; }
        public decimal KdvOrani { get; init; }
        public decimal Matrah { get; init; }
        public decimal KdvTutari { get; init; }
        public decimal ToplamTutar { get; init; }
        public int? ProjeId { get; init; }
        public string ProjeNo { get; init; } = string.Empty;
        public FinansIsTuru? IsTuru { get; init; }
        public bool IptalEdildi { get; init; }
        public string? IptalAciklamasi { get; init; }
    }

    public sealed record FinansGiderKategoriModel(int Id, string Ad, bool Aktif);
    public sealed record FinansGiderKategoriKaydetModel(string Ad, bool Aktif);
    public sealed record FinansGiderKalemiModel(
        int Id,
        int KategoriId,
        string Kod,
        string Ad,
        bool Aktif,
        string? VarsayilanFirmaVeyaKisi = null,
        decimal? VarsayilanMiktar = null,
        string? VarsayilanBirim = null,
        decimal? VarsayilanBirimFiyat = null,
        string? VarsayilanParaBirimi = null,
        bool VarsayilanKdvDahil = false,
        decimal? VarsayilanKdvOrani = null);

    public sealed record FinansGiderKalemiKaydetModel(
        int KategoriId,
        string Kod,
        string Ad,
        bool Aktif,
        string? VarsayilanFirmaVeyaKisi = null,
        decimal? VarsayilanMiktar = null,
        string? VarsayilanBirim = null,
        decimal? VarsayilanBirimFiyat = null,
        string? VarsayilanParaBirimi = null,
        bool VarsayilanKdvDahil = false,
        decimal? VarsayilanKdvOrani = null);

    public sealed record FinansGideriKutuphaneyeKaydetModel(
        string Kod,
        string Ad,
        bool Aktif = true);

    public sealed record FinansUrunEslesmesiModel(
        int? Id,
        FinansIsTuru IsTuru,
        string? SandikAdi,
        string? SandikTipi,
        decimal? Boy,
        decimal? En,
        decimal? Yukseklik,
        int? IcSandikSablonId,
        bool Aktif);

    public sealed class FinansUrunModel
    {
        public int Id { get; init; }
        public string Kod { get; init; } = string.Empty;
        public string Ad { get; init; } = string.Empty;
        public FinansFiyatlandirmaBirimi FiyatlandirmaBirimi { get; init; }
        public bool Aktif { get; init; }
        public int Sira { get; init; }
        public decimal? GuncelBirimFiyat { get; init; }
        public string? GuncelParaBirimi { get; init; }
        public decimal? GuncelKdvOrani { get; init; }
        public decimal BirimFiyat => GuncelBirimFiyat ?? 0m;
        public string ParaBirimi => GuncelParaBirimi ?? "EUR";
        public decimal KdvOrani => GuncelKdvOrani ?? 0m;
        public IReadOnlyList<FinansUrunEslesmesiModel> Eslesmeler { get; init; } = Array.Empty<FinansUrunEslesmesiModel>();
    }

    /// <summary>
    /// Fiyat görme yetkisi olmayan operasyon kullanıcılarının ürün seçebilmesi için
    /// yalnız fiyat dışı alanları taşıyan güvenli seçenek modeli.
    /// </summary>
    public sealed record FinansUrunSecenekModel(
        int Id,
        string Kod,
        string Ad,
        FinansFiyatlandirmaBirimi FiyatlandirmaBirimi);

    public sealed record FinansUrunKaydetModel(
        string Kod,
        string Ad,
        FinansFiyatlandirmaBirimi FiyatlandirmaBirimi,
        bool Aktif,
        int Sira,
        IReadOnlyList<FinansUrunEslesmesiModel> Eslesmeler,
        decimal? BirimFiyat = null,
        string? ParaBirimi = null,
        decimal? KdvOrani = null);

    public sealed record FinansFiyatTarifesiKaydetModel(
        int FinansUrunId,
        int Yil,
        DateTime GecerlilikBaslangici,
        DateTime GecerlilikBitisi,
        decimal BirimFiyat,
        string ParaBirimi,
        decimal KdvOrani,
        bool Aktif);

    public sealed record FinansFiyatTarifesiModel(
        int Id,
        int FinansUrunId,
        string UrunKodu,
        string UrunAdi,
        int Yil,
        DateTime GecerlilikBaslangici,
        DateTime GecerlilikBitisi,
        decimal BirimFiyat,
        string ParaBirimi,
        decimal KdvOrani,
        bool Aktif);

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
        string? Kullanici);

    public sealed class FinansRaporModel
    {
        public FinansListeFiltre Filtre { get; init; } = new();
        public IReadOnlyList<FinansIsKaydiModel> Isler { get; init; } = Array.Empty<FinansIsKaydiModel>();
        public IReadOnlyList<FinansGiderModel> Giderler { get; init; } = Array.Empty<FinansGiderModel>();
        public IReadOnlyList<FinansParaToplamiModel> GelirToplamlari { get; init; } = Array.Empty<FinansParaToplamiModel>();
        public IReadOnlyList<FinansParaToplamiModel> GiderToplamlari { get; init; } = Array.Empty<FinansParaToplamiModel>();
        public IReadOnlyList<FinansParaToplamiModel> NetToplamlari { get; init; } = Array.Empty<FinansParaToplamiModel>();
    }
}
