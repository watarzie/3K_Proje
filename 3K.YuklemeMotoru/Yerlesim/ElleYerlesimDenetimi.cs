using System.Globalization;

namespace _3K.YuklemeMotoru.Yerlesim;

/// <summary>
/// Elle yerleşim denetimi (Simülasyon isterleri 20–22). Kullanıcının araçlara koyduğu yükler kabul edilir ama hiçbir
/// kontrol atlanmaz: otomatik yerleşimin araç kontrolleri aynen çalışır, ayrıca yük düzeyinde çakışma, kasa sınırı,
/// yükseklik, destek (havada / kısmi oturma), istif izni, üst yük, döndürme izni, giriş açıklığı, engel ve zorunlu
/// ekipman denetlenir. Sorunlu yerleşim kaydedilebilir; sonuç "Uygunsuz" olur ve raporlar taslak basılır.
/// </summary>
public static class ElleYerlesimDenetimi
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
    private const int Tol = AracYerlesimi.ElleTolerans;

    private static string Mm(int mm) => mm.ToString("#,0", Tr) + " mm";
    private static string Kg(decimal kg) => kg.ToString("#,0", Tr) + " kg";

    /// <summary>Yerleşmiş yük ve konumu (döndürmeyle ölçü).</summary>
    private sealed record Yer(YerlesimYuku Yuk, int X, int Y, int Z, int Boy, int En, bool Donduruldu)
    {
        public int H => Yuk.YukseklikMm;
        public int Ust => Z + H;
        public bool Ortusur(Yer b) => X < b.X + b.Boy && b.X < X + Boy && Y < b.Y + b.En && b.Y < Y + En;
        public bool IcindeMi(Yer b) => X >= b.X - Tol && X + Boy <= b.X + b.Boy + Tol && Y >= b.Y - Tol && Y + En <= b.Y + b.En + Tol;
        // Z'de istif kurulurken kabul edilen tolerans çakışmada da kullanılır (1–2 mm gömülme hem istif hem çakışma sayılmaz).
        public bool Kesisir(Yer b) => Ortusur(b) && Z < b.Ust - Tol && b.Z < Ust - Tol;
    }

    public static YerlesimSonucu Denetle(YerlesimGirdisi girdi, IReadOnlyList<ElleArac> araclar)
    {
        var yukler = girdi.Yukler.ToDictionary(y => y.Anahtar);
        var adaylar = girdi.Adaylar.ToDictionary(a => a.KonfigurasyonId);
        var kullanilan = new HashSet<string>(StringComparer.Ordinal);
        var sonuclar = new List<YerlesimAraci>();
        var yerlesimler = new List<AracYerlesimi>();
        var genelUyarilar = new List<string>();

        foreach (var elle in araclar)
        {
            if (!adaylar.TryGetValue(elle.KonfigurasyonId, out var aday) || aday.TeknikVeri.Yuzeyler.Count == 0)
            {
                genelUyarilar.Add($"Araç {sonuclar.Count + 1}: ekipman konfigürasyonu {elle.KonfigurasyonId} bulunamadı ya da yükleme yüzeyi yok; araç değerlendirilmedi.");
                continue;
            }
            var arac = new AracYerlesimi(aday, girdi.Ayarlar, girdi.Yasal);
            var sorunlar = new List<YukSorunu>();
            var yerler = new List<Yer>();
            foreach (var k in elle.Konumlar)
            {
                if (!yukler.TryGetValue(k.YukAnahtari, out var yuk) || !kullanilan.Add(k.YukAnahtari))
                {
                    genelUyarilar.Add($"Araç {sonuclar.Count + 1}: '{k.YukAnahtari}' yükü planda yok ya da birden fazla kez yerleştirildi; yok sayıldı.");
                    continue;
                }
                yerler.Add(YerKur(yuk, k));
                if (k.Donduruldu && !yuk.Dondurulebilir)
                    sorunlar.Add(new(yuk.Anahtar, KontrolSonucu.Uygunsuz, "Tabanda 90° döndürme izni yok", KontrolTuru.ElleYerlesim));
            }

            var (istifler, destekSorunlari) = IstifleriKur(arac, yerler);
            sorunlar.AddRange(destekSorunlari);
            sorunlar.AddRange(YuzeyVeYukseklik(arac, yerler));
            sorunlar.AddRange(Cakismalar(arac, yerler, girdi.Ayarlar.YerlesimBosluguMm));
            sorunlar.AddRange(IstifKurallari(istifler));
            foreach (var istif in istifler)
            {
                if (IstifEkle(arac, istif) != GirisYonu.Yok)
                    continue;
                var taban = istif[0];
                var yukseklik = istif[^1].Ust - taban.Z;
                foreach (var y in istif)
                    sorunlar.Add(new(y.Yuk.Anahtar, KontrolSonucu.Uygunsuz, istif.Count > 1
                        ? $"Girişten geçemiyor: istif grubu {Mm(taban.Boy)} × {Mm(taban.En)} × {Mm(yukseklik)} açıklıktan geçmiyor (içeride istif yapılmaz)"
                        : $"Girişten geçemiyor: {Mm(taban.Boy)} × {Mm(taban.En)} × {Mm(yukseklik)} açıklıktan geçmiyor", KontrolTuru.KapidanGecis));
            }
            // Zorunlu özel yük yalnız özel yük ekipmanında taşınır (isterler 10.1, kabul 607).
            foreach (var y in yerler.Where(y => y.Yuk.Zorunluluk == OzelEkipmanZorunlulugu.Her && !aday.OzelYukEkipmani))
                sorunlar.Add(new(y.Yuk.Anahtar, KontrolSonucu.Uygunsuz,
                    $"Yanlış ekipman: {y.Yuk.Kod} {(y.Yuk.YukTuruAdi ?? "özel yük").ToLower(Tr)} uygun lowbed / özel yük ekipmanı gerektirir", KontrolTuru.ElleYerlesim));

            var zorunlu = yerler.Any(y => y.Yuk.Zorunluluk == OzelEkipmanZorunlulugu.Her) && aday.OzelYukEkipmani;
            var gerekce = aday.AileIci ? null
                : zorunlu ? $"Zorunlu özel ekipman: {string.Join(", ", yerler.Where(y => y.Yuk.Zorunluluk == OzelEkipmanZorunlulugu.Her).Select(y => y.Yuk.Kod))} için {aday.Kod} (elle)."
                : $"Aile dışı ekipman elle eklendi: {aday.Kod}.";
            var sonuc = AracKontrolcu.Olustur(sonuclar.Count + 1, arac, girdi.Ayarlar, girdi.Yasal, gerekce, zorunlu, ortala: false);

            var ekKontroller = new List<AracKontrolu> { Organizasyon(girdi, yerler) };
            var yukSonucu = AracKontrolcu.EnKotu(sorunlar.Select(s => s.Sonuc));
            ekKontroller.Add(sorunlar.Count == 0
                ? new(KontrolTuru.ElleYerlesim, KontrolSonucu.Uygun, "Elle yerleşimde çakışma, destek, istif ve giriş sorunu yok")
                : new(KontrolTuru.ElleYerlesim, yukSonucu, $"{sorunlar.Select(s => s.YukAnahtari).Distinct().Count()} yükte {sorunlar.Count} sorun; yük listesine bakın"));
            var kodlar = yerler.ToDictionary(y => y.Yuk.Anahtar, y => y.Yuk.Kod);
            var kontroller = ekKontroller.Concat(Birlestir(sonuc.Kontroller, sorunlar, kodlar)).ToList();
            sonuclar.Add(sonuc with
            {
                Kontroller = kontroller,
                Sonuc = AracKontrolcu.EnKotu(kontroller.Select(x => x.Sonuc)),
                YukSorunlari = sorunlar
            });
            yerlesimler.Add(arac);
        }

        // Hiçbir araca konmayan yükler bekleyenler listesindedir.
        var bekleyenler = girdi.Yukler.Where(y => !kullanilan.Contains(y.Anahtar))
            .Select(y => new YerlesemeyenYuk(y.Anahtar, y.Kod, "Bekleyenler listesinde (elle yerleştirilmedi)")).ToList();
        var uyarilar = YerlesimMotoru.Uyarilar(girdi, yerlesimler, sonuclar, null);
        uyarilar.InsertRange(0, genelUyarilar);
        uyarilar.Insert(0, "Yerleşim elle düzenlendi; kontroller elle konumlar üzerinden yapıldı.");
        if (bekleyenler.Count > 0)
            uyarilar.Insert(1, $"{bekleyenler.Count} yük bekleyenler listesinde; plan eksiktir.");
        var genel = bekleyenler.Count > 0 ? KontrolSonucu.Uygunsuz : AracKontrolcu.EnKotu(sonuclar.Select(a => a.Sonuc));
        if (sonuclar.Count == 0 && genel == KontrolSonucu.Uygun)
            genel = KontrolSonucu.TeyitEdilmedi;
        return new YerlesimSonucu
        {
            Araclar = sonuclar,
            Yerlesemeyenler = bekleyenler,
            Uyarilar = uyarilar,
            Sonuc = genel,
            ElleDuzenlendi = true
        };
    }

    private static Yer YerKur(YerlesimYuku yuk, ElleKonum k)
    {
        var (boy, en) = k.Donduruldu ? (yuk.EnMm, yuk.BoyMm) : (yuk.BoyMm, yuk.EnMm);
        return new Yer(yuk, k.XMm, k.YMm, k.ZMm, boy, en, k.Donduruldu);
    }

    /// <summary>
    /// İstifi araca ekler (taban yüzeyi; desteksizse en yakın yüzey). Giriş yoksa yöntem "Yok" yazılır (raporda "-");
    /// bileşik bayrak yöntem gibi okunmasın. Giriş yönünü döndürür.
    /// </summary>
    private static GirisYonu IstifEkle(AracYerlesimi arac, List<Yer> istif)
    {
        var taban = istif[0];
        var yuzey = arac.TabanYuzeyi(taban.X, taban.Y, taban.Z, taban.Boy, taban.En) ?? arac.EnYakinYuzey(taban.X, taban.Z);
        var giris = arac.Giris(taban.Boy, taban.En, istif[^1].Ust - taban.Z, taban.X);
        arac.ElleIstifEkle(yuzey, giris, istif.Select(y => (y.Yuk, y.X, y.Y, y.Z, y.Boy, y.En, y.Donduruldu)).ToList());
        return giris;
    }

    /// <summary>
    /// Elle konumlardan aracı kurar (istifler sabit); "kalanı otomatik diz" bu yüklerin etrafına yerleştirir. Planda
    /// olmayan yük atlanır; kontrol yapılmaz (sonuç ayrıca <see cref="Denetle"/> ile denetlenir).
    /// </summary>
    internal static AracYerlesimi SabitAracKur(YerlesimGirdisi girdi, EkipmanAdayi aday, ElleArac elle, IReadOnlyDictionary<string, YerlesimYuku> yukler)
    {
        var arac = new AracYerlesimi(aday, girdi.Ayarlar, girdi.Yasal);
        var yerler = elle.Konumlar.Where(k => yukler.ContainsKey(k.YukAnahtari)).Select(k => YerKur(yukler[k.YukAnahtari], k)).ToList();
        foreach (var istif in IstifleriKur(arac, yerler).Istifler)
            IstifEkle(arac, istif);
        return arac;
    }

    /// <summary>
    /// Araç kontrolleri istifler üzerinden yapılır ve yük düzeyindeki elle sorunları görmez (havadaki yük kendi istifinin
    /// tabanı sayılır, kapıdan geçmeyen istif yine eklenir). Aynı türdeki yük sorunu kontrolü kötüleştirir; basılan
    /// raporda yük sorunu varken aynı kontrol "Uygun" görünmez.
    /// </summary>
    private static IEnumerable<AracKontrolu> Birlestir(
        IReadOnlyList<AracKontrolu> kontroller, List<YukSorunu> sorunlar, Dictionary<string, string> kodlar) =>
        kontroller.Select(k =>
        {
            var ilgili = sorunlar.Where(s => s.Tur == k.Tur).ToList();
            var en = AracKontrolcu.EnKotu(ilgili.Select(s => s.Sonuc).Append(k.Sonuc));
            if (ilgili.Count == 0 || en == k.Sonuc)
                return k;
            var yukler = ilgili.Select(s => kodlar.GetValueOrDefault(s.YukAnahtari, s.YukAnahtari)).Distinct().ToList();
            return k with
            {
                Sonuc = en,
                Mesaj = $"Elle yerleşimde {yukler.Count} yükte sorun ({string.Join(", ", yukler.Take(5))}{(yukler.Count > 5 ? ", …" : string.Empty)}): {ilgili.First(s => s.Sonuc == en).Mesaj}"
            };
        });

    /// <summary>
    /// Destek: tabanı yüzeyde olan yük istif başlatır; üstteki yük, üst yüzeyi kendi tabanına denk gelen tek bir yükün
    /// içine tam oturmalıdır (eşit dağılım ve köprüleme varsayılmaz). Aksi hâlde desteksiz ya da yetersiz destek.
    /// </summary>
    private static (List<List<Yer>> Istifler, List<YukSorunu> Sorunlar) IstifleriKur(AracYerlesimi arac, List<Yer> yerler)
    {
        var sorunlar = new List<YukSorunu>();
        var istifi = new Dictionary<Yer, List<Yer>>();
        var istifler = new List<List<Yer>>();
        foreach (var y in yerler.OrderBy(y => y.Z).ThenBy(y => y.X).ThenBy(y => y.Y))
        {
            if (arac.TabanYuzeyi(y.X, y.Y, y.Z, y.Boy, y.En) != null)
            {
                var yeni = new List<Yer> { y };
                istifler.Add(yeni);
                istifi[y] = yeni;
                continue;
            }
            var altlar = yerler.Where(b => b != y && Math.Abs(b.Ust - y.Z) <= Tol && b.Ortusur(y)).ToList();
            if (altlar.Count == 1 && y.IcindeMi(altlar[0]) && istifi.TryGetValue(altlar[0], out var alttakiIstif) && alttakiIstif[^1] == altlar[0])
            {
                alttakiIstif.Add(y);
                istifi[y] = alttakiIstif;
                continue;
            }
            sorunlar.Add(new(y.Yuk.Anahtar, KontrolSonucu.Uygunsuz, altlar.Count == 0
                ? $"Desteksiz: yük havada (Z {Mm(y.Z)}); yüzeye ya da başka bir yüke oturmuyor"
                : altlar.Count > 1
                    ? $"Yetersiz destek: birden çok yüke oturuyor ({string.Join(", ", altlar.Select(a => a.Yuk.Kod))}); köprüleme kabul edilmez"
                    : !y.IcindeMi(altlar[0])
                        ? $"Yetersiz destek: {altlar[0].Yuk.Kod} üzerine tam oturmuyor (taşan kısım var)"
                        : $"Yetersiz destek: {altlar[0].Yuk.Kod} üstünde zaten başka yük var; bir yükün üstüne tek yük oturur",
                KontrolTuru.Destek));
            var tek = new List<Yer> { y };
            istifler.Add(tek);
            istifi[y] = tek;
        }
        return (istifler, sorunlar);
    }

    /// <summary>Kasa sınırı (duvarlı gövdede yüzey dışına taşma; yön ve miktar) ve iç yükseklik aşımı.</summary>
    private static IEnumerable<YukSorunu> YuzeyVeYukseklik(AracYerlesimi arac, List<Yer> yerler)
    {
        foreach (var y in yerler)
        {
            var yuzey = arac.TabanYuzeyi(y.X, y.Y, y.Z, y.Boy, y.En);
            var olcuYuzeyi = yuzey ?? arac.EnYakinYuzey(y.X, y.Z);
            if (yuzey != null)
            {
                var tasmalar = new List<string>();
                if (y.X < yuzey.XBas) tasmalar.Add($"ön {Mm(yuzey.XBas - y.X)}");
                if (y.X + y.Boy > yuzey.XBas + yuzey.Uzunluk) tasmalar.Add($"arka {Mm(y.X + y.Boy - yuzey.XBas - yuzey.Uzunluk)}");
                if (y.Y < yuzey.YBas) tasmalar.Add($"sol {Mm(yuzey.YBas - y.Y)}");
                if (y.Y + y.En > yuzey.YBas + yuzey.Genislik) tasmalar.Add($"sağ {Mm(y.Y + y.En - yuzey.YBas - yuzey.Genislik)}");
                // Açık ekipmanda taşma teyit gerektirir; ağırlık merkezi yüzey dışındaysa yük devrilir (kısmi oturma), uygunsuzdur.
                var merkezDisarida = y.X + y.Boy / 2.0 < yuzey.XBas || y.X + y.Boy / 2.0 > yuzey.XBas + yuzey.Uzunluk ||
                                     y.Y + y.En / 2.0 < yuzey.YBas || y.Y + y.En / 2.0 > yuzey.YBas + yuzey.Genislik;
                if (tasmalar.Count > 0)
                    yield return arac.Aday.Duvarli
                        ? new(y.Yuk.Anahtar, KontrolSonucu.Uygunsuz, $"Kasa sınırı aşımı: {string.Join(", ", tasmalar)}", KontrolTuru.Tasma)
                        : merkezDisarida
                            ? new(y.Yuk.Anahtar, KontrolSonucu.Uygunsuz, $"Kısmi oturma: ağırlık merkezi yükleme yüzeyinin dışında ({string.Join(", ", tasmalar)})", KontrolTuru.Tasma)
                            : new(y.Yuk.Anahtar, KontrolSonucu.TeyitEdilmedi, $"Yükleme yüzeyinden taşıyor: {string.Join(", ", tasmalar)}", KontrolTuru.Tasma);
            }
            if (arac.Aday.Duvarli && olcuYuzeyi.IcYukseklik is { } ic && y.Ust > olcuYuzeyi.Z + ic)
                yield return new(y.Yuk.Anahtar, KontrolSonucu.Uygunsuz,
                    $"Yükseklik aşımı: üst kot {Mm(y.Ust - olcuYuzeyi.Z)}, iç yükseklik {Mm(ic)}, aşım {Mm(y.Ust - olcuYuzeyi.Z - ic)}", KontrolTuru.IcYukseklik);
        }
    }

    /// <summary>Çakışma (ilgili yük kodları), yerleşim boşluğunun altında kalma ve sökülemeyen engelle çakışma.</summary>
    private static IEnumerable<YukSorunu> Cakismalar(AracYerlesimi arac, List<Yer> yerler, int bosluk)
    {
        for (var i = 0; i < yerler.Count; i++)
        {
            var a = yerler[i];
            var cakisan = new List<string>();
            var yakin = new List<string>();
            for (var j = 0; j < yerler.Count; j++)
            {
                if (i == j)
                    continue;
                var b = yerler[j];
                if (a.Kesisir(b))
                    cakisan.Add(b.Yuk.Kod);
                else if (a.Z < b.Ust && b.Z < a.Ust && Mesafe(a, b) < bosluk)
                    yakin.Add(b.Yuk.Kod);
            }
            if (cakisan.Count > 0)
                yield return new(a.Yuk.Anahtar, KontrolSonucu.Uygunsuz, $"Çakışma: {string.Join(", ", cakisan)}");
            else if (yakin.Count > 0)
                yield return new(a.Yuk.Anahtar, KontrolSonucu.TeyitEdilmedi, $"Yerleşim boşluğu ({Mm(bosluk)}) altında: {string.Join(", ", yakin)}");
            var kutu = new AracYerlesimi.Kutu(a.X, a.Y, a.Z, a.Boy, a.En, a.H);
            if (arac.SokulemezEngeller.Any(e => e.Kesisir(kutu, 0)))
                yield return new(a.Yuk.Anahtar, KontrolSonucu.Uygunsuz, "Sökülemeyen araç engeliyle çakışıyor");
        }
    }

    /// <summary>Yan yana iki kutunun yatay boşluğu (örtüşüyorsa 0).</summary>
    private static int Mesafe(Yer a, Yer b)
    {
        var dx = Math.Max(0, Math.Max(b.X - (a.X + a.Boy), a.X - (b.X + b.Boy)));
        var dy = Math.Max(0, Math.Max(b.Y - (a.Y + a.En), a.Y - (b.Y + b.En)));
        return Math.Max(dx, dy);
    }

    /// <summary>İstif izni (alttaki altta kalabilmeli, üstteki üste konabilmeli) ve her katın üst yük sınırı.</summary>
    private static IEnumerable<YukSorunu> IstifKurallari(List<List<Yer>> istifler)
    {
        foreach (var istif in istifler.Where(i => i.Count > 1))
        {
            for (var i = 0; i < istif.Count; i++)
            {
                var y = istif[i];
                if (i > 0 && !y.Yuk.UsteKonabilir)
                    yield return new(y.Yuk.Anahtar, KontrolSonucu.Uygunsuz, "İstif izni ihlali: bu yük başka yükün üstüne konamaz");
                if (i < istif.Count - 1)
                {
                    if (!y.Yuk.AltaKalabilir)
                        yield return new(y.Yuk.Anahtar, KontrolSonucu.Uygunsuz, "İstif izni ihlali: bu yükün üstüne yük konamaz");
                    var ustYuk = istif.Skip(i + 1).Sum(x => x.Yuk.BrutKg);
                    if (y.Yuk.UstYukSiniriKg is not { } sinir)
                        yield return new(y.Yuk.Anahtar, KontrolSonucu.TeyitEdilmedi, $"Üst yük {Kg(ustYuk)}; üst yük sınırı tanımlı değil");
                    else if (ustYuk > sinir)
                        yield return new(y.Yuk.Anahtar, KontrolSonucu.Uygunsuz, $"Üst yük sınırı aşımı: üstündeki {Kg(ustYuk)}, sınır {Kg(sinir)}");
                }
            }
        }
    }

    /// <summary>Proje bazında ayrı / lokasyona göre ayrı tercihine uyum (isterler 24–25).</summary>
    private static AracKontrolu Organizasyon(YerlesimGirdisi girdi, List<Yer> yerler)
    {
        var sorunlar = new List<string>();
        if (girdi.ProjeBazindaAyri && yerler.Select(y => y.Yuk.ProjeId).Distinct().Count() > 1)
            sorunlar.Add($"proje bazında ayrı yükleme seçili ama araçta {yerler.Select(y => y.Yuk.ProjeNo ?? "projesiz").Distinct().Count()} proje var");
        if (girdi.LokasyonaGoreAyri && yerler.Select(y => y.Yuk.LokasyonId).Distinct().Count() > 1)
            sorunlar.Add("lokasyona göre ayrı planlama seçili ama araçta farklı lokasyonlardan yük var");
        return sorunlar.Count == 0
            ? new(KontrolTuru.Organizasyon, KontrolSonucu.Uygun, "Proje ve lokasyon tercihine uygun")
            : new(KontrolTuru.Organizasyon, KontrolSonucu.Uygunsuz, string.Join("; ", sorunlar));
    }
}
