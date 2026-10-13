using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.ProjeIslemleri.DTOs;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.ProjeIslemleri.Commands
{
    /// <summary>
    /// Projenin fotoğraf sayfası bağlantısını ekler, değiştirir veya boş gönderilirse kaldırır. Proje tipinden bağımsız
    /// tek izin: <see cref="YetkiKodlari.Proje.FotografBaglantisi"/> W. Dönen değer kaydedilen (normalleştirilmiş) adrestir; JSON nesnesi olarak.
    /// </summary>
    public class ProjeFotografBaglantisiGuncelleCommand : IRequest<Result<ProjeFotografBaglantisiDto>>, ISecuredRequest, IRequiresMenuPermission
    {
        public int ProjeId { get; set; }
        public string? Baglanti { get; set; }

        public string RequiredMenuKod => YetkiKodlari.Proje.FotografBaglantisi;
        public YetkiTipi RequiredYetkiTipi => YetkiTipi.W;
    }

    public class ProjeFotografBaglantisiGuncelleCommandHandler(
        IUnitOfWork unitOfWork,
        IHareketService hareketService,
        ICurrentUserService currentUserService)
        : IRequestHandler<ProjeFotografBaglantisiGuncelleCommand, Result<ProjeFotografBaglantisiDto>>
    {
        public async Task<Result<ProjeFotografBaglantisiDto>> Handle(ProjeFotografBaglantisiGuncelleCommand request, CancellationToken cancellationToken)
        {
            var proje = await unitOfWork.GetRepository<Proje>().GetByIdAsync(request.ProjeId);
            if (proje == null)
                return Result<ProjeFotografBaglantisiDto>.Failure("Proje bulunamadı.", 404);

            var eski = proje.FotografBaglantisi;
            var yeni = ProjeFotografBaglantisiKurali.Normalize(request.Baglanti);
            if (string.Equals(eski, yeni, StringComparison.Ordinal))
                return Basarili(yeni);

            // İzlenen proje doğrudan değiştirilir; sevk edilmiş projede de fotoğraf eklenebildiği için durum kilidi yoktur.
            proje.FotografBaglantisi = yeni;
            await unitOfWork.SaveChangesAsync(cancellationToken);

            await hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = proje.Id,
                KullaniciId = currentUserService.UserId ?? 0,
                ReferansTipi = "Proje",
                ReferansId = proje.Id.ToString(),
                Islem = eski == null ? "Fotoğraf Bağlantısı Eklendi"
                    : yeni == null ? "Fotoğraf Bağlantısı Kaldırıldı"
                    : "Fotoğraf Bağlantısı Güncellendi",
                IslemTipiId = null,
                EskiDeger = eski ?? "Yok",
                YeniDeger = yeni ?? "Yok",
                Aciklama = "Projenin fotoğraf sayfası bağlantısı değiştirildi."
            });

            return Basarili(yeni);
        }

        private static Result<ProjeFotografBaglantisiDto> Basarili(string? baglanti) =>
            Result<ProjeFotografBaglantisiDto>.Success(new ProjeFotografBaglantisiDto { FotografBaglantisi = baglanti });
    }
}
