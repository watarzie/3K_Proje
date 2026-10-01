using _3K.Core.Exceptions;
using _3K.Core.Models;

namespace _3K.Infrastructure.Services.Finans;

/// <summary>
/// Rapor önizlemesi: indirilecek dosyanın başlığı, sütunları ve ilk satırları. Satırlar rapor dosyalarıyla aynı
/// üreticilerden gelir; kullanıcı PDF/Excel indirmeden önce filtrenin neyi kapsadığını görür.
/// </summary>
internal sealed partial class FinansRaporService
{
    private static readonly string[] IsBasliklari =
    [
        "Kayıt No", "Proje No", "Müşteri", "İş Türü", "İş Adı", "Sandık No", "Adet",
        "Talep Eden", "Talep Eden Bölüm", "Birim m³", "Toplam m³", "Üretim Tarihi", "Finans Dönemi", "Durum",
        "Fiyatlandırma", "Birim Fiyat", "Para Birimi", "KDV %", "Net", "KDV", "Toplam", "PO", "Fatura"
    ];

    private static readonly string[] GiderBasliklari =
    [
        "Kayıt No", "Tarih", "Finans Dönemi", "Kategori", "Gider Kalemi", "Firma/Kişi", "Açıklama",
        "Miktar", "Birim", "Birim Fiyat", "Para Birimi", "KDV Dahil", "KDV %", "Matrah", "KDV", "Toplam", "Proje"
    ];

    private static object?[] IsSatiri(FinansIsKaydiModel item) =>
    [
        item.Id, item.ProjeNo, item.Musteri, item.IsTuru.ToString(), item.IsAdi, item.SandikNo,
        item.Adet, item.TalepEdenKisi, item.TalepEdenBolum, item.BirimM3, item.ToplamM3, item.UretimTarihi, item.FinansDonemi, item.Durum.ToString(),
        item.FiyatlandirmaBirimi.ToString(), item.BirimFiyat, item.ParaBirimi, item.KdvOrani,
        item.NetTutar, item.KdvTutari, item.ToplamTutar,
        string.Join(", ", item.PoNumaralari), string.Join(", ", item.FaturaNumaralari)
    ];

    private static object?[] GiderSatiri(FinansGiderModel item) =>
    [
        item.Id, item.Tarih, item.FinansDonemi, item.Kategori, item.GiderKalemi,
        item.FirmaVeyaKisi, item.Aciklama, item.Miktar, item.Birim, item.BirimFiyat,
        item.ParaBirimi, item.KdvDahil ? "Evet" : "Hayır", item.KdvOrani,
        item.Matrah, item.KdvTutari, item.ToplamTutar, item.ProjeNo
    ];

    public async Task<FinansRaporOnizlemeModel> OnizlemeAsync(string tur, FinansListeFiltre filtre, CancellationToken cancellationToken)
    {
        string baslik;
        string[] basliklar;
        IReadOnlyList<object?[]> satirlar;
        switch (tur)
        {
            case "is":
                baslik = "Finans İşleri Raporu";
                basliklar = IsBasliklari;
                satirlar = (await _raporVerisi.RaporVerisiAsync(filtre, cancellationToken)).Isler.Select(IsSatiri).ToList();
                break;
            case "gider":
                baslik = "Finans Gider Raporu";
                basliklar = GiderBasliklari;
                satirlar = (await _raporVerisi.RaporVerisiAsync(filtre, cancellationToken)).Giderler.Select(GiderSatiri).ToList();
                break;
            case "alacak":
                baslik = "Alacak (Vade/Tahsilat) Raporu";
                basliklar = AlacakBasliklari;
                satirlar = AlacakSatirlari(await AlacakFaturalariAsync(filtre, cancellationToken)).ToList();
                break;
            default:
                (baslik, basliklar, var ozet) = await OzetTablosuAsync(tur, filtre, cancellationToken);
                satirlar = ozet;
                break;
        }
        return new FinansRaporOnizlemeModel(
            baslik,
            basliklar,
            satirlar.Take(FinansRaporOnizlemeModel.EnFazlaSatir)
                .Select(satir => (IReadOnlyList<string>)satir.Select(OnizlemeDegeri).ToList())
                .ToList(),
            satirlar.Count);
    }

    private static string OnizlemeDegeri(object? deger) => deger switch
    {
        null => "—",
        decimal sayi => Format(sayi),
        DateTime tarih => tarih.ToString("dd.MM.yyyy"),
        _ => deger.ToString() ?? string.Empty
    };
}
