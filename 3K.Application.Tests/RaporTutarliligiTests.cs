using System.IO.Compression;
using System.Reflection;
using _3K.Application.Features.PdfIslemleri.Queries;
using _3K.Application.Features.AmbalajIslemleri.Queries;
using _3K.Core.Common;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Services;

namespace _3K.Application.Tests;

public class RaporTutarliligiTests
{
    [Fact]
    public void SandikSirasi_RakamParcalariSifirlarBosVeBuyukSayilarKararlidir()
    {
        string?[] values = ["SND-10", "10", "SND-2", "2", "1", "01", "SND-1", "A2B10", "A2B2", "99999999999999999999999999999999", "100000000000000000000000000000000", "", null];
        string?[] expected = ["01", "1", "2", "10", "99999999999999999999999999999999", "100000000000000000000000000000000", "A2B2", "A2B10", "SND-1", "SND-2", "SND-10", null, ""];
        Assert.Equal(expected, values.OrderBy(x => x, SandikNumarasiComparer.Instance));
        Assert.Equal(expected, values.Reverse().OrderBy(x => x, SandikNumarasiComparer.Instance));
    }

    [Fact]
    public void AmbalajRaporlari_AyniDogalSirayiKullanirProjeGruplariVeMiktarlarKorunur()
    {
        var uow = new OrtakMemoryUow();
        uow.Repo<AmbalajUretimKaydi>().Rows.AddRange([
            new() { Id = 3, ProjeId = 2, SandikNo = "1", Adet = 3 },
            new() { Id = 1, ProjeId = 1, SandikNo = "SND-10", Adet = 1 },
            new() { Id = 2, ProjeId = 1, SandikNo = "SND-2", Adet = 2 }
        ]);
        var rows = AmbalajRaporVerisi.KayitlariGetir(uow, new GetAmbalajRaporQuery());
        Assert.Equal([2, 1, 3], rows.Select(r => r.Id));
        Assert.Equal(6, rows.Sum(r => r.Adet));
        var groups = AmbalajUretimFormuGruplayici.Grupla([
            new AmbalajUretimFormuKalemiModel { SandikNo = "SND-10", SandikAdi = "A", Adet = 1 },
            new AmbalajUretimFormuKalemiModel { SandikNo = "SND-2", SandikAdi = "B", Adet = 2 }
        ]);
        Assert.Equal(["SND-2", "SND-10"], groups.Select(g => g.SandikNo));
        Assert.Equal(3, groups.Sum(g => g.Adet));
    }

    [Fact]
    public void SandikAdi_BolunmusSatirHerGercekIliskiyiBirKezGosterirMiktariDegistirmez()
    {
        var project = new Proje { Id = 1, ProjeTipiId = (int)ProjeTipi.Saha };
        var crate2 = new Sandik { Id = 2, ProjeId = 1, SandikNo = "SND-2", Ad = "Mekanik", AdIngilizce = "Mechanical" };
        var crate10 = new Sandik { Id = 10, ProjeId = 1, SandikNo = "SND-10", Ad = " ", AdIngilizce = null };
        var row = new CekiSatiri { IstenenAdet = 3.5m, GelenMiktar = 1m, FiiliSandikNo = "YANLIS-ESKI" };
        row.SandikIcerikleri.Add(new() { Sandik = crate10, TahsisMiktari = 1.5m });
        row.SandikIcerikleri.Add(new() { Sandik = crate2, TahsisMiktari = 2m });
        row.SandikIcerikleri.Add(new() { Sandik = crate2, TahsisMiktari = 0m });
        row.SandikIcerikleri.Add(new() { Sandik = new() { Id = 30, ProjeId = 999, SandikNo = "3", Ad = "Başka proje" } });
        var method = typeof(PdfService).GetMethod("GetEksikRaporSandikMetni", BindingFlags.NonPublic | BindingFlags.Static)!;
        var text = (string)method.Invoke(null, [row, project])!;
        Assert.Equal("SND-2: Mekanik / Mechanical\nSND-10: -", text);
        Assert.Equal(3.5m, row.IstenenAdet);
        Assert.Equal(1m, row.GelenMiktar);
        Assert.Equal(3.5m, row.SandikIcerikleri.Sum(x => x.TahsisMiktari));
    }

