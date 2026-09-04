using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.AmbalajIslemleri.Queries
{
    internal static class AmbalajSorguYardimcisi
    {
        public static IQueryable<AmbalajUretimKaydi> Filtrele(
            IQueryable<AmbalajUretimKaydi> query,
            IAmbalajRaporFiltresi filtre)
        {
            if (!filtre.IptallerDahil)
                query = query.Where(k => !k.IptalMi);
            if (filtre.ProjeId.HasValue)
                query = query.Where(k => k.ProjeId == filtre.ProjeId.Value);
            if (!string.IsNullOrWhiteSpace(filtre.ManuelProjeNo))
            {
                var projeNo = filtre.ManuelProjeNo.Trim().ToLower();
                query = query.Where(k => k.ProjeId == null && k.ManuelProjeNo != null &&
                                         k.ManuelProjeNo.ToLower().Contains(projeNo));
            }
            if (filtre.Tur.HasValue)
                query = query.Where(k => k.Tur == filtre.Tur.Value);
            if (filtre.KaynakModul.HasValue)
                query = query.Where(k => k.KaynakModul == filtre.KaynakModul.Value);
            if (filtre.SandikCinsi.HasValue)
                query = query.Where(k => k.SandikCinsi == filtre.SandikCinsi.Value);
            if (filtre.UretimDurumu.HasValue)
                query = query.Where(k => k.UretimDurumu == filtre.UretimDurumu.Value);
            if (filtre.BaslangicTarihi.HasValue)
                query = query.Where(k => k.UretimTarihi >= filtre.BaslangicTarihi.Value);
            if (filtre.BitisTarihi.HasValue)
            {
                var bitisExclusive = filtre.BitisTarihi.Value.Date.AddDays(1);
                query = query.Where(k => k.UretimTarihi < bitisExclusive);
            }
            if (filtre.Yil.HasValue)
                query = query.Where(k => k.UretimTarihi.HasValue && k.UretimTarihi.Value.Year == filtre.Yil.Value);
            if (filtre.Ay.HasValue)
                query = query.Where(k => k.UretimTarihi.HasValue && k.UretimTarihi.Value.Month == filtre.Ay.Value);
            if (!string.IsNullOrWhiteSpace(filtre.TalepEdenKisi))
            {
                var kisi = filtre.TalepEdenKisi.Trim().ToLower();
                query = query.Where(k => k.TalepEdenKisi != null && k.TalepEdenKisi.ToLower().Contains(kisi));
            }
            if (!string.IsNullOrWhiteSpace(filtre.TalepEdenBolum))
            {
                var bolum = filtre.TalepEdenBolum.Trim().ToLower();
                query = query.Where(k => k.TalepEdenBolum != null && k.TalepEdenBolum.ToLower().Contains(bolum));
            }
            if (!string.IsNullOrWhiteSpace(filtre.TalimatVeren))
            {
                var talimatVeren = filtre.TalimatVeren.Trim().ToLower();
                query = query.Where(k => k.TalimatVeren != null && k.TalimatVeren.ToLower().Contains(talimatVeren));
            }
            if (!string.IsNullOrWhiteSpace(filtre.FirinPartiNo))
            {
                var firinPartiNo = filtre.FirinPartiNo.Trim().ToLower();
                query = query.Where(k => k.FirinPartiNo != null && k.FirinPartiNo.ToLower().Contains(firinPartiNo));
            }
            if (filtre.AmbalajaDahil.HasValue)
                query = query.Where(k => k.AmbalajaDahil == filtre.AmbalajaDahil.Value);
            if (filtre.UretimeAlindi.HasValue)
                query = query.Where(k => k.UretimeAlindi == filtre.UretimeAlindi.Value);
            if (filtre.OzelSandiklar.HasValue)
                query = filtre.OzelSandiklar.Value
                    ? query.Where(k => k.Tur != _3K.Core.Enums.AmbalajSandikTuru.Normal)
                    : query.Where(k => k.Tur == _3K.Core.Enums.AmbalajSandikTuru.Normal);
            if (!string.IsNullOrWhiteSpace(filtre.Arama))
            {
                var arama = filtre.Arama.Trim().ToLower();
                query = query.Where(k =>
                    k.SandikNo.ToLower().Contains(arama) ||
                    (k.Ad != null && k.Ad.ToLower().Contains(arama)) ||
                    (k.ManuelProjeNo != null && k.ManuelProjeNo.ToLower().Contains(arama)) ||
                    (k.ManuelProjeAdi != null && k.ManuelProjeAdi.ToLower().Contains(arama)) ||
                    (k.TalepEdenKisi != null && k.TalepEdenKisi.ToLower().Contains(arama)) ||
                    (k.TalepEdenBolum != null && k.TalepEdenBolum.ToLower().Contains(arama)));
            }
            return query;
        }

        public static IReadOnlyList<AmbalajUretimKaydiDto> DtolariOlustur(
            IUnitOfWork unitOfWork,
            IReadOnlyList<AmbalajUretimKaydi> kayitlar)
        {
            var projeIds = kayitlar.Where(k => k.ProjeId.HasValue).Select(k => k.ProjeId!.Value).Distinct().ToList();
            var projeler = projeIds.Count == 0
                ? new Dictionary<int, Proje>()
                : unitOfWork.GetRepository<Proje>().Queryable()
                    .Where(p => projeIds.Contains(p.Id))
                    .ToList()
                    .ToDictionary(p => p.Id);
            var ustIds = kayitlar.Where(k => k.UstKayitId.HasValue).Select(k => k.UstKayitId!.Value).Distinct().ToList();
            var ustler = ustIds.Count == 0
                ? new Dictionary<int, string>()
                : unitOfWork.GetRepository<AmbalajUretimKaydi>().Queryable()
                    .Where(k => ustIds.Contains(k.Id))
                    .Select(k => new { k.Id, k.SandikNo })
                    .ToList()
                    .ToDictionary(k => k.Id, k => k.SandikNo);

            return kayitlar.Select(k =>
            {
                projeler.TryGetValue(k.ProjeId ?? 0, out var proje);
                ustler.TryGetValue(k.UstKayitId ?? 0, out var ustSandikNo);
                return AmbalajUretimYardimcilari.DtoOlustur(k, proje?.ProjeNo, proje?.Musteri, ustSandikNo);
            }).ToList();
        }
    }
}
