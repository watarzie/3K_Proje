using _3K.Core.Entities;

namespace _3K.Core.Interfaces
{
    /// <summary>
    /// JWT Authentication servisi
    /// </summary>
    public interface IAuthService
    {
        Task<string> LoginAsync(string email, string sifre);
        Task<Kullanici> RegisterAsync(string adSoyad, string email, string sifre, string rol);
        Task<Kullanici?> GetKullaniciByEmailAsync(string email);
        string GenerateBasHarf(string adSoyad);
    }
}
