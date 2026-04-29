using _3K.Core.Entities;

namespace _3K.Core.Interfaces
{
    /// <summary>
    /// GridDurumu + UcKDurumu kombinasyonundan genel Durum (UrunDurum) otomatik hesaplar.
    /// ID bazlı çalışır.
    /// </summary>
    public interface IDurumHesaplaService
    {
        int HesaplaGenelDurum(int gridDurumuId, int ucKDurumuId);

        /// <summary>
        /// Merkezi kalan miktar hesaplaması ve GenelDurum override'ı.
        /// KalanMiktar <= 0 ise DurumId = Tamamlandi yapılır (HataliMiktar hariç).
        /// DRY: Tüm command handler'lardan çağrılacak tek merkezi nokta.
        /// </summary>
        void HesaplaKalanVeDurum(CekiSatiri satir);
    }
}
