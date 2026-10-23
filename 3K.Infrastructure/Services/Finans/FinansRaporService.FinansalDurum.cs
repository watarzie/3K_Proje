using _3K.Core.Helpers;
using _3K.Core.Models;

namespace _3K.Infrastructure.Services.Finans;

/// <summary>
/// Finansal durum (gelir–gider) raporu. Tek doğru kaynak Kontrol Panelidir: özet, gelir kırılımı, aylık trend ve proje
/// tutarları Kontrol Paneli servisinin (<see cref="_3K.Core.Interfaces.IFinansPanelService"/>) aynı dönem için döndürdüğü değerlerdir; formül yeniden yazılmaz.
/// Gider kategorisi kırılımı rapor verisinin avans ve iptal hariç giderlerinden gelir (panelin gider toplamıyla aynı küme).
/// </summary>
internal sealed partial class FinansRaporService
{
    private const string PanelAdi = "Kontrol Paneli";
    private const int ProjeSiniri = 15;

    public async Task<byte[]> FinansalDurumExcelAsync(FinansListeFiltre filtre, CancellationToken cancellationToken)
        => BolumluRaporExcel(await FinansalDurumRaporuAsync(filtre, cancellationToken));

    public async Task<byte[]> FinansalDurumPdfAsync(FinansListeFiltre filtre, CancellationToken cancellationToken)
        => BolumluRaporPdf(await FinansalDurumRaporuAsync(filtre, cancellationToken));

    private async Task<BolumluRapor> FinansalDurumRaporuAsync(FinansListeFiltre filtre, CancellationToken cancellationToken)
    {
        var (baslangic, bitis) = FinansTarihKurallari.FinansalDurumDonemi(filtre.Baslangic, filtre.Bitis);
        var panel = await _panel.PanelAsync(baslangic, bitis, cancellationToken);
        var paraBirimi = string.IsNullOrWhiteSpace(filtre.ParaBirimi) ? null : filtre.ParaBirimi.Trim().ToUpperInvariant();
        var veri = await _raporVerisi.RaporVerisiAsync(
            new FinansListeFiltre { Baslangic = baslangic, Bitis = bitis, ParaBirimi = paraBirimi, IptalEdilenleriDahilEt = false },
            cancellationToken);
        return FinansalDurumRaporu(panel, veri.Isler, veri.Giderler, paraBirimi);
    }

