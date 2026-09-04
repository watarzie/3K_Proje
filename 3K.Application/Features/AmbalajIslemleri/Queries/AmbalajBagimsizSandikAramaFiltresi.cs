using _3K.Core.Entities;
using _3K.Core.Enums;

namespace _3K.Application.Features.AmbalajIslemleri.Queries;

/// <summary>
/// Özel sandık aramasını ilişkili proje ve kaynak kayıtları dahil olmak üzere
/// IQueryable üzerinde tutar. Böylece arama sayfalama yapılmadan önce veritabanında uygulanır.
/// </summary>
internal static class AmbalajBagimsizSandikAramaFiltresi
{
    public static IQueryable<AmbalajUretimKaydi> Uygula(
        IQueryable<AmbalajUretimKaydi> query,
        IQueryable<Proje> projeler,
        IQueryable<Sandik> kaynakSandiklar,
        IQueryable<AmbalajUretimKaydi> tumAmbalajKayitlari,
        string? aramaMetni)
    {
        if (string.IsNullOrWhiteSpace(aramaMetni))
            return query;

        var arama = aramaMetni.Trim().ToLower();
        var normalizeProjeNoArama = AmbalajProjeAramaFiltresi.ProjeNoAramasiniNormalizeEt(arama);
        var ahsapEslesir = "ahşap kapalı".Contains(arama);
        var kafesEslesir = "kafes sandık".Contains(arama);
        var kontrplakEslesir = "kontrplak sandık".Contains(arama);
        var katlanirEslesir = "katlanır sandık".Contains(arama);
        var digerCinsEslesir = "diğer".Contains(arama);
        var ilaveEslesir = "ilave".Contains(arama);
        var icEslesir = "iç sandık".Contains(arama);
        var sahaEslesir = "saha".Contains(arama);
        var yedekEslesir = "yedek".Contains(arama);

        return query.Where(k =>
            (ahsapEslesir && k.SandikCinsi == AmbalajSandikCinsi.AhsapKapali) ||
            (kafesEslesir && k.SandikCinsi == AmbalajSandikCinsi.Kafes) ||
            (kontrplakEslesir && k.SandikCinsi == AmbalajSandikCinsi.Kontrplak) ||
            (katlanirEslesir && k.SandikCinsi == AmbalajSandikCinsi.Katlanir) ||
            (digerCinsEslesir && k.SandikCinsi == AmbalajSandikCinsi.Diger) ||
            (ilaveEslesir && k.Tur == AmbalajSandikTuru.Ilave) ||
            (icEslesir && k.Tur == AmbalajSandikTuru.Ic) ||
            (sahaEslesir && k.Tur == AmbalajSandikTuru.Saha) ||
            (yedekEslesir && k.Tur == AmbalajSandikTuru.Yedek) ||
            k.SandikNo.ToLower().Contains(arama) ||
            (k.Ad != null && k.Ad.ToLower().Contains(arama)) ||
            (k.DigerSandikCinsi != null && k.DigerSandikCinsi.ToLower().Contains(arama)) ||
            (k.KullanimAmaci != null && k.KullanimAmaci.ToLower().Contains(arama)) ||
            (k.TalimatVeren != null && k.TalimatVeren.ToLower().Contains(arama)) ||
            (k.Aciklama != null && k.Aciklama.ToLower().Contains(arama)) ||
            projeler.Any(p => k.ProjeId.HasValue && p.Id == k.ProjeId.Value &&
                (p.ProjeNo.ToLower().Contains(arama) ||
                 (normalizeProjeNoArama.Length > 0 && p.ProjeNo
                     .Replace(" ", string.Empty)
                     .Replace("-", string.Empty)
                     .Replace("_", string.Empty)
                     .Replace("–", string.Empty)
                     .Replace("—", string.Empty)
                     .ToLower()
                     .Contains(normalizeProjeNoArama)) ||
                 p.Musteri.ToLower().Contains(arama) ||
                 (p.FBNo != null && p.FBNo.ToLower().Contains(arama)))) ||
            kaynakSandiklar.Any(s => k.KaynakKayitId.HasValue && s.Id == k.KaynakKayitId.Value &&
                (s.SandikNo.ToLower().Contains(arama) ||
                 (s.Ad != null && s.Ad.ToLower().Contains(arama)))) ||
            tumAmbalajKayitlari.Any(u => k.UstKayitId.HasValue && u.Id == k.UstKayitId.Value &&
                (u.SandikNo.ToLower().Contains(arama) ||
                 (u.Ad != null && u.Ad.ToLower().Contains(arama)))));
    }
}
