namespace _3K.Core.Entities;

/// <summary>Kayıt yoksa rol devralınır. false açık ret, true yalnız hedef kod için ek izindir.</summary>
public sealed class KullaniciYetki : BaseEntity
{
    public int KullaniciId { get; set; }
    public int MenuTanimiId { get; set; }
    public bool IzinVerildi { get; set; }
    public Kullanici Kullanici { get; set; } = null!;
    public MenuTanimi MenuTanimi { get; set; } = null!;
}
