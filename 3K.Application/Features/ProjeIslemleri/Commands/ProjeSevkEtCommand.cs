using System.Collections.Generic;
using System.Linq;
using MediatR;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Helpers;

namespace _3K.Application.Features.ProjeIslemleri.Commands
{
    public class ProjeSevkEtCommand : IRequest<Result>, ISecuredRequest
    {
        // UI'da W yetkisi olanlar tetikleyebilecek, backend'de yetki yönetimi Pipeline üzerinden de yapılıyor.

        public int ProjeId { get; set; }
        public DateTime? SevkTarihi { get; set; }
        public List<int>? SandikIds { get; set; }
        public string? Aciklama { get; set; }
        public string? AracPlaka { get; set; }
    }

    public class ProjeSevkEtCommandHandler : IRequestHandler<ProjeSevkEtCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHareketService _hareketService;
        private readonly ICurrentUserService _currentUserService;

        public ProjeSevkEtCommandHandler(IUnitOfWork unitOfWork, IHareketService hareketService, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _hareketService = hareketService;
            _currentUserService = currentUserService;
        }

        public async Task<Result> Handle(ProjeSevkEtCommand request, CancellationToken cancellationToken)
        {
            var projeRepo = _unitOfWork.GetRepository<Proje>();
            var sandikRepo = _unitOfWork.GetRepository<Sandik>();
            var sevkiyatSandikRepo = _unitOfWork.GetRepository<SevkiyatSandik>();

            if (!_currentUserService.UserId.HasValue)
                return Result.Failure("Oturum acmaniz gerekiyor.", 401);

            var proje = await projeRepo.GetByIdAsync(request.ProjeId);

            if (proje == null)
                return Result.Failure("Proje bulunamadı.");

            int eskiDurum = proje.DurumId;
            var sandiklar = (await sandikRepo.FindAsync(s => s.ProjeId == request.ProjeId)).ToList();

            if (sandiklar.Count == 0)
                return Result.Failure("Projeye ait sandık bulunamadı.");

            if (proje.DurumId == (int)ProjeDurum.SevkEdildi)
                return Result.Failure("Proje zaten sevk edilmiş durumda. Düzeltmeye açık sandıklar için Düzeltmeyi Tamamla işlemini kullanın.");

            var secilenSandikIds = request.SandikIds?
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            List<Sandik> sevkEdilecekSandiklar;
            if (secilenSandikIds is { Count: > 0 })
            {
                var secilenSandiklar = sandiklar.Where(s => secilenSandikIds.Contains(s.Id)).ToList();
                if (secilenSandiklar.Count != secilenSandikIds.Count)
                    return Result.Failure("Seçilen sandıklardan bazıları bu projeye ait değil.");

                if (secilenSandiklar.Any(s => s.DurumId == (int)SandikDurum.Sevkedildi))
                    return Result.Failure("Seçilen sandıklardan bazıları zaten sevk edilmiş. Düzeltmeye açık sandıklar için Düzeltmeyi Tamamla işlemini kullanın.");

                sevkEdilecekSandiklar = secilenSandiklar;
            }
            else
            {
                sevkEdilecekSandiklar = sandiklar
                    .Where(s => s.DurumId != (int)SandikDurum.Sevkedildi)
                    .ToList();
            }

            if (sevkEdilecekSandiklar.Count == 0)
                return Result.Failure("Sevk edilecek sandık bulunamadı.");

            var sevkEdilecekSandikIdleri = sevkEdilecekSandiklar.Select(s => s.Id).ToList();
            var mevcutSevkiyatBagliSandikIdleri = (await sevkiyatSandikRepo.FindAsync(ss =>
                    sevkEdilecekSandikIdleri.Contains(ss.SandikId)))
                .Select(ss => ss.SandikId)
                .ToHashSet();

            var sevkTarihi = request.SevkTarihi ?? TurkeyTime.Now;
            int sevkEdilenSandikSayisi = 0;
            foreach (var sandik in sevkEdilecekSandiklar)
            {
                sandik.SevkOncesiDurumId ??= sandik.DurumId;
                sandik.DurumId = (int)SandikDurum.Sevkedildi;
                sandik.SevkiyatDuzeltmeAcikMi = false;
                sandikRepo.Update(sandik);
                sevkEdilenSandikSayisi++;
            }

            var yeniSevkiyatSandiklari = sevkEdilecekSandiklar
                .Where(s => !mevcutSevkiyatBagliSandikIdleri.Contains(s.Id))
                .ToList();
            var duzeltmeSonrasiKilitlenenSayisi = sevkEdilecekSandiklar.Count - yeniSevkiyatSandiklari.Count;

            Sevkiyat? sevkiyat = null;
            if (yeniSevkiyatSandiklari.Count > 0)
            {
                sevkiyat = await SevkiyatKayitHelper.OlusturAsync(
                    _unitOfWork,
                    proje.Id,
                    yeniSevkiyatSandiklari,
                    sevkTarihi,
                    request.Aciklama,
                    request.AracPlaka,
                    _currentUserService.UserId.Value);
            }

            proje.DurumId = ProjeSevkDurumHelper.Hesapla(sandiklar, proje.DurumId);
            proje.GerceklesenSevkTarihi ??= sevkTarihi;
            projeRepo.Update(proje);

            await _unitOfWork.SaveChangesAsync();

            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = proje.Id,
                KullaniciId = _currentUserService.UserId.Value,
                ReferansTipi = "Proje",
                ReferansId = proje.Id.ToString(),
                Islem = "Proje Sevk Edildi",
                IslemTipiId = (int)IslemTipi.ProjeSevkEdildi,
                EskiDeger = eskiDurum.ToString(),
                YeniDeger = proje.DurumId.ToString(),
                Aciklama = GetSevkAciklamasi(sevkEdilenSandikSayisi, yeniSevkiyatSandiklari.Count, duzeltmeSonrasiKilitlenenSayisi, sevkiyat)
            });

            return Result.Success();
        }

        private static string GetSevkAciklamasi(int toplamSandik, int yeniSevkSandikSayisi, int duzeltmeSonrasiKilitlenenSayisi, Sevkiyat? sevkiyat)
        {
            if (yeniSevkSandikSayisi > 0 && sevkiyat != null && duzeltmeSonrasiKilitlenenSayisi > 0)
            {
                return $"Proje sevk işlemi yapıldı. {yeniSevkSandikSayisi} sandık {sevkiyat.SevkiyatNo}. sevkiyat ile sevk edildi, {duzeltmeSonrasiKilitlenenSayisi} sandık mevcut sevkiyat kaydı korunarak yeniden kilitlendi.";
            }

            if (yeniSevkSandikSayisi > 0 && sevkiyat != null)
                return $"Proje sevk işlemi yapıldı. {toplamSandik} sandık {sevkiyat.SevkiyatNo}. sevkiyat ile sevk edildi.";

            return $"Sevkiyat düzeltmesi tamamlandı. {duzeltmeSonrasiKilitlenenSayisi} sandık mevcut sevkiyat kaydı korunarak yeniden kilitlendi.";
        }
    }
}

