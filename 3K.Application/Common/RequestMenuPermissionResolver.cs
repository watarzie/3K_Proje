using _3K.Application.Behaviors;
using _3K.Application.Features.CekiIslemleri.Commands;
using _3K.Application.Features.DashboardIslemleri.Queries;
using _3K.Application.Features.GridIslemleri.Commands;
using _3K.Application.Features.GridIslemleri.Queries;
using _3K.Application.Features.HareketGecmisiIslemleri.Queries;
using _3K.Application.Features.LookupIslemleri.Commands;
using _3K.Application.Features.PdfIslemleri.Queries;
using _3K.Application.Features.ProjeIslemleri.Commands;
using _3K.Application.Features.ProjeIslemleri.Queries;
using _3K.Application.Features.RolIslemleri.Commands;
using _3K.Application.Features.RolIslemleri.Queries;
using _3K.Application.Features.SandikIslemleri.Commands;
using _3K.Application.Features.SandikIslemleri.Queries;
using _3K.Application.Features.SevkiyatIslemleri.Queries;
using _3K.Application.Features.StokIslemleri.Commands;
using _3K.Application.Features.StokIslemleri.Queries;
using _3K.Application.Features.UcKIslemleri.Commands;
using _3K.Application.Features.UcKIslemleri.Queries;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using SandikKapat = _3K.Application.Features.SandikIslemleri.Commands.SandikKapatCommand;
using EskiSandikKapat = _3K.Application.Features.ProjeIslemleri.Commands.SandikKapatCommand;
using K = _3K.Core.Constants.YetkiKodlari;

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
            RolOlusturCommand or RolGuncelleCommand or RolSilCommand => One(K.RolYonetimi, YetkiTipi.W),
            GetRolDetayQuery => One(K.RolYonetimi, YetkiTipi.R),
            GetRollerQuery => Any(YetkiTipi.R, K.RolYonetimi, K.Kullanicilar, K.Onay.KurallariYonet),
            DashboardOzetQuery or DashboardProjelerQuery or DashboardKritikEksiklerQuery or
                DashboardEksikSiralamaQuery or DashboardSahayaAktarilanSandiklarQuery or
                DashboardProjeFilterOptionsQuery or DashboardProjeSandikDurumQuery => One(K.Dashboard, YetkiTipi.R),
            GetUcKIsListesiQuery => One(K.Sandik.UcKIsListesi, YetkiTipi.R),
            GetProjeHareketleriQuery => One(K.HareketGecmisi, YetkiTipi.R),
            GetEksikUrunlerByProjeQuery => Any(YetkiTipi.R, K.Saha.SahayaAktar, K.Saha.Sandiklar, K.Yedek.Sandiklar,
                K.Sandik.Yonetimi),
            StokListeleQuery or StokDurumSayilariQuery or GetStokPdfQuery => One(K.Stok.Kok, YetkiTipi.R),
            // Sevkiyatlar sayfası: tır seferleri ve istatistikler bütün proje tiplerini okur; ayrı görüntüleme izni.
            SevkSeferleriQuery or SevkSeferiDetayQuery or SevkiyatIstatistikQuery or SevkiyatDonemRaporuQuery or SevkSeferiRaporuQuery
                => One(K.Proje.Sevkiyatlar, YetkiTipi.R),
            StokKaydiGuncelleCommand or StokKaydiOlusturCommand => One(K.Stok.Kok, YetkiTipi.W),
            DepoLokasyonOlusturCommand or DepoLokasyonSilCommand => One(K.DepoDurumu, YetkiTipi.W),
            KaliteDurumGuncelleCommand => One(K.Sandik.KaliteModulu, YetkiTipi.W),
            SurecDurumGuncelleCommand => One(K.Sandik.SurecModulu, YetkiTipi.W),
            CekiSatiriAnaVeriGuncelleCommand => One(K.Sandik.CekiVerisiDuzenle, YetkiTipi.W),
            CekiSatirlariSilCommand => One(K.Sandik.CekiVerisiSil, YetkiTipi.W),
            GetDepoSandikPdfQuery or GetProjeDepoSandikPdfQuery => One(K.DepoDurumu, YetkiTipi.R),
            // Geçersiz seçimde istek kapsamının izniyle validator'a ulaşılır; 400 mesajı korunur.
            GetTopluEksikUrunlerRaporuQuery x when !GecerliProjeSecimi(x.ProjeIds) =>
                ProjectPolicy((int)x.ProjeTipi, ProjectMenuOperation.MissingItemsReport, YetkiTipi.R),
            GetTopluTalepFormuQuery x when !GecerliProjeSecimi(x.ProjeIds) =>
                ProjectPolicy((int)x.ProjeTipi, ProjectMenuOperation.Grid, YetkiTipi.R),
            ProjeOlusturCommand x => ProjectPolicy(x.ProjeTipiId, ProjectMenuOperation.ProjectCreate, YetkiTipi.W),
            ProjeListeleQuery x => ProjectListPolicy(x.ProjeTipiId, x.IsSevkEdilen),
            // Sadece seçim için asgari proje bilgisi dönen ortak lookup; operasyon izni vermez.
            ProjeDropdownQuery => Any(YetkiTipi.R, K.Proje.AktifProjeler, K.Proje.SevkEdilen, K.Sandik.Yonetimi,
                K.Saha.Yonetimi, K.Yedek.Yonetimi, K.Sandik.GridModulu, K.Sandik.UcKModulu, K.Saha.GridModulu,
                K.Saha.UcKModulu, K.Yedek.GridModulu, K.Yedek.UcKModulu, K.Saha.SahayaAktar,
                K.DepoDurumu, K.Stok.Kok, K.HareketGecmisi, K.Sandik.GridIsListesi, K.Sandik.UcKIsListesi),
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
        GetSandikDuzeltmeDurumuQuery x => new(ProjectMenuOperation.CrateRead, YetkiTipi.R, SandikIds: [x.SandikId]),
        GetSandikDurumGecmisiQuery x => new(ProjectMenuOperation.CrateRead, YetkiTipi.R, SandikIds: [x.SandikId]),
        GetProjeSevkiyatlariQuery x => new(ProjectMenuOperation.ProjectRead, YetkiTipi.R, x.ProjeId),
        GetSahaProjePdfQuery x => new(ProjectMenuOperation.CrateReport, YetkiTipi.R, x.ProjeId),
        GetSevkeHazirSandikRaporuQuery x => new(ProjectMenuOperation.CrateReport, YetkiTipi.R, x.ProjeId),
        GetGerceklesenCekiListesiPdfQuery x => new(ProjectMenuOperation.ActualPackingReport, YetkiTipi.R, x.ProjeId),
        GetGerceklesenCekiListesiExcelQuery x => new(ProjectMenuOperation.ActualPackingReport, YetkiTipi.R, x.ProjeId),
        GetSahaGerceklesenCekiListesiPdfQuery x => new(ProjectMenuOperation.ActualPackingReport, YetkiTipi.R, x.ProjeId),
        GetSahaGerceklesenCekiListesiExcelQuery x => new(ProjectMenuOperation.ActualPackingReport, YetkiTipi.R, x.ProjeId),
        GetMusteriCekiListesiQuery x => new(ProjectMenuOperation.ActualPackingReport, YetkiTipi.R, x.ProjeId),
        GetUcKSandikDurumPdfQuery x => new(ProjectMenuOperation.UcKCrateReport, YetkiTipi.R, x.ProjeId),
        GetTopluEksikUrunlerRaporuQuery x => new(ProjectMenuOperation.MissingItemsReport, YetkiTipi.R,
            ProjectIds: x.ProjeIds),
        // G4: talep formu Grid verisidir; her proje kendi gerçek tipinin Grid okuma izniyle açılır.
        GetTopluTalepFormuQuery x => new(ProjectMenuOperation.Grid, YetkiTipi.R, ProjectIds: x.ProjeIds),
        ProjeSevkEtCommand x => new(ProjectMenuOperation.Shipment, YetkiTipi.W, x.ProjeId),
        ProjeKilidiAcCommand x => new(ProjectMenuOperation.ProjectUnlock, YetkiTipi.W, x.ProjeId,
            SevkSonrasiDuzeltme: x.KilitAcmaTipiId == (int)SevkiyatKilitAcmaTipi.SevkiyatKaydiKorunarakAc),
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
        SandikIcerikBilgisiGuncelleCommand x => new(ProjectMenuOperation.ProductInfoWrite, YetkiTipi.W, x.ProjeId,
            [x.SandikId], IcerikIds: [x.SandikIcerikId]),
        SandikKapat x => new(ProjectMenuOperation.CrateWrite, YetkiTipi.W, SandikIds: [x.SandikId]),
        EskiSandikKapat x => new(ProjectMenuOperation.CrateWrite, YetkiTipi.W, SandikIds: [x.SandikId]),
        TopluSandikKapatCommand x => new(ProjectMenuOperation.CrateWrite, YetkiTipi.W, SandikIds: x.SandikIds),
        SandikSilCommand x => new(ProjectMenuOperation.CrateWrite, YetkiTipi.W, x.ProjeId, [x.SandikId]),
        SandikKilidiAcCommand x => new(ProjectMenuOperation.CrateWrite, YetkiTipi.W, x.ProjeId, [x.SandikId],
            SevkSonrasiDuzeltme: x.KilitAcmaTipiId == (int)SevkiyatKilitAcmaTipi.SevkiyatKaydiKorunarakAc),
        SandikSevkiyatDuzeltmeTamamlaCommand x => new(ProjectMenuOperation.CrateWrite, YetkiTipi.W, x.ProjeId, [x.SandikId]),
        SandikSevkiyatDuzeltmeTopluTamamlaCommand x => new(ProjectMenuOperation.CrateWrite, YetkiTipi.W, x.ProjeId),
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
        if (spec.ProjectIds != null)
        {
            if (spec.ProjectIds.Any(x => x <= 0)) return RequestMenuPermissions.Denied;
            projects.UnionWith(spec.ProjectIds);
        }

        var sandikQuery = unitOfWork.GetRepository<Sandik>().Queryable();
        var cekiQuery = unitOfWork.GetRepository<Ceki>().Queryable();
        var satirQuery = unitOfWork.GetRepository<CekiSatiri>().Queryable();
        var icerikQuery = unitOfWork.GetRepository<SandikIcerik>().Queryable();
        if (!await AddProjectsAsync(spec.SandikIds, sandikQuery.Select(x => new Relation { Id = x.Id, ProjectId = x.ProjeId }), projects, cancellationToken) ||
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
        // Sevk sonrası düzeltme (sevkiyat kaydı korunarak açma) proje tipinin izniyle birlikte ayrı, dar düzeltme
        // iznini de ister.
        if (spec.SevkSonrasiDuzeltme)
            groups.AddRange(One(K.Sandik.SevkSonrasiDuzeltme, YetkiTipi.W).Groups);
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
        if (shipped == true) return One(K.Proje.SevkEdilen, YetkiTipi.R);
        if (type.HasValue) return ProjectPolicy(type.Value, ProjectMenuOperation.ProjectRead, YetkiTipi.R);
        return Any(YetkiTipi.R, K.Proje.AktifProjeler, K.Sandik.Yonetimi, K.Proje.SevkEdilen, K.DepoDurumu);
    }

    /// <summary>Gerçek proje tipinin ilgili işlem için gerektirdiği menü kodları.</summary>
    public static RequestMenuPermissions ProjectPolicy(int type, ProjectMenuOperation operation, YetkiTipi yetki)
    {
        if (ProjeTipiYetkiKodlari.Getir(type) is not { } p) return RequestMenuPermissions.Denied;
        var normal = p.Tip == ProjeTipi.Normal;
        return operation switch
        {
            ProjectMenuOperation.Grid => One(p.GridModulu, yetki),
            ProjectMenuOperation.UcK => One(p.UcKModulu, yetki),
            ProjectMenuOperation.CrateWrite => Any(yetki, p.Sandiklar, p.UcKModulu),
            // Ürün bilgisi (seri no, SKT, lot) Grid ve 3K ekranlarında aynı alandır. Grid yazma izni yalnız bu
            // işlemi açar; sandık yazmanın geri kalanı (CrateWrite) Grid iznine bağlanmaz.
            ProjectMenuOperation.ProductInfoWrite => Any(yetki, p.Sandiklar, p.UcKModulu, p.GridModulu),
            ProjectMenuOperation.CrateRead => Any(yetki, p.Sandiklar, p.ProjeYonetimi, p.UcKModulu, p.GridModulu,
                K.DepoDurumu, K.Saha.SahayaAktar),
            ProjectMenuOperation.ProjectRead => normal
                ? Any(yetki, K.Proje.AktifProjeler, K.Sandik.Yonetimi, K.Proje.SevkEdilen, K.DepoDurumu,
                    p.GridModulu, p.UcKModulu, K.Saha.SahayaAktar)
                : Any(yetki, p.ProjeYonetimi, p.Sandiklar, p.GridModulu, p.UcKModulu, K.Proje.SevkEdilen,
                    K.DepoDurumu, K.Saha.SahayaAktar),
            ProjectMenuOperation.ProjectCreate => normal
                ? Any(yetki, K.Proje.AktifProjeler, K.Sandik.Yonetimi)
                : One(p.ProjeYonetimi, yetki),
            ProjectMenuOperation.RevisionHistory => Any(yetki, p.ProjeYonetimi, K.Sandik.CekiRevizyonYukle, K.Proje.SevkEdilen),
            ProjectMenuOperation.ProjectUnlock => Any(yetki, p.ProjeYonetimi, K.Proje.SevkEdilen),
            ProjectMenuOperation.Shipment => One(p.SevkEt, yetki),
            ProjectMenuOperation.ProjectDelete => One(p.ProjeSil, yetki),
            ProjectMenuOperation.PlannedShipmentDate => One(p.PlanlananSevkTarihi, yetki),
            ProjectMenuOperation.ActualPackingReport => One(p.GerceklesenCekiRaporu, yetki),
            ProjectMenuOperation.UcKCrateReport => One(p.UcKSandikDurumRaporu, yetki),
            ProjectMenuOperation.CrateReport => p.SandikRaporu is { } rapor
                ? One(rapor, yetki)
                : Any(yetki, K.Sandik.Yonetimi, K.Proje.AktifProjeler),
            ProjectMenuOperation.MissingItemsReport => One(p.EksikRaporu, yetki),
            _ => RequestMenuPermissions.Denied
        };
    }

    private static bool GecerliProjeSecimi(IReadOnlyCollection<int>? ids) =>
        ids is { Count: > 0 and <= 25 } && ids.All(x => x > 0) && ids.Distinct().Count() == ids.Count;

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
        IReadOnlyCollection<int>? IcerikIds = null, IReadOnlyCollection<int>? ProjectIds = null,
        bool SevkSonrasiDuzeltme = false);
}
