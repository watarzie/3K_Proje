namespace _3K.Core.Models;

/// <summary>
/// Sunucu tarafından doğrulanmış revizyon talebinin onaya alındığını veya
/// doğrudan uygulandığını bildirir. Excel içeriği bu modelle istemciye taşınmaz.
/// </summary>
public sealed class CekiRevizyonOnayTalebiSonuc
{
    /// <summary>
    /// İstemcinin 202/onay kuyruğu ile 200/doğrudan uygulama sonucunu açıkça
    /// ayırt edebilmesini sağlayan kararlı discriminator.
    /// </summary>
    public string SonucTipi { get; init; } = CekiRevizyonTalepSonucTipleri.OnayBekliyor;
    public int TalepId { get; init; }
    public int ProjeId { get; init; }
    public string ProjeNo { get; init; } = string.Empty;
    public int AnaCekiId { get; init; }
    public string DosyaAdi { get; init; } = string.Empty;
    public int EklenenSatirSayisi { get; init; }
    public int GuncellenenSatirSayisi { get; init; }
    public int SilinenSatirSayisi { get; init; }
    public int? UygulananRevizyonCekiId { get; init; }
    public string Mesaj { get; init; } = string.Empty;
}

public static class CekiRevizyonTalepSonucTipleri
{
    public const string OnayBekliyor = "OnayBekliyor";
    public const string Uygulandi = "Uygulandi";
}
