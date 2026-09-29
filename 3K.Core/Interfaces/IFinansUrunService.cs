using _3K.Core.Models;

namespace _3K.Core.Interfaces;

/// <summary>Finans ürünleri: fiyat tarifeleri ve üretim kayıtlarıyla eşleşmeleri.</summary>
public interface IFinansUrunService
{
    Task<FinansSayfaliSonuc<FinansUrunModel>> UrunlerSayfaliAsync(bool sadeceAktif, DateTime? tarifeTarihi, string? arama, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<FinansUrunModel> UrunOlusturAsync(FinansUrunKaydetModel model, CancellationToken cancellationToken);
    Task<FinansUrunModel?> UrunGuncelleAsync(int id, FinansUrunKaydetModel model, CancellationToken cancellationToken);
    Task<bool> UrunPasiflestirAsync(int id, CancellationToken cancellationToken);
    /// <summary>
    /// D4: ürüne bağlı, PO'ya hiç bağlanmamış açık kalemleri finans tarihlerinde geçerli tarife sürümüyle yeniden
    /// fiyatlar. Ürün yoksa null döner.
    /// </summary>
    Task<FinansYenidenFiyatlamaSonucModel?> YenidenFiyatlaAsync(int id, string aciklama, CancellationToken cancellationToken);
}
