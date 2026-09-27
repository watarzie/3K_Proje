using _3K.Application.Features.AmbalajIslemleri;
using _3K.Application.Features.AmbalajIslemleri.Commands;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Tests;

/// <summary>Ambalaj kayıtlarındaki kritik değişikliklerin izin, ilk form ve gerekçe kapıları.</summary>
public sealed class AmbalajKritikIslemTests
{
    private static readonly string[] YazmaYok = [];

    [Theory]
    [InlineData(AmbalajUretimDurumu.Planlandi, AmbalajUretimDurumu.Uretimde, false, true, false)]
    [InlineData(AmbalajUretimDurumu.Uretimde, AmbalajUretimDurumu.Tamamlandi, true, false, false)]
    [InlineData(AmbalajUretimDurumu.Tamamlandi, AmbalajUretimDurumu.Uretimde, true, false, true)]
    [InlineData(AmbalajUretimDurumu.Uretimde, AmbalajUretimDurumu.Planlandi, true, false, true)]
    public void DurumGecisi_IlerlemeIlkFormIster_GeriAlmaVeYenidenAcmaKritiktir(
        AmbalajUretimDurumu mevcut, AmbalajUretimDurumu hedef, bool formuVar, bool ilkFormGerekli, bool kritik)
    {
        var gereksinim = Degerlendir(new(Kayit(mevcut), formuVar, false) { HedefDurum = hedef });

        Assert.Equal(ilkFormGerekli, gereksinim.IlkFormGerekli);
        Assert.Equal(kritik, gereksinim.Kritik);
        Assert.Equal(YetkiKodlari.Ambalaj.DurumDuzenle, gereksinim.Yetkiler[0]);
        Assert.Equal(hedef == AmbalajUretimDurumu.Tamamlandi, gereksinim.Yetkiler.Contains(YetkiKodlari.Ambalaj.UretimiTamamla));
        Assert.Equal(hedef < mevcut, gereksinim.Yetkiler.Contains(YetkiKodlari.Ambalaj.DurumuGeriAl));
        Assert.Equal(mevcut == AmbalajUretimDurumu.Tamamlandi, gereksinim.Yetkiler.Contains(YetkiKodlari.Ambalaj.UretimiYenidenAc));
    }

