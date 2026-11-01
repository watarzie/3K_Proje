namespace _3K.Application.Features.SevkiyatIslemleri.Commands;

/// <summary>
/// Sevkin sefer başlığı: tarih, araç/dorse plakası, şoför ve açıklama. Toplu sevk (tır seferi) ve tekil proje sevki
/// aynı alanları taşır ve <see cref="Validators.SevkSeferiBilgisiValidator"/> ile aynı kurallarla doğrulanır.
/// </summary>
public interface ISevkSeferiBilgisi
{
    /// <summary>Boşsa tekil sevkte işlem anı kullanılır; toplu sevkte her zaman doludur.</summary>
    DateTime? SeferSevkTarihi { get; }
    string? AracPlaka { get; }
    string? Sofor { get; }
    string? DorsePlaka { get; }
    string? Aciklama { get; }
}
