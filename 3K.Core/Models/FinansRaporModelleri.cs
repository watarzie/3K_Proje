namespace _3K.Core.Models;

public sealed class FinansRaporModel
{
    public FinansListeFiltre Filtre { get; init; } = new();
    public IReadOnlyList<FinansIsKaydiModel> Isler { get; init; } = Array.Empty<FinansIsKaydiModel>();
    public IReadOnlyList<FinansGiderModel> Giderler { get; init; } = Array.Empty<FinansGiderModel>();
    public IReadOnlyList<FinansParaToplamiModel> GelirToplamlari { get; init; } = Array.Empty<FinansParaToplamiModel>();
    public IReadOnlyList<FinansParaToplamiModel> GiderToplamlari { get; init; } = Array.Empty<FinansParaToplamiModel>();
    public IReadOnlyList<FinansParaToplamiModel> NetToplamlari { get; init; } = Array.Empty<FinansParaToplamiModel>();
}
