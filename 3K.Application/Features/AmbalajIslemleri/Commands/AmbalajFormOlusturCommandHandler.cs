using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Application.Features.AmbalajIslemleri.Queries;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.AmbalajIslemleri.Commands;

public sealed class AmbalajFormOlusturCommandHandler(IUnitOfWork uow, ICurrentUserService user,
    IRolService roles, IFinansUretimAktarimService finans, IAmbalajKaynakSenkronizasyonService? sync = null,
    IAmbalajFormIslemKilidi? islemKilidi = null)
    : IRequestHandler<AmbalajFormOlusturCommand, Result<AmbalajFormSurumuDto>>
{
    public async Task<Result<AmbalajFormSurumuDto>> Handle(AmbalajFormOlusturCommand request, CancellationToken ct)
    {
        if (!user.IslemKullaniciId.HasValue) return Result<AmbalajFormSurumuDto>.Failure("Oturum gereklidir.", 401);
        var ids = request.KayitIdleri.Distinct().Order().ToArray();
        var sources = request.KaynakSandikIdleri.Distinct().Order().ToArray();
        if (ids.Length + sources.Length is 0 or > 500 || ids.Concat(sources).Any(id => id <= 0) || request.IdempotencyAnahtari == Guid.Empty)
            return Result<AmbalajFormSurumuDto>.Failure("1–500 geçerli kayıt ve işlem kimliği gereklidir.");
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(new { ids, sources, request.ProjeId, request.YenidenOlustur, Aciklama = request.Aciklama?.Trim() }))));
        var forms = uow.GetRepository<AmbalajUretimFormuSurumu>();
        var tekrar = forms.Queryable().FirstOrDefault(x => x.IdempotencyAnahtari == request.IdempotencyAnahtari);
        if (tekrar != null)
            return tekrar.IstekHash != hash
                ? Result<AmbalajFormSurumuDto>.Failure("İşlem kimliği farklı içerikle kullanıldı.", 409)
                : Result<AmbalajFormSurumuDto>.Success(await AmbalajYasamDongusuYardimcisi.FormDtoAsync(tekrar, roles, user, ct));

        return await uow.ExecuteInTransactionAsync(async token =>
        {
            if (islemKilidi != null) await islemKilidi.KilitleAsync(token);
            var kilitSonrasiTekrar = forms.Queryable().FirstOrDefault(x => x.IdempotencyAnahtari == request.IdempotencyAnahtari);
            if (kilitSonrasiTekrar != null)
                return kilitSonrasiTekrar.IstekHash != hash
                    ? Result<AmbalajFormSurumuDto>.Failure("İşlem kimliği farklı içerikle kullanıldı.", 409)
                    : Result<AmbalajFormSurumuDto>.Success(await AmbalajYasamDongusuYardimcisi.FormDtoAsync(kilitSonrasiTekrar, roles, user, token));
            var repo = uow.GetRepository<AmbalajUretimKaydi>();
            if (sources.Length > 0)
            {
                if (!request.ProjeId.HasValue || sync == null || uow.GetRepository<Sandik>().Queryable().Count(s => sources.Contains(s.Id) && s.ProjeId == request.ProjeId) != sources.Length)
                    return Result<AmbalajFormSurumuDto>.Failure("Kaynak sandıklar belirtilen projeye ait değil.", 409);
                var synced = await sync.SenkronizeEtAsync(request.ProjeId.Value, user, token);
                if (!synced.IsSuccess) return Result<AmbalajFormSurumuDto>.Failure(synced.Error!.Message, synced.StatusCode);
                ids = ids.Concat(repo.Queryable().Where(k => k.KaynakKayitId.HasValue && sources.Contains(k.KaynakKayitId.Value))
                    .Select(k => k.Id).ToList()).Distinct().Order().ToArray();
            }
            var kayitlar = (await repo.FindAsync(x => ids.Contains(x.Id))).ToList();
            // Bütün seçim ve snapshot doğrulanmadan hiçbir kayıt değiştirilmez.
            var sonuc = await AmbalajUretimFormuOlusturucu.OlusturAsync(uow, null, null,
                cancellationToken: token, kayitIdleri: ids, ilkForm: true);
            if (!sonuc.IsSuccess) return Result<AmbalajFormSurumuDto>.Failure(sonuc.Error!.Message, sonuc.StatusCode);
            var kapsam = kayitlar[0].ProjeId.HasValue ? $"P:{kayitlar[0].ProjeId}" : $"M:{kayitlar[0].ManuelProjeNo?.Trim()}";
            var onceki = forms.Queryable().Where(x => x.KapsamAnahtari == kapsam).OrderByDescending(x => x.Surum).FirstOrDefault();
            var formKayitIds = uow.GetRepository<AmbalajUretimFormuKaydi>().Queryable()
                .Where(x => ids.Contains(x.AmbalajUretimKaydiId)).Select(x => x.AmbalajUretimKaydiId).ToHashSet();
            if (!request.YenidenOlustur && formKayitIds.Count > 0)
                return Result<AmbalajFormSurumuDto>.Failure("Seçimde formu bulunan kayıt var. Yeniden oluşturma işlemini kullanın.", 409);
            if (request.YenidenOlustur && (onceki == null || string.IsNullOrWhiteSpace(request.Aciklama)))
                return Result<AmbalajFormSurumuDto>.Failure("Yeniden oluşturma için mevcut form ve gerekçe gereklidir.");
            var proje = kayitlar[0].ProjeId.HasValue ? await uow.GetRepository<Proje>().GetByIdAsync(kayitlar[0].ProjeId!.Value) : null;
            foreach (var kayit in kayitlar)
            {
                var eski = AmbalajUretimYardimcilari.Snapshot(kayit);
                if (!formKayitIds.Contains(kayit.Id) && kayit.UretimDurumu == AmbalajUretimDurumu.Planlandi)
                {
                    kayit.UretimeAlindi = true;
                    kayit.UretimDurumu = AmbalajUretimDurumu.Uretimde;
                    kayit.UretimTarihi ??= TurkeyTime.Now;
                }
                // Tamamlanmış satırda da xmin karşılaştırılır; paralel form sürümleri çatışır.
                kayit.UpdatedDate = TurkeyTime.Now;
                kayit.KaynakSenkronizasyonuKilitliMi = true;
                repo.Update(kayit);
                await AmbalajUretimYardimcilari.AlanHareketleriniEkleAsync(uow, kayit, eski,
                    "Üretim formu oluşturuldu", user.IslemKullaniciId.Value, request.Aciklama);
            }
            var form = new AmbalajUretimFormuSurumu
            {
                FormKimligi = onceki?.FormKimligi ?? Guid.NewGuid(), Surum = (onceki?.Surum ?? 0) + 1,
                IdempotencyAnahtari = request.IdempotencyAnahtari, IstekHash = hash,
                KapsamAnahtari = kapsam, ProjeId = kayitlar[0].ProjeId,
                SnapshotJson = JsonSerializer.Serialize(sonuc.Value),
                OlusturanKullaniciId = user.IslemKullaniciId.Value, Aciklama = request.Aciklama?.Trim(),
                CreatedDate = TurkeyTime.Now, CreatedBy = user.IslemKullaniciId.Value.ToString(),
                Kayitlar = ids.Select(id => new AmbalajUretimFormuKaydi { AmbalajUretimKaydiId = id }).ToList()
            };
            await forms.AddAsync(form);
            await uow.SaveChangesAsync(token);
            await finans.UretimKayitlariniAktarAsync(kayitlar.Select(k => AmbalajFinansSenkronizasyonu.ModelOlustur(k, proje)).ToList(), token);
            return Result<AmbalajFormSurumuDto>.Success(await AmbalajYasamDongusuYardimcisi.FormDtoAsync(form, roles, user, token));
        }, ct);
    }
}
