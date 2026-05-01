using MediatR;
using _3K.Core.Entities;
using _3K.Core.Interfaces;
using _3K.Core.Enums;
using _3K.Application.Common;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    public class ManuelUrunSilCommandHandler : IRequestHandler<ManuelUrunSilCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHareketService _hareketService;
        private readonly ICurrentUserService _currentUserService;

        public ManuelUrunSilCommandHandler(IUnitOfWork unitOfWork, IHareketService hareketService, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _hareketService = hareketService;
            _currentUserService = currentUserService;
        }

        public async Task<Result> Handle(ManuelUrunSilCommand request, CancellationToken cancellationToken)
        {
            // ===== CASE 1: Saha/Yedek ürünleri — SandikIcerikId ile silme =====
            if (request.SandikIcerikId.HasValue && request.SandikIcerikId.Value > 0)
            {
                return await SahaYedekUrunSil(request);
            }

            // ===== CASE 2: Normal proje manuel ürünleri — CekiSatiriId ile silme =====
            if (request.CekiSatiriId.HasValue && request.CekiSatiriId.Value > 0)
            {
                return await NormalManuelUrunSil(request);
            }

            return Result.Failure("CekiSatiriId veya SandikIcerikId belirtilmelidir.");
        }

        /// <summary>
        /// Saha/Yedek projelerindeki ürünleri siler.
        /// Bu ürünlerin CekiSatiri bağlantısı yoktur, doğrudan SandikIcerik tablosundadır.
        /// </summary>
        private async Task<Result> SahaYedekUrunSil(ManuelUrunSilCommand request)
        {
            var sandikIcerikRepo = _unitOfWork.GetRepository<SandikIcerik>();
            var icerik = await sandikIcerikRepo.GetByIdAsync(request.SandikIcerikId!.Value);

            if (icerik == null)
                return Result.Failure("Ürün bulunamadı.", 404);

            // CekiSatiri bağlantısı varsa buradan silinemez
            if (icerik.CekiSatiriId != null)
                return Result.Failure("Bu ürün çekiden gelmiştir. Saha/Yedek silme ile silinemez.");

            // Sandık projeye ait mi kontrol et
            var sandikRepo = _unitOfWork.GetRepository<Sandik>();
            var sandik = await sandikRepo.GetByIdAsync(icerik.SandikId);
            if (sandik == null || sandik.ProjeId != request.ProjeId)
                return Result.Failure("Sandık bulunamadı veya projeye ait değil.");

            var urunBilgi = $"{icerik.BarkodNo ?? "-"} - {icerik.Isim ?? "-"} ({icerik.Miktar} adet)";

            sandikIcerikRepo.Remove(icerik);
            await _unitOfWork.SaveChangesAsync();

            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                KullaniciId = _currentUserService.UserId ?? 0,
                ReferansTipi = "SandikIcerik",
                ReferansId = request.SandikIcerikId!.Value.ToString(),
                Islem = "Manuel Ürün Silindi",
                IslemTipiId = (int)IslemTipi.ManuelUrunSilindi,
                Aciklama = $"Saha/Yedek ürün silindi: {urunBilgi}"
            });

            return Result.Success();
        }

        /// <summary>
        /// Normal projelerdeki manuel eklenen ürünleri siler.
        /// IsManuelEklenen=true ve üzerinde işlem yapılmamış olmalıdır.
        /// </summary>
        private async Task<Result> NormalManuelUrunSil(ManuelUrunSilCommand request)
        {
            var cekiSatiriRepo = _unitOfWork.GetRepository<CekiSatiri>();
            var satir = await cekiSatiriRepo.GetByIdAsync(request.CekiSatiriId!.Value);

            if (satir == null)
                return Result.Failure("Ürün bulunamadı.", 404);

            if (!satir.IsManuelEklenen)
                return Result.Failure("Sadece manuel eklenen ürünler silinebilir. Çekiden gelen ürünler silinemez.");

            // Üzerinde işlem yapılmış mı kontrol et
            if (satir.GelenMiktar > 0 || satir.KarsilananMiktar > 0 || satir.HataliMiktar > 0)
                return Result.Failure("Bu ürün üzerinde işlem yapılmış (gelen/karşılanan/hatalı miktar mevcut). Silmeden önce işlemleri geri alın.");

            // İlişkili SandikIcerik kayıtlarını sil
            var sandikIcerikRepo = _unitOfWork.GetRepository<SandikIcerik>();
            var ilgiliIcerikler = await sandikIcerikRepo.FindAsync(x => x.CekiSatiriId == satir.Id);
            foreach (var icerik in ilgiliIcerikler)
            {
                sandikIcerikRepo.Remove(icerik);
            }

            // Ürün bilgilerini sakla (hareket kaydı için)
            var urunBilgi = $"{satir.BarkodNo} - {satir.Aciklama} ({satir.IstenenAdet} adet)";

            // CekiSatiri'ni sil
            cekiSatiriRepo.Remove(satir);

            await _unitOfWork.SaveChangesAsync();

            // Hareket kaydı
            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                KullaniciId = _currentUserService.UserId ?? 0,
                ReferansTipi = "CekiSatiri",
                ReferansId = request.CekiSatiriId!.Value.ToString(),
                Islem = "Manuel Ürün Silindi",
                IslemTipiId = (int)IslemTipi.ManuelUrunSilindi,
                Aciklama = $"Manuel eklenen ürün silindi: {urunBilgi}"
            });

            return Result.Success();
        }
    }
}
