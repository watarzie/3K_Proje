using _3K.YuklemeMotoru.Ekipman;

namespace _3K.YuklemeMotoru.Yerlesim;

/// <summary>
/// Tek aracın yerleşimi. Yükler yükleme yüzeyinde "uç nokta" yöntemiyle yerleşir: yüzeyin ön-sol köşesi ve yerleşmiş
/// her yükün arkası ve yanı aday konumdur; yük en öndeki, sonra en soldaki uygun konuma (iki yönüyle denenir) konur.
/// Böylece kısa yükün arkasında kalan boşluk da kullanılır. Önce kullanılmış boy içinde taban, sonra istif, en son
/// boyu uzatan taban denenir. İstif, alttaki yükün üst yüzeyine tamamen oturan yükle kurulur (köprüleme ve taşma yok:
/// destek ölçütü işletmeyle kesinleşmedi). İstif dışarıda hazırlanmış grup sayılır ve bütün hâlinde giriş açıklığından
/// geçmelidir (isterler 14). Üst ve yan açıklıkta grup, açıklığın boyuna aralığında olmalıdır (isterler 15). Boyuna
/// örtüşen yükler öbek olarak enine ortalanır (isterler 16); dingilli araçta yük bloğu dingil ve king-pin dengesine göre
/// boyuna kaydırılır. Elle konmuş (sabit) yük ortalanmaz ve kaydırılmaz. Yükseklik sınırı bilinmeyen yüzeyde istif yapılmaz.
/// </summary>
internal sealed class AracYerlesimi
{
    private readonly YerlesimAyarlari ayar;
    private readonly YasalSinirlar? yasal;
    private readonly List<Yuzey> yuzeyler;
    private readonly List<Kutu> engeller;
    private readonly int kenar;

    public AracYerlesimi(EkipmanAdayi aday, YerlesimAyarlari ayar, YasalSinirlar? yasal)
    {
        Aday = aday;
        this.ayar = ayar;
        this.yasal = yasal;
        kenar = aday.Duvarli ? ayar.YerlesimBosluguMm : 0;
        var veri = aday.TeknikVeri;
        DisGenislik = veri.DisOlcu?.GenislikMm ?? (veri.Yuzeyler.Count == 0 ? 0 : veri.Yuzeyler.Max(y => y.GenislikMm));
        DisUzunluk = veri.DisOlcu?.UzunlukMm ?? (veri.Yuzeyler.Count == 0 ? 0 : veri.Yuzeyler.Max(y => y.BaslangicXMm + y.UzunlukMm));
        yuzeyler = veri.Yuzeyler.OrderBy(y => y.BaslangicXMm).Select(YuzeyKur).ToList();
        engeller = veri.Engeller.Where(e => !e.Sokulebilir)
            .Select(e => new Kutu(e.XMm, e.YMm, e.ZMm, e.UzunlukMm, e.GenislikMm, e.YukseklikMm)).ToList();
    }

    public EkipmanAdayi Aday { get; }
    public int DisGenislik { get; }
    public int DisUzunluk { get; }
    public decimal ToplamKg { get; private set; }
    public bool Bos => Istifler.Count == 0;
    public List<Istif> Istifler { get; } = [];

    /// <summary>İzinler uygun olduğu hâlde yüzeyin yükseklik sınırı bilinmediği için istif kurulamadı (uyarı için).</summary>
    public bool IstifSiniriBilinmiyor { get; private set; }

    /// <summary>Arka kapı ölçüsü bilinmeden arkadan yükleme kabul edildi (kontrol "Teyit edilmedi" olur).</summary>
    public bool KapiBilinmiyor { get; private set; }

    /// <summary>Duvarlı gövdede (tenteli) yan açıklık ölçüsü bilinmeden yandan yükleme kabul edildi.</summary>
    public bool YanBilinmiyor { get; private set; }

    public IEnumerable<YerlesimYuku> Yukler => Istifler.SelectMany(i => i.Katlar).Select(k => k.Yuk);

    /// <summary>Toplam taban alanı (mm²).</summary>
    public long TabanAlani => yuzeyler.Sum(y => (long)y.Uzunluk * y.Genislik);

    /// <summary>Kendi yürüyen aksamı olan karayolu ekipmanı mı (kategoriden); konteynerde yerden yükseklik şasiye bağlıdır.</summary>
    public bool DingilliMi => Aday.Dingilli;

