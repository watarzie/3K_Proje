using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Exceptions;
using _3K.Core.Models;
using _3K.Infrastructure.Data;

namespace _3K.Infrastructure.Services.Finans;

/// <summary>
/// İş kaydının fiyat snapshot'ını tarifeden belirler: ürünün tarihte geçerli tarifesi ve üretim
/// kaydının en özgül ürün eşleşmesine göre otomatik fiyat. Yazılan snapshot, tarife sonradan
/// değişse de korunur.
/// </summary>
internal sealed class FinansFiyatBelirleyici(AppDbContext context)
{
    /// <summary>Ürünün tarihte geçerli aktif tarifesi; çakışan iki aktif tarife iş kuralı hatasıdır.</summary>
    public async Task<FinansFiyatTarifesi?> GecerliTarifeAsync(
        int finansUrunId,
        DateTime tarih,
        CancellationToken cancellationToken)
    {
        var tariffs = await context.Set<FinansFiyatTarifesi>()
            .AsNoTracking()
            .Where(x => x.FinansUrunId == finansUrunId &&
                        x.Aktif &&
                        x.GecerlilikBaslangici.Date <= tarih.Date &&
                        x.GecerlilikBitisi.Date >= tarih.Date)
            .OrderByDescending(x => x.GecerlilikBaslangici)
            .ThenByDescending(x => x.Id)
            .Take(2).ToListAsync(cancellationToken);
        if (tariffs.Count > 1) throw IsKuraliException.Cakisma("Çakışan aktif fiyat tarifeleri var; fiyatlandırmadan önce tarifeleri düzeltin.");
        return tariffs.SingleOrDefault();
    }

    /// <summary>
    /// İşi en özgül aktif ürün eşleşmesine ve finans tarihindeki tarifeye göre fiyatlar. Kontrplak sandıkta
    /// malzemeli eşleşme varsa ahşap m³ ahşap tarifesiyle, kontra m³ kontra tarifesiyle fiyatlanır
    /// (<see cref="FinansTutarKurallari.IsNet"/>); yoksa eski tek tarifeli hesap geçerlidir. Katlanır sandık tarifeyle
    /// fiyatlanmaz: adetle fiyatsız kalır ve elle fiyat bekler (<see cref="FinansKatlanirKurali"/>).
    /// </summary>
    public async Task OtomatikFiyatUygulaAsync(FinansIsKaydi entity, CancellationToken cancellationToken)
    {
        if (FinansKatlanirKurali.Katlanir(entity))
        {
            FiyatsizBirak(entity);
            entity.FiyatlandirmaBirimiSnapshot = FinansFiyatlandirmaBirimi.Adet;
            entity.TarifeIdSnapshot = null;
            return;
        }

        var matchQuery = context.Set<FinansUrunEslesmesi>()
            .AsNoTracking()
            .Include(x => x.FinansUrun)
            .Where(x => x.Aktif && x.FinansUrun.Aktif && x.IsTuru == entity.IsTuru);
        var matches = await matchQuery.OrderBy(x => x.FinansUrun.Sira).ThenBy(x => x.Id).ToListAsync(cancellationToken);
        var adaylar = matches
            .Where(x => string.IsNullOrWhiteSpace(x.SandikAdi) || string.Equals(x.SandikAdi, entity.SandikAdi, StringComparison.OrdinalIgnoreCase))
            .Where(x => string.IsNullOrWhiteSpace(x.SandikTipi) || string.Equals(x.SandikTipi, entity.SandikTipi, StringComparison.OrdinalIgnoreCase))
            .Where(x => !x.IcSandikSablonId.HasValue || x.IcSandikSablonId == entity.IcSandikSablonId)
            .ToList();
        var malzeme = FinansHacimKurallari.Malzeme(entity.SandikCinsi, entity.SandikTipi);
        if (malzeme == FinansSandikMalzemesi.Kontra && adaylar.Any(x => x.Malzeme.HasValue))
        {
            await MalzemeliFiyatUygulaAsync(entity, adaylar, cancellationToken);
            return;
        }

        // Tek tarifeli hesap: malzemesiz eşleşme ya da işin kendi malzemesine ait eşleşme.
        var match = EnOzgul(adaylar.Where(x => !x.Malzeme.HasValue || x.Malzeme == malzeme));
        MalzemeliFiyatiTemizle(entity);
        if (match is null)
        {
            FiyatsizBirak(entity);
            return;
        }

        entity.FinansUrunId = match.FinansUrunId;
        entity.FiyatlandirmaBirimiSnapshot = match.FinansUrun.FiyatlandirmaBirimi;
        var tariff = await GecerliTarifeAsync(match.FinansUrunId, entity.FinansTarihi, cancellationToken);
        if (tariff is null)
        {
            entity.BirimFiyatSnapshot = 0;
            entity.ParaBirimiSnapshot = "EUR";
            entity.KdvOraniSnapshot = 0;
            entity.TarifeYiliSnapshot = null;
            return;
        }

        entity.BirimFiyatSnapshot = tariff.BirimFiyat;
        entity.ParaBirimiSnapshot = tariff.ParaBirimi;
        entity.KdvOraniSnapshot = tariff.KdvOrani;
        entity.TarifeYiliSnapshot = tariff.Yil;
        entity.TarifeIdSnapshot = tariff.Id;
    }

