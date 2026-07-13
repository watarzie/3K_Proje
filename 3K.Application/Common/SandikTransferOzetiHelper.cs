using System.Globalization;
using _3K.Core.Entities;

namespace _3K.Application.Common
{
    /// <summary>
    /// Yapılandırılmış sandık transfer kayıtlarını kullanıcıya gösterilecek kısa özete dönüştürür.
    /// Hesaplama yalnızca read-model katmanında yapılır; hareket defteri değiştirilmez.
    /// </summary>
    public static class SandikTransferOzetiHelper
    {
        public static SandikTransferOzeti Hesapla(
            IEnumerable<SandikUrunTransferi> transferler,
            int sandikId)
        {
            var liste = transferler.ToList();
            var girisler = liste
                .Where(t => t.HedefSandikId == sandikId)
                .GroupBy(t => t.KaynakSandikNo)
                .Select(g => new TransferYonOzeti(g.Key, g.Sum(t => t.Miktar), true));
            var cikislar = liste
                .Where(t => t.KaynakSandikId == sandikId)
                .GroupBy(t => t.HedefSandikNo)
                .Select(g => new TransferYonOzeti(g.Key, g.Sum(t => t.Miktar), false));

            var yonler = girisler
                .Concat(cikislar)
                .OrderByDescending(x => x.GirisMi)
                .ThenBy(x => x.DigerSandikNo, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return new SandikTransferOzeti(
                yonler.Where(x => x.GirisMi).Sum(x => x.Miktar),
                yonler.Where(x => !x.GirisMi).Sum(x => x.Miktar),
                string.Join(" · ", yonler.Select(Formatla)));
        }

        private static string Formatla(TransferYonOzeti hareket)
        {
            var miktar = hareket.Miktar.ToString("0.####", CultureInfo.GetCultureInfo("tr-TR"));
            return hareket.GirisMi
                ? $"{miktar} adet ← Sandık {hareket.DigerSandikNo}"
                : $"{miktar} adet → Sandık {hareket.DigerSandikNo}";
        }

        private sealed record TransferYonOzeti(string DigerSandikNo, decimal Miktar, bool GirisMi);
    }

    public sealed record SandikTransferOzeti(decimal Giris, decimal Cikis, string Metin);
}
