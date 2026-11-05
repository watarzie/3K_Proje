using System.Globalization;
using ClosedXML.Excel;
using _3K.Core.Enums;
using _3K.Core.Models;
using _3K.YuklemeMotoru.Ekipman;

namespace _3K.Infrastructure.Services.YuklemePlanlama;

/// <summary>Excel'deki bir konfigürasyon: kimlik, model, kaynak ve teknik veri (çocuk sayfalardan birleştirilmiş).</summary>
internal sealed record EkipmanExcelKaydi(
    int Satir,
    string Kod,
    string Ad,
    string TipKodu,
    string Uretici,
    string ModelAdi,
    EkipmanKaynakTuru KaynakTuru,
    string? KaynakAciklama,
    DateTime? KaynakTarihi,
    string? Aciklama,
    EkipmanTeknikVerisi TeknikVeri);

internal sealed record EkipmanExcelOkumasi(IReadOnlyList<EkipmanExcelKaydi> Kayitlar, IReadOnlyList<EkipmanIceAktarimSorunu> Sorunlar);

/// <summary>Şablonun "Tipler" sayfasındaki satır.</summary>
internal sealed record EkipmanExcelTipi(string Kod, string Ad, string AileAdi, EkipmanKategorisi Kategori);

/// <summary>
/// Ekipman kitaplığının Excel biçimi: şablon, dışa aktarım ve içe aktarım aynı sayfa ve sütun tanımını kullanır.
/// 1. satır başlık, 2. satır birim/açıklama, veri 3. satırdan başlar. Uzunluklar tamsayı mm, ağırlıklar tamsayı kg'dır;
/// boş hücre "bilinmiyor" demektir ve tahminle doldurulmaz. Sorunlu satır sayfa ve satır numarasıyla bildirilir.
/// </summary>
internal static class EkipmanExcelDosyasi
{
    public const string KonfigurasyonSayfasi = "Konfigurasyonlar";
    public const string YuzeySayfasi = "Yuzeyler";
    public const string AciklikSayfasi = "Acikliklar";
    public const string EngelSayfasi = "Engeller";
    public const string DingilSayfasi = "Dingiller";
    public const string BaglamaSayfasi = "BaglamaNoktalari";
    public const string TipSayfasi = "Tipler";
    public const string AciklamaSayfasi = "Aciklama";

    private const int BaslikSatiri = 1;
    private const int IlkVeriSatiri = 3;
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    private sealed record Sutun(string Baslik, string Ipucu, bool Zorunlu = false);

    private static readonly Sutun[] KonfigurasyonSutunlari =
    [
        new("Kod", "Benzersiz kod (en fazla 50)", true), new("Ad", "Görünen ad", true), new("TipKodu", "Tipler sayfasından", true),
        new("Uretici", "Üretici veya 'Jenerik ISO'", true), new("Model", "Model adı", true),
        new("KaynakTuru", "ISO asgari / Taşıyıcı föyü / Üretici föyü / Sahada ölçüldü / Diğer", true),
        new("KaynakAciklama", "Belge adı, no, sayfa"), new("KaynakTarihi", "gg.aa.yyyy"),
        new("DisUzunlukMm", "mm"), new("DisGenislikMm", "mm"), new("DisYukseklikMm", "mm"),
        new("DaraKg", "kg"), new("TeknikKapasiteKg", "kg"), new("AzamiBrutKg", "kg"), new("UzamaEnFazlaMm", "mm"),
        new("KingPinXMm", "ön uçtan mm"), new("AzamiBaglantiYukuKg", "kg"), new("Aciklama", "Serbest metin")
    ];

    private static readonly Sutun[] YuzeySutunlari =
    [
        new("KonfigurasyonKodu", "Konfigurasyonlar.Kod", true), new("YuzeyKodu", "ör. ANA, BOYUN, HAVUZ", true), new("Ad", ""),
        new("BaslangicXMm", "ön uçtan mm", true), new("UzunlukMm", "mm", true), new("GenislikMm", "mm", true),
        new("YerdenYukseklikMm", "mm", true), new("KullanilabilirYukseklikMm", "iç yükseklik, mm"),
        new("TabanYukuKgM2", "kg/m²"), new("NoktasalYukKg", "kg")
    ];

    private static readonly Sutun[] AciklikSutunlari =
    [
        new("KonfigurasyonKodu", "Konfigurasyonlar.Kod", true), new("Taraf", "Arka / Ön / Sol / Sağ / Üst", true),
        new("BaslangicXMm", "yan ve üst için, mm"), new("UzunlukMm", "yan ve üst için, mm"),
        new("GenislikMm", "arka, ön ve üst için, mm"), new("YukseklikMm", "arka, ön ve yan için, mm")
    ];

