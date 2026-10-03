using _3K.Core.Constants;
using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri.Commands;

public sealed class FinansBelgeYukleCommand : FinansCommand<FinansBelgeModel>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.BelgeYukle;
    public FinansBelgeYukleModel Model { get; init; } = null!;
}

public sealed class FinansKaliciSilCommand : FinansCommand
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.KaliciSil;
    public FinansKaliciSilModel Model { get; init; } = null!;
}
