using _3K.Core.Enums;
using _3K.Core.Models;

namespace _3K.Infrastructure.Services.Finans;

/// <summary>
/// İşler ve gider raporları. İşler raporu siparişi açılan (tam ya da kısmi) ve açılmayan işleri ayrı bölümlerde ya da
/// birlikte verir; her bölümün altında ve en altta para birimi bazında toplamlar vardır. İlave, Saha, Yedek ve Özel
/// işlerde ölçü; bütün işlerde açıklama, sandık cinsi ve talep eden yazılır. İptal edilmiş ya da kaynağı pasif iş
/// ("iptalleri dahil et" seçilince listelenir) panel kuralıyla aynı biçimde toplamlara girmez, ayrı toplanır. Gider
/// raporu kategoriye ya da projeye göre ara toplamlıdır; avans ve iptal maliyet değildir, toplamlara girmez, avans ayrı
/// toplanır. PDF ve Excel aynı içeriği taşır.
/// </summary>
internal sealed partial class FinansRaporService
{
    internal static readonly string[] IsRaporuBasliklari =
    [
        "Kayıt No", "Proje", "Müşteri", "Kategori", "İş Türü", "İş Adı", "Sandık No", "Sandık Cinsi", "Ölçü (mm)",
        "Adet", "Toplam m³", "Ahşap m³", "Kontra m³", "Talep Eden", "Açıklama", "Finans Tarihi", "Durum", "Para Birimi",
        "Fiyatlandırma", "Birim Fiyat", "KDV %", "Net", "KDV", "Toplam", "Sipariş Net", "Kalan Sipariş", "Sipariş Durumu",
        "PO", "Fatura"
    ];

    private static readonly float[] IsRaporuPdfGenislikleri =
    [
        0.55f, 1f, 1.1f, 0.85f, 0.85f, 1.35f, 0.55f, 0.8f, 1f, 0.45f, 0.6f, 0.6f, 0.6f, 1f, 1.7f, 0.75f, 0.8f, 0.5f,
        0.75f, 0.85f, 0.45f, 0.95f, 0.85f, 0.95f, 0.95f, 0.95f, 0.85f, 0.9f, 0.9f
    ];

    internal static readonly string[] GiderRaporuBasliklari =
    [
        "Kayıt No", "Tarih", "Finans Tarihi", "Kategori", "Gider Kalemi", "Firma/Kişi", "Açıklama", "Proje", "Belge No",
        "PO No", "Miktar", "Birim", "Birim Fiyat", "KDV Dahil", "KDV %", "Para Birimi", "Matrah", "KDV", "Toplam", "Durum"
    ];

    private static readonly float[] GiderRaporuPdfGenislikleri =
        [0.55f, 0.75f, 0.75f, 1.15f, 1.15f, 1.15f, 2.1f, 0.95f, 0.85f, 0.85f, 0.65f, 0.55f, 0.85f, 0.55f, 0.45f, 0.55f, 0.95f, 0.85f, 0.95f, 0.65f];

    // Toplam satırlarında değerlerin yazıldığı sütunlar; yerleri başlıktan bulunur (sütun eklenince kaymaz).
    private static readonly int IsEtiketSutunu = Array.IndexOf(IsRaporuBasliklari, "Proje");
    private static readonly int IsParaBirimiSutunu = Array.IndexOf(IsRaporuBasliklari, "Para Birimi");
    private static readonly int[] IsM3Sutunlari =
        new[] { "Toplam m³", "Ahşap m³", "Kontra m³" }.Select(x => Array.IndexOf(IsRaporuBasliklari, x)).ToArray();
    private static readonly int GiderEtiketSutunu = Array.IndexOf(GiderRaporuBasliklari, "Kategori");
    private static readonly int GiderParaBirimiSutunu = Array.IndexOf(GiderRaporuBasliklari, "Para Birimi");

    public async Task<byte[]> IslerExcelAsync(FinansListeFiltre filtre, FinansIsRaporuKapsami kapsam, CancellationToken cancellationToken)
        => BolumluRaporExcel(await IsRaporuAsync(filtre, kapsam, cancellationToken));

    public async Task<byte[]> IslerPdfAsync(FinansListeFiltre filtre, FinansIsRaporuKapsami kapsam, CancellationToken cancellationToken)
        => BolumluRaporPdf(await IsRaporuAsync(filtre, kapsam, cancellationToken));

