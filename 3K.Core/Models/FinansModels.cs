using _3K.Core.Entities;

namespace _3K.Core.Models
{
    public record FinansSayfaliSonuc<T>(IReadOnlyList<T> Items, int PageNumber, int PageSize, int TotalCount, int TotalPages)
    {
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }

    public record FinansListeFiltre(
        string? Arama = null,
        string? ProjeNo = null,
        string? Musteri = null,
        string? IsTuru = null,
        string? PoNumarasi = null,
        string? FaturaNumarasi = null,
        int? SiparisDurumu = null,
        int? FaturaDurumu = null,
        DateTime? Baslangic = null,
        DateTime? Bitis = null,
        bool? Belgeli = null,
        string? Olusturan = null,
        int Page = 1,
        int PageSize = 25,
        string SortBy = "createdDate",
        string SortDirection = "desc");

    public record FinansBelgeDto(int Id, FinansBelgeTuru BelgeTuru, int BagliKayitId, string DosyaAdi,
        string DosyaUzantisi, string IcerikTuru, long Boyut, string YukleyenKullanici, DateTime YuklenmeTarihi);

    public record FinansSiparisKalemiDto(int Id, int IsKaydiId, string SandikNo, string SandikAdi, FinansIsTuru IsTuru,
        decimal Adet, decimal M3, decimal FaturalananAdet, decimal FaturalananM3, decimal KalanAdet, decimal KalanM3,
        int? UrunId, string UrunKodu, string UrunAdi, FinansFiyatlandirmaBirimi FiyatlandirmaBirimi,
        decimal FiyatlandirmaMiktari, decimal BirimFiyat, string ParaBirimi, decimal KdvOrani,
        decimal NetTutar, decimal KdvTutari, decimal ToplamTutar, bool FiyatManuelDegistirildi);
    public record FinansSiparisDetayDto(FinansSiparisDto Ozet, IReadOnlyList<FinansSiparisKalemiDto> Kalemler,
        IReadOnlyList<FinansBelgeDto> Belgeler, DateTime CreatedDate, string? CreatedBy);
    public record FinansFaturaKalemiDto(int Id, int SiparisKalemiId, string SandikNo, string SandikAdi,
        decimal Adet, decimal M3);
    public record FinansFaturaDetayDto(FinansFaturaDto Ozet, IReadOnlyList<FinansFaturaKalemiDto> Kalemler,
        IReadOnlyList<FinansBelgeDto> Belgeler, DateTime CreatedDate, string? CreatedBy);
    public record FinansOzelIsDto(int Id, string KayitNo, string IsTuru, string Musteri, int? ProjeId, string ProjeNo,
        string IsAdi, string? Aciklama, decimal Miktar, string Birim, decimal BirimFiyat, string ParaBirimi,
        decimal KdvOrani, int? FinansKaydiId, DateTime IsTarihi, int? DuzenliIsId,
        string? DonemAnahtari, bool IptalEdildi, int BelgeSayisi, string[] PoNumaralari, string[] FaturaNumaralari,
        int? FaturaBekleyenSiparisId, DateTime CreatedDate, string? CreatedBy,
        FinansHesaplamaYontemi HesaplamaYontemi = FinansHesaplamaYontemi.DegiskenAdet, string RaporGrubu = "Özel İş");
    public record FinansDuzenliIsDto(int Id, int? ProjeId, string ProjeNo, string IsAdi, string IsTuru, string Musteri,
        string? Aciklama, string TekrarSikligi, DateTime BaslangicTarihi, DateTime? BitisTarihi, int OlusturmaGunu,
        decimal Miktar, string Birim, decimal BirimFiyat, string ParaBirimi, decimal KdvOrani,
        bool Aktif, DateTime? SonOlusturulanDonem, DateTime? SonrakiOlusturulacakDonem,
        DateTime CreatedDate, string? CreatedBy,
        FinansHesaplamaYontemi HesaplamaYontemi = FinansHesaplamaYontemi.DegiskenAdet, string RaporGrubu = "Özel İş");
    public record FinansGiderDto(int Id, DateTime Tarih, int KategoriId, string Kategori, string? AltKategori,
        string? FirmaVeyaKisi, string Aciklama, decimal Tutar, string ParaBirimi, bool KdvDahil, decimal KdvOrani, decimal Matrah,
        decimal KdvTutari, decimal ToplamTutar, int? ProjeId, string ProjeNo, FinansIsTuru? IsTuru,
        bool IptalEdildi, int BelgeSayisi, DateTime CreatedDate, string? CreatedBy);
    public record FinansGiderKategoriDto(int Id, string Ad, bool Aktif);
    public record FinansIsTuruTanimiDto(int Id, string Ad, bool Aktif, int Sira);
    public record FinansUrunEslesmesiDto(int Id, FinansIsTuru IsTuru, string? SandikAdi, string? SandikTipi,
        decimal? Boy, decimal? En, decimal? Yukseklik, int? IcSandikSablonId, bool Aktif);
    public record FinansUrunDto(int Id, string Kod, string Ad, FinansFiyatlandirmaBirimi FiyatlandirmaBirimi,
        decimal BirimFiyat, string ParaBirimi, decimal KdvOrani, bool Aktif, int Sira,
        IReadOnlyList<FinansUrunEslesmesiDto> Eslesmeler);
    public record FinansUrunKaydetRequest(string Kod, string Ad, FinansFiyatlandirmaBirimi FiyatlandirmaBirimi,
        decimal BirimFiyat, string ParaBirimi, decimal KdvOrani, bool Aktif, int Sira,
        IReadOnlyList<FinansUrunEslesmesiKaydetRequest> Eslesmeler);
    public record FinansUrunEslesmesiKaydetRequest(FinansIsTuru IsTuru, string? SandikAdi, int? IcSandikSablonId = null,
        string? SandikTipi = null, decimal? Boy = null, decimal? En = null, decimal? Yukseklik = null);
    public record FinansParaToplamiDto(string ParaBirimi, decimal NetTutar, decimal KdvTutari, decimal ToplamTutar);

