using _3K.Application.Common;
using _3K.Application.Features.UcKIslemleri.Commands;
using _3K.Application.Features.UcKIslemleri.Validators;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Services;

namespace _3K.Application.Tests;

/// <summary>
/// GRID_3K_IS_KURALLARI.md 8. ve 9.A-D bölümlerindeki kaynak, transfer,
/// stok, fazla teslim, iade ve reset korunumlarını gerçek handler sınırında
/// doğrular. Test altyapısı yalnız bellektedir; veritabanı/servis bağlantısı yoktur.
/// </summary>
public sealed class UcKKaynakTransferStokIadeKatalogTests
{
    [Fact]
    public async Task OlmayanSatirVeyaBaskaSatiraAitSandikSecimi_ReddedilirVeYazmaYapilmaz()
    {
        using var kurgu = new Kurgu(2m, 2m);
        var olmayanSatir = await kurgu.Handler.Handle(new UcKDurumGuncelleCommand
        {
            ProjeId = kurgu.Proje.Id,
            CekiSatiriId = 999,
            KarsilamaTipiId = (int)UcKDurum.TamGeldi
        }, default);
        var yanlisIcerik = await kurgu.Handler.Handle(new UcKDurumGuncelleCommand
        {
            ProjeId = kurgu.Proje.Id,
            CekiSatiriId = kurgu.Satir.Id,
            SandikIcerikId = 999,
            KarsilamaTipiId = (int)UcKDurum.TedarikcidenGeldi,
            GelenAdet = 1m
        }, default);

        Assert.False(olmayanSatir.IsSuccess);
        Assert.Equal(404, olmayanSatir.StatusCode);
        Assert.False(yanlisIcerik.IsSuccess);
        Assert.Equal(0, kurgu.Uow.SaveCount);
    }

    [Fact]
    public async Task AktifSahaKaynagi_TekilUcKIsleminiMutasyonsuzReddeder()
    {
        using var kurgu = new Kurgu(2m, 2m);
        kurgu.Saha.AktifKaynaklar.Add(kurgu.Satir.Id);
        var onceki = Snapshot(kurgu.Satir);

        var sonuc = await kurgu.KarsilaAsync(UcKDurum.TedarikcidenGeldi, 1m);

        Assert.False(sonuc.IsSuccess);
        Assert.Contains("saha", sonuc.Error!.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, kurgu.Uow.SaveCount);
        Assert.Equal(onceki, Snapshot(kurgu.Satir));
    }

    [Fact]
    public async Task SevkEdilmisSandik_TekilUcKIsleminiMutasyonsuzReddeder()
    {
        using var kurgu = new Kurgu(2m, 2m);
        kurgu.Sandik.DurumId = (int)SandikDurum.Sevkedildi;
        var onceki = Snapshot(kurgu.Satir);

        var sonuc = await kurgu.KarsilaAsync(UcKDurum.TedarikcidenGeldi, 1m);

        Assert.False(sonuc.IsSuccess);
        Assert.Contains("sevk edilmiş", sonuc.Error!.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, kurgu.Uow.SaveCount);
        Assert.Equal(onceki, Snapshot(kurgu.Satir));
    }

    [Theory]
    [InlineData(GridDurum.Iptal)]
    [InlineData(GridDurum.GridKapandi)]
    public async Task TerminalGridDurumu_TekilUcKIsleminiMutasyonsuzReddeder(GridDurum durum)
    {
        using var kurgu = new Kurgu(2m, 2m);
        kurgu.Satir.GridDurumuId = (int)durum;
        var onceki = Snapshot(kurgu.Satir);

        var sonuc = await kurgu.KarsilaAsync(UcKDurum.TedarikcidenGeldi, 1m);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(0, kurgu.Uow.SaveCount);
        Assert.Equal(onceki, Snapshot(kurgu.Satir));
    }

    [Fact]
    public async Task KaliteTadilatta_TekilUcKIsleminiMutasyonsuzReddeder()
    {
        using var kurgu = new Kurgu(2m, 2m);
        kurgu.Satir.KaliteDurumId = 51;
        kurgu.Lookup.Ayarla<LookupKaliteDurum>(51, "Tadilatta");
        var onceki = Snapshot(kurgu.Satir);

        var sonuc = await kurgu.KarsilaAsync(UcKDurum.TedarikcidenGeldi, 1m);

        Assert.False(sonuc.IsSuccess);
        Assert.Contains("Tadilatta", sonuc.Error!.Message);
        Assert.Equal(0, kurgu.Uow.SaveCount);
        Assert.Equal(onceki, Snapshot(kurgu.Satir));
    }

    [Fact]
    public async Task TrafoSevkAktifPartisizFizikselIslemVeGridGelmediKaynakDisiIslemReddedilir()
    {
        using var trafo = new Kurgu(2m, 2m);
        trafo.Satir.GridDurumuId = (int)GridDurum.TrafoSevk;
        trafo.Satir.GridSevkMiktari = 0m;
        trafo.Satir.AktifGridSevkKarsilananMiktari = 0m;
        trafo.Satir.AktifGridSevkPartisiErkenSonuclandirildiMi = false;
        var trafoSonuc = await trafo.KarsilaAsync(UcKDurum.TamGeldi);

        using var gelmedi = new Kurgu(2m, 2m);
        gelmedi.Satir.GridDurumuId = (int)GridDurum.Gelmedi;
        var gelmediSonuc = await gelmedi.KarsilaAsync(UcKDurum.TamGeldi);

        Assert.False(trafoSonuc.IsSuccess);
        Assert.False(gelmediSonuc.IsSuccess);
        Assert.Equal(0, trafo.Uow.SaveCount);
        Assert.Equal(0, gelmedi.Uow.SaveCount);
    }

    [Theory]
    [InlineData(UcKDurum.ProjedenKarsilandi)]
    [InlineData(UcKDurum.StoktanKarsilandi)]
    [InlineData(UcKDurum.TedarikcidenGeldi)]
    public async Task GridTam_OtuzBesHedeftenOtuzIkiTeslimde_AlternatifKaynakKapaliKalirVeYazmaYapilmaz(
        UcKDurum kaynakTipi)
    {
        using var kurgu = new Kurgu(35m, 35m);
        kurgu.Satir.GridDurumuId = (int)GridDurum.TamGeldi;
        kurgu.Satir.GridGelenAdet = 35m;
        kurgu.Satir.GridSevkDurumuId = (int)GridSevkDurum.SevkEdildi;
        kurgu.Satir.GridSevkMiktari = 32m;
        kurgu.Satir.GelenMiktar = 32m;
        kurgu.Satir.AktifGridSevkKarsilananMiktari = 32m;
        kurgu.Satir.AktifGridSevkPartisiErkenSonuclandirildiMi = false;
        kurgu.Icerik.KonulanAdet = 32m;
        kurgu.Icerik.EksikAdet = 3m;
        kurgu.Icerik.AktifGridSevkKarsilananMiktari = 32m;

        var stok = kurgu.StokEkle("MALZEME", 10m);
        var oncekiSatir = Snapshot(kurgu.Satir);
        var oncekiIcerik = Snapshot(kurgu.Icerik);
        var sonuc = await kurgu.KarsilaAsync(
            kaynakTipi,
            1m,
            stokKaydiId: stok.Id,
            kaynakCekiSatiriId: 999,
            kaynakProjeNo: "PA-KAYNAK");

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(0, kurgu.Uow.SaveCount);
        Assert.Equal(oncekiSatir, Snapshot(kurgu.Satir));
        Assert.Equal(oncekiIcerik, Snapshot(kurgu.Icerik));
        Assert.Equal(10m, stok.Miktar);
        Assert.Empty(kurgu.Uow.Repo<StokHareketi>().Rows);
        Assert.Empty(kurgu.Uow.Repo<ProjeTransfer>().Rows);
    }

