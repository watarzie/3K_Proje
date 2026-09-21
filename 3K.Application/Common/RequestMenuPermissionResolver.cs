using _3K.Application.Behaviors;
using _3K.Application.Features.CekiIslemleri.Commands;
using _3K.Application.Features.CekiIslemleri.Queries;
using _3K.Application.Features.DashboardIslemleri.Queries;
using _3K.Application.Features.GridIslemleri.Commands;
using _3K.Application.Features.GridIslemleri.Queries;
using _3K.Application.Features.HareketGecmisiIslemleri.Queries;
using _3K.Application.Features.LookupIslemleri.Commands;
using _3K.Application.Features.PdfIslemleri.Commands;
using _3K.Application.Features.PdfIslemleri.Queries;
using _3K.Application.Features.ProjeIslemleri.Commands;
using _3K.Application.Features.ProjeIslemleri.Queries;
using _3K.Application.Features.RolIslemleri.Commands;
using _3K.Application.Features.RolIslemleri.Queries;
using _3K.Application.Features.SandikIslemleri.Commands;
using _3K.Application.Features.SandikIslemleri.Queries;
using _3K.Application.Features.StokIslemleri.Commands;
using _3K.Application.Features.StokIslemleri.Queries;
using _3K.Application.Features.UcKIslemleri.Commands;
using _3K.Application.Features.UcKIslemleri.Queries;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using SandikKapat = _3K.Application.Features.SandikIslemleri.Commands.SandikKapatCommand;
using EskiSandikKapat = _3K.Application.Features.ProjeIslemleri.Commands.SandikKapatCommand;

namespace _3K.Application.Common;

