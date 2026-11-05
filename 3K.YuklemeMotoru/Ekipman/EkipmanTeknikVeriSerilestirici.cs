using System.Text.Json;
using System.Text.Json.Serialization;

namespace _3K.YuklemeMotoru.Ekipman;

/// <summary>
/// Teknik verinin JSON biçimi: camelCase alan ve enum adları, boş alan yazılmaz. Aynı veri her zaman aynı metni
/// üretir; içe aktarım "değişti mi" kararını bu metinle verir.
/// </summary>
public static class EkipmanTeknikVeriSerilestirici
{
    private static readonly JsonSerializerOptions Secenekler = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string Yaz(EkipmanTeknikVerisi veri) => JsonSerializer.Serialize(veri, Secenekler);

    /// <summary>Okunamayan veya şema dışı JSON için null döner.</summary>
    public static EkipmanTeknikVerisi? Oku(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<EkipmanTeknikVerisi>(json, Secenekler);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
