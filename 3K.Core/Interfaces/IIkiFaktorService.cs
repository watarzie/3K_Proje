using _3K.Core.Enums;
using _3K.Core.Models;

namespace _3K.Core.Interfaces
{
    /// <summary>
    /// TOTP kurulumu, tek kullanımlık giriş talepleri ve kurtarma kodları için
    /// altyapı bağımsız sözleşme.
    /// </summary>
    public interface IIkiFaktorService
    {
        Task<bool> AyarEtkinMiAsync(
            int kullaniciId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyDictionary<int, IkiFaktorAyarDurumu>> AyarDurumlariniGetirAsync(
            IReadOnlyCollection<int> kullaniciIdleri,
            CancellationToken cancellationToken = default);

        Task<IkiFaktorTalepSonucu> TalepOlusturAsync(
            int kullaniciId,
            IkiFaktorTalepAmaci amac,
            bool beniHatirla,
            CancellationToken cancellationToken = default);

        Task<IkiFaktorKurulumSonucu> KurulumuBaslatAsync(
            string talepTokeni,
            CancellationToken cancellationToken = default);

        Task<IkiFaktorDogrulamaSonucu> KurulumuDogrulaAsync(
            string talepTokeni,
            string kod,
            CancellationToken cancellationToken = default);

        Task<IkiFaktorDogrulamaSonucu> GirisiDogrulaAsync(
            string talepTokeni,
            string kod,
            CancellationToken cancellationToken = default);

        Task<IkiFaktorDogrulamaSonucu> KurtarmaKoduylaGirisiDogrulaAsync(
            string talepTokeni,
            string kurtarmaKodu,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Kullanıcının TOTP ayarını, kurtarma kodlarını ve açık giriş
        /// taleplerini iptal eder. Kullanıcı yoksa false döner.
        /// </summary>
        Task<bool> SifirlaAsync(
            int kullaniciId,
            CancellationToken cancellationToken = default);
    }
}
