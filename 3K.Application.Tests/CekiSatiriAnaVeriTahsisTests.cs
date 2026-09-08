using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using _3K.Application.Common;
using _3K.Application.Features.CekiIslemleri.Commands;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Services;

namespace _3K.Application.Tests;

public sealed class CekiSatiriAnaVeriTahsisTests
{
    [Fact]
    public async Task IlkMiktarDegisikligi_OrijinaliAyriSaklar_TahsisGuncelMiktariIzler()
    {
        using var kurgu = new Kurgu(1);

        var sonuc = await kurgu.CalistirAsync(3);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(1m, kurgu.Satir.OrijinalIstenenAdet);
        Assert.Equal(3, kurgu.Satir.IstenenAdet);
        Assert.Equal(3, kurgu.TekIcerik.TahsisMiktari);
        Assert.Equal(3, kurgu.TekIcerik.EksikAdet);
        Assert.Equal(3, kurgu.Satir.KalanMiktar);
        Assert.Equal(0, kurgu.TekIcerik.KonulanAdet);
    }

    [Fact]
    public async Task ArdArdaMiktarDegisikligi_IlkOrijinaliEzmez()
    {
        using var kurgu = new Kurgu(1);
        var ilk = await kurgu.CalistirAsync(3);

        var ikinci = await kurgu.CalistirAsync(5);

        Assert.True(ilk.IsSuccess, ilk.Error?.Message);
        Assert.True(ikinci.IsSuccess, ikinci.Error?.Message);
        Assert.Equal(1m, kurgu.Satir.OrijinalIstenenAdet);
        Assert.Equal(5, kurgu.Satir.IstenenAdet);
        Assert.Equal(5, kurgu.TekIcerik.TahsisMiktari);
        Assert.Equal(5, kurgu.TekIcerik.EksikAdet);
        Assert.Equal(0, kurgu.TekIcerik.KonulanAdet);
        Assert.Equal(2, kurgu.Uow.TransactionCount);
    }

    [Fact]
    public async Task MiktarIlkDegereGeriDonseDe_DegisiklikGecmisiSilinmez()
    {
        using var kurgu = new Kurgu(1);
        var fizikselOnce = kurgu.FizikselKayitlar();
        Assert.Null(kurgu.Satir.OrijinalIstenenAdet);

        var artis = await kurgu.CalistirAsync(3);
        var geriDonus = await kurgu.CalistirAsync(1);

        Assert.True(artis.IsSuccess, artis.Error?.Message);
        Assert.True(geriDonus.IsSuccess, geriDonus.Error?.Message);
        Assert.Equal(1m, kurgu.Satir.OrijinalIstenenAdet);
        Assert.Equal(1, kurgu.Satir.IstenenAdet);
        Assert.Equal(1, kurgu.TekIcerik.TahsisMiktari);
        Assert.Equal(1, kurgu.TekIcerik.EksikAdet);
        Assert.Equal(1, kurgu.Satir.KalanMiktar);
        Assert.Equal(1m, geriDonus.Value!.OrijinalIstenenAdet);
        Assert.Equal(fizikselOnce, kurgu.FizikselKayitlar());
        Assert.Equal(2, kurgu.Uow.TransactionCount);
    }

    [Fact]
    public async Task DahaOnceSaklananOrijinal_MiktarAzalsaDaKorunur()
    {
        using var kurgu = new Kurgu(5);
        kurgu.Satir.OrijinalIstenenAdet = 1.25m;

        var sonuc = await kurgu.CalistirAsync(3);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(1.25m, kurgu.Satir.OrijinalIstenenAdet);
        Assert.Equal(3, kurgu.Satir.IstenenAdet);
        Assert.Equal(3, kurgu.TekIcerik.TahsisMiktari);
    }

