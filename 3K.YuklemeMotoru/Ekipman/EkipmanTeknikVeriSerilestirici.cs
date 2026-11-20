using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace _3K.YuklemeMotoru.Ekipman;

/// <summary>
/// Katı okumanın sonucu: veri ya da okunamayan yerin JSON yolu (ör. "yuzeyler[0].uzunlukMm"; kökün kendisi için boş).
/// </summary>
public sealed record EkipmanTeknikVeriOkumasi(EkipmanTeknikVerisi? Veri, string? HataYolu);

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

    // Dışarıdan gelen (ekrandan girilen) veri için: bilinmeyen alan, yinelenen alan, null liste/metin ve alt kayıtlardaki
    // eksik zorunlu alan okuma hatasıdır. Saklanan veriyi okuyan Oku gevşek kalır.
    private static readonly JsonSerializerOptions KatiSecenekler = new(JsonSerializerDefaults.Web)
    {
        // Hazır enum dönüştürücüsü virgüllü metni bit birleşimi olarak okur ("travers, kapak" tanımsız 7 olur, "arka, sol"
        // sessizce "sol" olur); katı okumada enum yalnız tanımlı tek adıyla kabul edilir.
        Converters = { new TekAdliEnum<AciklikTarafi>(), new TekAdliEnum<EngelTuru>(), new TekAdliEnum<DingilTuru>() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectNullableAnnotations = true,
        AllowDuplicateProperties = false,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver { Modifiers = { ZorunluAlanlariIsaretle } }
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

    /// <summary>
    /// Dışarıdan gelen JSON'u katı okur: bilinmeyen ya da yinelenen alan, yanlış tür ve alt kayıtta (yüzey, açıklık, engel,
    /// dingil, bağlama noktası, dış ölçü, king-pin) eksik zorunlu alan kabul edilmez; eksik sayı sıfır sayılmaz. Kökteki
    /// alanların hepsi isteğe bağlıdır (boş = bilinmiyor).
    /// </summary>
    public static EkipmanTeknikVeriOkumasi KatiOku(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<EkipmanTeknikVerisi>(json, KatiSecenekler) is { } veri
                ? new EkipmanTeknikVeriOkumasi(veri, null)
                : new EkipmanTeknikVeriOkumasi(null, string.Empty);
        }
        catch (JsonException ex)
        {
            return new EkipmanTeknikVeriOkumasi(null, Yol(ex.Path));
        }
    }

    /// <summary>
    /// Alt listeleri kanonik sıraya dizer: satırların giriş sırası değişti diye farklı metin (ve yeni revizyon) çıkmaz.
    /// Excel içe aktarımı ve ekrandan kayıt aynı sırayı kullanır.
    /// </summary>
    public static EkipmanTeknikVerisi KanonikSira(EkipmanTeknikVerisi veri) => veri with
    {
        Yuzeyler = veri.Yuzeyler.OrderBy(s => s.BaslangicXMm).ThenBy(s => s.Kod, StringComparer.Ordinal).ToList(),
        Acikliklar = veri.Acikliklar.OrderBy(s => s.Taraf).ThenBy(s => s.BaslangicXMm).ToList(),
        Engeller = veri.Engeller.OrderBy(s => s.XMm).ThenBy(s => s.YMm).ThenBy(s => s.ZMm).ThenBy(s => s.Tur).ToList(),
        Dingiller = veri.Dingiller.OrderBy(s => s.XMm).ToList(),
        BaglamaNoktalari = veri.BaglamaNoktalari.OrderBy(s => s.XMm).ThenBy(s => s.YMm).ThenBy(s => s.ZMm).ToList()
    };

    /// <summary>
    /// Alt kayıtlarda boş bırakılamayan alanlar: tamsayı, enum ve boş olamayan metin (Excel şablonunun zorunlu sütunları).
    /// Kök nesnede hiçbir alan zorunlu değildir; "sokulebilir" gibi mantıksal alanlar verilmezse hayır sayılır.
    /// </summary>
    private static void ZorunluAlanlariIsaretle(JsonTypeInfo tip)
    {
        if (tip.Kind != JsonTypeInfoKind.Object || tip.Type == typeof(EkipmanTeknikVerisi))
            return;

        foreach (var alan in tip.Properties)
        {
            var tur = alan.PropertyType;
            if (tur == typeof(int) || tur.IsEnum || (tur == typeof(string) && !alan.IsSetNullable))
                alan.IsRequired = true;
        }
    }

    /// <summary>
    /// Enum'u yalnız tanımlı tek adıyla (yazımdaki camelCase ad; harf büyüklüğü önemsiz) okur: sayı, sayı metni, virgüllü
    /// birleşim ve tanımsız ad okuma hatasıdır.
    /// </summary>
    private sealed class TekAdliEnum<T> : JsonConverter<T> where T : struct, Enum
    {
        private static readonly Dictionary<string, T> Adlar = Enum.GetValues<T>()
            .ToDictionary(Ad, deger => deger, StringComparer.OrdinalIgnoreCase);

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            reader.TokenType == JsonTokenType.String && reader.GetString() is { } ad && Adlar.TryGetValue(ad, out var deger)
                ? deger
                : throw new JsonException();

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) => writer.WriteStringValue(Ad(value));

        private static string Ad(T deger) => JsonNamingPolicy.CamelCase.ConvertName(deger.ToString());
    }

    /// <summary>"$.yuzeyler[0].kod" → "yuzeyler[0].kod"; kök ya da bilinmeyen yol için boş metin.</summary>
    private static string Yol(string? jsonYolu)
    {
        if (string.IsNullOrEmpty(jsonYolu) || jsonYolu == "$")
            return string.Empty;
        return jsonYolu.StartsWith("$.", StringComparison.Ordinal) ? jsonYolu[2..] : jsonYolu;
    }
}
