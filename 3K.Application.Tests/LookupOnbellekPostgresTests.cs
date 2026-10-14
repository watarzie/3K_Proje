using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using _3K.Application.Features.SandikIslemleri.Queries;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Repositories;
using _3K.Infrastructure.Services;

namespace _3K.Application.Tests;

/// <summary>
/// Lookup önbelleği: önbellekte olmayan lookup metni "?" döner. Birim ve proje tipi açılışta yüklenmediği için
/// Sandık İçerik Yönetimi'nde miktarların altında "?" görünüyordu. Uygulamanın okuduğu her lookup çözülmelidir.
/// </summary>
public sealed class LookupOnbellekPostgresTests
{
    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Postgres_Acilista_UygulamaninOkuduguButunLookuplarCozulur()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            await using var db = new AppDbContext(options);
            var onbellek = new LookupCacheService(new MemoryCache(new MemoryCacheOptions()), db);
            await onbellek.WarmupAsync();

            Assert.Equal("Adet", onbellek.GetDeger<LookupBirim>((int)Birim.Adet));
            Assert.Equal("Metreküp", onbellek.GetDeger<LookupBirim>((int)Birim.Metrekup));
            Assert.Equal("Normal", onbellek.GetDeger<LookupProjeTipi>((int)ProjeTipi.Normal));
            // Uygulama kodunda GetDeger ile okunan bütün lookup türleri.
            Assert.NotEqual("?", onbellek.GetDeger<LookupDepoLokasyon>((int)DepoLokasyon.Belirsiz));
            Assert.NotEqual("?", onbellek.GetDeger<LookupGeriGonderilmeSebebi>(1));
            Assert.NotEqual("?", onbellek.GetDeger<LookupGridDurum>((int)GridDurum.Bekliyor));
            Assert.NotEqual("?", onbellek.GetDeger<LookupGridSevkDurum>((int)GridSevkDurum.Bekliyor));
            Assert.NotEqual("?", onbellek.GetDeger<LookupKaliteDurum>(1));
            Assert.NotEqual("?", onbellek.GetDeger<LookupProjeDurum>((int)ProjeDurum.Hazirlaniyor));
            Assert.NotEqual("?", onbellek.GetDeger<LookupSandikDurum>((int)SandikDurum.Hazirlaniyor));
            Assert.NotEqual("?", onbellek.GetDeger<LookupSandikTipi>((int)SandikTipi.AhsapKapali));
            Assert.NotEqual("?", onbellek.GetDeger<LookupSurecDurum>(1));
            Assert.NotEqual("?", onbellek.GetDeger<LookupUcKDurum>((int)UcKDurum.Bekliyor));
            Assert.NotEqual("?", onbellek.GetDeger<LookupUrunDurum>((int)UrunDurum.Bekliyor));
        });
    }

    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Postgres_SandikIcerigi_BirimiTahsisteYoksaCekiSatirindanGosterir()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            await using var db = new AppDbContext(options);
            var proje = new Proje { ProjeNo = "QA-BIRIM", Musteri = "Sentetik", ProjeTipiId = (int)ProjeTipi.Normal };
            var ceki = new Ceki { Proje = proje, OrijinalDosyaYolu = "sentetik.xlsx", YuklemeTarihi = new DateTime(2026, 10, 14) };
            var sandik = new Sandik { Proje = proje, SandikNo = "4", DurumId = (int)SandikDurum.Hazirlaniyor, TipId = (int)SandikTipi.AhsapKapali };
            CekiSatiri Satir(int sira, Birim birim) => new()
            {
                Ceki = ceki, SiraNo = sira, BarkodNo = $"QA-{sira}", Aciklama = "Sentetik ürün", CekideGecenSandikNo = "4",
                IstenenAdet = 4, BirimId = (int)birim, GridDurumuId = (int)GridDurum.Gelmedi
            };
            // Çeki yüklemesi tahsiste birim tutmaz; düzenlenmiş tahsis kendi birimini taşır.
            db.SandikIcerikleri.AddRange(
                new SandikIcerik { Sandik = sandik, CekiSatiri = Satir(1, Birim.Set), TahsisMiktari = 4 },
                new SandikIcerik { Sandik = sandik, CekiSatiri = Satir(2, Birim.Adet), TahsisMiktari = 4, BirimId = (int)Birim.Metre });
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            var onbellek = new LookupCacheService(new MemoryCache(new MemoryCacheOptions()), db);
            await onbellek.WarmupAsync();
            using var uow = new UnitOfWork(db, NullLogger<UnitOfWork>.Instance);
            var sonuc = await new GetSandikIcerikQueryHandler(
                    new SandikService(uow, db), onbellek, new SahaTamamlamaService(db), new SahaAktarimSilmeKorumaService(db), uow)
                .Handle(new GetSandikIcerikQuery { SandikId = sandik.Id }, default);

            Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
            Assert.Equal(["Set", "Metre"], sonuc.Value!.Icerikler.OrderBy(i => i.Id).Select(i => i.BirimMetni));
        });
    }
}
