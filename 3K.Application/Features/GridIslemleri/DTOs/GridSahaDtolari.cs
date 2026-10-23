using _3K.Application.Common;

namespace _3K.Application.Features.GridIslemleri.DTOs
{
    /// <summary>Grid Sahası görünümü, ürünler sekmesi: KPI özeti ve sayfalı liste.</summary>
    public sealed class GridSahaUrunleriDto
    {
        public GridSahaOzetDto Ozet { get; init; } = new();
        public PaginatedList<GridSahaUrunDto> Liste { get; init; } = new([], 0, 1, 1);
    }

    /// <summary>Arama ve süzgeçlerden bağımsız özet: Grid'de bekleyen ürünler ve bugünkü mal kabul sayısı.</summary>
    public sealed class GridSahaOzetDto
    {
        /// <summary>Grid'de bekleyen (gelmiş, 3K'ya ulaşmamış miktarı olan) satır sayısı.</summary>
        public int BekleyenSatir { get; init; }
        public decimal BekleyenAdet { get; init; }
        public int ProjeSayisi { get; init; }
        /// <summary>Bekleyenlerden Eksik Geldi olanlar.</summary>
        public int EksikGelenSatir { get; init; }
        /// <summary>Bugün yapılan mal kabul kaydı sayısı (canlı ve arşiv hareketleri).</summary>
        public int BugunMalKabul { get; init; }
    }

    /// <summary>Grid'e gelmiş tek çeki satırı (satır bazlı; sandık dağılımına bölünmez).</summary>
    public sealed class GridSahaUrunDto
    {
        public int CekiSatiriId { get; init; }
        public int ProjeId { get; init; }
        public string ProjeNo { get; init; } = string.Empty;
        public int ProjeTipiId { get; init; }
        public string Musteri { get; init; } = string.Empty;
        public string SandikNo { get; init; } = string.Empty;
        public int SiraNo { get; init; }
        public string BarkodNo { get; init; } = string.Empty;
        public string? OlcuResmiPozNo { get; init; }
        public string Aciklama { get; init; } = string.Empty;
        /// <summary>Birim lookup metni (enum adı değil).</summary>
        public string Birim { get; init; } = string.Empty;
        public decimal IstenenAdet { get; init; }
        public decimal GridGelenAdet { get; init; }
        /// <summary>3K'nın bütün partilerden teslim aldığı kümülatif miktar.</summary>
        public decimal UcKGelenMiktar { get; init; }
        /// <summary>Aktif sevk partisinin 3K tarafından henüz karşılanmamış bölümü.</summary>
        public decimal YoldakiMiktar { get; init; }
        /// <summary>Grid'de fiziksel bekleyen miktar (GridSahaBekleyenKurali).</summary>
        public decimal GriddeBekleyenMiktar { get; init; }
        public int GridDurumuId { get; init; }
        public string GridDurumuMetni { get; init; } = string.Empty;
        public int GridSevkDurumuId { get; init; }
        public string GridSevkDurumuMetni { get; init; } = string.Empty;
        /// <summary>En son mal kabul kaydının tarihi (hareket geçmişinden); kaydı olmayan eski satırda boş.</summary>
        public DateTime? SonMalKabulTarihi { get; init; }
        public string? SonMalKabulPersoneli { get; init; }
        /// <summary>Normal projenin kök satırı aktif saha aktarımına bağlıysa true; satır listeden düşmez, rozetle gösterilir.</summary>
        public bool SahayaAktarildiMi { get; init; }
        public string? GridAciklama { get; init; }
    }

    /// <summary>Grid Sahası görünümü, mal kabul sekmesi: uygulanan tarih aralığı, sayfalı hareketler ve personel özeti.</summary>
    public sealed class GridMalKabulGecmisiDto
    {
        public DateTime Baslangic { get; init; }
        public DateTime Bitis { get; init; }
        public PaginatedList<GridMalKabulHareketDto> Liste { get; init; } = new([], 0, 1, 1);
        public IReadOnlyList<GridMalKabulPersonelDto> PersonelOzeti { get; init; } = [];
    }

    public sealed class GridMalKabulPersonelDto
    {
        public int KullaniciId { get; init; }
        public string KullaniciAdi { get; init; } = string.Empty;
        public int IslemSayisi { get; init; }
    }

    /// <summary>Canlı veya arşiv tablosundan gelen tek Grid hareketi; ürün ve proje bilgisi çeki satırından tamamlanır.</summary>
    public sealed class GridMalKabulHareketDto
    {
        /// <summary>Ekranda satır kimliği: canlı ve arşiv kimlikleri çakışabildiği için kaynak öneklidir (C-12, A-12).</summary>
        public string Anahtar { get; init; } = string.Empty;
        public DateTime Tarih { get; init; }
        public int KullaniciId { get; init; }
        public string KullaniciAdi { get; init; } = string.Empty;
        public int ProjeId { get; init; }
        public string ProjeNo { get; init; } = string.Empty;
        /// <summary>Proje silinmişse boş.</summary>
        public int? ProjeTipiId { get; init; }
        /// <summary>İşlemin kısa adı: Tam Geldi, Eksik Geldi, Manuel ürün, Toplu sevk.</summary>
        public string IslemMetni { get; init; } = string.Empty;
        /// <summary>Tekil ya da toplu (aynı anda birden çok ürün işaretlendi) kayıt.</summary>
        public bool TopluMu { get; init; }
        /// <summary>Hareketin çeki satırı; satır silinmişse veya toplu sevk kaydıysa boş.</summary>
        public int? CekiSatiriId { get; init; }
        public string UrunMetni { get; init; } = string.Empty;
        public string? SandikNo { get; init; }
        /// <summary>Önceki Grid durumunun lookup metni ("Gelmedi"); manuel ve toplu sevk kaydında boş.</summary>
        public string? OncekiDurumMetni { get; init; }
        /// <summary>
        /// Grid'e gelen adet: hareket açıklamasındaki "Gelen:" eki; eski kayıtta boş. <see cref="GuncelGridGelenAdet"/>
        /// satırın bugünkü değeridir.
        /// </summary>
        public decimal? GelenAdet { get; init; }
        public decimal? GuncelGridGelenAdet { get; init; }
        /// <summary>Toplu sevk kaydındaki ürün sayısı; tekil kayıtta boş.</summary>
        public int? TopluUrunSayisi { get; init; }
        public string? Aciklama { get; init; }
    }
}