    private static readonly Sutun[] EngelSutunlari =
    [
        new("KonfigurasyonKodu", "Konfigurasyonlar.Kod", true),
        new("Tur", "Dikme / Travers / Teker havuzu / Uç duvar / Kapak / Diğer", true),
        new("XMm", "mm", true), new("YMm", "soldan mm", true), new("ZMm", "yerden mm", true),
        new("UzunlukMm", "mm", true), new("GenislikMm", "mm", true), new("YukseklikMm", "mm", true),
        new("Sokulebilir", "Evet / Hayır"), new("Aciklama", "")
    ];

    private static readonly Sutun[] DingilSutunlari =
    [
        new("KonfigurasyonKodu", "Konfigurasyonlar.Kod", true), new("XMm", "ön uçtan mm", true),
        new("Tur", "Tahriksiz / Tahrikli / Pandül", true), new("GrupNo", "aynı gruptakiler yükü paylaşır", true),
        new("KapasiteKg", "kg")
    ];

    private static readonly Sutun[] BaglamaSutunlari =
    [
        new("KonfigurasyonKodu", "Konfigurasyonlar.Kod", true), new("XMm", "mm", true), new("YMm", "mm", true),
        new("ZMm", "mm", true), new("KapasiteDaN", "daN")
    ];

    private static readonly (EkipmanKaynakTuru Deger, string Ad)[] KaynakTurleri =
    [
        (EkipmanKaynakTuru.IsoAsgari, "ISO asgari"), (EkipmanKaynakTuru.TasiyiciFoyu, "Taşıyıcı föyü"),
        (EkipmanKaynakTuru.UreticiFoyu, "Üretici föyü"), (EkipmanKaynakTuru.SahadaOlculdu, "Sahada ölçüldü"),
        (EkipmanKaynakTuru.Diger, "Diğer")
    ];

    private static readonly (AciklikTarafi Deger, string Ad)[] Taraflar =
    [
        (AciklikTarafi.Arka, "Arka"), (AciklikTarafi.On, "Ön"), (AciklikTarafi.Sol, "Sol"), (AciklikTarafi.Sag, "Sağ"),
        (AciklikTarafi.Ust, "Üst")
    ];

    private static readonly (EngelTuru Deger, string Ad)[] EngelTurleri =
    [
        (EngelTuru.Dikme, "Dikme"), (EngelTuru.Travers, "Travers"), (EngelTuru.TekerHavuzu, "Teker havuzu"),
        (EngelTuru.UcDuvar, "Uç duvar"), (EngelTuru.Kapak, "Kapak"), (EngelTuru.Diger, "Diğer")
    ];

    private static readonly (DingilTuru Deger, string Ad)[] DingilTurleri =
    [
        (DingilTuru.Tahriksiz, "Tahriksiz"), (DingilTuru.Tahrikli, "Tahrikli"), (DingilTuru.Pandul, "Pandül")
    ];

    public static string KaynakTuruAdi(EkipmanKaynakTuru tur) => KaynakTurleri.First(k => k.Deger == tur).Ad;

