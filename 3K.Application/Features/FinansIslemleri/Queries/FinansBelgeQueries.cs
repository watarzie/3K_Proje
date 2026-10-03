using _3K.Core.Constants;
using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri.Queries;

public sealed class FinansBelgelerQuery : FinansQuery<IReadOnlyList<FinansBelgeModel>>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.BelgeIndir;
    public string HedefTuru { get; init; } = string.Empty;
    public int HedefId { get; init; }
}

public sealed class FinansBelgeIndirQuery : FinansQuery<DosyaIcerigi>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.BelgeIndir;
    public int Id { get; init; }
}

public sealed class FinansKaliciSilOnizlemeQuery : FinansQuery<FinansKaliciSilOnizlemeModel>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.KaliciSil;
    public string VarlikTuru { get; init; } = string.Empty;
    public int Id { get; init; }
}
