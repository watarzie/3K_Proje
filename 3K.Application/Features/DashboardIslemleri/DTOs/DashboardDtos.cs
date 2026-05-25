namespace _3K.Application.Features.DashboardIslemleri.DTOs
{
    public class DashboardOzetDto
    {
        public int ToplamProje { get; set; }
        public int HazirlananProje { get; set; }
        public int BeklemedeProje { get; set; }
        public int TamamlananProje { get; set; }
        public int SevkEdilenProje { get; set; }
        public int ToplamSandik { get; set; }
        public int EksikUrunSayisi { get; set; }
        public int ToplamDepoSandik { get; set; }
        public int DepoUcKSandik { get; set; }
        public int DepoSeymenSandik { get; set; }
        public int DepoGridSandik { get; set; }
        public int DepoDigerSandik { get; set; }
        public List<DashboardDepoDagilimDto> DepoDagilimlari { get; set; } = new();
        public List<DashboardDepoDagilimDto> NormalDepoDagilimlari { get; set; } = new();
        public List<DashboardDepoDagilimDto> SahaDepoDagilimlari { get; set; } = new();
        public List<DashboardDepoDagilimDto> YedekDepoDagilimlari { get; set; } = new();
        public int NormalSandik { get; set; }
        public int SahaSandik { get; set; }
        public int YedekSandik { get; set; }
        public int SahaYuzde { get; set; }
        public int YedekYuzde { get; set; }
    }

    public class DashboardDepoDagilimDto
    {
        public int DepoLokasyonId { get; set; }
        public string DepoLokasyonMetni { get; set; } = string.Empty;
        public int SandikSayisi { get; set; }
    }

    public class DashboardProjeItemDto
    {
        public int Id { get; set; }
        public string ProjeNo { get; set; } = string.Empty;
        public string Musteri { get; set; } = string.Empty;
        public int DurumId { get; set; }
        public string DurumMetni { get; set; } = string.Empty;
        public int ProjeTipiId { get; set; }
        public string ProjeTipiMetni { get; set; } = string.Empty;
        public DateTime BaslamaTarihi { get; set; }
        public int CalismaGunSayisi { get; set; }
        public DateTime? GerceklesenSevkTarihi { get; set; }
        public string? Lokasyon { get; set; }
        public int SandikSayisi { get; set; }
        public int ToplamUrunSayisi { get; set; }
        public int TamamlananUrunSayisi { get; set; }
        public int TamamlanmaYuzdesi { get; set; }
    }

    public class DashboardKritikProjeDto
    {
        public string ProjeNo { get; set; } = string.Empty;
        public int Eksik { get; set; }
        public int Toplam { get; set; }
        public int Sandik { get; set; }
    }

    public class DashboardEksikSiralamaDto
    {
        public string ProjeNo { get; set; } = string.Empty;
        public string? Lokasyon { get; set; }
        public int EksikYuzde { get; set; }
        public int EksikAdet { get; set; }
    }

    public class DashboardPagedResultDto<T>
    {
        public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public bool HasMore { get; set; }
    }
}