    /// <summary>Şablonu yazar; kayıt verilirse (dışa aktarım) veri satırları da yazılır.</summary>
    public static byte[] Yaz(IReadOnlyList<EkipmanExcelTipi> tipler, IReadOnlyList<EkipmanExcelKaydi> kayitlar, string? kesilmeNotu = null)
    {
        using var wb = new XLWorkbook();
        var konf = SayfaKur(wb, KonfigurasyonSayfasi, KonfigurasyonSutunlari);
        var yuzey = SayfaKur(wb, YuzeySayfasi, YuzeySutunlari);
        var aciklik = SayfaKur(wb, AciklikSayfasi, AciklikSutunlari);
        var engel = SayfaKur(wb, EngelSayfasi, EngelSutunlari);
        var dingil = SayfaKur(wb, DingilSayfasi, DingilSutunlari);
        var baglama = SayfaKur(wb, BaglamaSayfasi, BaglamaSutunlari);
        int k = IlkVeriSatiri, y = IlkVeriSatiri, a = IlkVeriSatiri, e = IlkVeriSatiri, d = IlkVeriSatiri, b = IlkVeriSatiri;
        foreach (var kayit in kayitlar)
        {
            var v = kayit.TeknikVeri;
            SatirYaz(konf, k++, kayit.Kod, kayit.Ad, kayit.TipKodu, kayit.Uretici, kayit.ModelAdi, KaynakTuruAdi(kayit.KaynakTuru),
                kayit.KaynakAciklama, kayit.KaynakTarihi?.ToString("dd.MM.yyyy", Tr), v.DisOlcu?.UzunlukMm, v.DisOlcu?.GenislikMm,
                v.DisOlcu?.YukseklikMm, v.DaraKg, v.TeknikKapasiteKg, v.AzamiBrutKg, v.UzamaEnFazlaMm, v.KingPin?.XMm,
                v.KingPin?.AzamiBaglantiYukuKg, kayit.Aciklama);
            foreach (var s in v.Yuzeyler)
                SatirYaz(yuzey, y++, kayit.Kod, s.Kod, s.Ad, s.BaslangicXMm, s.UzunlukMm, s.GenislikMm, s.YerdenYukseklikMm,
                    s.KullanilabilirYukseklikMm, s.TabanYukuKgM2, s.NoktasalYukKg);
            foreach (var s in v.Acikliklar)
                SatirYaz(aciklik, a++, kayit.Kod, Taraflar.First(t => t.Deger == s.Taraf).Ad, s.BaslangicXMm, s.UzunlukMm, s.GenislikMm, s.YukseklikMm);
            foreach (var s in v.Engeller)
                SatirYaz(engel, e++, kayit.Kod, EngelTurleri.First(t => t.Deger == s.Tur).Ad, s.XMm, s.YMm, s.ZMm, s.UzunlukMm,
                    s.GenislikMm, s.YukseklikMm, s.Sokulebilir ? "Evet" : "Hayır", s.Aciklama);
            foreach (var s in v.Dingiller)
                SatirYaz(dingil, d++, kayit.Kod, s.XMm, DingilTurleri.First(t => t.Deger == s.Tur).Ad, s.GrupNo, s.KapasiteKg);
            foreach (var s in v.BaglamaNoktalari)
                SatirYaz(baglama, b++, kayit.Kod, s.XMm, s.YMm, s.ZMm, s.KapasiteDaN);
        }

        var tip = wb.Worksheets.Add(TipSayfasi);
        string[] tipBasliklari = ["TipKodu", "Ad", "Aile", "Kategori", "YuklemeYontemi", "SandikTasimayaUygun"];
        for (var i = 0; i < tipBasliklari.Length; i++)
            tip.Cell(BaslikSatiri, i + 1).Value = tipBasliklari[i];
        tip.Row(BaslikSatiri).Style.Font.Bold = true;
        var satir = BaslikSatiri + 1;
        foreach (var t in tipler)
            SatirYaz(tip, satir++, t.Kod, t.Ad, t.AileAdi, EkipmanKategoriKurali.Ad(t.Kategori),
                EkipmanKategoriKurali.YontemMetni(EkipmanKategoriKurali.YuklemeYontemleri(t.Kategori)),
                EkipmanKategoriKurali.SandikTasimayaUygunMu(t.Kategori) ? "Evet" : "Hayır");
        tip.Columns().AdjustToContents();

        var aciklama = wb.Worksheets.Add(AciklamaSayfasi);
        string[] satirlar =
        [
            "Ekipman kitaplığı içe aktarım şablonu",
            "Her konfigürasyon Konfigurasyonlar sayfasında bir satırdır; yüzey, açıklık, engel, dingil ve bağlama noktaları ilgili sayfalarda KonfigurasyonKodu ile bağlanır.",
            "1. satır başlıktır, 2. satır birim/açıklamadır; veri 3. satırdan başlar. Başlıkları değiştirmeyin.",
            "Uzunluklar tamsayı milimetre, ağırlıklar tamsayı kilogramdır. Koordinatlar: X ekipmanın ön ucundan arkaya, Y sol kenardan sağa, Z yerden yukarı.",
            "Bilinmeyen değeri boş bırakın; tahminle doldurmayın. Boş veriye dayanan kontrol 'Teyit edilmedi' olur.",
            "Var olan kod yeniden yüklenirse teknik verisi veya kaynağı değişen konfigürasyon yeni (doğrulanmamış) revizyon alır; değişmeyene dokunulmaz. Dosyada olmayan konfigürasyon silinmez.",
            "Dosyada bir sorun varsa hiçbir kayıt yazılmaz; önizleme sorunları sayfa ve satır numarasıyla listeler."
        ];
        for (var i = 0; i < satirlar.Length; i++)
            aciklama.Cell(i + 1, 1).Value = satirlar[i];
        aciklama.Cell(1, 1).Style.Font.Bold = true;
        if (kesilmeNotu != null)
        {
            var not = aciklama.Cell(satirlar.Length + 2, 1);
            not.Value = kesilmeNotu;
            not.Style.Font.Bold = true;
            not.Style.Font.FontColor = XLColor.DarkRed;
        }
        aciklama.Column(1).Width = 140;

        using var akis = new MemoryStream();
        wb.SaveAs(akis);
        return akis.ToArray();
    }

