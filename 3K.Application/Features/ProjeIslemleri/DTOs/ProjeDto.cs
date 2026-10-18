using _3K.Core.Entities;

namespace _3K.Application.Features.ProjeIslemleri.DTOs
{
    public class ProjeDto
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
        public DateTime? PlanlananSevkTarihi { get; set; }
        public DateTime? GerceklesenSevkTarihi { get; set; }
        public bool FizikselSevkEdilmisSandikVarMi { get; set; }
        public string SorumluKisi { get; set; } = string.Empty;
        public int SandikSayisi { get; set; }
        public int HazirSandikSayisi { get; set; }
        public int DepoSandikSayisi { get; set; }
        public int DepoUcKSandikSayisi { get; set; }
        public int DepoSeymenSandikSayisi { get; set; }
        public int DepoGridSandikSayisi { get; set; }
        public List<ProjeDepoDagilimDto> DepoDagilimlari { get; set; } = new();
        public int ToplamUrunSayisi { get; set; }
        public int TamamlananUrunSayisi { get; set; }

        // Teknik bilgiler
        public string? FBNo { get; set; }
        public string? Guc { get; set; }
        public string? Gerilim { get; set; }
        public string? Lokasyon { get; set; }
        public string? OlcuResmiNo { get; set; }
        public string? NakilOlcuResmiNo { get; set; }
        public string? SonMontajResmiNo { get; set; }
        public string? ProjeMuduru { get; set; }

        /// <summary>Fotoğraf bağlantıları (eklenme sırasıyla); yalnız proje-fotograf-baglantisi R izniyle dolu döner.</summary>
        public List<ProjeFotografBaglantisiDto> FotografBaglantilari { get; set; } = [];
    }

    /// <summary>Projenin bir fotoğraf bağlantısı; ekleme/düzenleme/silme sonucu projenin güncel listesidir.</summary>
    public class ProjeFotografBaglantisiDto
    {
        public int Id { get; init; }
        public string Adres { get; init; } = string.Empty;
        public string? Aciklama { get; init; }

        /// <summary>Bağlantılar eklenme sırasıyla (kimlik artan).</summary>
        public static List<ProjeFotografBaglantisiDto> Liste(IEnumerable<ProjeFotografBaglantisi> baglantilar) =>
            baglantilar.OrderBy(x => x.Id)
                .Select(x => new ProjeFotografBaglantisiDto { Id = x.Id, Adres = x.Adres, Aciklama = x.Aciklama })
                .ToList();
    }

    public class ProjeDepoDagilimDto
    {
        public int DepoLokasyonId { get; set; }
        public string DepoLokasyonMetni { get; set; } = string.Empty;
        public int SandikSayisi { get; set; }
    }
}