    [Fact]
    public async Task StoktanOndalikKarsilama_AdiNormalizeEder_BakiyeHareketTahsisBorcVeLokasyonuSenkronizeEder()
    {
        using var kurgu = new Kurgu(3.5m, 3.5m, "Türbin pompası");
        kurgu.EksikPartiKur(2.25m, 1.25m);
        var stok = kurgu.StokEkle("  TÜRBİN - - POMPASI ", 1.25m);

        var sonuc = await kurgu.KarsilaAsync(UcKDurum.StoktanKarsilandi, 1.25m, stokKaydiId: stok.Id);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(0m, stok.Miktar);
        Assert.Equal((int)StokDurum.Tukendi, stok.DurumId);
        Assert.Equal(1.25m, kurgu.Satir.StokKarsilanan);
        Assert.Equal(1.25m, kurgu.Satir.KarsilananMiktar);
        Assert.Equal(0m, kurgu.Satir.YenidenSevkGerekliAdet);
        Assert.Equal((int)GridSevkDurum.SevkEdildi, kurgu.Satir.GridSevkDurumuId);
        Assert.Equal(3.5m, kurgu.Icerik.KonulanAdet);
        Assert.Equal(1.25m, kurgu.Icerik.StokKarsilanan);
        Assert.Equal(0m, kurgu.Icerik.EksikAdet);
        Assert.Equal((int)DepoLokasyon.UcK, kurgu.Sandik.DepoLokasyonId);
        var hareket = Assert.Single(kurgu.Uow.Repo<StokHareketi>().Rows);
        Assert.Equal(1.25m, hareket.Miktar);
        Assert.Equal((int)IslemTipi.StoktanKarsilandi, hareket.IslemTipiId);
    }

    [Theory]
    [InlineData("BASKA MALZEME", 5, "eşleşmelidir")]
    [InlineData("MALZEME", 0.5, "yeterli miktar")]
    public async Task StokAdiVeyaBakiyeGecersizse_HedefStokVeHareketlerDegismez(
        string stokAdi,
        double bakiye,
        string hataParcasi)
    {
        using var kurgu = new Kurgu(3m, 3m, "MALZEME");
        kurgu.EksikPartiKur(2m, 1m);
        var stok = kurgu.StokEkle(stokAdi, (decimal)bakiye);
        var oncekiSatir = Snapshot(kurgu.Satir);
        var oncekiIcerik = Snapshot(kurgu.Icerik);

        var sonuc = await kurgu.KarsilaAsync(UcKDurum.StoktanKarsilandi, 1m, stokKaydiId: stok.Id);

        Assert.False(sonuc.IsSuccess);
        Assert.Contains(hataParcasi, sonuc.Error!.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, kurgu.Uow.SaveCount);
        Assert.Equal((decimal)bakiye, stok.Miktar);
        Assert.Equal(oncekiSatir, Snapshot(kurgu.Satir));
        Assert.Equal(oncekiIcerik, Snapshot(kurgu.Icerik));
        Assert.Empty(kurgu.Uow.Repo<StokHareketi>().Rows);
    }

    [Fact]
    public async Task TedarikcidenOndalikKarsilama_BorcuKapatir_StokHareketiUretmezVeLokasyonuAtar()
    {
        using var kurgu = new Kurgu(2.75m, 2.75m);
        kurgu.EksikPartiKur(1.5m, 1.25m);

        var sonuc = await kurgu.KarsilaAsync(UcKDurum.TedarikcidenGeldi, 1.25m);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(1.25m, kurgu.Satir.TedarikciKarsilanan);
        Assert.Equal(1.25m, kurgu.Satir.KarsilananMiktar);
        Assert.Equal(0m, kurgu.Satir.YenidenSevkGerekliAdet);
        Assert.Equal(2.75m, kurgu.Icerik.KonulanAdet);
        Assert.Equal(1.25m, kurgu.Icerik.TedarikciKarsilanan);
        Assert.Equal((int)DepoLokasyon.UcK, kurgu.Sandik.DepoLokasyonId);
        Assert.Empty(kurgu.Uow.Repo<StokHareketi>().Rows);
    }

    [Fact]
    public async Task ProjedenOndalikKarsilama_HedefDonorTransferDefteriVeTelafiBorcunuKorunumluGunceller()
    {
        using var kurgu = new Kurgu(5m, 5m, "Pompa", "PA-HEDEF");
        kurgu.EksikPartiKur(2m, 3m);
        var donor = kurgu.DonorEkle(
            projeNo: "PA-KAYNAK",
            istenen: 5m,
            gelen: 3.75m,
            projeKarsilanan: 0.5m,
            projeGonderilen: 0.25m,
            gridSevk: 4m);

        var sonuc = await kurgu.KarsilaAsync(
            UcKDurum.ProjedenKarsilandi,
            1.25m,
            kaynakCekiSatiriId: donor.Id,
            kaynakProjeNo: "PA-KAYNAK");

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(1.25m, kurgu.Satir.ProjeKarsilanan);
        Assert.Equal(1.25m, kurgu.Satir.KarsilananMiktar);
        Assert.Equal(3.25m, kurgu.Icerik.KonulanAdet);
        Assert.Equal(1.25m, kurgu.Icerik.ProjeKarsilanan);
        Assert.Equal(1.5m, donor.ProjeGonderilen);
        Assert.Equal(3.75m, donor.GelenMiktar);
        Assert.Equal(2.25m, donor.YenidenSevkGerekliAdet);
        Assert.Equal((int)GridSevkDurum.YenidenSevkGerekli, donor.GridSevkDurumuId);
        var transfer = Assert.Single(kurgu.Uow.Repo<ProjeTransfer>().Rows);
        Assert.Equal(donor.Id, transfer.KaynakCekiSatiriId);
        Assert.Equal(kurgu.Satir.Id, transfer.HedefCekiSatiriId);
        Assert.Equal(1.25m, transfer.Miktar);
        Assert.Equal((int)ProjeTransferDurum.Aktif, transfer.DurumId);
        Assert.Equal((int)ProjeTransferTipi.Karsilama, transfer.TransferTipiId);
    }

    [Fact]
    public async Task ProjedenKarsilamada_KendiProjeMetniReddedilirVeKaliciDegisiklikOlusmaz()
    {
        using var kurgu = new Kurgu(3m, 3m, "Pompa", "PA-AYNI");
        kurgu.EksikPartiKur(1m, 2m);
        var onceki = Snapshot(kurgu.Satir);

        var sonuc = await kurgu.KarsilaAsync(
            UcKDurum.ProjedenKarsilandi,
            1m,
            kaynakCekiSatiriId: 999,
            kaynakProjeNo: "PA-AYNI");

        Assert.False(sonuc.IsSuccess);
        Assert.Contains("kendi", sonuc.Error!.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, kurgu.Uow.SaveCount);
        Assert.Equal(onceki, Snapshot(kurgu.Satir));
        Assert.Empty(kurgu.Uow.Repo<ProjeTransfer>().Rows);
    }

