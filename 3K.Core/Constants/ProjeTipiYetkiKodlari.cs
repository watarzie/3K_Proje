using _3K.Core.Enums;

namespace _3K.Core.Constants;

/// <summary>
/// Aynı işlemin Normal/Saha/Yedek projelerde hangi menü koduyla yetkilendirildiğini tek yerde tutar.
/// Proje tipi daima veritabanındaki gerçek kayıttan okunur; istemcinin bildirdiği tip kullanılmaz.
/// </summary>
public sealed record ProjeTipiYetkiKodlari(
    ProjeTipi Tip,
    string ProjeYonetimi,
    string Sandiklar,
    string GridModulu,
    string UcKModulu,
    string SevkEt,
    string ProjeSil,
    string PlanlananSevkTarihi,
    string GerceklesenCekiRaporu,
    string UcKSandikDurumRaporu,
    string EksikRaporu,
    string? SandikRaporu)
{
    public static readonly ProjeTipiYetkiKodlari Normal = new(
        ProjeTipi.Normal,
        ProjeYonetimi: YetkiKodlari.Sandik.Yonetimi,
        Sandiklar: YetkiKodlari.Sandik.Yonetimi,
        GridModulu: YetkiKodlari.Sandik.GridModulu,
        UcKModulu: YetkiKodlari.Sandik.UcKModulu,
        SevkEt: YetkiKodlari.Proje.SevkEt,
        ProjeSil: YetkiKodlari.Proje.Sil,
        PlanlananSevkTarihi: YetkiKodlari.Proje.PlanlananSevkTarihi,
        GerceklesenCekiRaporu: YetkiKodlari.Sandik.GerceklesenCekiRaporu,
        UcKSandikDurumRaporu: YetkiKodlari.Sandik.UcKSandikDurumRaporu,
        EksikRaporu: YetkiKodlari.Sandik.EksikRaporu,
        // Normal projede sandık raporu ayrı bir koda bağlı değildir (sandık yönetimi veya aktif projeler).
        SandikRaporu: null);

    public static readonly ProjeTipiYetkiKodlari Saha = new(
        ProjeTipi.Saha,
        ProjeYonetimi: YetkiKodlari.Saha.Yonetimi,
        Sandiklar: YetkiKodlari.Saha.Sandiklar,
        GridModulu: YetkiKodlari.Saha.GridModulu,
        UcKModulu: YetkiKodlari.Saha.UcKModulu,
        SevkEt: YetkiKodlari.Saha.SevkEt,
        ProjeSil: YetkiKodlari.Saha.ProjeSil,
        // Saha projeleri planlanan sevk tarihini normal projelerle aynı izinle değiştirir.
        PlanlananSevkTarihi: YetkiKodlari.Proje.PlanlananSevkTarihi,
        GerceklesenCekiRaporu: YetkiKodlari.Saha.GerceklesenCekiRaporu,
        UcKSandikDurumRaporu: YetkiKodlari.Saha.UcKSandikDurumRaporu,
        EksikRaporu: YetkiKodlari.Saha.SevkSonrasiEksikRaporu,
        SandikRaporu: YetkiKodlari.Saha.Raporu);

    public static readonly ProjeTipiYetkiKodlari Yedek = new(
        ProjeTipi.Yedek,
        ProjeYonetimi: YetkiKodlari.Yedek.Yonetimi,
        Sandiklar: YetkiKodlari.Yedek.Sandiklar,
        GridModulu: YetkiKodlari.Yedek.GridModulu,
        UcKModulu: YetkiKodlari.Yedek.UcKModulu,
        SevkEt: YetkiKodlari.Yedek.SevkEt,
        ProjeSil: YetkiKodlari.Yedek.ProjeSil,
        PlanlananSevkTarihi: YetkiKodlari.Yedek.PlanlananSevkTarihi,
        GerceklesenCekiRaporu: YetkiKodlari.Yedek.GerceklesenCekiRaporu,
        UcKSandikDurumRaporu: YetkiKodlari.Yedek.UcKSandikDurumRaporu,
        EksikRaporu: YetkiKodlari.Yedek.EksikRaporu,
        SandikRaporu: YetkiKodlari.Yedek.Raporu);

    /// <summary>Tanımsız proje tipi için null döner; çağıran taraf erişimi reddetmelidir.</summary>
    public static ProjeTipiYetkiKodlari? Getir(int projeTipiId) => projeTipiId switch
    {
        (int)ProjeTipi.Normal => Normal,
        (int)ProjeTipi.Saha => Saha,
        (int)ProjeTipi.Yedek => Yedek,
        _ => null
    };
}
