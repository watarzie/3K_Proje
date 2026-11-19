using _3K.Core.Models;

namespace _3K.Core.Interfaces;

/// <summary>
/// Yükleme planının iki PDF raporu (Simülasyon isterleri 28–29). İkisi de aynı plan, revizyon ve araç numaralarını
/// kullanır; girdi son kaydedilmiş revizyonun yerleşimidir (yerleşim yoksa rapor üretilmez).
/// </summary>
public interface IYuklemePlaniRaporOlusturucu
{
    /// <summary>Araç talep formu: depoya gereken araç tipleri ve adetleri; özel araç gerekçesi ve atanamayan yükler.</summary>
    byte[] AracTalepRaporu(YuklemePlaniDetayi plan, DateTime raporTarihi);

    /// <summary>
    /// Araç bazlı yükleme planı: her araç çizimleri ve sandık numaralı yük listesiyle aynı sayfada (yükleme adımı sırası);
    /// sayfaya sığmayan liste sonraki sayfada iki sütunla sürer.
    /// </summary>
    byte[] YuklemePlaniRaporu(YuklemePlaniDetayi plan, DateTime raporTarihi);
}
