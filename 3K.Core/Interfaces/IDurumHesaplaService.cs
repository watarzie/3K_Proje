namespace _3K.Core.Interfaces
{
    /// <summary>
    /// GridDurumu + UcKDurumu kombinasyonundan genel Durum (UrunDurum) otomatik hesaplar.
    /// </summary>
    public interface IDurumHesaplaService
    {
        string HesaplaGenelDurum(string gridDurumu, string ucKDurumu);
    }
}
