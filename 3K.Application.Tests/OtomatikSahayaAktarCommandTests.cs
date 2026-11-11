using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.ProjeIslemleri.Commands;
using _3K.Application.Features.ProjeIslemleri.DTOs;
using _3K.Application.Features.ProjeIslemleri.Queries;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Tests;

/// <summary>
/// Otomatik sahaya aktarımın önizle → uygula → geri al akışı (bellek içi; SQL/transaction kanıtı değildir):
/// elle akışın motorunu kullanır, tekrar istek kayıt üretmez, ret durumunda hiçbir kayıt değişmez.
/// </summary>
public class OtomatikSahayaAktarCommandTests
{
    private const int AnaProjeId = 1;
    private const int Sevkli2 = 12;
    private const int Sevksiz3 = 13;
    private const int Islenmis4 = 14;

    [Fact]
    public async Task Onizle_SonraUygula_SevksizSandikVeEksiklerSahayaGider()
    {
        var k = new Kurgu();

        var onizleme = await k.OnizleAsync();

        Assert.True(onizleme.AktarilacakVarMi);
        Assert.True(onizleme.YeniSahaProjesi);
        Assert.Equal("PA723-01-SAHA", onizleme.HedefSahaProjeNo);
        var sandik = Assert.Single(onizleme.Sandiklar);
        Assert.Equal("3", sandik.HedefSandikNo);
        Assert.NotNull(onizleme.EksiklerSandigi);
        Assert.Equal(SahaAktarimConstants.EksiklerSandikNo, onizleme.EksiklerSandigi!.SandikNo);
        // Elle "Eksiklerden oluştur" ile aynı: sevkli 2'nin eksiği ve sandık olarak gidemeyen (işlenmiş) sevksiz 4'ün eksiği.
        Assert.Equal([(Kurgu.R2, 4m), (Kurgu.R4, 3m)],
            onizleme.EksiklerSandigi.Urunler.Select(u => (u.CekiSatiriId, u.Miktar)).OrderBy(x => x.CekiSatiriId));
        var islenmis = Assert.Single(onizleme.Atlananlar, a => a.SandikNo == "4");
        Assert.Contains("ana projede kalır", islenmis.Neden);
        Assert.Contains("Eksik 1 ürünü", islenmis.Neden);
        Assert.NotEmpty(islenmis.Ayrintilar);
        Assert.DoesNotContain(onizleme.EksiklerSandigi.Urunler, u => u.CekiSatiriId == Kurgu.R1);

        var sonuc = await k.UygulaAsync(onizleme.PlanOzeti);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        var saha = Assert.Single(k.Uow.Repo<Proje>().Rows, p => p.ProjeTipiId == (int)ProjeTipi.Saha);
        Assert.Equal("PA723-01-SAHA", saha.ProjeNo);
        var sahaSandiklari = k.Uow.Repo<Sandik>().Rows.Where(s => s.ProjeId == saha.Id).ToList();
        var eksiklerSandigi = Assert.Single(sahaSandiklari, s => s.SandikNo == "1");
        Assert.Equal(SahaAktarimConstants.EksiklerSandikAdi, eksiklerSandigi.Ad);
        Assert.Single(sahaSandiklari, s => s.SandikNo == "3");

        var kalemler = k.Uow.Repo<SahaAktarimKalemi>().Rows;
        var sandikKalemi = Assert.Single(kalemler, x => x.KaynakSandikId == Sevksiz3);
        Assert.Equal((int)SahaAktarimTipi.SandikBazli, sandikKalemi.AktarimTipiId);
        Assert.Equal(5, sandikKalemi.Miktar);
        var eksikKalemi = Assert.Single(kalemler, x => x.KaynakSandikId == Sevkli2);
        Assert.Equal((int)SahaAktarimTipi.UrunBazli, eksikKalemi.AktarimTipiId);
        Assert.Equal(4, eksikKalemi.Miktar);
        var islenmisEksik = Assert.Single(kalemler, x => x.KaynakSandikId == Islenmis4);
        Assert.Equal(((int)SahaAktarimTipi.UrunBazli, 3m), (islenmisEksik.AktarimTipiId, islenmisEksik.Miktar));
        Assert.All(kalemler, x => Assert.Equal(sonuc.Value!.SahaAktarimId, x.SahaAktarimId));
        Assert.Contains(k.Hareket.Rows, h => h.ProjeId == AnaProjeId && h.Islem == "Otomatik Sahaya Aktarım");
    }

