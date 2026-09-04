namespace _3K.Application.Common
{
    using _3K.Core.Enums;

    /// <summary>
    /// Marks a request for authorization pipeline checks.
    /// Permission decisions come from RolYetkileri for the active menu context.
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
    /// kullanılır. AuthorizationBehavior tüm maddeleri AND mantığıyla doğrular.
    /// Mevcut tek menü kodlu isteklerle geriye dönük uyumludur.
    /// </summary>
    public interface IRequiresMenuPermissions
    {
        IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions { get; }
    }

    public sealed record MenuPermissionRequirement(string MenuKod, YetkiTipi YetkiTipi);

}
