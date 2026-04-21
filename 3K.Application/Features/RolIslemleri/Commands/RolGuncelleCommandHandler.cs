using MediatR;
using _3K.Core.Enums;
using _3K.Application.Common;
using _3K.Application.Features.RolIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.RolIslemleri.Commands
{
    public class RolGuncelleCommandHandler : IRequestHandler<RolGuncelleCommand, Result<RolDetayDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRolService _rolService;

        public RolGuncelleCommandHandler(IUnitOfWork unitOfWork, IRolService rolService)
        {
            _unitOfWork = unitOfWork;
            _rolService = rolService;
        }

        public async Task<Result<RolDetayDto>> Handle(RolGuncelleCommand request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.GetRepository<Rol>();
            var rol = (await repo.GetAllAsync()).FirstOrDefault(r => r.Id == request.Id);

            if (rol == null)
                return Result<RolDetayDto>.Failure("Rol bulunamadı.", 404);

            // Rol adını güncelle
            if (!string.IsNullOrWhiteSpace(request.Ad))
            {
                rol.Ad = request.Ad;
                repo.Update(rol);
                await _unitOfWork.SaveChangesAsync();
            }

            // Yetkileri güncelle (delete all + insert new)
            if (request.Yetkiler.Any())
            {
                var yetkiEntities = request.Yetkiler.Select(y => new RolYetki
                {
                    RolId = request.Id,
                    MenuTanimiId = y.MenuTanimiId,
                    YetkiTipiId = y.YetkiTipiId
                }).ToList();

                await _rolService.YetkileriGuncelleAsync(request.Id, yetkiEntities, cancellationToken);
            }

            // Güncel menü ağacını döndür
            var menuAgaci = await _rolService.GetMenuAgaciAsync(cancellationToken);
            var guncelYetkiler = await _rolService.GetRolYetkileriAsync(request.Id, cancellationToken);
            var yetkiMap = guncelYetkiler.ToDictionary(y => y.MenuTanimiId, y => y.YetkiTipiId);

            return Result<RolDetayDto>.Success(new RolDetayDto
            {
                Id = rol.Id,
                Ad = rol.Ad,
                MenuAgaci = menuAgaci
                    .OrderBy(m => m.Sira)
                    .Select(m => MapToDto(m, yetkiMap))
                    .ToList()
            });
        }

        private MenuTreeDto MapToDto(MenuTanimi menu, Dictionary<int, int> yetkiMap)
        {
            return new MenuTreeDto
            {
                Id = menu.Id,
                Kod = menu.Kod,
                LabelKey = menu.LabelKey,
                Icon = menu.Icon,
                Route = menu.Route,
                Sira = menu.Sira,
                YetkiTipiId = yetkiMap.TryGetValue(menu.Id, out var yetkiId) ? yetkiId : 1,
                YetkiTipiMetni = yetkiMap.TryGetValue(menu.Id, out var yt) ? ((YetkiTipi)yt).ToString() : "N",
                Children = menu.Children?
                    .OrderBy(c => c.Sira)
                    .Select(c => MapToDto(c, yetkiMap))
                    .ToList() ?? new()
            };
        }
    }
}
