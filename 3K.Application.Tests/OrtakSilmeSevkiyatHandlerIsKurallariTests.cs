using _3K.Application.Common;
using _3K.Application.Features.CekiIslemleri.Commands;
using _3K.Application.Features.SandikIslemleri.Commands;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Infrastructure.Services;

namespace _3K.Application.Tests;

public class OrtakSilmeSevkiyatHandlerIsKurallariTests
{
    [Theory]
    [InlineData("manuelDegil")][InlineData("baskaProje")][InlineData("baskaTip")]
    [InlineData("kaynak")][InlineData("aktifSaha")][InlineData("kilit")]
    [InlineData("islem")][InlineData("satirYok")][InlineData("cekiYok")]
    public async Task NormalManuelSilme_UygunlukKontrolleriHicbirKaydiSilmedenReddeder(string reason)
    {
        var data = new Fixture(ProjeTipi.Normal);
        if (reason == "manuelDegil") data.Satir.IsManuelEklenen = false;
        if (reason == "baskaTip") data.Proje.ProjeTipiId = (int)ProjeTipi.Saha;
        data.ApplyBlock(reason);
        if (reason == "cekiYok") data.Uow.Repo<Ceki>().Rows.Clear();
        var counts = data.Counts();
        var result = await data.Manual(false, reason == "baskaProje" ? 2 : 1);
        Assert.False(result.IsSuccess);
        data.AssertUnchanged(counts);
    }

    [Theory]
    [InlineData(ProjeTipi.Saha, "manuelDegil")][InlineData(ProjeTipi.Yedek, "manuelDegil")]
    [InlineData(ProjeTipi.Saha, "kaynak")][InlineData(ProjeTipi.Yedek, "kaynak")]
    [InlineData(ProjeTipi.Saha, "aktifSaha")][InlineData(ProjeTipi.Yedek, "aktifSaha")]
    [InlineData(ProjeTipi.Saha, "kilit")][InlineData(ProjeTipi.Yedek, "kilit")]
    [InlineData(ProjeTipi.Saha, "islem")][InlineData(ProjeTipi.Yedek, "islem")]
    [InlineData(ProjeTipi.Saha, "baskaProje")][InlineData(ProjeTipi.Yedek, "baskaProje")]
    [InlineData(ProjeTipi.Saha, "baskaTip")][InlineData(ProjeTipi.Yedek, "baskaTip")]
    [InlineData(ProjeTipi.Saha, "satirYok")][InlineData(ProjeTipi.Yedek, "satirYok")]
    public async Task SahaYedekManuelSilme_ImportKaynakIslemKilitVeProjeBaglariniKorur(ProjeTipi type, string reason)
    {
        var data = new Fixture(type);
        if (reason == "manuelDegil") data.Satir.IsManuelEklenen = false;
        if (reason == "baskaTip") data.Proje.ProjeTipiId = (int)ProjeTipi.Normal;
        data.ApplyBlock(reason);
        var counts = data.Counts();
        var result = await data.Manual(true, reason == "baskaProje" ? 2 : 1);
        Assert.False(result.IsSuccess);
        data.AssertUnchanged(counts);
    }

    [Theory]
    [InlineData(ProjeTipi.Normal)][InlineData(ProjeTipi.Saha)][InlineData(ProjeTipi.Yedek)]
    public async Task ManuelSilme_BagliDigerTahsisKilitliyseSeciliAcikSandiktaDaSilmez(ProjeTipi type)
    {
        var data = new Fixture(type);
        data.AddSecondAllocation(true);
        var counts = data.Counts();
        Assert.False((await data.Manual(type != ProjeTipi.Normal)).IsSuccess);
        data.AssertUnchanged(counts);
        Assert.Equal((int)SandikDurum.Kapandi, data.Sandik.DurumId);
    }

