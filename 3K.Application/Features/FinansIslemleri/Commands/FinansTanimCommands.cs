using _3K.Core.Constants;
using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri.Commands;

public sealed class FinansDuzenliIsOlusturCommand : FinansCommand<FinansDuzenliIsModel>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.DuzenliIsYonet;
    public FinansDuzenliIsKaydetModel Model { get; init; } = null!;
}

public sealed class FinansDuzenliIsGuncelleCommand : FinansCommand<FinansDuzenliIsModel>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.DuzenliIsYonet;
    public int Id { get; init; }
    public FinansDuzenliIsKaydetModel Model { get; init; } = null!;
}

public sealed class FinansGiderKategoriOlusturCommand : FinansCommand<FinansGiderKategoriModel>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.GiderKutuphanesiYonet;
    public FinansGiderKategoriKaydetModel Model { get; init; } = null!;
}

public sealed class FinansGiderKategoriGuncelleCommand : FinansCommand<FinansGiderKategoriModel>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.GiderKutuphanesiYonet;
    public int Id { get; init; }
    public FinansGiderKategoriKaydetModel Model { get; init; } = null!;
}

public sealed class FinansGiderKalemiOlusturCommand : FinansCommand<FinansGiderKalemiModel>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.GiderKutuphanesiYonet;
    public FinansGiderKalemiKaydetModel Model { get; init; } = null!;
}

public sealed class FinansGiderKalemiGuncelleCommand : FinansCommand<FinansGiderKalemiModel>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.GiderKutuphanesiYonet;
    public int Id { get; init; }
    public FinansGiderKalemiKaydetModel Model { get; init; } = null!;
}

public sealed class FinansUrunOlusturCommand : FinansCommand<FinansUrunModel>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.IsKutuphanesiYonet;
    public FinansUrunKaydetModel Model { get; init; } = null!;
}

public sealed class FinansUrunGuncelleCommand : FinansCommand<FinansUrunModel>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.IsKutuphanesiYonet;
    public int Id { get; init; }
    public FinansUrunKaydetModel Model { get; init; } = null!;
}

public sealed class FinansUrunPasiflestirCommand : FinansCommand
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.IsKutuphanesiYonet;
    public int Id { get; init; }
}

public sealed class FinansSablonKaydetCommand : FinansCommand<FinansSablonModel>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.SablonYonet;
    public int? Id { get; init; }
    public FinansSablonKaydetModel Model { get; init; } = null!;
}
