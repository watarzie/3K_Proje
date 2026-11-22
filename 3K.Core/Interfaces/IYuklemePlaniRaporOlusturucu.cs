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
    /// Araç bazlı yükleme planı: ilk sayfa planın özeti (araç sayısı, tip dağılımı, yük sayısı, toplam ağırlık ve hacim);
    /// her araç kendi sayfasında 3B çizim, yandan/üstten/arkadan görünümler, araç–kullanım–toplam–ağırlık merkezi
    /// bilgileri ve sandık numaralı yük tablosuyla (yükleme adımı sırası, her sandık ayrı renkte) başlar; sayfaya sığmayan
    /// liste iki sütunla sonraki sayfada sürer. Araca atanamayan yükler son sayfadadır.
    /// </summary>
    byte[] YuklemePlaniRaporu(YuklemePlaniDetayi plan, DateTime raporTarihi);
}
