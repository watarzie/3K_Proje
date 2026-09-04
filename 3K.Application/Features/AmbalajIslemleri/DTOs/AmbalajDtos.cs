using _3K.Core.Enums;

namespace _3K.Application.Features.AmbalajIslemleri.DTOs
{
    public sealed class AmbalajFiltreOzetiDto
    {
        public int KayitSayisi { get; set; }
        public int ProjeSandikKayitSayisi { get; set; }
        public int OzelSandikKayitSayisi { get; set; }
        public int ToplamSandikAdedi { get; set; }
        public decimal NetM3 { get; set; }
        public decimal SarfM3 { get; set; }
        public decimal ToplamM3 { get; set; }
    }

    public sealed class AmbalajManuelProjeSecenegiDto
    {
        public string No { get; set; } = string.Empty;
        public string Ad { get; set; } = string.Empty;
        public int KayitSayisi { get; set; }
        public int AktifKayitSayisi { get; set; }
        public int AmbalajaDahilKayitSayisi { get; set; }
        public int UretimeAlinmisKayitSayisi { get; set; }
        public int ToplamSandikAdedi { get; set; }
        public decimal NetM3 { get; set; }
        public decimal SarfM3 { get; set; }
        public decimal ToplamM3 { get; set; }
    }

    public sealed class AmbalajManuelProjeSecenekleriSayfasiDto
    {
        public List<AmbalajManuelProjeSecenegiDto> Items { get; set; } = [];
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }

    public sealed class AmbalajUretimSayfasiDto
    {
        public List<AmbalajUretimKaydiDto> Items { get; set; } = [];
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public AmbalajFiltreOzetiDto FilteredSummary { get; set; } = new();
    }

    public class AmbalajUretimKaydiDto
    {
        public bool M3BilgisiGorunurMu { get; set; } = true;
        public bool SarfBilgisiGorunurMu { get; set; } = true;
        public bool KaynakBilgisiGorunurMu { get; set; } = true;
        public int Id { get; set; }
        public Guid IsAkisKimligi { get; set; }
        public int? ProjeId { get; set; }
        public string? ProjeNo { get; set; }
        public string? ProjeAdi { get; set; }
        public int? UstKayitId { get; set; }
        public string? UstSandikNo { get; set; }
        public AmbalajSandikTuru Tur { get; set; }
        public string TurMetni { get; set; } = string.Empty;
        public AmbalajKaynakModulu KaynakModul { get; set; }
        public string KaynakModulMetni { get; set; } = string.Empty;
        public int? KaynakKayitId { get; set; }
        public bool KaynakSenkronizasyonuKilitliMi { get; set; }
        public DateTime? KaynakSonSenkronizasyonTarihi { get; set; }
        public string SandikNo { get; set; } = string.Empty;
        public string? Ad { get; set; }
        public AmbalajSandikCinsi SandikCinsi { get; set; }
        public string SandikCinsiMetni { get; set; } = string.Empty;
        public string? DigerSandikCinsi { get; set; }
        public int Adet { get; set; }
        public decimal Boy { get; set; }
        public decimal En { get; set; }
        public decimal Yukseklik { get; set; }
        public bool OlcuEksikMi { get; set; }
        public bool AmbalajaDahil { get; set; }
        public bool UretimeAlindi { get; set; }
        public decimal HesaplananBirimM3 { get; set; }
        public decimal HesaplananToplamM3 { get; set; }
        public decimal? M3Override { get; set; }
        public string? M3OverrideNedeni { get; set; }
        public decimal NetM3 { get; set; }
        public string M3HesaplamaVersiyonu { get; set; } = string.Empty;
        public decimal SarfOrani { get; set; }
        public decimal SarfM3 { get; set; }
        public decimal ToplamM3 { get; set; }
        public string? KullanimAmaci { get; set; }
        public string? TalepEdenKisi { get; set; }
        public string? TalepEdenBolum { get; set; }
        public string? TalimatVeren { get; set; }
        public string? FirinPartiNo { get; set; }
        public string? Aciklama { get; set; }
        public AmbalajUretimDurumu UretimDurumu { get; set; }
        public string UretimDurumuMetni { get; set; } = string.Empty;
        public DateTime? UretimTarihi { get; set; }
        public DateTime? TamamlanmaTarihi { get; set; }
        public bool FinansAktarimaHazirMi { get; set; }
        public bool IptalMi { get; set; }
        public DateTime? IptalTarihi { get; set; }
        public int? IptalEdenKullaniciId { get; set; }
        public string? IptalNedeni { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
    }

    public sealed class AmbalajUretimKaydiDetayDto : AmbalajUretimKaydiDto
    {
        public IReadOnlyList<AmbalajUretimHareketiDto> Hareketler { get; set; } = [];
    }

    public sealed class AmbalajUretimHareketiDto
    {
        public int Id { get; set; }
        public Guid IslemGrubu { get; set; }
        public int KullaniciId { get; set; }
        public DateTime Tarih { get; set; }
        public string Islem { get; set; } = string.Empty;
        public string AlanAdi { get; set; } = string.Empty;
        public string? EskiDeger { get; set; }
        public string? YeniDeger { get; set; }
        public string? Aciklama { get; set; }
        public bool DegerlerGizliMi { get; set; }
    }

    public sealed class AmbalajProjeOzetDto
    {
        public bool M3BilgisiGorunurMu { get; set; } = true;
        public bool SarfBilgisiGorunurMu { get; set; } = true;
        public bool KaynakBilgisiGorunurMu { get; set; } = true;
        public int ProjeId { get; set; }
        public string ProjeNo { get; set; } = string.Empty;
        public string? FbNo { get; set; }
        public string Musteri { get; set; } = string.Empty;
        public int ProjeTipiId { get; set; }
        public int KaynakSandikSayisi { get; set; }
        public int EksikOlculuKaynakSayisi { get; set; }
        public int AmbalajaDahilSandikAdedi { get; set; }
        public int UretimeAlinanSandikAdedi { get; set; }
        public int TamamlananSandikAdedi { get; set; }
        public decimal NetM3 { get; set; }
        public decimal SarfM3 { get; set; }
        public decimal ToplamM3 { get; set; }
        public DateTime? SonUretimTarihi { get; set; }
    }

    public sealed class AmbalajRaporDto
    {
        public bool M3BilgisiGorunurMu { get; set; } = true;
        public bool SarfBilgisiGorunurMu { get; set; } = true;
        public bool KaynakBilgisiGorunurMu { get; set; } = true;
        public IReadOnlyList<AmbalajUretimKaydiDto> Kayitlar { get; set; } = [];
        public int KayitSayisi { get; set; }
        public int ToplamSandikAdedi { get; set; }
        public decimal NetM3 { get; set; }
        public decimal SarfM3 { get; set; }
        public decimal ToplamM3 { get; set; }
    }

    /// <summary>
    /// Kullanıcının açık proje altında toplu üretim formuna dahil ettiği kayıtlar.
    /// </summary>
    public sealed class AmbalajSeciliUretimFormuDosyasiRequest
    {
        public List<int> KayitIdleri { get; set; } = [];
    }

    public sealed record AmbalajDosyaDto(byte[] Icerik, string IcerikTuru, string DosyaAdi);
}