    [Fact]
    public async Task EksikGeldi_OndalikAktifPartiyiBirKezSonuclandirir_ChildVeBorcSenkronKalir()
    {
        using var kurgu = new Kurgu(4m, 4m);
        kurgu.AcikPartiKur(3.5m);

        var sonuc = await kurgu.KarsilaAsync(UcKDurum.EksikGeldi, 1.25m);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(1.25m, kurgu.Satir.GelenMiktar);
        Assert.Equal(1.25m, kurgu.Satir.AktifGridSevkKarsilananMiktari);
        Assert.Equal(2.25m, kurgu.Satir.YenidenSevkGerekliAdet);
        Assert.True(kurgu.Satir.AktifGridSevkPartisiErkenSonuclandirildiMi);
        Assert.Equal(1.25m, kurgu.Icerik.KonulanAdet);
        Assert.Equal(1.25m, kurgu.Icerik.AktifGridSevkKarsilananMiktari);
        Assert.Equal(2.75m, kurgu.Icerik.EksikAdet);
    }

    [Fact]
    public async Task EksikGeldi_TahsisKadarGirildigindeReddedilirVeSayaclarDegismez()
    {
        using var kurgu = new Kurgu(4m, 4m);
        kurgu.AcikPartiKur(4m);
        var oncekiSatir = Snapshot(kurgu.Satir);
        var oncekiIcerik = Snapshot(kurgu.Icerik);

        var sonuc = await kurgu.KarsilaAsync(UcKDurum.EksikGeldi, 4m);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(0, kurgu.Uow.SaveCount);
        Assert.Equal(oncekiSatir, Snapshot(kurgu.Satir));
        Assert.Equal(oncekiIcerik, Snapshot(kurgu.Icerik));
    }

    [Fact]
    public async Task Gelmedi_AktifPartininTamaminiBorcaCevirirVeDepoLokasyonunuDegistirmez()
    {
        using var kurgu = new Kurgu(4m, 4m);
        kurgu.AcikPartiKur(2.5m);

        var sonuc = await kurgu.KarsilaAsync(UcKDurum.Gelmedi);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(0m, kurgu.Satir.GelenMiktar);
        Assert.Equal(0m, kurgu.Satir.AktifGridSevkKarsilananMiktari);
        Assert.Equal(2.5m, kurgu.Satir.YenidenSevkGerekliAdet);
        Assert.True(kurgu.Satir.AktifGridSevkPartisiErkenSonuclandirildiMi);
        Assert.Equal((int)GridSevkDurum.YenidenSevkGerekli, kurgu.Satir.GridSevkDurumuId);
        Assert.Equal((int)DepoLokasyon.Belirsiz, kurgu.Sandik.DepoLokasyonId);
    }

    [Fact]
    public async Task Gelmedi_AktifPartideKismiTeslimVarsaReddedilirVeDegisiklikYazilmaz()
    {
        using var kurgu = new Kurgu(4m, 4m);
        kurgu.AcikPartiKur(2.5m);
        kurgu.Satir.GelenMiktar = 0.5m;
        kurgu.Satir.AktifGridSevkKarsilananMiktari = 0.5m;
        kurgu.Icerik.KonulanAdet = 0.5m;
        kurgu.Icerik.AktifGridSevkKarsilananMiktari = 0.5m;
        var oncekiSatir = Snapshot(kurgu.Satir);
        var oncekiIcerik = Snapshot(kurgu.Icerik);

        var sonuc = await kurgu.KarsilaAsync(UcKDurum.Gelmedi);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(0, kurgu.Uow.SaveCount);
        Assert.Equal(oncekiSatir, Snapshot(kurgu.Satir));
        Assert.Equal(oncekiIcerik, Snapshot(kurgu.Icerik));
    }

    [Fact]
    public async Task FazlaGeldi_NormalKalanIleStokFazlasiniAyirirVeMenseiHareketiniKorur()
    {
        using var kurgu = new Kurgu(3m, 3m, "Fazla ürün", "PA-FAZLA");
        kurgu.Satir.GridDurumuId = (int)GridDurum.TamGeldi;
        kurgu.Satir.GridGelenAdet = 3m;
        kurgu.Satir.GridSevkDurumuId = (int)GridSevkDurum.SevkEdildi;
        kurgu.Satir.GridSevkMiktari = 2m;
        kurgu.Satir.GelenMiktar = 2m;
        kurgu.Satir.AktifGridSevkKarsilananMiktari = 1m;
        kurgu.Satir.AktifGridSevkPartisiErkenSonuclandirildiMi = false;
        kurgu.Icerik.KonulanAdet = 2m;
        kurgu.Icerik.EksikAdet = 1m;
        kurgu.Icerik.AktifGridSevkKarsilananMiktari = 1m;

        var sonuc = await kurgu.KarsilaAsync(UcKDurum.FazlaGeldi, 0.75m, stogaAktar: true);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(3m, kurgu.Satir.GelenMiktar);
        Assert.Equal(2m, kurgu.Satir.AktifGridSevkKarsilananMiktari);
        Assert.Equal((int)UcKDurum.TamGeldi, kurgu.Satir.UcKDurumuId);
        Assert.Equal((int)UcKDurum.FazlaGeldi, kurgu.Satir.UcKKarsilamaTipiId);
        Assert.Equal(3m, kurgu.Icerik.KonulanAdet);
        var stok = Assert.Single(kurgu.Uow.Repo<StokKaydi>().Rows);
        Assert.Equal("Fazla ürün", stok.MalzemeAdi);
        Assert.Equal("PA-FAZLA", stok.KaynakProje);
        Assert.Equal("Fazla teslim", stok.StokGirisNedeni);
        Assert.Equal(0.75m, stok.Miktar);
        var hareket = Assert.Single(kurgu.Uow.Repo<StokHareketi>().Rows);
        Assert.Equal(stok.Id, hareket.StokKaydiId);
        Assert.Equal(0.75m, hareket.Miktar);
        Assert.Equal((int)IslemTipi.FazlaTeslimStogaAktarildi, hareket.IslemTipiId);
    }

    [Fact]
    public async Task FazlaGeldi_StogaAktarSecilmezseHicbirSayacVeyaStokDegismez()
    {
        using var kurgu = new Kurgu(2m, 2m);
        kurgu.AcikPartiKur(2m);
        var oncekiSatir = Snapshot(kurgu.Satir);
        var oncekiIcerik = Snapshot(kurgu.Icerik);

        var sonuc = await kurgu.KarsilaAsync(UcKDurum.FazlaGeldi, 0.5m, stogaAktar: false);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(0, kurgu.Uow.SaveCount);
        Assert.Equal(oncekiSatir, Snapshot(kurgu.Satir));
        Assert.Equal(oncekiIcerik, Snapshot(kurgu.Icerik));
        Assert.Empty(kurgu.Uow.Repo<StokKaydi>().Rows);
        Assert.Empty(kurgu.Uow.Repo<StokHareketi>().Rows);
    }

