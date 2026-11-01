using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using _3K.Application.Behaviors;
using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri;
using _3K.Application.Features.AmbalajIslemleri.Commands;
using _3K.Application.Features.SandikIslemleri.Services;

namespace _3K.Application;

/// <summary>Application katmanının kullanım senaryolarını, pipeline adımlarını ve servislerini kaydeder.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);

            // Sıra (dıştan içe): iş hatası logu → hata yakalama → yetki → doğrulama → dışa aktarım sınırı → onay → handler.
            // Log en dıştadır; istisnadan Result'a çevrilen retler de dahil her başarısız sonucu görür.
            // Doğrulama onaydan önce çalışır; geçersiz bir komut onay kuyruğuna alınmaz.
            // Dışa aktarım sınırı yetki ve doğrulamadan sonradır: reddedilen ya da geçersiz dosya isteği kotadan düşmez.
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(IsHatasiLoglamaBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(DisaAktarimHizSiniriBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ApprovalBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);

        services.AddScoped<IApprovalExecutionContext, ApprovalExecutionContext>();
        services.AddScoped<IRequestMenuPermissionResolver, RequestMenuPermissionResolver>();
        services.AddScoped<ISandikLokasyonGuncellemeService, SandikLokasyonGuncellemeService>();
        services.AddScoped<IAmbalajKaynakSenkronizasyonService, AmbalajKaynakSenkronizasyonService>();
        services.AddScoped<AmbalajKritikIslemDenetimi>();

        return services;
    }
}