    /// <summary>Dosyayı okur; tip kodunun varlığı ve teknik veri denetimi çağıranındır.</summary>
    public static EkipmanExcelOkumasi Oku(Stream dosya, CancellationToken cancellationToken = default)
    {
        var sorunlar = new List<EkipmanIceAktarimSorunu>();
        XLWorkbook wb;
        try
        {
            wb = new XLWorkbook(dosya);
        }
        catch (Exception)
        {
            sorunlar.Add(new(KonfigurasyonSayfasi, null, null, "Dosya okunamadı; şablondaki .xlsx biçimini kullanın."));
            return new EkipmanExcelOkumasi([], sorunlar);
        }

        using (wb)
        {
            var konf = SayfaAc(wb, KonfigurasyonSayfasi, KonfigurasyonSutunlari, sorunlar, zorunlu: true);
            if (konf == null)
                return new EkipmanExcelOkumasi([], sorunlar);

            var basliklar = new List<(int Satir, string Kod, string Ad, string TipKodu, string Uretici, string Model, EkipmanKaynakTuru Kaynak,
                string? KaynakAciklama, DateTime? KaynakTarihi, string? Aciklama, Olcu? Dis, int? Dara, int? Kapasite, int? Brut, int? Uzama, KingPin? KingPin)>();
            var kodlar = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!SatirSiniriIcinde(konf, sorunlar))
                return new EkipmanExcelOkumasi([], sorunlar);
            foreach (var r in konf.Satirlar())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var hata = sorunlar.Count;
                var kod = EkipmanKitapligiKurali.KodNormalize(konf.Metin(r, "Kod"));
                if (!Zorunlu(konf, r, kod, "Kod", EkipmanKitapligiKurali.KodEnFazla, null, sorunlar) ||
                    !kodlar.Add(kod))
                {
                    if (kod.Length > 0 && sorunlar.Count == hata)
                        sorunlar.Add(new(konf.Ad, r, kod, $"'{kod}' kodu dosyada birden fazla kez geçiyor."));
                    continue;
                }
                var ad = konf.Metin(r, "Ad");
                var tipKodu = konf.Metin(r, "TipKodu");
                var uretici = konf.Metin(r, "Uretici");
                var model = konf.Metin(r, "Model");
                Zorunlu(konf, r, ad, "Ad", EkipmanKitapligiKurali.AdEnFazla, kod, sorunlar);
                Zorunlu(konf, r, tipKodu, "TipKodu", EkipmanKitapligiKurali.KodEnFazla, kod, sorunlar);
                Zorunlu(konf, r, uretici, "Uretici", EkipmanKitapligiKurali.UreticiEnFazla, kod, sorunlar);
                Zorunlu(konf, r, model, "Model", EkipmanKitapligiKurali.ModelEnFazla, kod, sorunlar);
                var kaynak = Etiket(konf, r, "KaynakTuru", KaynakTurleri, kod, sorunlar, zorunlu: true);
                var kaynakAciklama = Istege(konf, r, "KaynakAciklama", EkipmanKitapligiKurali.KaynakAciklamaEnFazla, kod, sorunlar);
                var aciklama = Istege(konf, r, "Aciklama", EkipmanKitapligiKurali.AciklamaEnFazla, kod, sorunlar);
                var tarih = Tarih(konf, r, "KaynakTarihi", kod, sorunlar);
                var disU = Tamsayi(konf, r, "DisUzunlukMm", kod, sorunlar);
                var disG = Tamsayi(konf, r, "DisGenislikMm", kod, sorunlar);
                var disY = Tamsayi(konf, r, "DisYukseklikMm", kod, sorunlar);
                Olcu? dis = null;
                if (disU is { } u && disG is { } g && disY is { } yk)
                    dis = new Olcu(u, g, yk);
                else if (disU != null || disG != null || disY != null)
                    sorunlar.Add(new(konf.Ad, r, kod, "Dış ölçünün uzunluk, genişlik ve yüksekliği birlikte girilmelidir."));
                var kingX = Tamsayi(konf, r, "KingPinXMm", kod, sorunlar);
                var kingYuk = Tamsayi(konf, r, "AzamiBaglantiYukuKg", kod, sorunlar);
                if (kingX == null && kingYuk != null)
                    sorunlar.Add(new(konf.Ad, r, kod, "Bağlantı yükü girilen satırda king-pin konumu da girilmelidir."));
                var dara = Tamsayi(konf, r, "DaraKg", kod, sorunlar);
                var kapasite = Tamsayi(konf, r, "TeknikKapasiteKg", kod, sorunlar);
                var brut = Tamsayi(konf, r, "AzamiBrutKg", kod, sorunlar);
                var uzama = Tamsayi(konf, r, "UzamaEnFazlaMm", kod, sorunlar);
                if (sorunlar.Count == hata)
                    basliklar.Add((r, kod, ad, tipKodu, uretici, model, kaynak!.Value, kaynakAciklama, tarih, aciklama, dis, dara, kapasite,
                        brut, uzama, kingX is { } x ? new KingPin(x, kingYuk) : null));
            }

