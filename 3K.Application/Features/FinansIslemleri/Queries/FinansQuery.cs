using MediatR;
using _3K.Application.Common;
using _3K.Core.Enums;

namespace _3K.Application.Features.FinansIslemleri.Queries;

/// <summary>
/// Finans sorgusu: yalnız ait olduğu sekme(ler)in okuma (R) iznini ister; birden çok sekme verilirse herhangi birinin
/// izni yeter. Sekmeyi görebilen kullanıcı sekmedeki bütün alanları görür (alan maskelemesi yoktur). İşlem önizlemeleri
/// <see cref="RequiredYetkiTipi"/>'ni W yapar.
/// </summary>
public abstract class FinansQuery<T> : IRequest<Result<T>>, ISecuredRequest, IRequiresMenuPermission, IRequiresMenuPermissions
{
    /// <summary>Sorgunun ait olduğu sekme kodları (YetkiKodlari.Finans.Sekme*).</summary>
    public abstract IReadOnlyList<string> Sekmeler { get; }
    public string RequiredMenuKod => Sekmeler[0];
    public virtual YetkiTipi RequiredYetkiTipi => YetkiTipi.R;
    public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
        Sekmeler.Select(kod => new MenuPermissionRequirement(kod, RequiredYetkiTipi)).ToArray();
    public MenuPermissionMatch PermissionMatch => MenuPermissionMatch.Any;
}
