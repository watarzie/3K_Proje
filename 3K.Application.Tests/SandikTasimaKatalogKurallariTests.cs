using _3K.Application.Common;
using _3K.Application.Features.SandikIslemleri.Commands;
using _3K.Core.Entities;
using _3K.Core.Enums;

namespace _3K.Application.Tests;

/// <summary>
/// Sandiklar arasi miktarli tasimanin kimlik, sevkiyat, belirsizlik,
/// idempotency ve fiili sandik kurallarini gercek handlerlar uzerinden sinar.
/// </summary>
public sealed class SandikTasimaKatalogKurallariTests
{
    [Theory]
    [InlineData("ayni-sandik", 400)]
    [InlineData("sandik-projesi", 400)]
    [InlineData("istek-projesi", 403)]
    [InlineData("ceki-projesi", 409)]
    public async Task SandikUrunTasi_ProjeCekiVeAyniSandikDogrulamalarindaMutasyonsuzReddedilir(
        string senaryo,
        int beklenenStatus)
    {
        using var kurgu = new Kurgu();
        var hedefSandikId = kurgu.HedefSandik.Id;
        var istekProjeId = Kurgu.ProjeId;

        switch (senaryo)
        {
            case "ayni-sandik":
                hedefSandikId = kurgu.KaynakSandik.Id;
                break;
            case "sandik-projesi":
                kurgu.HedefSandik.ProjeId = Kurgu.DigerProjeId;
                break;
            case "istek-projesi":
                istekProjeId = Kurgu.DigerProjeId;
                break;
            case "ceki-projesi":
                kurgu.Ceki.ProjeId = Kurgu.DigerProjeId;
                break;
        }

        var once = kurgu.AnlikDurum();

        var sonuc = await kurgu.TasiAsync(
            hedefSandikId: hedefSandikId,
            projeId: istekProjeId,
            miktar: 1,
            islemAnahtari: Guid.NewGuid());

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(beklenenStatus, sonuc.StatusCode);
        Assert.Equal(once, kurgu.AnlikDurum());
        Assert.Equal(0, kurgu.Uow.SaveCount);
        Assert.Empty(kurgu.Uow.Repo<SandikUrunTransferi>().Rows);
        Assert.Empty(kurgu.Uow.Repo<HareketGecmisi>().Rows);
    }

    [Theory]
    [InlineData("kaynak")]
    [InlineData("hedef")]
    public async Task SandikUrunTasi_SevkedilmisKaynakVeyaHedefDuzeltmeAcikOlsaDaMutasyonsuzReddedilir(
        string sevkedilmisSandik)
    {
        using var kurgu = new Kurgu();
        var sandik = sevkedilmisSandik == "kaynak" ? kurgu.KaynakSandik : kurgu.HedefSandik;
        sandik.DurumId = (int)SandikDurum.Sevkedildi;
        sandik.SevkiyatDuzeltmeAcikMi = true;
        var once = kurgu.AnlikDurum();

        var sonuc = await kurgu.TasiAsync(miktar: 1, islemAnahtari: Guid.NewGuid());

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(409, sonuc.StatusCode);
        Assert.Equal(once, kurgu.AnlikDurum());
        Assert.Equal(0, kurgu.Uow.SaveCount);
        Assert.Empty(kurgu.Uow.Repo<SandikUrunTransferi>().Rows);
        Assert.Empty(kurgu.Uow.Repo<HareketGecmisi>().Rows);
    }

