using _3K.Application.Features.ProjeIslemleri.Services;
using _3K.Core.Entities;
using _3K.Core.Enums;

namespace _3K.Application.Tests;

/// <summary>
/// Otomatik sahaya aktarım planı (veritabanısız): elle akışla aynı kurallar, 1 numaralı sandık kuralı ve plan özeti.
/// </summary>
public class OtomatikSahaAktarimPlanlayiciTests
{
    [Fact]
    public void SevkliSandiktakiKalan_EksiklerSandiginaGider()
    {
        var k = new Kurgu();
        var sandik = k.Sandik("2", sevkli: true);
        var satir = k.Satir(istenen: 10, gelen: 6);
        k.Icerik(sandik, satir, tahsis: 10, konulan: 6);

        var plan = k.Planla();

        Assert.Empty(plan.SandikAktarimlari);
        var eksik = Assert.Single(plan.EksikKalemleri);
        Assert.Equal(satir.Id, eksik.Satir.Id);
        Assert.Equal(4, eksik.Miktar);
        Assert.Equal(sandik.Id, eksik.KaynakSandikId);
        Assert.Equal("2", eksik.KaynakSandikNo);
    }

    [Fact]
    public void SevkEdilmemisIslemGormemisSandik_ElleSandikAktarimiGibiAktarilir()
    {
        var k = new Kurgu();
        k.Sandik("2", sevkli: true);
        var sandik = k.Sandik("3");
        var satir = k.Satir(istenen: 5);
        k.Icerik(sandik, satir, tahsis: 5);

        var plan = k.Planla();

        var aktarim = Assert.Single(plan.SandikAktarimlari);
        Assert.Equal(sandik.Id, aktarim.KaynakSandik.Id);
        var urun = Assert.Single(aktarim.Urunler);
        Assert.Equal(5, urun.Miktar);
        Assert.Empty(plan.EksikKalemleri);
    }

    [Fact]
    public void SevkEdilmemisIslenmisSandik_AnaProjedeKalir_EksikleriElleEksikListesiGibiAktarilir()
    {
        // PA703-01 vakası: sevk edilmemiş sandıkta ürünlerin bir kısmı gelip konulmuş, bir kısmı gelmemiş. Elle sandık
        // aktarımı sandığı reddeder (işlenmiş içerik); elle "Eksiklerden oluştur" gelmeyenleri gösterir. Otomatik de böyle.
        var k = new Kurgu();
        k.Sandik("2", sevkli: true);
        var sandik = k.Sandik("11");
        var gelmis = k.Satir(istenen: 2, gelen: 2);
        var gelmemis = k.Satir(istenen: 4);
        var kismi = k.Satir(istenen: 5, gelen: 2);
        k.Icerik(sandik, gelmis, tahsis: 2, konulan: 2);
        k.Icerik(sandik, gelmemis, tahsis: 4);
        k.Icerik(sandik, kismi, tahsis: 5, konulan: 2);

        var plan = k.Planla();

        Assert.Empty(plan.SandikAktarimlari);
        Assert.Equal([(gelmemis.Id, 4m, (int?)sandik.Id), (kismi.Id, 3m, sandik.Id)],
            plan.EksikKalemleri.Select(e => (e.Satir.Id, e.Miktar, e.KaynakSandikId)).OrderBy(x => x.Item1));
        var atlanan = Assert.Single(plan.Atlananlar);
        Assert.Equal("11", atlanan.SandikNo);
        Assert.Equal("Sandıkta işlem görmüş ürün var (gelmiş, konulmuş ya da karşılanmış); sandık ana projede kalır. Eksik 2 ürünü sahaya eksik olarak aktarılır.", atlanan.Neden);
        // Ürün ürün gerekçe ayrıntıdadır, kısa nedene karışmaz.
        Assert.NotEmpty(atlanan.Ayrintilar!);
    }

