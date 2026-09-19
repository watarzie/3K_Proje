using System.Reflection;
using _3K.Application.Features.PdfIslemleri.Queries;
using _3K.Application.Features.SandikIslemleri.Commands;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Tests;

public class OrtakSandikRaporIsKurallariTests
{
    [Theory]
    [InlineData("oturum")][InlineData("yok")][InlineData("sevk")][InlineData("kapali")]
    public async Task TekilKapat_GecersizVeyaZatenSonuclanmisSandigaDokunmaz(string state)
    {
        var data = new KapatFixture();
        if (state == "yok") data.Uow.Repo<Sandik>().Rows.Clear();
        if (state == "sevk") { data.Sandik.DurumId = (int)SandikDurum.Sevkedildi; data.Sandik.SevkiyatDuzeltmeAcikMi = true; }
        if (state == "kapali") data.Sandik.DurumId = (int)SandikDurum.Kapandi;
        var before = data.Sandik.DurumId;
        var result = await data.Single(true, state != "oturum");
        Assert.False(result.IsSuccess);
        Assert.Equal(before, data.Sandik.DurumId);
        Assert.Equal(0, data.Uow.SaveCount);
        Assert.Empty(data.Hareket.Rows);
    }

    [Theory]
    [InlineData(false, false)][InlineData(false, true)][InlineData(true, false)][InlineData(true, true)]
    public async Task EksikVeyaHataliKapatma_ForceYalnizUyariyiKabulEderTeslimUretmez(bool defect, bool force)
    {
        var data = new KapatFixture();
        data.Satir.GelenMiktar = defect ? 3m : 2.1234m;
        data.Satir.HataliMiktar = defect ? .0001m : 0m;
        var kalan = data.Satir.KalanMiktar;
        var result = await data.Single(force);
        Assert.Equal(force, result.IsSuccess);
        Assert.Equal(!force, result.HasMissingOrDefectiveItems);
        Assert.Equal(force ? (int)SandikDurum.Kapandi : (int)SandikDurum.Hazirlaniyor, data.Sandik.DurumId);
        Assert.Equal(kalan, data.Satir.KalanMiktar);
        Assert.Equal(defect ? 3m : 2.1234m, data.Satir.GelenMiktar);
        Assert.Equal(0m, data.Icerik.KonulanAdet);
        Assert.Equal(force ? 1 : 0, data.Uow.SaveCount);
        Assert.Equal(force ? 1 : 0, data.Hareket.Rows.Count);
    }

    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task AktifSahaKaynagi_TekilVeTopluForceKapatmaylaAsilamaz(bool force)
    {
        var data = new KapatFixture();
        data.Saha.AktifKaynaklar.Add(data.Satir.Id);
        Assert.False((await data.Single(force)).IsSuccess);
        Assert.False((await data.Bulk(force)).IsSuccess);
        Assert.Equal((int)SandikDurum.Hazirlaniyor, data.Sandik.DurumId);
        Assert.Equal(0, data.Uow.SaveCount);
        Assert.Empty(data.Hareket.Rows);
    }

    [Theory]
    [InlineData("sevk", false)][InlineData("sevk", true)]
    [InlineData("saha", false)][InlineData("saha", true)][InlineData("eksik", false)]
    public async Task TopluKapat_BirSandikEngelliyseUygunSandigiDaDegistirmez(string blocked, bool force)
    {
        var data = new KapatFixture();
        data.Uow.Repo<Sandik>().Rows.Add(new() { Id = 2, ProjeId = 1, SandikNo = "2", DurumId = (int)SandikDurum.Hazirlaniyor });
        if (blocked == "sevk") data.Sandik.DurumId = (int)SandikDurum.Sevkedildi;
        if (blocked == "saha") data.Saha.AktifKaynaklar.Add(data.Satir.Id);
        if (blocked == "eksik") data.Satir.GelenMiktar = 2m;
        Assert.False((await data.Bulk(force, [2, 1])).IsSuccess);
        Assert.Equal((int)SandikDurum.Hazirlaniyor, data.Uow.Repo<Sandik>().Rows[1].DurumId);
        Assert.Equal(0, data.Uow.SaveCount);
        Assert.Empty(data.Hareket.Rows);
    }

    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task TopluKapat_KapaliyiAtlarTamamlananiKapatirVeTekHareketYazar(bool force)
    {
        var data = new KapatFixture();
        data.Uow.Repo<Sandik>().Rows.Add(new() { Id = 2, ProjeId = 1, SandikNo = "2", DurumId = (int)SandikDurum.Kapandi });
        Assert.True((await data.Bulk(force, [1, 2])).IsSuccess);
        Assert.All(data.Uow.Repo<Sandik>().Rows, x => Assert.Equal((int)SandikDurum.Kapandi, x.DurumId));
        Assert.Equal("1", Assert.Single(data.Hareket.Rows).ReferansId);
        Assert.Equal(3m, data.Satir.GelenMiktar);
        Assert.Equal(0m, data.Icerik.KonulanAdet);
    }

