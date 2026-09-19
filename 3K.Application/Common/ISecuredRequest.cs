namespace _3K.Application.Common
{
    using _3K.Core.Enums;

    /// <summary>
    /// Marks a request for authorization pipeline checks.
    /// Permission decisions come from RolYetkileri and server-defined operations.
    /// </summary>
    public interface ISecuredRequest { }

    /// <summary>
    /// Secures a request with a fixed menu code instead of the active UI context.
    /// Use this for action endpoints that must not be authorized by another menu header.
    /// </summary>
    public interface IRequiresMenuPermission
    {
        string RequiredMenuKod { get; }
    }

    /// <summary>
    /// Bir isteğin birden fazla işlem yetkisini birlikte gerektirdiği durumlarda
    /// kullanılır. Varsayılan AND'dir; ortak ekranlar için Any açıkça belirtilir.
    /// Ayrı gruplar her zaman birlikte sağlanmalıdır.
    /// </summary>
    public interface IRequiresMenuPermissions
    {
        IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions { get; }
        MenuPermissionMatch PermissionMatch => MenuPermissionMatch.All;
    }

    public sealed record MenuPermissionRequirement(string MenuKod, YetkiTipi YetkiTipi);

    public enum MenuPermissionMatch { All, Any }

    /// <summary>Proje tipi istemciden değil, yetki çözümleyicisinde veritabanından okunur.</summary>
    public interface IRequiresProjectMenuPermission
    {
        int PermissionProjectId { get; }
        ProjectMenuOperation PermissionOperation { get; }
        YetkiTipi RequiredYetkiTipi { get; }
    }

    /// <summary>Toplu işlemlerde her gerçek sandığın proje bağlamı doğrulanır.</summary>
    public interface IRequiresSandikMenuPermission
    {
        IReadOnlyCollection<int> PermissionSandikIds { get; }
        ProjectMenuOperation PermissionOperation { get; }
        YetkiTipi RequiredYetkiTipi { get; }
    }

    public enum ProjectMenuOperation
    {
        ProjectRead, CrateRead, CrateWrite, Grid, UcK, Shipment, RevisionHistory,
        ProjectDelete, PlannedShipmentDate, CrateReport, ActualPackingReport, UcKCrateReport, ProjectUnlock, ProjectCreate
    }

    public sealed record MenuPermissionGroup(
        IReadOnlyCollection<MenuPermissionRequirement> Requirements,
        MenuPermissionMatch Match = MenuPermissionMatch.All);

    /// <summary>Gruplar AND, bir grubun içindeki koşullar açık All/Any semantiğiyle değerlendirilir.</summary>
    public sealed record RequestMenuPermissions(IReadOnlyCollection<MenuPermissionGroup> Groups)
    {
        public static RequestMenuPermissions Denied { get; } = new([]);
    }

    public interface IRequestMenuPermissionResolver
    {
        Task<RequestMenuPermissions> ResolveAsync(object request, CancellationToken cancellationToken);
    }

}
