using MediatR;
using _3K.Core.Enums;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;
using _3K.Core.Helpers;

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
        private readonly ISahaTamamlamaService _sahaTamamlamaService;

        public UcKDurumSifirlaCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IDurumHesaplaService durumHesaplaService,
            IHareketService hareketService,
            ISahaTamamlamaService sahaTamamlamaService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _durumHesaplaService = durumHesaplaService;
            _hareketService = hareketService;
            _sahaTamamlamaService = sahaTamamlamaService;
        }

        public async Task<Result> Handle(UcKDurumSifirlaCommand request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.GetRepository<CekiSatiri>();
            var satir = await repo.GetByIdAsync(request.CekiSatiriId);

            if (satir == null)
                return Result.Failure("Ürün bulunamadı.", 404);

            var seciliIcerikResult = await UcKSandikIcerikSenkronizasyonHelper.GetSeciliIcerikAsync(
                _unitOfWork,
                satir.Id,
                request.SandikIcerikId);
            if (!seciliIcerikResult.IsSuccess)
                return Result.Failure(seciliIcerikResult.Error!.Message, seciliIcerikResult.StatusCode);

            var seciliIcerik = seciliIcerikResult.Value;
            if (seciliIcerik != null && (seciliIcerik.StokKarsilanan > 0 || seciliIcerik.ProjeKarsilanan > 0))
            {
                return Result.Failure(
                    "Stoktan veya projeden karşılanan parçanın yalnız bir sandık için geri alınması güvenli değildir. Önce ilgili kaynak hareketini geri alın veya ürünün tüm 3K işlemini sıfırlayın.");
            }

            if (await SahaAktarimBlokajHelper.KaynakSatirAktarildiMiAsync(_sahaTamamlamaService, satir, cancellationToken))
                return Result.Failure(SahaAktarimBlokajHelper.UcKMesaji);

            if (await SandikSevkKilidiHelper.CekiSatiriSevkEdilmisSandiktaMiAsync(_unitOfWork, satir))
                return Result.Failure(SandikSevkKilidiHelper.UrunKilitliMesaji);

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
                && satir.ProjeGonderilen == 0
                && satir.TedarikciKarsilanan == 0
                && satir.HataliMiktar == 0
                && satir.GeriGonderilenMiktar == 0
                && satir.YenidenSevkGerekliAdet == 0)
                return Result.Failure("Bu ürün zaten başlangıç durumunda.");

            // ===== Eski değerleri kaydet (hareket logu için) =====
            var transferRepo = _unitOfWork.GetRepository<ProjeTransfer>();
            var aktifTransferler = (await transferRepo.FindAsync(t =>
                t.DurumId == (int)ProjeTransferDurum.Aktif &&
                (t.KaynakCekiSatiriId == satir.Id || t.HedefCekiSatiriId == satir.Id)))
                .ToList();

            var aktifGidenTransferler = aktifTransferler.Where(t => t.KaynakCekiSatiriId == satir.Id).ToList();
            if (aktifGidenTransferler.Any())
                return Result.Failure("Bu urun baska projeye kaynak olarak verilmis. Once hedef projedeki karsilama geri alinmalidir.");

            var aktifGelenTransferler = aktifTransferler.Where(t => t.HedefCekiSatiriId == satir.Id).ToList();

            var eskiDurum = satir.UcKDurumuId;
            var eskiKarsilamaTipi = satir.UcKKarsilamaTipiId;
            var eskiGelenMiktar = satir.GelenMiktar;
            var eskiKarsilananMiktar = satir.KarsilananMiktar;
            var eskiStokKarsilanan = satir.StokKarsilanan;
            var eskiProjeKarsilanan = satir.ProjeKarsilanan;
            var eskiTedarikciKarsilanan = satir.TedarikciKarsilanan;
            var eskiHataliMiktar = satir.HataliMiktar;
            var eskiGeriGonderilenMiktar = satir.GeriGonderilenMiktar;

            if (seciliIcerik == null)
            {
                var stokGeriAlSonucu = await UcKStokHareketGeriAlHelper.GeriAlAsync(_unitOfWork, satir.Id);
                if (!stokGeriAlSonucu.IsSuccess)
                    return stokGeriAlSonucu;
            }

            // ===== 3K alanlarını sıfırla =====
            if (seciliIcerik == null)
            {
                satir.UcKDurumuId = (int)UcKDurum.Bekliyor;
                satir.UcKKarsilamaTipiId = (int)UcKDurum.Bekliyor;
                satir.GelenMiktar = 0;
                satir.KarsilananMiktar = 0;
                satir.StokKarsilanan = 0;
                satir.ProjeKarsilanan = 0;
                satir.ProjeGonderilen = 0;
                satir.TedarikciKarsilanan = 0;
                satir.HataliMiktar = 0;
                satir.GeriGonderilenMiktar = 0;
                satir.TeslimTarihi = null;
                satir.UcKAciklama = null;
                satir.KaynakHedefProjeNo = null;
                satir.KaynakProjeId = null;
                satir.GeriGonderilmeSebebiId = null;
                satir.YenidenSevkGerekliAdet = 0;
            }
            else
            {
                var sandikUcKGelen = Math.Max(
                    seciliIcerik.KonulanAdet - seciliIcerik.StokKarsilanan - seciliIcerik.ProjeKarsilanan - seciliIcerik.TedarikciKarsilanan,
                    0);
                satir.GelenMiktar = Math.Max(satir.GelenMiktar - sandikUcKGelen, 0);
                satir.TedarikciKarsilanan = Math.Max(satir.TedarikciKarsilanan - seciliIcerik.TedarikciKarsilanan, 0);
                satir.KarsilananMiktar = satir.StokKarsilanan + satir.ProjeKarsilanan + satir.TedarikciKarsilanan;

                var kalanTamamlanan = satir.GelenMiktar + satir.KarsilananMiktar - satir.ProjeGonderilen;
                satir.UcKDurumuId = kalanTamamlanan > 0 ? (int)UcKDurum.EksikGeldi : (int)UcKDurum.Bekliyor;
                satir.UcKKarsilamaTipiId = satir.UcKDurumuId;
                if (kalanTamamlanan <= 0)
                {
                    satir.TeslimTarihi = null;
                    satir.UcKAciklama = null;
                }
            }
            if (satir.GridSevkDurumuId == (int)GridSevkDurum.YenidenSevkGerekli)
                satir.GridSevkDurumuId = (int)GridSevkDurum.SevkEdildi;

            // ===== Genel durumu yeniden hesapla =====
            satir.DurumId = _durumHesaplaService.HesaplaGenelDurum(satir.GridDurumuId, satir.UcKDurumuId);
            _durumHesaplaService.HesaplaKalanVeDurum(satir);

            repo.Update(satir);

            // ===== SandıkIçerik senkronizasyonu — konulan adeti de sıfırla =====
            foreach (var transfer in seciliIcerik == null ? aktifGelenTransferler : Enumerable.Empty<ProjeTransfer>())
            {
                var kaynakSatir = await repo.GetByIdAsync(transfer.KaynakCekiSatiriId);
                if (kaynakSatir != null)
                {
                    kaynakSatir.ProjeGonderilen = Math.Max(kaynakSatir.ProjeGonderilen - transfer.Miktar, 0);
                    kaynakSatir.DurumId = _durumHesaplaService.HesaplaGenelDurum(kaynakSatir.GridDurumuId, kaynakSatir.UcKDurumuId);
                    _durumHesaplaService.HesaplaKalanVeDurum(kaynakSatir);
                    repo.Update(kaynakSatir);
                }

                transfer.DurumId = (int)ProjeTransferDurum.GeriAlindi;
                transfer.IptalTarihi = TurkeyTime.Now;
                transfer.IptalAciklama = "3K durumu geri alindigi icin transfer pasife cekildi.";
                transferRepo.Update(transfer);
            }

            var senkronizasyonResult = await UcKSandikIcerikSenkronizasyonHelper.SenkronizeAsync(
                _unitOfWork,
                satir,
                request.SandikIcerikId);
            if (!senkronizasyonResult.IsSuccess)
                return Result.Failure(senkronizasyonResult.Error!.Message, senkronizasyonResult.StatusCode);

            await _unitOfWork.SaveChangesAsync();

            if (satir.KaynakCekiSatiriId.HasValue)
                await _sahaTamamlamaService.SenkronizeKaynakProjelerAsync(new[] { satir.KaynakCekiSatiriId.Value }, cancellationToken);

            // ===== Hareket kaydı =====
            var detay = seciliIcerik == null
                ? $"3K Sıfırlandı: UcKDurum:{eskiDurum}→Bekliyor, " +
                  $"GelenMiktar:{eskiGelenMiktar}→0, StokKarsilanan:{eskiStokKarsilanan}→0, " +
                  $"ProjeKarsilanan:{eskiProjeKarsilanan}→0, TedarikciKarsilanan:{eskiTedarikciKarsilanan}→0, " +
                  $"GeriGonderilen:{eskiGeriGonderilenMiktar}→0, HataliMiktar:{eskiHataliMiktar}→0"
                : $"Sandık bazlı 3K sıfırlandı: SandikIcerikId:{seciliIcerik.Id}, " +
                  $"SandıkMiktarı:{seciliIcerik.TahsisMiktari}, Konulan:{seciliIcerik.KonulanAdet}→0";

            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                KullaniciId = _currentUserService.UserId ?? 0,
                ReferansTipi = seciliIcerik == null ? "CekiSatiri" : "SandikIcerik",
                ReferansId = (seciliIcerik?.Id ?? satir.Id).ToString(),
                Islem = seciliIcerik == null ? "3K Durum Sıfırlandı" : "Sandık Bazlı 3K Durum Sıfırlandı",
                IslemTipiId = (int)IslemTipi.UcKDurumSifirlandi,
                EskiDeger = $"KarsilamaTipi:{eskiKarsilamaTipi}, UcKDurum:{eskiDurum}, GelenMiktar:{eskiGelenMiktar}",
                YeniDeger = seciliIcerik == null ? "Bekliyor (Sıfırlandı)" : "Seçili sandık tahsisi sıfırlandı",
                Aciklama = string.IsNullOrWhiteSpace(request.Aciklama)
                    ? detay
                    : $"{request.Aciklama} | {detay}"
            });

            return Result.Success();
        }
    }
}
