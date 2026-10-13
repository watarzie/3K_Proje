namespace _3K.Core.Constants;

/// <summary>
/// Finans yetkisi yalnız sekme ve alt sekme düzeyindedir (İş Takibi, Tanımlar ve PO/Fatura işlemleri alt sekmeye
/// bağlıdır; sekme alt sekmenin tavanıdır): sekmenin R izni sekmedeki bütün verileri (tutar, fiyat, gelir, gider,
/// kârlılık, ölçü, m³ dahil) gösterir, W izni sekmedeki bütün işlemleri (kalıcı silme dahil) yapar. Birden çok sekmede
/// açılan ortak işlemler (ör. kayıt detayı, belgeler) aşağıdaki sekme kümelerinden herhangi birinin izniyle yapılır.
/// Ön yüz aynası: <c>features/finans-yonetimi/ortak/finans-izinleri.ts</c>.
/// </summary>
public static class FinansSekmeleri
{
    /// <summary>İş kaydı, PO ve fatura detayını, hareketleri ve genel aramayı gösteren sekmeler.</summary>
    public static readonly IReadOnlyList<string> Kayit =
    [
        YetkiKodlari.Finans.SekmeIsTakibi,
        YetkiKodlari.Finans.SekmePanel,
        YetkiKodlari.Finans.SekmePoFatura,
        YetkiKodlari.Finans.SekmeMunferitIsler,
        YetkiKodlari.Finans.SekmeRaporlar
    ];

    /// <summary>Kayıt sekmeleri ve Giderler: belgeler ve değişiklik geçmişi.</summary>
    public static readonly IReadOnlyList<string> KayitVeGider = [.. Kayit, YetkiKodlari.Finans.SekmeGiderler];

    /// <summary>İş kaydını değiştiren işlemler (fiyatlandırma, hacim, finans tarihi, iptal/geri alma).</summary>
    public static readonly IReadOnlyList<string> IsDuzenleme =
    [
        YetkiKodlari.Finans.SekmeIsTakibi,
        YetkiKodlari.Finans.SekmeMunferitIsler
    ];

    /// <summary>PO açma: PO'lar alt sekmesi ve "Sipariş aç" düğmesi bulunan İş Takibi ile Münferit İşler sekmeleri.</summary>
    public static readonly IReadOnlyList<string> SiparisAcma =
    [
        YetkiKodlari.Finans.SekmePoFaturaPo,
        YetkiKodlari.Finans.SekmeIsTakibi,
        YetkiKodlari.Finans.SekmeMunferitIsler
    ];

    /// <summary>Gider kategori ve kalemi listesi: gider girişi, kategori tanımı ve rapor filtreleri.</summary>
    public static readonly IReadOnlyList<string> GiderTanimlari =
    [
        YetkiKodlari.Finans.SekmeGiderler,
        YetkiKodlari.Finans.SekmeGiderKategorileri,
        YetkiKodlari.Finans.SekmeRaporlar
    ];

    /// <summary>
    /// Belge yükleme ve kalıcı silmede hedef kaydın sekmesi: iş kaydı → İş Takibi/Münferit İşler, PO → PO'lar,
    /// fatura → Faturalar alt sekmesi, gider → Giderler. Bilinmeyen tür İş Takibi sayılır (tür doğrulaması servistedir).
    /// </summary>
    public static IReadOnlyList<string> Varlik(string? varlikTuru) => varlikTuru switch
    {
        "Siparis" => [YetkiKodlari.Finans.SekmePoFaturaPo],
        "Fatura" => [YetkiKodlari.Finans.SekmePoFaturaFatura],
        "Gider" => [YetkiKodlari.Finans.SekmeGiderler],
        "IsKaydi" => IsDuzenleme,
        _ => [YetkiKodlari.Finans.SekmeIsTakibi]
    };
}
