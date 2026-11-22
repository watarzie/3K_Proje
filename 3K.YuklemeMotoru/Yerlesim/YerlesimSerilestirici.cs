using System.Text.Json;
using System.Text.Json.Serialization;

namespace _3K.YuklemeMotoru.Yerlesim;

/// <summary>
/// Yerleşim sonucunun saklama biçimi (plan revizyonunda jsonb): camelCase alan ve enum adları. Sonuç hesaplandığı
/// andaki hâliyle saklanır; okurken yeniden hesaplanmaz.
/// </summary>
public static class YerlesimSerilestirici
{
    private static readonly JsonSerializerOptions Secenekler = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string Yaz(YerlesimSonucu sonuc) => JsonSerializer.Serialize(sonuc, Secenekler);

    /// <summary>Okunamayan JSON için null döner.</summary>
    public static YerlesimSonucu? Oku(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;
        try
        {
            return JsonSerializer.Deserialize<YerlesimSonucu>(json, Secenekler);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