    [Fact]
    public async Task IkinciBasis_AktarilacakKalemYok_AyniIstekKayitUretmez()
    {
        var k = new Kurgu();
        var onizleme = await k.OnizleAsync();
        Assert.True((await k.UygulaAsync(onizleme.PlanOzeti)).IsSuccess);
        var satirSayisi = k.Uow.Repo<CekiSatiri>().Rows.Count;
        var kalemSayisi = k.Uow.Repo<SahaAktarimKalemi>().Rows.Count;

        var ikinciOnizleme = await k.OnizleAsync();
        var tekrar = await k.UygulaAsync(onizleme.PlanOzeti);

        Assert.False(ikinciOnizleme.AktarilacakVarMi);
        Assert.False(tekrar.IsSuccess);
        Assert.Equal(409, tekrar.StatusCode);
        Assert.Equal(satirSayisi, k.Uow.Repo<CekiSatiri>().Rows.Count);
        Assert.Equal(kalemSayisi, k.Uow.Repo<SahaAktarimKalemi>().Rows.Count);
        Assert.Single(k.Uow.Repo<SahaAktarim>().Rows);
    }

    [Fact]
    public async Task PlanOzetiFarkliysa_409VeHicbirKayitYazilmaz()
    {
        var k = new Kurgu();
        var once = k.Sayilar();

        var sonuc = await k.UygulaAsync(new string('0', 64));

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(409, sonuc.StatusCode);
        Assert.Equal(once, k.Sayilar());
        Assert.Empty(k.Hareket.Rows);
    }

    [Fact]
    public async Task MevcutSahaProjesi_SevkEdilmemisBirNumaraliSandigaEklenir()
    {
        var k = new Kurgu();
        k.Uow.Repo<Proje>().Rows.Add(new Proje
        {
            Id = 2,
            ProjeNo = "PA723-01-SAHA",
            ProjeTipiId = (int)ProjeTipi.Saha,
            DurumId = (int)ProjeDurum.Hazirlaniyor
        });
        k.Uow.Repo<Sandik>().Rows.Add(new Sandik { Id = 21, ProjeId = 2, SandikNo = "1", DurumId = (int)SandikDurum.Hazirlaniyor });

        var onizleme = await k.OnizleAsync();
        var sonuc = await k.UygulaAsync(onizleme.PlanOzeti);

        Assert.Equal(2, onizleme.HedefSahaProjeId);
        Assert.True(onizleme.EksiklerSandigi!.MevcutSandigaEklenir);
        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Single(k.Uow.Repo<Sandik>().Rows, s => s.ProjeId == 2 && SahaAktarimSandikKurali.BirNumaraliSandikMi(s.SandikNo));
        var eksikKalemi = Assert.Single(k.Uow.Repo<SahaAktarimKalemi>().Rows, x => x.KaynakSandikId == Sevkli2);
        Assert.Equal(21, eksikKalemi.SahaSandikId);
    }

