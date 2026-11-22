using _3K.Core.Models;

namespace _3K.Core.Interfaces;

/// <summary>
/// Araç yükleme (saha): plan revizyonuna sabit yükleme oturumu ve yük bazlı "yüklendi" işaretleri. Hiçbir işlem sandık
/// durumunu, sevk kayıtlarını, hareket geçmişini ya da plan revizyonunu değiştirmez.
/// </summary>
public interface IYuklemeOturumuService
{
    /// <summary>Yerleşimi olan ya da yüklemesi süren planlar (sayfalı; süren oturumu olanlar önce).</summary>
    Task<SayfaliSonuc<AracYuklemePlanOzeti>> YuklenecekPlanlarAsync(string? arama, int? sayfa, int? boyut, CancellationToken cancellationToken);

    /// <summary>Planın yükleme durumu; plan yoksa null.</summary>
    Task<AracYuklemeDurumu?> DurumAsync(int planId, CancellationToken cancellationToken);

    /// <summary>Oturumdaki aracın yerleşimi ve işaretleri; oturum ya da araç yoksa null.</summary>
    Task<AracYuklemeAraci?> AracAsync(int oturumId, int aracNo, CancellationToken cancellationToken);

    /// <summary>Okutulan barkodu çözer (durum değiştirmez). <paramref name="aracNo"/> ekranda açık olan araçtır.</summary>
    Task<YukCozumu> CozumleAsync(int oturumId, string barkod, int aracNo, CancellationToken cancellationToken);

    /// <summary>
    /// Planın son revizyonuyla oturum başlatır. Revizyon beklenenden farklıysa ya da süren oturum varsa 409; son
    /// revizyonda yerleşim yoksa 400.
    /// </summary>
    Task<AracYuklemeDurumu> BaslatAsync(int planId, int beklenenRevizyonNo, int? kullaniciId, CancellationToken cancellationToken);

    /// <summary>
    /// Yükü yüklendi işaretler. Yük zaten yüklüyse yeni kayıt yazmaz. Sıradaki olmayan yük onaysız gelirse 409; yük bu
    /// araçta ya da yerleşimde değilse 400; oturum kapalıysa 409.
    /// </summary>
    Task<YukIsaretSonucu> IsaretleAsync(YukIsaretleModeli model, int? kullaniciId, CancellationToken cancellationToken);

    /// <summary>İşareti gerekçeyle geri alır (kayıt silinmez); yük yeniden işaretlenebilir.</summary>
    Task<AracYuklemeAraci> GeriAlAsync(int oturumId, int isaretId, string gerekce, int? kullaniciId, CancellationToken cancellationToken);

    /// <summary>Oturumu tamamlar (eksik yük varsa gerekçe zorunlu) ya da iptal eder (gerekçe zorunlu).</summary>
    Task<AracYuklemeDurumu> KapatAsync(int oturumId, bool iptal, string? gerekce, int? kullaniciId, CancellationToken cancellationToken);
}
