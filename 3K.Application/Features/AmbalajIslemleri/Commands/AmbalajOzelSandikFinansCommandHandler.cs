using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.AmbalajIslemleri.Commands;

/// <summary>
/// Özel sandığın finans ilişkisi tek adımdır: üretim formu ve üretim adımları yoktur. "Finansa aktar" sandığı üretime
/// alınmış sayar ve Finans iş kaydını (tarifeyle fiyatlanarak) açar; "Finanstan geri al" iş kaydını pasifleştirir.
/// Geri alma, iş kaydı PO'ya bağlıysa veya sandık proje tamamlamasıyla tamamlandıysa reddedilir.
/// </summary>
public sealed class AmbalajOzelSandikFinansCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IRolService roller,
    IFinansUretimAktarimService finans,
    AmbalajKritikIslemDenetimi denetim)
    : IRequestHandler<AmbalajOzelSandikFinansCommand, Result<AmbalajBagimsizSandikDto>>
{
    /// <summary>Hareket geçmişindeki işlem adları; liste "finansa aktaran" bilgisini bu adla bulur.</summary>
    public const string AktarimIslemi = "Özel sandık finansa aktarıldı";
    public const string GeriAlmaIslemi = "Özel sandık finanstan geri alındı";

    public Task<Result<AmbalajBagimsizSandikDto>> Handle(
        AmbalajOzelSandikFinansCommand request,
        CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAtomicAsync(async token =>
        {
            var repo = unitOfWork.GetRepository<AmbalajUretimKaydi>();
            var kayit = await repo.GetByIdAsync(request.SandikId);
            if (kayit == null || !kayit.BagimsizKayitMi || kayit.IptalMi)
                return Result<AmbalajBagimsizSandikDto>.Failure("Özel sandık bulunamadı.", 404);
            if (!await AmbalajYetkilendirmeYardimcisi.KaynakMudahalesineYetkiliMiAsync(kayit, roller, currentUser, token))
                return Result<AmbalajBagimsizSandikDto>.Failure(
                    "Kaynak modülden gelen kayda müdahale için Ambalaj Üretim Listesi yazma yetkisi gereklidir.", 403);

            var hata = request.Aktar ? AktarimEngeli(kayit) : GeriAlmaEngeli(kayit);
            if (hata != null) return Result<AmbalajBagimsizSandikDto>.Failure(hata, 409);

            var eski = AmbalajUretimYardimcilari.Snapshot(kayit);
            if (request.Aktar)
            {
                kayit.UretimeAlindi = true;
                kayit.UretimTarihi ??= TurkeyTime.Now;
                // Girilen tarih finans kaleminin finans tarihidir; boşsa aktarım günü (üretim tarihi) kullanılır.
                if (request.FinansTarihi.HasValue) kayit.FinansTarihi = request.FinansTarihi.Value.Date;
                if (kayit.UretimDurumu == AmbalajUretimDurumu.Planlandi) kayit.UretimDurumu = AmbalajUretimDurumu.Uretimde;
            }
            else
            {
                kayit.UretimeAlindi = false;
                kayit.UretimTarihi = null;
                kayit.FinansTarihi = null;
                kayit.UretimDurumu = AmbalajUretimDurumu.Planlandi;
                kayit.TamamlanmaTarihi = null;
            }
            kayit.UpdatedDate = TurkeyTime.Now;
            kayit.UpdatedBy = currentUser.IslemKullaniciId?.ToString();
            repo.Update(kayit);
            await AmbalajUretimYardimcilari.AlanHareketleriniEkleAsync(
                unitOfWork, kayit, eski,
                request.Aktar ? AktarimIslemi : GeriAlmaIslemi,
                currentUser.IslemKullaniciId ?? 0, request.Gerekce);
            var proje = kayit.ProjeId.HasValue ? await unitOfWork.GetRepository<Proje>().GetByIdAsync(kayit.ProjeId.Value) : null;
            await AmbalajFinansSenkronizasyonu.KaydetVeAktarAsync(unitOfWork, finans, kayit, proje, token);

            var ust = kayit.UstKayitId.HasValue ? await repo.GetByIdAsync(kayit.UstKayitId.Value) : null;
            var kaynak = kayit.KaynakKayitId.HasValue
                ? await unitOfWork.GetRepository<Sandik>().GetByIdAsync(kayit.KaynakKayitId.Value)
                : null;
            var izin = await AmbalajYetkilendirmeYardimcisi.GorunumYetkileriniGetirAsync(roller, currentUser, token);
            return Result<AmbalajBagimsizSandikDto>.Success(AmbalajAlanMaskesi.Maskele(
                AmbalajPlanlamaYardimcisi.BagimsizDtoOlustur(kayit, proje, ust, kaynak) with
                {
                    OlusturmaTarihi = kayit.CreatedDate,
                    FinansaAktarimTarihi = kayit.UretimeAlindi ? TurkeyTime.Now : null
                }, izin));
        }, cancellationToken);

    private static string? AktarimEngeli(AmbalajUretimKaydi kayit)
    {
        if (kayit.UretimeAlindi) return $"{kayit.SandikNo} numaralı özel sandık zaten finansa aktarılmış.";
        if (!kayit.AmbalajaDahil) return "Yapılmayacak olarak işaretlenmiş özel sandık finansa aktarılamaz.";
        if (!AmbalajUretimYardimcilari.UretimMiktariGecerli(kayit))
            return "Adedi ve ölçüleri eksik özel sandık finansa aktarılamaz; önce sandığı düzenleyip eksikleri tamamlayın.";
        return null;
    }

    private string? GeriAlmaEngeli(AmbalajUretimKaydi kayit)
    {
        if (!kayit.UretimeAlindi) return $"{kayit.SandikNo} numaralı özel sandık finansa aktarılmamış.";
        if (kayit.UretimDurumu == AmbalajUretimDurumu.Tamamlandi)
            return "Proje tamamlamasıyla tamamlanmış özel sandık finanstan geri alınamaz; önce proje tamamlamasını geri alın.";
        var degisiklik = denetim.DegisiklikleriHazirla([kayit], d => d).Single();
        return degisiklik.FinansaBagli
            ? $"{kayit.SandikNo} numaralı özel sandığın finans kaydı PO'ya bağlı olduğu için finanstan geri alınamaz."
            : null;
    }
}
