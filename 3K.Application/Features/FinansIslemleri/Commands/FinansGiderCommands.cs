using _3K.Core.Constants;
using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri.Commands;

public sealed class FinansGiderOlusturCommand : FinansCommand<FinansGiderModel>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.GiderEkle;
    public FinansGiderKaydetModel Model { get; init; } = null!;
}

public sealed class FinansGiderGuncelleCommand : FinansCommand<FinansGiderModel>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.GiderDuzenle;
    public int Id { get; init; }
    public FinansGiderKaydetModel Model { get; init; } = null!;
}

public sealed class FinansGiderIptalCommand : FinansCommand
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.GiderIptal;
    public int Id { get; init; }
    public string Aciklama { get; init; } = string.Empty;
}
