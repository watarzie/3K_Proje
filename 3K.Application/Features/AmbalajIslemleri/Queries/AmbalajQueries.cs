using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Core.Enums;
using _3K.Core.Models;
using _3K.Core.Constants;

namespace _3K.Application.Features.AmbalajIslemleri.Queries
{
    public sealed class GetAmbalajProjeleriQuery
        : PaginatedQuery<Result<PaginatedList<AmbalajProjeOzetDto>>>, ISecuredRequest, IRequiresMenuPermission, IAmbalajDurumKapsamli,
          IRequiresMenuPermissions
    {
        [System.Text.Json.Serialization.JsonIgnore] public int[]? IzinliDurumlar { get; set; }
        public string RequiredMenuKod => YetkiKodlari.Ambalaj.Listele;
        public YetkiTipi RequiredYetkiTipi => YetkiTipi.R;
        public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
            [MenuPermissionRequirement.Read(YetkiKodlari.Ambalaj.Listele)];
        public string? Arama { get; set; }
        public int? ProjeTipiId { get; set; }
    }

    public sealed class GetAmbalajUretimKayitlariQuery
        : PaginatedQuery<Result<PaginatedList<AmbalajUretimKaydiDto>>>, ISecuredRequest, IRequiresMenuPermission,
          IRequiresMenuPermissions, IAmbalajRaporFiltresi, IAmbalajDurumKapsamli
    {
        [System.Text.Json.Serialization.JsonIgnore] public int[]? IzinliDurumlar { get; set; }
        public string RequiredMenuKod => YetkiKodlari.Ambalaj.Listele;
        public YetkiTipi RequiredYetkiTipi => YetkiTipi.R;
        public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
            [MenuPermissionRequirement.Read(YetkiKodlari.Ambalaj.Listele)];
        public int? ProjeId { get; set; }
        public string? ManuelProjeNo { get; set; }
        public AmbalajSandikTuru? Tur { get; set; }
        public AmbalajKaynakModulu? KaynakModul { get; set; }
        public AmbalajSandikCinsi? SandikCinsi { get; set; }
        public AmbalajUretimDurumu? UretimDurumu { get; set; }
        public DateTime? BaslangicTarihi { get; set; }
        public DateTime? BitisTarihi { get; set; }
        public int? Ay { get; set; }
        public int? Yil { get; set; }
        public string? TalepEdenKisi { get; set; }
        public string? TalepEdenBolum { get; set; }
        public string? TalimatVeren { get; set; }
        public string? FirinPartiNo { get; set; }
        public string? Arama { get; set; }
        public bool IptallerDahil { get; set; }
        public bool? AmbalajaDahil { get; set; }
        public bool? UretimeAlindi { get; set; }
        public bool? OzelSandiklar { get; set; }
    }

    public sealed class GetAmbalajUretimKaydiDetayQuery
        : IRequest<Result<AmbalajUretimKaydiDetayDto>>, ISecuredRequest, IRequiresMenuPermission,
          IRequiresMenuPermissions
    {
        public string RequiredMenuKod => YetkiKodlari.Ambalaj.Listele;
        public YetkiTipi RequiredYetkiTipi => YetkiTipi.R;
        public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
            [MenuPermissionRequirement.Read(YetkiKodlari.Ambalaj.Listele)];
        public int Id { get; set; }
    }