    [Theory]
    [InlineData(ProjeTipi.Normal)][InlineData(ProjeTipi.Saha)][InlineData(ProjeTipi.Yedek)]
    public async Task ManuelSilme_TemizSatirinTumTahsisleriniSilerDigerAyniBarkoduKorur(ProjeTipi type)
    {
        var data = new Fixture(type);
        data.AddSecondAllocation(false);
        var survivor = data.AddSurvivor();
        var result = await data.Manual(type != ProjeTipi.Normal);
        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Same(survivor, Assert.Single(data.Uow.Repo<CekiSatiri>().Rows));
        var remaining = Assert.Single(data.Uow.Repo<SandikIcerik>().Rows);
        Assert.Equal(survivor.Id, remaining.CekiSatiriId);
        Assert.Equal((int)SandikDurum.Hazirlaniyor, data.Sandik.DurumId);
        Assert.Equal((int)SandikDurum.Bos, data.Uow.Repo<Sandik>().Rows.Single(x => x.Id == 21).DurumId);
        Assert.Equal((int)ProjeDurum.Hazirlaniyor, data.Proje.DurumId);
        Assert.Single(data.Hareket.Rows);
        Assert.Equal(1, data.Uow.SaveCount);
        Assert.Single(data.Uow.Repo<Ceki>().Rows); // Manuel silme çekiyi silmez.
    }

    [Theory]
    [InlineData(ProjeTipi.Saha)][InlineData(ProjeTipi.Yedek)]
    public async Task SahaYedekBaglantisizManuelIcerik_YalnizKendiIceriginiSiler(ProjeTipi type)
    {
        var data = new Fixture(type);
        data.Icerik.CekiSatiriId = null;
        Assert.True((await data.Manual(true)).IsSuccess);
        Assert.Empty(data.Uow.Repo<SandikIcerik>().Rows);
        Assert.Same(data.Satir, Assert.Single(data.Uow.Repo<CekiSatiri>().Rows));
        Assert.Equal((int)SandikDurum.Bos, data.Sandik.DurumId);
        Assert.Single(data.Hareket.Rows);
    }

    [Fact]
    public async Task ImportSatiri_ManuelSilmeYolundanSilinmez_CekiSilmeYoluAyridir()
    {
        var data = new Fixture(ProjeTipi.Normal);
        data.Satir.IsManuelEklenen = false;
        Assert.False((await data.Manual(false)).IsSuccess);
        Assert.Equal(0, data.Uow.SaveCount);
        var result = await data.CekiSil([10, 10, -1, 0]);
        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(1, result.Value!.SilinenSatirSayisi);
        Assert.Equal(1, result.Value.SilinenSandikSayisi);
        Assert.Equal(1, result.Value.SilinenCekiSayisi);
        Assert.Empty(data.Uow.Repo<CekiSatiri>().Rows);
        Assert.Empty(data.Uow.Repo<SandikIcerik>().Rows);
        Assert.Empty(data.Uow.Repo<Sandik>().Rows);
        Assert.Empty(data.Uow.Repo<Ceki>().Rows);
        Assert.Equal("10", Assert.Single(data.Uow.Repo<HareketGecmisi>().Rows).ReferansId);
    }

    [Theory]
    [InlineData("secimsiz")][InlineData("birSatirYok")][InlineData("cekiYok")]
    [InlineData("aktifSaha")][InlineData("kilit")][InlineData("disTransfer")][InlineData("hedefsizTransfer")]
    public async Task CekiSatirlariSil_TumSecimDogrulanmadanUygunSatiriBileSilmez(string reason)
    {
        var data = new Fixture(ProjeTipi.Normal);
        data.AddSurvivor();
        if (reason == "cekiYok") data.Uow.Repo<Ceki>().Rows.Clear();
        data.ApplyBlock(reason);
        if (reason is "disTransfer" or "hedefsizTransfer")
            data.Uow.Repo<ProjeTransfer>().Rows.Add(new() { Id = 50, KaynakCekiSatiriId = 10,
                HedefCekiSatiriId = reason == "disTransfer" ? 99 : null, DurumId = (int)ProjeTransferDurum.Aktif, Miktar = .0001m });
        var counts = data.Counts();
        var selected = reason == "secimsiz" ? new List<int> { 0, -1 } : new List<int> { 11, 10 };
        if (reason == "birSatirYok") selected.Add(999);
        var result = await data.CekiSil(selected);
        Assert.False(result.IsSuccess);
        if (reason is "birSatirYok" or "cekiYok") Assert.Equal(404, result.StatusCode);
        if (reason == "aktifSaha") Assert.Equal(409, result.StatusCode);
        data.AssertUnchanged(counts);
        Assert.Empty(data.Uow.Repo<HareketGecmisi>().Rows);
        Assert.All(data.Uow.Repo<ProjeTransfer>().Rows, x => Assert.Equal((int)ProjeTransferDurum.Aktif, x.DurumId));
    }

