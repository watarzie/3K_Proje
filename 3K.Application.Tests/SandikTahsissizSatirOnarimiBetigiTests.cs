using Microsoft.EntityFrameworkCore;
using Npgsql;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Infrastructure.Data;

namespace _3K.Application.Tests;

/// <summary>
/// <c>20261014_03_Sandik_Tahsissiz_Satir_Onarimi.sql</c>: sandık tahsisi olmayan çeki satırları, sandık numaralarının
/// projede tek ve sevk edilmemiş bir sandıkla eşleştiği durumda o sandığa bağlanır. Değerler 3K senkronunun ürettiği
/// değerlerdir; tahsisi olan, eşleşmeyen, belirsiz veya sevk edilmiş sandığı gösteren satırlara dokunulmaz.
/// Betik tekrar çalıştırılınca hiçbir şey değişmez; durum sorgusu öncesini/sonrasını doğru gösterir.
/// </summary>
public sealed class SandikTahsissizSatirOnarimiBetigiTests
{
    private const string Betik = "20261014_03_Sandik_Tahsissiz_Satir_Onarimi.sql";

    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Postgres_TahsissizSatirlar_TekSevkEdilmemisSandigaBaglanir_TekrarindaDegismez()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            await using var db = new AppDbContext(options);
            await db.Database.OpenConnectionAsync();

            var proje = new Proje { ProjeNo = "QA-ONARIM", Musteri = "Sentetik", ProjeTipiId = (int)ProjeTipi.Normal };
            var ceki = new Ceki { Proje = proje, OrijinalDosyaYolu = "sentetik.xlsx", YuklemeTarihi = new DateTime(2026, 10, 14) };
            Sandik SandikEkle(string no, int durum = (int)SandikDurum.Kapandi)
            {
                var sandik = new Sandik { Proje = proje, SandikNo = no, DurumId = durum, TipId = (int)SandikTipi.AhsapKapali };
                db.Sandiklar.Add(sandik);
                return sandik;
            }
            CekiSatiri SatirEkle(int sira, string sandikNo, decimal istenen, decimal gelen = 0, decimal stok = 0,
                decimal projeGonderilen = 0, string? fiili = null)
            {
                var satir = new CekiSatiri
                {
                    Ceki = ceki, SiraNo = sira, BarkodNo = $"QA-{sira}", Aciklama = "Sentetik ürün",
                    CekideGecenSandikNo = sandikNo, FiiliSandikNo = fiili, IstenenAdet = istenen, GelenMiktar = gelen,
                    StokKarsilanan = stok, ProjeGonderilen = projeGonderilen, BirimId = (int)Birim.Adet,
                    GridDurumuId = (int)GridDurum.TamGeldi
                };
                db.CekiSatirlari.Add(satir);
                return satir;
            }

            var s36 = SandikEkle("36");
            var sA1 = SandikEkle("a-1");
            SandikEkle("D1");
            SandikEkle("d1");
            SandikEkle("SEVK", (int)SandikDurum.Sevkedildi);

            // Tam teslim (3 gelen + 1 stok): numarası boşluklu yazılmış.
            var tam = SatirEkle(73, " 36 ", istenen: 4, gelen: 3, stok: 1);
            // Kısmi teslim, başka projeye 1 verilmiş; fiili numara çekidekini ezer, büyük/küçük harf duyarsız.
            var kismi = SatirEkle(74, "ESKI", istenen: 5, gelen: 2, projeGonderilen: 1, fiili: "A-1");
            // Tahsisi zaten olan satır.
            var tahsisli = SatirEkle(75, "36", istenen: 2, gelen: 2);
            var eskiTahsis = new SandikIcerik { Sandik = s36, CekiSatiri = tahsisli, TahsisMiktari = 2, KonulanAdet = 2 };
            db.SandikIcerikleri.Add(eskiTahsis);
            var sandiksiz = SatirEkle(76, "99", istenen: 1, gelen: 1);
            var belirsiz = SatirEkle(77, "D1", istenen: 1, gelen: 1);
            var sevkEdilmis = SatirEkle(78, "SEVK", istenen: 1, gelen: 1);
            // Çekide sandık numarası boş gelen ve hiç numara verilmemiş satır.
            var numarasiz = SatirEkle(79, "", istenen: 1, gelen: 1);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            Assert.Equal("UYGULANMAMIŞ", await BetikDurumuAsync(db));

