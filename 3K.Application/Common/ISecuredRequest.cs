namespace _3K.Application.Common
{
    /// <summary>
    /// Bu interface'i implemente eden Command/Query'ler,
    /// AuthorizationBehavior tarafından otomatik rol kontrolüne tabi tutulur.
    /// </summary>
    public interface ISecuredRequest
    {
        string[] RequiredRoles { get; }
    }
}