    [Fact]
    public async Task SandikUrunTasi_HedefteAyniSatiraAitBirdenCokIcerikVarsaMutasyonsuzReddedilir()
    {
        using var kurgu = new Kurgu();
        kurgu.IcerikEkle(kurgu.HedefSandik, tahsis: 1, konulan: 0);
        kurgu.IcerikEkle(kurgu.HedefSandik, tahsis: 1, konulan: 0);
        var once = kurgu.AnlikDurum();

        var sonuc = await kurgu.TasiAsync(miktar: 1, islemAnahtari: Guid.NewGuid());

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(409, sonuc.StatusCode);
        Assert.Contains("birden fazla", sonuc.Error?.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(once, kurgu.AnlikDurum());
        Assert.Equal(0, kurgu.Uow.SaveCount);
        Assert.Empty(kurgu.Uow.Repo<SandikUrunTransferi>().Rows);
        Assert.Empty(kurgu.Uow.Repo<HareketGecmisi>().Rows);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SandikUrunTasi_AyniIslemAnahtariniAyniPayloadIcinIdempotent_FarkliPayloadIcin409Yapar(
        bool ayniPayload)
    {
        using var kurgu = new Kurgu();
        var islemAnahtari = Guid.NewGuid();
        var ilk = await kurgu.TasiAsync(miktar: 1.25m, islemAnahtari: islemAnahtari);
        Assert.True(ilk.IsSuccess, ilk.Error?.Message);
        var once = kurgu.AnlikDurum();
        var saveOnce = kurgu.Uow.SaveCount;

        var tekrar = await kurgu.TasiAsync(
            miktar: ayniPayload ? 1.25m : 1.5m,
            islemAnahtari: islemAnahtari);

        Assert.Equal(ayniPayload, tekrar.IsSuccess);
        Assert.Equal(ayniPayload ? 200 : 409, tekrar.StatusCode);
        Assert.Equal(once, kurgu.AnlikDurum());
        Assert.Equal(saveOnce, kurgu.Uow.SaveCount);
        Assert.Single(kurgu.Uow.Repo<SandikUrunTransferi>().Rows);
    }

    [Theory]
    [InlineData(false, "2")]
    [InlineData(true, "1")]
    public async Task SandikUrunTasi_TekAktifSandikKalincaFiiliSandigiGunceller_CokluTahsisVarkenUydurmaz(
        bool baskaAktifTahsisVar,
        string beklenenFiiliSandik)
    {
        using var kurgu = new Kurgu(tahsis: 3, konulan: 2);
        if (baskaAktifTahsisVar)
        {
            var ucuncu = kurgu.SandikEkle(3, "3");
            kurgu.IcerikEkle(ucuncu, tahsis: 2, konulan: 1);
        }

        var sonuc = await kurgu.TasiAsync(miktar: 3, islemAnahtari: Guid.NewGuid());

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(beklenenFiiliSandik, kurgu.Satir.FiiliSandikNo);
        Assert.DoesNotContain(
            kurgu.Uow.Repo<SandikIcerik>().Rows,
            i => i.Id == kurgu.KaynakIcerik.Id);
        var hedef = Assert.Single(
            kurgu.Uow.Repo<SandikIcerik>().Rows,
            i => i.SandikId == kurgu.HedefSandik.Id);
        Assert.Equal(3, hedef.TahsisMiktari);
        Assert.Equal(2, hedef.KonulanAdet);
        Assert.Equal(1, hedef.EksikAdet);
    }

    [Fact]
    public async Task FiiliSandikDegistir_CokluTahsisliSatiri409IleMutasyonsuzReddeder()
    {
        using var kurgu = new Kurgu(tahsis: 3, konulan: 2);
        var ucuncu = kurgu.SandikEkle(3, "3");
        kurgu.IcerikEkle(ucuncu, tahsis: 2, konulan: 1);
        var once = kurgu.AnlikDurum();

        var sonuc = await new FiiliSandikDegistirCommandHandler(
                kurgu.Uow,
                kurgu.Hareket,
                kurgu.Saha)
            .Handle(new FiiliSandikDegistirCommand
            {
                CekiSatiriId = kurgu.Satir.Id,
                YeniFiiliSandikNo = "4",
                ProjeId = Kurgu.ProjeId,
                KullaniciId = 7
            }, default);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(409, sonuc.StatusCode);
        Assert.Equal(once, kurgu.AnlikDurum());
        Assert.Equal(0, kurgu.Uow.SaveCount);
        Assert.Empty(kurgu.Uow.Repo<Revizyon>().Rows);
        Assert.Empty(kurgu.Hareket.Rows);
        Assert.DoesNotContain(kurgu.Uow.Repo<Sandik>().Rows, s => s.SandikNo == "4");
    }

    private sealed class Kurgu : IDisposable
    {
        public const int ProjeId = 10;
        public const int DigerProjeId = 20;
        private int _siradakiIcerikId = 12;

        public Kurgu(decimal tahsis = 5, decimal konulan = 3)
        {
            Proje = new Proje
            {
                Id = ProjeId,
                ProjeNo = "PA-TEST",
                Musteri = "Test",
                ProjeTipiId = (int)ProjeTipi.Normal,
                DurumId = (int)ProjeDurum.Hazirlaniyor
            };
            Ceki = new Ceki
            {
                Id = 100,
                ProjeId = ProjeId,
                Proje = Proje,
                YuklemeTarihi = DateTime.UtcNow
            };
            Satir = new CekiSatiri
            {
                Id = 1000,
                CekiId = Ceki.Id,
                Ceki = Ceki,
                SiraNo = 1,
                BarkodNo = "FCT-TEST",
                Aciklama = "Test urunu",
                IstenenAdet = 5,
                BirimId = (int)Birim.Adet,
                CekideGecenSandikNo = "1",
                FiiliSandikNo = "1",
                GridDurumuId = (int)GridDurum.Bekliyor,
                GridSevkDurumuId = (int)GridSevkDurum.SevkEdilmedi,
                UcKDurumuId = (int)UcKDurum.Bekliyor,
                UcKKarsilamaTipiId = (int)UcKDurum.Bekliyor,
                DurumId = (int)UrunDurum.Bekliyor
            };
            KaynakSandik = SandikEkle(1, "1");
            HedefSandik = SandikEkle(2, "2");
            KaynakIcerik = IcerikEkle(KaynakSandik, tahsis, konulan);

            Proje.Cekiler.Add(Ceki);
            Ceki.CekiSatirlari.Add(Satir);
            Uow.Repo<Proje>().Rows.Add(Proje);
            Uow.Repo<Ceki>().Rows.Add(Ceki);
            Uow.Repo<CekiSatiri>().Rows.Add(Satir);
        }

        public OrtakMemoryUow Uow { get; } = new();
        public OrtakHareket Hareket { get; } = new();
        public OrtakSaha Saha { get; } = new();
        public Proje Proje { get; }
        public Ceki Ceki { get; }
        public CekiSatiri Satir { get; }
        public Sandik KaynakSandik { get; }
        public Sandik HedefSandik { get; }
        public SandikIcerik KaynakIcerik { get; }

        public Sandik SandikEkle(int id, string no)
        {
            var sandik = new Sandik
            {
                Id = id,
                ProjeId = ProjeId,
                Proje = Proje,
                SandikNo = no,
                DurumId = (int)SandikDurum.Hazirlaniyor,
                DepoLokasyonId = (int)DepoLokasyon.Belirsiz
            };
            Proje.Sandiklar.Add(sandik);
            Uow.Repo<Sandik>().Rows.Add(sandik);
            return sandik;
        }

        public SandikIcerik IcerikEkle(Sandik sandik, decimal tahsis, decimal konulan)
        {
            var icerik = new SandikIcerik
            {
                Id = _siradakiIcerikId++,
                SandikId = sandik.Id,
                Sandik = sandik,
                CekiSatiriId = Satir.Id,
                CekiSatiri = Satir,
                TahsisMiktari = tahsis,
                KonulanAdet = konulan,
                EksikAdet = Math.Max(tahsis - konulan, 0),
                Miktar = tahsis,
                BirimId = Satir.BirimId
            };
            sandik.SandikIcerikleri.Add(icerik);
            Satir.SandikIcerikleri.Add(icerik);
            Uow.Repo<SandikIcerik>().Rows.Add(icerik);
            return icerik;
        }

        public Task<Result> TasiAsync(
            decimal miktar,
            Guid islemAnahtari,
            int? hedefSandikId = null,
            int? projeId = null) =>
            new SandikUrunTasiCommandHandler(Uow, new OrtakUser(), Saha)
                .Handle(new SandikUrunTasiCommand
                {
                    KaynakSandikIcerikId = KaynakIcerik.Id,
                    HedefSandikId = hedefSandikId ?? HedefSandik.Id,
                    TasinanAdet = miktar,
                    ProjeId = projeId ?? ProjeId,
                    IslemAnahtari = islemAnahtari
                }, default);

        public string AnlikDurum()
        {
            var sandiklar = string.Join(";", Uow.Repo<Sandik>().Rows
                .OrderBy(s => s.Id)
                .Select(s => $"{s.Id}:{s.ProjeId}:{s.SandikNo}:{s.DurumId}:{s.SevkiyatDuzeltmeAcikMi}:{s.DepoLokasyonId}"));
            var icerikler = string.Join(";", Uow.Repo<SandikIcerik>().Rows
                .OrderBy(i => i.Id)
                .Select(i => $"{i.Id}:{i.SandikId}:{i.CekiSatiriId}:{i.TahsisMiktari}:{i.KonulanAdet}:{i.EksikAdet}:{i.Miktar}:{i.StokKarsilanan}:{i.ProjeKarsilanan}:{i.TedarikciKarsilanan}:{i.AktifGridSevkKarsilananMiktari}"));
            var transferler = string.Join(";", Uow.Repo<SandikUrunTransferi>().Rows
                .OrderBy(t => t.Id)
                .Select(t => $"{t.Id}:{t.ProjeId}:{t.KaynakSandikIcerikId}:{t.HedefSandikId}:{t.Miktar}:{t.IslemAnahtari}"));
            return $"{Satir.Id}:{Satir.CekiId}:{Satir.FiiliSandikNo}:{Satir.IstenenAdet}|{Ceki.Id}:{Ceki.ProjeId}|{sandiklar}|{icerikler}|{transferler}";
        }

        public void Dispose() => Uow.Dispose();
    }
}
