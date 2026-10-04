using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace _3K.Application.Behaviors
{
    /// <summary>
    /// Başarısız isteğin girdisini loga yazılacak, maskeli tek satır JSON'a çevirir. Parola, token, anahtar ve
    /// doğrulama kodu alanları (iç içe nesnelerde de) "***" olur; e-posta alanı kısmen gizlenir; dosya akışı okunmaz,
    /// ham bayt yazılmaz, yalnız boyutları yazılır; uzun metin, uzun liste ve toplam uzunluk kısaltılır. Yetki
    /// bildiren (Required…) alanlar isteğin girdisi olmadığı için atlanır. Log yazımı isteği hiçbir koşulda bozmaz.
    /// </summary>
    internal static class IstekGirdisi
    {
        internal const string Maske = "***";
        private const int AzamiMetin = 200;
        private const int AzamiListe = 20;
        private const int AzamiToplam = 2000;

        // Adında geçtiğinde gizlenen parçalar (Sifre, YeniSifre, ChallengeToken, RefreshToken, ApiAnahtari…).
        private static readonly string[] GizliParcalar = ["sifre", "şifre", "password", "parola", "token", "secret", "apikey", "anahtar"];

        // Adında geçtiğinde e-posta sayılan parçalar; değer kısmen gizlenir (bkz. EpostayiMaskele).
        private static readonly string[] EpostaParcalari = ["mail", "eposta"];

        // Tam adıyla gizlenen doğrulama kodları; "Kod" parçası barkod/proje kodu gibi alanlarda da geçtiği için tam eşleşir.
        private static readonly HashSet<string> GizliAdlar = new(StringComparer.OrdinalIgnoreCase)
        {
            "kod", "code", "kurtarmaKodu", "recoveryCode", "otp", "totp",
        };

        private static readonly JsonSerializerOptions Ayarlar = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            // Yalnız log dosyasına yazılır; Türkçe karakterler \u kaçışına çevrilmesin.
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            MaxDepth = 16,
        };

        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> Alanlar = new();

        internal static string Yaz(object istek)
        {
            try
            {
                var kok = new JsonObject();
                foreach (var alan in Alanlar.GetOrAdd(istek.GetType(), AlanlariBul))
                    kok[JsonNamingPolicy.CamelCase.ConvertName(alan.Name)] = GizliMi(alan.Name) ? Maske : Deger(alan, istek);

                Temizle(kok);
                var metin = kok.ToJsonString(Ayarlar);
                return metin.Length > AzamiToplam ? metin[..AzamiToplam] + "…" : metin;
            }
            catch (Exception)
            {
                // Girdi yazılamasa da hata satırı yazılmalı; asıl yanıtı etkilememeli.
                return "<girdi yazılamadı>";
            }
        }

        private static PropertyInfo[] AlanlariBul(Type tip) => tip
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0 &&
                        !p.Name.StartsWith("Required", StringComparison.Ordinal) &&
                        p.Name != "PermissionMatch")
            .ToArray();

        private static bool GizliMi(string ad) =>
            GizliAdlar.Contains(ad) || GizliParcalar.Any(parca => ad.Contains(parca, StringComparison.OrdinalIgnoreCase));

        private static JsonNode? Deger(PropertyInfo alan, object istek)
        {
            var deger = alan.GetValue(istek);
            return deger switch
            {
                null => null,
                Stream akis => akis.CanSeek ? $"<dosya akışı, {akis.Length} bayt>" : "<dosya akışı>",
                byte[] baytlar => $"<{baytlar.Length} bayt>",
                _ => Dugum(deger),
            };
        }

        private static JsonNode? Dugum(object deger)
        {
            try
            {
                return JsonSerializer.SerializeToNode(deger, deger.GetType(), Ayarlar);
            }
            catch (Exception exception) when (exception is NotSupportedException or InvalidOperationException or JsonException)
            {
                // Ör. iç içe bir model dosya akışı taşıyorsa; alan atlanır, diğerleri yazılır.
                return "<yazılamadı>";
            }
        }

        private static void Temizle(JsonNode? dugum)
        {
            switch (dugum)
            {
                case JsonObject nesne:
                    foreach (var ad in nesne.Select(x => x.Key).ToList())
                    {
                        var deger = nesne[ad];
                        if (deger is null) continue;
                        if (GizliMi(ad)) nesne[ad] = Maske;
                        else if (EpostaMi(ad) && deger is JsonValue eposta && eposta.TryGetValue<string>(out var adres))
                            nesne[ad] = EpostayiMaskele(adres);
                        else if (KisaltilacakMetin(deger) is { } kisa) nesne[ad] = kisa;
                        else Temizle(deger);
                    }
                    break;

                case JsonArray dizi:
                    var fazla = dizi.Count - AzamiListe;
                    while (dizi.Count > AzamiListe) dizi.RemoveAt(dizi.Count - 1);
                    for (var i = 0; i < dizi.Count; i++)
                    {
                        if (KisaltilacakMetin(dizi[i]) is { } kisa) dizi[i] = kisa;
                        else Temizle(dizi[i]);
                    }
                    if (fazla > 0) dizi.Add($"…(+{fazla})");
                    break;
            }
        }

        private static bool EpostaMi(string ad) =>
            EpostaParcalari.Any(parca => ad.Contains(parca, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Kim olduğu anlaşılacak kadarını bırakır: "deneme@gmail.com" → "de***@gmail.com". Değer e-posta biçiminde
        /// değilse tamamen gizlenir; kullanıcının şifresini yanlışlıkla e-posta kutusuna yazdığı durumda şifre loga düşmez.
        /// </summary>
        internal static string EpostayiMaskele(string deger)
        {
            var metin = deger.Trim();
            var at = metin.IndexOf('@');
            var bicimli = at > 0 && at == metin.LastIndexOf('@') && at < metin.Length - 1 &&
                          metin.IndexOf('.', at + 1) > at + 1 && !metin.EndsWith('.') && !metin.Any(char.IsWhiteSpace);
            if (!bicimli) return Maske;
            // Yerel kısmın en fazla ilk iki harfi, hiçbir zaman tamamı görünmez.
            var gorunen = Math.Min(2, at - 1);
            return metin[..gorunen] + Maske + metin[at..];
        }

        private static string? KisaltilacakMetin(JsonNode? dugum) =>
            dugum is JsonValue deger && deger.TryGetValue<string>(out var metin) && metin.Length > AzamiMetin
                ? metin[..AzamiMetin] + "…"
                : null;
    }
}
