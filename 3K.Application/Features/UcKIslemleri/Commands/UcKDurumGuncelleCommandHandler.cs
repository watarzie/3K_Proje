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
            { StatusConstants.UcKDurum.TamGeldi, StatusConstants.UcKDurum.EksikGeldi, StatusConstants.UcKDurum.ProjedenKarsilandi, StatusConstants.UrunDurum.StoktanKarsilandi,
              StatusConstants.UcKDurum.TedarikcidenGeldi, StatusConstants.UcKDurum.BaskaProyeVerildi, StatusConstants.UcKDurum.HataliUrun };

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

                case StatusConstants.UcKDurum.BaskaProyeVerildi:
                    if (string.IsNullOrWhiteSpace(request.KaynakHedefProjeNo))
                        return Result.Failure("Hangi projeye verildiği yazılmalıdır.");
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
                    satir.GelenMiktar = satir.IstenenAdet;
                    satir.UcKDurumu = StatusConstants.UcKDurum.TamGeldi;
                    satir.TeslimTarihi = DateTime.UtcNow;
                    break;

                case StatusConstants.UcKDurum.EksikGeldi:
                    satir.GelenMiktar = request.GelenAdet!.Value;
                    satir.UcKDurumu = StatusConstants.UcKDurum.EksikGeldi;
                    satir.TeslimTarihi = DateTime.UtcNow;
                    break;

                case StatusConstants.UcKDurum.ProjedenKarsilandi:
                    satir.GelenMiktar = request.GelenAdet!.Value;
                    satir.UcKDurumu = StatusConstants.UcKDurum.ProjedenKarsilandi;
                    satir.TeslimTarihi = DateTime.UtcNow;
                    // Cross-project transfer
                    await HandleProjedenKarsilandi(satir, request);
                    break;

                case StatusConstants.UrunDurum.StoktanKarsilandi:
                    satir.GelenMiktar = request.GelenAdet!.Value;
                    satir.UcKDurumu = StatusConstants.UrunDurum.StoktanKarsilandi;
                    satir.TeslimTarihi = DateTime.UtcNow;
                    break;

                case StatusConstants.UcKDurum.TedarikcidenGeldi:
                    satir.GelenMiktar = request.GelenAdet!.Value;
                    satir.UcKDurumu = StatusConstants.UcKDurum.TedarikcidenGeldi;
                    satir.TeslimTarihi = DateTime.UtcNow;
                    break;

                case StatusConstants.UcKDurum.BaskaProyeVerildi:
                    satir.GelenMiktar = 0;
                    satir.UcKDurumu = StatusConstants.UcKDurum.BaskaProyeVerildi;
                    // Cross-project transfer
                    await HandleBaskaProyeVerildi(satir, request);
                    break;

                case StatusConstants.UcKDurum.HataliUrun:
                    satir.GelenMiktar = request.GelenAdet!.Value;
                    satir.UcKDurumu = StatusConstants.UcKDurum.HataliUrun;
                    satir.TeslimTarihi = DateTime.UtcNow;
                    break;
            }

            // Toplam kontrol
            var toplam = satir.GelenMiktar + satir.TrafoSevkAdet;
            if (toplam > satir.IstenenAdet)
                return Result.Failure("Toplam tamamlanan adet, çeki miktarını aşamaz.");

            // Genel durumu hesapla
            satir.Durum = _durumHesaplaService.HesaplaGenelDurum(satir.GridDurumu, satir.UcKDurumu);

            repo.Update(satir);
            await _unitOfWork.SaveChangesAsync();

            // ===== Sandık Tamamlanma Kontrolü =====
            // Tamamlanan 3K durumları
            var tamamlananTipler = new[] { StatusConstants.UcKDurum.TamGeldi, StatusConstants.UcKDurum.ProjedenKarsilandi, StatusConstants.UrunDurum.StoktanKarsilandi, StatusConstants.UcKDurum.TedarikcidenGeldi };

            if (tamamlananTipler.Contains(request.KarsilamaTipi))
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
            // Kaynak proje bul
            var projeRepo = _unitOfWork.GetRepository<Proje>();
            var kaynakProjeler = await projeRepo.FindAsync(p => p.ProjeNo == request.KaynakHedefProjeNo);
            var kaynakProje = kaynakProjeler.FirstOrDefault();
            if (kaynakProje == null) return;

            // Kaynak projede barkod ile eşleşen satırı bul
            var cekiRepo = _unitOfWork.GetRepository<Ceki>();
            var kaynakCekiler = await cekiRepo.FindAsync(c => c.ProjeId == kaynakProje.Id);
            var kaynakCekiIdler = kaynakCekiler.Select(c => c.Id).ToList();

            var cekiSatiriRepo = _unitOfWork.GetRepository<CekiSatiri>();
            var kaynakSatirlar = await cekiSatiriRepo.FindAsync(cs =>
                kaynakCekiIdler.Contains(cs.CekiId) && cs.BarkodNo == hedefSatir.BarkodNo);
            var kaynakSatir = kaynakSatirlar.FirstOrDefault();

            // Transfer kaydı oluştur
            var hedefProjeRepo = _unitOfWork.GetRepository<Proje>();
            var hedefProjeler = await hedefProjeRepo.FindAsync(p => p.Cekiler.Any(c => c.Id == hedefSatir.CekiId));
            var hedefProje = hedefProjeler.FirstOrDefault();

            if (hedefProje != null)
            {
                var transferRepo = _unitOfWork.GetRepository<ProjeTransfer>();
                var transfer = new ProjeTransfer
                {
                    KaynakProjeId = kaynakProje.Id,
                    HedefProjeId = hedefProje.Id,
                    KaynakCekiSatiriId = kaynakSatir?.Id ?? 0,
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

        /// <summary>
        /// BAŞKA PROJEYE VERİLDİ: Hedef projede eşleşen ürünü bul, adet ekle, transfer kaydı oluştur.
        /// </summary>
        private async Task HandleBaskaProyeVerildi(CekiSatiri kaynakSatir, UcKDurumGuncelleCommand request)
        {
            // Hedef proje bul
            var projeRepo = _unitOfWork.GetRepository<Proje>();
            var hedefProjeler = await projeRepo.FindAsync(p => p.ProjeNo == request.KaynakHedefProjeNo);
            var hedefProje = hedefProjeler.FirstOrDefault();
            if (hedefProje == null) return;

            // Hedef projede barkod ile eşleşen satırı bul
            var cekiRepo = _unitOfWork.GetRepository<Ceki>();
            var hedefCekiler = await cekiRepo.FindAsync(c => c.ProjeId == hedefProje.Id);
            var hedefCekiIdler = hedefCekiler.Select(c => c.Id).ToList();

            var cekiSatiriRepo = _unitOfWork.GetRepository<CekiSatiri>();
            var hedefSatirlar = await cekiSatiriRepo.FindAsync(cs =>
                hedefCekiIdler.Contains(cs.CekiId) && cs.BarkodNo == kaynakSatir.BarkodNo);
            var hedefSatir = hedefSatirlar.FirstOrDefault();

            // Hedef satırda gelen miktarı artır
            if (hedefSatir != null)
            {
                hedefSatir.GelenMiktar += request.GelenAdet ?? 0;
                hedefSatir.UcKKarsilamaTipi = StatusConstants.UcKDurum.ProjedenKarsilandi;
                hedefSatir.UcKDurumu = StatusConstants.UcKDurum.ProjedenKarsilandi;
                hedefSatir.KaynakHedefProjeNo = request.KaynakHedefProjeNo;
                hedefSatir.Durum = _durumHesaplaService.HesaplaGenelDurum(hedefSatir.GridDurumu, hedefSatir.UcKDurumu);
                cekiSatiriRepo.Update(hedefSatir);
            }

            // Kaynak proje bul
            var kaynakProjeRepo = _unitOfWork.GetRepository<Proje>();
            var kaynakProjeler = await kaynakProjeRepo.FindAsync(p => p.Cekiler.Any(c => c.Id == kaynakSatir.CekiId));
            var kaynakProje = kaynakProjeler.FirstOrDefault();

            if (kaynakProje != null)
            {
                var transferRepo = _unitOfWork.GetRepository<ProjeTransfer>();
                var transfer = new ProjeTransfer
                {
                    KaynakProjeId = kaynakProje.Id,
                    HedefProjeId = hedefProje.Id,
                    KaynakCekiSatiriId = kaynakSatir.Id,
                    HedefCekiSatiriId = hedefSatir?.Id,
                    BarkodNo = kaynakSatir.BarkodNo,
                    Miktar = request.GelenAdet ?? 0,
                    KullaniciId = _currentUserService.UserId ?? 0,
                    Aciklama = request.Aciklama,
                    Tarih = DateTime.UtcNow
                };
                await transferRepo.AddAsync(transfer);
            }
        }

        /// <summary>
        /// Sandıktaki tüm ürünler tamamlanmış mı kontrol et.
        /// Tamamlanan durumlar: TamGeldi, ProjedenKarsilandi, StoktanKarsilandi, TedarikcidenGeldi
        /// Hepsi tamamsa → Sandık.Durum = StatusConstants.SandikDurum.Hazir
        /// </summary>
        private async Task SandikTamamlanmaKontrol(CekiSatiri guncellenenSatir)
        {
            var sandikNo = guncellenenSatir.FiiliSandikNo ?? guncellenenSatir.CekideGecenSandikNo;
            if (string.IsNullOrWhiteSpace(sandikNo)) return;

            // Aynı çeki üzerinden projeId bul
            var cekiRepo = _unitOfWork.GetRepository<Ceki>();
            var ceki = await cekiRepo.GetByIdAsync(guncellenenSatir.CekiId);
            if (ceki == null) return;

            // Bu sandıktaki tüm ürünleri bul
            var cekiSatiriRepo = _unitOfWork.GetRepository<CekiSatiri>();
            var tumCekiler = await cekiRepo.FindAsync(c => c.ProjeId == ceki.ProjeId);
            var tumCekiIdler = tumCekiler.Select(c => c.Id).ToList();

            var sandiktakiUrunler = await cekiSatiriRepo.FindAsync(cs =>
                tumCekiIdler.Contains(cs.CekiId) &&
                (cs.FiiliSandikNo == sandikNo || cs.CekideGecenSandikNo == sandikNo));

            if (!sandiktakiUrunler.Any()) return;

            // Tamamlanan durumlar
            var tamamlananTipler = new[] { StatusConstants.UcKDurum.TamGeldi, StatusConstants.UcKDurum.ProjedenKarsilandi, StatusConstants.UrunDurum.StoktanKarsilandi, StatusConstants.UcKDurum.TedarikcidenGeldi };

            var hepsiTamam = sandiktakiUrunler.All(u =>
                tamamlananTipler.Contains(u.UcKKarsilamaTipi) || u.GridDurumu == StatusConstants.GridDurum.TrafoSevk);

            if (hepsiTamam)
            {
                // Sandığı bul ve Hazir yap
                var sandikRepo = _unitOfWork.GetRepository<Sandik>();
                var sandiklar = await sandikRepo.FindAsync(s =>
                    s.ProjeId == ceki.ProjeId && s.SandikNo == sandikNo);
                var sandik = sandiklar.FirstOrDefault();

                if (sandik != null && sandik.Durum != StatusConstants.SandikDurum.Hazir)
                {
                    sandik.Durum = StatusConstants.SandikDurum.Hazir;
                    sandikRepo.Update(sandik);
                    await _unitOfWork.SaveChangesAsync();

                    // Hareket kaydı
                    await _hareketService.HareketKaydetAsync(new HareketGecmisi
                    {
                        ProjeId = ceki.ProjeId,
                        KullaniciId = _currentUserService.UserId ?? 0,
                        ReferansTipi = "Sandik",
                        ReferansId = sandik.Id.ToString(),
                        Islem = "Sandık Otomatik Hazır",
                        EskiDeger = StatusConstants.ProjeDurum.Hazirlaniyor,
                        YeniDeger = StatusConstants.SandikDurum.Hazir,
                        Aciklama = $"Sandık {sandikNo} içindeki tüm ürünler tamamlandı."
                    });
                }
            }
        }
    }
}