    [Fact]
    public void AyniDurum_HicbirKapiIstemez()
    {
        var gereksinim = Degerlendir(new(Kayit(AmbalajUretimDurumu.Uretimde), false, false) { HedefDurum = AmbalajUretimDurumu.Uretimde });

        Assert.Empty(gereksinim.Yetkiler);
        Assert.False(gereksinim.Kritik);
        Assert.False(gereksinim.IlkFormGerekli);
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    public void UretimSecimi_YalnizFormluKaynakKaydindaKritiktir(bool formuVar, bool kaynakli, bool kritik)
    {
        var kayit = Kayit(AmbalajUretimDurumu.Uretimde);
        kayit.KaynakKayitId = kaynakli ? 101 : null;

        var gereksinim = Degerlendir(new(kayit, formuVar, false) { HedefUretimSecimi = false });

        Assert.Equal(kritik, gereksinim.Kritik);
        Assert.Equal(kritik
            ? [YetkiKodlari.Ambalaj.FormSonrasiSecimDegistir, YetkiKodlari.Ambalaj.KritikVeriDuzenle]
            : YazmaYok, gereksinim.Yetkiler);
    }

    [Fact]
    public void Karar_FormluKaydiHaricTutmakFormSonrasiSecimVeKritikVeriIster()
    {
        var gereksinim = Degerlendir(new(Kayit(AmbalajUretimDurumu.Uretimde), true, false) { HedefAmbalajaDahil = false });

        Assert.True(gereksinim.Kritik);
        Assert.Equal(new[]
        {
            YetkiKodlari.Ambalaj.HaricTut,
            YetkiKodlari.Ambalaj.FormSonrasiSecimDegistir,
            YetkiKodlari.Ambalaj.KritikVeriDuzenle
        }, gereksinim.Yetkiler);
    }

    [Fact]
    public void Karar_TamamlanmisProjedeHaricKaydiDahilEtmekKritiktir()
    {
        var kayit = Kayit(AmbalajUretimDurumu.Planlandi);
        kayit.AmbalajaDahil = false;

        var gereksinim = Degerlendir(new(kayit, false, false) { HedefAmbalajaDahil = true, ProjeTamamlandi = true });

        Assert.True(gereksinim.Kritik);
        Assert.Equal(new[] { YetkiKodlari.Ambalaj.DahilEt, YetkiKodlari.Ambalaj.TamamlananProjeyeEkle }, gereksinim.Yetkiler);
    }

    [Theory]
    [InlineData(false, false, AmbalajUretimDurumu.Uretimde, false)]
    [InlineData(true, false, AmbalajUretimDurumu.Uretimde, true)]
    [InlineData(false, true, AmbalajUretimDurumu.Uretimde, true)]
    [InlineData(false, false, AmbalajUretimDurumu.Tamamlandi, true)]
    public void VeriDegisikligi_FormTamamlanmaVeyaAktifSiparisVarsaKritiktir(
        bool formuVar, bool finansaBagli, AmbalajUretimDurumu durum, bool kritik)
    {
        var gereksinim = Degerlendir(new(Kayit(durum), formuVar, finansaBagli) { VeriDegisiyor = true });

        Assert.Equal(kritik, gereksinim.Kritik);
        Assert.Equal(kritik ? [YetkiKodlari.Ambalaj.KritikVeriDuzenle] : YazmaYok, gereksinim.Yetkiler);
    }

    [Fact]
    public void Silme_HerZamanIptalIzniVeGerekceIster()
    {
        var gereksinim = Degerlendir(new(Kayit(AmbalajUretimDurumu.Planlandi), false, false) { Siliniyor = true });

        Assert.True(gereksinim.Kritik);
        Assert.Equal(new[] { YetkiKodlari.Ambalaj.Iptal }, gereksinim.Yetkiler);
    }

    [Theory]
    [InlineData(false, null, 403)]
    [InlineData(true, null, 400)]
    [InlineData(true, "Hatalı giriş", 200)]
    public async Task KalemSil_IptalIzniVeGerekceIster_KritikIslemiGecmiseYazar(bool iptalIzni, string? gerekce, int beklenen)
    {
        var f = new Fixture(iptalIzni ? OrtakRoller.TumIzinler() : OrtakRoller.Haric(YetkiKodlari.Ambalaj.Iptal));
        var kalem = f.ManuelKalem(1);

        var sonuc = await new AmbalajPlanKalemSilCommandHandler(f.Uow, f.User, f.Finans, f.Denetim)
            .Handle(new AmbalajPlanKalemSilCommand { KalemId = kalem.Id, Gerekce = gerekce }, default);

        Assert.Equal(beklenen, sonuc.StatusCode);
        Assert.Equal(beklenen == 200, kalem.IptalMi);
        if (beklenen == 200)
            Assert.Contains(f.Hareketler, h => h.AlanAdi == "YetkiliRevizyon" && h.AmbalajUretimKaydiId == kalem.Id &&
                h.YeniDeger == nameof(AmbalajPlanKalemSilCommand) && h.Aciklama == gerekce);
        else
        {
            Assert.Equal(0, f.Uow.SaveCount);
            Assert.Empty(f.Hareketler);
        }
    }

    [Theory]
    [InlineData(false, 403)]
    [InlineData(true, 200)]
    public async Task KalemGuncelle_FormluKaydinVerisiKritikVeriIzniIster(bool kritikIzin, int beklenen)
    {
        var f = new Fixture(kritikIzin ? OrtakRoller.TumIzinler() : OrtakRoller.Haric(YetkiKodlari.Ambalaj.KritikVeriDuzenle));
        var kalem = f.ManuelKalem(1);
        f.FormEkle(kalem);

        var sonuc = await new AmbalajPlanKalemKaydetCommandHandler(f.Uow, f.User, f.Roller, f.Finans, f.Denetim)
            .Handle(KalemKomutu(kalem.Id, adet: 5, gerekce: "Ölçü düzeltmesi"), default);

        Assert.Equal(beklenen, sonuc.StatusCode);
        Assert.Equal(beklenen == 200 ? 5 : 1, kalem.Adet);
        if (beklenen == 403)
            Assert.Contains(YetkiKodlari.Ambalaj.KritikVeriDuzenle, sonuc.Error!.Message);
        else
            Assert.Contains(f.Hareketler, h => h.AlanAdi == "YetkiliRevizyon" && h.Aciklama == "Ölçü düzeltmesi");
    }

    [Fact]
    public async Task KalemEkle_TamamlanmisProjeyeEklemeIzinGerekceVeGecmisIster()
    {
        var f = new Fixture(OrtakRoller.Haric(YetkiKodlari.Ambalaj.TamamlananProjeyeEkle));
        f.TamamlanmisKayit(1);

        var reddedildi = await new AmbalajPlanKalemKaydetCommandHandler(f.Uow, f.User, f.Roller, f.Finans, f.Denetim)
            .Handle(KalemKomutu(null, adet: 1, gerekce: "Ek sipariş"), default);
        Assert.Equal(403, reddedildi.StatusCode);

        var izinli = new Fixture(OrtakRoller.TumIzinler(), f.Uow);
        var eklendi = await new AmbalajPlanKalemKaydetCommandHandler(izinli.Uow, izinli.User, izinli.Roller, izinli.Finans, izinli.Denetim)
            .Handle(KalemKomutu(null, adet: 1, gerekce: "Ek sipariş"), default);

        Assert.True(eklendi.IsSuccess, eklendi.Error?.Message);
        Assert.Contains(izinli.Hareketler, h => h.AlanAdi == "YetkiliRevizyon" &&
            h.AmbalajUretimKaydiId == eklendi.Value!.Id && h.Aciklama == "Ek sipariş");
    }

    [Fact]
    public async Task Karar_TamamlanmisProjedeYeniKaynagiDahilEtmek_SenkronizeKaydiGecmiseYazar()
    {
        var f = new Fixture(OrtakRoller.TumIzinler());
        f.TamamlanmisKayit(1);
        f.Uow.Repo<Sandik>().Rows.Add(new Sandik
        {
            Id = 202, ProjeId = 10, SandikNo = "2", Ad = "Yeni radyatör", TipId = (int)SandikTipi.AhsapKapali,
            Boy = 2000, En = 1000, Yukseklik = 1200
        });
        var senkronizasyon = new AmbalajKaynakSenkronizasyonService(f.Uow, f.Finans);
        var handler = new AmbalajKarariKaydetCommandHandler(f.Uow, f.User, f.Roller, senkronizasyon, f.Finans, f.Denetim);

        var gerekcesiz = await handler.Handle(new AmbalajKarariKaydetCommand { SandikId = 202, AmbalajaDahilMi = true }, default);
        Assert.Equal(400, gerekcesiz.StatusCode);
        Assert.DoesNotContain(f.Uow.Repo<AmbalajUretimKaydi>().Rows, k => k.KaynakKayitId == 202);

        var sonuc = await handler.Handle(
            new AmbalajKarariKaydetCommand { SandikId = 202, AmbalajaDahilMi = true, Gerekce = "Sonradan eklendi" }, default);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        var kaynakKaydi = Assert.Single(f.Uow.Repo<AmbalajUretimKaydi>().Rows, k => k.KaynakKayitId == 202);
        Assert.True(kaynakKaydi.AmbalajaDahil);
        Assert.Contains(f.Hareketler, h => h.AlanAdi == "YetkiliRevizyon" &&
            h.AmbalajUretimKaydiId == kaynakKaydi.Id && h.YeniDeger == nameof(AmbalajKarariKaydetCommand));
    }

    private static AmbalajIslemGereksinimi Degerlendir(AmbalajKayitDegisikligi degisiklik)
    {
        var gereksinim = new AmbalajIslemGereksinimi();
        AmbalajIslemKurallari.Degerlendir(degisiklik, gereksinim);
        return gereksinim;
    }

    private static AmbalajUretimKaydi Kayit(AmbalajUretimDurumu durum) => new()
    {
        Id = 1, ProjeId = 10, IsAkisKimligi = Guid.NewGuid(), SandikNo = "1", AmbalajaDahil = true,
        UretimeAlindi = true, UretimDurumu = durum
    };

    private static AmbalajPlanKalemKaydetCommand KalemKomutu(int? kalemId, int adet, string gerekce) => new()
    {
        KalemId = kalemId, ProjeId = 10, Tur = 2, SandikNo = kalemId.HasValue ? "ILV-001" : string.Empty,
        Ad = "İlave sandık", SandikTipi = "Ahşap Kapalı", Adet = adet, Boy = 1200, En = 1000, Yukseklik = 900,
        TalimatVeren = "Planlama", Gerekce = gerekce
    };

    private sealed class Fixture
    {
        public Fixture(OrtakRoller roller, OrtakMemoryUow? uow = null)
        {
            Uow = uow ?? new OrtakMemoryUow();
            Roller = roller;
            if (Uow.Repo<Proje>().Rows.Count == 0)
                Uow.Repo<Proje>().Rows.Add(new Proje { Id = 10, ProjeNo = "PA-10", Musteri = "Müşteri", ProjeTipiId = (int)ProjeTipi.Normal });
            Denetim = new AmbalajKritikIslemDenetimi(Uow, Roller, User);
        }

        public OrtakMemoryUow Uow { get; }
        public OrtakRoller Roller { get; }
        public OrtakUser User { get; } = new();
        public FinansAktarimi Finans { get; } = new();
        public AmbalajKritikIslemDenetimi Denetim { get; }
        public List<AmbalajUretimHareketi> Hareketler => Uow.Repo<AmbalajUretimHareketi>().Rows;

        public AmbalajUretimKaydi ManuelKalem(int id)
        {
            var kayit = new AmbalajUretimKaydi
            {
                Id = id, ProjeId = 10, IsAkisKimligi = Guid.NewGuid(), Tur = AmbalajSandikTuru.Ilave,
                KaynakModul = AmbalajKaynakModulu.Manuel, SandikNo = "ILV-001", Ad = "İlave sandık",
                SandikCinsi = AmbalajSandikCinsi.AhsapKapali, Adet = 1, Boy = 1200, En = 1000, Yukseklik = 900,
                AmbalajaDahil = true, UretimeAlindi = true, UretimDurumu = AmbalajUretimDurumu.Uretimde,
                TalimatVeren = "Planlama"
            };
            AmbalajUretimYardimcilari.M3DegerleriniHesapla(kayit);
            Uow.Repo<AmbalajUretimKaydi>().Rows.Add(kayit);
            return kayit;
        }

        public void TamamlanmisKayit(int id)
        {
            var kayit = ManuelKalem(id);
            kayit.SandikNo = $"TAM-{id}";
            kayit.UretimDurumu = AmbalajUretimDurumu.Tamamlandi;
        }

        public void FormEkle(AmbalajUretimKaydi kayit) =>
            Uow.Repo<AmbalajUretimFormuKaydi>().Rows.Add(new AmbalajUretimFormuKaydi { FormSurumuId = 1, AmbalajUretimKaydiId = kayit.Id });
    }

    private sealed class FinansAktarimi : IFinansUretimAktarimService
    {
        public Task<FinansSenkronizasyonSonucModel> UretimKayitlariniAktarAsync(
            IReadOnlyList<FinansUretimAktarimModel> modeller, CancellationToken cancellationToken) =>
            Task.FromResult(new FinansSenkronizasyonSonucModel(0, modeller.Count, 0));
    }
}
