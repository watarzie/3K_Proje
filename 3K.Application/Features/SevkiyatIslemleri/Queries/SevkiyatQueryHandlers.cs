using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.SevkiyatIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.SevkiyatIslemleri.Queries;

/// <summary>Sevkiyatlar sayfası sorguları: sefer listesi, sefer detayı ve istatistikler.</summary>
public sealed class SevkiyatQueryHandlers(IUnitOfWork unitOfWork, IReadQueryExecutor readQueries) :
    IRequestHandler<SevkSeferleriQuery, Result<PaginatedList<SevkSeferiOzetDto>>>,
    IRequestHandler<SevkSeferiDetayQuery, Result<SevkSeferiDetayDto>>,
    IRequestHandler<SevkiyatIstatistikQuery, Result<SevkiyatIstatistikDto>>
{
    public async Task<Result<PaginatedList<SevkSeferiOzetDto>>> Handle(SevkSeferleriQuery request, CancellationToken cancellationToken)
    {
        var satirlar = await SevkSeferiOkuyucu.SatirlarAsync(unitOfWork, readQueries, request.Baslangic, request.Bitis,
            request.ProjeTipiId, cancellationToken);
        var projeNo = request.ProjeNo?.Trim();
        var seferler = SevkSeferiOkuyucu.Seferler(satirlar)
            .Where(x => SevkSeferiOkuyucu.PlakaEslesir(x.AracPlaka, request.Plaka))
            .Where(x => string.IsNullOrEmpty(projeNo) ||
                        x.ProjeNolari.Any(p => p.Contains(projeNo, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var pageNumber = Math.Max(1, request.PageNumber);
        var sayfa = seferler.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
        return Result<PaginatedList<SevkSeferiOzetDto>>.Success(new PaginatedList<SevkSeferiOzetDto>(sayfa, seferler.Count, pageNumber, pageSize));
    }

    public async Task<Result<SevkSeferiDetayDto>> Handle(SevkSeferiDetayQuery request, CancellationToken cancellationToken)
    {
        var query = unitOfWork.GetRepository<Sevkiyat>().Queryable().Where(s => s.Sandiklar.Any());
        if (request.SeferId is int seferId)
        {
            query = query.Where(s => s.SevkSeferiId == seferId);
        }
        else if (request.EskiTarih.HasValue)
        {
            var gun = request.EskiTarih.Value.Date;
            var sonraki = gun.AddDays(1);
            query = query.Where(s => s.SevkSeferiId == null && s.SevkTarihi >= gun && s.SevkTarihi < sonraki);
        }
        else
        {
            return Result<SevkSeferiDetayDto>.Failure("Sefer belirtilmelidir.");
        }

        var sevkiyatlar = await readQueries.ToListAsync(query
            .OrderBy(s => s.Proje.ProjeNo)
            .Select(s => new
            {
                s.Id,
                s.ProjeId,
                s.Proje.ProjeNo,
                s.Proje.ProjeTipiId,
                s.SevkiyatNo,
                s.AracPlaka,
                s.Aciklama,
                KullaniciAdSoyad = s.Kullanici.AdSoyad,
                Sandiklar = s.Sandiklar.Select(ss => new SevkSeferiSandikDto
                {
                    SandikId = ss.SandikId,
                    SandikNo = ss.Sandik.SandikNo,
                    SandikAdi = ss.Sandik.Ad,
                    BrutKg = ss.Sandik.GrossKg,
                    SevkiyatDuzeltmeAcikMi = ss.Sandik.SevkiyatDuzeltmeAcikMi
                }).ToList()
            }), cancellationToken);
        if (!request.SeferId.HasValue)
        {
            // Eski kayıt seferi: aynı gün ve aynı (normalize) plaka.
            var plaka = AracPlakaKurali.Normalize(request.EskiPlaka);
            sevkiyatlar = sevkiyatlar.Where(s => AracPlakaKurali.Normalize(s.AracPlaka) == plaka).ToList();
        }
        if (sevkiyatlar.Count == 0)
            return Result<SevkSeferiDetayDto>.Failure("Sefer bulunamadı.", 404);

        var idler = sevkiyatlar.Select(s => s.Id).ToArray();
        var satirlar = await SevkSeferiOkuyucu.SatirlarAsync(unitOfWork, readQueries, null, null, null, cancellationToken, idler);
        var ozet = SevkSeferiOkuyucu.Seferler(satirlar).Single();
        SevkSeferi? sefer = request.SeferId is int id
            ? (await unitOfWork.GetRepository<SevkSeferi>().FindAsync(x => x.Id == id)).SingleOrDefault()
            : null;
        return Result<SevkSeferiDetayDto>.Success(new SevkSeferiDetayDto
        {
            Sefer = ozet,
            Aciklama = sefer?.Aciklama ?? sevkiyatlar.Select(s => s.Aciklama).FirstOrDefault(a => !string.IsNullOrWhiteSpace(a)),
            KullaniciAdSoyad = sevkiyatlar[0].KullaniciAdSoyad,
            Projeler = sevkiyatlar.Select(s => new SevkSeferiProjeDto
            {
                ProjeId = s.ProjeId,
                ProjeNo = s.ProjeNo,
                ProjeTipiId = s.ProjeTipiId,
                SevkiyatNo = s.SevkiyatNo,
                Sandiklar = s.Sandiklar
                    .OrderBy(x => x.SandikNo.Length).ThenBy(x => x.SandikNo, StringComparer.Ordinal)
                    .ToArray()
            }).ToArray()
        });
    }

    public async Task<Result<SevkiyatIstatistikDto>> Handle(SevkiyatIstatistikQuery request, CancellationToken cancellationToken)
    {
        var satirlar = await SevkSeferiOkuyucu.SatirlarAsync(unitOfWork, readQueries, request.Baslangic, request.Bitis,
            null, cancellationToken);
        var seferler = SevkSeferiOkuyucu.Seferler(satirlar);
        var bugun = TurkeyTime.Now;
        var plakalar = satirlar.Where(x => x.Plaka != null)
            .GroupBy(x => x.Plaka!)
            .Select(g => new PlakaIstatistikDto
            {
                Plaka = AracPlakaKurali.Bicimle(g.Key)!,
                SeferSayisi = g.Select(x => x.SeferAnahtari).Distinct().Count(),
                SandikSayisi = g.Sum(x => x.SandikSayisi),
                ProjeSayisi = g.Select(x => x.ProjeId).Distinct().Count(),
                SonSevk = g.Max(x => x.SevkTarihi)
            })
            .OrderByDescending(x => x.SeferSayisi).ThenByDescending(x => x.SandikSayisi).ThenBy(x => x.Plaka, StringComparer.Ordinal)
            .ToArray();
        var seferNolari = seferler.ToDictionary(x => x.Anahtar, x => x.EskiKayit ? $"{x.SevkTarihi:dd.MM.yyyy}" : x.SeferNo);
        return Result<SevkiyatIstatistikDto>.Success(new SevkiyatIstatistikDto
        {
            SeferSayisi = seferler.Count,
            SandikSayisi = seferler.Sum(x => x.SandikSayisi),
            ProjeSayisi = satirlar.Select(x => x.ProjeId).Distinct().Count(),
            BuAySeferSayisi = seferler.Count(x => x.SevkTarihi.Year == bugun.Year && x.SevkTarihi.Month == bugun.Month),
            EnCokKullanilanPlaka = plakalar.FirstOrDefault()?.Plaka,
            Plakalar = plakalar,
            Projeler = satirlar.GroupBy(x => new { x.ProjeId, x.ProjeNo, x.ProjeTipiId })
                .Select(g => new ProjeSevkIstatistikDto
                {
                    ProjeId = g.Key.ProjeId,
                    ProjeNo = g.Key.ProjeNo,
                    ProjeTipiId = g.Key.ProjeTipiId,
                    SeferSayisi = g.Select(x => x.SeferAnahtari).Distinct().Count(),
                    SandikSayisi = g.Sum(x => x.SandikSayisi),
                    SeferNolari = g.Select(x => seferNolari[x.SeferAnahtari]).Distinct().ToArray(),
                    Plakalar = g.Where(x => x.Plaka != null).Select(x => AracPlakaKurali.Bicimle(x.Plaka)!).Distinct().ToArray()
                })
                .OrderBy(x => x.ProjeNo, StringComparer.Ordinal)
                .ToArray(),
            Aylik = seferler.GroupBy(x => new { x.SevkTarihi.Year, x.SevkTarihi.Month })
                .Select(g => new AylikSevkIstatistikDto
                {
                    Yil = g.Key.Year,
                    Ay = g.Key.Month,
                    SeferSayisi = g.Count(),
                    SandikSayisi = g.Sum(x => x.SandikSayisi)
                })
                .OrderBy(x => x.Yil).ThenBy(x => x.Ay)
                .ToArray()
        });
    }
}
