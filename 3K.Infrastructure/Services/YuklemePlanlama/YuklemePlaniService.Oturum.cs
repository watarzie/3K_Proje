using System.Data;
using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Exceptions;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.YuklemeMotoru.Yerlesim;

namespace _3K.Infrastructure.Services.YuklemePlanlama;

/// <summary>
/// Araç yükleme oturumu (saha): oturum başladığı plan revizyonunun yerleşimine sabittir; plan sonradan revize edilse de
/// aynı yerleşimle sürer. Yükleme işareti yalnız oturum tablolarına yazılır: sandık durumu, sevk kayıtları, hareket
/// geçmişi ve plan revizyonu değişmez. Oturuma yazan işlemler (işaretle, geri al, kapat) oturum satırının kilidi altında
/// sırayla yürür: iki tablet aynı yükü aynı anda okutursa ikincisi "zaten yüklendi" yanıtını alır, kapanmış oturuma
/// işaret düşmez. Kısmi benzersiz indeksler aynı kuralın veritabanı güvencesidir.
/// </summary>
internal sealed partial class YuklemePlaniService : IYuklemeOturumuService
{
    private const string OturumBulunamadi = "Yükleme oturumu bulunamadı.";
    private const string AracBulunamadi = "Araç bu yükleme oturumunda yok.";
    private const string IsaretBulunamadi = "Yükleme işareti bulunamadı ya da geri alınmış.";
    private const string YerlesimdeYukYok = "Yerleşimde araca konmuş yük yok; oturum başlatılamaz.";
    private const string TekAcikOturumIndeksi = "IX_YuklemeOturumlari_PlanId";

    public async Task<SayfaliSonuc<AracYuklemePlanOzeti>> YuklenecekPlanlarAsync(string? arama, int? sayfa, int? boyut, CancellationToken cancellationToken)
    {
        // Yalnız yüklenebilecek planlar: son revizyonunda yerleşim olan ya da yüklemesi süren.
        var sorgu = context.YuklemePlanlari.AsNoTracking().Where(p =>
            context.YuklemeOturumlari.Any(o => o.PlanId == p.Id && o.Durum == YuklemeOturumDurumu.Acik) ||
            p.Revizyonlar.Any(r => r.RevizyonNo == p.SonRevizyonNo && r.YerlesimJson != null));
        if (!string.IsNullOrWhiteSpace(arama))
        {
            var desen = $"%{IlikeDeseniniKacir(arama.Trim())}%";
            sorgu = sorgu.Where(p => EF.Functions.ILike(p.PlanNo, desen, "\\") || EF.Functions.ILike(p.Ad, desen, "\\"));
        }

        var toplam = await sorgu.CountAsync(cancellationToken);
        var sayfaBoyutu = SayfaKurali.Boyut(boyut);
        var gecerliSayfa = SayfaKurali.Sayfa(sayfa, sayfaBoyutu, toplam);
        var planlar = await sorgu
            .OrderByDescending(p => context.YuklemeOturumlari.Any(o => o.PlanId == p.Id && o.Durum == YuklemeOturumDurumu.Acik))
            .ThenByDescending(p => p.UpdatedDate ?? p.CreatedDate).ThenByDescending(p => p.Id)
            .Skip(SayfaKurali.Atlanan(gecerliSayfa, sayfaBoyutu)).Take(sayfaBoyutu)
            .Select(p => new
            {
                p.Id, p.PlanNo, p.Ad, p.YuklemeTarihi, p.SonRevizyonNo,
                YerlesimVar = p.Revizyonlar.Any(r => r.RevizyonNo == p.SonRevizyonNo && r.YerlesimJson != null)
            })
            .ToListAsync(cancellationToken);

        var idler = planlar.Select(p => p.Id).ToList();
        var oturumlar = await context.YuklemeOturumlari.AsNoTracking()
            .Where(o => idler.Contains(o.PlanId) && o.Durum == YuklemeOturumDurumu.Acik)
            .ToListAsync(cancellationToken);
        var modeller = await OturumModelleriAsync(oturumlar, planlar.ToDictionary(p => p.Id, p => p.SonRevizyonNo), cancellationToken);

        return new SayfaliSonuc<AracYuklemePlanOzeti>
        {
            Items = planlar.Select(p => new AracYuklemePlanOzeti
            {
                Id = p.Id,
                PlanNo = p.PlanNo,
                Ad = p.Ad,
                YuklemeTarihi = p.YuklemeTarihi,
                RevizyonNo = p.SonRevizyonNo,
                YerlesimVar = p.YerlesimVar,
                AcikOturum = oturumlar.FirstOrDefault(o => o.PlanId == p.Id) is { } acik ? modeller[acik.Id] : null
            }).ToList(),
            PageNumber = gecerliSayfa,
            PageSize = sayfaBoyutu,
            TotalCount = toplam
        };
    }

