using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.ProjeIslemleri.DTOs;
using _3K.Application.Features.ProjeIslemleri.Services;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.ProjeIslemleri.Commands
{
    /// <summary>
    /// Elle ürün/sandık bazlı saha aktarımı; doğrulama ve yazma ortak <see cref="SahaAktarimIslemi"/> motorundadır.
    /// </summary>
    public class EksiklerdenSahaProjesiOlusturCommandHandler : IRequestHandler<EksiklerdenSahaProjesiOlusturCommand, Result<ProjeDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHareketService _hareketService;
        private readonly ILookupCacheService _lookupCache;
        private readonly ICurrentUserService _currentUserService;
        private readonly ISahaTamamlamaService _sahaTamamlamaService;

        public EksiklerdenSahaProjesiOlusturCommandHandler(
            IUnitOfWork unitOfWork,
            IHareketService hareketService,
            ILookupCacheService lookupCache,
            ICurrentUserService currentUserService,
            ISahaTamamlamaService sahaTamamlamaService)
        {
            _unitOfWork = unitOfWork;
            _hareketService = hareketService;
            _lookupCache = lookupCache;
            _currentUserService = currentUserService;
            _sahaTamamlamaService = sahaTamamlamaService;
        }

        public Task<Result<ProjeDto>> Handle(EksiklerdenSahaProjesiOlusturCommand request, CancellationToken cancellationToken) =>
            _unitOfWork.ExecuteAtomicAsync(async token =>
            {
                var islem = new SahaAktarimIslemi(
                    _unitOfWork,
                    _hareketService,
                    _lookupCache,
                    _currentUserService,
                    _sahaTamamlamaService);
                var sonuc = await islem.UygulaAsync(request, token);

                return sonuc.IsSuccess
                    ? Result<ProjeDto>.Success(sonuc.Value!.Proje)
                    : SahaAktarimSonucEslemesi.Basarisiz<ProjeDto>(sonuc);
            }, cancellationToken);
    }
}
