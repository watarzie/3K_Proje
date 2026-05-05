namespace _3K.Application.Common
{
    /// <summary>
    /// Bu interface'i implemente eden Command/Query'ler,
    /// AuthorizationBehavior tarafından otomatik rol kontrolüne tabi tutulur.
    /// RequiredMenuKod belirtilirse, menü bazlı yetki kontrolü de yapılır.
    /// </summary>
    public interface ISecuredRequest
    {
        string[] RequiredRoles { get; }

        /// <summary>
        /// Opsiyonel: Menü kodu bazlı yetki kontrolü.
        /// Belirtilirse kullanıcının bu menü koduna W yetkisi olup olmadığı kontrol edilir.
        /// </summary>
        string? RequiredMenuKod => null;
    }
}
