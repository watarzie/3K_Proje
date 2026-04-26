namespace _3K.Core.Entities
{
    /// <summary>
    /// Tüm lookup entity'leri için temel sınıf.
    /// Her grup kendi tablosunu alır, FK ilişkileri Id (PK) üzerinden kurulur.
    /// </summary>
    public abstract class LookupBase : BaseEntity
    {
        public int Anahtar { get; set; }
        public string Deger { get; set; } = string.Empty;
    }

    public class LookupProjeDurum : LookupBase { }
    public class LookupSandikDurum : LookupBase { }
    public class LookupSandikTipi : LookupBase { }
    public class LookupDepoLokasyon : LookupBase { }
    public class LookupUrunDurum : LookupBase { }
    public class LookupGridDurum : LookupBase { }
    public class LookupGridSevkDurum : LookupBase { }
    public class LookupUcKDurum : LookupBase { }
    public class LookupYetkiTipi : LookupBase { }
    public class LookupStokDurum : LookupBase { }
    public class LookupIslemTipi : LookupBase { }
    public class LookupGeriGonderilmeSebebi : LookupBase { }
    public class LookupProjeTipi : LookupBase { }
    public class LookupBirim : LookupBase { }
    public class LookupNotYazanTaraf : LookupBase { }
}
