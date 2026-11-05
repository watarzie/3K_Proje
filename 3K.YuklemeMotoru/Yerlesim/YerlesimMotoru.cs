namespace _3K.YuklemeMotoru.Yerlesim;

/// <summary>
/// Otomatik yerleşim (Simülasyon isterleri 2, 8–10, 14–17, 24–25). Hedef bütün zorunlu koşullar sağlanarak en az araç
/// kullanmaktır. Normal yükler yalnız seçilen ailenin uygun alt tiplerine yerleşir; aile dışı ekipman yalnız zorunlu özel
/// yük için (trafonun kendisi her zaman, diğer özel yük ailede teknik olarak taşınamıyorsa) gerekçesiyle eklenir. Proje
/// bazında ayrı ve lokasyona göre ayrı tercihlerinde gruplar aynı araca girmez. Hiçbir yük sessizce düşmez: araca
/// girmeyen her yük nedeniyle "yerleşemeyen" listesindedir.
/// </summary>
public static class YerlesimMotoru
{
    public static YerlesimSonucu Hesapla(YerlesimGirdisi girdi, CancellationToken cancellationToken = default)
    {
        var baglam = new Baglam(girdi, cancellationToken);
        var aileAdaylari = girdi.Adaylar.Where(a => a.AileIci && a.TeknikVeri.Yuzeyler.Count > 0).ToList();
        var ozelAdaylar = girdi.Adaylar.Where(a => a.OzelYukEkipmani && a.TeknikVeri.Yuzeyler.Count > 0).ToList();
        var havuz = MevcutHavuz(girdi);
        var aileAraclari = new List<(AracYerlesimi Arac, string? Gerekce)>();
        var ozelAraclar = new List<(AracYerlesimi Arac, string? Gerekce)>();
        var yerlesemeyenler = new List<YerlesemeyenYuk>();
        var disaridaKalanlar = new List<YerlesimYuku>();

        var gruplar = girdi.Yukler
            .GroupBy(y => (Proje: girdi.ProjeBazindaAyri ? y.ProjeId : null, Lokasyon: girdi.LokasyonaGoreAyri ? y.LokasyonId : null))
            .OrderBy(g => g.Key.Proje ?? int.MaxValue).ThenBy(g => g.Key.Lokasyon ?? int.MaxValue);
        foreach (var grup in gruplar)
        {
            var aileYukleri = new List<YerlesimYuku>();
            var ozelYukler = new List<(YerlesimYuku Yuk, string? AiledeNeden)>();
            foreach (var yuk in Sirala(grup))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (yuk.Zorunluluk == OzelEkipmanZorunlulugu.Her)
                {
                    ozelYukler.Add((yuk, null));
                    continue;
                }
                if (aileAdaylari.Count == 0)
                {
                    // Aile dışına çıkış veri eksikliğiyle değil, yalnız teknik zorunlulukla yapılır (isterler 10.2).
                    yerlesemeyenler.Add(new(yuk.Anahtar, yuk.Kod, $"{girdi.AileAdi} ailesinde sandık taşımaya uygun ekipman verisi yok"));
                    continue;
                }
                var nedenler = aileAdaylari.Select(a => baglam.Neden(a, yuk)).ToList();
                if (nedenler.Any(n => n == null))
                    aileYukleri.Add(yuk);
                else if (yuk.OzelYuk)
                    ozelYukler.Add((yuk, Ozetle(nedenler)));
                else
                    yerlesemeyenler.Add(new(yuk.Anahtar, yuk.Kod, $"{girdi.AileAdi} ailesindeki hiçbir ekipmana sığmıyor: {Ozetle(nedenler)}"));
            }

            // Zorunlu özel yük önce: kullanıcının mevcut araç listesindeki lowbed sandıklara harcanmasın.
            foreach (var (yuk, ailedeNeden) in ozelYukler)
            {
                if (havuz != null && HavuzdanAl(havuz, girdi, yuk, ozelEkipman: true) is { } mevcut)
                {
                    ozelAraclar.Add((mevcut, mevcut.Aday.AileIci ? null : $"Zorunlu özel ekipman: {yuk.Kod} için mevcut araç listesindeki {mevcut.Aday.Kod} kullanıldı."));
                    continue;
                }
                var (arac, neden) = OzelYerlestir(yuk, ozelAdaylar, girdi);
                if (arac == null)
                {
                    yerlesemeyenler.Add(new(yuk.Anahtar, yuk.Kod, neden!));
                    continue;
                }
                ozelAraclar.Add((arac, arac.Aday.AileIci ? null : Gerekce(yuk, arac.Aday, girdi.AileAdi, ailedeNeden)));
            }

            if (havuz != null)
            {
                var acilanlar = new List<AracYerlesimi>();
                foreach (var yuk in aileYukleri)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (acilanlar.Any(a => a.Ekle(yuk)))
                        continue;
                    var yeni = HavuzdanAl(havuz, girdi, yuk, ozelEkipman: false);
                    if (yeni != null)
                        acilanlar.Add(yeni);
                    else
                        disaridaKalanlar.Add(yuk);
                }
                aileAraclari.AddRange(acilanlar.Select(a => (a, a.Aday.AileIci ? null : (string?)$"Mevcut araç listesinden {a.Aday.Kod} kullanıldı.")));
            }
            else
            {
                var (paket, sigmayanlar) = IhtiyacaGore(aileYukleri, aileAdaylari, baglam);
                aileAraclari.AddRange(paket.Select(a => (a, (string?)null)));
                yerlesemeyenler.AddRange(sigmayanlar.Select(y => new YerlesemeyenYuk(y.Anahtar, y.Kod, "Seçilen alt tiplere birlikte yerleştirilemedi")));
            }
        }

        var ekIhtiyaclar = new List<EkEkipmanIhtiyaci>();
        if (disaridaKalanlar.Count > 0)
        {
            foreach (var yuk in disaridaKalanlar)
                yerlesemeyenler.Add(new(yuk.Anahtar, yuk.Kod, "Mevcut araçlara sığmadı"));
            ekIhtiyaclar = IhtiyacaGore(disaridaKalanlar, aileAdaylari, baglam).Araclar
                .GroupBy(a => a.Aday)
                .Select(g => new EkEkipmanIhtiyaci(g.Key.KonfigurasyonId, g.Key.Kod, g.Key.Ad, g.Count()))
                .ToList();
        }

        // Aile araçları önce, zorunlu özel ekipman sonra numaralanır.
        var araclar = aileAraclari.Concat(ozelAraclar).Where(a => !a.Arac.Bos).ToList();
        var sonuclar = araclar.Select((a, i) => AracKontrolcu.Olustur(i + 1, a.Arac, girdi.Ayarlar, girdi.Yasal, a.Gerekce)).ToList();
        var uyarilar = Uyarilar(girdi, araclar.Select(a => a.Arac).ToList(), sonuclar, havuz);
        // Genel sonuç araçların en kötüsüdür; yerleşemeyen yük varsa plan eksiktir ve uygunluk üretilmez.
        var genel = yerlesemeyenler.Count > 0 ? KontrolSonucu.Uygunsuz : AracKontrolcu.EnKotu(sonuclar.Select(a => a.Sonuc));
        if (sonuclar.Count == 0 && genel == KontrolSonucu.Uygun)
            genel = KontrolSonucu.TeyitEdilmedi;
        if (yerlesemeyenler.Count > 0)
            uyarilar.Insert(0, $"{yerlesemeyenler.Count} yük yerleşemedi; plan eksiktir.");
        return new YerlesimSonucu
        {
            Araclar = sonuclar,
            Yerlesemeyenler = yerlesemeyenler,
            EkIhtiyaclar = ekIhtiyaclar,
            Uyarilar = uyarilar,
            Sonuc = genel
        };
    }

    /// <summary>Hesap boyunca ortak durum: boş araçta sığma sonucu (aday, yük) başına bir kez hesaplanır; iptal izlenir.</summary>
    private sealed class Baglam(YerlesimGirdisi girdi, CancellationToken iptal)
    {
        private readonly Dictionary<(int, string), string?> nedenler = [];

        public YerlesimGirdisi Girdi { get; } = girdi;
        public CancellationToken Iptal { get; } = iptal;

        public string? Neden(EkipmanAdayi aday, YerlesimYuku yuk)
        {
            var anahtar = (aday.KonfigurasyonId, yuk.Anahtar);
            if (!nedenler.TryGetValue(anahtar, out var neden))
                nedenler[anahtar] = neden = AracYerlesimi.SigmamaNedeni(aday, Girdi.Ayarlar, Girdi.Yasal, yuk);
            return neden;
        }
    }

    /// <summary>Taban alanı büyükten küçüğe, sonra ağırdan hafife: ağır ve geniş yük alta, hafif yük üste gelir.</summary>
    private static IEnumerable<YerlesimYuku> Sirala(IEnumerable<YerlesimYuku> yukler) =>
        yukler.OrderByDescending(y => (long)y.BoyMm * y.EnMm).ThenByDescending(y => y.BrutKg).ThenBy(y => y.Kod, StringComparer.Ordinal);

    private static string Ozetle(IEnumerable<string?> nedenler) =>
        string.Join("; ", nedenler.Where(n => n != null).Distinct().Take(3));

    /// <summary>
    /// Verisi eksik adayın (kapasite, kapı açıklığı, doğrulama) seçimde geri planda kalması için eksik sayısı; bilinmeyen
    /// veri "sınırsız" sayıldığı için aynı kapsamda verisi tam aday öne alınır.
    /// </summary>
    private static int EksikVeri(EkipmanAdayi a) =>
        (a.TeknikVeri.TeknikKapasiteKg == null ? 1 : 0) +
        (a.Giris.HasFlag(GirisYonu.Arkadan) && !a.TeknikVeri.Acikliklar.Any(x => x.Taraf == Ekipman.AciklikTarafi.Arka && x.GenislikMm != null && x.YukseklikMm != null) ? 1 : 0) +
        (a.Dogrulandi ? 0 : 1);

    /// <summary>
    /// İhtiyaca göre araç: her adım kalan yüklerin en çoğunu en az araçla alan alt tip seçilir (eşitlikte verisi tam olan,
    /// sonra tabanı küçük olan); son (en boş) araç, bütün yüklerini alabilen daha küçük tabanlı bir alt tiple değiştirilir
    /// (aynı aile içinde alt tipler karışabilir). Paketlenemeyen yük ayrıca döner.
    /// </summary>
    private static (List<AracYerlesimi> Araclar, List<YerlesimYuku> Sigmayanlar) IhtiyacaGore(
        List<YerlesimYuku> yukler, List<EkipmanAdayi> adaylar, Baglam baglam)
    {
        var sonuc = new List<AracYerlesimi>();
        var sigmayanlar = new List<YerlesimYuku>();
        var kalan = Sirala(yukler).ToList();
        while (kalan.Count > 0)
        {
            baglam.Iptal.ThrowIfCancellationRequested();
            (EkipmanAdayi Aday, List<YerlesimYuku> Kapsanan, List<AracYerlesimi> Araclar, List<YerlesimYuku> Disarida)? enIyi = null;
            foreach (var aday in adaylar)
            {
                var kapsanan = kalan.Where(y => baglam.Neden(aday, y) == null).ToList();
                if (kapsanan.Count == 0)
                    continue;
                var (paket, disarida) = Paketle(aday, kapsanan, baglam);
                var alinan = kapsanan.Count - disarida.Count;
                if (enIyi is not { } e || alinan > e.Kapsanan.Count - e.Disarida.Count ||
                    (alinan == e.Kapsanan.Count - e.Disarida.Count && (paket.Count < e.Araclar.Count ||
                     (paket.Count == e.Araclar.Count && (EksikVeri(aday) < EksikVeri(e.Aday) ||
                      (EksikVeri(aday) == EksikVeri(e.Aday) && TabanAlani(aday) < TabanAlani(e.Aday)))))))
                    enIyi = (aday, kapsanan, paket, disarida);
            }
            if (enIyi is not { } secilen)
            {
                sigmayanlar.AddRange(kalan);
                break;
            }
            if (secilen.Araclar.Count > 0)
            {
                var son = secilen.Araclar[^1];
                var sonYukler = son.Yukler.ToList();
                foreach (var a in adaylar.Where(a => TabanAlani(a) < TabanAlani(secilen.Aday)).OrderBy(TabanAlani))
                {
                    var (tek, disarida) = Paketle(a, sonYukler, baglam);
                    if (tek.Count != 1 || disarida.Count != 0)
                        continue;
                    secilen.Araclar[^1] = tek[0];
                    break;
                }
            }
            sonuc.AddRange(secilen.Araclar);
            sigmayanlar.AddRange(secilen.Disarida);
            var islenen = secilen.Kapsanan.Select(y => y.Anahtar).ToHashSet();
            kalan = kalan.Where(y => !islenen.Contains(y.Anahtar)).ToList();
        }
        return (sonuc, sigmayanlar);
    }

    private static long TabanAlani(EkipmanAdayi a) => a.TeknikVeri.Yuzeyler.Sum(y => (long)y.UzunlukMm * y.GenislikMm);

    /// <summary>Aynı alt tipten araçlara ilk uyan yöntemiyle (yükler büyükten küçüğe); boş araca da girmeyen yük ayrıca döner.</summary>
    private static (List<AracYerlesimi> Araclar, List<YerlesimYuku> Disarida) Paketle(EkipmanAdayi aday, List<YerlesimYuku> yukler, Baglam baglam)
    {
        var araclar = new List<AracYerlesimi>();
        var disarida = new List<YerlesimYuku>();
        foreach (var yuk in Sirala(yukler))
        {
            baglam.Iptal.ThrowIfCancellationRequested();
            if (araclar.Any(a => a.Ekle(yuk)))
                continue;
            var yeni = new AracYerlesimi(aday, baglam.Girdi.Ayarlar, baglam.Girdi.Yasal);
            if (yeni.Ekle(yuk))
                araclar.Add(yeni);
            else
                disarida.Add(yuk);
        }
        return (araclar, disarida);
    }

    /// <summary>
    /// Zorunlu özel yük: uygun özel yük ekipmanlarından yükü alan en alçak yüzeyli olan (toplam yüksekliği en az). Aday
    /// yoksa ya da hiçbiri uygun değilse uygunluk üretilmez, neden yazılır.
    /// </summary>
    private static (AracYerlesimi? Arac, string? Neden) OzelYerlestir(YerlesimYuku yuk, List<EkipmanAdayi> adaylar, YerlesimGirdisi girdi)
    {
        if (adaylar.Count == 0)
            return (null, "Uygun özel yük ekipmanı (lowbed) verisi kitaplıkta yok; uygunluk üretilmedi");
        var nedenler = new List<string>();
        AracYerlesimi? enIyi = null;
        foreach (var aday in adaylar)
        {
            var arac = new AracYerlesimi(aday, girdi.Ayarlar, girdi.Yasal);
            if (!arac.TekYukYerlestir(yuk, out var neden))
            {
                nedenler.Add(neden!);
                continue;
            }
            var z = arac.Konumlar()[0].ZMm;
            if (enIyi == null || z < enIyi.Konumlar()[0].ZMm ||
                (z == enIyi.Konumlar()[0].ZMm && (aday.TeknikVeri.TeknikKapasiteKg ?? int.MaxValue) < (enIyi.Aday.TeknikVeri.TeknikKapasiteKg ?? int.MaxValue)))
                enIyi = arac;
        }
        return enIyi != null ? (enIyi, null) : (null, $"Uygun özel yük ekipmanı bulunamadı: {Ozetle(nedenler)}");
    }

    private static string Gerekce(YerlesimYuku yuk, EkipmanAdayi aday, string aileAdi, string? ailedeNeden)
    {
        var tur = string.IsNullOrWhiteSpace(yuk.YukTuruAdi) ? "yükü" : yuk.YukTuruAdi.ToLower(System.Globalization.CultureInfo.GetCultureInfo("tr-TR"));
        return yuk.Zorunluluk == OzelEkipmanZorunlulugu.Her
            ? $"Zorunlu özel ekipman: {yuk.Kod} {tur} için {aday.TipAdi} gereklidir. Genel {aileAdi} tercihinin dışında 1 adet {aday.Kod} plana eklenmiştir."
            : $"Zorunlu özel ekipman: {yuk.Kod} {aileAdi} ailesinde taşınamıyor ({ailedeNeden}). 1 adet {aday.Kod} plana eklenmiştir.";
    }

    private sealed class HavuzAraci(EkipmanAdayi aday)
    {
        public EkipmanAdayi Aday { get; } = aday;
        public bool Kullanildi { get; set; }
    }

    private static List<HavuzAraci>? MevcutHavuz(YerlesimGirdisi girdi)
    {
        if (girdi.MevcutAraclar == null)
            return null;
        var adaylar = girdi.Adaylar.ToDictionary(a => a.KonfigurasyonId);
        return girdi.MevcutAraclar
            .Where(m => adaylar.ContainsKey(m.KonfigurasyonId))
            .SelectMany(m => Enumerable.Range(0, Math.Max(0, m.Adet)).Select(_ => new HavuzAraci(adaylar[m.KonfigurasyonId])))
            .ToList();
    }

    /// <summary>
    /// Mevcut araçlardan sıradaki kullanılmamış ve yükü alabilen araç. Özel yük için yalnız özel yük ekipmanı; normal yük
    /// için önce özel yük ekipmanı olmayanlar (lowbed son seçenektir).
    /// </summary>
    private static AracYerlesimi? HavuzdanAl(List<HavuzAraci> havuz, YerlesimGirdisi girdi, YerlesimYuku yuk, bool ozelEkipman)
    {
        var sira = ozelEkipman
            ? havuz.Where(h => !h.Kullanildi && h.Aday.OzelYukEkipmani)
            : havuz.Where(h => !h.Kullanildi).OrderBy(h => h.Aday.OzelYukEkipmani);
        foreach (var h in sira)
        {
            var arac = new AracYerlesimi(h.Aday, girdi.Ayarlar, girdi.Yasal);
            var oldu = ozelEkipman ? arac.TekYukYerlestir(yuk, out _) : arac.Ekle(yuk);
            if (!oldu)
                continue;
            h.Kullanildi = true;
            return arac;
        }
        return null;
    }

    private static List<string> Uyarilar(YerlesimGirdisi girdi, List<AracYerlesimi> araclar, List<YerlesimAraci> sonuclar, List<HavuzAraci>? havuz)
    {
        var uyarilar = new List<string>();
        if (girdi.Yasal == null)
            uyarilar.Add("Onaylı mevzuat kural seti yok; yasal ölçü ve ağırlık kontrolleri teyit edilmedi.");
        var ayar = girdi.Ayarlar;
        var tanimsiz = new List<string>();
        if (ayar.YukseklikPayiMm == null) tanimsiz.Add("yükseklik");
        if (ayar.GenislikPayiMm == null) tanimsiz.Add("genişlik");
        if (ayar.UzunlukPayiMm == null) tanimsiz.Add("uzunluk");
        if (ayar.AgirlikPayiKg == null) tanimsiz.Add("ağırlık");
        if (ayar.YuklemeHareketiPayiMm == null) tanimsiz.Add("yükleme hareketi");
        if (tanimsiz.Count > 0)
            uyarilar.Add($"Tanımlanmamış güvenlik payları: {string.Join(", ", tanimsiz)}. İlgili kontroller teyit edilmedi.");
        for (var i = 0; i < araclar.Count; i++)
        {
            if (araclar[i].IstifSiniriBilinmiyor)
                uyarilar.Add($"Araç {i + 1} ({araclar[i].Aday.Kod}): yükseklik sınırı bilinmediği için istif yapılmadı.");
        }
        foreach (var arac in sonuclar.Where(a => a.LokasyonIdleri.Count > 1))
            uyarilar.Add($"Araç {arac.AracNo}: farklı lokasyonlardaki yükler aynı araca atandı; fiziksel yükleme organizasyonu belirlenmelidir.");
        var bos = havuz?.Count(h => !h.Kullanildi) ?? 0;
        if (bos > 0)
            uyarilar.Add($"Mevcut araçlardan {bos} tanesi boş kaldı.");
        return uyarilar;
    }
}
