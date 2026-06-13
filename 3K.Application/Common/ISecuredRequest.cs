namespace _3K.Application.Common
{
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

}
