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

        public async Task<Kullanici?> ValidateCredentialsAsync(
            string email,
            string sifre,
            CancellationToken cancellationToken = default)
        {
            var kullanici = await _context.Kullanicilar
                .AsNoTracking()
                .Include(k => k.Rol)
                .FirstOrDefaultAsync(k => k.Email == email.Trim(), cancellationToken);

            if (kullanici == null || !BCrypt.Net.BCrypt.Verify(sifre, kullanici.SifreHash))
                return null;

            return kullanici;
        }

        public async Task<Kullanici?> GetKullaniciByIdAsync(
            int kullaniciId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Kullanicilar
                .AsNoTracking()
                .Include(k => k.Rol)
                .FirstOrDefaultAsync(k => k.Id == kullaniciId, cancellationToken);
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

        public string HashPassword(string plainPassword)
        {
            return BCrypt.Net.BCrypt.HashPassword(plainPassword);
        }

        public string GenerateAccessToken(Kullanici kullanici, bool ikiFaktorDogrulandi)
        {
            return GenerateJwtToken(kullanici, ikiFaktorDogrulandi);
        }

        public async Task<string> RefreshTokenAsync(
            int userId,
            bool ikiFaktorDogrulandi,
            CancellationToken cancellationToken = default)
        {
            var kullanici = await GetKullaniciByIdAsync(userId, cancellationToken)
                ?? throw new UnauthorizedAccessException("Kullanıcı bulunamadı.");

            if (kullanici.IkiFaktorZorunluMu)
            {
                var etkinAyarVar = await _context.KullaniciIkiFaktorAyarlari
                    .AsNoTracking()
                    .AnyAsync(
                        x => x.KullaniciId == userId && x.EtkinMi,
                        cancellationToken);

                if (!ikiFaktorDogrulandi || !etkinAyarVar)
                {
                    throw new UnauthorizedAccessException(
                        "İki faktörlü doğrulama tamamlanmadan oturum yenilenemez.");
                }
            }

            return GenerateJwtToken(kullanici, ikiFaktorDogrulandi);
        }

        private string GenerateJwtToken(Kullanici kullanici, bool ikiFaktorDogrulandi)
        {
            var jwtKey = _configuration["JwtSettings:SecretKey"]
                ?? Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
                ?? throw new InvalidOperationException("JWT SecretKey yapılandırılmamış.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var now = DateTimeOffset.UtcNow;
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, kullanici.Id.ToString()),
                new Claim(ClaimTypes.Name, kullanici.AdSoyad ?? ""),
                new Claim(ClaimTypes.Email, kullanici.Email ?? ""),
                // AuthorizationBehavior bu claim'i okuyarak rol kontrolü yapar
                new Claim(ClaimTypes.Role, kullanici.Rol?.Ad ?? "Unknown"),
                new Claim("RolId", kullanici.RolId.ToString()),
                new Claim("BasHarf", kullanici.BasHarf ?? ""),
                new Claim("KullaniciId", kullanici.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
                new Claim(
                    "auth_time",
                    now.ToUnixTimeSeconds().ToString(),
                    ClaimValueTypes.Integer64),
                new Claim("amr", "pwd"),
                new Claim("mfa", ikiFaktorDogrulandi ? "true" : "false", ClaimValueTypes.Boolean)
            };

            if (ikiFaktorDogrulandi)
                claims.Add(new Claim("amr", "otp"));

            var expirationHours = _configuration.GetValue<double?>("JwtSettings:ExpirationHours") ?? 8;
            if (expirationHours <= 0)
                expirationHours = 8;

            var tokenDescriptor = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"] ?? "3K_API",
                audience: _configuration["JwtSettings:Audience"] ?? "3K_Client",
                claims: claims,
                notBefore: now.UtcDateTime,
                expires: now.AddHours(expirationHours).UtcDateTime,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
        }
    }
}
