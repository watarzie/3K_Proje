using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Services;
using _3K.Infrastructure.Services.Raporlar;

namespace _3K.Application.Tests;

/// <summary>
/// R-07: gerçekleşen çeki ve eksik/sevk sonrası eksik raporlarında SEVK ürünün sevkini, Durum ise iş tamamlanmasını
/// gösterir. Grid İptal sevk edilmez, Grid Kapandı Grid tarafında sevk edilmiştir; diğerlerinde bir adet yalnız raporlanan
/// projenin kendi sevkli sandığına fiilen konmuş ve 3K'nın teslim aldığı adetse gitmiş sayılır; Grid Kapandı ve trafodaki
/// ürün satırın sandığıyla gider. Sahaya aktarılan kısım normal SEVK'e karışmaz; bir kısmı gittiyse Eksik Sevk.
/// Gerçek PDF ve Excel sentetik izole veritabanından üretilir.
/// </summary>
public sealed class RaporSevkDurumuPostgresTests
{
    private const string TamOzet = "5 sevk edildi / 2 eksik sevk / 9 sevk edilmedi / 1 trafoyla";

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
                var gerceklesen = Gerceklesen(context);
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
                    Assert.Equal("2 / 4", sandikSayfasi.Cell(3, 9).GetString());
                    var sandiklar = Satirlar(sandikSayfasi).ToDictionary(r => r.Cell(2).GetString(), r => r.Cell(10).GetString());
                    Assert.Equal("Sevk Edildi", sandiklar["1"]);
                    Assert.Equal("Sevk Edildi", sandiklar["2"]);
                    Assert.Equal("Sevk Edilmedi", sandiklar["3"]);
                    Assert.Equal("Sevk Edilmedi", sandiklar["4"]);
                    // Sevk tarihi sütunu yoktur; sahaya aktarım yalnız normal projeden yapılır, sütunu yalnız normalde vardır.
                    Assert.Equal(tip == ProjeTipi.Normal ? "Sahaya Aktarıldı" : "", sandikSayfasi.Cell(6, 11).GetString());
                    Assert.DoesNotContain(sandikSayfasi.Row(6).CellsUsed(), c => c.GetString() == "Sevk Tarihi");

