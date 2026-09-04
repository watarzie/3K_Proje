using _3K.Core.Enums;

namespace _3K.Core.Models
{
    public sealed class AmbalajRaporSatiri
    {
        public int KayitId { get; set; }
        public Guid IsAkisKimligi { get; set; }
        public string ProjeNo { get; set; } = string.Empty;
        public string? ProjeAdi { get; set; }
        public string SandikNo { get; set; } = string.Empty;
        public string? SandikAdi { get; set; }
        public AmbalajSandikTuru Tur { get; set; }
        public AmbalajKaynakModulu KaynakModul { get; set; }
        public string SandikCinsi { get; set; } = string.Empty;
        public int Adet { get; set; }
        public decimal Boy { get; set; }
        public decimal En { get; set; }
        public decimal Yukseklik { get; set; }
        public decimal BirimM3 { get; set; }
        public decimal NetM3 { get; set; }
        public decimal SarfOrani { get; set; }
        public decimal SarfM3 { get; set; }
        public decimal ToplamM3 { get; set; }
        public bool AmbalajaDahil { get; set; }
        public bool UretimeAlindi { get; set; }
        public AmbalajUretimDurumu UretimDurumu { get; set; }
        public DateTime? UretimTarihi { get; set; }
        public string? TalepEdenKisi { get; set; }
        public string? TalepEdenBolum { get; set; }
        public string? TalimatVeren { get; set; }
        public string? FirinPartiNo { get; set; }
        public string? Aciklama { get; set; }
        public bool IptalMi { get; set; }
        public string? IptalNedeni { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public sealed record AmbalajRaporOzeti(
        int KayitSayisi,
        int ToplamSandikAdedi,
        decimal NetM3,
        decimal SarfM3,
        decimal ToplamM3);

    public sealed class AmbalajUretimFormuModel
    {
        public int? ProjeId { get; set; }
        public string ProjeNo { get; set; } = string.Empty;
        public string? ProjeAdi { get; set; }
        public string? FBNo { get; set; }
        public IReadOnlyList<AmbalajUretimFormuKalemiModel> Kalemler { get; set; } = [];
        public decimal NetM3 { get; set; }
        public decimal SarfM3 { get; set; }
        public decimal ToplamM3 { get; set; }
    }

    public sealed class AmbalajUretimFormuKalemiModel
    {
        public int KayitId { get; set; }
        public Guid IsAkisKimligi { get; set; }
        public string SandikNo { get; set; } = string.Empty;
        public string? SandikAdi { get; set; }
        public string SandikTuru { get; set; } = string.Empty;
        public string SandikCinsi { get; set; } = string.Empty;
        public int Adet { get; set; }
        public decimal? BrutKg { get; set; }
        public string? KullanimAmaci { get; set; }
        public string? TalimatVeren { get; set; }
        public string? Aciklama { get; set; }
        public AmbalajOlculeri IcOlculer { get; set; } = new(0, 0, 0);
        public AmbalajOlculeri DisOlculer { get; set; } = new(0, 0, 0);
        public int UstKizakAdedi { get; set; }
        public int AyakAdedi { get; set; }
        public int YanKusakAdedi { get; set; }
        public decimal OnDuvarYuksekligi { get; set; }
        public string FormulVersiyonu { get; set; } = string.Empty;
        public decimal HesaplananNetM3 { get; set; }
        public decimal? M3Override { get; set; }
        public decimal NetM3 { get; set; }
        public decimal SarfOrani { get; set; }
        public decimal SarfM3 { get; set; }
        public decimal ToplamM3 { get; set; }
        public string? FirinPartiNo { get; set; }
        public DateTime? UretimTarihi { get; set; }
        public IReadOnlyList<AmbalajUretimFormuParcasiModel> Parcalar { get; set; } = [];
    }

    public sealed class AmbalajUretimFormuParcasiModel
    {
        public string Kod { get; set; } = string.Empty;
        public string Grup { get; set; } = string.Empty;
        public string Aciklama { get; set; } = string.Empty;
        public string Malzeme { get; set; } = string.Empty;
        public decimal KesitEn { get; set; }
        public decimal KesitYukseklik { get; set; }
        public decimal Uzunluk { get; set; }
        public decimal TeorikAdet { get; set; }
        public int KesimAdedi { get; set; }
        public decimal HacimM3 { get; set; }
    }
}
