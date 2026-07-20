namespace _3K.Core.Models;

/// <summary>
/// Revizyon çekisinin ön izlemesi veya uygulanması sırasında bulunan,
/// son kullanıcıya güvenle gösterilebilecek iş kuralı ihlalini tanımlar.
/// </summary>
public sealed class CekiRevizyonSorunu
{
    public string Kod { get; init; } = string.Empty;
    public string Mesaj { get; init; } = string.Empty;
    public string Kategori { get; init; } = CekiRevizyonSorunKategorileri.Dogrulama;
    public int? ExcelSatirNo { get; init; }
    public string? CheckKodu { get; init; }
    public int? SiraNo { get; init; }
    public string? BarkodNo { get; init; }
    public string? PozNo { get; init; }
    public string? Tanim { get; init; }
    public string? SandikNo { get; init; }
}

public static class CekiRevizyonSorunKategorileri
{
    public const string Dogrulama = "Dogrulama";
    public const string DurumCakismasi = "DurumCakismasi";
}

public static class CekiRevizyonSorunKodlari
{
    public const string GecersizDosya = "REVIZYON_GECERSIZ_DOSYA";
    public const string ProjeBilgisiEksik = "REVIZYON_PROJE_BILGISI_EKSIK";
    public const string ProjeBulunamadi = "REVIZYON_PROJE_BULUNAMADI";
    public const string AnaCekiBulunamadi = "REVIZYON_ANA_CEKI_BULUNAMADI";
    public const string IsaretliSatirBulunamadi = "REVIZYON_ISARETLI_SATIR_BULUNAMADI";
    public const string BosSatir = "REVIZYON_BOS_SATIR";
    public const string GecersizMiktar = "REVIZYON_GECERSIZ_MIKTAR";
    public const string SandikNoEksik = "REVIZYON_SANDIK_NO_EKSIK";
    public const string AnaSatirBulunamadi = "REVIZYON_ANA_SATIR_BULUNAMADI";
    public const string SevkKilidi = "REVIZYON_SEVK_KILIDI";
    public const string AktifProjeTransferi = "REVIZYON_AKTIF_PROJE_TRANSFERI";
    public const string AktifSahaAktarimi = "REVIZYON_AKTIF_SAHA_AKTARIMI";
    public const string StokHareketiKullanilmis = "REVIZYON_STOK_HAREKETI_KULLANILMIS";
    public const string StokMiktariYetersiz = "REVIZYON_STOK_MIKTARI_YETERSIZ";
    public const string EszamanliDegisiklik = "REVIZYON_ESZAMANLI_DEGISIKLIK";
    public const string DosyaBoyutuAsildi = "REVIZYON_DOSYA_BOYUTU_ASILDI";
    public const string SatirSiniriAsildi = "REVIZYON_SATIR_SINIRI_ASILDI";
    public const string OnizlemeBoyutuAsildi = "REVIZYON_ONIZLEME_BOYUTU_ASILDI";
    public const string OnayDosyasiDegisti = "REVIZYON_ONAY_DOSYASI_DEGISTI";
    public const string OnayOnizlemesiDegisti = "REVIZYON_ONAY_ONIZLEMESI_DEGISTI";
}
