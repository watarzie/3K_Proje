using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _3K.Infrastructure.Data;

namespace _3K_API.Controllers;

[ApiController]
[Authorize]
[Route("api/yukleme-simulasyonu")]
public sealed class YuklemeSimulasyonuController : ControllerBase
{
    private readonly AppDbContext _context;

    public YuklemeSimulasyonuController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("projeler")]
    public async Task<ActionResult<IReadOnlyList<YuklemeProjeDto>>> Projeler(CancellationToken cancellationToken)
    {
        var projeler = await _context.Projeler.AsNoTracking()
            .OrderByDescending(proje => proje.Id)
            .Select(proje => new YuklemeProjeDto(proje.Id, proje.ProjeNo, proje.FBNo, proje.Musteri, proje.ProjeTipiId))
            .ToListAsync(cancellationToken);
        return Ok(projeler);
    }

    [HttpGet("projeler/{projeId:int}/sandiklar")]
    public async Task<ActionResult<IReadOnlyList<YuklemeSandikDto>>> Sandiklar(int projeId, CancellationToken cancellationToken)
    {
        if (!await _context.Projeler.AsNoTracking().AnyAsync(proje => proje.Id == projeId, cancellationToken))
            return NotFound(new { message = "Proje bulunamadı." });

        var bagimsizKaynakIdleri = _context.AmbalajBagimsizSandiklar.AsNoTracking()
            .Where(sandik => sandik.ProjeId == projeId && sandik.Tur != 3 && sandik.KaynakSandikId.HasValue)
            .Select(sandik => sandik.KaynakSandikId!.Value);

        var kaynakSandiklar = await _context.Sandiklar.AsNoTracking()
            .Where(sandik => sandik.ProjeId == projeId && !bagimsizKaynakIdleri.Contains(sandik.Id))
            .Select(sandik => new
            {
                sandik.Id,
                sandik.SandikNo,
                sandik.Ad,
                sandik.Boy,
                sandik.En,
                sandik.Yukseklik,
                sandik.GrossKg,
                SandikTipi = sandik.TipLookup != null ? sandik.TipLookup.Deger : "Proje Sandığı"
            })
            .ToListAsync(cancellationToken);

        var manuelKalemler = await _context.AmbalajUretimKalemleri.AsNoTracking()
            .Where(kalem => kalem.AmbalajUretimPlani.ProjeId == projeId
                && !kalem.KaynakSandikId.HasValue && kalem.Tur != 3)
            .Select(kalem => new YuklemeSandikDto($"kalem-{kalem.Id}", kalem.SandikNo, kalem.Ad, kalem.SandikTipi,
                kalem.Adet, kalem.Boy, kalem.En, kalem.Yukseklik, null, "Manuel Ambalaj"))
            .ToListAsync(cancellationToken);

        var bagimsizSandiklar = await _context.AmbalajBagimsizSandiklar.AsNoTracking()
            .Where(sandik => sandik.ProjeId == projeId && sandik.Tur != 3)
            .Select(sandik => new YuklemeSandikDto($"ozel-{sandik.Id}", sandik.SandikNo, sandik.Ad, sandik.SandikTipi,
                sandik.Adet, sandik.Boy, sandik.En, sandik.Yukseklik, null, "Özel Sandık"))
            .ToListAsync(cancellationToken);

        var sonuc = kaynakSandiklar.Select(sandik => new YuklemeSandikDto(
                $"sandik-{sandik.Id}", sandik.SandikNo, sandik.Ad, sandik.SandikTipi,
                SandikAdedi(sandik.SandikNo), sandik.Boy ?? 0, sandik.En ?? 0, sandik.Yukseklik ?? 0,
                sandik.GrossKg, "Proje Sandığı"))
            .Concat(manuelKalemler)
            .Concat(bagimsizSandiklar)
            .OrderBy(sandik => SandikSira(sandik.SandikNo))
            .ThenBy(sandik => sandik.SandikNo)
            .ToList();

        return Ok(sonuc);
    }

    private static int SandikAdedi(string sandikNo)
    {
        var match = Regex.Match(sandikNo ?? string.Empty, @"^(\d+)\s*-\s*(\d+)$");
        if (!match.Success) return 1;
        var baslangic = int.Parse(match.Groups[1].Value);
        var bitis = int.Parse(match.Groups[2].Value);
        return bitis >= baslangic ? bitis - baslangic + 1 : 1;
    }

    private static int SandikSira(string? sandikNo)
    {
        var match = Regex.Match(sandikNo ?? string.Empty, @"\d+");
        return match.Success && int.TryParse(match.Value, out var sira) ? sira : int.MaxValue;
    }
}

public sealed record YuklemeProjeDto(int Id, string ProjeNo, string? FbNo, string Musteri, int ProjeTipiId);

public sealed record YuklemeSandikDto(
    string Id,
    string SandikNo,
    string? Ad,
    string SandikTipi,
    int Adet,
    decimal Boy,
    decimal En,
    decimal Yukseklik,
    decimal? BrutKg,
    string Kaynak);