using System.Globalization;
using System.Text.RegularExpressions;
using _3K.Core.Models;

namespace _3K.Infrastructure.Services
{
    /// <summary>
    /// PDF ve Excel üretim formlarında aynı fiziksel üretim tarifine sahip
    /// sandıkları tek bir rapor kaleminde toplar.
    /// </summary>
    internal static class AmbalajUretimFormuGruplayici
    {
        internal static IReadOnlyList<AmbalajUretimGrubu> Grupla(
            IReadOnlyList<AmbalajUretimFormuKalemiModel> kalemler)
        {
            ArgumentNullException.ThrowIfNull(kalemler);

            return kalemler
                .GroupBy(GrupAnahtariOlustur)
                .Select(grup => GrupOlustur(grup))
                .OrderBy(grup => SandikTuruSiraNo(grup.Temsilci.SandikTuru))
                .ThenBy(grup => grup.SandikNo, DogalSandikNoKarsilastiricisi.Instance)
                .ThenBy(grup => grup.Temsilci.SandikTuru, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static AmbalajUretimGrubu GrupOlustur(
            IGrouping<AmbalajUretimGrupAnahtari, AmbalajUretimFormuKalemiModel> grup)
        {
            var temsilci = grup.First();
            var parcalar = grup
                .SelectMany(kalem => kalem.Parcalar)
                .GroupBy(parca => new AmbalajParcaAnahtari(
                    NormalizeMetin(parca.Kod),
                    NormalizeMetin(parca.Grup),
                    NormalizeMetin(parca.Aciklama),
                    NormalizeMetin(parca.Malzeme),
                    parca.KesitEn,
                    parca.KesitYukseklik,
                    parca.Uzunluk))
                .Select(parcaGrubu =>
                {
                    var ilk = parcaGrubu.First();
                    var teorikAdet = parcaGrubu.Sum(p => p.TeorikAdet);
                    return new AmbalajUretimFormuParcasiModel
                    {
                        Kod = ilk.Kod,
                        Grup = ilk.Grup,
                        Aciklama = ilk.Aciklama,
                        Malzeme = ilk.Malzeme,
                        KesitEn = ilk.KesitEn,
                        KesitYukseklik = ilk.KesitYukseklik,
                        Uzunluk = ilk.Uzunluk,
                        TeorikAdet = teorikAdet,
                        KesimAdedi = (int)Math.Ceiling(teorikAdet),
                        HacimM3 = parcaGrubu.Sum(p => p.HacimM3)
                    };
                })
                .OrderBy(p => ParcaSiraNo(NormalizeMetin(p.Kod)))
                .ThenBy(p => p.Kod, StringComparer.Ordinal)
                .ToList();

            return new AmbalajUretimGrubu(
                temsilci,
                KoliNumaralariniBirlestir(grup.Select(kalem => kalem.SandikNo)),
                grup.Sum(kalem => kalem.Adet),
                grup.Sum(kalem => kalem.NetM3),
                grup.Sum(kalem => kalem.SarfM3),
                grup.Sum(kalem => kalem.ToplamM3),
                parcalar,
                grup.Where(kalem => kalem.UretimTarihi.HasValue)
                    .Select(kalem => kalem.UretimTarihi!.Value)
                    .Distinct()
                    .OrderBy(tarih => tarih)
                    .ToList());
        }

        private static AmbalajUretimGrupAnahtari GrupAnahtariOlustur(
            AmbalajUretimFormuKalemiModel kalem)
        {
            var adet = Math.Max(kalem.Adet, 1);
            var parcaImzasi = string.Join(";", kalem.Parcalar
                .OrderBy(p => p.Kod, StringComparer.Ordinal)
                .Select(p => string.Join("|",
                    NormalizeMetin(p.Kod),
                    NormalizeMetin(p.Grup),
                    NormalizeMetin(p.Aciklama),
                    NormalizeMetin(p.Malzeme),
                    Invariant(p.KesitEn),
                    Invariant(p.KesitYukseklik),
                    Invariant(p.Uzunluk),
                    Invariant(p.TeorikAdet / adet),
                    Invariant(p.HacimM3 / adet))));

            return new AmbalajUretimGrupAnahtari(
                NormalizeMetin(kalem.SandikAdi),
                NormalizeMetin(kalem.SandikTuru),
                NormalizeMetin(kalem.SandikCinsi),
                kalem.IcOlculer,
                kalem.DisOlculer,
                kalem.UstKizakAdedi,
                kalem.AyakAdedi,
                kalem.YanKusakAdedi,
                kalem.OnDuvarYuksekligi,
                NormalizeMetin(kalem.FormulVersiyonu),
                kalem.BrutKg,
                NormalizeMetin(kalem.KullanimAmaci),
                NormalizeMetin(kalem.TalimatVeren),
                NormalizeMetin(kalem.Aciklama),
                NormalizeMetin(kalem.FirinPartiNo),
                parcaImzasi);
        }

        internal static string KoliNumaralariniBirlestir(IEnumerable<string> sandikNumaralari)
        {
            ArgumentNullException.ThrowIfNull(sandikNumaralari);

            var sayilar = new SortedSet<int>();
            var digerNumaralar = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var sandikNo in sandikNumaralari)
            {
                var deger = sandikNo?.Trim() ?? string.Empty;
                var aralik = Regex.Match(deger, @"^(\d+)\s*-\s*(\d+)$", RegexOptions.CultureInvariant);
                if (aralik.Success &&
                    int.TryParse(aralik.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var baslangic) &&
                    int.TryParse(aralik.Groups[2].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var bitis) &&
                    bitis >= baslangic && bitis - baslangic <= 10_000)
                {
                    for (var sayi = baslangic; sayi <= bitis; sayi++)
                        sayilar.Add(sayi);
                    continue;
                }

                if (int.TryParse(deger, NumberStyles.None, CultureInfo.InvariantCulture, out var tekSayi))
                    sayilar.Add(tekSayi);
                else if (!string.IsNullOrWhiteSpace(deger))
                    digerNumaralar.Add(deger);
            }

            var parcalar = new List<string>();
            var siraliSayilar = sayilar.ToList();
            for (var index = 0; index < siraliSayilar.Count;)
            {
                var baslangic = siraliSayilar[index];
                var bitis = baslangic;
                while (index + 1 < siraliSayilar.Count && siraliSayilar[index + 1] == bitis + 1)
                {
                    index++;
                    bitis = siraliSayilar[index];
                }

                parcalar.Add(baslangic == bitis
                    ? baslangic.ToString(CultureInfo.InvariantCulture)
                    : $"{baslangic.ToString(CultureInfo.InvariantCulture)}-{bitis.ToString(CultureInfo.InvariantCulture)}");
                index++;
            }

            parcalar.AddRange(digerNumaralar);
            return string.Join(", ", parcalar);
        }

        private static int ParcaSiraNo(string kod) => kod switch
        {
            "AP_3" => 10,
            "AP_2" => 11,
            "AP_1" => 12,
            "OD_4" => 20,
            "OD_5" => 21,
            "OD_10" => 22,
            "UT_7" => 30,
            "UT_6" => 31,
            "UT_11" => 32,
            "YD_9" => 40,
            "YD_8" => 41,
            "YD_12" => 42,
            "YD_13" => 43,
            _ => int.MaxValue
        };

        private static int SandikTuruSiraNo(string tur) => NormalizeMetin(tur) switch
        {
            "NORMAL" => 1,
            "İLAVE" or "ILAVE" => 2,
            "SAHA" => 3,
            "YEDEK" => 4,
            "İÇ" or "IC" => 5,
            "DİĞER" or "DIGER" => 6,
            _ => int.MaxValue
        };

        private static string NormalizeMetin(string? deger) =>
            string.IsNullOrWhiteSpace(deger) ? string.Empty : deger.Trim().ToUpperInvariant();

        private static string Invariant(decimal deger) =>
            deger.ToString("G29", CultureInfo.InvariantCulture);

        private sealed record AmbalajUretimGrupAnahtari(
            string SandikAdi,
            string SandikTuru,
            string SandikCinsi,
            AmbalajOlculeri IcOlculer,
            AmbalajOlculeri DisOlculer,
            int UstKizakAdedi,
            int AyakAdedi,
            int YanKusakAdedi,
            decimal OnDuvarYuksekligi,
            string FormulVersiyonu,
            decimal? BrutKg,
            string KullanimAmaci,
            string TalimatVeren,
            string Aciklama,
            string FirinPartiNo,
            string ParcaImzasi);

        private sealed record AmbalajParcaAnahtari(
            string Kod,
            string Grup,
            string Aciklama,
            string Malzeme,
            decimal KesitEn,
            decimal KesitYukseklik,
            decimal Uzunluk);

        private sealed class DogalSandikNoKarsilastiricisi : IComparer<string>
        {
            internal static readonly DogalSandikNoKarsilastiricisi Instance = new();

            public int Compare(string? x, string? y)
            {
                if (ReferenceEquals(x, y))
                    return 0;
                if (x is null)
                    return -1;
                if (y is null)
                    return 1;

                var xSayisi = IlkSayiyiBul(x);
                var ySayisi = IlkSayiyiBul(y);
                if (xSayisi.HasValue && ySayisi.HasValue)
                {
                    var sayiKarsilastirmasi = xSayisi.Value.CompareTo(ySayisi.Value);
                    if (sayiKarsilastirmasi != 0)
                        return sayiKarsilastirmasi;
                }
                else if (xSayisi.HasValue)
                {
                    return -1;
                }
                else if (ySayisi.HasValue)
                {
                    return 1;
                }

                var metinKarsilastirmasi = StringComparer.OrdinalIgnoreCase.Compare(x, y);
                return metinKarsilastirmasi != 0
                    ? metinKarsilastirmasi
                    : StringComparer.Ordinal.Compare(x, y);
            }

            private static int? IlkSayiyiBul(string deger)
            {
                var eslesme = Regex.Match(deger, @"^\s*(\d+)", RegexOptions.CultureInvariant);
                return eslesme.Success &&
                       int.TryParse(eslesme.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var sayi)
                    ? sayi
                    : null;
            }
        }
    }

    internal sealed record AmbalajUretimGrubu(
        AmbalajUretimFormuKalemiModel Temsilci,
        string SandikNo,
        int Adet,
        decimal NetM3,
        decimal SarfM3,
        decimal ToplamM3,
        IReadOnlyList<AmbalajUretimFormuParcasiModel> Parcalar,
        IReadOnlyList<DateTime> UretimTarihleri);
}
