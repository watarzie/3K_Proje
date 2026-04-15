using _3K.Core.Entities;
using _3K.Core.Interfaces;
using _3K.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace _3K.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(IUnitOfWork unitOfWork, AppDbContext context, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _context = context;
            _configuration = configuration;
        }

        public async Task<string> LoginAsync(string email, string sifre)
        {
            var kullanici = await _context.Kullanicilar
                .Include(k => k.Rol)
                .FirstOrDefaultAsync(k => k.Email == email);

            if (kullanici == null)
                throw new UnauthorizedAccessException("Geçersiz email veya şifre.");

            if (!BCrypt.Net.BCrypt.Verify(sifre, kullanici.SifreHash))
                throw new UnauthorizedAccessException("Geçersiz email veya şifre.");

            return GenerateJwtToken(kullanici);
        }

        public async Task<Kullanici> RegisterAsync(string adSoyad, string email, string sifre, int rolId)
        {
            // Email benzersizlik kontrolü
            var mevcut = await _context.Kullanicilar.AnyAsync(k => k.Email == email);
            if (mevcut)
                throw new InvalidOperationException("Bu email adresi zaten kayıtlı.");

            var kullaniciRepo = _unitOfWork.GetRepository<Kullanici>();

            var kullanici = new Kullanici
            {
                AdSoyad = adSoyad,
                BasHarf = GenerateBasHarf(adSoyad),
                Email = email,
                SifreHash = BCrypt.Net.BCrypt.HashPassword(sifre),
                RolId = rolId
            };

            await kullaniciRepo.AddAsync(kullanici);
            await _unitOfWork.SaveChangesAsync();

            // Rol navigation'ı yükle (JWT claim için)
            await _context.Entry(kullanici).Reference(k => k.Rol).LoadAsync();
            return kullanici;
        }

        public async Task<Kullanici?> GetKullaniciByEmailAsync(string email)
        {
            return await _context.Kullanicilar
                .Include(k => k.Rol)
                .FirstOrDefaultAsync(k => k.Email == email);
        }

        /// <summary>
        /// İş akışı 7: Baş harf otomatik üretimi
        /// Örnek: Hakan Kaya → HK, Selim Korkmaz → SK
        /// </summary>
        public string GenerateBasHarf(string adSoyad)
        {
            if (string.IsNullOrWhiteSpace(adSoyad)) return "";

            var parcalar = adSoyad.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return string.Join("", parcalar.Select(p => char.ToUpper(p[0])));
        }

        private string GenerateJwtToken(Kullanici kullanici)
        {
            var jwtKey = _configuration["JwtSettings:SecretKey"]
                ?? Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
                ?? throw new InvalidOperationException("JWT SecretKey yapılandırılmamış.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, kullanici.Id.ToString()),
                new Claim(ClaimTypes.Name, kullanici.AdSoyad ?? ""),
                new Claim(ClaimTypes.Email, kullanici.Email ?? ""),
                // AuthorizationBehavior bu claim'i okuyarak rol kontrolü yapar
                new Claim(ClaimTypes.Role, kullanici.Rol?.Ad ?? "Unknown"),
                new Claim("RolId", kullanici.RolId.ToString()),
                new Claim("BasHarf", kullanici.BasHarf ?? ""),
                new Claim("KullaniciId", kullanici.Id.ToString())
            };

            var tokenDescriptor = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"] ?? "3K_API",
                audience: _configuration["JwtSettings:Audience"] ?? "3K_Client",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
        }
    }
}
