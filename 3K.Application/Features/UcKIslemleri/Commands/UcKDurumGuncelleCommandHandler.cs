using MediatR;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.UcKIslemleri.Commands
{
    public class UcKDurumGuncelleCommandHandler : IRequestHandler<UcKDurumGuncelleCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IDurumHesaplaService _durumHesaplaService;
        private readonly IHareketService _hareketService;

        private static readonly string[] GecerliTipler =
            { StatusConstants.UcKDurum.TamGeldi, StatusConstants.UcKDurum.EksikGeldi, StatusConstants.UcKDurum.Gelmedi, StatusConstants.UcKDurum.ProjedenKarsilandi, StatusConstants.UrunDurum.StoktanKarsilandi,
              StatusConstants.UcKDurum.TedarikcidenGeldi, StatusConstants.UcKDurum.GeriGonderildi, StatusConstants.UcKDurum.HataliUrun };

        public UcKDurumGuncelleCommandHandler(
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

        public async Task<Result> Handle(UcKDurumGuncelleCommand request, CancellationToken cancellationToken)
        {
            if (!GecerliTipler.Contains(request.KarsilamaTipi))
                return Result.Failure($"Geçersiz karşılama tipi: {request.KarsilamaTipi}");

            var repo = _unitOfWork.GetRepository<CekiSatiri>();
            var satir = await repo.GetByIdAsync(request.CekiSatiriId);
            if (satir == null)
                return Result.Failure("Ürün bulunamadı.", 404);

            var eskiDurum = satir.UcKKarsilamaTipi;

            // ===== Grid İptal Blokajı =====
            if (satir.GridDurumu == StatusConstants.GridDurum.Iptal)
                return Result.Failure("Bu ürün Grid tarafından iptal edildiği için üzerinde hiçbir işlem yapılamaz.");

            // ===== Eksiksiz (Tam) Geldi Validasyonu =====
            if (request.KarsilamaTipi == StatusConstants.UcKDurum.TamGeldi && satir.GridDurumu != StatusConstants.GridDurum.SevkEdildi)
                return Result.Failure("Grid tarafından eksiksiz sevk edilmeden ürün 'Tam Geldi' olarak işaretlenemez.");

            // ===== Validasyon =====
            switch (request.KarsilamaTipi)
            {
                case StatusConstants.UcKDurum.EksikGeldi:
                    if (request.GelenAdet == null || request.GelenAdet <= 0)
                        return Result.Failure("Eksik geldi durumunda gelen adet girilmelidir.");
                    if (request.GelenAdet >= satir.IstenenAdet)
                        return Result.Failure("Gelen adet miktardan küçük olmalıdır.");
                    break;

                case StatusConstants.UcKDurum.ProjedenKarsilandi:
                    if (request.GelenAdet == null || request.GelenAdet <= 0)
                        return Result.Failure("Projeden karşılama adeti girilmelidir.");
                    if (string.IsNullOrWhiteSpace(request.KaynakHedefProjeNo))
                        return Result.Failure("Kaynak proje girilmelidir.");
                    break;

                case StatusConstants.UrunDurum.StoktanKarsilandi:
                case StatusConstants.UcKDurum.TedarikcidenGeldi:
                    if (request.GelenAdet == null || request.GelenAdet <= 0)
                        return Result.Failure("Gelen adet girilmelidir.");
                    break;

                case StatusConstants.UcKDurum.GeriGonderildi:
                    if (string.IsNullOrWhiteSpace(request.Aciklama))
                        return Result.Failure("Geri gönderilme sebebi girilmelidir.");
                    break;

                case StatusConstants.UcKDurum.HataliUrun:
                    if (request.GelenAdet == null || request.GelenAdet <= 0)
                        return Result.Failure("Gelen adet girilmelidir.");
                    if (string.IsNullOrWhiteSpace(request.Aciklama))
                        return Result.Failure("Hatalı ürün açıklaması girilmelidir.");
                    break;
            }

            // ===== Alanları güncelle =====
            satir.UcKKarsilamaTipi = request.KarsilamaTipi;
            satir.UcKAciklama = request.Aciklama;
            satir.UcKNotu = request.Not;
            satir.KaynakHedefProjeNo = request.KaynakHedefProjeNo;

            switch (request.KarsilamaTipi)
            {
                case StatusConstants.UcKDurum.TamGeldi:
                    satir.GelenMiktar = satir.IstenenAdet - satir.KarsilananMiktar;
                    satir.UcKDurumu = StatusConstants.UcKDurum.TamGeldi;
                    satir.TeslimTarihi = DateTime.UtcNow;
                    break;

                case StatusConstants.UcKDurum.EksikGeldi:
                    satir.GelenMiktar += request.GelenAdet!.Value;
                    satir.UcKDurumu = StatusConstants.UcKDurum.EksikGeldi;
                    satir.TeslimTarihi = DateTime.UtcNow;
                    break;

                case StatusConstants.UcKDurum.Gelmedi:
                    satir.UcKDurumu = StatusConstants.UcKDurum.Gelmedi;
                    satir.TeslimTarihi = DateTime.UtcNow;
                    break;

                case StatusConstants.UcKDurum.ProjedenKarsilandi:
                    satir.KarsilananMiktar += request.GelenAdet!.Value;
                    satir.UcKDurumu = StatusConstants.UcKDurum.ProjedenKarsilandi;
                    satir.TeslimTarihi = DateTime.UtcNow;
                    // Cross-project transfer
                    await HandleProjedenKarsilandi(satir, request);
                    break;

                case StatusConstants.UrunDurum.StoktanKarsilandi:
                    satir.KarsilananMiktar += request.GelenAdet!.Value;
                    satir.UcKDurumu = StatusConstants.UrunDurum.StoktanKarsilandi;
                    satir.TeslimTarihi = DateTime.UtcNow;
                    break;

                case StatusConstants.UcKDurum.TedarikcidenGeldi:
                    satir.KarsilananMiktar += request.GelenAdet!.Value;
                    satir.UcKDurumu = StatusConstants.UcKDurum.TedarikcidenGeldi;
                    satir.TeslimTarihi = DateTime.UtcNow;
                    break;

                case StatusConstants.UcKDurum.GeriGonderildi:
                    satir.UcKDurumu = StatusConstants.UcKDurum.GeriGonderildi;
                    satir.GeriGonderilmeSebebi = request.Aciklama;
                    break;

                case StatusConstants.UcKDurum.HataliUrun:
                    satir.HataliMiktar += request.GelenAdet!.Value;
                    satir.UcKDurumu = StatusConstants.UcKDurum.HataliUrun;
                    satir.TeslimTarihi = DateTime.UtcNow;
                    break;
            }

            // Toplam kontrol
            var toplam = satir.GelenMiktar + satir.KarsilananMiktar;
            if (toplam > satir.IstenenAdet)
                return Result.Failure($"Toplam tamamlanan adet ({toplam}), çeki miktarını ({satir.IstenenAdet}) aşamaz.");

            // Genel durumu hesapla
            satir.Durum = _durumHesaplaService.HesaplaGenelDurum(satir.GridDurumu, satir.UcKDurumu);

            repo.Update(satir);
            await _unitOfWork.SaveChangesAsync();

            // NOT: Sandık otomatik kapanma KALDIRILDI (İş Kuralı 7).
            // Sandık sadece Admin onayıyla SandikKapatCommand ile kapatılır.

            // Hareket kaydı
            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                KullaniciId = _currentUserService.UserId ?? 0,
                ReferansTipi = "CekiSatiri",
                ReferansId = satir.Id.ToString(),
                Islem = "3K Durum Güncellendi",
                EskiDeger = eskiDurum,
                YeniDeger = request.KarsilamaTipi,
                Aciklama = request.Aciklama
            });

            return Result.Success();
        }

        /// <summary>
        /// PROJEDEN KARŞILANDI: Kaynak projede eşleşen ürünü bul, adet düş, transfer kaydı oluştur.
        /// </summary>
        private async Task HandleProjedenKarsilandi(CekiSatiri hedefSatir, UcKDurumGuncelleCommand request)
        {
            if (request.KaynakCekiSatiriId == null) return;

            var cekiSatiriRepo = _unitOfWork.GetRepository<CekiSatiri>();
            var kaynakSatir = await cekiSatiriRepo.GetByIdAsync(request.KaynakCekiSatiriId.Value);

            if (kaynakSatir == null) return;

            // Kaynak projedeki ürünün fiziksel gelen stoğundan eksilt
            kaynakSatir.GelenMiktar = Math.Max(0, kaynakSatir.GelenMiktar - (request.GelenAdet ?? 0));
            cekiSatiriRepo.Update(kaynakSatir);

            // Kaynak projeyi bul
            var projeRepo = _unitOfWork.GetRepository<Proje>();
            var kaynakProjeler = await projeRepo.FindAsync(p => p.Cekiler.Any(c => c.Id == kaynakSatir.CekiId));
            var kaynakProje = kaynakProjeler.FirstOrDefault();

            var hedefProjeler = await projeRepo.FindAsync(p => p.Cekiler.Any(c => c.Id == hedefSatir.CekiId));
            var hedefProje = hedefProjeler.FirstOrDefault();

            if (kaynakProje != null && hedefProje != null)
            {
                var transferRepo = _unitOfWork.GetRepository<ProjeTransfer>();
                var transfer = new ProjeTransfer
                {
                    KaynakProjeId = kaynakProje.Id,
                    HedefProjeId = hedefProje.Id,
                    KaynakCekiSatiriId = kaynakSatir.Id,
                    HedefCekiSatiriId = hedefSatir.Id,
                    BarkodNo = hedefSatir.BarkodNo,
                    Miktar = request.GelenAdet ?? 0,
                    KullaniciId = _currentUserService.UserId ?? 0,
                    Aciklama = request.Aciklama,
                    Tarih = DateTime.UtcNow
                };
                await transferRepo.AddAsync(transfer);
            }
        }
    }
}
