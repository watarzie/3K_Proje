using _3K.Core.Enums;

namespace _3K.Application.Common
{
    /// <summary>
    /// ID bazlı durum sabitleri. Tüm iş mantığı Enum üzerinden yürür.
    /// Bu sınıf geriye dönük uyumluluk ve kısa erişim sağlar.
    /// </summary>
    public static class StatusConstants
    {
        public const int ActionQueuedForApproval = 202;

        /// <summary>
        /// Kullanıcı rol string'leri — JWT claim'lerde kullanılır.
        /// </summary>
        public static class KullaniciRol
        {
            public const string Admin = "Admin";
            public const string Personel3K = "Personel3K";
            public const string PersonelGrid = "PersonelGrid";
            public const string Yonetici = "Yonetici";
        }
    }
}
