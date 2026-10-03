using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.AmbalajIslemleri;

/// <summary>
/// Seçilen kayıtlardan üretim formunu güncel veriyle hesaplar. Form oluşturma komutu ve proje formu
/// indirmesi (D10) aynı hesabı kullanır; oluşturma anındaki hesap kayıt izi olarak saklanır.
/// </summary>
internal static class AmbalajUretimFormuOlusturucu
{
    public const int EnFazlaKayit = 500;

    public static async Task<Result<AmbalajUretimFormuModel>> OlusturAsync(
        IUnitOfWork unitOfWork,
        IReadOnlyCollection<int> kayitIdleri,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var secimIdleri = kayitIdleri.Distinct().ToArray();
        if (secimIdleri.Length == 0)
            return Result<AmbalajUretimFormuModel>.Failure("Üretim formu için en az bir kayıt seçilmelidir.", 400);
        if (secimIdleri.Any(id => id <= 0))
            return Result<AmbalajUretimFormuModel>.Failure("Seçili kayıt kimlikleri sıfırdan büyük olmalıdır.", 400);
        if (secimIdleri.Length > EnFazlaKayit)
            return Result<AmbalajUretimFormuModel>.Failure(
                $"Tek seferde en fazla {EnFazlaKayit} sandık seçilebilir.", 400);

        var kayitlar = (await unitOfWork.GetRepository<AmbalajUretimKaydi>().FindAsync(
                k => secimIdleri.Contains(k.Id)))
            .ToList();
        var bulunanIdler = kayitlar.Select(k => k.Id).ToHashSet();
        var bulunamayanIdler = secimIdleri.Where(id => !bulunanIdler.Contains(id)).ToArray();
        if (bulunamayanIdler.Length > 0)
            return Result<AmbalajUretimFormuModel>.Failure(
                $"Seçilen ambalaj üretim kayıtları bulunamadı: {KimlikleriYaz(bulunamayanIdler)}.", 404);

        // Form üretimi başlatır; kaydın önceden üretime alınmış olması beklenmez.
        var uygunOlmayanlar = kayitlar.Where(k => k.IptalMi || !k.AmbalajaDahil).OrderBy(k => k.Id).ToArray();
        if (uygunOlmayanlar.Length > 0)
            return Result<AmbalajUretimFormuModel>.Failure(
                "Seçilen sandıkların tamamı aktif ve ambalaja dahil olmalıdır. " +
                $"Uygun olmayan kayıtlar: {KayitlariYaz(uygunOlmayanlar)}.",
                409);

        var projeHatasi = AyniProjeKontrolu(kayitlar);
        if (projeHatasi != null)
            return Result<AmbalajUretimFormuModel>.Failure(projeHatasi, 409);
        var projeId = kayitlar.Select(k => k.ProjeId).FirstOrDefault(id => id.HasValue);
        var proje = projeId.HasValue
            ? await unitOfWork.GetRepository<Proje>().GetByIdAsync(projeId.Value)
            : null;
        if (projeId.HasValue && proje == null)
            return Result<AmbalajUretimFormuModel>.Failure("Seçilen sandıkların projesi bulunamadı.", 404);

        kayitlar = kayitlar.OrderBy(k => k.Tur).ThenBy(k => k.SandikNo).ThenBy(k => k.Id).ToList();
        var uyumsuzFormul = kayitlar.FirstOrDefault(k => AmbalajUretimPolitikasi.M3HesaplanabilirMi(k.SandikCinsi) && !string.Equals(
            k.M3HesaplamaVersiyonu,
            AmbalajHesaplayici.FormulVersiyonu,
            StringComparison.Ordinal));
        if (uyumsuzFormul != null)
            return Result<AmbalajUretimFormuModel>.Failure(
                $"{uyumsuzFormul.SandikNo} numaralı sandığın {uyumsuzFormul.M3HesaplamaVersiyonu} formül sürümüne ait parça snapshotı bulunmuyor. " +
                $"Güncel {AmbalajHesaplayici.FormulVersiyonu} formülü eski sürüm etiketiyle kullanılamaz.",
                409);
        var eksik = kayitlar.FirstOrDefault(k => !AmbalajUretimYardimcilari.OlculerGecerli(k));
        if (eksik != null)
            return Result<AmbalajUretimFormuModel>.Failure(
                $"{eksik.SandikNo} numaralı sandığın ölçüleri eksik olduğu için üretim formu oluşturulamadı.",
                409);

        var kaynakSandikIds = kayitlar
            .Where(k => k.KaynakModul == AmbalajKaynakModulu.Sandik && k.KaynakKayitId.HasValue)
            .Select(k => k.KaynakKayitId!.Value)
            .Distinct()
            .ToList();
        var brutKgMap = kaynakSandikIds.Count == 0
            ? new Dictionary<int, decimal?>()
            : (await unitOfWork.GetRepository<Sandik>().FindAsync(s => kaynakSandikIds.Contains(s.Id)))
                .ToDictionary(s => s.Id, s => s.GrossKg);

        cancellationToken.ThrowIfCancellationRequested();
        var kalemler = kayitlar.Select(k => KalemOlustur(k, brutKgMap)).ToList();
        var ilk = kayitlar[0];
        return Result<AmbalajUretimFormuModel>.Success(new AmbalajUretimFormuModel
        {
            ProjeId = proje?.Id,
            ProjeNo = proje?.ProjeNo ?? ilk.ManuelProjeNo ?? "BAĞIMSIZ",
            ProjeAdi = proje?.Musteri ?? ilk.ManuelProjeAdi,
            FBNo = proje?.FBNo,
            Kalemler = kalemler,
            NetM3 = kalemler.Any(k => k.NetM3.HasValue) ? kalemler.Sum(k => k.NetM3) : null,
            SarfM3 = kalemler.Any(k => k.SarfM3.HasValue) ? kalemler.Sum(k => k.SarfM3) : null,
            ToplamM3 = kalemler.Any(k => k.ToplamM3.HasValue) ? kalemler.Sum(k => k.ToplamM3) : null
        });
    }

