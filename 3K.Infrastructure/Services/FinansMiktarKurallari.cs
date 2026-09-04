using _3K.Core.Enums;

namespace _3K.Infrastructure.Services
{
    internal readonly record struct FinansDagitimMiktari(decimal Adet, decimal M3);

    /// <summary>
    /// Sipariş ve fatura dağıtımlarında yalnız fiyatlandırma birimini esas miktar kabul eder.
    /// İkincil miktarı kaynak oranından türeterek adet ve m³'ün birbirinden kopmasını önler.
    /// </summary>
    internal static class FinansMiktarKurallari
    {
        internal const decimal Tolerance = 0.000001m;

        internal static FinansDagitimMiktari DagitimiNormalizeEt(
            FinansFiyatlandirmaBirimi fiyatlandirmaBirimi,
            decimal istenenAdet,
            decimal istenenM3,
            decimal kapasiteAdet,
            decimal kapasiteM3,
            decimal kullanilanAdet,
            decimal kullanilanM3,
            bool mevcutDagitimVar,
            string islemAdi)
        {
            if (istenenAdet < 0 || istenenM3 < 0 ||
                (istenenAdet <= Tolerance && istenenM3 <= Tolerance))
                throw new InvalidOperationException($"{islemAdi} miktarı sıfırdan büyük olmalıdır.");

            return fiyatlandirmaBirimi switch
            {
                FinansFiyatlandirmaBirimi.Adet => AdetDagitimi(
                    istenenAdet, kapasiteAdet, kapasiteM3, kullanilanAdet, islemAdi),
                FinansFiyatlandirmaBirimi.Metrekup => MetrekupDagitimi(
                    istenenM3, kapasiteAdet, kapasiteM3, kullanilanM3, islemAdi),
                FinansFiyatlandirmaBirimi.SabitTutar => SabitDagitim(
                    kapasiteAdet, kapasiteM3, mevcutDagitimVar, islemAdi),
                _ => throw new InvalidOperationException("Geçerli bir fiyatlandırma birimi bulunamadı.")
            };
        }

        internal static bool TamamiDagitildi(
            FinansFiyatlandirmaBirimi fiyatlandirmaBirimi,
            decimal kapasiteAdet,
            decimal kapasiteM3,
            decimal kullanilanAdet,
            decimal kullanilanM3,
            bool dagitimVar)
            => fiyatlandirmaBirimi switch
            {
                FinansFiyatlandirmaBirimi.Adet => kullanilanAdet + Tolerance >= kapasiteAdet,
                FinansFiyatlandirmaBirimi.Metrekup => kullanilanM3 + Tolerance >= kapasiteM3,
                FinansFiyatlandirmaBirimi.SabitTutar => dagitimVar,
                _ => kullanilanAdet + Tolerance >= kapasiteAdet && kullanilanM3 + Tolerance >= kapasiteM3
            };

        internal static bool DagitimVar(
            FinansFiyatlandirmaBirimi fiyatlandirmaBirimi,
            decimal adet,
            decimal m3,
            bool kayitVar)
            => fiyatlandirmaBirimi switch
            {
                FinansFiyatlandirmaBirimi.Adet => adet > Tolerance,
                FinansFiyatlandirmaBirimi.Metrekup => m3 > Tolerance,
                FinansFiyatlandirmaBirimi.SabitTutar => kayitVar,
                _ => adet > Tolerance || m3 > Tolerance
            };

        internal static bool KapasiteAsiliyor(
            FinansFiyatlandirmaBirimi fiyatlandirmaBirimi,
            decimal kapasiteAdet,
            decimal kapasiteM3,
            decimal kullanilanAdet,
            decimal kullanilanM3,
            int dagitimSayisi)
            => fiyatlandirmaBirimi switch
            {
                FinansFiyatlandirmaBirimi.Adet => kullanilanAdet > kapasiteAdet + Tolerance,
                FinansFiyatlandirmaBirimi.Metrekup => kullanilanM3 > kapasiteM3 + Tolerance,
                FinansFiyatlandirmaBirimi.SabitTutar => dagitimSayisi > 1,
                _ => kullanilanAdet > kapasiteAdet + Tolerance || kullanilanM3 > kapasiteM3 + Tolerance
            };

        internal static FinansDagitimMiktari DuzenliIsMiktari(
            FinansFiyatlandirmaBirimi fiyatlandirmaBirimi,
            decimal miktar)
        {
            if (miktar <= Tolerance)
                throw new InvalidOperationException("Düzenli iş miktarı sıfırdan büyük olmalıdır.");
            return fiyatlandirmaBirimi switch
            {
                FinansFiyatlandirmaBirimi.Metrekup => new FinansDagitimMiktari(1, miktar),
                FinansFiyatlandirmaBirimi.SabitTutar => new FinansDagitimMiktari(1, 0),
                _ => new FinansDagitimMiktari(miktar, 0)
            };
        }

        private static FinansDagitimMiktari AdetDagitimi(
            decimal istenenAdet,
            decimal kapasiteAdet,
            decimal kapasiteM3,
            decimal kullanilanAdet,
            string islemAdi)
        {
            if (istenenAdet <= Tolerance)
                throw new InvalidOperationException($"{islemAdi} adedi sıfırdan büyük olmalıdır.");
            if (kapasiteAdet <= Tolerance)
                throw new InvalidOperationException($"{islemAdi} için adet kapasitesi bulunamadı.");
            var kalan = Math.Max(0, kapasiteAdet - kullanilanAdet);
            if (istenenAdet > kalan + Tolerance)
                throw new InvalidOperationException($"{islemAdi} adedi kalanı aşamaz. Kalan: {kalan}.");
            var m3 = kapasiteM3 <= Tolerance
                ? 0
                : decimal.Round(kapasiteM3 * istenenAdet / kapasiteAdet, 6, MidpointRounding.AwayFromZero);
            return new FinansDagitimMiktari(istenenAdet, m3);
        }

        private static FinansDagitimMiktari MetrekupDagitimi(
            decimal istenenM3,
            decimal kapasiteAdet,
            decimal kapasiteM3,
            decimal kullanilanM3,
            string islemAdi)
        {
            if (istenenM3 <= Tolerance)
                throw new InvalidOperationException($"{islemAdi} m³ değeri sıfırdan büyük olmalıdır.");
            if (kapasiteM3 <= Tolerance)
                throw new InvalidOperationException($"{islemAdi} için m³ kapasitesi bulunamadı.");
            var kalan = Math.Max(0, kapasiteM3 - kullanilanM3);
            if (istenenM3 > kalan + Tolerance)
                throw new InvalidOperationException($"{islemAdi} m³ değeri kalanı aşamaz. Kalan: {kalan}.");
            var adet = kapasiteAdet <= Tolerance
                ? 0
                : decimal.Round(kapasiteAdet * istenenM3 / kapasiteM3, 6, MidpointRounding.AwayFromZero);
            return new FinansDagitimMiktari(adet, istenenM3);
        }

        private static FinansDagitimMiktari SabitDagitim(
            decimal kapasiteAdet,
            decimal kapasiteM3,
            bool mevcutDagitimVar,
            string islemAdi)
        {
            if (mevcutDagitimVar)
                throw new InvalidOperationException($"Sabit tutarlı kayıt {islemAdi.ToLowerInvariant()} için yalnız bir kez kullanılabilir.");
            return new FinansDagitimMiktari(kapasiteAdet, kapasiteM3);
        }
    }
}
