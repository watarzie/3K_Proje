namespace _3K.YuklemeMotoru.Ekipman;

/// <summary>
/// Denetimin ekipman kategorisine bağlı beklentileri. Kategori kuralı (hangi açıklık gerekir, iç yükseklik gerekir mi,
/// ekipmanın dingili var mı) motorun dışında tanımlıdır; çağıran verir.
/// </summary>
public sealed record EkipmanDenetimBaglami(
    IReadOnlyCollection<AciklikTarafi> GerekenAcikliklar,
    bool IcYukseklikGerekli,
    bool DingilliMi);

/// <summary>
/// Hatalar veriyi geçersiz kılar (kaydedilmez). Uyarılar eksik veridir: kaydedilir ama o veriye dayanan kontrol
/// "Teyit edilmedi" olur.
/// </summary>
public sealed record EkipmanDenetimSonucu(IReadOnlyList<string> Hatalar, IReadOnlyList<string> Uyarilar)
{
    public bool GecerliMi => Hatalar.Count == 0;
}

/// <summary>Ekipman teknik verisinin yapısal denetimi; değer tahmin etmez, eksik veriyi uyarı olarak bildirir.</summary>
public static class EkipmanTeknikVeriDenetimi
{
    public const int EnFazlaUzunlukMm = 60_000;
    public const int EnFazlaGenislikMm = 10_000;
    public const int EnFazlaYukseklikMm = 10_000;
    public const int EnFazlaAgirlikKg = 2_000_000;

    public static EkipmanDenetimSonucu Denetle(EkipmanTeknikVerisi veri, EkipmanDenetimBaglami baglam)
    {
        var hatalar = new List<string>();
        var uyarilar = new List<string>();

        if (veri.SemaSurumu != EkipmanTeknikVerisi.GuncelSemaSurumu)
            hatalar.Add($"Teknik veri şema sürümü {veri.SemaSurumu} desteklenmiyor.");

        DisOlcuyuDenetle(veri, hatalar, uyarilar);
        AgirliklariDenetle(veri, baglam, hatalar, uyarilar);
        YuzeyleriDenetle(veri, baglam, hatalar, uyarilar);
        AcikliklariDenetle(veri, baglam, hatalar, uyarilar);
        EngelleriDenetle(veri, hatalar);
        DingilleriDenetle(veri, baglam, hatalar, uyarilar);
        BaglamaNoktalariniDenetle(veri, hatalar);

        return new EkipmanDenetimSonucu(hatalar, uyarilar);
    }

    private static void DisOlcuyuDenetle(EkipmanTeknikVerisi veri, List<string> hatalar, List<string> uyarilar)
    {
        if (veri.DisOlcu is not { } dis)
        {
            uyarilar.Add("Dış ölçü tanımlı değil; toplam yüklü ölçü teyit edilemez.");
            return;
        }

        if (!Aralikta(dis.UzunlukMm, EnFazlaUzunlukMm) || !Aralikta(dis.GenislikMm, EnFazlaGenislikMm) ||
            !Aralikta(dis.YukseklikMm, EnFazlaYukseklikMm))
            hatalar.Add("Dış ölçüler sıfırdan büyük ve gerçekçi olmalıdır.");

        if (veri.UzamaEnFazlaMm is { } uzama && !Aralikta(uzama, EnFazlaUzunlukMm))
            hatalar.Add("Uzama sıfırdan büyük ve gerçekçi olmalıdır.");
    }

    private static void AgirliklariDenetle(
        EkipmanTeknikVerisi veri, EkipmanDenetimBaglami baglam, List<string> hatalar, List<string> uyarilar)
    {
        foreach (var (deger, ad) in new[] { (veri.DaraKg, "Boş ağırlık"), (veri.TeknikKapasiteKg, "Teknik kapasite"), (veri.AzamiBrutKg, "Azami brüt ağırlık") })
        {
            if (deger is { } kg && !Aralikta(kg, EnFazlaAgirlikKg))
                hatalar.Add($"{ad} sıfırdan büyük ve gerçekçi olmalıdır.");
        }

        if (veri is { DaraKg: { } dara, AzamiBrutKg: { } brut } && dara >= brut)
            hatalar.Add("Boş ağırlık azami brüt ağırlıktan küçük olmalıdır.");

        if (veri.TeknikKapasiteKg is null)
            uyarilar.Add("Teknik kapasite tanımlı değil; ağırlık kontrolü teyit edilemez.");
        if (baglam.DingilliMi && veri.DaraKg is null)
            uyarilar.Add("Boş ağırlık tanımlı değil; dingil ve toplam ağırlık kontrolü teyit edilemez.");
    }