    [Fact]
    public async Task MevcutKapanmisBirNumaraliSandik_EksikleriAlincaYenidenAcilir()
    {
        var k = new Kurgu();
        k.Uow.Repo<Proje>().Rows.Add(new Proje
        {
            Id = 2,
            ProjeNo = "PA723-01-SAHA",
            ProjeTipiId = (int)ProjeTipi.Saha,
            DurumId = (int)ProjeDurum.Hazirlaniyor
        });
        k.Uow.Repo<Sandik>().Rows.Add(new Sandik { Id = 21, ProjeId = 2, SandikNo = "1", DurumId = (int)SandikDurum.Kapandi });

        var onizleme = await k.OnizleAsync();
        var sonuc = await k.UygulaAsync(onizleme.PlanOzeti);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.True(onizleme.EksiklerSandigi!.MevcutSandigaEklenir);
        // Proje içi taşımayla aynı kural: içeriği değişen kapalı sandık yeniden kontrol edilmeden sevk edilemez.
        Assert.Equal((int)SandikDurum.Hazirlaniyor, k.Uow.Repo<Sandik>().Rows.Single(s => s.Id == 21).DurumId);
        Assert.Contains(k.Hareket.Rows, h => h.ProjeId == 2 && h.Islem == "Sandık Geri Açıldı" && h.ReferansId == "21");
    }

    [Fact]
    public async Task AktarimGeriAlinir_YenidenAktarimAyniPlaniVeBosSandiklariKullanir()
    {
        var k = new Kurgu();
        var ilkOnizleme = await k.OnizleAsync();
        var uygulama = await k.UygulaAsync(ilkOnizleme.PlanOzeti);
        var sahaId = uygulama.Value!.SahaProjeId;

        var geriAl = await k.GeriAlAsync(uygulama.Value.SahaAktarimId);

        Assert.True(geriAl.IsSuccess, geriAl.Error?.Message);
        Assert.Equal(3, geriAl.Value!.GeriAlinanSatirSayisi);
        Assert.All(k.Uow.Repo<SahaAktarimKalemi>().Rows, x =>
        {
            Assert.Equal((int)SahaAktarimDurum.GeriAlindi, x.DurumId);
            Assert.Null(x.SahaCekiSatiriId);
        });
        Assert.Equal(4, k.Uow.Repo<CekiSatiri>().Rows.Count);
        Assert.All(k.Uow.Repo<Sandik>().Rows.Where(s => s.ProjeId == sahaId), s => Assert.Equal((int)SandikDurum.Bos, s.DurumId));
        Assert.Contains(k.Saha.SenkronizeEdilen, id => id == Kurgu.R2);

        // Plan aynıdır (aynı sandık ve eksik miktarları); özet bu kez mevcut sahayı ve boş sandık eşleşmesini de içerir.
        var yeniOnizleme = await k.OnizleAsync();
        Assert.Equal(PlanMetni(ilkOnizleme), PlanMetni(yeniOnizleme));
        Assert.NotEqual(ilkOnizleme.PlanOzeti, yeniOnizleme.PlanOzeti);
        Assert.Equal(sahaId, yeniOnizleme.HedefSahaProjeId);
        Assert.True(yeniOnizleme.EksiklerSandigi!.MevcutSandigaEklenir);
        Assert.True(Assert.Single(yeniOnizleme.Sandiklar).MevcutSandigaEklenir);

        Assert.True((await k.UygulaAsync(yeniOnizleme.PlanOzeti, sahaId)).IsSuccess);
        Assert.Equal(2, k.Uow.Repo<Sandik>().Rows.Count(s => s.ProjeId == sahaId));
        Assert.All(k.Uow.Repo<Sandik>().Rows.Where(s => s.ProjeId == sahaId), s => Assert.Equal((int)SandikDurum.Hazirlaniyor, s.DurumId));
    }

    [Fact]
    public async Task GeriAlma_IslemGormusSatirVarsa_409VeHicbirKayitDegismez()
    {
        var k = new Kurgu();
        var onizleme = await k.OnizleAsync();
        var uygulama = await k.UygulaAsync(onizleme.PlanOzeti);
        var sahaSatiri = k.Uow.Repo<CekiSatiri>().Rows.Single(s => s.KaynakCekiSatiriId == Kurgu.R3);
        sahaSatiri.GelenMiktar = 1;
        var once = k.Sayilar();
        var hareketSayisi = k.Hareket.Rows.Count;

        var geriAl = await k.GeriAlAsync(uygulama.Value!.SahaAktarimId);

        Assert.False(geriAl.IsSuccess);
        Assert.Equal(409, geriAl.StatusCode);
        Assert.Equal(once, k.Sayilar());
        Assert.All(k.Uow.Repo<SahaAktarimKalemi>().Rows, x => Assert.Equal((int)SahaAktarimDurum.Planlandi, x.DurumId));
        Assert.Equal(hareketSayisi, k.Hareket.Rows.Count);
    }

