using _3K.Application.Behaviors;
using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri;
using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Application.Features.AmbalajIslemleri.Queries;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Tests;

public sealed class AmbalajAlanMaskelemeTests
{
    [Fact]
    public async Task K33_PlanlamaLegacyOlcuM3_NestedKalemVeToplamdaNullOlur()
    {
        var row = new AmbalajPlanlamaKalemDto(1, 10, null, null, 1, "Proje", true,
            "1", "Sandık", "Ahşap", 3, 1200, 1000, 900, null, null, null, 5m);
        var plan = new AmbalajPlanlamaPlanDto(1, "P-1", null, "Müşteri", 1, "Normal", null, null,
            null, 1, 1, 1, [row], 3, 5m);
        var result = await Run(new GetAmbalajPlanlamaPlanQuery(), plan);
        Assert.Null(result.Kalemler[0].Boy);
        Assert.Null(result.Kalemler[0].En);
        Assert.Null(result.Kalemler[0].Yukseklik);
        Assert.Null(result.Kalemler[0].HacimM3);
        Assert.Null(result.SeciliHacimM3);
        Assert.Equal(3, result.SeciliSandikAdedi);
        Assert.Equal("P-1", result.ProjeNo);
    }

    [Fact]
    public async Task K33_GlobalVeFiltrelenmisOzetler_YalnizHacmiGizler_SayaclariKorur()
    {
        var value = new AmbalajBagimsizSandiklarSayfasiDto
        {
            TotalCount = 3, PageSize = 25,
            FilteredSummary = new()
            {
                KayitSayisi = 3, ToplamSandikAdedi = 12, ToplamHacimM3 = 7.25m,
                TurOzetleri = [new() { Tur = 2, KayitSayisi = 3, ToplamSandikAdedi = 12, ToplamHacimM3 = 7.25m }]
            }
        };
        var result = await Run(new GetAmbalajBagimsizSandiklarQuery(), value);
        Assert.Null(result.FilteredSummary!.ToplamHacimM3);
        Assert.Null(result.FilteredSummary.TurOzetleri[0].ToplamHacimM3);
        Assert.Equal(12, result.FilteredSummary.ToplamSandikAdedi);
        Assert.Equal(3, result.TotalCount);
    }

    [Fact]
    public async Task K33_IcSandikSablonSecenekleri_OlcuIzniOlmadanBoyutTasiyamaz()
    {
        IReadOnlyList<AmbalajIcSandikSablonDto> value = [new(1, "Seçenek", "Kontrplak", 1200, 1000, 900)];
        var result = await Run(new GetAmbalajIcSandikSablonlariQuery(), value);
        Assert.Null(result[0].Boy);
        Assert.Null(result[0].En);
        Assert.Null(result[0].Yukseklik);
        Assert.Equal("Seçenek", result[0].Ad);
    }

    [Fact]
    public async Task K33_M3VarSarfYok_NetKorunur_SarfVeBirlesikToplamGizlenir()
    {
        var value = new AmbalajUretimSayfasiDto
        {
            Items = [new() { Adet = 4, NetM3 = 10, SarfM3 = 2, SarfOrani = 20, ToplamM3 = 12 }],
            FilteredSummary = new() { KayitSayisi = 1, ToplamSandikAdedi = 4, NetM3 = 10, SarfM3 = 2, ToplamM3 = 12 }
        };
        var result = await Run(new GetAmbalajUretimSayfasiQuery(), value, AmbalajMenuKodlari.M3Goruntule);
        Assert.Equal(10, result.Items[0].NetM3);
        Assert.Equal(10, result.FilteredSummary.NetM3);
        Assert.Null(result.Items[0].SarfM3);
        Assert.Null(result.Items[0].SarfOrani);
        Assert.Null(result.Items[0].ToplamM3);
        Assert.Null(result.FilteredSummary.SarfM3);
        Assert.Null(result.FilteredSummary.ToplamM3);
        Assert.Equal(4, result.FilteredSummary.ToplamSandikAdedi);
    }

