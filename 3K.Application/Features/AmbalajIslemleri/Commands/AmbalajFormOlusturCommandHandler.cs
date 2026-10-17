using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Core.Models;

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
        if (ids.Length + sources.Length is 0 or > AmbalajUretimFormuOlusturucu.EnFazlaKayit ||
            ids.Concat(sources).Any(id => id <= 0) || request.IdempotencyAnahtari == Guid.Empty)
            return Result<AmbalajFormSurumuDto>.Failure(
                $"1–{AmbalajUretimFormuOlusturucu.EnFazlaKayit} geçerli kayıt ve işlem kimliği gereklidir.");
        if (request.FinansTarihi.HasValue && !FinansTarihKurallari.GecerliTarih(request.FinansTarihi.Value))
            return Result<AmbalajFormSurumuDto>.Failure("Finans tarihi geçerli bir tarih olmalıdır.");
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(new { ids, sources, request.ProjeId, request.YenidenOlustur, Aciklama = request.Aciklama?.Trim(),
                FinansTarihi = request.FinansTarihi?.Date }))));
        var forms = uow.GetRepository<AmbalajUretimFormuSurumu>();
        var tekrar = forms.Queryable().FirstOrDefault(x => x.IdempotencyAnahtari == request.IdempotencyAnahtari);
        if (tekrar != null)
            return tekrar.IstekHash != hash
                ? Result<AmbalajFormSurumuDto>.Failure("İşlem kimliği farklı içerikle kullanıldı.", 409)
                : Result<AmbalajFormSurumuDto>.Success(await AmbalajYasamDongusuYardimcisi.FormDtoAsync(tekrar, roles, user, ct));

        // Senkronizasyon veya snapshot başarısız olursa önceki bütün değişiklikler geri alınır.
        return await uow.ExecuteAtomicAsync(async token =>
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
            // İzlenen sorgu: senkronizasyon bu kayıtları aynı DbContext'te izliyor olabilir; izlenmeyen ikinci
            // örnek Update ile eklenince EF aynı anahtarı iki kez izleyemez (plan kaydedilmeden form oluşturma).
            var kayitlar = repo.Queryable().Where(x => ids.Contains(x.Id)).ToList();
            // Özel sandıkta üretim formu yoktur; finans ilişkisi "Finansa aktar" ile kurulur.
            if (kayitlar.Any(k => k.BagimsizKayitMi))
                return Result<AmbalajFormSurumuDto>.Failure(
                    "Özel sandık için üretim formu oluşturulmaz; Özel Sandıklar listesinde \"Finansa aktar\" işlemini kullanın.", 409);
            // Sandık ölçüsü/tipi ilk formdan sonra değişmiş olabilir: otomatik senkronizasyon üretimdeki kilitli
            // kaydı ezmediği için form, tamamlanmamış kaynak kayıtları güncel sandıktan alır.
            var tazeleme = await KaynaktanTazeleAsync(kayitlar, user.IslemKullaniciId.Value, token);
            if (!tazeleme.IsSuccess) return Result<AmbalajFormSurumuDto>.Failure(tazeleme.Error!.Message, tazeleme.StatusCode);
            // Seçim ve snapshot doğrulanamazsa tazeleme dahil bütün değişiklikler geri alınır.
            var sonuc = await AmbalajUretimFormuOlusturucu.OlusturAsync(uow, ids, token);
            if (!sonuc.IsSuccess) return Result<AmbalajFormSurumuDto>.Failure(sonuc.Error!.Message, sonuc.StatusCode);
            var kapsam = kayitlar[0].ProjeId.HasValue ? $"P:{kayitlar[0].ProjeId}" : $"M:{kayitlar[0].ManuelProjeNo?.Trim()}";
            var onceki = forms.Queryable().Where(x => x.KapsamAnahtari == kapsam).OrderByDescending(x => x.Surum).FirstOrDefault();
            var formKayitIds = uow.GetRepository<AmbalajUretimFormuKaydi>().Queryable()
                .Where(x => ids.Contains(x.AmbalajUretimKaydiId)).Select(x => x.AmbalajUretimKaydiId).ToHashSet();
            if (!request.YenidenOlustur && formKayitIds.Count > 0)
                return Result<AmbalajFormSurumuDto>.Failure("Seçimde formu bulunan kayıt var. Yeniden oluşturma işlemini kullanın.", 409);
            // A2/D11: üretim formu işlemleri gerekçe istemez; yeniden oluşturma güncel veriyle yapılır.
            if (request.YenidenOlustur && onceki == null)
                return Result<AmbalajFormSurumuDto>.Failure("Yeniden oluşturma için projede mevcut üretim formu gereklidir.");
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
                // A6: formda girilen tarih finans kaleminin varsayılan Finans Tarihi olur.
                if (request.FinansTarihi.HasValue)
                    kayit.FinansTarihi = request.FinansTarihi.Value.Date;
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

    /// <summary>
    /// Seçimdeki tamamlanmamış kaynak kayıtları güncel sandıktan tazeler ve aynı transaction'da kaydeder; form
    /// hesabı kayıtları veritabanından okur. Kaynakta ölçüsü eksik sandık veya PO geçmişi olan finans kaleminde
    /// fiyat eşleşmesini (ad, cins, ölçü) değiştiren tazeleme 409 ile, hiçbir kayıt değiştirilmeden reddedilir.
    /// Kaynağı silinmiş ya da başka projeye taşınmış sandığın kaydı bilinen son değerleriyle kalır.
    /// </summary>
    private async Task<Result> KaynaktanTazeleAsync(IReadOnlyList<AmbalajUretimKaydi> kayitlar, int kullaniciId,
        CancellationToken ct)
    {
        var adaylar = kayitlar.Where(k => k.FormdaKaynaktanTazelenirMi() && k.ProjeId.HasValue).ToList();
        if (adaylar.Count == 0) return Result.Success();
        var sandikIdleri = adaylar.Select(k => k.KaynakKayitId!.Value).Distinct().ToList();
        var sandiklar = uow.GetRepository<Sandik>().Queryable().Where(s => sandikIdleri.Contains(s.Id)).ToDictionary(s => s.Id);
        var projeIdleri = adaylar.Select(k => k.ProjeId!.Value).Distinct().ToList();
        var projeler = uow.GetRepository<Proje>().Queryable().Where(p => projeIdleri.Contains(p.Id)).ToDictionary(p => p.Id);
        var eslesmeler = adaylar
            .Where(k => projeler.ContainsKey(k.ProjeId!.Value) &&
                        sandiklar.TryGetValue(k.KaynakKayitId!.Value, out var kaynakSandik) && kaynakSandik.ProjeId == k.ProjeId)
            .Select(k => (Kayit: k, Sandik: sandiklar[k.KaynakKayitId!.Value], Proje: projeler[k.ProjeId!.Value]))
            .ToList();

        var olcusuEksik = eslesmeler.Where(x =>
            {
                var kaynak = AmbalajUretimPolitikasi.KaynakDegerleri(x.Sandik);
                return !AmbalajUretimYardimcilari.OlculerGecerli(true, kaynak.Cins,
                    AmbalajUretimYardimcilari.SandikAdediHesapla(x.Sandik.SandikNo), kaynak.Boy, kaynak.En, kaynak.Yukseklik);
            })
            .Select(x => x.Sandik.SandikNo).ToList();
        if (olcusuEksik.Count > 0)
            return Result.Failure(
                $"{string.Join(", ", olcusuEksik)} numaralı sandığın ölçüleri eksik olduğu için üretim formu oluşturulamadı.", 409);

        // Finans servisi PO geçmişi olan kalemde fiyat eşleşmesi değişikliğini reddeder; kullanıcıya sandık
        // numarasıyla, hiçbir kayıt değiştirilmeden burada söylenir.
        var eslesmesiDegisenler = eslesmeler
            .Where(x => AmbalajUretimPolitikasi.KaynakFiyatEslesmesiniDegistirirMi(x.Kayit, x.Sandik)).ToList();
        if (eslesmesiDegisenler.Count > 0)
        {
            var akisIdleri = eslesmesiDegisenler.Select(x => x.Kayit.IsAkisKimligi.ToString("D")).ToList();
            var poGecmisliAkislar = uow.GetRepository<FinansIsKaydi>().Queryable()
                .Where(f => f.KaynakTuru.ToUpper() == FinansKaynakTurleri.AmbalajUretim &&
                            akisIdleri.Contains(f.KaynakKayitId!) && f.SiparisKalemleri.Any())
                .Select(f => f.KaynakKayitId!)
                .ToHashSet();
            var poluSandiklar = eslesmesiDegisenler
                .Where(x => poGecmisliAkislar.Contains(x.Kayit.IsAkisKimligi.ToString("D")))
                .Select(x => x.Kayit.SandikNo).ToList();
            if (poluSandiklar.Count > 0)
                return Result.Failure(
                    $"{string.Join(", ", poluSandiklar)} numaralı sandığın ölçü, tip veya adı kaynak sandıkta değişti; " +
                    "finans kaleminin PO geçmişi olduğu için fiyat eşleşmesi korunur ve üretim formu güncel değerlerle " +
                    "oluşturulamadı. Önce Finans'ta ilgili PO/fatura kayıtlarını uzlaştırın.", 409);
        }

        var repo = uow.GetRepository<AmbalajUretimKaydi>();
        var tazelenen = 0;
        foreach (var (kayit, sandik, proje) in eslesmeler)
        {
            var eski = AmbalajUretimYardimcilari.Snapshot(kayit);
            var eskiSenkronizasyonTarihi = kayit.KaynakSonSenkronizasyonTarihi;
            AmbalajKaynakSenkronizasyonService.FormIcinKaynaktanTazele(kayit, proje, sandik);
            var degismedi = AmbalajUretimYardimcilari.Snapshot(kayit)
                .Where(x => x.Key != nameof(AmbalajUretimKaydi.KaynakSonSenkronizasyonTarihi))
                .All(x => eski.TryGetValue(x.Key, out var eskiDeger) && eskiDeger == x.Value);
            if (degismedi)
            {
                // Değişiklik yoksa kayıt ve geçmiş yazılmaz; son senkronizasyon tarihi de yerinde kalır.
                kayit.KaynakSonSenkronizasyonTarihi = eskiSenkronizasyonTarihi;
                continue;
            }
            repo.Update(kayit);
            await AmbalajUretimYardimcilari.AlanHareketleriniEkleAsync(uow, kayit, eski,
                "Kaynak sandık değişiklikleri üretim formuna alındı", kullaniciId);
            tazelenen++;
        }
        if (tazelenen > 0) await uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}
