namespace _3K.Application.Common
{
    /// <summary>
    /// Marks a request for authorization pipeline checks.
    /// Permission decisions come from RolYetkileri for the active menu context.
    /// </summary>
    public interface ISecuredRequest { }

    /// <summary>
    /// Marks a request that can only be executed by the Admin role.
    /// </summary>
    public interface IAdminOnlyRequest : ISecuredRequest { }
}
