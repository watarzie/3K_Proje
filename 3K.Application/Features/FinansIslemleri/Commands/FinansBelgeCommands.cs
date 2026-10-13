using _3K.Core.Constants;
using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri.Commands;

public sealed class FinansBelgeYukleCommand : FinansCommand<FinansBelgeModel>
{
    public override IReadOnlyList<string> Sekmeler => FinansSekmeleri.Varlik(Model?.HedefTuru);
    public FinansBelgeYukleModel Model { get; init; } = null!;
}

public sealed class FinansKaliciSilCommand : FinansCommand
{
    public override IReadOnlyList<string> Sekmeler => FinansSekmeleri.Varlik(Model?.VarlikTuru);
    public FinansKaliciSilModel Model { get; init; } = null!;
}
