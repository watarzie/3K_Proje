using _3K.Core.Models;

namespace _3K.Core.Interfaces;

/// <summary>
/// Ekipman kitaplığı: aileler ve tipler, konfigürasyon listesi ve ayrıntısı, Excel şablonu/dışa aktarımı, Excel'den
/// içe aktarma (önizleme ve atomik uygulama) ve revizyon doğrulama.
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

    /// <summary>Son revizyonu doğrulanmış işaretler; revizyon son değilse veya zaten doğrulanmışsa 409.</summary>
    Task<EkipmanKonfigurasyonDetayi> DogrulaAsync(int konfigurasyonId, int revizyonNo, int kullaniciId, CancellationToken cancellationToken);
}
