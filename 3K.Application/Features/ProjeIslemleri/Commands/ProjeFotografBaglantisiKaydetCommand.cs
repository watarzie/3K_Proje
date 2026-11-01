using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.ProjeIslemleri.DTOs;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.ProjeIslemleri.Commands
{
    /// <summary>
    /// Projeye fotoğraf veya gerçekleşen çeki bağlantısı ekler (<see cref="BaglantiId"/> boş; tür <see cref="Tur"/>, boşsa
    /// fotoğraf) ya da var olanın adresini/açıklamasını değiştirir (tür değişmez). Proje tipinden ve sevk durumundan
    /// bağımsız tek izin: <see cref="YetkiKodlari.Proje.FotografBaglantisi"/> W. Sonuç projenin güncel bağlantı listesidir.
    /// </summary>
    public class ProjeFotografBaglantisiKaydetCommand : IRequest<Result<List<ProjeFotografBaglantisiDto>>>, ISecuredRequest, IRequiresMenuPermission
    {
        public int ProjeId { get; set; }
        public int? BaglantiId { get; set; }
        public string? Adres { get; set; }
        public string? Aciklama { get; set; }
        /// <summary>Yalnız eklemede okunur; boşsa <see cref="ProjeBaglantiTuru.Fotograf"/>.</summary>
        public ProjeBaglantiTuru? Tur { get; set; }

        public string RequiredMenuKod => YetkiKodlari.Proje.FotografBaglantisi;
        public YetkiTipi RequiredYetkiTipi => YetkiTipi.W;
    }

    public class ProjeFotografBaglantisiKaydetCommandHandler(
        IUnitOfWork unitOfWork,
        IHareketService hareketService,
        ICurrentUserService currentUserService)
        : IRequestHandler<ProjeFotografBaglantisiKaydetCommand, Result<List<ProjeFotografBaglantisiDto>>>
    {
        private static readonly System.Globalization.CultureInfo TurkceKultur = System.Globalization.CultureInfo.GetCultureInfo("tr-TR");

        public Task<Result<List<ProjeFotografBaglantisiDto>>> Handle(ProjeFotografBaglantisiKaydetCommand request, CancellationToken cancellationToken) =>
            // Bağlantı ve değişiklik geçmişi birlikte yazılır ya da hiçbiri yazılmaz.
            unitOfWork.ExecuteAtomicAsync(async token =>
            {
                if (await unitOfWork.GetRepository<Proje>().GetByIdAsync(request.ProjeId) is null)
                    return Result<List<ProjeFotografBaglantisiDto>>.Failure("Proje bulunamadı.", 404);

                var adres = ProjeFotografBaglantisiKurali.Normalize(request.Adres)!;
                var aciklama = ProjeFotografBaglantisiKurali.AciklamaNormalize(request.Aciklama);
                var repo = unitOfWork.GetRepository<ProjeFotografBaglantisi>();
                var mevcutlar = (await repo.FindAsync(x => x.ProjeId == request.ProjeId)).ToList();
                if (mevcutlar.Any(x => x.Id != request.BaglantiId && x.Adres == adres))
                    return Result<List<ProjeFotografBaglantisiDto>>.Failure("Bu adres projede zaten kayıtlı.", 409);

                if (request.BaglantiId is not int baglantiId)
                {
                    var tur = request.Tur ?? ProjeBaglantiTuru.Fotograf;
                    if (mevcutlar.Count(x => x.Tur == tur) >= ProjeFotografBaglantisiKurali.EnFazlaBaglanti)
                        return Result<List<ProjeFotografBaglantisiDto>>.Failure(
                            $"Bir projeye en fazla {ProjeFotografBaglantisiKurali.EnFazlaBaglanti} {ProjeFotografBaglantisiKurali.TurAdi(tur).ToLower(TurkceKultur)} eklenebilir.");

                    var yeni = new ProjeFotografBaglantisi { ProjeId = request.ProjeId, Tur = tur, Adres = adres, Aciklama = aciklama };
                    await repo.AddAsync(yeni);
                    await unitOfWork.SaveChangesAsync(token);
                    await hareketService.HareketKaydetAsync(ProjeFotografBaglantisiGecmisi.Kayit(
                        request.ProjeId, currentUserService.UserId, ProjeFotografBaglantisiGecmisi.Islem(tur, "Eklendi"), null, yeni));
                }
                else
                {
                    // İzlenen kayıt doğrudan değiştirilir; başka projenin bağlantısı bu projeden düzenlenemez.
                    var baglanti = await repo.GetByIdAsync(baglantiId);
                    if (baglanti is null || baglanti.ProjeId != request.ProjeId)
                        return Result<List<ProjeFotografBaglantisiDto>>.Failure("Fotoğraf bağlantısı bulunamadı.", 404);
                    if (baglanti.Adres == adres && baglanti.Aciklama == aciklama)
                        return Result<List<ProjeFotografBaglantisiDto>>.Success(ProjeFotografBaglantisiDto.Liste(mevcutlar));

                    var eski = ProjeFotografBaglantisiGecmisi.Metin(baglanti);
                    baglanti.Adres = adres;
                    baglanti.Aciklama = aciklama;
                    await unitOfWork.SaveChangesAsync(token);
                    await hareketService.HareketKaydetAsync(ProjeFotografBaglantisiGecmisi.Kayit(
                        request.ProjeId, currentUserService.UserId, ProjeFotografBaglantisiGecmisi.Islem(baglanti.Tur, "Güncellendi"), eski, baglanti));
                }

                return Result<List<ProjeFotografBaglantisiDto>>.Success(
                    ProjeFotografBaglantisiDto.Liste(await repo.FindAsync(x => x.ProjeId == request.ProjeId)));
            }, cancellationToken);
    }

    /// <summary>Fotoğraf ve gerçekleşen çeki bağlantısı değişikliklerinin proje hareket geçmişi kaydı.</summary>
    internal static class ProjeFotografBaglantisiGecmisi
    {
        /// <summary>İşlem adı: "Fotoğraf Bağlantısı Eklendi", "Gerçekleşen Çeki Bağlantısı Kaldırıldı" gibi.</summary>
        public static string Islem(ProjeBaglantiTuru tur, string fiil) => $"{ProjeFotografBaglantisiKurali.TurAdi(tur)} {fiil}";

        /// <summary>Geçmişte görünen değer: "Açıklama · adres" ya da yalnız adres.</summary>
        public static string Metin(ProjeFotografBaglantisi baglanti) =>
            baglanti.Aciklama is null ? baglanti.Adres : $"{baglanti.Aciklama} · {baglanti.Adres}";

        public static HareketGecmisi Kayit(int projeId, int? kullaniciId, string islem, string? eski, ProjeFotografBaglantisi? yeni) => new()
        {
            ProjeId = projeId,
            KullaniciId = kullaniciId ?? 0,
            ReferansTipi = "Proje",
            ReferansId = projeId.ToString(),
            Islem = islem,
            IslemTipiId = null,
            EskiDeger = eski ?? "Yok",
            YeniDeger = yeni is null ? "Yok" : Metin(yeni),
            Aciklama = "Projenin bağlantıları değiştirildi."
        };
    }
}
