using _3K.Application.Features.DashboardIslemleri.Queries;
using _3K.Application.Features.ProjeIslemleri.Queries;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace _3K.Application.Tests;

public sealed class SahaProjeTamamlanmaRegresyonTests
{
    [Theory]
    [InlineData(ProjeTipi.Saha, GridDurum.Iptal)]
    [InlineData(ProjeTipi.Saha, GridDurum.GridKapandi)]
    [InlineData(ProjeTipi.Yedek, GridDurum.Iptal)]
    [InlineData(ProjeTipi.Yedek, GridDurum.GridKapandi)]
    public async Task YetmisBesUrun_BiriIptalVeyaKapali_KalanlarSifirsaIkiListedeDeYuzdeYuz(
        ProjeTipi tip, GridDurum kapaliDurum)
    {
        var proje = ProjeOlustur(tip);
        for (var id = 1; id <= 75; id++)
        {
            IcerikEkle(proje, new CekiSatiri
            {
                Id = id,
                IstenenAdet = 1,
                GelenMiktar = id == 75 ? 0 : 1,
                GridDurumuId = id == 75 ? (int)kapaliDurum : (int)GridDurum.TamGeldi
            }, id == 75 ? 0 : 1);
        }

        var iptal = proje.Cekiler.Single().CekiSatirlari.Last();
        var iptalIcerik = proje.Sandiklar.Single().SandikIcerikleri.Last();
        var repo = new ProjeRepo(proje);
        var saha = new SahaStub();
        var sonuc = await new ProjeListeleQueryHandler(repo, new LookupStub(), saha)
            .Handle(new ProjeListeleQuery(), default);
        var dashboard = await new DashboardProjelerQueryHandler(repo, new LookupStub(), saha)
            .Handle(new DashboardProjelerQuery(), default);

        Assert.True(sonuc.IsSuccess);
        var dto = Assert.Single(sonuc.Value!.Items);
        Assert.Equal(75, dto.ToplamUrunSayisi);
        Assert.Equal(75, dto.TamamlananUrunSayisi);
        var dashboardDto = Assert.Single(dashboard.Value!.Items);
        Assert.Equal(75, dashboardDto.TamamlananUrunSayisi);
        Assert.Equal(100, dashboardDto.TamamlanmaYuzdesi);
        Assert.Equal(0, iptal.GelenMiktar);
        Assert.Equal(0, iptalIcerik.KonulanAdet);
        Assert.Equal(1, iptal.IstenenAdet);
        Assert.Equal((int)ProjeDurum.Hazirlaniyor, dto.DurumId);
        Assert.Equal((int)SandikDurum.Hazirlaniyor, proje.Sandiklar.Single().DurumId);
    }

    [Fact]
    public async Task AnaProje_IsTamamlanmasiniSayarken_FizikselSevkMapiniAyriTutar()
    {
        var proje = ProjeOlustur(ProjeTipi.Normal);
        IcerikEkle(proje, new CekiSatiri { Id = 5, IstenenAdet = 1 }, 0);
        var saha = new SahaStub { IsTamamlama = new Dictionary<int, decimal> { [5] = 1 } };
        var repo = new ProjeRepo(proje);

        var sonuc = await new ProjeListeleQueryHandler(repo, new LookupStub(), saha)
            .Handle(new ProjeListeleQuery(), default);
        var dashboard = await new DashboardProjelerQueryHandler(repo, new LookupStub(), saha)
            .Handle(new DashboardProjelerQuery(), default);

        var dto = Assert.Single(sonuc.Value!.Items);
        Assert.Equal(1, dto.TamamlananUrunSayisi);
        Assert.False(dto.FizikselSevkEdilmisSandikVarMi);
        Assert.Equal((int)ProjeDurum.Hazirlaniyor, dto.DurumId);
        Assert.Equal(100, Assert.Single(dashboard.Value!.Items).TamamlanmaYuzdesi);
        Assert.Equal(2, saha.IsTamamlamaOkumaSayisi);
        Assert.Equal(2, saha.SevkGerceklesenOkumaSayisi);
        Assert.Equal(0, proje.Cekiler.Single().CekiSatirlari.Single().GelenMiktar);
    }

    [Theory]
    [InlineData(0, 0, false)]
    [InlineData(1, 0, false)]
    [InlineData(1, 1, true)]
    [InlineData(1, 2, true)]
    public void ManuelIcerik_MevcutPozitifMiktarVeFizikselTamamlamaKuraliKorunur(
        decimal miktar, decimal konulan, bool beklenen)
    {
        Assert.Equal(beklenen, SahaYedekUrunTamamlanmaHelper.TamamlandiMi(new SandikIcerik
        {
            Miktar = miktar, KonulanAdet = konulan
        }));
    }

