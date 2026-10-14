using _3K.Application.Common;
using _3K.Application.Features.GridIslemleri.DTOs;
using _3K.Application.Features.GridIslemleri.Queries;
using _3K.Application.Features.UcKIslemleri.DTOs;
using _3K.Application.Features.UcKIslemleri.Queries;
using _3K.Application.Features.UcKIslemleri.Validators;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Tests;

/// <summary>
/// 3K "Projeden Karşılandı" kaynak seçimi. Kaynak listesi eskiden Grid ekranının uç noktasından
/// okunuyordu; Grid izni olmayan 3K personeli 403 alıyor, ön yüz bunu "ürün yok" diye gösteriyordu.
/// Yeni sorgu aynı satır ve miktarları döndürmeli, yalnız seçilemeyen manuel içerikleri dışarıda bırakmalıdır.
/// </summary>
public sealed class ProjedenKarsilamaKaynaklariTests
{
    private const int HedefProjeId = 1;
    private const int KaynakProjeId = 2;

    [Fact]
    public async Task KaynakSatirlari_GridListesiyleAyniSatirVeMiktarlariDondurur()
    {
        using var k = new Kurgu(ProjeTipi.Normal);
        var sandik1 = k.SandikEkle(301, "S1");
        var sandik2 = k.SandikEkle(302, "S2");

        // Tek sandıkta, iki sandığa bölünmüş ve sandık içeriği olmayan üç çeki satırı.
        var tek = k.SatirEkle(101, siraNo: 1, barkod: "SMC45112", aciklama: "Kollektör Konsolu", istenen: 2, gelen: 2);
        k.IcerikEkle(401, sandik1, tek, tahsis: 2, konulan: 2);
        var bolunmus = k.SatirEkle(102, siraNo: 2, barkod: "SMC45113", aciklama: "Kollektör Konsolu", istenen: 3, gelen: 3);
        k.IcerikEkle(402, sandik1, bolunmus, tahsis: 1, konulan: 1);
        k.IcerikEkle(403, sandik2, bolunmus, tahsis: 2, konulan: 2);
        k.SatirEkle(103, siraNo: 3, barkod: "SMC45114", aciklama: "Kollektör Destek", istenen: 1, gelen: 1);

        var grid = await k.GridAsync();
        var kaynaklar = await k.KaynaklarAsync();

        Assert.True(kaynaklar.IsSuccess, kaynaklar.Error?.Message);
        Assert.Equal(4, grid.Count);
        Assert.Equal(grid.Select(Ozet), kaynaklar.Value!.Select(Ozet));
        Assert.Equal(new[] { 2m, 1m, 2m, 1m }, kaynaklar.Value!.Select(x => x.NetKullanilabilir));
        Assert.Equal(new[] { 101, 102, 102, 103 }, kaynaklar.Value!.Select(x => x.CekiSatiriId));
        Assert.Equal(0, k.Uow.SaveCount);
        Assert.Equal(0, k.Uow.Repo<CekiSatiri>().UpdateCount);
    }

    [Theory]
    [InlineData(ProjeTipi.Saha)]
    [InlineData(ProjeTipi.Yedek)]
    public async Task ManuelSandikIcerigi_KaynakSecenegiOlarakSunulmaz(ProjeTipi kaynakTipi)
    {
        using var k = new Kurgu(kaynakTipi);
        var sandik = k.SandikEkle(301, "S1");
        var satir = k.SatirEkle(101, siraNo: 1, barkod: "SMC45112", aciklama: "Kollektör Konsolu", istenen: 2, gelen: 2);
        k.IcerikEkle(401, sandik, satir, tahsis: 2, konulan: 2);
        k.Uow.Repo<SandikIcerik>().Rows.Add(new SandikIcerik
        {
            Id = 402, SandikId = sandik.Id, Sandik = sandik, Isim = "Kollektör Konsolu", Miktar = 5, KonulanAdet = 5
        });

        var grid = await k.GridAsync();
        var kaynaklar = await k.KaynaklarAsync();

        // Grid manuel içeriği negatif kimlikle gösterir; karşılama komutu pozitif çeki satırı ister.
        Assert.Contains(grid, x => x.IsSahaManuelSandikIcerigi && x.CekiSatiriId < 0);
        Assert.True(kaynaklar.IsSuccess, kaynaklar.Error?.Message);
        Assert.Equal(new[] { 101 }, kaynaklar.Value!.Select(x => x.CekiSatiriId));
        Assert.Equal(grid.Where(x => !x.IsSahaManuelSandikIcerigi).Select(Ozet), kaynaklar.Value!.Select(Ozet));
    }

    [Fact]
    public async Task UrunsuzKaynakProje_BosListeDoner_GridGibi404Vermez()
    {
        using var k = new Kurgu(ProjeTipi.Normal);

        var grid = await new GetGridUrunlerQueryHandler(k.Uow, new LookupStub(), new OrtakSaha())
            .Handle(new GetGridUrunlerQuery { ProjeId = KaynakProjeId }, default);
        var kaynaklar = await k.KaynaklarAsync();

        Assert.Equal(404, grid.StatusCode);
        Assert.True(kaynaklar.IsSuccess, kaynaklar.Error?.Message);
        Assert.Empty(kaynaklar.Value!);
    }