    private static void YuzeyleriDenetle(
        EkipmanTeknikVerisi veri, EkipmanDenetimBaglami baglam, List<string> hatalar, List<string> uyarilar)
    {
        if (veri.Yuzeyler.Count == 0)
        {
            hatalar.Add("En az bir yükleme yüzeyi tanımlanmalıdır.");
            return;
        }

        var kodlar = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var yuzey in veri.Yuzeyler)
        {
            var ad = string.IsNullOrWhiteSpace(yuzey.Kod) ? "(kodsuz)" : yuzey.Kod;
            if (string.IsNullOrWhiteSpace(yuzey.Kod))
                hatalar.Add("Yükleme yüzeyinin kodu boş olamaz.");
            else if (!kodlar.Add(yuzey.Kod.Trim()))
                hatalar.Add($"Yükleme yüzeyi kodu '{yuzey.Kod}' birden fazla kez kullanılmış.");

            if (!KonumX(yuzey.BaslangicXMm) || yuzey.YerdenYukseklikMm < 0 ||
                !Aralikta(yuzey.UzunlukMm, EnFazlaUzunlukMm) || !Aralikta(yuzey.GenislikMm, EnFazlaGenislikMm) ||
                yuzey.YerdenYukseklikMm > EnFazlaYukseklikMm)
                hatalar.Add($"Yüzey '{ad}': başlangıç ve yükseklik negatif olamaz, uzunluk ve genişlik sıfırdan büyük olmalıdır.");

            if (yuzey.KullanilabilirYukseklikMm is { } ic && !Aralikta(ic, EnFazlaYukseklikMm))
                hatalar.Add($"Yüzey '{ad}': kullanılabilir yükseklik sıfırdan büyük olmalıdır.");
            else if (yuzey.KullanilabilirYukseklikMm is null && baglam.IcYukseklikGerekli)
                uyarilar.Add($"Yüzey '{ad}': kullanılabilir (iç) yükseklik tanımlı değil; yükseklik kontrolü teyit edilemez.");

            if (yuzey.TabanYukuKgM2 is { } taban && !Aralikta(taban, EnFazlaAgirlikKg))
                hatalar.Add($"Yüzey '{ad}': taban yükü sınırı sıfırdan büyük olmalıdır.");
            if (yuzey.NoktasalYukKg is { } noktasal && !Aralikta(noktasal, EnFazlaAgirlikKg))
                hatalar.Add($"Yüzey '{ad}': noktasal yük sınırı sıfırdan büyük olmalıdır.");

            if (veri.DisOlcu is { } dis &&
                ((long)yuzey.BaslangicXMm + yuzey.UzunlukMm > dis.UzunlukMm || yuzey.GenislikMm > dis.GenislikMm))
                hatalar.Add($"Yüzey '{ad}' dış ölçülerin dışına taşıyor.");
        }