    public sealed class GetAmbalajUretimSayfasiQuery
        : AmbalajRaporFiltresi, IRequest<Result<AmbalajUretimSayfasiDto>>, ISecuredRequest,
          IRequiresMenuPermission, IRequiresMenuPermissions
    {
        public string RequiredMenuKod => YetkiKodlari.Ambalaj.Listele;
        public YetkiTipi RequiredYetkiTipi => YetkiTipi.R;
        public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
            [MenuPermissionRequirement.Read(YetkiKodlari.Ambalaj.Listele)];
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public sealed class GetAmbalajManuelProjeSecenekleriQuery
        : IRequest<Result<AmbalajManuelProjeSecenekleriSayfasiDto>>, ISecuredRequest, IAmbalajDurumKapsamli,
          IRequiresMenuPermission, IRequiresMenuPermissions
    {
        [System.Text.Json.Serialization.JsonIgnore] public int[]? IzinliDurumlar { get; set; }
        public string RequiredMenuKod => YetkiKodlari.Ambalaj.Listele;
        public YetkiTipi RequiredYetkiTipi => YetkiTipi.R;
        public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
            [MenuPermissionRequirement.Read(YetkiKodlari.Ambalaj.Listele)];
        public string? Arama { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public interface IAmbalajRaporFiltresi
    {
        int? ProjeId { get; }
        string? ManuelProjeNo { get; }
        AmbalajSandikTuru? Tur { get; }
        AmbalajKaynakModulu? KaynakModul { get; }
        AmbalajSandikCinsi? SandikCinsi { get; }
        AmbalajUretimDurumu? UretimDurumu { get; }
        DateTime? BaslangicTarihi { get; }
        DateTime? BitisTarihi { get; }
        int? Ay { get; }
        int? Yil { get; }
        string? TalepEdenKisi { get; }
        string? TalepEdenBolum { get; }
        string? TalimatVeren { get; }
        string? FirinPartiNo { get; }
        string? Arama { get; }
        bool IptallerDahil { get; }
        bool? AmbalajaDahil { get; }
        bool? UretimeAlindi { get; }
        bool? OzelSandiklar { get; }
    }

    public abstract class AmbalajRaporFiltresi : IAmbalajRaporFiltresi, IAmbalajDurumKapsamli
    {
        [System.Text.Json.Serialization.JsonIgnore] public int[]? IzinliDurumlar { get; set; }
        public int? ProjeId { get; set; }
        public string? ManuelProjeNo { get; set; }
        public AmbalajSandikTuru? Tur { get; set; }
        public AmbalajKaynakModulu? KaynakModul { get; set; }
        public AmbalajSandikCinsi? SandikCinsi { get; set; }
        public AmbalajUretimDurumu? UretimDurumu { get; set; }
        public DateTime? BaslangicTarihi { get; set; }
        public DateTime? BitisTarihi { get; set; }
        public int? Ay { get; set; }
        public int? Yil { get; set; }
        public string? TalepEdenKisi { get; set; }
        public string? TalepEdenBolum { get; set; }
        public string? TalimatVeren { get; set; }
        public string? FirinPartiNo { get; set; }
        public string? Arama { get; set; }
        public bool IptallerDahil { get; set; }
        public bool? AmbalajaDahil { get; set; }
        public bool? UretimeAlindi { get; set; }
        public bool? OzelSandiklar { get; set; }
    }

    public sealed class GetAmbalajRaporQuery
        : AmbalajRaporFiltresi, IRequest<Result<AmbalajRaporDto>>, ISecuredRequest, IRequiresMenuPermission,
          IRequiresMenuPermissions
    {
        public string RequiredMenuKod => YetkiKodlari.Ambalaj.RaporGoruntule;
        public YetkiTipi RequiredYetkiTipi => YetkiTipi.R;
        public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
            [MenuPermissionRequirement.Read(YetkiKodlari.Ambalaj.RaporGoruntule)];
    }

    public sealed class GetAmbalajRaporDosyasiQuery
        : AmbalajRaporFiltresi, IRequest<Result<DosyaIcerigi>>, ISecuredRequest, IRequiresMenuPermission,
          IRequiresMenuPermissions
    {
        public string RequiredMenuKod => YetkiKodlari.Ambalaj.RaporGoruntule;
        public YetkiTipi RequiredYetkiTipi => YetkiTipi.R;
        public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
        [
            MenuPermissionRequirement.Read(YetkiKodlari.Ambalaj.RaporGoruntule),
            MenuPermissionRequirement.Read(string.Equals(Format, "pdf", StringComparison.OrdinalIgnoreCase)
                ? YetkiKodlari.Ambalaj.PdfIndir
                : YetkiKodlari.Ambalaj.ExcelIndir)
        ];
        /// <summary>xlsx veya pdf</summary>
        public string Format { get; set; } = "xlsx";
    }

    public sealed class GetAmbalajUretimFormuQuery
        : IRequest<Result<_3K.Core.Models.AmbalajUretimFormuModel>>, ISecuredRequest, IRequiresMenuPermission,
          IRequiresMenuPermissions
    {
        public string RequiredMenuKod => YetkiKodlari.Ambalaj.FormGoruntule;
        public YetkiTipi RequiredYetkiTipi => YetkiTipi.R;
        public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
        [
            MenuPermissionRequirement.Read(YetkiKodlari.Ambalaj.FormGoruntule)
        ];
        public int? KayitId { get; set; }
        public int? ProjeId { get; set; }
        public string? ManuelProjeNo { get; set; }
    }

    public sealed class GetAmbalajUretimFormuDosyasiQuery
        : IRequest<Result<DosyaIcerigi>>, ISecuredRequest, IRequiresMenuPermission, IRequiresMenuPermissions
    {
        internal const int EnFazlaSecilebilirKayit = 500;

        public string RequiredMenuKod => YetkiKodlari.Ambalaj.FormIndir;
        public YetkiTipi RequiredYetkiTipi => YetkiTipi.R;
        public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
        [
            MenuPermissionRequirement.Read(YetkiKodlari.Ambalaj.FormGoruntule),
            MenuPermissionRequirement.Read(YetkiKodlari.Ambalaj.FormIndir),
            MenuPermissionRequirement.Read(string.Equals(Format, "xlsx", StringComparison.OrdinalIgnoreCase)
                ? YetkiKodlari.Ambalaj.ExcelIndir
                : YetkiKodlari.Ambalaj.PdfIndir)
        ];
        public int? KayitId { get; set; }
        public int? ProjeId { get; set; }
        public string? ManuelProjeNo { get; set; }
        public AmbalajSandikTuru? Tur { get; set; }
        public bool? BagimsizKayitMi { get; set; }
        /// <summary>
        /// Aynı sistem ya da manuel proje altında kullanıcı tarafından seçilen ambalaj üretim kayıtları.
        /// Büyük seçimlerin query string sınırına takılmaması için POST dosya endpoint'i üzerinden gönderilir.
        /// </summary>
        public List<int> KayitIdleri { get; set; } = [];
        public string Format { get; set; } = "pdf";
    }
}
