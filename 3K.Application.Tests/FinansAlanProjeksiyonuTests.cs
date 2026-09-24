using System.Text.Json.Nodes;
using _3K.Core.Models;

namespace _3K.Application.Tests;

public sealed class FinansAlanProjeksiyonuTests
{
    private static readonly AlanErisimYetkileri All = new(true, true, true, true, true, true, true, true, true);

    [Theory]
    [InlineData("2")]
    [InlineData("\"Metrekup\"")]
    [InlineData("\"M3\"")]
    public void K33_M3FiyatlandirmaMiktari_EnumVeMetinleAyniSekildeGizlenir(string unit)
    {
        var json = JsonNode.Parse("{\"adet\":3,\"fiyatlandirmaBirimi\":" + unit + ",\"fiyatlandirmaMiktari\":72.123}")!;
        FinansAlanProjeksiyonu.Uygula(json, All with { UretimM3 = false, Sarf = false });
        Assert.Null(json["fiyatlandirmaMiktari"]);
        Assert.Equal(3, json["adet"]!.GetValue<int>());
    }

    [Theory]
    [InlineData("m3")]
    [InlineData("m³")]
    public void K33_AylikM3IcinSiparisVeFaturaMiktarlariDaGizlenir(string unit)
    {
        var json = new JsonObject
        {
            ["birim"] = unit, ["miktar"] = 72.123m, ["siparisMiktari"] = 60m,
            ["faturalananMiktar"] = 50m, ["sandikAdedi"] = 3m
        };
        FinansAlanProjeksiyonu.Uygula(json, All with { UretimM3 = false, Sarf = false });
        Assert.Null(json["miktar"]);
        Assert.Null(json["siparisMiktari"]);
        Assert.Null(json["faturalananMiktar"]);
        Assert.Equal(3m, json["sandikAdedi"]!.GetValue<decimal>());
    }

    [Fact]
    public void K33_SarfRet_NetM3IzniSarfHacminiAcamaz()
    {
        var json = JsonNode.Parse("""
            { "items": [
              { "kaynakBileseni":"NET", "isTuru":1, "adet":3, "birimM3":5, "toplamM3":15 },
              { "kaynakBileseni":"SARF", "isTuru":9, "adet":3, "birimM3":1, "toplamM3":3 }
            ] }
            """)!;
        FinansAlanProjeksiyonu.Uygula(json, All with { Sarf = false });
        Assert.Equal(15, json["items"]![0]!["toplamM3"]!.GetValue<int>());
        Assert.Null(json["items"]![1]!["birimM3"]);
        Assert.Null(json["items"]![1]!["toplamM3"]);
    }

    [Theory]
    [InlineData("Olcu")]
    [InlineData("UretimM3")]
    [InlineData("Sarf")]
    public void K33_DinamikSablonStringleriVeBilesenler_YalnizMaliIzinleAcilmaz(string denied)
    {
        var permissions = denied switch
        {
            "Olcu" => All with { Olcu = false },
            "UretimM3" => All with { UretimM3 = false },
            _ => All with { Sarf = false }
        };
        var json = JsonNode.Parse("""
            { "id":1, "alanDegerleri":{"serbest-olcu":"1234x567x890"},
              "bilesenler":[{"ad":"Kereste 1234 mm","yontem":2,"miktar":72.123,"birimFiyat":777.25}] }
            """)!;
        FinansAlanProjeksiyonu.Uygula(json, permissions);
        Assert.Null(json["alanDegerleri"]);
        Assert.Null(json["bilesenler"]);
        Assert.Equal(1, json["id"]!.GetValue<int>());
    }

    [Fact]
    public void K33_TamAlanIzinleri_SayisalEnumVeSifirDegeriDegistirmez()
    {
        var json = JsonNode.Parse("""{"tur":9,"birim":2,"adet":0,"netTutar":0,"fiyatlandirmaBirimi":2,"fiyatlandirmaMiktari":5} """)!;
        var original = json.ToJsonString();
        FinansAlanProjeksiyonu.Uygula(json, All);
        Assert.Equal(original, json.ToJsonString());
    }
}
