using _3K.Application.Features.GridIslemleri.Queries;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Tests;

public sealed class GridSozlesmeVeSorguIsKurallariTests
{
    [Fact]
    public void GridDurumKimlikleri_VeritabaniVeApiSozlesmesiniKorur()
    {
        var beklenen = new Dictionary<GridDurum, int>
        {
            [GridDurum.Bekliyor] = 1, [GridDurum.Uretimde] = 2,
            [GridDurum.StokHazir] = 3, [GridDurum.SevkEdildi] = 4,
            [GridDurum.KismiSevkEdildi] = 5, [GridDurum.Bekletiliyor] = 6,
            [GridDurum.IptalEdildi] = 7, [GridDurum.TamGeldi] = 8,
            [GridDurum.EksikGeldi] = 9, [GridDurum.Gelmedi] = 10,
            [GridDurum.TrafoSevk] = 11, [GridDurum.Iptal] = 12,
            [GridDurum.Sipariste] = 13, [GridDurum.GridKapandi] = 14
        };

        Assert.Equal(beklenen.Count, Enum.GetValues<GridDurum>().Length);
        Assert.All(beklenen, x => Assert.Equal(x.Value, (int)x.Key));
    }

    [Fact]
    public void GridSevkKimlikleri_GridKabulDurumundanBagimsizSozlesmeyiKorur()
    {
        var beklenen = new Dictionary<GridSevkDurum, int>
        {
            [GridSevkDurum.SevkEdildi] = 1, [GridSevkDurum.Bekliyor] = 2,
            [GridSevkDurum.SevkEdilmedi] = 3, [GridSevkDurum.YenidenSevkGerekli] = 4
        };

        Assert.Equal(beklenen.Count, Enum.GetValues<GridSevkDurum>().Length);
        Assert.All(beklenen, x => Assert.Equal(x.Value, (int)x.Key));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProjeVeyaUrunYoksa_NotFoundDonerVeKayitYapilmaz(bool projeVar)
    {
        using var uow = new OrtakMemoryUow();
        if (projeVar)
            uow.Repo<Proje>().Rows.Add(new Proje { Id = 1, ProjeTipiId = (int)ProjeTipi.Normal });

        var sonuc = await new GetGridUrunlerQueryHandler(uow, new LookupStub(), new OrtakSaha())
            .Handle(new GetGridUrunlerQuery { ProjeId = 1 }, default);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(404, sonuc.StatusCode);
        Assert.Equal(0, uow.SaveCount);
    }

    [Fact]
    public async Task UrunListesi_ProjeIzolasyonunuVeSiraNumarasiniKorur_YalnizOkur()
    {
        using var uow = new OrtakMemoryUow();
        var proje = new Proje { Id = 1, ProjeTipiId = (int)ProjeTipi.Normal };
        var ceki = new Ceki { Id = 10, ProjeId = proje.Id, Proje = proje };
        uow.Repo<Proje>().Rows.Add(proje);
        uow.Repo<Ceki>().Rows.Add(ceki);
        foreach (var (id, sira, satirCeki) in new[]
        {
            (101, 3, ceki), (102, 1, ceki), (103, 2, ceki),
            (201, 1, new Ceki { Id = 20, ProjeId = 2, Proje = new Proje { Id = 2 } })
        })
        {
            uow.Repo<CekiSatiri>().Rows.Add(new CekiSatiri
            {
                Id = id, Ceki = satirCeki, CekiId = satirCeki.Id, SiraNo = sira,
                BarkodNo = $"BC-{id}", Aciklama = "Ürün", IstenenAdet = 3,
                BirimId = (int)Birim.Adet, GridDurumuId = (int)GridDurum.Gelmedi,
                GridSevkDurumuId = (int)GridSevkDurum.SevkEdilmedi
            });
        }

        var sonuc = await new GetGridUrunlerQueryHandler(uow, new LookupStub(), new OrtakSaha())
            .Handle(new GetGridUrunlerQuery { ProjeId = proje.Id }, default);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(new[] { 102, 103, 101 }, sonuc.Value!.Select(x => x.CekiSatiriId));
        Assert.All(sonuc.Value!, x => Assert.Equal(3m, x.AnaIstenenAdet));
        Assert.Equal(0, uow.Repo<CekiSatiri>().UpdateCount);
        Assert.Equal(0, uow.SaveCount);
    }

    private sealed class LookupStub : ILookupCacheService
    {
        public string GetDeger<TLookup>(int id) where TLookup : LookupBase => $"{typeof(TLookup).Name}:{id}";
        public Task WarmupAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task RefreshAsync<TLookup>(CancellationToken ct = default) where TLookup : LookupBase => Task.CompletedTask;
    }
}
