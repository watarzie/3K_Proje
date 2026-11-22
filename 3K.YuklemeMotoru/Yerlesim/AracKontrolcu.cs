using System.Globalization;

namespace _3K.YuklemeMotoru.Yerlesim;

/// <summary>
/// Yerleşmiş aracın toplamları ve kontrolleri (isterler 22–23). Veri yoksa ya da ilgili güvenlik payı tanımlanmamışsa
/// kontrol "Teyit edilmedi" çıkar ve gösterilmez (sahanın isteği, 20 Kasım 2026): araçta yalnız uygun ve uygunsuz
/// kontroller kalır, araç sonucu da onlardan türer. Tanımsız pay sıfır sayılmaz (o kontrol uygun yazılmaz, gizlenir).
/// Mevzuat (yasal ölçü) kontrolü yapılmaz.
/// </summary>
internal static class AracKontrolcu
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    public static YerlesimAraci Olustur(int aracNo, AracYerlesimi arac, YerlesimAyarlari ayar, string? aileDisiGerekce,
        bool zorunluOzelEkipman, bool ortala = true, bool teyitsizleriAyikla = true)
    {
        // Elle yerleşimde kullanıcının konumu korunur (ortalanmaz, dengelenmez).
        if (ortala)
        {
            arac.Ortala();
            arac.Dengele();
        }
        var konumlar = arac.Konumlar();
        var veri = arac.Aday.TeknikVeri;
        var yukler = arac.Yukler.ToDictionary(y => y.Anahtar);

        // Boş araç (elle eklenmiş) da raporlanır: ölçüler ekipmanın kendisidir.
        var minX = Math.Min(0, konumlar.Select(k => k.XMm).DefaultIfEmpty(0).Min());
        var maxX = Math.Max(arac.DisUzunluk, konumlar.Select(k => k.XMm + k.BoyMm).DefaultIfEmpty(0).Max());
        var minY = Math.Min(0, konumlar.Select(k => k.YMm).DefaultIfEmpty(0).Min());
        var maxY = Math.Max(arac.DisGenislik, konumlar.Select(k => k.YMm + k.EnMm).DefaultIfEmpty(0).Max());
        var yukluYukseklik = Math.Max(veri.DisOlcu?.YukseklikMm ?? 0, konumlar.Select(k => k.ZMm + k.YukseklikMm).DefaultIfEmpty(0).Max());
        var toplamKg = arac.ToplamKg;
        // Boyuna ağırlık merkezi raporla aynı hesaptan gelir (rapor enine ve düşey değerleri de yazar).
        var agirlikMerkezi = AgirlikMerkeziHesabi.Hesapla(konumlar.Select(k =>
            new AgirlikliKutu(yukler[k.YukAnahtari].BrutKg, k.XMm, k.YMm, k.ZMm, k.BoyMm, k.EnMm, k.YukseklikMm)))?.XMm;
        var taban = konumlar.Where(k => k.Kat == 1).Sum(k => (long)k.BoyMm * k.EnMm);
        var doluluk = arac.TabanAlani > 0 ? (int)Math.Min(100, Math.Round(taban * 100m / arac.TabanAlani)) : 0;

        var olcu = new Olculer(maxY - minY, maxX - minX, yukluYukseklik, -minY, maxY - arac.DisGenislik, -minX, maxX - arac.DisUzunluk);
        var kontroller = new List<AracKontrolu>
        {
            Agirlik(arac, ayar),
            AgirlikDagilimi(arac, ayar, konumlar, yukler),
            IcYukseklik(arac, ayar, konumlar)
        };
        if (KapiKontrolu(arac, ayar, konumlar) is { } kapi)
            kontroller.Add(kapi);
        kontroller.Add(Tasma(olcu));
        kontroller.AddRange(DingilVeBaglanti(arac, konumlar, yukler));
        kontroller.Add(Destek(arac, konumlar, yukler));
        // Elle denetim yük sorunlarını aynı türdeki kontrolle birleştirdikten sonra ayıklar (teyit edilemeyen destek
        // kontrolü, havadaki yükün sorunuyla uygunsuza döner).
        var gosterilecek = teyitsizleriAyikla ? Gosterilecekler(kontroller) : kontroller;

        var projeler = konumlar.Select(k => yukler[k.YukAnahtari].ProjeId).Distinct().ToList();
        var lokasyonlar = konumlar.Select(k => yukler[k.YukAnahtari].LokasyonId).Distinct().ToList();
        return new YerlesimAraci(aracNo, arac.Aday.KonfigurasyonId, arac.Aday.RevizyonNo, arac.Aday.Kod, arac.Aday.Ad,
            arac.Aday.AileAdi, arac.Aday.TipAdi, !arac.Aday.AileIci, zorunluOzelEkipman && !arac.Aday.AileIci, aileDisiGerekce, projeler, lokasyonlar, konumlar,
            toplamKg, veri.TeknikKapasiteKg, doluluk, agirlikMerkezi, olcu.YukseklikMm, olcu.GenislikMm, olcu.UzunlukMm,
            Math.Max(0, olcu.Sol), Math.Max(0, olcu.Sag), Math.Max(0, olcu.On), Math.Max(0, olcu.Arka), gosterilecek,
            EnKotu(gosterilecek.Select(k => k.Sonuc)));
    }

    /// <summary>
    /// Araçta gösterilecek kontroller: "Teyit edilmedi" çıkanlar (veri, pay ya da sınır tanımsız) listeden düşer; araç
    /// sonucu da yalnız kalanlardan türer. Taşma miktarı aracın taşma alanlarında ayrıca durur.
    /// </summary>
    public static List<AracKontrolu> Gosterilecekler(IEnumerable<AracKontrolu> kontroller) =>
        kontroller.Where(k => k.Sonuc != KontrolSonucu.TeyitEdilmedi).ToList();

    public static KontrolSonucu EnKotu(IEnumerable<KontrolSonucu> sonuclar)
    {
        var liste = sonuclar.ToList();
        return liste.Contains(KontrolSonucu.Uygunsuz) ? KontrolSonucu.Uygunsuz
            : liste.Contains(KontrolSonucu.TeyitEdilmedi) ? KontrolSonucu.TeyitEdilmedi
            : KontrolSonucu.Uygun;
    }

    private sealed record Olculer(int GenislikMm, int UzunlukMm, int YukseklikMm, int Sol, int Sag, int On, int Arka);

    private static string Kg(decimal kg) => kg.ToString("#,0", Tr) + " kg";
    private static string Mm(int mm) => mm.ToString("#,0", Tr) + " mm";

    /// <summary>
    /// Payı tanımlı değilse geçen kontrol "Teyit edilmedi" olur. Açıkça 0 girilen pay bilinçli karar sayılır
    /// (kullanıcı kararı, 5 Kasım 2026); boş pay ile ayrılır.
    /// </summary>
    private static AracKontrolu Payli(KontrolTuru tur, bool asildi, int? pay, string olcum, string payAdi) =>
        asildi ? new(tur, KontrolSonucu.Uygunsuz, olcum)
        : pay == null ? new(tur, KontrolSonucu.TeyitEdilmedi, $"{olcum}; {payAdi} payı tanımlanmadı")
        : new(tur, KontrolSonucu.Uygun, olcum);

    private static AracKontrolu Agirlik(AracYerlesimi arac, YerlesimAyarlari ayar)
    {
        if (arac.Aday.TeknikVeri.TeknikKapasiteKg is not { } kapasite)
            return new(KontrolTuru.Agirlik, KontrolSonucu.TeyitEdilmedi, $"Toplam {Kg(arac.ToplamKg)}; teknik kapasite tanımlı değil");
        var gereken = arac.ToplamKg + (ayar.AgirlikPayiKg ?? 0);
        var olcum = gereken > kapasite
            ? $"Toplam {Kg(arac.ToplamKg)}, sınır {Kg(kapasite)}, aşım {Kg(gereken - kapasite)}"
            : $"Toplam {Kg(arac.ToplamKg)}, sınır {Kg(kapasite)}";
        return Payli(KontrolTuru.Agirlik, gereken > kapasite, ayar.AgirlikPayiKg, olcum, "ağırlık");
    }

    /// <summary>
    /// Ağırlık dağılımı: yükün ağırlık merkezinin araç ortasından boyuna (önde/arkada) ve enine (solda/sağda) sapması,
    /// mm ve araç boyu/genişliğinin yüzdesi olarak. Sınır plan ayarıdır; tanımlı değilse "Teyit edilmedi".
    /// </summary>
    private static AracKontrolu AgirlikDagilimi(AracYerlesimi arac, YerlesimAyarlari ayar, List<YukKonumu> konumlar, Dictionary<string, YerlesimYuku> yukler)
    {
        var toplam = konumlar.Sum(k => yukler[k.YukAnahtari].BrutKg);
        if (toplam <= 0 || arac.DisUzunluk <= 0 || arac.DisGenislik <= 0)
            return new(KontrolTuru.AgirlikDagilimi, KontrolSonucu.Uygun, "Araç boş");
        var mx = konumlar.Sum(k => yukler[k.YukAnahtari].BrutKg * (k.XMm + k.BoyMm / 2m)) / toplam;
        var my = konumlar.Sum(k => yukler[k.YukAnahtari].BrutKg * (k.YMm + k.EnMm / 2m)) / toplam;
        var boyuna = mx - arac.DisUzunluk / 2m;
        var enine = my - arac.DisGenislik / 2m;
        var boyYuzde = Math.Abs(boyuna) * 100 / arac.DisUzunluk;
        var enYuzde = Math.Abs(enine) * 100 / arac.DisGenislik;
        var olcum = $"Ağırlık merkezi araç ortasının {Mm((int)Math.Round(Math.Abs(boyuna)))} {(boyuna < 0 ? "önünde" : "arkasında")} " +
                    $"(%{boyYuzde.ToString("0.#", Tr)}), {Mm((int)Math.Round(Math.Abs(enine)))} {(enine < 0 ? "solunda" : "sağında")} (%{enYuzde.ToString("0.#", Tr)})";
        if (ayar.AgirlikMerkeziSapmaYuzde is not { } sinir)
            return new(KontrolTuru.AgirlikDagilimi, KontrolSonucu.TeyitEdilmedi, $"{olcum}; ağırlık merkezi sapma sınırı tanımlanmadı");
        return boyYuzde > sinir || enYuzde > sinir
            ? new(KontrolTuru.AgirlikDagilimi, KontrolSonucu.Uygunsuz, $"{olcum}; sınır %{sinir}")
            : new(KontrolTuru.AgirlikDagilimi, KontrolSonucu.Uygun, $"{olcum}; sınır %{sinir}");
    }

    private static AracKontrolu IcYukseklik(AracYerlesimi arac, YerlesimAyarlari ayar, List<YukKonumu> konumlar)
    {
        if (!arac.Aday.Duvarli)
            return new(KontrolTuru.IcYukseklik, KontrolSonucu.Uygun, "Açık ekipman; iç yükseklik sınırı yok");
        var yuzeyler = arac.Aday.TeknikVeri.Yuzeyler.ToDictionary(y => y.Kod);
        if (konumlar.Count == 0)
            return new(KontrolTuru.IcYukseklik, KontrolSonucu.Uygun, "Araç boş");
        var bilinmeyen = konumlar.Select(k => k.YuzeyKodu).Distinct().Where(k => yuzeyler[k].KullanilabilirYukseklikMm == null).ToList();
        if (bilinmeyen.Count > 0)
            return new(KontrolTuru.IcYukseklik, KontrolSonucu.TeyitEdilmedi, $"İç yükseklik tanımlı değil ({string.Join(", ", bilinmeyen)})");
        var enYuksek = konumlar.GroupBy(k => (k.YuzeyKodu, k.IstifNo))
            .Select(g => (Yuzey: g.Key.YuzeyKodu, H: g.Sum(k => k.YukseklikMm)))
            .MaxBy(g => g.H - yuzeyler[g.Yuzey].KullanilabilirYukseklikMm!.Value);
        var ic = yuzeyler[enYuksek.Yuzey].KullanilabilirYukseklikMm!.Value;
        var gereken = enYuksek.H + (ayar.YukseklikPayiMm ?? 0);
        return Payli(KontrolTuru.IcYukseklik, gereken > ic, ayar.YukseklikPayiMm,
            $"En yüksek istif {Mm(enYuksek.H)}, iç yükseklik {Mm(ic)}", "yükseklik");
    }

    private static AracKontrolu? KapiKontrolu(AracYerlesimi arac, YerlesimAyarlari ayar, List<YukKonumu> konumlar)
    {
        if (!arac.Aday.Giris.HasFlag(GirisYonu.Arkadan))
        {
            // Duvarlı gövdeye (tenteli) yandan yükleme: yan açıklık ölçüsü bilinmeden geçiş teyit edilemez.
            if (!arac.Aday.Duvarli || !arac.Aday.Giris.HasFlag(GirisYonu.Yandan))
                return null;
            return arac.YanBilinmiyor
                ? new(KontrolTuru.KapidanGecis, KontrolSonucu.TeyitEdilmedi, "Yan açıklık ölçüsü tanımlı değil; yandan geçiş teyit edilemedi")
                : Payli(KontrolTuru.KapidanGecis, false, ayar.YuklemeHareketiPayiMm, "Bütün gruplar yan açıklıktan geçiyor", "yükleme hareket");
        }
        var ustten = konumlar.Where(k => k.YuklemeYontemi == GirisYonu.Ustten).Select(k => k.IstifNo).Distinct().Count();
        var ek = ustten > 0 ? $"; {ustten} grup üst açıklıktan yüklenir" : string.Empty;
        if (arac.KapiBilinmiyor)
            return new(KontrolTuru.KapidanGecis, KontrolSonucu.TeyitEdilmedi, "Arka kapı açıklığı tanımlı değil; kapıdan geçiş teyit edilemedi" + ek);
        var arka = arac.Aday.TeknikVeri.Acikliklar.FirstOrDefault(a => a.Taraf == Ekipman.AciklikTarafi.Arka);
        var olcum = arka?.GenislikMm is { } g && arka.YukseklikMm is { } h
            ? $"Bütün gruplar kapıdan geçiyor (kapı {g}×{h} mm){ek}"
            : $"Bütün gruplar üst açıklıktan yüklenir{ek}";
        return Payli(KontrolTuru.KapidanGecis, false, ayar.YuklemeHareketiPayiMm, olcum, "yükleme hareket");
    }

    /// <summary>
    /// Kasa dışına taşma (yön ve miktar). Taşma varsa sonuç "Teyit edilmedi"dir (izin kararı motorun değildir) ve
    /// kontrol listesinde gösterilmez; miktar aracın taşma alanlarında yazılır.
    /// </summary>
    private static AracKontrolu Tasma(Olculer olcu)
    {
        var parcalar = new List<string>();
        if (olcu.Sol > 0) parcalar.Add($"sol {Mm(olcu.Sol)}");
        if (olcu.Sag > 0) parcalar.Add($"sağ {Mm(olcu.Sag)}");
        if (olcu.On > 0) parcalar.Add($"ön {Mm(olcu.On)}");
        if (olcu.Arka > 0) parcalar.Add($"arka {Mm(olcu.Arka)}");
        if (parcalar.Count == 0)
            return new(KontrolTuru.Tasma, KontrolSonucu.Uygun, "Kasa dışına taşma yok");
        return new(KontrolTuru.Tasma, KontrolSonucu.TeyitEdilmedi, $"Kasa dışına taşma: {string.Join(", ", parcalar)}; izin gereksinimi teyit edilmelidir");
    }

    /// <summary>
    /// Yükten gelen dingil grubu ve king-pin payı (basit kiriş). Dara ve çekici dağılımı bilinmediği için sonuç ancak
    /// aşım varsa kesindir; aksi halde "Teyit edilmedi".
    /// </summary>
    private static IEnumerable<AracKontrolu> DingilVeBaglanti(AracYerlesimi arac, List<YukKonumu> konumlar, Dictionary<string, YerlesimYuku> yukler)
    {
        var veri = arac.Aday.TeknikVeri;
        if (!arac.DingilliMi)
        {
            yield return new(KontrolTuru.Dingil, KontrolSonucu.TeyitEdilmedi, "Konteyner: dingil yükü taşıyıcı şasi ve çekiciye bağlıdır; şasi verisi yok");
            yield break;
        }
        if (veri.KingPin is not { } kp || veri.Dingiller.Count == 0)
        {
            yield return new(KontrolTuru.Dingil, KontrolSonucu.TeyitEdilmedi, "Dingil veya king-pin konumu tanımlı değil; dingil yükü hesaplanamadı");
            yield break;
        }
        if (veri.Dingiller.Select(d => d.GrupNo).Distinct().Count() > 1)
        {
            // Birden çok dingil grubunda yük dağılımı tek mesnetle hesaplanamaz (eşit paylaşım varsayılmaz).
            yield return new(KontrolTuru.Dingil, KontrolSonucu.TeyitEdilmedi, "Birden çok dingil grubu var; grup yükleri bu sürümde hesaplanmadı");
            yield break;
        }
        var xa = (decimal)veri.Dingiller.Average(d => d.XMm);
        decimal dingil = 0, baglanti = 0;
        foreach (var k in konumlar)
        {
            var (d, b) = AracYerlesimi.Tepkiler(yukler[k.YukAnahtari].BrutKg, k.XMm + k.BoyMm / 2m, kp.XMm, xa);
            dingil += d;
            baglanti += b;
        }
        const string not = "dara ve çekici dağılımı bilinmediği için yalnız yükün payı";
        if (veri.Dingiller.All(d => d.KapasiteKg != null))
        {
            var kapasite = veri.Dingiller.Sum(d => (decimal)d.KapasiteKg!.Value);
            yield return dingil > kapasite
                ? new(KontrolTuru.Dingil, KontrolSonucu.Uygunsuz, $"Dingil grubuna yükten {Kg(dingil)}, kapasite {Kg(kapasite)}, aşım {Kg(dingil - kapasite)}")
                : new(KontrolTuru.Dingil, KontrolSonucu.TeyitEdilmedi, $"Dingil grubuna yükten {Kg(dingil)}, kapasite {Kg(kapasite)} ({not})");
        }
        else
        {
            yield return new(KontrolTuru.Dingil, KontrolSonucu.TeyitEdilmedi, $"Dingil grubuna yükten {Kg(dingil)}; dingil kapasitesi tanımlı değil");
        }
        if (baglanti < 0)
        {
            // Yükün payı eksi: dara king-pin'e yük bindirdiği için kesin sonuç verilemez.
            yield return new(KontrolTuru.BaglantiYuku, KontrolSonucu.TeyitEdilmedi, $"Yük dingil grubunun arkasına kayıyor; king-pin'den yükten {Kg(-baglanti)} kaldırma ({not})");
        }
        else if (kp.AzamiBaglantiYukuKg is { } bSinir)
        {
            yield return baglanti > bSinir
                ? new(KontrolTuru.BaglantiYuku, KontrolSonucu.Uygunsuz, $"King-pin'e yükten {Kg(baglanti)}, sınır {Kg(bSinir)}, aşım {Kg(baglanti - bSinir)}")
                : new(KontrolTuru.BaglantiYuku, KontrolSonucu.TeyitEdilmedi, $"King-pin'e yükten {Kg(baglanti)}, sınır {Kg(bSinir)} ({not})");
        }
        else
        {
            yield return new(KontrolTuru.BaglantiYuku, KontrolSonucu.TeyitEdilmedi, $"King-pin'e yükten {Kg(baglanti)}; bağlantı yükü sınırı tanımlı değil");
        }
    }

    /// <summary>
    /// Destek: istifte üstteki yük alttakine tamamen oturur (yerleşim kuralı). Yüzeyden taşan ya da özel yükte destek ve
    /// bağlama noktaları tanımlı olmadığı için "Teyit edilmedi".
    /// </summary>
    private static AracKontrolu Destek(AracYerlesimi arac, List<YukKonumu> konumlar, Dictionary<string, YerlesimYuku> yukler)
    {
        var yuzeyler = arac.Aday.TeknikVeri.Yuzeyler.ToDictionary(y => y.Kod);
        var tasan = new List<string>();
        foreach (var k in konumlar.Where(k => k.Kat == 1))
        {
            var y = yuzeyler[k.YuzeyKodu];
            var yBas = Math.Max(0, (arac.DisGenislik - y.GenislikMm) / 2);
            if (k.XMm < y.BaslangicXMm || k.XMm + k.BoyMm > y.BaslangicXMm + y.UzunlukMm || k.YMm < yBas || k.YMm + k.EnMm > yBas + y.GenislikMm)
                tasan.Add(yukler[k.YukAnahtari].Kod);
        }
        var ozel = konumlar.Select(k => yukler[k.YukAnahtari]).Where(y => y.OzelYuk).Select(y => y.Kod).Distinct().ToList();

        // Taban yükü: istifin toplam ağırlığı taban izine yayılır (yük yüzeye tam oturur). Sınır tanımlı değilse teyit gerekir.
        var asan = new List<string>();
        var sinirsiz = new HashSet<string>();
        var noktasal = new HashSet<string>();
        foreach (var istif in konumlar.GroupBy(k => k.IstifNo))
        {
            var taban = istif.Single(k => k.Kat == 1);
            var y = yuzeyler[taban.YuzeyKodu];
            if (y.NoktasalYukKg != null)
                noktasal.Add(y.Kod);
            if (y.TabanYukuKgM2 is not { } sinir)
            {
                sinirsiz.Add(y.Kod);
                continue;
            }
            var kg = istif.Sum(k => yukler[k.YukAnahtari].BrutKg);
            var basinc = kg / (taban.BoyMm * (decimal)taban.EnMm / 1_000_000m);
            if (basinc > sinir)
                asan.Add($"{yukler[taban.YukAnahtari].Kod} ({basinc:0} kg/m², sınır {sinir} kg/m²)");
        }
        if (asan.Count > 0)
            return new(KontrolTuru.Destek, KontrolSonucu.Uygunsuz, $"Taban yükü aşımı: {string.Join(", ", asan)}");

        var parcalar = new List<string>();
        if (tasan.Count > 0) parcalar.Add($"yüzeyden taşan yük: {string.Join(", ", tasan)}");
        if (ozel.Count > 0) parcalar.Add($"özel yükün destek ve bağlama şartları tanımlı değil: {string.Join(", ", ozel)}");
        if (sinirsiz.Count > 0) parcalar.Add($"taban yükü sınırı tanımlı değil ({string.Join(", ", sinirsiz)})");
        if (noktasal.Count > 0) parcalar.Add($"noktasal yük sınırı var, destek noktaları tanımlı olmadığı için hesaplanmadı ({string.Join(", ", noktasal)})");
        return parcalar.Count == 0
            ? new(KontrolTuru.Destek, KontrolSonucu.Uygun, "Bütün yükler yüzeye ya da alttaki yükün üst yüzeyine tam oturuyor; taban yükü sınır içinde")
            : new(KontrolTuru.Destek, KontrolSonucu.TeyitEdilmedi, string.Join("; ", parcalar));
    }
}
