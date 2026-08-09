using System.Linq;
using MediatR;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Helpers;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    /// <summary>
    /// Tekil sandık sevk komutu. Sandığı "Sevk Edildi" durumuna geçirir.
    /// </summary>
    public class SandikSevkEtCommand : IRequest<Result>, ISecuredRequest
    {

        public int ProjeId { get; set; }
        public int SandikId { get; set; }
        public string? Aciklama { get; set; }
        public string? AracPlaka { get; set; }
    }

    public class SandikSevkEtCommandHandler : IRequestHandler<SandikSevkEtCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHareketService _hareketService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ISahaTamamlamaService _sahaTamamlamaService;

        public SandikSevkEtCommandHandler(
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

        public async Task<Result> Handle(SandikSevkEtCommand request, CancellationToken cancellationToken)
        {
            var sandikRepo = _unitOfWork.GetRepository<Sandik>();
            var projeRepo = _unitOfWork.GetRepository<Proje>();
            var sevkiyatSandikRepo = _unitOfWork.GetRepository<SevkiyatSandik>();

            if (!_currentUserService.UserId.HasValue)
                return Result.Failure("Oturum acmaniz gerekiyor.", 401);

            var sandik = await sandikRepo.GetByIdAsync(request.SandikId);

            if (sandik == null || sandik.ProjeId != request.ProjeId)
                return Result.Failure("Sandık bulunamadı veya projeye ait değil.", 404);

            var proje = await projeRepo.GetByIdAsync(request.ProjeId);
            if (proje == null)
                return Result.Failure("Proje bulunamadı.", 404);

            var projeSandiklari = (await sandikRepo.FindAsync(s => s.ProjeId == request.ProjeId)).ToList();
            var kaynakSandikSahaDurumu = proje.ProjeTipiId == (int)ProjeTipi.Normal
                ? await _sahaTamamlamaService.GetKaynakSandikSahaAktarimDurumuAsync(
                    projeSandiklari.Select(s => s.Id),
                    cancellationToken)
                : new _3K.Core.Models.KaynakSandikSahaAktarimDurumu();

            if (kaynakSandikSahaDurumu.AktifAktarimaBagliSandikIds.Contains(sandik.Id))
                return Result.Failure("Bu sandık aktif saha aktarımına bağlı olduğu için ana projeden yeniden sevk edilemez. Gerekirse aktarımı saha projesinden geri alın.", 409);

            if (sandik.DurumId == (int)SandikDurum.Sevkedildi && sandik.SevkiyatDuzeltmeAcikMi)
                return Result.Failure("Bu sandık zaten sevk edilmiş ve düzeltmeye açık. Düzeltmeyi Tamamla işlemini kullanın.");

            if (sandik.DurumId == (int)SandikDurum.Sevkedildi)
                return Result.Failure("Sandık zaten sevk edilmiş durumda.");

            if (!SandikSevkKilidiHelper.SandikSevkeHazirMi(sandik))
                return Result.Failure(SandikSevkKilidiHelper.SandikSevkeHazirDegilMesaji, 409);

            int eskiDurum = sandik.DurumId;
            return await _unitOfWork.ExecuteInTransactionAsync(async transactionCancellationToken =>
            {
                var sevkTarihi = TurkeyTime.Now;
                var mevcutSevkiyatBagliMi = (await sevkiyatSandikRepo.FindAsync(ss => ss.SandikId == sandik.Id)).Any();
                sandik.SevkOncesiDurumId ??= sandik.DurumId;
                sandik.DurumId = (int)SandikDurum.Sevkedildi;
                sandik.SevkiyatDuzeltmeAcikMi = false;
                sandikRepo.Update(sandik);

                var sandiklar = projeSandiklari;
                var guncelSandik = sandiklar.FirstOrDefault(s => s.Id == request.SandikId);
                if (guncelSandik != null)
                {
                    guncelSandik.DurumId = (int)SandikDurum.Sevkedildi;
                    guncelSandik.SevkiyatDuzeltmeAcikMi = false;
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

                Sevkiyat? sevkiyat = null;
                if (!mevcutSevkiyatBagliMi)
                {
                    sevkiyat = await SevkiyatKayitHelper.OlusturAsync(
                        _unitOfWork,
                        request.ProjeId,
                        new[] { sandik },
                        sevkTarihi,
                        request.Aciklama,
                        request.AracPlaka,
                        _currentUserService.UserId.Value);
                }

                await _unitOfWork.SaveChangesAsync(transactionCancellationToken);

                if (proje.ProjeTipiId == (int)ProjeTipi.Saha)
                {
                    await _sahaTamamlamaService.SenkronizeKaynakProjelerBySahaSandikIdsAsync(
                        new[] { sandik.Id },
                        transactionCancellationToken);
                }

                await _hareketService.HareketKaydetAsync(new HareketGecmisi
                {
                    ProjeId = request.ProjeId,
                    KullaniciId = _currentUserService.UserId.Value,
                    ReferansTipi = "Sandik",
                    ReferansId = sandik.Id.ToString(),
                    Islem = "Sandık Sevk Edildi",
                    IslemTipiId = (int)IslemTipi.SandikSevkEdildi,
                    EskiDeger = eskiDurum.ToString(),
                    YeniDeger = sandik.DurumId.ToString(),
                    Aciklama = mevcutSevkiyatBagliMi
                        ? $"Sandık {sandik.SandikNo} mevcut sevkiyat kaydı korunarak yeniden kilitlendi."
                        : $"Sandık {sandik.SandikNo} {sevkiyat!.SevkiyatNo}. sevkiyat ile sevk edildi."
                });

                return Result.Success();
            }, cancellationToken);
        }
    }
}