    [Fact]
    public void SevkEdilmemisIslenmisSandik_DahaOnceKismiPlanlananUrun_KalaniKadarAktarilir()
    {
        // PA703-01'deki SMC45108 gibi: ürünün kalanının bir kısmı daha önce başka bir sahaya planlanmış.
        var k = new Kurgu();
        k.Sandik("2", sevkli: true);
        var sandik = k.Sandik("11");
        var gelmis = k.Satir(istenen: 1, gelen: 1);
        var oncedenPlanli = k.Satir(istenen: 3);
        k.Icerik(sandik, gelmis, tahsis: 1, konulan: 1);
        k.Icerik(sandik, oncedenPlanli, tahsis: 3);
        k.Planlanmis[oncedenPlanli.Id] = 1;

        var eksik = Assert.Single(k.Planla().EksikKalemleri);
        Assert.Equal((oncedenPlanli.Id, 2m), (eksik.Satir.Id, eksik.Miktar));

        k.Planlanmis[oncedenPlanli.Id] = 3;
        var tamamPlanli = k.Planla();
        Assert.Empty(tamamPlanli.EksikKalemleri);
        Assert.Contains("Sahaya gidecek eksik ürünü yok", Assert.Single(tamamPlanli.Atlananlar).Neden);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("01")]
    [InlineData(" 1 ")]
    public void SevkEdilmemisBirNumaraliSandik_VeUrunleriAnaProjedeKalir(string sandikNo)
    {
        var k = new Kurgu();
        k.Sandik("2", sevkli: true);
        var birNumara = k.Sandik(sandikNo);
        var satir = k.Satir(istenen: 1);
        k.Icerik(birNumara, satir, tahsis: 1);

        var plan = k.Planla();

        Assert.True(plan.BosMu);
        var atlanan = Assert.Single(plan.Atlananlar);
        Assert.Contains("1 numaralı sandık ana projede kalır", atlanan.Neden);
    }

    [Fact]
    public void SevkliBirNumaraliSandiginEksikleri_DigerSevkliSandiklarGibiAktarilir()
    {
        var k = new Kurgu();
        var birNumara = k.Sandik("1", sevkli: true);
        var satir = k.Satir(istenen: 3, gelen: 1);
        k.Icerik(birNumara, satir, tahsis: 3, konulan: 1);

        var plan = k.Planla();

        Assert.Empty(plan.SandikAktarimlari);
        var eksik = Assert.Single(plan.EksikKalemleri);
        Assert.Equal(2, eksik.Miktar);
        Assert.Equal(birNumara.Id, eksik.KaynakSandikId);
    }

    [Fact]
    public void ZatenSahayaAktarilanSandik_TekrarAktarilmaz()
    {
        var k = new Kurgu();
        k.Sandik("2", sevkli: true);
        var sandik = k.Sandik("3");
        var satir = k.Satir(istenen: 5);
        k.Icerik(sandik, satir, tahsis: 5);
        k.AktifAktarimaBagli.Add(sandik.Id);
        k.Planlanmis[satir.Id] = 5;

        var plan = k.Planla();

        Assert.True(plan.BosMu);
        Assert.Contains(plan.Atlananlar, a => a.SandikNo == "3" && a.Neden.Contains("zaten sahaya aktarılmış"));
    }

    [Fact]
    public void HataliUrun_OtomatikAktarilmazGerekcesiyleListelenir()
    {
        var k = new Kurgu();
        var sandik = k.Sandik("2", sevkli: true);
        var satir = k.Satir(istenen: 4, gelen: 4);
        satir.HataliMiktar = 1;
        k.Icerik(sandik, satir, tahsis: 4, konulan: 4);

        var plan = k.Planla();

        Assert.Empty(plan.EksikKalemleri);
        var atlanan = Assert.Single(plan.Atlananlar);
        Assert.Equal(satir.Id, atlanan.Satir?.Id);
        Assert.Contains("Hatalı", atlanan.Neden);
    }

    [Fact]
    public void DahaOncePlanlananMiktar_ElleEksikListesiGibiDusulur()
    {
        var k = new Kurgu();
        var sandik = k.Sandik("2", sevkli: true);
        var satir = k.Satir(istenen: 10, gelen: 6);
        k.Icerik(sandik, satir, tahsis: 10, konulan: 6);
        k.Planlanmis[satir.Id] = 3;

        var eksik = Assert.Single(k.Planla().EksikKalemleri);

        Assert.Equal(1, eksik.Miktar);
    }

