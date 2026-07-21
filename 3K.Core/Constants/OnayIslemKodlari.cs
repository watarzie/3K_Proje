using _3K.Core.Enums;

namespace _3K.Core.Constants
{
    public static class OnayIslemKodlari
    {
        public sealed record OnayIslemTanim(string IslemKodu, string IslemAdi);

        public const string Genel = "GENEL";
        public const string UcKProjedenKarsilandi = "UCK_PROJEDEN_KARSILANDI";
        public const string UcKStoktanKarsilandi = "UCK_STOKTAN_KARSILANDI";
        public const string UcKTedarikcidenGeldi = "UCK_TEDARIKCIDEN_GELDI";
        public const string SandikKilidiAc = "SANDIK_KILIDI_AC";
        public const string ProjeKilidiAc = "PROJE_KILIDI_AC";
        public const string CekiRevizyonuUygula = "CEKI_REVIZYONU_UYGULA";
        public const string SandikLokasyonGuncelle = "SANDIK_LOKASYON_GUNCELLE";

        public static string FromUcKDurumId(int lookupUcKDurumId)
        {
            return lookupUcKDurumId switch
            {
                (int)UcKDurum.ProjedenKarsilandi => UcKProjedenKarsilandi,
                (int)UcKDurum.StoktanKarsilandi => UcKStoktanKarsilandi,
                (int)UcKDurum.TedarikcidenGeldi => UcKTedarikcidenGeldi,
                _ => $"UCK_DURUM_{lookupUcKDurumId}"
            };
        }

        public static IReadOnlyList<OnayIslemTanim> SabitOnayIslemleri { get; } =
        [
            new(Genel, "Genel İşlem"),
            new(SandikKilidiAc, "Sandık Kilidi Açma"),
            new(ProjeKilidiAc, "Proje Kilidi Açma")
        ];

        /// <summary>
        /// Onay zorunluluğu yönetim ekranından açılıp kapatılabilen operasyonlar.
        /// Yeni bir ayarlanabilir operasyon eklendiğinde güvenli varsayılan true'dur.
        /// </summary>
        public static IReadOnlyList<OnayIslemTanim> AyarlanabilirOnayIslemleri { get; } =
        [
            new(CekiRevizyonuUygula, "Çeki Revizyonu Uygulama"),
            new(SandikLokasyonGuncelle, "Manuel Sandık Lokasyonu Atama")
        ];

        public static bool AyarlanabilirMi(string? islemKodu) =>
            !string.IsNullOrWhiteSpace(islemKodu) &&
            AyarlanabilirOnayIslemleri.Any(tanim =>
                string.Equals(tanim.IslemKodu, islemKodu.Trim(), StringComparison.Ordinal));

        public static string DisplayName(string? islemKodu)
        {
            return islemKodu switch
            {
                UcKProjedenKarsilandi => "Projeden Karşılandı",
                UcKStoktanKarsilandi => "Stoktan Karşılandı",
                UcKTedarikcidenGeldi => "Tedarikçiden Geldi",
                SandikKilidiAc => "Sandık Kilidi Açma",
                ProjeKilidiAc => "Proje Kilidi Açma",
                CekiRevizyonuUygula => "Çeki Revizyonu Uygulama",
                SandikLokasyonGuncelle => "Manuel Sandık Lokasyonu Atama",
                Genel => "Genel İşlem",
                _ when !string.IsNullOrWhiteSpace(islemKodu) => islemKodu,
                _ => "Genel İşlem"
            };
        }
    }

    public static class OnayReferansTipleri
    {
        public const string CekiSatiri = "CekiSatiri";
        public const string Proje = "Proje";
        public const string Sandik = "Sandik";
        public const string CekiRevizyonTalebi = "CekiRevizyonTalebi";
    }
}
