using _3K.Application.Features.SandikIslemleri.Queries;
using _3K.Core.Entities;
using _3K.Core.Enums;

namespace _3K.Application.Tests;

// Aynı bellekteki kaynak/saha verisini iki gerçek sorgu handler'ından geçirir.
// Repository predicate'leri çalışır; bu PostgreSQL SQL/transaction testi değildir.
public sealed class EksikUrunSorguIsKurallariTests
{
    [Fact]
    public async Task PlanlanabilirEksik_SahaTamamlamaPlaniniDuserVeKaynagiDegistirmez()
    {
        using var data = new Kurgu();
        var kaynak = data.Ekle(1, 1, 5m, 1m);
        data.Ekle(2, 2, 3m, 1m, kaynak.Id);

        var plan = await new GetEksikUrunlerByProjeQueryHandler(data.Uow)
            .Handle(new GetEksikUrunlerByProjeQuery { ProjeId = 1 }, default);

        Assert.True(plan.IsSuccess, plan.Error?.Message);
        var secilebilir = Assert.Single(plan.Value!);
        Assert.Equal(1m, secilebilir.KalanMiktar);
        Assert.Equal(3m, secilebilir.TamamlamaPlanlananAdet);
        Assert.Equal(5m, secilebilir.IstenenAdet);
        Assert.Equal(1m, secilebilir.GelenMiktar);
        Assert.Equal("FIILI", secilebilir.SandikNo);
        Assert.Equal(0, data.Uow.SaveCount);
        Assert.Equal(4m, kaynak.KalanMiktar);
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
    public async Task ProjeYoksa_NotFoundDoner()
    {
        using var data = new Kurgu();
        var proje = await new GetEksikUrunlerByProjeQueryHandler(data.Uow)
            .Handle(new GetEksikUrunlerByProjeQuery { ProjeId = 99 }, default);
        Assert.Equal(404, proje.StatusCode);
    }

    private sealed class Kurgu : IDisposable
    {
        public OrtakMemoryUow Uow { get; } = new();

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

        public void Dispose() => Uow.Dispose();
    }
}