    public async Task<AracYuklemeDurumu?> DurumAsync(int planId, CancellationToken cancellationToken)
    {
        var plan = await context.YuklemePlanlari.AsNoTracking().SingleOrDefaultAsync(p => p.Id == planId, cancellationToken);
        if (plan == null)
            return null;

        // Süren oturum; yoksa en son kapanan (sonucu görülsün).
        var oturum = await context.YuklemeOturumlari.AsNoTracking()
            .Where(o => o.PlanId == planId)
            .OrderBy(o => o.Durum == YuklemeOturumDurumu.Acik ? 0 : 1).ThenByDescending(o => o.Id)
            .FirstOrDefaultAsync(cancellationToken);
        var sonRevizyon = await RevizyonAsync(plan.Id, plan.SonRevizyonNo, cancellationToken);
        var sonYerlesim = await YerlesimAsync(plan, sonRevizyon, YerlesimSerilestirici.Oku(sonRevizyon.YerlesimJson), cancellationToken);
        // Liste ile aynı görünürlük: ne yüklemesi ne yerleşimi olan plan (taslak) araç yükleme izniyle görülmez.
        if (oturum == null && sonYerlesim == null)
            return null;

        // Araçlar oturumun sabitlendiği revizyondan; oturum yoksa başlatılınca yüklenecek son revizyondan.
        var yerlesim = oturum == null || oturum.RevizyonNo == plan.SonRevizyonNo
            ? sonYerlesim
            : await OturumYerlesimiAsync(plan, oturum.RevizyonNo, cancellationToken);
        var yuklenenler = oturum == null
            ? []
            : await context.YuklemeOturumYukleri.AsNoTracking()
                .Where(y => y.OturumId == oturum.Id && y.GeriAlinmaTarihi == null)
                .GroupBy(y => y.AracNo).Select(g => new { AracNo = g.Key, Adet = g.Count() })
                .ToDictionaryAsync(x => x.AracNo, x => x.Adet, cancellationToken);
        var oturumModeli = oturum == null
            ? null
            : (await OturumModelleriAsync([oturum], new Dictionary<int, int> { [plan.Id] = plan.SonRevizyonNo }, cancellationToken))[oturum.Id];

        return new AracYuklemeDurumu
        {
            PlanId = plan.Id,
            PlanNo = plan.PlanNo,
            PlanAdi = plan.Ad,
            YuklemeTarihi = plan.YuklemeTarihi,
            SonRevizyonNo = plan.SonRevizyonNo,
            YerlesimVar = sonYerlesim != null,
            YerlesimSonucu = sonYerlesim?.Sonuc,
            Baslatilabilir = sonYerlesim != null && oturum?.Durum != YuklemeOturumDurumu.Acik,
            Oturum = oturumModeli,
            Araclar = (yerlesim?.Araclar ?? []).Select(a => new AracYuklemeAracOzeti
            {
                AracNo = a.AracNo,
                EkipmanKodu = a.EkipmanKodu,
                EkipmanAdi = a.EkipmanAdi,
                TipAdi = a.TipAdi,
                ProjeNolari = a.ProjeNolari,
                ToplamKg = a.ToplamKg,
                ToplamYuk = a.Konumlar.Count,
                YuklenenYuk = yuklenenler.GetValueOrDefault(a.AracNo),
                Sonuc = a.Sonuc
            }).ToList()
        };
    }

