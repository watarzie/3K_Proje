using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using _3K.Core.Interfaces;
using _3K_API.Extensions;

namespace _3K.Application.Tests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void Uygulama_servisleri_eksiksiz_ve_yasam_suresi_uyumlu_kayitlidir()
    {
        // Development ortamında Build() ValidateOnBuild + ValidateScopes uygular. Host başlatılmaz;
        // arka plan işleri çalışmaz ve veritabanına bağlanılmaz.
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Development });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Host=127.0.0.1;Database=di_validation;Username=unused;Password=unused",
            ["JwtSettings:SecretKey"] = new string('k', 64)
        });
        builder.Services.AddApi(builder.Configuration, builder.Environment);

        using var app = builder.Build();
        using var scope = app.Services.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IUnitOfWork>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<MediatR.IMediator>());
    }
}
