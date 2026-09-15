using _3K.Application.Common;
using _3K.Application.Features.UcKIslemleri.Commands;
using _3K.Application.Features.UcKIslemleri.Queries;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Tests;

public sealed class UcKSozlesmeListeVeDtoKatalogTests
{
    [Fact]
    public void TekilKomut_DinamikOnayVeReferansSozlesmesiniTasirkenTopluVeResetKomutlariYalnizGuvenlidir()
    {
        var tekil = new UcKDurumGuncelleCommand
        {
            CekiSatiriId = 23,
            ProjeId = 10,
            KarsilamaTipiId = (int)UcKDurum.StoktanKarsilandi
        };

        Assert.IsAssignableFrom<ISecuredRequest>(tekil);
        Assert.IsAssignableFrom<IRequireApproval>(tekil);
        Assert.IsAssignableFrom<IApprovalOperation>(tekil);
        Assert.IsAssignableFrom<IApprovalReference>(tekil);
        var referans = tekil.GetApprovalReference();
        Assert.Equal(23, referans.ReferansId);
        Assert.Equal(10, referans.ProjeId);
        Assert.Equal("CekiSatiri", referans.ReferansTipi);

        object[] digerleri =
        [
            new UcKDurumSifirlaCommand(),
            new UcKTopluTamGeldiCommand(),
            new UcKTopluTedarikciCommand(),
            new UcKTopluSifirlaCommand()
        ];
        Assert.All(digerleri, x => Assert.IsAssignableFrom<ISecuredRequest>(x));
        Assert.All(digerleri, x => Assert.False(x is IRequireApproval));
        Assert.All(digerleri, x => Assert.False(x is IApprovalOperation));
        Assert.All(digerleri, x => Assert.False(x is IApprovalReference));
    }

