using _3K.Core.Entities;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.AmbalajIslemleri;

/// <summary>
/// Kaynak sandığın çekiden mi geldiğini (<see cref="AmbalajUretimPolitikasi.CekiDisiEklenenSandikMi"/>) belirlemek
/// için projelerin çeki satırlarındaki sandık numaralarını okur. Senkronizasyon ve okuma tarafı aynı sorguyu kullanır.
/// </summary>
internal static class AmbalajCekiSandiklari
{
    internal sealed class CekiSandikNoSatiri
    {
        public int ProjeId { get; init; }
        public string SandikNo { get; init; } = string.Empty;
    }

    /// <summary>
    /// Verilen çekilerin satırlarında geçen (proje, sandık no) çiftleri. Elle eklenen ürün satırı (Grid/sandık
    /// manuel ürün ekleme) çeki verisi değildir, sayılmaz.
    /// </summary>
    internal static IQueryable<CekiSandikNoSatiri> Sorgula(IQueryable<Ceki> cekiler, IQueryable<CekiSatiri> satirlar) =>
        (from ceki in cekiler
         join satir in satirlar on ceki.Id equals satir.CekiId
         where !satir.IsManuelEklenen
         select new CekiSandikNoSatiri { ProjeId = ceki.ProjeId, SandikNo = satir.CekideGecenSandikNo })
        .Distinct();

    internal static IReadOnlyDictionary<int, IReadOnlySet<string>> ProjeyeGoreGrupla(IEnumerable<CekiSandikNoSatiri> satirlar) =>
        satirlar
            .GroupBy(x => x.ProjeId)
            .ToDictionary(g => g.Key, g => AmbalajUretimPolitikasi.CekiSandikNolari(g.Select(x => x.SandikNo)));

    internal static IReadOnlySet<string> ProjeIcinGetir(IUnitOfWork unitOfWork, int projeId) =>
        AmbalajUretimPolitikasi.CekiSandikNolari(Sorgula(
                unitOfWork.GetRepository<Ceki>().Queryable().Where(c => c.ProjeId == projeId),
                unitOfWork.GetRepository<CekiSatiri>().Queryable())
            .Select(x => x.SandikNo)
            .ToList());
}