            await CalistirAsync(db);

            var tahsisler = await db.SandikIcerikleri.AsNoTracking().ToListAsync();
            var tamTahsis = Assert.Single(tahsisler, x => x.CekiSatiriId == tam.Id);
            Assert.Equal(s36.Id, tamTahsis.SandikId);
            Assert.Equal(4, tamTahsis.TahsisMiktari);
            Assert.Equal(4, tamTahsis.KonulanAdet);
            Assert.Equal(0, tamTahsis.EksikAdet);
            Assert.Equal(1, tamTahsis.StokKarsilanan);
            Assert.Equal(0, tamTahsis.ProjeKarsilanan);
            Assert.Equal((int)Birim.Adet, tamTahsis.BirimId);
            Assert.Equal(Betik, tamTahsis.CreatedBy);

            var kismiTahsis = Assert.Single(tahsisler, x => x.CekiSatiriId == kismi.Id);
            Assert.Equal(sA1.Id, kismiTahsis.SandikId);
            Assert.Equal(5, kismiTahsis.TahsisMiktari);
            Assert.Equal(1, kismiTahsis.KonulanAdet);
            Assert.Equal(4, kismiTahsis.EksikAdet);

            var mevcut = Assert.Single(tahsisler, x => x.CekiSatiriId == tahsisli.Id);
            Assert.Equal(eskiTahsis.Id, mevcut.Id);
            Assert.DoesNotContain(tahsisler, x => x.CekiSatiriId == sandiksiz.Id || x.CekiSatiriId == belirsiz.Id
                || x.CekiSatiriId == sevkEdilmis.Id || x.CekiSatiriId == numarasiz.Id);
            Assert.Equal(3, tahsisler.Count);

            // Kalan satırlar onarılamaz (sandık yok / belirsiz / sevk edilmiş); durum sorgusu onarım bitti der.
            Assert.Equal("uygulanmış", await BetikDurumuAsync(db));

            var once = Ozet(tahsisler);
            await CalistirAsync(db);
            Assert.Equal(once, Ozet(await db.SandikIcerikleri.AsNoTracking().ToListAsync()));
        });
    }

    private static string Ozet(IEnumerable<SandikIcerik> tahsisler) => string.Join(Environment.NewLine, tahsisler
        .OrderBy(x => x.Id)
        .Select(x => $"{x.Id} {x.SandikId} {x.CekiSatiriId} {x.TahsisMiktari} {x.KonulanAdet} {x.EksikAdet} {x.CreatedDate:O}"));

    private static async Task CalistirAsync(AppDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync(await File.ReadAllTextAsync(FinansSekmeAgaci.BetikYolu(Betik)));
        db.ChangeTracker.Clear();
    }

    private static async Task<string> BetikDurumuAsync(AppDbContext db)
    {
        await using var komut = (NpgsqlCommand)db.Database.GetDbConnection().CreateCommand();
        komut.CommandText = await File.ReadAllTextAsync(FinansSekmeAgaci.BetikYolu(Path.Combine("kontrol", "Betik_Durumu.sql")));
        await using var okuyucu = await komut.ExecuteReaderAsync();
        while (await okuyucu.ReadAsync())
        {
            if (okuyucu.GetString(1) == Path.GetFileNameWithoutExtension(Betik))
                return okuyucu.GetString(2);
        }
        throw new InvalidOperationException("Durum sorgusunda betik satırı yok.");
    }
}