    /// <summary>Tek form yalnız aynı sistem projesinin veya aynı manuel projenin kayıtlarını kapsar.</summary>
    private static string? AyniProjeKontrolu(IReadOnlyList<AmbalajUretimKaydi> kayitlar)
    {
        var sistemProjeIdleri = kayitlar
            .Where(k => k.ProjeId.HasValue)
            .Select(k => k.ProjeId!.Value)
            .Distinct()
            .ToArray();
        var manuelKayitVar = kayitlar.Any(k => !k.ProjeId.HasValue);
        if (sistemProjeIdleri.Length > 1 || (sistemProjeIdleri.Length == 1 && manuelKayitVar))
            return "Seçilen sandıklar farklı projelere ait. Toplu üretim formu yalnız aynı proje altındaki sandıklar için oluşturulabilir.";

        var manuelProjeAnahtarlari = kayitlar
            .Where(k => !k.ProjeId.HasValue)
            .Select(k => $"{k.ManuelProjeNo?.Trim()}\u001f{k.ManuelProjeAdi?.Trim()}")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        return manuelProjeAnahtarlari > 1
            ? "Seçilen sandıklar farklı manuel projelere ait. Toplu üretim formu yalnız aynı manuel proje altındaki sandıklar için oluşturulabilir."
            : null;
    }

    private static string KimlikleriYaz(IEnumerable<int> kimlikler)
    {
        const int gosterilecekKayitSayisi = 10;
        var liste = kimlikler.Take(gosterilecekKayitSayisi).ToArray();
        var metin = string.Join(", ", liste);
        return kimlikler.Skip(gosterilecekKayitSayisi).Any() ? $"{metin}, ..." : metin;
    }

    private static string KayitlariYaz(IEnumerable<AmbalajUretimKaydi> kayitlar)
    {
        const int gosterilecekKayitSayisi = 10;
        var liste = kayitlar.Take(gosterilecekKayitSayisi)
            .Select(k => string.IsNullOrWhiteSpace(k.SandikNo) ? $"ID {k.Id}" : $"{k.SandikNo} (ID {k.Id})")
            .ToArray();
        var metin = string.Join(", ", liste);
        return kayitlar.Skip(gosterilecekKayitSayisi).Any() ? $"{metin}, ..." : metin;
    }

