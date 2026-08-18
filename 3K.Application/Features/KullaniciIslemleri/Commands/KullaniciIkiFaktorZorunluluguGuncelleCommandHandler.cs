using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.KullaniciIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.KullaniciIslemleri.Commands
{
    public sealed class KullaniciIkiFaktorZorunluluguGuncelleCommandHandler
        : IRequestHandler<
            KullaniciIkiFaktorZorunluluguGuncelleCommand,
            Result<KullaniciIkiFaktorDurumDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIkiFaktorService _ikiFaktorService;

        public KullaniciIkiFaktorZorunluluguGuncelleCommandHandler(
            IUnitOfWork unitOfWork,
            IIkiFaktorService ikiFaktorService)
        {
            _unitOfWork = unitOfWork;
            _ikiFaktorService = ikiFaktorService;
        }

        public async Task<Result<KullaniciIkiFaktorDurumDto>> Handle(
            KullaniciIkiFaktorZorunluluguGuncelleCommand request,
            CancellationToken cancellationToken)
        {
            if (!request.ZorunluMu.HasValue)
            {
                return Result<KullaniciIkiFaktorDurumDto>.Failure(
                    "ZorunluMu alanı zorunludur.",
                    400);
            }

            var kullaniciRepo = _unitOfWork.GetRepository<Kullanici>();
            var kullanici = await kullaniciRepo.GetByIdAsync(request.KullaniciId);
            if (kullanici == null)
            {
                return Result<KullaniciIkiFaktorDurumDto>.Failure(
                    "Kullanıcı bulunamadı.",
                    404);
            }

            // GetByIdAsync tracked entity döndürür. Yalnız bu alanı değiştirmek,
            // eşzamanlı profil/rol güncellemesinin kolonlarını ezmez.
            kullanici.IkiFaktorZorunluMu = request.ZorunluMu.Value;
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var ayarDurumlari = await _ikiFaktorService.AyarDurumlariniGetirAsync(
                new[] { kullanici.Id },
                cancellationToken);
            ayarDurumlari.TryGetValue(kullanici.Id, out var ayarDurumu);

            return Result<KullaniciIkiFaktorDurumDto>.Success(
                new KullaniciIkiFaktorDurumDto
                {
                    KullaniciId = kullanici.Id,
                    IkiFaktorZorunluMu = kullanici.IkiFaktorZorunluMu,
                    IkiFaktorEtkinMi = ayarDurumu?.EtkinMi ?? false,
                    IkiFaktorDogrulandiTarihiUtc = ayarDurumu?.DogrulandiTarihiUtc
                });
        }
    }
}
