using _3K.Application.Common;
using _3K.Application.Features.SandikIslemleri.Commands;
using _3K.Application.Features.SandikIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.SandikIslemleri.Services
{
    public sealed class SandikLokasyonGuncellemeService : ISandikLokasyonGuncellemeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISandikService _sandikService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IHareketService _hareketService;

        public SandikLokasyonGuncellemeService(
            IUnitOfWork unitOfWork,
            ISandikService sandikService,
            ICurrentUserService currentUserService,
            IHareketService hareketService)
        {
            _unitOfWork = unitOfWork;
            _sandikService = sandikService;
            _currentUserService = currentUserService;
            _hareketService = hareketService;
        }

        public async Task<Result<SandikLokasyonOnayliUygulaCommand>> PlanlaAsync(
            IReadOnlyCollection<int>? sandikIds,
            int depoLokasyonId,
            CancellationToken cancellationToken)
        {
            var normalizeSonucu = NormalizeSandikIds(sandikIds);
            if (!normalizeSonucu.IsSuccess || normalizeSonucu.Value == null)
                return PlanHatasi(normalizeSonucu);

            var lokasyonSonucu = await LokasyonlariGetirAsync(depoLokasyonId);
            if (!lokasyonSonucu.IsSuccess || lokasyonSonucu.Value == null)
                return Result<SandikLokasyonOnayliUygulaCommand>.Failure(
                    lokasyonSonucu.Error?.Message ?? "Seçilen depo lokasyonu bulunamadı.",
                    lokasyonSonucu.StatusCode);

            var sandikSonucu = await SandiklariGetirAsync(normalizeSonucu.Value);
            if (!sandikSonucu.IsSuccess || sandikSonucu.Value == null)
                return PlanHatasi(sandikSonucu);

            var sandiklar = sandikSonucu.Value;
            var projeId = sandiklar[0].ProjeId;
            if (sandiklar.Any(sandik => sandik.ProjeId != projeId))
            {
                return Result<SandikLokasyonOnayliUygulaCommand>.Failure(
                    "Lokasyon ataması yalnızca aynı projeye ait sandıklar için toplu yapılabilir.");
            }

            var degisecekSandiklar = sandiklar
                .Where(sandik => sandik.DepoLokasyonId != depoLokasyonId)
                .OrderBy(sandik => sandik.Id)
                .ToList();
            var isKuraliSonucu = await IsKurallariniDogrulaAsync(
                degisecekSandiklar,
                depoLokasyonId,
                cancellationToken);
            if (!isKuraliSonucu.IsSuccess)
                return Result<SandikLokasyonOnayliUygulaCommand>.Failure(
                    isKuraliSonucu.Error?.Message ?? "Lokasyon atama kuralları doğrulanamadı.",
                    isKuraliSonucu.StatusCode);

            var proje = await _unitOfWork.GetRepository<Proje>().GetByIdAsync(projeId);
            if (proje == null)
                return Result<SandikLokasyonOnayliUygulaCommand>.Failure("Proje bulunamadı.", 404);

            var lokasyonlar = lokasyonSonucu.Value;
            var plan = new SandikLokasyonOnayliUygulaCommand
            {
                ProjeId = proje.Id,
                ProjeNo = proje.ProjeNo,
                DepoLokasyonId = depoLokasyonId,
                DepoLokasyonAdi = lokasyonlar[depoLokasyonId],
                Kalemler = degisecekSandiklar
                    .OrderBy(sandik => sandik.SandikNo, StringComparer.OrdinalIgnoreCase)
                    .Select(sandik => new SandikLokasyonDegisiklikKalemi
                    {
                        SandikId = sandik.Id,
                        SandikNo = sandik.SandikNo,
                        BeklenenDepoLokasyonId = sandik.DepoLokasyonId,
                        BeklenenDepoLokasyonAdi = lokasyonlar.GetValueOrDefault(
                            sandik.DepoLokasyonId,
                            sandik.DepoLokasyonId.ToString())
                    })
                    .ToList()
            };

            return Result<SandikLokasyonOnayliUygulaCommand>.Success(plan);
        }

        public async Task<Result<bool>> UygulaAsync(
            SandikLokasyonOnayliUygulaCommand plan,
            CancellationToken cancellationToken)
        {
            if (plan.ProjeId <= 0 ||
                plan.DepoLokasyonId <= 0 ||
                plan.Kalemler == null ||
                plan.Kalemler.Count == 0 ||
                plan.Kalemler.Any(kalem => kalem == null))
                return Result<bool>.Failure("Lokasyon atama planı geçersiz.");

            var kalemler = plan.Kalemler
                .GroupBy(kalem => kalem.SandikId)
                .Select(grup => grup.First())
                .ToList();
            if (kalemler.Count != plan.Kalemler.Count ||
                kalemler.Any(kalem =>
                    kalem.SandikId <= 0 || kalem.BeklenenDepoLokasyonId <= 0))
                return Result<bool>.Failure("Lokasyon atama planındaki sandık bilgileri geçersiz.");

            var lokasyonSonucu = await LokasyonlariGetirAsync(plan.DepoLokasyonId);
            if (!lokasyonSonucu.IsSuccess || lokasyonSonucu.Value == null)
                return Result<bool>.Failure(
                    lokasyonSonucu.Error?.Message ?? "Seçilen depo lokasyonu bulunamadı.",
                    lokasyonSonucu.StatusCode);

            var proje = await _unitOfWork.GetRepository<Proje>().GetByIdAsync(plan.ProjeId);
            if (proje == null)
                return Result<bool>.Failure("Lokasyon atama planındaki proje artık bulunamıyor.", 404);

            if (!AyniMetin(proje.ProjeNo, plan.ProjeNo))
            {
                return Result<bool>.Failure(
                    "Proje bilgisi talep oluşturulduktan sonra değiştirildi. Güncel verilerle yeni bir talep oluşturun.",
                    409);
            }

            var lokasyonlar = lokasyonSonucu.Value;
            if (!AyniMetin(lokasyonlar[plan.DepoLokasyonId], plan.DepoLokasyonAdi))
            {
                return Result<bool>.Failure(
                    "Hedef lokasyonun adı talep oluşturulduktan sonra değiştirildi. Güncel verilerle yeni bir talep oluşturun.",
                    409);
            }

            var sandikIds = kalemler.Select(kalem => kalem.SandikId).ToArray();
            var sandikSonucu = await SandiklariGetirAsync(sandikIds);
            if (!sandikSonucu.IsSuccess || sandikSonucu.Value == null)
                return UygulamaHatasi(sandikSonucu);

            var sandiklar = sandikSonucu.Value;
            if (sandiklar.Any(sandik => sandik.ProjeId != plan.ProjeId))
                return Result<bool>.Failure("Lokasyon atama planındaki sandıkların proje bilgisi değişmiş.", 409);

            var kalemBySandikId = kalemler.ToDictionary(kalem => kalem.SandikId);
            var etiketiDegisenSandiklar = sandiklar
                .Where(sandik =>
                {
                    var kalem = kalemBySandikId[sandik.Id];
                    var kaynakLokasyonAdi = lokasyonlar.GetValueOrDefault(
                        kalem.BeklenenDepoLokasyonId,
                        kalem.BeklenenDepoLokasyonId.ToString());

                    return !AyniMetin(sandik.SandikNo, kalem.SandikNo) ||
                           !AyniMetin(kaynakLokasyonAdi, kalem.BeklenenDepoLokasyonAdi);
                })
                .Select(sandik => sandik.SandikNo)
                .ToList();
            if (etiketiDegisenSandiklar.Count > 0)
            {
                return Result<bool>.Failure(
                    $"{string.Join(", ", etiketiDegisenSandiklar)} numaralı sandıkların görünen bilgileri talep oluşturulduktan sonra değiştirildi. Güncel verilerle yeni bir talep oluşturun.",
                    409);
            }

            var cakisanSandiklar = sandiklar
                .Where(sandik =>
                {
                    var beklenen = kalemBySandikId[sandik.Id].BeklenenDepoLokasyonId;
                    return sandik.DepoLokasyonId != beklenen &&
                           sandik.DepoLokasyonId != plan.DepoLokasyonId;
                })
                .Select(sandik => sandik.SandikNo)
                .ToList();

            if (cakisanSandiklar.Count > 0)
            {
                return Result<bool>.Failure(
                    $"{string.Join(", ", cakisanSandiklar)} numaralı sandıkların lokasyonu talep oluşturulduktan sonra değiştirildi. Güncel verilerle yeni bir talep oluşturun.",
                    409);
            }

            // Aynı talep iki kez kuyruğa girdiyse ikinci onay hareket kaydı
            // üretmeden başarılı olur. Farklı hedefli eski talepler üstte çatışır.
            var degisecekSandiklar = sandiklar
                .Where(sandik => sandik.DepoLokasyonId != plan.DepoLokasyonId)
                .OrderBy(sandik => sandik.Id)
                .ToList();
            if (degisecekSandiklar.Count == 0)
                return Result<bool>.Success(true);

            var isKuraliSonucu = await IsKurallariniDogrulaAsync(
                degisecekSandiklar,
                plan.DepoLokasyonId,
                cancellationToken);
            if (!isKuraliSonucu.IsSuccess)
                return Result<bool>.Failure(
                    isKuraliSonucu.Error?.Message ?? "Lokasyon atama kuralları doğrulanamadı.",
                    isKuraliSonucu.StatusCode);

            var repo = _unitOfWork.GetRepository<Sandik>();
            var yeniLokasyonMetni = lokasyonlar[plan.DepoLokasyonId];

            foreach (var sandik in degisecekSandiklar)
            {
                var eskiLokasyonId = sandik.DepoLokasyonId;
                var eskiLokasyonMetni = lokasyonlar.GetValueOrDefault(
                    eskiLokasyonId,
                    eskiLokasyonId.ToString());

                sandik.DepoLokasyonId = plan.DepoLokasyonId;
                repo.Update(sandik);

                await _hareketService.HareketKaydetAsync(new HareketGecmisi
                {
                    ProjeId = sandik.ProjeId,
                    KullaniciId = _currentUserService.UserId ?? 0,
                    ReferansTipi = "Sandik",
                    ReferansId = sandik.Id.ToString(),
                    Islem = "Lokasyon Güncelleme",
                    IslemTipiId = (int)IslemTipi.SandikLokasyonGuncellendi,
                    EskiDeger = eskiLokasyonMetni,
                    YeniDeger = yeniLokasyonMetni,
                    Aciklama = $"Sandık lokasyonu '{eskiLokasyonMetni}' değerinden '{yeniLokasyonMetni}' olarak değiştirildi."
                });
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<bool>.Success(true);
        }

        private async Task<Result> IsKurallariniDogrulaAsync(
            IReadOnlyCollection<Sandik> sandiklar,
            int depoLokasyonId,
            CancellationToken cancellationToken)
        {
            var sevkEdilmisSandiklar = sandiklar
                .Where(sandik => sandik.DurumId == (int)SandikDurum.Sevkedildi)
                .Select(sandik => sandik.SandikNo)
                .ToList();
            if (sevkEdilmisSandiklar.Count > 0)
            {
                return Result.Failure(
                    $"{string.Join(", ", sevkEdilmisSandiklar)} numaralı sandık(lar) sevk edildiği için lokasyon değiştirilemez.");
            }

            if (sandiklar.Count == 0 || SandikDepoKurali.BelirsizLokasyonMu(depoLokasyonId))
                return Result.Success();

            var sandikIdler = sandiklar.Select(sandik => sandik.Id).ToHashSet();
            var etkinIceriklerBySandik = await _sandikService
                .GetEtkinSandikIcerikleriAsync(sandikIdler, cancellationToken);
            var atanamayanSandiklar = sandiklar
                .Where(sandik => !SandikDepoKurali.DepoLokasyonuAtanabilir(
                    sandik,
                    etkinIceriklerBySandik.GetValueOrDefault(sandik.Id) ?? Array.Empty<SandikIcerik>()))
                .Select(sandik => sandik.SandikNo)
                .ToList();

            if (atanamayanSandiklar.Count == 0)
                return Result.Success();

            return Result.Failure(
                $"{string.Join(", ", atanamayanSandiklar)} numaralı sandık(lar) için lokasyon atanamaz. {SandikDepoKurali.LokasyonAtamaUyariMesaji}");
        }

        private async Task<Result<IReadOnlyList<Sandik>>> SandiklariGetirAsync(
            IReadOnlyCollection<int> sandikIds)
        {
            var sandiklar = (await _unitOfWork
                    .GetRepository<Sandik>()
                    .FindAsync(sandik => sandikIds.Contains(sandik.Id)))
                .ToList();

            if (sandiklar.Count == sandikIds.Count)
                return Result<IReadOnlyList<Sandik>>.Success(sandiklar);

            return Result<IReadOnlyList<Sandik>>.Failure(
                "Seçilen sandıklardan biri veya birkaçı artık bulunamıyor.",
                404);
        }

        private async Task<Result<IReadOnlyDictionary<int, string>>> LokasyonlariGetirAsync(
            int depoLokasyonId)
        {
            if (depoLokasyonId <= 0)
            {
                return Result<IReadOnlyDictionary<int, string>>.Failure(
                    "Geçerli bir depo lokasyonu seçilmelidir.");
            }

            var lokasyonlar = (await _unitOfWork
                    .GetRepository<LookupDepoLokasyon>()
                    .FindAsync(lokasyon => true))
                .ToDictionary(lokasyon => lokasyon.Id, lokasyon => lokasyon.Deger);

            return lokasyonlar.ContainsKey(depoLokasyonId)
                ? Result<IReadOnlyDictionary<int, string>>.Success(lokasyonlar)
                : Result<IReadOnlyDictionary<int, string>>.Failure(
                    "Seçilen depo lokasyonu bulunamadı.",
                    404);
        }

        private static Result<IReadOnlyCollection<int>> NormalizeSandikIds(
            IReadOnlyCollection<int>? sandikIds)
        {
            if (sandikIds == null || sandikIds.Count == 0)
                return Result<IReadOnlyCollection<int>>.Failure("Güncellenecek sandık seçilmedi.");

            var normalizeIds = sandikIds
                .Where(id => id > 0)
                .Distinct()
                .ToArray();
            if (normalizeIds.Length != sandikIds.Count)
                return Result<IReadOnlyCollection<int>>.Failure("Seçilen sandık bilgileri geçersiz.");

            return Result<IReadOnlyCollection<int>>.Success(normalizeIds);
        }

        private static Result<SandikLokasyonOnayliUygulaCommand> PlanHatasi<T>(Result<T> sonuc)
        {
            var hata = sonuc.Error;
            if (hata?.Issues != null)
            {
                return Result<SandikLokasyonOnayliUygulaCommand>.Failure(
                    hata.Message,
                    sonuc.StatusCode,
                    hata.Issues);
            }

            return Result<SandikLokasyonOnayliUygulaCommand>.Failure(
                hata?.Message ?? "Lokasyon atama planı oluşturulamadı.",
                sonuc.StatusCode);
        }

        private static Result<bool> UygulamaHatasi<T>(Result<T> sonuc)
        {
            var hata = sonuc.Error;
            if (hata?.Issues != null)
                return Result<bool>.Failure(hata.Message, sonuc.StatusCode, hata.Issues);

            return Result<bool>.Failure(
                hata?.Message ?? "Lokasyon atama planı uygulanamadı.",
                sonuc.StatusCode);
        }

        private static bool AyniMetin(string? sol, string? sag) =>
            string.Equals(sol?.Trim(), sag?.Trim(), StringComparison.Ordinal);
    }
}
