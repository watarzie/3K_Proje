namespace _3K.Application.Features.DashboardIslemleri.DTOs
{
    public class DashboardOzetDto
    {
        public int ToplamProje { get; set; }
        public int HazirlananProje { get; set; }
        public int BeklemedeProje { get; set; }
        public int TamamlananProje { get; set; }
        public int SevkEdilenProje { get; set; }
        public int EksikSevkEdilenProje { get; set; }
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
        public List<DashboardSandikDurumDto> SandikDurumOzetleri { get; set; } = new();
        public int SahaYuzde { get; set; }
        public int YedekYuzde { get; set; }
        public List<DashboardProjeTipiOzetDto> ProjeTipiOzetleri { get; set; } = new();
    }

    public class DashboardDepoDagilimDto
    {
        public int DepoLokasyonId { get; set; }
        public string DepoLokasyonMetni { get; set; } = string.Empty;
        public int SandikSayisi { get; set; }
    }

    public class DashboardProjeTipiOzetDto
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
        public List<DashboardDepoDagilimDto> DepoDagilimlari { get; set; } = new();
        public List<DashboardSandikDurumDto> SandikDurumOzetleri { get; set; } = new();
    }

    public class DashboardSandikDurumDto
    {
        public int DurumId { get; set; }
        public string DurumMetni { get; set; } = string.Empty;
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
        public List<DashboardSandikDurumDto> SandikDurumOzetleri { get; set; } = new();
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

    public class DashboardSahayaAktarilanSandikDto
    {
        public int SahaAktarimId { get; set; }
        public int KaynakProjeId { get; set; }
        public string KaynakProjeNo { get; set; } = string.Empty;
        public int KaynakSandikId { get; set; }
        public string KaynakSandikNo { get; set; } = string.Empty;
        public int SahaProjeId { get; set; }
        public string SahaProjeNo { get; set; } = string.Empty;
        public int SahaSandikId { get; set; }
        public string SahaSandikNo { get; set; } = string.Empty;
        public int SandikDurumId { get; set; }
        public string SandikDurumMetni { get; set; } = string.Empty;
        public int ToplamUrunSayisi { get; set; }
        public decimal ToplamMiktar { get; set; }
        public DateTime AktarimTarihi { get; set; }
        public DateTime? SevkTarihi { get; set; }
        public List<DashboardSahaAktarimDurumDto> AktarimDurumlari { get; set; } = new();
    }

    public class DashboardSahaAktarimDurumDto
    {
        public int DurumId { get; set; }
        public string DurumMetni { get; set; } = string.Empty;
        public int UrunSayisi { get; set; }
    }

    public class DashboardProjeFilterOptionDto
    {
        public int Id { get; set; }
        public string ProjeNo { get; set; } = string.Empty;
        public string Musteri { get; set; } = string.Empty;
        public int ProjeTipiId { get; set; }
    }

    public class DashboardProjeSandikDurumDto
    {
        public int ProjeId { get; set; }
        public string ProjeNo { get; set; } = string.Empty;
        public string Musteri { get; set; } = string.Empty;
        public int ProjeTipiId { get; set; }
        public int ToplamSandik { get; set; }
        public List<DashboardSandikDurumDto> SandikDurumOzetleri { get; set; } = new();
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
