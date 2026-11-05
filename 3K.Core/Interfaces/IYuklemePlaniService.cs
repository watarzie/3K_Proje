using _3K.Core.Enums;
using _3K.Core.Models;

namespace _3K.Core.Interfaces;

/// <summary>
/// Yükleme planı: plan listesi, oluşturma, kaydetme (her kaydetme değişmez yeni revizyon) ve kaynaktan güncelleme.
/// Ret <see cref="Exceptions.IsKuraliException"/> (400/409) veya <see cref="Exceptions.KayitBulunamadiException"/>
/// (404) ile bildirilir; ret durumunda hiçbir kayıt değişmez.
/// </summary>
public interface IYuklemePlaniService
{
    Task<YuklemePlaniTanimlari> TanimlarAsync(CancellationToken cancellationToken);

    /// <summary>Plana eklenebilecek projeler (proje no veya müşteriyle süzülür), sayfalı.</summary>
    Task<SayfaliSonuc<PlanProjeAdayi>> ProjeAdaylariAsync(string? arama, int? sayfa, int? boyut, CancellationToken cancellationToken);

    /// <summary>Planlar en son kaydedilen önce, sayfalı.</summary>
    Task<SayfaliSonuc<YuklemePlaniOzeti>> PlanlarAsync(string? arama, int? sayfa, int? boyut, CancellationToken cancellationToken);

    /// <summary>Planın son revizyonu ve güncel kaynakla farkları; plan yoksa null.</summary>
    Task<YuklemePlaniDetayi?> PlanGetirAsync(int planId, CancellationToken cancellationToken);

    /// <summary>Planı ve ilk revizyonunu yazar: projelerin sevk edilmemiş bütün sandıkları seçili ve varsayılan izinli gelir.</summary>
    Task<YuklemePlaniDetayi> OlusturAsync(YuklemePlaniOlusturModel model, int? kullaniciId, CancellationToken cancellationToken);

    /// <summary>Yeni revizyon yazar; beklenen revizyon son revizyon değilse 409.</summary>
    Task<YuklemePlaniDetayi> KaydetAsync(YuklemePlaniKaydetModel model, int? kullaniciId, CancellationToken cancellationToken);

    /// <summary>
    /// Kaynak farklarını uygulayan yeni revizyon yazar: değişen ölçü/ağırlık/lokasyon/yük türü kopyalanır, elle
    /// değiştirilmemiş izinler yeni varsayılandan kurulur, yeni sandık eklenir, silinen veya sevk edilen sandık çıkar.
    /// </summary>
    Task<YuklemePlaniDetayi> KaynaktanGuncelleAsync(int planId, int beklenenRevizyonNo, int? kullaniciId, CancellationToken cancellationToken);

    /// <summary>
    /// Son revizyonun yüklerini otomatik yerleştirir ve sonucu yeni revizyon olarak yazar (yükler ve ayarlar aynen
    /// kopyalanır). Arada başka biri kaydettiyse 409. Lokasyon tercihi plana kaydedilir.
    /// </summary>
    Task<YuklemePlaniDetayi> HesaplaAsync(int planId, int beklenenRevizyonNo, YuklemeLokasyonTercihi lokasyonTercihi, int? kullaniciId, CancellationToken cancellationToken);
}
