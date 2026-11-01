namespace _3K.Core.Models
{
    /// <summary>
    /// Sunucuda sayfalanmış liste: ön yüzdeki <c>app-server-pager</c> ile aynı alanlar. Satırlar veritabanında süzülüp
    /// sıralanır ve yalnız istenen sayfa okunur (<see cref="SayfaKurali"/>).
    /// </summary>
    public sealed class SayfaliSonuc<T>
    {
        public IReadOnlyList<T> Items { get; init; } = [];
        public int PageNumber { get; init; }
        public int PageSize { get; init; }
        public int TotalCount { get; init; }
        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }

    /// <summary>
    /// Sayfa ve boyut kuralı: boyut 1–<see cref="EnFazlaBoyut"/> arasına çekilir (boşsa <see cref="VarsayilanBoyut"/>);
    /// sayfa 1'den küçük olamaz, son sayfadan büyükse son sayfa döner.
    /// </summary>
    public static class SayfaKurali
    {
        public const int VarsayilanBoyut = 25;
        public const int EnFazlaBoyut = 100;

        public static int Boyut(int? boyut) => boyut is > 0 ? Math.Min(boyut.Value, EnFazlaBoyut) : VarsayilanBoyut;

        /// <summary>Toplam satıra göre geçerli sayfa (en az 1, en çok son sayfa).</summary>
        public static int Sayfa(int? sayfa, int boyut, int toplam)
        {
            var sonSayfa = Math.Max(1, (int)Math.Ceiling(toplam / (double)boyut));
            return Math.Clamp(sayfa ?? 1, 1, sonSayfa);
        }

        public static int Atlanan(int sayfa, int boyut) => (sayfa - 1) * boyut;
    }
}
