using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.ProjeIslemleri.DTOs;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.ProjeIslemleri.Queries
{
    public class GetSahaAktarimlariQuery : IRequest<Result<List<SahaAktarimDto>>>, ISecuredRequest, IRequiresMenuPermission
    {
        public string RequiredMenuKod => YetkiKodlari.Saha.AktarimGeriAl;
        public YetkiTipi RequiredYetkiTipi => YetkiTipi.R;
        public int ProjeId { get; set; }
    }

    public class GetSahaAktarimlariQueryHandler : IRequestHandler<GetSahaAktarimlariQuery, Result<List<SahaAktarimDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILookupCacheService _lookupCache;

        public GetSahaAktarimlariQueryHandler(IUnitOfWork unitOfWork, ILookupCacheService lookupCache)
        {
            _unitOfWork = unitOfWork;
            _lookupCache = lookupCache;
        }

        public async Task<Result<List<SahaAktarimDto>>> Handle(GetSahaAktarimlariQuery request, CancellationToken cancellationToken)
        {
            if (request.ProjeId <= 0)
                return Result<List<SahaAktarimDto>>.Failure("Saha projesi seçilmelidir.", 400);

            var projeRepo = _unitOfWork.GetRepository<Proje>();
            var sahaProje = await projeRepo.GetByIdAsync(request.ProjeId);
            if (sahaProje == null)
                return Result<List<SahaAktarimDto>>.Failure("Saha projesi bulunamadı.", 404);

            if (sahaProje.ProjeTipiId != (int)ProjeTipi.Saha)
                return Result<List<SahaAktarimDto>>.Failure("Aktarım listesi yalnızca saha projeleri için görüntülenebilir.", 400);

            var cekiRepo = _unitOfWork.GetRepository<Ceki>();
            var cekiSatiriRepo = _unitOfWork.GetRepository<CekiSatiri>();
            var sandikIcerikRepo = _unitOfWork.GetRepository<SandikIcerik>();
            var sandikRepo = _unitOfWork.GetRepository<Sandik>();

            var sahaCekiIds = (await cekiRepo.FindAsync(c => c.ProjeId == sahaProje.Id))
                .Select(c => c.Id)
                .ToList();

            if (sahaCekiIds.Count == 0)
                return Result<List<SahaAktarimDto>>.Success(new List<SahaAktarimDto>());

            var sahaSatirlar = (await cekiSatiriRepo.FindAsync(cs =>
                    sahaCekiIds.Contains(cs.CekiId) &&
                    cs.KaynakCekiSatiriId.HasValue))
                .OrderBy(cs => cs.SiraNo)
                .ToList();

            if (sahaSatirlar.Count == 0)
                return Result<List<SahaAktarimDto>>.Success(new List<SahaAktarimDto>());

            var sahaSatirIds = sahaSatirlar.Select(cs => cs.Id).ToList();
            var kaynakSatirIds = sahaSatirlar
                .Select(cs => cs.KaynakCekiSatiriId!.Value)
                .Distinct()
                .ToList();

            var sahaIcerikler = (await sandikIcerikRepo.FindAsync(si =>
                    si.CekiSatiriId.HasValue &&
                    sahaSatirIds.Contains(si.CekiSatiriId.Value)))
                .ToList();

            var sahaSandikIds = sahaIcerikler
                .Select(si => si.SandikId)
                .Distinct()
                .ToList();

            var sahaSandiklar = (await sandikRepo.FindAsync(s => sahaSandikIds.Contains(s.Id)))
                .ToDictionary(s => s.Id);

            var kaynakSatirlar = (await cekiSatiriRepo.FindAsync(cs => kaynakSatirIds.Contains(cs.Id)))
                .ToDictionary(cs => cs.Id);

            var kaynakCekiIds = kaynakSatirlar.Values
                .Select(cs => cs.CekiId)
                .Distinct()
                .ToList();

            var kaynakCekiler = (await cekiRepo.FindAsync(c => kaynakCekiIds.Contains(c.Id)))
                .ToDictionary(c => c.Id);

            var kaynakProjeIds = kaynakCekiler.Values
                .Select(c => c.ProjeId)
                .Distinct()
                .ToList();

            var kaynakProjeler = (await projeRepo.FindAsync(p => kaynakProjeIds.Contains(p.Id)))
                .ToDictionary(p => p.Id);

            var icerikMap = sahaIcerikler
                .GroupBy(si => si.CekiSatiriId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Satırın aktif defter kaydı hangi aktarım başlığına aitse "aktarımın tamamını geri al" o başlıkla çalışır.
            var aktifKalemMap = (await _unitOfWork.GetRepository<SahaAktarimKalemi>().FindAsync(k =>
                    k.SahaCekiSatiriId.HasValue &&
                    sahaSatirIds.Contains(k.SahaCekiSatiriId.Value) &&
                    k.DurumId != (int)SahaAktarimDurum.GeriAlindi &&
                    k.DurumId != (int)SahaAktarimDurum.Iptal))
                .GroupBy(k => k.SahaCekiSatiriId!.Value)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(k => k.Id).First());
            var aktarimIds = aktifKalemMap.Values.Select(k => k.SahaAktarimId).Distinct().ToList();
            var aktarimMap = aktarimIds.Count == 0
                ? new Dictionary<int, SahaAktarim>()
                : (await _unitOfWork.GetRepository<SahaAktarim>().FindAsync(a => aktarimIds.Contains(a.Id)))
                    .ToDictionary(a => a.Id);

            var result = new List<SahaAktarimDto>();

            foreach (var sahaSatir in sahaSatirlar)
            {
                if (!sahaSatir.KaynakCekiSatiriId.HasValue ||
                    !kaynakSatirlar.TryGetValue(sahaSatir.KaynakCekiSatiriId.Value, out var kaynakSatir) ||
                    !kaynakCekiler.TryGetValue(kaynakSatir.CekiId, out var kaynakCeki) ||
                    !kaynakProjeler.TryGetValue(kaynakCeki.ProjeId, out var kaynakProje))
                {
                    continue;
                }

                var satirIcerikleri = icerikMap.GetValueOrDefault(sahaSatir.Id) ?? new List<SandikIcerik>();
                var ilkIcerik = satirIcerikleri.FirstOrDefault();
                Sandik? sahaSandik = null;
                if (ilkIcerik != null)
                    sahaSandiklar.TryGetValue(ilkIcerik.SandikId, out sahaSandik);

                var sevkEdildiMi = satirIcerikleri
                    .Select(si => si.SandikId)
                    .Distinct()
                    .Any(id => sahaSandiklar.TryGetValue(id, out var sandik) && sandik.DurumId == (int)SandikDurum.Sevkedildi);
                var islemGormusMu = SahaAktarimGeriAlmaPolicy.IslemGormusMu(sahaSatir, satirIcerikleri);
                var geriAlinabilirMi = !sevkEdildiMi && !islemGormusMu;
                SahaAktarim? aktarim = null;
                if (aktifKalemMap.TryGetValue(sahaSatir.Id, out var aktifKalem))
                    aktarimMap.TryGetValue(aktifKalem.SahaAktarimId, out aktarim);

                result.Add(new SahaAktarimDto
                {
                    SahaCekiSatiriId = sahaSatir.Id,
                    SahaProjeId = sahaProje.Id,
                    SahaProjeNo = sahaProje.ProjeNo,
                    SahaSandikId = sahaSandik?.Id,
                    SahaSandikNo = sahaSandik?.SandikNo ?? sahaSatir.FiiliSandikNo ?? sahaSatir.CekideGecenSandikNo,
                    KaynakProjeId = kaynakProje.Id,
                    KaynakProjeNo = kaynakProje.ProjeNo,
                    KaynakSandikNo = kaynakSatir.FiiliSandikNo ?? kaynakSatir.CekideGecenSandikNo,
                    SiraNo = kaynakSatir.SiraNo,
                    BarkodNo = kaynakSatir.BarkodNo,
                    Aciklama = kaynakSatir.Aciklama,
                    Miktar = sahaSatir.IstenenAdet,
                    Birim = _lookupCache.GetDeger<LookupBirim>(sahaSatir.BirimId),
                    DurumMetni = sahaSandik != null
                        ? _lookupCache.GetDeger<LookupSandikDurum>(sahaSandik.DurumId)
                        : _lookupCache.GetDeger<LookupUrunDurum>(sahaSatir.DurumId),
                    SevkEdildiMi = sevkEdildiMi,
                    IslemGormusMu = islemGormusMu,
                    GeriAlinabilirMi = geriAlinabilirMi,
                    SahaAktarimId = aktarim?.Id,
                    SahaAktarimTarihi = aktarim?.Tarih,
                    SahaAktarimAciklama = aktarim?.Aciklama,
                    SahaAktarimTasimaMi = aktarim?.KaynakSahaProjeId.HasValue == true,
                    GeriAlinamamaNedeni = geriAlinabilirMi
                        ? null
                        : sevkEdildiMi
                            ? "Saha sandığı sevk edildiği için geri alınamaz."
                            : "Saha projesinde Grid/3K işlemi başladığı için önce bu işlemler geri alınmalıdır."
                });
            }

            return Result<List<SahaAktarimDto>>.Success(
                result
                    .OrderBy(x => x.KaynakProjeNo)
                    .ThenBy(x => x.KaynakSandikNo)
                    .ThenBy(x => x.SiraNo)
                    .ToList());
        }

    }
}
