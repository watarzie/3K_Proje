using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.AmbalajIslemleri.Queries
{
    public sealed class GetAmbalajUretimKaydiDetayQueryHandler
        : IRequestHandler<GetAmbalajUretimKaydiDetayQuery, Result<AmbalajUretimKaydiDetayDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRolService _rolService;
        private readonly ICurrentUserService _currentUserService;

        public GetAmbalajUretimKaydiDetayQueryHandler(
            IUnitOfWork unitOfWork,
            IRolService rolService,
            ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _rolService = rolService;
            _currentUserService = currentUserService;
        }

        public async Task<Result<AmbalajUretimKaydiDetayDto>> Handle(
            GetAmbalajUretimKaydiDetayQuery request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var kayit = await _unitOfWork.GetRepository<AmbalajUretimKaydi>().GetByIdAsync(request.Id);
            if (kayit == null)
                return Result<AmbalajUretimKaydiDetayDto>.Failure("Ambalaj üretim kaydı bulunamadı.", 404);
            var yetkiler = await AmbalajYetkilendirmeYardimcisi.GorunumYetkileriniGetirAsync(
                _rolService, _currentUserService, cancellationToken);

            var temelDto = AmbalajSorguYardimcisi.DtolariOlustur(_unitOfWork, [kayit]).Single();
            AmbalajYetkilendirmeYardimcisi.DtoyuMaskele(temelDto, yetkiler);
            var hareketler = _unitOfWork.GetRepository<AmbalajUretimHareketi>().Queryable()
                .Where(h => h.AmbalajUretimKaydiId == kayit.Id)
                .OrderByDescending(h => h.Tarih)
                .ThenByDescending(h => h.Id)
                .Select(h => new AmbalajUretimHareketiDto
                {
                    Id = h.Id,
                    IslemGrubu = h.IslemGrubu,
                    KullaniciId = h.KullaniciId,
                    Tarih = h.Tarih,
                    Islem = h.Islem,
                    AlanAdi = h.AlanAdi,
                    EskiDeger = h.EskiDeger,
                    YeniDeger = h.YeniDeger,
                    Aciklama = h.Aciklama
                })
                .ToList();
            hareketler.ForEach(hareket =>
                AmbalajYetkilendirmeYardimcisi.HareketiMaskele(hareket, yetkiler));

            var detay = new AmbalajUretimKaydiDetayDto();
            foreach (var property in typeof(AmbalajUretimKaydiDto).GetProperties())
                property.SetValue(detay, property.GetValue(temelDto));
            detay.Hareketler = hareketler;
            return Result<AmbalajUretimKaydiDetayDto>.Success(detay);
        }
    }
}
