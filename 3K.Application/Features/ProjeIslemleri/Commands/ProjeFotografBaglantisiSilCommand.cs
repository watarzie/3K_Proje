using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.ProjeIslemleri.DTOs;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.ProjeIslemleri.Commands
{
    /// <summary>
    /// Projenin bir fotoğraf veya gerçekleşen çeki bağlantısını siler (içerik sitede kalır, yalnız bağlantı kaldırılır). İzin:
    /// <see cref="YetkiKodlari.Proje.FotografBaglantisi"/> W. Sonuç projenin güncel bağlantı listesidir.
    /// </summary>
    public class ProjeFotografBaglantisiSilCommand : IRequest<Result<List<ProjeFotografBaglantisiDto>>>, ISecuredRequest, IRequiresMenuPermission
    {
        public int ProjeId { get; set; }
        public int BaglantiId { get; set; }

        public string RequiredMenuKod => YetkiKodlari.Proje.FotografBaglantisi;
        public YetkiTipi RequiredYetkiTipi => YetkiTipi.W;
    }

    public class ProjeFotografBaglantisiSilCommandHandler(
        IUnitOfWork unitOfWork,
        IHareketService hareketService,
        ICurrentUserService currentUserService)
        : IRequestHandler<ProjeFotografBaglantisiSilCommand, Result<List<ProjeFotografBaglantisiDto>>>
    {
        public Task<Result<List<ProjeFotografBaglantisiDto>>> Handle(ProjeFotografBaglantisiSilCommand request, CancellationToken cancellationToken) =>
            unitOfWork.ExecuteAtomicAsync(async token =>
            {
                var repo = unitOfWork.GetRepository<ProjeFotografBaglantisi>();
                var baglanti = await repo.GetByIdAsync(request.BaglantiId);
                if (baglanti is null || baglanti.ProjeId != request.ProjeId)
                    return Result<List<ProjeFotografBaglantisiDto>>.Failure("Fotoğraf bağlantısı bulunamadı.", 404);

                var eski = ProjeFotografBaglantisiGecmisi.Metin(baglanti);
                repo.Remove(baglanti);
                await unitOfWork.SaveChangesAsync(token);
                await hareketService.HareketKaydetAsync(ProjeFotografBaglantisiGecmisi.Kayit(
                    request.ProjeId, currentUserService.UserId, ProjeFotografBaglantisiGecmisi.Islem(baglanti.Tur, "Kaldırıldı"), eski, null));

                return Result<List<ProjeFotografBaglantisiDto>>.Success(
                    ProjeFotografBaglantisiDto.Liste(await repo.FindAsync(x => x.ProjeId == request.ProjeId)));
            }, cancellationToken);
    }
}
