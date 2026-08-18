using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;
using _3K.Core.Entities;
using _3K.Infrastructure.Services;

namespace _3K.Application.Tests;

public sealed class JwtIkiFaktorClaimTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AccessToken_IkiFaktorKanitiniAmrVeMfaClaimlerineYazar(
        bool ikiFaktorDogrulandi)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"] = "test-only-secret-key-that-is-at-least-32-bytes-long",
                ["JwtSettings:Issuer"] = "test-issuer",
                ["JwtSettings:Audience"] = "test-audience",
                ["JwtSettings:ExpirationHours"] = "1"
            })
            .Build();
        var service = new AuthService(null!, null!, configuration);
        var rol = new Rol { Id = 1, Ad = "Admin" };
        var kullanici = new Kullanici
        {
            Id = 9,
            AdSoyad = "Test Kullanıcı",
            Email = "test@example.com",
            RolId = rol.Id,
            Rol = rol
        };

        var token = service.GenerateAccessToken(kullanici, ikiFaktorDogrulandi);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var amr = jwt.Claims.Where(x => x.Type == "amr").Select(x => x.Value).ToList();

        Assert.Contains("pwd", amr);
        Assert.Equal(ikiFaktorDogrulandi, amr.Contains("otp"));
        Assert.Equal(
            ikiFaktorDogrulandi ? "true" : "false",
            jwt.Claims.Single(x => x.Type == "mfa").Value);
        Assert.NotNull(jwt.Claims.SingleOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti));
        Assert.NotNull(jwt.Claims.SingleOrDefault(x => x.Type == "auth_time"));
    }
}
