using _3K.Core.Entities;

namespace _3K.Core.Interfaces
{
    public interface IUrunService
    {
        Task<CekiSatiri?> GetUrunDetayAsync(int cekiSatiriId);
        Task<bool> UrunAdetGuncelleAsync(int cekiSatiriId, int konulanAdet, int eksikAdet);
        Task<bool> PaketleyenAtaAsync(int cekiSatiriId, int paketleyenId);
        Task<bool> KontrolEdenAtaAsync(int cekiSatiriId, int kontrolEdenId);
        Task<bool> AciklamaGuncelleAsync(int cekiSatiriId, string aciklama);
        Task<bool> DurumGuncelleAsync(int cekiSatiriId, string yeniDurum);
    }
}
