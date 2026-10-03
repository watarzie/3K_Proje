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
using _3K.Infrastructure.Services.Raporlar;

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
            new() { Id = 3, ProjeId = 2, SandikNo = "1", Adet = 3, Tur = AmbalajSandikTuru.Ilave },
            new() { Id = 1, ProjeId = 1, SandikNo = "SND-10", Adet = 1, Tur = AmbalajSandikTuru.Ilave },
            new() { Id = 2, ProjeId = 1, SandikNo = "SND-2", Adet = 2, Tur = AmbalajSandikTuru.Ilave },
            new() { Id = 4, ProjeId = 1, SandikNo = "SND-1", Adet = 9, Tur = AmbalajSandikTuru.Saha },
            new() { Id = 5, ProjeId = 1, SandikNo = "SND-3", Adet = 9, Tur = AmbalajSandikTuru.Ilave, IptalMi = true }
        ]);
        var rows = AmbalajRaporVerisi.KayitlariGetir(uow, AmbalajSandikTuru.Ilave, izinliDurumlar: null);
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
        var text = EksikUrunlerRaporService.GetEksikRaporSandikMetni(row, project);
        Assert.Equal("SND-2: Mekanik / Mechanical\nSND-10: -", text);
        Assert.Equal(3.5m, row.IstenenAdet);
        Assert.Equal(1m, row.GelenMiktar);
        Assert.Equal(3.5m, row.SandikIcerikleri.Sum(x => x.TahsisMiktari));
    }

    [Theory]
    [InlineData(ProjeTipi.Normal, EksikUrunlerRaporDosyaTuru.Pdf, "_EksikRaporu.pdf")]
    [InlineData(ProjeTipi.Normal, EksikUrunlerRaporDosyaTuru.Excel, "_EksikRaporu.xlsx")]
    [InlineData(ProjeTipi.Saha, EksikUrunlerRaporDosyaTuru.Pdf, "_SevkSonrasiEksikRaporu.pdf")]
    [InlineData(ProjeTipi.Saha, EksikUrunlerRaporDosyaTuru.Excel, "_SevkSonrasiEksikRaporu.xlsx")]
    [InlineData(ProjeTipi.Yedek, EksikUrunlerRaporDosyaTuru.Pdf, "_EksikRaporu.pdf")]
    [InlineData(ProjeTipi.Yedek, EksikUrunlerRaporDosyaTuru.Excel, "_EksikRaporu.xlsx")]
    public async Task TopluEksik_TekilRaporunAyniBytesiniZipler(
        ProjeTipi type, EksikUrunlerRaporDosyaTuru format, string raporSoneki)
    {
        // Proje başına gerçek tip yetkisi çözümleyicide doğrulanır: ServerAuthorizationRegressionTests.
        var fixture = new Fixture(type);
        var result = await fixture.Handle(type, format);
        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal("application/zip", result.Value!.IcerikTuru);
        Assert.StartsWith("TopluEksikRaporlari_", result.Value.DosyaAdi);
        Assert.Equal([1, 2], fixture.Pdf.Calls.Select(c => c.Id));
        Assert.All(fixture.Pdf.Calls, call => Assert.Equal(format == EksikUrunlerRaporDosyaTuru.Pdf
            ? nameof(IEksikUrunlerRaporService.EksikUrunlerRaporuPdfOlusturAsync) : nameof(IEksikUrunlerRaporService.EksikUrunlerRaporuExcelOlusturAsync), call.Method));
        using var zip = new ZipArchive(new MemoryStream(result.Value.Icerik), ZipArchiveMode.Read);
        Assert.Equal(2, zip.Entries.Count);
        foreach (var entry in zip.Entries)
        {
            Assert.EndsWith(raporSoneki, entry.FullName);
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

    private sealed class Fixture
    {
        public OrtakMemoryUow Uow { get; } = new();
        public IEksikUrunlerRaporService Service { get; } = DispatchProxy.Create<IEksikUrunlerRaporService, RaporProxy>();
        public RaporProxy Pdf => (RaporProxy)Service;
        public GetTopluEksikUrunlerRaporuQueryHandler Handler { get; }
        public Fixture(ProjeTipi type)
        {
            Uow.Repo<Proje>().Rows.AddRange([new() { Id = 1, ProjeNo = "../../unsafe\\PA-1", ProjeTipiId = (int)type }, new() { Id = 2, ProjeNo = "PA-2", ProjeTipiId = (int)type }]);
            Handler = new(Uow, Service);
        }
        public Task<_3K.Application.Common.Result<DosyaIcerigi>> Handle(ProjeTipi type, EksikUrunlerRaporDosyaTuru format = EksikUrunlerRaporDosyaTuru.Pdf) =>
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
