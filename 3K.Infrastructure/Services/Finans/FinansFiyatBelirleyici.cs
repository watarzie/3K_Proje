using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Exceptions;
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

    /// <summary>İşi en özgül aktif ürün eşleşmesine ve finans tarihindeki tarifeye göre fiyatlar.</summary>
    public async Task OtomatikFiyatUygulaAsync(FinansIsKaydi entity, CancellationToken cancellationToken)
    {
        var matchQuery = context.Set<FinansUrunEslesmesi>()
            .AsNoTracking()
            .Include(x => x.FinansUrun)
            .Where(x => x.Aktif && x.FinansUrun.Aktif && x.IsTuru == entity.IsTuru);
        var matches = await matchQuery.OrderBy(x => x.FinansUrun.Sira).ThenBy(x => x.Id).ToListAsync(cancellationToken);
        var match = matches
            .Where(x => string.IsNullOrWhiteSpace(x.SandikAdi) || string.Equals(x.SandikAdi, entity.SandikAdi, StringComparison.OrdinalIgnoreCase))
            .Where(x => string.IsNullOrWhiteSpace(x.SandikTipi) || string.Equals(x.SandikTipi, entity.SandikTipi, StringComparison.OrdinalIgnoreCase))
            .Where(x => !x.IcSandikSablonId.HasValue || x.IcSandikSablonId == entity.IcSandikSablonId)
            .Where(x => !x.Boy.HasValue || x.Boy == entity.Boy)
            .Where(x => !x.En.HasValue || x.En == entity.En)
            .Where(x => !x.Yukseklik.HasValue || x.Yukseklik == entity.Yukseklik)
            .OrderByDescending(x => MatchSpecificity(x))
            .ThenBy(x => x.FinansUrun.Sira)
            .FirstOrDefault();

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

    /// <summary>Otomatik fiyat snapshot'ını ürünsüz ve fiyatsız (0 EUR, KDV 0) duruma döndürür.</summary>
    public static void FiyatsizBirak(FinansIsKaydi entity)
    {
        entity.FinansUrunId = null;
        entity.FiyatlandirmaBirimiSnapshot = entity.BirimM3 > 0
            ? FinansFiyatlandirmaBirimi.Metrekup
            : FinansFiyatlandirmaBirimi.Adet;
        entity.BirimFiyatSnapshot = 0;
        entity.ParaBirimiSnapshot = "EUR";
        entity.KdvOraniSnapshot = 0;
        entity.TarifeYiliSnapshot = null;
    }

    private static int MatchSpecificity(FinansUrunEslesmesi value)
        => (string.IsNullOrWhiteSpace(value.SandikAdi) ? 0 : 1) +
           (string.IsNullOrWhiteSpace(value.SandikTipi) ? 0 : 1) +
           (value.IcSandikSablonId.HasValue ? 1 : 0) +
           (value.Boy.HasValue ? 1 : 0) +
           (value.En.HasValue ? 1 : 0) +
           (value.Yukseklik.HasValue ? 1 : 0);
}
