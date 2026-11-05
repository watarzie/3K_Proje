using _3K.YuklemeMotoru.Ekipman;

namespace _3K.YuklemeMotoru.Yerlesim;

/// <summary>
/// Tek aracın yerleşimi. Yükler yükleme yüzeylerinde önden arkaya sıralara (enine yan yana) dizilir; istif, alttaki
/// yükün üst yüzeyine tamamen oturan yükle kurulur (köprüleme ve taşma yok: destek ölçütü işletmeyle kesinleşmedi).
/// İstif dışarıda hazırlanmış grup sayılır ve bütün hâlinde giriş açıklığından geçmelidir (isterler 14). Üst ve yan
/// açıklıkta grup, açıklığın boyuna aralığında olmalıdır (isterler 15). Sıralar sonunda enine ortalanır (isterler 16).
/// Yükseklik sınırı bilinmeyen yüzeyde istif yapılmaz.
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

    /// <summary>Yükü bu araca ekler (yerdeki sırada, istifin üstünde ya da yeni sırada). Sığmazsa false.</summary>
    public bool Ekle(YerlesimYuku yuk)
    {
        if (!AgirlikUygun(yuk))
            return false;
        return SiradaYerlestir(yuk) || IstifeYerlestir(yuk) || YeniSirayaYerlestir(yuk);
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
            var istif = new Istif(yuzey, null, x, yKonum, GirisYonu.Yandan);
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

    private bool SiradaYerlestir(YerlesimYuku yuk)
    {
        foreach (var y in yuzeyler)
        {
            if (y.IcYukseklik is { } ic && yuk.YukseklikMm > ic)
                continue;
            foreach (var sira in y.Siralar)
            {
                foreach (var (boy, en) in Yonler(yuk))
                {
                    if (boy > sira.Derinlik)
                        continue;
                    var yKonum = sira.Istifler.Count == 0 ? y.YBas + kenar : sira.SonY + ayar.YerlesimBosluguMm;
                    if (yKonum + en + kenar > y.YBas + y.Genislik)
                        continue;
                    if (Yerlestir(y, sira, sira.XBas, yKonum, boy, en, yuk))
                        return true;
                }
            }
        }
        return false;
    }

    private bool YeniSirayaYerlestir(YerlesimYuku yuk)
    {
        var up = ayar.UzunlukPayiMm ?? 0;
        foreach (var y in yuzeyler)
        {
            if (y.IcYukseklik is { } ic && yuk.YukseklikMm > ic)
                continue;
            var sonSira = y.Siralar.LastOrDefault();
            var ilkX = sonSira == null ? y.XBas + kenar : sonSira.XBas + sonSira.Derinlik + ayar.YerlesimBosluguMm;
            // Kısa kenar boyuna: sıra derinliği az olur, arkaya daha çok sıra sığar.
            foreach (var (boy, en) in Yonler(yuk).OrderBy(o => o.Boy))
            {
                var yKonum = y.YBas + kenar;
                if (yKonum + en + kenar > y.YBas + y.Genislik)
                    continue;
                var x = ilkX;
                for (var deneme = 0; deneme < 20; deneme++)
                {
                    if (x + boy + up + kenar > y.XBas + y.Uzunluk)
                        break;
                    var kutu = new Kutu(x, yKonum, y.Z, boy, en, yuk.YukseklikMm);
                    var engel = engeller.FirstOrDefault(e => e.Kesisir(kutu, ayar.YerlesimBosluguMm));
                    if (engel != null)
                    {
                        x = engel.X + engel.U + ayar.YerlesimBosluguMm;
                        continue;
                    }
                    var sira = new Sira(x, boy);
                    if (Yerlestir(y, sira, x, yKonum, boy, en, yuk))
                    {
                        y.Siralar.Add(sira);
                        return true;
                    }
                    // Giriş yalnız açıklığın boyuna aralığından olabiliyorsa sıra açıklığın başına kaydırılır.
                    var aciklikBasi = Aday.TeknikVeri.Acikliklar
                        .Where(a => a.Taraf is AciklikTarafi.Ust or AciklikTarafi.Sol or AciklikTarafi.Sag && a.BaslangicXMm > x)
                        .Select(a => a.BaslangicXMm!.Value).DefaultIfEmpty(-1).Min();
                    if (aciklikBasi < 0)
                        break;
                    x = aciklikBasi;
                }
            }
        }
        return false;
    }

    private bool Yerlestir(Yuzey y, Sira sira, int x, int yKonum, int boy, int en, YerlesimYuku yuk)
    {
        var kutu = new Kutu(x, yKonum, y.Z, boy, en, yuk.YukseklikMm);
        if (engeller.Any(e => e.Kesisir(kutu, ayar.YerlesimBosluguMm)))
            return false;
        var giris = Giris(boy, en, yuk.YukseklikMm, x);
        if (giris == GirisYonu.Yok)
            return false;
        var istif = new Istif(y, sira, x, yKonum, giris);
        istif.Katlar.Add(new Katman(yuk, boy, en, boy != yuk.BoyMm, 0, 0, 0));
        sira.Istifler.Add(istif);
        Istifler.Add(istif);
        ToplamKg += yuk.BrutKg;
        return true;
    }

    private bool IstifeYerlestir(YerlesimYuku yuk)
    {
        if (!yuk.UsteKonabilir)
            return false;
        foreach (var istif in Istifler)
        {
            if (istif.Sira == null)
                continue;
            var ust = istif.Katlar[^1];
            if (!ust.Yuk.AltaKalabilir)
                continue;
            if (istif.Yuzey.IstifSiniri is not { } sinir)
            {
                IstifSiniriBilinmiyor = true;
                continue;
            }
            var yeniYukseklik = istif.Yukseklik + yuk.YukseklikMm;
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
                var kutu = new Kutu(istif.X + xo, istif.Y + yo, istif.Yuzey.Z + istif.Yukseklik, boy, en, yuk.YukseklikMm);
                if (engeller.Any(e => e.Kesisir(kutu, ayar.YerlesimBosluguMm)))
                    continue;
                // İstif dışarıda hazırlanır ve bütün hâlinde girer: taban ölçüsü ve yeni toplam yükseklik açıklıktan geçmeli.
                var taban = istif.Katlar[0];
                var giris = Giris(taban.Boy, taban.En, yeniYukseklik, istif.X);
                if (giris == GirisYonu.Yok)
                    continue;
                istif.Katlar.Add(new Katman(yuk, boy, en, boy != yuk.BoyMm, xo, yo, istif.Yukseklik));
                istif.Yontem = giris;
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
    /// </summary>
    internal GirisYonu Giris(int boy, int en, int yukseklik, int? x = null)
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
                if (Aday.Duvarli)
                    YanBilinmiyor = true;
                return GirisYonu.Yandan;
            }
            if (yan.Any(a => (a.YukseklikMm == null || yukseklik + hp <= a.YukseklikMm) && AralikIcinde(a, x, boy)))
            {
                if (yan.All(a => a.YukseklikMm == null))
                    YanBilinmiyor = true;
                return GirisYonu.Yandan;
            }
        }
        if (arkaBilinmiyor)
        {
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
    /// </summary>
    internal void ElleIstifEkle(Yuzey yuzey, GirisYonu yontem, IReadOnlyList<(YerlesimYuku Yuk, int X, int Y, int Z, int Boy, int En, bool Donduruldu)> katlar)
    {
        var taban = katlar[0];
        var istif = new Istif(yuzey, null, taban.X, taban.Y, yontem);
        foreach (var k in katlar)
        {
            istif.Katlar.Add(new Katman(k.Yuk, k.Boy, k.En, k.Donduruldu, k.X - taban.X, k.Y - taban.Y, k.Z - yuzey.Z));
            ToplamKg += k.Yuk.BrutKg;
        }
        Istifler.Add(istif);
    }

    /// <summary>Sıraları enine ortalar (tek yük orta eksende; yan yana yükler grup olarak). Engel varsa sıra kaydırılmaz.</summary>
    public void Ortala()
    {
        foreach (var y in yuzeyler)
        {
            foreach (var sira in y.Siralar)
            {
                if (sira.Istifler.Count == 0)
                    continue;
                var sol = sira.Istifler.Min(i => i.Y);
                var sag = sira.Istifler.Max(i => i.Y + i.Katlar[0].En);
                var fark = y.YBas + (y.Genislik - (sag - sol)) / 2 - sol;
                if (fark == 0)
                    continue;
                var kesisir = sira.Istifler.SelectMany(i => i.Kutular(fark)).Any(k => engeller.Any(e => e.Kesisir(k, ayar.YerlesimBosluguMm)));
                if (kesisir)
                    continue;
                foreach (var istif in sira.Istifler)
                    istif.Y += fark;
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

    internal sealed record Yuzey(string Kod, int XBas, int Uzunluk, int YBas, int Genislik, int Z, int? IcYukseklik, int? IstifSiniri)
    {
        public List<Sira> Siralar { get; } = [];
    }

    internal sealed class Sira(int xBas, int derinlik)
    {
        public int XBas { get; } = xBas;
        public int Derinlik { get; } = derinlik;
        public List<Istif> Istifler { get; } = [];
        public int SonY => Istifler.Max(i => i.Y + i.Katlar[0].En);
    }

    internal sealed class Istif(Yuzey yuzey, Sira? sira, int x, int y, GirisYonu yontem)
    {
        public Yuzey Yuzey { get; } = yuzey;
        public Sira? Sira { get; } = sira;
        public int X { get; } = x;
        public int Y { get; set; } = y;
        public GirisYonu Yontem { get; set; } = yontem;
        public List<Katman> Katlar { get; } = [];
        public int Yukseklik => Katlar.Sum(k => k.Yuk.YukseklikMm);

        public IEnumerable<Kutu> Kutular(int yKaydir) =>
            Katlar.Select(k => new Kutu(X + k.XOfs, Y + yKaydir + k.YOfs, Yuzey.Z + k.Z, k.Boy, k.En, k.Yuk.YukseklikMm));
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
