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

    /// <summary>Planı ve ilk revizyonunu yazar: projelerin bütün sandıkları (sevk edilmişler dahil) seçili ve varsayılan izinli gelir.</summary>
    Task<YuklemePlaniDetayi> OlusturAsync(YuklemePlaniOlusturModel model, int? kullaniciId, CancellationToken cancellationToken);

    /// <summary>Yeni revizyon yazar; beklenen revizyon son revizyon değilse 409.</summary>
    Task<YuklemePlaniDetayi> KaydetAsync(YuklemePlaniKaydetModel model, int? kullaniciId, CancellationToken cancellationToken);

    /// <summary>
    /// Kaynak farklarını uygulayan yeni revizyon yazar: değişen ölçü/ağırlık/lokasyon/yük türü kopyalanır, elle
    /// değiştirilmemiş izinler yeni varsayılandan kurulur, yeni sandık eklenir, silinen ya da projeden çıkan sandık düşer.
    /// </summary>
    Task<YuklemePlaniDetayi> KaynaktanGuncelleAsync(int planId, int beklenenRevizyonNo, int? kullaniciId, CancellationToken cancellationToken);

    /// <summary>
    /// Tek adımlı plan hesabı (saha geri bildirimi 19.11.2026 madde 5): planı (yoksa açar) seçilen projelerin güncel
    /// sandıklarıyla, kullanıcının sandık seçimleri, aile/tip, yükleme yeri tercihi ve yükler arası boşlukla yeniden kurar ve yerleşimi
    /// aynı revizyonda yazar. Plan verilmezse aynı proje kümesinin en son planına yazılır. Kaydedilecek yerleşim yoksa
    /// (hesaplanabilir yük yok) hiçbir şey yazılmaz.
    /// </summary>
    Task<YuklemePlaniDetayi> PlanHesaplaAsync(YuklemePlaniHesaplaModel model, int? kullaniciId, CancellationToken cancellationToken);

    /// <summary>
    /// Seçilen projelerin plana gelecek sandıkları (hesapla ile aynı kaynak ve sıra, varsayılan izinlerle); durum
    /// değiştirmez. Proje bulunamazsa 404.
    /// </summary>
    Task<PlanYukOnizlemesi> YukOnizlemeAsync(IReadOnlyList<int> projeIdleri, CancellationToken cancellationToken);

    /// <summary>Elle yerleşimi kaydetmeden denetler (sürükle-bırak sonrası anlık kontrol, isterler 22).</summary>
    Task<PlanYerlesimi> ElleDenetleAsync(int planId, IReadOnlyList<ElleAracModeli> araclar, CancellationToken cancellationToken);

    /// <summary>
    /// Elle düzenlenen yerleşimi denetleyip yeni revizyon olarak yazar (yükler ve ayarlar aynen kopyalanır). Sorunlu
    /// yerleşim de kaydedilir; sonuç "Uygunsuz" olur. Arada başka biri kaydettiyse 409.
    /// </summary>
    Task<YuklemePlaniDetayi> ElleKaydetAsync(ElleYerlesimModeli model, int? kullaniciId, CancellationToken cancellationToken);

    /// <summary>
    /// "Kalanı otomatik diz": verilen araçlardaki yükler yerinde kalır, araçlara konmamış yükler boşluklara ve gerekirse
    /// yeni araçlara yerleşir; sonuç denetlenip döner, kaydedilmez.
    /// </summary>
    Task<PlanYerlesimi> ElleTamamlaAsync(int planId, IReadOnlyList<ElleAracModeli> araclar, CancellationToken cancellationToken);

    /// <summary>Elle yerleşimde eklenebilecek ekipmanlar (teknik verisiyle).</summary>
    Task<IReadOnlyList<ElleEkipmanSecenegi>> ElleEkipmanlariAsync(int planId, CancellationToken cancellationToken);

    /// <summary>
    /// Son revizyonun yüklerini otomatik yerleştirir ve sonucu yeni revizyon olarak yazar (yükler ve ayarlar aynen
    /// kopyalanır). Arada başka biri kaydettiyse 409. Lokasyon tercihi plana kaydedilir.
    /// </summary>
    Task<YuklemePlaniDetayi> HesaplaAsync(int planId, int beklenenRevizyonNo, YuklemeLokasyonTercihi lokasyonTercihi, int? kullaniciId, CancellationToken cancellationToken);
}
