using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.RolIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.RolIslemleri.Queries;

/// <summary>Yalnız oturum sahibinin güncel DB rolü; istemciden rol/kullanıcı kimliği alınmaz.</summary>
public sealed class GetKullaniciMenuQuery : IRequest<Result<RolDetayDto>>;

public sealed class GetKullaniciMenuQueryHandler(
    ICurrentUserService currentUserService, IUnitOfWork unitOfWork, IRolService rolService)
    : IRequestHandler<GetKullaniciMenuQuery, Result<RolDetayDto>>
{
    public async Task<Result<RolDetayDto>> Handle(GetKullaniciMenuQuery request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.UserId is not int userId)
            return Result<RolDetayDto>.Failure("Oturum açmanız gerekiyor.", 401);
        var user = await unitOfWork.GetRepository<Kullanici>().GetByIdAsync(userId);
        if (user == null)
            return Result<RolDetayDto>.Failure("Kullanıcı bulunamadı.", 401);

        // Bu iç okuma yalnız doğrulanmış kullanıcının kendi rolüyle sınırlıdır.
        return await GetRolDetayQueryHandler.OkuAsync(unitOfWork, rolService, user.RolId, cancellationToken);
    }
}