    [Theory]
    [InlineData(ProjeTipi.Normal, "eksik-raporu")]
    [InlineData(ProjeTipi.Saha, "saha-sevk-sonrasi-eksik-raporu")]
    [InlineData(ProjeTipi.Yedek, "yedek-eksik-raporu")]
    public async Task EksikRaporu_GercekProjeTipiVeSabitMenuEslestirilir(ProjeTipi type, string menu)
    {
        var uow = new OrtakMemoryUow();
        uow.Repo<Proje>().Rows.Add(new() { Id = 10, ProjeTipiId = (int)type });
        var pdf = DispatchProxy.Create<IPdfService, RaporProxy>();
        var proxy = (RaporProxy)pdf;
        var command = new GetEksikUrunlerPdfQuery { ProjeId = 10, ProjeTipi = type };
        Assert.Equal(menu, command.RequiredMenuKod);
        var result = await new GetEksikUrunlerPdfQueryHandler(uow, pdf, new OrtakUser(), new RaporRolService()).Handle(command, default);
        Assert.True(result.IsSuccess);
        Assert.Equal(proxy.Bytes, result.Value);
        Assert.Equal(10, Assert.Single(proxy.Projeler));
    }

    [Theory]
    [InlineData(ProjeTipi.Normal, ProjeTipi.Saha)]
    [InlineData(ProjeTipi.Saha, ProjeTipi.Normal)]
    [InlineData(ProjeTipi.Yedek, ProjeTipi.Normal)]
    [InlineData(ProjeTipi.Normal, (ProjeTipi)999)]
    public async Task EksikRaporu_YanlisTipBaskaMenuYetkisineGecitVermez(ProjeTipi actual, ProjeTipi requested)
    {
        var uow = new OrtakMemoryUow();
        uow.Repo<Proje>().Rows.Add(new() { Id = 10, ProjeTipiId = (int)actual });
        var pdf = DispatchProxy.Create<IPdfService, RaporProxy>();
        var result = await new GetEksikUrunlerPdfQueryHandler(uow, pdf, new OrtakUser(), new RaporRolService()).Handle(new() { ProjeId = 10, ProjeTipi = requested }, default);
        Assert.False(result.IsSuccess);
        Assert.Equal(403, result.StatusCode);
        Assert.Empty(((RaporProxy)pdf).Projeler);
    }

    [Fact]
    public async Task EksikRaporu_ProjeYoksaVeyaIstekIptalseServisiCalistirmaz()
    {
        var pdf = DispatchProxy.Create<IPdfService, RaporProxy>();
        var handler = new GetEksikUrunlerPdfQueryHandler(new OrtakMemoryUow(), pdf, new OrtakUser(), new RaporRolService());
        var query = new GetEksikUrunlerPdfQuery { ProjeId = 10, ProjeTipi = ProjeTipi.Normal };
        Assert.Equal(404, (await handler.Handle(query, default)).StatusCode);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => handler.Handle(query, new CancellationToken(true)));
        Assert.Empty(((RaporProxy)pdf).Projeler);
    }

    public class RaporProxy : DispatchProxy
    {
        public List<int> Projeler { get; } = [];
        public byte[] Bytes { get; } = [1, 2, 3]; // Gerçek PDF render testi değildir.
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method?.Name != nameof(IPdfService.EksikUrunlerRaporuPdfOlusturAsync)) throw new NotSupportedException();
            Projeler.Add((int)args![0]!);
            return Task.FromResult(Bytes);
        }
    }

    private sealed class KapatFixture
    {
        public OrtakMemoryUow Uow { get; } = new();
        public OrtakHareket Hareket { get; } = new();
        public OrtakSaha Saha { get; } = new();
        public Sandik Sandik { get; } = new() { Id = 1, ProjeId = 1, SandikNo = "1", DurumId = (int)SandikDurum.Hazirlaniyor };
        public CekiSatiri Satir { get; } = new() { Id = 10, IstenenAdet = 3m, GelenMiktar = 3m };
        public SandikIcerik Icerik { get; } = new() { Id = 100, SandikId = 1, CekiSatiriId = 10, TahsisMiktari = 3m };
        public KapatFixture() { Uow.Repo<Sandik>().Rows.Add(Sandik); Uow.Repo<CekiSatiri>().Rows.Add(Satir); Uow.Repo<SandikIcerik>().Rows.Add(Icerik); }
        public Task<SandikKapatResult> Single(bool force, bool auth = true) =>
            new SandikKapatCommandHandler(Uow, new OrtakUser(IsAuthenticated: auth), Hareket, Saha)
            .Handle(new() { SandikId = 1, ForceClose = force }, default);
        public Task<TopluSandikKapatResult> Bulk(bool force, List<int>? ids = null) =>
            new TopluSandikKapatCommandHandler(Uow, new OrtakUser(), Hareket, Saha)
            .Handle(new() { SandikIds = ids ?? [1], ForceClose = force }, default);
    }
}
