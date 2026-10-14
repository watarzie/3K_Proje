using MediatR;
using _3K.Application.Common;
using _3K.Core.Enums;

namespace _3K.Application.Features.FinansIslemleri.Commands;

/// <summary>
/// Finans işlemi: yalnız ait olduğu sekme(ler)in yazma (W) iznini ister; birden çok sekme verilirse herhangi birinin
/// izni yeter (<see cref="_3K.Core.Constants.FinansSekmeleri"/>). Alt işlem veya alan izni yoktur.
/// </summary>
public abstract class FinansCommand<T> : IRequest<Result<T>>, ISecuredRequest, IRequiresMenuPermission, IRequiresMenuPermissions
{
    /// <summary>İşlemin ait olduğu sekme kodları (YetkiKodlari.Finans.Sekme*).</summary>
    public abstract IReadOnlyList<string> Sekmeler { get; }
    public string RequiredMenuKod => Sekmeler[0];
    public YetkiTipi RequiredYetkiTipi => YetkiTipi.W;
    public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions => Sekmeler.Select(MenuPermissionRequirement.Write).ToArray();
    public MenuPermissionMatch PermissionMatch => MenuPermissionMatch.Any;
}

/// <inheritdoc cref="FinansCommand{T}"/>
public abstract class FinansCommand : IRequest<Result>, ISecuredRequest, IRequiresMenuPermission, IRequiresMenuPermissions
{
    /// <summary>İşlemin ait olduğu sekme kodları (YetkiKodlari.Finans.Sekme*).</summary>
    public abstract IReadOnlyList<string> Sekmeler { get; }
    public string RequiredMenuKod => Sekmeler[0];
    public YetkiTipi RequiredYetkiTipi => YetkiTipi.W;
    public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions => Sekmeler.Select(MenuPermissionRequirement.Write).ToArray();
    public MenuPermissionMatch PermissionMatch => MenuPermissionMatch.Any;
}
