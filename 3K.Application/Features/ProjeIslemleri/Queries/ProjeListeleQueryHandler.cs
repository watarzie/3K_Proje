using MediatR;
using _3K.Core.Enums;
using _3K.Application.Common;
using _3K.Application.Features.ProjeIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.ProjeIslemleri.Queries
{
    public class ProjeListeleQueryHandler : IRequestHandler<ProjeListeleQuery, Result<IEnumerable<ProjeDto>>>
    {
        private readonly IProjeRepository _projeRepository;
        private readonly ILookupCacheService _lookupCache;

        public ProjeListeleQueryHandler(IProjeRepository projeRepository, ILookupCacheService lookupCache)
        {
            _projeRepository = projeRepository;
            _lookupCache = lookupCache;
        }

        public async Task<Result<IEnumerable<ProjeDto>>> Handle(ProjeListeleQuery request, CancellationToken cancellationToken)
        {
            var projeler = await _projeRepository.GetAllWithDetailsAsync(cancellationToken);

            // ProjeTipiId filtresi
            if (request.ProjeTipiId.HasValue)
            {
                projeler = projeler.Where(p => p.ProjeTipiId == request.ProjeTipiId.Value);
            }

            var result = projeler.Select(p =>
            {
                var sandiklar = p.Sandiklar ?? new List<Sandik>();
                var cekiSatirlari = p.Cekiler?.SelectMany(c => c.CekiSatirlari).ToList()
                                    ?? new List<CekiSatiri>();

                var toplamSandik = sandiklar.Count;
                var hazirSandik = sandiklar.Count(s => s.DurumId == (int)SandikDurum.Hazir);
                var toplamUrun = cekiSatirlari.Count;
                var tamamlananUrun = cekiSatirlari.Count(cs =>
                    cs.GridDurumuId != (int)GridDurum.Bekliyor || cs.UcKKarsilamaTipiId != (int)UcKDurum.Bekliyor);

                // Durum hesaplama
                int durumId;
                if (p.DurumId == (int)ProjeDurum.SevkEdildi)
                    durumId = (int)ProjeDurum.SevkEdildi; // Kilitli/Sevk Edilmiş proje statüsü ezilemez
                else if (toplamUrun == 0)
                    durumId = (int)ProjeDurum.Hazirlaniyor;
                else if (hazirSandik == toplamSandik && toplamSandik > 0)
                    durumId = (int)ProjeDurum.Tamamlandi;
                else if (tamamlananUrun > 0)
                    durumId = (int)ProjeDurum.Devam;
                else
                    durumId = p.DurumId;

                return new ProjeDto
                {
                    Id = p.Id,
                    ProjeNo = p.ProjeNo,
                    Musteri = p.Musteri,
                    DurumId = durumId,
                    DurumMetni = _lookupCache.GetDeger<LookupProjeDurum>(durumId),
                    ProjeTipiId = p.ProjeTipiId,
                    ProjeTipiMetni = _lookupCache.GetDeger<LookupProjeTipi>(p.ProjeTipiId),
                    PlanlananSevkTarihi = p.PlanlananSevkTarihi,
                    SorumluKisi = p.SorumluKisi,
                    SandikSayisi = toplamSandik,
                    HazirSandikSayisi = hazirSandik,
                    ToplamUrunSayisi = toplamUrun,
                    TamamlananUrunSayisi = tamamlananUrun,
                    FBNo = p.FBNo,
                    Guc = p.Guc,
                    Gerilim = p.Gerilim,
                    Lokasyon = p.Lokasyon,
                    OlcuResmiNo = p.OlcuResmiNo,
                    NakilOlcuResmiNo = p.NakilOlcuResmiNo,
                    SonMontajResmiNo = p.SonMontajResmiNo,
                    ProjeMuduru = p.ProjeMuduru
                };
            });

            return Result<IEnumerable<ProjeDto>>.Success(result);
        }
    }
}