    [Fact]
    public async Task ElleSandikAktarimi_BirNumaraliAnaSandigiReddeder()
    {
        var k = new Kurgu();
        k.Uow.Repo<Sandik>().Rows.Single(s => s.Id == 11).DurumId = (int)SandikDurum.Hazirlaniyor;
        var handler = new SandiklardanSahaProjesiOlusturCommandHandler(k.Uow, new KullanilmayanMediator(), k.Saha, k.SandikServisi);

        var sonuc = await handler.Handle(new SandiklardanSahaProjesiOlusturCommand
        {
            KaynakProjeId = AnaProjeId,
            SandikIds = [11]
        }, CancellationToken.None);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(409, sonuc.StatusCode);
        Assert.Contains("1 numaralı", sonuc.Error!.Message);
    }

    [Fact]
    public async Task AktarimMotoru_BirNumaraliSandigiSandikOlarakAktarmayiReddeder()
    {
        var k = new Kurgu();
        var once = k.Sayilar();
        var handler = new EksiklerdenSahaProjesiOlusturCommandHandler(k.Uow, k.Hareket, new LookupStub(), new OrtakUser(), k.Saha);

        var sonuc = await handler.Handle(new EksiklerdenSahaProjesiOlusturCommand
        {
            KaynakProjeId = AnaProjeId,
            Sandiklar =
            [
                new EksikSahaSandikDto
                {
                    SandikNo = "1",
                    Urunler =
                    [
                        new EksikSahaUrunDto
                        {
                            CekiSatiriId = Kurgu.R1,
                            KaynakSandikId = 11,
                            Miktar = 1,
                            Aciklama = $"{SahaAktarimConstants.SandikBazliAktarimAciklamaPrefix} 1"
                        }
                    ]
                }
            ]
        }, CancellationToken.None);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(409, sonuc.StatusCode);
        Assert.Equal(once, k.Sayilar());
    }

    [Fact]
    public async Task AktarimMotoru_SandikBazliUrunBildirilenSandiktaDegilse_409()
    {
        var k = new Kurgu();
        var once = k.Sayilar();
        var handler = new EksiklerdenSahaProjesiOlusturCommandHandler(k.Uow, k.Hareket, new LookupStub(), new OrtakUser(), k.Saha);

        // 1 numaralı sandıktaki ürün başka sandığın kimliğiyle sandık bazlı gönderilirse motor reddeder.
        var sonuc = await handler.Handle(new EksiklerdenSahaProjesiOlusturCommand
        {
            KaynakProjeId = AnaProjeId,
            Sandiklar =
            [
                new EksikSahaSandikDto
                {
                    SandikNo = "3",
                    Urunler =
                    [
                        new EksikSahaUrunDto
                        {
                            CekiSatiriId = Kurgu.R3,
                            KaynakSandikId = Islenmis4,
                            Miktar = 1,
                            Aciklama = $"{SahaAktarimConstants.SandikBazliAktarimAciklamaPrefix} 4"
                        }
                    ]
                }
            ]
        }, CancellationToken.None);

        Assert.Equal(409, sonuc.StatusCode);
        Assert.Equal(once, k.Sayilar());
    }

