using _3K.Core.Enums;

namespace _3K.Application.Features.AmbalajIslemleri.DTOs
{
    public sealed class AmbalajUretimKaydiDto
    {
        public bool M3HesaplanabilirMi { get; set; } = true;
        public bool OlcuBilgisiGorunurMu { get; set; } = true;
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
        public decimal? Boy { get; set; }
        public decimal? En { get; set; }
        public decimal? Yukseklik { get; set; }
        public bool OlcuEksikMi { get; set; }
        public bool AmbalajaDahil { get; set; }
        public bool UretimeAlindi { get; set; }
        public decimal? HesaplananBirimM3 { get; set; }
        public decimal? HesaplananToplamM3 { get; set; }
        public decimal? M3Override { get; set; }
        public string? M3OverrideNedeni { get; set; }
        public decimal? NetM3 { get; set; }
        public string M3HesaplamaVersiyonu { get; set; } = string.Empty;
        public decimal? SarfOrani { get; set; }
        public decimal? SarfM3 { get; set; }
        public decimal? ToplamM3 { get; set; }
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
}