/// <summary>
/// İşlem/izin kataloğu. Yeni korumalı bir kullanım senaryosu burada veya açık
/// permission sözleşmesinde tanımlanmadan çalışmaz. İstemci menü başlığı okunmaz.
/// </summary>
public sealed class RequestMenuPermissionResolver(IUnitOfWork unitOfWork, IReadQueryExecutor queryExecutor,
    ICurrentUserService? currentUserService = null)
    : IRequestMenuPermissionResolver
{
    public async Task<RequestMenuPermissions> ResolveAsync(object request, CancellationToken cancellationToken)
    {
        if (request is SandikUrunTasiCommand transfer && transfer.IslemAnahtari != Guid.Empty &&
            currentUserService?.UserId is int userId)
        {
            // Tam tahsis taşındığında kaynak içerik silinebilir. Ağ tekrarını ancak
            // aynı kullanıcı + aynı değişmez hareket snapshot'ı kanıtlarsa yetkilendir.
            var replay = await queryExecutor.ToListAsync(unitOfWork.GetRepository<SandikUrunTransferi>().Queryable()
                .Where(x => x.IslemAnahtari == transfer.IslemAnahtari && x.KullaniciId == userId &&
                    x.ProjeId == transfer.ProjeId && x.KaynakSandikIcerikId == transfer.KaynakSandikIcerikId &&
                    x.HedefSandikId == transfer.HedefSandikId && x.Miktar == transfer.TasinanAdet)
                .Select(x => x.ProjeId), cancellationToken);
            if (replay.Count == 1)
                return await ResolveProjectAccessAsync(
                    new(ProjectMenuOperation.CrateWrite, YetkiTipi.W, replay[0]), cancellationToken);
        }
        var fixedPolicy = GetFixedPolicy(request);
        if (fixedPolicy != null)
            return fixedPolicy;

        var spec = GetProjectAccess(request);
        if (spec != null)
            return await ResolveProjectAccessAsync(spec, cancellationToken);

        return AuthorizationBehavior<object, Result>.DeclaredPermissions(request);
    }

    public static bool HasServerDefinition(object request) =>
        request is IRequiresMenuPermission or IRequiresMenuPermissions or
            IRequiresProjectMenuPermission or IRequiresSandikMenuPermission ||
        GetFixedPolicy(request) != null || GetProjectAccess(request) != null;

    private static RequestMenuPermissions? GetFixedPolicy(object request) => request switch
        {
            RolOlusturCommand or RolGuncelleCommand or RolSilCommand => new(
                [new([new("rol-yonetimi", YetkiTipi.W), new(_3K.Core.Constants.YetkiKodlari.YetkiAtama, YetkiTipi.W)])]),
            GetRolDetayQuery => One("rol-yonetimi", YetkiTipi.R),
            GetRollerQuery => Any(YetkiTipi.R, "rol-yonetimi", "kullanicilar", "onay-kurallari-yonet"),
            DashboardOzetQuery or DashboardProjelerQuery or DashboardKritikEksiklerQuery or
                DashboardEksikSiralamaQuery or DashboardSahayaAktarilanSandiklarQuery or
                DashboardProjeFilterOptionsQuery or DashboardProjeSandikDurumQuery => One("dashboard", YetkiTipi.R),
            GetUcKIsListesiQuery => One("3k-is-listesi", YetkiTipi.R),
            GetProjeHareketleriQuery => One("hareket-gecmisi", YetkiTipi.R),
            GetEksikUrunlerByProjeQuery => Any(YetkiTipi.R, "sahaya-aktar", "saha-sandiklar", "yedek-sandiklar", "sandik-yonetimi"),
            StokListeleQuery or GetStokPdfQuery => One("stok", YetkiTipi.R),
            StokKaydiGuncelleCommand or StokKaydiOlusturCommand or StokKarsilaCommand => One("stok", YetkiTipi.W),
            DepoLokasyonOlusturCommand or DepoLokasyonSilCommand => One("depo-durumu", YetkiTipi.W),
            KaliteDurumGuncelleCommand => One("kalite-modulu", YetkiTipi.W),
            SurecDurumGuncelleCommand => One("surec-modulu", YetkiTipi.W),
            CekiSatiriAnaVeriGuncelleCommand => One("ceki-verisi-duzenle", YetkiTipi.W),
            CekiSatirlariSilCommand => One("ceki-verisi-sil", YetkiTipi.W),
            GetDepoSandikPdfQuery or GetProjeDepoSandikPdfQuery => One("depo-durumu", YetkiTipi.R),
            ProjeOlusturCommand x => ProjectPolicy(x.ProjeTipiId, ProjectMenuOperation.ProjectCreate, YetkiTipi.W),
            ProjeListeleQuery x => ProjectListPolicy(x.ProjeTipiId, x.IsSevkEdilen),
            // Sadece seçim için asgari proje bilgisi dönen ortak lookup; operasyon izni vermez.
            ProjeDropdownQuery => Any(YetkiTipi.R, "aktif-projeler", "sevk-edilen", "sandik-yonetimi",
                "saha-yonetimi", "yedek-yonetimi", "grid-modulu", "3k-modulu", "saha-grid-modulu",
                "saha-3k-modulu", "yedek-grid-modulu", "yedek-3k-modulu", "sahaya-aktar",
                "depo-durumu", "stok", "hareket-gecmisi", "grid-is-listesi", "3k-is-listesi"),
            _ => null
        };

    private static ProjectAccess? GetProjectAccess(object request) => request switch
    {
        IRequiresProjectMenuPermission x => new(x.PermissionOperation, x.RequiredYetkiTipi, x.PermissionProjectId),
        IRequiresSandikMenuPermission x => new(x.PermissionOperation, x.RequiredYetkiTipi, SandikIds: x.PermissionSandikIds),
        GetGridUrunlerQuery x => new(ProjectMenuOperation.Grid, YetkiTipi.R, x.ProjeId, Optional(x.SandikId)),
        GetUcKUrunlerQuery x => new(ProjectMenuOperation.UcK, YetkiTipi.R, x.ProjeId, Optional(x.SandikId)),
        GetProjeSandiklariQuery x => new(ProjectMenuOperation.CrateRead, YetkiTipi.R, x.ProjeId),
        GetSandikIcerikQuery x => new(ProjectMenuOperation.CrateRead, YetkiTipi.R, SandikIds: [x.SandikId]),
        GetEksikUrunlerQuery x => new(ProjectMenuOperation.ProjectRead, YetkiTipi.R, x.ProjeId),
        CekiSatirlariQuery x => new(ProjectMenuOperation.ProjectRead, YetkiTipi.R, CekiIds: [x.CekiId]),
        GetProjeSevkiyatlariQuery x => new(ProjectMenuOperation.ProjectRead, YetkiTipi.R, x.ProjeId),
        GetSahaProjePdfQuery x => new(ProjectMenuOperation.CrateReport, YetkiTipi.R, x.ProjeId),
        GetSahaSandikPdfQuery x => new(ProjectMenuOperation.CrateReport, YetkiTipi.R, SandikIds: [x.SandikId]),
        GetGerceklesenCekiListesiPdfQuery x => new(ProjectMenuOperation.ActualPackingReport, YetkiTipi.R, x.ProjeId),
        GetGerceklesenCekiListesiExcelQuery x => new(ProjectMenuOperation.ActualPackingReport, YetkiTipi.R, x.ProjeId),
        GetSahaGerceklesenCekiListesiPdfQuery x => new(ProjectMenuOperation.ActualPackingReport, YetkiTipi.R, x.ProjeId),
        GetSahaGerceklesenCekiListesiExcelQuery x => new(ProjectMenuOperation.ActualPackingReport, YetkiTipi.R, x.ProjeId),
        GetUcKSandikDurumPdfQuery x => new(ProjectMenuOperation.UcKCrateReport, YetkiTipi.R, x.ProjeId),
        PdfOlusturCommand x => new(ProjectMenuOperation.CrateReport, YetkiTipi.R, x.ProjeId),
        ExcelOlusturCommand x => new(ProjectMenuOperation.CrateReport, YetkiTipi.R, x.ProjeId),
        ProjeSevkEtCommand x => new(ProjectMenuOperation.Shipment, YetkiTipi.W, x.ProjeId),
        ProjeKilidiAcCommand x => new(ProjectMenuOperation.ProjectUnlock, YetkiTipi.W, x.ProjeId),
        ProjeSevkTarihiGuncelleCommand x => new(ProjectMenuOperation.PlannedShipmentDate, YetkiTipi.W, x.ProjeId),
        ProjeSilCommand x => new(ProjectMenuOperation.ProjectDelete, YetkiTipi.W, x.ProjeId),
        GridManuelUrunEkleCommand x => new(ProjectMenuOperation.Grid, YetkiTipi.W, x.ProjeId),
        GridDurumGuncelleCommand x => new(ProjectMenuOperation.Grid, YetkiTipi.W, x.ProjeId, CekiSatiriIds: [x.CekiSatiriId]),
        GridDurumSifirlaCommand x => new(ProjectMenuOperation.Grid, YetkiTipi.W, x.ProjeId, CekiSatiriIds: [x.CekiSatiriId]),
        GridTopluDurumGuncelleCommand x => new(ProjectMenuOperation.Grid, YetkiTipi.W, x.ProjeId, CekiSatiriIds: x.CekiSatiriIdler),
        GridTopluSevkCommand x => new(ProjectMenuOperation.Grid, YetkiTipi.W, x.ProjeId, CekiSatiriIds: x.CekiSatiriIdler),
        GridTopluSifirlaCommand x => new(ProjectMenuOperation.Grid, YetkiTipi.W, x.ProjeId, CekiSatiriIds: x.CekiSatiriIdler),
        UcKDurumGuncelleCommand x => new(ProjectMenuOperation.UcK, YetkiTipi.W, x.ProjeId,
            CekiSatiriIds: Optional(x.CekiSatiriId), IcerikIds: Optional(x.SandikIcerikId)),
        UcKDurumSifirlaCommand x => new(ProjectMenuOperation.UcK, YetkiTipi.W, x.ProjeId,
            CekiSatiriIds: Optional(x.CekiSatiriId), IcerikIds: Optional(x.SandikIcerikId)),
        UcKTopluTamGeldiCommand x => UcKTopluAccess(x.ProjeId, x.CekiSatiriIdler, x.Secimler),
        UcKTopluTedarikciCommand x => UcKTopluAccess(x.ProjeId, x.CekiSatiriIdler, x.Secimler),
        UcKTopluSifirlaCommand x => UcKTopluAccess(x.ProjeId, x.CekiSatiriIdler, x.Secimler),
        TopluDurumGuncelleCommand x => UcKTopluAccess(x.ProjeId, x.CekiSatiriIdler, x.Secimler),
        SandikEkleCommand x => new(ProjectMenuOperation.CrateWrite, YetkiTipi.W, x.ProjeId),
        SandikOzellikGuncelleCommand x => new(ProjectMenuOperation.CrateWrite, YetkiTipi.W, SandikIds: [x.SandikId]),
        SandikKapat x => new(ProjectMenuOperation.CrateWrite, YetkiTipi.W, SandikIds: [x.SandikId]),
        EskiSandikKapat x => new(ProjectMenuOperation.CrateWrite, YetkiTipi.W, SandikIds: [x.SandikId]),
        TopluSandikKapatCommand x => new(ProjectMenuOperation.CrateWrite, YetkiTipi.W, SandikIds: x.SandikIds),
        SandikSilCommand x => new(ProjectMenuOperation.CrateWrite, YetkiTipi.W, x.ProjeId, [x.SandikId]),
        SandikKilidiAcCommand x => new(ProjectMenuOperation.CrateWrite, YetkiTipi.W, x.ProjeId, [x.SandikId]),
        SandikSevkEtCommand x => new(ProjectMenuOperation.CrateWrite, YetkiTipi.W, x.ProjeId, [x.SandikId]),
        SandikSevkiyatDuzeltmeTamamlaCommand x => new(ProjectMenuOperation.CrateWrite, YetkiTipi.W, x.ProjeId, [x.SandikId]),
        SandikUrunTasiCommand x => new(ProjectMenuOperation.CrateWrite, YetkiTipi.W, x.ProjeId,
            [x.HedefSandikId], IcerikIds: [x.KaynakSandikIcerikId]),
        ManuelUrunEkleCommand x => new(ProjectMenuOperation.CrateWrite, YetkiTipi.W, x.ProjeId, [x.SandikId]),
        // CekiSatiriId burada başka normal projeden alınan eksik kaynağıdır;
        // hedefle aynı projeye zorlamak mevcut saha tamamlama akışını bozar.
        // Kaynak uygunluğu/kapasitesi handler'ın aktarım politikasıyla doğrulanır.
        SahaYedekMalzemeEkleCommand x => new(ProjectMenuOperation.CrateWrite, YetkiTipi.W, x.ProjeId, [x.SandikId]),
        ManuelUrunSilCommand x => new(ProjectMenuOperation.CrateWrite, YetkiTipi.W, x.ProjeId,
            CekiSatiriIds: Optional(x.CekiSatiriId), IcerikIds: Optional(x.SandikIcerikId)),
        UrunGuncelleCommand x => new(ProjectMenuOperation.CrateWrite, YetkiTipi.W, x.ProjeId,
            [x.SandikId], Optional(x.CekiSatiriId), Optional(x.SandikIcerikId)),
        UrunIptalCommand x => new(ProjectMenuOperation.Grid, YetkiTipi.W, x.ProjeId, CekiSatiriIds: [x.CekiSatiriId]),
        FiiliSandikDegistirCommand x => new(ProjectMenuOperation.CrateWrite, YetkiTipi.W, x.ProjeId, CekiSatiriIds: [x.CekiSatiriId]),
        UcKTeslimAlCommand x => new(ProjectMenuOperation.UcK, YetkiTipi.W, x.ProjeId, CekiSatiriIds: [x.CekiSatiriId]),
        UcKTopluTeslimAlCommand x when x.Urunler == null || x.Urunler.Any(s => s == null)
            => new(ProjectMenuOperation.UcK, YetkiTipi.W, -1),
        UcKTopluTeslimAlCommand x => new(ProjectMenuOperation.UcK, YetkiTipi.W, x.ProjeId,
            CekiSatiriIds: x.Urunler.Select(s => s.CekiSatiriId).Where(id => id > 0).ToArray(),
            IcerikIds: x.Urunler.Where(s => s.SandikIcerikId.HasValue).Select(s => s.SandikIcerikId!.Value).ToArray()),
        FBDenKarsilaCommand x => new(ProjectMenuOperation.UcK, YetkiTipi.W, x.ProjeId, CekiSatiriIds: [x.CekiSatiriId]),
        StoktanKarsilaCommand x => new(ProjectMenuOperation.UcK, YetkiTipi.W, x.ProjeId, CekiSatiriIds: [x.CekiSatiriId]),
        _ => null
    };

    private static ProjectAccess UcKTopluAccess(int projectId, IReadOnlyCollection<int>? ids,
        IReadOnlyCollection<UcKSandikSecimDto>? selections)
    {
        // Authorization, validation'dan önce çalışır. Bozuk JSON koleksiyonu 500
        // üretmemeli ve eksik ilişkiyi atlayarak yetki kazanamamalıdır.
        if (ids == null || selections == null || selections.Any(x => x == null))
            return new(ProjectMenuOperation.UcK, YetkiTipi.W, -1);
        return new(ProjectMenuOperation.UcK, YetkiTipi.W, projectId,
            CekiSatiriIds: ids.Concat(selections.Select(x => x.CekiSatiriId)).Where(x => x > 0).ToArray(),
            IcerikIds: selections.Where(x => x.SandikIcerikId.HasValue).Select(x => x.SandikIcerikId!.Value).ToArray());
    }

    private async Task<RequestMenuPermissions> ResolveProjectAccessAsync(ProjectAccess spec, CancellationToken cancellationToken)
    {
        var projects = new HashSet<int>();
        if (spec.ProjectId.HasValue)
        {
            if (spec.ProjectId.Value <= 0) return RequestMenuPermissions.Denied;
            projects.Add(spec.ProjectId.Value);
        }

        var sandikQuery = unitOfWork.GetRepository<Sandik>().Queryable();
        var cekiQuery = unitOfWork.GetRepository<Ceki>().Queryable();
        var satirQuery = unitOfWork.GetRepository<CekiSatiri>().Queryable();
        var icerikQuery = unitOfWork.GetRepository<SandikIcerik>().Queryable();
        if (!await AddProjectsAsync(spec.SandikIds, sandikQuery.Select(x => new Relation { Id = x.Id, ProjectId = x.ProjeId }), projects, cancellationToken) ||
            !await AddProjectsAsync(spec.CekiIds, cekiQuery.Select(x => new Relation { Id = x.Id, ProjectId = x.ProjeId }), projects, cancellationToken) ||
            !await AddProjectsAsync(spec.CekiSatiriIds, from s in satirQuery join c in cekiQuery on s.CekiId equals c.Id
                select new Relation { Id = s.Id, ProjectId = c.ProjeId }, projects, cancellationToken) ||
            !await AddProjectsAsync(spec.IcerikIds, from i in icerikQuery join s in sandikQuery on i.SandikId equals s.Id
                select new Relation { Id = i.Id, ProjectId = s.ProjeId }, projects, cancellationToken))
            return RequestMenuPermissions.Denied;

        // Gövde proje kimliği, başka projenin satır/sandığını yetkilendiremez.
        if (projects.Count == 0 || spec.ProjectId.HasValue && projects.Any(x => x != spec.ProjectId.Value))
            return RequestMenuPermissions.Denied;
        var ids = projects.ToArray();
        var types = await queryExecutor.ToListAsync(unitOfWork.GetRepository<Proje>().Queryable()
            .Where(x => ids.Contains(x.Id)).Select(x => new { x.Id, x.ProjeTipiId }), cancellationToken);
        if (types.Count != ids.Length) return RequestMenuPermissions.Denied;
        var groups = new List<MenuPermissionGroup>();
        foreach (var project in types)
        {
            var policy = ProjectPolicy(project.ProjeTipiId, spec.Operation, spec.Yetki);
            if (policy.Groups.Count == 0) return RequestMenuPermissions.Denied;
            groups.AddRange(policy.Groups);
        }
        return new(groups);
    }

    private async Task<bool> AddProjectsAsync(IReadOnlyCollection<int>? ids, IQueryable<Relation> query,
        HashSet<int> projects, CancellationToken cancellationToken)
    {
        if (ids == null || ids.Count == 0) return true;
        if (ids.Any(x => x <= 0)) return false;
        var distinct = ids.Distinct().ToArray();
        var relations = await queryExecutor.ToListAsync(query.Where(x => distinct.Contains(x.Id)), cancellationToken);
        if (relations.Count != distinct.Length) return false;
        foreach (var relation in relations) projects.Add(relation.ProjectId);
        return true;
    }

    private static RequestMenuPermissions ProjectListPolicy(int? type, bool? shipped)
    {
        if (shipped == true) return One("sevk-edilen", YetkiTipi.R);
        if (type.HasValue) return ProjectPolicy(type.Value, ProjectMenuOperation.ProjectRead, YetkiTipi.R);
        return Any(YetkiTipi.R, "aktif-projeler", "sandik-yonetimi", "sevk-edilen", "depo-durumu");
    }

    public static RequestMenuPermissions ProjectPolicy(int type, ProjectMenuOperation operation, YetkiTipi yetki)
    {
        if (!Enum.IsDefined((ProjeTipi)type)) return RequestMenuPermissions.Denied;
        var prefix = type == (int)ProjeTipi.Saha ? "saha-" : type == (int)ProjeTipi.Yedek ? "yedek-" : "";
        var projectMenu = type == (int)ProjeTipi.Normal ? "sandik-yonetimi" : prefix + "yonetimi";
        var crateMenu = type == (int)ProjeTipi.Normal ? "sandik-yonetimi" : prefix + "sandiklar";
        return operation switch
        {
            ProjectMenuOperation.Grid => One(prefix + "grid-modulu", yetki),
            ProjectMenuOperation.UcK => One(prefix + "3k-modulu", yetki),
            ProjectMenuOperation.CrateWrite => Any(yetki, crateMenu, prefix + "3k-modulu"),
            ProjectMenuOperation.CrateRead => Any(yetki, crateMenu, projectMenu, prefix + "3k-modulu", prefix + "grid-modulu", "depo-durumu", "sahaya-aktar"),
            ProjectMenuOperation.ProjectRead => type == (int)ProjeTipi.Normal
                ? Any(yetki, "aktif-projeler", "sandik-yonetimi", "sevk-edilen", "depo-durumu", "grid-modulu", "3k-modulu", "sahaya-aktar")
                : Any(yetki, projectMenu, crateMenu, prefix + "grid-modulu", prefix + "3k-modulu", "sevk-edilen", "depo-durumu", "sahaya-aktar"),
            ProjectMenuOperation.ProjectCreate => type == (int)ProjeTipi.Normal
                ? Any(yetki, "aktif-projeler", "sandik-yonetimi") : One(projectMenu, yetki),
            ProjectMenuOperation.RevisionHistory => Any(yetki, projectMenu, "ceki-revizyon-yukle", "sevk-edilen"),
            ProjectMenuOperation.ProjectUnlock => Any(yetki, projectMenu, "sevk-edilen"),
            ProjectMenuOperation.Shipment => One(type == (int)ProjeTipi.Normal ? "proje-sevk-et" : prefix + "sevk-et", yetki),
            ProjectMenuOperation.ProjectDelete => One(prefix + "proje-sil", yetki),
            ProjectMenuOperation.PlannedShipmentDate => One(type == (int)ProjeTipi.Yedek ? "yedek-planlanan-sevk-tarihi" : "planlanan-sevk-tarihi", yetki),
            ProjectMenuOperation.ActualPackingReport => One(prefix + "gerceklesen-ceki-raporu", yetki),
            ProjectMenuOperation.UcKCrateReport => One(prefix + "3k-sandik-durum-raporu", yetki),
            ProjectMenuOperation.CrateReport => type == (int)ProjeTipi.Normal
                ? Any(yetki, "sandik-yonetimi", "aktif-projeler") : One(prefix + "raporu", yetki),
            _ => RequestMenuPermissions.Denied
        };
    }

    private static RequestMenuPermissions One(string menu, YetkiTipi yetki) => new([new([new(menu, yetki)])]);
    private static RequestMenuPermissions Any(YetkiTipi yetki, params string[] menus) =>
        new([new(menus.Distinct().Select(x => new MenuPermissionRequirement(x, yetki)).ToArray(), MenuPermissionMatch.Any)]);
    private static int[] Optional(int? id) => id is > 0 ? [id.Value] : [];
    private sealed class Relation
    {
        public int Id { get; init; }
        public int ProjectId { get; init; }
    }
    private sealed record ProjectAccess(ProjectMenuOperation Operation, YetkiTipi Yetki, int? ProjectId = null,
        IReadOnlyCollection<int>? SandikIds = null, IReadOnlyCollection<int>? CekiSatiriIds = null,
        IReadOnlyCollection<int>? IcerikIds = null, IReadOnlyCollection<int>? CekiIds = null);
}