    [Fact]
    public async Task CekiSatirlariSil_AktifTransferinIkiUcuSecildiyseDisKaynakBlokajiYaratmaz()
    {
        var data = new Fixture(ProjeTipi.Normal);
        data.AddSurvivor();
        data.Uow.Repo<ProjeTransfer>().Rows.Add(new() { Id = 50, KaynakCekiSatiriId = 10, HedefCekiSatiriId = 11,
            DurumId = (int)ProjeTransferDurum.Aktif, Miktar = .0001m });
        var result = await data.CekiSil([10, 11]);
        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(2, result.Value!.SilinenSatirSayisi);
        Assert.Empty(data.Uow.Repo<ProjeTransfer>().Rows);
        Assert.Empty(data.Uow.Repo<CekiSatiri>().Rows);
        Assert.Equal(2, data.Uow.Repo<HareketGecmisi>().Rows.Count);
    }

    [Fact]
    public async Task CekiSatirlariSil_HedefSilininceDisKaynakVeStokHareketiBirKezGeriAlinir()
    {
        var data = new Fixture(ProjeTipi.Normal);
        var source = new CekiSatiri { Id = 99, CekiId = 199, IstenenAdet = 2m, GelenMiktar = 2m,
            ProjeGonderilen = .2501m, GridDurumuId = (int)GridDurum.TamGeldi, UcKDurumuId = (int)UcKDurum.TamGeldi };
        data.Uow.Repo<CekiSatiri>().Rows.Add(source);
        var transfer = new ProjeTransfer { Id = 50, KaynakCekiSatiriId = 99, HedefCekiSatiriId = 10,
            DurumId = (int)ProjeTransferDurum.Aktif, Miktar = .2501m };
        data.Uow.Repo<ProjeTransfer>().Rows.Add(transfer);
        var stok = new StokKaydi { Id = 60, Miktar = 1.0001m, DurumId = (int)StokDurum.Aktif };
        data.Uow.Repo<StokKaydi>().Rows.Add(stok);
        data.Uow.Repo<StokHareketi>().Rows.Add(new() { Id = 61, StokKaydiId = 60, CekiSatiriId = 10,
            Miktar = -.1234m, IslemTipiId = (int)IslemTipi.StoktanKarsilandi });
        var result = await data.CekiSil([10]);
        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(0m, source.ProjeGonderilen);
        Assert.Equal(2m, source.GelenMiktar);
        Assert.Equal(0m, source.KalanMiktar);
        Assert.Equal((int)UrunDurum.Tamamlandi, source.DurumId);
        Assert.Equal((int)ProjeTransferDurum.GeriAlindi, transfer.DurumId);
        Assert.Null(transfer.HedefCekiSatiriId);
        Assert.NotNull(transfer.IptalTarihi);
        Assert.Equal(1.1235m, stok.Miktar);
        Assert.Empty(data.Uow.Repo<StokHareketi>().Rows);
        Assert.Equal(1, result.Value!.PasifeAlinanTransferSayisi);
        Assert.Equal(1, result.Value.IadeEdilenStokHareketiSayisi);
        Assert.Equal(404, (await data.CekiSil([10])).StatusCode);
        Assert.Equal(1.1235m, stok.Miktar);
        Assert.Equal(1, data.Uow.SaveCount);
    }

