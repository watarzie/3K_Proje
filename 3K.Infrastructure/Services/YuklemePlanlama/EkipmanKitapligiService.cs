using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Exceptions;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;
using _3K.YuklemeMotoru.Ekipman;

namespace _3K.Infrastructure.Services.YuklemePlanlama;

/// <summary>
/// Ekipman kitaplığı (Simülasyon isterleri 11–12): aile/tip/model/konfigürasyon ve değişmez teknik veri revizyonları.
/// Teknik veri yükleme motorunun şemasıyla okunur ve denetlenir; eksik veri uyarı olarak saklanır, değer tahmin edilmez.
/// Excel içe aktarımı ve ekrandan kayıt tek konfigürasyon için aynı kararı ve aynı yazımı kullanır.
/// </summary>
internal sealed class EkipmanKitapligiService(AppDbContext context, IUnitOfWork unitOfWork) : IEkipmanKitapligiService
{
    private const string KonfigurasyonBulunamadi = "Ekipman konfigürasyonu bulunamadı.";

    public async Task<IReadOnlyList<EkipmanAilesiModeli>> AileleriListeleAsync(CancellationToken cancellationToken)
    {
        var aileler = await context.EkipmanAileleri.AsNoTracking()
            .Include(a => a.Tipler)
            .OrderBy(a => a.Sira)
            .ToListAsync(cancellationToken);
        return aileler.Select(a => new EkipmanAilesiModeli
        {
            Id = a.Id,
            Kod = a.Kod,
            Ad = a.Ad,
            Sira = a.Sira,
            Tipler = a.Tipler.OrderBy(t => t.Sira).Select(TipModeli).ToList()
        }).ToList();
    }

