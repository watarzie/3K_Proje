using System.Text.Json.Nodes;

namespace _3K.Core.Models;

/// <summary>Sahte sıfır yerine JSON null üretir; özet/grafik/alt satır ve audit aynı kurala tabidir.</summary>
public static class FinansAlanProjeksiyonu
{
    private static readonly HashSet<string> Fiyat = new(StringComparer.OrdinalIgnoreCase)
    { "birimFiyat", "birimFiyatSnapshot", "guncelBirimFiyat", "varsayilanBirimFiyat", "netBirimFiyat" };
    private static readonly HashSet<string> Tutar = new(StringComparer.OrdinalIgnoreCase)
    { "netTutar", "kdvTutari", "toplamTutar", "tutar", "matrah", "kdvOrani", "guncelKdvOrani", "varsayilanKdvOrani", "belgeNetTutar", "belgeKdvTutari", "belgeToplamTutar", "mutabakatFarki", "siparisNetTutar", "faturalananNetTutar", "kalanSiparisNetTutar", "kalanFaturaNetTutar", "kalanNetTutar", "isBedeli", "siparisToplamTutar", "faturalananToplamTutar", "siparisBekleyen", "faturaBekleyen", "siparisAcik", "faturalanan", "toplam", "manuelNetTutar", "finansOzeti", "grupToplamlari", "toplamlar", "tutarlar", "bilesenler", "alanDegerleri" };
    private static readonly HashSet<string> Gelir = new(StringComparer.OrdinalIgnoreCase)
    { "gelir", "gelirler", "gelirToplamlari", "yilGelir", "faturalananTutarlar", "siparisBekleyenTutarlar", "siparisAcikTutarlar" };
    private static readonly HashSet<string> Gider = new(StringComparer.OrdinalIgnoreCase)
    { "gider", "giderler", "giderToplamlari", "yilGider", "buAyGider", "buAyGiderler", "giderTurleri" };
    private static readonly HashSet<string> Kar = new(StringComparer.OrdinalIgnoreCase)
    { "kar", "tahminiKar", "karOrani", "fark", "net", "netler", "netToplamlari" };
    private static readonly HashSet<string> Olcu = new(StringComparer.OrdinalIgnoreCase) { "boy", "en", "yukseklik" };
    private static readonly HashSet<string> M3 = new(StringComparer.OrdinalIgnoreCase)
    { "birimM3", "toplamM3", "m3", "siparisM3", "siparisBekleyenM3", "faturalananM3", "kalanM3", "siparisAcikM3", "faturaBekleyenM3" };

    public static void Uygula(JsonNode? node, AlanErisimYetkileri permissions, bool audit = false)
    {
        if (node is JsonArray array) { foreach (var item in array) Uygula(item, permissions, audit); return; }
        if (node is not JsonObject obj) return;
        var fullMoney = permissions.ParasalVeri && permissions.BirimFiyat && permissions.Tutar && permissions.Gelir && permissions.Gider && permissions.Karlilik;
        var expense = Metin(obj["tur"]) == "Gider" || obj.ContainsKey("kategoriId");
        var volumeQuantity = Metin(obj["birim"]) is "m³" or "m3" || Metrekup(obj["yontem"]);
        var volumePricing = Metrekup(obj["fiyatlandirmaBirimi"]);
        var sarf = Metin(obj["kaynakBileseni"]) == "SARF" || Metin(obj["isTuru"]) == "SarfKereste" ||
            obj["isTuru"] is JsonValue type && type.TryGetValue<int>(out var kind) && kind == 9;
        var countObject = obj.ContainsKey("toplamIs") || obj.ContainsKey("siparisTam") || obj.ContainsKey("toplamProje");
        foreach (var key in obj.Select(x => x.Key).ToArray())
        {
            var monetaryField = Tutar.Contains(key) && !(countObject && key is "siparisBekleyen" or "faturaBekleyen" or "siparisAcik" or "faturalanan");
            var hidden = Fiyat.Contains(key) && !permissions.BirimFiyat || monetaryField && !permissions.Tutar ||
                         Gelir.Contains(key) && !permissions.Gelir || Gider.Contains(key) && !permissions.Gider || Kar.Contains(key) && !permissions.Karlilik ||
                         Olcu.Contains(key) && !permissions.Olcu || M3.Contains(key) && !permissions.UretimM3 ||
                         (key.Equals("miktar", StringComparison.OrdinalIgnoreCase) && volumeQuantity && !permissions.UretimM3) ||
                         (key is "siparisMiktari" or "faturalananMiktar" && volumeQuantity && !permissions.UretimM3) ||
                         (sarf && !permissions.Sarf && (M3.Contains(key) || key is "miktar" or "siparisMiktari" or "faturalananMiktar" or "fiyatlandirmaMiktari")) ||
                         (key.Equals("fiyatlandirmaMiktari", StringComparison.OrdinalIgnoreCase) && volumePricing && !permissions.UretimM3) ||
                         (expense && (Fiyat.Contains(key) || Tutar.Contains(key)) && !permissions.Gider) ||
                         (audit && (key is "eskiDeger" or "yeniDeger" or "aciklama" or "referans") && (!fullMoney || !permissions.Olcu || !permissions.UretimM3 || !permissions.Sarf)) ||
                         (!permissions.ParasalVeri && (Fiyat.Contains(key) || monetaryField || Gelir.Contains(key) || Gider.Contains(key) || Kar.Contains(key))) ||
                         (key is "bilesenler" or "alanDegerleri" && (!fullMoney || !permissions.Olcu || !permissions.UretimM3 || !permissions.Sarf));
            if (hidden) obj[key] = null;
            else Uygula(obj[key], permissions, audit);
        }
    }

    private static string? Metin(JsonNode? value) => value is JsonValue json && json.TryGetValue<string>(out var text) ? text : null;
    private static bool Metrekup(JsonNode? value) => Metin(value) is "Metrekup" or "M3" or "m3" or "2" ||
        value is JsonValue json && json.TryGetValue<int>(out var number) && number == 2;
}
