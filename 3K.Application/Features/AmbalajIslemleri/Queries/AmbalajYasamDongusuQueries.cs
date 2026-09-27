using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Core.Models;

namespace _3K.Application.Features.AmbalajIslemleri.Queries;

public sealed class GetAmbalajFormSurumleriQuery : IRequest<Result<IReadOnlyList<AmbalajFormSurumuDto>>>, ISecuredRequest, IRequiresMenuPermissions
{
    public int? ProjeId { get; set; }
    public int? KayitId { get; set; }
    public int? Id { get; set; }
    public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions => [AmbalajMenuKodlari.Read(AmbalajMenuKodlari.FormGoruntule)];
}

public sealed class GetAmbalajFormSurumuDosyasiQuery : IRequest<Result<DosyaIcerigi>>, ISecuredRequest, IRequiresMenuPermissions
{
    public int Id { get; set; }
    public string Format { get; set; } = "pdf";
    public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
        [AmbalajMenuKodlari.Read(AmbalajMenuKodlari.FormGoruntule), AmbalajMenuKodlari.Read(Format == "xlsx" ? AmbalajMenuKodlari.ExcelIndir : AmbalajMenuKodlari.FormIndir)];
}

public sealed class GetAmbalajGerceklesenRaporQuery : IRequest<Result<AmbalajGerceklesenRaporDto>>, ISecuredRequest, IRequiresMenuPermissions
{
    public DateTime Baslangic { get; set; }
    public DateTime Bitis { get; set; }
    public int? ProjeId { get; set; }
    public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions => [AmbalajMenuKodlari.Read(AmbalajMenuKodlari.RaporGoruntule)];
}

public sealed class GetAmbalajGerceklesenRaporDosyasiQuery : IRequest<Result<DosyaIcerigi>>, ISecuredRequest, IRequiresMenuPermissions
{
    public DateTime Baslangic { get; set; }
    public DateTime Bitis { get; set; }
    public int? ProjeId { get; set; }
    public string Format { get; set; } = "pdf";
    public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
        [AmbalajMenuKodlari.Read(AmbalajMenuKodlari.RaporGoruntule), AmbalajMenuKodlari.Read(Format == "xlsx" ? AmbalajMenuKodlari.ExcelIndir : AmbalajMenuKodlari.PdfIndir)];
}