    [Fact]
    public void SevkliVeSevksizSandigaBolunmusSatir_PlanlanabilirKalaninTamamiAktarilir()
    {
        // Bölünmüş satır sevksiz sandığı sandık olarak göndermeye engeldir; satırın kalanı "Eksiklerden oluştur" gibi
        // bütün olarak gider (iki sandıktaki eksik toplamı = satırın kalanı).
        var k = new Kurgu();
        var sevkli = k.Sandik("2", sevkli: true);
        var sevksiz = k.Sandik("4");
        var satir = k.Satir(istenen: 10, gelen: 5);
        k.Icerik(sevkli, satir, tahsis: 6, konulan: 4);
        k.Icerik(sevksiz, satir, tahsis: 4, konulan: 1);

        var plan = k.Planla();

        var eksik = Assert.Single(plan.EksikKalemleri);
        Assert.Equal((5m, (int?)null, "2, 4"), (eksik.Miktar, eksik.KaynakSandikId, eksik.KaynakSandikNo));
        Assert.Contains(plan.Atlananlar, a => a.SandikNo == "4" && a.Neden.Contains("Eksik 1 ürünü"));
    }

    [Fact]
    public void BolunmusSatirdaOncekiPlan_KalandanDusulur_TekrarPlanlanmaz()
    {
        var k = new Kurgu();
        var sevkli = k.Sandik("2", sevkli: true);
        var sevksiz = k.Sandik("4");
        var satir = k.Satir(istenen: 10, gelen: 5);
        k.Icerik(sevkli, satir, tahsis: 6, konulan: 4);
        k.Icerik(sevksiz, satir, tahsis: 4, konulan: 1);
        k.Planlanmis[satir.Id] = 5;

        Assert.Empty(k.Planla().EksikKalemleri);

        k.Planlanmis[satir.Id] = 2;
        Assert.Equal(3, Assert.Single(k.Planla().EksikKalemleri).Miktar);
    }

    [Fact]
    public void SevksizSandiktakiSifirMiktarliGercekTahsis_UrunuEksiklereGondermez()
    {
        var k = new Kurgu();
        k.Sandik("2", sevkli: true);
        var birNumara = k.Sandik("1");
        var satir = k.Satir(istenen: 3);
        // Eski veride gerçek tahsis kaydının bütün miktarları sıfır kalmış olabilir; ürün yine bu sandıktadır.
        k.Icerik(birNumara, satir, tahsis: 0);

        var plan = k.Planla();

        Assert.Empty(plan.EksikKalemleri);
        Assert.Contains(plan.Atlananlar, a => a.SandikNo == "1");
    }

    [Fact]
    public void SevkiyatDuzeltmesiAcikSandik_PlanaGirmezGerekcesiyleListelenir()
    {
        var k = new Kurgu();
        var duzeltmede = k.Sandik("2", sevkli: true);
        duzeltmede.SevkiyatDuzeltmeAcikMi = true;
        var satir = k.Satir(istenen: 10, gelen: 6);
        k.Icerik(duzeltmede, satir, tahsis: 10, konulan: 6);

        var plan = k.Planla();

        Assert.True(plan.BosMu);
        Assert.Contains(plan.Atlananlar, a => a.SandikNo == "2" && a.Neden.Contains("düzeltmesi"));
    }

    [Fact]
    public void SandigiOlmayanSatirinKalani_EksiklerSandiginaKaynakSandiksizGider()
    {
        var k = new Kurgu();
        k.Sandik("2", sevkli: true);
        var satir = k.Satir(istenen: 2);

        var eksik = Assert.Single(k.Planla().EksikKalemleri);

        Assert.Equal((satir.Id, 2m, (int?)null), (eksik.Satir.Id, eksik.Miktar, eksik.KaynakSandikId));
    }

    [Theory]
    [InlineData(GridDurum.Iptal)]
    [InlineData(GridDurum.GridKapandi)]
    public void GridIptalVeKapandi_PlanaGirmez(GridDurum durum)
    {
        var k = new Kurgu();
        var sandik = k.Sandik("2", sevkli: true);
        var satir = k.Satir(istenen: 10);
        satir.GridDurumuId = (int)durum;
        k.Icerik(sandik, satir, tahsis: 10);

        Assert.True(k.Planla().BosMu);
    }

    [Fact]
    public void NormalOlmayanProje_Reddedilir()
    {
        var k = new Kurgu();
        k.Proje.ProjeTipiId = (int)ProjeTipi.Saha;
        k.Sandik("2", sevkli: true);

        var sonuc = OtomatikSahaAktarimPlanlayici.Planla(k.Girdi());

        Assert.False(sonuc.IsSuccess);
    }

    [Fact]
    public void HicSevkGormemisProje_409Doner()
    {
        var k = new Kurgu();
        k.Proje.DurumId = (int)ProjeDurum.Devam;
        k.Sandik("2");

        var sonuc = OtomatikSahaAktarimPlanlayici.Planla(k.Girdi());

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(409, sonuc.StatusCode);
    }

