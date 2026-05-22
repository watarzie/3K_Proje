using MediatR;
using _3K.Core.Enums;
using _3K.Application.Common;
using _3K.Application.Features.ProjeIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Helpers;
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
                var gridKapandiSandikNolari = cekiSatirlari
                    .Where(cs => cs.GridDurumuId == (int)GridDurum.GridKapandi)
                    .Select(cs => cs.FiiliSandikNo ?? cs.CekideGecenSandikNo)
                    .Where(sandikNo => !string.IsNullOrWhiteSpace(sandikNo))
                    .Select(sandikNo => sandikNo!.Trim())
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var toplamSandik = sandiklar.Count;
                var hazirSandik = sandiklar.Count(s => 
                    s.DurumId == (int)SandikDurum.Kapandi || 
                    s.DurumId == (int)SandikDurum.Sevkedildi);
                var depoSandiklar = sandiklar
                    .Where(s => DepodaSayilacakSandik(s, gridKapandiSandikNolari))
                    .Where(s => EtkinDepoLokasyonId(s, gridKapandiSandikNolari) != (int)DepoLokasyon.Belirsiz)
                    .ToList();
                var isSahaYedek = p.ProjeTipiId == (int)ProjeTipi.Saha || p.ProjeTipiId == (int)ProjeTipi.Yedek;
                var sandikIcerikleri = sandiklar.SelectMany(s => s.SandikIcerikleri).ToList();
                
                var toplamUrun = isSahaYedek 
                    ? sandikIcerikleri.Count
                    : cekiSatirlari.Count;
                
                // Tamamlanma ürün bazlı ilerler: normal projelerde kalan 0 olan çekisatırları tamamlanmış sayılır.
                var tamamlananUrun = isSahaYedek 
                    ? sandikIcerikleri.Count(si =>
                    {
                        var istenen = si.CekiSatiri?.IstenenAdet ?? si.Miktar;
                        return istenen > 0 && si.KonulanAdet >= istenen;
                    })
                    : cekiSatirlari.Count(cs => cs.KalanMiktar <= 0);

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
                    BaslamaTarihi = p.CreatedDate,
                    CalismaGunSayisi = HesaplaCalismaGunSayisi(p.CreatedDate, p.GerceklesenSevkTarihi),
                    PlanlananSevkTarihi = p.PlanlananSevkTarihi,
                    GerceklesenSevkTarihi = p.GerceklesenSevkTarihi,
                    SorumluKisi = p.SorumluKisi,
                    SandikSayisi = toplamSandik,
                    HazirSandikSayisi = hazirSandik,
                    DepoSandikSayisi = depoSandiklar.Count,
                    DepoUcKSandikSayisi = depoSandiklar.Count(s => EtkinDepoLokasyonId(s, gridKapandiSandikNolari) == (int)DepoLokasyon.UcK),
                    DepoSeymenSandikSayisi = depoSandiklar.Count(s => EtkinDepoLokasyonId(s, gridKapandiSandikNolari) == (int)DepoLokasyon.Seymen),
                    DepoGridSandikSayisi = depoSandiklar.Count(s => EtkinDepoLokasyonId(s, gridKapandiSandikNolari) == (int)DepoLokasyon.Grid),
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

        private static bool DepodaSayilacakSandik(Sandik sandik, IReadOnlySet<string> gridKapandiSandikNolari)
        {
            if (sandik.DurumId == (int)SandikDurum.Sevkedildi)
                return false;

            if (SandiktaGridKapandiUrunVar(sandik, gridKapandiSandikNolari))
                return true;

            return sandik.SandikIcerikleri.Any(i =>
            {
                var satir = i.CekiSatiri;
                if (satir == null)
                    return i.Miktar > 0 || i.KonulanAdet > 0 || i.StokKarsilanan > 0 || i.ProjeKarsilanan > 0 || i.TedarikciKarsilanan > 0;

                return satir.GelenMiktar > 0
                    || satir.ProjeKarsilanan > 0
                    || satir.StokKarsilanan > 0
                    || satir.TedarikciKarsilanan > 0;
            });
        }

        private static int EtkinDepoLokasyonId(Sandik sandik, IReadOnlySet<string> gridKapandiSandikNolari)
        {
            return SandiktaGridKapandiUrunVar(sandik, gridKapandiSandikNolari)
                ? (int)DepoLokasyon.Grid
                : sandik.DepoLokasyonId;
        }

        private static bool SandiktaGridKapandiUrunVar(Sandik sandik, IReadOnlySet<string> gridKapandiSandikNolari)
        {
            return gridKapandiSandikNolari.Contains(sandik.SandikNo.Trim())
                || sandik.SandikIcerikleri.Any(i => i.CekiSatiri?.GridDurumuId == (int)GridDurum.GridKapandi);
        }

        private static int HesaplaCalismaGunSayisi(DateTime baslamaTarihi, DateTime? bitisTarihi)
        {
            var bitis = (bitisTarihi ?? TurkeyTime.Now).Date;
            var gunSayisi = (bitis - baslamaTarihi.Date).Days;
            return Math.Max(0, gunSayisi);
        }
    }
}
