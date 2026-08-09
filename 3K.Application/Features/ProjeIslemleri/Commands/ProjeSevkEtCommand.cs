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
        private readonly ISahaTamamlamaService _sahaTamamlamaService;

        public ProjeSevkEtCommandHandler(
            IUnitOfWork unitOfWork,
            IHareketService hareketService,
            ICurrentUserService currentUserService,
            ISahaTamamlamaService sahaTamamlamaService)
        {
            _unitOfWork = unitOfWork;
            _hareketService = hareketService;
            _currentUserService = currentUserService;
            _sahaTamamlamaService = sahaTamamlamaService;
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

            var kaynakSandikSahaDurumu = proje.ProjeTipiId == (int)ProjeTipi.Normal
                ? await _sahaTamamlamaService.GetKaynakSandikSahaAktarimDurumuAsync(
                    sandiklar.Select(s => s.Id),
                    cancellationToken)
                : new _3K.Core.Models.KaynakSandikSahaAktarimDurumu();
            var aktifSahaAktarimliSandikIds = kaynakSandikSahaDurumu.AktifAktarimaBagliSandikIds;

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

                if (secilenSandiklar.Any(s => aktifSahaAktarimliSandikIds.Contains(s.Id)))
                    return Result.Failure("Seçilen sandıklardan bazıları aktif saha aktarımına bağlı. Bu sandıklar ana projeden yeniden sevk edilemez; gerekirse aktarımı saha projesinden geri alın.", 409);

                if (secilenSandiklar.Any(s => !SandikSevkKilidiHelper.SandikSevkeHazirMi(s)))
                    return Result.Failure(SandikSevkKilidiHelper.SandikSevkeHazirDegilMesaji, 409);

                sevkEdilecekSandiklar = secilenSandiklar;
            }
            else
            {
                var sevkeHazirOlmayanAdayVar = sandiklar.Any(s =>
                    s.DurumId != (int)SandikDurum.Sevkedildi &&
                    !aktifSahaAktarimliSandikIds.Contains(s.Id) &&
                    !SandikSevkKilidiHelper.SandikSevkeHazirMi(s));

                if (sevkeHazirOlmayanAdayVar)
                    return Result.Failure(SandikSevkKilidiHelper.SandikSevkeHazirDegilMesaji, 409);

                sevkEdilecekSandiklar = sandiklar
                    .Where(s =>
                        SandikSevkKilidiHelper.SandikSevkeHazirMi(s) &&
                        !aktifSahaAktarimliSandikIds.Contains(s.Id))
                    .ToList();
            }

            if (sevkEdilecekSandiklar.Count == 0)
                return Result.Failure("Sevk edilecek sandık bulunamadı.");

            var sevkEdilecekSandikIdleri = sevkEdilecekSandiklar.Select(s => s.Id).ToList();
            return await _unitOfWork.ExecuteInTransactionAsync(async transactionCancellationToken =>
            {
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

                if (proje.ProjeTipiId == (int)ProjeTipi.Normal)
                {
                    var sahaUzerindenSevkEdilenSandikIds = kaynakSandikSahaDurumu.SahaUzerindenSevkEdilenSandikIds;
                    var etkinSevkEdilenSandikSayisi = sandiklar.Count(s =>
                        s.DurumId == (int)SandikDurum.Sevkedildi ||
                        sahaUzerindenSevkEdilenSandikIds.Contains(s.Id));
                    proje.DurumId = ProjeSevkDurumHelper.Hesapla(
                        sandiklar.Count,
                        etkinSevkEdilenSandikSayisi,
                        proje.DurumId);
                }
                else
                {
                    proje.DurumId = ProjeSevkDurumHelper.Hesapla(sandiklar, proje.DurumId);
                }
                proje.GerceklesenSevkTarihi ??= sevkTarihi;
                projeRepo.Update(proje);

                await _unitOfWork.SaveChangesAsync(transactionCancellationToken);

                if (proje.ProjeTipiId == (int)ProjeTipi.Saha)
                {
                    await _sahaTamamlamaService.SenkronizeKaynakProjelerBySahaSandikIdsAsync(
                        sevkEdilecekSandikIdleri,
                        transactionCancellationToken);
                }

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
            }, cancellationToken);
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
