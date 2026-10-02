using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.RolIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Enums;
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

        /// <summary>
        /// Rolün menü ağacını <see cref="YetkiAgaci"/> kuralıyla hesaplanan etkin seviyeleriyle döner;
        /// oturum sahibinin menüsü de rolünün bu ağacıdır.
        /// </summary>
        internal static async Task<Result<RolDetayDto>> OkuAsync(
            IUnitOfWork unitOfWork, IRolService rolService, int rolId, CancellationToken cancellationToken)
        {
            var rol = await unitOfWork.GetRepository<Rol>().GetByIdAsync(rolId);
            if (rol == null)
                return Result<RolDetayDto>.Failure("Rol bulunamadı.", 404);

            var menuAgaci = await rolService.GetMenuAgaciAsync(cancellationToken);
            var atamalar = (await rolService.GetRolYetkileriAsync(rolId, cancellationToken))
                .ToDictionary(y => y.MenuTanimiId, y => y.YetkiTipiId);

            var agac = new YetkiAgaci(Duzlestir(menuAgaci).Select(m => new YetkiDugumu(m.Id, m.Kod, m.ParentId)));
            var seviyeler = agac.RolSeviyeleri(atamalar);

            return Result<RolDetayDto>.Success(new RolDetayDto
            {
                Id = rol.Id,
                Ad = rol.Ad,
                MenuAgaci = menuAgaci.OrderBy(m => m.Sira).Select(m => MapToMenuTreeDto(m, seviyeler)).ToList()
            });
        }

        private static IEnumerable<MenuTanimi> Duzlestir(IEnumerable<MenuTanimi> menuler) =>
            menuler.SelectMany(m => Duzlestir(m.Children ?? []).Prepend(m));

        private static MenuTreeDto MapToMenuTreeDto(MenuTanimi menu, IReadOnlyDictionary<int, YetkiTipi> seviyeler)
        {
            var etkin = seviyeler.GetValueOrDefault(menu.Id, YetkiTipi.N);
            return new MenuTreeDto
            {
                Id = menu.Id,
                Kod = menu.Kod,
                LabelKey = menu.LabelKey,
                Icon = menu.Icon,
                Route = menu.Route,
                Sira = menu.Sira,
                YetkiTipiId = (int)etkin,
                YetkiTipiMetni = etkin.ToString(),
                Children = menu.Children?
                    .OrderBy(c => c.Sira)
                    .Select(c => MapToMenuTreeDto(c, seviyeler))
                    .ToList() ?? new()
            };
        }
    }
}
