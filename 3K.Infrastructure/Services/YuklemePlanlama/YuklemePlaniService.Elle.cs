using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Exceptions;
using _3K.Core.Helpers;
using _3K.Core.Models;
using _3K.YuklemeMotoru.Yerlesim;

namespace _3K.Infrastructure.Services.YuklemePlanlama;

/// <summary>
/// Elle yerleşim düzenleme (Simülasyon isterleri 20–22): kullanıcının konumları son revizyonun yükleriyle denetlenir
/// (otomatik yerleşimin kontrolleri ve yük düzeyindeki elle kontroller). Denetleme kaydetmez; kaydetme sonucu yeni
/// revizyonda saklar. Kontroller hiçbir zaman atlanmaz; sorunlu yerleşim de kaydedilir ve raporlar taslak basılır.
/// </summary>
internal sealed partial class YuklemePlaniService
{
    public async Task<PlanYerlesimi> ElleDenetleAsync(int planId, IReadOnlyList<ElleAracModeli> araclar, CancellationToken cancellationToken)
    {
        var plan = await context.YuklemePlanlari.AsNoTracking().Include(p => p.TasimaAilesi).SingleOrDefaultAsync(p => p.Id == planId, cancellationToken)
                   ?? throw new KayitBulunamadiException(PlanBulunamadi);
        var revizyon = await RevizyonAsync(plan.Id, plan.SonRevizyonNo, cancellationToken);
        var sonuc = await ElleSonucAsync(plan, revizyon, araclar, cancellationToken);
        // Denetim kaydedilmez; tarih önceki revizyonun değil, bu denetimin zamanıdır.
        return (await YerlesimAsync(plan, revizyon, sonuc, cancellationToken, TurkeyTime.Now))!;
    }

    public async Task<YuklemePlaniDetayi> ElleKaydetAsync(ElleYerlesimModeli model, int? kullaniciId, CancellationToken cancellationToken)
    {
        var plan = await context.YuklemePlanlari.Include(p => p.TasimaAilesi).SingleOrDefaultAsync(p => p.Id == model.PlanId, cancellationToken)
                   ?? throw new KayitBulunamadiException(PlanBulunamadi);
        if (plan.SonRevizyonNo != model.BeklenenRevizyonNo)
            throw IsKuraliException.Cakisma(EszamanliKayit);

        var onceki = await RevizyonAsync(plan.Id, plan.SonRevizyonNo, cancellationToken);
        var sonuc = await ElleSonucAsync(plan, onceki, model.Araclar, cancellationToken);
        var satirlar = onceki.Yukler.OrderBy(y => y.SiraNo).Select((y, i) => Kopyala(y, i + 1)).ToList();
        var projeler = onceki.Projeler.Select(p => new YuklemePlanProjesi { ProjeId = p.ProjeId, ProjeNo = p.ProjeNo }).ToList();
        YeniRevizyonEkle(plan, kullaniciId, "Yerleşim elle düzenlendi.", Ayarlar(onceki), projeler, satirlar, YerlesimSerilestirici.Yaz(sonuc));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return (await PlanGetirAsync(plan.Id, cancellationToken))!;
    }

