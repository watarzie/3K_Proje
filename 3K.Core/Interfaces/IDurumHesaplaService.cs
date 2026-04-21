namespace _3K.Core.Interfaces
{
    /// <summary>
    /// GridDurumu + UcKDurumu kombinasyonundan genel Durum (UrunDurum) otomatik hesaplar.
    /// ID bazlı çalışır.
    /// </summary>
    public interface IDurumHesaplaService
    {
        int HesaplaGenelDurum(int gridDurumuId, int ucKDurumuId);
    }
}