    [Theory]
    [InlineData("baskaProje")][InlineData("sandikYok")][InlineData("projeYok")]
    [InlineData("oturum")][InlineData("bos")][InlineData("hazirlaniyor")]
    public async Task SandikSevkEt_ProjeSandikOturumVeKapaliDurumuDogrulamadanKayitUretmez(string reason)
    {
        var data = new Fixture(ProjeTipi.Yedek);
        if (reason == "sandikYok") data.Uow.Repo<Sandik>().Rows.Clear();
        if (reason == "projeYok") data.Uow.Repo<Proje>().Rows.Clear();
        if (reason == "bos") data.Sandik.DurumId = (int)SandikDurum.Bos;
        if (reason == "hazirlaniyor") data.Sandik.DurumId = (int)SandikDurum.Hazirlaniyor;
        var before = data.Sandik.DurumId;
        var result = await data.Sevk(reason == "baskaProje" ? 2 : 1, reason != "oturum");
        Assert.False(result.IsSuccess);
        Assert.Equal(before, data.Sandik.DurumId);
        Assert.Empty(data.Uow.Repo<Sevkiyat>().Rows);
        Assert.Empty(data.Uow.Repo<SevkiyatSandik>().Rows);
        Assert.Equal(0, data.Uow.SaveCount);
        Assert.Empty(data.Hareket.Rows);
    }

    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task SandikSevkEt_IlkSevkTarihiniYazarTekrarIstegiIkinciSevkiyatUretmez(bool correction)
    {
        var data = new Fixture(ProjeTipi.Yedek);
        Assert.True((await data.Sevk()).IsSuccess);
        var firstDate = data.Proje.GerceklesenSevkTarihi;
        Assert.NotNull(firstDate);
        Assert.Equal((int)SandikDurum.Kapandi, data.Sandik.SevkOncesiDurumId);
        data.Sandik.SevkiyatDuzeltmeAcikMi = correction;
        var result = await data.Sevk();
        Assert.False(result.IsSuccess);
        if (correction) Assert.Contains("Düzeltmeyi Tamamla", result.Error!.Message);
        Assert.Equal(firstDate, data.Proje.GerceklesenSevkTarihi);
        Assert.Single(data.Uow.Repo<Sevkiyat>().Rows);
        Assert.Single(data.Uow.Repo<SevkiyatSandik>().Rows);
        Assert.Single(data.Hareket.Rows);
        Assert.Equal(1, data.Uow.SaveCount);
    }

    [Fact]
    public async Task SandikSevkEt_MevcutSevkiyatBaginiVeIlkTarihiKorumakIcinYeniKayitUretmez()
    {
        var data = new Fixture(ProjeTipi.Yedek);
        var date = new DateTime(2026, 9, 1);
        data.Proje.GerceklesenSevkTarihi = date;
        data.Uow.Repo<Sevkiyat>().Rows.Add(new() { Id = 50, ProjeId = 1, SevkiyatNo = 1, SevkTarihi = date });
        data.Uow.Repo<SevkiyatSandik>().Rows.Add(new() { Id = 51, SevkiyatId = 50, SandikId = 20 });
        Assert.True((await data.Sevk()).IsSuccess);
        Assert.Equal(50, Assert.Single(data.Uow.Repo<Sevkiyat>().Rows).Id);
        Assert.Equal(51, Assert.Single(data.Uow.Repo<SevkiyatSandik>().Rows).Id);
        Assert.Equal(date, data.Proje.GerceklesenSevkTarihi);
        Assert.Equal((int)SandikDurum.Sevkedildi, data.Sandik.DurumId);
    }

