namespace _3K.YuklemeMotoru.Yerlesim;

/// <summary>
/// Otomatik yerleşim (Simülasyon isterleri 2, 8–10, 14–17, 24–25; saha geri bildirimi 19.11.2026 madde 1–4). Hedef bütün
/// zorunlu koşullar sağlanarak en az araç kullanmaktır. Araç tipi yük başına seçilir: her yük seçilen ailede kendisini
/// alan en sade kademeye (<see cref="EkipmanAdayi.Kademe"/>) gider; en büyük yükün gerektirdiği tip bütün araçlara
/// uygulanmaz. Özel kademe aracına (ör. Open Top) sığan daha sade yükler de konur, kalanlar kendi kademelerinin
/// araçlarına gider. Aileye sığmayan yük (trafonun kendisi her zaman) gerekçesiyle özel yük ekipmanına (lowbed) gider;
/// lowbed tonaja göre seçilir ve yanına başka yük konmaz. Proje bazında ayrı ve lokasyona göre ayrı tercihlerinde gruplar
/// aynı araca girmez. Hiçbir yük sessizce düşmez: araca girmeyen her yük nedeniyle "yerleşemeyen" listesindedir.
/// </summary>
public static class YerlesimMotoru
{
    public static YerlesimSonucu Hesapla(YerlesimGirdisi girdi, CancellationToken cancellationToken = default)
    {
        var baglam = new Baglam(girdi, cancellationToken);
        var aileAdaylari = AileAdaylari(girdi);
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
                    yerlesemeyenler.Add(new(yuk.Anahtar, yuk.Kod, AiledeAdayYokNedeni(girdi)));
                    continue;
                }
                var nedenler = aileAdaylari.Select(a => baglam.Neden(a, yuk)).ToList();
                if (nedenler.Any(n => n == null))
                    aileYukleri.Add(yuk);
                else
                    // Ailede taşınamayan her yük (türü ne olursa olsun) gerekçesiyle uygun özel araca gider.
                    ozelYukler.Add((yuk, Ozetle(nedenler)));
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
                    yerlesemeyenler.Add(new(yuk.Anahtar, yuk.Kod, ailedeNeden == null
                        ? neden!
                        : $"{girdi.AileAdi} ailesindeki hiçbir ekipmana sığmıyor: {ailedeNeden}. {neden}"));
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
                var (paket, sigmayanlar) = Kademeli(aileYukleri, aileAdaylari, baglam);
                aileAraclari.AddRange(paket);
                yerlesemeyenler.AddRange(sigmayanlar.Select(y => new YerlesemeyenYuk(y.Anahtar, y.Kod, "Seçilen alt tiplere birlikte yerleştirilemedi")));
            }
        }

        var ekIhtiyaclar = new List<EkEkipmanIhtiyaci>();
        if (disaridaKalanlar.Count > 0)
        {
            foreach (var yuk in disaridaKalanlar)
                yerlesemeyenler.Add(new(yuk.Anahtar, yuk.Kod, "Mevcut araçlara sığmadı"));
            ekIhtiyaclar = Kademeli(disaridaKalanlar, aileAdaylari, baglam).Araclar
                .Select(a => a.Arac)
                .GroupBy(a => a.Aday)
                .Select(g => new EkEkipmanIhtiyaci(g.Key.KonfigurasyonId, g.Key.Kod, g.Key.Ad, g.Count()))
                .ToList();
        }

        // Aile araçları önce, zorunlu özel ekipman sonra numaralanır.
        var araclar = aileAraclari.Select(a => (a.Arac, a.Gerekce, Zorunlu: false))
            .Concat(ozelAraclar.Select(a => (a.Arac, a.Gerekce, Zorunlu: true)))
            .Where(a => !a.Arac.Bos).ToList();
        var sonuclar = araclar.Select((a, i) => AracKontrolcu.Olustur(i + 1, a.Arac, girdi.Ayarlar, a.Gerekce, a.Zorunlu)).ToList();
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

    /// <summary>
    /// Elle yerleşimi tamamlar ("kalanı otomatik diz"): kullanıcının araçlara koyduğu yükler yerinde kalır (sabit);
    /// araçlara konmamış yükler önce var olan araçlardaki boşluklara (proje/lokasyon ayrımına uyarak), sığmayanlar
    /// otomatik yerleşimle açılan yeni araçlara yerleşir. Zorunlu özel yük ve ailede taşınamayan özel yük özel yük
    /// ekipmanına gider. Hiçbir yere sığmayan yük araçlara konmaz (bekleyenlerde kalır). Sonuç konum listesidir;
    /// kontroller <see cref="ElleYerlesimDenetimi.Denetle"/> ile yapılır.
    /// </summary>
    public static IReadOnlyList<ElleArac> ElleTamamla(YerlesimGirdisi girdi, IReadOnlyList<ElleArac> araclar, CancellationToken cancellationToken = default)
    {
        var baglam = new Baglam(girdi, cancellationToken);
        var yukler = girdi.Yukler.ToDictionary(y => y.Anahtar);
        var adaylar = girdi.Adaylar.ToDictionary(a => a.KonfigurasyonId);
        var aileAdaylari = AileAdaylari(girdi);
        var ozelAdaylar = girdi.Adaylar.Where(a => a.OzelYukEkipmani && a.TeknikVeri.Yuzeyler.Count > 0).ToList();

        // Ekipmanı bilinmeyen araç olduğu gibi kalır (denetim uyarır); diğerleri sabit yükleriyle kurulur.
        var sonuc = araclar.Select(elle => adaylar.TryGetValue(elle.KonfigurasyonId, out var aday) && aday.TeknikVeri.Yuzeyler.Count > 0
            ? (Arac: ElleYerlesimDenetimi.SabitAracKur(girdi, aday, elle, yukler), Ham: (ElleArac?)null)
            : (Arac: (AracYerlesimi?)null, Ham: elle)).ToList();
        var yerlesen = araclar.SelectMany(a => a.Konumlar).Select(k => k.YukAnahtari).ToHashSet(StringComparer.Ordinal);

        bool Uyumlu(AracYerlesimi arac, YerlesimYuku yuk) =>
            (!girdi.ProjeBazindaAyri || arac.Yukler.All(y => y.ProjeId == yuk.ProjeId)) &&
            (!girdi.LokasyonaGoreAyri || arac.Yukler.All(y => y.LokasyonId == yuk.LokasyonId));

        // Özel araca giden yük: kullanıcının eklediği boş özel yük ekipmanı önce; yoksa uygun özel ekipman açılır.
        void OzeleKoy(YerlesimYuku yuk)
        {
            var bos = sonuc.Select(s => s.Arac).FirstOrDefault(a => a is { Bos: true } && a.Aday.OzelYukEkipmani && a.TekYukYerlestir(yuk, out _));
            if (bos == null && OzelYerlestir(yuk, ozelAdaylar, girdi).Arac is { } yeni)
                sonuc.Add((yeni, null));
        }

        // Normal yük var olan araçlara eklenir: aile araçları (seçilen aile lowbed ailesiyse onlar da); zorunlu özel yükü
        // taşıyan aracın yanına yük konmaz.
        bool YukAlir(AracYerlesimi arac) =>
            (!arac.Aday.OzelYukEkipmani || arac.Aday.AileIci) && arac.Yukler.All(y => y.Zorunluluk != OzelEkipmanZorunlulugu.Her);

        var acikta = new List<YerlesimYuku>();
        foreach (var yuk in Sirala(girdi.Yukler.Where(y => !yerlesen.Contains(y.Anahtar))))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (yuk.Zorunluluk == OzelEkipmanZorunlulugu.Her)
            {
                OzeleKoy(yuk);
                continue;
            }
            if (!sonuc.Any(s => s.Arac is { } a && YukAlir(a) && Uyumlu(a, yuk) && a.Ekle(yuk)))
                acikta.Add(yuk);
        }

        // Var olan araçlara sığmayanlar: proje/lokasyon grubuna göre otomatik yerleşimle yeni araçlar. Ailede aday yoksa
        // yük aile dışına çıkmaz (isterler 10.2; Hesapla ile aynı), bekleyenlerde kalır.
        foreach (var grup in acikta.GroupBy(y => (Proje: girdi.ProjeBazindaAyri ? y.ProjeId : null, Lokasyon: girdi.LokasyonaGoreAyri ? y.LokasyonId : null)))
        {
            if (aileAdaylari.Count == 0)
                break;
            var (paket, sigmayanlar) = Kademeli(grup.ToList(), aileAdaylari, baglam);
            sonuc.AddRange(paket.Select(a => (Arac: (AracYerlesimi?)a.Arac, Ham: (ElleArac?)null)));
            foreach (var yuk in sigmayanlar)
                OzeleKoy(yuk);
        }

        return sonuc.Select(s =>
        {
            if (s.Arac is not { } arac)
                return s.Ham!;
            arac.Ortala();
            arac.Dengele();
            return new ElleArac(arac.Aday.KonfigurasyonId,
                arac.Konumlar().Select(k => new ElleKonum(k.YukAnahtari, k.XMm, k.YMm, k.ZMm, k.Donduruldu)).ToList());
        }).ToList();
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
                nedenler[anahtar] = neden = AracYerlesimi.SigmamaNedeni(aday, Girdi.Ayarlar, yuk);
            return neden;
        }
    }

    /// <summary>
    /// Normal yüklerin aday kümesi: seçilen ailenin verisi olan ve otomatik yerleşimde kullanılan (kademesi olan) tipleri.
    /// Mevcut araçlara yerleştirmede kullanıcının filosu esastır: kademesi olmayan aile tipi (ör. Reefer) de sayılır.
    /// </summary>
    internal static List<EkipmanAdayi> AileAdaylari(YerlesimGirdisi girdi) =>
        girdi.Adaylar.Where(a => a.AileIci && (a.Kademe != null || girdi.MevcutAraclar != null) && a.TeknikVeri.Yuzeyler.Count > 0).ToList();

    private static string AiledeAdayYokNedeni(YerlesimGirdisi girdi) =>
        girdi.Adaylar.Any(a => a.AileIci && a.TeknikVeri.Yuzeyler.Count > 0)
            ? $"{girdi.AileAdi} ailesinde otomatik kullanılan araç tipi yok; planda araç tipi seçin"
            : $"{girdi.AileAdi} ailesinde sandık taşımaya uygun ekipman verisi yok";

    /// <summary>Kademe sırası: seçilen (ya da otomatik) tipler önce sadelik sırasıyla, yedek tipler en sonda.</summary>
    private static long Seviye(EkipmanAdayi a) => (a.Yedek ? 1L << 32 : 0) + (a.Kademe ?? 1);

    /// <summary>
    /// Yük başına araç tipi (saha geri bildirimi madde 1): her yük kendisini alan en sade kademeye atanır. Kademeler en
    /// özelden başlayarak paketlenir (kademe içinde en az araç, son araç küçültülür); açılan özel araçlara daha sade
    /// yükler de konur, kalanlar sonraki kademelere geçer. Böylece yalnız gereken yük için özel tip gelir. Araçlar
    /// sadeden özele sıralanır; yedek tip aracı hangi yükler için açıldığını gerekçesinde yazar. Hiçbir adaya girmeyen
    /// yük ayrıca döner.
    /// </summary>
    private static (List<(AracYerlesimi Arac, string? Gerekce)> Araclar, List<YerlesimYuku> Sigmayanlar) Kademeli(
        List<YerlesimYuku> yukler, List<EkipmanAdayi> adaylar, Baglam baglam)
    {
        var sigmayanlar = new List<YerlesimYuku>();
        var gerekli = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (var yuk in yukler)
        {
            var uyanlar = adaylar.Where(a => baglam.Neden(a, yuk) == null).ToList();
            if (uyanlar.Count == 0)
                sigmayanlar.Add(yuk);
            else
                gerekli[yuk.Anahtar] = uyanlar.Min(Seviye);
        }

        var acilanlar = new List<(long Seviye, AracYerlesimi Arac, string? Gerekce)>();
        var kalan = Sirala(yukler.Where(y => gerekli.ContainsKey(y.Anahtar))).ToList();
        foreach (var seviye in gerekli.Values.Distinct().OrderDescending())
        {
            baglam.Iptal.ThrowIfCancellationRequested();
            var grup = kalan.Where(y => gerekli[y.Anahtar] == seviye).ToList();
            if (grup.Count == 0)
                continue;
            var (paket, disarida) = IhtiyacaGore(grup, adaylar.Where(a => Seviye(a) == seviye).ToList(), baglam);
            sigmayanlar.AddRange(disarida);
            var islenen = grup.Select(y => y.Anahtar).ToHashSet(StringComparer.Ordinal);
            // Özel tipin araçlarına sığan daha sade yükler de konur ("yanına aldığı kadar").
            foreach (var yuk in kalan.Where(y => !islenen.Contains(y.Anahtar)))
                if (paket.Any(a => a.Ekle(yuk)))
                    islenen.Add(yuk.Anahtar);
            kalan = kalan.Where(y => !islenen.Contains(y.Anahtar)).ToList();
            foreach (var arac in paket)
                acilanlar.Add((seviye, arac, arac.Aday.Yedek ? YedekGerekce(arac, grup) : null));
        }
        return (acilanlar.OrderBy(a => a.Seviye).Select(a => (a.Arac, a.Gerekce)).ToList(), sigmayanlar);
    }

    private static string YedekGerekce(AracYerlesimi arac, List<YerlesimYuku> gerektirenler)
    {
        var anahtarlar = arac.Yukler.Select(y => y.Anahtar).ToHashSet(StringComparer.Ordinal);
        return YedekGerekce(arac.Aday, gerektirenler.Where(y => anahtarlar.Contains(y.Anahtar)).Select(y => y.Kod));
    }

    /// <summary>Yedek tip aracının gerekçesi (otomatik yerleşim ve elle denetim aynı metni üretir).</summary>
    internal static string YedekGerekce(EkipmanAdayi aday, IEnumerable<string> kodlar) =>
        $"Seçilen tiplere sığmadı: {string.Join(", ", kodlar)}. Bu yükler için 1 adet {aday.Kod} ({aday.TipAdi}) plana eklenmiştir.";

    /// <summary>Aileye sığmadığı için özel araca giden yükün gerekçesi (elle denetim; otomatik yerleşim nedenleriyle yazar).</summary>
    internal static string AileyeSigmayanGerekce(EkipmanAdayi aday, string aileAdi, IEnumerable<string> kodlar) =>
        $"Zorunlu özel ekipman: {string.Join(", ", kodlar)} {aileAdi} ailesinde taşınamıyor. 1 adet {aday.Kod} plana eklenmiştir.";

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

    /// <summary>Bu sayıdan çok yükte yalnız temel sıralama denenir (hesap süresi sınırlı kalsın).</summary>
    private const int CokluDenemeYukSiniri = 300;

    /// <summary>
    /// Denenen yük sıralamaları (hepsi belirlenimci; eşitlikte kod sırası): taban alanı, en uzun kenar, hacim, ağırlık ve
    /// en kısa kenar önce. Farklı sıralama farklı boşluk bırakır; en iyi sonuç seçilir.
    /// </summary>
    private static IEnumerable<List<YerlesimYuku>> Siralamalar(List<YerlesimYuku> yukler)
    {
        yield return Sirala(yukler).ToList();
        if (yukler.Count > CokluDenemeYukSiniri)
            yield break;
        static long Alan(YerlesimYuku y) => (long)y.BoyMm * y.EnMm;
        yield return yukler.OrderByDescending(y => Math.Max(y.BoyMm, y.EnMm)).ThenByDescending(Alan).ThenBy(y => y.Kod, StringComparer.Ordinal).ToList();
        yield return yukler.OrderByDescending(y => Alan(y) * y.YukseklikMm).ThenByDescending(y => y.BrutKg).ThenBy(y => y.Kod, StringComparer.Ordinal).ToList();
        yield return yukler.OrderByDescending(y => y.BrutKg).ThenByDescending(Alan).ThenBy(y => y.Kod, StringComparer.Ordinal).ToList();
        yield return yukler.OrderByDescending(y => Math.Min(y.BoyMm, y.EnMm)).ThenByDescending(Alan).ThenBy(y => y.Kod, StringComparer.Ordinal).ToList();
    }

    /// <summary>
    /// Aynı alt tipten araçlara paketleme: her sıralama denenir; dışarıda kalan yükü en az, sonra aracı en az, sonra en
    /// dengeli (ağırlık merkezi araç ortasına en yakın) olan seçilir. Eşitlikte önce denenen kalır.
    /// </summary>
    private static (List<AracYerlesimi> Araclar, List<YerlesimYuku> Disarida) Paketle(EkipmanAdayi aday, List<YerlesimYuku> yukler, Baglam baglam)
    {
        (List<AracYerlesimi> Araclar, List<YerlesimYuku> Disarida, decimal Sapma)? enIyi = null;
        foreach (var sirali in Siralamalar(yukler))
        {
            var (araclar, disarida) = SirayaGorePaketle(aday, sirali, baglam);
            var sapma = araclar.Count == 0 ? 0 : araclar.Max(a => a.BoyunaSapma);
            if (enIyi is not { } e || disarida.Count < e.Disarida.Count ||
                (disarida.Count == e.Disarida.Count && (araclar.Count < e.Araclar.Count ||
                 (araclar.Count == e.Araclar.Count && sapma < e.Sapma))))
                enIyi = (araclar, disarida, sapma);
        }
        return (enIyi!.Value.Araclar, enIyi.Value.Disarida);
    }

    /// <summary>Aynı alt tipten araçlara ilk uyan yöntemiyle, verilen sırayla; boş araca da girmeyen yük ayrıca döner.</summary>
    private static (List<AracYerlesimi> Araclar, List<YerlesimYuku> Disarida) SirayaGorePaketle(EkipmanAdayi aday, List<YerlesimYuku> yukler, Baglam baglam)
    {
        var araclar = new List<AracYerlesimi>();
        var disarida = new List<YerlesimYuku>();
        foreach (var yuk in yukler)
        {
            baglam.Iptal.ThrowIfCancellationRequested();
            if (araclar.Any(a => a.Ekle(yuk)))
                continue;
            var yeni = new AracYerlesimi(aday, baglam.Girdi.Ayarlar);
            if (yeni.Ekle(yuk))
                araclar.Add(yeni);
            else
                disarida.Add(yuk);
        }
        return (araclar, disarida);
    }

    /// <summary>
    /// Özel yük ekipmanı (lowbed, ağır yük dorsesi): yükü alabilenlerden teknik kapasitesi en küçük olan (tonaja göre;
    /// saha geri bildirimi madde 2), eşitlikte yüzeyi en alçak olan. Araç yalnız bu yükü taşır, yanına sandık konmaz. Aday
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
            var arac = new AracYerlesimi(aday, girdi.Ayarlar);
            if (!arac.TekYukYerlestir(yuk, out var neden))
            {
                nedenler.Add(neden!);
                continue;
            }
            var kapasite = aday.TeknikVeri.TeknikKapasiteKg ?? int.MaxValue;
            var enIyiKapasite = enIyi?.Aday.TeknikVeri.TeknikKapasiteKg ?? int.MaxValue;
            if (enIyi == null || kapasite < enIyiKapasite ||
                (kapasite == enIyiKapasite && arac.Konumlar()[0].ZMm < enIyi.Konumlar()[0].ZMm))
                enIyi = arac;
        }
        return enIyi != null ? (enIyi, null) : (null, $"Uygun özel yük ekipmanı bulunamadı: {Ozetle(nedenler)}");
    }

    private static string Gerekce(YerlesimYuku yuk, EkipmanAdayi aday, string aileAdi, string? ailedeNeden)
    {
        var tur = string.IsNullOrWhiteSpace(yuk.YukTuruAdi) ? "yükü" : yuk.YukTuruAdi.ToLower(System.Globalization.CultureInfo.GetCultureInfo("tr-TR"));
        return yuk.Zorunluluk == OzelEkipmanZorunlulugu.Her || ailedeNeden == null
            ? $"Zorunlu özel ekipman: {yuk.Kod} {tur} için {aday.TipAdi} gereklidir. Genel {aileAdi} tercihinin dışında 1 adet {aday.Kod} plana eklenmiştir."
            : $"Zorunlu özel ekipman: {yuk.Kod} {aileAdi} ailesinde taşınamıyor ({ailedeNeden}). 1 adet {aday.Kod} plana eklenmiştir.";
    }

    internal sealed class HavuzAraci(EkipmanAdayi aday)
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
            var arac = new AracYerlesimi(h.Aday, girdi.Ayarlar);
            var oldu = ozelEkipman ? arac.TekYukYerlestir(yuk, out _) : arac.Ekle(yuk);
            if (!oldu)
                continue;
            h.Kullanildi = true;
            return arac;
        }
        return null;
    }

    internal static List<string> Uyarilar(YerlesimGirdisi girdi, List<AracYerlesimi> araclar, List<YerlesimAraci> sonuclar, List<HavuzAraci>? havuz)
    {
        // Teyit edilemeyen kontroller gösterilmez (AracKontrolcu.Gosterilecekler); tanımsız güvenlik payı ayrıca uyarılmaz.
        var uyarilar = new List<string>();
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
