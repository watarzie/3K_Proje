using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.AuthIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.KullaniciIslemleri.Queries
{
    public class KullaniciListeleQueryHandler : IRequestHandler<KullaniciListeleQuery, Result<IEnumerable<KullaniciDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIkiFaktorService _ikiFaktorService;

        public KullaniciListeleQueryHandler(
            IUnitOfWork unitOfWork,
            IIkiFaktorService ikiFaktorService)
        {
            _unitOfWork = unitOfWork;
            _ikiFaktorService = ikiFaktorService;
        }

        public async Task<Result<IEnumerable<KullaniciDto>>> Handle(KullaniciListeleQuery request, CancellationToken cancellationToken)
        {
            var kullaniciRepo = _unitOfWork.GetRepository<Kullanici>();
            var kullanicilar = await kullaniciRepo.GetAllWithIncludeAsync(k => k.Rol);
            var kullaniciListesi = kullanicilar.ToList();
            var ayarDurumlari = await _ikiFaktorService.AyarDurumlariniGetirAsync(
                kullaniciListesi.Select(x => x.Id).ToArray(),
                cancellationToken);

            var result = kullaniciListesi.Select(k =>
            {
                ayarDurumlari.TryGetValue(k.Id, out var ayarDurumu);
                return AuthDtoFactory.Kullanici(
                    k,
                    ayarDurumu,
                    varsayilanRolAdi: "Belirtilmemiş");
            }).ToList();

            return Result<IEnumerable<KullaniciDto>>.Success(result);
        }
    }
}
