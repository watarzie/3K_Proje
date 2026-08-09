using MediatR;
using _3K.Application.Common;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    public class ManuelUrunSilCommandHandler : IRequestHandler<ManuelUrunSilCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHareketService _hareketService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ISahaAktarimSilmeKorumaService _sahaAktarimSilmeKorumaService;

        public ManuelUrunSilCommandHandler(
            IUnitOfWork unitOfWork,
            IHareketService hareketService,
            ICurrentUserService currentUserService,
            ISahaAktarimSilmeKorumaService sahaAktarimSilmeKorumaService)
        {
            _unitOfWork = unitOfWork;
            _hareketService = hareketService;
            _currentUserService = currentUserService;
            _sahaAktarimSilmeKorumaService = sahaAktarimSilmeKorumaService;
        }

        public async Task<Result> Handle(ManuelUrunSilCommand request, CancellationToken cancellationToken)
        {
            // CASE 1: Saha/Yedek ürünleri — SandikIcerikId ile silme
            if (request.SandikIcerikId is > 0)
                return await SahaYedekUrunSilAsync(request, cancellationToken);

            // CASE 2: Normal proje manuel ürünleri — CekiSatiriId ile silme
            if (request.CekiSatiriId is > 0)
                return await NormalManuelUrunSilAsync(request, cancellationToken);

            return Result.Failure("CekiSatiriId veya SandikIcerikId belirtilmelidir.");
        }

        /// <summary>
        /// Saha/Yedek projelerindeki ürünleri siler.
        /// Yalnız gerçekten manuel eklenen kayıtları siler. ÇEKİ/import kaynaklı bağlı
        /// satırlar aynı endpoint çağrılsa bile silinemez.
        /// </summary>
        private async Task<Result> SahaYedekUrunSilAsync(
            ManuelUrunSilCommand request,
            CancellationToken cancellationToken)
        {
            var sandikIcerikRepo = _unitOfWork.GetRepository<SandikIcerik>();
            var icerik = await sandikIcerikRepo.GetByIdAsync(request.SandikIcerikId!.Value);

            if (icerik == null)
                return Result.Failure("Ürün bulunamadı.", 404);

            var sandikRepo = _unitOfWork.GetRepository<Sandik>();
            var sandik = await sandikRepo.GetByIdAsync(icerik.SandikId);
            if (sandik == null || sandik.ProjeId != request.ProjeId)
                return Result.Failure("Sandık bulunamadı veya projeye ait değil.");

            var projeRepo = _unitOfWork.GetRepository<Proje>();
            var proje = await projeRepo.GetByIdAsync(sandik.ProjeId);
            var isSahaYedek = proje != null &&
                (proje.ProjeTipiId == (int)ProjeTipi.Saha || proje.ProjeTipiId == (int)ProjeTipi.Yedek);

            if (!isSahaYedek)
                return Result.Failure("Bu silme işlemi yalnızca Saha/Yedek projelerinde kullanılabilir.", 409);

            if (SandikSevkKilidiHelper.SandikKilitliMi(sandik))
                return Result.Failure("Sevk edilmiş sandıktan ürün silinemez.");

            var urunBilgi = $"{icerik.BarkodNo ?? "-"} - {icerik.Isim ?? "-"} ({icerik.Miktar} adet)";
            var cekiSatiriRepo = _unitOfWork.GetRepository<CekiSatiri>();
            CekiSatiri? bagliSatir = null;

            if (icerik.CekiSatiriId.HasValue)
                bagliSatir = await cekiSatiriRepo.GetByIdAsync(icerik.CekiSatiriId.Value);

            if (icerik.CekiSatiriId.HasValue && bagliSatir == null)
            {
                return Result.Failure(
                    "Ürünün bağlı çeki satırı bulunamadı. Veri bütünlüğü kontrol edilmeden ürün silinemez.",
                    409);
            }

            if (bagliSatir is { IsManuelEklenen: false })
            {
                return Result.Failure(
                    "Bu ürün ÇEKİ dosyasından gelmiştir. Yalnızca manuel eklenen ürünler silinebilir.",
                    409);
            }

            if (bagliSatir?.KaynakCekiSatiriId.HasValue == true)
            {
                return Result.Failure(
                    "Bu ürün saha aktarımıyla oluşturulmuştur. Ürünü tek başına silmek yerine resmi saha aktarımını geri alın.",
                    409);
            }

            if (bagliSatir != null && ManuelUrunSilmeKurali.IslemGormusMu(bagliSatir))
            {
                return Result.Failure(
                    "Bu manuel ürün üzerinde Grid, 3K, kalite veya süreç işlemi var. Silmeden önce işlemleri geri alın.",
                    409);
            }

            if (bagliSatir != null && await AktifSahaAktariminaBagliMiAsync(bagliSatir.Id, cancellationToken))
                return Result.Failure(SahaAktarimSilmeKorumaMesajlari.Urun, 409);

            if (bagliSatir != null)
            {
                var bagliIcerikler = await sandikIcerikRepo.FindAsync(x => x.CekiSatiriId == bagliSatir.Id);
                var bagliIcerikListesi = bagliIcerikler.ToList();
                var bagliSandikIds = bagliIcerikListesi.Select(x => x.SandikId).Distinct().ToList();
                var bagliSandiklar = (await sandikRepo.FindAsync(x => bagliSandikIds.Contains(x.Id))).ToList();

                if (bagliSandiklar.Any(SandikSevkKilidiHelper.SandikKilitliMi))
                    return Result.Failure(SandikSevkKilidiHelper.UrunKilitliMesaji, 409);

                foreach (var bagliIcerik in bagliIcerikListesi)
                {
                    // Seçili içerik GetByIdAsync ile zaten tracked. FindAsync AsNoTracking
                    // kopyasını aynı anahtarla attach etmek yerine tracked örneği sileriz.
                    sandikIcerikRepo.Remove(bagliIcerik.Id == icerik.Id ? icerik : bagliIcerik);
                }

                cekiSatiriRepo.Remove(bagliSatir);
                await SandikDurumlariniSilmeSonrasiHazirlaAsync(
                    sandikIcerikRepo,
                    sandikRepo,
                    bagliIcerikListesi,
                    sandik);
            }
            else
            {
                sandikIcerikRepo.Remove(icerik);
                await SandikDurumlariniSilmeSonrasiHazirlaAsync(
                    sandikIcerikRepo,
                    sandikRepo,
                    new[] { icerik },
                    sandik);
            }

            if (proje!.DurumId == (int)ProjeDurum.Tamamlandi)
            {
                proje.DurumId = (int)ProjeDurum.Hazirlaniyor;
                projeRepo.Update(proje);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                KullaniciId = _currentUserService.UserId ?? 0,
                ReferansTipi = "SandikIcerik",
                ReferansId = request.SandikIcerikId!.Value.ToString(),
                Islem = "Manuel Ürün Silindi",
                IslemTipiId = (int)IslemTipi.ManuelUrunSilindi,
                ReferansMetni = urunBilgi,
                Aciklama = $"Saha/Yedek ürün silindi: {urunBilgi}"
            });

            return Result.Success();
        }

        /// <summary>
        /// Normal projelerdeki manuel eklenen ürünleri siler.
        /// IsManuelEklenen=true ve üzerinde işlem yapılmamış olmalıdır.
        /// </summary>
        private async Task<Result> NormalManuelUrunSilAsync(
            ManuelUrunSilCommand request,
            CancellationToken cancellationToken)
        {
            var cekiSatiriRepo = _unitOfWork.GetRepository<CekiSatiri>();
            var satir = await cekiSatiriRepo.GetByIdAsync(request.CekiSatiriId!.Value);

            if (satir == null)
                return Result.Failure("Ürün bulunamadı.", 404);

            var ceki = await _unitOfWork.GetRepository<Ceki>().GetByIdAsync(satir.CekiId);
            if (ceki == null || ceki.ProjeId != request.ProjeId)
                return Result.Failure("Ürün bulunamadı veya belirtilen projeye ait değil.", 404);

            var projeRepo = _unitOfWork.GetRepository<Proje>();
            var proje = await projeRepo.GetByIdAsync(request.ProjeId);
            if (proje?.ProjeTipiId != (int)ProjeTipi.Normal)
            {
                return Result.Failure(
                    "Bu silme yolu yalnızca normal projelerdeki manuel ürünler için kullanılabilir.",
                    409);
            }

            if (satir.KaynakCekiSatiriId.HasValue)
            {
                return Result.Failure(
                    "Kaynak bağlantısı bulunan ürün doğrudan silinemez. İlgili aktarım veya karşılama geri alma akışını kullanın.",
                    409);
            }

            if (await SandikSevkKilidiHelper.CekiSatiriSevkEdilmisSandiktaMiAsync(_unitOfWork, satir))
                return Result.Failure(SandikSevkKilidiHelper.UrunKilitliMesaji);

            if (!satir.IsManuelEklenen)
                return Result.Failure("Sadece manuel eklenen ürünler silinebilir. Çekiden gelen ürünler silinemez.");

            if (await AktifSahaAktariminaBagliMiAsync(satir.Id, cancellationToken))
                return Result.Failure(SahaAktarimSilmeKorumaMesajlari.Urun, 409);

            if (ManuelUrunSilmeKurali.IslemGormusMu(satir))
            {
                return Result.Failure(
                    "Bu ürün üzerinde Grid, 3K, kalite veya süreç işlemi var. Silmeden önce işlemleri geri alın.");
            }

            var sandikIcerikRepo = _unitOfWork.GetRepository<SandikIcerik>();
            var ilgiliIcerikler = (await sandikIcerikRepo.FindAsync(x => x.CekiSatiriId == satir.Id)).ToList();
            foreach (var icerik in ilgiliIcerikler)
                sandikIcerikRepo.Remove(icerik);

            await SandikDurumlariniSilmeSonrasiHazirlaAsync(
                sandikIcerikRepo,
                _unitOfWork.GetRepository<Sandik>(),
                ilgiliIcerikler);

            if (proje?.DurumId == (int)ProjeDurum.Tamamlandi)
            {
                proje.DurumId = (int)ProjeDurum.Hazirlaniyor;
                projeRepo.Update(proje);
            }

            var urunBilgi = $"{satir.BarkodNo} - {satir.Aciklama} ({satir.IstenenAdet} adet)";
            cekiSatiriRepo.Remove(satir);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                KullaniciId = _currentUserService.UserId ?? 0,
                ReferansTipi = "CekiSatiri",
                ReferansId = request.CekiSatiriId!.Value.ToString(),
                Islem = "Manuel Ürün Silindi",
                IslemTipiId = (int)IslemTipi.ManuelUrunSilindi,
                ReferansMetni = urunBilgi,
                Aciklama = $"Manuel eklenen ürün silindi: {urunBilgi}"
            });

            return Result.Success();
        }

        private static async Task SandikDurumlariniSilmeSonrasiHazirlaAsync(
            IGenericRepository<SandikIcerik> sandikIcerikRepo,
            IGenericRepository<Sandik> sandikRepo,
            IReadOnlyCollection<SandikIcerik> silinecekIcerikler,
            Sandik? bilinenSandik = null)
        {
            var etkilenenSandikIds = silinecekIcerikler
                .Select(i => i.SandikId)
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (etkilenenSandikIds.Count == 0)
                return;

            var silinecekIcerikIds = silinecekIcerikler.Select(i => i.Id).ToHashSet();
            var kalanIcerikler = (await sandikIcerikRepo.FindAsync(i =>
                    etkilenenSandikIds.Contains(i.SandikId) &&
                    !silinecekIcerikIds.Contains(i.Id)))
                .ToList();

            foreach (var sandikId in etkilenenSandikIds)
            {
                var sandik = bilinenSandik?.Id == sandikId
                    ? bilinenSandik
                    : await sandikRepo.GetByIdAsync(sandikId);

                if (sandik == null || sandik.DurumId == (int)SandikDurum.Sevkedildi)
                    continue;

                var yeniDurumId = kalanIcerikler.Any(i =>
                        i.SandikId == sandikId &&
                        (i.TahsisMiktari != 0 ||
                         i.KonulanAdet != 0 ||
                         i.EksikAdet != 0 ||
                         i.StokKarsilanan != 0 ||
                         i.ProjeKarsilanan != 0 ||
                         i.TedarikciKarsilanan != 0 ||
                         i.Miktar != 0))
                    ? (int)SandikDurum.Hazirlaniyor
                    : (int)SandikDurum.Bos;

                if (sandik.DurumId == yeniDurumId)
                    continue;

                sandik.DurumId = yeniDurumId;
                sandikRepo.Update(sandik);
            }
        }

        private async Task<bool> AktifSahaAktariminaBagliMiAsync(
            int cekiSatiriId,
            CancellationToken cancellationToken)
        {
            var bagliSatirIds = await _sahaAktarimSilmeKorumaService
                .GetAktifAktarimBagliCekiSatiriIdsAsync(new[] { cekiSatiriId }, cancellationToken);

            return bagliSatirIds.Contains(cekiSatiriId);
        }
    }
}