    [Fact]
    public void PlanOzeti_SiraBagimsizdirVeMiktarDegisinceDegisir()
    {
        var a = IkiKalemliKurgu(sirayiTersCevir: false);
        var b = IkiKalemliKurgu(sirayiTersCevir: true);
        var ozetA = a.Planla().PlanOzeti;

        Assert.Equal(ozetA, b.Planla().PlanOzeti);
        Assert.Equal(64, ozetA.Length);

        a.Satirlar[0].GelenMiktar += 1;
        Assert.NotEqual(ozetA, a.Planla().PlanOzeti);
    }

    private static Kurgu IkiKalemliKurgu(bool sirayiTersCevir)
    {
        var k = new Kurgu();
        var sevkli = k.Sandik("2", sevkli: true);
        var sevksiz = k.Sandik("3");
        var s1 = k.Satir(istenen: 10, gelen: 6);
        var s2 = k.Satir(istenen: 5);
        if (sirayiTersCevir)
        {
            k.Icerik(sevksiz, s2, tahsis: 5);
            k.Icerik(sevkli, s1, tahsis: 10, konulan: 6);
        }
        else
        {
            k.Icerik(sevkli, s1, tahsis: 10, konulan: 6);
            k.Icerik(sevksiz, s2, tahsis: 5);
        }

        return k;
    }

    private sealed class Kurgu
    {
        private int _id = 100;
        public Proje Proje { get; } = new() { Id = 1, ProjeNo = "PA723-01", ProjeTipiId = (int)ProjeTipi.Normal, DurumId = (int)ProjeDurum.EksikSevkEdildi };
        public List<Sandik> Sandiklar { get; } = [];
        public List<CekiSatiri> Satirlar { get; } = [];
        public List<SandikIcerik> Icerikler { get; } = [];
        public Dictionary<int, decimal> Planlanmis { get; } = [];
        public HashSet<int> AktifAktarimaBagli { get; } = [];

        public Sandik Sandik(string no, bool sevkli = false)
        {
            var sandik = new Sandik
            {
                Id = ++_id,
                ProjeId = Proje.Id,
                SandikNo = no,
                DurumId = sevkli ? (int)SandikDurum.Sevkedildi : (int)SandikDurum.Hazirlaniyor
            };
            Sandiklar.Add(sandik);
            return sandik;
        }

        public CekiSatiri Satir(decimal istenen, decimal gelen = 0)
        {
            var satir = new CekiSatiri
            {
                Id = ++_id,
                CekiId = 1,
                SiraNo = Satirlar.Count + 1,
                Aciklama = $"Ürün {Satirlar.Count + 1}",
                BarkodNo = $"B{_id}",
                IstenenAdet = istenen,
                GelenMiktar = gelen,
                GridDurumuId = gelen > 0 ? (int)GridDurum.TamGeldi : (int)GridDurum.Gelmedi,
                GridSevkDurumuId = (int)GridSevkDurum.SevkEdilmedi,
                UcKDurumuId = gelen > 0 ? (int)UcKDurum.EksikGeldi : (int)UcKDurum.Bekliyor,
                UcKKarsilamaTipiId = (int)UcKDurum.Bekliyor
            };
            Satirlar.Add(satir);
            return satir;
        }

        public void Icerik(Sandik sandik, CekiSatiri satir, decimal tahsis, decimal konulan = 0)
        {
            Icerikler.Add(new SandikIcerik
            {
                Id = ++_id,
                SandikId = sandik.Id,
                CekiSatiriId = satir.Id,
                CekiSatiri = satir,
                TahsisMiktari = tahsis,
                KonulanAdet = konulan
            });
        }

        public OtomatikSahaAktarimGirdisi Girdi() => new(
            Proje,
            Sandiklar,
            Sandiklar.ToDictionary(
                s => s.Id,
                s => (IReadOnlyCollection<SandikIcerik>)Icerikler.Where(i => i.SandikId == s.Id).ToList()),
            Satirlar.ToDictionary(s => s.Id),
            Icerikler,
            Planlanmis,
            Planlanmis,
            AktifAktarimaBagli);

        public OtomatikSahaAktarimPlani Planla()
        {
            var sonuc = OtomatikSahaAktarimPlanlayici.Planla(Girdi());
            Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
            return sonuc.Value!;
        }
    }
}
