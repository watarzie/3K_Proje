using MediatR;
using _3K.Core.Enums;
using _3K.Application.Common;
using _3K.Application.Features.ProjeIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.ProjeIslemleri.Queries
{
    public class ProjeListeleQueryHandler : IRequestHandler<ProjeListeleQuery, Result<PaginatedList<ProjeDto>>>
    {
        private readonly IProjeRepository _projeRepository;
        private readonly ILookupCacheService _lookupCache;
        private readonly ISahaTamamlamaService _sahaTamamlamaService;

        public ProjeListeleQueryHandler(
            IProjeRepository projeRepository,
            ILookupCacheService lookupCache,
            ISahaTamamlamaService sahaTamamlamaService)
        {
            _projeRepository = projeRepository;
            _lookupCache = lookupCache;
            _sahaTamamlamaService = sahaTamamlamaService;
        }

        public async Task<Result<PaginatedList<ProjeDto>>> Handle(ProjeListeleQuery request, CancellationToken cancellationToken)
        {
            var (projeler, totalCount) = await _projeRepository.GetFilteredPagedAsync(
                request.ProjeTipiId, request.SearchTerm, request.IsSevkEdilen,
                request.PageNumber, request.PageSize, cancellationToken);

            var normalKaynakSatirIds = projeler
                .Where(p => p.ProjeTipiId == (int)ProjeTipi.Normal)
                .SelectMany(p => p.Cekiler?.SelectMany(c => c.CekiSatirlari) ?? Enumerable.Empty<CekiSatiri>())
                .Where(cs => !cs.KaynakCekiSatiriId.HasValue)
                .Select(cs => cs.Id)
                .ToList();
            var sahaTamamlamaMap = await _sahaTamamlamaService.GetSevkEdilenTamamlamaMapAsync(normalKaynakSatirIds, cancellationToken);

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
                var sevkEdilmisSandikSayisi = sandiklar.Count(s => s.DurumId == (int)SandikDurum.Sevkedildi);
                var hazirSandik = sandiklar.Count(s => 
                    s.DurumId == (int)SandikDurum.Kapandi || 
                    s.DurumId == (int)SandikDurum.Sevkedildi);
                var depoSandiklar = sandiklar
                    .Where(s => DepodaSayilacakSandik(s, gridKapandiSandikNolari))
                    .Where(s => s.DepoLokasyonId != (int)DepoLokasyon.Belirsiz)
                    .ToList();
                var depoDagilimlari = depoSandiklar
                    .GroupBy(s => s.DepoLokasyonId)
                    .Select(g => new ProjeDepoDagilimDto
                    {
                        DepoLokasyonId = g.Key,
                        DepoLokasyonMetni = _lookupCache.GetDeger<LookupDepoLokasyon>(g.Key),
                        SandikSayisi = g.Count()
                    })
                    .OrderBy(x => x.DepoLokasyonId)
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
                    : cekiSatirlari.Count(cs => CekiSatiriKalanHelper.HesaplaEtkinKalan(cs, sahaTamamlamaMap) <= 0);
                var normalUrunlerTamamlandi = !isSahaYedek && toplamUrun > 0 && tamamlananUrun == toplamUrun;

                // Durum hesaplama
                int durumId;
                if (p.DurumId == (int)ProjeDurum.SevkEdildi ||
                    (p.DurumId == (int)ProjeDurum.EksikSevkEdildi && normalUrunlerTamamlandi))
                    durumId = (int)ProjeDurum.SevkEdildi;
                else if (p.DurumId == (int)ProjeDurum.EksikSevkEdildi)
                    durumId = p.DurumId; // Kilitli/Sevk edilmiş proje statüsü ezilemez
                else if (hazirSandik == toplamSandik && toplamSandik > 0)
                    durumId = (int)ProjeDurum.Tamamlandi;
                else
                    durumId = (int)ProjeDurum.Hazirlaniyor; // Proje üzerinde çalışılıyor veya henüz başlanmadı

                var aktifSevkKaydiVar = durumId == (int)ProjeDurum.SevkEdildi ||
                    durumId == (int)ProjeDurum.EksikSevkEdildi ||
                    sevkEdilmisSandikSayisi > 0;
                var gerceklesenSevkTarihi = aktifSevkKaydiVar ? p.GerceklesenSevkTarihi : null;

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
                    CalismaGunSayisi = HesaplaCalismaGunSayisi(p.CreatedDate, gerceklesenSevkTarihi),
                    PlanlananSevkTarihi = p.PlanlananSevkTarihi,
                    GerceklesenSevkTarihi = gerceklesenSevkTarihi,
                    SorumluKisi = p.SorumluKisi,
                    SandikSayisi = toplamSandik,
                    HazirSandikSayisi = hazirSandik,
                    DepoSandikSayisi = depoSandiklar.Count,
                    DepoUcKSandikSayisi = depoSandiklar.Count(s => s.DepoLokasyonId == (int)DepoLokasyon.UcK),
                    DepoSeymenSandikSayisi = depoSandiklar.Count(s => s.DepoLokasyonId == (int)DepoLokasyon.Seymen),
                    DepoGridSandikSayisi = depoSandiklar.Count(s => s.DepoLokasyonId == (int)DepoLokasyon.Grid),
                    DepoDagilimlari = depoDagilimlari,
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

            var resultList = result.ToList();
            var paginatedList = new PaginatedList<ProjeDto>(resultList, totalCount, request.PageNumber, request.PageSize);
            return Result<PaginatedList<ProjeDto>>.Success(paginatedList);
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
