using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.AmbalajIslemleri.Commands;

/// <summary>
/// Projenin ambalaja dahil bütün aktif sandıklarını birlikte tamamlar veya tamamlanmayı geri alır. Kayda
/// bağlı kurallar (<see cref="AmbalajIslemKurallari"/>) her sandık için değişiklik öncesi hâliyle, aynı
/// transaction içinde değerlendirilir; finans aktarımı değişen bütün sandıklar için tek pakette yapılır.
/// Önce bütün izinler (403) denetlenir; tamamlamada formu olmayan, üretime alınmamış ve ölçüsü eksik bütün
/// sandıklar tek 409 yanıtında numarası ve grubuyla bildirilir (<see cref="AmbalajProjeTamamlamaEngelleri"/>).
/// </summary>
public sealed class AmbalajProjeUretimDurumuCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IFinansUretimAktarimService finans,
    IRolService roller,
    AmbalajKritikIslemDenetimi denetim)
    : IRequestHandler<AmbalajProjeUretimDurumuCommand, Result<AmbalajProjeUretimDurumuSonucDto>>
{
    public Task<Result<AmbalajProjeUretimDurumuSonucDto>> Handle(
        AmbalajProjeUretimDurumuCommand request,
        CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAtomicAsync(async token =>
        {
            var repo = unitOfWork.GetRepository<AmbalajUretimKaydi>();
            var kayitlar = repo.Queryable()
                .Where(k => k.ProjeId == request.ProjeId && !k.IptalMi && k.AmbalajaDahil)
                .ToList();
            if (kayitlar.Count == 0)
                return Result<AmbalajProjeUretimDurumuSonucDto>.Failure("Projede ambalaja dahil aktif sandık bulunmuyor.", 409);

            var hedefDurum = request.Tamamla ? AmbalajUretimDurumu.Tamamlandi : AmbalajUretimDurumu.Uretimde;
            var degisecekler = request.Tamamla
                ? kayitlar.Where(k => k.UretimDurumu != AmbalajUretimDurumu.Tamamlandi).ToList()
                : kayitlar.Where(k => k.UretimDurumu == AmbalajUretimDurumu.Tamamlandi).ToList();
            if (degisecekler.Count == 0)
                return Result<AmbalajProjeUretimDurumuSonucDto>.Failure(request.Tamamla
                    ? "Projenin tamamlanacak sandığı yok."
                    : "Projede geri alınacak tamamlanmış sandık yok.", 409);

            var degisiklikler = denetim.DegisiklikleriHazirla(degisecekler,
                degisiklik => degisiklik with { HedefDurum = hedefDurum });
            var gereksinim = denetim.Degerlendir(degisiklikler);
            var yetki = await denetim.YetkileriDenetleAsync(gereksinim, token);
            if (!yetki.IsSuccess)
                return Result<AmbalajProjeUretimDurumuSonucDto>.Failure(yetki.Error!.Message, yetki.StatusCode);
            foreach (var kayit in degisecekler)
            {
                if (!await AmbalajYetkilendirmeYardimcisi.KaynakMudahalesineYetkiliMiAsync(kayit, roller, currentUser, token))
                    return Result<AmbalajProjeUretimDurumuSonucDto>.Failure(
                        "Kaynak modülden gelen kayda müdahale için Ambalaj Üretim Listesi yazma yetkisi gereklidir.", 403);
            }
            if (request.Tamamla)
            {
                var engeller = AmbalajProjeTamamlamaEngelleri.Belirle(degisiklikler);
                if (engeller.Count > 0)
                    return Result<AmbalajProjeUretimDurumuSonucDto>.Failure(
                        AmbalajProjeTamamlamaEngelleri.Mesaj(engeller), 409, engeller);
            }
            var kapi = AmbalajKritikIslemDenetimi.IsKurallariniDenetle(gereksinim, request.Aciklama);
            if (!kapi.IsSuccess)
                return Result<AmbalajProjeUretimDurumuSonucDto>.Failure(kapi.Error!.Message, kapi.StatusCode);

            var kullaniciId = currentUser.IslemKullaniciId ?? 0;
            foreach (var kayit in degisecekler)
            {
                var eski = AmbalajUretimYardimcilari.Snapshot(kayit);
                kayit.UretimDurumu = hedefDurum;
                if (request.Tamamla)
                {
                    kayit.UretimTarihi ??= TurkeyTime.Now;
                    kayit.TamamlanmaTarihi ??= TurkeyTime.Now;
                }
                else
                {
                    kayit.TamamlanmaTarihi = null;
                }
                repo.Update(kayit);
                if (request.Tamamla)
                    await AmbalajYasamDongusuYardimcisi.GerceklesmeyiKaydetAsync(unitOfWork, kayit, kullaniciId);
                await AmbalajUretimYardimcilari.AlanHareketleriniEkleAsync(unitOfWork, kayit, eski,
                    request.Tamamla ? "Proje üretimi toplu tamamlandı" : "Proje üretim tamamlaması geri alındı",
                    kullaniciId, request.Aciklama);
            }
            await unitOfWork.SaveChangesAsync(token);
            var proje = await unitOfWork.GetRepository<Proje>().GetByIdAsync(request.ProjeId);
            await finans.UretimKayitlariniAktarAsync(
                degisecekler.Select(k => AmbalajFinansSenkronizasyonu.ModelOlustur(k, proje)).ToList(), token);
            await denetim.KritikIslemiKaydetAsync(gereksinim, degisecekler, nameof(AmbalajProjeUretimDurumuCommand), request.Aciklama, token);
            return Result<AmbalajProjeUretimDurumuSonucDto>.Success(new AmbalajProjeUretimDurumuSonucDto(
                request.ProjeId, degisecekler.Count, AmbalajUretimPolitikasi.ProjeDurumu(kayitlar)));
        }, cancellationToken);
}
