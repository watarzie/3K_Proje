using ClosedXML.Excel;
using System.IO.Compression;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using _3K.Application.Features.PdfIslemleri.Queries;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Repositories;
using _3K.Infrastructure.Services;

namespace _3K.Application.Tests;

/// <summary>
/// Yalnız özel test sunucusunda sentetik bir veritabanı oluşturur.
/// Uygulama appsettings/connection string dosyalarını hiçbir zaman okumaz.
/// </summary>
public class RaporPostgresEntegrasyonTests
{
    [PostgresRaporFact]
    [Trait("Category", "Postgres")]
    public async Task SentetikRaporlar_GercekPdfExcelDogalSiraAdlarVeMiktarlar()
    {
        var connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("THREEK_TEST_POSTGRES"));
        Assert.True(connection.Host is "127.0.0.1" or "localhost", "Yalnız yerel izole test sunucusu kullanılabilir.");
        Assert.Equal(55439, connection.Port);
        Assert.Equal("postgres", connection.Database);
        var databaseName = $"report_tests_{Guid.NewGuid():N}";
        await using var admin = new NpgsqlConnection(connection.ConnectionString);
        await admin.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", admin))
            await create.ExecuteNonQueryAsync();

        try
        {
            connection.Database = databaseName;
            var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection.ConnectionString).Options;
            await using var context = new AppDbContext(options);
            await context.Database.EnsureCreatedAsync();