    [Fact]
    public async Task HedefteKaynakNumarasiSevkliSandiktaysa_SiradakiNumaraVerilir_EkliNumaraAcilmaz()
    {
        // 28.10.2026: eskiden "3-2" açılır, 3K sandık kartında ikinci bir "3" gibi görünürdü.
        var k = new Kurgu();
        k.Uow.Repo<Proje>().Rows.Add(new Proje { Id = 2, ProjeNo = "PA723-01-SAHA", ProjeTipiId = (int)ProjeTipi.Saha, DurumId = (int)ProjeDurum.Hazirlaniyor });
        k.Uow.Repo<Sandik>().Rows.AddRange(
        [
            new Sandik { Id = 21, ProjeId = 2, SandikNo = "1", DurumId = (int)SandikDurum.Hazirlaniyor },
            new Sandik { Id = 22, ProjeId = 2, SandikNo = "3", DurumId = (int)SandikDurum.Sevkedildi },
            new Sandik { Id = 23, ProjeId = 2, SandikNo = "07", DurumId = (int)SandikDurum.Sevkedildi }
        ]);

        var onizleme = await k.OnizleAsync();
        var sonuc = await k.UygulaAsync(onizleme.PlanOzeti);

        var sandik = Assert.Single(onizleme.Sandiklar);
        Assert.Equal(("8", true), (sandik.HedefSandikNo, sandik.NumaraDegisti));
        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Single(k.Uow.Repo<Sandik>().Rows, s => s.ProjeId == 2 && s.SandikNo == "8");
        Assert.DoesNotContain(k.Uow.Repo<Sandik>().Rows, s => s.ProjeId == 2 && s.SandikNo.Contains('-'));
    }

    [Theory]
    [InlineData("5")]
    [InlineData("05")]
    public async Task AktarimMotoru_VerilenNumaraHedefteVarsa_409_SandikAcilmaz(string verilen)
    {
        var k = new Kurgu();
        k.Uow.Repo<Proje>().Rows.Add(new Proje { Id = 2, ProjeNo = "PA723-01-SAHA", ProjeTipiId = (int)ProjeTipi.Saha, DurumId = (int)ProjeDurum.Hazirlaniyor });
        k.Uow.Repo<Sandik>().Rows.Add(new Sandik { Id = 21, ProjeId = 2, SandikNo = "5", DurumId = (int)SandikDurum.Hazirlaniyor });
        var once = k.Sayilar();
        var handler = new EksiklerdenSahaProjesiOlusturCommandHandler(k.Uow, k.Hareket, new LookupStub(), new OrtakUser(), k.Saha);

        // Elle aktarımda yeni sandığın numarası kullanıcınındır: dolu numara sessizce "5-2" olmaz, açık 409 döner.
        var sonuc = await handler.Handle(new EksiklerdenSahaProjesiOlusturCommand
        {
            KaynakProjeId = AnaProjeId,
            HedefSahaProjeId = 2,
            Sandiklar =
            [
                new EksikSahaSandikDto
                {
                    SandikNo = verilen,
                    Urunler = [new EksikSahaUrunDto { CekiSatiriId = Kurgu.R2, KaynakSandikId = Sevkli2, Miktar = 1 }]
                }
            ]
        }, CancellationToken.None);

        Assert.Equal(409, sonuc.StatusCode);
        Assert.Contains("numaralı sandık zaten var", sonuc.Error!.Message);
        Assert.Equal(once.Sandik, k.Sayilar().Sandik);
        Assert.Equal(once.Kalem, k.Sayilar().Kalem);
    }

    [Fact]
    public async Task OnizlemedenSonraHedeftekiBirNumaraliSandikSevkEdilirse_409()
    {
        var k = new Kurgu();
        k.Uow.Repo<Proje>().Rows.Add(new Proje { Id = 2, ProjeNo = "PA723-01-SAHA", ProjeTipiId = (int)ProjeTipi.Saha, DurumId = (int)ProjeDurum.Hazirlaniyor });
        k.Uow.Repo<Sandik>().Rows.Add(new Sandik { Id = 21, ProjeId = 2, SandikNo = "1", DurumId = (int)SandikDurum.Hazirlaniyor });
        var onizleme = await k.OnizleAsync();
        k.Uow.Repo<Sandik>().Rows.Single(s => s.Id == 21).DurumId = (int)SandikDurum.Sevkedildi;
        var once = k.Sayilar();

        var sonuc = await k.UygulaAsync(onizleme.PlanOzeti);

        Assert.Equal(409, sonuc.StatusCode);
        Assert.Equal(once, k.Sayilar());
    }