    [Fact]
    public async Task OndalikOrijinalMiktar_IlkDegisiklikteYuvarlanmaz()
    {
        using var kurgu = new Kurgu(1.2375m);

        var sonuc = await kurgu.CalistirAsync(3.8125m);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(1.2375m, kurgu.Satir.OrijinalIstenenAdet);
        Assert.Equal(3.8125m, kurgu.TekIcerik.TahsisMiktari);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AyniMiktarlaAnaVeriDuzenleme_OrijinalSnapshotOlusturmazVeyaDegistirmez(bool orijinalVar)
    {
        using var kurgu = new Kurgu(3, tahsis: 1);
        kurgu.Satir.OrijinalIstenenAdet = orijinalVar ? 2m : null;

        var sonuc = await kurgu.CalistirAsync(3);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(orijinalVar ? 2m : (decimal?)null, kurgu.Satir.OrijinalIstenenAdet);
        Assert.Equal(orijinalVar ? 2m : (decimal?)null, sonuc.Value!.OrijinalIstenenAdet);
        Assert.Equal("YENI ACIKLAMA", kurgu.Satir.Aciklama);
        Assert.Equal(3, kurgu.Satir.IstenenAdet);
        Assert.Equal(1, kurgu.TekIcerik.TahsisMiktari);
    }

    [Fact]
    public async Task ReddedilenMiktarDegisikligi_OrijinalSnapshotKaydetmez()
    {
        using var kurgu = new Kurgu(5, konulan: 3);

        var sonuc = await kurgu.CalistirAsync(2);

        Assert.False(sonuc.IsSuccess);
        Assert.Null(kurgu.Satir.OrijinalIstenenAdet);
        Assert.Equal(5, kurgu.Satir.IstenenAdet);
        Assert.Equal(0, kurgu.Uow.WriteCount);
        Assert.Equal(0, kurgu.Uow.SaveCount);
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 4)]
    public async Task TekTamTahsis_MiktarArtincaAnaMiktariTakipEder(int eski, int yeni)
    {
        using var kurgu = new Kurgu(eski);

        var sonuc = await kurgu.CalistirAsync(yeni);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(yeni, kurgu.Satir.IstenenAdet);
        Assert.Equal(yeni, kurgu.TekIcerik.TahsisMiktari);
        Assert.Equal(yeni, kurgu.TekIcerik.EksikAdet);
        Assert.Equal(0, kurgu.TekIcerik.KonulanAdet);
        Assert.Equal(yeni, kurgu.Satir.KalanMiktar);
        Assert.Equal(1, kurgu.Uow.TransactionCount);
        Assert.Equal(1, kurgu.Uow.SaveCount);
    }

    [Fact]
    public async Task OndalikMiktarArtisi_KesirKaybetmedenTahsisVeEksigiGunceller()
    {
        using var kurgu = new Kurgu(1.25m, konulan: 0.35m);

        var sonuc = await kurgu.CalistirAsync(2.625m, birim: Birim.Metre);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(2.625m, kurgu.Satir.IstenenAdet);
        Assert.Equal(2.625m, kurgu.TekIcerik.TahsisMiktari);
        Assert.Equal(2.275m, kurgu.TekIcerik.EksikAdet);
        Assert.Equal(0.35m, kurgu.TekIcerik.KonulanAdet);
        Assert.Equal((int)Birim.Metre, kurgu.Satir.BirimId);
        Assert.Equal((int)Birim.Metre, kurgu.TekIcerik.BirimId);
    }

    [Fact]
    public async Task MiktarArtisi_FizikselTeslimSevkVeKarsilamaKayitlariniDegistirmez()
    {
        using var kurgu = new Kurgu(10, konulan: 3);
        kurgu.Satir.GridGelenAdet = 5;
        kurgu.Satir.GridSevkMiktari = 4;
        kurgu.Satir.TrafoSevkAdet = 1;
        kurgu.Satir.GelenMiktar = 3;
        kurgu.Satir.StokKarsilanan = 0.5m;
        kurgu.Satir.ProjeKarsilanan = 0.75m;
        kurgu.Satir.TedarikciKarsilanan = 0.25m;
        kurgu.Satir.ProjeGonderilen = 0.5m;
        kurgu.TekIcerik.StokKarsilanan = 0.5m;
        kurgu.TekIcerik.ProjeKarsilanan = 0.75m;
        kurgu.TekIcerik.TedarikciKarsilanan = 0.25m;
        var once = kurgu.FizikselKayitlar();

        var sonuc = await kurgu.CalistirAsync(12);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(once, kurgu.FizikselKayitlar());
        Assert.Equal(12, kurgu.TekIcerik.TahsisMiktari);
        Assert.Equal(9, kurgu.TekIcerik.EksikAdet);
    }

    [Theory]
    [InlineData(3, 0)]
    [InlineData(4, 1)]
    public async Task TekTamTahsis_KonulanMiktaraKadarAzaltilabilir(int yeni, int eksik)
    {
        using var kurgu = new Kurgu(5, konulan: 3);

        var sonuc = await kurgu.CalistirAsync(yeni);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(yeni, kurgu.TekIcerik.TahsisMiktari);
        Assert.Equal(eksik, kurgu.TekIcerik.EksikAdet);
        Assert.Equal(3, kurgu.TekIcerik.KonulanAdet);
    }

