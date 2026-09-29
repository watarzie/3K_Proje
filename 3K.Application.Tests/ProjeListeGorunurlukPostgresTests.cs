using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Repositories;

namespace _3K.Application.Tests;

/// <summary>S-11 ve G1: aktif/sevk edilen liste filtreleri gerçek PostgreSQL verisinde.</summary>
public sealed class ProjeListeGorunurlukPostgresTests
{
    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task S11_SevkEdilenSahaYedekModulListesindenCikar_KayitKorunarakKilidiAcilincaGeriDoner()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            await using var db = new AppDbContext(options);
            var sahaSevkli = Proje("SAHA-SEVK", ProjeTipi.Saha, ProjeDurum.SevkEdildi, Sandik(SandikDurum.Sevkedildi));
            var sahaKilidiAcik = Proje("SAHA-ACIK", ProjeTipi.Saha, ProjeDurum.SevkEdildi, Sandik(SandikDurum.Sevkedildi, duzeltmeAcik: true));
            var sahaKismi = Proje("SAHA-KISMI", ProjeTipi.Saha, ProjeDurum.EksikSevkEdildi, Sandik(SandikDurum.Sevkedildi), Sandik(SandikDurum.Kapandi));
            var sahaAktif = Proje("SAHA-AKTIF", ProjeTipi.Saha, ProjeDurum.Hazirlaniyor, Sandik(SandikDurum.Kapandi));
            var yedekSevkli = Proje("YEDEK-SEVK", ProjeTipi.Yedek, ProjeDurum.SevkEdildi, Sandik(SandikDurum.Sevkedildi));
            var yedekKilidiAcik = Proje("YEDEK-ACIK", ProjeTipi.Yedek, ProjeDurum.SevkEdildi, Sandik(SandikDurum.Sevkedildi, duzeltmeAcik: true));
            // Kapsam dışı: normal projenin görünürlüğü değişmez (S-11 önceki kuralı).
            var normalKilidiAcik = Proje("NORMAL-ACIK", ProjeTipi.Normal, ProjeDurum.SevkEdildi, Sandik(SandikDurum.Sevkedildi, duzeltmeAcik: true));
            db.AddRange(sahaSevkli, sahaKilidiAcik, sahaKismi, sahaAktif, yedekSevkli, yedekKilidiAcik, normalKilidiAcik);
            await db.SaveChangesAsync();

            var repo = new ProjeRepository(db);
            async Task<string[]> Liste(ProjeTipi? tip, bool? sevkEdilen)
                => (await repo.GetFilteredPagedAsync((int?)tip, null, sevkEdilen, 1, 100)).Items.Select(x => x.ProjeNo).Order().ToArray();
            async Task<string[]> AcilirListe(ProjeTipi tip, bool sevkEdilen)
                => (await repo.GetLightFilteredAsync((int)tip, null, sevkEdilen, 100, null)).Select(x => x.ProjeNo).Order().ToArray();

            Assert.Equal(["SAHA-ACIK", "SAHA-AKTIF", "SAHA-KISMI"], await Liste(ProjeTipi.Saha, false));
            Assert.Equal(["SAHA-ACIK", "SAHA-KISMI", "SAHA-SEVK"], await Liste(ProjeTipi.Saha, true));
            Assert.Equal(["YEDEK-ACIK"], await Liste(ProjeTipi.Yedek, false));
            Assert.Equal(["YEDEK-ACIK", "YEDEK-SEVK"], await Liste(ProjeTipi.Yedek, true));
            Assert.Empty(await Liste(ProjeTipi.Normal, false));
            Assert.Equal(["NORMAL-ACIK"], await Liste(ProjeTipi.Normal, true));
            Assert.Equal(await Liste(ProjeTipi.Saha, false), await AcilirListe(ProjeTipi.Saha, false));
            Assert.Equal(await Liste(ProjeTipi.Yedek, true), await AcilirListe(ProjeTipi.Yedek, true));
            Assert.Equal(7, (await Liste(null, null)).Length);
        });
    }

    private static Proje Proje(string no, ProjeTipi tip, ProjeDurum durum, params Sandik[] sandiklar)
    {
        var proje = new Proje { ProjeNo = no, Musteri = "QA", ProjeTipiId = (int)tip, DurumId = (int)durum };
        var sira = 1;
        foreach (var sandik in sandiklar)
        {
            sandik.SandikNo = (sira++).ToString();
            proje.Sandiklar.Add(sandik);
        }
        return proje;
    }

    private static Sandik Sandik(SandikDurum durum, bool duzeltmeAcik = false) => new()
    {
        Ad = "QA sandık", TipId = (int)SandikTipi.AhsapKapali, DurumId = (int)durum, SevkiyatDuzeltmeAcikMi = duzeltmeAcik
    };
}