    [Fact]
    public async Task K33_GecmisIzniYoksa_DetayHareketleriCikmaz()
    {
        var value = new AmbalajUretimKaydiDetayDto
        {
            Id = 1, Hareketler = [new() { AlanAdi = "Boy", EskiDeger = "1200", YeniDeger = "1500" }]
        };
        var result = await Run(new GetAmbalajUretimKaydiDetayQuery { Id = 1 }, value);
        Assert.Empty(result.Hareketler);
    }

    [Fact]
    public void K33_OlcuRet_GerceklesmeAuditSnapshotStringiniDaGizler()
    {
        var hareket = new AmbalajUretimHareketiDto
        {
            AlanAdi = "GerceklesmeSnapshot", Islem = "Düzeltme", EskiDeger = "{\"Boy\":1200}",
            YeniDeger = "{\"Boy\":1500}", Aciklama = "1200 yerine 1500"
        };
        var result = AmbalajYetkilendirmeYardimcisi.HareketiMaskele(hareket, new(true, true, true, false));
        Assert.Null(result.EskiDeger);
        Assert.Null(result.YeniDeger);
        Assert.Null(result.Aciklama);
        Assert.True(result.DegerlerGizliMi);
    }

    [Fact]
    public void K33_KayitliForm_OlcuReddindeOnDuvarSahteSifirOlmaz_ParcaBoyutlariSizmaz()
    {
        var form = new AmbalajUretimFormuModel
        {
            NetM3 = 10, SarfM3 = 2, ToplamM3 = 12,
            Kalemler = [new()
            {
                Adet = 4, IcOlculer = new(1200, 1000, 900), DisOlculer = new(1292, 1092, 1155),
                OnDuvarYuksekligi = 900, NetM3 = 10, SarfM3 = 2, ToplamM3 = 12,
                Parcalar = [new() { KesitEn = 22, KesitYukseklik = 100, Uzunluk = 900, HacimM3 = 0.5m }]
            }]
        };
        AmbalajYasamDongusuYardimcisi.Maskele(form, new(true, true, true, false));
        Assert.Null(form.Kalemler[0].IcOlculer);
        Assert.Null(form.Kalemler[0].DisOlculer);
        Assert.Null(form.Kalemler[0].OnDuvarYuksekligi);
        Assert.Empty(form.Kalemler[0].Parcalar);
        Assert.Equal(4, form.Kalemler[0].Adet);
        Assert.Equal(10, form.NetM3);
    }

    [Fact]
    public void K33_LegacyRapor_MaskeliNullToplamlarSahteSifiraDonusmez()
    {
        var result = AmbalajRaporVerisi.OzetOlustur([new()
        {
            Adet = 4, AmbalajaDahil = true, UretimeAlindi = true, NetM3 = null, SarfM3 = null, ToplamM3 = null
        }]);
        Assert.Null(result.NetM3);
        Assert.Null(result.SarfM3);
        Assert.Null(result.ToplamM3);
        Assert.Equal(4, result.ToplamSandikAdedi);
    }

    private static async Task<T> Run<T>(object request, T value, params string[] permissions)
    {
        var result = await new AmbalajYasamDongusuBehavior<object, Result<T>>(
            new OrtakMemoryUow(), new Roles(permissions), new OrtakUser()).Handle(request,
            () => Task.FromResult(Result<T>.Success(value)), default);
        Assert.True(result.IsSuccess, result.Error?.Message);
        return result.Value!;
    }

    private sealed class Roles(params string[] allowed) : IRolService
    {
        public Task<bool> HasUserPermissionAsync(int userId, string menuKod, YetkiTipi requiredYetkiTipi, CancellationToken ct = default) =>
            Task.FromResult(allowed.Contains(menuKod) && requiredYetkiTipi == YetkiTipi.R);
        public Task<bool> IsAdminAsync(int userId, CancellationToken ct = default) => Task.FromResult(false);
        public Task<List<MenuTanimi>> GetMenuAgaciAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<List<RolYetki>> GetRolYetkileriAsync(int rolId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task YetkileriGuncelleAsync(int rolId, List<RolYetki> yetkiler, CancellationToken ct = default) => throw new NotSupportedException();
    }
}
