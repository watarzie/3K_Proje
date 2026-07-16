using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.ProjeIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.ProjeIslemleri.Commands
{
    public sealed class SahaSandikAktarimlariGeriAlCommand :
        IRequest<Result<SahaSandikAktarimlariGeriAlResultDto>>,
        ISecuredRequest,
        IRequiresMenuPermission
    {
        public string RequiredMenuKod => "saha-aktarim-geri-al";
        public int SahaSandikId { get; set; }
        public string? Aciklama { get; set; }
    }

    public sealed class SahaSandikAktarimlariGeriAlCommandHandler :
        IRequestHandler<SahaSandikAktarimlariGeriAlCommand, Result<SahaSandikAktarimlariGeriAlResultDto>>
    {
        private const string VarsayilanAciklama =
            "Kullanıcı tarafından saha sandığındaki tüm aktarımlar geri alındı.";

        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IHareketService _hareketService;
        private readonly ISahaTamamlamaService _sahaTamamlamaService;

        public SahaSandikAktarimlariGeriAlCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IHareketService hareketService,
            ISahaTamamlamaService sahaTamamlamaService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _hareketService = hareketService;
            _sahaTamamlamaService = sahaTamamlamaService;
        }

        public async Task<Result<SahaSandikAktarimlariGeriAlResultDto>> Handle(
            SahaSandikAktarimlariGeriAlCommand request,
            CancellationToken cancellationToken)
        {
            if (request.SahaSandikId <= 0)
            {
                return Result<SahaSandikAktarimlariGeriAlResultDto>.Failure(
                    "Geri alınacak saha sandığı seçilmelidir.",
                    400);
            }

            return await _unitOfWork.ExecuteInTransactionAsync(
                transactionCancellationToken => GeriAlAsync(request, transactionCancellationToken),
                cancellationToken);
        }

        private async Task<Result<SahaSandikAktarimlariGeriAlResultDto>> GeriAlAsync(
            SahaSandikAktarimlariGeriAlCommand request,
            CancellationToken cancellationToken)
        {
            var sandikRepo = _unitOfWork.GetRepository<Sandik>();
            var projeRepo = _unitOfWork.GetRepository<Proje>();
            var cekiRepo = _unitOfWork.GetRepository<Ceki>();
            var cekiSatiriRepo = _unitOfWork.GetRepository<CekiSatiri>();
            var sandikIcerikRepo = _unitOfWork.GetRepository<SandikIcerik>();
            var aktarimKalemiRepo = _unitOfWork.GetRepository<SahaAktarimKalemi>();

            var sahaSandik = await sandikRepo.GetByIdAsync(request.SahaSandikId);
            if (sahaSandik == null)
            {
                return Result<SahaSandikAktarimlariGeriAlResultDto>.Failure(
                    "Saha sandığı bulunamadı.",
                    404);
            }

            var sahaProje = await projeRepo.GetByIdAsync(sahaSandik.ProjeId);
            if (sahaProje == null)
            {
                return Result<SahaSandikAktarimlariGeriAlResultDto>.Failure(
                    "Saha projesi bulunamadı.",
                    404);
            }

            if (sahaProje.ProjeTipiId != (int)ProjeTipi.Saha)
            {
                return Result<SahaSandikAktarimlariGeriAlResultDto>.Failure(
                    "Toplu aktarım geri alma işlemi yalnızca saha sandıklarında yapılabilir.",
                    400);
            }

            if (sahaSandik.DurumId == (int)SandikDurum.Sevkedildi ||
                sahaSandik.SevkiyatDuzeltmeAcikMi)
            {
                return Result<SahaSandikAktarimlariGeriAlResultDto>.Failure(
                    "Saha sandığı sevk veya sevkiyat düzeltme kapsamında olduğu için aktarımlar geri alınamaz. Önce sevkiyatı geri alın.",
                    409);
            }

            var tumSandikIcerikleri = (await sandikIcerikRepo.FindAsync(i =>
                    i.SandikId == sahaSandik.Id))
                .ToList();

            var sandikCekiSatiriIds = tumSandikIcerikleri
                .Where(i => i.CekiSatiriId.HasValue)
                .Select(i => i.CekiSatiriId!.Value)
                .Distinct()
                .ToList();

            var sandikCekiSatirlari = (await cekiSatiriRepo.FindAsync(cs =>
                    sandikCekiSatiriIds.Contains(cs.Id)))
                .ToList();

            var sahaAktarimSatirlari = sandikCekiSatirlari
                .Where(cs => cs.KaynakCekiSatiriId.HasValue)
                .OrderBy(cs => cs.SiraNo)
                .ToList();

            if (sahaAktarimSatirlari.Count == 0)
            {
                return Result<SahaSandikAktarimlariGeriAlResultDto>.Failure(
                    "Seçilen saha sandığında geri alınabilecek aktarım bulunamadı.",
                    404);
            }

            var sahaAktarimSatirIds = sahaAktarimSatirlari
                .Select(cs => cs.Id)
                .ToHashSet();

            var tumAktarimIcerikleri = (await sandikIcerikRepo.FindAsync(i =>
                    i.CekiSatiriId.HasValue &&
                    sahaAktarimSatirIds.Contains(i.CekiSatiriId.Value)))
                .ToList();

            if (tumAktarimIcerikleri.Any(i => i.SandikId != sahaSandik.Id))
            {
                return Result<SahaSandikAktarimlariGeriAlResultDto>.Failure(
                    "Aktarım satırlarından biri başka bir saha sandığında da kullanılıyor. Veri bütünlüğü kontrol edilmeden toplu geri alma yapılamaz.",
                    409);
            }

            var sahaCekiIds = sahaAktarimSatirlari
                .Select(cs => cs.CekiId)
                .Distinct()
                .ToList();

            var sahaCekiler = (await cekiRepo.FindAsync(c => sahaCekiIds.Contains(c.Id)))
                .ToList();

            if (sahaCekiler.Count != sahaCekiIds.Count ||
                sahaCekiler.Any(c => c.ProjeId != sahaProje.Id))
            {
                return Result<SahaSandikAktarimlariGeriAlResultDto>.Failure(
                    "Sandıktaki aktarım satırları saha projesiyle eşleşmiyor. Veri bütünlüğü kontrol edilmeden toplu geri alma yapılamaz.",
                    409);
            }

            var tumAktarimKalemleri = (await aktarimKalemiRepo.FindAsync(k =>
                    (k.SahaCekiSatiriId.HasValue &&
                     sahaAktarimSatirIds.Contains(k.SahaCekiSatiriId.Value)) ||
                    k.SahaSandikId == sahaSandik.Id))
                .ToList();

            var aktifSandikKalemleri = tumAktarimKalemleri
                .Where(k => k.SahaSandikId == sahaSandik.Id && AktifMi(k))
                .ToList();

            if (aktifSandikKalemleri.Any(k =>
                    !k.SahaCekiSatiriId.HasValue ||
                    !sahaAktarimSatirIds.Contains(k.SahaCekiSatiriId.Value) ||
                    k.SahaProjeId != sahaProje.Id))
            {
                return Result<SahaSandikAktarimlariGeriAlResultDto>.Failure(
                    "Sandığın aktif aktarım defterinde hedef satır eşleşmesi bulunmayan kayıtlar var. Veri bütünlüğü kontrol edilmeden toplu geri alma yapılamaz.",
                    409);
            }

            var aktifSatirKalemleri = tumAktarimKalemleri
                .Where(k =>
                    k.SahaCekiSatiriId.HasValue &&
                    sahaAktarimSatirIds.Contains(k.SahaCekiSatiriId.Value) &&
                    AktifMi(k))
                .ToList();

            if (aktifSatirKalemleri.Any(k =>
                    k.SahaSandikId.HasValue &&
                    k.SahaSandikId.Value != sahaSandik.Id))
            {
                return Result<SahaSandikAktarimlariGeriAlResultDto>.Failure(
                    "Aktarım satırlarından biri farklı bir hedef sandıkla ilişkilidir. Veri bütünlüğü kontrol edilmeden toplu geri alma yapılamaz.",
                    409);
            }

            var hedefKaynakSatirMap = sahaAktarimSatirlari.ToDictionary(
                cs => cs.Id,
                cs => cs.KaynakCekiSatiriId!.Value);

            if (aktifSatirKalemleri.Any(k =>
                    k.SahaProjeId != sahaProje.Id ||
                    !k.SahaCekiSatiriId.HasValue ||
                    hedefKaynakSatirMap[k.SahaCekiSatiriId.Value] != k.KaynakCekiSatiriId))
            {
                return Result<SahaSandikAktarimlariGeriAlResultDto>.Failure(
                    "Aktarım defterindeki kaynak veya hedef proje ilişkisi sandık içeriğiyle eşleşmiyor. Veri bütünlüğü kontrol edilmeden toplu geri alma yapılamaz.",
                    409);
            }

            if (aktifSatirKalemleri.Any(SevkiyatKapsamindaMi))
            {
                return Result<SahaSandikAktarimlariGeriAlResultDto>.Failure(
                    "Sandıktaki aktarımlardan biri sevkiyat kapsamındadır. Önce ilgili sevkiyatı geri alın.",
                    409);
            }

            var icerikMap = tumAktarimIcerikleri
                .GroupBy(i => i.CekiSatiriId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            var islemGormusSatir = sahaAktarimSatirlari.FirstOrDefault(satir =>
                SahaAktarimGeriAlmaPolicy.IslemGormusMu(
                    satir,
                    icerikMap.GetValueOrDefault(satir.Id) ?? []));

            if (islemGormusSatir != null)
            {
                return Result<SahaSandikAktarimlariGeriAlResultDto>.Failure(
                    $"Sandık {sahaSandik.SandikNo} içindeki #{islemGormusSatir.SiraNo} numaralı aktarımda Grid/3K veya karşılama işlemi başlamıştır. Hiçbir kayıt değiştirilmedi; önce ilgili işlemleri geri alın.",
                    409);
            }

            var kaynakSatirIds = sahaAktarimSatirlari
                .Select(cs => cs.KaynakCekiSatiriId!.Value)
                .Distinct()
                .ToList();

            var kaynakSatirlar = (await cekiSatiriRepo.FindAsync(cs => kaynakSatirIds.Contains(cs.Id)))
                .ToList();

            if (kaynakSatirlar.Count != kaynakSatirIds.Count)
            {
                return Result<SahaSandikAktarimlariGeriAlResultDto>.Failure(
                    "Aktarımlardan bazılarının kaynak ürün kaydı bulunamadı. Veri bütünlüğü kontrol edilmeden toplu geri alma yapılamaz.",
                    409);
            }

            var kaynakCekiIds = kaynakSatirlar
                .Select(cs => cs.CekiId)
                .Distinct()
                .ToList();

            var kaynakCekiler = (await cekiRepo.FindAsync(c => kaynakCekiIds.Contains(c.Id)))
                .ToList();

            if (kaynakCekiler.Count != kaynakCekiIds.Count)
            {
                return Result<SahaSandikAktarimlariGeriAlResultDto>.Failure(
                    "Aktarımlardan bazılarının kaynak çeki kaydı bulunamadı. Veri bütünlüğü kontrol edilmeden toplu geri alma yapılamaz.",
                    409);
            }

            var kaynakProjeIds = kaynakCekiler
                .Select(c => c.ProjeId)
                .Distinct()
                .ToList();

            var kaynakProjeler = (await projeRepo.FindAsync(p => kaynakProjeIds.Contains(p.Id)))
                .ToList();

            if (kaynakProjeler.Count != kaynakProjeIds.Count)
            {
                return Result<SahaSandikAktarimlariGeriAlResultDto>.Failure(
                    "Aktarımlardan bazılarının kaynak projesi bulunamadı. Veri bütünlüğü kontrol edilmeden toplu geri alma yapılamaz.",
                    409);
            }

            var aciklama = string.IsNullOrWhiteSpace(request.Aciklama)
                ? VarsayilanAciklama
                : request.Aciklama.Trim();
            var geriAlmaTarihi = TurkeyTime.Now;

            foreach (var kalem in tumAktarimKalemleri.Where(k =>
                         k.SahaCekiSatiriId.HasValue &&
                         sahaAktarimSatirIds.Contains(k.SahaCekiSatiriId.Value)))
            {
                if (AktifMi(kalem))
                {
                    kalem.DurumId = (int)SahaAktarimDurum.GeriAlindi;
                    kalem.SevkiyatKapsamindaMi = false;
                    kalem.DuzeltmeyeAcikMi = false;
                    kalem.GeriAlmaTarihi = geriAlmaTarihi;
                    kalem.GeriAlmaAciklama = aciklama;
                }

                // Hedef satırlar silinmeden önce tarihsel defter bağlantısı koparılır.
                kalem.SahaCekiSatiriId = null;
                aktarimKalemiRepo.Update(kalem);
            }

            var silinenIcerikIds = tumAktarimIcerikleri
                .Select(i => i.Id)
                .ToHashSet();

            foreach (var icerik in tumAktarimIcerikleri)
                sandikIcerikRepo.Remove(icerik);

            foreach (var satir in sahaAktarimSatirlari)
                cekiSatiriRepo.Remove(satir);

            var kalanIcerikVar = tumSandikIcerikleri.Any(i => !silinenIcerikIds.Contains(i.Id));
            if (!kalanIcerikVar)
            {
                sahaSandik.DurumId = (int)SandikDurum.Bos;
            }
            else if (sahaSandik.DurumId == (int)SandikDurum.Bos)
            {
                sahaSandik.DurumId = (int)SandikDurum.Hazirlaniyor;
            }

            sandikRepo.Update(sahaSandik);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _sahaTamamlamaService.SenkronizeKaynakProjelerAsync(
                kaynakSatirIds,
                cancellationToken);

            var toplamMiktar = sahaAktarimSatirlari.Sum(cs => cs.IstenenAdet);
            await AuditKaydetAsync(
                sahaSandik,
                sahaProje,
                sahaAktarimSatirlari,
                kaynakSatirlar,
                kaynakCekiler,
                kaynakProjeler,
                sahaAktarimSatirlari.Count,
                aktifSatirKalemleri.Count,
                aciklama);

            var sandikDurumMetni = GetSandikDurumMetni(sahaSandik.DurumId);

            return Result<SahaSandikAktarimlariGeriAlResultDto>.Success(
                new SahaSandikAktarimlariGeriAlResultDto
                {
                    SahaSandikId = sahaSandik.Id,
                    SahaSandikNo = sahaSandik.SandikNo,
                    GeriAlinanSatirSayisi = sahaAktarimSatirlari.Count,
                    GeriAlinanToplamMiktar = toplamMiktar,
                    SandikBosaldiMi = sahaSandik.DurumId == (int)SandikDurum.Bos,
                    SandikDurumId = sahaSandik.DurumId,
                    SandikDurumu = sandikDurumMetni
                });
        }

        private async Task AuditKaydetAsync(
            Sandik sahaSandik,
            Proje sahaProje,
            IReadOnlyCollection<CekiSatiri> sahaAktarimSatirlari,
            IReadOnlyCollection<CekiSatiri> kaynakSatirlar,
            IReadOnlyCollection<Ceki> kaynakCekiler,
            IReadOnlyCollection<Proje> kaynakProjeler,
            int satirSayisi,
            int aktarimSayisi,
            string aciklama)
        {
            var kullaniciId = _currentUserService.UserId ?? 0;
            var yeniSandikDurumu = GetSandikDurumMetni(sahaSandik.DurumId);

            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = sahaProje.Id,
                ReferansTipi = "Sandik",
                ReferansId = sahaSandik.Id.ToString(),
                Islem = "Sandık Aktarımları Geri Alındı",
                IslemTipiId = null,
                KullaniciId = kullaniciId,
                EskiDeger = $"Sandık:{sahaSandik.SandikNo}, Satır:{satirSayisi}, Aktarım:{aktarimSayisi}",
                YeniDeger = $"Aktarımlar kaldırıldı, SandıkDurumu:{yeniSandikDurumu}",
                Aciklama = $"Saha sandığı {sahaSandik.SandikNo} içindeki {satirSayisi} aktarım satırı tek işlemle geri alındı. {aciklama}".Trim()
            });

            var kaynakCekiProjeMap = kaynakCekiler.ToDictionary(c => c.Id, c => c.ProjeId);
            var kaynakProjeMap = kaynakProjeler.ToDictionary(p => p.Id);

            foreach (var projeGrubu in kaynakSatirlar
                         .Where(cs => kaynakCekiProjeMap.ContainsKey(cs.CekiId))
                         .GroupBy(cs => kaynakCekiProjeMap[cs.CekiId]))
            {
                if (!kaynakProjeMap.TryGetValue(projeGrubu.Key, out var kaynakProje))
                    continue;

                var satirIds = projeGrubu.Select(cs => cs.Id).Distinct().ToList();
                var satirIdSet = satirIds.ToHashSet();
                var projeAktarimSatirSayisi = sahaAktarimSatirlari.Count(cs =>
                    cs.KaynakCekiSatiriId.HasValue &&
                    satirIdSet.Contains(cs.KaynakCekiSatiriId.Value));

                await _hareketService.HareketKaydetAsync(new HareketGecmisi
                {
                    ProjeId = kaynakProje.Id,
                    ReferansTipi = "TopluCekiSatiri",
                    ReferansId = string.Join(",", satirIds),
                    Islem = "Sandık Aktarımları Geri Alındı",
                    IslemTipiId = null,
                    KullaniciId = kullaniciId,
                    EskiDeger = $"SahaProje:{sahaProje.ProjeNo}, SahaSandık:{sahaSandik.SandikNo}, Satır:{projeAktarimSatirSayisi}",
                    YeniDeger = "Kaynak ürünler normal proje eksik takibine geri döndü",
                    Aciklama = $"{sahaProje.ProjeNo} saha projesindeki {sahaSandik.SandikNo} numaralı sandığın aktarımları geri alındı. {aciklama}".Trim()
                });
            }
        }

        private static bool AktifMi(SahaAktarimKalemi kalem)
        {
            return kalem.DurumId != (int)SahaAktarimDurum.GeriAlindi &&
                kalem.DurumId != (int)SahaAktarimDurum.Iptal;
        }

        private static bool SevkiyatKapsamindaMi(SahaAktarimKalemi kalem)
        {
            return kalem.SevkiyatKapsamindaMi ||
                kalem.DurumId == (int)SahaAktarimDurum.SevkiyatDuzeltmede ||
                kalem.DurumId == (int)SahaAktarimDurum.SevkEdildi;
        }

        private static string GetSandikDurumMetni(int durumId)
        {
            return (SandikDurum)durumId switch
            {
                SandikDurum.Bos => "Boş",
                SandikDurum.Hazirlaniyor => "Hazırlanıyor",
                SandikDurum.Kapandi => "Kapandı",
                SandikDurum.Sevkedildi => "Sevk Edildi",
                _ => "Bilinmiyor"
            };
        }
    }
}
