using _3K.Application.Common;
using _3K.Core.Constants;
using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri.Queries;

public sealed class FinansGiderlerQuery : FinansQuery<FinansSayfaliSonuc<FinansGiderModel>>
{
    public override IReadOnlyList<string> Sekmeler => [YetkiKodlari.Finans.SekmeGiderler];
    public FinansListeFiltre Filtre { get; init; } = new();
}
