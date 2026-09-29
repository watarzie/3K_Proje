using System.Text.Json;
using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.AmbalajIslemleri;

internal static class AmbalajYasamDongusuYardimcisi
{
    /// <summary>
    /// Projenin üretim formu oluşturulmuş aktif ve ambalaja dahil sandıklarından, istenen tür ve bağımsızlık
    /// durumundakilerin formunu güncel veriden yeniden hesaplar (D10: form sürümü tutulmaz; "tüm üretim
    /// formlarını indir" her seferinde güncel veriyi gösterir). Formu olan sandık yoksa null döner.
    /// </summary>
    public static async Task<Result<AmbalajUretimFormuModel>?> GuncelFormAsync(IUnitOfWork uow, int projeId,
        _3K.Core.Enums.AmbalajSandikTuru? tur, bool bagimsizKayitMi, CancellationToken cancellationToken)
    {
        var formluKayitlar = uow.GetRepository<AmbalajUretimFormuKaydi>().Queryable()
            .Select(x => x.AmbalajUretimKaydiId);
        var ids = uow.GetRepository<AmbalajUretimKaydi>().Queryable()
            .Where(k => k.ProjeId == projeId && !k.IptalMi && k.AmbalajaDahil && k.BagimsizKayitMi == bagimsizKayitMi &&
                        (!tur.HasValue || k.Tur == tur) && formluKayitlar.Contains(k.Id))
            .Select(k => k.Id)
            .ToList();
        if (ids.Count == 0) return null;
        return await AmbalajUretimFormuOlusturucu.OlusturAsync(uow, ids, cancellationToken);
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
