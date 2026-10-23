using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.SandikIslemleri.Services;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    public class SandikSevkiyatDuzeltmeTamamlaCommand : IRequest<Result>, ISecuredRequest
    {
        public int ProjeId { get; set; }
        public int SandikId { get; set; }
    }

    /// <summary>
    /// Projede sevk sonrası düzeltmeye açık bütün sandıkların düzeltmesini tek işlemde tamamlar; döner değer
    /// tamamlanan sandık sayısıdır. Sandık bazındaki tamamlamayla aynı kuralları uygular.
    /// </summary>
    public class SandikSevkiyatDuzeltmeTopluTamamlaCommand : IRequest<Result<int>>, ISecuredRequest
    {
        public int ProjeId { get; set; }
    }

    public class SandikSevkiyatDuzeltmeTamamlaCommandHandler :
        IRequestHandler<SandikSevkiyatDuzeltmeTamamlaCommand, Result>,
        IRequestHandler<SandikSevkiyatDuzeltmeTopluTamamlaCommand, Result<int>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHareketService _hareketService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ISahaTamamlamaService _sahaTamamlamaService;
        private readonly IReadQueryExecutor _readQueries;
        private readonly ILookupCacheService _lookupCache;

        public SandikSevkiyatDuzeltmeTamamlaCommandHandler(
            IUnitOfWork unitOfWork,
            IHareketService hareketService,
            ICurrentUserService currentUserService,
            ISahaTamamlamaService sahaTamamlamaService,
            IReadQueryExecutor readQueries,
            ILookupCacheService lookupCache)
        {
            _unitOfWork = unitOfWork;
            _hareketService = hareketService;
            _currentUserService = currentUserService;
            _sahaTamamlamaService = sahaTamamlamaService;
            _readQueries = readQueries;
            _lookupCache = lookupCache;
        }

        public async Task<Result> Handle(SandikSevkiyatDuzeltmeTamamlaCommand request, CancellationToken cancellationToken)
        {
            var sandikRepo = _unitOfWork.GetRepository<Sandik>();

            if (!_currentUserService.UserId.HasValue)
                return Result.Failure("Oturum açmanız gerekiyor.", 401);

            var sandik = await sandikRepo.GetByIdAsync(request.SandikId);
            if (sandik == null || sandik.ProjeId != request.ProjeId)
                return Result.Failure("Sandık bulunamadı veya projeye ait değil.", 404);

            if (sandik.DurumId != (int)SandikDurum.Sevkedildi)
                return Result.Failure("Sadece sevk edilmiş sandıklarda düzeltme tamamlanabilir.");

            if (!sandik.SevkiyatDuzeltmeAcikMi)
                return Result.Failure("Bu sandıkta açık bir sevkiyat düzeltmesi bulunmuyor.");

            var proje = await _unitOfWork.GetRepository<Proje>().GetByIdAsync(request.ProjeId);
            if (proje == null)
                return Result.Failure("Proje bulunamadı.", 404);

            await TamamlaAsync(proje, [sandik], cancellationToken);
            return Result.Success();
        }

        public async Task<Result<int>> Handle(SandikSevkiyatDuzeltmeTopluTamamlaCommand request, CancellationToken cancellationToken)
        {
            if (!_currentUserService.UserId.HasValue)
                return Result<int>.Failure("Oturum açmanız gerekiyor.", 401);

            var proje = await _unitOfWork.GetRepository<Proje>().GetByIdAsync(request.ProjeId);
            if (proje == null)
                return Result<int>.Failure("Proje bulunamadı.", 404);

            // İzlenen sandıklar üzerinden: bayrak kapanışı ve fark kaydı aynı kaydetmede yazılır.
            var sandikRepo = _unitOfWork.GetRepository<Sandik>();
            var sandiklar = await _readQueries.ToListAsync(
                sandikRepo.Queryable()
                    .Where(s => s.ProjeId == proje.Id &&
                        s.DurumId == (int)SandikDurum.Sevkedildi &&
                        s.SevkiyatDuzeltmeAcikMi)
                    .OrderBy(s => s.Id),
                cancellationToken);
            if (sandiklar.Count == 0)
                return Result<int>.Failure("Bu projede açık sevk sonrası düzeltme bulunmuyor.", 409);

            await TamamlaAsync(proje, sandiklar, cancellationToken);
            return Result<int>.Success(sandiklar.Count);
        }

        /// <summary>
        /// Sandıkların düzeltme kayıtlarını farkıyla kapatır, düzeltme bayrağını indirir, proje durumunu bir kez yeniden
        /// hesaplar ve her sandık için hareket yazar. Sandıklar sevkli kalır; sevkiyat kaydı değişmez.
        /// </summary>
        private async Task TamamlaAsync(Proje proje, IReadOnlyList<Sandik> sandiklar, CancellationToken cancellationToken)
        {
            var kullaniciId = _currentUserService.UserId!.Value;
            var duzeltmeKayitIslemi = new SandikDuzeltmeKayitIslemi(_unitOfWork);
            var farklar = new Dictionary<int, IReadOnlyList<string>?>();
            foreach (var sandik in sandiklar)
            {
                // Fark, kilit kapanmadan önceki son içerikten çıkarılır ve düzeltme kaydıyla aynı kaydetmede yazılır.
                farklar[sandik.Id] = await duzeltmeKayitIslemi.TamamlaAsync(sandik, kullaniciId, _lookupCache, cancellationToken);
                sandik.SevkiyatDuzeltmeAcikMi = false;
            }

            if (proje.ProjeTipiId == (int)ProjeTipi.Normal)
            {
                var sandikRepo = _unitOfWork.GetRepository<Sandik>();
                var projeSandiklari = await _readQueries.ToListAsync(
                    _readQueries.AsNoTracking(sandikRepo.Queryable())
                        .Where(s => s.ProjeId == proje.Id), cancellationToken);
                var sahaDurumu = await _sahaTamamlamaService.GetKaynakSandikSahaAktarimDurumuAsync(
                    projeSandiklari.Select(s => s.Id), cancellationToken);
                var yeniDurum = await NormalProjeSevkDurumHesaplayici.HesaplaAsync(
                    _unitOfWork, _readQueries, _sahaTamamlamaService,
                    proje.Id, proje.DurumId, projeSandiklari,
                    sahaDurumu.SahaUzerindenSevkEdilenSandikIds, cancellationToken);

                if (yeniDurum != proje.DurumId)
                {
                    proje.DurumId = yeniDurum;
                    _unitOfWork.GetRepository<Proje>().Update(proje);
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (proje.ProjeTipiId == (int)ProjeTipi.Saha)
            {
                await _sahaTamamlamaService.SenkronizeKaynakProjelerBySahaSandikIdsAsync(
                    sandiklar.Select(s => s.Id).ToList(),
                    cancellationToken);
            }

            foreach (var sandik in sandiklar)
            {
                var fark = farklar[sandik.Id];
                await _hareketService.HareketKaydetAsync(new HareketGecmisi
                {
                    ProjeId = proje.Id,
                    KullaniciId = kullaniciId,
                    ReferansTipi = "Sandik",
                    ReferansId = sandik.Id.ToString(),
                    Islem = "Sevkiyat Düzeltmesi Tamamlandı",
                    IslemTipiId = null,
                    EskiDeger = "SevkiyatDuzeltmeAcikMi:True",
                    YeniDeger = $"SevkiyatDuzeltmeAcikMi:{sandik.SevkiyatDuzeltmeAcikMi}",
                    Aciklama = $"Sandık {sandik.SandikNo} için sevkiyat kaydı korunarak açılan düzeltme tamamlandı. Sandık sevk edilmiş durumda kilitlendi." +
                        (fark == null ? string.Empty : $" Değişiklikler: {string.Join("; ", fark)}")
                });
            }
        }
    }
}