    public async Task<byte[]> GiderlerExcelAsync(FinansListeFiltre filtre, FinansGiderRaporuGruplamasi gruplama, CancellationToken cancellationToken)
        => BolumluRaporExcel(await GiderRaporuAsync(filtre, gruplama, cancellationToken));

    public async Task<byte[]> GiderlerPdfAsync(FinansListeFiltre filtre, FinansGiderRaporuGruplamasi gruplama, CancellationToken cancellationToken)
        => BolumluRaporPdf(await GiderRaporuAsync(filtre, gruplama, cancellationToken));

    private async Task<BolumluRapor> IsRaporuAsync(FinansListeFiltre filtre, FinansIsRaporuKapsami kapsam, CancellationToken cancellationToken)
    {
        var report = await _raporVerisi.RaporVerisiAsync(filtre, cancellationToken);
        return IsRaporu(report.Isler, kapsam, FiltreMetni(filtre));
    }

    /// <summary>Siparişi açılan iş: PO'ya bağlanan net tutar sıfırdan büyük (tam ya da kısmi; kısmide kalan sipariş görünür).</summary>
    internal static bool SiparisiAcildiMi(FinansIsKaydiModel item) =>
        FinansTutarKurallari.KalemKarsilanma(item.NetTutar, item.SiparisNetTutar) != FinansKarsilanma.Yok;

    /// <summary>Toplamlara giren iş: iptal edilmemiş ve kaynağı aktif (Kontrol Paneli ve filtre kuralıyla aynı).</summary>
    internal static bool IsToplamaDahil(FinansIsKaydiModel item) => !item.IptalEdildi && item.KaynakAktif;

    internal static BolumluRapor IsRaporu(IReadOnlyList<FinansIsKaydiModel> isler, FinansIsRaporuKapsami kapsam, string altBaslik)
    {
        var bolumler = new List<(FinansIsRaporuKapsami Kapsam, List<FinansIsKaydiModel> Isler)>();
        if (kapsam != FinansIsRaporuKapsami.SiparisiAcilmayan)
            bolumler.Add((FinansIsRaporuKapsami.SiparisiAcilan, isler.Where(SiparisiAcildiMi).ToList()));
        if (kapsam != FinansIsRaporuKapsami.SiparisiAcilan)
            bolumler.Add((FinansIsRaporuKapsami.SiparisiAcilmayan, isler.Where(x => !SiparisiAcildiMi(x)).ToList()));

        var satirlar = new List<RaporSatiri>();
        foreach (var (bolumKapsami, bolumIsleri) in bolumler)
        {
            satirlar.Add(RaporSatiri.Bolum($"{FinansEtiketleri.IsRaporuKapsami(bolumKapsami)} ({bolumIsleri.Count} kayıt)"));
            satirlar.AddRange(bolumIsleri.Select(x => new RaporSatiri(RaporSatirTuru.Veri, IsRaporuSatiri(x))));
            satirlar.AddRange(IsToplamSatirlari(bolumIsleri.Where(IsToplamaDahil).ToList(), "Ara toplam", RaporSatirTuru.AraToplam));
        }

        var dahilIsler = bolumler.SelectMany(x => x.Isler).ToList();
        satirlar.Add(RaporSatiri.Bolum("Genel toplam"));
        satirlar.AddRange(IsToplamSatirlari(dahilIsler.Where(IsToplamaDahil).ToList(), "GENEL TOPLAM", RaporSatirTuru.GenelToplam));
        satirlar.AddRange(IsToplamSatirlari(dahilIsler.Where(x => !IsToplamaDahil(x)).ToList(),
            "İptal / kaynağı pasif (toplama dahil değil)", RaporSatirTuru.AraToplam));

        return new BolumluRapor(
            $"Finans İşleri Raporu — {FinansEtiketleri.IsRaporuKapsami(kapsam)}",
            altBaslik,
            [new RaporTablosu("İşler", IsRaporuBasliklari, satirlar, IsRaporuPdfGenislikleri, IsM3Sutunlari)],
            "Toplamlar para birimi bazındadır, para birimleri toplanmaz. Sipariş durumu PO'ya bağlanan net tutara göredir; kısmi açılan işte kalan sipariş görünür. İptal edilmiş ve kaynağı pasif işler toplamlara girmez.");
    }

