using MediatR;
using _3K.Core.Enums;
using _3K.Application.Common;
using _3K.Application.Features.NotIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.NotIslemleri.Queries
{
    public class GetNotlarQueryHandler : IRequestHandler<GetNotlarQuery, Result<List<NotDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetNotlarQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<List<NotDto>>> Handle(GetNotlarQuery request, CancellationToken cancellationToken)
        {
            var notRepo = _unitOfWork.GetRepository<Not>();
            var notlar = await notRepo.FindAsync(n =>
                n.BagliReferansTipi == request.BagliReferansTipi &&
                n.BagliReferansId == request.BagliReferansId);

            // Kullanıcı bilgilerini çekmek için
            var kullaniciRepo = _unitOfWork.GetRepository<Kullanici>();
            var kullaniciIdler = notlar.Select(n => n.KullaniciId).Distinct().ToList();
            var kullanicilar = await kullaniciRepo.FindAsync(k => kullaniciIdler.Contains(k.Id));
            var kullaniciMap = kullanicilar.ToDictionary(k => k.Id, k => k);

            var result = notlar
                .OrderByDescending(n => n.Tarih)
                .Select(n =>
                {
                    kullaniciMap.TryGetValue(n.KullaniciId, out var kullanici);
                    return new NotDto
                    {
                        Id = n.Id,
                        YazanTaraf = n.YazanTarafId == (int)NotYazanTaraf.Grid ? "Grid" : "3K",
                        Icerik = n.Icerik,
                        Tarih = n.Tarih,
                        KullaniciAdi = kullanici?.AdSoyad ?? "Bilinmeyen",
                        KullaniciBasHarf = kullanici?.BasHarf,
                        BagliReferansTipi = n.BagliReferansTipi,
                        BagliReferansId = n.BagliReferansId
                    };
                })
                .ToList();

            return Result<List<NotDto>>.Success(result);
        }
    }
}
