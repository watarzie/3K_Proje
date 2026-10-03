using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using _3K.Core.Interfaces;
using _3K.Infrastructure;
using _3K.Infrastructure.Data;

namespace _3K.Application.Tests;

/// <summary>
/// Finans alan servislerini üretimdeki DI kaydıyla tek bir istek kapsamında kurar; servisler aynı
/// bağlamı, transaction yürütücüsünü ve denetim işlem grubunu paylaşır.
/// </summary>
internal sealed class FinansTestServisleri
{
    private readonly IServiceProvider _kapsam;

    public FinansTestServisleri(AppDbContext db, ICurrentUserService kullanici)
    {
        var kok = new ServiceCollection()
            .AddSingleton(db)
            .AddSingleton(kullanici)
            .AddFinansServices()
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        _kapsam = kok.CreateScope().ServiceProvider;
    }

    public T Al<T>() where T : notnull => _kapsam.GetRequiredService<T>();

    public IFinansPanelService Panel => Al<IFinansPanelService>();
    public IFinansIsKaydiService IsKaydi => Al<IFinansIsKaydiService>();
    public IFinansAylikOzetService AylikOzet => Al<IFinansAylikOzetService>();
    public IFinansSiparisFaturaService SiparisFatura => Al<IFinansSiparisFaturaService>();
    public IFinansGiderService Gider => Al<IFinansGiderService>();
    public IFinansGiderTanimService GiderTanim => Al<IFinansGiderTanimService>();
    public IFinansUrunService Urun => Al<IFinansUrunService>();
    public IFinansParametreService Parametre => Al<IFinansParametreService>();
    public IFinansDuzenliIsService DuzenliIs => Al<IFinansDuzenliIsService>();
    public IFinansBelgeService Belge => Al<IFinansBelgeService>();
    public IFinansKaliciSilmeService KaliciSilme => Al<IFinansKaliciSilmeService>();
    public IFinansRaporVerisiService RaporVerisi => Al<IFinansRaporVerisiService>();
    public IFinansUretimAktarimService UretimAktarim => Al<IFinansUretimAktarimService>();
    public IFinansRaporService Raporlar => Al<IFinansRaporService>();

    /// <summary>Sorgu çalıştırılmadıkça bağlantı açmayan bağlam; change tracker ve SQL üretimi için.</summary>
    public static AppDbContext BaglanmayanContext() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseNpgsql("Host=localhost;Database=model_validation;Username=test;Password=test").Options);
}