    public record FinansDosyaIcerigi(Stream Stream, string IcerikTuru, string DosyaAdi);

    public record FinansSiparisGuncelleRequest(string PoNumarasi, DateTime SiparisTarihi, string? Aciklama);
    public record FinansFaturaGuncelleRequest(string FaturaNumarasi, DateTime FaturaTarihi, string? Aciklama);
    public record FinansOzelIsGuncelleRequest(string IsTuru, string Musteri, int? ProjeId, string IsAdi,
        string? Aciklama, decimal Miktar, string Birim, DateTime IsTarihi,
        FinansHesaplamaYontemi HesaplamaYontemi = FinansHesaplamaYontemi.DegiskenAdet,
        string RaporGrubu = "Özel İş", decimal BirimFiyat = 0, string ParaBirimi = "EUR", decimal KdvOrani = 0);
    public record FinansGiderGuncelleRequest(DateTime Tarih, int KategoriId, string? AltKategori,
        string? FirmaVeyaKisi, string Aciklama, decimal Tutar, string ParaBirimi, bool KdvDahil, decimal KdvOrani,
        int? ProjeId, FinansIsTuru? IsTuru);
    public record FinansDonemOlusturSonuc(int Taranan, int Olusturulan, DateTime ReferansTarihi);

    public record FinansMiktarDto(decimal Adet, decimal M3);

    public record FinansProjeOzetDto(
        int? ProjeId,
        string ProjeNo,
        string Musteri,
        int ToplamIsAdedi,
        decimal ToplamSandikAdedi,
        decimal ToplamM3,
        decimal SiparisAcikM3,
        decimal SiparisBekleyenM3,
        decimal FaturalananM3,
        decimal FaturaBekleyenM3,
        DateTime SonUretimeAlmaTarihi,
        string GenelDurum,
        decimal BirimFiyat,
        string ParaBirimi,
        decimal KdvOrani,
        decimal NetTutar,
        decimal KdvTutari,
        decimal ToplamTutar,
        bool TarifeEksik,
        string[] PoNumaralari,
        string[] FaturaNumaralari,
        int? FaturaBekleyenSiparisId);

