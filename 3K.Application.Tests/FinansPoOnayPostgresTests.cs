using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using _3K.Application.Behaviors;
using _3K.Application.Common;
using _3K.Application.Features.FinansIslemleri.Commands;
using _3K.Application.Features.OnayIslemleri.Commands;
using _3K.Application.Features.OnayIslemleri.Queries;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Exceptions;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Repositories;
using _3K.Infrastructure.Services;

namespace _3K.Application.Tests;

/// <summary>
/// PO öncesi iş onayı gerçek PostgreSQL ve gerçek MediatR onay hattıyla: onaysız proje kalemi yeni PO'ya, mevcut PO'ya
/// (kalem ve grup yolu) ve PO tutar artışına bağlanamaz; "Onaya gönder" grup başına talep açar, Onay Merkezi kararı
/// (onay/ret) kalemlere yansır; onaycı detayı görür; kalem talepten sonra değişirse onay uygulanmaz; onaydan sonra
/// tutar değişirse onay düşer. Kural kapalıyken PO onay aramaz ve gönderme kalemi hemen onaylar. Geçiş betiği PO'ya
/// bağlı proje kayıtlarını uygulamanın net tutar kuralıyla aynı değerle onaylı sayar.
/// </summary>
public sealed class FinansPoOnayPostgresTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;
    private static readonly DateTime Tarih = new(2026, 10, 17);

    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task KuralAcik_OnaysizKalemPoyaBaglanmaz_OnayRetVeDusmeKalemlereYansir()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            await using var db = new AppDbContext(options);
            var talepEden = YeniKullanici("talep", 4);
            var onaylayan = YeniKullanici("onay", 4);
            var yetkisiz = YeniKullanici("kalite", 5);
            db.AddRange(talepEden, onaylayan, yetkisiz);
            db.Add(new OnayOperasyonKurali { IslemKodu = OnayIslemKodlari.FinansPoIsOnayi, OnayGerektirirMi = true });
            db.Add(new OnayIslemYetki { IslemKodu = OnayIslemKodlari.FinansPoIsOnayi, RolId = 4 });
            var ana = new[]
            {
                Kalem("1", FinansIsTuru.AnaAmbalaj, 1.0m, 100m, Tarih),
                Kalem("2", FinansIsTuru.AnaAmbalaj, 1.5m, 100m, Tarih.AddDays(1)),
                Kalem("3", FinansIsTuru.AnaAmbalaj, 2.0m, 100m, Tarih.AddDays(2))
            };
            var sarf = Kalem("1", FinansIsTuru.SarfKereste, 0.5m, 200m, Tarih);
            var ozel = OzelIs();
            db.AddRange(ana);
            db.AddRange(sarf, ozel);
            await db.SaveChangesAsync();
            var anaIds = ana.Select(x => x.Id).ToArray();
            var kullanici = new TestKullanici { UserId = talepEden.Id };
            await using var provider = Saglayici(db, kullanici);
            var mediator = provider.GetRequiredService<IMediator>();
            var siparis = provider.GetRequiredService<IFinansSiparisFaturaService>();
            var isKaydi = provider.GetRequiredService<IFinansIsKaydiService>();

            // 1) Onaysız proje grubu PO'ya bağlanamaz; özel iş onay kapsamı dışındadır.
            var grup = await TekGrup(siparis, FinansIsKategorisi.AnaAmbalaj);
            Assert.Equal((FinansPoOnayDurumu.OnayaSunulmadi, false, true), (grup.PoOnayDurumu, grup.PoyaBaglanabilir, grup.PoOnayaGonderilebilir));
            var ozelGrup = await TekGrup(siparis, FinansIsKategorisi.OzelIs);
            Assert.Equal((FinansPoOnayDurumu.Gerekmiyor, true, false), (ozelGrup.PoOnayDurumu, ozelGrup.PoyaBaglanabilir, ozelGrup.PoOnayaGonderilebilir));
            var red = await Assert.ThrowsAsync<IsKuraliException>(() => siparis.SiparisOlusturAsync(
                new("PO-1", Tarih, null, [], "EUR", Gruplar: [new FinansSiparisGrupDagitimModel(anaIds, 450m)]), Ct));
            Assert.Equal(409, red.StatusCode);
            Assert.Contains("PA727-03 · Normal Ambalaj - 1 (Onaya sunulmadı)", red.Message);
            db.ChangeTracker.Clear();
            var ozelPo = await siparis.SiparisOlusturAsync(new("PO-OZEL", Tarih, null, [new(ozel.Id, 0, 0, NetTutar: 50m)], "EUR"), Ct);
            db.ChangeTracker.Clear();
            await Assert.ThrowsAsync<IsKuraliException>(() => siparis.SiparisKalemEkleAsync(ozelPo.Id,
                new(Kalemler: [new FinansSiparisDagitimModel(ana[0].Id, 0, 0, NetTutar: 10m)]), Ct));
            db.ChangeTracker.Clear();
            await Assert.ThrowsAsync<IsKuraliException>(() => siparis.SiparisKalemEkleAsync(ozelPo.Id,
                new(Gruplar: [new FinansSiparisGrupDagitimModel(anaIds, 10m)]), Ct));
            db.ChangeTracker.Clear();
            Assert.Equal([ozel.Id], await db.Set<FinansSiparisKalemi>().Select(x => x.FinansIsKaydiId).ToListAsync());

            // 2) Onaya gönder: grup başına bir talep Onay Merkezi'ne düşer (202); özel iş atlanır, PO hâlâ açılmaz.
            var gonder = await mediator.Send(new FinansPoOnayGonderCommand { Model = new([.. anaIds, sarf.Id, ozel.Id]) });
            Assert.True(gonder.IsSuccess, gonder.Error?.Message);
            Assert.Equal(StatusConstants.ActionQueuedForApproval, gonder.StatusCode);
            Assert.Equal((2, 4, 1, 2, 0), (gonder.Value!.Talepler.Count, gonder.Value.KalemSayisi, gonder.Value.AtlananKalemSayisi,
                gonder.Value.OnayaAlinanTalepSayisi, gonder.Value.DogrudanOnaylananTalepSayisi));
            db.ChangeTracker.Clear();
            var kuyruk = await db.OnayBekleyenIslemler.AsNoTracking().OrderBy(x => x.Id).ToListAsync();
            Assert.Equal(2, kuyruk.Count);
            Assert.All(kuyruk, x => Assert.Equal(
                (OnayIslemKodlari.FinansPoIsOnayi, OnayReferansTipleri.FinansPoOnayTalebi, talepEden.Id, OnayDurumu.Bekliyor),
                (x.IslemKodu, x.ReferansTipi, x.TalepEdenKullaniciId, x.Durum)));
            var anaTalep = gonder.Value.Talepler.Single(x => x.Ad == "Ana Ambalaj");
            var anaOnay = kuyruk.Single(x => x.ReferansId == anaTalep.TalepId);
            Assert.Equal("PA727-03 · Ana Ambalaj · 3 kalem · 450,00 EUR: PO öncesi iş onayı bekliyor.", anaOnay.IslemAciklamasi);
            Assert.Equal(FinansPoOnayDurumu.OnayBekliyor, (await TekGrup(siparis, FinansIsKategorisi.AnaAmbalaj)).PoOnayDurumu);
            var bekliyor = await Assert.ThrowsAsync<IsKuraliException>(() => siparis.SiparisOlusturAsync(
                new("PO-1", Tarih, null, [], "EUR", Gruplar: [new FinansSiparisGrupDagitimModel(anaIds, 450m)]), Ct));
            Assert.Contains("(Onay bekliyor)", bekliyor.Message);
            db.ChangeTracker.Clear();
            var tekrar = await mediator.Send(new FinansPoOnayGonderCommand { Model = new(anaIds) });
            Assert.Equal(409, tekrar.StatusCode);
            db.ChangeTracker.Clear();
            Assert.Equal(2, await db.Set<FinansPoOnayTalebi>().CountAsync());

            // 3) Onaycı talebin görüntüsünü görür; talep eden (Admin değil) kendi talebini onaylayamaz.
            kullanici.UserId = onaylayan.Id;
            var detay = await mediator.Send(new GetOnayGecmisiDetayiQuery { Id = anaOnay.Id });
            Assert.True(detay.IsSuccess, detay.Error?.Message);
            var finansDetayi = detay.Value!.FinansPoOnayDetayi!;
            Assert.Equal(("PA727-03", FinansIsKategorisi.AnaAmbalaj, 3, 450m, 4.5m, "EUR", 0),
                (finansDetayi.Onizleme.ProjeNo, finansDetayi.Onizleme.Kategori, finansDetayi.Onizleme.KalemSayisi,
                 finansDetayi.Onizleme.NetTutar, finansDetayi.Onizleme.ToplamM3, finansDetayi.Onizleme.ParaBirimi, finansDetayi.DegisenKalemSayisi));
            Assert.Equal(["1", "2", "3"], finansDetayi.Onizleme.Kalemler.Select(x => x.SandikNo));
            Assert.Equal([100m, 150m, 200m], finansDetayi.Onizleme.Kalemler.Select(x => x.NetTutar));
            kullanici.UserId = talepEden.Id;
            Assert.Equal(403, (await mediator.Send(new IslemOnaylaCommand { OnayBekleyenIslemId = anaOnay.Id })).StatusCode);
            // Onay merkezi okuması olup bu kuralın onaycısı, talep edeni veya karar vereni olmayan kişi detayı göremez.
            kullanici.UserId = yetkisiz.Id;
            Assert.Equal(404, (await mediator.Send(new GetOnayGecmisiDetayiQuery { Id = anaOnay.Id })).StatusCode);
            // Detay yalnız onay kaydındaki talep eden ve projeyle eşleşen talepten okunur.
            var onayServisi = provider.GetRequiredService<IFinansPoOnayService>();
            Assert.NotNull(await onayServisi.TalepDetayiAsync(anaTalep.TalepId, talepEden.Id, null, Ct));
            Assert.Null(await onayServisi.TalepDetayiAsync(anaTalep.TalepId, onaylayan.Id, null, Ct));
            Assert.Null(await onayServisi.TalepDetayiAsync(anaTalep.TalepId, talepEden.Id, 395, Ct));

            // 4) Onay: kalemler onaylanır (onaylayan, tutar, denetim kaydı), grup PO'ya bağlanır.
            kullanici.UserId = onaylayan.Id;
            var onay = await mediator.Send(new IslemOnaylaCommand { OnayBekleyenIslemId = anaOnay.Id });
            Assert.True(onay.IsSuccess, onay.Error?.Message);
            db.ChangeTracker.Clear();
            var onaylilar = await db.Set<FinansIsKaydi>().AsNoTracking().Where(x => anaIds.Contains(x.Id)).OrderBy(x => x.Id).ToListAsync();
            Assert.All(onaylilar, x => Assert.Equal(((int?)anaTalep.TalepId, "EUR", (int?)onaylayan.Id), (x.PoOnayTalebiId, x.PoOnayParaBirimi, x.PoOnaylayanKullaniciId)));
            Assert.Equal([100m, 150m, 200m], onaylilar.Select(x => x.PoOnayNetTutar!.Value));
            Assert.Equal(3, await db.Set<FinansDegisiklikGecmisi>().CountAsync(x => x.Islem == "PO Onayı"));
            grup = await TekGrup(siparis, FinansIsKategorisi.AnaAmbalaj);
            Assert.Equal((FinansPoOnayDurumu.Onaylandi, true, false), (grup.PoOnayDurumu, grup.PoyaBaglanabilir, grup.PoOnayaGonderilebilir));
            Assert.Equal(FinansPoOnayDurumu.Onaylandi, (await isKaydi.IsKaydiGetirAsync(ana[0].Id, Ct))!.PoOnayDurumu);
            db.ChangeTracker.Clear();
            var po = await siparis.SiparisOlusturAsync(
                new("PO-2", Tarih, null, [], "EUR", Gruplar: [new FinansSiparisGrupDagitimModel(anaIds, 300m)]), Ct);
            Assert.Equal([100m, 150m, 50m], po.Kalemler.OrderBy(x => x.IsKaydiId).Select(x => x.NetTutar));

            // 5) Ret: neden kalemde görünür, kalem yeniden gönderilebilir.
            var sarfOnay = kuyruk.Single(x => x.Id != anaOnay.Id);
            var ret = await mediator.Send(new IslemReddetCommand { OnayBekleyenIslemId = sarfOnay.Id, RedAciklamasi = "Sarf m³ kontrol edilsin." });
            Assert.True(ret.IsSuccess, ret.Error?.Message);
            db.ChangeTracker.Clear();
            var sarfGrup = await TekGrup(siparis, FinansIsKategorisi.SarfKereste);
            Assert.Equal((FinansPoOnayDurumu.Reddedildi, "Sarf m³ kontrol edilsin.", false, true),
                (sarfGrup.PoOnayDurumu, sarfGrup.PoOnayAciklamasi, sarfGrup.PoyaBaglanabilir, sarfGrup.PoOnayaGonderilebilir));
            Assert.Equal(FinansPoOnayDurumu.Reddedildi, (await isKaydi.IsKaydiGetirAsync(sarf.Id, Ct))!.PoOnayDurumu);

            // 6) Talepten sonra kalem değişirse onaycı bunu görür, onay uygulanmaz, kalem "onay düştü" olur.
            kullanici.UserId = talepEden.Id;
            db.ChangeTracker.Clear();
            var yeniden = await mediator.Send(new FinansPoOnayGonderCommand { Model = new([sarf.Id]) });
            Assert.Equal(StatusConstants.ActionQueuedForApproval, yeniden.StatusCode);
            db.ChangeTracker.Clear();
            await db.Set<FinansIsKaydi>().Where(x => x.Id == sarf.Id).ExecuteUpdateAsync(x => x.SetProperty(y => y.ToplamM3, 0.6m));
            var yeniOnay = await db.OnayBekleyenIslemler.AsNoTracking().SingleAsync(x => x.Durum == OnayDurumu.Bekliyor);
            kullanici.UserId = onaylayan.Id;
            var degisen = (await mediator.Send(new GetOnayGecmisiDetayiQuery { Id = yeniOnay.Id })).Value!.FinansPoOnayDetayi!;
            Assert.Equal(1, degisen.DegisenKalemSayisi);
            Assert.Equal("Net tutar 100,00 → 120,00 EUR.", Assert.Single(degisen.KalemDurumlari).Degisiklik);
            Assert.False((await mediator.Send(new IslemOnaylaCommand { OnayBekleyenIslemId = yeniOnay.Id })).IsSuccess);
            db.ChangeTracker.Clear();
            Assert.Equal(OnayCalistirmaDurumu.Basarisiz,
                (await db.OnayBekleyenIslemler.AsNoTracking().SingleAsync(x => x.Id == yeniOnay.Id)).CalistirmaDurumu);
            Assert.Null((await db.Set<FinansIsKaydi>().AsNoTracking().SingleAsync(x => x.Id == sarf.Id)).PoOnayNetTutar);
            sarfGrup = await TekGrup(siparis, FinansIsKategorisi.SarfKereste);
            Assert.Equal((FinansPoOnayDurumu.OnayDustu, true), (sarfGrup.PoOnayDurumu, sarfGrup.PoOnayAciklamasi!.Contains("değişti")));

            // 7) Onaydan sonra tutar değişirse onay düşer: kalan bakiye ve PO tutar artışı yeniden onay ister; azaltma serbest.
            await db.Set<FinansIsKaydi>().Where(x => x.Id == ana[2].Id).ExecuteUpdateAsync(x => x.SetProperty(y => y.ToplamM3, 2.5m));
            db.ChangeTracker.Clear();
            var dusen = await TekGrup(siparis, FinansIsKategorisi.AnaAmbalaj);
            Assert.Equal((FinansPoOnayDurumu.OnayDustu, false, true), (dusen.PoOnayDurumu, dusen.PoyaBaglanabilir, dusen.PoOnayaGonderilebilir));
            Assert.Equal([ana[2].Id], dusen.IsKaydiIds);
            await Assert.ThrowsAsync<IsKuraliException>(() => siparis.SiparisKalemEkleAsync(po.Id,
                new(Gruplar: [new FinansSiparisGrupDagitimModel([ana[2].Id], 10m)]), Ct));
            db.ChangeTracker.Clear();
            await Assert.ThrowsAsync<IsKuraliException>(() => siparis.SiparisGuncelleAsync(po.Id,
                new("PO-2", Tarih, null, [new(ana[2].Id, 0, 0, NetTutar: 60m)], "Müşteri PO tutarı revize edildi."), Ct));
            db.ChangeTracker.Clear();
            var azaltma = await siparis.SiparisGuncelleAsync(po.Id,
                new("PO-2", Tarih, null, [new(ana[2].Id, 0, 0, NetTutar: 40m)], "Müşteri PO tutarı revize edildi."), Ct);
            Assert.Equal(40m, azaltma!.Kalemler.Single(x => x.IsKaydiId == ana[2].Id).NetTutar);
        });
    }

    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task KuralKapali_OnaysizPoAcilir_GondermeHemenOnaylar_KuralKaydiYoksaOnayAranir()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            await using var db = new AppDbContext(options);
            var talepEden = YeniKullanici("talep", 2);
            db.Add(talepEden);
            var kural = new OnayOperasyonKurali { IslemKodu = OnayIslemKodlari.FinansPoIsOnayi, OnayGerektirirMi = false };
            db.Add(kural);
            var ana = new[] { Kalem("1", FinansIsTuru.AnaAmbalaj, 1m, 100m, Tarih), Kalem("2", FinansIsTuru.AnaAmbalaj, 2m, 100m, Tarih) };
            db.AddRange(ana);
            await db.SaveChangesAsync();
            var kullanici = new TestKullanici { UserId = talepEden.Id };
            await using var provider = Saglayici(db, kullanici);
            var mediator = provider.GetRequiredService<IMediator>();
            var siparis = provider.GetRequiredService<IFinansSiparisFaturaService>();

            var grup = await TekGrup(siparis, FinansIsKategorisi.AnaAmbalaj);
            Assert.Equal((FinansPoOnayDurumu.OnayaSunulmadi, true, false), (grup.PoOnayDurumu, grup.PoyaBaglanabilir, grup.PoOnayaGonderilebilir));
            var po = await siparis.SiparisOlusturAsync(
                new("PO-1", Tarih, null, [], "EUR", Gruplar: [new FinansSiparisGrupDagitimModel([ana[0].Id], 100m)]), Ct);
            Assert.Single(po.Kalemler);

            db.ChangeTracker.Clear();
            var gonder = await mediator.Send(new FinansPoOnayGonderCommand { Model = new([ana[1].Id]) });
            Assert.True(gonder.IsSuccess, gonder.Error?.Message);
            Assert.Equal((200, 0, 1), (gonder.StatusCode, gonder.Value!.OnayaAlinanTalepSayisi, gonder.Value.DogrudanOnaylananTalepSayisi));
            db.ChangeTracker.Clear();
            Assert.Empty(await db.OnayBekleyenIslemler.ToListAsync());
            var onayli = await db.Set<FinansIsKaydi>().AsNoTracking().SingleAsync(x => x.Id == ana[1].Id);
            Assert.Equal(((decimal?)200m, (int?)talepEden.Id), (onayli.PoOnayNetTutar, onayli.PoOnaylayanKullaniciId));

            // Kural kaydı silinirse güvenli varsayılan açıktır: yeni onaysız kalem PO'ya bağlanamaz.
            await db.Set<OnayOperasyonKurali>().Where(x => x.IslemKodu == OnayIslemKodlari.FinansPoIsOnayi).ExecuteDeleteAsync();
            var yeni = Kalem("3", FinansIsTuru.AnaAmbalaj, 1m, 100m, Tarih);
            db.Add(yeni);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            await Assert.ThrowsAsync<IsKuraliException>(() => siparis.SiparisKalemEkleAsync(po.Id,
                new(Kalemler: [new FinansSiparisDagitimModel(yeni.Id, 0, 0, NetTutar: 100m)]), Ct));
            db.ChangeTracker.Clear();
            var eklenen = await siparis.SiparisKalemEkleAsync(po.Id,
                new(Kalemler: [new FinansSiparisDagitimModel(ana[1].Id, 0, 0, NetTutar: 200m)]), Ct);
            Assert.Equal(2, eklenen!.Kalemler.Count);
        });
    }

    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task GecisBetigi_PoyaBagliProjeKayitlariniUygulamaNetiyleOnaylar_TekrarindaDegistirmez()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            await using var db = new AppDbContext(options);
            await db.Database.OpenConnectionAsync();
            db.Add(new OnayOperasyonKurali { IslemKodu = OnayIslemKodlari.FinansPoIsOnayi, OnayGerektirirMi = false });
            var tarifeli = Kalem("1", FinansIsTuru.AnaAmbalaj, 1.25m, 333.33m, Tarih);
            var malzemeli = Kalem("2", FinansIsTuru.AnaAmbalaj, 1.0m, 300m, Tarih);
            malzemeli.AhsapBirimFiyatSnapshot = 400m;
            malzemeli.AhsapM3 = 0.4m;
            malzemeli.KontraM3 = 0.6m;
            var manuel = Kalem("3", FinansIsTuru.SarfKereste, 1.0m, 100m, Tarih);
            manuel.ManuelNetTutar = 99.99m;
            var adetli = Kalem("4", FinansIsTuru.IlaveSandik, 1.0m, 12.345m, Tarih);
            adetli.FiyatlandirmaBirimiSnapshot = FinansFiyatlandirmaBirimi.Adet;
            adetli.Adet = 3;
            var siparissiz = Kalem("5", FinansIsTuru.AnaAmbalaj, 1.0m, 100m, Tarih);
            var iptalPolu = Kalem("6", FinansIsTuru.AnaAmbalaj, 1.0m, 100m, Tarih);
            var ozel = OzelIs();
            db.AddRange(tarifeli, malzemeli, manuel, adetli, siparissiz, iptalPolu, ozel);
            await db.SaveChangesAsync();
            var finans = new FinansTestServisleri(db, new TestKullanici { UserId = 7 });
            foreach (var (kalem, no) in new[] { (tarifeli, "A"), (malzemeli, "B"), (manuel, "C"), (adetli, "D"), (ozel, "E") })
            {
                db.ChangeTracker.Clear();
                await finans.SiparisFatura.SiparisOlusturAsync(
                    new($"PO-{no}", Tarih, null, [new(kalem.Id, 0, 0, NetTutar: 10m)], kalem.ParaBirimiSnapshot), Ct);
            }
            db.ChangeTracker.Clear();
            var iptalPo = await finans.SiparisFatura.SiparisOlusturAsync(
                new("PO-F", Tarih, null, [new(iptalPolu.Id, 0, 0, NetTutar: 10m)], "EUR"), Ct);
            db.ChangeTracker.Clear();
            Assert.True(await finans.SiparisFatura.SiparisIptalAsync(iptalPo.Id, "Müşteri PO'yu iptal etti.", Ct));

            // Önceki sürüm: onay tablosu, kolonları ve kural kaydı yok.
            await db.Database.ExecuteSqlRawAsync("""
                ALTER TABLE "FinansIsKayitlari" DROP COLUMN "PoOnayTalebiId", DROP COLUMN "PoOnayNetTutar",
                    DROP COLUMN "PoOnayParaBirimi", DROP COLUMN "PoOnayTarihi", DROP COLUMN "PoOnaylayanKullaniciId";
                DROP TABLE "FinansPoOnayTalepleri";
                DELETE FROM "OnayOperasyonKurallari";
                """);
            await BetikCalistirAsync(db);
            db.ChangeTracker.Clear();

            var kayitlar = await db.Set<FinansIsKaydi>().AsNoTracking().ToDictionaryAsync(x => x.Id);
            foreach (var kalem in new[] { tarifeli, malzemeli, manuel, adetli })
            {
                var kayit = kayitlar[kalem.Id];
                Assert.True(FinansPoOnayKurali.OnayGecerli(kayit), $"{kayit.IsAdi}: {kayit.PoOnayNetTutar} ≠ {FinansTutarKurallari.IsNet(kayit)}");
                Assert.Equal(((int?)null, (int?)null, true), (kayit.PoOnayTalebiId, kayit.PoOnaylayanKullaniciId, kayit.PoOnayTarihi.HasValue));
            }
            Assert.Equal([416.66m, 340m, 99.99m, 37.04m],
                new[] { tarifeli, malzemeli, manuel, adetli }.Select(x => kayitlar[x.Id].PoOnayNetTutar!.Value));
            Assert.All(new[] { siparissiz, iptalPolu, ozel }, x => Assert.Null(kayitlar[x.Id].PoOnayNetTutar));
            Assert.True(await db.Set<OnayOperasyonKurali>().AnyAsync(x => x.IslemKodu == OnayIslemKodlari.FinansPoIsOnayi && x.OnayGerektirirMi));
            Assert.Equal([SistemRolleri.AdminId], await db.Set<OnayIslemYetki>()
                .Where(x => x.IslemKodu == OnayIslemKodlari.FinansPoIsOnayi).Select(x => x.RolId).ToListAsync());

            // Tekrar: geçiş yeniden uygulanmaz, yöneticinin kural ve rol kararı değişmez.
            await db.Set<FinansIsKaydi>().Where(x => x.Id == tarifeli.Id).ExecuteUpdateAsync(x => x.SetProperty(y => y.PoOnayNetTutar, (decimal?)null));
            await db.Set<OnayOperasyonKurali>().ExecuteUpdateAsync(x => x.SetProperty(y => y.OnayGerektirirMi, false));
            await db.Set<OnayIslemYetki>().Where(x => x.IslemKodu == OnayIslemKodlari.FinansPoIsOnayi).ExecuteDeleteAsync();
            await BetikCalistirAsync(db);
            Assert.Null(await db.Set<FinansIsKaydi>().Where(x => x.Id == tarifeli.Id).Select(x => x.PoOnayNetTutar).SingleAsync());
            Assert.False(await db.Set<OnayOperasyonKurali>().Where(x => x.IslemKodu == OnayIslemKodlari.FinansPoIsOnayi)
                .Select(x => x.OnayGerektirirMi).SingleAsync());
            Assert.False(await db.Set<OnayIslemYetki>().AnyAsync(x => x.IslemKodu == OnayIslemKodlari.FinansPoIsOnayi));
        });
    }

    private static async Task BetikCalistirAsync(AppDbContext db)
        => await db.Database.ExecuteSqlRawAsync(
            await File.ReadAllTextAsync(FinansSekmeAgaci.BetikYolu("20261017_01_Finans_Po_Is_Onayi.sql")));

    private static async Task<FinansSiparisGrubuModel> TekGrup(IFinansSiparisFaturaService siparis, FinansIsKategorisi kategori)
        => Assert.Single((await siparis.SiparisGruplariAsync(new(Kategoriler: [kategori]), Ct)).Items);

    private static Kullanici YeniKullanici(string ad, int rolId) => new()
    {
        AdSoyad = $"Sentetik {ad}",
        Email = $"{ad}-{Guid.NewGuid():N}@example.invalid",
        SifreHash = "synthetic",
        RolId = rolId
    };

    private static FinansIsKaydi Kalem(string sandikNo, FinansIsTuru isTuru, decimal m3, decimal fiyat, DateTime tarih) => new()
    {
        ProjeNo = "PA727-03",
        Musteri = "GE Vernova",
        IsTuru = isTuru,
        IsAdi = $"{(isTuru == FinansIsTuru.SarfKereste ? "Sarf Kereste" : "Normal Ambalaj")} - {sandikNo}",
        SandikNo = sandikNo,
        SandikTipi = "Ahşap Kapalı",
        SandikCinsi = AmbalajSandikCinsi.AhsapKapali,
        Adet = 1,
        Birim = "m³",
        BirimM3 = m3,
        ToplamM3 = m3,
        FiyatlandirmaBirimiSnapshot = FinansFiyatlandirmaBirimi.Metrekup,
        BirimFiyatSnapshot = fiyat,
        ParaBirimiSnapshot = "EUR",
        KdvOraniSnapshot = 20,
        TarifeYiliSnapshot = 2026,
        UretimTarihi = tarih,
        FinansTarihi = tarih,
        FinansDonemi = FinansTarihKurallari.AyBasi(tarih),
        KayitTarihi = tarih,
        KaynakTuru = FinansKaynakTurleri.AmbalajUretim,
        KaynakKayitId = Guid.NewGuid().ToString("D"),
        KaynakBileseni = isTuru == FinansIsTuru.SarfKereste ? "SARF" : "NET",
        KaynakAktif = true
    };

    private static FinansIsKaydi OzelIs() => new()
    {
        IsAdi = "Forklift kiralama",
        ProjeNo = "PA727-03",
        Musteri = "GE Vernova",
        IsTuru = FinansIsTuru.OzelIs,
        KaynakTuru = FinansKaynakTurleri.Manuel,
        KaynakAktif = true,
        Adet = 1,
        Birim = "Hizmet",
        FiyatlandirmaBirimiSnapshot = FinansFiyatlandirmaBirimi.SabitTutar,
        BirimFiyatSnapshot = 50m,
        ParaBirimiSnapshot = "EUR",
        KdvOraniSnapshot = 20,
        UretimTarihi = Tarih,
        FinansTarihi = Tarih,
        FinansDonemi = FinansTarihKurallari.AyBasi(Tarih),
        KayitTarihi = Tarih
    };

    /// <summary>Üretimdeki onay hattı: hata dönüşümü, onay kuyruğu, onay/ret, onay geçmişi detayı ve Finans servisleri.</summary>
    private static ServiceProvider Saglayici(AppDbContext db, TestKullanici kullanici)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();
        services.AddSingleton(db);
        services.AddSingleton<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<ICurrentUserService>(kullanici);
        services.AddSingleton<IApprovalExecutionContext, ApprovalExecutionContext>();
        services.AddSingleton<ITakipKoduSaglayici>(new TakipKoduSaglayici(new HttpContextAccessor()));
        services.AddSingleton<ISseNotifier, SessizBildirim>();
        services.AddSingleton<IOnayIslemRepository, OnayIslemRepository>();
        services.AddSingleton<IOnayYetkiService, OnayYetkiService>();
        services.AddSingleton<IRolService, TumIzinler>();
        services.AddFinansServices();
        services.AddMediatR(config => config.RegisterServicesFromAssemblyContaining<FinansPoOnayGonderCommand>());
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ApprovalBehavior<,>));
        return services.BuildServiceProvider();
    }

    private sealed class TestKullanici : ICurrentUserService
    {
        public int? UserId { get; set; }
        public bool IsAuthenticated => true;
    }

    private sealed class SessizBildirim : ISseNotifier
    {
        public Task BroadcastApprovalUpdateAsync() => Task.CompletedTask;
        public Task SubscribeAsync(object context, int userId) => throw new NotSupportedException();
        public Task NotifyUsersAsync(IEnumerable<int> users, string name, string data = "refresh") => Task.CompletedTask;
    }

    /// <summary>Onay merkezi menü izni bu testin konusu değildir; onay yetkisi OnayIslemYetkileri'nden gelir.</summary>
    private sealed class TumIzinler : IRolService
    {
        public Task<bool> HasUserPermissionAsync(int id, string kod, YetkiTipi tip, CancellationToken ct = default) => Task.FromResult(true);
        public Task<List<MenuTanimi>> GetMenuAgaciAsync(CancellationToken ct = default) => Task.FromResult(new List<MenuTanimi>());
        public Task<List<RolYetki>> GetRolYetkileriAsync(int id, CancellationToken ct = default) => Task.FromResult(new List<RolYetki>());
        public Task YetkileriGuncelleAsync(int id, List<RolYetki> list, CancellationToken ct = default) => Task.CompletedTask;
    }
}