    internal static object?[] IsRaporuSatiri(FinansIsKaydiModel item) =>
    [
        item.Id, item.ProjeNo, item.Musteri, FinansKategoriKurali.Ad(item.Kategori), FinansEtiketleri.IsTuru(item.IsTuru),
        item.IsAdi, item.SandikNo, BosIseNull(FinansEtiketleri.SandikCinsi(item.SandikCinsi, item.SandikTipi)), IsOlcusu(item),
        item.Adet, item.ToplamM3, item.AhsapM3, item.KontraM3, TalepEden(item), item.Aciklama, item.FinansTarihi,
        item.IptalEdildi ? "İptal" : !item.KaynakAktif ? "Kaynağı pasif" : FinansEtiketleri.IsDurumu(item.Durum),
        item.ParaBirimi, FinansEtiketleri.FiyatlandirmaBirimi(item.FiyatlandirmaBirimi), item.BirimFiyat, item.KdvOrani,
        item.NetTutar, item.KdvTutari, item.ToplamTutar, item.SiparisNetTutar, item.KalanSiparisNetTutar,
        SiparisKarsilanmaMetni(FinansTutarKurallari.KalemKarsilanma(item.NetTutar, item.SiparisNetTutar)),
        string.Join(", ", item.PoNumaralari), string.Join(", ", item.FaturaNumaralari)
    ];

    /// <summary>
    /// Para birimi başına toplam satırı: m³ ve tutarlar. Adet toplanmaz; kalemlerin birimi farklıdır (adet, ay, sefer…).
    /// </summary>
    private static IEnumerable<RaporSatiri> IsToplamSatirlari(IReadOnlyCollection<FinansIsKaydiModel> isler, string etiket, RaporSatirTuru tur)
    {
        foreach (var grup in isler.GroupBy(x => x.ParaBirimi).OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            var degerler = new object?[IsRaporuBasliklari.Length];
            void Yaz(string sutun, object deger) => degerler[Array.IndexOf(IsRaporuBasliklari, sutun)] = deger;
            degerler[IsEtiketSutunu] = $"{etiket} ({grup.Count()} kayıt)";
            degerler[IsParaBirimiSutunu] = grup.Key;
            Yaz("Toplam m³", grup.Sum(x => x.ToplamM3));
            Yaz("Ahşap m³", grup.Sum(x => x.AhsapM3));
            Yaz("Kontra m³", grup.Sum(x => x.KontraM3));
            Yaz("Net", grup.Sum(x => x.NetTutar));
            Yaz("KDV", grup.Sum(x => x.KdvTutari));
            Yaz("Toplam", grup.Sum(x => x.ToplamTutar));
            Yaz("Sipariş Net", grup.Sum(x => x.SiparisNetTutar));
            Yaz("Kalan Sipariş", grup.Sum(x => x.KalanSiparisNetTutar));
            yield return new RaporSatiri(tur, degerler);
        }
    }

    /// <summary>İlave, Saha, Yedek ve Özel işte "B × E × Y" (mm); üç ölçü de girilmemişse ya da tür dışındaysa boş.</summary>
    internal static string? IsOlcusu(FinansIsKaydiModel item) =>
        FinansIsRaporAlanlari.OlculuIsTuru(item.IsTuru) && item.Boy > 0 && item.En > 0 && item.Yukseklik > 0
            ? OlcuMetni(item.Boy, item.En, item.Yukseklik)
            : null;

    internal static string? TalepEden(FinansIsKaydiModel item) =>
        FinansIsRaporAlanlari.TalepEden(item.TalepEdenKisi, item.TalepEdenBolum);

    private static string? BosIseNull(string? deger) => string.IsNullOrWhiteSpace(deger) ? null : deger;

    private async Task<BolumluRapor> GiderRaporuAsync(FinansListeFiltre filtre, FinansGiderRaporuGruplamasi gruplama, CancellationToken cancellationToken)
    {
        var report = await _raporVerisi.RaporVerisiAsync(filtre, cancellationToken);
        return GiderRaporu(report.Giderler, gruplama, FiltreMetni(filtre));
    }

    /// <summary>Maliyete giren gider: iptal edilmemiş ve avans olmayan (panel kuralıyla aynı).</summary>
    private static bool MaliyetMi(FinansGiderModel gider) => !gider.IptalEdildi && !gider.AvansMi;