            if (kodlar.Count > EkipmanKitapligiKurali.DosyaEnFazlaKonfigurasyon)
                sorunlar.Add(new(konf.Ad, null, null, $"Bir dosyada en fazla {EkipmanKitapligiKurali.DosyaEnFazlaKonfigurasyon} konfigürasyon olabilir."));

            var yuzeyler = CocukOku(wb, YuzeySayfasi, YuzeySutunlari, kodlar, sorunlar, cancellationToken, (s, r, kod) =>
            {
                var yuzeyKodu = s.Metin(r, "YuzeyKodu");
                if (!Zorunlu(s, r, yuzeyKodu, "YuzeyKodu", EkipmanKitapligiKurali.KodEnFazla, kod, sorunlar))
                    return null;
                var x = ZorunluTamsayi(s, r, "BaslangicXMm", kod, sorunlar);
                var l = ZorunluTamsayi(s, r, "UzunlukMm", kod, sorunlar);
                var w = ZorunluTamsayi(s, r, "GenislikMm", kod, sorunlar);
                var z = ZorunluTamsayi(s, r, "YerdenYukseklikMm", kod, sorunlar);
                var ic = Tamsayi(s, r, "KullanilabilirYukseklikMm", kod, sorunlar);
                var taban = Tamsayi(s, r, "TabanYukuKgM2", kod, sorunlar);
                var nokta = Tamsayi(s, r, "NoktasalYukKg", kod, sorunlar);
                var adi = Istege(s, r, "Ad", EkipmanKitapligiKurali.AdEnFazla, kod, sorunlar);
                return x is { } bx && l is { } bl && w is { } bw && z is { } bz
                    ? new YukYuzeyi(yuzeyKodu, adi, bx, bl, bw, bz, ic, taban, nokta)
                    : null;
            });
            var acikliklar = CocukOku(wb, AciklikSayfasi, AciklikSutunlari, kodlar, sorunlar, cancellationToken, (s, r, kod) =>
            {
                var taraf = Etiket(s, r, "Taraf", Taraflar, kod, sorunlar, zorunlu: true);
                var x = Tamsayi(s, r, "BaslangicXMm", kod, sorunlar);
                var l = Tamsayi(s, r, "UzunlukMm", kod, sorunlar);
                var w = Tamsayi(s, r, "GenislikMm", kod, sorunlar);
                var h = Tamsayi(s, r, "YukseklikMm", kod, sorunlar);
                return taraf is { } t ? new Aciklik(t, x, l, w, h) : null;
            });
            var engeller = CocukOku(wb, EngelSayfasi, EngelSutunlari, kodlar, sorunlar, cancellationToken, (s, r, kod) =>
            {
                var tur = Etiket(s, r, "Tur", EngelTurleri, kod, sorunlar, zorunlu: true);
                var x = ZorunluTamsayi(s, r, "XMm", kod, sorunlar);
                var yv = ZorunluTamsayi(s, r, "YMm", kod, sorunlar);
                var z = ZorunluTamsayi(s, r, "ZMm", kod, sorunlar);
                var l = ZorunluTamsayi(s, r, "UzunlukMm", kod, sorunlar);
                var w = ZorunluTamsayi(s, r, "GenislikMm", kod, sorunlar);
                var h = ZorunluTamsayi(s, r, "YukseklikMm", kod, sorunlar);
                var sokulebilir = Etiket(s, r, "Sokulebilir", [(true, "Evet"), (false, "Hayır")], kod, sorunlar, zorunlu: false) ?? false;
                var aciklama = Istege(s, r, "Aciklama", EkipmanKitapligiKurali.AciklamaEnFazla, kod, sorunlar);
                return tur is { } t && x is { } bx && yv is { } by && z is { } bz && l is { } bl && w is { } bw && h is { } bh
                    ? new Engel(t, bx, by, bz, bl, bw, bh, sokulebilir, aciklama)
                    : null;
            });
            var dingiller = CocukOku(wb, DingilSayfasi, DingilSutunlari, kodlar, sorunlar, cancellationToken, (s, r, kod) =>
            {
                var x = ZorunluTamsayi(s, r, "XMm", kod, sorunlar);
                var tur = Etiket(s, r, "Tur", DingilTurleri, kod, sorunlar, zorunlu: true);
                var grup = ZorunluTamsayi(s, r, "GrupNo", kod, sorunlar);
                var kapasite = Tamsayi(s, r, "KapasiteKg", kod, sorunlar);
                return x is { } bx && tur is { } t && grup is { } bg ? new Dingil(bx, t, bg, kapasite) : null;
            });
            var baglamalar = CocukOku(wb, BaglamaSayfasi, BaglamaSutunlari, kodlar, sorunlar, cancellationToken, (s, r, kod) =>
            {
                var x = ZorunluTamsayi(s, r, "XMm", kod, sorunlar);
                var yv = ZorunluTamsayi(s, r, "YMm", kod, sorunlar);
                var z = ZorunluTamsayi(s, r, "ZMm", kod, sorunlar);
                var kapasite = Tamsayi(s, r, "KapasiteDaN", kod, sorunlar);
                return x is { } bx && yv is { } by && z is { } bz ? new BaglamaNoktasi(bx, by, bz, kapasite) : null;
            });

