using _3K.Core.Models;

namespace _3K.Core.Interfaces;

/// <summary>Mevzuat kural setleri, kuralları ve kaynakları (salt okunur).</summary>
public interface IMevzuatKuralService
{
    /// <summary>Kural seti sürümleri, yetki alanı ve sürüme göre (küçük küme).</summary>
    Task<IReadOnlyList<MevzuatKuralSetiOzeti>> KuralSetleriAsync(CancellationToken cancellationToken);

    Task<MevzuatKuralSetiDetayi?> KuralSetiGetirAsync(int kuralSetiId, CancellationToken cancellationToken);
}
