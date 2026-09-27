using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using _3K.Core.Interfaces;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Data.Interceptors;
using _3K.Infrastructure.Repositories;
using _3K.Infrastructure.Services;

namespace _3K.Infrastructure;

/// <summary>Kalıcılık, dış servis ve arka plan iş kayıtlarını tek yerde toplar.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.AddDomainServices();
        services.AddBackgroundJobs();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<ISseNotifier, SseNotifier>();
        services.AddSingleton(TimeProvider.System);
        services.AddMemoryCache();
        services.AddScoped<ILookupCacheService, LookupCacheService>();

        return services;
    }

    private static void AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<AuditInterceptor>();
        services.AddScoped<ProjectLockInterceptor>();
        services.AddDbContext<AppDbContext>((sp, options) =>
            // Şema EF migration ile değil, scripts/database altındaki PostgreSQL betikleriyle yönetilir.
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
                   .AddInterceptors(
                       sp.GetRequiredService<AuditInterceptor>(),
                       sp.GetRequiredService<ProjectLockInterceptor>()));

        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IReadQueryExecutor, EntityFrameworkReadQueryExecutor>();
        services.AddScoped<IProjeRepository, ProjeRepository>();
        services.AddScoped<IDashboardSandikQueryRepository, DashboardSandikQueryRepository>();
        services.AddScoped<IBildirimRepository, BildirimRepository>();
        services.AddScoped<IOnayIslemRepository, OnayIslemRepository>();
        services.AddScoped<IAmbalajKaynakSenkronizasyonKuyrugu, PostgreSqlAmbalajKaynakSenkronizasyonKuyrugu>();
        services.AddScoped<IDatabaseHealthProbe, DatabaseHealthProbe>();
    }

    private static void AddDomainServices(this IServiceCollection services)
    {
        services.AddScoped<ICekiService, CekiService>();
        services.AddScoped<ICekiRevizyonGecmisiService, CekiRevizyonGecmisiService>();
        services.AddScoped<IYedekCekiImportService, YedekCekiImportService>();
        services.AddScoped<ISandikService, SandikService>();
        services.AddScoped<IUrunService, UrunService>();
        services.AddScoped<IStokService, StokService>();
        services.AddScoped<IHareketService, HareketService>();
        services.AddScoped<IArsivService, ArsivService>();
        services.AddScoped<IPdfService, PdfService>();
        services.AddScoped<IMailService, MailService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IIkiFaktorService, IkiFaktorService>();
        services.AddScoped<IDurumHesaplaService, DurumHesaplaService>();
        services.AddScoped<ILookupService, LookupService>();
        services.AddScoped<IRolService, RolService>();
        services.AddScoped<IAlanErisimService, AlanErisimService>();
        services.AddScoped<IAmbalajFormIslemKilidi, AmbalajFormIslemKilidi>();
        services.AddScoped<IKullaniciYetkiService, KullaniciYetkiService>();
        services.AddScoped<IOnayYetkiService, OnayYetkiService>();
        services.AddScoped<IProjectLockService, ProjectLockService>();
        services.AddScoped<IDashboardStatsProvider, DashboardStatsProvider>();
        services.AddScoped<ISahaTamamlamaService, SahaTamamlamaService>();
        services.AddScoped<ISahaAktarimSilmeKorumaService, SahaAktarimSilmeKorumaService>();
        services.AddScoped<IProjeSilmeService, ProjeSilmeService>();
        services.AddScoped<IBildirimService, BildirimService>();
        services.AddScoped<IAmbalajRaporDosyaService, AmbalajRaporDosyaService>();

        services.AddScoped<FinansService>();
        services.AddScoped<IFinansService>(sp => sp.GetRequiredService<FinansService>());
        services.AddScoped<IFinansUretimAktarimService>(sp => sp.GetRequiredService<FinansService>());
        services.AddScoped<IFinansRaporService, FinansRaporService>();
    }

    private static void AddBackgroundJobs(this IServiceCollection services)
    {
        services.AddHostedService<ArsivBackgroundService>();
        services.AddHostedService<CekiRevizyonTalebiTemizlemeBackgroundService>();
        services.AddHostedService<FinansDuzenliIsBackgroundService>();
    }
}