    private Yuzey YuzeyKur(YukYuzeyi y)
    {
        var yBas = Math.Max(0, (DisGenislik - y.GenislikMm) / 2);
        var yp = ayar.YukseklikPayiMm ?? 0;
        // İstif sınırı: iç yükseklik (biliniyorsa) ve onaylı yasal yükseklikten yüzey yüksekliği düşülerek; ikisi de
        // bilinmiyorsa istif yapılmaz.
        int? sinir = null;
        if (y.KullanilabilirYukseklikMm is { } ic)
            sinir = ic - yp;
        if (yasal?.YukseklikMm is { } yasalY && Aday.Dingilli)
            sinir = Math.Min(sinir ?? int.MaxValue, yasalY - y.YerdenYukseklikMm - yp);
        return new Yuzey(y.Kod, y.BaslangicXMm, y.UzunlukMm, yBas, y.GenislikMm, y.YerdenYukseklikMm,
            y.KullanilabilirYukseklikMm is { } i2 ? i2 - yp : null, sinir);
    }

    /// <summary>
    /// Yükü bu araca ekler: kullanılmış boy içinde tabana, olmazsa bir istifin üstüne, olmazsa boyu uzatarak tabana.
    /// Sığmazsa false.
    /// </summary>
    public bool Ekle(YerlesimYuku yuk)
    {
        if (!AgirlikUygun(yuk))
            return false;
        if (TabanYeri(yuk, kullanilanBoyda: true) is { } ici)
        {
            TabanaKoy(ici, yuk);
            return true;
        }
        if (IstifeYerlestir(yuk))
            return true;
        if (TabanYeri(yuk, kullanilanBoyda: false) is { } yeni)
        {
            TabanaKoy(yeni, yuk);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Boyuna sapma oranı: yükün ağırlık merkezinin aracın boyuna ortasından uzaklığı / araç boyu (0 = ortada). Aynı
    /// araç sayısı veren denemeler arasında dengeli olanı seçmek için.
    /// </summary>
    public decimal BoyunaSapma
    {
        get
        {
            if (ToplamKg <= 0 || DisUzunluk <= 0)
                return 0;
            var moment = Istifler.SelectMany(i => i.Katlar.Select(k => k.Yuk.BrutKg * (i.X + k.XOfs + k.Boy / 2m))).Sum();
            return Math.Abs(moment / ToplamKg - DisUzunluk / 2m) / DisUzunluk;
        }
    }

    /// <summary>Boş araçta yükün neden sığmadığı (yerleşemeyen yük açıklaması); sığıyorsa null.</summary>
    public static string? SigmamaNedeni(EkipmanAdayi aday, YerlesimAyarlari ayar, YasalSinirlar? yasal, YerlesimYuku yuk)
    {
        var bos = new AracYerlesimi(aday, ayar, yasal);
        if (bos.Ekle(yuk))
            return null;
        if (aday.TeknikVeri.Yuzeyler.Count == 0)
            return $"{aday.Kod}: yükleme yüzeyi tanımlı değil";
        if (!bos.AgirlikUygun(yuk))
            return $"{aday.Kod}: teknik kapasite {aday.TeknikVeri.TeknikKapasiteKg:0} kg, yük {yuk.BrutKg:0} kg";
        foreach (var (boy, en) in Yonler(yuk))
        {
            foreach (var y in bos.yuzeyler)
            {
                if (boy + bos.kenar * 2 + (ayar.UzunlukPayiMm ?? 0) <= y.Uzunluk && en + bos.kenar * 2 <= y.Genislik &&
                    (y.IcYukseklik == null || yuk.YukseklikMm <= y.IcYukseklik))
                {
                    var giris = bos.GirisNedeni(boy, en, yuk.YukseklikMm);
                    return giris ?? $"{aday.Kod}: araç engelleri ya da açıklık konumu yüzünden yerleşmiyor";
                }
            }
        }
        var y0 = bos.yuzeyler.MaxBy(y => (long)y.Uzunluk * y.Genislik)!;
        return $"{aday.Kod}: iç ölçülere sığmıyor (yük {yuk.BoyMm}×{yuk.EnMm}×{yuk.YukseklikMm} mm, yüzey {y0.Uzunluk}×{y0.Genislik}" +
               (y0.IcYukseklik is { } h ? $"×{h}" : string.Empty) + " mm)";
    }

    /// <summary>
    /// Özel yük ekipmanına tek yük: en alçak uygun yüzey, enine ekipman ekseninde (yüzeyden taşabilir; taşma ayrıca
    /// raporlanır), sökülemeyen engellerle çakışmayan konumlar arasında dingil ve king-pin kapasiteleri biliniyorsa en
    /// dengeli olanda, bilinmiyorsa yüzeyin önünde.
    /// </summary>
    public bool TekYukYerlestir(YerlesimYuku yuk, out string? neden)
    {
        neden = null;
        if (!AgirlikUygun(yuk))
        {
            neden = $"{Aday.Kod}: teknik kapasite {Aday.TeknikVeri.TeknikKapasiteKg:0} kg, yük {yuk.BrutKg:0} kg";
            return false;
        }
        var up = ayar.UzunlukPayiMm ?? 0;
        var adaylar = (from y in yuzeyler
                       from o in Yonler(yuk)
                       where o.Boy + up <= y.Uzunluk
                       orderby y.Z, y.Uzunluk
                       select (Yuzey: y, o.Boy, o.En)).ToList();
        if (adaylar.Count == 0)
        {
            neden = $"{Aday.Kod}: yük boyu {yuk.BoyMm} mm hiçbir yükleme yüzeyine sığmıyor (en uzun yüzey {(yuzeyler.Count == 0 ? 0 : yuzeyler.Max(y => y.Uzunluk))} mm)";
            return false;
        }
        foreach (var (yuzey, boy, en) in adaylar)
        {
            var yKonum = DisGenislik / 2 - en / 2;
            if (DengeliX(yuzey, boy, en, yKonum, yuk) is not { } x)
                continue;
            // Özel yükün üstüne yük konmaz (destek ve bağlama şartları tanımlı değil).
            var istif = new Istif(yuzey, x, yKonum, GirisYonu.Yandan) { UstuneAcik = false };
            istif.Katlar.Add(new Katman(yuk, boy, en, boy != yuk.BoyMm, 0, 0, 0));
            Istifler.Add(istif);
            ToplamKg += yuk.BrutKg;
            return true;
        }
        neden = $"{Aday.Kod}: uygun yüzeylerde sökülemeyen engellerle çakışmayan konum yok";
        return false;
    }

    /// <summary>Engelle çakışmayan konumlar arasında yük payı en dengeli olan X; konum yoksa null.</summary>
    private int? DengeliX(Yuzey yuzey, int boy, int en, int yKonum, YerlesimYuku yuk)
    {
        var veri = Aday.TeknikVeri;
        var bas = yuzey.XBas;
        var son = yuzey.XBas + yuzey.Uzunluk - boy - (ayar.UzunlukPayiMm ?? 0);
        var gecerli = new List<int>();
        for (var x = bas; x <= son; x += 50)
        {
            var kutu = new Kutu(x, yKonum, yuzey.Z, boy, en, yuk.YukseklikMm);
            if (!engeller.Any(e => e.Kesisir(kutu, ayar.YerlesimBosluguMm)))
                gecerli.Add(x);
        }
        if (gecerli.Count == 0)
            return null;
        if (veri.KingPin is not { } kp || veri.Dingiller.Count == 0)
            return gecerli[0];
        var dingilKapasite = veri.Dingiller.All(d => d.KapasiteKg != null) ? veri.Dingiller.Sum(d => (decimal)d.KapasiteKg!.Value) : (decimal?)null;
        var kpKapasite = (decimal?)kp.AzamiBaglantiYukuKg;
        if (dingilKapasite == null && kpKapasite == null)
            return gecerli[0];
        var xa = (decimal)veri.Dingiller.Average(d => d.XMm);
        return gecerli.MinBy(x =>
        {
            var (dingil, baglanti) = Tepkiler(yuk.BrutKg, x + boy / 2m, kp.XMm, xa);
            return Math.Max(dingilKapasite is { } dk && dk > 0 ? dingil / dk : 0, kpKapasite is { } kk && kk > 0 ? baglanti / kk : 0);
        });
    }

    /// <summary>
    /// Yükten gelen tepki: king-pin ile dingil grubunun ağırlık merkezi arasında basit kiriş. Dara ve çekici dağılımı
    /// bilinmediği için yalnız yükün payıdır.
    /// </summary>
    public static (decimal Dingil, decimal Baglanti) Tepkiler(decimal kg, decimal merkezX, int kingPinX, decimal dingilX)
    {
        if (dingilX == kingPinX)
            return (kg, 0);
        var dingil = kg * (merkezX - kingPinX) / (dingilX - kingPinX);
        return (dingil, kg - dingil);
    }

    private bool AgirlikUygun(YerlesimYuku yuk) =>
        Aday.TeknikVeri.TeknikKapasiteKg is not { } kapasite ||
        ToplamKg + yuk.BrutKg + (ayar.AgirlikPayiKg ?? 0) <= kapasite;

    private static IEnumerable<(int Boy, int En)> Yonler(YerlesimYuku yuk)
    {
        yield return (yuk.BoyMm, yuk.EnMm);
        if (yuk.Dondurulebilir && yuk.BoyMm != yuk.EnMm)
            yield return (yuk.EnMm, yuk.BoyMm);
    }

    /// <summary>Tabanda bir yükün yeri: yüzey, konum, yerleşik ölçü ve giriş yönü.</summary>
    private sealed record TabanAdayi(Yuzey Yuzey, int X, int Y, int Boy, int En, GirisYonu Giris);

    /// <summary>
    /// Tabanda en öndeki, sonra en soldaki uygun konum (yüzeyler önden arkaya; ilk uygun yüzey). Eşit konumda kısa kenarı
    /// boyuna olan yön seçilir (arkaya daha çok yük sığar). <paramref name="kullanilanBoyda"/> ise yük yüzeyde zaten
    /// kullanılan boyun ötesine taşmamalıdır (boş yüzey atlanır).
    /// </summary>
    private TabanAdayi? TabanYeri(YerlesimYuku yuk, bool kullanilanBoyda)
    {
        foreach (var yuzey in yuzeyler)
        {
            if (yuzey.IcYukseklik is { } ic && yuk.YukseklikMm > ic)
                continue;
            var buradakiler = Istifler.Where(i => i.Yuzey == yuzey).ToList();
            if (kullanilanBoyda && buradakiler.Count == 0)
                continue;
            var kullanilanSon = buradakiler.Count == 0 ? int.MaxValue : buradakiler.Max(i => i.X + i.Katlar[0].Boy);
            var noktalar = Noktalar(yuzey, buradakiler);
            TabanAdayi? enIyi = null;
            foreach (var (boy, en) in Yonler(yuk))
            {
                foreach (var (x, y) in noktalar)
                {
                    if (enIyi is { } e && (x > e.X || (x == e.X && (y > e.Y || (y == e.Y && boy >= e.Boy)))))
                        break;
                    if (kullanilanBoyda && x + boy > kullanilanSon)
                        continue;
                    if (!Sigar(yuzey, x, y, boy, en, yuk.YukseklikMm))
                        continue;
                    var giris = Giris(boy, en, yuk.YukseklikMm, x, bayrakKur: false);
                    if (giris == GirisYonu.Yok)
                        continue;
                    enIyi = new TabanAdayi(yuzey, x, y, boy, en, giris);
                    break;
                }
            }
            if (enIyi != null)
                return enIyi;
        }
        return null;
    }

    /// <summary>
    /// Aday konumlar (önden arkaya, soldan sağa sıralı): yüzeyin ön-sol köşesi; yerleşmiş her tabanın arkası, yanı ve
    /// arkasının sol kenarı; sökülemeyen engelin arkası ve yanı; üst/yan açıklığın başlangıcı (giriş yalnız oradan olabilir).
    /// </summary>
    private List<(int X, int Y)> Noktalar(Yuzey yuzey, List<Istif> buradakiler)
    {
        var g = ayar.YerlesimBosluguMm;
        var x0 = yuzey.XBas + kenar;
        var y0 = yuzey.YBas + kenar;
        var noktalar = new HashSet<(int X, int Y)> { (x0, y0) };
        foreach (var i in buradakiler)
        {
            var taban = i.Katlar[0];
            noktalar.Add((i.X + taban.Boy + g, i.Y));
            noktalar.Add((i.X + taban.Boy + g, y0));
            noktalar.Add((i.X, i.Y + taban.En + g));
        }
        foreach (var e in engeller.Where(e => e.X < yuzey.XBas + yuzey.Uzunluk && e.X + e.U > yuzey.XBas))
        {
            noktalar.Add((e.X + e.U + g, y0));
            noktalar.Add((Math.Max(x0, e.X), e.Y + e.G + g));
        }
        foreach (var a in Aday.TeknikVeri.Acikliklar.Where(a => (a.Taraf is AciklikTarafi.Ust or AciklikTarafi.Sol or AciklikTarafi.Sag) && a.BaslangicXMm != null))
            if (a.BaslangicXMm!.Value > x0 && a.BaslangicXMm.Value < yuzey.XBas + yuzey.Uzunluk)
                noktalar.Add((a.BaslangicXMm.Value, y0));
        return noktalar.OrderBy(n => n.X).ThenBy(n => n.Y).ToList();
    }

    /// <summary>
    /// Konum yüzeyde (kenar boşluğu ve arkada uzunluk payıyla) kalıyor, sökülemeyen engelle ve araçtaki bütün yüklerle
    /// (elle konan yük komşu yüzeye taşabilir) yerleşim boşluğundan az mesafeye girmiyor mu.
    /// </summary>
    private bool Sigar(Yuzey yuzey, int x, int y, int boy, int en, int yukseklik)
    {
        if (x < yuzey.XBas + kenar || y < yuzey.YBas + kenar ||
            x + boy + (ayar.UzunlukPayiMm ?? 0) + kenar > yuzey.XBas + yuzey.Uzunluk || y + en + kenar > yuzey.YBas + yuzey.Genislik)
            return false;
        var kutu = new Kutu(x, y, yuzey.Z, boy, en, yukseklik);
        var g = ayar.YerlesimBosluguMm;
        return !engeller.Any(e => e.Kesisir(kutu, g)) && !Istifler.SelectMany(i => i.Kutular()).Any(k => k.Kesisir(kutu, g));
    }

    private void TabanaKoy(TabanAdayi t, YerlesimYuku yuk)
    {
        // Giriş seçilen konumda bayraklarıyla (kapı/yan ölçüsü bilinmiyor) yeniden değerlendirilir.
        var istif = new Istif(t.Yuzey, t.X, t.Y, Giris(t.Boy, t.En, yuk.YukseklikMm, t.X));
        istif.Katlar.Add(new Katman(yuk, t.Boy, t.En, t.Boy != yuk.BoyMm, 0, 0, 0));
        Istifler.Add(istif);
        ToplamKg += yuk.BrutKg;
    }

    private bool IstifeYerlestir(YerlesimYuku yuk)
    {
        if (!yuk.UsteKonabilir)
            return false;
        foreach (var istif in Istifler)
        {
            // Tabanı yüzeyde olmayan (elle konmuş desteksiz ya da köprü) istifin üstüne yük konmaz.
            if (!istif.UstuneAcik || istif.Katlar[0].Z > ElleTolerans)
                continue;
            var ust = istif.Katlar[^1];
            if (!ust.Yuk.AltaKalabilir)
                continue;
            if (istif.Yuzey.IstifSiniri is not { } sinir)
            {
                IstifSiniriBilinmiyor = true;
                continue;
            }
            // Üst kot gerçek konumdan: elle istifte katlar arasında sapma olabilir.
            var ustKot = ust.Z + ust.Yuk.YukseklikMm;
            var yeniYukseklik = ustKot + yuk.YukseklikMm;
            if (yeniYukseklik > sinir)
                continue;
            if (!UstYukUygun(istif, yuk.BrutKg))
                continue;
            foreach (var (boy, en) in Yonler(yuk))
            {
                if (boy > ust.Boy || en > ust.En)
                    continue;
                var xo = ust.XOfs + (ust.Boy - boy) / 2;
                var yo = ust.YOfs + (ust.En - en) / 2;
                var kutu = new Kutu(istif.X + xo, istif.Y + yo, istif.Yuzey.Z + ustKot, boy, en, yuk.YukseklikMm);
                var g = ayar.YerlesimBosluguMm;
                // Engel ve başka istifler (elle konan köprü yük alttakinin üstünü kaplayabilir) ile çakışmamalı.
                if (engeller.Any(e => e.Kesisir(kutu, g)) ||
                    Istifler.Where(i => i != istif).SelectMany(i => i.Kutular()).Any(k => k.Kesisir(kutu, g)))
                    continue;
                // İstif dışarıda hazırlanır ve bütün hâlinde girer: taban ölçüsü ve yeni toplam yükseklik açıklıktan geçmeli.
                var taban = istif.Katlar[0];
                if (Giris(taban.Boy, taban.En, yeniYukseklik, istif.X, bayrakKur: false) == GirisYonu.Yok)
                    continue;
                istif.Katlar.Add(new Katman(yuk, boy, en, boy != yuk.BoyMm, xo, yo, ustKot));
                istif.Yontem = Giris(taban.Boy, taban.En, yeniYukseklik, istif.X);
                ToplamKg += yuk.BrutKg;
                return true;
            }
        }
        return false;
    }

    /// <summary>Alttaki her katın üzerindeki toplam ağırlık (yeni yük dahil) kendi üst yük sınırını aşmamalı; sınırı bilinmeyen kata yük konmaz.</summary>
    private static bool UstYukUygun(Istif istif, decimal yeniKg)
    {
        var ustundeki = yeniKg;
        for (var i = istif.Katlar.Count - 1; i >= 0; i--)
        {
            var kat = istif.Katlar[i];
            if (kat.Yuk.UstYukSiniriKg is not { } sinir || ustundeki > sinir)
                return false;
            ustundeki += kat.Yuk.BrutKg;
        }
        return true;
    }

    /// <summary>Grubun boyuna aralığı açıklığın aralığında mı; aralık tanımlı değilse bütün boy açıktır.</summary>
    private static bool AralikIcinde(Aciklik a, int? x, int boy) =>
        x == null || a.BaslangicXMm is not { } bas || a.UzunlukMm is not { } u || (x >= bas && x + boy <= bas + u);

    /// <summary>
    /// Grubun (tek yük ya da istif) giriş yolu: arkadan kapıdan, olmuyorsa üst açıklıktan, sonra yandan. Arka kapı ya da
    /// duvarlı gövdenin yan açıklık ölçüsü bilinmeden giriş kabul edilir ama kontrol "Teyit edilmedi" olur. Konum
    /// (<paramref name="x"/>) verilirse üst ve yan açıklıkta grup açıklığın boyuna aralığında olmalıdır.
    /// <paramref name="bayrakKur"/> false ise (aday denemesi) "ölçü bilinmiyor" bayrakları kurulmaz; bayrak yalnız seçilen
    /// konumda kurulur, seçilmeyen deneme kontrolü "Teyit edilmedi"ye çekmez.
    /// </summary>
    internal GirisYonu Giris(int boy, int en, int yukseklik, int? x = null, bool bayrakKur = true)
    {
        var hp = ayar.YuklemeHareketiPayiMm ?? 0;
        var acikliklar = Aday.TeknikVeri.Acikliklar;
        var arkaBilinmiyor = false;
        if (Aday.Giris.HasFlag(GirisYonu.Arkadan))
        {
            var arka = acikliklar.FirstOrDefault(a => a.Taraf == AciklikTarafi.Arka);
            if (arka?.GenislikMm is { } g && arka.YukseklikMm is { } h)
            {
                if (en + hp <= g && yukseklik + hp <= h)
                    return GirisYonu.Arkadan;
            }
            else
            {
                arkaBilinmiyor = true;
            }
        }
        if (Aday.Giris.HasFlag(GirisYonu.Ustten))
        {
            var ust = acikliklar.FirstOrDefault(a => a.Taraf == AciklikTarafi.Ust);
            if (ust?.UzunlukMm is { } u && ust.GenislikMm is { } g && boy + hp <= u && en + hp <= g && AralikIcinde(ust, x, boy))
                return GirisYonu.Ustten;
        }
        if (Aday.Giris.HasFlag(GirisYonu.Yandan))
        {
            var yan = acikliklar.Where(a => a.Taraf is AciklikTarafi.Sol or AciklikTarafi.Sag).ToList();
            if (yan.Count == 0)
            {
                // Açık platformda yan açıklık sınırı yoktur; duvarlı (tenteli) gövdede ölçü bilinmeden kabul teyit ister.
                if (Aday.Duvarli && bayrakKur)
                    YanBilinmiyor = true;
                return GirisYonu.Yandan;
            }
            if (yan.Any(a => (a.YukseklikMm == null || yukseklik + hp <= a.YukseklikMm) && AralikIcinde(a, x, boy)))
            {
                if (yan.All(a => a.YukseklikMm == null) && bayrakKur)
                    YanBilinmiyor = true;
                return GirisYonu.Yandan;
            }
        }
        if (arkaBilinmiyor)
        {
            if (bayrakKur)
                KapiBilinmiyor = true;
            return GirisYonu.Arkadan;
        }
        return GirisYonu.Yok;
    }

    /// <summary>Boş araçta girişin neden olmadığını açıklar (kapı ve üst açıklık ölçüleriyle).</summary>
    private string? GirisNedeni(int boy, int en, int yukseklik)
    {
        if (Giris(boy, en, yukseklik) != GirisYonu.Yok)
            return null;
        var hp = ayar.YuklemeHareketiPayiMm ?? 0;
        var arka = Aday.TeknikVeri.Acikliklar.FirstOrDefault(a => a.Taraf == AciklikTarafi.Arka);
        if (arka?.GenislikMm is { } g && arka.YukseklikMm is { } h)
            return $"{Aday.Kod}: iç hacme sığıyor ama arka kapıdan geçmiyor (gereken {en + hp}×{yukseklik + hp} mm, kapı {g}×{h} mm)";
        return $"{Aday.Kod}: giriş açıklığından geçmiyor";
    }

    /// <summary>Elle yerleşimde yükün oturduğu yükleme yüzeyi: tabanı yüzey yüksekliğinde ve yüzeyle boyuna/enine örtüşen.</summary>
    internal Yuzey? TabanYuzeyi(int x, int y, int z, int boy, int en) =>
        yuzeyler.FirstOrDefault(s => Math.Abs(z - s.Z) <= ElleTolerans &&
                                     x < s.XBas + s.Uzunluk && s.XBas < x + boy && y < s.YBas + s.Genislik && s.YBas < y + en);

    /// <summary>Konumu en yakın (altındaki) yüzey; desteksiz yük çizim ve hesap için buna bağlanır.</summary>
    internal Yuzey EnYakinYuzey(int x, int z) =>
        yuzeyler.Where(s => x >= s.XBas - 1 && x <= s.XBas + s.Uzunluk).OrderBy(s => Math.Abs(z - s.Z)).FirstOrDefault()
        ?? yuzeyler.OrderBy(s => Math.Abs(z - s.Z)).First();

    /// <summary>Elle yerleşimde yükseklik/konum eşleşmesinde kabul edilen ölçü sapması (mm).</summary>
    internal const int ElleTolerans = 2;

    internal IReadOnlyList<Kutu> SokulemezEngeller => engeller;

    /// <summary>
    /// Elle yerleşimde bir istif (tabandaki yük ve üstüne oturanlar) ekler; konumlar mutlaktır, kontrol ayrıca yapılır.
    /// İstif sabittir: ortalama ve dengeleme onu kaydırmaz ("kalanı otomatik diz" etrafına yerleştirir).
    /// </summary>
    internal void ElleIstifEkle(Yuzey yuzey, GirisYonu yontem, IReadOnlyList<(YerlesimYuku Yuk, int X, int Y, int Z, int Boy, int En, bool Donduruldu)> katlar)
    {
        var taban = katlar[0];
        var istif = new Istif(yuzey, taban.X, taban.Y, yontem) { Sabit = true };
        foreach (var k in katlar)
        {
            istif.Katlar.Add(new Katman(k.Yuk, k.Boy, k.En, k.Donduruldu, k.X - taban.X, k.Y - taban.Y, k.Z - yuzey.Z));
            ToplamKg += k.Yuk.BrutKg;
        }
        Istifler.Add(istif);
    }

    /// <summary>
    /// Yüzeydeki istifleri boyuna örtüşen öbeklere ayırır (önden arkaya); bir öbek birlikte ortalanır. Öbekler boyuna
    /// ayrık olduğu için birinin enine kaydırılması diğerine çarpmaz.
    /// </summary>
    private List<List<Istif>> Obekler(Yuzey yuzey)
    {
        var obekler = new List<List<Istif>>();
        var obekSonu = int.MinValue;
        foreach (var istif in Istifler.Where(i => i.Yuzey == yuzey).OrderBy(i => i.X))
        {
            if (obekler.Count == 0 || istif.X >= obekSonu)
            {
                obekler.Add([istif]);
                obekSonu = istif.X + istif.Katlar[0].Boy;
            }
            else
            {
                obekler[^1].Add(istif);
                obekSonu = Math.Max(obekSonu, istif.X + istif.Katlar[0].Boy);
            }
        }
        return obekler;
    }

    /// <summary>
    /// Öbekleri enine ortalar (tek yük orta eksende; yan yana yükler grup olarak). Engelle çakışacaksa ya da öbekte elle
    /// konmuş (sabit) yük varsa öbek kaydırılmaz.
    /// </summary>
    public void Ortala()
    {
        foreach (var yuzey in yuzeyler)
        {
            foreach (var obek in Obekler(yuzey))
            {
                if (obek.Any(i => i.Sabit))
                    continue;
                var sol = obek.Min(i => i.Y);
                var sag = obek.Max(i => i.Y + i.Katlar[0].En);
                var fark = yuzey.YBas + (yuzey.Genislik - (sag - sol)) / 2 - sol;
                if (fark == 0)
                    continue;
                if (obek.SelectMany(i => i.Kutular(0, fark)).Any(k => engeller.Any(e => e.Kesisir(k, ayar.YerlesimBosluguMm))))
                    continue;
                foreach (var istif in obek)
                    istif.Y += fark;
            }
        }
    }

    /// <summary>
    /// Dingilli araçta yüzeydeki yük bloğu, boşta kalan boy içinde arkaya kaydırılarak dingil grubu ve king-pin kullanım
    /// oranlarının büyüğü en aza indirilir (yalnız yükün payı; tek dingil grubu, dingil ve king-pin kapasitelerinin ikisi
    /// de bilinmeli: biri bilinmezse amaç bütün yükü bilinmeyen tarafa yığar). Kayma engele çarpmamalı ve her grup
    /// girişinden geçmeye devam etmelidir; giriş yönü kaymadan sonra yenilenir. Elle konmuş yük olan yüzey kaydırılmaz.
    /// </summary>
    public void Dengele()
    {
        var veri = Aday.TeknikVeri;
        if (!Aday.Dingilli || veri.KingPin is not { } kp || veri.Dingiller.Count == 0 ||
            veri.Dingiller.Select(d => d.GrupNo).Distinct().Count() > 1)
            return;
        var dingilKapasite = veri.Dingiller.All(d => d.KapasiteKg != null) ? veri.Dingiller.Sum(d => (decimal)d.KapasiteKg!.Value) : (decimal?)null;
        var kpKapasite = (decimal?)kp.AzamiBaglantiYukuKg;
        if (dingilKapasite is not > 0 || kpKapasite is not > 0)
            return;
        var dingilX = (decimal)veri.Dingiller.Average(d => d.XMm);
        var g = ayar.YerlesimBosluguMm;

        decimal Oran(IReadOnlyList<Istif> kayan, int d)
        {
            decimal dingil = 0, baglanti = 0;
            foreach (var istif in Istifler)
            {
                var kay = kayan.Contains(istif) ? d : 0;
                foreach (var k in istif.Katlar)
                {
                    var (dy, by) = Tepkiler(k.Yuk.BrutKg, istif.X + kay + k.XOfs + k.Boy / 2m, kp.XMm, dingilX);
                    dingil += dy;
                    baglanti += by;
                }
            }
            return Math.Max(dingil / dingilKapasite.Value, baglanti / kpKapasite.Value);
        }

        foreach (var yuzey in yuzeyler)
        {
            var buradakiler = Istifler.Where(i => i.Yuzey == yuzey).ToList();
            if (buradakiler.Count == 0 || buradakiler.Any(i => i.Sabit))
                continue;
            var bos = yuzey.XBas + yuzey.Uzunluk - kenar - (ayar.UzunlukPayiMm ?? 0) - buradakiler.Max(i => i.X + i.Katlar[0].Boy);
            if (bos <= 0)
                continue;
            var enIyi = 0;
            var enIyiOran = Oran(buradakiler, 0);
            for (var d = Math.Min(50, bos); d <= bos; d = d == bos ? bos + 1 : Math.Min(d + 50, bos))
            {
                var oran = Oran(buradakiler, d);
                if (oran >= enIyiOran)
                    continue;
                var gecerli = buradakiler.All(i =>
                    !i.Kutular(d, 0).Any(k => engeller.Any(e => e.Kesisir(k, g))) &&
                    Giris(i.Katlar[0].Boy, i.Katlar[0].En, i.Yukseklik, i.X + d, bayrakKur: false) != GirisYonu.Yok);
                if (!gecerli)
                    continue;
                (enIyi, enIyiOran) = (d, oran);
            }
            if (enIyi == 0)
                continue;
            foreach (var istif in buradakiler)
            {
                istif.X += enIyi;
                istif.Yontem = Giris(istif.Katlar[0].Boy, istif.Katlar[0].En, istif.Yukseklik, istif.X);
            }
        }
    }

    /// <summary>Konumlar: istifler önden arkaya (sonra soldan sağa) sıralanır; yükleme sırası istif sırasıdır.</summary>
    public List<YukKonumu> Konumlar()
    {
        var sirali = Istifler.OrderBy(i => i.X).ThenBy(i => i.Y).ToList();
        var sonuc = new List<YukKonumu>();
        for (var n = 0; n < sirali.Count; n++)
        {
            var istif = sirali[n];
            for (var k = 0; k < istif.Katlar.Count; k++)
            {
                var kat = istif.Katlar[k];
                sonuc.Add(new YukKonumu(kat.Yuk.Anahtar, istif.X + kat.XOfs, istif.Y + kat.YOfs, istif.Yuzey.Z + kat.Z, kat.Boy, kat.En,
                    kat.Yuk.YukseklikMm, kat.Donduruldu, istif.Yuzey.Kod, n + 1, k + 1, n + 1, istif.Yontem));
            }
        }
        return sonuc;
    }

    internal sealed record Yuzey(string Kod, int XBas, int Uzunluk, int YBas, int Genislik, int Z, int? IcYukseklik, int? IstifSiniri);

    internal sealed class Istif(Yuzey yuzey, int x, int y, GirisYonu yontem)
    {
        public Yuzey Yuzey { get; } = yuzey;
        public int X { get; set; } = x;
        public int Y { get; set; } = y;
        public GirisYonu Yontem { get; set; } = yontem;
        public List<Katman> Katlar { get; } = [];
        /// <summary>Tabandan en üst katın üstüne yükseklik (gerçek kotlardan; elle istifte katlar arası sapma olabilir).</summary>
        public int Yukseklik => Katlar.Max(k => k.Z + k.Yuk.YukseklikMm) - Katlar[0].Z;

        /// <summary>Elle konmuş: ortalama ve dengeleme kaydırmaz.</summary>
        public bool Sabit { get; init; }

        /// <summary>Üstüne yük konabilir mi (özel yük ekipmanındaki tek yükün üstüne konmaz).</summary>
        public bool UstuneAcik { get; init; } = true;

        public IEnumerable<Kutu> Kutular(int xKaydir = 0, int yKaydir = 0) =>
            Katlar.Select(k => new Kutu(X + xKaydir + k.XOfs, Y + yKaydir + k.YOfs, Yuzey.Z + k.Z, k.Boy, k.En, k.Yuk.YukseklikMm));
    }

    internal sealed record Katman(YerlesimYuku Yuk, int Boy, int En, bool Donduruldu, int XOfs, int YOfs, int Z);

    internal sealed record Kutu(int X, int Y, int Z, int U, int G, int H)
    {
        /// <summary>Kutular arasında boşluk payından az mesafe varsa kesişir sayılır.</summary>
        public bool Kesisir(Kutu b, int bosluk) =>
            X < b.X + b.U + bosluk && b.X < X + U + bosluk &&
            Y < b.Y + b.G + bosluk && b.Y < Y + G + bosluk &&
            Z < b.Z + b.H && b.Z < Z + H;
    }
}
