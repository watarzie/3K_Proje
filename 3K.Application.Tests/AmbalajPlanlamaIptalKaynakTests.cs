using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri;
using _3K.Application.Features.AmbalajIslemleri.Commands;
using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Application.Features.AmbalajIslemleri.Queries;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Tests;

public sealed class AmbalajPlanlamaIptalKaynakTests
{
    [Theory]
    [InlineData(1, AmbalajSandikTuru.Normal)]
    [InlineData(2, AmbalajSandikTuru.Ilave)]
    public async Task IptalKaynak_OngoruOzetListeVePlanKalemineYenidenGirmez(int grup, AmbalajSandikTuru tur)
    {
        var f = new Fixture(); f.Source(11); f.Source(12);
        var iptal = f.Record(1, 11, iptal: true, tur: tur);
        f.Record(2, 12, tur: tur);
        var ozet = await f.List(grup); var plan = await f.Plan();

        Assert.Equal(2m, ozet.FilteredSummary!.ToplamHacimM3);
        Assert.Equal(1, ozet.FilteredSummary.ToplamSandikAdedi);
        var row = Assert.Single(ozet.Items);
        Assert.Equal(2m, row.ToplamHacimM3);
        Assert.Equal(2m, grup == 1 ? row.ProjeSandiklariHacimM3 : row.IlaveSandiklarHacimM3);
        Assert.Equal(12, Assert.Single(plan.Kalemler).KaynakSandikId);
        Assert.Equal(0m, plan.SeciliHacimM3); Assert.Equal(0m, row.UretimHacimM3);
        Assert.True(iptal.IptalMi); Assert.Equal(0, f.Uow.SaveCount);
    }

    [Theory]
    [InlineData(1, 99, true)]
    [InlineData(99, 1, true)]
    [InlineData(1, 99, false)]
    [InlineData(99, 1, false)]
    public async Task AyniKaynaktaAktifKarar_IptalGecmisiDahaYeniOlsaBileTercihEdilir(int aktifId, int iptalId, bool dahil)
    {
        var f = new Fixture(); f.Source(11).Ad = "Transformatör";
        var aktif = f.Record(aktifId, 11); aktif.AmbalajaDahil = dahil; aktif.M3Override = 7.25m;
        var iptal = f.Record(iptalId, 11, iptal: true); iptal.HesaplananToplamM3 = 100m;
        var ozet = await f.List(); var plan = await f.Plan();

        var expected = dahil ? 7.25m : 0m;
        Assert.Equal(expected, ozet.FilteredSummary!.ToplamHacimM3);
        Assert.Equal(expected, Assert.Single(ozet.Items).ProjeSandiklariHacimM3);
        var kalem = Assert.Single(plan.Kalemler);
        Assert.Equal(aktifId, kalem.Id); Assert.Equal(dahil, kalem.AmbalajaDahilMi);
        Assert.Equal(7.25m, kalem.HacimM3);
        Assert.Equal(dahil ? 1 : 0, ozet.FilteredSummary.ToplamSandikAdedi);
        Assert.True(iptal.IptalMi); Assert.False(aktif.IptalMi); Assert.Equal(0, f.Uow.SaveCount);
    }

    [Fact]
    public async Task IptalGecmisi_PlanBaslangiciniGeriCekipYeniKaynagiIlaveyeTasiyamaz()
    {
        var f = new Fixture(); f.Source(11); f.Source(12);
        f.Source(13).CreatedDate = new(2026, 8, 1);
        f.Record(1, 11, iptal: true).CreatedDate = new(2025, 1, 1);
        f.Record(2, 12).CreatedDate = new(2026, 9, 1);

        var normal = await f.List(1); var ilave = await f.List(2); var plan = await f.Plan();
        Assert.Equal(2, normal.FilteredSummary!.ToplamSandikAdedi);
        Assert.Equal(0, ilave.FilteredSummary!.ToplamSandikAdedi);
        Assert.Equal(0m, ilave.FilteredSummary.ToplamHacimM3);
        Assert.All(plan.Kalemler, x => Assert.Equal(1, x.Tur));
        Assert.DoesNotContain(plan.Kalemler, x => x.KaynakSandikId == 11);
        Assert.Equal(0, f.Uow.SaveCount);
    }

    [Fact]
    public async Task TumKayitlarProjeksiyonaAlinsaDa_IptalManuelVeBagimsizKayitlarDonmez()
    {
        var f = new Fixture();
        var iptalManuel = f.Record(1, null, iptal: true);
        var bagimsiz = f.Record(2, null); bagimsiz.BagimsizKayitMi = true;
        var aktifManuel = f.Record(3, null); aktifManuel.Tur = AmbalajSandikTuru.Ic;
        var ozet = await f.List(3); var plan = await f.Plan();

        Assert.Equal(3, Assert.Single(plan.Kalemler).Id);
        Assert.Equal(2m, ozet.FilteredSummary!.ToplamHacimM3);
        Assert.Equal(2m, Assert.Single(ozet.Items).IcSandiklarHacimM3);
        Assert.True(iptalManuel.IptalMi); Assert.Equal(0, f.Uow.SaveCount);
    }

