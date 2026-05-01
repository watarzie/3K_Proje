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
                var hazirSandik = sandiklar.Count(s => 
                    s.DurumId == (int)SandikDurum.Hazir || 
                    s.DurumId == (int)SandikDurum.Sevkedildi);
                var isSahaYedek = p.ProjeTipiId == (int)ProjeTipi.Saha || p.ProjeTipiId == (int)ProjeTipi.Yedek;
                
                var toplamUrun = isSahaYedek 
                    ? sandiklar.SelectMany(s => s.SandikIcerikleri).Count() 
                    : cekiSatirlari.Count;
                
                // Gerçek tamamlanan: 3K durumu TamGeldi olanlar (veya GridDurumuId TamGeldi)
                var tamamlananUrun = isSahaYedek 
                    ? sandiklar.SelectMany(s => s.SandikIcerikleri)
                        .Count(si => si.CekiSatiri != null && 
                            (si.CekiSatiri.UcKKarsilamaTipiId == (int)UcKDurum.TamGeldi
                            || si.CekiSatiri.UcKKarsilamaTipiId == (int)UcKDurum.KontrolEdildi
                            || si.CekiSatiri.UcKKarsilamaTipiId == (int)UcKDurum.ProjedenKarsilandi
                            || si.CekiSatiri.UcKKarsilamaTipiId == (int)UcKDurum.StoktanKarsilandi
                            || si.CekiSatiri.UcKKarsilamaTipiId == (int)UcKDurum.TedarikcidenGeldi))
                    : cekiSatirlari.Count(cs => 
                        cs.GelenMiktar + cs.StokKarsilanan + cs.ProjeKarsilanan + cs.TedarikciKarsilanan >= cs.IstenenAdet);

                // Durum hesaplama
                int durumId;
                if (p.DurumId == (int)ProjeDurum.SevkEdildi)
                    durumId = (int)ProjeDurum.SevkEdildi; // Kilitli/Sevk Edilmiş proje statüsü ezilemez
                else if (hazirSandik == toplamSandik && toplamSandik > 0)
                    durumId = (int)ProjeDurum.Tamamlandi;
                else
                    durumId = (int)ProjeDurum.Hazirlaniyor; // Proje üzerinde çalışılıyor veya henüz başlanmadı

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
                    GerceklesenSevkTarihi = p.GerceklesenSevkTarihi,
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
