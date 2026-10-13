using _3K.Core.Constants;
using _3K.Core.Enums;
using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri.Queries;

public sealed class FinansBelgelerQuery : FinansQuery<IReadOnlyList<FinansBelgeModel>>
{
    public override IReadOnlyList<string> Sekmeler => FinansSekmeleri.KayitVeGider;
    public string HedefTuru { get; init; } = string.Empty;
    public int HedefId { get; init; }
}

public sealed class FinansBelgeIndirQuery : FinansQuery<DosyaIcerigi>
{
    public override IReadOnlyList<string> Sekmeler => FinansSekmeleri.KayitVeGider;
    public int Id { get; init; }
}

public sealed class FinansKaliciSilOnizlemeQuery : FinansQuery<FinansKaliciSilOnizlemeModel>
{
    public override IReadOnlyList<string> Sekmeler => FinansSekmeleri.Varlik(VarlikTuru);
    public override YetkiTipi RequiredYetkiTipi => YetkiTipi.W;
    public string VarlikTuru { get; init; } = string.Empty;
    public int Id { get; init; }
}
