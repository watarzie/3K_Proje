using _3K.Core.Models;

namespace _3K.Core.Interfaces;

/// <summary>
/// Yükleme planının iki PDF raporu (Simülasyon isterleri 28–29). İkisi de aynı plan, revizyon ve araç numaralarını
/// kullanır; girdi son kaydedilmiş revizyonun yerleşimidir (yerleşim yoksa rapor üretilmez).
/// </summary>
public interface IYuklemePlaniRaporOlusturucu
{
    /// <summary>Araç talep ve planlama raporu: araç tipleri ve adetleri, zorunlu özel ekipman ve gerekçesi, uyarılar.</summary>
    byte[] AracTalepRaporu(YuklemePlaniDetayi plan, DateTime raporTarihi);

    /// <summary>Araç bazlı yükleme planı: her araç için görünümler, yük listesi, yükleme sırası, kontroller.</summary>
    byte[] YuklemePlaniRaporu(YuklemePlaniDetayi plan, DateTime raporTarihi);
}
