using System.Globalization;

namespace _3K.Core.Models
{
    /// <summary>Sevk sonrası düzeltmenin açılış ya da tamamlanma anındaki sandık bilgisi ve içeriği.</summary>
    public sealed record SandikDuzeltmeGoruntusu
    {
        public string SandikNo { get; init; } = string.Empty;
        public string? Ad { get; init; }
        public int TipId { get; init; }
        public decimal? En { get; init; }
        public decimal? Boy { get; init; }
        public decimal? Yukseklik { get; init; }
        public decimal? NetKg { get; init; }
        public decimal? GrossKg { get; init; }
        public IReadOnlyList<SandikDuzeltmeKalemi> Kalemler { get; init; } = [];
    }

    /// <summary>Görüntüdeki bir sandık içeriği; çeki satırı varsa satırın Grid/3K durumu da tutulur.</summary>
    public sealed record SandikDuzeltmeKalemi
    {
        public int? SandikIcerikId { get; init; }
        public int? CekiSatiriId { get; init; }
        public string BarkodNo { get; init; } = string.Empty;
        public string Urun { get; init; } = string.Empty;
        public decimal Tahsis { get; init; }
        public decimal Konulan { get; init; }
        public int? GridDurumuId { get; init; }
        public int? UcKDurumuId { get; init; }
        public string? UrunBilgisi { get; init; }
    }

    /// <summary>
    /// Sevk sonrası düzeltmenin "önce → sonra" farkı. Saf kuraldır: açılış görüntüsüyle güncel görüntüyü karşılaştırır,
    /// kullanıcıya gösterilecek Türkçe satırlar üretir. Ürün çeki satırıyla (yoksa sandık içeriğiyle) eşlenir; başka
    /// sandığa taşınan ürün bu sandıkta "çıkarıldı" görünür.
    /// </summary>
    public static class SandikDuzeltmeFarkKurali
    {
        public const string DegisiklikYok = "Değişiklik yok.";