    [Fact]
    public async Task GeriGonderildi_OndalikGridPayiniAzaltir_KaynakPaylariniKorurVeBorcAcar()
    {
        using var kurgu = new Kurgu(5m, 5m);
        kurgu.Satir.GridDurumuId = (int)GridDurum.TamGeldi;
        kurgu.Satir.GridGelenAdet = 5m;
        kurgu.Satir.GridSevkDurumuId = (int)GridSevkDurum.SevkEdildi;
        kurgu.Satir.GridSevkMiktari = 3.5m;
        kurgu.Satir.GelenMiktar = 3.5m;
        kurgu.Satir.StokKarsilanan = 0.5m;
        kurgu.Satir.ProjeKarsilanan = 0.25m;
        kurgu.Satir.TedarikciKarsilanan = 0.25m;
        kurgu.Satir.KarsilananMiktar = 1m;
        kurgu.Satir.AktifGridSevkKarsilananMiktari = 3.5m;
        kurgu.Satir.AktifGridSevkPartisiErkenSonuclandirildiMi = false;
        kurgu.Icerik.KonulanAdet = 4.5m;
        kurgu.Icerik.EksikAdet = 0.5m;
        kurgu.Icerik.StokKarsilanan = 0.5m;
        kurgu.Icerik.ProjeKarsilanan = 0.25m;
        kurgu.Icerik.TedarikciKarsilanan = 0.25m;
        kurgu.Icerik.AktifGridSevkKarsilananMiktari = 3.5m;

        var sonuc = await kurgu.KarsilaAsync(UcKDurum.GeriGonderildi, 0.75m, geriSebepId: 1);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(2.75m, kurgu.Satir.GelenMiktar);
        Assert.Equal(2.75m, kurgu.Satir.AktifGridSevkKarsilananMiktari);
        Assert.Equal(0.75m, kurgu.Satir.GeriGonderilenMiktar);
        Assert.Equal(0.75m, kurgu.Satir.YenidenSevkGerekliAdet);
        Assert.Equal(0.5m, kurgu.Satir.StokKarsilanan);
        Assert.Equal(0.25m, kurgu.Satir.ProjeKarsilanan);
        Assert.Equal(0.25m, kurgu.Satir.TedarikciKarsilanan);
        Assert.Equal(3.75m, kurgu.Icerik.KonulanAdet);
        Assert.Equal(2.75m, kurgu.Icerik.AktifGridSevkKarsilananMiktari);
        Assert.True(kurgu.Satir.AktifGridSevkPartisiErkenSonuclandirildiMi);
    }

    [Fact]
    public async Task HataliUrun_GuncelKomutTipiDegildir_ReddedilirVeDegisiklikYapilmaz()
    {
        using var kurgu = new Kurgu(2m, 2m);
        kurgu.AcikPartiKur(2m);
        var onceki = Snapshot(kurgu.Satir);

        var sonuc = await kurgu.KarsilaAsync(UcKDurum.HataliUrun, 1m);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(0, kurgu.Uow.SaveCount);
        Assert.Equal(onceki, Snapshot(kurgu.Satir));
    }

    [Fact]
    public void HataliUrun_LegacyDurumEnumDegeriniKorur()
    {
        Assert.Equal(13, (int)UcKDurum.HataliUrun);
    }

    [Fact]
    public async Task TamReset_GelenProjeTransferiniTersler_DonoruIadeEderVeKapaliSandigiAcar()
    {
        using var kurgu = new Kurgu(4m, 4m, "Pompa", "PA-HEDEF");
        kurgu.Satir.GridDurumuId = (int)GridDurum.Gelmedi;
        kurgu.Satir.UcKDurumuId = (int)UcKDurum.ProjedenKarsilandi;
        kurgu.Satir.UcKKarsilamaTipiId = (int)UcKDurum.ProjedenKarsilandi;
        kurgu.Satir.KarsilananMiktar = 1.25m;
        kurgu.Satir.ProjeKarsilanan = 1.25m;
        kurgu.Satir.KaliteDurumId = 7;
        kurgu.Satir.SurecDurumId = 8;
        kurgu.Icerik.KonulanAdet = 1.25m;
        kurgu.Icerik.ProjeKarsilanan = 1.25m;
        kurgu.Icerik.EksikAdet = 2.75m;
        kurgu.Sandik.DurumId = (int)SandikDurum.Kapandi;
        var donor = kurgu.DonorEkle("PA-KAYNAK", 4m, 2m, 0m, 1.25m, 2m);
        var transfer = new ProjeTransfer
        {
            Id = 700,
            KaynakProjeId = donor.Ceki.ProjeId,
            HedefProjeId = kurgu.Proje.Id,
            KaynakCekiSatiriId = donor.Id,
            HedefCekiSatiriId = kurgu.Satir.Id,
            BarkodNo = kurgu.Satir.BarkodNo,
            UrunAdi = kurgu.Satir.Aciklama,
            Miktar = 1.25m,
            DurumId = (int)ProjeTransferDurum.Aktif,
            KullaniciId = 7
        };
        kurgu.Uow.Repo<ProjeTransfer>().Rows.Add(transfer);

        var sonuc = await kurgu.SifirlaAsync();

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(0m, kurgu.Satir.KarsilananMiktar);
        Assert.Equal(0m, kurgu.Satir.ProjeKarsilanan);
        Assert.Equal((int)UcKDurum.Bekliyor, kurgu.Satir.UcKDurumuId);
        Assert.Null(kurgu.Satir.KaliteDurumId);
        Assert.Null(kurgu.Satir.SurecDurumId);
        Assert.Equal(0m, kurgu.Icerik.KonulanAdet);
        Assert.Equal(0m, kurgu.Icerik.ProjeKarsilanan);
        Assert.Equal(0m, donor.ProjeGonderilen);
        Assert.Equal((int)ProjeTransferDurum.GeriAlindi, transfer.DurumId);
        Assert.NotNull(transfer.IptalTarihi);
        Assert.Equal((int)SandikDurum.Hazirlaniyor, kurgu.Sandik.DurumId);
    }

    [Fact]
    public async Task AktifGidenTransferVarkenDonorReset_ReddedilirVeTumDegerlerKorunur()
    {
        using var kurgu = new Kurgu(4m, 4m);
        kurgu.Satir.GridDurumuId = (int)GridDurum.TamGeldi;
        kurgu.Satir.UcKDurumuId = (int)UcKDurum.TamGeldi;
        kurgu.Satir.UcKKarsilamaTipiId = (int)UcKDurum.TamGeldi;
        kurgu.Satir.GelenMiktar = 2m;
        kurgu.Satir.ProjeGonderilen = 1m;
        kurgu.Icerik.KonulanAdet = 1m;
        kurgu.Uow.Repo<ProjeTransfer>().Rows.Add(new ProjeTransfer
        {
            Id = 701,
            KaynakProjeId = kurgu.Proje.Id,
            HedefProjeId = 999,
            KaynakCekiSatiriId = kurgu.Satir.Id,
            HedefCekiSatiriId = 999,
            Miktar = 1m,
            DurumId = (int)ProjeTransferDurum.Aktif
        });
        var oncekiSatir = Snapshot(kurgu.Satir);
        var oncekiIcerik = Snapshot(kurgu.Icerik);

        var sonuc = await kurgu.SifirlaAsync();

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(0, kurgu.Uow.SaveCount);
        Assert.Equal(oncekiSatir, Snapshot(kurgu.Satir));
        Assert.Equal(oncekiIcerik, Snapshot(kurgu.Icerik));
    }

