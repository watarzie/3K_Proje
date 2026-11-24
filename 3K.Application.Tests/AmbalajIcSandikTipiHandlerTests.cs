using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri;
using _3K.Application.Features.AmbalajIslemleri.Commands;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Tests;

/// <summary>
/// İç sandık tipi ve "İç Sandık Katlanır" kurallarının handler bağlantısı (bellek içi iş birimi): yalnız iç sandık,
/// pasif tip reddi ve bağlı pasif tipin korunması, tip ile kaydın cins uyumu, kullanılan tipte katlanır geçişinin ve
/// silmenin reddi. Ret durumunda kayıt eklenmez ve değişmez.
/// </summary>
public sealed class AmbalajIcSandikTipiHandlerTests
{
    private const int AhsapTip = 1, IcKatlanirTip = 2, PasifTip = 3;

    [Fact]
    public async Task Plan_kalemi_ic_sandik_disinda_ic_sandik_katlanir_secemez()
    {
        var f = new Fixture();

        var sonuc = await f.KalemHandler().Handle(Kalem(tur: 2, IcSandikKatlanirKurali.SandikTipiAdi, null), default);

        Assert.Equal((400, IcSandikKatlanirKurali.YalnizIcSandikMesaji), (sonuc.StatusCode, sonuc.Error!.Message));
        Assert.Empty(f.Kayitlar);
    }

    [Fact]
    public async Task Ozel_sandik_saha_turunde_ic_sandik_katlanir_secemez()
    {
        var f = new Fixture();

        var sonuc = await f.BagimsizHandler().Handle(new AmbalajBagimsizSandikKaydetCommand
        {
            Tur = 4, ProjeId = 11, Ad = "Saha", SandikTipi = IcSandikKatlanirKurali.SandikTipiAdi, Adet = 1,
            Boy = 500, En = 500, Yukseklik = 500, TalimatVeren = "Planlama"
        }, default);

        Assert.Equal((400, IcSandikKatlanirKurali.YalnizIcSandikMesaji), (sonuc.StatusCode, sonuc.Error!.Message));
        Assert.Empty(f.Kayitlar);
    }

    [Fact]
    public async Task Pasif_tip_yeni_kayitta_reddedilir_kayit_eklenmez()
    {
        var f = new Fixture();

        var sonuc = await f.KalemHandler().Handle(Kalem(tur: 3, "Ahşap Kapalı", PasifTip), default);

        Assert.Equal("Seçilen iç sandık tipi pasif; yeni kayıtta kullanılamaz.", sonuc.Error!.Message);
        Assert.Empty(f.Kayitlar);
    }

    [Fact]
    public async Task Bagli_oldugu_tip_sonradan_pasif_olan_kayit_duzenlenebilir()
    {
        var f = new Fixture();
        var kayit = f.IcKalem(PasifTip, AmbalajSandikCinsi.AhsapKapali);

        var sonuc = await f.KalemHandler().Handle(Kalem(tur: 3, "Ahşap Kapalı", PasifTip, kalemId: kayit.Id, adet: 2), default);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal((2, (int?)PasifTip), (kayit.Adet, kayit.IcSandikSablonId));
    }

    [Theory]
    [InlineData("İç Sandık Katlanır", AhsapTip, false)]
    [InlineData("Ahşap Kapalı", IcKatlanirTip, false)]
    [InlineData("İç Sandık Katlanır", IcKatlanirTip, true)]
    [InlineData("Ahşap Kapalı", AhsapTip, true)]
    public async Task Ic_sandik_katlanir_kaydi_ve_tipi_ayni_cinste_olmali(string sandikTipi, int tipId, bool gecerli)
    {
        var f = new Fixture();

        var sonuc = await f.KalemHandler().Handle(Kalem(tur: 3, sandikTipi, tipId), default);

        Assert.Equal(gecerli, sonuc.IsSuccess);
        if (!gecerli)
        {
            Assert.Equal(AmbalajPlanlamaYardimcisi.IcKatlanirTipUyumMesaji, sonuc.Error!.Message);
            Assert.Empty(f.Kayitlar);
        }
    }

    [Fact]
    public async Task Kullanilan_tip_ic_sandik_katlanira_cevrilemez_kullanilmayan_cevrilebilir()
    {
        var f = new Fixture();
        f.Uow.Repo<FinansIsKaydi>().Rows.Add(new FinansIsKaydi { Id = 50, IcSandikSablonId = AhsapTip });
        var tip = f.Tip(AhsapTip);
        AmbalajIcSandikSablonuGuncelleCommand Komut(int id) => new()
        {
            SablonId = id, Ad = $"Tip {id} v2", SandikTipi = IcSandikKatlanirKurali.SandikTipiAdi, Boy = 900, En = 700, Yukseklik = 500
        };

        var red = await f.GuncelleHandler().Handle(Komut(AhsapTip), default);
        Assert.Equal((409, AmbalajIcSandikSablonKullanimi.KatlanirGecisiMesaji), (red.StatusCode, red.Error!.Message));
        Assert.Equal((AmbalajSandikCinsi.AhsapKapali, "Ahşap tip"), (tip.SandikCinsi, tip.Ad));

        var bos = new AmbalajIcSandikSablonu { Id = 9, Ad = "Boş tip", SandikCinsi = AmbalajSandikCinsi.AhsapKapali, Boy = 1, En = 1, Yukseklik = 1 };
        f.Uow.Repo<AmbalajIcSandikSablonu>().Rows.Add(bos);
        var gecti = await f.GuncelleHandler().Handle(Komut(9), default);
        Assert.True(gecti.IsSuccess, gecti.Error?.Message);
        Assert.Equal(AmbalajSandikCinsi.IcKatlanir, bos.SandikCinsi);
    }

