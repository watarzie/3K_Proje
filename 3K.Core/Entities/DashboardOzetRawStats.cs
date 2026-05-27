namespace _3K.Core.Entities
{
    /// <summary>
    /// Dashboard özet ham verileri.
    /// IDashboardStatsProvider tarafından SQL aggregate sorgularıyla doldurulur.
    /// </summary>
    public class DashboardOzetRawStats
    {
        // Proje durum sayıları
        public int ToplamProje { get; set; }
        public int HazirlananProje { get; set; }
        public int BeklemedeProje { get; set; }
        public int TamamlananProje { get; set; }
        public int SevkEdilenProje { get; set; }
        public int EksikSevkEdilenProje { get; set; }

        // Sandık sayıları
        public int ToplamSandik { get; set; }
        public int NormalSandik { get; set; }
        public int SahaSandik { get; set; }
        public int YedekSandik { get; set; }

        // Eksik ürün
        public int EksikUrunSayisi { get; set; }

        // Depo lokasyon bazlı
        public int ToplamDepoSandik { get; set; }
        public int DepoUcKSandik { get; set; }
        public int DepoSeymenSandik { get; set; }
        public int DepoGridSandik { get; set; }
        public int DepoDigerSandik { get; set; }
        public List<DashboardDepoDagilimRawStats> DepoDagilimlari { get; set; } = new();
        public List<DashboardDepoDagilimRawStats> NormalDepoDagilimlari { get; set; } = new();
        public List<DashboardDepoDagilimRawStats> SahaDepoDagilimlari { get; set; } = new();
        public List<DashboardDepoDagilimRawStats> YedekDepoDagilimlari { get; set; } = new();

        // Saha/Yedek yüzdeleri
        public int SahaYuzde { get; set; }
        public int YedekYuzde { get; set; }
        public List<DashboardProjeTipiOzetRawStats> ProjeTipiOzetleri { get; set; } = new();
    }

    public class DashboardDepoDagilimRawStats
    {
        public int DepoLokasyonId { get; set; }
        public string DepoLokasyonMetni { get; set; } = string.Empty;
        public int SandikSayisi { get; set; }
    }

    public class DashboardProjeTipiOzetRawStats
    {
        public int ProjeTipiId { get; set; }
        public string ProjeTipiMetni { get; set; } = string.Empty;
        public int ToplamProje { get; set; }
        public int HazirlananProje { get; set; }
        public int SevkEdilenProje { get; set; }
        public int EksikSevkEdilenProje { get; set; }
        public int TamamlananProje { get; set; }
        public int ToplamSandik { get; set; }
        public int EksikUrunSayisi { get; set; }
        public int ToplamDepoSandik { get; set; }
        public int TamamlanmaYuzdesi { get; set; }
        public List<DashboardDepoDagilimRawStats> DepoDagilimlari { get; set; } = new();
    }
}
