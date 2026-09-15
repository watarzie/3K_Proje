using _3K.Application.Common;
using _3K.Application.Features.GridIslemleri.Commands;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Tests;

public class OrtakKaliteSurecIsKurallariTests
{
    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task KaliteVeSurec_BirbiriniVeyaMiktarlariDegistirmedenEskiYeniDegeriLoglar(bool kalite)
    {
        var data = new Fixture();
        var result = await data.Run(kalite);
        Assert.True(result.IsSuccess);
        Assert.Equal(kalite ? 2 : 1, data.Satir.KaliteDurumId);
        Assert.Equal(kalite ? 1 : 2, data.Satir.SurecDurumId);
        Assert.Equal(3.0001m, data.Satir.IstenenAdet);
        Assert.Equal(2.0001m, data.Satir.GelenMiktar);
        var history = Assert.Single(data.Hareket.Rows);
        Assert.Equal("Eski", history.EskiDeger);
        Assert.Equal("Yeni", history.YeniDeger);
        Assert.Equal(7, history.KullaniciId);
        Assert.Equal("10", history.ReferansId);
        Assert.Equal(1, data.Uow.SaveCount);
    }

    [Theory]
    [InlineData(false, "secimsiz")][InlineData(true, "secimsiz")]
    [InlineData(false, "baskaProje")][InlineData(true, "baskaProje")]
    [InlineData(false, "saha")][InlineData(true, "saha")]
    [InlineData(false, "kilit")][InlineData(true, "kilit")]
    public async Task KaliteVeSurec_SecimProjeSahaVeSevkKilitleriniKorur(bool kalite, string reason)
    {
        var data = new Fixture();
        if (reason == "saha") data.Saha.AktifKaynaklar.Add(10);
        if (reason == "kilit")
        {
            data.Uow.Repo<SandikIcerik>().Rows.Add(new() { Id = 1, CekiSatiriId = 10, SandikId = 20 });
            data.Uow.Repo<Sandik>().Rows.Add(new() { Id = 20, DurumId = (int)SandikDurum.Sevkedildi });
        }
        var result = await data.Run(kalite, reason == "baskaProje" ? 2 : 1, reason == "secimsiz" ? [] : [10]);
        Assert.False(result.IsSuccess);
        Assert.Equal(1, data.Satir.KaliteDurumId);
        Assert.Equal(1, data.Satir.SurecDurumId);
        Assert.Equal(0, data.Uow.SaveCount);
        Assert.Empty(data.Hareket.Rows);
    }

    [Fact]
    public async Task TamamlanmisSurec_BaskaSureceCevrilemez()
    {
        var data = new Fixture();
        data.Satir.SurecDurumId = (int)SurecDurum.Tamamlandi;
        Assert.Equal(409, (await data.Run(false)).StatusCode);
        Assert.Equal((int)SurecDurum.Tamamlandi, data.Satir.SurecDurumId);
        Assert.Equal(0, data.Uow.SaveCount);
        Assert.Empty(data.Hareket.Rows);
    }

    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task AyniBarkoddaBaskaSatirVeProjeyeDokunulmaz(bool kalite)
    {
        var data = new Fixture();
        var diger = new CekiSatiri { Id = 11, Ceki = new() { ProjeId = 1 }, BarkodNo = "AYNI", KaliteDurumId = 1, SurecDurumId = 1 };
        var baska = new CekiSatiri { Id = 12, Ceki = new() { ProjeId = 2 }, BarkodNo = "AYNI", KaliteDurumId = 1, SurecDurumId = 1 };
        data.Satir.BarkodNo = "AYNI";
        data.Uow.Repo<CekiSatiri>().Rows.AddRange([diger, baska]);
        Assert.True((await data.Run(kalite)).IsSuccess);
        Assert.Equal(1, diger.KaliteDurumId);
        Assert.Equal(1, diger.SurecDurumId);
        Assert.Equal(1, baska.KaliteDurumId);
        Assert.Equal(1, baska.SurecDurumId);
        Assert.Single(data.Hareket.Rows);
    }

    [Theory]
    [InlineData(GridDurum.TamGeldi, "3", false, true)]
    [InlineData(GridDurum.EksikGeldi, "2.9999", false, false)]
    [InlineData(GridDurum.Iptal, "0", false, false)]
    [InlineData(GridDurum.GridKapandi, "0", false, true)]
    [InlineData(GridDurum.Gelmedi, "0", true, true)]
    public void SurecOtomatikTamamlama_GridEksiginiIzlerIptaldenTamamlamaUretmezVeTamamlananiAcmaz(GridDurum grid, string gelen, bool zatenTam, bool expected)
    {
        var satir = new CekiSatiri { GridDurumuId = (int)grid, IstenenAdet = 3m,
            GridGelenAdet = decimal.Parse(gelen, System.Globalization.CultureInfo.InvariantCulture),
            SurecDurumId = zatenTam ? (int)SurecDurum.Tamamlandi : (int)SurecDurum.Ambar };
        GridSurecDurumHelper.SyncSurecTamamlandi(satir);
        Assert.Equal(expected, GridSurecDurumHelper.IsTamamlandi(satir));
    }

    private sealed class Fixture
    {
        public OrtakMemoryUow Uow { get; } = new();
        public OrtakHareket Hareket { get; } = new();
        public OrtakSaha Saha { get; } = new();
        public CekiSatiri Satir { get; } = new() { Id = 10, Ceki = new() { ProjeId = 1 }, KaliteDurumId = 1, SurecDurumId = 1, IstenenAdet = 3.0001m, GelenMiktar = 2.0001m };
        public Fixture() => Uow.Repo<CekiSatiri>().Rows.Add(Satir);
        public Task<Result> Run(bool kalite, int proje = 1, List<int>? ids = null) => kalite
            ? new KaliteDurumGuncelleCommandHandler(Uow, Hareket, new OrtakUser(), new Lookup(), Saha)
                .Handle(new() { ProjeId = proje, CekiSatiriIdler = ids ?? [10], KaliteDurumId = 2 }, default)
            : new SurecDurumGuncelleCommandHandler(Uow, Hareket, new OrtakUser(), new Lookup(), Saha)
                .Handle(new() { ProjeId = proje, CekiSatiriIdler = ids ?? [10], SurecDurumId = 2 }, default);
    }

    private sealed class Lookup : ILookupCacheService
    {
        public string GetDeger<T>(int id) where T : LookupBase => id switch { 1 => "Eski", 2 => "Yeni", _ => "?" };
        public Task WarmupAsync(CancellationToken token = default) => throw new NotSupportedException();
        public Task RefreshAsync<T>(CancellationToken token = default) where T : LookupBase => throw new NotSupportedException();
    }
}
