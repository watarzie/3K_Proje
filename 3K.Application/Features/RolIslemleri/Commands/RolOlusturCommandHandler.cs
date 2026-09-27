using System.Text.Json;
using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.RolIslemleri.DTOs;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.RolIslemleri.Commands;

public sealed class RolOlusturCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    : IRequestHandler<RolOlusturCommand, Result<RolDto>>
{
    public async Task<Result<RolDto>> Handle(RolOlusturCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Ad) || SistemRolleri.AyrilmisAdMi(request.Ad))
            return Result<RolDto>.Failure("Geçerli ve ayrılmış olmayan bir rol adı giriniz.");
        if (currentUser.UserId is not int actor)
            return Result<RolDto>.Failure("Oturum açmanız gerekiyor.", 401);

        var ad = request.Ad.Trim();
        var repo = unitOfWork.GetRepository<Rol>();
        if ((await repo.FindAsync(x => x.Ad.ToLower() == ad.ToLower())).Any())
            return Result<RolDto>.Failure("Bu isimde bir rol zaten mevcut.");

        return await unitOfWork.ExecuteAtomicAsync(async ct =>
        {
            var rol = new Rol { Ad = ad };
            await repo.AddAsync(rol);
            await unitOfWork.SaveChangesAsync(ct);
            await unitOfWork.GetRepository<YetkiDegisikligi>().AddAsync(new()
            {
                AktorKullaniciId = actor, HedefTuru = "Rol", HedefId = rol.Id,
                OncekiDeger = "null", YeniDeger = JsonSerializer.Serialize(new { rol.Ad })
            });
            await unitOfWork.SaveChangesAsync(ct);
            return Result<RolDto>.Success(new RolDto { Id = rol.Id, Ad = rol.Ad });
        }, cancellationToken);
    }
}