    [Fact]
    public async Task KonulanMiktarinAltinaAzaltma_HicbirAlaniDegistirmedenReddedilir()
    {
        using var kurgu = new Kurgu(5, konulan: 3);
        var once = kurgu.TumKayitlar();

        var sonuc = await kurgu.CalistirAsync(2, sandikNo: "YENI", birim: Birim.Kg);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(once, kurgu.TumKayitlar());
        Assert.Equal(0, kurgu.Uow.SaveCount);
        Assert.Equal(0, kurgu.Uow.WriteCount);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task IslenmisMiktarinAltinaAzaltma_MevcutAltSinirKorunur(bool grid)
    {
        using var kurgu = new Kurgu(5);
        if (grid)
        {
            kurgu.Satir.GridGelenAdet = 2;
            kurgu.Satir.TrafoSevkAdet = 1;
        }
        else
        {
            kurgu.Satir.GelenMiktar = 2;
            kurgu.Satir.StokKarsilanan = 1;
        }
        var once = kurgu.TumKayitlar();

        var sonuc = await kurgu.CalistirAsync(2);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(once, kurgu.TumKayitlar());
        Assert.Equal(0, kurgu.Uow.SaveCount);
        Assert.Equal(0, kurgu.Uow.WriteCount);
    }

    [Theory]
    [InlineData(12)]
    [InlineData(5)]
    public async Task BilincliTekParcaliTahsis_AnaMiktarDegisseDeDagilimVeEksikKorunur(int yeni)
    {
        using var kurgu = new Kurgu(10, tahsis: 4, konulan: 1);
        kurgu.TekIcerik.EksikAdet = 2.5m;

        var sonuc = await kurgu.CalistirAsync(yeni);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(yeni, kurgu.Satir.IstenenAdet);
        Assert.Equal(4, kurgu.TekIcerik.TahsisMiktari);
        Assert.Equal(1, kurgu.TekIcerik.KonulanAdet);
        Assert.Equal(2.5m, kurgu.TekIcerik.EksikAdet);
    }

    [Fact]
    public async Task CokluTahsis_MiktarArtincaSandikDagilimiVeEksikKorunur()
    {
        using var kurgu = new Kurgu(10, tahsis: 4, konulan: 1);
        kurgu.IkinciTahsisEkle(6, 2);
        var once = kurgu.TahsisKayitlari();

        var sonuc = await kurgu.CalistirAsync(12);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(12, kurgu.Satir.IstenenAdet);
        Assert.Equal(once, kurgu.TahsisKayitlari());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ParcaliTahsisToplamininAltinaAzaltma_OtomatikDagilimYapmadanReddedilir(bool coklu)
    {
        using var kurgu = new Kurgu(10, tahsis: 4, konulan: 1);
        if (coklu) kurgu.IkinciTahsisEkle(3, 1);
        var once = kurgu.TumKayitlar();

        var sonuc = await kurgu.CalistirAsync(coklu ? 6 : 3);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(once, kurgu.TumKayitlar());
        Assert.Equal(0, kurgu.Uow.SaveCount);
        Assert.Equal(0, kurgu.Uow.WriteCount);
    }

    [Fact]
    public async Task CokluSandiktakiKonulanToplami_AzaltmadaDikkateAlinir()
    {
        using var kurgu = new Kurgu(10, tahsis: 2, konulan: 3);
        kurgu.IkinciTahsisEkle(2, 3);
        var once = kurgu.TumKayitlar();

        var sonuc = await kurgu.CalistirAsync(5);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(once, kurgu.TumKayitlar());
        Assert.Equal(0, kurgu.Uow.SaveCount);
        Assert.Equal(0, kurgu.Uow.WriteCount);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(2)]
    [InlineData(0)]
    public async Task MiktarDegismediyse_EskiTahsisVeyaEksikIcinOnarimVarsayilmaz(int tahsis)
    {
        using var kurgu = new Kurgu(4, tahsis: tahsis);
        kurgu.TekIcerik.EksikAdet = 0.5m;

        var sonuc = await kurgu.CalistirAsync(4);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal("YENI ACIKLAMA", kurgu.Satir.Aciklama);
        Assert.Equal(tahsis, kurgu.TekIcerik.TahsisMiktari);
        Assert.Equal(0.5m, kurgu.TekIcerik.EksikAdet);
    }

    [Fact]
    public async Task TekLegacySifirTahsis_MiktarDegisinceAnaMiktaraSenkronizeEdilir()
    {
        using var kurgu = new Kurgu(2, tahsis: 0, konulan: 0.5m);

        var sonuc = await kurgu.CalistirAsync(4);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(4, kurgu.TekIcerik.TahsisMiktari);
        Assert.Equal(3.5m, kurgu.TekIcerik.EksikAdet);
        Assert.Equal(0.5m, kurgu.TekIcerik.KonulanAdet);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MiktarBirimVeSandikBirlikteDegisince_TekTahsisAyniIslemdeTasinarakSenkronizeEdilir(bool yeniSandik)
    {
        using var kurgu = new Kurgu(1);
        if (!yeniSandik) kurgu.HedefSandikEkle();

        var sonuc = await kurgu.CalistirAsync(2, sandikNo: "2", birim: Birim.Set);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        var hedef = Assert.Single(kurgu.Uow.Repo<Sandik>().Rows, s => s.SandikNo == "2");
        Assert.True(hedef.Id > 0);
        Assert.Equal(hedef.Id, kurgu.TekIcerik.SandikId);
        Assert.Equal("2", kurgu.Satir.CekideGecenSandikNo);
        Assert.Equal("2", kurgu.Satir.FiiliSandikNo);
        Assert.Equal(2, kurgu.TekIcerik.TahsisMiktari);
        Assert.Equal(2, kurgu.TekIcerik.EksikAdet);
        Assert.Equal((int)Birim.Set, kurgu.TekIcerik.BirimId);
        Assert.Equal(1, kurgu.Uow.TransactionCount);
        Assert.InRange(kurgu.Uow.SaveCount, 1, 2);
    }

    [Fact]
    public async Task AktifSahaKaynakSatiri_MiktarArtisindaDaDegistirilemez()
    {
        using var kurgu = new Kurgu(1);
        kurgu.Saha.Aktif = true;
        var once = kurgu.TumKayitlar();

        var sonuc = await kurgu.CalistirAsync(2);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(SahaAktarimBlokajHelper.SandikMesaji, sonuc.Error!.Message);
        Assert.Equal(once, kurgu.TumKayitlar());
        Assert.Equal(0, kurgu.Uow.SaveCount);
        Assert.Equal(0, kurgu.Uow.WriteCount);
    }

    [Fact]
    public async Task SevkEdilmisKilitliSandik_MiktarArtisindaDaDegistirilemez()
    {
        using var kurgu = new Kurgu(1);
        kurgu.Uow.Repo<Sandik>().Rows[0].DurumId = (int)SandikDurum.Sevkedildi;
        var once = kurgu.TumKayitlar();

        var sonuc = await kurgu.CalistirAsync(2);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(SandikSevkKilidiHelper.UrunKilitliMesaji, sonuc.Error!.Message);
        Assert.Equal(once, kurgu.TumKayitlar());
        Assert.Equal(0, kurgu.Uow.SaveCount);
        Assert.Equal(0, kurgu.Uow.WriteCount);
    }

    [Fact]
    public async Task CokluTahsis_SandikNumarasiDegisinceTekSandigaToplanmaz()
    {
        using var kurgu = new Kurgu(10, tahsis: 4, konulan: 1);
        kurgu.IkinciTahsisEkle(6, 2);
        var once = kurgu.TumKayitlar();

        var sonuc = await kurgu.CalistirAsync(12, sandikNo: "YENI");

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(once, kurgu.TumKayitlar());
        Assert.Equal(0, kurgu.Uow.SaveCount);
        Assert.Equal(0, kurgu.Uow.WriteCount);
    }

    [Fact]
    public async Task SevkEdilmisKilitliHedefSandigaTasima_AnaMiktariDaDegistirmedenReddedilir()
    {
        using var kurgu = new Kurgu(1);
        kurgu.HedefSandikEkle();
        kurgu.Uow.Repo<Sandik>().Rows.Single(s => s.SandikNo == "2").DurumId = (int)SandikDurum.Sevkedildi;
        var once = kurgu.TumKayitlar();

        var sonuc = await kurgu.CalistirAsync(2, sandikNo: "2");

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(SandikSevkKilidiHelper.SandikKilitliMesaji, sonuc.Error!.Message);
        Assert.Equal(once, kurgu.TumKayitlar());
        Assert.Equal(0, kurgu.Uow.SaveCount);
        Assert.Equal(0, kurgu.Uow.WriteCount);
    }

    private sealed class Kurgu : IDisposable
    {
        public TestUnitOfWork Uow { get; } = new();
        public SahaStub Saha { get; } = new();
        public CekiSatiri Satir => Uow.Repo<CekiSatiri>().Rows.Single();
        public SandikIcerik TekIcerik => Assert.Single(Uow.Repo<SandikIcerik>().Rows);

        public Kurgu(decimal miktar, decimal? tahsis = null, decimal konulan = 0)
        {
            Uow.Repo<Proje>().Rows.Add(new Proje { Id = 10, ProjeTipiId = (int)ProjeTipi.Normal });
            Uow.Repo<Ceki>().Rows.Add(new Ceki { Id = 60, ProjeId = 10 });
            Uow.Repo<CekiSatiri>().Rows.Add(new CekiSatiri
            {
                Id = 50, CekiId = 60, SiraNo = 1, IstenenAdet = miktar,
                BarkodNo = "ESKI", Aciklama = "ESKI ACIKLAMA", OlcuResmiPozNo = "ESKI POZ",
                CekideGecenSandikNo = "1", FiiliSandikNo = "1", BirimId = (int)Birim.Adet
            });
            Uow.Repo<Sandik>().Rows.Add(new Sandik
            {
                Id = 20, ProjeId = 10, SandikNo = "1", DurumId = (int)SandikDurum.Hazirlaniyor
            });
            Uow.Repo<SandikIcerik>().Rows.Add(new SandikIcerik
            {
                Id = 70, CekiSatiriId = 50, SandikId = 20,
                TahsisMiktari = tahsis ?? miktar, KonulanAdet = konulan,
                EksikAdet = Math.Max((tahsis ?? miktar) - konulan, 0), BirimId = (int)Birim.Adet
            });
        }

        public void HedefSandikEkle() => Uow.Repo<Sandik>().Rows.Add(new Sandik
        {
            Id = 21, ProjeId = 10, SandikNo = "2", DurumId = (int)SandikDurum.Hazirlaniyor
        });

        public void IkinciTahsisEkle(decimal tahsis, decimal konulan)
        {
            HedefSandikEkle();
            Uow.Repo<SandikIcerik>().Rows.Add(new SandikIcerik
            {
                Id = 71, CekiSatiriId = 50, SandikId = 21, TahsisMiktari = tahsis,
                KonulanAdet = konulan, EksikAdet = Math.Max(tahsis - konulan, 0), BirimId = (int)Birim.Adet
            });
        }

        public Task<Result<CekiSatiriAnaVeriGuncelleDto>> CalistirAsync(decimal miktar, string sandikNo = "1", Birim birim = Birim.Adet) =>
            new CekiSatiriAnaVeriGuncelleCommandHandler(Uow, new DurumHesaplaService(), Saha).Handle(
                new CekiSatiriAnaVeriGuncelleCommand
                {
                    CekiSatiriId = 50, SiraNo = 2, BarkodNo = "YENI", Aciklama = "YENI ACIKLAMA",
                    OlcuResmiPozNo = "YENI POZ", IstenenAdet = miktar, BirimId = (int)birim, SandikNo = sandikNo
                }, default);

        public string TahsisKayitlari() => JsonSerializer.Serialize(Uow.Repo<SandikIcerik>().Rows);
        public string TumKayitlar() => JsonSerializer.Serialize(new
        {
            Satir, Sandiklar = Uow.Repo<Sandik>().Rows, Icerikler = Uow.Repo<SandikIcerik>().Rows
        });

        public string FizikselKayitlar() => JsonSerializer.Serialize(new
        {
            Satir.GridGelenAdet, Satir.GridSevkMiktari, Satir.TrafoSevkAdet, Satir.GelenMiktar,
            Satir.StokKarsilanan, Satir.ProjeKarsilanan, Satir.TedarikciKarsilanan, Satir.ProjeGonderilen,
            Satir.GridSevkTarihi, Satir.TeslimTarihi,
            Icerikler = Uow.Repo<SandikIcerik>().Rows.Select(i => new
            {
                i.KonulanAdet, i.StokKarsilanan, i.ProjeKarsilanan, i.TedarikciKarsilanan
            })
        });

        public void Dispose() => Uow.Dispose();
    }

    // Veritabanına bağlanmaz. FindAsync gerçek repository gibi detached kopyalar döndürür;
    // bu sayede aynı içerik için eski bir kopyanın sonraki Update ile düzeltmeyi ezmesi yakalanır.
    private sealed class TestUnitOfWork : IUnitOfWork
    {
        private readonly Dictionary<Type, object> _repos = new();
        public int SaveCount { get; private set; }
        public int WriteCount { get; private set; }
        public int TransactionCount { get; private set; }
        public bool HasActiveTransaction { get; private set; }

        public Repo<T> Repo<T>() where T : BaseEntity
        {
            if (!_repos.TryGetValue(typeof(T), out var repo))
                _repos.Add(typeof(T), repo = new Repo<T>(this));
            return (Repo<T>)repo;
        }

        public IGenericRepository<T> GetRepository<T>() where T : BaseEntity => Repo<T>();
        public void OkumaKontrolu() => Assert.True(HasActiveTransaction, "Veritabanı işlemleri aynı transaction içinde olmalı.");
        public void YazmaKontrolu() { OkumaKontrolu(); WriteCount++; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            OkumaKontrolu();
            SaveCount++;
            return Task.FromResult(1);
        }

        public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken token = default)
        {
            Assert.False(HasActiveTransaction);
            TransactionCount++;
            HasActiveTransaction = true;
            try { return await operation(token); }
            finally { HasActiveTransaction = false; }
        }

        public void RegisterAfterCommit(Func<CancellationToken, Task> callback) => throw new NotSupportedException();
        public void RegisterAfterRollback(Func<CancellationToken, Task> callback) => throw new NotSupportedException();
        public void Dispose() { }
    }

    private sealed class Repo<T>(TestUnitOfWork uow) : IGenericRepository<T> where T : BaseEntity
    {
        private static readonly MethodInfo CloneMethod = typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic)!;
        public List<T> Rows { get; } = [];
        public Task<T?> GetByIdAsync(int id)
        {
            uow.OkumaKontrolu();
            return Task.FromResult(Rows.SingleOrDefault(r => r.Id == id));
        }

        public IQueryable<T> Queryable() { uow.OkumaKontrolu(); return Rows.AsQueryable(); }
        public Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            uow.OkumaKontrolu();
            return Task.FromResult<IEnumerable<T>>(Rows.Where(predicate.Compile()).Select(r => (T)CloneMethod.Invoke(r, null)!).ToList());
        }

        public Task AddAsync(T entity)
        {
            uow.YazmaKontrolu();
            if (entity.Id == 0) entity.Id = Rows.Count == 0 ? 1 : Rows.Max(r => r.Id) + 1;
            Rows.Add(entity);
            return Task.CompletedTask;
        }

        public void Update(T entity)
        {
            uow.YazmaKontrolu();
            var index = Rows.FindIndex(r => r.Id == entity.Id);
            Assert.True(index >= 0);
            Rows[index] = entity;
        }

        public void Remove(T entity) => throw new NotSupportedException();
        public Task<IEnumerable<T>> GetAllAsync() => throw new NotSupportedException();
        public Task<IEnumerable<T>> GetAllWithIncludeAsync<TProp>(Expression<Func<T, TProp>> include) => throw new NotSupportedException();
    }

    private sealed class SahaStub : ISahaTamamlamaService
    {
        public bool Aktif { get; set; }
        public Task<bool> AktifTamamlamaVarMiAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult(Aktif);
        public Task<Dictionary<int, decimal>> GetSevkEdilenTamamlamaMapAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Dictionary<int, decimal>> GetAktifTamamlamaMapAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Dictionary<int, decimal>> GetAktifGerceklesenTamamlamaMapAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Dictionary<int, decimal>> GetAktifIsTamamlamaMapAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default) => GetAktifGerceklesenTamamlamaMapAsync(ids, cancellationToken);
        public Task<Dictionary<int, decimal>> GetSevkEdilenGerceklesenTamamlamaMapAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<HashSet<int>> GetAktifSandikBazliAktarimSatirIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<KaynakSandikSahaAktarimDurumu> GetKaynakSandikSahaAktarimDurumuAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SenkronizeKaynakProjelerAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SenkronizeKaynakProjelerBySahaSandikIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