    [Theory]
    [InlineData("saha")]
    [InlineData("sevk")]
    [InlineData("iptal")]
    public async Task TekilReset_AktifSahaSevkKilidiVeyaGridIptaldeMutasyonsuzReddedilir(string blokaj)
    {
        using var kurgu = new Kurgu(2m, 2m);
        kurgu.Satir.UcKDurumuId = (int)UcKDurum.TamGeldi;
        kurgu.Satir.UcKKarsilamaTipiId = (int)UcKDurum.TamGeldi;
        kurgu.Satir.GelenMiktar = 1m;
        kurgu.Icerik.KonulanAdet = 1m;
        kurgu.Icerik.EksikAdet = 1m;

        if (blokaj == "saha")
            kurgu.Saha.AktifKaynaklar.Add(kurgu.Satir.Id);
        else if (blokaj == "sevk")
            kurgu.Sandik.DurumId = (int)SandikDurum.Sevkedildi;
        else
            kurgu.Satir.GridDurumuId = (int)GridDurum.Iptal;

        var oncekiSatir = Snapshot(kurgu.Satir);
        var oncekiIcerik = Snapshot(kurgu.Icerik);

        var sonuc = await kurgu.SifirlaAsync();

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(0, kurgu.Uow.SaveCount);
        Assert.Equal(oncekiSatir, Snapshot(kurgu.Satir));
        Assert.Equal(oncekiIcerik, Snapshot(kurgu.Icerik));
    }

    [Fact]
    public async Task TopluReset_SecimlerdenBiriAktifSahadaysaUygunSatiraDaDokunmadanTumIstegiReddeder()
    {
        using var kurgu = new Kurgu(2m, 2m);
        ResetIcinDoldur(kurgu.Satir, kurgu.Icerik);
        var ikinci = ResetSatiriEkle(kurgu, 24);
        kurgu.Saha.AktifKaynaklar.Add(ikinci.Satir.Id);
        var oncekiBirinci = Snapshot(kurgu.Satir);
        var oncekiIkinci = Snapshot(ikinci.Satir);

        var sonuc = await kurgu.TopluSifirlaAsync(
            new() { CekiSatiriId = kurgu.Satir.Id },
            new() { CekiSatiriId = ikinci.Satir.Id });

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(0, kurgu.Uow.SaveCount);
        Assert.Equal(oncekiBirinci, Snapshot(kurgu.Satir));
        Assert.Equal(oncekiIkinci, Snapshot(ikinci.Satir));
    }

    [Theory]
    [InlineData("sevk")]
    [InlineData("iptal")]
    [InlineData("baslangic")]
    [InlineData("gecersiz-child")]
    public async Task TopluReset_YalnizAtlananSecimlerVarsaBasarisizDonerVeKaliciMutasyonYapmaz(string neden)
    {
        using var kurgu = new Kurgu(2m, 2m);
        if (neden != "baslangic")
            ResetIcinDoldur(kurgu.Satir, kurgu.Icerik);

        if (neden == "sevk")
            kurgu.Sandik.DurumId = (int)SandikDurum.Sevkedildi;
        else if (neden == "iptal")
            kurgu.Satir.GridDurumuId = (int)GridDurum.Iptal;

        var secim = new UcKSandikSecimDto
        {
            CekiSatiriId = kurgu.Satir.Id,
            SandikIcerikId = neden == "gecersiz-child" ? 999 : null
        };
        var oncekiSatir = Snapshot(kurgu.Satir);
        var oncekiIcerik = Snapshot(kurgu.Icerik);

        var sonuc = await kurgu.TopluSifirlaAsync(secim);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(0, kurgu.Uow.SaveCount);
        Assert.Equal(oncekiSatir, Snapshot(kurgu.Satir));
        Assert.Equal(oncekiIcerik, Snapshot(kurgu.Icerik));
    }

    [Fact]
    public async Task TopluReset_StokVeyaProjeKaynakliChildSeciminiMutasyonsuzReddeder()
    {
        using var kurgu = new Kurgu(2m, 2m);
        ResetIcinDoldur(kurgu.Satir, kurgu.Icerik);
        kurgu.Satir.StokKarsilanan = 0.5m;
        kurgu.Satir.KarsilananMiktar = 0.5m;
        kurgu.Icerik.StokKarsilanan = 0.5m;
        var oncekiSatir = Snapshot(kurgu.Satir);
        var oncekiIcerik = Snapshot(kurgu.Icerik);

        var sonuc = await kurgu.TopluSifirlaAsync(
            new UcKSandikSecimDto { CekiSatiriId = kurgu.Satir.Id, SandikIcerikId = kurgu.Icerik.Id });

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(0, kurgu.Uow.SaveCount);
        Assert.Equal(oncekiSatir, Snapshot(kurgu.Satir));
        Assert.Equal(oncekiIcerik, Snapshot(kurgu.Icerik));
    }

    [Fact]
    public async Task TopluReset_BirSecimBasariliDigeriAtlanirsaBasariliDonerVeYalnizUygunSatiriSifirlar()
    {
        using var kurgu = new Kurgu(2m, 2m);
        ResetIcinDoldur(kurgu.Satir, kurgu.Icerik);
        var atlanacak = ResetSatiriEkle(kurgu, 24);
        atlanacak.Satir.GridDurumuId = (int)GridDurum.Iptal;
        var oncekiAtlanan = Snapshot(atlanacak.Satir);

        var sonuc = await kurgu.TopluSifirlaAsync(
            new() { CekiSatiriId = kurgu.Satir.Id },
            new() { CekiSatiriId = atlanacak.Satir.Id });

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(1, kurgu.Uow.SaveCount);
        Assert.Equal(0m, kurgu.Satir.GelenMiktar);
        Assert.Equal((int)UcKDurum.Bekliyor, kurgu.Satir.UcKDurumuId);
        Assert.Equal(oncekiAtlanan, Snapshot(atlanacak.Satir));
    }

    [Fact]
    public async Task SandikBazliReset_DigerUcKPayiKalirkenKaliteVeSureciKorur()
    {
        using var kurgu = new Kurgu(2m, 1m);
        kurgu.Satir.GridDurumuId = (int)GridDurum.TamGeldi;
        kurgu.Satir.GridGelenAdet = 2m;
        kurgu.Satir.GridSevkDurumuId = (int)GridSevkDurum.SevkEdildi;
        kurgu.Satir.GridSevkMiktari = 2m;
        kurgu.Satir.AktifGridSevkKarsilananMiktari = 2m;
        kurgu.Satir.AktifGridSevkPartisiErkenSonuclandirildiMi = false;
        kurgu.Satir.GelenMiktar = 2m;
        kurgu.Satir.UcKDurumuId = (int)UcKDurum.TamGeldi;
        kurgu.Satir.UcKKarsilamaTipiId = (int)UcKDurum.TamGeldi;
        kurgu.Satir.KaliteDurumId = 7;
        kurgu.Satir.SurecDurumId = 8;
        kurgu.Icerik.KonulanAdet = 1m;
        kurgu.Icerik.EksikAdet = 0m;
        kurgu.Icerik.AktifGridSevkKarsilananMiktari = 1m;
        IkinciIcerikEkle(kurgu, 41, 31);

        var sonuc = await kurgu.SifirlaAsync(kurgu.Icerik.Id);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(1m, kurgu.Satir.GelenMiktar);
        Assert.Equal(7, kurgu.Satir.KaliteDurumId);
        Assert.Equal(8, kurgu.Satir.SurecDurumId);
    }

