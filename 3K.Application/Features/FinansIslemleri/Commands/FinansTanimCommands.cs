using _3K.Core.Constants;
using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri.Commands;

public sealed class FinansDuzenliIsOlusturCommand : FinansCommand<FinansDuzenliIsModel>
{
    public override IReadOnlyList<string> Sekmeler => [YetkiKodlari.Finans.SekmeTanimlar];
    public FinansDuzenliIsKaydetModel Model { get; init; } = null!;
}

public sealed class FinansDuzenliIsGuncelleCommand : FinansCommand<FinansDuzenliIsModel>
{
    public override IReadOnlyList<string> Sekmeler => [YetkiKodlari.Finans.SekmeTanimlar];
    public int Id { get; init; }
    public FinansDuzenliIsKaydetModel Model { get; init; } = null!;
}

/// <summary>Düzenli işin verilen tarihe kadarki dönem kayıtlarını üretir (gelecek aylar için PO açılabilsin diye).</summary>
public sealed class FinansDuzenliIsDonemUretCommand : FinansCommand<FinansDonemOlusturSonucModel>
{
    public override IReadOnlyList<string> Sekmeler => [YetkiKodlari.Finans.SekmeTanimlar];
    public int Id { get; init; }
    public DateTime BitisTarihi { get; init; }
}

public sealed class FinansGiderKategoriOlusturCommand : FinansCommand<FinansGiderKategoriModel>
{
    public override IReadOnlyList<string> Sekmeler => [YetkiKodlari.Finans.SekmeGiderKategorileri];
    public FinansGiderKategoriKaydetModel Model { get; init; } = null!;
}

public sealed class FinansGiderKategoriGuncelleCommand : FinansCommand<FinansGiderKategoriModel>
{
    public override IReadOnlyList<string> Sekmeler => [YetkiKodlari.Finans.SekmeGiderKategorileri];
    public int Id { get; init; }
    public FinansGiderKategoriKaydetModel Model { get; init; } = null!;
}

public sealed class FinansGiderKalemiOlusturCommand : FinansCommand<FinansGiderKalemiModel>
{
    public override IReadOnlyList<string> Sekmeler => [YetkiKodlari.Finans.SekmeGiderKategorileri];
    public FinansGiderKalemiKaydetModel Model { get; init; } = null!;
}

public sealed class FinansGiderKalemiGuncelleCommand : FinansCommand<FinansGiderKalemiModel>
{
    public override IReadOnlyList<string> Sekmeler => [YetkiKodlari.Finans.SekmeGiderKategorileri];
    public int Id { get; init; }
    public FinansGiderKalemiKaydetModel Model { get; init; } = null!;
}

public sealed class FinansUrunOlusturCommand : FinansCommand<FinansUrunModel>
{
    public override IReadOnlyList<string> Sekmeler => [YetkiKodlari.Finans.SekmeTanimlar];
    public FinansUrunKaydetModel Model { get; init; } = null!;
}

public sealed class FinansUrunGuncelleCommand : FinansCommand<FinansUrunModel>
{
    public override IReadOnlyList<string> Sekmeler => [YetkiKodlari.Finans.SekmeTanimlar];
    public int Id { get; init; }
    public FinansUrunKaydetModel Model { get; init; } = null!;
}

public sealed class FinansUrunPasiflestirCommand : FinansCommand
{
    public override IReadOnlyList<string> Sekmeler => [YetkiKodlari.Finans.SekmeTanimlar];
    public int Id { get; init; }
}

/// <summary>
/// D4: tarife değişikliği açık kalemleri kendiliğinden değiştirmez; birim fiyat değiştirme yetkisiyle ürüne bağlı,
/// PO'ya bağlanmamış kalemler bu işlemle geçerli tarife sürümüne göre yeniden fiyatlanır.
/// </summary>
public sealed class FinansUrunYenidenFiyatlaCommand : FinansCommand<FinansYenidenFiyatlamaSonucModel>
{
    public override IReadOnlyList<string> Sekmeler => [YetkiKodlari.Finans.SekmeTanimlar];
    public int Id { get; init; }
    public string Aciklama { get; init; } = string.Empty;
}

/// <summary>
/// Hatalı tarife düzeltme: sürümün birim fiyatı/KDV'si gerekçeyle düzeltilir ve yalnız o sürümün tarife değeriyle
/// fiyatlanmış açık kalemler yeni değere çekilir (<see cref="FinansTarifeDuzeltmeKurali"/>).
/// </summary>
public sealed class FinansTarifeDuzeltCommand : FinansCommand<FinansTarifeDuzeltmeSonucModel>
{
    public override IReadOnlyList<string> Sekmeler => [YetkiKodlari.Finans.SekmeTanimlar];
    public int UrunId { get; init; }
    public int TarifeId { get; init; }
    public FinansTarifeDuzeltModel Model { get; init; } = null!;
}

/// <summary>Para birimi tanımı (F1); finans tanımlarıyla aynı yetkiyle yönetilir.</summary>
public sealed class FinansParaBirimiOlusturCommand : FinansCommand<FinansParaBirimiModel>
{
    public override IReadOnlyList<string> Sekmeler => [YetkiKodlari.Finans.SekmeTanimlar];
    public FinansParaBirimiKaydetModel Model { get; init; } = null!;
}

public sealed class FinansParaBirimiGuncelleCommand : FinansCommand<FinansParaBirimiModel>
{
    public override IReadOnlyList<string> Sekmeler => [YetkiKodlari.Finans.SekmeTanimlar];
    public int Id { get; init; }
    public FinansParaBirimiKaydetModel Model { get; init; } = null!;
}

/// <summary>Vade parametreleri (D3): varsayılan vade günü ve vade uyarısı penceresi.</summary>
public sealed class FinansParametreleriKaydetCommand : FinansCommand<FinansParametrelerModel>
{
    public override IReadOnlyList<string> Sekmeler => [YetkiKodlari.Finans.SekmeTanimlar];
    public FinansParametrelerModel Model { get; init; } = null!;
}
