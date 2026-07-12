using _3K.Core.Enums;

namespace _3K.Core.Models
{
    public enum OnayGecmisiKapsami
    {
        Tumu = 0,
        KararVerdiklerim = 1,
        Taleplerim = 2,
        Bekleyenler = 3
    }

    public sealed class OnayErisimKapsami
    {
        public bool TumIslemler { get; init; }
        public bool KendiTalepleriniOnaylayabilir { get; init; }
        public IReadOnlyCollection<string> IslemKodlari { get; init; } = [];
    }

    public sealed class OnayGecmisiFiltresi
    {
        public OnayGecmisiKapsami Kapsam { get; init; }
        public OnayDurumu? Durum { get; init; }
        public OnayCalistirmaDurumu? CalistirmaDurumu { get; init; }
        public DateTime? BaslangicTarihi { get; init; }
        public DateTime? BitisTarihiHaric { get; init; }
        public string? Arama { get; init; }
        public int Sayfa { get; init; }
        public int SayfaBoyutu { get; init; }
    }

    public sealed class OnayGecmisiKaydi
    {
        public int Id { get; init; }
        public string IslemKodu { get; init; } = string.Empty;
        public string IslemAciklamasi { get; init; } = string.Empty;
        public int TalepEdenKullaniciId { get; init; }
        public string TalepEdenKisi { get; init; } = string.Empty;
        public int? KararVerenKullaniciId { get; init; }
        public string? KararVerenKisi { get; init; }
        public OnayDurumu Durum { get; init; }
        public DateTime TalepTarihi { get; init; }
        public DateTime? KararTarihi { get; init; }
        public string? KararAciklamasi { get; init; }
        public OnayCalistirmaDurumu CalistirmaDurumu { get; init; }
        public DateTime? CalistirmaBaslamaTarihi { get; init; }
        public DateTime? CalistirmaBitisTarihi { get; init; }
        public string? CalistirmaHatasi { get; init; }
        public string? ReferansTipi { get; init; }
        public int? ReferansId { get; init; }
        public int? ProjeId { get; init; }
        public string? ProjeNo { get; init; }
        public string? HedefUrl { get; init; }
        public bool AksiyonAktifMi { get; init; }
    }

    public sealed class OnayGecmisiSayfaliSonuc
    {
        public IReadOnlyList<OnayGecmisiKaydi> Kayitlar { get; init; } = [];
        public int ToplamKayit { get; init; }
    }

    public sealed class OnayBekleyenSorguKaydi
    {
        public int Id { get; init; }
        public string IslemKodu { get; init; } = string.Empty;
        public string IslemAciklamasi { get; init; } = string.Empty;
        public string TalepEdenKisi { get; init; } = string.Empty;
        public DateTime OlusturulmaTarihi { get; init; }
        public OnayDurumu Durum { get; init; }
    }
}