            // Teknik veri kanonik sırayla kurulur: satır sırası değişti diye yeni revizyon yazılmaz.
            var kayitlar = basliklar.Select(k => new EkipmanExcelKaydi(
                k.Satir, k.Kod, k.Ad, k.TipKodu, k.Uretici, k.Model, k.Kaynak, k.KaynakAciklama, k.KaynakTarihi, k.Aciklama,
                new EkipmanTeknikVerisi
                {
                    DisOlcu = k.Dis,
                    DaraKg = k.Dara,
                    TeknikKapasiteKg = k.Kapasite,
                    AzamiBrutKg = k.Brut,
                    UzamaEnFazlaMm = k.Uzama,
                    KingPin = k.KingPin,
                    Yuzeyler = yuzeyler.GetValueOrDefault(k.Kod, []).OrderBy(s => s.BaslangicXMm).ThenBy(s => s.Kod, StringComparer.Ordinal).ToList(),
                    Acikliklar = acikliklar.GetValueOrDefault(k.Kod, []).OrderBy(s => s.Taraf).ThenBy(s => s.BaslangicXMm).ToList(),
                    Engeller = engeller.GetValueOrDefault(k.Kod, []).OrderBy(s => s.XMm).ThenBy(s => s.YMm).ThenBy(s => s.ZMm).ThenBy(s => s.Tur).ToList(),
                    Dingiller = dingiller.GetValueOrDefault(k.Kod, []).OrderBy(s => s.XMm).ToList(),
                    BaglamaNoktalari = baglamalar.GetValueOrDefault(k.Kod, []).OrderBy(s => s.XMm).ThenBy(s => s.YMm).ThenBy(s => s.ZMm).ToList()
                })).ToList();
            return new EkipmanExcelOkumasi(kayitlar, sorunlar);
        }
    }

    /// <summary>Başlıkları sütun numarasına eşlenmiş sayfa.</summary>
    private sealed class Sayfa(IXLWorksheet ws, Dictionary<string, int> sutunlar)
    {
        public string Ad => ws.Name;

        public IXLCell Hucre(int satir, string baslik) => ws.Cell(satir, sutunlar[baslik]);

        public string Metin(int satir, string baslik) => Hucre(satir, baslik).GetString().Trim();

        /// <summary>Son dolu satır; satırlar okunmadan önce sınır denetimi için.</summary>
        public int SonSatir => ws.LastRowUsed()?.RowNumber() ?? 0;

        public IEnumerable<int> Satirlar()
        {
            var son = SonSatir;
            for (var r = IlkVeriSatiri; r <= son; r++)
            {
                if (sutunlar.Values.Any(c => !ws.Cell(r, c).IsEmpty()))
                    yield return r;
            }
        }
    }

    private static IXLWorksheet SayfaKur(XLWorkbook wb, string ad, Sutun[] sutunlar)
    {
        var ws = wb.Worksheets.Add(ad);
        for (var i = 0; i < sutunlar.Length; i++)
        {
            ws.Cell(BaslikSatiri, i + 1).Value = sutunlar[i].Baslik + (sutunlar[i].Zorunlu ? " *" : string.Empty);
            ws.Cell(BaslikSatiri + 1, i + 1).Value = sutunlar[i].Ipucu;
        }
        ws.Row(BaslikSatiri).Style.Font.Bold = true;
        ws.Row(BaslikSatiri).Style.Fill.BackgroundColor = XLColor.FromHtml("#E8F0EA");
        ws.Row(BaslikSatiri + 1).Style.Font.Italic = true;
        ws.Row(BaslikSatiri + 1).Style.Font.FontColor = XLColor.Gray;
        ws.SheetView.FreezeRows(BaslikSatiri + 1);
        ws.Columns(1, sutunlar.Length).Width = 18;
        return ws;
    }

    private static void SatirYaz(IXLWorksheet ws, int satir, params object?[] degerler)
    {
        for (var i = 0; i < degerler.Length; i++)
        {
            var hucre = ws.Cell(satir, i + 1);
            switch (degerler[i])
            {
                case null: break;
                case int sayi: hucre.Value = sayi; break;
                case string metin: hucre.Value = metin; break;
                default: hucre.Value = Convert.ToString(degerler[i], Tr); break;
            }
        }
    }

    private static Sayfa? SayfaAc(XLWorkbook wb, string ad, Sutun[] sutunlar, List<EkipmanIceAktarimSorunu> sorunlar, bool zorunlu)
    {
        if (!wb.Worksheets.TryGetWorksheet(ad, out var ws))
        {
            if (zorunlu)
                sorunlar.Add(new(ad, null, null, $"'{ad}' sayfası bulunamadı; şablonu kullanın."));
            return null;
        }

        var basliklar = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var sonSutun = ws.Row(BaslikSatiri).LastCellUsed()?.Address.ColumnNumber ?? 0;
        for (var c = 1; c <= sonSutun; c++)
        {
            var baslik = ws.Cell(BaslikSatiri, c).GetString().Replace("*", string.Empty, StringComparison.Ordinal).Trim();
            if (baslik.Length > 0)
                basliklar.TryAdd(baslik, c);
        }
        var eksik = sutunlar.Where(s => !basliklar.ContainsKey(s.Baslik)).Select(s => s.Baslik).ToList();
        if (eksik.Count > 0)
        {
            sorunlar.Add(new(ad, BaslikSatiri, null, $"Sütun bulunamadı: {string.Join(", ", eksik)}. Başlıkları değiştirmeyin."));
            return null;
        }
        return new Sayfa(ws, basliklar);
    }

    /// <summary>Sayfa taranacak satır sınırını aşıyorsa sorun ekler ve sayfa okunmaz.</summary>
    private static bool SatirSiniriIcinde(Sayfa sayfa, List<EkipmanIceAktarimSorunu> sorunlar)
    {
        if (sayfa.SonSatir - IlkVeriSatiri + 1 <= EkipmanKitapligiKurali.SayfaEnFazlaSatir)
            return true;
        sorunlar.Add(new(sayfa.Ad, null, null,
            $"Sayfada en fazla {EkipmanKitapligiKurali.SayfaEnFazlaSatir} veri satırı olabilir; boş satırların altındaki hücreleri temizleyin."));
        return false;
    }

    /// <summary>Çocuk sayfayı konfigürasyon koduna göre gruplar; bilinmeyen kod ve hatalı satır sorun olur.</summary>
    private static Dictionary<string, List<T>> CocukOku<T>(
        XLWorkbook wb, string ad, Sutun[] sutunlar, IReadOnlySet<string> kodlar, List<EkipmanIceAktarimSorunu> sorunlar,
        CancellationToken cancellationToken, Func<Sayfa, int, string, T?> oku) where T : class
    {
        var sonuc = new Dictionary<string, List<T>>(StringComparer.OrdinalIgnoreCase);
        var sayfa = SayfaAc(wb, ad, sutunlar, sorunlar, zorunlu: false);
        if (sayfa == null || !SatirSiniriIcinde(sayfa, sorunlar))
            return sonuc;
        foreach (var r in sayfa.Satirlar())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var kod = EkipmanKitapligiKurali.KodNormalize(sayfa.Metin(r, "KonfigurasyonKodu"));
            if (!kodlar.Contains(kod))
            {
                sorunlar.Add(new(ad, r, kod.Length == 0 ? null : kod,
                    kod.Length == 0 ? "KonfigurasyonKodu boş." : $"'{kod}' kodu Konfigurasyonlar sayfasında yok."));
                continue;
            }
            if (oku(sayfa, r, kod) is { } deger)
            {
                if (!sonuc.TryGetValue(kod, out var liste))
                    sonuc[kod] = liste = [];
                liste.Add(deger);
            }
        }
        return sonuc;
    }

    private static bool Zorunlu(Sayfa s, int r, string deger, string baslik, int enFazla, string? kod, List<EkipmanIceAktarimSorunu> sorunlar)
    {
        if (deger.Length == 0)
        {
            sorunlar.Add(new(s.Ad, r, kod, $"{baslik} zorunludur."));
            return false;
        }
        if (deger.Length > enFazla)
        {
            sorunlar.Add(new(s.Ad, r, kod, $"{baslik} en fazla {enFazla} karakter olabilir."));
            return false;
        }
        return true;
    }

    private static string? Istege(Sayfa s, int r, string baslik, int enFazla, string? kod, List<EkipmanIceAktarimSorunu> sorunlar)
    {
        var deger = s.Metin(r, baslik);
        if (deger.Length > enFazla)
            sorunlar.Add(new(s.Ad, r, kod, $"{baslik} en fazla {enFazla} karakter olabilir."));
        return deger.Length == 0 ? null : deger;
    }

    private static int? ZorunluTamsayi(Sayfa s, int r, string baslik, string? kod, List<EkipmanIceAktarimSorunu> sorunlar)
    {
        var deger = Tamsayi(s, r, baslik, kod, sorunlar, out var gecersiz);
        if (deger == null && !gecersiz)
            sorunlar.Add(new(s.Ad, r, kod, $"{baslik} zorunludur."));
        return deger;
    }

    private static int? Tamsayi(Sayfa s, int r, string baslik, string? kod, List<EkipmanIceAktarimSorunu> sorunlar) =>
        Tamsayi(s, r, baslik, kod, sorunlar, out _);

    /// <summary>Tamsayı hücre: sayı veya metin ("1.400" ya da "1400"); ondalıklı değer kabul edilmez (birim mm/kg).</summary>
    private static int? Tamsayi(Sayfa s, int r, string baslik, string? kod, List<EkipmanIceAktarimSorunu> sorunlar, out bool gecersiz)
    {
        gecersiz = false;
        var hucre = s.Hucre(r, baslik);
        if (hucre.IsEmpty())
            return null;
        if (hucre.DataType == XLDataType.Number)
        {
            var sayi = hucre.GetDouble();
            if (sayi == Math.Floor(sayi) && sayi is >= int.MinValue and <= int.MaxValue)
                return (int)sayi;
        }
        else
        {
            var metin = hucre.GetString().Trim();
            if (metin.Length == 0)
                return null;
            if (int.TryParse(metin, NumberStyles.AllowThousands | NumberStyles.AllowLeadingSign, Tr, out var deger))
                return deger;
        }
        gecersiz = true;
        sorunlar.Add(new(s.Ad, r, kod, $"{baslik} tamsayı olmalıdır (mm veya kg)."));
        return null;
    }

    private static DateTime? Tarih(Sayfa s, int r, string baslik, string? kod, List<EkipmanIceAktarimSorunu> sorunlar)
    {
        var hucre = s.Hucre(r, baslik);
        if (hucre.IsEmpty())
            return null;
        if (hucre.DataType == XLDataType.DateTime)
            return hucre.GetDateTime().Date;
        if (DateTime.TryParseExact(hucre.GetString().Trim(), ["dd.MM.yyyy", "d.M.yyyy"], Tr, DateTimeStyles.None, out var tarih))
            return tarih;
        sorunlar.Add(new(s.Ad, r, kod, $"{baslik} gg.aa.yyyy biçiminde olmalıdır."));
        return null;
    }

    /// <summary>Türkçe harfleri ASCII karşılığına indirip küçültür; Excel'de "Sag", "SAĞ" ve "sağ" aynı etikettir.</summary>
    private static string Sadelestir(string metin)
    {
        var kucuk = metin.Trim().ToLower(Tr);
        return string.Create(kucuk.Length, kucuk, (hedef, kaynak) =>
        {
            for (var i = 0; i < kaynak.Length; i++)
            {
                hedef[i] = kaynak[i] switch
                {
                    'ç' => 'c', 'ğ' => 'g', 'ı' => 'i', 'ö' => 'o', 'ş' => 's', 'ü' => 'u',
                    var c => c
                };
            }
        });
    }

    /// <summary>Etiketli değer: Türkçe ad büyük/küçük harf ve Türkçe harf duyarsız eşlenir ("Sag" = "Sağ").</summary>
    private static TDeger? Etiket<TDeger>(
        Sayfa s, int r, string baslik, (TDeger Deger, string Ad)[] etiketler, string? kod, List<EkipmanIceAktarimSorunu> sorunlar,
        bool zorunlu) where TDeger : struct
    {
        var metin = s.Metin(r, baslik);
        if (metin.Length == 0)
        {
            if (zorunlu)
                sorunlar.Add(new(s.Ad, r, kod, $"{baslik} zorunludur ({string.Join(" / ", etiketler.Select(e => e.Ad))})."));
            return null;
        }
        var sade = Sadelestir(metin);
        foreach (var (deger, ad) in etiketler)
        {
            if (string.Equals(sade, Sadelestir(ad), StringComparison.Ordinal))
                return deger;
        }
        sorunlar.Add(new(s.Ad, r, kod, $"{baslik} '{metin}' tanınmadı ({string.Join(" / ", etiketler.Select(e => e.Ad))})."));
        return null;
    }
}
