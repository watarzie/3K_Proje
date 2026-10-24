using _3K.Core.Models;

namespace _3K.Core.Interfaces
{
    /// <summary>
    /// Sandık açılış/kapanış olaylarının okunması: tek sandığın zaman çizelgesi ve süzgece göre geniş istatistik
    /// (sandık, proje, müşteri, kişi ve sebep kırılımları) ile Excel çıktısı. Yalnız okur.
    /// </summary>
    public interface ISandikDurumIstatistikService
    {
        Task<IReadOnlyList<SandikDurumOlayModeli>> SandikGecmisiAsync(int sandikId, CancellationToken cancellationToken);

        Task<SandikDurumIstatistigi> IstatistikAsync(SandikDurumIstatistikKriteri kriter, CancellationToken cancellationToken);

        /// <summary>Proje bazında sayılar; geri açılması çok olan önce, sayfalı.</summary>
        Task<SayfaliSonuc<SandikDurumProjeSatiri>> ProjelerAsync(
            SandikDurumIstatistikKriteri kriter, int? sayfa, int? boyut, CancellationToken cancellationToken);

        /// <summary>Müşteri bazında sayılar (müşteri adı kırpılarak gruplanır), sayfalı.</summary>
        Task<SayfaliSonuc<SandikDurumMusteriSatiri>> MusterilerAsync(
            SandikDurumIstatistikKriteri kriter, int? sayfa, int? boyut, CancellationToken cancellationToken);

        /// <summary>Kişi bazında kapatma/geri açma sayıları, sayfalı.</summary>
        Task<SayfaliSonuc<SandikDurumKisiSatiri>> KisilerAsync(
            SandikDurumIstatistikKriteri kriter, int? sayfa, int? boyut, CancellationToken cancellationToken);

        /// <summary>Süzgeçteki olaylar en yeniden eskiye, sayfalı.</summary>
        Task<SayfaliSonuc<SandikDurumOlayModeli>> OlaylarAsync(
            SandikDurumIstatistikKriteri kriter, int? sayfa, int? boyut, CancellationToken cancellationToken);

        /// <summary>İstatistik ve süzgeçteki bütün olaylar tek Excel dosyasında.</summary>
        Task<byte[]> ExcelOlusturAsync(SandikDurumIstatistikKriteri kriter, CancellationToken cancellationToken);
    }
}
