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

    /// <summary>
    /// Hatalı tarife düzeltmenin etkisini değiştirmeden gösterir: düzeltilecek ve korunacak kalemler, toplam net
    /// bedelin eski/yeni hâli. Ürün ya da ürüne ait aktif sürüm yoksa null döner.
    /// </summary>
    Task<FinansTarifeDuzeltmeOnizlemeModel?> TarifeDuzeltmeOnizleAsync(int urunId, int tarifeId, decimal birimFiyat, decimal kdvOrani, CancellationToken cancellationToken);

    /// <summary>
    /// Sürümün birim fiyatını/KDV'sini gerekçeyle düzeltir ve yalnız o sürümün tarife değeriyle fiyatlanmış açık
    /// kalemleri yeni değere çeker (<see cref="FinansTarifeDuzeltmeKurali"/>). Ürün ya da sürüm yoksa null döner.
    /// </summary>
    Task<FinansTarifeDuzeltmeSonucModel?> TarifeDuzeltAsync(int urunId, int tarifeId, FinansTarifeDuzeltModel model, CancellationToken cancellationToken);
}