    [Fact]
    public async Task StokHareketGeriAl_OndalikTuketimVeFazlaStoguKorunumluTersler()
    {
        using var uow = new OrtakMemoryUow();
        var tuketilen = new StokKaydi { Id = 1, MalzemeAdi = "A", Miktar = 0m, DurumId = (int)StokDurum.Tukendi };
        var fazla = new StokKaydi { Id = 2, MalzemeAdi = "B", Miktar = 0.75m, DurumId = (int)StokDurum.Aktif };
        uow.Repo<StokKaydi>().Rows.AddRange([tuketilen, fazla]);
        uow.Repo<StokHareketi>().Rows.AddRange([
            new StokHareketi { Id = 11, StokKaydiId = 1, CekiSatiriId = 23, Miktar = 1.25m, IslemTipiId = (int)IslemTipi.StoktanKarsilandi },
            new StokHareketi { Id = 12, StokKaydiId = 2, CekiSatiriId = 23, Miktar = 0.75m, IslemTipiId = (int)IslemTipi.FazlaTeslimStogaAktarildi }
        ]);

        var sonuc = await UcKStokHareketGeriAlHelper.GeriAlAsync(uow, 23);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(1.25m, tuketilen.Miktar);
        Assert.Equal((int)StokDurum.Aktif, tuketilen.DurumId);
        Assert.Equal(0m, fazla.Miktar);
        Assert.Equal((int)StokDurum.Tukendi, fazla.DurumId);
        Assert.Empty(uow.Repo<StokHareketi>().Rows);
    }

    [Fact]
    public async Task FazlaStokBaskaHareketteKullanildiysa_OnceTumGruplarDogrulanirVeHicbiriDegismez()
    {
        using var uow = new OrtakMemoryUow();
        var tuketilen = new StokKaydi { Id = 1, MalzemeAdi = "A", Miktar = 0m, DurumId = (int)StokDurum.Tukendi };
        var fazla = new StokKaydi { Id = 2, MalzemeAdi = "B", Miktar = 0.75m, DurumId = (int)StokDurum.Aktif };
        uow.Repo<StokKaydi>().Rows.AddRange([tuketilen, fazla]);
        uow.Repo<StokHareketi>().Rows.AddRange([
            new StokHareketi { Id = 11, StokKaydiId = 1, CekiSatiriId = 23, Miktar = 1.25m, IslemTipiId = (int)IslemTipi.StoktanKarsilandi },
            new StokHareketi { Id = 12, StokKaydiId = 2, CekiSatiriId = 23, Miktar = 0.75m, IslemTipiId = (int)IslemTipi.FazlaTeslimStogaAktarildi },
            new StokHareketi { Id = 13, StokKaydiId = 2, CekiSatiriId = 99, Miktar = 0.25m, IslemTipiId = (int)IslemTipi.StoktanKarsilandi }
        ]);

        var sonuc = await UcKStokHareketGeriAlHelper.GeriAlAsync(uow, 23);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(0m, tuketilen.Miktar);
        Assert.Equal(0.75m, fazla.Miktar);
        Assert.Equal(3, uow.Repo<StokHareketi>().Rows.Count);
        Assert.Equal(0, uow.Repo<StokKaydi>().UpdateCount);
    }

    [Fact]
    public async Task TopluTedarikci_AyniSandikSeciminiTekillestirirVeYalnizKalanKadarKarsilar()
    {
        using var kurgu = new Kurgu(3m, 3m);
        kurgu.EksikPartiKur(1m, 2m);
        var handler = new UcKTopluTedarikciCommandHandler(
            kurgu.Uow, kurgu.User, kurgu.Durum, kurgu.Hareket, kurgu.Saha);

        var sonuc = await handler.Handle(new UcKTopluTedarikciCommand
        {
            ProjeId = kurgu.Proje.Id,
            CekiSatiriIdler = [kurgu.Satir.Id],
            Secimler =
            [
                new() { CekiSatiriId = kurgu.Satir.Id, SandikIcerikId = kurgu.Icerik.Id },
                new() { CekiSatiriId = kurgu.Satir.Id, SandikIcerikId = kurgu.Icerik.Id }
            ],
            Aciklama = "Kalanı tedarikçiden tamamla"
        }, default);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(2m, kurgu.Satir.TedarikciKarsilanan);
        Assert.Equal(2m, kurgu.Satir.KarsilananMiktar);
        Assert.Equal(3m, kurgu.Icerik.KonulanAdet);
        Assert.Equal(2m, kurgu.Icerik.TedarikciKarsilanan);
        Assert.Single(kurgu.Hareket.Rows);
        Assert.Equal(1, kurgu.Uow.SaveCount);
    }

    [Fact]
    public void TopluSecimHelper_AcikSecimleriOnceliklendirirVeBilesikAnahtarlaTekillestirir()
    {
        var sonuc = UcKSandikSecimHelper.Olustur(
            [1, 2, 2],
            [
                new() { CekiSatiriId = 3, SandikIcerikId = 30 },
                new() { CekiSatiriId = 3, SandikIcerikId = 30 },
                new() { CekiSatiriId = 3, SandikIcerikId = 31 },
                new() { CekiSatiriId = 0, SandikIcerikId = 99 }
            ]);

        Assert.Equal(2, sonuc.Count);
        Assert.Contains(sonuc, x => x.CekiSatiriId == 3 && x.SandikIcerikId == 30);
        Assert.Contains(sonuc, x => x.CekiSatiriId == 3 && x.SandikIcerikId == 31);
        Assert.DoesNotContain(sonuc, x => x.CekiSatiriId is 1 or 2);
    }

    [Fact]
    public void MiktarValidatoru_DortOndaligiKabulEder_BesOndaligiReddeder()
    {
        var validator = new UcKDurumGuncelleCommandValidator();
        var gecerli = validator.Validate(new UcKDurumGuncelleCommand
        {
            CekiSatiriId = 1, ProjeId = 1, KarsilamaTipiId = (int)UcKDurum.EksikGeldi, GelenAdet = 0.1234m
        });
        var gecersiz = validator.Validate(new UcKDurumGuncelleCommand
        {
            CekiSatiriId = 1, ProjeId = 1, KarsilamaTipiId = (int)UcKDurum.EksikGeldi, GelenAdet = 0.12345m
        });

        Assert.True(gecerli.IsValid);
        Assert.Contains(gecersiz.Errors, x => x.PropertyName == nameof(UcKDurumGuncelleCommand.GelenAdet));
    }

    private static (decimal Gelen, decimal Karsilanan, decimal Stok, decimal Proje, decimal Gonderilen,
        decimal Tedarikci, decimal Iade, decimal Borc, decimal? Aktif, bool? Erken, int UcK, int Tip, int GridSevk) Snapshot(CekiSatiri s) =>
        (s.GelenMiktar, s.KarsilananMiktar, s.StokKarsilanan, s.ProjeKarsilanan, s.ProjeGonderilen,
         s.TedarikciKarsilanan, s.GeriGonderilenMiktar, s.YenidenSevkGerekliAdet,
         s.AktifGridSevkKarsilananMiktari, s.AktifGridSevkPartisiErkenSonuclandirildiMi,
         s.UcKDurumuId, s.UcKKarsilamaTipiId, s.GridSevkDurumuId);

    private static (decimal Konulan, decimal Eksik, decimal Stok, decimal Proje, decimal Tedarikci, decimal? Aktif) Snapshot(SandikIcerik i) =>
        (i.KonulanAdet, i.EksikAdet, i.StokKarsilanan, i.ProjeKarsilanan, i.TedarikciKarsilanan,
         i.AktifGridSevkKarsilananMiktari);

