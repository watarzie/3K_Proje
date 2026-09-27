using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Core.Constants;

namespace _3K.Application.Features.AmbalajIslemleri.Queries;

public abstract class AmbalajPlanlamaQuery<T> : IRequest<Result<T>>, ISecuredRequest,
    IRequiresMenuPermissions, IAmbalajDurumKapsamli
{
    [System.Text.Json.Serialization.JsonIgnore] public int[]? IzinliDurumlar { get; set; }
    public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
        this switch
        {
            GetAmbalajPlanlamaPlanQuery => [MenuPermissionRequirement.Read(YetkiKodlari.Ambalaj.PlanGoruntule), MenuPermissionRequirement.Read(YetkiKodlari.Ambalaj.ProjeDetayGoruntule)],
            GetAmbalajProjeSandikSecenekleriQuery or GetAmbalajIlaveSandikAdaylariQuery => [MenuPermissionRequirement.Read(YetkiKodlari.Ambalaj.SandikListesiGoruntule)],
            _ => [MenuPermissionRequirement.Read(YetkiKodlari.Ambalaj.Listele)]
        };
}

public sealed class GetAmbalajPlanlamaProjeleriQuery
    : AmbalajPlanlamaQuery<AmbalajPlanlamaProjeleriSayfasiDto>
{
    public string? Arama { get; set; }
    public int? ProjeTipiId { get; set; }
    public int Grup { get; set; } = 1;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 15;
    public bool IncludeSummary { get; set; } = true;
}

public sealed class GetAmbalajPlanlamaPlanQuery : AmbalajPlanlamaQuery<AmbalajPlanlamaPlanDto>
{
    public int ProjeId { get; init; }
    public int? KaynakProjeTipiId { get; init; }
    public int? Grup { get; init; }
}

public sealed class GetAmbalajIcSandikSablonlariQuery
    : AmbalajPlanlamaQuery<IReadOnlyList<AmbalajIcSandikSablonDto>>;

public sealed class GetAmbalajTalepEdenKullanicilarQuery
    : AmbalajPlanlamaQuery<IReadOnlyList<AmbalajKullaniciSecenegiDto>>;

public sealed class GetAmbalajBagimsizSandiklarQuery
    : AmbalajPlanlamaQuery<AmbalajBagimsizSandiklarSayfasiDto>
{
    public string? Arama { get; set; }
    public int? Tur { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public bool IncludeSummary { get; set; } = true;
}

public sealed class GetAmbalajIlaveSandikAdaylariQuery
    : AmbalajPlanlamaQuery<IReadOnlyList<AmbalajIlaveSandikAdayDto>>
{
    public int ProjeId { get; init; }
    public int? MevcutKayitId { get; init; }
}

public sealed class GetAmbalajProjeSandikSecenekleriQuery
    : AmbalajPlanlamaQuery<IReadOnlyList<AmbalajSandikSecenegiDto>>
{
    public int ProjeId { get; init; }
}
