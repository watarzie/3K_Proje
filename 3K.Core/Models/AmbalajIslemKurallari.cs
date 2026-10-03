using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;

namespace _3K.Core.Models;

/// <summary>
/// Bir ambalaj kaydında yapılmak istenen değişiklik. Kayıt değişiklik öncesi hâlidir;
/// yalnız istenen alanlar doldurulur.
/// </summary>
public sealed record AmbalajKayitDegisikligi(AmbalajUretimKaydi Kayit, bool FormuVar, bool FinansaBagli)
{
    public AmbalajUretimDurumu? HedefDurum { get; init; }
    /// <summary>Plan kaydında kaynaklı kaydın yeni üretim seçimi.</summary>
    public bool? HedefUretimSecimi { get; init; }
    /// <summary>Ambalaj kararında kaydın yeni dahil/haric durumu.</summary>
    public bool? HedefAmbalajaDahil { get; init; }
    /// <summary>Kaydın projesi tamamlanmış mı; yalnız haric kaydı dahil etmede kullanılır.</summary>
    public bool ProjeTamamlandi { get; init; }
    public bool PartiDegisiyor { get; init; }
    public bool VeriDegisiyor { get; init; }
    public bool Siliniyor { get; init; }
}

/// <summary>Değişikliklerin gerektirdiği ek yazma izinleri ve yaşam döngüsü kapıları.</summary>
public sealed class AmbalajIslemGereksinimi
{
    private readonly List<string> _yetkiler = [];

    /// <summary>Eklenme sırasıyla; ilk eksik izin hata mesajında gösterilir.</summary>
    public IReadOnlyList<string> Yetkiler => _yetkiler;
    /// <summary>Kritik işlem gerekçe ister ve kayıt geçmişine yazılır.</summary>
    public bool Kritik { get; private set; }
    /// <summary>Kayıt ilk üretim formu oluşturulmadan ilerletilemez.</summary>
    public bool IlkFormGerekli { get; private set; }
    /// <summary>İzin veya gerekçeyle aşılamayan iş kuralı engeli (409).</summary>
    public string? Engel { get; private set; }

    internal void YetkiEkle(string kod, bool kritik = false)
    {
        if (!_yetkiler.Contains(kod))
            _yetkiler.Add(kod);
        Kritik |= kritik;
    }

    internal void IlkFormIste() => IlkFormGerekli = true;

    internal void Engelle(string mesaj) => Engel ??= mesaj;
}

/// <summary>
/// Ambalaj üretim kaydındaki durum, seçim, karar, veri ve silme değişikliklerinin
/// hangi ek izinleri gerektirdiğini ve hangilerinin kritik olduğunu belirler.
/// </summary>
public static class AmbalajIslemKurallari
{
    public static void Degerlendir(AmbalajKayitDegisikligi degisiklik, AmbalajIslemGereksinimi gereksinim)
    {
        var kayit = degisiklik.Kayit;
        if (degisiklik.HedefDurum is { } hedef && hedef != kayit.UretimDurumu)
        {
            if (hedef != AmbalajUretimDurumu.Planlandi && !degisiklik.FormuVar)
                gereksinim.IlkFormIste();
            gereksinim.YetkiEkle(YetkiKodlari.Ambalaj.DurumDuzenle);
            if (hedef == AmbalajUretimDurumu.Tamamlandi)
                gereksinim.YetkiEkle(YetkiKodlari.Ambalaj.UretimiTamamla);
            if (hedef < kayit.UretimDurumu)
                gereksinim.YetkiEkle(YetkiKodlari.Ambalaj.DurumuGeriAl, kritik: true);
            if (kayit.UretimDurumu == AmbalajUretimDurumu.Tamamlandi)
            {
                gereksinim.YetkiEkle(YetkiKodlari.Ambalaj.UretimiYenidenAc, kritik: true);
                // A3/§3.3: finans kalemi PO'ya (ve dolayısıyla faturaya) bağlanmış üretim geri alınamaz.
                if (degisiklik.FinansaBagli)
                    gereksinim.Engelle(
                        $"{kayit.SandikNo} numaralı sandığın finans kalemi PO veya faturaya bağlı olduğu için tamamlanan üretim geri alınamaz.");
            }
        }

        var uretimSecimiDegisiyor = degisiklik.HedefUretimSecimi is { } secim &&
            kayit.KaynakKayitId.HasValue && kayit.UretimeAlindi != secim;
        if (uretimSecimiDegisiyor && degisiklik.FormuVar)
        {
            gereksinim.YetkiEkle(YetkiKodlari.Ambalaj.FormSonrasiSecimDegistir, kritik: true);
            gereksinim.YetkiEkle(YetkiKodlari.Ambalaj.KritikVeriDuzenle, kritik: true);
        }

        var kararDegisiyor = degisiklik.HedefAmbalajaDahil is { } dahil && dahil != kayit.AmbalajaDahil;
        if (kararDegisiyor)
        {
            gereksinim.YetkiEkle(kayit.AmbalajaDahil ? YetkiKodlari.Ambalaj.HaricTut : YetkiKodlari.Ambalaj.DahilEt);
            if (degisiklik.FormuVar)
                gereksinim.YetkiEkle(YetkiKodlari.Ambalaj.FormSonrasiSecimDegistir, kritik: true);
            if (!kayit.AmbalajaDahil && kayit.ProjeId.HasValue && degisiklik.ProjeTamamlandi)
                gereksinim.YetkiEkle(YetkiKodlari.Ambalaj.TamamlananProjeyeEkle, kritik: true);
        }

        // Form, tamamlanma veya aktif sipariş bağı olan kaydın verisi değişiyorsa kritik düzeltmedir.
        var veriDegisiyor = degisiklik.VeriDegisiyor || kararDegisiyor || degisiklik.PartiDegisiyor;
        if (veriDegisiyor && (degisiklik.FormuVar || kayit.UretimDurumu == AmbalajUretimDurumu.Tamamlandi || degisiklik.FinansaBagli))
            gereksinim.YetkiEkle(YetkiKodlari.Ambalaj.KritikVeriDuzenle, kritik: true);

        if (degisiklik.Siliniyor)
            gereksinim.YetkiEkle(YetkiKodlari.Ambalaj.Iptal, kritik: true);
    }

    /// <summary>Tamamlanmış projeye yeni kayıt eklemek kritik işlemdir.</summary>
    public static void TamamlanmisProjeyeEkleme(AmbalajIslemGereksinimi gereksinim) =>
        gereksinim.YetkiEkle(YetkiKodlari.Ambalaj.TamamlananProjeyeEkle, kritik: true);
}