    private static void ResetIcinDoldur(CekiSatiri satir, SandikIcerik icerik)
    {
        satir.GridDurumuId = (int)GridDurum.TamGeldi;
        satir.GridGelenAdet = 2m;
        satir.GridSevkDurumuId = (int)GridSevkDurum.SevkEdildi;
        satir.GridSevkMiktari = 2m;
        satir.AktifGridSevkKarsilananMiktari = 1m;
        satir.AktifGridSevkPartisiErkenSonuclandirildiMi = false;
        satir.GelenMiktar = 1m;
        satir.UcKDurumuId = (int)UcKDurum.EksikGeldi;
        satir.UcKKarsilamaTipiId = (int)UcKDurum.EksikGeldi;
        icerik.KonulanAdet = 1m;
        icerik.EksikAdet = Math.Max(icerik.TahsisMiktari - 1m, 0m);
        icerik.AktifGridSevkKarsilananMiktari = 1m;
    }

    private static (CekiSatiri Satir, Sandik Sandik, SandikIcerik Icerik) ResetSatiriEkle(Kurgu kurgu, int satirId)
    {
        var satir = new CekiSatiri
        {
            Id = satirId,
            CekiId = kurgu.Ceki.Id,
            Ceki = kurgu.Ceki,
            SiraNo = satirId,
            BarkodNo = $"BC-{satirId}",
            Aciklama = "MALZEME",
            IstenenAdet = 2m,
            BirimId = (int)Birim.Adet,
            CekideGecenSandikNo = satirId.ToString(),
            FiiliSandikNo = satirId.ToString()
        };
        var sandik = new Sandik
        {
            Id = satirId + 100,
            ProjeId = kurgu.Proje.Id,
            Proje = kurgu.Proje,
            SandikNo = satirId.ToString(),
            DurumId = (int)SandikDurum.Hazirlaniyor,
            DepoLokasyonId = (int)DepoLokasyon.Belirsiz
        };
        var icerik = new SandikIcerik
        {
            Id = satirId + 200,
            CekiSatiriId = satir.Id,
            CekiSatiri = satir,
            SandikId = sandik.Id,
            Sandik = sandik,
            TahsisMiktari = 2m,
            BirimId = (int)Birim.Adet
        };
        ResetIcinDoldur(satir, icerik);
        kurgu.Ceki.CekiSatirlari.Add(satir);
        kurgu.Proje.Sandiklar.Add(sandik);
        satir.SandikIcerikleri.Add(icerik);
        sandik.SandikIcerikleri.Add(icerik);
        kurgu.Uow.Repo<CekiSatiri>().Rows.Add(satir);
        kurgu.Uow.Repo<Sandik>().Rows.Add(sandik);
        kurgu.Uow.Repo<SandikIcerik>().Rows.Add(icerik);
        return (satir, sandik, icerik);
    }

    private static SandikIcerik IkinciIcerikEkle(Kurgu kurgu, int icerikId, int sandikId)
    {
        var sandik = new Sandik
        {
            Id = sandikId,
            ProjeId = kurgu.Proje.Id,
            Proje = kurgu.Proje,
            SandikNo = "2",
            DurumId = (int)SandikDurum.Hazirlaniyor,
            DepoLokasyonId = (int)DepoLokasyon.Belirsiz
        };
        var icerik = new SandikIcerik
        {
            Id = icerikId,
            CekiSatiriId = kurgu.Satir.Id,
            CekiSatiri = kurgu.Satir,
            SandikId = sandik.Id,
            Sandik = sandik,
            TahsisMiktari = 1m,
            KonulanAdet = 1m,
            EksikAdet = 0m,
            AktifGridSevkKarsilananMiktari = 1m,
            BirimId = (int)Birim.Adet
        };
        kurgu.Proje.Sandiklar.Add(sandik);
        kurgu.Satir.SandikIcerikleri.Add(icerik);
        sandik.SandikIcerikleri.Add(icerik);
        kurgu.Uow.Repo<Sandik>().Rows.Add(sandik);
        kurgu.Uow.Repo<SandikIcerik>().Rows.Add(icerik);
        return icerik;
    }

    private sealed class Kurgu : IDisposable
    {
        public OrtakMemoryUow Uow { get; } = new();
        public OrtakUser User { get; } = new();
        public DurumHesaplaService Durum { get; } = new();
        public HareketStub Hareket { get; } = new();
        public LookupStub Lookup { get; } = new();
        public OrtakSaha Saha { get; } = new();
        public Proje Proje { get; }
        public Ceki Ceki { get; }
        public CekiSatiri Satir { get; }
        public Sandik Sandik { get; }
        public SandikIcerik Icerik { get; }
        public UcKDurumGuncelleCommandHandler Handler =>
            new(Uow, User, Durum, Hareket, Lookup, Saha);

        public Kurgu(decimal istenen, decimal tahsis, string urunAdi = "MALZEME", string projeNo = "PA-HEDEF")
        {
            Proje = new Proje { Id = 10, ProjeNo = projeNo, ProjeTipiId = (int)ProjeTipi.Normal };
            Ceki = new Ceki { Id = 20, ProjeId = Proje.Id, Proje = Proje };
            Satir = new CekiSatiri
            {
                Id = 23,
                CekiId = Ceki.Id,
                Ceki = Ceki,
                SiraNo = 1,
                BarkodNo = "BC-001",
                Aciklama = urunAdi,
                IstenenAdet = istenen,
                BirimId = (int)Birim.Adet,
                CekideGecenSandikNo = "1",
                FiiliSandikNo = "1",
                GridDurumuId = (int)GridDurum.EksikGeldi,
                GridGelenAdet = 0,
                GridSevkDurumuId = (int)GridSevkDurum.YenidenSevkGerekli,
                GridSevkMiktari = 0,
                AktifGridSevkKarsilananMiktari = 0,
                AktifGridSevkPartisiErkenSonuclandirildiMi = true,
                UcKDurumuId = (int)UcKDurum.Bekliyor,
                UcKKarsilamaTipiId = (int)UcKDurum.Bekliyor
            };
            Sandik = new Sandik
            {
                Id = 30,
                ProjeId = Proje.Id,
                Proje = Proje,
                SandikNo = "1",
                DurumId = (int)SandikDurum.Hazirlaniyor,
                DepoLokasyonId = (int)DepoLokasyon.Belirsiz
            };
            Icerik = new SandikIcerik
            {
                Id = 40,
                CekiSatiriId = Satir.Id,
                CekiSatiri = Satir,
                SandikId = Sandik.Id,
                Sandik = Sandik,
                TahsisMiktari = tahsis,
                EksikAdet = tahsis,
                AktifGridSevkKarsilananMiktari = 0,
                BirimId = (int)Birim.Adet
            };
            Proje.Cekiler.Add(Ceki);
            Proje.Sandiklar.Add(Sandik);
            Ceki.CekiSatirlari.Add(Satir);
            Satir.SandikIcerikleri.Add(Icerik);
            Sandik.SandikIcerikleri.Add(Icerik);
            Uow.Repo<Proje>().Rows.Add(Proje);
            Uow.Repo<Ceki>().Rows.Add(Ceki);
            Uow.Repo<CekiSatiri>().Rows.Add(Satir);
            Uow.Repo<Sandik>().Rows.Add(Sandik);
            Uow.Repo<SandikIcerik>().Rows.Add(Icerik);
        }

