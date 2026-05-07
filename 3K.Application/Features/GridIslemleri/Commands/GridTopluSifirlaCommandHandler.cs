using MediatR;
using _3K.Core.Enums;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.GridIslemleri.Commands
{
    /// <summary>
    /// Seçili ürünlerin Grid durumlarını toplu sıfırlar — her satır için tekli sıfırlama mantığı uygulanır.
    /// </summary>
    public class GridTopluSifirlaCommandHandler : IRequestHandler<GridTopluSifirlaCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IDurumHesaplaService _durumHesaplaService;
        private readonly IHareketService _hareketService;

        public GridTopluSifirlaCommandHandler(
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

        public async Task<Result> Handle(GridTopluSifirlaCommand request, CancellationToken cancellationToken)
        {
            if (request.CekiSatiriIdler == null || request.CekiSatiriIdler.Count == 0)
                return Result.Failure("En az bir ürün seçilmelidir.", 400);

            var repo = _unitOfWork.GetRepository<CekiSatiri>();
            var satirlar = await repo.FindAsync(cs => request.CekiSatiriIdler.Contains(cs.Id));

            if (!satirlar.Any())
                return Result.Failure("Seçilen ürünler bulunamadı.", 404);

            var kullaniciId = _currentUserService.UserId ?? 0;
            int basarili = 0;
            var hatalar = new List<string>();

            foreach (var satir in satirlar)
            {
                // 3K işlem yapılmışsa sıfırlama engelle
                if (satir.UcKDurumuId != (int)UcKDurum.Bekliyor || satir.GelenMiktar > 0 || satir.KarsilananMiktar > 0)
                {
                    hatalar.Add($"#{satir.SiraNo}: 3K işlem yapılmış.");
                    continue;
                }

                // Zaten sıfırlanmış mı?
                if (satir.GridDurumuId == (int)GridDurum.Gelmedi
                    && satir.GridGelenAdet == 0
                    && satir.TrafoSevkAdet == 0
                    && satir.GridSevkDurumuId == (int)GridSevkDurum.SevkEdilmedi
                    && satir.YenidenSevkGerekliAdet == 0)
                {
                    continue; // Zaten sıfır, sessizce atla
                }

                var eskiGridDurum = satir.GridDurumuId;

                // Sıfırla
                satir.GridDurumuId = (int)GridDurum.Gelmedi;
                satir.GridGelenAdet = 0;
                satir.TrafoSevkAdet = 0;
                satir.GridSevkDurumuId = (int)GridSevkDurum.SevkEdilmedi;
                satir.GridSevkMiktari = null;
                satir.YenidenSevkGerekliAdet = 0;
                satir.GridSevkTarihi = null;
                satir.GridPersonelId = null;
                satir.GridAciklama = null;

                // Genel durumu hesapla
                satir.DurumId = _durumHesaplaService.HesaplaGenelDurum(satir.GridDurumuId, satir.UcKDurumuId);
                _durumHesaplaService.HesaplaKalanVeDurum(satir);

                repo.Update(satir);
                basarili++;

                // Hareket kaydı
                await _hareketService.HareketKaydetAsync(new HareketGecmisi
                {
                    ProjeId = request.ProjeId,
                    KullaniciId = kullaniciId,
                    ReferansTipi = "CekiSatiri",
                    ReferansId = satir.Id.ToString(),
                    Islem = "Grid Toplu Sıfırlandı",
                    IslemTipiId = (int)IslemTipi.GridDurumSifirlandi,
                    EskiDeger = $"GridDurum:{eskiGridDurum}",
                    YeniDeger = "Bekliyor (Sıfırlandı)",
                    Aciklama = $"Toplu Grid sıfırlama — {(string.IsNullOrWhiteSpace(request.Aciklama) ? "Açıklama yok" : request.Aciklama)}"
                });
            }

            if (basarili == 0)
                return Result.Failure("Hiçbir ürün sıfırlanamadı. " + (hatalar.Any() ? string.Join("; ", hatalar.Take(3)) : ""));

            await _unitOfWork.SaveChangesAsync();

            if (hatalar.Any())
                return Result.Success();

            return Result.Success();
        }
    }
}
