using System.Text.Json;
using _3K.Application.Features.AmbalajIslemleri;
using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Application.Features.AmbalajIslemleri.Queries;
using _3K.Core.Models;

namespace _3K.Application.Tests;

public sealed class AmbalajAlanMaskelemeTests
{
    private static readonly AmbalajGorunumYetkileri HicbirIzin = new(false, false, false, false);

    [Fact]
    public void K33_PlanlamaLegacyOlcuM3_NestedKalemVeToplamdaNullOlur()
    {
        var row = new AmbalajPlanlamaKalemDto(1, 10, null, null, 1, "Proje", true,
            "1", "Sandık", "Ahşap", 3, 1200, 1000, 900, null, null, null, 5m);
        var plan = new AmbalajPlanlamaPlanDto(1, "P-1", null, "Müşteri", 1, "Normal", null, null,
            null, 1, 1, 1, [row], 3, 5m);

        var result = AmbalajAlanMaskesi.Maskele(plan, HicbirIzin);

        Assert.Null(result.Kalemler[0].Boy);
        Assert.Null(result.Kalemler[0].En);
        Assert.Null(result.Kalemler[0].Yukseklik);
        Assert.Null(result.Kalemler[0].HacimM3);
        Assert.Null(result.SeciliHacimM3);
        Assert.Equal(3, result.SeciliSandikAdedi);
        Assert.Equal("P-1", result.ProjeNo);
    }

    [Fact]
    public void K33_GlobalVeFiltrelenmisOzetler_YalnizHacmiGizler_SayaclariKorur()
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

        var result = AmbalajAlanMaskesi.Maskele(value, HicbirIzin);