    [Fact]
    public async Task CokluTahsis_AnaSatirinKalaniniIzler_AyrilanVeKonulanMiktarlariDegistirmez()
    {
        var proje = ProjeOlustur(ProjeTipi.Saha);
        var satir = new CekiSatiri { Id = 5, IstenenAdet = 10, GelenMiktar = 10 };
        var ilk = IcerikEkle(proje, satir, 4);
        ilk.TahsisMiktari = 4;
        var ikinci = new SandikIcerik
        {
            Id = 6, CekiSatiriId = satir.Id, CekiSatiri = satir,
            TahsisMiktari = 6, KonulanAdet = 6
        };
        proje.Sandiklar.Single().SandikIcerikleri.Add(ikinci);

        var sonuc = await new ProjeListeleQueryHandler(new ProjeRepo(proje), new LookupStub(), new SahaStub())
            .Handle(new ProjeListeleQuery(), default);

        var dto = Assert.Single(sonuc.Value!.Items);
        Assert.Equal(2, dto.ToplamUrunSayisi);
        Assert.Equal(2, dto.TamamlananUrunSayisi);
        Assert.Equal(4, ilk.TahsisMiktari);
        Assert.Equal(6, ikinci.TahsisMiktari);
        Assert.Equal(4, ilk.KonulanAdet);
        Assert.Equal(6, ikinci.KonulanAdet);
    }

    [Fact]
    public void SqlVeBellekKosulu_MerkeziKalanKuraliylaAyniSonucuVerir()
    {
        foreach (var grid in new[] { GridDurum.Iptal, GridDurum.GridKapandi, GridDurum.Gelmedi })
        foreach (var istenen in new[] { 0m, 1m, 5m })
        foreach (var gelen in new[] { 0m, 1m, 8m })
        foreach (var hatali in new[] { 0m, 1m })
        foreach (var durum in new[] { UrunDurum.Bekliyor, UrunDurum.HataliUyumsuzGonderim })
        {
            var satir = new CekiSatiri
            {
                Id = 5, IstenenAdet = istenen, GelenMiktar = gelen,
                HataliMiktar = hatali, DurumId = (int)durum, GridDurumuId = (int)grid,
                StokKarsilanan = 0.25m, ProjeKarsilanan = 0.5m,
                TedarikciKarsilanan = 0.5m, ProjeGonderilen = 0.75m, TrafoSevkAdet = 0.25m
            };
            var icerik = new SandikIcerik { CekiSatiriId = satir.Id, CekiSatiri = satir };
            Assert.Equal(satir.KalanMiktar <= 0, SahaYedekUrunTamamlanmaHelper.TamamlandiMi(icerik));
            Assert.Equal(satir.KalanMiktar <= 0,
                new[] { icerik }.AsQueryable().Any(SahaYedekUrunTamamlanmaHelper.TamamlandiKosulu));
        }
    }

    [Fact]
    public void DashboardGruplamaVeProjeSayaci_PostgresqlSorgusunaCevrilir()
    {
        using var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=database-must-not-be-contacted.invalid;Database=translation_only;Username=test;Password=test")
            .Options);
        var tamamlanan = context.SandikIcerikleri.Where(SahaYedekUrunTamamlanmaHelper.TamamlandiKosulu);
        var topluSql = tamamlanan
            .GroupBy(i => i.Sandik.Proje.ProjeTipiId)
            .Select(g => new { ProjeTipiId = g.Key, Count = g.Count() })
            .ToQueryString();
        var projeSql = context.Projeler.Select(p => new
        {
            p.Id,
            Tamamlanan = p.ProjeTipiId == (int)ProjeTipi.Saha || p.ProjeTipiId == (int)ProjeTipi.Yedek
                ? tamamlanan.Count(i => i.Sandik.ProjeId == p.Id) : 0
        }).ToQueryString();