    /// <summary>
    /// Kontrplak sandık: kontra tarafı kontra eşleşmesinin (yoksa malzemesiz genel m³ eşleşmesinin), ahşap tarafı
    /// ahşap eşleşmesinin finans tarihindeki tarifesiyle fiyatlanır (genel eşleşme yalnız m³ tarifesiyse kullanılır). Kontra
    /// tarafı için hiç eşleşme yoksa iş fiyatsız kalır; ahşap tarifesi yoksa (veya para birimi kontrayla
    /// aynı değilse) ahşap fiyatı 0'dır ve ahşap m³ girilirse tutar eksik kalır (ekranda "ahşap tarifesi yok" görünür).
    /// </summary>
    private async Task MalzemeliFiyatUygulaAsync(FinansIsKaydi entity, IReadOnlyList<FinansUrunEslesmesi> adaylar, CancellationToken cancellationToken)
    {
        var kontra = EnOzgul(adaylar.Where(x => x.Malzeme == FinansSandikMalzemesi.Kontra))
            ?? EnOzgul(adaylar.Where(x => !x.Malzeme.HasValue && x.FinansUrun.FiyatlandirmaBirimi == FinansFiyatlandirmaBirimi.Metrekup));
        var ahsap = EnOzgul(adaylar.Where(x => x.Malzeme == FinansSandikMalzemesi.Ahsap));
        if (kontra is null)
        {
            MalzemeliFiyatiTemizle(entity);
            FiyatsizBirak(entity);
            return;
        }

        entity.FinansUrunId = kontra.FinansUrunId;
        entity.FiyatlandirmaBirimiSnapshot = FinansFiyatlandirmaBirimi.Metrekup;
        var kontraTarife = await GecerliTarifeAsync(kontra.FinansUrunId, entity.FinansTarihi, cancellationToken);
        entity.BirimFiyatSnapshot = kontraTarife?.BirimFiyat ?? 0;
        entity.ParaBirimiSnapshot = kontraTarife?.ParaBirimi ?? "EUR";
        entity.KdvOraniSnapshot = kontraTarife?.KdvOrani ?? 0;
        entity.TarifeYiliSnapshot = kontraTarife?.Yil;
        entity.TarifeIdSnapshot = kontraTarife?.Id;

        var ahsapTarife = ahsap is null ? null : await GecerliTarifeAsync(ahsap.FinansUrunId, entity.FinansTarihi, cancellationToken);
        var ayniParaBirimi = ahsapTarife is not null &&
            string.Equals(ahsapTarife.ParaBirimi, entity.ParaBirimiSnapshot, StringComparison.OrdinalIgnoreCase);
        entity.AhsapFinansUrunId = ahsap?.FinansUrunId;
        entity.AhsapBirimFiyatSnapshot = ayniParaBirimi ? ahsapTarife!.BirimFiyat : 0;
        entity.AhsapTarifeIdSnapshot = ayniParaBirimi ? ahsapTarife!.Id : null;
    }

    /// <summary>Malzemeli fiyatın ahşap tarafını kaldırır; iş yeniden tek tarifeyle hesaplanır.</summary>
    public static void MalzemeliFiyatiTemizle(FinansIsKaydi entity)
    {
        entity.AhsapFinansUrunId = null;
        entity.AhsapBirimFiyatSnapshot = null;
        entity.AhsapTarifeIdSnapshot = null;
    }

    private static FinansUrunEslesmesi? EnOzgul(IEnumerable<FinansUrunEslesmesi> adaylar) => adaylar
        .OrderByDescending(MatchSpecificity)
        .ThenBy(x => x.FinansUrun.Sira)
        .FirstOrDefault();

    /// <summary>Otomatik fiyat snapshot'ını ürünsüz ve fiyatsız (0 EUR, KDV 0) duruma döndürür.</summary>
    public static void FiyatsizBirak(FinansIsKaydi entity)
    {
        MalzemeliFiyatiTemizle(entity);
        entity.FinansUrunId = null;
        entity.FiyatlandirmaBirimiSnapshot = entity.BirimM3 > 0
            ? FinansFiyatlandirmaBirimi.Metrekup
            : FinansFiyatlandirmaBirimi.Adet;
        entity.BirimFiyatSnapshot = 0;
        entity.ParaBirimiSnapshot = "EUR";
        entity.KdvOraniSnapshot = 0;
        entity.TarifeYiliSnapshot = null;
    }

    /// <summary>Eşleşmenin özgüllüğü: dolu sandık adı, sandık tipi ve iç sandık şablonu (boyut artık eşleşmede kullanılmaz).</summary>
    private static int MatchSpecificity(FinansUrunEslesmesi value)
        => (string.IsNullOrWhiteSpace(value.SandikAdi) ? 0 : 1) +
           (string.IsNullOrWhiteSpace(value.SandikTipi) ? 0 : 1) +
           (value.IcSandikSablonId.HasValue ? 1 : 0);
}
