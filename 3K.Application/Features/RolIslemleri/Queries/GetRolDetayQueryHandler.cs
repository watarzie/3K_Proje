using MediatR;
using _3K.Core.Enums;
using _3K.Application.Common;
using _3K.Application.Features.RolIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Interfaces;
using _3K.Core.Models;

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
            => await OkuAsync(_unitOfWork, _rolService, request.RolId, cancellationToken);

        internal static async Task<Result<RolDetayDto>> OkuAsync(
            IUnitOfWork unitOfWork, IRolService rolService, int rolId, CancellationToken cancellationToken)
        {
            var rolRepo = unitOfWork.GetRepository<Rol>();
            var rol = await rolRepo.GetByIdAsync(rolId);

            if (rol == null)
                return Result<RolDetayDto>.Failure("Rol bulunamadı.", 404);

            // Menü ağacını getir
            var menuAgaci = await rolService.GetMenuAgaciAsync(cancellationToken);

            // Rolün mevcut yetkilerini getir
            var yetkiler = await rolService.GetRolYetkileriAsync(rolId, cancellationToken);
            var yetkiMap = yetkiler.ToDictionary(y => y.MenuTanimiId, y => y.YetkiTipiId);

            // Menü ağacını DTO'ya dönüştür (recursive)
            var menuTree = menuAgaci
                .OrderBy(m => m.Sira)
                .Select(m => MapToMenuTreeDto(m, yetkiMap, (int)YetkiTipi.W))
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
        private static MenuTreeDto MapToMenuTreeDto(MenuTanimi menu, Dictionary<int, int> yetkiMap, int ustYetkisi)
        {
            var atanmisYetki = yetkiMap.GetValueOrDefault(menu.Id, (int)YetkiTipi.N);
            var etkinYetki = YetkiDegerlendirici.UstSinirliYetki(menu.Kod, atanmisYetki, ustYetkisi);
            return new MenuTreeDto
            {
                Id = menu.Id,
                Kod = menu.Kod,
                LabelKey = menu.LabelKey,
                Ad = _3K.Core.Constants.YetkiKatalogu.Bul(menu.Kod)?.Ad,
                GerekenYetkiTipiId = (int?)_3K.Core.Constants.YetkiKatalogu.Bul(menu.Kod)?.GerekenYetki,
                KritikMi = _3K.Core.Constants.YetkiKatalogu.Bul(menu.Kod)?.Kritik ?? false,
                Icon = menu.Icon,
                Route = menu.Route,
                Sira = menu.Sira,
                YetkiTipiId = etkinYetki,
                YetkiTipiMetni = ((YetkiTipi)etkinYetki).ToString(),
                Children = menu.Children?
                    .OrderBy(c => c.Sira)
                    .Select(c => MapToMenuTreeDto(c, yetkiMap, etkinYetki))
                    .ToList() ?? new()
            };
        }
    }
}