    /// <summary>
    /// <paramref name="isler"/> ve <paramref name="giderler"/> dönemin rapor verisidir (iptaller ve kaynağı pasif işler
    /// hariç); gider kırılımı ve aylık trend buradan hesaplanır. Panelin aylık serisi yılbaşından yüklenir ve ay
    /// ortasında başlayan dönemin ilk ayına dönem öncesi günleri katar; trendin toplamı özetle aynı kalsın diye kullanılmaz.
    /// </summary>
    internal static BolumluRapor FinansalDurumRaporu(
        FinansPanelModel panel,
        IReadOnlyList<FinansIsKaydiModel> isler,
        IReadOnlyList<FinansGiderModel> giderler,
        string? paraBirimi)
    {
        bool Dahil(string pb) => paraBirimi == null || string.Equals(pb, paraBirimi, StringComparison.OrdinalIgnoreCase);
        var alacaklar = (panel.Alacaklar ?? []).Where(x => Dahil(x.ParaBirimi)).ToDictionary(x => x.ParaBirimi);
        // Sütunlar panelin tutar ve alacak para birimlerinin birleşimidir: yalnız alacağı olan para birimi kaybolmaz.
        var tutarMap = panel.Tutarlar.Where(x => Dahil(x.ParaBirimi)).ToDictionary(x => x.ParaBirimi);
        var paraBirimleri = tutarMap.Keys.Union(alacaklar.Keys).Order(StringComparer.Ordinal).ToList();
        var tutarlar = paraBirimleri
            .Select(pb => tutarMap.GetValueOrDefault(pb) ?? new FinansPanelParaModel(pb, null, null, null, null, null, null, null, null, null, null, null))
            .ToList();
        var avanslar = giderler.Where(x => !x.IptalEdildi && x.AvansMi).GroupBy(x => x.ParaBirimi).ToDictionary(x => x.Key, x => x.Sum(y => y.Matrah));

        // 1. Özet: kalem × para birimi.
        (string Ad, Func<FinansPanelParaModel, decimal?> Deger)[] kalemler =
        [
            ("İş net bedeli", t => t.IsBedeli),
            ("Sipariş açılan net", t => t.SiparisNetTutar),
            ("Sipariş bekleyen net", t => t.SiparisBekleyen),
            ("Faturalanan net gelir", t => t.Gelir),
            ("Fatura bekleyen net", t => t.FaturaBekleyen),
            ("Tahsil edilen (KDV dahil)", t => alacaklar.GetValueOrDefault(t.ParaBirimi)?.OdenenTutar ?? 0),
            ("Açık alacak (KDV dahil)", t => alacaklar.GetValueOrDefault(t.ParaBirimi)?.BekleyenTutar ?? 0),
            ("Vadesi geçen alacak (KDV dahil)", t => alacaklar.GetValueOrDefault(t.ParaBirimi)?.VadesiGecenTutar ?? 0),
            ("Net gider (avans hariç)", t => t.Gider),
            ("Avans (bilgi; maliyete dahil değil)", t => avanslar.GetValueOrDefault(t.ParaBirimi)),
            ("Fark (gelir − gider)", t => t.Fark),
            ("Tahmini kâr (iş net − gider)", t => t.TahminiKar),
            ("Kâr oranı %", t => t.KarOrani)
        ];
        // Değeri olmayan hücre boş yazılır (ör. iş bedeli sıfırken kâr oranı hesaplanamaz; "0,00" değildir).
        var ozet = kalemler
            .Select(k => new RaporSatiri(
                RaporSatirTuru.Veri,
                new object?[] { k.Ad }.Concat(tutarlar.Select(t => (object?)k.Deger(t))).ToArray()))
            .ToList();
        var ozetTablosu = new RaporTablosu(
            "Özet",
            new[] { "Kalem" }.Concat(paraBirimleri).ToArray(),
            ozet,
            new[] { 2.4f }.Concat(paraBirimleri.Select(_ => 1f)).ToArray());

        // 2. Gelir kırılımı: iş kategorisi.
        var gelir = (panel.KategoriOzetleri ?? [])
            .Where(x => Dahil(x.ParaBirimi))
            .OrderBy(x => x.ParaBirimi, StringComparer.Ordinal).ThenByDescending(x => x.IsBedeli)
            .Select(x => new RaporSatiri(RaporSatirTuru.Veri,
                [FinansKategoriKurali.Ad(x.Kategori), x.ParaBirimi, x.IsKaydi, x.Proje, x.IsBedeli ?? 0m, x.SiparisNetTutar ?? 0m, x.FaturalananNetTutar ?? 0m]))
            .ToList();
        var gelirTablosu = new RaporTablosu(
            "Gelir kırılımı",
            ["İş Kategorisi", "Para Birimi", "İş Kaydı", "Proje", "İş Net", "Sipariş Net", "Faturalanan Net"],
            gelir,
            [2f, 0.8f, 0.8f, 0.8f, 1.2f, 1.2f, 1.2f]);

        // 3. Gider kırılımı: gider kategorisi (avans ve iptal hariç), avans ayrı satırda.
        var giderSatirlari = giderler.Where(MaliyetMi)
            .GroupBy(x => (Kategori: string.IsNullOrWhiteSpace(x.Kategori) ? "Kategorisiz" : x.Kategori, x.ParaBirimi))
            .OrderBy(x => x.Key.ParaBirimi, StringComparer.Ordinal).ThenByDescending(x => x.Sum(y => y.Matrah))
            .Select(x => new RaporSatiri(RaporSatirTuru.Veri,
                [x.Key.Kategori, x.Key.ParaBirimi, x.Count(), x.Sum(y => y.Matrah), x.Sum(y => y.KdvTutari), x.Sum(y => y.ToplamTutar)]))
            .ToList();
        giderSatirlari.AddRange(giderler.Where(x => !x.IptalEdildi && x.AvansMi)
            .GroupBy(x => x.ParaBirimi).OrderBy(x => x.Key, StringComparer.Ordinal)
            .Select(x => new RaporSatiri(RaporSatirTuru.AraToplam,
                ["Avans (maliyete dahil değil)", x.Key, x.Count(), x.Sum(y => y.Matrah), x.Sum(y => y.KdvTutari), x.Sum(y => y.ToplamTutar)])));
        var giderTablosu = new RaporTablosu(
            "Gider kırılımı",
            ["Gider Kategorisi", "Para Birimi", "Kayıt", "Matrah", "KDV", "Toplam"],
            giderSatirlari,
            [2f, 0.8f, 0.8f, 1.2f, 1.2f, 1.2f]);

        // 4. Aylık trend: yalnız dönem içindeki kayıtlar (finans tarihinin ayı); gelir faturalanan net, gider maliyet.
        var aylikGelir = isler.Where(x => Dahil(x.ParaBirimi))
            .Select(x => (Ay: FinansTarihKurallari.AyBasi(x.FinansTarihi), x.ParaBirimi, Gelir: x.FaturalananNetTutar, Gider: 0m));
        var aylikGider = giderler.Where(x => MaliyetMi(x) && Dahil(x.ParaBirimi))
            .Select(x => (Ay: FinansTarihKurallari.AyBasi(x.FinansTarihi), x.ParaBirimi, Gelir: 0m, Gider: x.Matrah));
        var aylik = aylikGelir.Concat(aylikGider)
            .GroupBy(x => (x.Ay, x.ParaBirimi))
            .OrderBy(x => x.Key.Ay).ThenBy(x => x.Key.ParaBirimi, StringComparer.Ordinal)
            .Select(x => new RaporSatiri(RaporSatirTuru.Veri,
                [x.Key.Ay.ToString("yyyy-MM"), x.Key.ParaBirimi, x.Sum(y => y.Gelir), x.Sum(y => y.Gider), x.Sum(y => y.Gelir - y.Gider)]))
            .ToList();
        var aylikTablosu = new RaporTablosu(
            "Aylık trend",
            ["Ay", "Para Birimi", "Faturalanan Gelir", "Gider", "Fark"],
            aylik,
            [1f, 0.8f, 1.2f, 1.2f, 1.2f]);

        // 5. Proje bazlı: para birimi başına iş net bedeline göre ilk 15 proje + "Diğer".
        var projeSatirlari = new List<RaporSatiri>();
        foreach (var grup in (panel.ProjeOzetleri ?? []).Where(x => Dahil(x.ParaBirimi)).GroupBy(x => x.ParaBirimi).OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            var sirali = grup.OrderByDescending(x => x.IsBedeli ?? 0m).ToList();
            projeSatirlari.AddRange(sirali.Take(ProjeSiniri).Select(x => new RaporSatiri(RaporSatirTuru.Veri,
                [x.ProjeNo, x.Musteri, x.ParaBirimi, x.IsBedeli ?? 0m, x.FaturalananNetTutar ?? 0m, x.Gider ?? 0m, (x.IsBedeli ?? 0m) - (x.Gider ?? 0m)])));
            var diger = sirali.Skip(ProjeSiniri).ToList();
            if (diger.Count > 0)
            {
                projeSatirlari.Add(new RaporSatiri(RaporSatirTuru.AraToplam,
                [
                    $"Diğer ({diger.Count} proje)", null, grup.Key, diger.Sum(x => x.IsBedeli ?? 0m), diger.Sum(x => x.FaturalananNetTutar ?? 0m),
                    diger.Sum(x => x.Gider ?? 0m), diger.Sum(x => (x.IsBedeli ?? 0m) - (x.Gider ?? 0m))
                ]));
            }
        }
        var projeTablosu = new RaporTablosu(
            "Proje bazlı",
            ["Proje", "Müşteri", "Para Birimi", "İş Net", "Faturalanan Net", "Gider", "Tahmini Kâr"],
            projeSatirlari,
            [1.2f, 1.8f, 0.8f, 1.2f, 1.2f, 1.2f, 1.2f]);

        return new BolumluRapor(
            "Finansal Durum Raporu",
            $"Finans tarihi: {panel.Baslangic:dd.MM.yyyy}–{panel.Bitis:dd.MM.yyyy}" +
            (paraBirimi == null ? string.Empty : $" · Para birimi: {paraBirimi}") +
            $" · Oluşturma: {TurkeyTime.Now:dd.MM.yyyy HH:mm}",
            [ozetTablosu, gelirTablosu, giderTablosu, aylikTablosu, projeTablosu],
            $"Tutarlar {PanelAdi}yle aynıdır: {panel.TarihEkseni} Para birimleri toplanmaz; alacaklar KDV dahildir, iptaller hariçtir.");
    }
}
