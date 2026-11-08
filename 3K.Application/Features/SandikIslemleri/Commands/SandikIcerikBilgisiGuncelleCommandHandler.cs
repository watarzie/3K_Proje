using MediatR;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    /// <summary>
    /// Ürün bilgisini ve (istenirse) ekranın açıklamasını yalnız projeye ait, sevk kilidi olmayan sandıktaki satırda
    /// değiştirir ve eski/yeni değerleri hareket geçmişine yazar. Değişmeyen alan yazılmaz; ikisi de değişmediyse hiçbir
    /// şey yazılmaz. Ekrandaki eski değer kayıtlı değerle eşleşmeyen alan varsa hiçbir alan yazılmadan 409 döner;
    /// eşzamanlı iki kayıt <c>xmin</c> ile ayrıca yakalanır. Açıklama düzenlemesi Grid/3K işlemi değildir.
    /// </summary>
    public class SandikIcerikBilgisiGuncelleCommandHandler(
        IUnitOfWork unitOfWork,
        IHareketService hareketService,
        ICurrentUserService currentUser)
        : IRequestHandler<SandikIcerikBilgisiGuncelleCommand, Result>
    {
        public async Task<Result> Handle(SandikIcerikBilgisiGuncelleCommand request, CancellationToken cancellationToken)
        {
            var icerik = unitOfWork.GetRepository<SandikIcerik>().Queryable()
                .FirstOrDefault(i => i.Id == request.SandikIcerikId);
            if (icerik == null || icerik.SandikId != request.SandikId)
                return Result.Failure("Sandık içeriği bulunamadı.", 404);

            var sandik = await unitOfWork.GetRepository<Sandik>().GetByIdAsync(request.SandikId);
            if (sandik == null || sandik.ProjeId != request.ProjeId)
                return Result.Failure("Sandık bulunamadı veya projeye ait değil.", 404);
            if (SandikSevkKilidiHelper.SandikKilitliMi(sandik))
                return Result.Failure(SandikSevkKilidiHelper.SandikKilitliMesaji);

            var yeniBilgi = SandikIcerikUrunBilgisiKurali.Normalize(request.UrunBilgisi);
            if (!SandikIcerikUrunBilgisiKurali.Gecerli(yeniBilgi))
                return Result.Failure(SandikIcerikUrunBilgisiKurali.UzunlukMesaji);
            var eskiBilgi = icerik.UrunBilgisi;
            // Aynı değerin tekrarı (çift tıklama, ağ tekrarı) çakışma sayılmaz; yazılacak bir şey yoktur.
            var bilgiDegisti = !string.Equals(eskiBilgi, yeniBilgi, StringComparison.Ordinal);
            if (bilgiDegisti && !SandikIcerikUrunBilgisiKurali.EkrandakiDegerGuncel(eskiBilgi, request.OncekiUrunBilgisi))
                return Result.Failure(SandikIcerikUrunBilgisiKurali.CakismaMesaji, 409);

            CekiSatiri? satir = null;
            string? eskiAciklama = null;
            string? yeniAciklama = null;
            var aciklamaDegisti = false;
            if (request.AciklamaAlani is { } alan)
            {
                if (!icerik.CekiSatiriId.HasValue)
                    return Result.Failure(UrunAciklamaKurali.CekiBagiYokMesaji);
                satir = await unitOfWork.GetRepository<CekiSatiri>().GetByIdAsync(icerik.CekiSatiriId.Value);
                if (satir == null)
                    return Result.Failure("Ürün bulunamadı.", 404);

                yeniAciklama = UrunAciklamaKurali.Normalize(request.Aciklama);
                if (!UrunAciklamaKurali.Gecerli(yeniAciklama))
                    return Result.Failure(UrunAciklamaKurali.UzunlukMesaji);
                eskiAciklama = UrunAciklamaKurali.Oku(satir, alan);
                aciklamaDegisti = !string.Equals(UrunAciklamaKurali.Normalize(eskiAciklama), yeniAciklama, StringComparison.Ordinal);
                if (aciklamaDegisti && !UrunAciklamaKurali.EkrandakiDegerGuncel(eskiAciklama, request.OncekiAciklama))
                    return Result.Failure(UrunAciklamaKurali.CakismaMesaji, 409);
            }

            if (!bilgiDegisti && !aciklamaDegisti)
                return Result.Success();

            if (bilgiDegisti)
                icerik.UrunBilgisi = yeniBilgi;
            if (aciklamaDegisti)
                UrunAciklamaKurali.Yaz(satir!, request.AciklamaAlani!.Value, yeniAciklama);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var kullaniciId = currentUser.IslemKullaniciId ?? 0;
            if (bilgiDegisti)
            {
                await hareketService.HareketKaydetAsync(new HareketGecmisi
                {
                    ProjeId = request.ProjeId,
                    ReferansTipi = "SandikIcerik",
                    ReferansId = icerik.Id.ToString(),
                    Islem = "Ürün Bilgisi Güncellendi",
                    IslemTipiId = (int)IslemTipi.NotEklendi,
                    EskiDeger = eskiBilgi,
                    YeniDeger = yeniBilgi,
                    KullaniciId = kullaniciId,
                    Aciklama = $"Sandık: {sandik.SandikNo}"
                });
            }

            if (aciklamaDegisti)
            {
                await hareketService.HareketKaydetAsync(new HareketGecmisi
                {
                    ProjeId = request.ProjeId,
                    ReferansTipi = "CekiSatiri",
                    ReferansId = satir!.Id.ToString(),
                    Islem = UrunAciklamaKurali.HareketAdi(request.AciklamaAlani!.Value),
                    IslemTipiId = (int)IslemTipi.NotEklendi,
                    EskiDeger = eskiAciklama,
                    YeniDeger = yeniAciklama,
                    KullaniciId = kullaniciId,
                    Aciklama = $"Sandık: {sandik.SandikNo}"
                });
            }

            return Result.Success();
        }
    }
}
