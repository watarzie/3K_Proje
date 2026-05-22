using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.SandikIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    public class SandikLokasyonGuncelleCommandHandler : IRequestHandler<SandikLokasyonGuncelleCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IHareketService _hareketService;

        public SandikLokasyonGuncelleCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IHareketService hareketService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _hareketService = hareketService;
        }

        public async Task<Result<bool>> Handle(SandikLokasyonGuncelleCommand request, CancellationToken cancellationToken)
        {
            if (!_currentUserService.IsAuthenticated) return Result<bool>.Failure("Oturum açmanız gerekiyor.");
            
            if (request.SandikIds == null || !request.SandikIds.Any())
            {
                return Result<bool>.Failure("Güncellenecek sandık seçilmedi.");
            }

            var repo = _unitOfWork.GetRepository<Sandik>();
            var sandiklar = (await repo.FindAsync(s => request.SandikIds.Contains(s.Id))).ToList();

            if (!sandiklar.Any())
            {
                return Result<bool>.Failure("Sandıklar bulunamadı.");
            }

            var lokasyonRepo = _unitOfWork.GetRepository<LookupDepoLokasyon>();
            var lokasyonlar = (await lokasyonRepo.FindAsync(l => true))
                .ToDictionary(l => l.Id, l => l.Deger);

            if (!lokasyonlar.ContainsKey(request.DepoLokasyonId))
            {
                return Result<bool>.Failure("Seçilen depo lokasyonu bulunamadı.");
            }

            if (!SandikDepoKurali.BelirsizLokasyonMu(request.DepoLokasyonId))
            {
                var sandikIdler = sandiklar.Select(s => s.Id).ToHashSet();
                var icerikRepo = _unitOfWork.GetRepository<SandikIcerik>();
                var cekiSatiriRepo = _unitOfWork.GetRepository<CekiSatiri>();

                var tumIcerikler = (await icerikRepo.FindAsync(i => sandikIdler.Contains(i.SandikId))).ToList();
                var cekiSatiriIdler = tumIcerikler
                    .Where(i => i.CekiSatiriId.HasValue)
                    .Select(i => i.CekiSatiriId!.Value)
                    .Distinct()
                    .ToList();

                var cekiSatirlari = cekiSatiriIdler.Count == 0
                    ? new Dictionary<int, CekiSatiri>()
                    : (await cekiSatiriRepo.FindAsync(c => cekiSatiriIdler.Contains(c.Id))).ToDictionary(c => c.Id);

                var iceriklerBySandik = tumIcerikler
                    .GroupBy(i => i.SandikId)
                    .ToDictionary(g => g.Key, g => (IReadOnlyCollection<SandikIcerik>)g.ToList());

                var atanamayanSandiklar = sandiklar
                    .Where(s => s.DepoLokasyonId != request.DepoLokasyonId)
                    .Where(s => !SandikDepoKurali.DepoLokasyonuAtanabilir(
                        s,
                        iceriklerBySandik.GetValueOrDefault(s.Id) ?? Array.Empty<SandikIcerik>(),
                        cekiSatirlari))
                    .Select(s => s.SandikNo)
                    .ToList();

                if (atanamayanSandiklar.Any())
                {
                    var sandikNoListesi = string.Join(", ", atanamayanSandiklar);
                    return Result<bool>.Failure($"{sandikNoListesi} numaralı sandık(lar) için lokasyon atanamaz. {SandikDepoKurali.LokasyonAtamaUyariMesaji}");
                }
            }

            foreach (var sandik in sandiklar)
            {
                var eskiLokasyon = sandik.DepoLokasyonId;
                sandik.DepoLokasyonId = request.DepoLokasyonId;
                repo.Update(sandik);

                var eskiLokasyonMetni = lokasyonlar.GetValueOrDefault(eskiLokasyon, eskiLokasyon.ToString());
                var yeniLokasyonMetni = lokasyonlar.GetValueOrDefault(sandik.DepoLokasyonId, sandik.DepoLokasyonId.ToString());

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

            await _unitOfWork.SaveChangesAsync();
            return Result<bool>.Success(true);
        }
    }
}
