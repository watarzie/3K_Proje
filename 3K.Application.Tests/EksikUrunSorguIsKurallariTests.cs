using System.Reflection;
using _3K.Application.Common;
using _3K.Application.Features.SandikIslemleri.DTOs;
using _3K.Application.Features.SandikIslemleri.Queries;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Tests;

// Aynı bellekteki kaynak/saha verisini iki gerçek sorgu handler'ından geçirir.
// Repository predicate'leri çalışır; bu PostgreSQL SQL/transaction testi değildir.
public sealed class EksikUrunSorguIsKurallariTests
{
    [Fact]
    public async Task AyniKaynaktaOperasyonelEksikVePlanlanabilirEksik_FarkliSahaMiktarlariniKullanir()
    {
        using var data = new Kurgu();
        var kaynak = data.Ekle(1, 1, 5m, 1m);
        data.Ekle(2, 2, 3m, 1m, kaynak.Id);
        data.Map.Miktarlar[kaynak.Id] = 1m;

        var operasyonel = await data.Operasyonel(1);
        var plan = await new GetEksikUrunlerByProjeQueryHandler(data.Uow)
            .Handle(new GetEksikUrunlerByProjeQuery { ProjeId = 1 }, default);

        Assert.True(operasyonel.IsSuccess, operasyonel.Error?.Message);
        Assert.True(plan.IsSuccess, plan.Error?.Message);
        Assert.Equal(3m, Assert.Single(operasyonel.Value!).EksikMiktar);
        var secilebilir = Assert.Single(plan.Value!);
        Assert.Equal(1m, secilebilir.KalanMiktar);
        Assert.Equal(3m, secilebilir.TamamlamaPlanlananAdet);
        Assert.Equal(5m, secilebilir.IstenenAdet);
        Assert.Equal(1m, secilebilir.GelenMiktar);
        Assert.Equal("FIILI", secilebilir.SandikNo);
        Assert.Equal([kaynak.Id], data.Map.IstenenIdler);
        Assert.Equal(0, data.Uow.SaveCount);
        Assert.Equal(4m, kaynak.KalanMiktar);
    }

    [Fact]
    public async Task SahaKopyasi_KendiEksigindenSahaHaritasiniTekrarDusmez()
    {
        using var data = new Kurgu();
        var kopya = data.Ekle(2, 2, 3.5001m, 1.25m, kaynakId: 1);
        data.Map.Miktarlar[kopya.Id] = 100m;

        var sonuc = await data.Operasyonel(2);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(2.2501m, Assert.Single(sonuc.Value!).EksikMiktar);
        Assert.Empty(data.Map.IstenenIdler);
        Assert.Equal(0, data.Uow.SaveCount);
    }

    [Fact]
    public async Task OperasyonelListe_SifirVeTerminalSatirlariEler_ProjeIzolasyonuVeSiraKorunur()
    {
        using var data = new Kurgu();
        data.Ekle(5, 1, 1m, 0m);
        data.Ekle(2, 1, 1.0001m, 1m);
        data.Ekle(1, 1, 1m, 1m);
        data.Ekle(3, 1, 1m, 0m).GridDurumuId = (int)GridDurum.Iptal;
        data.Ekle(4, 1, 1m, 0m).GridDurumuId = (int)GridDurum.GridKapandi;
        data.Ekle(6, 1, 1m, 0m);
        data.Map.Miktarlar[6] = 10m;
        data.Ekle(7, 2, 99m, 0m);

        var sonuc = await data.Operasyonel(1);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal([2, 5], sonuc.Value!.Select(x => x.CekiSatiriId));
        Assert.Equal(.0001m, sonuc.Value![0].EksikMiktar);
        Assert.Equal(0, data.Uow.SaveCount);
    }

    [Fact]
    public async Task PlanliTamamlamalarToplanir_AsiriPlanNegatifSecilebilirMiktarUretmez()
    {
        using var data = new Kurgu();
        var kaynak = data.Ekle(1, 1, 3.0001m, 1m);
        data.Ekle(2, 2, 1m, 0m, kaynak.Id);
        data.Ekle(3, 2, 1.0002m, 0m, kaynak.Id);

        var sonuc = await new GetEksikUrunlerByProjeQueryHandler(data.Uow)
            .Handle(new GetEksikUrunlerByProjeQuery { ProjeId = 1 }, default);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Empty(sonuc.Value!);
        Assert.Equal(2.0001m, kaynak.KalanMiktar);
        Assert.Equal(0, data.Uow.SaveCount);
    }