    [Theory]
    [InlineData(UcKDurum.ProjedenKarsilandi, "PA-KAYNAK", null, "PA-KAYNAK")]
    [InlineData(UcKDurum.StoktanKarsilandi, null, 77, "77")]
    [InlineData(UcKDurum.TedarikcidenGeldi, null, null, "__ADET__")]
    public void TekilOnayAciklamasi_KaynakVeMiktarBilgisiniTasir(
        UcKDurum tip,
        string? kaynakProje,
        int? stokId,
        string beklenen)
    {
        var command = new UcKDurumGuncelleCommand
        {
            CekiSatiriId = 23,
            ProjeId = 10,
            KarsilamaTipiId = (int)tip,
            GelenAdet = 1.25m,
            MevcutProjeNo = "PA-HEDEF",
            MevcutSandikNo = "2",
            UrunAdi = "Pompa",
            KaynakHedefProjeNo = kaynakProje,
            KaynakUrunAdi = "Donör pompa",
            StokKaydiId = stokId
        };

        var beklenenParca = beklenen == "__ADET__"
            ? 1.25m.ToString(System.Globalization.CultureInfo.CurrentCulture)
            : beklenen;
        Assert.False(string.IsNullOrWhiteSpace(command.GetApprovalOperationCode()));
        Assert.Contains(beklenenParca, command.GetApprovalDescription(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UrunListesi_CokluTahsisleriAyriDtoYapar_AktifPartiKalaniniChildBazindaBolmedenKorur()
    {
        using var uow = new OrtakMemoryUow();
        var proje = new Proje { Id = 1, ProjeNo = "PA-DTO", ProjeTipiId = (int)ProjeTipi.Normal };
        var ceki = new Ceki { Id = 2, ProjeId = proje.Id, Proje = proje };
        var satir = SatirOlustur(3, ceki, 4m, 4m, 1m, 4m, 1m, false);
        satir.OrijinalIstenenAdet = 3m;
        var sandik1 = SandikOlustur(11, proje, "1");
        var sandik2 = SandikOlustur(12, proje, "2");
        var icerik1 = IcerikOlustur(21, satir, sandik1, 2m, 1m, 1m);
        var icerik2 = IcerikOlustur(22, satir, sandik2, 2m, 0m, 0m);
        BaglaVeEkle(uow, proje, ceki, [satir], [sandik1, sandik2], [icerik1, icerik2]);

        var sonuc = await new GetUcKUrunlerQueryHandler(uow, new LookupStub(), new OrtakSaha())
            .Handle(new GetUcKUrunlerQuery { ProjeId = proje.Id }, default);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        var dtolar = sonuc.Value!;
        Assert.Equal(2, dtolar.Count);
        Assert.Equal([21, 22], dtolar.Select(x => x.SandikIcerikId!.Value).Order().ToArray());
        Assert.All(dtolar, dto =>
        {
            Assert.Equal(4m, dto.AnaIstenenAdet);
            Assert.Equal(3m, dto.OrijinalIstenenAdet);
            Assert.Equal(2m, dto.SandikMiktari);
            Assert.True(dto.SandikBazliDagitim);
            Assert.True(dto.AktifGridSevkPartisiTeslimeAcikMi);
        });
        Assert.Equal(3m, dtolar.Sum(x => x.AktifGridSevkPartisiKalanMiktari));
        Assert.Equal(1m, dtolar.Single(x => x.SandikIcerikId == 21).AktifGridSevkPartisiKalanMiktari);
        Assert.Equal(2m, dtolar.Single(x => x.SandikIcerikId == 22).AktifGridSevkPartisiKalanMiktari);
    }

    [Fact]
    public async Task UrunListesi_TahsisYoksaFiiliSandiklaTekFallbackDtoUretirVeAnaMiktariKullanir()
    {
        using var uow = new OrtakMemoryUow();
        var proje = new Proje { Id = 1, ProjeNo = "PA-FALLBACK", ProjeTipiId = (int)ProjeTipi.Normal };
        var ceki = new Ceki { Id = 2, ProjeId = proje.Id, Proje = proje };
        var satir = SatirOlustur(3, ceki, 2.5m, 0m, 0m, 0m, 0m, null);
        satir.FiiliSandikNo = "7";
        var sandik = SandikOlustur(11, proje, "7");
        BaglaVeEkle(uow, proje, ceki, [satir], [sandik], []);

        var sonuc = await new GetUcKUrunlerQueryHandler(uow, new LookupStub(), new OrtakSaha())
            .Handle(new GetUcKUrunlerQuery { ProjeId = proje.Id }, default);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        var dto = Assert.Single(sonuc.Value!);
        Assert.Null(dto.SandikIcerikId);
        Assert.Equal("7", dto.SandikNo);
        Assert.Equal(2.5m, dto.AnaIstenenAdet);
        Assert.Equal(2.5m, dto.SandikMiktari);
        Assert.Equal(2.5m, dto.IstenenAdet);
        Assert.False(dto.SandikBazliDagitim);
    }

    [Fact]
    public async Task UrunListesi_ManuelSahaIceriginiTamamlanmisNegatifSentetikKimlikleMapler()
    {
        using var uow = new OrtakMemoryUow();
        var proje = new Proje { Id = 1, ProjeNo = "SAHA", ProjeTipiId = (int)ProjeTipi.Saha };
        var sandik = SandikOlustur(11, proje, "S-1");
        var manuel = new SandikIcerik
        {
            Id = 31,
            SandikId = sandik.Id,
            Sandik = sandik,
            CekiSatiriId = null,
            Isim = "Manuel vana",
            BarkodNo = "MN-1",
            Miktar = 1.75m,
            BirimId = (int)Birim.Adet,
            KaynakProjeNo = "PA-KAYNAK"
        };
        proje.Sandiklar.Add(sandik);
        sandik.SandikIcerikleri.Add(manuel);
        uow.Repo<Proje>().Rows.Add(proje);
        uow.Repo<Sandik>().Rows.Add(sandik);
        uow.Repo<SandikIcerik>().Rows.Add(manuel);

        var sonuc = await new GetUcKUrunlerQueryHandler(uow, new LookupStub(), new OrtakSaha())
            .Handle(new GetUcKUrunlerQuery { ProjeId = proje.Id }, default);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        var dto = Assert.Single(sonuc.Value!);
        Assert.Equal(-31, dto.CekiSatiriId);
        Assert.True(dto.IsSahaManuelSandikIcerigi);
        Assert.True(dto.IsManuelEklenen);
        Assert.Equal(1.75m, dto.AnaIstenenAdet);
        Assert.Equal(1.75m, dto.GelenMiktar);
        Assert.Equal(0m, dto.Kalan);
        Assert.Equal((int)UcKDurum.TamGeldi, dto.UcKKarsilamaTipiId);
        Assert.Equal((int)UrunDurum.Tamamlandi, dto.GenelDurumId);
    }

    [Fact]
    public async Task IsListesi_SayfalamayiSatirDegilProjeBazindaYaparVeFiltreOncesiOzetSayaclariniKorur()
    {
        using var uow = new OrtakMemoryUow();
        var projeA = new Proje { Id = 1, ProjeNo = "PA-A", DurumId = (int)ProjeDurum.Hazirlaniyor };
        var cekiA = new Ceki { Id = 11, ProjeId = projeA.Id, Proje = projeA };
        var a1 = SatirOlustur(101, cekiA, 3m, 3m, 0m, 3m, 0m, false);
        var a2 = SatirOlustur(102, cekiA, 2m, 2m, 0m, 2m, 0m, false);
        a1.UpdatedDate = new DateTime(2026, 9, 12, 10, 0, 0);
        a2.UpdatedDate = new DateTime(2026, 9, 12, 9, 0, 0);
        BaglaVeEkle(uow, projeA, cekiA, [a1, a2], [], []);

        var projeB = new Proje { Id = 2, ProjeNo = "PA-B", DurumId = (int)ProjeDurum.Hazirlaniyor };
        var cekiB = new Ceki { Id = 12, ProjeId = projeB.Id, Proje = projeB };
        var b1 = SatirOlustur(201, cekiB, 4m, 4m, 2m, 4m, 2m, true);
        b1.GridSevkDurumuId = (int)GridSevkDurum.YenidenSevkGerekli;
        b1.YenidenSevkGerekliAdet = 2m;
        b1.UpdatedDate = new DateTime(2026, 9, 11, 10, 0, 0);
        BaglaVeEkle(uow, projeB, cekiB, [b1], [], []);

        var projeC = new Proje { Id = 3, ProjeNo = "PA-C", DurumId = (int)ProjeDurum.Hazirlaniyor };
        var cekiC = new Ceki { Id = 13, ProjeId = projeC.Id, Proje = projeC };
        var tamam = SatirOlustur(301, cekiC, 1m, 1m, 1m, 1m, 1m, false);
        BaglaVeEkle(uow, projeC, cekiC, [tamam], [], []);

        var handler = new GetUcKIsListesiQueryHandler(uow, new LookupStub());
        var ilkSayfa = await handler.Handle(new GetUcKIsListesiQuery { Page = 1, PageSize = 1 }, default);
        var yenidenFiltre = await handler.Handle(new GetUcKIsListesiQuery
        {
            Page = 1, PageSize = 1, IsTipi = "yeniden"
        }, default);
        var sinirlanmis = await handler.Handle(new GetUcKIsListesiQuery
        {
            Page = 0, PageSize = 999
        }, default);

        Assert.True(ilkSayfa.IsSuccess, ilkSayfa.Error?.Message);
        Assert.Equal(2, ilkSayfa.Value!.Liste.TotalCount);
        Assert.True(ilkSayfa.Value.Liste.HasMore);
        Assert.Equal(2, ilkSayfa.Value.Liste.Items.Count);
        Assert.All(ilkSayfa.Value.Liste.Items, x => Assert.Equal(projeA.Id, x.ProjeId));
        Assert.Equal(3, ilkSayfa.Value.Toplam);
        Assert.Equal(2, ilkSayfa.Value.TeslimBekleyen);
        Assert.Equal(1, ilkSayfa.Value.YenidenSevkGerekli);

        Assert.True(yenidenFiltre.IsSuccess, yenidenFiltre.Error?.Message);
        Assert.Equal(3, yenidenFiltre.Value!.Toplam);
        Assert.Equal(2, yenidenFiltre.Value.TeslimBekleyen);
        Assert.Equal(1, yenidenFiltre.Value.YenidenSevkGerekli);
        var yeniden = Assert.Single(yenidenFiltre.Value.Liste.Items);
        Assert.Equal(projeB.Id, yeniden.ProjeId);
        Assert.Equal("yeniden", yeniden.IsTipi);
        Assert.Equal(1, sinirlanmis.Value!.Liste.Page);
        Assert.Equal(100, sinirlanmis.Value.Liste.PageSize);
    }

    [Fact]
    public async Task IsListesi_SevkEdilmisProjeyiYalnizSandikDuzeltmesiAcikkenGosterir()
    {
        using var uow = new OrtakMemoryUow();
        var proje = new Proje
        {
            Id = 1,
            ProjeNo = "PA-SEVK",
            DurumId = (int)ProjeDurum.SevkEdildi
        };
        var ceki = new Ceki { Id = 11, ProjeId = proje.Id, Proje = proje };
        var satir = SatirOlustur(101, ceki, 2m, 2m, 0m, 2m, 0m, false);
        var sandik = SandikOlustur(21, proje, "1");
        sandik.DurumId = (int)SandikDurum.Sevkedildi;
        var icerik = IcerikOlustur(31, satir, sandik, 2m, 0m, 0m);
        BaglaVeEkle(uow, proje, ceki, [satir], [sandik], [icerik]);
        var handler = new GetUcKIsListesiQueryHandler(uow, new LookupStub());

        var kapali = await handler.Handle(new GetUcKIsListesiQuery { Page = 1, PageSize = 10 }, default);
        sandik.SevkiyatDuzeltmeAcikMi = true;
        var acik = await handler.Handle(new GetUcKIsListesiQuery { Page = 1, PageSize = 10 }, default);

        Assert.True(kapali.IsSuccess, kapali.Error?.Message);
        Assert.Empty(kapali.Value!.Liste.Items);
        Assert.True(acik.IsSuccess, acik.Error?.Message);
        Assert.Single(acik.Value!.Liste.Items);
        Assert.Equal(proje.Id, acik.Value.Liste.Items[0].ProjeId);
    }

    private static CekiSatiri SatirOlustur(
        int id,
        Ceki ceki,
        decimal istenen,
        decimal gridGelen,
        decimal gelen,
        decimal? sevk,
        decimal? aktif,
        bool? erken)
    {
        return new CekiSatiri
        {
            Id = id,
            CekiId = ceki.Id,
            Ceki = ceki,
            SiraNo = id,
            BarkodNo = $"BC-{id}",
            Aciklama = $"Ürün {id}",
            IstenenAdet = istenen,
            BirimId = (int)Birim.Adet,
            CekideGecenSandikNo = "1",
            GridDurumuId = (int)GridDurum.TamGeldi,
            GridGelenAdet = gridGelen,
            GridSevkDurumuId = (int)GridSevkDurum.SevkEdildi,
            GridSevkMiktari = sevk,
            AktifGridSevkKarsilananMiktari = aktif,
            AktifGridSevkPartisiErkenSonuclandirildiMi = erken,
            GelenMiktar = gelen,
            UcKDurumuId = gelen > 0 ? (int)UcKDurum.EksikGeldi : (int)UcKDurum.Bekliyor,
            UcKKarsilamaTipiId = gelen > 0 ? (int)UcKDurum.EksikGeldi : (int)UcKDurum.Bekliyor
        };
    }

    private static Sandik SandikOlustur(int id, Proje proje, string no) => new()
    {
        Id = id,
        ProjeId = proje.Id,
        Proje = proje,
        SandikNo = no,
        DurumId = (int)SandikDurum.Hazirlaniyor,
        DepoLokasyonId = (int)DepoLokasyon.Belirsiz
    };

    private static SandikIcerik IcerikOlustur(
        int id,
        CekiSatiri satir,
        Sandik sandik,
        decimal tahsis,
        decimal konulan,
        decimal aktif) => new()
    {
        Id = id,
        CekiSatiriId = satir.Id,
        CekiSatiri = satir,
        SandikId = sandik.Id,
        Sandik = sandik,
        TahsisMiktari = tahsis,
        KonulanAdet = konulan,
        EksikAdet = Math.Max(tahsis - konulan, 0),
        AktifGridSevkKarsilananMiktari = aktif,
        BirimId = (int)Birim.Adet
    };

    private static void BaglaVeEkle(
        OrtakMemoryUow uow,
        Proje proje,
        Ceki ceki,
        IEnumerable<CekiSatiri> satirlar,
        IEnumerable<Sandik> sandiklar,
        IEnumerable<SandikIcerik> icerikler)
    {
        if (!proje.Cekiler.Contains(ceki)) proje.Cekiler.Add(ceki);
        foreach (var satir in satirlar)
        {
            if (!ceki.CekiSatirlari.Contains(satir)) ceki.CekiSatirlari.Add(satir);
            uow.Repo<CekiSatiri>().Rows.Add(satir);
        }
        foreach (var sandik in sandiklar)
        {
            if (!proje.Sandiklar.Contains(sandik)) proje.Sandiklar.Add(sandik);
            uow.Repo<Sandik>().Rows.Add(sandik);
        }
        foreach (var icerik in icerikler)
        {
            icerik.CekiSatiri?.SandikIcerikleri.Add(icerik);
            icerik.Sandik.SandikIcerikleri.Add(icerik);
            uow.Repo<SandikIcerik>().Rows.Add(icerik);
        }
        uow.Repo<Proje>().Rows.Add(proje);
        uow.Repo<Ceki>().Rows.Add(ceki);
    }

    private sealed class LookupStub : ILookupCacheService
    {
        public string GetDeger<TLookup>(int id) where TLookup : LookupBase => $"{typeof(TLookup).Name}:{id}";
        public Task WarmupAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task RefreshAsync<TLookup>(CancellationToken ct = default) where TLookup : LookupBase => Task.CompletedTask;
    }
}
