using _3K.Core.Models;

namespace _3K.Core.Interfaces;

/// <summary>
/// Ekipman kitaplığı: aileler ve tipler, konfigürasyon listesi ve ayrıntısı, Excel şablonu/dışa aktarımı, Excel'den
/// içe aktarma (önizleme ve atomik uygulama), ekrandan ekleme/güncelleme ve revizyon doğrulama.
/// </summary>
public interface IEkipmanKitapligiService
{
    Task<IReadOnlyList<EkipmanAilesiModeli>> AileleriListeleAsync(CancellationToken cancellationToken);

    Task<SayfaliSonuc<EkipmanKonfigurasyonOzeti>> KonfigurasyonlariListeleAsync(
        EkipmanListeKriteri kriter, int? sayfa, int? boyut, CancellationToken cancellationToken);

    Task<EkipmanKonfigurasyonDetayi?> KonfigurasyonGetirAsync(int konfigurasyonId, CancellationToken cancellationToken);

    /// <summary>Boş içe aktarım şablonu: sütun açıklamaları ve tip kodları sayfası.</summary>
    Task<byte[]> SablonOlusturAsync(CancellationToken cancellationToken);

    /// <summary>Kitaplığın etkin konfigürasyonları şablon biçiminde (düzenleyip yeniden içe aktarmak için).</summary>
    Task<byte[]> DisaAktarAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Excel'i okur ve denetler; <paramref name="uygula"/> doğruysa ve sorun yoksa tek transaction'da yazar. Teknik verisi
    /// değişen konfigürasyon yeni (doğrulanmamış) revizyon alır, değişmeyene dokunulmaz.
    /// </summary>
    Task<EkipmanIceAktarimSonucu> IceAktarAsync(Stream dosya, bool uygula, int? kullaniciId, CancellationToken cancellationToken);

    /// <summary>
    /// Ekrandan yeni konfigürasyon: tek konfigürasyonluk bir içe aktarımla aynı kurallardan geçer ve ilk (doğrulanmamış)
    /// revizyonu yazar. Aynı kod varsa 409; kural sorunu varsa hiçbir şey yazılmaz ve sorunlar döner.
    /// </summary>
    Task<EkipmanKayitSonucu> EkleAsync(EkipmanKonfigurasyonKaydi kayit, int? kullaniciId, CancellationToken cancellationToken);

    /// <summary>
    /// Ekrandan güncelleme, içe aktarımla aynı revizyon kuralıyla: teknik verisi ya da kaynağı değişen konfigürasyon yeni
    /// (doğrulanmamış) revizyon alır; yalnız adı değişenin adı güncellenir; değişiklik yoksa hiçbir şey yazılmaz. Kayıt
    /// yoksa 404, kod farklıysa 400, <paramref name="beklenenRevizyonNo"/> son revizyon değilse 409.
    /// </summary>
    Task<EkipmanKayitSonucu> GuncelleAsync(
        int konfigurasyonId, int beklenenRevizyonNo, EkipmanKonfigurasyonKaydi kayit, int? kullaniciId, CancellationToken cancellationToken);

    /// <summary>Son revizyonu doğrulanmış işaretler; revizyon son değilse veya zaten doğrulanmışsa 409.</summary>
    Task<EkipmanKonfigurasyonDetayi> DogrulaAsync(int konfigurasyonId, int revizyonNo, int kullaniciId, CancellationToken cancellationToken);
}
