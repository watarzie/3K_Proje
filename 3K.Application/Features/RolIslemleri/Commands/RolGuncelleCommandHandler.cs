using System.Text.Json;
using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.RolIslemleri.DTOs;
using _3K.Application.Features.RolIslemleri.Queries;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.RolIslemleri.Commands;

public sealed class RolGuncelleCommandHandler(IUnitOfWork unitOfWork, IRolService rolService,
    ICurrentUserService currentUser) : IRequestHandler<RolGuncelleCommand, Result<RolDetayDto>>
{
    public async Task<Result<RolDetayDto>> Handle(RolGuncelleCommand request, CancellationToken cancellationToken)
    {
        return await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var rol = await unitOfWork.GetRepository<Rol>().GetByIdAsync(request.Id);
            if (rol == null) return Result<RolDetayDto>.Failure("Rol bulunamadı.", 404);
            if (string.IsNullOrWhiteSpace(request.Ad) ||
                (rol.Id != 1 && string.Equals(request.Ad.Trim(), "Admin", StringComparison.OrdinalIgnoreCase)))
                return Result<RolDetayDto>.Failure("Geçerli ve ayrılmış olmayan bir rol adı giriniz.");
            var validation = await YetkiAtamaKurallari.DogrulaAsync(unitOfWork, rolService, currentUser,
                request.Id, request.Yetkiler, ct);
            if (!validation.IsSuccess) return Result<RolDetayDto>.Failure(validation.Error!.Message, validation.StatusCode);
            var onceki = await rolService.GetRolYetkileriAsync(request.Id, ct);
            await unitOfWork.GetRepository<YetkiDegisikligi>().AddAsync(new()
            {
                AktorKullaniciId = currentUser.UserId!.Value, HedefTuru = "Rol", HedefId = rol.Id,
                OncekiDeger = JsonSerializer.Serialize(new { rol.Ad, Yetkiler = onceki.Select(x => new { x.MenuTanimiId, x.YetkiTipiId }) }),
                YeniDeger = JsonSerializer.Serialize(new { request.Ad, request.Yetkiler })
            });
            rol.Ad = request.Ad.Trim();
            // Boş liste bütün izinleri kaldırır; boş isteği sessizce yok sayma.
            await rolService.YetkileriGuncelleAsync(rol.Id, request.Yetkiler.Select(x => new RolYetki
            { RolId = rol.Id, MenuTanimiId = x.MenuTanimiId, YetkiTipiId = x.YetkiTipiId }).ToList(), ct);
            await unitOfWork.SaveChangesAsync(ct);
            return await GetRolDetayQueryHandler.OkuAsync(unitOfWork, rolService, rol.Id, ct);
        }, cancellationToken);
    }
}
