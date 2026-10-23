using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.SandikIslemleri.DTOs;
using _3K.Application.Features.SandikIslemleri.Services;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.SandikIslemleri.Queries
{
    /// <summary>
    /// Sandığın sevk sonrası düzeltme durumu: açık düzeltmenin gerekçesi, açılıştan bu yana değişiklikler
    /// (tamamlamadan önce gösterilir) ve tamamlanmış düzeltmelerin geçmişi. Sandık okuma izniyle açılır.
    /// </summary>
    public class GetSandikDuzeltmeDurumuQuery : IRequest<Result<SandikDuzeltmeDurumuDto>>, ISecuredRequest
    {
        public int SandikId { get; set; }
    }

    public class GetSandikDuzeltmeDurumuQueryHandler(IUnitOfWork unitOfWork, ILookupCacheService lookupCache)
        : IRequestHandler<GetSandikDuzeltmeDurumuQuery, Result<SandikDuzeltmeDurumuDto>>
    {
        public async Task<Result<SandikDuzeltmeDurumuDto>> Handle(
            GetSandikDuzeltmeDurumuQuery request, CancellationToken cancellationToken)
        {
            var sandik = (await unitOfWork.GetRepository<Sandik>().FindAsync(s => s.Id == request.SandikId))
                .FirstOrDefault();
            if (sandik == null)
                return Result<SandikDuzeltmeDurumuDto>.Failure("Sandık bulunamadı.", 404);

            var kayitlar = (await unitOfWork.GetRepository<SandikDuzeltmeKaydi>()
                    .FindAsync(k => k.SandikId == sandik.Id))
                .OrderByDescending(k => k.AcilisTarihi)
                .ToList();
            var kullaniciIds = kayitlar
                .SelectMany(k => new[] { k.AcanKullaniciId, k.TamamlayanKullaniciId ?? 0 })
                .Where(id => id > 0)
                .Distinct()
                .ToList();
            var kullanicilar = kullaniciIds.Count == 0
                ? new Dictionary<int, string>()
                : (await unitOfWork.GetRepository<Kullanici>().FindAsync(k => kullaniciIds.Contains(k.Id)))
                    .ToDictionary(k => k.Id, k => k.AdSoyad);

            var acik = sandik.SevkiyatDuzeltmeAcikMi
                ? kayitlar.FirstOrDefault(k => k.TamamlanmaTarihi == null)
                : null;
            var degisiklikler = acik == null
                ? []
                : await new SandikDuzeltmeKayitIslemi(unitOfWork).FarkAsync(acik, sandik, lookupCache, cancellationToken);

            return Result<SandikDuzeltmeDurumuDto>.Success(new SandikDuzeltmeDurumuDto
            {
                SandikId = sandik.Id,
                AcikMi = sandik.SevkiyatDuzeltmeAcikMi,
                Gerekce = acik?.Gerekce,
                AcanKullanici = acik == null ? null : kullanicilar.GetValueOrDefault(acik.AcanKullaniciId),
                AcilisTarihi = acik?.AcilisTarihi,
                Degisiklikler = degisiklikler,
                Gecmis = kayitlar
                    .Where(k => k.TamamlanmaTarihi != null)
                    .Select(k => new SandikDuzeltmeGecmisDto
                    {
                        Gerekce = k.Gerekce,
                        AcanKullanici = kullanicilar.GetValueOrDefault(k.AcanKullaniciId),
                        AcilisTarihi = k.AcilisTarihi,
                        TamamlayanKullanici = k.TamamlayanKullaniciId is int id ? kullanicilar.GetValueOrDefault(id) : null,
                        TamamlanmaTarihi = k.TamamlanmaTarihi,
                        Degisiklikler = string.IsNullOrWhiteSpace(k.FarkOzeti)
                            ? []
                            : k.FarkOzeti.Split('\n')
                    })
                    .ToList()
            });
        }
    }
}