    [Fact]
    public async Task AktifHalefinDurumIzniYoksa_IptalKayitVeyaPlansizTahminleGosterilmez()
    {
        var f = new Fixture(); f.Source(11); f.Source(12);
        f.Record(1, 11, iptal: true);
        f.Record(2, 11).UretimDurumu = AmbalajUretimDurumu.Tamamlandi;
        f.Record(3, 12).UretimDurumu = AmbalajUretimDurumu.Uretimde;
        var ozet = await f.List(allowed: [2]); var plan = await f.Plan([2]);

        Assert.Equal(2m, ozet.FilteredSummary!.ToplamHacimM3);
        Assert.Equal(2m, Assert.Single(ozet.Items).ProjeSandiklariHacimM3);
        Assert.Equal(12, Assert.Single(plan.Kalemler).KaynakSandikId);
        Assert.Equal(0, f.Uow.SaveCount);
    }

    [Fact]
    public async Task PlanKaydetResponse_IptalKaynaklariYenidenCanlandirmaz()
    {
        var f = new Fixture(); f.Source(11); f.Source(12);
        var iptal = f.Record(1, 11, iptal: true); var aktif = f.Record(2, 12);
        aktif.UretimeAlindi = true;
        var result = await new AmbalajPlanKaydetCommandHandler(f.Uow, new OrtakUser(), new Sync(), new Finans())
            .Handle(new() { ProjeId = 10, SeciliKaynakSandikIds = [12] }, default);
        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(12, Assert.Single(result.Value!.Kalemler).KaynakSandikId);
        Assert.True(iptal.IptalMi); Assert.False(iptal.UretimeAlindi);
        Assert.Equal(0, f.Uow.Repo<AmbalajUretimKaydi>().UpdateCount);
    }

    private sealed class Fixture
    {
        public OrtakMemoryUow Uow { get; } = new();
        public Fixture()
        {
            Uow.Repo<Proje>().Rows.Add(new() { Id = 10, ProjeNo = "IPTAL", ProjeTipiId = 1 });
            Uow.Repo<LookupProjeTipi>().Rows.Add(new() { Id = 1, Anahtar = 1, Deger = "Normal" });
        }
        public Sandik Source(int id)
        {
            var row = new Sandik { Id = id, ProjeId = 10, SandikNo = id.ToString(), Ad = "Radyatör",
                TipId = 1, Boy = 2000, En = 1000, Yukseklik = 1200, CreatedDate = new(2026, 1, 1) };
            Uow.Repo<Sandik>().Rows.Add(row); return row;
        }
        public AmbalajUretimKaydi Record(int id, int? kaynak, bool iptal = false, AmbalajSandikTuru tur = AmbalajSandikTuru.Normal)
        {
            var row = new AmbalajUretimKaydi { Id = id, ProjeId = 10, KaynakKayitId = kaynak,
                KaynakModul = kaynak.HasValue ? AmbalajKaynakModulu.Sandik : AmbalajKaynakModulu.Manuel,
                Tur = tur, IptalMi = iptal, AmbalajaDahil = true, UretimeAlindi = false,
                SandikNo = id.ToString(), Ad = "Radyatör", Adet = 1, SandikCinsi = AmbalajSandikCinsi.AhsapKapali,
                Boy = 2000, En = 1000, Yukseklik = 1200, HesaplananToplamM3 = 2m, CreatedDate = new(2026, 2, 1) };
            Uow.Repo<AmbalajUretimKaydi>().Rows.Add(row); return row;
        }
        public async Task<AmbalajPlanlamaProjeleriSayfasiDto> List(int group = 1, int[]? allowed = null)
        {
            var result = await new GetAmbalajPlanlamaProjeleriQueryHandler(Uow)
                .Handle(new() { Grup = group, IzinliDurumlar = allowed }, default);
            Assert.True(result.IsSuccess, result.Error?.Message); return result.Value!;
        }
        public async Task<AmbalajPlanlamaPlanDto> Plan(int[]? allowed = null)
        {
            var result = await new GetAmbalajPlanlamaPlanQueryHandler(Uow)
                .Handle(new() { ProjeId = 10, IzinliDurumlar = allowed }, default);
            Assert.True(result.IsSuccess, result.Error?.Message); return result.Value!;
        }
    }
    private sealed class Sync : IAmbalajKaynakSenkronizasyonService
    {
        public Task<Result<AmbalajSenkronizasyonSonucuDto>> SenkronizeEtAsync(int id, ICurrentUserService actor,
            CancellationToken ct, bool sonucKayitlariniOlustur = true) =>
            Task.FromResult(Result<AmbalajSenkronizasyonSonucuDto>.Success(new(0, 0, 0, 0, [])));
    }
    private sealed class Finans : IFinansUretimAktarimService
    {
        public Task<FinansSenkronizasyonSonucModel> UretimKayitlariniAktarAsync(IReadOnlyList<FinansUretimAktarimModel> rows, CancellationToken ct) =>
            throw new InvalidOperationException("Etkisiz plan kaydı finans aktarımı yapmamalıdır.");
    }
}
