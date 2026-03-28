using _3K.Core.Entities;
using _3K.Core.Enums;

namespace _3K.Core.Interfaces
{
    /// <summary>
    /// İş akışı 3, 7, 8: Ürün operasyonları
    /// UML Sequence Diagram: UrunService
    /// </summary>
    public interface IUrunService
    {
        Task<CekiSatiri?> GetUrunDetayAsync(int cekiSatiriId);
        Task<bool> UrunAdetGuncelleAsync(int cekiSatiriId, int konulanAdet, int eksikAdet);
        Task<bool> PaketleyenAtaAsync(int cekiSatiriId, int paketleyenId);
        Task<bool> KontrolEdenAtaAsync(int cekiSatiriId, int kontrolEdenId);
        Task<bool> AciklamaGuncelleAsync(int cekiSatiriId, string aciklama);
        Task<bool> DurumGuncelleAsync(int cekiSatiriId, UrunDurum yeniDurum);
    }
}
