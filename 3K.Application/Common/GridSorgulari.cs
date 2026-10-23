using System.Linq.Expressions;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Models;

namespace _3K.Application.Common;

/// <summary>
/// Grid listelerinin ortak sorgu yapı taşları. <see cref="GriddeBekleyenIfadesi"/>,
/// <see cref="GridSahaBekleyenKurali.BekleyenMiktar"/> kuralının SQL karşılığıdır (filtre, sıralama ve projeksiyonda
/// veritabanında hesaplanır); eşdeğerlik <c>GridSahaTests.SqlIfadeleri_RastgeleSatirlardaSafKuralaEsittir</c> ile sabitlenir. Sevk kilidi dışlaması Grid iş
/// listesi ile Grid Sahası görünümünün ortak kuralıdır.
/// </summary>
public static class GridSorgulari
{
    // Statik ifadeler bildirim sırasıyla kurulur ve birbirine bağlıdır (yolda → bekleyen → pozitif); sırayı değiştirme.
    /// <summary>
    /// <see cref="GridSahaBekleyenKurali.YoldakiMiktar"/> SQL karşılığı: Sevk Edildi ve erken sonuçlandırılmamış
    /// partide parti miktarı − karşılanan (sayaçsız partide yalnız 3K'da Tam/Fazla Geldi ile parti kadar teslim
    /// alınmışsa parti), en az 0; diğer durumlarda 0.
    /// </summary>
    public static readonly Expression<Func<CekiSatiri, decimal>> YoldakiMiktarIfadesi = cs =>
        cs.GridSevkDurumuId == (int)GridSevkDurum.SevkEdildi && cs.AktifGridSevkPartisiErkenSonuclandirildiMi != true
            ? ((cs.GridSevkMiktari ?? 0m) -
               (cs.AktifGridSevkKarsilananMiktari ??
                ((cs.UcKDurumuId == (int)UcKDurum.TamGeldi || cs.UcKDurumuId == (int)UcKDurum.FazlaGeldi) &&
                 cs.GelenMiktar == cs.GridSevkMiktari ? cs.GridSevkMiktari ?? 0m : 0m)) > 0m
                ? (cs.GridSevkMiktari ?? 0m) -
                  (cs.AktifGridSevkKarsilananMiktari ??
                   ((cs.UcKDurumuId == (int)UcKDurum.TamGeldi || cs.UcKDurumuId == (int)UcKDurum.FazlaGeldi) &&
                    cs.GelenMiktar == cs.GridSevkMiktari ? cs.GridSevkMiktari ?? 0m : 0m))
                : 0m)
            : 0m;

    /// <summary><see cref="GridSahaBekleyenKurali.BekleyenMiktar"/> SQL karşılığı: Grid gelen − 3K'ya ulaşan − yolda, en az 0.</summary>
    public static readonly Expression<Func<CekiSatiri, decimal>> GriddeBekleyenIfadesi = BekleyenIfadesiniKur();

    /// <summary>Bekleyen miktarı pozitif satırlar (<see cref="GriddeBekleyenIfadesi"/> &gt; 0).</summary>
    public static readonly Expression<Func<CekiSatiri, bool>> BekleyenPozitifIfadesi = PozitifIfadesiniKur();

    /// <summary>Grid'e fiziksel gelmiş durumlar (<see cref="GridSahaBekleyenKurali.GelenDurumlar"/>: Tam Geldi, Eksik Geldi).</summary>
    public static readonly Expression<Func<CekiSatiri, bool>> GridGelmisDurumIfadesi = cs =>
        cs.GridDurumuId == (int)GridDurum.TamGeldi || cs.GridDurumuId == (int)GridDurum.EksikGeldi;

    /// <summary>
    /// Sevk kilidi dışlaması (Grid iş listesi ile ortak): tamamen sevk edilmiş projenin satırları yalnız açık
    /// sevkiyat düzeltmesi varsa; sevk edilmiş ve düzeltmesi kapalı sandıktaki satırlar (tahsisle ya da sandık
    /// numarasıyla eşleşen) hiç listelenmez.
    /// </summary>
    public static IQueryable<CekiSatiri> SevkKilidiDisinda(IQueryable<CekiSatiri> sorgu) => sorgu
        .Where(cs =>
            cs.Ceki.Proje.DurumId != (int)ProjeDurum.SevkEdildi ||
            cs.SandikIcerikleri.Any(si => si.Sandik.SevkiyatDuzeltmeAcikMi) ||
            cs.Ceki.Proje.Sandiklar.Any(s =>
                s.SevkiyatDuzeltmeAcikMi &&
                s.SandikNo == (string.IsNullOrWhiteSpace(cs.FiiliSandikNo)
                    ? cs.CekideGecenSandikNo
                    : cs.FiiliSandikNo)))
        .Where(cs => !cs.SandikIcerikleri.Any(si =>
            si.Sandik.DurumId == (int)SandikDurum.Sevkedildi &&
            !si.Sandik.SevkiyatDuzeltmeAcikMi))
        .Where(cs => !cs.Ceki.Proje.Sandiklar.Any(s =>
            s.SandikNo == (string.IsNullOrWhiteSpace(cs.FiiliSandikNo)
                ? cs.CekideGecenSandikNo
                : cs.FiiliSandikNo) &&
            s.DurumId == (int)SandikDurum.Sevkedildi &&
            !s.SevkiyatDuzeltmeAcikMi));

    /// <summary>Projeksiyona Grid'de bekleyen miktarı ekler: seçici satırı ve <see cref="GriddeBekleyenIfadesi"/> değerini alır.</summary>
    public static Expression<Func<CekiSatiri, T>> BekleyenIle<T>(Expression<Func<CekiSatiri, decimal, T>> secici)
    {
        var satir = secici.Parameters[0];
        var bekleyen = new ParametreDegistirici(GriddeBekleyenIfadesi.Parameters[0], satir).Visit(GriddeBekleyenIfadesi.Body);
        var govde = new ParametreDegistirici(secici.Parameters[1], bekleyen).Visit(secici.Body);
        return Expression.Lambda<Func<CekiSatiri, T>>(govde, satir);
    }

    private static Expression<Func<CekiSatiri, decimal>> BekleyenIfadesiniKur()
    {
        var satir = Expression.Parameter(typeof(CekiSatiri), "cs");
        var yolda = new ParametreDegistirici(YoldakiMiktarIfadesi.Parameters[0], satir).Visit(YoldakiMiktarIfadesi.Body);
        var fark = Expression.Subtract(
            Expression.Subtract(
                Expression.Property(satir, nameof(CekiSatiri.GridGelenAdet)),
                Expression.Property(satir, nameof(CekiSatiri.GelenMiktar))),
            yolda);
        var sifir = Expression.Constant(0m);
        return Expression.Lambda<Func<CekiSatiri, decimal>>(
            Expression.Condition(Expression.GreaterThan(fark, sifir), fark, sifir), satir);
    }

    private static Expression<Func<CekiSatiri, bool>> PozitifIfadesiniKur()
    {
        var satir = GriddeBekleyenIfadesi.Parameters[0];
        return Expression.Lambda<Func<CekiSatiri, bool>>(
            Expression.GreaterThan(GriddeBekleyenIfadesi.Body, Expression.Constant(0m)), satir);
    }

    private sealed class ParametreDegistirici(ParameterExpression eski, Expression yeni) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => node == eski ? yeni : base.VisitParameter(node);
    }
}