        Assert.Null(result.FilteredSummary!.ToplamHacimM3);
        Assert.Null(result.FilteredSummary.TurOzetleri[0].ToplamHacimM3);
        Assert.Equal(12, result.FilteredSummary.ToplamSandikAdedi);
        Assert.Equal(3, result.TotalCount);
    }

    [Fact]
    public void K33_IcSandikSablonSecenekleri_OlcuIzniOlmadanBoyutTasiyamaz()
    {
        var result = AmbalajAlanMaskesi.Maskele(new AmbalajIcSandikSablonDto(1, "Seçenek", "Kontrplak", 1200, 1000, 900), HicbirIzin);

        Assert.Null(result.Boy);
        Assert.Null(result.En);
        Assert.Null(result.Yukseklik);
        Assert.Equal("Seçenek", result.Ad);
    }

    [Fact]
    public void K33_M3VarSarfYok_NetKorunur_SarfVeBirlesikToplamGizlenir()
    {
        var dto = new AmbalajUretimKaydiDto
        {
            Adet = 4, AmbalajaDahil = true, UretimeAlindi = true,
            NetM3 = 10, SarfM3 = 2, SarfOrani = 20, ToplamM3 = 12
        };

        AmbalajYetkilendirmeYardimcisi.DtoyuMaskele(dto, new(M3Gorunur: true, SarfGorunur: false, KaynakGorunur: true));
        var ozet = AmbalajRaporVerisi.OzetOlustur([dto]);

        Assert.Equal(10, dto.NetM3);
        Assert.Null(dto.SarfM3);
        Assert.Null(dto.SarfOrani);
        Assert.Null(dto.ToplamM3);
        Assert.Equal(10, ozet.NetM3);
        Assert.Null(ozet.SarfM3);
        Assert.Null(ozet.ToplamM3);
        Assert.Equal(4, ozet.ToplamSandikAdedi);
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
    public void K33_OzelSandikRaporu_MaskeliNullToplamlarSahteSifiraDonusmez()
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

    public static TheoryData<string> PlanlamaDtoTurleri() =>
        new(OrneklerVeMaskeler.Keys);

    /// <summary>
    /// Açık maske, kaldırılan pipeline reflection maskesiyle her izin kombinasyonunda aynı alanları gizler.
    /// </summary>
    [Theory]
    [MemberData(nameof(PlanlamaDtoTurleri))]
    public void K33_AcikMaske_EskiReflectionMaskesiyleAyniSonucuUretir(string tur)
    {
        var (ornek, acikMaske) = OrneklerVeMaskeler[tur];
        foreach (var izin in TumIzinKombinasyonlari())
        {
            var beklenen = ornek();
            EskiReflectionMaskesi(beklenen, izin);
            var gercek = acikMaske(ornek(), izin);
            Assert.Equal(JsonSerializer.Serialize(beklenen, beklenen.GetType()), JsonSerializer.Serialize(gercek, gercek.GetType()));
        }
    }

    private static readonly Dictionary<string, (Func<object> Ornek, Func<object, AmbalajGorunumYetkileri, object> Maske)> OrneklerVeMaskeler = new()
    {
        [nameof(AmbalajPlanlamaProjeleriSayfasiDto)] = (() => new AmbalajPlanlamaProjeleriSayfasiDto
        {
            Items = [ProjeOzeti()], PageNumber = 1, PageSize = 15, TotalCount = 1, TotalPages = 1,
            FilteredSummary = new() { ProjeSayisi = 1, ToplamSandikAdedi = 4, ToplamHacimM3 = 7.5m, EksikOlculuProjeSayisi = 1 }
        }, (x, izin) => AmbalajAlanMaskesi.Maskele((AmbalajPlanlamaProjeleriSayfasiDto)x, izin)),
        [nameof(AmbalajPlanlamaPlanDto)] = (() => new AmbalajPlanlamaPlanDto(1, "P-1", "FB", "Müşteri", 1, "Normal",
                "FP", "FP-2", "FP-3", 1, 2, 3, [PlanKalemi(), PlanKalemi() with { Id = 2 }], 2, 5.5m, 2, 3, 1),
            (x, izin) => AmbalajAlanMaskesi.Maskele((AmbalajPlanlamaPlanDto)x, izin)),
        [nameof(AmbalajPlanlamaKalemDto)] = (() => PlanKalemi(),
            (x, izin) => AmbalajAlanMaskesi.Maskele((AmbalajPlanlamaKalemDto)x, izin)),
        [nameof(AmbalajBagimsizSandiklarSayfasiDto)] = (() => new AmbalajBagimsizSandiklarSayfasiDto
        {
            Items = [BagimsizSandik()], PageNumber = 1, PageSize = 25, TotalCount = 1, TotalPages = 1,
            FilteredSummary = new()
            {
                KayitSayisi = 1, ToplamSandikAdedi = 2, UretimeAlinanSandikAdedi = 2, ToplamHacimM3 = 3.5m,
                TurOzetleri = [new() { Tur = 4, KayitSayisi = 1, ToplamSandikAdedi = 2, ToplamHacimM3 = 3.5m }]
            }
        }, (x, izin) => AmbalajAlanMaskesi.Maskele((AmbalajBagimsizSandiklarSayfasiDto)x, izin)),
        [nameof(AmbalajBagimsizSandikDto)] = (() => BagimsizSandik(),
            (x, izin) => AmbalajAlanMaskesi.Maskele((AmbalajBagimsizSandikDto)x, izin)),
        [nameof(AmbalajIcSandikSablonDto)] = (() => new AmbalajIcSandikSablonDto(1, "Şablon", "Kontrplak", 1200, 1000, 900),
            (x, izin) => AmbalajAlanMaskesi.Maskele((AmbalajIcSandikSablonDto)x, izin)),
        [nameof(AmbalajIlaveSandikAdayDto)] = (() => new AmbalajIlaveSandikAdayDto(1, "7", "Aday", 1200, 1000, 900),
            (x, izin) => AmbalajAlanMaskesi.Maskele((AmbalajIlaveSandikAdayDto)x, izin)),
        [nameof(AmbalajSandikSecenegiDto)] = (() => new AmbalajSandikSecenegiDto(1, "8", "Seçenek", 1200, 1000, 900),
            (x, izin) => AmbalajAlanMaskesi.Maskele((AmbalajSandikSecenegiDto)x, izin)),
    };

    private static AmbalajPlanlamaProjeOzetDto ProjeOzeti() => new(1, "P-1", "FB", "Müşteri", 1, "Normal", 4, 3, 1,
        ["9"], 7.5m, "FP", 2, 1, 1, 2.5m, 1, 2, 3, "FP-2", "FP-3", 2, 4.5m, 1.5m, 1.0m, 2, 4, 1);

    private static AmbalajPlanlamaKalemDto PlanKalemi() => new(1, 10, 5, 3, 1, "Proje", true, "1", "Sandık", "Ahşap",
        3, 1200, 1000, 900, "Amaç", "Talimat", "Açıklama", 5m, true, false, true, 2);

    private static AmbalajBagimsizSandikDto BagimsizSandik() => new(1, 4, "Saha", 5, "P-5", "Müşteri", 11, "11", "Kaynak",
        12, 3, "12", "Üst", true, "SAH-001", "Saha sandığı", "Ahşap", 2, 1200, 1000, 900, "Amaç", "Talimat", "Açıklama", 3.5m);

    private static IEnumerable<AmbalajGorunumYetkileri> TumIzinKombinasyonlari() =>
        from m3 in new[] { false, true }
        from sarf in new[] { false, true }
        from kaynak in new[] { false, true }
        from olcu in new[] { false, true }
        select new AmbalajGorunumYetkileri(m3, sarf, kaynak, olcu);

    /// <summary>Kaldırılan AmbalajYasamDongusuBehavior.MaskeleAsync algoritmasının birebir kopyası (kâhin).</summary>
    private static void EskiReflectionMaskesi(object? kok, AmbalajGorunumYetkileri izin)
    {
        var gorulen = new HashSet<object>(ReferenceEqualityComparer.Instance);
        void Gez(object? value)
        {
            if (value == null || value is string || value.GetType().IsValueType || value is byte[] || !gorulen.Add(value)) return;
            if (value is System.Collections.IEnumerable list) { foreach (var item in list) Gez(item); return; }
            foreach (var property in value.GetType().GetProperties())
            {
                if (property.GetIndexParameters().Length != 0 || !property.CanRead) continue;
                var name = property.Name;
                var hidden = (!izin.OlcuGorunur && name is "Boy" or "En" or "Yukseklik" or "IcOlculer" or "DisOlculer" or "OnDuvarYuksekligi") ||
                    (!izin.M3Gorunur && name.Contains("M3", StringComparison.Ordinal) && property.PropertyType == typeof(decimal?)) ||
                    (!izin.SarfGorunur && name is "SarfOrani" or "SarfM3" or "ToplamM3" or "SarfDahilM3");
                if (hidden && property.CanWrite && (!property.PropertyType.IsValueType || Nullable.GetUnderlyingType(property.PropertyType) != null))
                    property.SetValue(value, null);
                else Gez(property.GetValue(value));
            }
        }
        Gez(kok);
    }
}