                    var detay = workbook.Worksheet("Gerçekleşen Çeki");
                    Assert.Equal("Durum", detay.Cell(6, 11).GetString());
                    Assert.Equal("Sevk", detay.Cell(6, 12).GetString());
                    // Sahaya aktarılan adet sütunu yoktur.
                    Assert.Equal("Açıklama", detay.Cell(6, 13).GetString());
                    var urunler = Satirlar(detay).ToDictionary(r => r.Cell(3).GetString(),
                        r => (Durum: r.Cell(11).GetString(), Sevk: r.Cell(12).GetString()));
                    Assert.Equal(17, urunler.Count);
                    Assert.Equal(("Tamamlandı", "Sevk Edildi"), urunler["B-1"]);
                    Assert.Equal(("Tamamlandı", "Sevk Edildi"), urunler["B-2"]);
                    // Tamamlanmış ama elde kalan (kapalı) sandıktaki ürün gitmemiştir.
                    Assert.Equal(("Tamamlandı", "Sevk Edilmedi"), urunler["B-3"]);
                    Assert.Equal(("Eksik", "Sevk Edilmedi"), urunler["B-4"]);
                    Assert.Equal(("Tamamlandı", "Eksik Sevk (1/2)"), urunler["B-5"]);
                    Assert.Equal(("Eksik", "Sevk Edilmedi"), urunler["B-6"]);
                    // İptal ürün sevk edilmez; sevk edilen sandığa atanmış ama içine konmamış ürün gitmemiştir.
                    Assert.Equal(("İptal", "Sevk Edilmedi"), urunler["B-7"]);
                    Assert.Equal(("Eksik", "Sevk Edilmedi"), urunler["B-8"]);
                    // Grid Kapandı ürün Grid tarafında sevk edilmiştir (3K sandığına konmamış olsa da).
                    Assert.Equal(("Tamamlandı", "Sevk Edildi"), urunler["B-9"]);
                    Assert.Equal(("Trafoda Sevk", "Trafoyla"), urunler["B-10"]);
                    Assert.Equal(("Tamamlandı", "Sevk Edildi"), urunler["B-11"]);
                    // İptal kararı sandık izinden önce gelir (canlıda 3 satır).
                    Assert.Equal(("İptal", "Sevk Edilmedi"), urunler["B-12"]);
                    // 4'ün 2'si gelip sevk edildi: Eksik Sevk.
                    Assert.Equal(("Eksik", "Eksik Sevk (2/4)"), urunler["B-13"]);
                    // Grid Kapandı ve trafodaki ürün satırın sandığıyla gider: sandığı (sevkiyatı geri alınmış ya da elde
                    // kalan) sevk edilmediyse gitmemiştir.
                    Assert.Equal(("Tamamlandı", "Sevk Edilmedi"), urunler["B-14"]);
                    Assert.Equal(("Trafoda Sevk", "Sevk Edilmedi"), urunler["B-15"]);
                    // Parçalı: 1 trafoda, 1 3K'dan sandıkta; sandık gitti (canlı PA600-36 SMC45152).
                    Assert.Equal(("Trafoda Sevk", "Sevk Edildi"), urunler["B-16"]);
                    Assert.Equal(("Trafoda Sevk", "Sevk Edilmedi"), urunler["B-17"]);
                    Assert.Equal(TamOzet, detay.Cell(3, 9).GetString());
                    Assert.Equal(XLColor.FromHtml("#D32F2F"), Satirlar(detay).Single(r => r.Cell(3).GetString() == "B-8").Cell(12).Style.Font.FontColor);
                    Assert.Equal(XLColor.FromHtml("#1565C0"), Satirlar(detay).Single(r => r.Cell(3).GetString() == "B-10").Cell(12).Style.Font.FontColor);
                    Assert.Equal(XLColor.FromHtml("#2E7D32"), Satirlar(detay).Single(r => r.Cell(3).GetString() == "B-9").Cell(12).Style.Font.FontColor);
                    Assert.Equal(XLColor.FromHtml("#F57C00"), Satirlar(detay).Single(r => r.Cell(3).GetString() == "B-13").Cell(12).Style.Font.FontColor);
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
                    Assert.Equal(tip == ProjeTipi.Saha ? 17 : 4, urunler.Count);
                    Assert.Equal("Sevk Edilmedi", urunler["B-4"]);
                    Assert.Equal("Sevk Edilmedi", urunler["B-6"]);
                    Assert.Equal("Sevk Edilmedi", urunler["B-8"]);
                    Assert.Equal("Eksik Sevk (2/4)", urunler["B-13"]);
                    if (tip == ProjeTipi.Saha)
                    {
                        Assert.Equal("Sevk Edildi", urunler["B-1"]);
                        Assert.Equal("Sevk Edilmedi", urunler["B-3"]);
                        Assert.Equal("Eksik Sevk (1/2)", urunler["B-5"]);
                        Assert.Equal("Sevk Edilmedi", urunler["B-7"]);
                        Assert.Equal("Sevk Edildi", urunler["B-9"]);
                        Assert.Equal("Trafoyla", urunler["B-10"]);
                        Assert.Equal("Sevk Edildi", urunler["B-11"]);
                        Assert.Equal("Sevk Edilmedi", urunler["B-12"]);
                        Assert.Equal("Sevk Edilmedi", urunler["B-14"]);
                        Assert.Equal("Sevk Edilmedi", urunler["B-15"]);
                        Assert.Equal("Sevk Edildi", urunler["B-16"]);
                        Assert.Equal("Sevk Edilmedi", urunler["B-17"]);
                        Assert.Equal(TamOzet, sayfa.Cell(3, 15).GetString());
                    }
                    else
                        Assert.Equal("0 sevk edildi / 1 eksik sevk / 3 sevk edilmedi / 0 trafoyla",
                            sayfa.Cell(3, 15).GetString());
                }
            }
        });
    }

    /// <summary>
    /// Normal projenin raporu kendi sevkiyatının anlık görüntüsüdür: sahaya aktarılan kısım saha sandığıyla gitse,
    /// sahada Grid İptal ya da Grid Kapandı olsa da normal SEVK'e karışmaz (canlı PA600-36 sıra 262–269, PA600-16
    /// sıra 164). Kısmi aktarımda normal kısım Eksik Sevk olur;
    /// geri alınmış ve iptal edilmiş aktarım sayılmaz. Sandık bazlı sahaya aktarılan normal sandık sandık sayfasında
    /// "Sevk Edilmedi" + "Aktarıldı" görünür (canlı PA704-06). KALAN normal projenin kendi kalanıdır. Grid'in beyan ettiği ama 3K'nın teslim almadığı ürün gitmemiştir (canlı PA675-01
    /// FCT00685015); sandık içeriği olmayan eski satır sandık numarasından okunur (canlı PA600-50 sıra 73/74).
    /// </summary>
    [PostgresRaporFact]
    [Trait("Category", "Postgres")]
    public async Task NormalRapor_KendiSevkiyatininAnlikGoruntusu_SahayaAktarilanAyriGosterilir()
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
                    (decimal Miktar, SahaAktarimDurum KalemDurumu, Sandik SahaSandik, decimal SahaKonulan, GridDurum SahaGrid)? sahaKismi = null,
                    decimal sahaTrafo = 0)
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
                        TrafoSevkAdet = sahaTrafo, GridDurumuId = (int)s.SahaGrid, KaynakCekiSatiri = satir
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
                // Kalem "Sevk Edildi" (saha sandığı gitti) ama ürün saha sandığına hiç konmadı.
                Urun("A-3", 1, elde, 0, (1, SahaAktarimDurum.SevkEdildi, sahaSevkli, 0, GridDurum.Gelmedi));
                // Saha satırı Grid İptal (saha kararı; normal satıra yazılmaz).
                Urun("A-4", 1, elde, 0, (1, SahaAktarimDurum.SevkEdildi, sahaSevkli, 0, GridDurum.Iptal));
                // Kısmi aktarım: 1 normal sevkli sandıktan + 2 sahadan.
                Urun("A-5", 3, sevkli, 1, (2, SahaAktarimDurum.SevkEdildi, sahaSevkli, 2, GridDurum.TamGeldi));
                // Aktarım geri alındı: yalnız normal sandık esas.
                Urun("A-6", 1, sevkli, 1, (1, SahaAktarimDurum.GeriAlindi, sahaElde, 0, GridDurum.Gelmedi));
                // Saha satırı Grid tarafında kapatıldı (saha kararı; normal satıra yazılmaz).
                Urun("A-7", 1, elde, 0, (1, SahaAktarimDurum.Hazirlaniyor, sahaElde, 0, GridDurum.GridKapandi));
                // 2'nin 1'i normal sevkiyatla gitti, 1'i sahaya aktarıldı ve sahada Grid İptal oldu: saha İptal'i normal
                // satıra sızsaydı "Sevk Edilmedi" çıkardı.
                Urun("A-12", 2, sevkli, 1, (1, SahaAktarimDurum.SevkEdildi, sahaSevkli, 0, GridDurum.Iptal));
                // Aynı yapı, saha satırının 1'i trafoyla kapandı: saha trafosu normal beklenenden düşülseydi "Sevk Edildi" çıkardı.
                Urun("A-13", 2, sevkli, 1, (1, SahaAktarimDurum.SevkEdildi, sahaElde, 0, GridDurum.TrafoSevk), sahaTrafo: 1);

                // Sandık bazlı aktarım: sandık 5 bütünüyle sahaya aktarıldı ve saha sandığıyla gitti. Normal sandığın kendi
                // durumu Kapandı kalır; ürün 3K'ya gelmeden sahada saha sandığına kondu.
                var sahayaGiden = new Sandik { Proje = proje, SandikNo = "5", Ad = "Sahaya aktarılan", DurumId = (int)SandikDurum.Kapandi };
                proje.Sandiklar.Add(sahayaGiden);
                var sandikBazli = new CekiSatiri
                {
                    Ceki = ceki, SiraNo = ceki.CekiSatirlari.Count + 1, BarkodNo = "A-8", Aciklama = "Sandıkla sahaya aktarılan ürün",
                    CekideGecenSandikNo = sahayaGiden.SandikNo, IstenenAdet = 2, GridDurumuId = (int)GridDurum.Gelmedi
                };
                Icerik(sandikBazli, sahayaGiden, 2, 0);
                ceki.CekiSatirlari.Add(sandikBazli);
                var sandikBazliSaha = new CekiSatiri
                {
                    Ceki = sahaCeki, SiraNo = sahaCeki.CekiSatirlari.Count + 1, BarkodNo = "A-8", Aciklama = "Saha satırı",
                    CekideGecenSandikNo = sahaSevkli.SandikNo, IstenenAdet = 2, GelenMiktar = 2, GridDurumuId = (int)GridDurum.TamGeldi,
                    KaynakCekiSatiri = sandikBazli
                };
                Icerik(sandikBazliSaha, sahaSevkli, 2, 2);
                sahaCeki.CekiSatirlari.Add(sandikBazliSaha);
                aktarim.Kalemler.Add(new SahaAktarimKalemi
                {
                    SahaAktarim = aktarim, KaynakProje = proje, SahaProje = saha, KaynakCekiSatiri = sandikBazli,
                    SahaCekiSatiri = sandikBazliSaha, KaynakSandik = sahayaGiden, SahaSandik = sahaSevkli, Miktar = 2,
                    AktarimTipiId = (int)SahaAktarimTipi.SandikBazli, DurumId = (int)SahaAktarimDurum.SevkEdildi,
                    SevkiyatKapsamindaMi = true, SevkTarihi = new DateTime(2026, 10, 7, 10, 0, 0)
                });

                // Grid manuel ekledi (sandıkta konan 1 kendiliğinden yazıldı), Grid "geldi/sevk edildi" dedi, 3K teslim
                // almadı; sandık gitti, ürün sonra eksik sayılıp sahaya aktarıldı ve sahada Grid İptal oldu.
                var gridBeyani = new CekiSatiri
                {
                    Ceki = ceki, SiraNo = 9999, BarkodNo = "A-10", Aciklama = "Grid manuel ürün", IsManuelEklenen = true,
                    CekideGecenSandikNo = sevkli.SandikNo, IstenenAdet = 1, GridGelenAdet = 1, GridSevkMiktari = 1,
                    GridDurumuId = (int)GridDurum.TamGeldi
                };
                Icerik(gridBeyani, sevkli, 1, 1);
                ceki.CekiSatirlari.Add(gridBeyani);
                var gridBeyaniSaha = new CekiSatiri
                {
                    Ceki = sahaCeki, SiraNo = sahaCeki.CekiSatirlari.Count + 1, BarkodNo = "A-10", Aciklama = "Eksik saha tamamlama",
                    CekideGecenSandikNo = sahaElde.SandikNo, IstenenAdet = 1, GridDurumuId = (int)GridDurum.Iptal,
                    KaynakCekiSatiri = gridBeyani
                };
                Icerik(gridBeyaniSaha, sahaElde, 1, 0);
                sahaCeki.CekiSatirlari.Add(gridBeyaniSaha);
                aktarim.Kalemler.Add(new SahaAktarimKalemi
                {
                    SahaAktarim = aktarim, KaynakProje = proje, SahaProje = saha, KaynakCekiSatiri = gridBeyani,
                    SahaCekiSatiri = gridBeyaniSaha, SahaSandik = sahaElde, Miktar = 1, DurumId = (int)SahaAktarimDurum.Planlandi
                });

                // Sandık içeriği hiç oluşmamış eski satır: fiili sandık numarası sevkli sandık 1'i gösteriyor.
                ceki.CekiSatirlari.Add(new CekiSatiri
                {
                    Ceki = ceki, SiraNo = ceki.CekiSatirlari.Count + 1, BarkodNo = "A-11", Aciklama = "Tahsissiz eski satır",
                    CekideGecenSandikNo = "9", FiiliSandikNo = " 1 ", IstenenAdet = 2, GelenMiktar = 2,
                    GridDurumuId = (int)GridDurum.TamGeldi
                });

                CekiSatiri NormalSatir(string barkod, decimal istenen, decimal gelen, GridDurum grid, Sandik? sandik = null,
                    decimal konulan = 0, decimal trafo = 0)
                {
                    var satir = new CekiSatiri
                    {
                        Ceki = ceki, SiraNo = ceki.CekiSatirlari.Count + 1, BarkodNo = barkod, Aciklama = "Sandık sayfası ürünü",
                        CekideGecenSandikNo = sandik?.SandikNo ?? string.Empty, IstenenAdet = istenen, GelenMiktar = gelen,
                        TrafoSevkAdet = trafo, GridDurumuId = (int)grid
                    };
                    if (sandik != null)
                        Icerik(satir, sandik, istenen, konulan);
                    ceki.CekiSatirlari.Add(satir);
                    return satir;
                }

                // Sandık 6: yalnız bir ürünü sandık bazlı aktarılmış; aktarılmamış teslim alınmış ürün de içinde duruyor.
                // Grid Kapandı, Grid İptal ve tam trafolu ürün sevki beklenen ürün sayılmaz: "Kısmen (1/2 ürün)".
                var kismenAktarilan = new Sandik { Proje = proje, SandikNo = "6", Ad = "Kısmen aktarılan", DurumId = (int)SandikDurum.Kapandi };
                proje.Sandiklar.Add(kismenAktarilan);
                var kismenAktarilanUrun = NormalSatir("A-14", 1, 0, GridDurum.Gelmedi, kismenAktarilan);
                var kismenSaha = new CekiSatiri
                {
                    Ceki = sahaCeki, SiraNo = sahaCeki.CekiSatirlari.Count + 1, BarkodNo = "A-14", Aciklama = "Saha satırı",
                    CekideGecenSandikNo = sahaElde.SandikNo, IstenenAdet = 1, GridDurumuId = (int)GridDurum.Gelmedi,
                    KaynakCekiSatiri = kismenAktarilanUrun
                };
                Icerik(kismenSaha, sahaElde, 1, 0);
                sahaCeki.CekiSatirlari.Add(kismenSaha);
                aktarim.Kalemler.Add(new SahaAktarimKalemi
                {
                    SahaAktarim = aktarim, KaynakProje = proje, SahaProje = saha, KaynakCekiSatiri = kismenAktarilanUrun,
                    SahaCekiSatiri = kismenSaha, KaynakSandik = kismenAktarilan, SahaSandik = sahaElde, Miktar = 1,
                    AktarimTipiId = (int)SahaAktarimTipi.SandikBazli, DurumId = (int)SahaAktarimDurum.Planlandi
                });
                NormalSatir("A-15", 1, 1, GridDurum.TamGeldi, kismenAktarilan, konulan: 1);
                NormalSatir("A-16", 1, 0, GridDurum.GridKapandi, kismenAktarilan);
                NormalSatir("A-17", 1, 0, GridDurum.Iptal, kismenAktarilan);
                NormalSatir("A-18", 1, 0, GridDurum.TrafoSevk, kismenAktarilan, trafo: 1);

                // Sandık 7: sandık bazlı aktarımı geri alınmış; aktarılmış sayılmaz.
                var geriAlinan = new Sandik { Proje = proje, SandikNo = "7", Ad = "Aktarımı geri alınan", DurumId = (int)SandikDurum.Kapandi };
                proje.Sandiklar.Add(geriAlinan);
                var geriAlinanUrun = NormalSatir("A-19", 1, 0, GridDurum.Gelmedi, geriAlinan);
                aktarim.Kalemler.Add(new SahaAktarimKalemi
                {
                    SahaAktarim = aktarim, KaynakProje = proje, SahaProje = saha, KaynakCekiSatiri = geriAlinanUrun,
                    KaynakSandik = geriAlinan, Miktar = 1, AktarimTipiId = (int)SahaAktarimTipi.SandikBazli,
                    DurumId = (int)SahaAktarimDurum.GeriAlindi
                });

                // İçeriği olmayan eski satır, kalanı olduğu için eksik raporunda da listelenir: sandık 1'den 2'nin 1'i gitti.
                var tahsissizEksik = NormalSatir("A-20", 2, 1, GridDurum.EksikGeldi);
                tahsissizEksik.CekideGecenSandikNo = "1";

                // İptal edilmiş kalem sayılmaz: saha satırı sevkli saha sandığında dursa da bu ürünün sevki değildir.
                var iptalKalemli = new CekiSatiri
                {
                    Ceki = ceki, SiraNo = ceki.CekiSatirlari.Count + 1, BarkodNo = "A-9", Aciklama = "İptal aktarımlı ürün",
                    CekideGecenSandikNo = elde.SandikNo, IstenenAdet = 1, GelenMiktar = 1, GridDurumuId = (int)GridDurum.TamGeldi
                };
                Icerik(iptalKalemli, elde, 1, 1);
                ceki.CekiSatirlari.Add(iptalKalemli);
                var iptalSaha = new CekiSatiri
                {
                    Ceki = sahaCeki, SiraNo = sahaCeki.CekiSatirlari.Count + 1, BarkodNo = "A-9", Aciklama = "İptal aktarımın saha satırı",
                    CekideGecenSandikNo = sahaSevkli.SandikNo, IstenenAdet = 1, GelenMiktar = 1, GridDurumuId = (int)GridDurum.TamGeldi
                };
                Icerik(iptalSaha, sahaSevkli, 1, 1);
                sahaCeki.CekiSatirlari.Add(iptalSaha);
                aktarim.Kalemler.Add(new SahaAktarimKalemi
                {
                    SahaAktarim = aktarim, KaynakProje = proje, SahaProje = saha, KaynakCekiSatiri = iptalKalemli,
                    SahaCekiSatiri = iptalSaha, SahaSandik = sahaSevkli, Miktar = 1, DurumId = (int)SahaAktarimDurum.Iptal
                });

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
            var gerceklesen = Gerceklesen(context);
            var gerceklesenPdf = await gerceklesen.GerceklesenCekiListesiRaporuPdfOlusturAsync(projeId);
            Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(gerceklesenPdf, 0, 4));
            await OrnekKaydetAsync("Aktarim-sevk-gerceklesen-ceki.pdf", gerceklesenPdf);
            var gerceklesenExcel = await gerceklesen.GerceklesenCekiListesiRaporuExcelOlusturAsync(projeId);
            await OrnekKaydetAsync("Aktarim-sevk-gerceklesen-ceki.xlsx", gerceklesenExcel);
            using (var workbook = new XLWorkbook(new MemoryStream(gerceklesenExcel)))
            {
                var sandikSayfasi = workbook.Worksheet("Sandık Bilgileri");
                // Sahaya aktarılan sandıklar (5, 6) bu projenin sevkiyatıyla gitmedi: 5 sandığın 1'i sevk edildi, 2'si aktarıldı.
                Assert.Equal("1 / 5", sandikSayfasi.Cell(3, 9).GetString());
                Assert.Equal("2", sandikSayfasi.Cell(3, 11).GetString());
                var sandiklar = Satirlar(sandikSayfasi).ToDictionary(r => r.Cell(2).GetString(),
                    r => (Sevk: r.Cell(10).GetString(), Saha: r.Cell(11).GetString()));
                Assert.Equal(("Sevk Edildi", "-"), sandiklar["1"]);
                Assert.Equal(("Sevk Edilmedi", "-"), sandiklar["3"]);
                // Saha sandığıyla gitmiş olsa da normal sevkiyatla gitmedi.
                Assert.Equal(("Sevk Edilmedi", "Aktarıldı"), sandiklar["5"]);
                Assert.Equal(("Sevk Edilmedi", "Kısmen (1/2 ürün)"), sandiklar["6"]);
                // Geri alınmış sandık bazlı aktarım sayılmaz.
                Assert.Equal(("Sevk Edilmedi", "-"), sandiklar["7"]);
                Assert.Equal(XLColor.FromHtml("#6A1B9A"), Satirlar(sandikSayfasi).Single(r => r.Cell(2).GetString() == "5").Cell(11).Style.Font.FontColor);

                var detay = workbook.Worksheet("Gerçekleşen Çeki");
                Assert.Equal("Açıklama", detay.Cell(6, 13).GetString());
                var urunler = Satirlar(detay).ToDictionary(r => r.Cell(3).GetString(),
                    r => (Kalan: r.Cell(10).GetString(), Durum: r.Cell(11).GetString(), Sevk: r.Cell(12).GetString()));
                // Normal sandık gitti ama ürün içinde değil (sahada, saha sandığı elde).
                Assert.Equal(("0", "Tamamlandı", "Sevk Edilmedi"), urunler["A-1"]);
                // Saha sandığıyla gitti; normal sevkiyatla gitmedi.
                Assert.Equal(("0", "Tamamlandı", "Sevk Edilmedi"), urunler["A-2"]);
                Assert.Equal(("0", "Tamamlandı", "Sevk Edilmedi"), urunler["A-3"]);
                // Sahada Grid İptal: saha durumu normal satıra yazılmaz.
                Assert.Equal(("0", "Tamamlandı", "Sevk Edilmedi"), urunler["A-4"]);
                // 3'ün 1'i normal sevkiyatla gitti, 2'si sahaya aktarıldı.
                Assert.Equal(("0", "Tamamlandı", "Eksik Sevk (1/3)"), urunler["A-5"]);
                // Geri alınan aktarım sayılmaz.
                Assert.Equal(("0", "Tamamlandı", "Sevk Edildi"), urunler["A-6"]);
                // Sahada Grid Kapandı: normal satır yine Sevk Edilmedi.
                Assert.Equal(("0", "Tamamlandı", "Sevk Edilmedi"), urunler["A-7"]);
                // Sandık bazlı aktarım; KALAN normal projenin kendi kalanıdır (sahada tamamlanan düşülmez).
                Assert.Equal(("2", "Eksik", "Sevk Edilmedi"), urunler["A-8"]);
                // İptal edilmiş kalem aktarılan sayılmaz.
                Assert.Equal(("0", "Tamamlandı", "Sevk Edilmedi"), urunler["A-9"]);
                // Grid beyanı 3K teslimi değildir (FCT00685015): sevkli sandıkta görünse de gitmedi.
                Assert.Equal(("1", "Eksik", "Sevk Edilmedi"), urunler["A-10"]);
                // İçeriği olmayan eski satır fiili sandık numarasıyla sevkli sandık 1'den okunur.
                Assert.Equal(("0", "Tamamlandı", "Sevk Edildi"), urunler["A-11"]);
                // Sahada Grid İptal ve saha trafosu normal satırın kendi sevkini değiştirmez.
                Assert.Equal(("0", "Tamamlandı", "Eksik Sevk (1/2)"), urunler["A-12"]);
                Assert.Equal(("0", "Tamamlandı", "Eksik Sevk (1/2)"), urunler["A-13"]);
                Assert.Equal(("1", "Eksik", "Sevk Edilmedi"), urunler["A-14"]);
                Assert.Equal(("0", "Tamamlandı", "Sevk Edilmedi"), urunler["A-15"]);
                // Sandık 6 sevk edilmedi: içindeki Grid Kapandı ve trafodaki ürün de gitmedi.
                Assert.Equal(("0", "Tamamlandı", "Sevk Edilmedi"), urunler["A-16"]);
                Assert.Equal(("0", "İptal", "Sevk Edilmedi"), urunler["A-17"]);
                Assert.Equal(("0", "Trafoda Sevk", "Sevk Edilmedi"), urunler["A-18"]);
                Assert.Equal(("1", "Eksik", "Sevk Edilmedi"), urunler["A-19"]);
                Assert.Equal(("1", "Eksik", "Eksik Sevk (1/2)"), urunler["A-20"]);
                Assert.Equal(20, urunler.Count);
                Assert.EndsWith("/ 6 kalan", detay.Cell(3, 5).GetString());
            }

            var eksik = new EksikUrunlerRaporService(context);
            Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(await eksik.EksikUrunlerRaporuPdfOlusturAsync(projeId), 0, 4));
            using (var workbook = new XLWorkbook(new MemoryStream(await eksik.EksikUrunlerRaporuExcelOlusturAsync(projeId))))
            {
                // Normal eksik raporu kalanı olmayan satırlardan yalnız aktif saha aktarımlıları listeler (R-02); SEVK
                // aynı kuralla yalnız kendi sevkiyatıdır, aktarılan kısım Hedef Saha sütunundadır.
                var sevkler = Satirlar(workbook.Worksheet(1)).ToDictionary(r => r.Cell(3).GetString(), r => r.Cell(16).GetString());
                Assert.Equal(["A-1", "A-10", "A-12", "A-13", "A-14", "A-19", "A-2", "A-20", "A-3", "A-4", "A-5", "A-7", "A-8"],
                    sevkler.Keys.Order(StringComparer.Ordinal));
                Assert.Equal("Eksik Sevk (1/3)", sevkler["A-5"]);
                Assert.Equal("Eksik Sevk (1/2)", sevkler["A-12"]);
                Assert.Equal("Eksik Sevk (1/2)", sevkler["A-13"]);
                // İçeriği olmayan eski satır eksik raporunda da sandık numarasından okunur.
                Assert.Equal("Eksik Sevk (1/2)", sevkler["A-20"]);
                string[] eksikSevkler = ["A-5", "A-12", "A-13", "A-20"];
                Assert.All(sevkler.Where(p => !eksikSevkler.Contains(p.Key)), p => Assert.Equal("Sevk Edilmedi", p.Value));
            }
        });
    }

    /// <summary>
    /// Saha ve yedek raporunda çeki bağı olmayan sandık içeriği (saha manuel malzemesi) de kendi sandığının sevkinden
    /// okunur: sevkli sandıktaki içerik gitmiş, elde kalan sandıktaki gitmemiştir.
    /// </summary>
    [PostgresRaporFact]
    [Trait("Category", "Postgres")]
    public async Task SahaYedekRaporu_CekiBagiOlmayanIcerik_SandigininSevkindenOkunur()
    {
        await WithDatabase(async options =>
        {
            foreach (var tip in new[] { ProjeTipi.Saha, ProjeTipi.Yedek })
            {
                int projeId;
                await using (var seed = new AppDbContext(options))
                {
                    var proje = new Proje { ProjeNo = $"QA-SEVK-ICERIK-{tip}", Musteri = "Sentetik İçerik", ProjeTipiId = (int)tip };
                    var sevkli = new Sandik { Proje = proje, SandikNo = "1", Ad = "Sevkli", DurumId = (int)SandikDurum.Sevkedildi };
                    var elde = new Sandik { Proje = proje, SandikNo = "2", Ad = "Elde kapalı", DurumId = (int)SandikDurum.Kapandi };
                    sevkli.SandikIcerikleri.Add(new SandikIcerik
                    {
                        Sandik = sevkli, BarkodNo = "M-1", Isim = "Saha manuel malzeme", Miktar = 2, TahsisMiktari = 2, KonulanAdet = 2
                    });
                    elde.SandikIcerikleri.Add(new SandikIcerik
                    {
                        Sandik = elde, BarkodNo = "M-2", Isim = "Elde kalan malzeme", Miktar = 1, TahsisMiktari = 1, KonulanAdet = 1
                    });
                    proje.Sandiklar.Add(sevkli);
                    proje.Sandiklar.Add(elde);
                    seed.Projeler.Add(proje);
                    await seed.SaveChangesAsync();
                    projeId = proje.Id;
                }

                await using var context = new AppDbContext(options);
                var gerceklesen = Gerceklesen(context);
                var excel = tip == ProjeTipi.Saha
                    ? await gerceklesen.SahaGerceklesenCekiListesiRaporuExcelOlusturAsync(projeId)
                    : await gerceklesen.GerceklesenCekiListesiRaporuExcelOlusturAsync(projeId);
                using var workbook = new XLWorkbook(new MemoryStream(excel));
                var urunler = Satirlar(workbook.Worksheet("Gerçekleşen Çeki"))
                    .ToDictionary(r => r.Cell(3).GetString(), r => (Durum: r.Cell(11).GetString(), Sevk: r.Cell(12).GetString()));
                Assert.Equal(2, urunler.Count);
                Assert.Equal(("Tamamlandı", "Sevk Edildi"), urunler["M-1"]);
                Assert.Equal(("Tamamlandı", "Sevk Edilmedi"), urunler["M-2"]);
                Assert.Equal("1 / 2", workbook.Worksheet("Sandık Bilgileri").Cell(3, 9).GetString());
            }
        });
    }

    private static GerceklesenCekiListesiRaporService Gerceklesen(AppDbContext context) =>
        new(context, new SahaTamamlamaService(context));

    /// <summary>
    /// Dört sandık: 1 ve 2 sevk edildi, 3 kapandı (elde), 4 hazırlanıyor. B-5 sevkli 2 ile elde kalan 3'e bölünmüştür;
    /// B-6 hiçbir sandıkta değildir; B-7/B-8/B-9/B-10 sevkli sandığa atanmış ama içine konmamıştır; B-11 sevkli
    /// sandığa konmuş, Grid durumu sonradan Kapandı yapılmıştır; B-12 sevkli sandığa konmuş ama Grid İptal'dir;
    /// B-13 4'ün 2'si gelip sevkli sandığa konmuştur; B-14/B-15 elde kalan sandıkta Grid Kapandı ve trafoda;
    /// B-16/B-17 1 trafoda + 1 sandıkta, sırasıyla sevkli ve elde kalan sandıkta.
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
        Urun(12, 1, 1, GridDurum.Iptal, icerikler: [("2", 1)]);
        Urun(13, 4, 2, GridDurum.EksikGeldi, icerikler: [("1", 2)]);
        // Sandık 3 elde (sevkiyatı geri alınmış sandık gibi sevk öncesi durumda): Grid Kapandı ve trafodaki ürünü gitmedi.
        Urun(14, 1, 0, GridDurum.GridKapandi, icerikler: [("3", 0)]);
        Urun(15, 1, 0, GridDurum.TrafoSevk, trafo: 1, icerikler: [("3", 0)]);
        // Parçalı: 2'nin 1'i trafoda, 1'i 3K'ya gelip sandığa kondu.
        Urun(16, 2, 1, GridDurum.TrafoSevk, trafo: 1, icerikler: [("2", 1)]);
        Urun(17, 2, 1, GridDurum.TrafoSevk, trafo: 1, icerikler: [("3", 1)]);

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
