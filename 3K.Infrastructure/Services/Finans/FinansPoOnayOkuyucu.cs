using Microsoft.EntityFrameworkCore;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Exceptions;
using _3K.Core.Models;
using _3K.Infrastructure.Data;

namespace _3K.Infrastructure.Services.Finans;

/// <summary>Kalemin PO öncesi onay durumu ve gösterilecek açıklaması (ret nedeni veya uygulanamama nedeni).</summary>
internal sealed record FinansPoOnayBilgisi(FinansPoOnayDurumu Durum, string? Aciklama, DateTime? OnayTarihi);

/// <summary>
/// PO öncesi iş onayının okuma tarafı: kuralın açık olup olmadığı, kalemlerin onay durumu (son talebin Onay Merkezi
/// kararıyla) ve PO'ya bağlama kontrolü. PO, liste ve onay servisleri aynı hesabı bu sınıftan alır.
/// </summary>
internal sealed class FinansPoOnayOkuyucu(AppDbContext context)
{
    /// <summary>Onay Merkezi'ndeki "Onay gerekli" anahtarı; kural kaydı yoksa açık sayılır.</summary>
    public async Task<bool> KuralAcikMiAsync(CancellationToken cancellationToken)
    {
        var deger = await context.Set<OnayOperasyonKurali>()
            .AsNoTracking()
            .Where(x => x.IslemKodu == OnayIslemKodlari.FinansPoIsOnayi)
            .Select(x => (bool?)x.OnayGerektirirMi)
            .FirstOrDefaultAsync(cancellationToken);
        return FinansPoOnayKurali.KuralAcik(deger);
    }

    /// <summary>Kalemlerin onay durumları; son talebin kararı Onay Merkezi kaydından okunur.</summary>
    public async Task<IReadOnlyDictionary<int, FinansPoOnayBilgisi>> BilgileriGetirAsync(
        IReadOnlyCollection<FinansIsKaydi> works,
        CancellationToken cancellationToken)
    {
        var talepIds = works
            .Where(x => x.PoOnayTalebiId.HasValue && !x.PoOnayNetTutar.HasValue)
            .Select(x => x.PoOnayTalebiId!.Value)
            .Distinct()
            .ToArray();
        var kararlar = talepIds.Length == 0
            ? new Dictionary<int, TalepKarari>()
            : (await context.Set<OnayBekleyenIslem>()
                    .AsNoTracking()
                    .Where(x => x.ReferansTipi == OnayReferansTipleri.FinansPoOnayTalebi &&
                                x.ReferansId.HasValue && talepIds.Contains(x.ReferansId.Value))
                    .Select(x => new { x.Id, TalepId = x.ReferansId!.Value, x.Durum, x.CalistirmaDurumu, x.KararAciklamasi, x.CalistirmaHatasi })
                    .ToListAsync(cancellationToken))
                .GroupBy(x => x.TalepId)
                .ToDictionary(
                    x => x.Key,
                    x => x.OrderByDescending(y => y.Id)
                        .Select(y => new TalepKarari(y.Durum, y.CalistirmaDurumu, y.KararAciklamasi, y.CalistirmaHatasi))
                        .First());

        return works.ToDictionary(x => x.Id, work =>
        {
            TalepKarari? karar = work.PoOnayTalebiId is { } talepId && kararlar.TryGetValue(talepId, out var bulunan)
                ? bulunan
                : null;
            var durum = FinansPoOnayKurali.Durum(work, karar?.Durum, karar?.Calistirma);
            var aciklama = durum switch
            {
                FinansPoOnayDurumu.Reddedildi => karar?.KararAciklamasi,
                FinansPoOnayDurumu.OnayDustu when karar is not null && !work.PoOnayNetTutar.HasValue => karar.CalistirmaHatasi,
                FinansPoOnayDurumu.OnayDustu => "Onaydan sonra net tutar veya para birimi değişti.",
                _ => null
            };
            return new FinansPoOnayBilgisi(durum, aciklama, durum == FinansPoOnayDurumu.Onaylandi ? work.PoOnayTarihi : null);
        });
    }

    /// <summary>Aylık iş satırlarına kalemlerinin birleşik onay durumunu ve onaylı/kapsamdaki kalem sayısını ekler.</summary>
    public async Task<IReadOnlyList<FinansAylikIsModel>> AylikSatirlaraEkleAsync(
        IReadOnlyList<FinansAylikIsModel> satirlar,
        IReadOnlyCollection<FinansIsKaydi> kayitlar,
        CancellationToken cancellationToken)
    {
        var bilgiler = await BilgileriGetirAsync(kayitlar, cancellationToken);
        return satirlar.Select(satir =>
        {
            var onaylar = satir.IsKaydiIds
                .Where(bilgiler.ContainsKey)
                .Select(id => bilgiler[id])
                .Where(x => x.Durum != FinansPoOnayDurumu.Gerekmiyor)
                .ToList();
            return satir with
            {
                PoOnayDurumu = FinansPoOnayKurali.Birlesik(onaylar.Select(x => x.Durum)),
                PoOnayGerekliKalemSayisi = onaylar.Count,
                PoOnayliKalemSayisi = onaylar.Count(x => x.Durum == FinansPoOnayDurumu.Onaylandi),
                PoOnayAciklamasi = onaylar.Select(x => x.Aciklama).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))
            };
        }).ToArray();
    }

    /// <summary>
    /// Kural açıkken kapsamdaki her kalemin geçerli onayı olmalıdır; yoksa hiçbir kalem PO'ya bağlanmaz (409).
    /// Yeni PO, mevcut PO'ya ekleme (kalem ve grup yolu) ve PO tutar artışı bu kontrolden geçer.
    /// </summary>
    public async Task PoyaBaglanabilirDogrulaAsync(
        IReadOnlyCollection<FinansIsKaydi> works,
        CancellationToken cancellationToken)
    {
        // İptal/pasif kalem onaydan bağımsız olarak PO'ya bağlanamaz; o ret kendi mesajıyla verilir.
        var onaysiz = works
            .Where(x => !x.IptalEdildi && x.KaynakAktif && !FinansPoOnayKurali.PoyaBaglanabilir(x, kuralAcik: true))
            .ToList();
        if (onaysiz.Count == 0 || !await KuralAcikMiAsync(cancellationToken))
            return;

        var bilgiler = await BilgileriGetirAsync(onaysiz, cancellationToken);
        throw IsKuraliException.Cakisma(FinansPoOnayKurali.PoEngelMesaji(
            onaysiz.Select(x => (x, bilgiler[x.Id].Durum)).ToArray()));
    }

    private sealed record TalepKarari(
        OnayDurumu Durum,
        OnayCalistirmaDurumu Calistirma,
        string? KararAciklamasi,
        string? CalistirmaHatasi);
}