    [Theory]
    [InlineData(ProjeTipi.Normal, EksikUrunlerRaporDosyaTuru.Pdf, "eksik-raporu")]
    [InlineData(ProjeTipi.Normal, EksikUrunlerRaporDosyaTuru.Excel, "eksik-raporu")]
    [InlineData(ProjeTipi.Saha, EksikUrunlerRaporDosyaTuru.Pdf, "saha-sevk-sonrasi-eksik-raporu")]
    [InlineData(ProjeTipi.Saha, EksikUrunlerRaporDosyaTuru.Excel, "saha-sevk-sonrasi-eksik-raporu")]
    [InlineData(ProjeTipi.Yedek, EksikUrunlerRaporDosyaTuru.Pdf, "yedek-eksik-raporu")]
    [InlineData(ProjeTipi.Yedek, EksikUrunlerRaporDosyaTuru.Excel, "yedek-eksik-raporu")]
    public async Task TopluEksik_TekilRaporunAyniBytesiniZiplerVeHerProjeyiOnceYetkilendirir(
        ProjeTipi type, EksikUrunlerRaporDosyaTuru format, string menu)
    {
        var fixture = new Fixture(type);
        var result = await fixture.Handle(type, format);
        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal([menu, menu], fixture.Roles.Calls.Select(c => c.Menu));
        Assert.All(fixture.Roles.Calls, call => Assert.Equal(YetkiTipi.R, call.Permission));
        Assert.Equal([1, 2], fixture.Pdf.Calls.Select(c => c.Id));
        Assert.All(fixture.Pdf.Calls, call => Assert.Equal(format == EksikUrunlerRaporDosyaTuru.Pdf
            ? nameof(IPdfService.EksikUrunlerRaporuPdfOlusturAsync) : nameof(IPdfService.EksikUrunlerRaporuExcelOlusturAsync), call.Method));
        using var zip = new ZipArchive(new MemoryStream(result.Value!), ZipArchiveMode.Read);
        Assert.Equal(2, zip.Entries.Count);
        foreach (var entry in zip.Entries)
        {
            Assert.DoesNotContain("..", entry.FullName);
            Assert.DoesNotContain("/", entry.FullName);
            Assert.DoesNotContain("\\", entry.FullName);
            using var stream = entry.Open();
            using var data = new MemoryStream();
            await stream.CopyToAsync(data);
            Assert.Equal(fixture.Pdf.Bytes, data.ToArray());
        }
    }

    [Theory]
    [InlineData(ProjeTipi.Normal)][InlineData(ProjeTipi.Saha)][InlineData(ProjeTipi.Yedek)]
    public async Task TopluEksik_IkinciProjeYetkisizseIlkRaporDaUretilmez(ProjeTipi type)
    {
        var fixture = new Fixture(type);
        fixture.Roles.DenyCall = 2;
        var result = await fixture.Handle(type);
        Assert.False(result.IsSuccess);
        Assert.Equal(403, result.StatusCode);
        Assert.Empty(fixture.Pdf.Calls);
    }

    [Theory]
    [InlineData(ProjeTipi.Saha, ProjeTipi.Normal)]
    [InlineData(ProjeTipi.Normal, ProjeTipi.Yedek)]
    [InlineData(ProjeTipi.Yedek, ProjeTipi.Saha)]
    public async Task TopluEksik_YanlisTipSecimiBasliklaAsilamaz(ProjeTipi actual, ProjeTipi requested)
    {
        var fixture = new Fixture(actual);
        var result = await fixture.Handle(requested);
        Assert.Equal(403, result.StatusCode);
        Assert.Empty(fixture.Pdf.Calls);
    }

    [Theory]
    [InlineData(0)][InlineData(26)]
    public async Task TopluEksik_ProjeSayisiSiniriRenderOncesiUygulanir(int count)
    {
        var fixture = new Fixture(ProjeTipi.Normal);
        var result = await fixture.Handler.Handle(new() { ProjeIds = Enumerable.Range(1, count).ToArray(), DosyaTuru = EksikUrunlerRaporDosyaTuru.Pdf }, default);
        Assert.False(result.IsSuccess);
        Assert.Empty(fixture.Pdf.Calls);
    }

    [Fact]
    public async Task TopluEksik_MukerrerKayipVeGecersizTipRenderEdilmez()
    {
        var fixture = new Fixture(ProjeTipi.Normal);
        Assert.False((await fixture.Handler.Handle(new() { ProjeIds = [1, 1], DosyaTuru = EksikUrunlerRaporDosyaTuru.Pdf }, default)).IsSuccess);
        Assert.Equal(404, (await fixture.Handler.Handle(new() { ProjeIds = [1, 99], DosyaTuru = EksikUrunlerRaporDosyaTuru.Pdf }, default)).StatusCode);
        Assert.False((await fixture.Handler.Handle(new() { ProjeIds = [1], ProjeTipi = (ProjeTipi)99, DosyaTuru = EksikUrunlerRaporDosyaTuru.Pdf }, default)).IsSuccess);
        Assert.Empty(fixture.Pdf.Calls);
    }

