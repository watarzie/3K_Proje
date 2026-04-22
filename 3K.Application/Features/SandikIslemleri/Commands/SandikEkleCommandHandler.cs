using MediatR;
using _3K.Core.Enums;
using _3K.Application.Common;
using _3K.Application.Features.SandikIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    public class SandikEkleCommandHandler : IRequestHandler<SandikEkleCommand, Result<SandikDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISandikService _sandikService;
        private readonly ILookupCacheService _lookupCache;

        public SandikEkleCommandHandler(IUnitOfWork unitOfWork, ISandikService sandikService, ILookupCacheService lookupCache)
        {
            _unitOfWork = unitOfWork;
            _sandikService = sandikService;
            _lookupCache = lookupCache;
        }

        public async Task<Result<SandikDto>> Handle(SandikEkleCommand request, CancellationToken cancellationToken)
        {
            var projeRepo = _unitOfWork.GetRepository<Proje>();
            var proje = await projeRepo.GetByIdAsync(request.ProjeId);
            if (proje == null)
                return Result<SandikDto>.Failure("Proje bulunamadı.", 404);

            if (string.IsNullOrWhiteSpace(request.SandikNo))
                return Result<SandikDto>.Failure("Sandık numarası boş olamaz.", 400);

            var mevcutSandik = await _sandikService.GetSandikByNoAsync(request.ProjeId, request.SandikNo);
            if (mevcutSandik != null)
                return Result<SandikDto>.Failure($"'{request.SandikNo}' numaralı sandık bu projede zaten mevcut.", 409);

            var sandikRepo = _unitOfWork.GetRepository<Sandik>();
            var sandik = new Sandik
            {
                ProjeId = request.ProjeId,
                SandikNo = request.SandikNo,
                TipId = request.TipId,
                DurumId = (int)SandikDurum.Bos,
                DepoLokasyonId = request.DepoLokasyonId,
                En = request.En,
                Boy = request.Boy,
                Yukseklik = request.Yukseklik,
                NetKg = request.NetKg,
                GrossKg = request.GrossKg
            };
            await sandikRepo.AddAsync(sandik);
            await _unitOfWork.SaveChangesAsync();

            var dto = new SandikDto
            {
                Id = sandik.Id,
                SandikNo = sandik.SandikNo,
                DurumId = sandik.DurumId,
                DurumMetni = _lookupCache.GetDeger<LookupSandikDurum>(sandik.DurumId),
                DepoLokasyonId = sandik.DepoLokasyonId,
                DepoLokasyonMetni = _lookupCache.GetDeger<LookupDepoLokasyon>(sandik.DepoLokasyonId),
                UrunSayisi = 0
            };

            return Result<SandikDto>.Success(dto);
        }
    }
}
