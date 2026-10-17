using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Exceptions;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;

namespace _3K.Application.Tests;

/// <summary>
/// Para birimi tablosu (F1), fatura vadesi ve ödeme işareti (D3), açık kalemlerin yeniden fiyatlanması (D4) ve
/// panel yaşlandırma kovaları (R1 §5) gerçek PostgreSQL üzerinde.
/// </summary>
public sealed class FinansVadeParaBirimiPostgresTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;
    private static readonly DateTime Tarih = new(2026, 9, 20);
    private static DateTime Bugun => TurkeyTime.Now.Date;

    /// <summary>§9 yeni para birimi: eklenen para birimi kart, özet ve seçimlerde kod değişmeden görünür; hiçbir tutar çevrilmez.</summary>
    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Yeni_para_birimi_kart_ve_ozetlerde_gorunur_pasif_para_birimi_yeni_secimde_kullanilamaz()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            await using var db = new AppDbContext(options);
            var kategori = new FinansGiderKategori { Ad = "Nakliye", Aktif = true };
            db.Add(kategori); await db.SaveChangesAsync();
            var finans = Finans(db);
            var ay = new DateTime(2026, 9, 1);
            var ayBitis = new DateTime(2026, 9, 30);

            Assert.Equal(["TRY", "EUR"], (await finans.Parametre.ParaBirimleriAsync(true, Ct)).Select(x => x.Kod));
            Assert.Equal(["TRY", "EUR"], (await finans.Panel.PanelAsync(ay, ayBitis, Ct)).Tutarlar.Select(x => x.ParaBirimi));
            var tanimsiz = await Assert.ThrowsAsync<IsKuraliException>(() => finans.Gider.GiderOlusturAsync(Gider(kategori.Id, 100m, "GBP"), Ct));
            Assert.Contains("GBP", tanimsiz.Message);

            var gbp = await finans.Parametre.ParaBirimiOlusturAsync(new(" gbp ", "İngiliz Sterlini", "£", true, 3), Ct);
            Assert.Equal("GBP", gbp.Kod);
            await Assert.ThrowsAsync<IsKuraliException>(() => finans.Parametre.ParaBirimiOlusturAsync(new("GBP", "Tekrar", null, true, 4), Ct));
            await Assert.ThrowsAsync<IsKuraliException>(() => finans.Parametre.ParaBirimiOlusturAsync(new("G1P", "Hatalı", null, true, 4), Ct));

            var panel = await finans.Panel.PanelAsync(ay, ayBitis, Ct);
            Assert.Equal(["TRY", "EUR", "GBP"], panel.Tutarlar.Select(x => x.ParaBirimi));
            Assert.Equal(["TRY", "EUR", "GBP"], panel.Alacaklar!.Select(x => x.ParaBirimi));
            Assert.Contains("GBP", (await finans.AylikOzet.AylikOzetSayfaliAsync(2026, 9, new(), Ct)).FinansOzeti.Select(x => x.ParaBirimi));

            var gider = await finans.Gider.GiderOlusturAsync(Gider(kategori.Id, 100m, "gbp"), Ct);
            Assert.Equal("GBP", gider.ParaBirimi);
            db.ChangeTracker.Clear();
            var pasif = await finans.Parametre.ParaBirimiGuncelleAsync(gbp.Id, new("GBP", "İngiliz Sterlini", "£", false, 3), Ct);
            Assert.False(pasif!.Aktif);
            await Assert.ThrowsAsync<IsKuraliException>(() => finans.Parametre.ParaBirimiGuncelleAsync(gbp.Id, new("USD", "Kod değişmez", null, true, 3), Ct));
            db.ChangeTracker.Clear();
            await Assert.ThrowsAsync<IsKuraliException>(() => finans.Gider.GiderOlusturAsync(Gider(kategori.Id, 50m, "GBP"), Ct));

            // Pasifleştirme geçmişi değiştirmez: mevcut GBP gideri kendi para birimiyle düzenlenir ve özetlerde kalır.
            db.ChangeTracker.Clear();
            var guncel = await finans.Gider.GiderGuncelleAsync(gider.Id, Gider(kategori.Id, 120m, "GBP"), Ct);
            Assert.Equal(120m, guncel!.Matrah);
            Assert.Equal(["TRY", "EUR"], (await finans.Parametre.ParaBirimleriAsync(true, Ct)).Select(x => x.Kod));
            Assert.Equal(["TRY", "EUR", "GBP"], (await finans.Parametre.ParaBirimleriAsync(false, Ct)).Select(x => x.Kod));
            Assert.Contains("GBP", (await finans.Panel.PanelAsync(ay, ayBitis, Ct)).Tutarlar.Select(x => x.ParaBirimi));
            Assert.True(await db.Set<FinansDegisiklikGecmisi>().AnyAsync(x =>
                x.VarlikTuru == nameof(FinansParaBirimi) && x.VarlikId == gbp.Id && x.AlanAdi == nameof(FinansParaBirimi.Aktif)));
        });
    }

    /// <summary>
    /// D3: vade varsayılanı parametredendir; kullanıcı vadesi gelen faturayı ödendi işaretler, uyarı listesi ve
    /// ödeme filtresi aynı kuralı kullanır; panel zamanında/geç ödeme istatistiğini ve beklenen tahsilatı verir.
    /// </summary>
    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Fatura_vadesi_odeme_isareti_uyarilar_filtre_rapor_ve_panel_istatistigi()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            await using var db = new AppDbContext(options);
            var is_ = OzelIs("Nakliye", 1200m, Bugun.AddDays(-90));
            db.Add(is_); await db.SaveChangesAsync();
            var finans = Finans(db);

            Assert.Equal(new FinansParametrelerModel(30, 7), await finans.Parametre.ParametrelerAsync(Ct));
            await Assert.ThrowsAsync<IsKuraliException>(() => finans.Parametre.ParametreleriKaydetAsync(new(-1, 7), Ct));
            await Assert.ThrowsAsync<IsKuraliException>(() => finans.Parametre.ParametreleriKaydetAsync(new(30, 91), Ct));
            Assert.Equal(new FinansParametrelerModel(45, 10), await finans.Parametre.ParametreleriKaydetAsync(new(45, 10), Ct));
            db.ChangeTracker.Clear();
            Assert.Equal(new FinansParametrelerModel(45, 10), await finans.Parametre.ParametrelerAsync(Ct));

            var po = await finans.SiparisFatura.SiparisOlusturAsync(new("PO-VADE", Bugun.AddDays(-90), null, [new(is_.Id, 0, 0, NetTutar: 1200m)]), Ct);
            var satir = Assert.Single(po.Kalemler).Id;
            async Task<FinansFaturaModel> Fatura(string no, DateTime tarih, decimal net, DateTime? vade = null)
            {
                db.ChangeTracker.Clear();
                return await finans.SiparisFatura.FaturaOlusturAsync(new(po.Id, no, tarih, null, [new(satir, 0, 0, net)], VadeTarihi: vade), Ct);
            }

            var gecen = await Fatura("F-GECEN", Bugun.AddDays(-60), 200m);
            Assert.Equal(Bugun.AddDays(-15), gecen.VadeTarihi);
            Assert.Equal((FinansOdemeDurumu.VadesiGecti, -15), (gecen.OdemeDurumu, gecen.VadeFarkiGun));
            var zamaninda = await Fatura("F-ZAMANINDA", Bugun.AddDays(-60), 200m, Bugun.AddDays(-10));
            await Assert.ThrowsAsync<IsKuraliException>(() => Fatura("F-HATALI", Bugun, 100m, Bugun.AddDays(-1)));
            var yaklasan = await Fatura("F-YAKLASAN", Bugun, 300m, Bugun.AddDays(5));
            var bekleyen = await Fatura("F-BEKLEYEN", Bugun, 300m, Bugun.AddDays(40));
            Assert.Equal((FinansOdemeDurumu.VadesiYaklasiyor, FinansOdemeDurumu.Bekliyor), (yaklasan.OdemeDurumu, bekleyen.OdemeDurumu));
            var iptal = await Fatura("F-IPTAL", Bugun, 200m);
            db.ChangeTracker.Clear();
            Assert.True(await finans.SiparisFatura.FaturaIptalAsync(iptal.Id, "Yanlış fatura", Ct));
            db.ChangeTracker.Clear();
            await Assert.ThrowsAsync<IsKuraliException>(() => finans.SiparisFatura.OdemeKaydetAsync(iptal.Id, new(true, Bugun, null), Ct));

            var uyarilar = await finans.SiparisFatura.VadeUyarilariAsync(20, Ct);
            Assert.Equal((10, 2, 1), (uyarilar.VadeUyariGunu, uyarilar.VadesiGecenAdet, uyarilar.YaklasanAdet));
            Assert.Equal(["F-GECEN", "F-ZAMANINDA", "F-YAKLASAN"], uyarilar.Kalemler.Select(x => x.FaturaNumarasi));
            Assert.Equal([-15, -10, 5], uyarilar.Kalemler.Select(x => x.KalanGun));
            Assert.Equal((240m, "EUR", "PO-VADE", "BAĞIMSIZ"), (uyarilar.Kalemler[0].ToplamTutar, uyarilar.Kalemler[0].ParaBirimi,
                uyarilar.Kalemler[0].PoNumarasi, uyarilar.Kalemler[0].ProjeNo));

            async Task<string[]> Liste(FinansOdemeFiltresi filtre)
                => (await finans.SiparisFatura.FaturalarAsync(new(OdemeDurumu: filtre), Ct)).Items.Select(x => x.FaturaNumarasi).ToArray();
            Assert.Equal(["F-GECEN", "F-ZAMANINDA"], await Liste(FinansOdemeFiltresi.VadesiGecen));
            Assert.Equal(["F-YAKLASAN"], await Liste(FinansOdemeFiltresi.VadesiYaklasan));
            Assert.Equal(["F-GECEN", "F-ZAMANINDA", "F-YAKLASAN", "F-BEKLEYEN"], await Liste(FinansOdemeFiltresi.Odenmedi));
            Assert.Empty(await Liste(FinansOdemeFiltresi.Odendi));

            // Ödeme dışarıdan gelir: ileri tarihli ve tarihsiz işaret reddedilir; işaret faturanın tamamını kapatır.
            db.ChangeTracker.Clear();
            await Assert.ThrowsAsync<IsKuraliException>(() => finans.SiparisFatura.OdemeKaydetAsync(gecen.Id, new(true, Bugun.AddDays(1), null), Ct));
            await Assert.ThrowsAsync<IsKuraliException>(() => finans.SiparisFatura.OdemeKaydetAsync(gecen.Id, new(true, null, null), Ct));
            db.ChangeTracker.Clear();
            var odendi = (await finans.SiparisFatura.OdemeKaydetAsync(gecen.Id, new(true, Bugun, "Banka dekontu"), Ct))!;
            Assert.Equal((true, (DateTime?)Bugun, FinansOdemeDurumu.GecOdendi, 15),
                (odendi.OdendiMi, odendi.OdemeTarihi, odendi.OdemeDurumu, odendi.VadeFarkiGun));
            db.ChangeTracker.Clear();
            await Assert.ThrowsAsync<IsKuraliException>(() => finans.SiparisFatura.FaturaIptalAsync(gecen.Id, "Ödenmiş fatura iptali", Ct));
            db.ChangeTracker.Clear();
            await Assert.ThrowsAsync<IsKuraliException>(() => finans.SiparisFatura.FaturaGuncelleAsync(gecen.Id,
                new("F-GECEN", gecen.FaturaTarihi, null, Kalemler: [new(satir, 0, 0, 150m)], Gerekce: "Tutar düzeltmesi"), Ct));
            db.ChangeTracker.Clear();
            await Assert.ThrowsAsync<IsKuraliException>(() => finans.SiparisFatura.OdemeKaydetAsync(gecen.Id, new(false, null, null), Ct));
            db.ChangeTracker.Clear();
            var geriAlinan = (await finans.SiparisFatura.OdemeKaydetAsync(gecen.Id, new(false, null, "Yanlış işaretlendi"), Ct))!;
            Assert.Equal((false, (DateTime?)null, FinansOdemeDurumu.VadesiGecti), (geriAlinan.OdendiMi, geriAlinan.OdemeTarihi, geriAlinan.OdemeDurumu));
            Assert.True(await db.Set<FinansDegisiklikGecmisi>().AnyAsync(x =>
                x.VarlikTuru == nameof(FinansFatura) && x.VarlikId == gecen.Id && x.Islem == "Ödeme Geri Alma" && x.Aciklama == "Yanlış işaretlendi"));
            db.ChangeTracker.Clear();
            await finans.SiparisFatura.OdemeKaydetAsync(gecen.Id, new(true, Bugun, null), Ct);
            db.ChangeTracker.Clear();
            var onceden = (await finans.SiparisFatura.OdemeKaydetAsync(zamaninda.Id, new(true, Bugun.AddDays(-12), null), Ct))!;
            Assert.Equal((FinansOdemeDurumu.ZamanindaOdendi, -2), (onceden.OdemeDurumu, onceden.VadeFarkiGun));
            Assert.Equal(["F-GECEN", "F-ZAMANINDA"], (await Liste(FinansOdemeFiltresi.Odendi)).Order());

            // Vade tarihi fatura düzenlemesinde değişebilir; fatura tarihinden önce olamaz.
            db.ChangeTracker.Clear();
            var ertelenen = (await finans.SiparisFatura.FaturaGuncelleAsync(bekleyen.Id,
                new("F-BEKLEYEN", Bugun, null, VadeTarihi: Bugun.AddDays(50)), Ct))!;
            Assert.Equal(Bugun.AddDays(50), ertelenen.VadeTarihi);
            db.ChangeTracker.Clear();
            await Assert.ThrowsAsync<IsKuraliException>(() => finans.SiparisFatura.FaturaGuncelleAsync(bekleyen.Id,
                new("F-BEKLEYEN", Bugun, null, VadeTarihi: Bugun.AddDays(-1)), Ct));

            db.ChangeTracker.Clear();
            var panel = await finans.Panel.PanelAsync(Bugun.AddDays(-30), Bugun, Ct);
            var eur = panel.Alacaklar!.Single(x => x.ParaBirimi == "EUR");
            Assert.Equal((720m, 2, 0m, 0, 360m, 1), (eur.BekleyenTutar, eur.BekleyenAdet, eur.VadesiGecenTutar, eur.VadesiGecenAdet, eur.YaklasanTutar, eur.YaklasanAdet));
            Assert.Equal((1, 1, 480m), (eur.ZamanindaOdenenAdet, eur.GecOdenenAdet, eur.OdenenTutar));
            Assert.Equal(720m, panel.BeklenenTahsilat!.Where(x => x.ParaBirimi == "EUR").Sum(x => x.Gelir));
            Assert.Equal(10, panel.VadeUyariGunu);
            // Genel durum sayıları belge tarihine göre aralıkta sayılır; iptal fatura sayılmaz.
            Assert.Equal((0, 0, 2, 0, 2, 0), (panel.Sayilar!.IsKaydi, panel.Sayilar.Po, panel.Sayilar.Fatura,
                panel.Sayilar.OdenenFatura, panel.Sayilar.OdenmemisFatura, panel.Sayilar.VadesiGecenFatura));
            var genel = await finans.Panel.PanelAsync(Bugun.AddDays(-120), Bugun, Ct);
            var sayilar = genel.Sayilar!;
            Assert.Equal((1, 1, 1, 0, 1, 0), (sayilar.IsKaydi, sayilar.Proje, sayilar.Po, sayilar.AcikPo, sayilar.KismiFaturaliPo, sayilar.KapaliPo));
            Assert.Equal((4, 2, 2, 0), (sayilar.Fatura, sayilar.OdenenFatura, sayilar.OdenmemisFatura, sayilar.VadesiGecenFatura));
            // Aylık grafik ilk veri ayından aralık sonuna kesintisizdir ve toplamları genel sayılarla aynıdır.
            Assert.Equal(FinansTarihKurallari.AyBasi(Bugun.AddDays(-90)).ToString("yyyy-MM"), genel.AylikSayilar![0].Ay);
            Assert.Equal(FinansTarihKurallari.AyBasi(Bugun).ToString("yyyy-MM"), genel.AylikSayilar[^1].Ay);
            Assert.Equal((1, 1, 4), (genel.AylikSayilar.Sum(x => x.IsKaydi), genel.AylikSayilar.Sum(x => x.Po), genel.AylikSayilar.Sum(x => x.Fatura)));

            var excel = await finans.Raporlar.AlacakExcelAsync(new(), Ct);
            using (var workbook = new XLWorkbook(new MemoryStream(excel)))
            {
                var sayfa = workbook.Worksheet("Alacaklar");
                Assert.Equal(5, sayfa.LastRowUsed()!.RowNumber());
                Assert.Equal("F-YAKLASAN", sayfa.Cell(2, 1).GetString());
                var ozet = workbook.Worksheet("Özet");
                Assert.Equal("EUR", ozet.Cell(2, 1).GetString());
                Assert.Equal(720m, ozet.Cell(2, 2).GetValue<decimal>());
            }
            Assert.NotEmpty(await finans.Raporlar.AlacakPdfAsync(new(OdemeDurumu: FinansOdemeFiltresi.Odenmedi), Ct));
        });
    }

    /// <summary>D4: tarife değişikliği açık kalemi kendiliğinden değiştirmez; açık yeniden fiyatlama yalnız PO'suz otomatik kalemi günceller.</summary>
    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Yeniden_fiyatlama_yalniz_posuz_otomatik_kalemleri_gecerli_tarifeyle_gunceller()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            await using var db = new AppDbContext(options);
            await FinansPostgresTests.PoOnayiniKapatAsync(db);
            var finans = Finans(db);
            var urun = await finans.Urun.UrunOlusturAsync(new("AHSAP-M3", "Ahşap m³", FinansFiyatlandirmaBirimi.Metrekup, true, 1, [],
                350m, "EUR", 20m, new DateTime(2026, 1, 1)), Ct);
            var eskiTarife = Assert.Single(urun.Tarifeler).Id;
            FinansIsKaydi Kalem(string no, DateTime tarih) => new()
            {
                ProjeNo = "PA727-03", Musteri = "GE Vernova", IsTuru = FinansIsTuru.AnaAmbalaj, IsAdi = $"Normal Ambalaj - {no}",
                SandikNo = no, SandikCinsi = AmbalajSandikCinsi.AhsapKapali, Adet = 1, Birim = "m³", BirimM3 = 2m, ToplamM3 = 2m,
                FinansUrunId = urun.Id, TarifeIdSnapshot = eskiTarife, TarifeYiliSnapshot = 2026,
                FiyatlandirmaBirimiSnapshot = FinansFiyatlandirmaBirimi.Metrekup, BirimFiyatSnapshot = 350m, ParaBirimiSnapshot = "EUR", KdvOraniSnapshot = 20m,
                UretimTarihi = tarih, FinansTarihi = tarih, FinansDonemi = FinansTarihKurallari.AyBasi(tarih), KayitTarihi = tarih,
                KaynakTuru = FinansKaynakTurleri.AmbalajUretim, KaynakKayitId = Guid.NewGuid().ToString("D"), KaynakAktif = true
            };
            var acik = Kalem("1", Tarih);
            var poLu = Kalem("2", Tarih);
            var agustos = Kalem("3", new DateTime(2026, 8, 20));
            var manuel = Kalem("4", Tarih); manuel.FinansMiktariManuel = true;
            db.AddRange(acik, poLu, agustos, manuel); await db.SaveChangesAsync();
            await finans.SiparisFatura.SiparisOlusturAsync(new("PO-ESKI", Tarih, null, [new(poLu.Id, 0, 0, NetTutar: 700m)]), Ct);
            db.ChangeTracker.Clear();

            await finans.Urun.UrunGuncelleAsync(urun.Id, new("AHSAP-M3", "Ahşap m³", FinansFiyatlandirmaBirimi.Metrekup, true, 1, [],
                400m, "EUR", 20m, new DateTime(2026, 9, 1)), Ct);
            db.ChangeTracker.Clear();
            Assert.Equal(700m, (await finans.IsKaydi.IsKaydiGetirAsync(acik.Id, Ct))!.NetTutar);

            await Assert.ThrowsAsync<IsKuraliException>(() => finans.Urun.YenidenFiyatlaAsync(urun.Id, "kısa", Ct));
            Assert.Null(await finans.Urun.YenidenFiyatlaAsync(999999, "Tarife güncellendi", Ct));
            db.ChangeTracker.Clear();
            var sonuc = (await finans.Urun.YenidenFiyatlaAsync(urun.Id, "Eylül tarifesi uygulandı", Ct))!;
            Assert.Equal((urun.Id, 2, 1), (sonuc.UrunId, sonuc.IncelenenKalem, sonuc.FiyatlananKalem));

            db.ChangeTracker.Clear();
            async Task<(decimal Fiyat, decimal Net)> Fiyat(int id)
            {
                var kayit = (await finans.IsKaydi.IsKaydiGetirAsync(id, Ct))!;
                return (kayit.BirimFiyat, kayit.NetTutar);
            }
            Assert.Equal((400m, 800m), await Fiyat(acik.Id));
            Assert.Equal((350m, 700m), await Fiyat(poLu.Id));
            Assert.Equal((350m, 700m), await Fiyat(agustos.Id));
            Assert.Equal((350m, 700m), await Fiyat(manuel.Id));
            Assert.True(await db.Set<FinansDegisiklikGecmisi>().AnyAsync(x =>
                x.VarlikId == acik.Id && x.Islem == "Yeniden Fiyatlandırma" && x.Aciklama == "Eylül tarifesi uygulandı"));
            db.ChangeTracker.Clear();
            Assert.Equal(0, (await finans.Urun.YenidenFiyatlaAsync(urun.Id, "Eylül tarifesi uygulandı", Ct))!.FiyatlananKalem);
        });
    }

    /// <summary>R1 §5: panel kovası (15+/30+/60+/90+) tıklanınca açılan bekleme listesiyle aynı kalemleri sayar.</summary>
    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Panel_yaslandirma_kovalari_bekleme_listesiyle_ayni_kalemleri_sayar()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            await using var db = new AppDbContext(options);
            var yeni = OzelIs("Yeni", 100m, Bugun.AddDays(-5));
            var yirmi = OzelIs("Yirmi gün", 200m, Bugun.AddDays(-20));
            var doksanBes = OzelIs("Doksan beş gün", 300m, Bugun.AddDays(-95));
            var faturaBekleyen = OzelIs("PO'lu iş", 1000m, Bugun.AddDays(-120));
            db.AddRange(yeni, yirmi, doksanBes, faturaBekleyen); await db.SaveChangesAsync();
            var finans = Finans(db);
            var po = await finans.SiparisFatura.SiparisOlusturAsync(new("PO-YAS", Bugun.AddDays(-35), null, [new(faturaBekleyen.Id, 0, 0, NetTutar: 1000m)]), Ct);
            db.ChangeTracker.Clear();
            await finans.SiparisFatura.FaturaOlusturAsync(new(po.Id, "F-YAS", Bugun, null, [new(po.Kalemler[0].Id, 0, 0, 400m)]), Ct);
            db.ChangeTracker.Clear();

            var panel = await finans.Panel.PanelAsync(FinansTarihKurallari.AyBasi(Bugun), Bugun, Ct);
            int Adet(string asama, int gun) => panel.Yaslandirma!.Single(x => x.Asama == asama && x.MinGun == gun).Adet;
            Assert.Equal([2, 1, 1, 1], new[] { 15, 30, 60, 90 }.Select(g => Adet(FinansBekleyenAsamalari.Siparis, g)));
            Assert.Equal([1, 1, 0, 0], new[] { 15, 30, 60, 90 }.Select(g => Adet(FinansBekleyenAsamalari.Fatura, g)));
            Assert.Equal(("EUR", 600m), panel.Yaslandirma!.Single(x => x.Asama == FinansBekleyenAsamalari.Fatura && x.MinGun == 30)
                .Tutarlar.Select(x => (x.ParaBirimi, x.NetTutar)).Single());
            foreach (var kova in panel.Yaslandirma!)
                Assert.Equal(kova.Adet, (await finans.Panel.YaslandirmaAsync(Bugun, kova.MinGun, kova.Asama, new(), Ct)).TotalCount);
            await Assert.ThrowsAsync<IsKuraliException>(() => finans.Panel.YaslandirmaAsync(Bugun, 15, "Diğer", new(), Ct));
        });
    }

    private static FinansIsKaydi OzelIs(string ad, decimal tutar, DateTime tarih) => new()
    {
        IsAdi = ad, ProjeNo = "BAĞIMSIZ", Musteri = "GE Vernova", IsTuru = FinansIsTuru.OzelIs, KaynakTuru = FinansKaynakTurleri.Manuel,
        KaynakAktif = true, Adet = 1, Birim = "Hizmet", FiyatlandirmaBirimiSnapshot = FinansFiyatlandirmaBirimi.SabitTutar,
        BirimFiyatSnapshot = tutar, ParaBirimiSnapshot = "EUR", KdvOraniSnapshot = 20,
        UretimTarihi = tarih, FinansTarihi = tarih, FinansDonemi = FinansTarihKurallari.AyBasi(tarih), KayitTarihi = tarih
    };

    private static FinansGiderKaydetModel Gider(int kategori, decimal tutar, string paraBirimi)
        => new(Tarih, new(2026, 9, 1), kategori, null, null, "QA Kişi", "Sentetik gider", 1, "Adet", tutar, paraBirimi,
            false, 0, null, null, null, "BELGE-1");

    private static FinansTestServisleri Finans(AppDbContext db) => new(db, new Kullanici());

    private sealed class Kullanici : ICurrentUserService
    {
        public int? UserId => 7;
        public bool IsAuthenticated => true;
    }
}