            foreach (var type in new[] { ProjeTipi.Normal, ProjeTipi.Saha, ProjeTipi.Yedek })
            {
                var project = BuildProject(type);
                context.Projeler.Add(project);
                await context.SaveChangesAsync();
                context.ChangeTracker.Clear();
                var service = new PdfService(context);
                var pdf = await service.EksikUrunlerRaporuPdfOlusturAsync(project.Id);
                var excel = await service.EksikUrunlerRaporuExcelOlusturAsync(project.Id);
                Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));

                using var workbook = new XLWorkbook(new MemoryStream(excel));
                var sheet = workbook.Worksheet(1);
                Assert.Equal(XLColor.FromHtml("#1565C0"), sheet.Cell(1, 1).Style.Fill.BackgroundColor);
                Assert.Equal(XLColor.FromHtml("#1565C0"), sheet.Cell(6, 1).Style.Fill.BackgroundColor);
                Assert.Equal(XLColor.White, sheet.Cell(6, 1).Style.Font.FontColor);
                var rows = sheet.RowsUsed().Where(r => r.RowNumber() > 6).ToList();
                Assert.Equal(type == ProjeTipi.Saha ? 5 : 4, rows.Count);
                Assert.Equal("TEST-2", rows[0].Cell(3).GetString());
                Assert.StartsWith("SND-2", rows[0].Cell(6).GetString());
                Assert.Equal("TEST-10", rows[1].Cell(3).GetString());
                Assert.StartsWith("SND-10", rows[1].Cell(6).GetString());
                Assert.Equal(6.5m + (type == ProjeTipi.Saha ? 1m : 0m), rows.Sum(r => r.Cell(7).GetValue<decimal>()));
                Assert.Equal(5.5m, rows.Sum(r => r.Cell(15).GetValue<decimal>()));
                if (type is ProjeTipi.Saha or ProjeTipi.Yedek)
                {
                    Assert.Contains("SND-2: Mekanik", rows[0].Cell(6).GetString());
                    Assert.Contains("SND-10: -", rows[1].Cell(6).GetString());
                    Assert.Contains("SND-10: -", rows[0].Cell(6).GetString());
                    var combined = await service.SahaProjeSandiklariPdfOlusturAsync(project.Id);
                    var single = await service.SahaSandikPdfOlusturAsync(project.Sandiklar.First(s => s.SandikNo == "SND-2").Id);
                    await SaveSampleAsync($"{type}-sandiklar.pdf", combined);
                    await SaveSampleAsync($"{type}-tek-sandik.pdf", single);
                }
                await SaveSampleAsync($"{type}-eksik.pdf", pdf);
                await SaveSampleAsync($"{type}-eksik.xlsx", excel);

                // Gerçek servis çıktısı ZIP'e değiştirilmeden girer; ayrıca tekil Excel
                // hücre/miktarları bağımsız ikinci üretimle karşılaştırılır.
                var recording = DispatchProxy.Create<IPdfService, RaporKayitProxy>();
                var proxy = (RaporKayitProxy)recording;
                proxy.Service = service;
                var uow = new UnitOfWork(context, NullLogger<UnitOfWork>.Instance);
                var bulk = new GetTopluEksikUrunlerRaporuQueryHandler(uow, recording, new OrtakUser(), new RaporRolService());
                foreach (var format in new[] { EksikUrunlerRaporDosyaTuru.Pdf, EksikUrunlerRaporDosyaTuru.Excel })
                {
                    var result = await bulk.Handle(new() { ProjeIds = [project.Id], ProjeTipi = type, DosyaTuru = format }, default);
                    Assert.True(result.IsSuccess, result.Error?.Message);
                    using var zip = new ZipArchive(new MemoryStream(result.Value!), ZipArchiveMode.Read);
                    using var entry = Assert.Single(zip.Entries).Open();
                    using var bytes = new MemoryStream();
                    await entry.CopyToAsync(bytes);
                    Assert.Equal(proxy.LastBytes, bytes.ToArray());
                    if (format == EksikUrunlerRaporDosyaTuru.Excel)
                    {
                        using var bulkWorkbook = new XLWorkbook(new MemoryStream(bytes.ToArray()));
                        var bulkSheet = bulkWorkbook.Worksheet(1);
                        for (var rowNumber = 6; rowNumber <= sheet.LastRowUsed()!.RowNumber(); rowNumber++)
                            for (var column = 1; column <= 18; column++)
                                Assert.Equal(sheet.Cell(rowNumber, column).Value, bulkSheet.Cell(rowNumber, column).Value);
                    }
                }
            }

            // Normal/yedek sıfır eksik durumunda tekil rapor boş dosya değil, başlık/özet üretir.
            var empty = new Proje { ProjeNo = "QA-EMPTY", Musteri = "Sentetik Test", ProjeTipiId = (int)ProjeTipi.Yedek };
            context.Projeler.Add(empty);
            await context.SaveChangesAsync();
            var emptyService = new PdfService(context);
            Assert.NotEmpty(await emptyService.EksikUrunlerRaporuPdfOlusturAsync(empty.Id));
            using var emptyWorkbook = new XLWorkbook(new MemoryStream(await emptyService.EksikUrunlerRaporuExcelOlusturAsync(empty.Id)));
            Assert.Equal(6, emptyWorkbook.Worksheet(1).LastRowUsed()!.RowNumber());
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            await using var drop = new NpgsqlCommand($"DROP DATABASE \"{databaseName}\" WITH (FORCE)", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }

    private static Proje BuildProject(ProjeTipi type)
    {
        var project = new Proje { ProjeNo = $"QA-{type}", Musteri = "Sentetik Rapor Doğrulama", ProjeTipiId = (int)type };
        var packing = new Ceki { Proje = project, OrijinalDosyaYolu = "sentetik.xlsx", YuklemeTarihi = new DateTime(2026, 9, 19, 9, 0, 0) };
        project.Cekiler.Add(packing);
        string[] numbers = ["SND-10", "SND-2", "SND-99999999999999999999999999999999", "SND-100000000000000000000000000000000", "SND-20"];
        foreach (var number in numbers)
        {
            var crate = new Sandik
            {
                Proje = project, SandikNo = number,
                Ad = number == "SND-2" ? "Mekanik ekipmanlar ve çok uzun sandık adı: bağlantı elemanları ile yardımcı donanımlar" : " ",
                AdIngilizce = number == "SND-2" ? "Mechanical equipment and accessories with a long crate name" : null,
                Boy = 1200, En = 800, Yukseklik = 700
            };
            var index = Array.IndexOf(numbers, number);
            var requested = index == 1 ? 3.5m : 1m;
            var received = index == 1 || number == "SND-20" ? 1m : 0m;
            var row = new CekiSatiri
            {
                Ceki = packing, SiraNo = index + 1, BarkodNo = "TEST-" + number[4..],
                Aciklama = "Sentetik test ürünü", CekideGecenSandikNo = number,
                IstenenAdet = requested, GelenMiktar = received,
                GridDurumuId = received > 0 ? (int)GridDurum.TamGeldi : (int)GridDurum.Gelmedi
            };
            var content = new SandikIcerik { Sandik = crate, CekiSatiri = row, TahsisMiktari = requested, KonulanAdet = received, EksikAdet = requested - received };
            row.SandikIcerikleri.Add(content);
            crate.SandikIcerikleri.Add(content);
            packing.CekiSatirlari.Add(row);
            project.Sandiklar.Add(crate);
        }
        var splitRow = packing.CekiSatirlari.Single(s => s.BarkodNo == "TEST-2");
        splitRow.SandikIcerikleri.Single().TahsisMiktari = 2m;
        var splitCrate = project.Sandiklar.Single(s => s.SandikNo == "SND-10");
        var split = new SandikIcerik { CekiSatiri = splitRow, Sandik = splitCrate, TahsisMiktari = 1.5m, EksikAdet = 1.5m };
        splitRow.SandikIcerikleri.Add(split);
        splitCrate.SandikIcerikleri.Add(split);
        return project;
    }

    private static async Task SaveSampleAsync(string name, byte[] data)
    {
        var output = Environment.GetEnvironmentVariable("THREEK_REPORT_OUTPUT");
        if (string.IsNullOrWhiteSpace(output)) return;
        Directory.CreateDirectory(output);
        await File.WriteAllBytesAsync(Path.Combine(output, name), data);
    }

    public class RaporKayitProxy : DispatchProxy
    {
        public IPdfService Service { get; set; } = null!;
        public byte[] LastBytes { get; private set; } = [];
        protected override object? Invoke(MethodInfo? method, object?[]? args) => Capture((Task<byte[]>)method!.Invoke(Service, args)!);
        private async Task<byte[]> Capture(Task<byte[]> render)
        {
            LastBytes = await render;
            return LastBytes;
        }
    }
}

internal sealed class PostgresRaporFactAttribute : FactAttribute
{
    public PostgresRaporFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("THREEK_TEST_POSTGRES")))
            Skip = "THREEK_TEST_POSTGRES ile izole yerel PostgreSQL test sunucusu belirtilmeli; uygulama veritabanı kullanılmaz.";
    }
}