    private static AmbalajUretimFormuKalemiModel KalemOlustur(
        AmbalajUretimKaydi kayit,
        IReadOnlyDictionary<int, decimal?> brutKgMap)
    {
        if (!AmbalajUretimPolitikasi.M3HesaplanabilirMi(kayit.SandikCinsi))
            return new AmbalajUretimFormuKalemiModel
            {
                Tur = kayit.Tur, BagimsizKayitMi = kayit.BagimsizKayitMi,
                KayitId = kayit.Id, IsAkisKimligi = kayit.IsAkisKimligi,
                SandikNo = kayit.SandikNo, SandikAdi = kayit.Ad,
                SandikTuru = AmbalajUretimYardimcilari.TurMetni(kayit.Tur),
                SandikCinsi = AmbalajUretimYardimcilari.CinsMetni(kayit.SandikCinsi, kayit.DigerSandikCinsi),
                Adet = kayit.Adet, M3HesaplanabilirMi = false,
                IcOlculer = new(kayit.Boy, kayit.En, kayit.Yukseklik),
                DisOlculer = new(kayit.Boy, kayit.En, kayit.Yukseklik),
                FormulVersiyonu = "uygulanmaz-v1", FirinPartiNo = kayit.FirinPartiNo,
                KullanimAmaci = kayit.KullanimAmaci, TalimatVeren = kayit.TalimatVeren,
                Aciklama = kayit.Aciklama, UretimTarihi = kayit.UretimTarihi
            };
        var ayakProfili = AmbalajAyakProfiliBelirleyici.Belirle(kayit.Ad, kayit.KullanimAmaci);
        var icOlculer = AmbalajUretimYardimcilari.HesaplamaIcOlculeriniGetir(kayit);
        var hesap = AmbalajHesaplayici.Hesapla(
            icOlculer.Boy,
            icOlculer.En,
            icOlculer.Yukseklik,
            ayakProfili,
            AmbalajUretimYardimcilari.KaynakSandikOlculeriMi(kayit) ? kayit.Boy : null);
        return new AmbalajUretimFormuKalemiModel
        {
            Tur = kayit.Tur, BagimsizKayitMi = kayit.BagimsizKayitMi,
            KayitId = kayit.Id,
            IsAkisKimligi = kayit.IsAkisKimligi,
            SandikNo = kayit.SandikNo,
            SandikAdi = kayit.Ad,
            SandikTuru = AmbalajUretimYardimcilari.TurMetni(kayit.Tur),
            SandikCinsi = AmbalajUretimYardimcilari.CinsMetni(kayit.SandikCinsi, kayit.DigerSandikCinsi),
            Adet = kayit.Adet,
            BrutKg = kayit.KaynakKayitId.HasValue && brutKgMap.TryGetValue(kayit.KaynakKayitId.Value, out var brutKg)
                ? brutKg
                : null,
            KullanimAmaci = kayit.KullanimAmaci,
            TalimatVeren = kayit.TalimatVeren,
            Aciklama = kayit.Aciklama,
            IcOlculer = hesap.IcOlculer,
            DisOlculer = hesap.DisOlculer,
            UstKizakAdedi = hesap.UstKizakAdedi,
            AyakAdedi = hesap.AyakAdedi,
            YanKusakAdedi = hesap.YanKusakAdedi,
            OnDuvarYuksekligi = hesap.OnDuvarYuksekligi,
            FormulVersiyonu = kayit.M3HesaplamaVersiyonu,
            HesaplananNetM3 = kayit.HesaplananToplamM3,
            M3Override = kayit.M3Override,
            NetM3 = kayit.M3Override ?? kayit.HesaplananToplamM3,
            SarfOrani = kayit.SarfOrani,
            SarfM3 = kayit.SarfM3,
            ToplamM3 = kayit.ToplamM3,
            FirinPartiNo = kayit.FirinPartiNo,
            UretimTarihi = kayit.UretimTarihi,
            Parcalar = hesap.Parcalar.Select(parca =>
            {
                var teorikAdet = parca.Adet * kayit.Adet;
                return new AmbalajUretimFormuParcasiModel
                {
                    Kod = parca.Kod,
                    Grup = parca.Grup,
                    Aciklama = parca.Aciklama,
                    Malzeme = parca.Malzeme,
                    KesitEn = parca.KesitEn,
                    KesitYukseklik = parca.KesitYukseklik,
                    Uzunluk = parca.Uzunluk,
                    TeorikAdet = teorikAdet,
                    KesimAdedi = (int)Math.Ceiling(teorikAdet),
                    HacimM3 = Math.Round(parca.HacimM3 * kayit.Adet, 6, MidpointRounding.AwayFromZero)
                };
            }).ToList()
        };
    }
}
