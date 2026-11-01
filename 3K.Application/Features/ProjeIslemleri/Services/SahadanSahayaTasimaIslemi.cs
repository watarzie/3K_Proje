using System.Globalization;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.ProjeIslemleri.Services;

/// <summary>
/// Sahadan sahaya taşıma isteği: ya seçilen saha satırları (<see cref="SahaCekiSatiriIds"/>) ya da bir saha sandığının
/// bütün içeriği (<see cref="KaynakSandikId"/>) hedef saha projesinin mevcut ya da yeni sandığına taşınır.
/// </summary>
internal sealed record SahadanSahayaTasimaTalebi(
    int KaynakSahaProjeId,
    IReadOnlyCollection<int> SahaCekiSatiriIds,
    int? KaynakSandikId,
    int HedefSahaProjeId,
    int? HedefSandikId,
    string? YeniSandikNo);

/// <summary>Taşınacak tek saha satırı: satır, sandık içeriği, aktif defter kaydı ve bulunduğu sandık.</summary>
internal sealed record SahaTasimaSatiri(CekiSatiri Satir, SandikIcerik Icerik, SahaAktarimKalemi Kalem, Sandik KaynakSandik);

/// <summary>Doğrulanmış taşıma: engel listesi boş değilse hiçbir kayıt yazılmaz.</summary>
internal sealed class SahadanSahayaTasimaHazirligi
{
    public required Proje KaynakProje { get; init; }
    public required Proje HedefProje { get; init; }
    public required IReadOnlyList<SahaTasimaSatiri> Satirlar { get; init; }
    public required IReadOnlyList<SandikIcerik> ManuelIcerikler { get; init; }
    public required IReadOnlyList<Sandik> KaynakSandiklar { get; init; }
    public required IReadOnlyDictionary<int, CekiSatiri> KokSatirlar { get; init; }
    public required IReadOnlyDictionary<int, string> KokProjeNolari { get; init; }
    public Sandik? HedefSandik { get; init; }
    public required string HedefSandikNo { get; init; }
    public required IReadOnlyList<string> Engeller { get; init; }
    /// <summary>Taşımadan sonra içeriği kalmayan (Boş olacak) kaynak sandık numaraları.</summary>
    public required IReadOnlyList<string> BosalacakSandikNolari { get; init; }
    /// <summary>Taşımadan sonra içeriği kalmayan kaynak sandıkların kimlikleri.</summary>
    public required IReadOnlySet<int> BosalacakSandikIds { get; init; }

    /// <summary>
    /// Kutunun kendisi taşınıyor mu: tek kaynak sandık var ve taşımadan sonra boşalıyor. Yalnız o zaman yeni hedef
    /// sandığa kaynak sandığın adı, tipi, ölçüsü, ağırlığı ve lokasyonu kopyalanır; içinde ürün kalan sandığın ölçüsü
    /// kopyalanırsa aynı kutu iki sahada sayılır (packing list ağırlığı, m³).
    /// </summary>
    public bool ButunSandikTasiniyor => KaynakSandiklar.Count == 1 && BosalacakSandikIds.Contains(KaynakSandiklar[0].Id);
}

internal sealed record SahadanSahayaTasimaSonucu(
    SahaAktarim Baslik,
    Sandik HedefSandik,
    int TasinanSatirSayisi,
    int TasinanManuelIcerikSayisi,
    decimal ToplamMiktar,
    IReadOnlyList<string> BosalanSandikNolari);

