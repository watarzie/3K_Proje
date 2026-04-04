namespace _3K.Core.Interfaces
{
    /// <summary>
    /// Mevcut HTTP isteğindeki kullanıcı bilgilerini soyutlar.
    /// JWT Token'dan UserId ve Roller çekilir.
    /// </summary>
    public interface ICurrentUserService
    {
        int? UserId { get; }
        string[] Roles { get; }
        bool IsAuthenticated { get; }
    }
}
