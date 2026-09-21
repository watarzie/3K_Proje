using System.Text.Json;
using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.AmbalajIslemleri;

internal static class AmbalajYasamDongusuYardimcisi
{
    public static AmbalajUretimFormuModel? KayitliForm(IUnitOfWork uow, int? kayitId, int? projeId,
        string? manuelProjeNo, IReadOnlyCollection<int>? kayitIds = null,
        _3K.Core.Enums.AmbalajSandikTuru? tur = null, bool? bagimsizKayitMi = null)
    {
        var forms = uow.GetRepository<AmbalajUretimFormuSurumu>().Queryable();
        if (projeId.HasValue) forms = forms.Where(x => x.ProjeId == projeId);
        if (!string.IsNullOrWhiteSpace(manuelProjeNo)) forms = forms.Where(x => x.KapsamAnahtari == "M:" + manuelProjeNo.Trim());
        var ids = kayitIds?.Distinct().ToArray() ?? (kayitId.HasValue ? [kayitId.Value] : []);
        if (ids.Length > 0)
        {
            var links = uow.GetRepository<AmbalajUretimFormuKaydi>().Queryable();
            forms = forms.Where(x => links.Count(l => l.FormSurumuId == x.Id && ids.Contains(l.AmbalajUretimKaydiId)) == ids.Length);
        }
        var entity = forms.OrderByDescending(x => x.CreatedDate).ThenByDescending(x => x.Surum).FirstOrDefault();
        if (entity == null) return null;
        var form = JsonSerializer.Deserialize<AmbalajUretimFormuModel>(entity.SnapshotJson)!;
        if (ids.Length > 0 || tur.HasValue || bagimsizKayitMi.HasValue)
        {
            form.Kalemler = form.Kalemler.Where(x => (ids.Length == 0 || ids.Contains(x.KayitId)) &&
                (!tur.HasValue || x.Tur == tur) && (!bagimsizKayitMi.HasValue || x.BagimsizKayitMi == bagimsizKayitMi)).ToList();
            if (form.Kalemler.Count == 0) return null;
            form.NetM3 = form.Kalemler.Any(x => x.NetM3.HasValue) ? form.Kalemler.Sum(x => x.NetM3) : null;
            form.SarfM3 = form.Kalemler.Any(x => x.SarfM3.HasValue) ? form.Kalemler.Sum(x => x.SarfM3) : null;
            form.ToplamM3 = form.Kalemler.Any(x => x.ToplamM3.HasValue) ? form.Kalemler.Sum(x => x.ToplamM3) : null;
        }
        return form;
    }
    public static async Task<AmbalajFormSurumuDto> FormDtoAsync(AmbalajUretimFormuSurumu entity,
        IRolService roles, ICurrentUserService user, CancellationToken ct)
    {
        var form = JsonSerializer.Deserialize<AmbalajUretimFormuModel>(entity.SnapshotJson)!;
        Maskele(form, await AmbalajYetkilendirmeYardimcisi.GorunumYetkileriniGetirAsync(roles, user, ct));
        return new(entity.Id, entity.FormKimligi, entity.Surum, entity.CreatedDate, entity.OlusturanKullaniciId, entity.Aciklama, form);
    }

    public static void Maskele(AmbalajUretimFormuModel form, AmbalajGorunumYetkileri izin)
    {
        if (!izin.M3Gorunur) form.NetM3 = null;
        if (!izin.SarfGorunur) form.SarfM3 = null;
        if (!izin.M3Gorunur || !izin.SarfGorunur) form.ToplamM3 = null;
        foreach (var k in form.Kalemler)
        {
            k.OlcuGorunur = izin.OlcuGorunur;
            if (!izin.OlcuGorunur)
            {
                k.IcOlculer = null!;
                k.DisOlculer = null!;
                k.Parcalar = [];
                k.OnDuvarYuksekligi = null;
            }
            if (!izin.M3Gorunur)
            {
                k.HesaplananNetM3 = k.NetM3 = k.M3Override = null;
                k.Parcalar = [];
            }
            if (!izin.SarfGorunur) k.SarfM3 = k.SarfOrani = null;
            if (!izin.M3Gorunur || !izin.SarfGorunur) k.ToplamM3 = null;
        }
    }

    public static async Task GerceklesmeyiKaydetAsync(IUnitOfWork uow, AmbalajUretimKaydi k, int userId)
    {
        var repo = uow.GetRepository<AmbalajUretimGerceklesmesi>();
        if (repo.Queryable().Any(x => x.AmbalajUretimKaydiId == k.Id)) return;
        var proje = k.ProjeId.HasValue ? await uow.GetRepository<Proje>().GetByIdAsync(k.ProjeId.Value) : null;
        var hesaplanir = AmbalajUretimPolitikasi.M3HesaplanabilirMi(k.SandikCinsi);
        await repo.AddAsync(new AmbalajUretimGerceklesmesi
        {
            GerceklesmeKimligi = Guid.NewGuid(), Surum = 1, AmbalajUretimKaydiId = k.Id,
            IsAkisKimligi = k.IsAkisKimligi, GerceklesmeTarihi = k.TamamlanmaTarihi ?? TurkeyTime.Now,
            ProjeId = k.ProjeId, ProjeNo = proje?.ProjeNo ?? k.ManuelProjeNo ?? string.Empty,
            ProjeAdi = proje?.Musteri ?? k.ManuelProjeAdi, SandikNo = k.SandikNo, SandikAdi = k.Ad,
            SandikCinsi = k.SandikCinsi, Tur = k.Tur, Adet = k.Adet, Boy = k.Boy, En = k.En, Yukseklik = k.Yukseklik,
            NetM3 = hesaplanir ? k.M3Override ?? k.HesaplananToplamM3 : null,
            SarfM3 = hesaplanir ? k.SarfM3 : null, FormulVersiyonu = k.M3HesaplamaVersiyonu, KullaniciId = userId
        });
    }
}