    public record FinansIsKaydiDto(
        int Id,
        int? ProjeId,
        string ProjeNo,
        string Musteri,
        string SandikNo,
        string SandikAdi,
        string? SandikTipi,
        decimal? Boy,
        decimal? En,
        decimal? Yukseklik,
        int? IcSandikSablonId,
        FinansIsTuru IsTuru,
        decimal Adet,
        decimal BirimM3,
        decimal ToplamM3,
        DateTime UretimeAlinmaTarihi,
        decimal SiparisAdedi,
        decimal SiparisM3,
        decimal SiparisBekleyenAdet,
        decimal SiparisBekleyenM3,
        decimal FaturalananAdet,
        decimal FaturalananM3,
        string[] PoNumaralari,
        string[] FaturaNumaralari,
        bool KaynakAktif);

    public record FinansDagitimRequest(int IsKaydiId, decimal Adet, decimal M3, int? UrunId = null,
        decimal? BirimFiyat = null, string? ParaBirimi = null, decimal? KdvOrani = null);

    public record FinansSiparisOlusturRequest(
        string PoNumarasi,
        DateTime SiparisTarihi,
        string? Aciklama,
        IReadOnlyList<FinansDagitimRequest> Kalemler);

    public record FinansFaturaDagitimRequest(int SiparisKalemiId, decimal Adet, decimal M3);

    public record FinansFaturaOlusturRequest(
        int SiparisId,
        string FaturaNumarasi,
        DateTime FaturaTarihi,
        string? Aciklama,
        IReadOnlyList<FinansFaturaDagitimRequest> Kalemler);

    public record FinansIptalRequest(string Aciklama);

    public record FinansSiparisDto(
        int Id,
        string KayitNo,
        string PoNumarasi,
        string ProjeNo,
        string Musteri,
        string[] IsTurleri,
        DateTime SiparisTarihi,
        decimal SandikAdedi,
        decimal ToplamM3,
        decimal FaturalananM3,
        decimal KalanM3,
        FinansSiparisDurumu Durum,
        FinansFaturaDurumu FaturaDurumu,
        bool Belgeli,
        string? Aciklama,
        IReadOnlyList<FinansParaToplamiDto> Tutarlar);

    public record FinansFaturaDto(
        int Id,
        string KayitNo,
        string FaturaNumarasi,
        DateTime FaturaTarihi,
        string PoNumarasi,
        string ProjeNo,
        string[] IsTurleri,
        decimal SandikAdedi,
        decimal ToplamM3,
        FinansFaturaDurumu Durum,
        bool Belgeli,
        string? Aciklama);

    public record FinansDashboardDto(
        int ToplamIs,
        decimal ToplamSandik,
        decimal ToplamM3,
        int SiparisBekleyen,
        int SiparisAcik,
        int KismiSiparis,
        int FaturaBekleyen,
        int Faturalanan,
        int BuAyOzelIs,
        decimal BuAyGider);

    public record FinansOzelIsKaydetRequest(
        string IsTuru,
        string Musteri,
        int? ProjeId,
        string IsAdi,
        string? Aciklama,
        decimal Miktar,
        string Birim,
        DateTime IsTarihi,
        FinansHesaplamaYontemi HesaplamaYontemi = FinansHesaplamaYontemi.DegiskenAdet,
        string RaporGrubu = "Özel İş", decimal BirimFiyat = 0, string ParaBirimi = "EUR", decimal KdvOrani = 0);

    public record FinansDuzenliIsKaydetRequest(
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

    public record FinansAylikIsGuncelleRequest(decimal? Miktar = null, decimal? NetBirimFiyat = null);

    public record FinansAylikIsDto(
        string KaynakTuru, int? OzelIsId, int? ProjeId, FinansIsTuru IsTuru, string IsGrubu,
        string ProjeNo, string IsAdi, string? SandikTipi, decimal? Boy, decimal? En, decimal? Yukseklik,
        DateTime UretimBaslangic, DateTime UretimBitis, decimal SandikAdedi,
        decimal Miktar, string Birim, decimal BirimFiyat, decimal KdvOrani, decimal NetTutar,
        decimal KdvTutari, decimal ToplamTutar, string ParaBirimi, decimal SiparisMiktari,
        decimal FaturalananMiktar, decimal SiparisToplamTutar, decimal FaturalananToplamTutar, int[] IsKaydiIds,
        string[] PoNumaralari, string[] FaturaNumaralari, string Durum, bool MiktarDuzenlenebilir,
        bool TutarDuzenlenebilir, bool IptalEdildi, string? IptalAciklamasi);

    public record FinansGiderKategoriKaydetRequest(string Ad);

    public record FinansGiderKaydetRequest(
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
}