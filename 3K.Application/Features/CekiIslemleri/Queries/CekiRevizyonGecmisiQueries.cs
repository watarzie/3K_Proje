using FluentValidation;
using MediatR;
using _3K.Application.Common;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.CekiIslemleri.Queries;

public sealed class GetCekiRevizyonGecmisiQuery : IRequest<Result<PaginatedList<CekiRevizyonGecmisiKaydi>>>, ISecuredRequest, IRequiresProjectMenuPermission
{
    public int ProjeId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int PermissionProjectId => ProjeId;
    public ProjectMenuOperation PermissionOperation => ProjectMenuOperation.RevisionHistory;
    public YetkiTipi RequiredYetkiTipi => YetkiTipi.R;
}

public sealed class GetCekiRevizyonGecmisiDetayQuery : IRequest<Result<CekiRevizyonGecmisiDetayi>>, ISecuredRequest, IRequiresProjectMenuPermission
{
    public int ProjeId { get; set; }
    public string Kaynak { get; set; } = "talep";
    public int KayitId { get; set; }
    public int PermissionProjectId => ProjeId;
    public ProjectMenuOperation PermissionOperation => ProjectMenuOperation.RevisionHistory;
    public YetkiTipi RequiredYetkiTipi => YetkiTipi.R;
}

public sealed class GetCekiRevizyonDosyaQuery : IRequest<Result<CekiRevizyonDosyasi>>, ISecuredRequest, IRequiresProjectMenuPermission
{
    public int ProjeId { get; set; }
    public string Kaynak { get; set; } = "talep";
    public int KayitId { get; set; }
    public int PermissionProjectId => ProjeId;
    public ProjectMenuOperation PermissionOperation => ProjectMenuOperation.RevisionHistory;
    public YetkiTipi RequiredYetkiTipi => YetkiTipi.R;
}

public sealed class GetCekiRevizyonGecmisiQueryValidator : AbstractValidator<GetCekiRevizyonGecmisiQuery>
{
    public GetCekiRevizyonGecmisiQueryValidator()
    {
        RuleFor(x => x.ProjeId).GreaterThan(0);
        RuleFor(x => x.PageNumber).InclusiveBetween(1, 1000000);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 50);
    }
}

public sealed class GetCekiRevizyonGecmisiDetayQueryValidator : AbstractValidator<GetCekiRevizyonGecmisiDetayQuery>
{
    public GetCekiRevizyonGecmisiDetayQueryValidator()
    {
        RuleFor(x => x.ProjeId).GreaterThan(0);
        RuleFor(x => x.KayitId).GreaterThan(0);
        RuleFor(x => x.Kaynak).Must(x => x is "talep" or "ceki");
    }
}

public sealed class GetCekiRevizyonDosyaQueryValidator : AbstractValidator<GetCekiRevizyonDosyaQuery>
{
    public GetCekiRevizyonDosyaQueryValidator()
    {
        RuleFor(x => x.ProjeId).GreaterThan(0);
        RuleFor(x => x.KayitId).GreaterThan(0);
        RuleFor(x => x.Kaynak).Must(x => x is "talep" or "ceki");
    }
}

public sealed class GetCekiRevizyonGecmisiQueryHandler(ICekiRevizyonGecmisiService service)
    : IRequestHandler<GetCekiRevizyonGecmisiQuery, Result<PaginatedList<CekiRevizyonGecmisiKaydi>>>
{
    public async Task<Result<PaginatedList<CekiRevizyonGecmisiKaydi>>> Handle(GetCekiRevizyonGecmisiQuery request, CancellationToken cancellationToken)
    {
        var result = await service.ListeleAsync(request.ProjeId, request.PageNumber, request.PageSize, cancellationToken);
        return Result<PaginatedList<CekiRevizyonGecmisiKaydi>>.Success(new(result.Items, result.TotalCount, request.PageNumber, request.PageSize));
    }
}

public sealed class GetCekiRevizyonGecmisiDetayQueryHandler(ICekiRevizyonGecmisiService service)
    : IRequestHandler<GetCekiRevizyonGecmisiDetayQuery, Result<CekiRevizyonGecmisiDetayi>>
{
    public async Task<Result<CekiRevizyonGecmisiDetayi>> Handle(GetCekiRevizyonGecmisiDetayQuery request, CancellationToken cancellationToken)
    {
        var result = await service.DetayAsync(request.ProjeId, request.Kaynak, request.KayitId, cancellationToken);
        return result == null ? Result<CekiRevizyonGecmisiDetayi>.Failure("Revizyon kaydı bu projede bulunamadı.", 404)
            : Result<CekiRevizyonGecmisiDetayi>.Success(result);
    }
}

public sealed class GetCekiRevizyonDosyaQueryHandler(ICekiRevizyonGecmisiService service)
    : IRequestHandler<GetCekiRevizyonDosyaQuery, Result<CekiRevizyonDosyasi>>
{
    public async Task<Result<CekiRevizyonDosyasi>> Handle(GetCekiRevizyonDosyaQuery request, CancellationToken cancellationToken)
    {
        var result = await service.DosyaAsync(request.ProjeId, request.Kaynak, request.KayitId, cancellationToken);
        return result == null ? Result<CekiRevizyonDosyasi>.Failure("Revizyon dosyası bulunamadı veya dosya bütünlüğü doğrulanamadı.", 404)
            : Result<CekiRevizyonDosyasi>.Success(result);
    }
}
