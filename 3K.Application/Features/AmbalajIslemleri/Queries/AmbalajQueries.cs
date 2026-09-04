using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Core.Enums;

namespace _3K.Application.Features.AmbalajIslemleri.Queries
{
    public sealed class GetAmbalajProjeleriQuery
        : PaginatedQuery<Result<PaginatedList<AmbalajProjeOzetDto>>>, ISecuredRequest, IRequiresMenuPermission,
          IRequiresMenuPermissions
    {
        public string RequiredMenuKod => AmbalajMenuKodlari.Listele;
        public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
            [AmbalajMenuKodlari.Read(AmbalajMenuKodlari.Listele)];
        public string? Arama { get; set; }
        public int? ProjeTipiId { get; set; }
    }

    public sealed class GetAmbalajUretimKayitlariQuery
        : PaginatedQuery<Result<PaginatedList<AmbalajUretimKaydiDto>>>, ISecuredRequest, IRequiresMenuPermission,
          IRequiresMenuPermissions, IAmbalajRaporFiltresi
    {
        public string RequiredMenuKod => AmbalajMenuKodlari.Listele;
        public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
            [AmbalajMenuKodlari.Read(AmbalajMenuKodlari.Listele)];
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
        public string RequiredMenuKod => AmbalajMenuKodlari.Listele;
        public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
            [AmbalajMenuKodlari.Read(AmbalajMenuKodlari.Listele)];
        public int Id { get; set; }
    }

    public sealed class GetAmbalajUretimSayfasiQuery
        : AmbalajRaporFiltresi, IRequest<Result<AmbalajUretimSayfasiDto>>, ISecuredRequest,
          IRequiresMenuPermission, IRequiresMenuPermissions
    {
        public string RequiredMenuKod => AmbalajMenuKodlari.Listele;
        public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
            [AmbalajMenuKodlari.Read(AmbalajMenuKodlari.Listele)];
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public sealed class GetAmbalajManuelProjeSecenekleriQuery
        : IRequest<Result<AmbalajManuelProjeSecenekleriSayfasiDto>>, ISecuredRequest,
          IRequiresMenuPermission, IRequiresMenuPermissions
    {
        public string RequiredMenuKod => AmbalajMenuKodlari.Listele;
        public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
            [AmbalajMenuKodlari.Read(AmbalajMenuKodlari.Listele)];
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

    public abstract class AmbalajRaporFiltresi : IAmbalajRaporFiltresi
    {
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
        public string RequiredMenuKod => AmbalajMenuKodlari.RaporGoruntule;
        public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
            [AmbalajMenuKodlari.Read(AmbalajMenuKodlari.RaporGoruntule)];
    }

    public sealed class GetAmbalajRaporDosyasiQuery
        : AmbalajRaporFiltresi, IRequest<Result<AmbalajDosyaDto>>, ISecuredRequest, IRequiresMenuPermission,
          IRequiresMenuPermissions
    {
        public string RequiredMenuKod => AmbalajMenuKodlari.RaporGoruntule;
        public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
        [
            AmbalajMenuKodlari.Read(AmbalajMenuKodlari.RaporGoruntule),
            AmbalajMenuKodlari.Read(AmbalajMenuKodlari.M3Goruntule),
            AmbalajMenuKodlari.Read(AmbalajMenuKodlari.SarfGoruntule),
            AmbalajMenuKodlari.Read(AmbalajMenuKodlari.KaynakGoruntule),
            AmbalajMenuKodlari.Write(string.Equals(Format, "pdf", StringComparison.OrdinalIgnoreCase)
                ? AmbalajMenuKodlari.PdfIndir
                : AmbalajMenuKodlari.ExcelIndir)
        ];
        /// <summary>xlsx veya pdf</summary>
        public string Format { get; set; } = "xlsx";
    }

    public sealed class GetAmbalajUretimFormuQuery
        : IRequest<Result<_3K.Core.Models.AmbalajUretimFormuModel>>, ISecuredRequest, IRequiresMenuPermission,
          IRequiresMenuPermissions
    {
        public string RequiredMenuKod => AmbalajMenuKodlari.FormGoruntule;
        public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
        [
            AmbalajMenuKodlari.Read(AmbalajMenuKodlari.FormGoruntule),
            AmbalajMenuKodlari.Read(AmbalajMenuKodlari.M3Goruntule),
            AmbalajMenuKodlari.Read(AmbalajMenuKodlari.SarfGoruntule)
        ];
        public int? KayitId { get; set; }
        public int? ProjeId { get; set; }
        public string? ManuelProjeNo { get; set; }
    }

    public sealed class GetAmbalajUretimFormuDosyasiQuery
        : IRequest<Result<AmbalajDosyaDto>>, ISecuredRequest, IRequiresMenuPermission, IRequiresMenuPermissions
    {
        internal const int EnFazlaSecilebilirKayit = 500;

        public string RequiredMenuKod => AmbalajMenuKodlari.FormIndir;
        public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
        [
            AmbalajMenuKodlari.Read(AmbalajMenuKodlari.FormGoruntule),
            AmbalajMenuKodlari.Write(AmbalajMenuKodlari.FormIndir),
            AmbalajMenuKodlari.Read(AmbalajMenuKodlari.M3Goruntule),
            AmbalajMenuKodlari.Read(AmbalajMenuKodlari.SarfGoruntule),
            AmbalajMenuKodlari.Write(string.Equals(Format, "xlsx", StringComparison.OrdinalIgnoreCase)
                ? AmbalajMenuKodlari.ExcelIndir
                : AmbalajMenuKodlari.PdfIndir)
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
