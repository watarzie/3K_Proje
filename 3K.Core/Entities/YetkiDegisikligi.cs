namespace _3K.Core.Entities;

/// <summary>Hedef kullanıcı/rol silinse de yetki değişikliğinin izi korunur; hedefe cascade FK yoktur.</summary>
public sealed class YetkiDegisikligi : BaseEntity
{
    public int AktorKullaniciId { get; set; }
    public string HedefTuru { get; set; } = string.Empty;
    public int HedefId { get; set; }
    public string OncekiDeger { get; set; } = string.Empty;
    public string YeniDeger { get; set; } = string.Empty;
}
