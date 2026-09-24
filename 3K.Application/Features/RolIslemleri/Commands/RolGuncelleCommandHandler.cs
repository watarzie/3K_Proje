using System.Text.Json;
using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.RolIslemleri.DTOs;
using _3K.Application.Features.RolIslemleri.Queries;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.RolIslemleri.Commands;

public sealed class RolGuncelleCommandHandler(IUnitOfWork unitOfWork, IRolService rolService,
    ICurrentUserService currentUser, ISseNotifier? sseNotifier = null) : IRequestHandler<RolGuncelleCommand, Result<RolDetayDto>>
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
            var validation = await YetkiAtamaKurallari.DogrulaVeSinirlaAsync(unitOfWork, rolService, currentUser,
                request.Yetkiler, ct);
            if (!validation.IsSuccess) return Result<RolDetayDto>.Failure(validation.Error!.Message, validation.StatusCode);
            var normalizedPermissions = validation.Value!;
            var onceki = await rolService.GetRolYetkileriAsync(request.Id, ct);
            await unitOfWork.GetRepository<YetkiDegisikligi>().AddAsync(new()
            {
                AktorKullaniciId = currentUser.UserId!.Value, HedefTuru = "Rol", HedefId = rol.Id,
                OncekiDeger = JsonSerializer.Serialize(new { rol.Ad, Yetkiler = onceki.Select(x => new { x.MenuTanimiId, x.YetkiTipiId }) }),
                YeniDeger = JsonSerializer.Serialize(new
                {
                    Ad = request.Ad.Trim(),
                    Yetkiler = normalizedPermissions.Where(x => x.YetkiTipiId >= 2)
                })
            });
            rol.Ad = request.Ad.Trim();
            // Boş liste bütün izinleri kaldırır; boş isteği sessizce yok sayma.
            await rolService.YetkileriGuncelleAsync(rol.Id, normalizedPermissions.Select(x => new RolYetki
            { RolId = rol.Id, MenuTanimiId = x.MenuTanimiId, YetkiTipiId = x.YetkiTipiId }).ToList(), ct);
            await unitOfWork.SaveChangesAsync(ct);
            var sonuc = await GetRolDetayQueryHandler.OkuAsync(unitOfWork, rolService, rol.Id, ct);
            if (sonuc.IsSuccess && sseNotifier != null)
            {
                // Bağlı istemciler notifier içinde kullanıcı kimliğiyle eşleştirilir.
                var kullaniciIdleri = (await unitOfWork.GetRepository<Kullanici>()
                    .FindAsync(kullanici => kullanici.RolId == rol.Id))
                    .Select(kullanici => kullanici.Id).ToArray();
                unitOfWork.RegisterAfterCommit(_ => sseNotifier.NotifyUsersAsync(
                    kullaniciIdleri, SseOlaylari.YetkiGuncellendi));
            }
            return sonuc;
        }, cancellationToken);
    }
}
