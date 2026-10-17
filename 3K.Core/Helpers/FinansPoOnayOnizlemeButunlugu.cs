using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using _3K.Core.Models;

namespace _3K.Core.Helpers
{
    /// <summary>
    /// Onaya sunulan PO öncesi iş onayı görüntüsünün kanonik özeti. Görüntü jsonb'de saklanırken sayıların ölçeği ve
    /// alan sırası değişebileceği için özet, sayıları sondaki sıfırlardan arındırılmış metne çevirerek üretilir.
    /// </summary>
    public static class FinansPoOnayOnizlemeButunlugu
    {
        public const int Surum = 1;

        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        public static string HashOlustur(FinansPoOnayOnizleme onizleme)
        {
            ArgumentNullException.ThrowIfNull(onizleme);

            var canonical = new
            {
                onizleme.Surum,
                onizleme.ProjeId,
                onizleme.ProjeNo,
                onizleme.Musteri,
                IsTuru = (int)onizleme.IsTuru,
                Kategori = (int)onizleme.Kategori,
                onizleme.ParaBirimi,
                onizleme.KalemSayisi,
                NetTutar = Sayi(onizleme.NetTutar),
                KdvTutari = Sayi(onizleme.KdvTutari),
                ToplamTutar = Sayi(onizleme.ToplamTutar),
                ToplamM3 = Sayi(onizleme.ToplamM3),
                AhsapM3 = Sayi(onizleme.AhsapM3),
                KontraM3 = Sayi(onizleme.KontraM3),
                IlkFinansTarihi = Tarih(onizleme.IlkFinansTarihi),
                SonFinansTarihi = Tarih(onizleme.SonFinansTarihi),
                Kalemler = (onizleme.Kalemler ?? Array.Empty<FinansPoOnayKalemi>())
                    .Select(kalem => new
                    {
                        kalem.IsKaydiId,
                        kalem.IsAdi,
                        kalem.SandikNo,
                        kalem.SandikAdi,
                        kalem.SandikTipi,
                        Boy = Sayi(kalem.Boy),
                        En = Sayi(kalem.En),
                        Yukseklik = Sayi(kalem.Yukseklik),
                        Adet = Sayi(kalem.Adet),
                        kalem.Birim,
                        ToplamM3 = Sayi(kalem.ToplamM3),
                        AhsapM3 = Sayi(kalem.AhsapM3),
                        KontraM3 = Sayi(kalem.KontraM3),
                        FiyatlandirmaBirimi = (int)kalem.FiyatlandirmaBirimi,
                        BirimFiyat = Sayi(kalem.BirimFiyat),
                        AhsapBirimFiyat = Sayi(kalem.AhsapBirimFiyat),
                        KdvOrani = Sayi(kalem.KdvOrani),
                        NetTutar = Sayi(kalem.NetTutar),
                        SiparisNetTutar = Sayi(kalem.SiparisNetTutar),
                        kalem.ParaBirimi,
                        UretimTarihi = Tarih(kalem.UretimTarihi),
                        FinansTarihi = Tarih(kalem.FinansTarihi)
                    })
                    .ToArray()
            };

            var canonicalBytes = JsonSerializer.SerializeToUtf8Bytes(canonical, SerializerOptions);
            return Convert.ToHexString(SHA256.HashData(canonicalBytes)).ToLowerInvariant();
        }

        public static bool HashDogrula(FinansPoOnayOnizleme onizleme, string? beklenenHash)
        {
            if (string.IsNullOrWhiteSpace(beklenenHash))
                return false;

            var beklenenBytes = Encoding.ASCII.GetBytes(beklenenHash.Trim().ToLowerInvariant());
            var guncelBytes = Encoding.ASCII.GetBytes(HashOlustur(onizleme));

            return beklenenBytes.Length == guncelBytes.Length &&
                   CryptographicOperations.FixedTimeEquals(beklenenBytes, guncelBytes);
        }

        private static string? Sayi(decimal? deger)
            => deger?.ToString("0.############################", CultureInfo.InvariantCulture);

        private static string Tarih(DateTime deger)
            => deger.ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture);
    }
}
