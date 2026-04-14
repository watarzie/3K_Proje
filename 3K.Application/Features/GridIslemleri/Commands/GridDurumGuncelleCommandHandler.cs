using MediatR;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.GridIslemleri.Commands
{
    public class GridDurumGuncelleCommandHandler : IRequestHandler<GridDurumGuncelleCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IDurumHesaplaService _durumHesaplaService;
        private readonly IHareketService _hareketService;

        private static readonly string[] GecerliDurumlar =
            { "TamGeldi", "EksikGeldi", "Gelmedi", "TrafoSevk", "Iptal", "Sipariste", "Bekliyor" };

        private static readonly string[] GecerliSevkDurumlari =
            { "SevkEdildi", "Bekliyor", "SevkEdilmedi" };

        public GridDurumGuncelleCommandHandler(
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

        public async Task<Result> Handle(GridDurumGuncelleCommand request, CancellationToken cancellationToken)
        {
            // Validate durum
            if (!GecerliDurumlar.Contains(request.YeniDurum))
                return Result.Failure($"Geçersiz durum: {request.YeniDurum}");

            var repo = _unitOfWork.GetRepository<CekiSatiri>();
            var satir = await repo.GetByIdAsync(request.CekiSatiriId);

            if (satir == null)
                return Result.Failure("Ürün bulunamadı.", 404);

            var eskiDurum = satir.GridDurumu;

            // Grid alanlarını güncelle
            satir.GridDurumu = request.YeniDurum;
            satir.GridPersonelId = _currentUserService.UserId;
            satir.GridNotu = request.Not;

            // ===== Durum bazlı alan yönetimi =====
            switch (request.YeniDurum)
            {
                case "TamGeldi":
                    satir.GridGelenAdet = satir.IstenenAdet; // otomatik
                    satir.TrafoSevkAdet = 0;
                    break;

                case "EksikGeldi":
                    if (request.GridGelenAdet == null || request.GridGelenAdet <= 0)
                        return Result.Failure("Eksik geldi durumunda gelen adet girilmelidir.");
                    if (request.GridGelenAdet >= satir.IstenenAdet)
                        return Result.Failure("Eksik geldi durumunda gelen adet miktardan küçük olmalıdır.");
                    satir.GridGelenAdet = request.GridGelenAdet.Value;
                    satir.TrafoSevkAdet = 0;
                    break;

                case "Gelmedi":
                    satir.GridGelenAdet = 0;
                    satir.TrafoSevkAdet = 0;
                    satir.GridSevkDurumu = "SevkEdilmedi";
                    satir.GridSevkMiktari = null;
                    break;

                case "TrafoSevk":
                    if (request.TrafoSevkAdet == null || request.TrafoSevkAdet <= 0)
                        return Result.Failure("Trafo sevk durumunda trafo sevk adeti girilmelidir.");
                    if (request.TrafoSevkAdet > satir.IstenenAdet)
                        return Result.Failure("Trafo sevk adeti miktardan büyük olamaz.");
                    satir.TrafoSevkAdet = request.TrafoSevkAdet.Value;
                    satir.GridGelenAdet = request.GridGelenAdet ?? 0;
                    // Toplam kontrol
                    if (satir.GridGelenAdet + satir.TrafoSevkAdet > satir.IstenenAdet)
                        return Result.Failure("Toplam adet, çeki miktarını aşamaz.");
                    break;

                case "Iptal":
                    satir.GridGelenAdet = 0;
                    satir.TrafoSevkAdet = 0;
                    satir.GridSevkDurumu = "SevkEdilmedi";
                    satir.GridSevkMiktari = null;
                    break;

                case "Sipariste":
                    satir.GridGelenAdet = 0;
                    satir.TrafoSevkAdet = 0;
                    satir.GridSevkDurumu = "SevkEdilmedi";
                    satir.GridSevkMiktari = null;
                    break;
            }

            // ===== Grid Sevk Durumu =====
            if (request.GridSevkDurumu != null && GecerliSevkDurumlari.Contains(request.GridSevkDurumu))
            {
                // Sevk edilebilmesi kontrolü
                if (request.GridSevkDurumu == "SevkEdildi")
                {
                    if (request.YeniDurum != "TamGeldi" && request.YeniDurum != "EksikGeldi")
                        return Result.Failure("Sevk edilmesi için durum TamGeldi veya EksikGeldi olmalıdır.");
                    if (request.SevkMiktari == null || request.SevkMiktari <= 0)
                        return Result.Failure("Sevk miktarı girilmelidir.");
                }

                satir.GridSevkDurumu = request.GridSevkDurumu;
                if (request.SevkMiktari.HasValue)
                {
                    satir.GridSevkMiktari = request.SevkMiktari.Value;
                    satir.GridSevkTarihi = DateTime.UtcNow;
                }
            }

            // Genel durumu otomatik hesapla
            satir.Durum = _durumHesaplaService.HesaplaGenelDurum(satir.GridDurumu, satir.UcKDurumu);

            repo.Update(satir);
            await _unitOfWork.SaveChangesAsync();

            // ===== Sandık Tamamlanma Kontrolü (TrafoSevk ise) =====
            if (request.YeniDurum == "TrafoSevk")
            {
                await SandikTamamlanmaKontrol(satir);
            }

            // Hareket kaydı
            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                KullaniciId = _currentUserService.UserId ?? 0,
                ReferansTipi = "CekiSatiri",
                ReferansId = satir.Id.ToString(),
                Islem = "Grid Durum Güncellendi",
                EskiDeger = eskiDurum,
                YeniDeger = request.YeniDurum,
                Aciklama = request.Not
            });

            return Result.Success();
        }

        /// <summary>
        /// Sandıktaki tüm ürünler tamamlanmış mı kontrol et.
        /// Tamamlanan: 3K tipleri (TamGeldi, ProjedenKarsilandi, StoktanKarsilandi, TedarikcidenGeldi) VEYA GridDurumu=TrafoSevk
        /// </summary>
        private async Task SandikTamamlanmaKontrol(CekiSatiri guncellenenSatir)
        {
            var sandikNo = guncellenenSatir.FiiliSandikNo ?? guncellenenSatir.CekideGecenSandikNo;
            if (string.IsNullOrWhiteSpace(sandikNo)) return;

            var cekiRepo = _unitOfWork.GetRepository<Ceki>();
            var ceki = await cekiRepo.GetByIdAsync(guncellenenSatir.CekiId);
            if (ceki == null) return;

            var cekiSatiriRepo = _unitOfWork.GetRepository<CekiSatiri>();
            var tumCekiler = await cekiRepo.FindAsync(c => c.ProjeId == ceki.ProjeId);
            var tumCekiIdler = tumCekiler.Select(c => c.Id).ToList();

            var sandiktakiUrunler = await cekiSatiriRepo.FindAsync(cs =>
                tumCekiIdler.Contains(cs.CekiId) &&
                (cs.FiiliSandikNo == sandikNo || cs.CekideGecenSandikNo == sandikNo));

            if (!sandiktakiUrunler.Any()) return;

            var tamamlananTipler = new[] { "TamGeldi", "ProjedenKarsilandi", "StoktanKarsilandi", "TedarikcidenGeldi" };
            var hepsiTamam = sandiktakiUrunler.All(u =>
                tamamlananTipler.Contains(u.UcKKarsilamaTipi) || u.GridDurumu == "TrafoSevk");

            if (hepsiTamam)
            {
                var sandikRepo = _unitOfWork.GetRepository<Sandik>();
                var sandiklar = await sandikRepo.FindAsync(s =>
                    s.ProjeId == ceki.ProjeId && s.SandikNo == sandikNo);
                var sandik = sandiklar.FirstOrDefault();

                if (sandik != null && sandik.Durum != "Hazir")
                {
                    sandik.Durum = "Hazir";
                    sandikRepo.Update(sandik);
                    await _unitOfWork.SaveChangesAsync();

                    await _hareketService.HareketKaydetAsync(new HareketGecmisi
                    {
                        ProjeId = ceki.ProjeId,
                        KullaniciId = _currentUserService.UserId ?? 0,
                        ReferansTipi = "Sandik",
                        ReferansId = sandik.Id.ToString(),
                        Islem = "Sandık Otomatik Hazır",
                        EskiDeger = "Hazirlaniyor",
                        YeniDeger = "Hazir",
                        Aciklama = $"Sandık {sandikNo} içindeki tüm ürünler tamamlandı."
                    });
                }
            }
        }
    }
}
