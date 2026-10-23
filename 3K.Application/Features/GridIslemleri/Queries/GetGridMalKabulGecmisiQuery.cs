using _3K.Application.Common;
using _3K.Application.Features.GridIslemleri.DTOs;
using _3K.Core.Constants;
using _3K.Core.Enums;
using _3K.Core.Models;

namespace _3K.Application.Features.GridIslemleri.Queries
{
    /// <summary>
    /// Grid Sahası görünümü, mal kabul kayıtları: Grid personelinin yaptığı mal kabuller (kim, ne zaman, hangi ürün,
    /// kaç adet) canlı ve arşiv hareket tablolarından birlikte okunur. Varsayılan aralık son 30 gün, varsayılan tür
    /// yalnız mal kabul (Tam/Eksik Geldi geçişi ve Grid manuel ürün); toplu sevk istenirse eklenir. Salt okunurdur;
    /// ekran kodu <c>grid-saha-gorunumu</c> R.
    /// </summary>
    public sealed class GetGridMalKabulGecmisiQuery
        : PaginatedQuery<Result<GridMalKabulGecmisiDto>>, ISecuredRequest, IRequiresMenuPermission
    {
        /// <summary>Varsayılan aralık (gün): bitişten geriye.</summary>
        public const int VarsayilanGun = 30;

        public string RequiredMenuKod => YetkiKodlari.Sandik.GridSahaGorunumu;
        public YetkiTipi RequiredYetkiTipi => YetkiTipi.R;

        public DateTime? Baslangic { get; set; }
        public DateTime? Bitis { get; set; }
        public int? PersonelId { get; set; }
        public int? ProjeId { get; set; }
        /// <summary>Proje no, ürün (referans metni), kullanıcı adı ve açıklamada arar.</summary>
        public string? Arama { get; set; }
        /// <summary><see cref="GridMalKabulTuru"/> değerlerinden biri; boşsa yalnız mal kabul.</summary>
        public string? Tur { get; set; }

        /// <summary>Kabul edilen en eski / en yeni tarih (taşmayı ve sınırsız taramayı önler).</summary>
        public static readonly DateTime EnErkenTarih = new(2000, 1, 1);
        public static readonly DateTime EnGecTarih = new(2100, 12, 31);

        /// <summary>
        /// Uygulanan gün aralığı (iki uç dahil): bitiş verilmezse bugün, başlangıç verilmezse bitişten
        /// <see cref="VarsayilanGun"/> gün öncesi. Doğrulayıcı en fazla bir yıl sınırını bu aralıkla denetler; yalnız
        /// bir ucun verilmesi sınırı delmez.
        /// </summary>
        public (DateTime Baslangic, DateTime Bitis) EtkinAralik(DateTime bugun)
        {
            var bitis = (Bitis ?? bugun).Date;
            return ((Baslangic ?? bitis.AddDays(-VarsayilanGun)).Date, bitis);
        }
    }

    /// <summary>Mal kabul kayıtlarının tür seçimi (istemciden gelen değer bu kümeyle sınırlıdır).</summary>
    public static class GridMalKabulTuru
    {
        public const string MalKabul = "mal-kabul";
        public const string TopluSevk = "toplu-sevk";
        public const string Hepsi = "hepsi";

        public static bool Gecerli(string? deger) =>
            string.IsNullOrWhiteSpace(deger) || deger.Trim().ToLowerInvariant() is MalKabul or TopluSevk or Hepsi;

        public static GridHareketTuru Cevir(string? deger) => deger?.Trim().ToLowerInvariant() switch
        {
            TopluSevk => GridHareketTuru.TopluSevk,
            Hepsi => GridHareketTuru.MalKabul | GridHareketTuru.TopluSevk,
            _ => GridHareketTuru.MalKabul
        };
    }
}