    /// <summary>
    /// Elle yerleşimin motor girdisi ve denetimi: yükler son revizyonun planlamaya dahil ve verisi tam satırlarıdır;
    /// planda olmayan ya da tekrarlanan yük ve kullanılamayan ekipman reddedilir (400). Kullanılabilir ekipman: plan
    /// ailesinin ve özel yük ekipmanının aktif konfigürasyonları, ayrıca mevcut araç listesindeki ya da son yerleşimde
    /// kullanılmış (otomatik seçilmiş) konfigürasyonlar; yükleme yüzeyi olmayan ekipman kullanılamaz.
    /// </summary>
    private async Task<YerlesimSonucu> ElleSonucAsync(
        YuklemePlani plan, YuklemePlanRevizyonu revizyon, IReadOnlyList<ElleAracModeli> araclar, CancellationToken cancellationToken)
    {
        var ayarlar = Ayarlar(revizyon);
        var yukTurleri = await context.LookupYukTurleri.AsNoTracking().ToDictionaryAsync(t => t.Id, t => t.Deger, cancellationToken);
        var yukler = revizyon.Yukler
            .Where(y => y.Dahil && YukVeriKurali.Sorunlar(y.BoyMm, y.EnMm, y.YukseklikMm, y.BrutKg).Count == 0)
            .OrderBy(y => y.SiraNo)
            .Select(y => MotorYuku(y, yukTurleri))
            .ToList();
        if (yukler.Count == 0)
            throw new IsKuraliException(HesaplanacakYukYok);

        var anahtarlar = yukler.Select(y => y.Anahtar).ToHashSet(StringComparer.Ordinal);
        var verilen = araclar.SelectMany(a => a.Konumlar).Select(k => k.YukAnahtari).ToList();
        if (verilen.Any(a => !anahtarlar.Contains(a)) || verilen.Distinct(StringComparer.Ordinal).Count() != verilen.Count)
            throw new IsKuraliException(YuklemePlaniKurali.ElleYukGecersizMesaji);

        var konfIdleri = araclar.Select(a => a.KonfigurasyonId).Distinct().ToList();
        var izinliDisarida = ayarlar.MevcutAraclar.Select(m => m.KonfigurasyonId)
            .Concat(YerlesimSerilestirici.Oku(revizyon.YerlesimJson)?.Araclar.Select(a => a.KonfigurasyonId) ?? [])
            .Distinct().ToList();
        var adaylar = await AdaylarAsync(plan.TasimaAilesiId, izinliDisarida, cancellationToken);
        if (konfIdleri.Any(id => adaylar.All(a => a.KonfigurasyonId != id || a.TeknikVeri.Yuzeyler.Count == 0)))
            throw new IsKuraliException(YuklemePlaniKurali.ElleEkipmanGecersizMesaji);

        var girdi = new YerlesimGirdisi(
            yukler,
            adaylar,
            new YerlesimAyarlari(ayarlar.YerlesimBosluguMm, ayarlar.YukseklikPayiMm, ayarlar.GenislikPayiMm, ayarlar.UzunlukPayiMm,
                ayarlar.AgirlikPayiKg, ayarlar.YuklemeHareketiPayiMm),
            plan.ProjeKarisimi == YuklemeProjeKarisimi.ProjeBazindaAyri,
            plan.LokasyonTercihi == YuklemeLokasyonTercihi.LokasyonaGoreAyri,
            null,
            await YasalSinirlarAsync(cancellationToken),
            plan.TasimaAilesi.Ad);
        var sonuc = ElleYerlesimDenetimi.Denetle(girdi, araclar
            .Select(a => new ElleArac(a.KonfigurasyonId, a.Konumlar.Select(k => new ElleKonum(k.YukAnahtari, k.XMm, k.YMm, k.ZMm, k.Donduruldu)).ToList()))
            .ToList());
        return plan.CalismaBicimi == YuklemeCalismaBicimi.MevcutAraclaraYerlestirme ? FiloAsimi(sonuc, ayarlar.MevcutAraclar) : sonuc;
    }

    /// <summary>
    /// Mevcut araçlara yerleştirmede elle eklenen araçlar bildirilen filoyu (tip ve adet) aşabilir; aşım sessiz kalmaz:
    /// uyarı yazılır ve sonuç en iyi ihtimalle "Teyit edilmedi" olur (ek araç temin edilmeli).
    /// </summary>
    private static YerlesimSonucu FiloAsimi(YerlesimSonucu sonuc, IReadOnlyList<PlanMevcutArac> mevcut)
    {
        var adetler = mevcut.ToDictionary(m => m.KonfigurasyonId, m => m.Adet);
        var asimlar = sonuc.Araclar.GroupBy(a => (a.KonfigurasyonId, a.EkipmanKodu))
            .Where(g => g.Count() > adetler.GetValueOrDefault(g.Key.KonfigurasyonId))
            .Select(g => $"{g.Key.EkipmanKodu}: planda {g.Count()}, mevcut listede {adetler.GetValueOrDefault(g.Key.KonfigurasyonId)}")
            .ToList();
        if (asimlar.Count == 0)
            return sonuc;
        return sonuc with
        {
            Uyarilar = [$"Mevcut araç listesi aşıldı ({string.Join("; ", asimlar)}); ek araç temin edilmeli.", .. sonuc.Uyarilar],
            Sonuc = sonuc.Sonuc == KontrolSonucu.Uygunsuz ? KontrolSonucu.Uygunsuz : KontrolSonucu.TeyitEdilmedi
        };
    }
}
