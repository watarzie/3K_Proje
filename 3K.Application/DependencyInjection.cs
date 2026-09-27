using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using _3K.Application.Behaviors;
using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri;
using _3K.Application.Features.AmbalajIslemleri.Commands;
using _3K.Application.Features.SandikIslemleri.Services;
using _3K.Core.Interfaces;

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

            // Sıra (dıştan içe): hata yakalama → yetki → doğrulama → onay → handler.
            // Doğrulama onaydan önce çalışır; geçersiz bir komut onay kuyruğuna alınmaz.
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
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
