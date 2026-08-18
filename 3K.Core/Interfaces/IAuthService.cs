using _3K.Core.Entities;

namespace _3K.Core.Interfaces
{
    /// <summary>
    /// JWT Authentication servisi
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// E-posta/parola çiftini doğrular. Bu metot hiçbir zaman JWT üretmez;
        /// tam erişim token'ı yalnızca gerekli tüm giriş adımları tamamlandıktan
        /// sonra <see cref="GenerateAccessToken"/> ile oluşturulur.
        /// </summary>
        Task<Kullanici?> ValidateCredentialsAsync(
            string email,
            string sifre,
            CancellationToken cancellationToken = default);

        Task<Kullanici?> GetKullaniciByIdAsync(
            int kullaniciId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Tam erişim JWT'si üretir. <paramref name="ikiFaktorDogrulandi"/>
        /// değeri token'daki amr/mfa kanıtına yazılır.
        /// </summary>
        string GenerateAccessToken(Kullanici kullanici, bool ikiFaktorDogrulandi);

        Task<Kullanici> RegisterAsync(string adSoyad, string email, string sifre, int rolId);
        Task<Kullanici?> GetKullaniciByEmailAsync(string email);
        string GenerateBasHarf(string adSoyad);
        /// <summary>Şifreyi hash'ler (BCrypt).</summary>
        string HashPassword(string plainPassword);

        /// <summary>
        /// Mevcut geçerli JWT'den yeni bir token üretir (silent refresh).
        /// Token süresi dolmamışsa kullanıcı bilgilerini koruyarak yeni token döner.
        /// </summary>
        Task<string> RefreshTokenAsync(
            int userId,
            bool ikiFaktorDogrulandi,
            CancellationToken cancellationToken = default);
    }
}
