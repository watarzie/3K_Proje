using _3K.Core.Entities;

namespace _3K.Core.Models;

/// <summary>
/// Hesap bazlı geçici giriş kilidi: art arda <see cref="EsikDenemeSayisi"/> hatalı paroladan sonra hesap
/// <see cref="KilitSuresi"/> boyunca kilitlenir; kilitliyken doğru parola da reddedilir. IP bazlı hız sınırı
/// aynı hesaba farklı IP'lerden yapılan denemeyi durdurmadığı için bu sınır hesabın kendisinde tutulur.
/// </summary>
public static class GirisKilidiKurali
{
    public const int EsikDenemeSayisi = 5;
    public static readonly TimeSpan KilitSuresi = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Hatalı parola, bilinmeyen e-posta ve kilitli hesap için aynı yanıt: hesabın var olup olmadığı ya da
    /// kilitli olduğu dışarıdan ayırt edilemez.
    /// </summary>
    public const string GenelHataMesaji =
        "E-posta veya şifre hatalı. Art arda 5 hatalı denemeden sonra giriş 15 dakika durdurulur.";

    public static bool KilitliMi(DateTime? kilitBitis, DateTime simdi) => kilitBitis > simdi;

    /// <summary>
    /// Hatalı denemeden sonraki sayaç ve kilit. Aktif kilit değişmez (kilitliyken deneme sayılmaz, kilit uzamaz);
    /// süresi dolmuş kilit sayacı baştan başlatır; eşiğe ulaşan deneme kilidi kurar. Veritabanındaki atomik güncelleme
    /// (<c>AuthService</c>) aynı kuralı tek koşullu UPDATE ile uygular.
    /// </summary>
    public static (int Sayac, DateTime? KilitBitis) HataliDenemeSonrasi(int sayac, DateTime? kilitBitis, DateTime simdi)
    {
        if (KilitliMi(kilitBitis, simdi))
            return (sayac, kilitBitis);
        if (kilitBitis != null)
            return (1, null);

        var yeniSayac = sayac + 1;
        return (yeniSayac, yeniSayac >= EsikDenemeSayisi ? simdi + KilitSuresi : null);
    }
}

/// <summary>E-posta/parola doğrulamasının sonucu; kullanıcıya üçü için de aynı yanıt döner.</summary>
public enum GirisDogrulamaDurumu
{
    Basarili = 1,
    Gecersiz = 2,
    Kilitli = 3
}

/// <summary>E-posta/parola doğrulamasının sonucu; <see cref="Kullanici"/> yalnız başarıda doludur.</summary>
public sealed record GirisDogrulamaSonucu(GirisDogrulamaDurumu Durum, Kullanici? Kullanici)
{
    public static readonly GirisDogrulamaSonucu Gecersiz = new(GirisDogrulamaDurumu.Gecersiz, null);
    public static readonly GirisDogrulamaSonucu Kilitli = new(GirisDogrulamaDurumu.Kilitli, null);

    public static GirisDogrulamaSonucu Basarili(Kullanici kullanici) => new(GirisDogrulamaDurumu.Basarili, kullanici);
}
