using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Exceptions;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;

namespace _3K.Application.Tests;

/// <summary>
/// Hatalı tarife düzeltme gerçek PostgreSQL üzerinde, canlıdaki vakayla: 565'ten 500'e yanlışlıkla düşürülen ve aynı gün
/// kalemi fiyatlanmış sürüm ekrandan aynı gün düzeltilemez; düzeltme işlemi yalnız o sürümün tarife fiyatlı açık
/// kalemini 565'e çeker, PO'lu/elle fiyatlı/iptal kalemi ve önceki sürümü korur, gerekçeyi denetime yazar.
/// </summary>
public sealed class FinansTarifeDuzeltmePostgresTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;
    private static readonly DateTime Ekim3 = new(2026, 10, 3);
    private static readonly DateTime Ekim4 = new(2026, 10, 4);
    private const string Gerekce = "500 yanlışlıkla girildi";

    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Yanlis_dusurulen_surum_duzeltilir_yalniz_o_surumle_tarifeden_fiyatlanan_acik_kalem_degisir()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            await using var db = new AppDbContext(options);
            var finans = new FinansTestServisleri(db, new Kullanici());
            static FinansUrunKaydetModel Urun(string kod, decimal fiyat, DateTime baslangic) =>
                new(kod, "AĞAÇ KERESTE", FinansFiyatlandirmaBirimi.Metrekup, true, 1, [], fiyat, "EUR", 20m, baslangic);

            var urun = await finans.Urun.UrunOlusturAsync(Urun("002", 565m, new DateTime(2026, 1, 1)), Ct);
            db.ChangeTracker.Clear();
            urun = (await finans.Urun.UrunGuncelleAsync(urun.Id, Urun("002", 500m, Ekim4), Ct))!;
            db.ChangeTracker.Clear();
            var onceki = urun.Tarifeler.Single(x => x.BirimFiyat == 565m);
            var hatali = urun.Tarifeler.Single(x => x.BirimFiyat == 500m);
            Assert.Equal(Ekim3, onceki.GecerlilikBitisi.Date);

            FinansIsKaydi Kalem(string no, DateTime tarih, FinansTarifeSurumuModel surum) => new()
            {
                ProjeNo = "PA727-03", Musteri = "GE Vernova", IsTuru = FinansIsTuru.AnaAmbalaj, IsAdi = $"Kereste - {no}",
                SandikNo = no, Adet = 1, Birim = "m³", BirimM3 = 2m, ToplamM3 = 2m, FinansUrunId = urun.Id,
                TarifeIdSnapshot = surum.Id, TarifeYiliSnapshot = 2026, FiyatlandirmaBirimiSnapshot = FinansFiyatlandirmaBirimi.Metrekup,
                BirimFiyatSnapshot = surum.BirimFiyat, ParaBirimiSnapshot = "EUR", KdvOraniSnapshot = 20m,
                UretimTarihi = tarih, FinansTarihi = tarih, FinansDonemi = FinansTarihKurallari.AyBasi(tarih), KayitTarihi = tarih,
                KaynakTuru = FinansKaynakTurleri.AmbalajUretim, KaynakKayitId = Guid.NewGuid().ToString("D"), KaynakAktif = true
            };
            var acik = Kalem("1", Ekim4, hatali);
            var poLu = Kalem("2", Ekim4, hatali);
            var elle = Kalem("3", Ekim4, hatali);
            elle.BirimFiyatSnapshot = 480m;
            var iptal = Kalem("4", Ekim4, hatali);
            iptal.IptalEdildi = true;
            var oncekiSurumle = Kalem("5", Ekim3, onceki);
            db.AddRange(acik, poLu, elle, iptal, oncekiSurumle);
            await db.SaveChangesAsync();
            await finans.SiparisFatura.SiparisOlusturAsync(new("PO-KERESTE", Ekim4, null, [new(poLu.Id, 0, 0, NetTutar: 1000m)]), Ct);
            db.ChangeTracker.Clear();

            // Canlıdaki hata: sürümle fiyatlanmış kalem olduğu için ekrandan aynı gün 565'e dönülemez.
            var ekran = await Assert.ThrowsAsync<IsKuraliException>(() => finans.Urun.UrunGuncelleAsync(urun.Id, Urun("002", 565m, Ekim4), Ct));
            Assert.Contains("geçmiş fiyat değiştirilemez", ekran.Message);
            db.ChangeTracker.Clear();

            var onizleme = (await finans.Urun.TarifeDuzeltmeOnizleAsync(urun.Id, hatali.Id, 565m, 20m, Ct))!;
            Assert.Equal((1, 2), (onizleme.DuzeltilecekKalemSayisi, onizleme.KorunanKalemSayisi));
            Assert.Equal((1000m, 1130m), (onizleme.Mevcut.NetTutar, onizleme.Yeni.NetTutar));
            Assert.Equal([poLu.Id, elle.Id, acik.Id], onizleme.Kalemler.Select(x => x.IsKaydiId));
            Assert.Equal(
                [FinansTarifeDuzeltmeKurali.PoyaBagli, FinansTarifeDuzeltmeKurali.ElleFiyat, null],
                onizleme.Kalemler.Select(x => x.KorunmaNedeni));
            Assert.Null(await finans.Urun.TarifeDuzeltmeOnizleAsync(urun.Id, 999999, 565m, 20m, Ct));
            Assert.Equal(500m, (await finans.IsKaydi.IsKaydiGetirAsync(acik.Id, Ct))!.BirimFiyat);
            db.ChangeTracker.Clear();

            await Assert.ThrowsAsync<IsKuraliException>(() => finans.Urun.TarifeDuzeltAsync(urun.Id, hatali.Id, new(565m, 20m, "kısa"), Ct));
            db.ChangeTracker.Clear();
            var baskaUrun = await finans.Urun.UrunOlusturAsync(Urun("003", 100m, new DateTime(2026, 1, 1)), Ct);
            db.ChangeTracker.Clear();
            Assert.Null(await finans.Urun.TarifeDuzeltAsync(baskaUrun.Id, hatali.Id, new(565m, 20m, Gerekce), Ct));
            db.ChangeTracker.Clear();

            var sonuc = (await finans.Urun.TarifeDuzeltAsync(urun.Id, hatali.Id, new(565m, 20m, Gerekce), Ct))!;
            Assert.Equal((hatali.Id, 1, 2), (sonuc.TarifeId, sonuc.DuzeltilenKalemSayisi, sonuc.KorunanKalemSayisi));
            db.ChangeTracker.Clear();

            async Task<(decimal Fiyat, decimal Net)> Fiyat(int id)
            {
                var kayit = (await finans.IsKaydi.IsKaydiGetirAsync(id, Ct))!;
                return (kayit.BirimFiyat, kayit.NetTutar);
            }
            Assert.Equal((565m, 1130m), await Fiyat(acik.Id));
            Assert.Equal((500m, 1000m), await Fiyat(poLu.Id));
            Assert.Equal((480m, 960m), await Fiyat(elle.Id));
            Assert.Equal((565m, 1130m), await Fiyat(oncekiSurumle.Id));
            Assert.Equal(500m, (await db.Set<FinansIsKaydi>().AsNoTracking().SingleAsync(x => x.Id == iptal.Id)).BirimFiyatSnapshot);

            var surumler = await db.Set<FinansFiyatTarifesi>().AsNoTracking()
                .Where(x => x.FinansUrunId == urun.Id).OrderBy(x => x.GecerlilikBaslangici).ToListAsync();
            Assert.Equal(
                [(565m, new DateTime(2026, 1, 1), Ekim3), (565m, Ekim4, FinansTarifeKurallari.AcikUcluBitis)],
                surumler.Select(x => (x.BirimFiyat, x.GecerlilikBaslangici.Date, x.GecerlilikBitisi.Date)));

            Task<bool> Denetim(string varlik, int id) => db.Set<FinansDegisiklikGecmisi>().AnyAsync(x =>
                x.VarlikTuru == varlik && x.VarlikId == id && x.Islem == "Tarife Düzeltme" && x.Aciklama == Gerekce);
            Assert.True(await Denetim(nameof(FinansFiyatTarifesi), hatali.Id));
            Assert.True(await Denetim(nameof(FinansIsKaydi), acik.Id));
            Assert.False(await Denetim(nameof(FinansIsKaydi), poLu.Id));
            Assert.False(await Denetim(nameof(FinansIsKaydi), elle.Id));

            // Aynı değerle ikinci düzeltme bir düzeltme değildir.
            db.ChangeTracker.Clear();
            await Assert.ThrowsAsync<IsKuraliException>(() => finans.Urun.TarifeDuzeltAsync(urun.Id, hatali.Id, new(565m, 20m, Gerekce), Ct));
        });
    }

    private sealed class Kullanici : ICurrentUserService
    {
        public int? UserId => 7;
        public bool IsAuthenticated => true;
    }
}
