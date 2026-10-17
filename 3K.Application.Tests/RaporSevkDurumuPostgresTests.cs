using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Services.Raporlar;

namespace _3K.Application.Tests;

/// <summary>
/// R-07: gerçekleşen çeki ve eksik/sevk sonrası eksik raporlarında SEVK sütunu ürünün gerçek (fiziksel) sevkini,
/// Durum ise iş tamamlanmasını gösterir. Bir adet yalnız sevkli sandığa (sahaya aktarılan kısımda saha satırının
/// sevkli saha sandığına) fiilen konmuşsa gitmiş sayılır; trafoyla kapanan ayrı gösterilir; Grid İptal/Kapandı
/// teslim beklemez ama fiilen giden ürün görünür. Gerçek PDF ve Excel sentetik izole veritabanından üretilir.
/// </summary>
public sealed class RaporSevkDurumuPostgresTests
{
    private const string SevkTarihi = "05.10.2026";
    private const string TamOzet = "3 sevk edildi / 1 kısmi sevk / 4 sevk edilmedi / 1 trafoyla";

    [PostgresRaporFact]
    [Trait("Category", "Postgres")]
    public async Task GerceklesenCekiVeEksikRaporu_SevkFizikselAdettenTurer_DurumIsTamamlanmasidir()
    {
        await WithDatabase(async options =>
        {
            int kullaniciId;
            await using (var seed = new AppDbContext(options))
            {
                var kullanici = new Kullanici { RolId = 1, AdSoyad = "Sentetik Sevk Operatörü", Email = "sevk@example.invalid" };
                seed.Kullanicilar.Add(kullanici);
                await seed.SaveChangesAsync();
                kullaniciId = kullanici.Id;
            }

            foreach (var tip in new[] { ProjeTipi.Normal, ProjeTipi.Saha, ProjeTipi.Yedek })
            {
                int projeId;
                await using (var seed = new AppDbContext(options))
                {
                    var proje = ProjeKur(tip, kullaniciId);
                    seed.Projeler.Add(proje);
                    await seed.SaveChangesAsync();
                    projeId = proje.Id;
                }

                await using var context = new AppDbContext(options);
                var gerceklesen = new GerceklesenCekiListesiRaporService(context);
                var pdf = tip == ProjeTipi.Saha
                    ? await gerceklesen.SahaGerceklesenCekiListesiRaporuPdfOlusturAsync(projeId)
                    : await gerceklesen.GerceklesenCekiListesiRaporuPdfOlusturAsync(projeId);
                var excel = tip == ProjeTipi.Saha
                    ? await gerceklesen.SahaGerceklesenCekiListesiRaporuExcelOlusturAsync(projeId)
                    : await gerceklesen.GerceklesenCekiListesiRaporuExcelOlusturAsync(projeId);
                Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
                await OrnekKaydetAsync($"{tip}-sevk-gerceklesen-ceki.pdf", pdf);
                await OrnekKaydetAsync($"{tip}-sevk-gerceklesen-ceki.xlsx", excel);

                using (var workbook = new XLWorkbook(new MemoryStream(excel)))
                {
                    var sandikSayfasi = workbook.Worksheet("Sandık Bilgileri");
                    Assert.Equal("Sevk", sandikSayfasi.Cell(6, 10).GetString());
                    Assert.Equal("Sevk Tarihi", sandikSayfasi.Cell(6, 11).GetString());
                    Assert.Equal("2 / 4", sandikSayfasi.Cell(3, 9).GetString());
                    var sandiklar = Satirlar(sandikSayfasi).ToDictionary(r => r.Cell(2).GetString(),
                        r => (Sevk: r.Cell(10).GetString(), Tarih: r.Cell(11).GetString()));
                    Assert.Equal(("Sevk Edildi", SevkTarihi), sandiklar["1"]);
                    Assert.Equal(("Sevk Edildi", SevkTarihi), sandiklar["2"]);
                    Assert.Equal(("Sevk Edilmedi", "-"), sandiklar["3"]);
                    Assert.Equal(("Sevk Edilmedi", "-"), sandiklar["4"]);

                    var detay = workbook.Worksheet("Gerçekleşen Çeki");
                    Assert.Equal("Durum", detay.Cell(6, 11).GetString());
                    Assert.Equal("Sevk", detay.Cell(6, 12).GetString());
                    var urunler = Satirlar(detay).ToDictionary(r => r.Cell(3).GetString(),
                        r => (Durum: r.Cell(11).GetString(), Sevk: r.Cell(12).GetString()));
                    Assert.Equal(11, urunler.Count);
                    Assert.Equal(("Tamamlandı", "Sevk Edildi"), urunler["B-1"]);
                    Assert.Equal(("Tamamlandı", "Sevk Edildi"), urunler["B-2"]);
                    // Tamamlanmış ama elde kalan (kapalı) sandıktaki ürün gitmemiştir.
                    Assert.Equal(("Tamamlandı", "Sevk Edilmedi"), urunler["B-3"]);
                    Assert.Equal(("Eksik", "Sevk Edilmedi"), urunler["B-4"]);
                    Assert.Equal(("Tamamlandı", "Kısmi Sevk (1/2)"), urunler["B-5"]);
                    Assert.Equal(("Eksik", "Sevk Edilmedi"), urunler["B-6"]);
                    // Canlı PA600-36'da görülen durumlar: sevk edilen sandığa atanmış ama içine konmamış iptal, eksik
                    // ve Grid Kapandı satırı sevk edilmiş görünmez; trafoyla kapanan ayrı gösterilir.
                    Assert.Equal(("İptal", "-"), urunler["B-7"]);
                    Assert.Equal(("Eksik", "Sevk Edilmedi"), urunler["B-8"]);
                    Assert.Equal(("Tamamlandı", "-"), urunler["B-9"]);
                    Assert.Equal(("Trafoda Sevk", "Trafoyla"), urunler["B-10"]);
                    // Canlı PA677-01'deki gibi: gelip sandığa konmuş ve sandığı sevk edilmiş ürünün Grid durumu sonradan
                    // Kapandı yapılmış; gerçekten gittiği gizlenmez.
                    Assert.Equal(("Tamamlandı", "Sevk Edildi"), urunler["B-11"]);
                    Assert.Equal(TamOzet, detay.Cell(3, 9).GetString());
                    Assert.Equal(XLColor.FromHtml("#D32F2F"), Satirlar(detay).Single(r => r.Cell(3).GetString() == "B-8").Cell(12).Style.Font.FontColor);
                    Assert.Equal(XLColor.FromHtml("#1565C0"), Satirlar(detay).Single(r => r.Cell(3).GetString() == "B-10").Cell(12).Style.Font.FontColor);
                }

                var eksik = new EksikUrunlerRaporService(context);
                var eksikPdf = await eksik.EksikUrunlerRaporuPdfOlusturAsync(projeId);
                var eksikExcel = await eksik.EksikUrunlerRaporuExcelOlusturAsync(projeId);
                Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(eksikPdf, 0, 4));
                await OrnekKaydetAsync($"{tip}-sevk-eksik.pdf", eksikPdf);
                await OrnekKaydetAsync($"{tip}-sevk-eksik.xlsx", eksikExcel);

                using (var workbook = new XLWorkbook(new MemoryStream(eksikExcel)))
                {
                    var sayfa = workbook.Worksheet(1);
                    Assert.Equal("Kalan", sayfa.Cell(6, 15).GetString());
                    Assert.Equal("Sevk", sayfa.Cell(6, 16).GetString());
                    Assert.Equal("3K Durum", sayfa.Cell(6, 17).GetString());
                    var urunler = Satirlar(sayfa).ToDictionary(r => r.Cell(3).GetString(), r => r.Cell(16).GetString());
                    // Normal/yedek eksik raporu kalanı olanları, saha sevk sonrası raporu tamamlananları da listeler (R-02).
                    Assert.Equal(tip == ProjeTipi.Saha ? 11 : 3, urunler.Count);
                    Assert.Equal("Sevk Edilmedi", urunler["B-4"]);
                    Assert.Equal("Sevk Edilmedi", urunler["B-6"]);
                    Assert.Equal("Sevk Edilmedi", urunler["B-8"]);
                    if (tip == ProjeTipi.Saha)
                    {
                        Assert.Equal("Sevk Edildi", urunler["B-1"]);
                        Assert.Equal("Sevk Edilmedi", urunler["B-3"]);
                        Assert.Equal("Kısmi Sevk (1/2)", urunler["B-5"]);
                        Assert.Equal("-", urunler["B-7"]);
                        Assert.Equal("-", urunler["B-9"]);
                        Assert.Equal("Trafoyla", urunler["B-10"]);
                        Assert.Equal("Sevk Edildi", urunler["B-11"]);
                        Assert.Equal(TamOzet, sayfa.Cell(3, 15).GetString());
                    }
                    else
                        Assert.Equal("0 sevk edildi / 0 kısmi sevk / 3 sevk edilmedi / 0 trafoyla", sayfa.Cell(3, 15).GetString());
                }
            }
        });
    }

    /// <summary>
    /// Normal projede sahaya aktarılan kısım saha satırının fiziksel izinden okunur. Aktarım kalemi saha sandığı
    /// sevk edilince "Sevk Edildi" olur ama ürün içine konmadıysa gitmemiştir (canlıda 36 kalem); saha satırı iptal
    /// edildiyse o kısım beklenmez; kısmi aktarım normal kısımla toplanır; geri alınmış aktarım sayılmaz.
    /// </summary>
    [PostgresRaporFact]
    [Trait("Category", "Postgres")]
    public async Task NormalRapor_SahayaAktarilanKisim_SahaSatirininFizikselIzindenOkunur()
    {
        await WithDatabase(async options =>
        {
            int projeId;
            await using (var seed = new AppDbContext(options))
            {
                var kullanici = new Kullanici { RolId = 1, AdSoyad = "Sentetik Aktarım Operatörü", Email = "aktarim@example.invalid" };
                var proje = new Proje { ProjeNo = "QA-SEVK-AKTARIM", Musteri = "Sentetik Aktarım", ProjeTipiId = (int)ProjeTipi.Normal };
                var saha = new Proje { ProjeNo = "QA-SEVK-AKTARIM-SAHA", Musteri = "Sentetik Aktarım", ProjeTipiId = (int)ProjeTipi.Saha };
                var ceki = new Ceki { Proje = proje, OrijinalDosyaYolu = "sentetik.xlsx", YuklemeTarihi = new DateTime(2026, 10, 1, 9, 0, 0) };
                var sahaCeki = new Ceki { Proje = saha, OrijinalDosyaYolu = "saha.xlsx", YuklemeTarihi = new DateTime(2026, 10, 2, 9, 0, 0) };
                proje.Cekiler.Add(ceki);
                saha.Cekiler.Add(sahaCeki);
                var sevkli = new Sandik { Proje = proje, SandikNo = "1", Ad = "Sevkli", DurumId = (int)SandikDurum.Sevkedildi };
                var elde = new Sandik { Proje = proje, SandikNo = "3", Ad = "Elde kapalı", DurumId = (int)SandikDurum.Kapandi };
                var sahaSevkli = new Sandik { Proje = saha, SandikNo = "S1", Ad = "Saha sevkli", DurumId = (int)SandikDurum.Sevkedildi };
                var sahaElde = new Sandik { Proje = saha, SandikNo = "S2", Ad = "Saha elde", DurumId = (int)SandikDurum.Kapandi };
                proje.Sandiklar.Add(sevkli);
                proje.Sandiklar.Add(elde);
                saha.Sandiklar.Add(sahaSevkli);
                saha.Sandiklar.Add(sahaElde);
                var aktarim = new SahaAktarim { SahaProje = saha, KaynakProje = proje, Kullanici = kullanici, Tarih = new DateTime(2026, 10, 2) };

                static void Icerik(CekiSatiri satir, Sandik sandik, decimal tahsis, decimal konulan)
                {
                    var icerik = new SandikIcerik { Sandik = sandik, CekiSatiri = satir, TahsisMiktari = tahsis, KonulanAdet = konulan, EksikAdet = tahsis - konulan };
                    satir.SandikIcerikleri.Add(icerik);
                    sandik.SandikIcerikleri.Add(icerik);
                }

                void Urun(string barkod, decimal istenen, Sandik sandik, decimal konulan,
                    (decimal Miktar, SahaAktarimDurum KalemDurumu, Sandik SahaSandik, decimal SahaKonulan, GridDurum SahaGrid)? sahaKismi = null)
                {
                    var satir = new CekiSatiri
                    {
                        Ceki = ceki, SiraNo = ceki.CekiSatirlari.Count + 1, BarkodNo = barkod, Aciklama = "Sahaya aktarılan ürün",
                        CekideGecenSandikNo = sandik.SandikNo, IstenenAdet = istenen, GelenMiktar = istenen, GridDurumuId = (int)GridDurum.TamGeldi
                    };
                    Icerik(satir, sandik, istenen, konulan);
                    ceki.CekiSatirlari.Add(satir);
                    if (sahaKismi is not { } s)
                        return;
                    if (s.KalemDurumu == SahaAktarimDurum.GeriAlindi)
                    {
                        // Geri alınmış aktarımın saha satırı kalmaz; yalnız defter kalemi durur.
                        aktarim.Kalemler.Add(new SahaAktarimKalemi
                        {
                            SahaAktarim = aktarim, KaynakProje = proje, SahaProje = saha, KaynakCekiSatiri = satir,
                            Miktar = s.Miktar, DurumId = (int)s.KalemDurumu
                        });
                        return;
                    }
                    var sahaSatir = new CekiSatiri
                    {
                        Ceki = sahaCeki, SiraNo = sahaCeki.CekiSatirlari.Count + 1, BarkodNo = barkod, Aciklama = "Saha satırı",
                        CekideGecenSandikNo = s.SahaSandik.SandikNo, IstenenAdet = s.Miktar, GelenMiktar = s.SahaKonulan,
                        GridDurumuId = (int)s.SahaGrid, KaynakCekiSatiri = satir
                    };
                    Icerik(sahaSatir, s.SahaSandik, s.Miktar, s.SahaKonulan);
                    sahaCeki.CekiSatirlari.Add(sahaSatir);
                    aktarim.Kalemler.Add(new SahaAktarimKalemi
                    {
                        SahaAktarim = aktarim, KaynakProje = proje, SahaProje = saha, KaynakCekiSatiri = satir, SahaCekiSatiri = sahaSatir,
                        SahaSandik = s.SahaSandik, Miktar = s.Miktar, DurumId = (int)s.KalemDurumu
                    });
                }

                // Normal sandık gitti ama ürün sahada, saha sandığı elde.
                Urun("A-1", 1, sevkli, 0, (1, SahaAktarimDurum.Hazirlaniyor, sahaElde, 1, GridDurum.TamGeldi));
                // Sahada sevkli sandığa konup gitti; normal sandık elde.
                Urun("A-2", 1, elde, 0, (1, SahaAktarimDurum.SevkEdildi, sahaSevkli, 1, GridDurum.TamGeldi));
                // Kalem "Sevk Edildi" (saha sandığı gitti) ama ürün saha sandığına hiç konmadı: gitmemiştir.
                Urun("A-3", 1, elde, 0, (1, SahaAktarimDurum.SevkEdildi, sahaSevkli, 0, GridDurum.Gelmedi));
                // Saha satırı iptal edildi, hiçbir şey gitmedi: beklenmez.
                Urun("A-4", 1, elde, 0, (1, SahaAktarimDurum.SevkEdildi, sahaSevkli, 0, GridDurum.Iptal));
                // Kısmi aktarım: 1 normal sevkli sandıktan + 2 sahadan.
                Urun("A-5", 3, sevkli, 1, (2, SahaAktarimDurum.SevkEdildi, sahaSevkli, 2, GridDurum.TamGeldi));
                // Aktarım geri alındı: yalnız normal sandık esas.
                Urun("A-6", 1, sevkli, 1, (1, SahaAktarimDurum.GeriAlindi, sahaElde, 0, GridDurum.Gelmedi));

                var sevkiyat = new Sevkiyat
                {
                    Proje = proje, SevkiyatNo = 1, SevkTarihi = new DateTime(2026, 10, 5, 14, 0, 0), Kullanici = kullanici, AracPlaka = "34ABC123"
                };
                sevkiyat.Sandiklar.Add(new SevkiyatSandik { Sevkiyat = sevkiyat, Sandik = sevkli });
                proje.Sevkiyatlar.Add(sevkiyat);
                seed.AddRange(proje, saha, aktarim);
                await seed.SaveChangesAsync();
                projeId = proje.Id;
            }

            await using var context = new AppDbContext(options);
            var gerceklesen = new GerceklesenCekiListesiRaporService(context);
            Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(await gerceklesen.GerceklesenCekiListesiRaporuPdfOlusturAsync(projeId), 0, 4));
            using (var workbook = new XLWorkbook(new MemoryStream(await gerceklesen.GerceklesenCekiListesiRaporuExcelOlusturAsync(projeId))))
            {
                var sevkler = Satirlar(workbook.Worksheet("Gerçekleşen Çeki"))
                    .ToDictionary(r => r.Cell(3).GetString(), r => r.Cell(12).GetString());
                Assert.Equal("Sevk Edilmedi", sevkler["A-1"]);
                Assert.Equal("Sevk Edildi", sevkler["A-2"]);
                Assert.Equal("Sevk Edilmedi", sevkler["A-3"]);
                Assert.Equal("-", sevkler["A-4"]);
                Assert.Equal("Sevk Edildi", sevkler["A-5"]);
                Assert.Equal("Sevk Edildi", sevkler["A-6"]);
            }

            var eksik = new EksikUrunlerRaporService(context);
            Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(await eksik.EksikUrunlerRaporuPdfOlusturAsync(projeId), 0, 4));
            using (var workbook = new XLWorkbook(new MemoryStream(await eksik.EksikUrunlerRaporuExcelOlusturAsync(projeId))))
            {
                // Normal eksik raporu kalanı olmayan satırlardan yalnız aktif saha aktarımlıları listeler (R-02).
                var sevkler = Satirlar(workbook.Worksheet(1)).ToDictionary(r => r.Cell(3).GetString(), r => r.Cell(16).GetString());
                Assert.Equal(["A-1", "A-2", "A-3", "A-4", "A-5"], sevkler.Keys.Order());
                Assert.Equal("Sevk Edilmedi", sevkler["A-1"]);
                Assert.Equal("Sevk Edildi", sevkler["A-2"]);
                Assert.Equal("Sevk Edilmedi", sevkler["A-3"]);
                Assert.Equal("-", sevkler["A-4"]);
                Assert.Equal("Sevk Edildi", sevkler["A-5"]);
            }
        });
    }

    /// <summary>
    /// Dört sandık: 1 ve 2 sevk edildi, 3 kapandı (elde), 4 hazırlanıyor. B-5 sevkli 2 ile elde kalan 3'e bölünmüştür;
    /// B-6 hiçbir sandıkta değildir; B-7/B-8/B-9/B-10 sevkli sandığa atanmış ama içine konmamıştır; B-11 sevkli
    /// sandığa konmuş, Grid durumu sonradan Kapandı yapılmıştır.
    /// </summary>
    private static Proje ProjeKur(ProjeTipi tip, int kullaniciId)
    {
        var proje = new Proje { ProjeNo = $"QA-SEVK-{tip}", Musteri = "Sentetik Sevk Doğrulama", ProjeTipiId = (int)tip };
        var ceki = new Ceki { Proje = proje, OrijinalDosyaYolu = "sentetik.xlsx", YuklemeTarihi = new DateTime(2026, 10, 1, 9, 0, 0) };
        proje.Cekiler.Add(ceki);
        var sandiklar = new Dictionary<string, Sandik>
        {
            ["1"] = new() { Proje = proje, SandikNo = "1", Ad = "Sevkli", DurumId = (int)SandikDurum.Sevkedildi },
            ["2"] = new() { Proje = proje, SandikNo = "2", Ad = "Sevkli", DurumId = (int)SandikDurum.Sevkedildi },
            ["3"] = new() { Proje = proje, SandikNo = "3", Ad = "Elde kapalı", DurumId = (int)SandikDurum.Kapandi },
            ["4"] = new() { Proje = proje, SandikNo = "4", Ad = "Hazırlanıyor", DurumId = (int)SandikDurum.Hazirlaniyor }
        };
        foreach (var sandik in sandiklar.Values) proje.Sandiklar.Add(sandik);

        void Urun(int sira, decimal istenen, decimal gelen, GridDurum? grid = null, decimal trafo = 0,
            params (string SandikNo, decimal Konulan)[] icerikler)
        {
            var satir = new CekiSatiri
            {
                Ceki = ceki, SiraNo = sira, BarkodNo = $"B-{sira}", Aciklama = "Sentetik sevk ürünü",
                CekideGecenSandikNo = icerikler.Length > 0 ? icerikler[0].SandikNo : string.Empty,
                IstenenAdet = istenen, GelenMiktar = gelen, TrafoSevkAdet = trafo,
                GridDurumuId = (int)(grid ?? (gelen >= istenen ? GridDurum.TamGeldi : GridDurum.Gelmedi))
            };
            foreach (var (sandikNo, konulan) in icerikler)
            {
                var icerik = new SandikIcerik
                {
                    Sandik = sandiklar[sandikNo], CekiSatiri = satir, TahsisMiktari = konulan > 0 ? konulan : istenen,
                    KonulanAdet = konulan, EksikAdet = konulan > 0 ? 0 : istenen
                };
                satir.SandikIcerikleri.Add(icerik);
                sandiklar[sandikNo].SandikIcerikleri.Add(icerik);
            }
            ceki.CekiSatirlari.Add(satir);
        }

        Urun(1, 2, 2, icerikler: [("1", 2)]);
        Urun(2, 1, 1, icerikler: [("2", 1)]);
        Urun(3, 1, 1, icerikler: [("3", 1)]);
        Urun(4, 2, 0, icerikler: [("4", 0)]);
        Urun(5, 2, 2, icerikler: [("2", 1), ("3", 1)]);
        Urun(6, 1, 0);
        Urun(7, 1, 0, GridDurum.Iptal, icerikler: [("1", 0)]);
        Urun(8, 1, 0, icerikler: [("2", 0)]);
        Urun(9, 1, 0, GridDurum.GridKapandi, icerikler: [("1", 0)]);
        Urun(10, 1, 0, GridDurum.TrafoSevk, trafo: 1, icerikler: [("1", 0)]);
        Urun(11, 1, 1, GridDurum.GridKapandi, icerikler: [("1", 1)]);

        var sevkiyat = new Sevkiyat
        {
            Proje = proje, SevkiyatNo = 1, SevkTarihi = new DateTime(2026, 10, 5, 14, 0, 0), KullaniciId = kullaniciId,
            AracPlaka = "34ABC123"
        };
        sevkiyat.Sandiklar.Add(new SevkiyatSandik { Sevkiyat = sevkiyat, Sandik = sandiklar["1"] });
        sevkiyat.Sandiklar.Add(new SevkiyatSandik { Sevkiyat = sevkiyat, Sandik = sandiklar["2"] });
        proje.Sevkiyatlar.Add(sevkiyat);
        return proje;
    }

    private static IEnumerable<IXLRow> Satirlar(IXLWorksheet sayfa) =>
        sayfa.RowsUsed().Where(r => r.RowNumber() > 6);

    private static async Task OrnekKaydetAsync(string ad, byte[] icerik)
    {
        var klasor = Environment.GetEnvironmentVariable("THREEK_REPORT_OUTPUT");
        if (string.IsNullOrWhiteSpace(klasor)) return;
        Directory.CreateDirectory(klasor);
        await File.WriteAllBytesAsync(Path.Combine(klasor, ad), icerik);
    }

    private static async Task WithDatabase(Func<DbContextOptions<AppDbContext>, Task> test)
    {
        var connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("THREEK_TEST_POSTGRES"));
        Assert.True(connection.Host is "127.0.0.1" or "localhost", "Yalnız yerel izole test sunucusu kullanılabilir.");
        Assert.Equal(55439, connection.Port);
        Assert.Equal("postgres", connection.Database);
        var database = $"rapor_sevk_tests_{Guid.NewGuid():N}";
        await using var admin = new NpgsqlConnection(connection.ConnectionString);
        await admin.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE DATABASE \"{database}\"", admin)) await create.ExecuteNonQueryAsync();
        try
        {
            connection.Database = database;
            var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection.ConnectionString).Options;
            await using (var ctx = new AppDbContext(options))
                await ctx.Database.EnsureCreatedAsync();
            await test(options);
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            // Yalnız bu testin oluşturduğu, sabit önek + GUID isimli sentetik veritabanı kaldırılır.
            Assert.Matches("^rapor_sevk_tests_[a-f0-9]{32}$", database);
            await using var drop = new NpgsqlCommand($"DROP DATABASE \"{database}\" WITH (FORCE)", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }
}
