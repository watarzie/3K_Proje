using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri;
using _3K.Application.Features.AmbalajIslemleri.Commands;
using _3K.Application.Features.AmbalajIslemleri.Queries;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Behaviors;

/// <summary>Eski ve yeni yazma yollarının kayda bağlı kritik geçişlerini aynı transaction'da korur.</summary>
public sealed class AmbalajYasamDongusuBehavior<TRequest, TResponse>(IUnitOfWork uow,
    IRolService roles, ICurrentUserService user) : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (request.GetType().Namespace?.Contains("AmbalajIslemleri", StringComparison.Ordinal) != true) return await next();
        if (request is IAmbalajDurumKapsamli kapsam)
        {
            var allowed = new List<int>();
            foreach (var (durum, kod) in new[] { (1, AmbalajMenuKodlari.BekleyenGoruntule), (2, AmbalajMenuKodlari.UretimdeGoruntule), (3, AmbalajMenuKodlari.TamamlananGoruntule) })
                if (await AmbalajYetkilendirmeYardimcisi.YetkiliMiAsync(roles, user, kod, ct, YetkiTipi.R)) allowed.Add(durum);
            kapsam.IzinliDurumlar = allowed.ToArray();
        }
        if (request is GetAmbalajUretimKaydiDetayQuery detay)
        {
            var record = await uow.GetRepository<AmbalajUretimKaydi>().GetByIdAsync(detay.Id);
            var kod = record?.UretimDurumu switch
            { AmbalajUretimDurumu.Tamamlandi => AmbalajMenuKodlari.TamamlananGoruntule,
              AmbalajUretimDurumu.Uretimde => AmbalajMenuKodlari.UretimdeGoruntule, _ => AmbalajMenuKodlari.BekleyenGoruntule };
            if (record != null && !await AmbalajYetkilendirmeYardimcisi.YetkiliMiAsync(roles, user, kod, ct, YetkiTipi.R))
                return Failure("Bu durumdaki üretim kaydını görüntüleme yetkiniz bulunmuyor.", 403);
        }
        if (request.GetType().Namespace != typeof(AmbalajFormOlusturCommand).Namespace ||
            request is AmbalajKaynaklariSenkronizeEtCommand)
            return await MaskeleAsync(await next(), ct);
        try
        {
            return await uow.ExecuteInTransactionAsync(async token =>
            {
                var records = uow.GetRepository<AmbalajUretimKaydi>().Queryable();
                var ids = request switch
                {
                    AmbalajUretimKaydiGuncelleCommand x => new[] { x.Id },
                    AmbalajUretimSecimGuncelleCommand x => [x.Id],
                    AmbalajUretimDurumuGuncelleCommand x => [x.Id],
                    AmbalajM3OverrideGuncelleCommand x => [x.Id],
                    AmbalajSarfOraniGuncelleCommand x => [x.Id],
                    AmbalajUretimKaydiIptalEtCommand x => [x.Id],
                    AmbalajUretimKaydiAktiflestirCommand x => [x.Id],
                    AmbalajPlanKalemKaydetCommand x when x.KalemId.HasValue => [x.KalemId.Value],
                    AmbalajPlanKalemSilCommand x => [x.KalemId],
                    AmbalajBagimsizSandikKaydetCommand x when x.SandikId.HasValue => [x.SandikId.Value],
                    AmbalajBagimsizSandikSilCommand x => [x.SandikId],
                    AmbalajKarariKaydetCommand x => records.Where(k => k.KaynakKayitId == x.SandikId).Select(k => k.Id).ToArray(),
                    AmbalajPlanKaydetCommand x => records.Where(k => k.ProjeId == x.ProjeId && !k.IptalMi && !k.BagimsizKayitMi &&
                        k.Tur == (x.Grup == 2 ? AmbalajSandikTuru.Ilave : x.Grup == 3 ? AmbalajSandikTuru.Ic : AmbalajSandikTuru.Normal)).Select(k => k.Id).ToArray(),
                    _ => []
                };
                var targets = records.Where(k => ids.Contains(k.Id)).ToList();
                var formIds = uow.GetRepository<AmbalajUretimFormuKaydi>().Queryable()
                    .Where(x => ids.Contains(x.AmbalajUretimKaydiId)).Select(x => x.AmbalajUretimKaydiId).ToHashSet();
                var reason = Gerekce(request);
                var gereken = new HashSet<string>();
                var kritik = false;
                var ilkFormGerekli = false;
                foreach (var k in targets)
                {
                    var hedefDurum = request switch
                    {
                        AmbalajUretimDurumuGuncelleCommand x => x.Durum,
                        _ => k.UretimDurumu
                    };
                    if (hedefDurum != k.UretimDurumu)
                    {
                        if (hedefDurum != AmbalajUretimDurumu.Planlandi && !formIds.Contains(k.Id))
                            ilkFormGerekli = true;
                        gereken.Add(AmbalajMenuKodlari.DurumDuzenle);
                        if (hedefDurum == AmbalajUretimDurumu.Tamamlandi) gereken.Add(AmbalajMenuKodlari.UretimiTamamla);
                        if (hedefDurum < k.UretimDurumu) { gereken.Add(AmbalajMenuKodlari.DurumuGeriAl); kritik = true; }
                        if (k.UretimDurumu == AmbalajUretimDurumu.Tamamlandi) { gereken.Add(AmbalajMenuKodlari.UretimiYenidenAc); kritik = true; }
                    }
                    var uretimSecimiDegisti = request is AmbalajPlanKaydetCommand plan && k.KaynakKayitId.HasValue &&
                        k.UretimeAlindi != plan.SeciliKaynakSandikIds.Contains(k.KaynakKayitId.Value);
                    if (uretimSecimiDegisti && formIds.Contains(k.Id))
                    { gereken.Add(AmbalajMenuKodlari.FormSonrasiSecimDegistir); gereken.Add(AmbalajMenuKodlari.KritikVeriDuzenle); kritik = true; }
                    var kararDegisti = request switch
                    {
                        AmbalajKarariKaydetCommand x => x.AmbalajaDahilMi != k.AmbalajaDahil,
                        AmbalajUretimSecimGuncelleCommand x => x.AmbalajaDahil != k.AmbalajaDahil,
                        _ => false
                    };
                    if (kararDegisti)
                    {
                        gereken.Add(k.AmbalajaDahil ? AmbalajMenuKodlari.HaricTut : AmbalajMenuKodlari.DahilEt);
                        if (formIds.Contains(k.Id)) { gereken.Add(AmbalajMenuKodlari.FormSonrasiSecimDegistir); kritik = true; }
                        if (!k.AmbalajaDahil && k.ProjeId.HasValue && AmbalajUretimPolitikasi.ProjeDurumu(records.Where(x => x.ProjeId == k.ProjeId).ToList()) == AmbalajUretimDurumu.Tamamlandi)
                        { gereken.Add(AmbalajMenuKodlari.TamamlananProjeyeEkle); kritik = true; }
                    }
                    var partiDegisti = request is AmbalajPlanKaydetCommand parti &&
                        (parti.Grup == 3 ? !k.KaynakKayitId.HasValue : k.KaynakKayitId.HasValue && k.AmbalajaDahil) &&
                        !string.Equals(AmbalajUretimYardimcilari.Temizle(k.FirinPartiNo),
                            AmbalajUretimYardimcilari.Temizle(parti.FirinPartiNo), StringComparison.Ordinal);
                    var veriDegisikligi = request is AmbalajUretimKaydiGuncelleCommand or AmbalajM3OverrideGuncelleCommand or
                        AmbalajSarfOraniGuncelleCommand or AmbalajPlanKalemKaydetCommand or AmbalajBagimsizSandikKaydetCommand || kararDegisti || partiDegisti;
                    var kaynakId = k.IsAkisKimligi.ToString("D");
                    var maliBagli = uow.GetRepository<FinansIsKaydi>().Queryable().Any(f =>
                        f.KaynakTuru.ToUpper() == "AMBALAJURETIM" && f.KaynakKayitId == kaynakId && f.SiparisKalemleri.Any(s => !s.FinansSiparis.IptalEdildi));
                    if (veriDegisikligi && (formIds.Contains(k.Id) || k.UretimDurumu == AmbalajUretimDurumu.Tamamlandi || maliBagli))
                    { gereken.Add(AmbalajMenuKodlari.KritikVeriDuzenle); kritik = true; }
                    if (request is AmbalajPlanKalemSilCommand or AmbalajBagimsizSandikSilCommand or AmbalajUretimKaydiIptalEtCommand)
                    { gereken.Add(AmbalajMenuKodlari.Iptal); kritik = true; }
                }
                var yeniProjeId = request switch
                {
                    AmbalajUretimKaydiOlusturCommand x => x.ProjeId,
                    AmbalajPlanKalemKaydetCommand x when !x.KalemId.HasValue => x.ProjeId,
                    AmbalajBagimsizSandikKaydetCommand x when !x.SandikId.HasValue => (int?)x.ProjeId,
                    AmbalajKarariKaydetCommand x when x.AmbalajaDahilMi && targets.Count == 0 =>
                        uow.GetRepository<Sandik>().Queryable().Where(s => s.Id == x.SandikId).Select(s => (int?)s.ProjeId).FirstOrDefault(),
                    _ => null
                };
                if (yeniProjeId.HasValue && AmbalajUretimPolitikasi.ProjeDurumu(records.Where(k => k.ProjeId == yeniProjeId).ToList()) == AmbalajUretimDurumu.Tamamlandi)
                { gereken.Add(AmbalajMenuKodlari.TamamlananProjeyeEkle); kritik = true; }
                foreach (var kod in gereken)
                    if (!await AmbalajYetkilendirmeYardimcisi.YetkiliMiAsync(roles, user, kod, token))
                        return Failure($"Bu işlem için gerekli yetkiniz bulunmuyor ({kod}).", 403);
                if (ilkFormGerekli) return Failure("Bu kayıt için önce Üretim Formu Oluştur işlemini kullanınız.", 409);
                if (kritik && string.IsNullOrWhiteSpace(reason)) return Failure("Kritik üretim değişikliği için gerekçe gereklidir.", 400);
                var result = await next();
                if (result is Result { IsSuccess: false }) throw new RollbackResult(result);
                if (kritik)
                {
                    // Tamamlanmış projeye ekleme öncesinde hedef henüz yoktur; oluşturulan kaydı da gerekçeyle izleriz.
                    if (targets.Count == 0 && yeniProjeId.HasValue)
                    {
                        var createdId = result?.GetType().GetProperty("Value")?.GetValue(result)?.GetType().GetProperty("Id");
                        var value = result?.GetType().GetProperty("Value")?.GetValue(result);
                        if (createdId?.GetValue(value) is int newId && records.FirstOrDefault(k => k.Id == newId) is { } created)
                            targets.Add(created);
                        if (request is AmbalajKarariKaydetCommand karar && records.FirstOrDefault(k => k.KaynakKayitId == karar.SandikId && !k.IptalMi) is { } source)
                            targets.Add(source);
                    }
                    foreach (var k in targets)
                        await uow.GetRepository<AmbalajUretimHareketi>().AddAsync(new()
                        {
                            AmbalajUretimKaydiId = k.Id, KullaniciId = user.IslemKullaniciId ?? 0,
                            Islem = "Kritik üretim işlemi", AlanAdi = "YetkiliRevizyon",
                            YeniDeger = request.GetType().Name, Aciklama = reason
                        });
                    await uow.SaveChangesAsync(token);
                }
                return await MaskeleAsync(result, token);
            }, ct);
        }
        catch (RollbackResult failure) { return failure.Response; }
    }

    private static string? Gerekce(object request) => new[] { "Gerekce", "Aciklama", "Neden", "IptalNedeni" }
        .Select(name => request.GetType().GetProperty(name)?.GetValue(request) as string)
        .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private async Task<TResponse> MaskeleAsync(TResponse response, CancellationToken ct)
    {
        var izin = await AmbalajYetkilendirmeYardimcisi.GorunumYetkileriniGetirAsync(roles, user, ct);
        var gecmis = await AmbalajYetkilendirmeYardimcisi.YetkiliMiAsync(roles, user, AmbalajMenuKodlari.GecmisGoruntule, ct, YetkiTipi.R);
        var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
        void Visit(object? value)
        {
            if (value == null || value is string || value.GetType().IsValueType || value is byte[] || !seen.Add(value)) return;
            if (value is System.Collections.IEnumerable list) { foreach (var item in list) Visit(item); return; }
            foreach (var property in value.GetType().GetProperties())
            {
                if (property.GetIndexParameters().Length != 0 || !property.CanRead) continue;
                var name = property.Name;
                var hidden = (!izin.OlcuGorunur && name is "Boy" or "En" or "Yukseklik" or "IcOlculer" or "DisOlculer" or "OnDuvarYuksekligi") ||
                    (!izin.M3Gorunur && name.Contains("M3", StringComparison.Ordinal) && property.PropertyType == typeof(decimal?)) ||
                    (!izin.SarfGorunur && name is "SarfOrani" or "SarfM3" or "ToplamM3" or "SarfDahilM3");
                if (hidden && property.CanWrite && (!property.PropertyType.IsValueType || Nullable.GetUnderlyingType(property.PropertyType) != null))
                    property.SetValue(value, null);
                else if (name == "Hareketler" && !gecmis && property.CanWrite)
                    property.SetValue(value, Array.Empty<_3K.Application.Features.AmbalajIslemleri.DTOs.AmbalajUretimHareketiDto>());
                else Visit(property.GetValue(value));
            }
        }
        Visit(response);
        return response;
    }

    private static TResponse Failure(string message, int code) => (TResponse)typeof(TResponse)
        .GetMethod("Failure", [typeof(string), typeof(int)])!.Invoke(null, [message, code])!;
    private sealed class RollbackResult(TResponse response) : Exception
    { public TResponse Response { get; } = response; }
}