    private static string PlanMetni(OtomatikSahaAktarimOnizlemeDto o) =>
        string.Join(";", o.Sandiklar.Select(s => $"S{s.KaynakSandikId}:{s.ToplamAdet}")) + "|" +
        string.Join(";", (o.EksiklerSandigi?.Urunler ?? []).Select(u => $"E{u.CekiSatiriId}:{u.Miktar}"));

    private sealed class Kurgu
    {
        public const int R1 = 101;
        public const int R2 = 102;
        public const int R3 = 103;
        public const int R4 = 104;

        public OrtakMemoryUow Uow { get; } = new();
        public OrtakHareket Hareket { get; } = new();
        public SahaServisi Saha { get; }
        public SandikServisi SandikServisi { get; }

        public Kurgu()
        {
            Saha = new SahaServisi(Uow);
            SandikServisi = new SandikServisi(Uow);
            Uow.Repo<Proje>().Rows.Add(new Proje
            {
                Id = AnaProjeId,
                ProjeNo = "PA723-01",
                Musteri = "Müşteri",
                ProjeTipiId = (int)ProjeTipi.Normal,
                DurumId = (int)ProjeDurum.EksikSevkEdildi
            });
            Uow.Repo<Ceki>().Rows.Add(new Ceki { Id = 1, ProjeId = AnaProjeId, CekiTipiId = (int)CekiTipi.Normal });

            // 1: transformatör (sevkli, tam), 2: sevkli ve 4 eksik, 3: sevk edilmemiş temiz, 4: sevk edilmemiş işlenmiş.
            Sandik(11, "1", sevkli: true);
            Sandik(Sevkli2, "2", sevkli: true);
            Sandik(Sevksiz3, "3", sevkli: false);
            Sandik(Islenmis4, "4", sevkli: false);
            Satir(R1, 11, istenen: 1, gelen: 1, konulan: 1);
            Satir(R2, Sevkli2, istenen: 10, gelen: 6, konulan: 6);
            Satir(R3, Sevksiz3, istenen: 5, gelen: 0, konulan: 0);
            Satir(R4, Islenmis4, istenen: 5, gelen: 2, konulan: 2);
        }

        public async Task<OtomatikSahaAktarimOnizlemeDto> OnizleAsync(int? hedef = null)
        {
            var sonuc = await new OtomatikSahaAktarimOnizleQueryHandler(Uow, SandikServisi, Saha, new LookupStub())
                .Handle(new OtomatikSahaAktarimOnizleQuery { ProjeId = AnaProjeId, HedefSahaProjeId = hedef }, CancellationToken.None);
            Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
            return sonuc.Value!;
        }

        public Task<Result<OtomatikSahaAktarimSonucDto>> UygulaAsync(string planOzeti, int? hedef = null) =>
            new OtomatikSahayaAktarCommandHandler(Uow, SandikServisi, Saha, Hareket, new LookupStub(), new OrtakUser())
                .Handle(new OtomatikSahayaAktarCommand
                {
                    ProjeId = AnaProjeId,
                    HedefSahaProjeId = hedef,
                    PlanOzeti = planOzeti
                }, CancellationToken.None);

        public Task<Result<SahaAktarimiGeriAlSonucDto>> GeriAlAsync(int sahaAktarimId) =>
            new SahaAktarimiGeriAlCommandHandler(Uow, new OrtakUser(), Hareket, Saha)
                .Handle(new SahaAktarimiGeriAlCommand { SahaAktarimId = sahaAktarimId }, CancellationToken.None);

        public (int Proje, int Sandik, int Satir, int Icerik, int Aktarim, int Kalem) Sayilar() => (
            Uow.Repo<Proje>().Rows.Count,
            Uow.Repo<Sandik>().Rows.Count,
            Uow.Repo<CekiSatiri>().Rows.Count,
            Uow.Repo<SandikIcerik>().Rows.Count,
            Uow.Repo<SahaAktarim>().Rows.Count,
            Uow.Repo<SahaAktarimKalemi>().Rows.Count);