    public async Task<AracYuklemeAraci?> AracAsync(int oturumId, int aracNo, CancellationToken cancellationToken)
    {
        var oturum = await context.YuklemeOturumlari.AsNoTracking().Include(o => o.Plan)
            .SingleOrDefaultAsync(o => o.Id == oturumId, cancellationToken);
        if (oturum == null)
            return null;
        var yerlesim = await OturumYerlesimiAsync(oturum.Plan, oturum.RevizyonNo, cancellationToken);
        var arac = yerlesim.Araclar.SingleOrDefault(a => a.AracNo == aracNo);
        return arac == null ? null : await AracModeliAsync(oturum, arac, cancellationToken);
    }

    public async Task<YukCozumu> CozumleAsync(int oturumId, string barkod, int aracNo, CancellationToken cancellationToken)
    {
        var oturum = await AcikOturumAsync(oturumId, izle: false, cancellationToken);
        if (YuklemeOturumuKurali.BarkoddanYukAnahtari(barkod) is not { } anahtar)
            return new YukCozumu { Sonuc = YukCozumSonucu.GecersizBarkod, Mesaj = YuklemeOturumuKurali.GecersizBarkodMesaji };

        var yerlesim = await OturumYerlesimiAsync(oturum.Plan, oturum.RevizyonNo, cancellationToken);
        var arac = yerlesim.Araclar.FirstOrDefault(a => a.Konumlar.Any(k => k.YukAnahtari == anahtar));
        if (arac == null)
            return new YukCozumu { Sonuc = YukCozumSonucu.PlandaYok, Mesaj = YuklemeOturumuKurali.PlandaYokMesaji, YukAnahtari = anahtar };

        var adimlar = YuklemeSirasiKurali.Adimlar(arac.Konumlar);
        var adim = adimlar.First(a => a.Yukler.Any(y => y.Konum.YukAnahtari == anahtar));
        var yuk = adim.Yukler.First(y => y.Konum.YukAnahtari == anahtar);
        var isaretler = await IsaretlerAsync(oturum.Id, arac.AracNo, cancellationToken);
        var isaret = isaretler.SingleOrDefault(i => i.YukAnahtari == anahtar);

        YukCozumu Sonuc(YukCozumSonucu sonuc, string mesaj) => new()
        {
            Sonuc = sonuc,
            Mesaj = mesaj,
            YukAnahtari = anahtar,
            Kod = yuk.Konum.Kod,
            AracNo = arac.AracNo,
            EkipmanKodu = arac.EkipmanKodu,
            AdimNo = adim.AdimNo,
            No = yuk.No,
            Isaret = isaret
        };

        if (isaret != null)
            return Sonuc(YukCozumSonucu.ZatenYuklendi,
                $"{yuk.Konum.Kod} zaten yüklendi: Araç {arac.AracNo}, {isaret.Yukleyen ?? "bilinmeyen kullanıcı"}, {isaret.YuklemeTarihi.ToString("dd.MM.yyyy HH:mm", Tr)}.");
        if (arac.AracNo != aracNo)
            return Sonuc(YukCozumSonucu.BaskaAracta, $"{yuk.Konum.Kod} bu aracın yükü değil: Araç {arac.AracNo} ({arac.EkipmanKodu}) yüküdür.");

        var yuklenenler = isaretler.Select(i => i.YukAnahtari).ToHashSet(StringComparer.Ordinal);
        return YuklemeOturumuKurali.SiraDisiMi(adimlar, yuklenenler, anahtar)
            ? Sonuc(YukCozumSonucu.SiraDisi, SiraDisiMesaji(adimlar, yuklenenler, yuk.Konum.Kod))
            : Sonuc(YukCozumSonucu.Yuklenebilir, $"{yuk.Konum.Kod}: adım {adim.AdimNo}, No {yuk.No}.");
    }

