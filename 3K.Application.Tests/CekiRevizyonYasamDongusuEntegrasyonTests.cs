using System.Reflection;
using System.Security.Cryptography;
using ClosedXML.Excel;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using _3K.Application.Behaviors;
using _3K.Application.Common;
using _3K.Application.Features.CekiIslemleri.Commands;
using _3K.Application.Features.OnayIslemleri.Commands;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Repositories;
using _3K.Infrastructure.Services;

namespace _3K.Application.Tests;

/// <summary>
/// Gerçek Excel parser, PostgreSQL transaction/row lock, MediatR onay akışı ve
/// revizyon dosya deposunu birlikte doğrular. Uygulama bağlantısını kullanmaz.
/// SSE ve bildirim taşıması dışında üretim servisleri çalışır.
/// </summary>
public sealed class CekiRevizyonYasamDongusuEntegrasyonTests
{
    [PostgresRaporFact]
    [Trait("Category", "Postgres")]
    public async Task GercekYukleme_DogrudanVeOnayli_AyniDosyaAdiSnapshotReplayVeTemizleme()
    {
        var builder = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("THREEK_TEST_POSTGRES"));
        Assert.Equal("127.0.0.1", builder.Host);
        Assert.Equal(55439, builder.Port);
        Assert.Equal("postgres", builder.Database);
        var database = $"revision_lifecycle_tests_{Guid.NewGuid():N}";
        var projectId = RandomNumberGenerator.GetInt32(1_000_000_000, 2_000_000_000);
        var uploadsRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "Uploads"));
        var ownProjectDirectory = Path.Combine(uploadsRoot, projectId.ToString());
        Assert.False(Directory.Exists(ownProjectDirectory));
        await using var admin = new NpgsqlConnection(builder.ConnectionString);
        await admin.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE DATABASE \"{database}\"", admin))
            await create.ExecuteNonQueryAsync();

        try
        {
            builder.Database = database;
            var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(builder.ConnectionString).Options;
            await using var context = new AppDbContext(options);
            await context.Database.EnsureCreatedAsync();
            var project = new Proje { Id = projectId, ProjeNo = "SYNTHETIC-REVISION", FBNo = "SYNTHETIC-REVISION", Musteri = "Sentetik test", ProjeTipiId = (int)ProjeTipi.Normal };
            var main = new Ceki { Proje = project, CekiTipiId = (int)CekiTipi.Normal, OrijinalDosyaYolu = "synthetic-main.xlsx" };
            var crate = new Sandik { Proje = project, SandikNo = "1", Ad = "Sentetik sandık" };
            var updateRow = CreateRow(main, crate, 1, 2m);
            var deleteRow = CreateRow(main, crate, 2, 1m);
            var uploader = new Kullanici { AdSoyad = "Sentetik yükleyen", Email = "upload@example.invalid", SifreHash = "synthetic", RolId = 1 };
            var approver = new Kullanici { AdSoyad = "Sentetik onaylayan", Email = "approve@example.invalid", SifreHash = "synthetic", RolId = 1 };
            context.AddRange(main, crate, updateRow, deleteRow, uploader, approver);
            await context.SaveChangesAsync();
            var rule = await context.OnayOperasyonKurallari.SingleOrDefaultAsync(r => r.IslemKodu == OnayIslemKodlari.CekiRevizyonuUygula);
            if (rule == null)
            {
                rule = new OnayOperasyonKurali { IslemKodu = OnayIslemKodlari.CekiRevizyonuUygula };
                context.Add(rule);
            }
            rule.OnayGerektirirMi = false;
            await context.SaveChangesAsync();

            var currentUser = new TestUser { UserId = uploader.Id };
            await using var provider = CreateProvider(context, currentUser);
            var mediator = provider.GetRequiredService<IMediator>();
            var history = new CekiRevizyonGecmisiService(context, uploadsRoot);
            const string sameName = "ayni-revizyon-adi.xlsx";
            var firstBytes = Workbook(project.FBNo, (1, "U", 3m));
            var first = await UploadAsync(mediator, uploader.Id, firstBytes, sameName);
            Assert.True(first.IsSuccess, first.Error?.Message);
            Assert.Equal(CekiRevizyonTalepSonucTipleri.Uygulandi, first.Value!.SonucTipi);
            Assert.Empty(await context.OnayBekleyenIslemler.ToListAsync());
            Assert.Equal(3m, await context.CekiSatirlari.Where(r => r.Id == updateRow.Id).Select(r => r.IstenenAdet).SingleAsync());
            Assert.Equal(3m, await context.SandikIcerikleri.Where(i => i.CekiSatiriId == updateRow.Id).Select(i => i.TahsisMiktari).SingleAsync());
            var firstRequest = await context.CekiRevizyonTalepleri.SingleAsync(t => t.Id == first.Value.TalepId);
            // jsonb anahtar sıralamasını normalleştirir; kalıcı ilk snapshot'ı esas al.
            var firstSnapshot = await context.CekiRevizyonTalepleri.Where(t => t.Id == firstRequest.Id).Select(t => t.OnizlemeJson).SingleAsync();
            var firstHash = firstRequest.OnizlemeHash;
            var firstFile = await context.Cekiler.Where(c => c.Id == first.Value.UygulananRevizyonCekiId).Select(c => c.OrijinalDosyaYolu).SingleAsync();
            Assert.Equal(firstBytes, await File.ReadAllBytesAsync(firstFile));

            // Aynı dosya adı, farklı A/U/D içeriği: onay verilmeden ana veri değişmez.
            rule.OnayGerektirirMi = true;
            await context.SaveChangesAsync();
            var secondBytes = Workbook(project.FBNo, (1, "U", 5m), (2, "D", 1m), (3, "A", 7m));
            var second = await UploadAsync(mediator, uploader.Id, secondBytes, sameName);
            Assert.True(second.IsSuccess, second.Error?.Message);
            Assert.Equal(StatusConstants.ActionQueuedForApproval, second.StatusCode);
            Assert.Equal(CekiRevizyonTalepSonucTipleri.OnayBekliyor, second.Value!.SonucTipi);
            Assert.Null(second.Value.UygulananRevizyonCekiId);
            Assert.Equal(3m, await context.CekiSatirlari.Where(r => r.Id == updateRow.Id).Select(r => r.IstenenAdet).SingleAsync());
            Assert.True(await context.CekiSatirlari.AnyAsync(r => r.Id == deleteRow.Id));
            Assert.False(await context.CekiSatirlari.AnyAsync(r => r.BarkodNo == "SYNTHETIC-3"));
            var pending = Assert.Single((await history.ListeleAsync(projectId, 1, 10, default)).Items, r => r.KayitId == second.Value.TalepId);
            Assert.Equal("OnayBekliyor", pending.Durum);
            var approval = await context.OnayBekleyenIslemler.AsNoTracking().SingleAsync();
            Assert.Equal(second.Value.TalepId, approval.ReferansId);

            // Gerçek onay handler/repository, kayıtlı payload'ı yeniden MediatR'a yollar.
            currentUser.UserId = approver.Id;
            var approvalResult = await mediator.Send(new IslemOnaylaCommand { OnayBekleyenIslemId = approval.Id });
            Assert.True(approvalResult.IsSuccess, approvalResult.Error?.Message);
            var appliedApproval = await context.OnayBekleyenIslemler.AsNoTracking().SingleAsync();
            Assert.Equal(OnayDurumu.Onaylandi, appliedApproval.Durum);
            Assert.Equal(OnayCalistirmaDurumu.Basarili, appliedApproval.CalistirmaDurumu);
            Assert.Equal(approver.Id, appliedApproval.OnaylayanKullaniciId);
            Assert.Equal(5m, await context.CekiSatirlari.Where(r => r.Id == updateRow.Id).Select(r => r.IstenenAdet).SingleAsync());
            Assert.False(await context.CekiSatirlari.AnyAsync(r => r.Id == deleteRow.Id));
            Assert.Equal(7m, await context.CekiSatirlari.Where(r => r.BarkodNo == "SYNTHETIC-3").Select(r => r.IstenenAdet).SingleAsync());

            var secondRequest = await context.CekiRevizyonTalepleri.AsNoTracking().SingleAsync(t => t.Id == second.Value.TalepId);
            Assert.Equal(1, secondRequest.EklenenSatirSayisi);
            Assert.Equal(1, secondRequest.GuncellenenSatirSayisi);
            Assert.Equal(1, secondRequest.SilinenSatirSayisi);
            var secondFile = await context.Cekiler.Where(c => c.Id == secondRequest.UygulananRevizyonCekiId).Select(c => c.OrijinalDosyaYolu).SingleAsync();
            Assert.NotEqual(firstFile, secondFile);
            Assert.Equal(firstBytes, await File.ReadAllBytesAsync(firstFile));
            Assert.Equal(secondBytes, await File.ReadAllBytesAsync(secondFile));
            Assert.Equal(2, Directory.GetFiles(Path.Combine(ownProjectDirectory, "Revizyonlar")).Length);

            var firstDetail = await history.DetayAsync(projectId, "talep", firstRequest.Id, default);
            var firstChange = Assert.Single(firstDetail!.Onizleme!.Satirlar);
            Assert.Equal(2m, firstChange.EskiIstenenAdet);
            Assert.Equal(3m, firstChange.YeniIstenenAdet);
            Assert.Equal(firstSnapshot, await context.CekiRevizyonTalepleri.Where(t => t.Id == firstRequest.Id).Select(t => t.OnizlemeJson).SingleAsync());
            Assert.Equal(firstHash, await context.CekiRevizyonTalepleri.Where(t => t.Id == firstRequest.Id).Select(t => t.OnizlemeHash).SingleAsync());
            var listed = await history.ListeleAsync(projectId, 1, 10, default);
            Assert.Equal(2, listed.TotalCount);
            Assert.All(listed.Items, r => Assert.Equal("Uygulandi", r.Durum));
            Assert.All(listed.Items, r => Assert.Equal(sameName, r.DosyaAdi));

            // Gerçek temizlik servisinin tek geçişi: disk artifact/snapshot kalır.
            var cleanup = new CekiRevizyonTalebiTemizlemeBackgroundService(provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<CekiRevizyonTalebiTemizlemeBackgroundService>.Instance);
            var cleanupMethod = typeof(CekiRevizyonTalebiTemizlemeBackgroundService).GetMethod("TemizleyeneKadarDeneAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
            using var cleanupLimit = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            await (Task)cleanupMethod.Invoke(cleanup, [cleanupLimit.Token])!;
            context.ChangeTracker.Clear();
            Assert.All(await context.CekiRevizyonTalepleri.ToListAsync(), t => Assert.Null(t.DosyaIcerigi));
            Assert.Equal(firstBytes, (await history.DosyaAsync(projectId, "talep", firstRequest.Id, default))!.Icerik);
            Assert.Equal(secondBytes, (await history.DosyaAsync(projectId, "talep", secondRequest.Id, default))!.Icerik);

            // Kaydedilmiş talebi yeniden yürütmek ek revizyon/hareket/dosya üretmez.
            var eventCount = await context.HareketGecmisleri.CountAsync();
            var replay = await provider.GetRequiredService<ICekiService>().OnayliCekiRevizyonunuUygulaAsync(secondRequest.Id, approver.Id);
            Assert.Equal(secondRequest.UygulananRevizyonCekiId, replay.RevizyonCekiId);
            Assert.Equal(2, await context.Cekiler.CountAsync(c => c.CekiTipiId == (int)CekiTipi.Revizyon));
            Assert.Equal(eventCount, await context.HareketGecmisleri.CountAsync());
            Assert.Equal(2, Directory.GetFiles(Path.Combine(ownProjectDirectory, "Revizyonlar")).Length);
            var repeatDecision = await mediator.Send(new IslemOnaylaCommand { OnayBekleyenIslemId = approval.Id });
            Assert.False(repeatDecision.IsSuccess);
            Assert.Equal(409, repeatDecision.StatusCode);
            Assert.Equal(2m, (await history.DetayAsync(projectId, "talep", firstRequest.Id, default))!.Onizleme!.Satirlar[0].EskiIstenenAdet);
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            await using var drop = new NpgsqlCommand($"DROP DATABASE \"{database}\" WITH (FORCE)", admin);
            await drop.ExecuteNonQueryAsync();
            // Yalnızca test başında bulunmadığı doğrulanan sentetik proje dizini.
            Assert.Equal(uploadsRoot, Directory.GetParent(Path.GetFullPath(ownProjectDirectory))!.FullName);
            if (Directory.Exists(ownProjectDirectory)) Directory.Delete(ownProjectDirectory, recursive: true);
        }
    }

    private static ServiceProvider CreateProvider(AppDbContext context, TestUser currentUser)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();
        services.AddSingleton(context);
        services.AddSingleton<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<ICurrentUserService>(currentUser);
        services.AddSingleton<IApprovalExecutionContext, ApprovalExecutionContext>();
        services.AddSingleton<ISseNotifier, NoopNotifier>();
        services.AddSingleton<IHareketService, HareketService>();
        services.AddSingleton<IDurumHesaplaService, DurumHesaplaService>();
        services.AddSingleton<ISahaAktarimSilmeKorumaService, SahaAktarimSilmeKorumaService>();
        services.AddSingleton<ICekiService, CekiService>();
        services.AddSingleton<IOnayIslemRepository, OnayIslemRepository>();
        services.AddSingleton<IOnayYetkiService, OnayYetkiService>();
        services.AddMediatR(config => config.RegisterServicesFromAssemblyContaining<CekiRevizyonYukleCommandHandler>());
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ApprovalBehavior<,>));
        services.AddSingleton<IPublisher, NoopPublisher>();
        return services.BuildServiceProvider();
    }

    private static CekiSatiri CreateRow(Ceki main, Sandik crate, int order, decimal quantity)
    {
        var row = new CekiSatiri { Ceki = main, SiraNo = order, BarkodNo = $"SYNTHETIC-{order}", Aciklama = "Sentetik ürün", CekideGecenSandikNo = "1", IstenenAdet = quantity };
        row.SandikIcerikleri.Add(new SandikIcerik { Sandik = crate, CekiSatiri = row, TahsisMiktari = quantity });
        return row;
    }

    private static byte[] Workbook(string projectNumber, params (int Order, string Code, decimal Quantity)[] changes)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("ÇIKTI SAYFASI");
        sheet.Cell(1, 1).Value = "FB NO";
        sheet.Cell(1, 2).Value = projectNumber;
        sheet.Cell(5, 11).Value = "CHECK";
        for (var i = 0; i < changes.Length; i++)
        {
            var change = changes[i];
            var row = 6 + i;
            sheet.Cell(row, 1).Value = change.Order;
            sheet.Cell(row, 3).Value = $"SYNTHETIC-{change.Order}";
            sheet.Cell(row, 4).Value = "Sentetik ürün";
            sheet.Cell(row, 5).Value = "1";
            sheet.Cell(row, 6).Value = change.Quantity;
            sheet.Cell(row, 7).Value = "Adet";
            sheet.Cell(row, 11).Value = change.Code;
        }
        using var output = new MemoryStream();
        workbook.SaveAs(output);
        return output.ToArray();
    }

    private static async Task<Result<CekiRevizyonOnayTalebiSonuc>> UploadAsync(IMediator mediator, int userId, byte[] bytes, string name)
    {
        using var stream = new MemoryStream(bytes);
        return await mediator.Send(new CekiRevizyonYukleCommand { ExcelDosya = stream, DosyaAdi = name, KullaniciId = userId });
    }

    private sealed class TestUser : ICurrentUserService
    {
        public int? UserId { get; set; }
        public bool IsAuthenticated => true;
        public string? MenuKod => "ceki-revizyon-yukle";
    }

    private sealed class NoopNotifier : ISseNotifier
    {
        public Task BroadcastApprovalUpdateAsync() => Task.CompletedTask;
        public Task SubscribeAsync(object context, int userId) => throw new NotSupportedException();
        public Task NotifyUsersAsync(IEnumerable<int> users, string name, string data = "refresh") => Task.CompletedTask;
    }

    private sealed class NoopPublisher : IPublisher
    {
        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification => Task.CompletedTask;
    }
}