    private sealed class Fixture
    {
        public OrtakMemoryUow Uow { get; } = new();
        public OrtakHareket Hareket { get; } = new();
        public SilmeKoruma Koruma { get; } = new();
        public Proje Proje { get; }
        public Sandik Sandik { get; } = new() { Id = 20, ProjeId = 1, SandikNo = "1", DurumId = (int)SandikDurum.Kapandi };
        public CekiSatiri Satir { get; }
        public SandikIcerik Icerik { get; } = new() { Id = 30, SandikId = 20, CekiSatiriId = 10, TahsisMiktari = 3m };
        public Fixture(ProjeTipi type)
        {
            Proje = new() { Id = 1, ProjeTipiId = (int)type, DurumId = (int)ProjeDurum.Tamamlandi };
            var ceki = new Ceki { Id = 100, ProjeId = 1 };
            Satir = new() { Id = 10, CekiId = 100, Ceki = ceki, BarkodNo = "AYNI", IsManuelEklenen = true, IstenenAdet = 3m,
                GridDurumuId = (int)GridDurum.Gelmedi, GridSevkDurumuId = (int)GridSevkDurum.SevkEdilmedi,
                UcKDurumuId = (int)UcKDurum.Bekliyor, UcKKarsilamaTipiId = (int)UcKDurum.Bekliyor };
            Uow.Repo<Proje>().Rows.Add(Proje); Uow.Repo<Ceki>().Rows.Add(ceki);
            Uow.Repo<CekiSatiri>().Rows.Add(Satir); Uow.Repo<Sandik>().Rows.Add(Sandik); Uow.Repo<SandikIcerik>().Rows.Add(Icerik);
        }
        public void ApplyBlock(string reason)
        {
            if (reason == "kaynak") Satir.KaynakCekiSatiriId = 999;
            if (reason == "aktifSaha") Koruma.BagliSatirlar.Add(10);
            if (reason == "kilit") Sandik.DurumId = (int)SandikDurum.Sevkedildi;
            if (reason == "islem") Satir.GelenMiktar = .0001m;
            if (reason == "satirYok") Uow.Repo<CekiSatiri>().Rows.Clear();
        }
        public void AddSecondAllocation(bool locked)
        {
            Uow.Repo<Sandik>().Rows.Add(new() { Id = 21, ProjeId = 1, SandikNo = "2", DurumId = locked ? (int)SandikDurum.Sevkedildi : (int)SandikDurum.Kapandi });
            Uow.Repo<SandikIcerik>().Rows.Add(new() { Id = 31, SandikId = 21, CekiSatiriId = 10, TahsisMiktari = 1m });
            Icerik.TahsisMiktari = 2m;
        }
        public CekiSatiri AddSurvivor()
        {
            var satir = new CekiSatiri { Id = 11, CekiId = 100, Ceki = Satir.Ceki, BarkodNo = "AYNI", IstenenAdet = .0001m };
            Uow.Repo<CekiSatiri>().Rows.Add(satir);
            Uow.Repo<SandikIcerik>().Rows.Add(new() { Id = 32, SandikId = 20, CekiSatiriId = 11, TahsisMiktari = .0001m });
            return satir;
        }
        public (int Satir, int Icerik, int Sandik, int Ceki) Counts() => (Uow.Repo<CekiSatiri>().Rows.Count, Uow.Repo<SandikIcerik>().Rows.Count, Uow.Repo<Sandik>().Rows.Count, Uow.Repo<Ceki>().Rows.Count);
        public void AssertUnchanged((int Satir, int Icerik, int Sandik, int Ceki) counts)
        { Assert.Equal(counts, Counts()); Assert.Equal(0, Uow.SaveCount); Assert.Empty(Hareket.Rows); }
        public Task<Result> Manual(bool content, int proje = 1) => new ManuelUrunSilCommandHandler(Uow, Hareket, new OrtakUser(), Koruma)
            .Handle(new() { ProjeId = proje, CekiSatiriId = content ? null : 10, SandikIcerikId = content ? 30 : null }, default);
        public Task<Result<CekiSatirlariSilDto>> CekiSil(List<int> ids) => new CekiSatirlariSilCommandHandler(Uow, new OrtakUser(), new DurumHesaplaService(), Koruma)
            .Handle(new() { CekiSatiriIds = ids }, default);
        public Task<Result> Sevk(int proje = 1, bool auth = true) => new SandikSevkEtCommandHandler(Uow, Hareket, new OrtakUser(UserId: auth ? 7 : null), new OrtakSaha(), new ReadQueries())
            .Handle(new() { ProjeId = proje, SandikId = 20 }, default);
    }

    private sealed class SilmeKoruma : ISahaAktarimSilmeKorumaService
    {
        public HashSet<int> BagliSatirlar { get; } = [];
        public Task<HashSet<int>> GetAktifAktarimBagliCekiSatiriIdsAsync(IEnumerable<int> ids, CancellationToken token = default) => Task.FromResult(ids.Where(BagliSatirlar.Contains).ToHashSet());
        public Task<HashSet<int>> GetAktifAktarimBagliSandikIdsAsync(IEnumerable<int> ids, CancellationToken token = default) => Task.FromResult(new HashSet<int>());
    }
    private sealed class ReadQueries : IReadQueryExecutor
    {
        public IQueryable<T> AsNoTracking<T>(IQueryable<T> query) where T : class => query;
        public Task<int> CountAsync<T>(IQueryable<T> query, CancellationToken token = default) => Task.FromResult(query.Count());
        public Task<List<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken token = default) => Task.FromResult(query.ToList());
    }
}
