namespace _3K.Core.Common
{
    /// <summary>
    /// Raporlardaki metin ve rakam parçalarını doğal sırada karşılaştırır.
    /// Rakamlar sayısal tipe çevrilmez; çok uzun sandık numaraları taşma üretmez.
    /// </summary>
    public sealed class SandikNumarasiComparer : IComparer<string?>
    {
        public static SandikNumarasiComparer Instance { get; } = new();

        private SandikNumarasiComparer() { }

        public int Compare(string? x, string? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            var a = x?.Trim() ?? string.Empty;
            var b = y?.Trim() ?? string.Empty;
            if (a.Length == 0 || b.Length == 0)
                return a.Length == b.Length ? StringComparer.Ordinal.Compare(x, y) : a.Length == 0 ? 1 : -1;

            var ai = 0;
            var bi = 0;
            while (ai < a.Length && bi < b.Length)
            {
                if (RakamMi(a[ai]) && RakamMi(b[bi]))
                {
                    var aEnd = ai;
                    var bEnd = bi;
                    while (aEnd < a.Length && RakamMi(a[aEnd])) aEnd++;
                    while (bEnd < b.Length && RakamMi(b[bEnd])) bEnd++;
                    while (ai < aEnd && a[ai] == '0') ai++;
                    while (bi < bEnd && b[bi] == '0') bi++;
                    var result = (aEnd - ai).CompareTo(bEnd - bi);
                    if (result != 0) return result;
                    result = a.AsSpan(ai, aEnd - ai).SequenceCompareTo(b.AsSpan(bi, bEnd - bi));
                    if (result != 0) return result;
                    ai = aEnd;
                    bi = bEnd;
                }
                else
                {
                    var result = char.ToUpperInvariant(a[ai]).CompareTo(char.ToUpperInvariant(b[bi]));
                    if (result != 0) return result;
                    ai++;
                    bi++;
                }
            }

            var remaining = (a.Length - ai).CompareTo(b.Length - bi);
            // Eşit doğal anahtarda sıfır önekleri/büyük-küçük harfler için kültürden bağımsız bağlayıcı.
            return remaining != 0 ? remaining : StringComparer.Ordinal.Compare(x, y);
        }

        private static bool RakamMi(char value) => value is >= '0' and <= '9';
    }
}