        private void Sandik(int id, string no, bool sevkli) =>
            Uow.Repo<Sandik>().Rows.Add(new Sandik
            {
                Id = id,
                ProjeId = AnaProjeId,
                SandikNo = no,
                Ad = $"Sandık {no}",
                DurumId = sevkli ? (int)SandikDurum.Sevkedildi : (int)SandikDurum.Hazirlaniyor
            });

        private void Satir(int id, int sandikId, decimal istenen, decimal gelen, decimal konulan)
        {
            var satir = new CekiSatiri
            {
                Id = id,
                CekiId = 1,
                SiraNo = id - 100,
                Aciklama = $"Ürün {id}",
                BarkodNo = $"B{id}",
                IstenenAdet = istenen,
                GelenMiktar = gelen,
                GridDurumuId = gelen > 0 ? (int)GridDurum.TamGeldi : (int)GridDurum.Gelmedi,
                GridSevkDurumuId = (int)GridSevkDurum.SevkEdilmedi,
                UcKDurumuId = gelen > 0 ? (int)UcKDurum.EksikGeldi : (int)UcKDurum.Bekliyor,
                UcKKarsilamaTipiId = (int)UcKDurum.Bekliyor
            };
            Uow.Repo<CekiSatiri>().Rows.Add(satir);
            Uow.Repo<SandikIcerik>().Rows.Add(new SandikIcerik
            {
                Id = id + 100,
                SandikId = sandikId,
                CekiSatiriId = id,
                TahsisMiktari = istenen,
                KonulanAdet = konulan
            });
        }
    }

    /// <summary>Fiziksel içerikleri bellekteki satırlarla eşleyerek döner (sentetik eski kayıt yok).</summary>
    private sealed class SandikServisi(OrtakMemoryUow uow) : ISandikService
    {
        public Task<IReadOnlyDictionary<int, IReadOnlyCollection<SandikIcerik>>> GetEtkinSandikIcerikleriAsync(
            IEnumerable<int> sandikIds,
            CancellationToken cancellationToken = default)
        {
            var satirlar = uow.Repo<CekiSatiri>().Rows.ToDictionary(s => s.Id);
            IReadOnlyDictionary<int, IReadOnlyCollection<SandikIcerik>> map = sandikIds.Distinct().ToDictionary(
                id => id,
                id => (IReadOnlyCollection<SandikIcerik>)uow.Repo<SandikIcerik>().Rows
                    .Where(i => i.SandikId == id)
                    .Select(i =>
                    {
                        if (i.CekiSatiriId is int satirId && satirlar.TryGetValue(satirId, out var satir))
                            i.CekiSatiri = satir;
                        return i;
                    })
                    .ToList());
            return Task.FromResult(map);
        }

        public Task<IEnumerable<Sandik>> GetProjeSandiklariAsync(int projeId) => throw new NotSupportedException();
        public Task<Sandik?> GetSandikDetayAsync(int sandikId) => throw new NotSupportedException();
        public Task<Sandik?> GetSandikByNoAsync(int projeId, string sandikNo) => throw new NotSupportedException();
        public Task<Sandik> SandikOlusturAsync(int projeId, string sandikNo, string depoLokasyonu = "Belirsiz") => throw new NotSupportedException();
        public Task<bool> SandikDegistirAsync(int cekiSatiriId, int yeniSandikId, int kullaniciId, int projeId) => throw new NotSupportedException();
        public Task<IEnumerable<SandikIcerik>> GetSandikIcerikAsync(int sandikId) => throw new NotSupportedException();
    }

    /// <summary>Saha haritalarını bellekteki aktif defter kayıtlarından hesaplar (servisin defter kuralıyla aynı).</summary>
    private sealed class SahaServisi(OrtakMemoryUow uow) : ISahaTamamlamaService
    {
        public List<int> SenkronizeEdilen { get; } = [];

