using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.ProjeIslemleri.Services;
using _3K.Application.Features.SevkiyatIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Exceptions;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.SevkiyatIslemleri.Commands;

/// <summary>
/// Toplu sevk: önce bütün projeler doğrulanır (proje, mülkiyet, kapanmış sandık, saha aktarımı, zaten sevk); bir engel
/// varsa hiçbir proje sevk edilmez ve engeller proje/sandık bazında döner. Başarıda tek sefer, proje başına bir
/// sevkiyat, sandık ve proje durumları aynı serializable transaction'da yazılır (tekil sevkle aynı motor).
/// </summary>
public sealed class TopluSevkEtCommandHandler(
    IUnitOfWork unitOfWork,
    IHareketService hareketService,
    ICurrentUserService currentUserService,
    ISahaTamamlamaService sahaTamamlamaService,
    IReadQueryExecutor readQueries) : IRequestHandler<TopluSevkEtCommand, Result<TopluSevkSonucDto>>
{
    private const string IslemAnahtariIndex = "IX_SevkSeferleri_IslemAnahtari";

    public async Task<Result<TopluSevkSonucDto>> Handle(TopluSevkEtCommand request, CancellationToken cancellationToken)
    {
        if (currentUserService.UserId is not int kullaniciId || kullaniciId <= 0)
            return Result<TopluSevkSonucDto>.Failure("Sevk işlemi için geçerli bir kullanıcı oturumu bulunamadı.", 401);

        var hash = IstekHashOlustur(request);
        var seferRepo = unitOfWork.GetRepository<SevkSeferi>();
        var tekrar = await TekrariDogrulaAsync();
        if (tekrar != null)
            return tekrar;

        try
        {
            return await unitOfWork.ExecuteAtomicAsync(async ct =>
            {
                var transactionTekrari = await TekrariDogrulaAsync();
                if (transactionTekrari != null)
                    return transactionTekrari;

                var projeIdleri = request.Projeler.Select(p => p.ProjeId).ToArray();
                var projeNolari = (await unitOfWork.GetRepository<Proje>().FindAsync(p => projeIdleri.Contains(p.Id)))
                    .ToDictionary(p => p.Id, p => p.ProjeNo);
                var islem = new ProjeSevkIslemi(unitOfWork, hareketService, sahaTamamlamaService, readQueries);
                var hazirliklar = new List<ProjeSevkHazirligi>();
                var sorunlar = new List<TopluSevkSorunu>();
                foreach (var proje in request.Projeler.OrderBy(p => p.ProjeId))
                {
                    var kontrol = await islem.HazirlaAsync(proje.ProjeId, proje.SandikIds, ct);
                    if (kontrol.Hata != null)
                        sorunlar.Add(new TopluSevkSorunu(proje.ProjeId, projeNolari.GetValueOrDefault(proje.ProjeId),
                            kontrol.SorunluSandikIds, kontrol.Hata.Error!.Message));
                    else
                        hazirliklar.Add(kontrol.Hazirlik!);
                }
                if (sorunlar.Count > 0)
                    return Result<TopluSevkSonucDto>.Failure(
                        $"{sorunlar.Count} projede sevk engeli var; hiçbir proje sevk edilmedi.", 409, sorunlar);

                var sefer = ProjeSevkIslemi.YeniSefer(await islem.SeferNoUretAsync(ct), request.SevkTarihi, request.AracPlaka,
                    request.Sofor, request.DorsePlaka, request.Aciklama, kullaniciId, request.IslemAnahtari, hash);
                await seferRepo.AddAsync(sefer);
                // Aynı anahtarlı eşzamanlı istek, sandıklar değişmeden önce benzersiz kayıtta yarışır.
                await unitOfWork.SaveChangesAsync(ct);

                var hareketEki = $"Toplu sevk: sefer {sefer.SeferNo}, plaka {AracPlakaKurali.Bicimle(sefer.AracPlaka)}.";
                var sandikSayisi = 0;
                // Saha senkronu kaynak (normal) projenin sandıklarını da okuyabilir; normal projeler önce uygulanır.
                foreach (var hazirlik in hazirliklar.OrderBy(h => h.Proje.ProjeTipiId).ThenBy(h => h.Proje.Id))
                {
                    ct.ThrowIfCancellationRequested();
                    var sonuc = await islem.UygulaAsync(hazirlik, request.SevkTarihi, request.Aciklama, request.AracPlaka,
                        request.Sofor, request.DorsePlaka, sefer, kullaniciId, hareketEki, ct);
                    sandikSayisi += sonuc.SandikSayisi;
                }

                return Result<TopluSevkSonucDto>.Success(new TopluSevkSonucDto
                {
                    SeferId = sefer.Id,
                    SeferNo = sefer.SeferNo,
                    ProjeSayisi = hazirliklar.Count,
                    SandikSayisi = sandikSayisi
                });
            }, cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return await TekrariDogrulaAsync() ?? Result<TopluSevkSonucDto>.Failure(
                "Sevk sırasında sandık veya proje başka bir işlemle değişti. Hiçbir proje sevk edilmedi; listeyi yenileyip tekrar deneyin.", 409);
        }
        catch (UniqueConstraintViolationException ex) when (ex.ConstraintName == IslemAnahtariIndex)
        {
            return await TekrariDogrulaAsync() ?? Result<TopluSevkSonucDto>.Failure(
                "Toplu sevk eşzamanlı bir istekle çakıştı. Tekrar deneyin.", 409);
        }
        catch (UniqueConstraintViolationException)
        {
            return Result<TopluSevkSonucDto>.Failure(
                "Toplu sevk eşzamanlı başka bir sevkle çakıştı. Hiçbir proje sevk edilmedi; tekrar deneyin.", 409);
        }

        async Task<Result<TopluSevkSonucDto>?> TekrariDogrulaAsync()
        {
            var onceki = (await seferRepo.FindAsync(x => x.IslemAnahtari == request.IslemAnahtari)).SingleOrDefault();
            if (onceki == null)
                return null;
            if (onceki.KullaniciId != kullaniciId || !string.Equals(onceki.IstekHash, hash, StringComparison.Ordinal))
                return Result<TopluSevkSonucDto>.Failure(
                    "İşlem anahtarı başka bir kullanıcı veya farklı bir toplu sevk için kullanılmış.", 409);
            var sevkiyatlar = await readQueries.ToListAsync(unitOfWork.GetRepository<Sevkiyat>().Queryable()
                .Where(s => s.SevkSeferiId == onceki.Id)
                .Select(s => new { s.ProjeId, SandikSayisi = s.Sandiklar.Count }));
            return Result<TopluSevkSonucDto>.Success(new TopluSevkSonucDto
            {
                SeferId = onceki.Id,
                SeferNo = onceki.SeferNo,
                ProjeSayisi = sevkiyatlar.Select(s => s.ProjeId).Distinct().Count(),
                SandikSayisi = sevkiyatlar.Sum(s => s.SandikSayisi)
            });
        }
    }

    /// <summary>İstek özeti: sefer bilgileri ve proje → sandık seçimi (sıralı); aynı anahtarla farklı istek reddedilir.</summary>
    internal static string IstekHashOlustur(TopluSevkEtCommand request)
    {
        var metin = FormattableString.Invariant(
                $"v1|{request.SevkTarihi:O}|{AracPlakaKurali.Normalize(request.AracPlaka)}|{request.Sofor?.Trim()}|") +
            FormattableString.Invariant($"{AracPlakaKurali.Normalize(request.DorsePlaka)}|{request.Aciklama?.Trim()}|") +
            string.Join(";", request.Projeler.OrderBy(p => p.ProjeId).Select(p =>
                p.ProjeId.ToString(CultureInfo.InvariantCulture) + ":" +
                string.Join(",", p.SandikIds.Order().Select(id => id.ToString(CultureInfo.InvariantCulture)))));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(metin)));
    }

    /// <summary>Toplu sevk engeli: proje ve (varsa) engele neden olan sandıklar.</summary>
    public sealed record TopluSevkSorunu(int ProjeId, string? ProjeNo, IReadOnlyList<int> SandikIds, string Mesaj);
}