/// <summary>
/// Sahadan sahaya taşıma motoru. Satır kopyalanmaz: aynı saha satırı (Grid/3K sayaçları, ürün bilgisi, kalite) hedef
/// sahanın tamamlama çekisine, sandık içeriği hedef sandığa, aktif defter kaydı hedef sahaya geçer; böylece aynı miktar
/// iki sahada sayılmaz ve kök satırın kalanı değişmez. Her taşınan satır için <see cref="SahaTasimaKalemi"/> izi yazılır.
/// <see cref="HazirlaAsync"/> veri değiştirmez; <see cref="UygulaAsync"/> çağıranın transaction'ında çalışır.
/// </summary>
internal sealed class SahadanSahayaTasimaIslemi(
    IUnitOfWork unitOfWork,
    IHareketService hareketService,
    ICurrentUserService currentUserService,
    ISahaTamamlamaService sahaTamamlamaService)
{
    /// <summary>Taşımayı yükler ve doğrular; yalnız okur (önizleme sorgusu da bunu kullanır).</summary>
    public static async Task<Result<SahadanSahayaTasimaHazirligi>> HazirlaAsync(
        IUnitOfWork unitOfWork,
        SahadanSahayaTasimaTalebi talep,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var satirModu = talep.SahaCekiSatiriIds.Count > 0;
        if (satirModu == talep.KaynakSandikId.HasValue)
            return Result<SahadanSahayaTasimaHazirligi>.Failure("Taşımak için ya ürün satırları ya da bir sandık seçilmelidir.");
        if (talep.KaynakSahaProjeId == talep.HedefSahaProjeId)
            return Result<SahadanSahayaTasimaHazirligi>.Failure("Kaynak ve hedef saha projesi aynı olamaz.");

        var projeRepo = unitOfWork.GetRepository<Proje>();
        var kaynakProje = await projeRepo.GetByIdAsync(talep.KaynakSahaProjeId);
        if (kaynakProje == null)
            return Result<SahadanSahayaTasimaHazirligi>.Failure("Kaynak saha projesi bulunamadı.", 404);
        var hedefProje = await projeRepo.GetByIdAsync(talep.HedefSahaProjeId);
        if (hedefProje == null)
            return Result<SahadanSahayaTasimaHazirligi>.Failure("Hedef saha projesi bulunamadı.", 404);
        if (kaynakProje.ProjeTipiId != (int)ProjeTipi.Saha || hedefProje.ProjeTipiId != (int)ProjeTipi.Saha)
            return Result<SahadanSahayaTasimaHazirligi>.Failure("Taşıma yalnız iki saha projesi arasında yapılabilir.");

        var engeller = new List<string>();
        // Engel proje değil sandık düzeyindedir (24.10.2026): Eksik Sevk Edildi sahanın sevk edilmemiş sandığındaki ürünler
        // taşınır ve Eksik Sevk Edildi saha da hedef olur; ürün yalnız sevk edilmemiş ya da yeni açılan sandığa gider. Sevk
        // edilmiş ya da sevkiyat düzeltmesindeki kaynak/hedef sandık ve sevkiyat kapsamındaki satır aşağıda engellenir.
        // Tamamen sevk edilmiş (Sevk Edildi) saha hedef olamaz: sevkli projeye yeni sevk edilmemiş sandık açılmaz.
        if (hedefProje.DurumId == (int)ProjeDurum.SevkEdildi)
            engeller.Add($"Hedef saha projesi {hedefProje.ProjeNo} tamamen sevk edilmiş; sevk edilmiş projeye ürün taşınamaz.");

        var cekiRepo = unitOfWork.GetRepository<Ceki>();
        var kaynakCekiIds = (await cekiRepo.FindAsync(c => c.ProjeId == kaynakProje.Id)).Select(c => c.Id).ToList();
        var cekiSatiriRepo = unitOfWork.GetRepository<CekiSatiri>();
        var icerikRepo = unitOfWork.GetRepository<SandikIcerik>();
        var sandikRepo = unitOfWork.GetRepository<Sandik>();

        List<CekiSatiri> satirlar;
        var manuelIcerikler = new List<SandikIcerik>();
        Sandik? seciliKaynakSandik = null;
        if (satirModu)
        {
            var satirIds = talep.SahaCekiSatiriIds.Where(id => id > 0).Distinct().ToList();
            satirlar = (await cekiSatiriRepo.FindAsync(cs => satirIds.Contains(cs.Id))).ToList();
            if (satirlar.Count != satirIds.Count || satirlar.Any(cs => !kaynakCekiIds.Contains(cs.CekiId)))
                return Result<SahadanSahayaTasimaHazirligi>.Failure("Seçilen satırlardan bazıları kaynak saha projesinde bulunamadı.", 404);
        }
        else
        {
            seciliKaynakSandik = (await sandikRepo.FindAsync(s => s.Id == talep.KaynakSandikId!.Value)).SingleOrDefault();
            if (seciliKaynakSandik == null || seciliKaynakSandik.ProjeId != kaynakProje.Id)
                return Result<SahadanSahayaTasimaHazirligi>.Failure("Kaynak sandık saha projesinde bulunamadı.", 404);

            var sandikIcerikleri = (await icerikRepo.FindAsync(i => i.SandikId == seciliKaynakSandik.Id)).ToList();
            manuelIcerikler = sandikIcerikleri.Where(i => !i.CekiSatiriId.HasValue).ToList();
            var sandikSatirIds = sandikIcerikleri.Where(i => i.CekiSatiriId.HasValue).Select(i => i.CekiSatiriId!.Value).Distinct().ToList();
            satirlar = sandikSatirIds.Count == 0
                ? []
                : (await cekiSatiriRepo.FindAsync(cs => sandikSatirIds.Contains(cs.Id))).ToList();
            if (satirlar.Count == 0 && manuelIcerikler.Count == 0)
                return Result<SahadanSahayaTasimaHazirligi>.Failure("Sandıkta taşınacak ürün yok.");
        }

        if (satirlar.Any(cs => !cs.KaynakCekiSatiriId.HasValue))
            return Result<SahadanSahayaTasimaHazirligi>.Failure("Yalnız normal projeden aktarılmış saha satırları taşınabilir.");

        var satirIdleri = satirlar.Select(cs => cs.Id).ToList();
        var satirIcerikleri = satirIdleri.Count == 0
            ? []
            : (await icerikRepo.FindAsync(i => i.CekiSatiriId.HasValue && satirIdleri.Contains(i.CekiSatiriId.Value))).ToList();
        var kalemRepo = unitOfWork.GetRepository<SahaAktarimKalemi>();
        var aktifKalemler = satirIdleri.Count == 0
            ? []
            : (await kalemRepo.FindAsync(k =>
                    k.SahaCekiSatiriId.HasValue &&
                    satirIdleri.Contains(k.SahaCekiSatiriId.Value) &&
                    k.DurumId != (int)SahaAktarimDurum.GeriAlindi &&
                    k.DurumId != (int)SahaAktarimDurum.Iptal))
                .ToList();
        var acikTransferliSatirIds = satirIdleri.Count == 0
            ? new HashSet<int>()
            : (await unitOfWork.GetRepository<ProjeTransfer>().FindAsync(t =>
                    t.DurumId == (int)ProjeTransferDurum.Aktif &&
                    (satirIdleri.Contains(t.KaynakCekiSatiriId) ||
                     (t.HedefCekiSatiriId.HasValue && satirIdleri.Contains(t.HedefCekiSatiriId.Value)))))
                .SelectMany(t => new[] { t.KaynakCekiSatiriId, t.HedefCekiSatiriId ?? 0 })
                .ToHashSet();

        var kaynakSandikIds = satirIcerikleri.Select(i => i.SandikId)
            .Concat(seciliKaynakSandik == null ? [] : [seciliKaynakSandik.Id])
            .Distinct()
            .ToList();
        var kaynakSandikMap = kaynakSandikIds.Count == 0
            ? new Dictionary<int, Sandik>()
            : (await sandikRepo.FindAsync(s => kaynakSandikIds.Contains(s.Id))).ToDictionary(s => s.Id);
        if (seciliKaynakSandik != null)
            kaynakSandikMap[seciliKaynakSandik.Id] = seciliKaynakSandik;

        var tasimaSatirlari = new List<SahaTasimaSatiri>();
        foreach (var satir in satirlar.OrderBy(s => s.SiraNo))
        {
            var etiket = $"#{satir.SiraNo} {satir.Aciklama}".Trim();
            var icerikler = satirIcerikleri.Where(i => i.CekiSatiriId == satir.Id).ToList();
            var kalemler = aktifKalemler.Where(k => k.SahaCekiSatiriId == satir.Id).ToList();

            if (icerikler.Count == 0)
            {
                engeller.Add($"{etiket}: sandık içeriği yok.");
                continue;
            }
            if (icerikler.Select(i => i.SandikId).Distinct().Count() > 1)
            {
                engeller.Add($"{etiket}: satır birden çok sandığa bölünmüş; önce tek sandıkta birleştirin.");
                continue;
            }
            var sandik = kaynakSandikMap.GetValueOrDefault(icerikler[0].SandikId);
            if (sandik == null || sandik.ProjeId != kaynakProje.Id)
            {
                engeller.Add($"{etiket}: sandığı kaynak saha projesinde değil.");
                continue;
            }
            if (kalemler.Count != 1 || kalemler[0].SahaProjeId != kaynakProje.Id)
            {
                engeller.Add($"{etiket}: aktarım defteri kaydı bulunamadı ya da tutarsız; satır taşınamaz.");
                continue;
            }
            var kalem = kalemler[0];
            if (kalem.SevkiyatKapsamindaMi ||
                kalem.DurumId is (int)SahaAktarimDurum.SevkEdildi or (int)SahaAktarimDurum.SevkiyatDuzeltmede)
            {
                engeller.Add($"{etiket}: sevkiyat kapsamında; önce sevkiyatı geri alın.");
                continue;
            }
            if (acikTransferliSatirIds.Contains(satir.Id))
            {
                engeller.Add($"{etiket}: açık projeden karşılama kaydı var; önce karşılamayı geri alın.");
                continue;
            }

            tasimaSatirlari.Add(new SahaTasimaSatiri(satir, icerikler[0], kalem, sandik));
        }

        var kaynakSandiklar = tasimaSatirlari.Select(s => s.KaynakSandik)
            .Concat(seciliKaynakSandik == null ? [] : [seciliKaynakSandik])
            .DistinctBy(s => s.Id)
            .ToList();
        foreach (var sandik in kaynakSandiklar.Where(s => s.DurumId == (int)SandikDurum.Sevkedildi || s.SevkiyatDuzeltmeAcikMi))
            engeller.Add($"Kaynak sandık {sandik.SandikNo} sevk edilmiş ya da sevkiyat düzeltmesinde; içinden ürün taşınamaz.");

        var hedefSandiklar = (await sandikRepo.FindAsync(s => s.ProjeId == hedefProje.Id)).ToList();
        Sandik? hedefSandik = null;
        string hedefSandikNo;
        if (talep.HedefSandikId.HasValue)
        {
            hedefSandik = hedefSandiklar.SingleOrDefault(s => s.Id == talep.HedefSandikId.Value);
            if (hedefSandik == null)
                return Result<SahadanSahayaTasimaHazirligi>.Failure("Hedef sandık hedef saha projesinde bulunamadı.", 404);
            if (hedefSandik.DurumId == (int)SandikDurum.Sevkedildi || hedefSandik.SevkiyatDuzeltmeAcikMi)
                engeller.Add($"Hedef sandık {hedefSandik.SandikNo} sevk edilmiş ya da sevkiyat düzeltmesinde.");
            hedefSandikNo = hedefSandik.SandikNo;
        }
        else
        {
            // Yeni sandık (28.10.2026): numara verilmezse hedefin sıradaki numarası (en büyük sayısal + 1; sevk edilmiş
            // sandıklar dahil) atanır, kaynak sandığın numarası kullanılmaz. Verilen numara hedefte (çeki biçiminde; "05" =
            // "5") varsa engeldir: kullanıcı o sandığı seçer ya da başka numara verir. Eskiden "2-2" ekli ikinci sandık
            // açılıyor, ekranda iki "2" görünüyordu.
            var kullanilanNolar = hedefSandiklar.Select(s => KoliNoKurali.Normalize(s.SandikNo)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(talep.YeniSandikNo))
            {
                hedefSandikNo = KoliNoKurali.SiradakiNumara(kullanilanNolar);
            }
            else
            {
                hedefSandikNo = talep.YeniSandikNo.Trim();
                if (kullanilanNolar.Contains(KoliNoKurali.Normalize(hedefSandikNo)))
                    engeller.Add($"Hedef saha projesinde {hedefSandikNo} numaralı sandık zaten var; o sandığı seçin ya da başka bir numara verin.");
            }
        }

        var tasinanIcerikIds = tasimaSatirlari.Select(s => s.Icerik.Id).Concat(manuelIcerikler.Select(i => i.Id)).ToHashSet();
        var kaynakSandikIdleri = kaynakSandiklar.Select(s => s.Id).ToList();
        // Kaynakta kalan aktif içerik (proje içi taşımadaki kural): tahsisi, konulanı ya da miktarlı manuel içeriği olan
        // satır. Sıfır tahsisli artık satır sandığı dolu saymaz.
        var kalanIcerikliSandikIds = kaynakSandikIdleri.Count == 0
            ? new HashSet<int>()
            : (await icerikRepo.FindAsync(i => kaynakSandikIdleri.Contains(i.SandikId)))
                .Where(i => !tasinanIcerikIds.Contains(i.Id) &&
                            (i.TahsisMiktari > 0 || i.KonulanAdet > 0 || (i.CekiSatiriId == null && i.Miktar > 0)))
                .Select(i => i.SandikId)
                .ToHashSet();

        var kokSatirIds = tasimaSatirlari.Select(s => s.Satir.KaynakCekiSatiriId!.Value).Distinct().ToList();
        var kokSatirlar = kokSatirIds.Count == 0
            ? new Dictionary<int, CekiSatiri>()
            : (await cekiSatiriRepo.FindAsync(cs => kokSatirIds.Contains(cs.Id))).ToDictionary(cs => cs.Id);
        var kokCekiIds = kokSatirlar.Values.Select(cs => cs.CekiId).Distinct().ToList();
        var kokCekiProjeleri = kokCekiIds.Count == 0
            ? new Dictionary<int, int>()
            : (await cekiRepo.FindAsync(c => kokCekiIds.Contains(c.Id))).ToDictionary(c => c.Id, c => c.ProjeId);
        var kokProjeIds = kokCekiProjeleri.Values.Distinct().ToList();
        var kokProjeNoMap = kokProjeIds.Count == 0
            ? new Dictionary<int, string>()
            : (await projeRepo.FindAsync(p => kokProjeIds.Contains(p.Id))).ToDictionary(p => p.Id, p => p.ProjeNo);
        var kokProjeNolari = kokSatirlar.Values.ToDictionary(
            cs => cs.Id,
            cs => kokCekiProjeleri.TryGetValue(cs.CekiId, out var projeId) ? kokProjeNoMap.GetValueOrDefault(projeId, "?") : "?");

        return Result<SahadanSahayaTasimaHazirligi>.Success(new SahadanSahayaTasimaHazirligi
        {
            KaynakProje = kaynakProje,
            HedefProje = hedefProje,
            Satirlar = tasimaSatirlari,
            ManuelIcerikler = manuelIcerikler,
            KaynakSandiklar = kaynakSandiklar,
            KokSatirlar = kokSatirlar,
            KokProjeNolari = kokProjeNolari,
            HedefSandik = hedefSandik,
            HedefSandikNo = hedefSandikNo,
            Engeller = engeller,
            BosalacakSandikNolari = kaynakSandiklar
                .Where(s => !kalanIcerikliSandikIds.Contains(s.Id))
                .Select(s => s.SandikNo)
                .ToList(),
            BosalacakSandikIds = kaynakSandiklar
                .Where(s => !kalanIcerikliSandikIds.Contains(s.Id))
                .Select(s => s.Id)
                .ToHashSet()
        });
    }

    /// <summary>Engelsiz hazırlığı uygular; çağıran transaction'ı (<c>ExecuteAtomicAsync</c>) sahibidir.</summary>
    public async Task<SahadanSahayaTasimaSonucu> UygulaAsync(
        SahadanSahayaTasimaHazirligi h,
        Guid islemAnahtari,
        string? aciklama,
        CancellationToken cancellationToken)
    {
        if (h.Engeller.Count > 0)
            throw new InvalidOperationException("Engelli taşıma uygulanamaz.");

        var cekiRepo = unitOfWork.GetRepository<Ceki>();
        var cekiSatiriRepo = unitOfWork.GetRepository<CekiSatiri>();
        var icerikRepo = unitOfWork.GetRepository<SandikIcerik>();
        var sandikRepo = unitOfWork.GetRepository<Sandik>();
        var kalemRepo = unitOfWork.GetRepository<SahaAktarimKalemi>();
        var tasimaRepo = unitOfWork.GetRepository<SahaTasimaKalemi>();
        var kullaniciId = currentUserService.UserId ?? 0;

        var hedefCeki = (await cekiRepo.FindAsync(c =>
                c.ProjeId == h.HedefProje.Id &&
                c.CekiTipiId == (int)CekiTipi.EksikTamamlama))
            .OrderBy(c => c.Id)
            .FirstOrDefault();
        if (hedefCeki == null)
        {
            hedefCeki = new Ceki
            {
                ProjeId = h.HedefProje.Id,
                OrijinalDosyaYolu = string.Empty,
                YuklemeTarihi = TurkeyTime.Now,
                CekiTipiId = (int)CekiTipi.EksikTamamlama,
                TamamlamaNo = 1,
                Aciklama = $"Sahadan taşıma: {h.KaynakProje.ProjeNo}"
            };
            await cekiRepo.AddAsync(hedefCeki);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var hedefSandik = h.HedefSandik;
        var hedefEskiDurumId = hedefSandik?.DurumId;
        if (hedefSandik == null)
        {
            // Kutunun kendisi taşınıyorsa (kaynak boşalıyor) ad, tip, ölçü ve lokasyon kopyalanır; aksi halde yeni kutu
            // ölçüsüzdür, ölçüyü kullanıcı girer.
            var ornek = h.ButunSandikTasiniyor ? h.KaynakSandiklar[0] : null;
            hedefSandik = new Sandik
            {
                ProjeId = h.HedefProje.Id,
                SandikNo = h.HedefSandikNo,
                Ad = ornek?.Ad,
                AdIngilizce = ornek?.AdIngilizce,
                TipId = ornek?.TipId ?? (int)SandikTipi.AhsapKapali,
                En = ornek?.En,
                Boy = ornek?.Boy,
                Yukseklik = ornek?.Yukseklik,
                NetKg = ornek?.NetKg,
                GrossKg = ornek?.GrossKg,
                DurumId = (int)SandikDurum.Hazirlaniyor,
                DepoLokasyonId = ornek?.DepoLokasyonId ?? (int)DepoLokasyon.Belirsiz
            };
            await sandikRepo.AddAsync(hedefSandik);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        else
        {
            // Proje içi taşımayla aynı kural: içine ürün alan boş ya da kapanmış sandık yeniden hazırlanır. Sandık her
            // zaman UPDATE edilir; xmin, okumadan sonra başlayan bir sevkiyatın taşımayla sessizce yarışmasını engeller.
            if (hedefSandik.DurumId is (int)SandikDurum.Bos or (int)SandikDurum.Kapandi)
                hedefSandik.DurumId = (int)SandikDurum.Hazirlaniyor;
            sandikRepo.Update(hedefSandik);
        }

        if (h.HedefProje.DurumId == (int)ProjeDurum.Tamamlandi)
        {
            h.HedefProje.DurumId = (int)ProjeDurum.Hazirlaniyor;
            unitOfWork.GetRepository<Proje>().Update(h.HedefProje);
        }

        var kokProjeNolari = h.Satirlar
            .Select(s => h.KokProjeNolari.GetValueOrDefault(s.Satir.KaynakCekiSatiriId!.Value, "?"))
            .Distinct()
            .ToList();
        var kokProjeIds = h.Satirlar.Select(s => s.Kalem.KaynakProjeId).Distinct().ToList();
        var ozet = $"Sahadan sahaya taşıma: {h.KaynakProje.ProjeNo} → {h.HedefProje.ProjeNo} ({hedefSandik.SandikNo})";
        var baslik = new SahaAktarim
        {
            SahaProjeId = h.HedefProje.Id,
            KaynakProjeId = kokProjeIds.Count == 1 ? kokProjeIds[0] : null,
            KaynakSahaProjeId = h.KaynakProje.Id,
            KullaniciId = kullaniciId,
            IslemAnahtari = islemAnahtari,
            Aciklama = string.IsNullOrWhiteSpace(aciklama) ? ozet : $"{ozet}. {aciklama.Trim()}"
        };
        await unitOfWork.GetRepository<SahaAktarim>().AddAsync(baslik);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var siraNo = (await cekiSatiriRepo.FindAsync(cs => cs.CekiId == hedefCeki.Id))
            .Select(cs => cs.SiraNo)
            .DefaultIfEmpty(0)
            .Max() + 1;
        var tarih = TurkeyTime.Now;
        foreach (var tasima in h.Satirlar)
        {
            var oncekiBaslikId = tasima.Kalem.SahaAktarimId;
            var satir = tasima.Satir;
            satir.CekiId = hedefCeki.Id;
            satir.SiraNo = siraNo++;
            satir.CekideGecenSandikNo = hedefSandik.SandikNo;
            satir.FiiliSandikNo = hedefSandik.SandikNo;
            cekiSatiriRepo.Update(satir);

            tasima.Icerik.SandikId = hedefSandik.Id;
            icerikRepo.Update(tasima.Icerik);

            tasima.Kalem.SahaProjeId = h.HedefProje.Id;
            tasima.Kalem.SahaSandikId = hedefSandik.Id;
            tasima.Kalem.SahaAktarimId = baslik.Id;
            kalemRepo.Update(tasima.Kalem);

            await tasimaRepo.AddAsync(new SahaTasimaKalemi
            {
                SahaAktarimId = baslik.Id,
                SahaAktarimKalemiId = tasima.Kalem.Id,
                OncekiSahaAktarimId = oncekiBaslikId,
                KaynakCekiSatiriId = satir.KaynakCekiSatiriId!.Value,
                SahaCekiSatiriId = satir.Id,
                KaynakSahaProjeId = h.KaynakProje.Id,
                KaynakSahaProjeNo = h.KaynakProje.ProjeNo,
                KaynakSahaSandikId = tasima.KaynakSandik.Id,
                KaynakSahaSandikNo = tasima.KaynakSandik.SandikNo,
                HedefSahaProjeId = h.HedefProje.Id,
                HedefSahaProjeNo = h.HedefProje.ProjeNo,
                HedefSahaSandikId = hedefSandik.Id,
                HedefSahaSandikNo = hedefSandik.SandikNo,
                Miktar = tasima.Kalem.Miktar,
                Tarih = tarih
            });
        }

        foreach (var icerik in h.ManuelIcerikler)
        {
            icerik.SandikId = hedefSandik.Id;
            icerikRepo.Update(icerik);
        }

        // Boşalan kaynak sandık silinmez, Boş olur (numara ve geçmiş korunur). İçinde ürün kalan kapanmış sandığın içeriği
        // değişti; yeniden kontrol edilmeden sevk edilemez, hazırlanıyor durumuna döner. Kaynaklar da her zaman UPDATE edilir.
        var yenidenAcilanKaynaklar = new List<Sandik>();
        foreach (var sandik in h.KaynakSandiklar)
        {
            if (h.BosalacakSandikIds.Contains(sandik.Id))
            {
                sandik.DurumId = (int)SandikDurum.Bos;
            }
            else if (sandik.DurumId == (int)SandikDurum.Kapandi)
            {
                sandik.DurumId = (int)SandikDurum.Hazirlaniyor;
                yenidenAcilanKaynaklar.Add(sandik);
            }
            sandikRepo.Update(sandik);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        var kokSatirIds = h.Satirlar.Select(s => s.Satir.KaynakCekiSatiriId!.Value).Distinct().ToList();
        if (kokSatirIds.Count > 0)
            await sahaTamamlamaService.SenkronizeKaynakProjelerAsync(kokSatirIds, cancellationToken);

        var toplamMiktar = h.Satirlar.Sum(s => s.Kalem.Miktar);
        var ayrinti = $"{h.Satirlar.Count} satır ({FormatAdet(toplamMiktar)} adet)" +
                      (h.ManuelIcerikler.Count > 0 ? $" ve {h.ManuelIcerikler.Count} manuel içerik" : string.Empty);
        await hareketService.HareketKaydetAsync(new HareketGecmisi
        {
            ProjeId = h.KaynakProje.Id,
            ReferansTipi = "SahaAktarim",
            ReferansId = baslik.Id.ToString(CultureInfo.InvariantCulture),
            Islem = "Sahadan Sahaya Taşıma (giden)",
            KullaniciId = kullaniciId,
            YeniDeger = $"Hedef:{h.HedefProje.ProjeNo}, Sandık:{hedefSandik.SandikNo}",
            Aciklama = $"{ayrinti} {h.HedefProje.ProjeNo} saha projesinin {hedefSandik.SandikNo} numaralı sandığına taşındı."
        });
        await hareketService.HareketKaydetAsync(new HareketGecmisi
        {
            ProjeId = h.HedefProje.Id,
            ReferansTipi = "SahaAktarim",
            ReferansId = baslik.Id.ToString(CultureInfo.InvariantCulture),
            Islem = "Sahadan Sahaya Taşıma (gelen)",
            KullaniciId = kullaniciId,
            EskiDeger = $"Kaynak:{h.KaynakProje.ProjeNo}",
            Aciklama = $"{h.KaynakProje.ProjeNo} saha projesinden {ayrinti} {hedefSandik.SandikNo} numaralı sandığa alındı."
        });
        if (hedefEskiDurumId == (int)SandikDurum.Kapandi)
            await SahaAktarimIslemi.SandikGeriAcildiKaydetAsync(hareketService, h.HedefProje.Id, hedefSandik, kullaniciId);
        foreach (var sandik in yenidenAcilanKaynaklar)
            await SahaAktarimIslemi.SandikGeriAcildiKaydetAsync(hareketService, h.KaynakProje.Id, sandik, kullaniciId);
        foreach (var kokProjeId in kokProjeIds)
        {
            var projeSatirlari = h.Satirlar.Where(s => s.Kalem.KaynakProjeId == kokProjeId).ToList();
            await hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = kokProjeId,
                ReferansTipi = "TopluCekiSatiri",
                ReferansId = string.Join(",", projeSatirlari.Select(s => s.Satir.KaynakCekiSatiriId!.Value).Distinct()),
                Islem = "Saha Taşıma",
                KullaniciId = kullaniciId,
                EskiDeger = $"Saha:{h.KaynakProje.ProjeNo}",
                YeniDeger = $"Saha:{h.HedefProje.ProjeNo}, Sandık:{hedefSandik.SandikNo}",
                Aciklama = $"{projeSatirlari.Count} ürün {h.KaynakProje.ProjeNo} sahasından {h.HedefProje.ProjeNo} sahasına taşındı; ürünler bu saha üzerinden gönderilecek."
            });
        }

        return new SahadanSahayaTasimaSonucu(
            baslik,
            hedefSandik,
            h.Satirlar.Count,
            h.ManuelIcerikler.Count,
            toplamMiktar,
            h.BosalacakSandikNolari);
    }

    private static string FormatAdet(decimal value)
    {
        return decimal.Truncate(value) == value
            ? decimal.Truncate(value).ToString("0", CultureInfo.InvariantCulture)
            : value.ToString("0.####", CultureInfo.GetCultureInfo("tr-TR"));
    }
}
