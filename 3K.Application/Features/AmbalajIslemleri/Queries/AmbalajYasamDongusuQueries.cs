using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Core.Models;
using _3K.Core.Constants;

namespace _3K.Application.Features.AmbalajIslemleri.Queries;

public sealed class GetAmbalajFormSurumleriQuery : IRequest<Result<IReadOnlyList<AmbalajFormSurumuDto>>>, ISecuredRequest, IRequiresMenuPermissions
{
    public int? ProjeId { get; set; }
    public int? KayitId { get; set; }
    public int? Id { get; set; }
    public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions => [MenuPermissionRequirement.Read(YetkiKodlari.Ambalaj.FormGoruntule)];
}

public sealed class GetAmbalajFormSurumuDosyasiQuery : IRequest<Result<DosyaIcerigi>>, ISecuredRequest, IRequiresMenuPermissions
{
    public int Id { get; set; }
    public string Format { get; set; } = "pdf";
    public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
        [MenuPermissionRequirement.Read(YetkiKodlari.Ambalaj.FormGoruntule), MenuPermissionRequirement.Read(Format == "xlsx" ? YetkiKodlari.Ambalaj.ExcelIndir : YetkiKodlari.Ambalaj.FormIndir)];
}

public sealed class GetAmbalajGerceklesenRaporQuery : IRequest<Result<AmbalajGerceklesenRaporDto>>, ISecuredRequest, IRequiresMenuPermissions
{
    public DateTime Baslangic { get; set; }
    public DateTime Bitis { get; set; }
    public int? ProjeId { get; set; }
    public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions => [MenuPermissionRequirement.Read(YetkiKodlari.Ambalaj.RaporGoruntule)];
}

public sealed class GetAmbalajGerceklesenRaporDosyasiQuery : IRequest<Result<DosyaIcerigi>>, ISecuredRequest, IRequiresMenuPermissions
{
    public DateTime Baslangic { get; set; }
    public DateTime Bitis { get; set; }
    public int? ProjeId { get; set; }
    public string Format { get; set; } = "pdf";
    public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
        [MenuPermissionRequirement.Read(YetkiKodlari.Ambalaj.RaporGoruntule), MenuPermissionRequirement.Read(Format == "xlsx" ? YetkiKodlari.Ambalaj.ExcelIndir : YetkiKodlari.Ambalaj.PdfIndir)];
}
