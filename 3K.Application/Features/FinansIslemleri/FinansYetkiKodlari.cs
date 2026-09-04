using _3K.Application.Common;
using _3K.Core.Enums;

namespace _3K.Application.Features.FinansIslemleri;

/// <summary>
/// Finans modülünün rol yönetiminde kullanılan ekran/işlem kodları.
/// Kodlar request sınıflarında sabit string olarak dağılmasın diye tek yerde tutulur.
/// </summary>
public static class FinansYetkiKodlari
{
    public const string Modul = "finans-yonetimi";
    public const string GelirGoruntule = Modul;
    public const string GiderGoruntule = Modul;
    public const string GiderYonet = Modul;
    public const string SiparisOperasyonGoruntule = Modul;
    public const string FaturaYonet = Modul;
    public const string TarifeYonet = Modul;
    public const string RaporGoruntule = Modul;
    public const string ManuelIsEkle = GelirGoruntule;
    public const string ManuelIsDuzenle = GelirGoruntule;
    public const string IsIptal = GelirGoruntule;
    public const string TarihDegistir = GelirGoruntule;
    public const string PoGir = SiparisOperasyonGoruntule;
    public const string PoDegistir = SiparisOperasyonGoruntule;
    public const string BirimFiyatGoruntule = GelirGoruntule;
    public const string BirimFiyatDegistir = TarifeYonet;
    public const string KarlilikGoruntule = GelirGoruntule;
    public const string ExcelAktar = RaporGoruntule;
    public const string PdfAktar = RaporGoruntule;
    public const string GiderEkle = GiderYonet;
    public const string GiderDuzenle = GiderYonet;
    public const string GiderKutuphanesiYonet = GiderYonet;
    public const string IsKutuphanesiYonet = TarifeYonet;
    public const string DuzenliIsYonet = TarifeYonet;

    public static MenuPermissionRequirement Read(string code) => new(code, YetkiTipi.R);
    public static MenuPermissionRequirement Write(string code) => new(code, YetkiTipi.W);
}