    [Fact]
    public async Task KaynakProjeYoksa_404Doner()
    {
        using var uow = new OrtakMemoryUow();
        uow.Repo<Proje>().Rows.Add(new Proje { Id = HedefProjeId, ProjeTipiId = (int)ProjeTipi.Normal });

        var sonuc = await new GetProjedenKarsilamaKaynaklariQueryHandler(uow, new LookupStub(), new OrtakSaha())
            .Handle(new GetProjedenKarsilamaKaynaklariQuery { HedefProjeId = HedefProjeId, KaynakProjeId = 99 }, default);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(404, sonuc.StatusCode);
        Assert.Equal("Kaynak proje bulunamadı.", sonuc.Error!.Message);
    }

    [Theory]
    [InlineData(1, 2, true, null)]
    [InlineData(1, 1, false, "Bir proje kendi ürünlerinden karşılama yapamaz.")]
    [InlineData(1, 0, false, "Geçerli bir kaynak proje ID belirtilmeli.")]
    [InlineData(0, 2, false, "Geçerli bir proje ID belirtilmeli.")]
    public void Validator_KendiProjesindenVeGecersizKimliktenKaynakListesiAcmaz(
        int hedef, int kaynak, bool gecerli, string? beklenenMesaj)
    {
        var sonuc = new GetProjedenKarsilamaKaynaklariQueryValidator()
            .Validate(new GetProjedenKarsilamaKaynaklariQuery { HedefProjeId = hedef, KaynakProjeId = kaynak });

        Assert.Equal(gecerli, sonuc.IsValid);
        if (beklenenMesaj != null)
            Assert.Contains(sonuc.Errors, e => e.ErrorMessage == beklenenMesaj);
    }

    private static (int, int, string, string?, string, decimal, decimal) Ozet(GridUrunDto x) =>
        (x.CekiSatiriId, x.SiraNo, x.BarkodNo, x.OlcuResmiPozNo, x.Aciklama, x.GelenMiktar, x.NetKullanilabilir);

    private static (int, int, string, string?, string, decimal, decimal) Ozet(ProjedenKarsilamaKaynakDto x) =>
        (x.CekiSatiriId, x.SiraNo, x.BarkodNo, x.OlcuResmiPozNo, x.Aciklama, x.GelenMiktar, x.NetKullanilabilir);

    private sealed class Kurgu : IDisposable
    {
        private readonly Proje _kaynak;
        private readonly Ceki _ceki;
        public OrtakMemoryUow Uow { get; } = new();

        public Kurgu(ProjeTipi kaynakTipi)
        {
            _kaynak = new Proje { Id = KaynakProjeId, ProjeNo = "PA600-23", ProjeTipiId = (int)kaynakTipi };
            _ceki = new Ceki { Id = 20, ProjeId = _kaynak.Id, Proje = _kaynak };
            Uow.Repo<Proje>().Rows.Add(new Proje { Id = HedefProjeId, ProjeNo = "PA600-24", ProjeTipiId = (int)ProjeTipi.Normal });
            Uow.Repo<Proje>().Rows.Add(_kaynak);
            Uow.Repo<Ceki>().Rows.Add(_ceki);
        }

        public Sandik SandikEkle(int id, string sandikNo)
        {
            var sandik = new Sandik { Id = id, ProjeId = _kaynak.Id, Proje = _kaynak, SandikNo = sandikNo };
            Uow.Repo<Sandik>().Rows.Add(sandik);
            return sandik;
        }

        public CekiSatiri SatirEkle(int id, int siraNo, string barkod, string aciklama,
            decimal istenen, decimal gelen)
        {
            var satir = new CekiSatiri
            {
                Id = id, CekiId = _ceki.Id, Ceki = _ceki, SiraNo = siraNo, BarkodNo = barkod,
                OlcuResmiPozNo = siraNo.ToString(), Aciklama = aciklama, IstenenAdet = istenen,
                GelenMiktar = gelen, BirimId = (int)Birim.Adet,
                GridDurumuId = (int)GridDurum.TamGeldi, GridGelenAdet = istenen,
                GridSevkDurumuId = (int)GridSevkDurum.SevkEdildi, UcKDurumuId = (int)UcKDurum.TamGeldi
            };
            Uow.Repo<CekiSatiri>().Rows.Add(satir);
            return satir;
        }

        public void IcerikEkle(int id, Sandik sandik, CekiSatiri satir, decimal tahsis, decimal konulan)
        {
            Uow.Repo<SandikIcerik>().Rows.Add(new SandikIcerik
            {
                Id = id, SandikId = sandik.Id, Sandik = sandik, CekiSatiriId = satir.Id, CekiSatiri = satir,
                TahsisMiktari = tahsis, KonulanAdet = konulan
            });
        }

        public async Task<List<GridUrunDto>> GridAsync()
        {
            var sonuc = await new GetGridUrunlerQueryHandler(Uow, new LookupStub(), new OrtakSaha())
                .Handle(new GetGridUrunlerQuery { ProjeId = KaynakProjeId }, default);
            Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
            return sonuc.Value!;
        }

        public Task<Result<List<ProjedenKarsilamaKaynakDto>>> KaynaklarAsync() =>
            new GetProjedenKarsilamaKaynaklariQueryHandler(Uow, new LookupStub(), new OrtakSaha())
                .Handle(new GetProjedenKarsilamaKaynaklariQuery { HedefProjeId = HedefProjeId, KaynakProjeId = KaynakProjeId }, default);

        public void Dispose() => Uow.Dispose();
    }

    private sealed class LookupStub : ILookupCacheService
    {
        public string GetDeger<TLookup>(int id) where TLookup : LookupBase => $"{typeof(TLookup).Name}:{id}";
        public Task WarmupAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task RefreshAsync<TLookup>(CancellationToken ct = default) where TLookup : LookupBase => Task.CompletedTask;
    }
}
