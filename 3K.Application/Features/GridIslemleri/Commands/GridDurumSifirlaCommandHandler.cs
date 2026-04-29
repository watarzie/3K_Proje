using MediatR;
using _3K.Core.Enums;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.GridIslemleri.Commands
{
    /// <summary>
    /// Grid personeli durum seçimini sıfırlar — ürünü çeki yüklendiğindeki ham durumuna döndürür.
    /// GridDurumuId, GridGelenAdet, TrafoSevkAdet, GridSevkDurumuId, GridSevkMiktari vb. sıfırlanır.
    /// GenelDurum yeniden hesaplanır.
    /// </summary>
    public class GridDurumSifirlaCommandHandler : IRequestHandler<GridDurumSifirlaCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IDurumHesaplaService _durumHesaplaService;
        private readonly IHareketService _hareketService;

        public GridDurumSifirlaCommandHandler(
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

        public async Task<Result> Handle(GridDurumSifirlaCommand request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.GetRepository<CekiSatiri>();
            var satir = await repo.GetByIdAsync(request.CekiSatiriId);

            if (satir == null)
                return Result.Failure("Ürün bulunamadı.", 404);

            // 3K tarafı işlem yapmışsa sıfırlama engelle
            if (satir.UcKDurumuId != (int)UcKDurum.Bekliyor || satir.GelenMiktar > 0)
                return Result.Failure("Bu ürün için 3K tarafında işlem yapılmış. Önce 3K durumunu sıfırlayın.");

            // Zaten sıfırlanmış mı kontrol et
            if (satir.GridDurumuId == (int)GridDurum.Gelmedi
                && satir.GridGelenAdet == 0
                && satir.TrafoSevkAdet == 0
                && satir.GridSevkDurumuId == (int)GridSevkDurum.SevkEdilmedi)
                return Result.Failure("Bu ürün zaten sıfırlanmış durumda.");

            // ===== Eski değerleri kaydet (hareket logu için) =====
            var eskiGridDurum = satir.GridDurumuId;
            var eskiGridGelenAdet = satir.GridGelenAdet;
            var eskiTrafoSevkAdet = satir.TrafoSevkAdet;
            var eskiSevkDurum = satir.GridSevkDurumuId;
            var eskiSevkMiktari = satir.GridSevkMiktari;

            // ===== Grid alanlarını sıfırla (çeki yüklendiğindeki ham durum) =====
            satir.GridDurumuId = (int)GridDurum.Gelmedi;
            satir.GridGelenAdet = 0;
            satir.TrafoSevkAdet = 0;
            satir.GridSevkDurumuId = (int)GridSevkDurum.SevkEdilmedi;
            satir.GridSevkMiktari = null;
            satir.GridSevkTarihi = null;
            satir.GridPersonelId = null;
            satir.GridAciklama = null;

            // ===== Genel durumu yeniden hesapla =====
            satir.DurumId = _durumHesaplaService.HesaplaGenelDurum(satir.GridDurumuId, satir.UcKDurumuId);
            _durumHesaplaService.HesaplaKalanVeDurum(satir);

            repo.Update(satir);
            await _unitOfWork.SaveChangesAsync();

            // ===== Hareket kaydı =====
            var detay = $"Grid Sıfırlandı: " +
                $"GridDurum:{eskiGridDurum}→Bekliyor, " +
                $"GridGelenAdet:{eskiGridGelenAdet}→0, " +
                $"TrafoSevkAdet:{eskiTrafoSevkAdet}→0, " +
                $"SevkDurum:{eskiSevkDurum}→SevkEdilmedi, " +
                $"SevkMiktari:{eskiSevkMiktari?.ToString() ?? "null"}→null";

            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                KullaniciId = _currentUserService.UserId ?? 0,
                ReferansTipi = "CekiSatiri",
                ReferansId = satir.Id.ToString(),
                Islem = "Grid Durum Sıfırlandı",
                IslemTipiId = (int)IslemTipi.GridDurumSifirlandi,
                EskiDeger = $"GridDurum:{eskiGridDurum}, GridGelenAdet:{eskiGridGelenAdet}, SevkDurum:{eskiSevkDurum}",
                YeniDeger = "Bekliyor (Sıfırlandı)",
                Aciklama = string.IsNullOrWhiteSpace(request.Aciklama)
                    ? detay
                    : $"{request.Aciklama} | {detay}"
            });

            return Result.Success();
        }
    }
}
