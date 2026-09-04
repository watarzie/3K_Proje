using _3K.Core.Entities;

namespace _3K.Application.Features.AmbalajIslemleri.Queries
{
    /// <summary>
    /// Proje listesindeki serbest metin aramasını veritabanı tarafında uygular.
    /// Proje numarası için kullanıcıdan PA öneki, boşluk veya tire biçimini
    /// birebir girmesini beklemez; müşteri ve FB araması ise özgün metinle sürer.
    /// </summary>
    public static class AmbalajProjeAramaFiltresi
    {
        public static IQueryable<Proje> Uygula(IQueryable<Proje> query, string? aramaMetni)
        {
            if (string.IsNullOrWhiteSpace(aramaMetni))
                return query;

            // Müşteri/FB aramasının mevcut kültür davranışını değiştirmiyoruz.
            var arama = aramaMetni.Trim().ToLower();
            var normalizeProjeNoArama = ProjeNoAramasiniNormalizeEt(arama);

            // Yalnız ayraç girilmişse Contains("") ile bütün projelerin dönmesini engelle.
            if (normalizeProjeNoArama.Length == 0)
            {
                return query.Where(p =>
                    p.ProjeNo.ToLower().Contains(arama) ||
                    p.Musteri.ToLower().Contains(arama) ||
                    (p.FBNo != null && p.FBNo.ToLower().Contains(arama)));
            }

            return query.Where(p =>
                p.ProjeNo.ToLower().Contains(arama) ||
                p.ProjeNo
                    .Replace(" ", string.Empty)
                    .Replace("-", string.Empty)
                    .Replace("_", string.Empty)
                    .Replace("–", string.Empty)
                    .Replace("—", string.Empty)
                    .ToLower()
                    .Contains(normalizeProjeNoArama) ||
                p.Musteri.ToLower().Contains(arama) ||
                (p.FBNo != null && p.FBNo.ToLower().Contains(arama)));
        }

        internal static string ProjeNoAramasiniNormalizeEt(string value) =>
            string.Concat(value.Where(character =>
                !char.IsWhiteSpace(character) && character is not '-' and not '_' and not '–' and not '—'));
    }
}
