using _3K.Application.Common;
using _3K.Core.Constants;
using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri.Commands;

public sealed class FinansSiparisOlusturCommand : FinansCommand<FinansSiparisModel>
{
    public override IReadOnlyList<string> Sekmeler => FinansSekmeleri.SiparisAcma;
    public FinansSiparisOlusturModel Model { get; init; } = null!;
}

/// <summary>
/// Mevcut aktif PO'ya yeni iş kalemi bağlar. Bu, aynı müşteri PO'su için sipariş açma işlemidir;
/// sipariş açma yetkisi ister.
/// </summary>
public sealed class FinansSiparisKalemEkleCommand : FinansCommand<FinansSiparisModel>
{
    public override IReadOnlyList<string> Sekmeler => FinansSekmeleri.SiparisAcma;
    public int Id { get; init; }
    public FinansSiparisKalemEkleModel Model { get; init; } = null!;
}

public sealed class FinansSiparisGuncelleCommand : FinansCommand<FinansSiparisModel>
{
    public override IReadOnlyList<string> Sekmeler => [YetkiKodlari.Finans.SekmePoFaturaPo];
    public int Id { get; init; }
    public FinansSiparisGuncelleModel Model { get; init; } = null!;
}

public sealed class FinansSiparisIptalCommand : FinansCommand
{
    public override IReadOnlyList<string> Sekmeler => [YetkiKodlari.Finans.SekmePoFaturaPo];
    public int Id { get; init; }
    public string Aciklama { get; init; } = string.Empty;
}

public sealed class FinansFaturaOlusturCommand : FinansCommand<FinansFaturaModel>
{
    public override IReadOnlyList<string> Sekmeler => [YetkiKodlari.Finans.SekmePoFaturaFatura];
    public FinansFaturaOlusturModel Model { get; init; } = null!;
}

public sealed class FinansFaturaGuncelleCommand : FinansCommand<FinansFaturaModel>
{
    public override IReadOnlyList<string> Sekmeler => [YetkiKodlari.Finans.SekmePoFaturaFatura];
    public int Id { get; init; }
    public FinansFaturaGuncelleModel Model { get; init; } = null!;
}

public sealed class FinansFaturaIptalCommand : FinansCommand
{
    public override IReadOnlyList<string> Sekmeler => [YetkiKodlari.Finans.SekmePoFaturaFatura];
    public int Id { get; init; }
    public string Aciklama { get; init; } = string.Empty;
}

/// <summary>Faturayı ödendi/ödenmedi işaretler (D3); tahsilat bilgisi dışarıdan gelir.</summary>
public sealed class FinansFaturaOdemeCommand : FinansCommand<FinansFaturaModel>
{
    public override IReadOnlyList<string> Sekmeler => [YetkiKodlari.Finans.SekmePoFaturaFatura];
    public int Id { get; init; }
    public FinansOdemeKaydetModel Model { get; init; } = null!;
}
