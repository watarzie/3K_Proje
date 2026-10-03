using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Exceptions;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;

namespace _3K.Application.Tests;

/// <summary>
/// Doğrudan PO girişi (belge bağımlılığı yok), proje × kategori grup dağıtımı, mevcut PO'ya kalem ekleme,
/// PO-1 benzersizliği, grup faturası, düzenli iş PO'su, tarife sürümlemesi (F12/R1 §11), PO-gider net sonucu
/// ve PO/fatura kalıcı silme (RR Q7) gerçek PostgreSQL üzerinde.
/// </summary>
public sealed class FinansPoAkisPostgresTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;
    private static readonly DateTime Tarih = new(2026, 9, 20);

    /// <summary>§9 Ana Ambalaj ve kısmi sipariş örnekleri: 12,50 m³ × 350 EUR = 4.375 EUR; PO-A önce 3.000, sonra kalan.</summary>
    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Grup_po_kismi_sonra_mevcut_poya_kalem_ekleme_ve_grup_faturasi()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            await using var db = new AppDbContext(options);
            var sandiklar = new[] { Sandik("1", 5.00m), Sandik("2", 4.50m), Sandik("3", 3.00m) };
            db.AddRange(sandiklar); await db.SaveChangesAsync();
            var finans = Finans(db);
            var ids = sandiklar.Select(x => x.Id).ToArray();

            var grup = Assert.Single((await finans.SiparisFatura.SiparisGruplariAsync(
                new(Kategoriler: [FinansIsKategorisi.AnaAmbalaj]), Ct)).Items);
            Assert.Equal((4375m, 4375m, 12.5m, 3), (grup.NetTutar, grup.KalanSiparisNetTutar, grup.AhsapM3, grup.KalemSayisi));
            Assert.Equal(ids, grup.IsKaydiIds);

            var po = await finans.SiparisFatura.SiparisOlusturAsync(new("po-a", Tarih, "Müşteri PO", [], "EUR",
                Gruplar: [new FinansSiparisGrupDagitimModel(grup.IsKaydiIds, 3000m)]), Ct);
            Assert.Equal("PO-A", po.PoNumarasi);
            Assert.Equal([1750m, 1250m], po.Kalemler.OrderBy(x => x.IsKaydiId).Select(x => x.NetTutar));
            Assert.Equal(1375m, (await Grup(finans)).KalanSiparisNetTutar);
            Assert.Equal(FinansKarsilanma.Kismi, (await Grup(finans)).SiparisKarsilanma);

            db.ChangeTracker.Clear();
            await Assert.ThrowsAsync<IsKuraliException>(() => finans.SiparisFatura.SiparisOlusturAsync(
                new(" PO-A ", Tarih, null, [], Gruplar: [new FinansSiparisGrupDagitimModel(ids, 100m)]), Ct));
            await Assert.ThrowsAsync<IsKuraliException>(() => finans.SiparisFatura.SiparisKalemEkleAsync(po.Id,
                new(Gruplar: [new FinansSiparisGrupDagitimModel(ids, 1375.01m)]), Ct));

            db.ChangeTracker.Clear();
            var tam = (await finans.SiparisFatura.SiparisKalemEkleAsync(po.Id,
                new(Gruplar: [new FinansSiparisGrupDagitimModel(ids, 1375m)]), Ct))!;
            Assert.Equal(3, tam.Kalemler.Count);
            Assert.Equal(4375m, tam.Tutarlar.Single().NetTutar);
            Assert.All(tam.Kalemler, x => Assert.Equal(0m, x.KalanSiparisNetTutar));
            Assert.Empty((await finans.SiparisFatura.SiparisGruplariAsync(new(Kategoriler: [FinansIsKategorisi.AnaAmbalaj]), Ct)).Items);

            db.ChangeTracker.Clear();
            var satirlar = tam.Kalemler.Select(x => x.Id).ToArray();
            await Assert.ThrowsAsync<IsKuraliException>(() => finans.SiparisFatura.FaturaOlusturAsync(
                new(po.Id, "F-ASIM", Tarih, null, [], Gruplar: [new FinansFaturaGrupKalemiModel(satirlar, 4375.01m)]), Ct));
            db.ChangeTracker.Clear();
            var fatura = await finans.SiparisFatura.FaturaOlusturAsync(
                new(po.Id, "F-1", Tarih, null, [], Gruplar: [new FinansFaturaGrupKalemiModel(satirlar, 2000m)]), Ct);
            Assert.Equal(2000m, fatura.Kalemler.Sum(x => x.NetTutar));
            Assert.All(fatura.Kalemler, x => Assert.Equal(FinansIsKategorisi.AnaAmbalaj, x.Kategori));
            var durum = await Grup(finans, sadeceBekleyen: false);
            Assert.Equal((FinansKarsilanma.Tam, FinansKarsilanma.Kismi), (durum.SiparisKarsilanma, durum.FaturaKarsilanma));

            db.ChangeTracker.Clear();
            await Assert.ThrowsAsync<IsKuraliException>(() => finans.SiparisFatura.SiparisIptalAsync(po.Id, "Aktif faturalı PO", Ct));
        });
    }

    /// <summary>§10: eşzamanlı yeni PO ve mevcut PO'ya kalem ekleme aynı grubun kapasitesini birlikte aşamaz (serializable).</summary>
    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Eszamanli_grup_dagitimi_ve_kalem_ekleme_kapasiteyi_asamaz()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            int[] ids; int mevcutPoId;
            await using (var db = new AppDbContext(options))
            {
                var sandiklar = new[] { Sandik("1", 5.00m), Sandik("2", 4.50m), Sandik("3", 3.00m) };
                var diger = OzelIs("Başka iş", 100m, "EUR");
                db.AddRange(sandiklar); db.Add(diger); await db.SaveChangesAsync();
                ids = sandiklar.Select(x => x.Id).ToArray();
                mevcutPoId = (await Finans(db).SiparisFatura.SiparisOlusturAsync(Po("PO-MEVCUT", diger.Id, 100m), Ct)).Id;
            }

            async Task<bool> Dene(Func<FinansTestServisleri, Task> islem)
            {
                await using var eszamanli = new AppDbContext(options);
                try { await islem(Finans(eszamanli)); return true; }
                catch (IsKuraliException) { return false; }
            }
            var sonuclar = await Task.WhenAll(
                Dene(f => f.SiparisFatura.SiparisOlusturAsync(new("PO-YENI", Tarih, null, [], Gruplar: [new FinansSiparisGrupDagitimModel(ids, 3000m)]), Ct)),
                Dene(f => f.SiparisFatura.SiparisKalemEkleAsync(mevcutPoId, new(Gruplar: [new FinansSiparisGrupDagitimModel(ids, 3000m)]), Ct)));

            Assert.Single(sonuclar, x => x);
            await using var dogrula = new AppDbContext(options);
            Assert.Equal(3000m, await dogrula.Set<FinansSiparisKalemi>().Where(x => ids.Contains(x.FinansIsKaydiId)).SumAsync(x => x.NetTutarSnapshot));
        });
    }

    /// <summary>PO-1: PO numarası yalnız iptal edilmemiş PO'lar arasında benzersizdir; iptal edilen numara yeniden girilebilir.</summary>
    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Iptal_edilen_po_numarasi_dogru_kayitla_yeniden_girilebilir()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            await using var db = new AppDbContext(options);
            var is_ = OzelIs("Kira", 1000m, "EUR"); db.Add(is_); await db.SaveChangesAsync();
            var finans = Finans(db);

            var yanlis = await finans.SiparisFatura.SiparisOlusturAsync(Po("4500123456", is_.Id, 400m), Ct);
            await finans.SiparisFatura.SiparisIptalAsync(yanlis.Id, "Yanlış tutarla girildi", Ct);
            db.ChangeTracker.Clear();
            var dogru = await finans.SiparisFatura.SiparisOlusturAsync(Po("4500123456", is_.Id, 1000m), Ct);

            Assert.NotEqual(yanlis.Id, dogru.Id);
            Assert.Equal(2, await db.Set<FinansSiparis>().CountAsync(x => x.PoNumarasi == "4500123456"));
            Assert.Equal(FinansIsDurumu.SiparisAcildi, (await finans.IsKaydi.IsKaydiGetirAsync(is_.Id, Ct))!.Durum);
        });
    }

    /// <summary>F15: düzenli işin gelecek ayları idempotent üretilir ve tek yıllık PO bütün aylara dağıtılır.</summary>
    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Duzenli_is_donemleri_uretilir_ve_yillik_po_aylara_dagitilir()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            await using var db = new AppDbContext(options);
            var kira = new FinansDuzenliIs
            {
                IsAdi = "3K Seymen Kira", IsTuru = FinansIsTuru.OzelIs, OzelIsTuru = "Depolama", Musteri = "GE Vernova",
                ManuelProjeNo = "BAĞIMSIZ", BaslangicTarihi = new(2026, 1, 1), BitisTarihi = new(2026, 12, 31), OlusturmaGunu = 1,
                Miktar = 1, Birim = "Hizmet", BirimFiyat = 360000m, ParaBirimi = "TRY", KdvOrani = 20, Aktif = true,
                HesaplamaYontemi = FinansHesaplamaYontemi.SabitAylik, RaporGrubu = "Kira"
            };
            db.Add(kira); await db.SaveChangesAsync();
            var finans = Finans(db);

            Assert.Equal(12, (await finans.DuzenliIs.DuzenliIsDonemleriniUretAsync(kira.Id, new(2027, 3, 31), Ct))!.Olusturulan);
            Assert.Equal(0, (await finans.DuzenliIs.DuzenliIsDonemleriniUretAsync(kira.Id, new(2026, 12, 31), Ct))!.Olusturulan);
            Assert.Equal(0, (await finans.DuzenliIs.DuzenliIsDonemiOlusturAsync(new(2026, 12, 31), Ct)).Olusturulan);
            await Assert.ThrowsAsync<IsKuraliException>(() => finans.DuzenliIs.DuzenliIsDonemleriniUretAsync(kira.Id, new(2030, 1, 1), Ct));

            var grup = Assert.Single((await finans.SiparisFatura.SiparisGruplariAsync(new(Kategoriler: [FinansIsKategorisi.DuzenliIs]), Ct)).Items);
            Assert.Equal((12, 4320000m, "TRY", kira.Id), (grup.KalemSayisi, grup.KalanSiparisNetTutar, grup.ParaBirimi, grup.DuzenliIsId!.Value));
            // Düzenli işten PO açma: seçici yalnız o işin dönemlerini getirir.
            Assert.Equal(grup.IsKaydiIds, Assert.Single((await finans.SiparisFatura.SiparisGruplariAsync(new(DuzenliIsId: kira.Id), Ct)).Items).IsKaydiIds);
            Assert.Empty((await finans.SiparisFatura.SiparisGruplariAsync(new(DuzenliIsId: kira.Id + 1000), Ct)).Items);

            var po = await finans.SiparisFatura.SiparisOlusturAsync(new("KIRA-2026", Tarih, "Yıllık kira PO", [], "TRY",
                Gruplar: [new FinansSiparisGrupDagitimModel(grup.IsKaydiIds, 4320000m)]), Ct);
            Assert.Equal(12, po.Kalemler.Count);
            Assert.All(po.Kalemler, x => Assert.Equal((360000m, FinansIsKategorisi.DuzenliIs), (x.NetTutar, x.Kategori)));
            var aylik = await finans.AylikOzet.AylikOzetSayfaliAsync(2026, 10, new(), Ct);
            var ekim = Assert.Single(aylik.Items, x => x.DuzenliIsKategori());
            Assert.Equal((FinansKarsilanma.Tam, FinansKarsilanma.Yok), (ekim.SiparisKarsilanma, ekim.FaturaKarsilanma));
        });
    }

    /// <summary>
    /// F12/R1 §11: tarife sonradan ve geçerlilik tarihiyle girildiğinde fiyatsız üretim kalemleri o tarihin sürümüyle
    /// fiyatlanır; fiyat değişikliği eski sürümü kapatır, fiyatlanmış kaydın snapshot'ını değiştirmez.
    /// </summary>
    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Geriye_donuk_tarife_fiyatsiz_ana_ambalaji_fiyatlar_fiyat_degisikligi_gecmisi_korur()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            await using var db = new AppDbContext(options);
            var eylul = Sandik("1", 2m, new DateTime(2026, 9, 10), fiyat: 0m);
            db.Add(eylul); await db.SaveChangesAsync();
            var finans = Finans(db);
            var eslesme = new FinansUrunEslesmesiModel(null, FinansIsTuru.AnaAmbalaj, null, null, null, null, null, null);

            var urun = await finans.Urun.UrunOlusturAsync(new("AMB", "Ana ambalaj", FinansFiyatlandirmaBirimi.Metrekup, true, 0,
                [eslesme], 550m, "EUR", 20, new DateTime(2026, 9, 26)), Ct);
            db.ChangeTracker.Clear();
            Assert.Equal(0m, (await finans.IsKaydi.IsKaydiGetirAsync(eylul.Id, Ct))!.NetTutar);

            var kayitliEslesme = eslesme with { Id = urun.Eslesmeler.Single().Id };
            await finans.Urun.UrunGuncelleAsync(urun.Id, new("AMB", "Ana ambalaj", FinansFiyatlandirmaBirimi.Metrekup, true, 0,
                [kayitliEslesme], 550m, "EUR", 20, new DateTime(2026, 9, 1)), Ct);
            db.ChangeTracker.Clear();
            var fiyatli = (await finans.IsKaydi.IsKaydiGetirAsync(eylul.Id, Ct))!;
            Assert.Equal((1100m, FinansIsDurumu.SiparisBekliyor), (fiyatli.NetTutar, fiyatli.Durum));

            await finans.Urun.UrunGuncelleAsync(urun.Id, new("AMB", "Ana ambalaj", FinansFiyatlandirmaBirimi.Metrekup, true, 0,
                [kayitliEslesme], 600m, "EUR", 20, new DateTime(2026, 10, 1)), Ct);
            db.ChangeTracker.Clear();
            var surumler = await db.Set<FinansFiyatTarifesi>().AsNoTracking().OrderBy(x => x.GecerlilikBaslangici)
                .Select(x => new { x.GecerlilikBaslangici, x.GecerlilikBitisi, x.BirimFiyat }).ToListAsync();
            Assert.Equal(
                [(new DateTime(2026, 9, 1), new DateTime(2026, 9, 25), 550m), (new DateTime(2026, 9, 26), new DateTime(2026, 9, 30), 550m),
                 (new DateTime(2026, 10, 1), FinansTarifeKurallari.AcikUcluBitis, 600m)],
                surumler.Select(x => (x.GecerlilikBaslangici, x.GecerlilikBitisi, x.BirimFiyat)));
            Assert.Equal(1100m, (await finans.IsKaydi.IsKaydiGetirAsync(eylul.Id, Ct))!.NetTutar);
        });
    }

    /// <summary>PO'nun net sonucu para birimi bazında faturalanan gelir − ilişkili gider; gider çevrilmez.</summary>
    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Poya_baglanan_gider_po_net_sonucuna_girer_ve_kalici_silmeyi_engeller()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            await using var db = new AppDbContext(options);
            var is_ = OzelIs("Nakliye", 1000m, "EUR");
            var bos = OzelIs("Boş PO işi", 500m, "EUR");
            var kategori = new FinansGiderKategori { Ad = "Nakliye", Aktif = true };
            db.AddRange(is_, bos, kategori); await db.SaveChangesAsync();
            var finans = Finans(db);

            var po = await finans.SiparisFatura.SiparisOlusturAsync(Po("PO-NET", is_.Id, 1000m), Ct);
            var gider = await finans.Gider.GiderOlusturAsync(Gider(kategori.Id, 300m, "EUR", po.Id), Ct);
            await finans.Gider.GiderOlusturAsync(Gider(kategori.Id, 1000m, "TRY", po.Id), Ct);
            Assert.Equal("PO-NET", gider.PoNumarasi);
            db.ChangeTracker.Clear();
            var kalem = Assert.Single((await finans.SiparisFatura.SiparisGetirAsync(po.Id, Ct))!.Kalemler);
            await finans.SiparisFatura.FaturaOlusturAsync(new(po.Id, "F-NET", Tarih, null, [new(kalem.Id, 0, 0, 1000m)]), Ct);
            db.ChangeTracker.Clear();

            var detay = (await finans.SiparisFatura.SiparisGetirAsync(po.Id, Ct))!;
            Assert.Equal(new FinansPoNetModel("EUR", 1000m, 1000m, 300m, 700m, 700m), detay.Netler.Single(x => x.ParaBirimi == "EUR"));
            Assert.Equal(new FinansPoNetModel("TRY", 0m, 0m, 1000m, -1000m, -1000m), detay.Netler.Single(x => x.ParaBirimi == "TRY"));
            Assert.Single((await finans.Gider.GiderlerAsync(new(PoNumarasi: "po-net", ParaBirimi: "EUR"), Ct)).Items);

            var onizleme = (await finans.KaliciSilme.KaliciSilOnizlemeAsync("Siparis", po.Id, Ct))!;
            Assert.False(onizleme.Silinebilir);
            Assert.Contains(onizleme.Engeller, x => x.Contains("F-NET"));
            Assert.Contains(onizleme.Engeller, x => x.Contains("Sentetik gider"));

            var bosPo = await finans.SiparisFatura.SiparisOlusturAsync(Po("PO-SIL", bos.Id, 500m), Ct);
            db.ChangeTracker.Clear();
            var silinebilir = (await finans.KaliciSilme.KaliciSilOnizlemeAsync("Siparis", bosPo.Id, Ct))!;
            Assert.True(silinebilir.Silinebilir);
            Assert.Single(silinebilir.BirlikteSilinecekler!, x => x.VarlikTuru == "SiparisKalemi");
            Assert.True(await finans.KaliciSilme.KaliciSilAsync(new("Siparis", bosPo.Id, silinebilir.Surum, true, "Yanlış açılan PO"), Ct));
            db.ChangeTracker.Clear();
            Assert.False(await db.Set<FinansSiparisKalemi>().AnyAsync(x => x.FinansSiparisId == bosPo.Id));
            var geriDonen = (await finans.IsKaydi.IsKaydiGetirAsync(bos.Id, Ct))!;
            Assert.Equal((FinansIsDurumu.SiparisBekliyor, 500m), (geriDonen.Durum, geriDonen.KalanSiparisNetTutar));
        });
    }

    /// <summary>D8: mahsubu yapılmış avans iptal edilemez.</summary>
    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Mahsubu_olan_avans_iptal_edilemez()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            await using var db = new AppDbContext(options);
            var kategori = new FinansGiderKategori { Ad = "Personel", Aktif = true }; db.Add(kategori); await db.SaveChangesAsync();
            var finans = Finans(db);
            var avans = await finans.Gider.GiderOlusturAsync(Gider(kategori.Id, 1000m, "TRY") with { AvansMi = true }, Ct);
            var mahsup = await finans.Gider.GiderOlusturAsync(Gider(kategori.Id, 400m, "TRY") with { MahsupEdilenAvansId = avans.Id }, Ct);

            await Assert.ThrowsAsync<IsKuraliException>(() => finans.Gider.GiderIptalAsync(avans.Id, "Avans geri alındı", Ct));
            db.ChangeTracker.Clear();
            Assert.True(await finans.Gider.GiderIptalAsync(mahsup.Id, "Mahsup hatalı", Ct));
            db.ChangeTracker.Clear();
            Assert.True(await finans.Gider.GiderIptalAsync(avans.Id, "Avans geri alındı", Ct));
        });
    }

    /// <summary>RR Q5: sipariş durum raporu seçilen karşılanmadaki kalemleri kategori filtresiyle listeler (400 yok).</summary>
    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Siparis_durum_filtresi_ve_kategori_kalem_duzeyinde_uygulanir_proje_ozetleri_birlesik()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            await using var db = new AppDbContext(options);
            var ana1 = Sandik("1", 1m); var ana2 = Sandik("2", 1m);
            var sarf = Sandik("S", 1m); sarf.IsTuru = FinansIsTuru.SarfKereste; sarf.KaynakBileseni = "SARF";
            db.AddRange(ana1, ana2, sarf); await db.SaveChangesAsync();
            var finans = Finans(db);
            await finans.SiparisFatura.SiparisOlusturAsync(Po("PO-ANA", ana1.Id, 350m), Ct);
            await finans.SiparisFatura.SiparisOlusturAsync(Po("PO-ANA2", ana2.Id, 100m), Ct);

            var kismi = await finans.RaporVerisi.RaporVerisiAsync(new(SiparisKarsilanma: FinansKarsilanma.Kismi), Ct);
            Assert.Equal([ana2.Id], kismi.Isler.Select(x => x.Id));
            var acilmayan = await finans.RaporVerisi.RaporVerisiAsync(
                new(SiparisKarsilanma: FinansKarsilanma.Yok, Kategoriler: [FinansIsKategorisi.SarfKereste]), Ct);
            Assert.Equal([sarf.Id], acilmayan.Isler.Select(x => x.Id));
            Assert.NotEmpty(await finans.Raporlar.SiparisDurumExcelAsync(new(Kategoriler: [FinansIsKategorisi.AnaAmbalaj]), Ct));

            var aylik = await finans.AylikOzet.AylikOzetSayfaliAsync(2026, 9, new(), Ct);
            var proje = Assert.Single(aylik.Projeler);
            Assert.Equal((FinansKarsilanma.Kismi, FinansIsDurumu.KismiSiparis, 3), (proje.SiparisKarsilanma, proje.Durum, proje.GerekliKalem));
            var panel = await finans.Panel.PanelAsync(new(2026, 9, 1), new(2026, 9, 30), Ct);
            Assert.Equal((1, 0, 1, 0), (panel.ProjeDurumlari!.ToplamProje, panel.ProjeDurumlari.SiparisYok,
                panel.ProjeDurumlari.SiparisKismi, panel.ProjeDurumlari.SiparisTam));

            // Panel detay dökümü: proje satırı bütün kalemleri toplar, durum aylık özetteki birleşik durumla aynıdır.
            var ozet = Assert.Single(panel.ProjeOzetleri!);
            Assert.Equal((3, 450m, 0m, FinansKarsilanma.Kismi, FinansKarsilanma.Yok),
                (ozet.IsKaydi, ozet.SiparisNetTutar, ozet.FaturalananNetTutar, ozet.Siparis, ozet.Fatura));
            Assert.Equal(ozet.IsBedeli - 450m, ozet.KalanSiparisNetTutar);
            Assert.Equal([(FinansIsKategorisi.AnaAmbalaj, 2, 1), (FinansIsKategorisi.SarfKereste, 1, 1)],
                panel.KategoriOzetleri!.Select(x => (x.Kategori, x.IsKaydi, x.Proje)));
        });
    }

    private static async Task<FinansSiparisGrubuModel> Grup(FinansTestServisleri finans, bool sadeceBekleyen = true)
        => Assert.Single((await finans.SiparisFatura.SiparisGruplariAsync(
            new(Kategoriler: [FinansIsKategorisi.AnaAmbalaj], SadeceSiparisBekleyen: sadeceBekleyen), Ct)).Items);

    private static FinansIsKaydi Sandik(string no, decimal m3, DateTime? finansTarihi = null, decimal fiyat = 350m)
    {
        var tarih = finansTarihi ?? Tarih;
        return new FinansIsKaydi
        {
            ProjeNo = "PA727-03", Musteri = "GE Vernova", IsTuru = FinansIsTuru.AnaAmbalaj, IsAdi = $"Normal Ambalaj - {no}",
            SandikNo = no, SandikTipi = "Ahşap Kapalı", SandikCinsi = AmbalajSandikCinsi.AhsapKapali,
            Adet = 1, Birim = "m³", BirimM3 = m3, ToplamM3 = m3,
            FiyatlandirmaBirimiSnapshot = FinansFiyatlandirmaBirimi.Metrekup, BirimFiyatSnapshot = fiyat,
            ParaBirimiSnapshot = "EUR", KdvOraniSnapshot = 20, TarifeYiliSnapshot = fiyat > 0 ? 2026 : null,
            UretimTarihi = tarih, FinansTarihi = tarih, FinansDonemi = FinansTarihKurallari.AyBasi(tarih), KayitTarihi = tarih,
            KaynakTuru = FinansKaynakTurleri.AmbalajUretim, KaynakKayitId = Guid.NewGuid().ToString("D"), KaynakAktif = true
        };
    }

    private static FinansIsKaydi OzelIs(string ad, decimal tutar, string paraBirimi) => new()
    {
        IsAdi = ad, ProjeNo = "BAĞIMSIZ", Musteri = "GE Vernova", IsTuru = FinansIsTuru.OzelIs, KaynakTuru = FinansKaynakTurleri.Manuel,
        KaynakAktif = true, Adet = 1, Birim = "Hizmet", FiyatlandirmaBirimiSnapshot = FinansFiyatlandirmaBirimi.SabitTutar,
        BirimFiyatSnapshot = tutar, ParaBirimiSnapshot = paraBirimi, KdvOraniSnapshot = 20,
        UretimTarihi = Tarih, FinansTarihi = Tarih, FinansDonemi = FinansTarihKurallari.AyBasi(Tarih), KayitTarihi = Tarih
    };

    private static FinansSiparisOlusturModel Po(string no, int isKaydiId, decimal tutar)
        => new(no, Tarih, null, [new(isKaydiId, 0, 0, NetTutar: tutar)]);

    private static FinansGiderKaydetModel Gider(int kategori, decimal tutar, string paraBirimi, int? siparisId = null)
        => new(Tarih, new(2026, 9, 1), kategori, null, null, "QA Kişi", "Sentetik gider", 1, "Adet", tutar, paraBirimi,
            false, 0, null, null, null, "BELGE-1", FinansSiparisId: siparisId);

    private static FinansTestServisleri Finans(AppDbContext db) => new(db, new Kullanici());

    private sealed class Kullanici : ICurrentUserService
    {
        public int? UserId => 7;
        public bool IsAuthenticated => true;
    }
}

internal static class FinansAylikIsTestUzantilari
{
    public static bool DuzenliIsKategori(this FinansAylikIsModel satir) => satir.Kategori == FinansIsKategorisi.DuzenliIs;
}
