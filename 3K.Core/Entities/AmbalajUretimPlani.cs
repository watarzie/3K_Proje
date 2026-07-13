namespace _3K.Core.Entities
{
    public class AmbalajUretimPlani : BaseEntity
    {
        public int ProjeId { get; set; }
        public string? FirinPartiNo { get; set; }
        public string? IlaveFirinPartiNo { get; set; }
        public string? IcSandikFirinPartiNo { get; set; }
        public int ProjeSandiklariDurumId { get; set; } = 1;
        public int IlaveSandiklarDurumId { get; set; } = 1;
        public int IcSandiklarDurumId { get; set; } = 1;

        public virtual Proje Proje { get; set; } = null!;
        public virtual ICollection<AmbalajUretimKalemi> Kalemler { get; set; } = new List<AmbalajUretimKalemi>();
    }

    public class AmbalajUretimKalemi : BaseEntity
    {
        public int AmbalajUretimPlaniId { get; set; }
        public int? KaynakSandikId { get; set; }
        public int? UstKalemId { get; set; }
        public int Tur { get; set; }
        public bool UretimeAlindi { get; set; } = true;
        public string SandikNo { get; set; } = string.Empty;
        public string? Ad { get; set; }
        public string SandikTipi { get; set; } = "Ahşap Kapalı";
        public int Adet { get; set; } = 1;
        public decimal Boy { get; set; }
        public decimal En { get; set; }
        public decimal Yukseklik { get; set; }
        public string? KullanimAmaci { get; set; }
        public string? TalimatVeren { get; set; }
        public string? Aciklama { get; set; }

        public virtual AmbalajUretimPlani AmbalajUretimPlani { get; set; } = null!;
        public virtual Sandik? KaynakSandik { get; set; }
        public virtual AmbalajUretimKalemi? UstKalem { get; set; }
        public virtual ICollection<AmbalajUretimKalemi> IcSandiklar { get; set; } = new List<AmbalajUretimKalemi>();
    }

    public class AmbalajIcSandikSablonu : BaseEntity
    {
        public string Ad { get; set; } = string.Empty;
        public string SandikTipi { get; set; } = "Ahşap Kapalı";
        public decimal Boy { get; set; }
        public decimal En { get; set; }
        public decimal Yukseklik { get; set; }
    }

    public class AmbalajBagimsizSandik : BaseEntity
    {
        public int Tur { get; set; }
        public int DurumId { get; set; } = 1;
        public bool UretimeAlindi { get; set; } = true;
        public string? FirinPartiNo { get; set; }
        public string SandikNo { get; set; } = string.Empty;
        public string Ad { get; set; } = string.Empty;
        public string SandikTipi { get; set; } = "Ahşap Kapalı";
        public int Adet { get; set; } = 1;
        public decimal Boy { get; set; }
        public decimal En { get; set; }
        public decimal Yukseklik { get; set; }
        public string? KullanimAmaci { get; set; }
        public string? TalimatVeren { get; set; }
        public string? Aciklama { get; set; }
    }
}