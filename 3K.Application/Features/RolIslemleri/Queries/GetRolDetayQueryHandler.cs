using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.RolIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.RolIslemleri.Queries
{
    public class GetRolDetayQueryHandler : IRequestHandler<GetRolDetayQuery, Result<RolDetayDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRolService _rolService;

        public GetRolDetayQueryHandler(IUnitOfWork unitOfWork, IRolService rolService)
        {
            _unitOfWork = unitOfWork;
            _rolService = rolService;
        }

        public async Task<Result<RolDetayDto>> Handle(GetRolDetayQuery request, CancellationToken cancellationToken)
        {
            var rolRepo = _unitOfWork.GetRepository<Rol>();
            var rol = (await rolRepo.GetAllAsync()).FirstOrDefault(r => r.Id == request.RolId);

            if (rol == null)
                return Result<RolDetayDto>.Failure("Rol bulunamadı.", 404);

            // Menü ağacını getir
            var menuAgaci = await _rolService.GetMenuAgaciAsync(cancellationToken);

            // Rolün mevcut yetkilerini getir
            var yetkiler = await _rolService.GetRolYetkileriAsync(request.RolId, cancellationToken);
            var yetkiMap = yetkiler.ToDictionary(y => y.MenuTanimiId, y => y.YetkiTipi);

            // Menü ağacını DTO'ya dönüştür (recursive)
            var menuTree = menuAgaci
                .OrderBy(m => m.Sira)
                .Select(m => MapToMenuTreeDto(m, yetkiMap))
                .ToList();

            return Result<RolDetayDto>.Success(new RolDetayDto
            {
                Id = rol.Id,
                Ad = rol.Ad,
                MenuAgaci = menuTree
            });
        }

        /// <summary>
        /// MenuTanimi → MenuTreeDto recursive dönüşümü.
        /// Her node'a rolün yetkisini ekler (W/R/N).
        /// </summary>
        private MenuTreeDto MapToMenuTreeDto(MenuTanimi menu, Dictionary<int, string> yetkiMap)
        {
            return new MenuTreeDto
            {
                Id = menu.Id,
                Kod = menu.Kod,
                LabelKey = menu.LabelKey,
                Icon = menu.Icon,
                Route = menu.Route,
                Sira = menu.Sira,
                YetkiTipi = yetkiMap.TryGetValue(menu.Id, out var yetki) ? yetki : "N",
                Children = menu.Children?
                    .OrderBy(c => c.Sira)
                    .Select(c => MapToMenuTreeDto(c, yetkiMap))
                    .ToList() ?? new()
            };
        }
    }
}
