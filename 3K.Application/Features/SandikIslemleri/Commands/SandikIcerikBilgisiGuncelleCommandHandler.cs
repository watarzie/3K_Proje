using MediatR;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    /// <summary>
    /// Ürün bilgisini yalnız projeye ait, sevk kilidi olmayan sandıktaki satırda değiştirir ve eski/yeni değeri
    /// hareket geçmişine yazar. Değer değişmediyse hiçbir şey yazılmaz.
    /// </summary>
    public class SandikIcerikBilgisiGuncelleCommandHandler(
        IUnitOfWork unitOfWork,
        IHareketService hareketService,
        ICurrentUserService currentUser)
        : IRequestHandler<SandikIcerikBilgisiGuncelleCommand, Result>
    {
        public async Task<Result> Handle(SandikIcerikBilgisiGuncelleCommand request, CancellationToken cancellationToken)
        {
            var icerik = unitOfWork.GetRepository<SandikIcerik>().Queryable()
                .FirstOrDefault(i => i.Id == request.SandikIcerikId);
            if (icerik == null || icerik.SandikId != request.SandikId)
                return Result.Failure("Sandık içeriği bulunamadı.", 404);

            var sandik = await unitOfWork.GetRepository<Sandik>().GetByIdAsync(request.SandikId);
            if (sandik == null || sandik.ProjeId != request.ProjeId)
                return Result.Failure("Sandık bulunamadı veya projeye ait değil.", 404);
            if (SandikSevkKilidiHelper.SandikKilitliMi(sandik))
                return Result.Failure(SandikSevkKilidiHelper.SandikKilitliMesaji);

            var yeni = SandikIcerikUrunBilgisiKurali.Normalize(request.UrunBilgisi);
            if (!SandikIcerikUrunBilgisiKurali.Gecerli(yeni))
                return Result.Failure(SandikIcerikUrunBilgisiKurali.UzunlukMesaji);
            var eski = icerik.UrunBilgisi;
            if (string.Equals(eski, yeni, StringComparison.Ordinal))
                return Result.Success();

            icerik.UrunBilgisi = yeni;
            await unitOfWork.SaveChangesAsync(cancellationToken);

            await hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                ReferansTipi = "SandikIcerik",
                ReferansId = icerik.Id.ToString(),
                Islem = "Ürün Bilgisi Güncellendi",
                IslemTipiId = (int)IslemTipi.NotEklendi,
                EskiDeger = eski,
                YeniDeger = yeni,
                KullaniciId = currentUser.IslemKullaniciId ?? 0,
                Aciklama = $"Sandık: {sandik.SandikNo}"
            });
            return Result.Success();
        }
    }
}
