using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.AmbalajIslemleri.Queries
{
    public sealed class GetAmbalajUretimKayitlariQueryHandler
        : IRequestHandler<GetAmbalajUretimKayitlariQuery, Result<PaginatedList<AmbalajUretimKaydiDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRolService _rolService;
        private readonly ICurrentUserService _currentUserService;

        public GetAmbalajUretimKayitlariQueryHandler(
            IUnitOfWork unitOfWork,
            IRolService rolService,
            ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _rolService = rolService;
            _currentUserService = currentUserService;
        }

        public async Task<Result<PaginatedList<AmbalajUretimKaydiDto>>> Handle(
            GetAmbalajUretimKayitlariQuery request,
            CancellationToken cancellationToken)
        {
            var yetkiler = await AmbalajYetkilendirmeYardimcisi.GorunumYetkileriniGetirAsync(
                _rolService, _currentUserService, cancellationToken);
            if (!yetkiler.KaynakGorunur)
                request.KaynakModul = null;
            var query = AmbalajSorguYardimcisi.Filtrele(
                _unitOfWork.GetRepository<AmbalajUretimKaydi>().Queryable(),
                request);
            var toplam = query.Count();
            var kayitlar = query
                .OrderByDescending(k => k.UretimTarihi ?? k.CreatedDate)
                .ThenByDescending(k => k.Id)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();
            var dtolar = AmbalajSorguYardimcisi.DtolariOlustur(_unitOfWork, kayitlar).ToList();
            dtolar.ForEach(dto => AmbalajYetkilendirmeYardimcisi.DtoyuMaskele(dto, yetkiler));
            return Result<PaginatedList<AmbalajUretimKaydiDto>>.Success(
                new PaginatedList<AmbalajUretimKaydiDto>(dtolar, toplam, request.PageNumber, request.PageSize));
        }
    }
}