    [Fact]
    public async Task Yalniz_finans_isiyle_kullanilan_tip_silinmez_durumla_pasif_ve_yeniden_aktif_olur()
    {
        var f = new Fixture();
        f.Uow.Repo<FinansIsKaydi>().Rows.Add(new FinansIsKaydi { Id = 50, IcSandikSablonId = AhsapTip });

        var sil = await new AmbalajIcSandikSablonuSilCommandHandler(f.Uow).Handle(new() { SablonId = AhsapTip }, default);
        Assert.Equal((409, AmbalajIcSandikSablonKullanimi.KullanimdaMesaji), (sil.StatusCode, sil.Error!.Message));
        Assert.NotNull(f.Tip(AhsapTip));

        var pasif = await f.DurumHandler().Handle(new() { SablonId = AhsapTip, Aktif = false }, default);
        Assert.Equal((false, 1), (pasif.Value!.Aktif, pasif.Value.KullanimSayisi));
        var aktif = await f.DurumHandler().Handle(new() { SablonId = AhsapTip, Aktif = true }, default);
        Assert.True(aktif.Value!.Aktif);
        Assert.True(f.Tip(AhsapTip).Aktif);
    }

    private static AmbalajPlanKalemKaydetCommand Kalem(int tur, string sandikTipi, int? tipId, int? kalemId = null, int adet = 1) => new()
    {
        KalemId = kalemId, ProjeId = 10, Tur = tur, SandikNo = kalemId.HasValue ? "IC-001" : string.Empty,
        Ad = "İç sandık", SandikTipi = sandikTipi, IcSandikSablonId = tipId, Adet = adet,
        Boy = 900, En = 700, Yukseklik = 500, TalimatVeren = "Planlama"
    };

    private sealed class Fixture
    {
        public Fixture()
        {
            Uow.Repo<Proje>().Rows.Add(new Proje { Id = 10, ProjeNo = "PA-10", Musteri = "Müşteri", ProjeTipiId = (int)ProjeTipi.Normal });
            Uow.Repo<Proje>().Rows.Add(new Proje { Id = 11, ProjeNo = "PA-11-SAHA", Musteri = "Müşteri", ProjeTipiId = (int)ProjeTipi.Saha });
            Uow.Repo<AmbalajIcSandikSablonu>().Rows.AddRange(
                new AmbalajIcSandikSablonu { Id = AhsapTip, Ad = "Ahşap tip", SandikCinsi = AmbalajSandikCinsi.AhsapKapali, Boy = 900, En = 700, Yukseklik = 500 },
                new AmbalajIcSandikSablonu { Id = IcKatlanirTip, Ad = "Katlanır iç tip", SandikCinsi = AmbalajSandikCinsi.IcKatlanir, Boy = 900, En = 700, Yukseklik = 500 },
                new AmbalajIcSandikSablonu { Id = PasifTip, Ad = "Pasif tip", SandikCinsi = AmbalajSandikCinsi.AhsapKapali, Boy = 900, En = 700, Yukseklik = 500, Aktif = false });
            Denetim = new AmbalajKritikIslemDenetimi(Uow, Roller, User);
        }

        public OrtakMemoryUow Uow { get; } = new();
        public OrtakRoller Roller { get; } = OrtakRoller.TumIzinler();
        public OrtakUser User { get; } = new();
        public FinansAktarimi Finans { get; } = new();
        public AmbalajKritikIslemDenetimi Denetim { get; }
        public List<AmbalajUretimKaydi> Kayitlar => Uow.Repo<AmbalajUretimKaydi>().Rows;
        public AmbalajIcSandikSablonu Tip(int id) => Uow.Repo<AmbalajIcSandikSablonu>().Rows.Single(x => x.Id == id);

        public AmbalajPlanKalemKaydetCommandHandler KalemHandler() => new(Uow, User, Roller, Finans, Denetim);
        public AmbalajBagimsizSandikKaydetCommandHandler BagimsizHandler() => new(Uow, User, Roller, new SenkronYok(), Finans, Denetim);
        public AmbalajIcSandikSablonuGuncelleCommandHandler GuncelleHandler() => new(Uow, User, Roller);
        public AmbalajIcSandikSablonuDurumCommandHandler DurumHandler() => new(Uow, User, Roller);

        public AmbalajUretimKaydi IcKalem(int tipId, AmbalajSandikCinsi cinsi)
        {
            var kayit = new AmbalajUretimKaydi
            {
                Id = 1, ProjeId = 10, IsAkisKimligi = Guid.NewGuid(), Tur = AmbalajSandikTuru.Ic, IcSandikSablonId = tipId,
                KaynakModul = AmbalajKaynakModulu.Manuel, SandikNo = "IC-001", Ad = "İç sandık", SandikCinsi = cinsi,
                Adet = 1, Boy = 900, En = 700, Yukseklik = 500, AmbalajaDahil = true, TalimatVeren = "Planlama"
            };
            Kayitlar.Add(kayit);
            return kayit;
        }
    }

    private sealed class FinansAktarimi : IFinansUretimAktarimService
    {
        public Task<FinansSenkronizasyonSonucModel> UretimKayitlariniAktarAsync(
            IReadOnlyList<FinansUretimAktarimModel> modeller, CancellationToken cancellationToken) =>
            Task.FromResult(new FinansSenkronizasyonSonucModel(0, modeller.Count, 0));
    }

    /// <summary>Bu testlerde kaynak sandık seçilmez; senkron çağrılırsa test hatalıdır.</summary>
    private sealed class SenkronYok : IAmbalajKaynakSenkronizasyonService
    {
        public Task<Result<AmbalajSenkronizasyonSonucu>> SenkronizeEtAsync(
            int projeId, ICurrentUserService islemiYapan, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Senkron beklenmiyordu.");
    }
}