        public void AcikPartiKur(decimal sevk)
        {
            Satir.GridDurumuId = (int)GridDurum.TamGeldi;
            Satir.GridGelenAdet = Math.Max(Satir.GridGelenAdet, sevk);
            Satir.GridSevkDurumuId = (int)GridSevkDurum.SevkEdildi;
            Satir.GridSevkMiktari = sevk;
            Satir.AktifGridSevkKarsilananMiktari = 0;
            Satir.AktifGridSevkPartisiErkenSonuclandirildiMi = false;
            Icerik.AktifGridSevkKarsilananMiktari = 0;
        }

        public void EksikPartiKur(decimal fizikselGelen, decimal borc)
        {
            Satir.GridDurumuId = (int)GridDurum.EksikGeldi;
            Satir.GridGelenAdet = fizikselGelen;
            Satir.GridSevkDurumuId = (int)GridSevkDurum.YenidenSevkGerekli;
            Satir.GridSevkMiktari = fizikselGelen;
            Satir.GelenMiktar = fizikselGelen;
            Satir.AktifGridSevkKarsilananMiktari = fizikselGelen;
            Satir.AktifGridSevkPartisiErkenSonuclandirildiMi = true;
            Satir.YenidenSevkGerekliAdet = borc;
            Satir.UcKDurumuId = (int)UcKDurum.EksikGeldi;
            Satir.UcKKarsilamaTipiId = (int)UcKDurum.EksikGeldi;
            Icerik.KonulanAdet = fizikselGelen;
            Icerik.EksikAdet = Math.Max(Icerik.TahsisMiktari - fizikselGelen, 0);
            Icerik.AktifGridSevkKarsilananMiktari = fizikselGelen;
        }

        public StokKaydi StokEkle(string ad, decimal miktar)
        {
            var stok = new StokKaydi
            {
                Id = Uow.Repo<StokKaydi>().Rows.Count + 100,
                MalzemeKodu = "STK",
                MalzemeAdi = ad,
                Miktar = miktar,
                BirimId = (int)Birim.Adet,
                DurumId = (int)StokDurum.Aktif
            };
            Uow.Repo<StokKaydi>().Rows.Add(stok);
            return stok;
        }

        public CekiSatiri DonorEkle(
            string projeNo,
            decimal istenen,
            decimal gelen,
            decimal projeKarsilanan,
            decimal projeGonderilen,
            decimal gridSevk)
        {
            var proje = new Proje { Id = 11, ProjeNo = projeNo, ProjeTipiId = (int)ProjeTipi.Normal };
            var ceki = new Ceki { Id = 21, ProjeId = proje.Id, Proje = proje };
            var satir = new CekiSatiri
            {
                Id = 24,
                CekiId = ceki.Id,
                Ceki = ceki,
                SiraNo = 1,
                BarkodNo = Satir.BarkodNo,
                Aciklama = Satir.Aciklama,
                IstenenAdet = istenen,
                BirimId = (int)Birim.Adet,
                GridDurumuId = (int)GridDurum.TamGeldi,
                GridGelenAdet = Math.Max(gelen, gridSevk),
                GridSevkDurumuId = (int)GridSevkDurum.SevkEdildi,
                GridSevkMiktari = gridSevk,
                AktifGridSevkKarsilananMiktari = Math.Min(gelen, gridSevk),
                AktifGridSevkPartisiErkenSonuclandirildiMi = false,
                GelenMiktar = gelen,
                KarsilananMiktar = projeKarsilanan,
                ProjeKarsilanan = projeKarsilanan,
                ProjeGonderilen = projeGonderilen,
                UcKDurumuId = (int)UcKDurum.EksikGeldi,
                UcKKarsilamaTipiId = (int)UcKDurum.EksikGeldi
            };
            proje.Cekiler.Add(ceki);
            ceki.CekiSatirlari.Add(satir);
            Uow.Repo<Proje>().Rows.Add(proje);
            Uow.Repo<Ceki>().Rows.Add(ceki);
            Uow.Repo<CekiSatiri>().Rows.Add(satir);
            return satir;
        }

        public Task<Result> KarsilaAsync(
            UcKDurum tip,
            decimal? adet = null,
            int? stokKaydiId = null,
            int? kaynakCekiSatiriId = null,
            string? kaynakProjeNo = null,
            bool stogaAktar = false,
            int? geriSebepId = null) =>
            Handler.Handle(new UcKDurumGuncelleCommand
                {
                    ProjeId = Proje.Id,
                    CekiSatiriId = Satir.Id,
                    SandikIcerikId = Icerik.Id,
                    KarsilamaTipiId = (int)tip,
                    GelenAdet = adet,
                    StokKaydiId = stokKaydiId,
                    KaynakCekiSatiriId = kaynakCekiSatiriId,
                    KaynakHedefProjeNo = kaynakProjeNo,
                    MevcutProjeNo = Proje.ProjeNo,
                    StogaAktar = stogaAktar,
                    GeriGonderilmeSebebiId = geriSebepId,
                    Aciklama = "Katalog testi"
                }, default);

        public Task<Result> SifirlaAsync(int? sandikIcerikId = null) =>
            new UcKDurumSifirlaCommandHandler(Uow, User, Durum, Hareket, Saha)
                .Handle(new UcKDurumSifirlaCommand
                {
                    ProjeId = Proje.Id,
                    CekiSatiriId = Satir.Id,
                    SandikIcerikId = sandikIcerikId,
                    Aciklama = "Katalog reset testi"
                }, default);

        public Task<Result> TopluSifirlaAsync(params UcKSandikSecimDto[] secimler) =>
            new UcKTopluSifirlaCommandHandler(Uow, User, Durum, Hareket, Saha)
                .Handle(new UcKTopluSifirlaCommand
                {
                    ProjeId = Proje.Id,
                    Secimler = secimler.ToList(),
                    Aciklama = "Katalog toplu reset testi"
                }, default);

        public void Dispose() => Uow.Dispose();
    }

    private sealed class HareketStub : IHareketService
    {
        public List<HareketGecmisi> Rows { get; } = [];
        public Task HareketKaydetAsync(HareketGecmisi hareket) { Rows.Add(hareket); return Task.CompletedTask; }
        public Task<IEnumerable<HareketGecmisi>> GetProjeHareketleriAsync(int projeId) =>
            Task.FromResult<IEnumerable<HareketGecmisi>>(Rows.Where(x => x.ProjeId == projeId));
        public Task<IEnumerable<HareketGecmisi>> GetUrunHareketleriAsync(string referansTipi, string referansId) =>
            Task.FromResult<IEnumerable<HareketGecmisi>>(Rows.Where(x => x.ReferansTipi == referansTipi && x.ReferansId == referansId));
        public Task<(IEnumerable<HareketGecmisi> Items, int TotalCount)> GetPaginatedProjeHareketleriAsync(
            int projeId, string? searchTerm, int? islemTipiId, int pageNumber, int pageSize)
        {
            var rows = Rows.Where(x => x.ProjeId == projeId).ToList();
            return Task.FromResult<(IEnumerable<HareketGecmisi>, int)>((rows, rows.Count));
        }
    }

    private sealed class LookupStub : ILookupCacheService
    {
        private readonly Dictionary<(Type Type, int Id), string> _degerler = [];
        public void Ayarla<TLookup>(int id, string deger) where TLookup : LookupBase =>
            _degerler[(typeof(TLookup), id)] = deger;
        public string GetDeger<TLookup>(int id) where TLookup : LookupBase =>
            _degerler.GetValueOrDefault((typeof(TLookup), id), id.ToString());
        public Task WarmupAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task RefreshAsync<TLookup>(CancellationToken ct = default) where TLookup : LookupBase => Task.CompletedTask;
    }
}
