using MediatR;
using _3K.Application.Common;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    public class SandikSilCommandHandler : IRequestHandler<SandikSilCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHareketService _hareketService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ISahaAktarimSilmeKorumaService _sahaAktarimSilmeKorumaService;
        private readonly ISandikService _sandikService;
        private readonly ISahaTamamlamaService _sahaTamamlamaService;

        public SandikSilCommandHandler(
            IUnitOfWork unitOfWork,
            IHareketService hareketService,
            ICurrentUserService currentUserService,
            ISahaAktarimSilmeKorumaService sahaAktarimSilmeKorumaService,
            ISandikService sandikService,
            ISahaTamamlamaService sahaTamamlamaService)
        {
            _unitOfWork = unitOfWork;
            _hareketService = hareketService;
            _currentUserService = currentUserService;
            _sahaAktarimSilmeKorumaService = sahaAktarimSilmeKorumaService;
            _sandikService = sandikService;
            _sahaTamamlamaService = sahaTamamlamaService;
        }

        public async Task<Result> Handle(SandikSilCommand request, CancellationToken cancellationToken)
        {
            return await _unitOfWork.ExecuteInTransactionAsync(
                transactionCancellationToken => HandleInTransactionAsync(request, transactionCancellationToken),
                cancellationToken);
        }

        private async Task<Result> HandleInTransactionAsync(
            SandikSilCommand request,
            CancellationToken cancellationToken)
        {
            var sandikRepo = _unitOfWork.GetRepository<Sandik>();
            var sandik = await sandikRepo.GetByIdAsync(request.SandikId);

            if (sandik == null)
                return Result.Failure("Sandık bulunamadı.", 404);

            if (sandik.ProjeId != request.ProjeId)
                return Result.Failure("Sandık bu projeye ait değil.");

            if (sandik.DurumId == (int)SandikDurum.Sevkedildi)
                return Result.Failure(SandikSevkKilidiHelper.SandikKilitliMesaji);

            var aktarimBagliSandikIds = await _sahaAktarimSilmeKorumaService
                .GetAktifAktarimBagliSandikIdsAsync(new[] { sandik.Id }, cancellationToken);

            if (aktarimBagliSandikIds.Contains(sandik.Id))
                return Result.Failure(SahaAktarimSilmeKorumaMesajlari.Sandik, 409);

            var etkinIcerikMap = await _sandikService.GetEtkinSandikIcerikleriAsync(
                new[] { sandik.Id },
                cancellationToken);
            var etkinIcerikler = etkinIcerikMap.GetValueOrDefault(sandik.Id)
                ?? Array.Empty<SandikIcerik>();

            if (etkinIcerikler.Any(i => i.Id <= 0))
            {
                return Result.Failure(
                    "Bu sandık eski çeki satırlarıyla ilişkilidir. Ürün bağlantıları düzeltilmeden sandık silinemez.",
                    409);
            }

            var sandikIcerikRepo = _unitOfWork.GetRepository<SandikIcerik>();
            var icerikler = (await sandikIcerikRepo.FindAsync(x => x.SandikId == sandik.Id)).ToList();
            var manuelSatirlar = new List<CekiSatiri>();

            if (icerikler.Any())
            {
                var cekiSatiriIds = icerikler
                    .Where(x => x.CekiSatiriId.HasValue)
                    .Select(x => x.CekiSatiriId!.Value)
                    .Distinct()
                    .ToArray();

                var cekiSatiriRepo = _unitOfWork.GetRepository<CekiSatiri>();
                var cekiSatirlari = (await cekiSatiriRepo.FindAsync(x => cekiSatiriIds.Contains(x.Id)))
                    .ToDictionary(x => x.Id);
                var digerSandikTahsisleri = (await sandikIcerikRepo.FindAsync(x =>
                        x.CekiSatiriId.HasValue &&
                        cekiSatiriIds.Contains(x.CekiSatiriId.Value) &&
                        x.SandikId != sandik.Id))
                    .ToList();

                if (digerSandikTahsisleri.Any())
                {
                    return Result.Failure(
                        "Bu sandiktaki manuel urunlerden en az biri baska bir sandiga da tahsis edilmis. " +
                        "Once urunun tum tahsislerini tek sandikta birlestirin veya urunu resmi silme akisiyla kaldirin.",
                        409);
                }

                foreach (var icerik in icerikler)
                {
                    if (!icerik.CekiSatiriId.HasValue
                        || !cekiSatirlari.TryGetValue(icerik.CekiSatiriId.Value, out var satir)
                        || !satir.IsManuelEklenen
                        || satir.KaynakCekiSatiriId.HasValue)
                    {
                        return Result.Failure($"Bu sandıkta {icerikler.Count} ürün bulunuyor. Önce ürünleri silin veya taşıyın.");
                    }

                    manuelSatirlar.Add(satir);
                }

                var islemGormusSatir = manuelSatirlar.FirstOrDefault(ManuelUrunSilmeKurali.IslemGormusMu);
                if (islemGormusSatir != null)
                {
                    return Result.Failure(
                        $"Bu manuel sandıktaki {islemGormusSatir.BarkodNo} ürünü üzerinde 3K işlemi var. Silmeden önce işlemleri geri alın.");
                }
            }

            var sandikNo = sandik.SandikNo;
            var silinenManuelUrunSayisi = manuelSatirlar.Select(x => x.Id).Distinct().Count();

            if (icerikler.Any())
            {
                foreach (var icerik in icerikler)
                    sandikIcerikRepo.Remove(icerik);

                var cekiSatiriRepo = _unitOfWork.GetRepository<CekiSatiri>();
                foreach (var satir in manuelSatirlar.GroupBy(x => x.Id).Select(x => x.First()))
                    cekiSatiriRepo.Remove(satir);
            }

            sandikRepo.Remove(sandik);

            var projeRepo = _unitOfWork.GetRepository<Proje>();
            var proje = await projeRepo.GetByIdAsync(request.ProjeId);
            if (proje?.ProjeTipiId is (int)ProjeTipi.Saha or (int)ProjeTipi.Yedek)
            {
                var kalanSandiklar = (await sandikRepo.FindAsync(s =>
                        s.ProjeId == request.ProjeId && s.Id != sandik.Id))
                    .ToList();

                var sevkDurumuYenidenHesaplanmali =
                    proje.DurumId is (int)ProjeDurum.Tamamlandi or
                        (int)ProjeDurum.SevkEdildi or
                        (int)ProjeDurum.EksikSevkEdildi ||
                    kalanSandiklar.Any(s => s.DurumId == (int)SandikDurum.Sevkedildi);

                if (sevkDurumuYenidenHesaplanmali)
                {
                    proje.DurumId = SahaYedekProjeDurumunuHesapla(kalanSandiklar);
                    projeRepo.Update(proje);
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (proje?.ProjeTipiId == (int)ProjeTipi.Normal)
            {
                var kalanKaynakSatirIds = (await _unitOfWork.GetRepository<CekiSatiri>().FindAsync(cs =>
                        cs.Ceki.ProjeId == request.ProjeId &&
                        !cs.KaynakCekiSatiriId.HasValue))
                    .Select(cs => cs.Id)
                    .Distinct()
                    .ToList();

                if (kalanKaynakSatirIds.Count > 0)
                {
                    await _sahaTamamlamaService.SenkronizeKaynakProjelerAsync(
                        kalanKaynakSatirIds,
                        cancellationToken);
                }
                else
                {
                    var kalanSandiklar = (await sandikRepo.FindAsync(s => s.ProjeId == request.ProjeId)).ToList();
                    proje.DurumId = SahaYedekProjeDurumunuHesapla(kalanSandiklar);
                    projeRepo.Update(proje);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }

            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                KullaniciId = _currentUserService.UserId ?? 0,
                ReferansTipi = "Sandik",
                ReferansId = request.SandikId.ToString(),
                Islem = "Sandık Silindi",
                IslemTipiId = (int)IslemTipi.SandikSilindi,
                ReferansMetni = $"No: {sandikNo}",
                Aciklama = silinenManuelUrunSayisi > 0
                    ? $"Manuel sandık {sandikNo} ve içindeki {silinenManuelUrunSayisi} manuel ürün silindi."
                    : $"Sandık {sandikNo} silindi."
            });

            return Result.Success();
        }

        private static int SahaYedekProjeDurumunuHesapla(IReadOnlyCollection<Sandik> sandiklar)
        {
            if (sandiklar.Count == 0)
                return (int)ProjeDurum.Hazirlaniyor;

            var sevkEdilenSayisi = sandiklar.Count(s => s.DurumId == (int)SandikDurum.Sevkedildi);
            if (sevkEdilenSayisi == sandiklar.Count)
                return (int)ProjeDurum.SevkEdildi;
            if (sevkEdilenSayisi > 0)
                return (int)ProjeDurum.EksikSevkEdildi;

            return sandiklar.All(s => s.DurumId == (int)SandikDurum.Kapandi)
                ? (int)ProjeDurum.Tamamlandi
                : (int)ProjeDurum.Hazirlaniyor;
        }

    }
}