    [Theory]
    [InlineData(ProjeTipi.Saha)]
    [InlineData(ProjeTipi.Yedek)]
    public async Task PlanSecimi_NormalOlmayanKaynakProjeyiReddeder(ProjeTipi tip)
    {
        using var data = new Kurgu();
        data.Uow.Repo<Proje>().Rows[0].ProjeTipiId = (int)tip;
        var sonuc = await new GetEksikUrunlerByProjeQueryHandler(data.Uow)
            .Handle(new GetEksikUrunlerByProjeQuery { ProjeId = 1 }, default);
        Assert.False(sonuc.IsSuccess);
        Assert.Equal(0, data.Uow.Repo<CekiSatiri>().FindCount);
        Assert.Equal(0, data.Uow.SaveCount);
    }

    [Fact]
    public async Task ProjeVeyaCekiYoksa_NotFoundDonerVeSahaSorgusuYapilmaz()
    {
        using var data = new Kurgu();
        var proje = await new GetEksikUrunlerByProjeQueryHandler(data.Uow)
            .Handle(new GetEksikUrunlerByProjeQuery { ProjeId = 99 }, default);
        var ceki = await data.Operasyonel(99);
        Assert.Equal(404, proje.StatusCode);
        Assert.Equal(404, ceki.StatusCode);
        Assert.False(data.Map.Cagirildi);
    }

    private sealed class Kurgu : IDisposable
    {
        public OrtakMemoryUow Uow { get; } = new();
        public ISahaTamamlamaService Saha { get; } = DispatchProxy.Create<ISahaTamamlamaService, SahaIsMapProxy>();
        public SahaIsMapProxy Map => (SahaIsMapProxy)Saha;

        public Kurgu()
        {
            foreach (var id in new[] { 1, 2 })
            {
                var proje = new Proje { Id = id, ProjeNo = $"PA-{id}", ProjeTipiId = (int)(id == 1 ? ProjeTipi.Normal : ProjeTipi.Saha) };
                Uow.Repo<Proje>().Rows.Add(proje);
                Uow.Repo<Ceki>().Rows.Add(new Ceki { Id = id, ProjeId = id, Proje = proje });
            }
        }

        public CekiSatiri Ekle(int id, int proje, decimal istenen, decimal gelen, int? kaynakId = null)
        {
            var ceki = Uow.Repo<Ceki>().Rows.Single(x => x.ProjeId == proje);
            var satir = new CekiSatiri
            {
                Id = id, CekiId = ceki.Id, Ceki = ceki, SiraNo = id,
                BarkodNo = "AYNI-BARKOD", Aciklama = "Urun", IstenenAdet = istenen,
                GelenMiktar = gelen, KaynakCekiSatiriId = kaynakId,
                GridDurumuId = (int)GridDurum.EksikGeldi, BirimId = (int)Birim.Adet,
                CekideGecenSandikNo = "ORIJINAL", FiiliSandikNo = "FIILI"
            };
            Uow.Repo<CekiSatiri>().Rows.Add(satir);
            return satir;
        }

        public Task<Result<List<EksikUrunDto>>> Operasyonel(int proje) =>
            new GetEksikUrunlerQueryHandler(Uow, new Lookup(), Saha)
                .Handle(new GetEksikUrunlerQuery { ProjeId = proje }, default);
        public void Dispose() => Uow.Dispose();
    }

    public class SahaIsMapProxy : DispatchProxy
    {
        public Dictionary<int, decimal> Miktarlar { get; } = [];
        public int[] IstenenIdler { get; private set; } = [];
        public bool Cagirildi { get; private set; }
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method?.Name != nameof(ISahaTamamlamaService.GetAktifIsTamamlamaMapAsync))
                throw new InvalidOperationException("Operasyonel eksik, aktarma/sevk haritasini degil is tamamlama haritasini istemelidir.");
            Cagirildi = true;
            IstenenIdler = ((IEnumerable<int>)args![0]!).ToArray();
            return Task.FromResult(Miktarlar);
        }
    }

    private sealed class Lookup : ILookupCacheService
    {
        public string GetDeger<TLookup>(int id) where TLookup : LookupBase => id.ToString();
        public Task WarmupAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task RefreshAsync<TLookup>(CancellationToken ct = default) where TLookup : LookupBase => Task.CompletedTask;
    }
}