        var sirali = veri.Yuzeyler.OrderBy(y => y.BaslangicXMm).ToList();
        for (var i = 1; i < sirali.Count; i++)
        {
            if (sirali[i].BaslangicXMm < (long)sirali[i - 1].BaslangicXMm + sirali[i - 1].UzunlukMm)
                hatalar.Add($"Yüzey '{sirali[i - 1].Kod}' ile '{sirali[i].Kod}' boyuna çakışıyor.");
        }
    }

    private static void AcikliklariDenetle(
        EkipmanTeknikVerisi veri, EkipmanDenetimBaglami baglam, List<string> hatalar, List<string> uyarilar)
    {
        foreach (var aciklik in veri.Acikliklar)
        {
            var gecerli = aciklik.Taraf switch
            {
                AciklikTarafi.Arka or AciklikTarafi.On =>
                    Pozitif(aciklik.GenislikMm, EnFazlaGenislikMm) && Pozitif(aciklik.YukseklikMm, EnFazlaYukseklikMm),
                AciklikTarafi.Sol or AciklikTarafi.Sag =>
                    aciklik.BaslangicXMm is { } solX && KonumX(solX) && Pozitif(aciklik.UzunlukMm, EnFazlaUzunlukMm) &&
                    Pozitif(aciklik.YukseklikMm, EnFazlaYukseklikMm),
                AciklikTarafi.Ust =>
                    aciklik.BaslangicXMm is { } ustX && KonumX(ustX) && Pozitif(aciklik.UzunlukMm, EnFazlaUzunlukMm) &&
                    Pozitif(aciklik.GenislikMm, EnFazlaGenislikMm),
                _ => false
            };
            if (!gecerli)
                hatalar.Add($"{TarafAdi(aciklik.Taraf)} açıklık eksik veya geçersiz: {GerekenOlculer(aciklik.Taraf)} sıfırdan büyük girilmelidir.");
        }

        foreach (var taraf in baglam.GerekenAcikliklar.Where(t => veri.Acikliklar.All(a => a.Taraf != t)))
            uyarilar.Add($"{TarafAdi(taraf)} açıklık tanımlı değil; bu yönden yükleme ve geçiş teyit edilemez.");
    }

    private static void EngelleriDenetle(EkipmanTeknikVerisi veri, List<string> hatalar)
    {
        foreach (var engel in veri.Engeller)
        {
            if (!KonumX(engel.XMm) || !KonumY(engel.YMm) || !KonumZ(engel.ZMm) ||
                !Aralikta(engel.UzunlukMm, EnFazlaUzunlukMm) || !Aralikta(engel.GenislikMm, EnFazlaGenislikMm) ||
                !Aralikta(engel.YukseklikMm, EnFazlaYukseklikMm))
                hatalar.Add($"Engel ({EngelAdi(engel.Tur)}, X={engel.XMm} mm): konum negatif olamaz ve ekipman sınırını aşamaz, ölçüler sıfırdan büyük olmalıdır.");
        }
    }

    private static void DingilleriDenetle(
        EkipmanTeknikVerisi veri, EkipmanDenetimBaglami baglam, List<string> hatalar, List<string> uyarilar)
    {
        foreach (var dingil in veri.Dingiller)
        {
            if (!KonumX(dingil.XMm) || dingil.GrupNo < 1 || dingil.KapasiteKg is { } kg && !Aralikta(kg, EnFazlaAgirlikKg))
                hatalar.Add($"Dingil (X={dingil.XMm} mm): konum negatif olamaz ve ekipman sınırını aşamaz, grup 1'den başlar, kapasite sıfırdan büyük olmalıdır.");
        }

        if (veri.KingPin is { } kingPin &&
            (!KonumX(kingPin.XMm) || kingPin.AzamiBaglantiYukuKg is { } yuk && !Aralikta(yuk, EnFazlaAgirlikKg)))
            hatalar.Add("King-pin konumu negatif olamaz, bağlantı yükü sıfırdan büyük olmalıdır.");

        if (baglam.DingilliMi && veri.Dingiller.Count == 0)
            uyarilar.Add("Dingil verisi yok; dingil yükü kontrolü teyit edilemez.");
    }

    private static void BaglamaNoktalariniDenetle(EkipmanTeknikVerisi veri, List<string> hatalar)
    {
        foreach (var nokta in veri.BaglamaNoktalari)
        {
            if (!KonumX(nokta.XMm) || !KonumY(nokta.YMm) || !KonumZ(nokta.ZMm) || nokta.KapasiteDaN is { } kapasite && kapasite <= 0)
                hatalar.Add($"Bağlama noktası (X={nokta.XMm} mm): konum negatif olamaz ve ekipman sınırını aşamaz, kapasite sıfırdan büyük olmalıdır.");
        }
    }

    private static bool Aralikta(int deger, int enFazla) => deger > 0 && deger <= enFazla;

    // Koordinatlar ekipman sınırının içinde kalmalı: üst sınırsız değer toplamada int taşmasına yol açıp dış ölçü ve
    // çakışma kontrollerini sessizce geçirebilirdi.
    private static bool KonumX(int x) => x >= 0 && x <= EnFazlaUzunlukMm;
    private static bool KonumY(int y) => y >= 0 && y <= EnFazlaGenislikMm;
    private static bool KonumZ(int z) => z >= 0 && z <= EnFazlaYukseklikMm;

    private static bool Pozitif(int? deger, int enFazla) => deger is { } d && Aralikta(d, enFazla);

    private static string TarafAdi(AciklikTarafi taraf) => taraf switch
    {
        AciklikTarafi.Arka => "Arka",
        AciklikTarafi.On => "Ön",
        AciklikTarafi.Sol => "Sol yan",
        AciklikTarafi.Sag => "Sağ yan",
        _ => "Üst"
    };

    private static string EngelAdi(EngelTuru tur) => tur switch
    {
        EngelTuru.Dikme => "dikme",
        EngelTuru.Travers => "travers",
        EngelTuru.TekerHavuzu => "teker havuzu",
        EngelTuru.UcDuvar => "uç duvar",
        EngelTuru.Kapak => "kapak",
        _ => "diğer"
    };

    private static string GerekenOlculer(AciklikTarafi taraf) => taraf switch
    {
        AciklikTarafi.Arka or AciklikTarafi.On => "genişlik ve yükseklik",
        AciklikTarafi.Sol or AciklikTarafi.Sag => "başlangıç, uzunluk ve yükseklik",
        _ => "başlangıç, uzunluk ve genişlik"
    };
}
