using System.Globalization;
using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.ProjeIslemleri.DTOs;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.ProjeIslemleri.Commands
{
    /// <summary>
    /// Bir saha aktarımının (otomatik ya da elle, tek aktarım başlığı) bütün aktif satırlarını hepsi-ya-hiçbiri geri alır.
    /// Satır geri alma ile aynı kural (<see cref="SahaAktarimGeriAlmaPolicy"/>) geçerlidir: satırlardan biri işlem
    /// görmüş, sevkiyatta ya da sevk edilmiş sandıktaysa hiçbir kayıt değişmez.
    /// </summary>
    public sealed class SahaAktarimiGeriAlCommand :
        IRequest<Result<SahaAktarimiGeriAlSonucDto>>,
        ISecuredRequest,
        IRequiresMenuPermission
    {
        public string RequiredMenuKod => YetkiKodlari.Saha.AktarimGeriAl;
        public YetkiTipi RequiredYetkiTipi => YetkiTipi.W;
        public int SahaAktarimId { get; set; }
        public string? Aciklama { get; set; }
    }

    public sealed class SahaAktarimiGeriAlCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IHareketService hareketService,
        ISahaTamamlamaService sahaTamamlamaService) :
        IRequestHandler<SahaAktarimiGeriAlCommand, Result<SahaAktarimiGeriAlSonucDto>>
    {
        private const string VarsayilanAciklama = "Kullanıcı tarafından saha aktarımının tamamı geri alındı.";

        public Task<Result<SahaAktarimiGeriAlSonucDto>> Handle(
            SahaAktarimiGeriAlCommand request,
            CancellationToken cancellationToken) =>
            unitOfWork.ExecuteAtomicAsync(token => GeriAlAsync(request, token), cancellationToken);

        private async Task<Result<SahaAktarimiGeriAlSonucDto>> GeriAlAsync(
            SahaAktarimiGeriAlCommand request,
            CancellationToken cancellationToken)
        {
            var aktarim = await unitOfWork.GetRepository<SahaAktarim>().GetByIdAsync(request.SahaAktarimId);
            if (aktarim == null)
                return Result<SahaAktarimiGeriAlSonucDto>.Failure("Saha aktarımı bulunamadı.", 404);

            // Taşıma başlığını geri almak ürünleri ana projeye döndürürdü; yanlış taşıma ters yönde yeni taşımayla düzeltilir.
            if (aktarim.KaynakSahaProjeId.HasValue)
            {
                return Result<SahaAktarimiGeriAlSonucDto>.Failure(
                    "Sahadan sahaya taşıma geri alınmaz; ürünleri ters yönde yeni bir taşımayla geri götürün.",
                    409);
            }

            var projeRepo = unitOfWork.GetRepository<Proje>();
            var sahaProje = await projeRepo.GetByIdAsync(aktarim.SahaProjeId);
            if (sahaProje == null || sahaProje.ProjeTipiId != (int)ProjeTipi.Saha)
                return Result<SahaAktarimiGeriAlSonucDto>.Failure("Aktarımın saha projesi bulunamadı.", 404);

            var kalemRepo = unitOfWork.GetRepository<SahaAktarimKalemi>();
            var aktifKalemler = (await kalemRepo.FindAsync(k =>
                    k.SahaAktarimId == aktarim.Id &&
                    k.SahaCekiSatiriId.HasValue &&
                    k.DurumId != (int)SahaAktarimDurum.GeriAlindi &&
                    k.DurumId != (int)SahaAktarimDurum.Iptal))
                .ToList();

            if (aktifKalemler.Count == 0)
            {
                return Result<SahaAktarimiGeriAlSonucDto>.Failure(
                    "Bu aktarımda geri alınabilecek satır kalmadı.",
                    404);
            }

            var sahaSatirIds = aktifKalemler.Select(k => k.SahaCekiSatiriId!.Value).Distinct().ToList();
            var cekiSatiriRepo = unitOfWork.GetRepository<CekiSatiri>();
            var sahaSatirlar = (await cekiSatiriRepo.FindAsync(cs => sahaSatirIds.Contains(cs.Id)))
                .OrderBy(cs => cs.SiraNo)
                .ToList();

            var sahaCekiIds = sahaSatirlar.Select(cs => cs.CekiId).Distinct().ToList();
            var sahaCekiler = (await unitOfWork.GetRepository<Ceki>().FindAsync(c => sahaCekiIds.Contains(c.Id))).ToList();
            if (sahaSatirlar.Count != sahaSatirIds.Count ||
                sahaSatirlar.Any(cs => !cs.KaynakCekiSatiriId.HasValue) ||
                sahaCekiler.Count != sahaCekiIds.Count ||
                sahaCekiler.Any(c => c.ProjeId != sahaProje.Id))
            {
                return Result<SahaAktarimiGeriAlSonucDto>.Failure(
                    "Aktarım defteri saha projesindeki satırlarla eşleşmiyor. Veri bütünlüğü kontrol edilmeden toplu geri alma yapılamaz.",
                    409);
            }

            var icerikRepo = unitOfWork.GetRepository<SandikIcerik>();
            var satirIcerikleri = (await icerikRepo.FindAsync(i =>
                    i.CekiSatiriId.HasValue &&
                    sahaSatirIds.Contains(i.CekiSatiriId.Value)))
                .ToList();
            var sandikIds = satirIcerikleri.Select(i => i.SandikId).Distinct().ToList();
            var sandikRepo = unitOfWork.GetRepository<Sandik>();
            var sandiklar = (await sandikRepo.FindAsync(s => sandikIds.Contains(s.Id))).ToList();

            var engeller = new List<string>();
            foreach (var sandik in sandiklar.Where(s =>
                         s.DurumId == (int)SandikDurum.Sevkedildi || s.SevkiyatDuzeltmeAcikMi))
            {
                engeller.Add($"Sandık {sandik.SandikNo} sevk edildi ya da sevkiyat düzeltmesinde; önce sevkiyatı geri alın.");
            }

            if (aktifKalemler.Any(k =>
                    k.SevkiyatKapsamindaMi ||
                    k.DurumId == (int)SahaAktarimDurum.SevkiyatDuzeltmede ||
                    k.DurumId == (int)SahaAktarimDurum.SevkEdildi))
            {
                engeller.Add("Aktarımın bir kısmı sevkiyat kapsamında; önce ilgili sevkiyatı geri alın.");
            }

            var icerikMap = satirIcerikleri
                .GroupBy(i => i.CekiSatiriId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());
            foreach (var satir in sahaSatirlar.Where(s =>
                         SahaAktarimGeriAlmaPolicy.IslemGormusMu(s, icerikMap.GetValueOrDefault(s.Id) ?? [])))
            {
                engeller.Add($"#{satir.SiraNo} {satir.Aciklama}: Grid/3K veya karşılama işlemi başlamış.");
            }

            if (engeller.Count > 0)
            {
                return Result<SahaAktarimiGeriAlSonucDto>.Failure(
                    "Aktarım geri alınamadı; hiçbir kayıt değiştirilmedi. Önce aşağıdaki işlemleri geri alın.",
                    409,
                    engeller);
            }

            var aciklama = string.IsNullOrWhiteSpace(request.Aciklama) ? VarsayilanAciklama : request.Aciklama.Trim();
            var geriAlmaTarihi = TurkeyTime.Now;

            // Satırlara bağlı bütün defter kayıtları (bu başlık dışındaki eski kayıtlar dahil) satır silinmeden önce koparılır.
            var satirKalemleri = (await kalemRepo.FindAsync(k =>
                    k.SahaCekiSatiriId.HasValue &&
                    sahaSatirIds.Contains(k.SahaCekiSatiriId.Value)))
                .ToList();
            foreach (var kalem in satirKalemleri)
            {
                if (kalem.DurumId != (int)SahaAktarimDurum.GeriAlindi && kalem.DurumId != (int)SahaAktarimDurum.Iptal)
                {
                    kalem.DurumId = (int)SahaAktarimDurum.GeriAlindi;
                    kalem.SevkiyatKapsamindaMi = false;
                    kalem.DuzeltmeyeAcikMi = false;
                    kalem.GeriAlmaTarihi = geriAlmaTarihi;
                    kalem.GeriAlmaAciklama = aciklama;
                }

                kalem.SahaCekiSatiriId = null;
                kalemRepo.Update(kalem);
            }

            var silinenIcerikIds = satirIcerikleri.Select(i => i.Id).ToHashSet();
            foreach (var icerik in satirIcerikleri)
                icerikRepo.Remove(icerik);
            foreach (var satir in sahaSatirlar)
                cekiSatiriRepo.Remove(satir);

            var kalanIcerikliSandikIds = sandikIds.Count == 0
                ? new HashSet<int>()
                : (await icerikRepo.FindAsync(i => sandikIds.Contains(i.SandikId)))
                    .Where(i => !silinenIcerikIds.Contains(i.Id))
                    .Select(i => i.SandikId)
                    .ToHashSet();
            var bosalanSandikSayisi = 0;
            foreach (var sandik in sandiklar.Where(s => !kalanIcerikliSandikIds.Contains(s.Id)))
            {
                // Sandık silinmez; aynı numara yeniden aktarımda boş sandık olarak tekrar kullanılır.
                sandik.DurumId = (int)SandikDurum.Bos;
                sandikRepo.Update(sandik);
                bosalanSandikSayisi++;
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            var kaynakSatirIds = sahaSatirlar.Select(cs => cs.KaynakCekiSatiriId!.Value).Distinct().ToList();
            await sahaTamamlamaService.SenkronizeKaynakProjelerAsync(kaynakSatirIds, cancellationToken);

            var toplamMiktar = sahaSatirlar.Sum(cs => cs.IstenenAdet);
            var kullaniciId = currentUserService.UserId ?? 0;
            await hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = sahaProje.Id,
                ReferansTipi = "SahaAktarim",
                ReferansId = aktarim.Id.ToString(CultureInfo.InvariantCulture),
                Islem = "Saha Aktarımı Geri Alındı",
                IslemTipiId = null,
                KullaniciId = kullaniciId,
                EskiDeger = $"Aktarım:{aktarim.Id}, Satır:{sahaSatirlar.Count}, Miktar:{FormatAdet(toplamMiktar)}",
                YeniDeger = $"Aktarım kaldırıldı, boşalan sandık:{bosalanSandikSayisi}",
                Aciklama = $"{aktarim.Aciklama} aktarımının {sahaSatirlar.Count} satırı tek işlemle geri alındı. {aciklama}".Trim()
            });

            var kaynakProjeIds = aktifKalemler.Select(k => k.KaynakProjeId).Distinct().ToList();
            foreach (var kaynakProjeId in kaynakProjeIds)
            {
                var projeKalemleri = aktifKalemler.Where(k => k.KaynakProjeId == kaynakProjeId).ToList();
                await hareketService.HareketKaydetAsync(new HareketGecmisi
                {
                    ProjeId = kaynakProjeId,
                    ReferansTipi = "TopluCekiSatiri",
                    ReferansId = string.Join(",", projeKalemleri.Select(k => k.KaynakCekiSatiriId).Distinct()),
                    Islem = "Saha Aktarımı Geri Alındı",
                    IslemTipiId = null,
                    KullaniciId = kullaniciId,
                    EskiDeger = $"SahaProje:{sahaProje.ProjeNo}, Aktarım:{aktarim.Id}, Satır:{projeKalemleri.Count}",
                    YeniDeger = "Kaynak ürünler normal proje eksik takibine geri döndü",
                    Aciklama = $"{sahaProje.ProjeNo} saha projesine yapılan aktarım geri alındı. {aciklama}".Trim()
                });
            }

            return Result<SahaAktarimiGeriAlSonucDto>.Success(new SahaAktarimiGeriAlSonucDto
            {
                SahaAktarimId = aktarim.Id,
                SahaProjeId = sahaProje.Id,
                GeriAlinanSatirSayisi = sahaSatirlar.Count,
                GeriAlinanToplamMiktar = toplamMiktar,
                BosalanSandikSayisi = bosalanSandikSayisi
            });
        }

        private static string FormatAdet(decimal value)
        {
            return decimal.Truncate(value) == value
                ? decimal.Truncate(value).ToString("0", CultureInfo.InvariantCulture)
                : value.ToString("0.####", CultureInfo.GetCultureInfo("tr-TR"));
        }
    }
}
