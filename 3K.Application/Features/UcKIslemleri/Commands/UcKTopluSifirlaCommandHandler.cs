using MediatR;
using _3K.Core.Enums;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.UcKIslemleri.Commands
{
    /// <summary>
    /// Seçili ürünlerin 3K durumlarını toplu sıfırlar — tekli sıfırlama mantığı toplu uygulanır.
    /// </summary>
    public class UcKTopluSifirlaCommandHandler : IRequestHandler<UcKTopluSifirlaCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IDurumHesaplaService _durumHesaplaService;
        private readonly IHareketService _hareketService;

        public UcKTopluSifirlaCommandHandler(
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

        public async Task<Result> Handle(UcKTopluSifirlaCommand request, CancellationToken cancellationToken)
        {
            if (request.CekiSatiriIdler == null || request.CekiSatiriIdler.Count == 0)
                return Result.Failure("En az bir ürün seçilmelidir.", 400);

            var repo = _unitOfWork.GetRepository<CekiSatiri>();
            var sandikIcerikRepo = _unitOfWork.GetRepository<SandikIcerik>();
            var satirlar = await repo.FindAsync(cs => request.CekiSatiriIdler.Contains(cs.Id));

            if (!satirlar.Any())
                return Result.Failure("Seçilen ürünler bulunamadı.", 404);

            var kullaniciId = _currentUserService.UserId ?? 0;
            int basarili = 0;
            var hatalar = new List<string>();
            var kilitliSatirIdleri = await SandikSevkKilidiHelper.GetSevkEdilmisSandikCekiSatiriIdleriAsync(
                _unitOfWork,
                satirlar.Select(s => s.Id));

            foreach (var satir in satirlar)
            {
                if (kilitliSatirIdleri.Contains(satir.Id))
                {
                    hatalar.Add($"#{satir.SiraNo}: {SandikSevkKilidiHelper.UrunKilitliMesaji}");
                    continue;
                }

                // Grid İptal blokajı
                if (satir.GridDurumuId == (int)GridDurum.Iptal)
                {
                    hatalar.Add($"#{satir.SiraNo}: Grid İptal.");
                    continue;
                }

                // Zaten başlangıç durumundaysa atla
                if (satir.UcKDurumuId == (int)UcKDurum.Bekliyor
                    && satir.UcKKarsilamaTipiId == (int)UcKDurum.Bekliyor
                    && satir.GelenMiktar == 0
                    && satir.KarsilananMiktar == 0
                    && satir.StokKarsilanan == 0
                    && satir.ProjeKarsilanan == 0
                    && satir.TedarikciKarsilanan == 0
                    && satir.HataliMiktar == 0
                    && satir.GeriGonderilenMiktar == 0
                    && satir.YenidenSevkGerekliAdet == 0)
                {
                    continue; // Zaten sıfır, atla
                }

                var eskiDurum = satir.UcKDurumuId;

                var stokGeriAlSonucu = await UcKStokHareketGeriAlHelper.GeriAlAsync(_unitOfWork, satir.Id);
                if (!stokGeriAlSonucu.IsSuccess)
                {
                    hatalar.Add($"#{satir.SiraNo}: {stokGeriAlSonucu.Error?.Message ?? "Stok hareketi geri alınamadı."}");
                    continue;
                }

                // 3K alanlarını sıfırla
                satir.UcKDurumuId = (int)UcKDurum.Bekliyor;
                satir.UcKKarsilamaTipiId = (int)UcKDurum.Bekliyor;
                satir.GelenMiktar = 0;
                satir.KarsilananMiktar = 0;
                satir.StokKarsilanan = 0;
                satir.ProjeKarsilanan = 0;
                satir.TedarikciKarsilanan = 0;
                satir.HataliMiktar = 0;
                satir.GeriGonderilenMiktar = 0;
                satir.TeslimTarihi = null;
                satir.UcKAciklama = null;
                satir.KaynakHedefProjeNo = null;
                satir.KaynakProjeId = null;
                satir.GeriGonderilmeSebebiId = null;
                satir.YenidenSevkGerekliAdet = 0;
                if (satir.GridSevkDurumuId == (int)GridSevkDurum.YenidenSevkGerekli)
                    satir.GridSevkDurumuId = (int)GridSevkDurum.SevkEdildi;

                // Genel durumu hesapla
                satir.DurumId = _durumHesaplaService.HesaplaGenelDurum(satir.GridDurumuId, satir.UcKDurumuId);
                _durumHesaplaService.HesaplaKalanVeDurum(satir);

                repo.Update(satir);

                // SandıkIçerik senkronizasyonu
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

                basarili++;

                // Hareket kaydı
                await _hareketService.HareketKaydetAsync(new HareketGecmisi
                {
                    ProjeId = request.ProjeId,
                    KullaniciId = kullaniciId,
                    ReferansTipi = "CekiSatiri",
                    ReferansId = satir.Id.ToString(),
                    Islem = "3K Toplu Sıfırlandı",
                    IslemTipiId = (int)IslemTipi.UcKDurumSifirlandi,
                    EskiDeger = $"UcKDurum:{eskiDurum}",
                    YeniDeger = "Bekliyor (Sıfırlandı)",
                    Aciklama = $"Toplu 3K sıfırlama — {(string.IsNullOrWhiteSpace(request.Aciklama) ? "Açıklama yok" : request.Aciklama)}"
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
