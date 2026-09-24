using _3K.Application.Common;
using _3K.Core.Constants;
using _3K.Core.Enums;

namespace _3K.Application.Features.FinansIslemleri;

public static class FinansYetkiKodlari
{
    public const string Modul = YetkiKodlari.Finans.Modul;
    public const string GelirGoruntule = YetkiKodlari.Finans.GelirGoruntule;
    public const string GiderGoruntule = YetkiKodlari.Finans.GiderGoruntule;
    public const string GiderYonet = YetkiKodlari.Finans.GiderYonet;
    public const string SiparisOperasyonGoruntule = YetkiKodlari.Finans.SiparisOperasyonGoruntule;
    public const string FaturaYonet = YetkiKodlari.Finans.FaturaYonet;
    public const string TarifeYonet = YetkiKodlari.Finans.TarifeYonet;
    public const string RaporGoruntule = YetkiKodlari.Finans.RaporGoruntule;
    public const string ManuelIsEkle = YetkiKodlari.Finans.ManuelIsEkle;
    public const string ManuelIsDuzenle = YetkiKodlari.Finans.ManuelIsDuzenle;
    public const string IsIptal = YetkiKodlari.Finans.IsIptal;
    public const string TarihDegistir = YetkiKodlari.Finans.TarihDegistir;
    public const string PoGir = YetkiKodlari.Finans.PoGir;
    public const string PoDegistir = YetkiKodlari.Finans.PoDegistir;
    public const string BirimFiyatGoruntule = YetkiKodlari.Finans.BirimFiyatGoruntule;
    public const string BirimFiyatDegistir = YetkiKodlari.Finans.BirimFiyatDegistir;
    public const string KarlilikGoruntule = YetkiKodlari.Finans.KarlilikGoruntule;
    public const string ExcelAktar = YetkiKodlari.Finans.ExcelAktar;
    public const string PdfAktar = YetkiKodlari.Finans.PdfAktar;
    public const string GiderEkle = YetkiKodlari.Finans.GiderEkle;
    public const string GiderDuzenle = YetkiKodlari.Finans.GiderDuzenle;
    public const string GiderKutuphanesiYonet = YetkiKodlari.Finans.GiderKutuphanesiYonet;
    public const string IsKutuphanesiYonet = YetkiKodlari.Finans.IsKutuphanesiYonet;
    public const string DuzenliIsYonet = YetkiKodlari.Finans.DuzenliIsYonet;
    public const string ParasalVeriGoruntule = YetkiKodlari.Finans.ParasalVeriGoruntule;
    public const string TutarGoruntule = YetkiKodlari.Finans.TutarGoruntule;
    public const string KayitGoruntule = YetkiKodlari.Finans.KayitGoruntule;
    public const string FaturaGir = YetkiKodlari.Finans.FaturaGir;
    public const string FaturaDegistir = YetkiKodlari.Finans.FaturaDegistir;
    public const string FaturaIptal = YetkiKodlari.Finans.FaturaIptal;
    public const string PoIptal = YetkiKodlari.Finans.PoIptal;
    public const string GiderIptal = YetkiKodlari.Finans.GiderIptal;
    public const string KaliciSil = YetkiKodlari.Finans.KaliciSil;
    public const string BelgeYukle = YetkiKodlari.Finans.BelgeYukle;
    public const string BelgeIndir = YetkiKodlari.Finans.BelgeIndir;
    public const string SablonYonet = YetkiKodlari.Finans.SablonYonet;
    public const string DenetimGor = YetkiKodlari.Finans.DenetimGor;
    public const string FiyatlandirmaDegistir = YetkiKodlari.Finans.FiyatlandirmaDegistir;
    public static MenuPermissionRequirement Read(string kod) => new(kod, YetkiTipi.R);
    public static MenuPermissionRequirement Write(string kod) => new(kod, YetkiTipi.W);
    public const string FinansTarihiDegistir = TarihDegistir;
}
