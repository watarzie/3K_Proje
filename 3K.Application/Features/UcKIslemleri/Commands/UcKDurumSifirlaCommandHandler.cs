using MediatR;
using _3K.Core.Enums;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.UcKIslemleri.Commands
{
    /// <summary>
    /// 3K karşılama seçimini sıfırlar — ürünü ilk başlangıç (ham) durumuna döndürür.
    /// GelenMiktar, KarsilananMiktar, parçalı karşılama alanları, HataliMiktar vb. sıfırlanır.
    /// GenelDurum yeniden hesaplanır.
    /// </summary>
    public class UcKDurumSifirlaCommandHandler : IRequestHandler<UcKDurumSifirlaCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IDurumHesaplaService _durumHesaplaService;
        private readonly IHareketService _hareketService;

        public UcKDurumSifirlaCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IDurumHesaplaService durumHesaplaService,
            IHareketService hareketService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _durumHesaplaService = durumHesaplaService;
            _hareketService = hareketService;
        }

        public async Task<Result> Handle(UcKDurumSifirlaCommand request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.GetRepository<CekiSatiri>();
            var satir = await repo.GetByIdAsync(request.CekiSatiriId);

            if (satir == null)
                return Result.Failure("Ürün bulunamadı.", 404);

            // Grid İptal blokajı
            if (satir.GridDurumuId == (int)GridDurum.Iptal)
                return Result.Failure("Bu ürün Grid tarafından iptal edildiği için sıfırlama yapılamaz.");

            // Zaten ham/başlangıç durumundaysa → sıfırlanacak bir şey yok
            if (satir.UcKDurumuId == (int)UcKDurum.Bekliyor
                && satir.UcKKarsilamaTipiId == (int)UcKDurum.Bekliyor
                && satir.GelenMiktar == 0
                && satir.KarsilananMiktar == 0
                && satir.StokKarsilanan == 0
                && satir.ProjeKarsilanan == 0
                && satir.TedarikciKarsilanan == 0
                && satir.HataliMiktar == 0)
                return Result.Failure("Bu ürün zaten başlangıç durumunda.");

            // ===== Eski değerleri kaydet (hareket logu için) =====
            var eskiDurum = satir.UcKDurumuId;
            var eskiKarsilamaTipi = satir.UcKKarsilamaTipiId;
            var eskiGelenMiktar = satir.GelenMiktar;
            var eskiKarsilananMiktar = satir.KarsilananMiktar;
            var eskiStokKarsilanan = satir.StokKarsilanan;
            var eskiProjeKarsilanan = satir.ProjeKarsilanan;
            var eskiTedarikciKarsilanan = satir.TedarikciKarsilanan;
            var eskiHataliMiktar = satir.HataliMiktar;

            // ===== 3K alanlarını sıfırla =====
            satir.UcKDurumuId = (int)UcKDurum.Bekliyor;
            satir.UcKKarsilamaTipiId = (int)UcKDurum.Bekliyor;
            satir.GelenMiktar = 0;
            satir.KarsilananMiktar = 0;
            satir.StokKarsilanan = 0;
            satir.ProjeKarsilanan = 0;
            satir.TedarikciKarsilanan = 0;
            satir.HataliMiktar = 0;
            satir.TeslimTarihi = null;
            satir.UcKAciklama = null;
            satir.KaynakHedefProjeNo = null;
            satir.KaynakProjeId = null;
            satir.GeriGonderilmeSebebiId = null;

            // ===== Genel durumu yeniden hesapla =====
            satir.DurumId = _durumHesaplaService.HesaplaGenelDurum(satir.GridDurumuId, satir.UcKDurumuId);
            _durumHesaplaService.HesaplaKalanVeDurum(satir);

            repo.Update(satir);

            // ===== SandıkIçerik senkronizasyonu — konulan adeti de sıfırla =====
            var sandikIcerikRepo = _unitOfWork.GetRepository<SandikIcerik>();
            var ilgiliIcerikler = await sandikIcerikRepo.FindAsync(x => x.CekiSatiriId == satir.Id);
            foreach (var icerik in ilgiliIcerikler)
            {
                icerik.KonulanAdet = 0;
                icerik.EksikAdet = 0;
                icerik.StokKarsilanan = 0;
                icerik.ProjeKarsilanan = 0;
                icerik.TedarikciKarsilanan = 0;
                sandikIcerikRepo.Update(icerik);
            }

            await _unitOfWork.SaveChangesAsync();

            // ===== Hareket kaydı =====
            var detay = $"3K Sıfırlandı: " +
                $"UcKDurum:{eskiDurum}→Bekliyor, " +
                $"GelenMiktar:{eskiGelenMiktar}→0, " +
                $"StokKarsilanan:{eskiStokKarsilanan}→0, " +
                $"ProjeKarsilanan:{eskiProjeKarsilanan}→0, " +
                $"TedarikciKarsilanan:{eskiTedarikciKarsilanan}→0, " +
                $"HataliMiktar:{eskiHataliMiktar}→0";

            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                KullaniciId = _currentUserService.UserId ?? 0,
                ReferansTipi = "CekiSatiri",
                ReferansId = satir.Id.ToString(),
                Islem = "3K Durum Sıfırlandı",
                IslemTipiId = (int)IslemTipi.UcKDurumSifirlandi,
                EskiDeger = $"KarsilamaTipi:{eskiKarsilamaTipi}, UcKDurum:{eskiDurum}, GelenMiktar:{eskiGelenMiktar}",
                YeniDeger = "Bekliyor (Sıfırlandı)",
                Aciklama = string.IsNullOrWhiteSpace(request.Aciklama)
                    ? detay
                    : $"{request.Aciklama} | {detay}"
            });

            return Result.Success();
        }
    }
}
