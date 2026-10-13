using _3K.Application.Common;
using _3K.Core.Constants;
using _3K.Core.Enums;
using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri.Queries;

public sealed class FinansDuzenliIslerQuery : FinansQuery<FinansSayfaliSonuc<FinansDuzenliIsModel>>
{
    public override IReadOnlyList<string> Sekmeler => [YetkiKodlari.Finans.SekmeTanimlar];
    public bool SadeceAktif { get; init; }
    public string? Arama { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}

public sealed class FinansGiderKategorileriQuery : FinansQuery<IReadOnlyList<FinansGiderKategoriModel>>
{
    public override IReadOnlyList<string> Sekmeler => FinansSekmeleri.GiderTanimlari;
    public bool SadeceAktif { get; init; }
}

public sealed class FinansGiderKalemleriQuery : FinansQuery<IReadOnlyList<FinansGiderKalemiModel>>
{
    public override IReadOnlyList<string> Sekmeler => FinansSekmeleri.GiderTanimlari;
    public int? KategoriId { get; init; }
    public bool SadeceAktif { get; init; }
}

public sealed class FinansGiderKutuphaneKategorileriQuery : FinansQuery<IReadOnlyList<FinansGiderKategoriModel>>
{
    public override IReadOnlyList<string> Sekmeler => [YetkiKodlari.Finans.SekmeGiderKategorileri];
    public bool SadeceAktif { get; init; }
}

public sealed class FinansGiderKutuphaneKalemleriQuery : FinansQuery<IReadOnlyList<FinansGiderKalemiModel>>
{
    public override IReadOnlyList<string> Sekmeler => [YetkiKodlari.Finans.SekmeGiderKategorileri];
    public int? KategoriId { get; init; }
    public bool SadeceAktif { get; init; }
}

/// <summary>
/// Hatalı tarife düzeltmenin önizlemesi; yalnız düzeltmeyi yapabilen (birim fiyat değiştirme yazma yetkisi)
/// kullanıcıya açıktır. Fiyat/tutar alanları ayrıca HTTP sınırında alan izinleriyle maskelenir.
/// </summary>
public sealed class FinansTarifeDuzeltmeOnizlemeQuery : FinansQuery<FinansTarifeDuzeltmeOnizlemeModel>
{
    public override IReadOnlyList<string> Sekmeler => [YetkiKodlari.Finans.SekmeTanimlar];
    public override YetkiTipi RequiredYetkiTipi => YetkiTipi.W;
    public int UrunId { get; init; }
    public int TarifeId { get; init; }
    public decimal BirimFiyat { get; init; }
    public decimal KdvOrani { get; init; }
}

/// <summary>
/// Münferit iş formunun ürün seçicisi: aktif ürünlerin kod/ad/fiyatlandırma birimi (fiyatsız, en fazla 50).
/// Münferit İşler sekmesini görebilen kullanıcıya açıktır; tarife listesi yetkisi istemez.
/// </summary>
public sealed class FinansUrunSecenekleriQuery : FinansQuery<IReadOnlyList<FinansUrunSecenekModel>>
{
    public override IReadOnlyList<string> Sekmeler => [YetkiKodlari.Finans.SekmeMunferitIsler];
    public string? Arama { get; init; }
    public _3K.Core.Enums.FinansIsTuru? IsTuru { get; init; }
    /// <summary>Fiyatı gösterilecek tarih (iş tarihi); boşsa bugün.</summary>
    public DateTime? Tarih { get; init; }
}

public sealed class FinansUrunlerQuery : FinansQuery<FinansSayfaliSonuc<FinansUrunModel>>
{
    public override IReadOnlyList<string> Sekmeler => [YetkiKodlari.Finans.SekmeTanimlar];
    public bool SadeceAktif { get; init; }
    public DateTime? TarifeTarihi { get; init; }
    public string? Arama { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}

/// <summary>Para birimi tanımları (F1); seçim listeleri, kartlar ve filtreler için finans modülüne erişen herkes okur.</summary>
public sealed class FinansParaBirimleriQuery : FinansQuery<IReadOnlyList<FinansParaBirimiModel>>
{
    public override IReadOnlyList<string> Sekmeler => [YetkiKodlari.Finans.Modul];
    public bool SadeceAktif { get; init; }
}

/// <summary>Vade parametreleri yalnız "İş ve Tarife Tanımları" ekranında okunur; yönetimiyle aynı kod (R).</summary>
/// <summary>Vade parametreleri: Tanımlar'da yönetilir, fatura formu varsayılan vade gününü okur (PO/Fatura).</summary>
public sealed class FinansParametrelerQuery : FinansQuery<FinansParametrelerModel>
{
    public override IReadOnlyList<string> Sekmeler => [YetkiKodlari.Finans.SekmeTanimlar, YetkiKodlari.Finans.SekmePoFatura];

}
