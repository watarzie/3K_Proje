using MediatR;
using _3K.Core.Enums;
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

        private static readonly int[] GecerliDurumlar =
            { (int)GridDurum.TamGeldi, (int)GridDurum.EksikGeldi, (int)GridDurum.Gelmedi, (int)GridDurum.TrafoSevk, (int)GridDurum.Iptal, (int)GridDurum.Sipariste, (int)GridDurum.Bekliyor };

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
            if (!GecerliDurumlar.Contains(request.YeniDurumId))
                return Result.Failure($"Geçersiz durum: {request.YeniDurumId}");

            var repo = _unitOfWork.GetRepository<CekiSatiri>();
            var satir = await repo.GetByIdAsync(request.CekiSatiriId);

            if (satir == null)
                return Result.Failure("Ürün bulunamadı.", 404);

            var eskiDurum = satir.GridDurumuId;

            // Grid alanlarını güncelle
            satir.GridDurumuId = request.YeniDurumId;
            satir.GridPersonelId = _currentUserService.UserId;
            satir.GridNotu = request.Not;

            // ===== Durum bazlı alan yönetimi =====
            switch (request.YeniDurumId)
            {
                case (int)GridDurum.TamGeldi:
                    satir.GridGelenAdet = satir.IstenenAdet; // otomatik
                    satir.TrafoSevkAdet = 0;
                    break;

                case (int)GridDurum.EksikGeldi:
                    if (request.GridGelenAdet == null || request.GridGelenAdet <= 0)
                        return Result.Failure("Eksik geldi durumunda gelen adet girilmelidir.");
                    if (request.GridGelenAdet >= satir.IstenenAdet)
                        return Result.Failure("Eksik geldi durumunda gelen adet miktardan küçük olmalıdır.");
                    satir.GridGelenAdet = request.GridGelenAdet.Value;
                    satir.TrafoSevkAdet = 0;
                    break;

                case (int)GridDurum.Gelmedi:
                    satir.GridGelenAdet = 0;
                    satir.TrafoSevkAdet = 0;
                    satir.GridSevkDurumuId = 3; // SevkEdilmedi
                    satir.GridSevkMiktari = null;
                    break;

                case (int)GridDurum.TrafoSevk:
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

                case (int)GridDurum.Iptal:
                    satir.GridGelenAdet = 0;
                    satir.TrafoSevkAdet = 0;
                    satir.GridSevkDurumuId = 3; // SevkEdilmedi
                    satir.GridSevkMiktari = null;
                    break;

                case (int)GridDurum.Sipariste:
                    satir.GridGelenAdet = 0;
                    satir.TrafoSevkAdet = 0;
                    satir.GridSevkDurumuId = 3; // SevkEdilmedi
                    satir.GridSevkMiktari = null;
                    break;
            }

            // ===== Grid Sevk Durumu =====
            if (request.GridSevkDurumuId.HasValue)
            {
                // Sevk edilebilmesi kontrolü 
                if (request.GridSevkDurumuId.Value == (int)GridSevkDurum.SevkEdildi)
                {
                    if (request.YeniDurumId != (int)GridDurum.TamGeldi && request.YeniDurumId != (int)GridDurum.EksikGeldi)
                        return Result.Failure("Sevk edilmesi için durum TamGeldi veya EksikGeldi olmalıdır.");
                    if (request.SevkMiktari == null || request.SevkMiktari <= 0)
                        return Result.Failure("Sevk miktarı girilmelidir.");
                }

                satir.GridSevkDurumuId = request.GridSevkDurumuId.Value;
                if (request.SevkMiktari.HasValue)
                {
                    satir.GridSevkMiktari = request.SevkMiktari.Value;
                    satir.GridSevkTarihi = DateTime.UtcNow;
                }
            }

            // Genel durumu otomatik hesapla
            satir.DurumId = _durumHesaplaService.HesaplaGenelDurum(satir.GridDurumuId, satir.UcKDurumuId);

            repo.Update(satir);
            await _unitOfWork.SaveChangesAsync();

            // ===== Sandık Tamamlanma Kontrolü (TrafoSevk ise) =====
            if (request.YeniDurumId == (int)GridDurum.TrafoSevk)
            {
                await SandikTamamlanmaKontrol(satir);
            }

            var sevkMetni = "";
            var sevkDurumAdi = Enum.GetName(typeof(GridSevkDurum), satir.GridSevkDurumuId) ?? "Belirsiz";
            sevkMetni = $" | Sevk Durumu: {sevkDurumAdi}";
            if (satir.GridSevkMiktari.HasValue)
            {
                sevkMetni += $" ({satir.GridSevkMiktari.Value} Adet)";
            }

            var aciklamaMetni = $"Durum: {Enum.GetName(typeof(GridDurum), request.YeniDurumId) ?? request.YeniDurumId.ToString()}{sevkMetni}";
            if (!string.IsNullOrWhiteSpace(request.Not))
            {
                aciklamaMetni += $" | Not: {request.Not}";
            }

            // Hareket kaydı
            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                KullaniciId = _currentUserService.UserId ?? 0,
                ReferansTipi = "CekiSatiri",
                ReferansId = satir.Id.ToString(),
                Islem = "Grid Durum Güncellendi",
                IslemTipiId = (int)IslemTipi.GridDurumGuncellendi,
                EskiDeger = eskiDurum.ToString(),
                YeniDeger = request.YeniDurumId.ToString(),
                Aciklama = aciklamaMetni
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

            var tamamlananTipler = new[] { (int)UcKDurum.TamGeldi, (int)UcKDurum.ProjedenKarsilandi, (int)UcKDurum.StoktanKarsilandi, (int)UcKDurum.TedarikcidenGeldi };
            var hepsiTamam = sandiktakiUrunler.All(u =>
                tamamlananTipler.Contains(u.UcKKarsilamaTipiId) || u.GridDurumuId == (int)GridDurum.TrafoSevk);

            if (hepsiTamam)
            {
                var sandikRepo = _unitOfWork.GetRepository<Sandik>();
                var sandiklar = await sandikRepo.FindAsync(s =>
                    s.ProjeId == ceki.ProjeId && s.SandikNo == sandikNo);
                var sandik = sandiklar.FirstOrDefault();

                if (sandik != null && sandik.DurumId != (int)SandikDurum.Hazir)
                {
                    sandik.DurumId = (int)SandikDurum.Hazir;
                    sandikRepo.Update(sandik);
                    await _unitOfWork.SaveChangesAsync();

                    await _hareketService.HareketKaydetAsync(new HareketGecmisi
                    {
                        ProjeId = ceki.ProjeId,
                        KullaniciId = _currentUserService.UserId ?? 0,
                        ReferansTipi = "Sandik",
                        ReferansId = sandik.Id.ToString(),
                        Islem = "Sandık Otomatik Hazır",
                        IslemTipiId = (int)IslemTipi.SandikOtomatikHazirlandi,
                        EskiDeger = ((int)SandikDurum.Hazirlaniyor).ToString(),
                        YeniDeger = ((int)SandikDurum.Hazir).ToString(),
                        Aciklama = $"Sandık {sandikNo} içindeki tüm ürünler tamamlandı."
                    });
                }
            }
        }
    }
}
