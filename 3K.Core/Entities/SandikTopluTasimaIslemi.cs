namespace _3K.Core.Entities;

/// <summary>
/// Bir toplu taşımanın değişmez istek özeti. Satır hareketleri SandikUrunTransferleri'ndedir.
/// Silinen içeriklerden sonra da ağ tekrarını doğrulayabilmek için nesne ID'leri snapshot'tır.
/// </summary>
public sealed class SandikTopluTasimaIslemi : BaseEntity
{
    public Guid IslemAnahtari { get; set; }
    public string IstekHash { get; set; } = string.Empty;
    public int KullaniciId { get; set; }
    public int ProjeId { get; set; }
    public int KaynakSandikId { get; set; }
    public int HedefSandikId { get; set; }
    public int SatirSayisi { get; set; }
}