        private IEnumerable<SahaAktarimKalemi> Aktif => uow.Repo<SahaAktarimKalemi>().Rows
            .Where(k => k.DurumId != (int)SahaAktarimDurum.GeriAlindi && k.DurumId != (int)SahaAktarimDurum.Iptal);

        public Task<Dictionary<int, decimal>> GetAktifTamamlamaMapAsync(IEnumerable<int> ids, CancellationToken token = default)
        {
            var kume = ids.ToHashSet();
            return Task.FromResult(Aktif
                .Where(k => kume.Contains(k.KaynakCekiSatiriId))
                .GroupBy(k => k.KaynakCekiSatiriId)
                .ToDictionary(g => g.Key, g => g.Sum(k => k.Miktar)));
        }

        public Task<KaynakSandikSahaAktarimDurumu> GetKaynakSandikSahaAktarimDurumuAsync(IEnumerable<int> ids, CancellationToken token = default)
        {
            var kume = ids.ToHashSet();
            return Task.FromResult(new KaynakSandikSahaAktarimDurumu
            {
                AktifAktarimaBagliSandikIds = Aktif
                    .Where(k => k.AktarimTipiId == (int)SahaAktarimTipi.SandikBazli &&
                                k.KaynakSandikId.HasValue &&
                                kume.Contains(k.KaynakSandikId.Value))
                    .Select(k => k.KaynakSandikId!.Value)
                    .ToHashSet()
            });
        }

        public Task SenkronizeKaynakProjelerAsync(IEnumerable<int> ids, CancellationToken token = default)
        {
            SenkronizeEdilen.AddRange(ids);
            return Task.CompletedTask;
        }

        public Task<bool> AktifTamamlamaVarMiAsync(int id, CancellationToken token = default) => throw new NotSupportedException();
        public Task<Dictionary<int, decimal>> GetAktifGerceklesenTamamlamaMapAsync(IEnumerable<int> ids, CancellationToken token = default) => throw new NotSupportedException();
        public Task<Dictionary<int, decimal>> GetAktifIsTamamlamaMapAsync(IEnumerable<int> ids, CancellationToken token = default) => throw new NotSupportedException();
        public Task<Dictionary<int, decimal>> GetSevkEdilenTamamlamaMapAsync(IEnumerable<int> ids, CancellationToken token = default) => throw new NotSupportedException();
        public Task<Dictionary<int, decimal>> GetSahaIptalKapananMapAsync(IEnumerable<int> ids, CancellationToken token = default) => Task.FromResult(new Dictionary<int, decimal>());
        public Task<Dictionary<int, decimal>> GetSevkEdilenGerceklesenTamamlamaMapAsync(IEnumerable<int> ids, CancellationToken token = default) => throw new NotSupportedException();
        public Task<HashSet<int>> GetAktifSandikBazliAktarimSatirIdsAsync(IEnumerable<int> ids, CancellationToken token = default) => throw new NotSupportedException();
        public Task SenkronizeKaynakProjelerBySahaSandikIdsAsync(IEnumerable<int> ids, CancellationToken token = default) => throw new NotSupportedException();
    }

    private sealed class LookupStub : ILookupCacheService
    {
        public string GetDeger<TLookup>(int id) where TLookup : LookupBase => id.ToString();
        public Task WarmupAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task RefreshAsync<TLookup>(CancellationToken ct = default) where TLookup : LookupBase => Task.CompletedTask;
    }

    /// <summary>1 numaralı sandık reddi motora ulaşmadan döner; mediator çağrılırsa test düşer.</summary>
    private sealed class KullanilmayanMediator : IMediator
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) => throw new InvalidOperationException("Mediator çağrılmamalıydı.");
        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest => throw new InvalidOperationException("Mediator çağrılmamalıydı.");
        public Task<object?> Send(object request, CancellationToken cancellationToken = default) => throw new InvalidOperationException("Mediator çağrılmamalıydı.");
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => throw new InvalidOperationException();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => throw new InvalidOperationException();
        public Task Publish(object notification, CancellationToken cancellationToken = default) => throw new InvalidOperationException();
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification => throw new InvalidOperationException();
    }
}
