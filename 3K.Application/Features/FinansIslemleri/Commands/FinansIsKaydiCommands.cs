using _3K.Application.Common;
using _3K.Core.Constants;
using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri.Commands;

public sealed class FinansIsKaydiOlusturCommand : FinansCommand<FinansIsKaydiModel>
{
    public override IReadOnlyList<string> Sekmeler => [YetkiKodlari.Finans.SekmeIsTakibi];
    public FinansIsKaydiKaydetModel Model { get; init; } = null!;
}

/// <summary>
/// Münferit iş oluşturma: fiyat tarifeden geldiği için yalnız iş ekleme ve Münferit İşler sekmesi yazma izni
/// ister (fiyat değiştirme izni gerekmez).
/// </summary>
public sealed class FinansMunferitIsOlusturCommand : FinansCommand<FinansIsKaydiModel>
{
    public override IReadOnlyList<string> Sekmeler => [YetkiKodlari.Finans.SekmeMunferitIsler];
    public FinansMunferitIsKaydetModel Model { get; init; } = null!;
}

public sealed class FinansIsKaydiIptalCommand : FinansCommand
{
    public override IReadOnlyList<string> Sekmeler => FinansSekmeleri.IsDuzenleme;
    public int Id { get; init; }
    public string Aciklama { get; init; } = string.Empty;
}

public sealed class FinansIsKaydiGeriAlCommand : FinansCommand
{
    public override IReadOnlyList<string> Sekmeler => FinansSekmeleri.IsDuzenleme;
    public int Id { get; init; }
}

public sealed class FinansOzelIsAylikDegerGuncelleCommand : FinansCommand
{
    public override IReadOnlyList<string> Sekmeler => [YetkiKodlari.Finans.SekmeIsTakibi];
    public int Id { get; init; }
    public FinansAylikDegerModel Model { get; init; } = null!;
}

public sealed class FinansTarihiDegistirCommand : FinansCommand<FinansIsKaydiModel>
{
    public override IReadOnlyList<string> Sekmeler => FinansSekmeleri.IsDuzenleme;
    public int Id { get; init; }
    public FinansTarihiDegistirModel Model { get; init; } = null!;
}

/// <summary>Ahşap ve kontra m³'ün elle girişi; fiyatlandırma değiştirme yazma iznini kullanır.</summary>
public sealed class FinansHacimGirCommand : FinansCommand<FinansIsKaydiModel>
{
    public override IReadOnlyList<string> Sekmeler => FinansSekmeleri.IsDuzenleme;
    public int Id { get; init; }
    public FinansHacimModel Model { get; init; } = null!;
}

public sealed class FinansFiyatlandirCommand : FinansCommand<FinansIsKaydiModel>
{
    public override IReadOnlyList<string> Sekmeler => FinansSekmeleri.IsDuzenleme;
    public int Id { get; init; }
    public FinansFiyatlandirmaModel Model { get; init; } = null!;
}