    [Fact]
    public async Task TopluEksik_YuzMiBHamsiniriveIptalKorunur()
    {
        var fixture = new Fixture(ProjeTipi.Normal);
        fixture.Pdf.Bytes = new byte[51 * 1024 * 1024];
        var result = await fixture.Handle(ProjeTipi.Normal);
        Assert.False(result.IsSuccess);
        Assert.Contains("100 MiB", result.Error?.Message);
        fixture.Pdf.Calls.Clear();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fixture.Handler.Handle(new() { ProjeIds = [1], DosyaTuru = EksikUrunlerRaporDosyaTuru.Pdf }, new CancellationToken(true)));
        Assert.Empty(fixture.Pdf.Calls);
    }

    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task TekilEksik_GercekMenuOkumaIzniOlmadanRenderEdilmez(bool excel)
    {
        var fixture = new Fixture(ProjeTipi.Saha);
        fixture.Roles.DenyCall = 1;
        var result = excel
            ? await new GetEksikUrunlerExcelQueryHandler(fixture.Uow, fixture.Service, new OrtakUser(), fixture.Roles)
                .Handle(new() { ProjeId = 1, ProjeTipi = ProjeTipi.Saha }, default)
            : await new GetEksikUrunlerPdfQueryHandler(fixture.Uow, fixture.Service, new OrtakUser(), fixture.Roles)
                .Handle(new() { ProjeId = 1, ProjeTipi = ProjeTipi.Saha }, default);
        Assert.Equal(403, result.StatusCode);
        Assert.Empty(fixture.Pdf.Calls);
    }

    private sealed class Fixture
    {
        public OrtakMemoryUow Uow { get; } = new();
        public RaporRolService Roles { get; } = new();
        public IPdfService Service { get; } = DispatchProxy.Create<IPdfService, RaporProxy>();
        public RaporProxy Pdf => (RaporProxy)Service;
        public GetTopluEksikUrunlerRaporuQueryHandler Handler { get; }
        public Fixture(ProjeTipi type)
        {
            Uow.Repo<Proje>().Rows.AddRange([new() { Id = 1, ProjeNo = "../../unsafe\\PA-1", ProjeTipiId = (int)type }, new() { Id = 2, ProjeNo = "PA-2", ProjeTipiId = (int)type }]);
            Handler = new(Uow, Service, new OrtakUser(), Roles);
        }
        public Task<_3K.Application.Common.Result<byte[]>> Handle(ProjeTipi type, EksikUrunlerRaporDosyaTuru format = EksikUrunlerRaporDosyaTuru.Pdf) =>
            Handler.Handle(new() { ProjeIds = [1, 2], ProjeTipi = type, DosyaTuru = format }, default);
    }

    public class RaporProxy : DispatchProxy
    {
        public List<(int Id, string Method)> Calls { get; } = [];
        public byte[] Bytes { get; set; } = [1, 2, 3];
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            ((CancellationToken)args![1]!).ThrowIfCancellationRequested();
            Calls.Add(((int)args[0]!, method!.Name));
            return Task.FromResult(Bytes);
        }
    }
}

internal sealed class RaporRolService : IRolService
{
    public List<(string Menu, YetkiTipi Permission)> Calls { get; } = [];
    public int DenyCall { get; set; }
    public Task<bool> HasUserPermissionAsync(int userId, string menuKod, YetkiTipi requiredYetkiTipi, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        Calls.Add((menuKod, requiredYetkiTipi));
        return Task.FromResult(Calls.Count != DenyCall);
    }
    public Task<bool> IsAdminAsync(int userId, CancellationToken ct = default) => Task.FromResult(false);
    public Task<List<MenuTanimi>> GetMenuAgaciAsync(CancellationToken ct = default) => Task.FromResult(new List<MenuTanimi>());
    public Task<List<RolYetki>> GetRolYetkileriAsync(int rolId, CancellationToken ct = default) => Task.FromResult(new List<RolYetki>());
    public Task YetkileriGuncelleAsync(int rolId, List<RolYetki> yetkiler, CancellationToken ct = default) => Task.CompletedTask;
}
