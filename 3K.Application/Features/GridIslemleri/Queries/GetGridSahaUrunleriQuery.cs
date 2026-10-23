using _3K.Application.Common;
using _3K.Application.Features.GridIslemleri.DTOs;
using _3K.Core.Constants;
using _3K.Core.Enums;

namespace _3K.Application.Features.GridIslemleri.Queries
{
    /// <summary>
    /// Grid Sahası görünümü, ürünler: Grid'e fiziksel gelmiş (Tam/Eksik Geldi) ürünler, proje tipinden bağımsız tek
    /// listede. Varsayılan kapsam Grid'de bekleyenler (3K'ya ulaşmamış miktarı olanlar); "tümü" gelmiş her ürünü
    /// gösterir. Sevk edilmiş sandık ya da proje satırları iki kapsamda da Grid iş listesindeki gibi dışarıdadır.
    /// Salt okunurdur; ekran kodu <c>grid-saha-gorunumu</c> R.
    /// </summary>
    public sealed class GetGridSahaUrunleriQuery
        : PaginatedQuery<Result<GridSahaUrunleriDto>>, ISecuredRequest, IRequiresMenuPermission
    {
        public string RequiredMenuKod => YetkiKodlari.Sandik.GridSahaGorunumu;
        public YetkiTipi RequiredYetkiTipi => YetkiTipi.R;

        /// <summary><see cref="GridSahaKapsami"/> değerlerinden biri; boşsa Grid'de bekleyenler.</summary>
        public string? Kapsam { get; set; }
        /// <summary>Proje no, müşteri, barkod, ürün açıklaması, ölçü resmi poz no ve sandık numarasında arar.</summary>
        public string? Arama { get; set; }
        public int? ProjeTipiId { get; set; }
        public int? ProjeId { get; set; }
        /// <summary>Tam Geldi (8) veya Eksik Geldi (9).</summary>
        public int? GridDurumuId { get; set; }
        /// <summary><see cref="GridSahaSiralama"/> değerlerinden biri; boşsa proje.</summary>
        public string? Siralama { get; set; }
        public bool Azalan { get; set; }
    }

    /// <summary>Grid Sahası ürün listesinin kapsamı.</summary>
    public static class GridSahaKapsami
    {
        /// <summary>Gelmiş ve 3K'ya ulaşmamış miktarı olan (Grid'de bekleyen) ürünler.</summary>
        public const string Bekleyen = "bekleyen";
        /// <summary>Grid'e gelmiş bütün ürünler (3K'ya gönderilmiş olanlar dahil).</summary>
        public const string Tumu = "tumu";

        public static bool Gecerli(string? deger) =>
            string.IsNullOrWhiteSpace(deger) || deger.Trim().ToLowerInvariant() is Bekleyen or Tumu;

        public static bool TumuMu(string? deger) => string.Equals(deger?.Trim(), Tumu, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Grid Sahası ürün listesinin sıralama alanları (istemciden gelen değer bu kümeyle sınırlıdır).</summary>
    public static class GridSahaSiralama
    {
        public const string Proje = "proje";
        public const string Bekleyen = "bekleyen";
        public const string Sandik = "sandik";

        public static readonly IReadOnlyList<string> Hepsi = [Proje, Bekleyen, Sandik];

        public static bool Gecerli(string? deger) =>
            !string.IsNullOrWhiteSpace(deger) && Hepsi.Contains(deger.Trim().ToLowerInvariant());

        public static string Normalize(string? deger) =>
            Gecerli(deger) ? deger!.Trim().ToLowerInvariant() : Proje;
    }
}
