namespace _3K.Core.Interfaces
{
    /// <summary>
    /// Mevcut isteğin doğrulanmış kullanıcısı. Kimlik yalnız JWT'den okunur; istemci
    /// başlıkları (ör. X-Menu-Kod) yetki veya kimlik kaynağı değildir.
    /// </summary>
    public interface ICurrentUserService
    {
        int? UserId { get; }

        /// <summary>İşlemi kaydeden kullanıcı; sistem işlerinde servis kullanıcısıdır.</summary>
        int? IslemKullaniciId => UserId;

        bool IsAuthenticated { get; }
    }
}