        public static IReadOnlyList<string> Hesapla(
            SandikDuzeltmeGoruntusu once,
            SandikDuzeltmeGoruntusu sonra,
            Func<int, string> gridEtiketi,
            Func<int, string> uckEtiketi,
            Func<int, string> tipEtiketi)
        {
            var satirlar = new List<string>();

            if (!string.Equals(once.Ad ?? string.Empty, sonra.Ad ?? string.Empty, StringComparison.Ordinal))
                satirlar.Add($"Sandık adı: {Metin(once.Ad)} → {Metin(sonra.Ad)}");
            if (once.TipId != sonra.TipId)
                satirlar.Add($"Sandık cinsi: {tipEtiketi(once.TipId)} → {tipEtiketi(sonra.TipId)}");
            if (once.En != sonra.En || once.Boy != sonra.Boy || once.Yukseklik != sonra.Yukseklik)
                satirlar.Add($"Ölçü: {Olcu(once)} → {Olcu(sonra)}");
            if (once.NetKg != sonra.NetKg)
                satirlar.Add($"Net kg: {Sayi(once.NetKg)} → {Sayi(sonra.NetKg)}");
            if (once.GrossKg != sonra.GrossKg)
                satirlar.Add($"Brüt kg: {Sayi(once.GrossKg)} → {Sayi(sonra.GrossKg)}");

            var onceki = once.Kalemler.GroupBy(Anahtar).ToDictionary(g => g.Key, Birlestir);
            var sonraki = sonra.Kalemler.GroupBy(Anahtar).ToDictionary(g => g.Key, Birlestir);

            foreach (var (_, kalem) in sonraki.Where(x => !onceki.ContainsKey(x.Key)).OrderBy(x => x.Key, StringComparer.Ordinal))
                satirlar.Add($"Eklendi: {Ad(kalem)} — {Sayi(kalem.Konulan)} adet");

            foreach (var (_, kalem) in onceki.Where(x => !sonraki.ContainsKey(x.Key)).OrderBy(x => x.Key, StringComparer.Ordinal))
                satirlar.Add($"Çıkarıldı: {Ad(kalem)} — {Sayi(kalem.Konulan)} adet");

            foreach (var (anahtar, eski) in onceki.Where(x => sonraki.ContainsKey(x.Key)).OrderBy(x => x.Key, StringComparer.Ordinal))
            {
                var yeni = sonraki[anahtar];
                var degisimler = new List<string>();
                if (eski.Konulan != yeni.Konulan)
                    degisimler.Add($"konulan {Sayi(eski.Konulan)} → {Sayi(yeni.Konulan)}");
                if (eski.Tahsis != yeni.Tahsis)
                    degisimler.Add($"sandık miktarı {Sayi(eski.Tahsis)} → {Sayi(yeni.Tahsis)}");
                if (eski.GridDurumuId != yeni.GridDurumuId)
                    degisimler.Add($"Grid {Etiket(eski.GridDurumuId, gridEtiketi)} → {Etiket(yeni.GridDurumuId, gridEtiketi)}");
                if (eski.UcKDurumuId != yeni.UcKDurumuId)
                    degisimler.Add($"3K {Etiket(eski.UcKDurumuId, uckEtiketi)} → {Etiket(yeni.UcKDurumuId, uckEtiketi)}");
                if (!string.Equals(eski.UrunBilgisi ?? string.Empty, yeni.UrunBilgisi ?? string.Empty, StringComparison.Ordinal))
                    degisimler.Add("ürün bilgisi güncellendi");
                if (!string.Equals(eski.Urun, yeni.Urun, StringComparison.Ordinal) ||
                    !string.Equals(eski.BarkodNo, yeni.BarkodNo, StringComparison.Ordinal))
                    degisimler.Add("ürün açıklaması/barkodu güncellendi");
                if (degisimler.Count > 0)
                    satirlar.Add($"{Ad(yeni)}: {string.Join(", ", degisimler)}");
            }

            return satirlar.Count == 0 ? [DegisiklikYok] : satirlar;
        }

        private static string Anahtar(SandikDuzeltmeKalemi kalem) => kalem.CekiSatiriId is int satirId
            ? "S" + satirId.ToString(CultureInfo.InvariantCulture)
            : "I" + (kalem.SandikIcerikId ?? 0).ToString(CultureInfo.InvariantCulture);

        // Aynı satır sandıkta birden çok içerikle bölünmüşse tek ürün olarak toplanır.
        private static SandikDuzeltmeKalemi Birlestir(IEnumerable<SandikDuzeltmeKalemi> kalemler)
        {
            var liste = kalemler.ToList();
            return liste[0] with
            {
                Tahsis = liste.Sum(k => k.Tahsis),
                Konulan = liste.Sum(k => k.Konulan),
                UrunBilgisi = string.Join(" | ", liste.Select(k => k.UrunBilgisi).Where(x => !string.IsNullOrWhiteSpace(x)))
            };
        }

        private static string Ad(SandikDuzeltmeKalemi kalem) => string.IsNullOrWhiteSpace(kalem.BarkodNo)
            ? Metin(kalem.Urun)
            : $"{Metin(kalem.Urun)} ({kalem.BarkodNo})";

        private static string Etiket(int? id, Func<int, string> etiket) => id is int deger ? etiket(deger) : "—";
        private static string Metin(string? deger) => string.IsNullOrWhiteSpace(deger) ? "—" : deger.Trim();
        private static string Olcu(SandikDuzeltmeGoruntusu g) => $"{Sayi(g.En)}×{Sayi(g.Boy)}×{Sayi(g.Yukseklik)}";
        private static string Sayi(decimal? deger) => deger is decimal d
            ? d.ToString("0.####", CultureInfo.GetCultureInfo("tr-TR"))
            : "—";
    }
}
