namespace _3K.Core.Helpers
{
    /// <summary>
    /// Merkezi Türkiye saat dilimi yardımcısı (UTC+3).
    /// Tüm CreatedDate, UpdatedDate, Tarih vb. alanlar bu sınıf üzerinden alınmalıdır.
    /// </summary>
    public static class TurkeyTime
    {
        private static readonly TimeZoneInfo _turkeyZone =
            TimeZoneInfo.FindSystemTimeZoneById(
                OperatingSystem.IsWindows() ? "Turkey Standard Time" : "Europe/Istanbul");

        /// <summary>
        /// Şu anki Türkiye saati.
        /// </summary>
        public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _turkeyZone);
    }
}
