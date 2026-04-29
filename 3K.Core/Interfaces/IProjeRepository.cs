using _3K.Core.Entities;

namespace _3K.Core.Interfaces
{
    /// <summary>
    /// Proje'ye özel repository — ilişkili verileri (Sandıklar, Cekiler→CekiSatırları)
    /// önceden yüklenmiş şekilde döndürür.
    /// EF Core Include detayları Infrastructure'da kalır.
    /// </summary>
    public interface IProjeRepository
    {
        Task<IEnumerable<Proje>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Dropdown'lar için hafif proje listesi — Include yok, sadece Proje tablosu.
        /// </summary>
        Task<IEnumerable<Proje>> GetAllLightAsync(CancellationToken cancellationToken = default);
    }
}