        Assert.Contains("GROUP BY", topluSql);
        Assert.Contains("GridDurumuId", topluSql);
        Assert.Contains("HataliMiktar", topluSql);
        Assert.Contains("count(*)", projeSql);
        Assert.Contains("GridDurumuId", projeSql);
    }

    private static Proje ProjeOlustur(ProjeTipi tip)
    {
        var proje = new Proje { Id = 1, ProjeNo = "PA549-01", ProjeTipiId = (int)tip };
        proje.Cekiler.Add(new Ceki { Id = 2, ProjeId = 1, Proje = proje });
        proje.Sandiklar.Add(new Sandik { Id = 3, ProjeId = 1, Proje = proje, SandikNo = "1", DurumId = (int)SandikDurum.Hazirlaniyor });
        return proje;
    }

    private static SandikIcerik IcerikEkle(Proje proje, CekiSatiri satir, decimal konulan)
    {
        var ceki = proje.Cekiler.Single();
        satir.Ceki = ceki;
        satir.CekiId = ceki.Id;
        ceki.CekiSatirlari.Add(satir);
        var sandik = proje.Sandiklar.Single();
        var icerik = new SandikIcerik
        {
            Id = satir.Id, CekiSatiriId = satir.Id, CekiSatiri = satir,
            Sandik = sandik, SandikId = sandik.Id, TahsisMiktari = satir.IstenenAdet, KonulanAdet = konulan
        };
        sandik.SandikIcerikleri.Add(icerik);
        satir.SandikIcerikleri.Add(icerik);
        return icerik;
    }

    private sealed class ProjeRepo(Proje proje) : IProjeRepository
    {
        public Task<(IEnumerable<Proje> Items, int TotalCount)> GetFilteredPagedAsync(
            int? tip, string? arama, bool? sevk, int sayfa, int boyut, CancellationToken token = default)
            => Task.FromResult<(IEnumerable<Proje>, int)>(([proje], 1));
        public Task<IEnumerable<Proje>> GetAllWithDetailsAsync(CancellationToken token = default) => throw new NotSupportedException();
        public Task<int> CountAsync(CancellationToken token = default) => throw new NotSupportedException();
        public Task<IEnumerable<Proje>> GetPagedWithDetailsAsync(int page, int size, CancellationToken token = default) => throw new NotSupportedException();
        public Task<IEnumerable<Proje>> GetAllLightAsync(CancellationToken token = default) => throw new NotSupportedException();
        public Task<IEnumerable<Proje>> GetLightFilteredAsync(int? tip, string? arama, bool? sevk,
            int take, IReadOnlyCollection<int>? ids, CancellationToken token = default) => throw new NotSupportedException();
    }

    private sealed class LookupStub : ILookupCacheService
    {
        public string GetDeger<T>(int id) where T : LookupBase => id.ToString();
        public Task WarmupAsync(CancellationToken token = default) => throw new NotSupportedException();
        public Task RefreshAsync<T>(CancellationToken token = default) where T : LookupBase => throw new NotSupportedException();
    }

    private sealed class SahaStub : ISahaTamamlamaService
    {
        public Dictionary<int, decimal> IsTamamlama { get; init; } = [];
        public int IsTamamlamaOkumaSayisi { get; private set; }
        public int SevkGerceklesenOkumaSayisi { get; private set; }
        public Task<Dictionary<int, decimal>> GetAktifIsTamamlamaMapAsync(IEnumerable<int> ids, CancellationToken token = default)
        { IsTamamlamaOkumaSayisi++; return Task.FromResult(IsTamamlama); }
        public Task<Dictionary<int, decimal>> GetSevkEdilenGerceklesenTamamlamaMapAsync(IEnumerable<int> ids, CancellationToken token = default)
        { SevkGerceklesenOkumaSayisi++; return Task.FromResult(new Dictionary<int, decimal>()); }
        public Task<KaynakSandikSahaAktarimDurumu> GetKaynakSandikSahaAktarimDurumuAsync(IEnumerable<int> ids, CancellationToken token = default)
            => Task.FromResult(new KaynakSandikSahaAktarimDurumu());
        public Task<Dictionary<int, decimal>> GetAktifGerceklesenTamamlamaMapAsync(IEnumerable<int> ids, CancellationToken token = default) => throw new NotSupportedException();
        public Task<Dictionary<int, decimal>> GetSevkEdilenTamamlamaMapAsync(IEnumerable<int> ids, CancellationToken token = default) => throw new NotSupportedException();
        public Task<Dictionary<int, decimal>> GetAktifTamamlamaMapAsync(IEnumerable<int> ids, CancellationToken token = default) => throw new NotSupportedException();
        public Task<HashSet<int>> GetAktifSandikBazliAktarimSatirIdsAsync(IEnumerable<int> ids, CancellationToken token = default) => throw new NotSupportedException();
        public Task<bool> AktifTamamlamaVarMiAsync(int id, CancellationToken token = default) => throw new NotSupportedException();
        public Task SenkronizeKaynakProjelerAsync(IEnumerable<int> ids, CancellationToken token = default) => throw new NotSupportedException();
        public Task SenkronizeKaynakProjelerBySahaSandikIdsAsync(IEnumerable<int> ids, CancellationToken token = default) => throw new NotSupportedException();
    }
}
