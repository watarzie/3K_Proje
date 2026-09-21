using _3K.Application.Common;
using _3K.Core.Constants;
using _3K.Core.Enums;

namespace _3K.Application.Features.AmbalajIslemleri;

public static class AmbalajMenuKodlari
{
    public const string Listele = YetkiKodlari.Ambalaj.Listele;
    public const string KayitDuzenle = YetkiKodlari.Ambalaj.KayitDuzenle;
    public const string RaporGoruntule = YetkiKodlari.Ambalaj.RaporGoruntule;
    public const string M3Goruntule = YetkiKodlari.Ambalaj.M3Goruntule;
    public const string SarfGoruntule = YetkiKodlari.Ambalaj.SarfGoruntule;
    public const string KaynakGoruntule = YetkiKodlari.Ambalaj.KaynakGoruntule;
    public const string M3Duzenle = YetkiKodlari.Ambalaj.M3Duzenle;
    public const string SarfDuzenle = YetkiKodlari.Ambalaj.SarfDuzenle;
    public const string KaynakSenkronizeEt = YetkiKodlari.Ambalaj.KaynakSenkronizeEt;
    public const string UretimeAl = YetkiKodlari.Ambalaj.UretimeAl;
    public const string UretimdenCikar = YetkiKodlari.Ambalaj.UretimdenCikar;
    public const string DahilEt = YetkiKodlari.Ambalaj.DahilEt;
    public const string HaricTut = YetkiKodlari.Ambalaj.HaricTut;
    public const string IlaveOlustur = YetkiKodlari.Ambalaj.IlaveOlustur;
    public const string SahaOlustur = YetkiKodlari.Ambalaj.SahaOlustur;
    public const string YedekOlustur = YetkiKodlari.Ambalaj.YedekOlustur;
    public const string IcOlustur = YetkiKodlari.Ambalaj.IcOlustur;
    public const string DigerOlustur = YetkiKodlari.Ambalaj.DigerOlustur;
    public const string ManuelProje = YetkiKodlari.Ambalaj.ManuelProje;
    public const string TurDuzenle = YetkiKodlari.Ambalaj.TurDuzenle;
    public const string CinsDuzenle = YetkiKodlari.Ambalaj.CinsDuzenle;
    public const string ProjeDuzenle = YetkiKodlari.Ambalaj.ProjeDuzenle;
    public const string OlcuDuzenle = YetkiKodlari.Ambalaj.OlcuDuzenle;
    public const string TalepBilgileriDuzenle = YetkiKodlari.Ambalaj.TalepBilgileriDuzenle;
    public const string DurumDuzenle = YetkiKodlari.Ambalaj.DurumDuzenle;
    public const string Iptal = YetkiKodlari.Ambalaj.Iptal;
    public const string GeriYukle = YetkiKodlari.Ambalaj.GeriYukle;
    public const string KaynakMudahalesi = YetkiKodlari.Ambalaj.KaynakMudahalesi;
    public const string FormGoruntule = YetkiKodlari.Ambalaj.FormGoruntule;
    public const string FormIndir = YetkiKodlari.Ambalaj.FormIndir;
    public const string ExcelIndir = YetkiKodlari.Ambalaj.ExcelIndir;
    public const string PdfIndir = YetkiKodlari.Ambalaj.PdfIndir;
    public const string FormOlustur = YetkiKodlari.Ambalaj.FormOlustur;
    public const string FormYenidenOlustur = YetkiKodlari.Ambalaj.FormYenidenOlustur;
    public const string UretimiTamamla = YetkiKodlari.Ambalaj.UretimiTamamla;
    public const string UretimiYenidenAc = YetkiKodlari.Ambalaj.UretimiYenidenAc;
    public const string DurumuGeriAl = YetkiKodlari.Ambalaj.DurumuGeriAl;
    public const string KritikVeriDuzenle = YetkiKodlari.Ambalaj.KritikVeriDuzenle;
    public const string FormSonrasiSecimDegistir = YetkiKodlari.Ambalaj.FormSonrasiSecimDegistir;
    public const string TamamlananProjeyeEkle = YetkiKodlari.Ambalaj.TamamlananProjeyeEkle;
    public const string OlcuGoruntule = YetkiKodlari.Ambalaj.OlcuGoruntule;
    public const string PlanGoruntule = YetkiKodlari.Ambalaj.PlanGoruntule;
    public const string PlanOlustur = YetkiKodlari.Ambalaj.PlanOlustur;
    public const string BekleyenGoruntule = YetkiKodlari.Ambalaj.BekleyenGoruntule;
    public const string UretimdeGoruntule = YetkiKodlari.Ambalaj.UretimdeGoruntule;
    public const string TamamlananGoruntule = YetkiKodlari.Ambalaj.TamamlananGoruntule;
    public const string GerceklesmeDuzelt = YetkiKodlari.Ambalaj.GerceklesmeDuzelt;
    public const string GecmisGoruntule = YetkiKodlari.Ambalaj.GecmisGoruntule;
    public const string ProjeDetayGoruntule = YetkiKodlari.Ambalaj.ProjeDetayGoruntule;
    public const string SandikListesiGoruntule = YetkiKodlari.Ambalaj.SandikListesiGoruntule;
    public const string M3RaporGoruntule = YetkiKodlari.Ambalaj.M3RaporGoruntule;
    public const string KayitEkle = YetkiKodlari.Ambalaj.KayitEkle;
    public const string KayitSil = YetkiKodlari.Ambalaj.KayitSil;
    public static MenuPermissionRequirement Read(string kod) => new(kod, YetkiTipi.R);
    public static MenuPermissionRequirement Write(string kod) => new(kod, YetkiTipi.W);

    public const string Duzenle = KayitDuzenle;
    public const string Rapor = RaporGoruntule;
    public static string? TurOlusturmaKodu(AmbalajSandikTuru tur) => tur switch
    {
        AmbalajSandikTuru.Ilave => IlaveOlustur,
        AmbalajSandikTuru.Saha => SahaOlustur,
        AmbalajSandikTuru.Yedek => YedekOlustur,
        AmbalajSandikTuru.Ic => IcOlustur,
        AmbalajSandikTuru.Diger => DigerOlustur,
        _ => null
    };
}
