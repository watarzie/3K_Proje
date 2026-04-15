using MediatR;
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

        public SandikEkleCommandHandler(IUnitOfWork unitOfWork, ISandikService sandikService)
        {
            _unitOfWork = unitOfWork;
            _sandikService = sandikService;
        }

        public async Task<Result<SandikDto>> Handle(SandikEkleCommand request, CancellationToken cancellationToken)
        {
            // 1. Proje var mı?
            var projeRepo = _unitOfWork.GetRepository<Proje>();
            var proje = await projeRepo.GetByIdAsync(request.ProjeId);
            if (proje == null)
                return Result<SandikDto>.Failure("Proje bulunamadı.", 404);

            // 2. SandıkNo boş olamaz
            if (string.IsNullOrWhiteSpace(request.SandikNo))
                return Result<SandikDto>.Failure("Sandık numarası boş olamaz.", 400);

            // 3. Aynı projede bu sandık numarası zaten var mı?
            var mevcutSandik = await _sandikService.GetSandikByNoAsync(request.ProjeId, request.SandikNo);
            if (mevcutSandik != null)
                return Result<SandikDto>.Failure($"'{request.SandikNo}' numaralı sandık bu projede zaten mevcut.", 409);

            // 4. Sandık oluştur
            var sandikRepo = _unitOfWork.GetRepository<Sandik>();
            var sandik = new Sandik
            {
                ProjeId = request.ProjeId,
                SandikNo = request.SandikNo,
                Tip = request.Tip,
                Durum = StatusConstants.SandikDurum.Bos,
                DepoLokasyonu = request.DepoLokasyonu
            };
            await sandikRepo.AddAsync(sandik);
            await _unitOfWork.SaveChangesAsync();

            // 5. Response
            var dto = new SandikDto
            {
                Id = sandik.Id,
                SandikNo = sandik.SandikNo,
                Durum = sandik.Durum,
                DepoLokasyonu = sandik.DepoLokasyonu,
                UrunSayisi = 0
            };

            return Result<SandikDto>.Success(dto);
        }
    }
}