    public async Task<SayfaliSonuc<EkipmanKonfigurasyonOzeti>> KonfigurasyonlariListeleAsync(
        EkipmanListeKriteri kriter, int? sayfa, int? boyut, CancellationToken cancellationToken)
    {
        var sorgu = context.EkipmanKonfigurasyonlari.AsNoTracking();
        if (!kriter.PasifleriGoster)
            sorgu = sorgu.Where(k => k.Aktif);
        if (kriter.AileId is { } aileId)
            sorgu = sorgu.Where(k => k.Model.Tip.AileId == aileId);
        if (kriter.TipId is { } tipId)
            sorgu = sorgu.Where(k => k.Model.TipId == tipId);
        if (kriter.DogrulamaDurumu is { } durum)
            sorgu = sorgu.Where(k => k.Revizyonlar.Any(r => r.RevizyonNo == k.SonRevizyonNo && r.DogrulamaDurumu == durum));
        if (!string.IsNullOrWhiteSpace(kriter.Arama))
        {
            var desen = $"%{IlikeDeseniniKacir(kriter.Arama.Trim())}%";
            sorgu = sorgu.Where(k => EF.Functions.ILike(k.Kod, desen, "\\") || EF.Functions.ILike(k.Ad, desen, "\\") ||
                                     EF.Functions.ILike(k.Model.Uretici, desen, "\\") || EF.Functions.ILike(k.Model.ModelAdi, desen, "\\"));
        }

        var toplam = await sorgu.CountAsync(cancellationToken);
        var sayfaBoyutu = SayfaKurali.Boyut(boyut);
        var gecerliSayfa = SayfaKurali.Sayfa(sayfa, sayfaBoyutu, toplam);
        var satirlar = await sorgu
            .OrderBy(k => k.Model.Tip.Aile.Sira).ThenBy(k => k.Model.Tip.Sira).ThenBy(k => k.Kod)
            .Skip(SayfaKurali.Atlanan(gecerliSayfa, sayfaBoyutu)).Take(sayfaBoyutu)
            .Select(k => new
            {
                k.Id,
                k.Kod,
                k.Ad,
                AileAdi = k.Model.Tip.Aile.Ad,
                TipAdi = k.Model.Tip.Ad,
                k.Model.Tip.Kategori,
                k.Model.Uretici,
                k.Model.ModelAdi,
                k.Aktif,
                k.SonRevizyonNo,
                Revizyon = k.Revizyonlar.Where(r => r.RevizyonNo == k.SonRevizyonNo)
                    .Select(r => new { r.DogrulamaDurumu, r.KaynakTuru, r.KaynakTarihi, r.TeknikVeriJson })
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return new SayfaliSonuc<EkipmanKonfigurasyonOzeti>
        {
            Items = satirlar.Select(k =>
            {
                var veri = k.Revizyon == null ? null : EkipmanTeknikVeriSerilestirici.Oku(k.Revizyon.TeknikVeriJson);
                return new EkipmanKonfigurasyonOzeti
                {
                    Id = k.Id,
                    Kod = k.Kod,
                    Ad = k.Ad,
                    AileAdi = k.AileAdi,
                    TipAdi = k.TipAdi,
                    Kategori = k.Kategori,
                    Uretici = k.Uretici,
                    ModelAdi = k.ModelAdi,
                    Aktif = k.Aktif,
                    RevizyonNo = k.SonRevizyonNo,
                    DogrulamaDurumu = k.Revizyon?.DogrulamaDurumu ?? EkipmanDogrulamaDurumu.Dogrulanmadi,
                    KaynakTuru = k.Revizyon?.KaynakTuru ?? EkipmanKaynakTuru.Diger,
                    KaynakTarihi = k.Revizyon?.KaynakTarihi,
                    DisUzunlukMm = veri?.DisOlcu?.UzunlukMm,
                    DisGenislikMm = veri?.DisOlcu?.GenislikMm,
                    DisYukseklikMm = veri?.DisOlcu?.YukseklikMm,
                    TeknikKapasiteKg = veri?.TeknikKapasiteKg
                };
            }).ToList(),
            PageNumber = gecerliSayfa,
            PageSize = sayfaBoyutu,
            TotalCount = toplam
        };
    }

    public async Task<EkipmanKonfigurasyonDetayi?> KonfigurasyonGetirAsync(int konfigurasyonId, CancellationToken cancellationToken)
    {
        var konf = await context.EkipmanKonfigurasyonlari.AsNoTracking()
            .Include(k => k.Model).ThenInclude(m => m.Tip).ThenInclude(t => t.Aile)
            .SingleOrDefaultAsync(k => k.Id == konfigurasyonId, cancellationToken);
        if (konf == null)
            return null;

        var revizyonlar = await context.EkipmanKonfigurasyonRevizyonlari.AsNoTracking()
            .Where(r => r.KonfigurasyonId == konfigurasyonId)
            .OrderByDescending(r => r.RevizyonNo)
            .Take(EkipmanKitapligiKurali.GosterilenRevizyonSayisi)
            .ToListAsync(cancellationToken);
        var kullaniciIdleri = revizyonlar.SelectMany(r => new[] { r.DogrulayanKullaniciId, r.OlusturanKullaniciId })
            .Where(id => id != null).Select(id => id!.Value).Distinct().ToList();
        var kullanicilar = await context.Kullanicilar.AsNoTracking()
            .Where(k => kullaniciIdleri.Contains(k.Id)).ToDictionaryAsync(k => k.Id, k => k.AdSoyad, cancellationToken);

        EkipmanRevizyonBilgisi Bilgi(EkipmanKonfigurasyonRevizyonu r) => new()
        {
            RevizyonNo = r.RevizyonNo,
            KaynakTuru = r.KaynakTuru,
            KaynakAciklama = r.KaynakAciklama,
            KaynakTarihi = r.KaynakTarihi,
            Aciklama = r.Aciklama,
            DogrulamaDurumu = r.DogrulamaDurumu,
            Dogrulayan = r.DogrulayanKullaniciId is { } d ? kullanicilar.GetValueOrDefault(d) : null,
            DogrulamaTarihi = r.DogrulamaTarihi,
            Olusturan = r.OlusturanKullaniciId is { } o ? kullanicilar.GetValueOrDefault(o) : null,
            Tarih = r.CreatedDate
        };

        var son = revizyonlar.FirstOrDefault(r => r.RevizyonNo == konf.SonRevizyonNo);
        var kategori = konf.Model.Tip.Kategori;
        var veri = son == null ? null : EkipmanTeknikVeriSerilestirici.Oku(son.TeknikVeriJson);
        return new EkipmanKonfigurasyonDetayi
        {
            Id = konf.Id,
            Kod = konf.Kod,
            Ad = konf.Ad,
            Aktif = konf.Aktif,
            AileAdi = konf.Model.Tip.Aile.Ad,
            TipId = konf.Model.TipId,
            TipKodu = konf.Model.Tip.Kod,
            TipAdi = konf.Model.Tip.Ad,
            Kategori = kategori,
            YuklemeYontemleri = EkipmanKategoriKurali.YuklemeYontemleri(kategori),
            SandikTasimayaUygun = EkipmanKategoriKurali.SandikTasimayaUygunMu(kategori),
            Uretici = konf.Model.Uretici,
            ModelAdi = konf.Model.ModelAdi,
            Revizyon = son == null ? new EkipmanRevizyonBilgisi() : Bilgi(son),
            TeknikVeriJson = son?.TeknikVeriJson ?? "{}",
            VeriUyarilari = veri == null ? [] : EkipmanTeknikVeriDenetimi.Denetle(veri, Baglam(kategori)).Uyarilar,
            Revizyonlar = revizyonlar.Select(Bilgi).ToList()
        };
    }

    public async Task<byte[]> SablonOlusturAsync(CancellationToken cancellationToken) =>
        EkipmanExcelDosyasi.Yaz(await ExcelTipleriAsync(cancellationToken), []);

    public async Task<byte[]> DisaAktarAsync(CancellationToken cancellationToken)
    {
        var toplam = await context.EkipmanKonfigurasyonlari.CountAsync(k => k.Aktif, cancellationToken);
        var satirlar = await context.EkipmanKonfigurasyonlari.AsNoTracking()
            .Where(k => k.Aktif)
            .OrderBy(k => k.Model.Tip.Aile.Sira).ThenBy(k => k.Model.Tip.Sira).ThenBy(k => k.Kod)
            .Take(EkipmanKitapligiKurali.DosyaEnFazlaKonfigurasyon)
            .Select(k => new
            {
                k.Kod,
                k.Ad,
                TipKodu = k.Model.Tip.Kod,
                k.Model.Uretici,
                k.Model.ModelAdi,
                Revizyon = k.Revizyonlar.Where(r => r.RevizyonNo == k.SonRevizyonNo)
                    .Select(r => new { r.KaynakTuru, r.KaynakAciklama, r.KaynakTarihi, r.Aciklama, r.TeknikVeriJson })
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);
        var kayitlar = satirlar
            .Where(k => k.Revizyon != null)
            .Select(k => new EkipmanExcelKaydi(0, k.Kod, k.Ad, k.TipKodu, k.Uretici, k.ModelAdi, k.Revizyon!.KaynakTuru,
                k.Revizyon.KaynakAciklama, k.Revizyon.KaynakTarihi, k.Revizyon.Aciklama,
                EkipmanTeknikVeriSerilestirici.Oku(k.Revizyon.TeknikVeriJson) ?? new EkipmanTeknikVerisi()))
            .ToList();
        var kesilmeNotu = toplam > EkipmanKitapligiKurali.DosyaEnFazlaKonfigurasyon
            ? $"Bu dosya {toplam} aktif konfigürasyonun ilk {EkipmanKitapligiKurali.DosyaEnFazlaKonfigurasyon} tanesini içerir (aile, tip, kod sırasıyla)."
            : null;
        return EkipmanExcelDosyasi.Yaz(await ExcelTipleriAsync(cancellationToken), kayitlar, kesilmeNotu);
    }

    public async Task<EkipmanIceAktarimSonucu> IceAktarAsync(Stream dosya, bool uygula, int? kullaniciId, CancellationToken cancellationToken)
    {
        var okuma = EkipmanExcelDosyasi.Oku(dosya, cancellationToken);
        var sorunlar = okuma.Sorunlar.ToList();
        var tipler = await context.EkipmanTipleri.AsNoTracking().ToDictionaryAsync(t => t.Kod, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var kodlar = okuma.Kayitlar.Select(k => k.Kod).ToList();
        var mevcutlar = await context.EkipmanKonfigurasyonlari
            .Include(k => k.Model).ThenInclude(m => m.Tip)
            .Where(k => kodlar.Contains(k.Kod))
            .ToDictionaryAsync(k => k.Kod, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var mevcutIdleri = mevcutlar.Values.Select(k => k.Id).ToList();
        var sonRevizyonlar = await context.EkipmanKonfigurasyonRevizyonlari.AsNoTracking()
            .Where(r => mevcutIdleri.Contains(r.KonfigurasyonId) && r.RevizyonNo == r.Konfigurasyon.SonRevizyonNo)
            .ToDictionaryAsync(r => r.KonfigurasyonId, cancellationToken);

        var satirlar = new List<EkipmanIceAktarimSatiri>();
        var yazilacaklar = new List<(EkipmanExcelKaydi Kayit, EkipmanTipi Tip, EkipmanKonfigurasyonu? Mevcut, EkipmanIceAktarimIslemi Islem, string Json)>();
        foreach (var kayit in okuma.Kayitlar)
        {
            if (!tipler.TryGetValue(kayit.TipKodu, out var tip))
            {
                sorunlar.Add(new(EkipmanExcelDosyasi.KonfigurasyonSayfasi, kayit.Satir, kayit.Kod,
                    $"TipKodu '{kayit.TipKodu}' tanımlı değil (Tipler sayfasına bakın)."));
                continue;
            }

            var mevcut = mevcutlar.GetValueOrDefault(kayit.Kod);
            var karar = Degerlendir(kayit, tip, mevcut, mevcut == null ? null : sonRevizyonlar.GetValueOrDefault(mevcut.Id));
            sorunlar.AddRange(karar.Denetim.Hatalar.Select(h => new EkipmanIceAktarimSorunu(EkipmanExcelDosyasi.KonfigurasyonSayfasi, kayit.Satir, kayit.Kod, h)));
            if (karar.BaskaModel)
            {
                sorunlar.Add(new(EkipmanExcelDosyasi.KonfigurasyonSayfasi, kayit.Satir, kayit.Kod,
                    $"'{kayit.Kod}' kodu başka bir modele ait ({mevcut!.Model.Tip.Kod} / {mevcut.Model.Uretici} / {mevcut.Model.ModelAdi}); kodu değiştirin veya modeli aynı yazın."));
                continue;
            }

            satirlar.Add(new EkipmanIceAktarimSatiri
            {
                Kod = kayit.Kod,
                Ad = kayit.Ad,
                OncekiAd = mevcut != null && !string.Equals(mevcut.Ad, kayit.Ad, StringComparison.Ordinal) ? mevcut.Ad : null,
                TipKodu = tip.Kod,
                Islem = karar.Islem,
                RevizyonNo = karar.Islem switch
                {
                    EkipmanIceAktarimIslemi.Yeni => 1,
                    EkipmanIceAktarimIslemi.YeniRevizyon => mevcut!.SonRevizyonNo + 1,
                    _ => mevcut!.SonRevizyonNo
                },
                VeriUyarilari = karar.Denetim.Uyarilar
            });
            yazilacaklar.Add((kayit, tip, mevcut, karar.Islem, karar.Json));
        }

        var sonuc = new EkipmanIceAktarimSonucu
        {
            Uygulandi = false,
            YeniSayisi = satirlar.Count(s => s.Islem == EkipmanIceAktarimIslemi.Yeni),
            YeniRevizyonSayisi = satirlar.Count(s => s.Islem == EkipmanIceAktarimIslemi.YeniRevizyon),
            DegismeyenSayisi = satirlar.Count(s => s.Islem == EkipmanIceAktarimIslemi.Degismedi),
            Konfigurasyonlar = satirlar,
            Sorunlar = sorunlar
        };
        if (!uygula || sorunlar.Count > 0 || okuma.Kayitlar.Count == 0)
        {
            if (uygula && sorunlar.Count == 0)
                return new EkipmanIceAktarimSonucu
                {
                    Sorunlar = [new(EkipmanExcelDosyasi.KonfigurasyonSayfasi, null, null, "Dosyada konfigürasyon yok.")]
                };
            return sonuc;
        }

        await YazAsync(yazilacaklar, kullaniciId, cancellationToken);
        return new EkipmanIceAktarimSonucu
        {
            Uygulandi = true,
            YeniSayisi = sonuc.YeniSayisi,
            YeniRevizyonSayisi = sonuc.YeniRevizyonSayisi,
            DegismeyenSayisi = sonuc.DegismeyenSayisi,
            Konfigurasyonlar = satirlar,
            Sorunlar = []
        };
    }

    public async Task<EkipmanKayitSonucu> EkleAsync(EkipmanKonfigurasyonKaydi kayit, int? kullaniciId, CancellationToken cancellationToken)
    {
        var kod = EkipmanKitapligiKurali.KodNormalize(kayit.Kod);
        // İçe aktarım var olan koda revizyon yazar; ekrandaki "yeni ekipman" var olan kaydı sessizce değiştirmez.
        if (await context.EkipmanKonfigurasyonlari.AnyAsync(k => k.Kod == kod, cancellationToken))
            throw IsKuraliException.Cakisma(EkipmanKitapligiKurali.KodZatenVarMesaji);
        return await KaydetAsync(kayit, kod, mevcut: null, kullaniciId, cancellationToken);
    }

    public async Task<EkipmanKayitSonucu> GuncelleAsync(
        int konfigurasyonId, int beklenenRevizyonNo, EkipmanKonfigurasyonKaydi kayit, int? kullaniciId, CancellationToken cancellationToken)
    {
        var mevcut = await context.EkipmanKonfigurasyonlari
                         .Include(k => k.Model).ThenInclude(m => m.Tip)
                         .SingleOrDefaultAsync(k => k.Id == konfigurasyonId, cancellationToken)
                     ?? throw new KayitBulunamadiException(KonfigurasyonBulunamadi);
        // Kod konfigürasyonun kimliğidir (içe aktarım da kodla eşler, harf duyarsız); güncellemede değişmez.
        if (!string.Equals(mevcut.Kod, EkipmanKitapligiKurali.KodNormalize(kayit.Kod), StringComparison.OrdinalIgnoreCase))
            throw new IsKuraliException(EkipmanKitapligiKurali.KodDegistirilemezMesaji);
        if (mevcut.SonRevizyonNo != beklenenRevizyonNo)
            throw IsKuraliException.Cakisma(EkipmanKitapligiKurali.KayitDegistiMesaji);
        return await KaydetAsync(kayit, mevcut.Kod, mevcut, kullaniciId, cancellationToken);
    }

    /// <summary>
    /// Ekrandan gelen kaydı Excel'deki tek konfigürasyon satırına çevirir ve içe aktarımın kararını
    /// (<see cref="Degerlendir"/>) ve yazımını (<see cref="YazAsync"/>) kullanır. Sorun varsa hiçbir şey yazılmaz.
    /// </summary>
    private async Task<EkipmanKayitSonucu> KaydetAsync(
        EkipmanKonfigurasyonKaydi kayit, string kod, EkipmanKonfigurasyonu? mevcut, int? kullaniciId, CancellationToken cancellationToken)
    {
        var girdi = EkipmanTeknikVeriGirdisi.Oku(kayit.TeknikVeriJson);
        var sorunlar = girdi.Sorunlar.ToList();
        var tip = await context.EkipmanTipleri.AsNoTracking().SingleOrDefaultAsync(t => t.Id == kayit.TipId, cancellationToken);
        if (tip == null)
            sorunlar.Add(EkipmanKitapligiKurali.TipBulunamadiMesaji);
        if (!EkipmanKitapligiKurali.KaynakTarihiGecerli(kayit.KaynakTarihi))
            sorunlar.Add(EkipmanKitapligiKurali.KaynakTarihiMesaji);
        // Okuma ya da sınır sorunu olan veri denetime girmez (Excel'de de sorunlu satır okunmadan düşer): sınırı aşan
        // kullanıcı metni denetim mesajlarında tekrarlanmaz.
        if (girdi.Veri is not { } teknikVeri || tip == null || sorunlar.Count > 0)
            return new EkipmanKayitSonucu { Sorunlar = sorunlar };

        // Metinler Excel hücresi gibi okunur (kırpılır, boş "yok" olur).
        var satir = new EkipmanExcelKaydi(0, kod,
            EkipmanKitapligiKurali.MetinNormalize(kayit.Ad) ?? string.Empty, tip.Kod,
            EkipmanKitapligiKurali.MetinNormalize(kayit.Uretici) ?? string.Empty,
            EkipmanKitapligiKurali.MetinNormalize(kayit.ModelAdi) ?? string.Empty,
            kayit.KaynakTuru, EkipmanKitapligiKurali.MetinNormalize(kayit.KaynakAciklama), kayit.KaynakTarihi,
            EkipmanKitapligiKurali.MetinNormalize(kayit.Aciklama), teknikVeri);
        var sonRevizyon = mevcut == null
            ? null
            : await context.EkipmanKonfigurasyonRevizyonlari.AsNoTracking()
                .SingleOrDefaultAsync(r => r.KonfigurasyonId == mevcut.Id && r.RevizyonNo == mevcut.SonRevizyonNo, cancellationToken);

        var karar = Degerlendir(satir, tip, mevcut, sonRevizyon);
        sorunlar.AddRange(karar.Denetim.Hatalar);
        if (karar.BaskaModel)
            sorunlar.Add($"Var olan ekipmanın tipi, üreticisi ve modeli değiştirilemez ({mevcut!.Model.Tip.Ad} / {mevcut.Model.Uretici} / {mevcut.Model.ModelAdi}); farklı model için yeni ekipman ekleyin.");
        if (sorunlar.Count > 0)
            return new EkipmanKayitSonucu { Sorunlar = sorunlar };

        // Teknik veri, kaynak ve ad aynıysa yazılacak bir şey yoktur (revizyon da açılmaz).
        var degisiklikYok = karar.Islem == EkipmanIceAktarimIslemi.Degismedi && string.Equals(mevcut!.Ad, satir.Ad, StringComparison.Ordinal);
        if (!degisiklikYok)
            await YazAsync([(satir, tip, mevcut, karar.Islem, karar.Json)], kullaniciId, cancellationToken);

        var id = mevcut?.Id ?? await context.EkipmanKonfigurasyonlari.Where(k => k.Kod == kod).Select(k => k.Id).SingleAsync(cancellationToken);
        return new EkipmanKayitSonucu { Detay = await KonfigurasyonGetirAsync(id, cancellationToken) };
    }

    public async Task<EkipmanKonfigurasyonDetayi> DogrulaAsync(int konfigurasyonId, int revizyonNo, int kullaniciId, CancellationToken cancellationToken)
    {
        var konf = await context.EkipmanKonfigurasyonlari.SingleOrDefaultAsync(k => k.Id == konfigurasyonId, cancellationToken)
                   ?? throw new KayitBulunamadiException(KonfigurasyonBulunamadi);
        if (konf.SonRevizyonNo != revizyonNo)
            throw IsKuraliException.Cakisma(EkipmanKitapligiKurali.GuncelRevizyonDegilMesaji);
        var revizyon = await context.EkipmanKonfigurasyonRevizyonlari
            .SingleAsync(r => r.KonfigurasyonId == konfigurasyonId && r.RevizyonNo == revizyonNo, cancellationToken);
        if (revizyon.DogrulamaDurumu == EkipmanDogrulamaDurumu.Dogrulandi)
            throw IsKuraliException.Cakisma(EkipmanKitapligiKurali.DogrulanmisMesaji);

        revizyon.DogrulamaDurumu = EkipmanDogrulamaDurumu.Dogrulandi;
        revizyon.DogrulayanKullaniciId = kullaniciId;
        revizyon.DogrulamaTarihi = TurkeyTime.Now;
        // Konfigürasyon satırı da yazılır: xmin, doğrulama ile aynı anda yeni revizyon yazılmasını yakalar (409).
        konf.UpdatedDate = TurkeyTime.Now;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return (await KonfigurasyonGetirAsync(konfigurasyonId, cancellationToken))!;
    }

    /// <summary>Yeni konfigürasyon ve revizyonları tek kaydetmede yazar (hepsi ya da hiçbiri).</summary>
    private async Task YazAsync(
        List<(EkipmanExcelKaydi Kayit, EkipmanTipi Tip, EkipmanKonfigurasyonu? Mevcut, EkipmanIceAktarimIslemi Islem, string Json)> yazilacaklar,
        int? kullaniciId, CancellationToken cancellationToken)
    {
        var tipIdleri = yazilacaklar.Select(y => y.Tip.Id).Distinct().ToList();
        var modeller = await context.EkipmanModelleri.Where(m => tipIdleri.Contains(m.TipId)).ToListAsync(cancellationToken);
        EkipmanModeli Model(EkipmanTipi tip, string uretici, string modelAdi)
        {
            var model = modeller.FirstOrDefault(m => m.TipId == tip.Id &&
                string.Equals(m.Uretici, uretici, StringComparison.Ordinal) && string.Equals(m.ModelAdi, modelAdi, StringComparison.Ordinal));
            if (model == null)
            {
                model = new EkipmanModeli { TipId = tip.Id, Uretici = uretici, ModelAdi = modelAdi };
                context.EkipmanModelleri.Add(model);
                modeller.Add(model);
            }
            return model;
        }

        foreach (var (kayit, tip, mevcut, islem, json) in yazilacaklar)
        {
            var konf = mevcut;
            if (konf == null)
            {
                konf = new EkipmanKonfigurasyonu { Model = Model(tip, kayit.Uretici, kayit.ModelAdi), Kod = kayit.Kod, Ad = kayit.Ad };
                context.EkipmanKonfigurasyonlari.Add(konf);
            }
            konf.Ad = kayit.Ad;
            if (islem == EkipmanIceAktarimIslemi.Degismedi)
                continue;

            konf.SonRevizyonNo++;
            konf.Revizyonlar.Add(new EkipmanKonfigurasyonRevizyonu
            {
                RevizyonNo = konf.SonRevizyonNo,
                TeknikVeriJson = json,
                KaynakTuru = kayit.KaynakTuru,
                KaynakAciklama = kayit.KaynakAciklama,
                KaynakTarihi = kayit.KaynakTarihi,
                Aciklama = kayit.Aciklama,
                OlusturanKullaniciId = kullaniciId
            });
        }
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Tek konfigürasyonun kararı: teknik veri denetimi, kodun başka modele ait olup olmadığı ve (aitse boş kalan)
    /// yazılacak işlem ile kanonik JSON.
    /// </summary>
    private sealed record KayitKarari(EkipmanDenetimSonucu Denetim, bool BaskaModel, EkipmanIceAktarimIslemi Islem, string Json);

    /// <summary>
    /// Tek konfigürasyonun kuralları; Excel içe aktarımı ve ekrandan kayıt aynı kararı kullanır: teknik veri kategoriye göre
    /// denetlenir, var olan kod başka tip/üretici/modele taşınamaz, teknik verisi ya da kaynağı değişen konfigürasyon yeni
    /// revizyon alır, değişmeyene dokunulmaz.
    /// </summary>
    private static KayitKarari Degerlendir(
        EkipmanExcelKaydi kayit, EkipmanTipi tip, EkipmanKonfigurasyonu? mevcut, EkipmanKonfigurasyonRevizyonu? sonRevizyon)
    {
        var denetim = EkipmanTeknikVeriDenetimi.Denetle(kayit.TeknikVeri, Baglam(tip.Kategori));
        if (mevcut != null &&
            (mevcut.Model.TipId != tip.Id ||
             !string.Equals(mevcut.Model.Uretici, kayit.Uretici, StringComparison.Ordinal) ||
             !string.Equals(mevcut.Model.ModelAdi, kayit.ModelAdi, StringComparison.Ordinal)))
            return new KayitKarari(denetim, BaskaModel: true, default, string.Empty);

        var json = EkipmanTeknikVeriSerilestirici.Yaz(kayit.TeknikVeri);
        var islem = mevcut == null
            ? EkipmanIceAktarimIslemi.Yeni
            : sonRevizyon != null && AyniRevizyon(sonRevizyon, kayit, json)
                ? EkipmanIceAktarimIslemi.Degismedi
                : EkipmanIceAktarimIslemi.YeniRevizyon;
        return new KayitKarari(denetim, BaskaModel: false, islem, json);
    }

    /// <summary>Teknik veri (kanonik JSON) ve kaynak bilgisi aynıysa yeni revizyon gerekmez.</summary>
    private static bool AyniRevizyon(EkipmanKonfigurasyonRevizyonu son, EkipmanExcelKaydi kayit, string yeniJson) =>
        EkipmanTeknikVeriSerilestirici.Oku(son.TeknikVeriJson) is { } eski &&
        EkipmanTeknikVeriSerilestirici.Yaz(eski) == yeniJson &&
        son.KaynakTuru == kayit.KaynakTuru &&
        son.KaynakAciklama == kayit.KaynakAciklama &&
        son.KaynakTarihi == kayit.KaynakTarihi &&
        son.Aciklama == kayit.Aciklama;

    private async Task<List<EkipmanExcelTipi>> ExcelTipleriAsync(CancellationToken cancellationToken) =>
        (await context.EkipmanTipleri.AsNoTracking()
            .Where(t => t.Aktif)
            .OrderBy(t => t.Aile.Sira).ThenBy(t => t.Sira)
            .Select(t => new { t.Kod, t.Ad, AileAdi = t.Aile.Ad, t.Kategori })
            .ToListAsync(cancellationToken))
        .Select(t => new EkipmanExcelTipi(t.Kod, t.Ad, t.AileAdi, t.Kategori))
        .ToList();

    /// <summary>Kategori kuralının motor denetimine çevirisi (açıklık tarafı adları motor şemasıyla aynı).</summary>
    private static EkipmanDenetimBaglami Baglam(EkipmanKategorisi kategori) => new(
        EkipmanKategoriKurali.GerekenAcikliklar(kategori).Select(taraf => taraf switch
        {
            EkipmanKategoriKurali.Arka => AciklikTarafi.Arka,
            EkipmanKategoriKurali.Sol => AciklikTarafi.Sol,
            EkipmanKategoriKurali.Sag => AciklikTarafi.Sag,
            EkipmanKategoriKurali.Ust => AciklikTarafi.Ust,
            _ => throw new ArgumentOutOfRangeException(nameof(kategori), taraf, "Tanımsız açıklık tarafı.")
        }).ToList(),
        EkipmanKategoriKurali.IcYukseklikGerekliMi(kategori),
        EkipmanKategoriKurali.DingilliMi(kategori));

    private static EkipmanTipiModeli TipModeli(EkipmanTipi t) => new()
    {
        Id = t.Id,
        Kod = t.Kod,
        Ad = t.Ad,
        Kategori = t.Kategori,
        YuklemeYontemleri = EkipmanKategoriKurali.YuklemeYontemleri(t.Kategori),
        SandikTasimayaUygun = EkipmanKategoriKurali.SandikTasimayaUygunMu(t.Kategori),
        Aktif = t.Aktif
    };

    private static string IlikeDeseniniKacir(string metin) =>
        metin.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
}