    public async Task<AracYuklemeDurumu> BaslatAsync(int planId, int beklenenRevizyonNo, int? kullaniciId, CancellationToken cancellationToken)
    {
        var plan = await context.YuklemePlanlari.AsNoTracking().SingleOrDefaultAsync(p => p.Id == planId, cancellationToken)
                   ?? throw new KayitBulunamadiException(PlanBulunamadi);
        if (plan.SonRevizyonNo != beklenenRevizyonNo)
            throw IsKuraliException.Cakisma(EszamanliKayit);
        if (await context.YuklemeOturumlari.AnyAsync(o => o.PlanId == planId && o.Durum == YuklemeOturumDurumu.Acik, cancellationToken))
            throw IsKuraliException.Cakisma(YuklemeOturumuKurali.AcikOturumVarMesaji);

        var revizyon = await context.YuklemePlanRevizyonlari.AsNoTracking()
            .Where(r => r.PlanId == planId && r.RevizyonNo == plan.SonRevizyonNo)
            .Select(r => new { r.Id, r.YerlesimJson })
            .SingleAsync(cancellationToken);
        var sonuc = YerlesimSerilestirici.Oku(revizyon.YerlesimJson)
                    ?? throw new IsKuraliException(YuklemeOturumuKurali.YerlesimYokMesaji);
        var toplamYuk = sonuc.Araclar.Sum(a => a.Konumlar.Count);
        if (toplamYuk == 0)
            throw new IsKuraliException(YerlesimdeYukYok);

        var oturum = new YuklemeOturumu
        {
            PlanId = planId,
            RevizyonId = revizyon.Id,
            RevizyonNo = plan.SonRevizyonNo,
            Durum = YuklemeOturumDurumu.Acik,
            AracSayisi = sonuc.Araclar.Count,
            ToplamYuk = toplamYuk,
            BaslatanKullaniciId = kullaniciId,
            BaslamaTarihi = TurkeyTime.Now
        };
        context.YuklemeOturumlari.Add(oturum);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException ihlal) when (ihlal.ConstraintName == TekAcikOturumIndeksi)
        {
            // İki kişi aynı anda başlattı: planda tek açık oturum olur.
            context.Entry(oturum).State = EntityState.Detached;
            throw IsKuraliException.Cakisma(YuklemeOturumuKurali.AcikOturumVarMesaji);
        }
        return (await DurumAsync(planId, cancellationToken))!;
    }

    public async Task<YukIsaretSonucu> IsaretleAsync(YukIsaretleModeli model, int? kullaniciId, CancellationToken cancellationToken)
    {
        if (model.Kaynak == YuklemeIsaretKaynagi.Barkod && !YuklemeOturumuKurali.BarkodYukleEslesir(model.Barkod, model.YukAnahtari))
            throw new IsKuraliException(YuklemeOturumuKurali.BarkodUyusmuyorMesaji);

        // Yerleşim oturumun sabit revizyonundan okunur (revizyon değişmez); bu ağır okuma kilitten önce yapılır.
        var onOkuma = await AcikOturumAsync(model.OturumId, izle: false, cancellationToken);
        var revizyon = await RevizyonAsync(onOkuma.PlanId, onOkuma.RevizyonNo, cancellationToken);
        var yerlesim = await YerlesimAsync(onOkuma.Plan, revizyon, YerlesimSerilestirici.Oku(revizyon.YerlesimJson), cancellationToken)
                       ?? throw new IsKuraliException(YuklemeOturumuKurali.YerlesimYokMesaji);
        var arac = yerlesim.Araclar.SingleOrDefault(a => a.AracNo == model.AracNo)
                   ?? throw new KayitBulunamadiException(AracBulunamadi);
        var konum = arac.Konumlar.SingleOrDefault(k => k.YukAnahtari == model.YukAnahtari);
        if (konum == null)
        {
            var baska = yerlesim.Araclar.FirstOrDefault(a => a.Konumlar.Any(k => k.YukAnahtari == model.YukAnahtari));
            throw new IsKuraliException(baska == null
                ? YuklemeOturumuKurali.PlandaYokMesaji
                : $"Bu yük bu aracın yükü değil: Araç {baska.AracNo} ({baska.EkipmanKodu}) yüküdür.");
        }
        var adimlar = YuklemeSirasiKurali.Adimlar(arac.Konumlar);
        var satir = revizyon.Yukler.FirstOrDefault(y =>
            YuklemePlaniKurali.YukAnahtari(y.Kaynak, y.SandikId, y.ManuelGrupNo, y.BirimNo) == model.YukAnahtari);

        var (oturum, zatenYuklendi) = await OturumKilidiyleAsync(model.OturumId, izle: false, async kilitli =>
        {
            // "Zaten yüklendi" ve sıra dışılık kilit altındaki görüntüden hesaplanır: başka tabletin okutması araya girmez.
            var yuklenenler = (await context.YuklemeOturumYukleri.AsNoTracking()
                    .Where(y => y.OturumId == kilitli.Id && y.AracNo == arac.AracNo && y.GeriAlinmaTarihi == null)
                    .Select(y => y.YukAnahtari)
                    .ToListAsync(cancellationToken))
                .ToHashSet(StringComparer.Ordinal);
            if (yuklenenler.Contains(model.YukAnahtari))
                return (kilitli, true);

            var siraDisi = YuklemeOturumuKurali.SiraDisiMi(adimlar, yuklenenler, model.YukAnahtari);
            if (siraDisi && !model.SiraDisiOnayi)
                throw IsKuraliException.Cakisma(SiraDisiMesaji(adimlar, yuklenenler, konum.Kod));

            context.YuklemeOturumYukleri.Add(new YuklemeOturumYuku
            {
                OturumId = kilitli.Id,
                AracNo = arac.AracNo,
                YukAnahtari = model.YukAnahtari,
                // Sandık bağı revizyondaki satırdan: sandık silindiyse boştur (anahtardaki kimlik artık yoktur).
                SandikId = satir?.SandikId,
                Kod = konum.Kod,
                AdimNo = adimlar.First(a => a.Yukler.Any(y => y.Konum.YukAnahtari == model.YukAnahtari)).AdimNo,
                Kaynak = model.Kaynak,
                SiraDisi = siraDisi,
                YukleyenKullaniciId = kullaniciId,
                YuklemeTarihi = TurkeyTime.Now
            });
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return (kilitli, false);
        }, cancellationToken);
        return new YukIsaretSonucu { ZatenYuklendi = zatenYuklendi, Arac = await AracModeliAsync(oturum, arac, cancellationToken) };
    }

    public async Task<AracYuklemeAraci> GeriAlAsync(int oturumId, int isaretId, string gerekce, int? kullaniciId, CancellationToken cancellationToken)
    {
        if (!YuklemeOturumuKurali.GerekceGecerli(gerekce))
            throw new IsKuraliException(YuklemeOturumuKurali.GerekceMesaji);
        var (oturum, aracNo) = await OturumKilidiyleAsync(oturumId, izle: false, async kilitli =>
        {
            // Kilit altında okunur: aynı işareti iki tablet geri alırsa ikincisi "bulunamadı" alır, ilk gerekçe ezilmez.
            var isaret = await context.YuklemeOturumYukleri
                             .SingleOrDefaultAsync(y => y.Id == isaretId && y.OturumId == oturumId && y.GeriAlinmaTarihi == null, cancellationToken)
                         ?? throw new KayitBulunamadiException(IsaretBulunamadi);
            isaret.GeriAlanKullaniciId = kullaniciId;
            isaret.GeriAlinmaTarihi = TurkeyTime.Now;
            isaret.GeriAlmaGerekcesi = gerekce.Trim();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return (kilitli, isaret.AracNo);
        }, cancellationToken);

        var yerlesim = await OturumYerlesimiAsync(oturum.Plan, oturum.RevizyonNo, cancellationToken);
        return await AracModeliAsync(oturum, yerlesim.Araclar.Single(a => a.AracNo == aracNo), cancellationToken);
    }

    public async Task<AracYuklemeDurumu> KapatAsync(int oturumId, bool iptal, string? gerekce, int? kullaniciId, CancellationToken cancellationToken)
    {
        var planId = await OturumKilidiyleAsync(oturumId, izle: true, async oturum =>
        {
            // Sayım kilit altında yapılır: sayımdan sonra geri alınan işaret eksik yük gerekçesini atlatamaz.
            var yuklenen = await context.YuklemeOturumYukleri.CountAsync(y => y.OturumId == oturumId && y.GeriAlinmaTarihi == null, cancellationToken);
            var gerekceVar = !string.IsNullOrWhiteSpace(gerekce);
            // İptal ve eksik yükle tamamlama gerekçe ister; tam yüklemede gerekçe isteğe bağlıdır.
            if (iptal && !YuklemeOturumuKurali.GerekceGecerli(gerekce))
                throw new IsKuraliException(YuklemeOturumuKurali.GerekceMesaji);
            if (!iptal && yuklenen < oturum.ToplamYuk && !YuklemeOturumuKurali.GerekceGecerli(gerekce))
                throw new IsKuraliException(YuklemeOturumuKurali.EksikYukGerekceMesaji);
            if (gerekceVar && !YuklemeOturumuKurali.GerekceGecerli(gerekce))
                throw new IsKuraliException(YuklemeOturumuKurali.GerekceMesaji);

            oturum.Durum = iptal ? YuklemeOturumDurumu.IptalEdildi : YuklemeOturumDurumu.Tamamlandi;
            oturum.KapatanKullaniciId = kullaniciId;
            oturum.KapanisTarihi = TurkeyTime.Now;
            oturum.KapanisGerekcesi = gerekceVar ? gerekce!.Trim() : null;
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return oturum.PlanId;
        }, cancellationToken);
        return (await DurumAsync(planId, cancellationToken))!;
    }

    /// <summary>
    /// Oturuma yazan işlemi (işaretle, geri al, kapat) oturum satırının kilidi altında yürütür: bu işlemler aynı oturumda
    /// sırayla çalışır. Böylece kapanmış oturuma işaret düşmez; "zaten yüklendi", sıra dışılık ve eksik yük sayımı başka
    /// tabletin araya giren işiyle bayatlamaz. Oturum, kilit alındıktan sonra okunur ve açık olmalıdır; kilit
    /// transaction bitince kalkar. Ağır okumalar (yerleşim) kilidin dışında yapılır.
    /// </summary>
    private async Task<T> OturumKilidiyleAsync<T>(int oturumId, bool izle, Func<YuklemeOturumu, Task<T>> islem, CancellationToken cancellationToken)
    {
        async Task<T> KilitleVeYurut()
        {
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT 1 FROM \"YuklemeOturumlari\" WHERE \"Id\" = {oturumId} FOR UPDATE", cancellationToken);
            return await islem(await AcikOturumAsync(oturumId, izle, cancellationToken));
        }

        // Dıştaki transaction varsa kilit onun sonuna kadar tutulur.
        if (context.Database.CurrentTransaction != null)
            return await KilitleVeYurut();

        var strateji = context.Database.CreateExecutionStrategy();
        return await strateji.ExecuteAsync(async () =>
        {
            // READ COMMITTED: kilidi bekleyen işlem, kilit kalkınca öncekinin yazdığını görür.
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
            var sonuc = await KilitleVeYurut();
            await transaction.CommitAsync(cancellationToken);
            return sonuc;
        });
    }

    /// <summary>Planın süren yükleme oturumu (plan ekranındaki uyarı için); yoksa null.</summary>
    private async Task<AracYuklemeOturumu?> SurenYuklemeAsync(YuklemePlani plan, CancellationToken cancellationToken)
    {
        var oturum = await context.YuklemeOturumlari.AsNoTracking()
            .SingleOrDefaultAsync(o => o.PlanId == plan.Id && o.Durum == YuklemeOturumDurumu.Acik, cancellationToken);
        return oturum == null
            ? null
            : (await OturumModelleriAsync([oturum], new Dictionary<int, int> { [plan.Id] = plan.SonRevizyonNo }, cancellationToken))[oturum.Id];
    }

    /// <summary>Açık oturum (planıyla); yoksa 404, kapalıysa 409.</summary>
    private async Task<YuklemeOturumu> AcikOturumAsync(int oturumId, bool izle, CancellationToken cancellationToken)
    {
        var sorgu = context.YuklemeOturumlari.Include(o => o.Plan).AsQueryable();
        if (!izle)
            sorgu = sorgu.AsNoTracking();
        var oturum = await sorgu.SingleOrDefaultAsync(o => o.Id == oturumId, cancellationToken)
                     ?? throw new KayitBulunamadiException(OturumBulunamadi);
        return oturum.Durum == YuklemeOturumDurumu.Acik
            ? oturum
            : throw IsKuraliException.Cakisma(YuklemeOturumuKurali.OturumKapaliMesaji);
    }

    /// <summary>Oturumun sabitlendiği revizyonun yerleşimi (ekran modeli).</summary>
    private async Task<PlanYerlesimi> OturumYerlesimiAsync(YuklemePlani plan, int revizyonNo, CancellationToken cancellationToken)
    {
        var revizyon = await RevizyonAsync(plan.Id, revizyonNo, cancellationToken);
        return await YerlesimAsync(plan, revizyon, YerlesimSerilestirici.Oku(revizyon.YerlesimJson), cancellationToken)
               ?? throw new IsKuraliException(YuklemeOturumuKurali.YerlesimYokMesaji);
    }

    /// <summary>Aracın etkin işaretleri (yükleyen adıyla), yükleme zamanına göre.</summary>
    private async Task<List<YukIsareti>> IsaretlerAsync(int oturumId, int aracNo, CancellationToken cancellationToken)
    {
        var kayitlar = await context.YuklemeOturumYukleri.AsNoTracking()
            .Where(y => y.OturumId == oturumId && y.AracNo == aracNo && y.GeriAlinmaTarihi == null)
            .OrderBy(y => y.YuklemeTarihi).ThenBy(y => y.Id)
            .ToListAsync(cancellationToken);
        var adlar = await KullaniciAdlariAsync(kayitlar.Select(k => k.YukleyenKullaniciId), cancellationToken);
        return kayitlar.Select(k => new YukIsareti
        {
            Id = k.Id,
            YukAnahtari = k.YukAnahtari,
            AdimNo = k.AdimNo,
            Kaynak = k.Kaynak,
            SiraDisi = k.SiraDisi,
            Yukleyen = k.YukleyenKullaniciId is { } id ? adlar.GetValueOrDefault(id) : null,
            YuklemeTarihi = k.YuklemeTarihi
        }).ToList();
    }

    private async Task<AracYuklemeAraci> AracModeliAsync(YuklemeOturumu oturum, PlanAraci arac, CancellationToken cancellationToken)
    {
        var isaretler = await IsaretlerAsync(oturum.Id, arac.AracNo, cancellationToken);
        var modeller = await OturumModelleriAsync([oturum], new Dictionary<int, int> { [oturum.PlanId] = oturum.Plan.SonRevizyonNo }, cancellationToken);
        return new AracYuklemeAraci
        {
            PlanId = oturum.PlanId,
            PlanNo = oturum.Plan.PlanNo,
            PlanAdi = oturum.Plan.Ad,
            Oturum = modeller[oturum.Id],
            Arac = arac,
            Isaretler = isaretler,
            SiradakiAdimNo = YuklemeOturumuKurali.SiradakiAdimNo(
                YuklemeSirasiKurali.Adimlar(arac.Konumlar), isaretler.Select(i => i.YukAnahtari).ToHashSet(StringComparer.Ordinal))
        };
    }

    /// <summary>Oturum başlıkları: kullanıcı adları ve etkin işaret sayılarıyla; plan revize edildiyse işaretlenir.</summary>
    private async Task<Dictionary<int, AracYuklemeOturumu>> OturumModelleriAsync(
        IReadOnlyCollection<YuklemeOturumu> oturumlar, IReadOnlyDictionary<int, int> planSonRevizyonlari, CancellationToken cancellationToken)
    {
        if (oturumlar.Count == 0)
            return [];
        var idler = oturumlar.Select(o => o.Id).ToList();
        var sayilar = await context.YuklemeOturumYukleri.AsNoTracking()
            .Where(y => idler.Contains(y.OturumId) && y.GeriAlinmaTarihi == null)
            .GroupBy(y => y.OturumId).Select(g => new { OturumId = g.Key, Adet = g.Count() })
            .ToDictionaryAsync(x => x.OturumId, x => x.Adet, cancellationToken);
        var adlar = await KullaniciAdlariAsync(oturumlar.SelectMany(o => new[] { o.BaslatanKullaniciId, o.KapatanKullaniciId }), cancellationToken);
        return oturumlar.ToDictionary(o => o.Id, o => new AracYuklemeOturumu
        {
            Id = o.Id,
            RevizyonNo = o.RevizyonNo,
            Durum = o.Durum,
            Baslatan = o.BaslatanKullaniciId is { } b ? adlar.GetValueOrDefault(b) : null,
            BaslamaTarihi = o.BaslamaTarihi,
            Kapatan = o.KapatanKullaniciId is { } k ? adlar.GetValueOrDefault(k) : null,
            KapanisTarihi = o.KapanisTarihi,
            KapanisGerekcesi = o.KapanisGerekcesi,
            AracSayisi = o.AracSayisi,
            ToplamYuk = o.ToplamYuk,
            YuklenenYuk = sayilar.GetValueOrDefault(o.Id),
            PlanDegisti = planSonRevizyonlari.GetValueOrDefault(o.PlanId, o.RevizyonNo) != o.RevizyonNo
        });
    }

    private async Task<Dictionary<int, string>> KullaniciAdlariAsync(IEnumerable<int?> kullaniciIdleri, CancellationToken cancellationToken)
    {
        var idler = kullaniciIdleri.Where(id => id != null).Select(id => id!.Value).Distinct().ToList();
        return idler.Count == 0
            ? []
            : await context.Kullanicilar.AsNoTracking().Where(k => idler.Contains(k.Id)).ToDictionaryAsync(k => k.Id, k => k.AdSoyad, cancellationToken);
    }

    private static string SiraDisiMesaji(IReadOnlyList<YuklemeAdimi> adimlar, IReadOnlySet<string> yuklenenler, string kod)
    {
        var siradaki = adimlar.First(a => a.AdimNo == YuklemeOturumuKurali.SiradakiAdimNo(adimlar, yuklenenler));
        var bekleyenler = siradaki.Yukler.Where(y => !yuklenenler.Contains(y.Konum.YukAnahtari)).Select(y => y.Konum.Kod);
        return $"Sıradaki yük {string.Join(", ", bekleyenler)} (adım {siradaki.AdimNo}); {kod} sıra dışı. Sıra dışı yükleme onay ister.";
    }
}
