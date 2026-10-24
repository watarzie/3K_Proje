using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using _3K.Core.Interfaces;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Data.Interceptors;
using _3K.Infrastructure.Repositories;
using _3K.Infrastructure.Services;
using _3K.Infrastructure.Services.Cekiler;
using _3K.Infrastructure.Services.Finans;
using _3K.Infrastructure.Services.Raporlar;

namespace _3K.Infrastructure;

/// <summary>Kalıcılık, dış servis ve arka plan iş kayıtlarını tek yerde toplar.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.AddDomainServices();
        services.AddFinansServices();
        services.AddRaporServices();
        services.AddCekiServices();
        services.AddBackgroundJobs();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<ITakipKoduSaglayici, TakipKoduSaglayici>();
        services.AddSingleton<ISseNotifier, SseNotifier>();
        services.AddSingleton(TimeProvider.System);
        services.AddMemoryCache();
        services.AddScoped<ILookupCacheService, LookupCacheService>();

        return services;
    }

    private static void AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<SandikDurumOlayInterceptor>();
        services.AddScoped<AuditInterceptor>();
        services.AddScoped<ProjectLockInterceptor>();
        services.AddDbContext<AppDbContext>((sp, options) =>
            // Şema EF migration ile değil, scripts/database altındaki PostgreSQL betikleriyle yönetilir.
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
                   .AddInterceptors(
                       // Olay interceptor'ı önce çalışır: eklediği olay kaydı denetim alanlarını da alır.
                       sp.GetRequiredService<SandikDurumOlayInterceptor>(),
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
        services.AddScoped<ICekiRevizyonGecmisiService, CekiRevizyonGecmisiService>();
        services.AddScoped<IYedekCekiImportService, YedekCekiImportService>();
        services.AddScoped<ISandikService, SandikService>();
        services.AddScoped<IUrunService, UrunService>();
        services.AddScoped<IStokService, StokService>();
        services.AddScoped<IHareketService, HareketService>();
        services.AddScoped<IGridHareketOkuyucu, GridHareketOkuyucu>();
        services.AddScoped<IArsivService, ArsivService>();
        services.AddScoped<IMailService, MailService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IIkiFaktorService, IkiFaktorService>();
        services.AddScoped<IDurumHesaplaService, DurumHesaplaService>();
        services.AddScoped<ILookupService, LookupService>();
        services.AddScoped<IRolService, RolService>();
        services.AddScoped<IAmbalajFormIslemKilidi, AmbalajFormIslemKilidi>();
        services.AddScoped<IRolAtamaService, RolAtamaService>();
        services.AddScoped<IOnayYetkiService, OnayYetkiService>();
        services.AddScoped<IProjectLockService, ProjectLockService>();
        services.AddScoped<IDashboardStatsProvider, DashboardStatsProvider>();
        services.AddScoped<ISahaTamamlamaService, SahaTamamlamaService>();
        services.AddScoped<ISahaAktarimSilmeKorumaService, SahaAktarimSilmeKorumaService>();
        services.AddScoped<IProjeSilmeService, ProjeSilmeService>();
        services.AddScoped<IBildirimService, BildirimService>();
        services.AddScoped<IAmbalajRaporDosyaService, AmbalajRaporDosyaService>();
    }

    /// <summary>
    /// Finans alan servisleri. Transaction, denetim ve fiyat bileşenleri istek kapsamındadır; aynı
    /// istekteki servisler tek transaction ve tek denetim işlem grubunu paylaşır.
    /// </summary>
    internal static IServiceCollection AddFinansServices(this IServiceCollection services)
    {
        services.AddScoped<FinansIslemYurutucu>();
        services.AddScoped<FinansDenetimKaydi>();
        services.AddScoped<FinansFiyatBelirleyici>();
        services.AddScoped<FinansPoOnayOkuyucu>();

        services.AddScoped<IFinansPanelService, FinansPanelService>();
        services.AddScoped<IFinansIsKaydiService, FinansIsKaydiService>();
        services.AddScoped<IFinansAylikOzetService, FinansAylikOzetService>();
        services.AddScoped<IFinansSiparisFaturaService, FinansSiparisFaturaService>();
        services.AddScoped<IFinansGiderService, FinansGiderService>();
        services.AddScoped<IFinansGiderTanimService, FinansGiderTanimService>();
        services.AddScoped<IFinansUrunService, FinansUrunService>();
        services.AddScoped<IFinansParametreService, FinansParametreService>();
        services.AddScoped<IFinansDuzenliIsService, FinansDuzenliIsService>();
        services.AddScoped<IFinansBelgeService, FinansBelgeService>();
        services.AddScoped<IFinansKaliciSilmeService, FinansKaliciSilmeService>();
        services.AddScoped<IFinansRaporVerisiService, FinansRaporVerisiService>();
        services.AddScoped<IFinansUretimAktarimService, FinansUretimAktarimService>();
        services.AddScoped<IFinansRaporService, FinansRaporService>();
        services.AddScoped<IFinansPoOnayService, FinansPoOnayService>();
        return services;
    }

    /// <summary>
    /// Çeki yükleme, revizyon (ön izleme, onaya sunma, uygulama) ve çeki satırı sorgusu. Revizyon
    /// bileşenleri aynı istek kapsamındaki bağlamı ve transaction'ı paylaşır.
    /// </summary>
    internal static IServiceCollection AddCekiServices(this IServiceCollection services)
    {
        services.AddScoped<CekiRevizyonSorgulari>();
        services.AddScoped<CekiRevizyonOnizleyici>();
        services.AddScoped<CekiRevizyonUygulayici>();
        services.AddScoped<ICekiYuklemeService, CekiYuklemeService>();
        services.AddScoped<ICekiRevizyonService, CekiRevizyonService>();
        services.AddScoped<ICekiSorguService, CekiSorguService>();
        return services;
    }

    /// <summary>Operasyon raporları; her rapor ailesi kendi arayüzünün arkasındadır.</summary>
    private static void AddRaporServices(this IServiceCollection services)
    {
        services.AddScoped<IBarkodUrunAramaService, BarkodUrunAramaService>();
        services.AddScoped<ISandikDurumIstatistikService, SandikDurumIstatistikService>();
        services.AddScoped<IDepoSandikRaporService, DepoSandikRaporService>();
        services.AddScoped<IEksikUrunlerRaporService, EksikUrunlerRaporService>();
        services.AddScoped<IGerceklesenCekiListesiRaporService, GerceklesenCekiListesiRaporService>();
        services.AddScoped<ISahaSandikRaporService, SahaSandikRaporService>();
        services.AddScoped<ISevkiyatRaporDosyaService, SevkiyatRaporDosyaService>();
        services.AddScoped<IStokRaporService, StokRaporService>();
        services.AddScoped<ITalepFormuRaporService, TalepFormuRaporService>();
        services.AddScoped<IUcKSandikDurumRaporService, UcKSandikDurumRaporService>();
    }

    private static void AddBackgroundJobs(this IServiceCollection services)
    {
        services.AddHostedService<ArsivBackgroundService>();
        services.AddHostedService<CekiRevizyonTalebiTemizlemeBackgroundService>();
        services.AddHostedService<FinansDuzenliIsBackgroundService>();
    }
}
