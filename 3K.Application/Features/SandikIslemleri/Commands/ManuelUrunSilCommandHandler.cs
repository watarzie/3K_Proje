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
                return await SahaYedekUrunSil(request, cancellationToken);
            }

            // ===== CASE 2: Normal proje manuel ürünleri — CekiSatiriId ile silme =====
            if (request.CekiSatiriId.HasValue && request.CekiSatiriId.Value > 0)
            {
                return await NormalManuelUrunSil(request, cancellationToken);
            }

            return Result.Failure("CekiSatiriId veya SandikIcerikId belirtilmelidir.");
        }

        /// <summary>
        /// Saha/Yedek projelerindeki ürünleri siler.
        /// Yalnız gerçekten manuel eklenen kayıtları siler. ÇEKİ/import kaynaklı bağlı
        /// satırlar aynı endpoint çağrılsa bile silinemez.
        /// </summary>
        private async Task<Result> SahaYedekUrunSil(
            ManuelUrunSilCommand request,
            CancellationToken cancellationToken)
        {
            var sandikIcerikRepo = _unitOfWork.GetRepository<SandikIcerik>();
            var icerik = await sandikIcerikRepo.GetByIdAsync(request.SandikIcerikId!.Value);

            if (icerik == null)
                return Result.Failure("Ürün bulunamadı.", 404);

            // Hedef sandık Saha/Yedek ise ürün kaynak çeki satırından bağımsız yönetilir.
            var sandikRepo = _unitOfWork.GetRepository<Sandik>();
            var sandik = await sandikRepo.GetByIdAsync(icerik.SandikId);
            if (sandik == null || sandik.ProjeId != request.ProjeId)
                return Result.Failure("Sandık bulunamadı veya projeye ait değil.");

            var projeRepo = _unitOfWork.GetRepository<Proje>();
            var proje = await projeRepo.GetByIdAsync(sandik.ProjeId);
            var isSahaYedek = proje != null &&
                (proje.ProjeTipiId == (int)ProjeTipi.Saha || proje.ProjeTipiId == (int)ProjeTipi.Yedek);

            if (!isSahaYedek)
                return Result.Failure("Bu silme işlemi yalnızca Saha/Yedek projelerinde kullanılabilir.", 409);

            if (SandikSevkKilidiHelper.SandikKilitliMi(sandik))
                return Result.Failure("Sevk edilmiş sandıktan ürün silinemez.");

            var urunBilgi = $"{icerik.BarkodNo ?? "-"} - {icerik.Isim ?? "-"} ({icerik.Miktar} adet)";

            CekiSatiri? bagliSatir = null;
            var cekiSatiriRepo = _unitOfWork.GetRepository<CekiSatiri>();
            if (icerik.CekiSatiriId.HasValue)
                bagliSatir = await cekiSatiriRepo.GetByIdAsync(icerik.CekiSatiriId.Value);

            if (icerik.CekiSatiriId.HasValue && bagliSatir == null)
            {
                return Result.Failure(
                    "Ürünün bağlı çeki satırı bulunamadı. Veri bütünlüğü kontrol edilmeden ürün silinemez.",
                    409);
            }

            if (bagliSatir is { IsManuelEklenen: false })
            {
                return Result.Failure(
                    "Bu ürün ÇEKİ dosyasından gelmiştir. Yalnızca manuel eklenen ürünler silinebilir.",
                    409);
            }

            if (bagliSatir != null)
            {
                var bagliIcerikler = await sandikIcerikRepo.FindAsync(x => x.CekiSatiriId == bagliSatir.Id);
                foreach (var bagliIcerik in bagliIcerikler)
                {
                    // Seçili içerik GetByIdAsync ile zaten tracked. FindAsync AsNoTracking
                    // kopyasını aynı anahtarla attach etmek yerine tracked örneği sileriz.
                    sandikIcerikRepo.Remove(bagliIcerik.Id == icerik.Id ? icerik : bagliIcerik);
                }

                cekiSatiriRepo.Remove(bagliSatir);
            }
            else
            {
                sandikIcerikRepo.Remove(icerik);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                KullaniciId = _currentUserService.UserId ?? 0,
                ReferansTipi = "SandikIcerik",
                ReferansId = request.SandikIcerikId!.Value.ToString(),
                Islem = "Manuel Ürün Silindi",
                IslemTipiId = (int)IslemTipi.ManuelUrunSilindi,
                ReferansMetni = urunBilgi,
                Aciklama = $"Saha/Yedek ürün silindi: {urunBilgi}"
            });

            return Result.Success();
        }

        /// <summary>
        /// Normal projelerdeki manuel eklenen ürünleri siler.
        /// IsManuelEklenen=true ve üzerinde işlem yapılmamış olmalıdır.
        /// </summary>
        private async Task<Result> NormalManuelUrunSil(
            ManuelUrunSilCommand request,
            CancellationToken cancellationToken)
        {
            var cekiSatiriRepo = _unitOfWork.GetRepository<CekiSatiri>();
            var satir = await cekiSatiriRepo.GetByIdAsync(request.CekiSatiriId!.Value);

            if (satir == null)
                return Result.Failure("Ürün bulunamadı.", 404);

            if (await SandikSevkKilidiHelper.CekiSatiriSevkEdilmisSandiktaMiAsync(_unitOfWork, satir))
                return Result.Failure(SandikSevkKilidiHelper.UrunKilitliMesaji);

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

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Hareket kaydı
            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                KullaniciId = _currentUserService.UserId ?? 0,
                ReferansTipi = "CekiSatiri",
                ReferansId = request.CekiSatiriId!.Value.ToString(),
                Islem = "Manuel Ürün Silindi",
                IslemTipiId = (int)IslemTipi.ManuelUrunSilindi,
                ReferansMetni = urunBilgi,
                Aciklama = $"Manuel eklenen ürün silindi: {urunBilgi}"
            });

            return Result.Success();
        }
    }
}