    internal static BolumluRapor GiderRaporu(IReadOnlyList<FinansGiderModel> giderler, FinansGiderRaporuGruplamasi gruplama, string altBaslik)
    {
        var gruplar = gruplama switch
        {
            FinansGiderRaporuGruplamasi.Kategori => giderler
                .GroupBy(x => string.IsNullOrWhiteSpace(x.Kategori) ? "Kategorisiz" : x.Kategori)
                .OrderBy(x => x.Key, StringComparer.Create(TrKultur, true))
                .Select(x => (Baslik: x.Key, Giderler: x.ToList()))
                .ToList(),
            FinansGiderRaporuGruplamasi.Proje => giderler
                .GroupBy(x => string.IsNullOrWhiteSpace(x.ProjeNo) ? "Projesiz" : x.ProjeNo)
                .OrderBy(x => x.Key, StringComparer.Create(TrKultur, true))
                .Select(x => (Baslik: x.Key, Giderler: x.ToList()))
                .ToList(),
            _ => [(Baslik: "Bütün giderler", Giderler: giderler.ToList())]
        };

        var satirlar = new List<RaporSatiri>();
        foreach (var (baslik, grupGiderleri) in gruplar)
        {
            if (gruplama != FinansGiderRaporuGruplamasi.Duz)
                satirlar.Add(RaporSatiri.Bolum($"{baslik} ({grupGiderleri.Count} kayıt)"));
            satirlar.AddRange(grupGiderleri.Select(x => new RaporSatiri(RaporSatirTuru.Veri, GiderRaporuSatiri(x))));
            if (gruplama != FinansGiderRaporuGruplamasi.Duz)
                satirlar.AddRange(GiderToplamSatirlari(grupGiderleri.Where(MaliyetMi).ToList(), "Ara toplam", RaporSatirTuru.AraToplam));
        }

        satirlar.Add(RaporSatiri.Bolum("Genel toplam"));
        satirlar.AddRange(GiderToplamSatirlari(giderler.Where(MaliyetMi).ToList(), "GENEL TOPLAM (avans hariç)", RaporSatirTuru.GenelToplam));
        satirlar.AddRange(GiderToplamSatirlari(giderler.Where(x => !x.IptalEdildi && x.AvansMi).ToList(), "Avans toplamı (maliyete dahil değil)", RaporSatirTuru.AraToplam));

        return new BolumluRapor(
            $"Finans Gider Raporu — {FinansEtiketleri.GiderRaporuGruplamasi(gruplama)}",
            altBaslik,
            [new RaporTablosu("Giderler", GiderRaporuBasliklari, satirlar, GiderRaporuPdfGenislikleri)],
            "Avans ve iptal edilmiş giderler maliyet toplamına girmez; avans ayrı toplanır. Toplamlar para birimi bazındadır.");
    }

    internal static object?[] GiderRaporuSatiri(FinansGiderModel item) =>
    [
        item.Id, item.Tarih, item.FinansTarihi, item.Kategori, item.GiderKalemi, item.FirmaVeyaKisi, item.Aciklama,
        BosIseNull(item.ProjeNo), item.BelgeNo, item.PoNumarasi, item.Miktar, item.Birim, item.BirimFiyat,
        item.KdvDahil ? "Evet" : "Hayır", item.KdvOrani, item.ParaBirimi, item.Matrah, item.KdvTutari, item.ToplamTutar,
        item.IptalEdildi ? "İptal" : item.AvansMi ? "Avans" : null
    ];

    private static IEnumerable<RaporSatiri> GiderToplamSatirlari(IReadOnlyCollection<FinansGiderModel> giderler, string etiket, RaporSatirTuru tur)
    {
        foreach (var grup in giderler.GroupBy(x => x.ParaBirimi).OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            var degerler = new object?[GiderRaporuBasliklari.Length];
            degerler[GiderEtiketSutunu] = $"{etiket} ({grup.Count()} kayıt)";
            degerler[GiderParaBirimiSutunu] = grup.Key;
            degerler[GiderParaBirimiSutunu + 1] = grup.Sum(x => x.Matrah);
            degerler[GiderParaBirimiSutunu + 2] = grup.Sum(x => x.KdvTutari);
            degerler[GiderParaBirimiSutunu + 3] = grup.Sum(x => x.ToplamTutar);
            yield return new RaporSatiri(tur, degerler);
        }
    }
}
