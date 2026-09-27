using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Models;

namespace _3K.Core.Interfaces
{
    /// <summary>
    /// Rol tanımları ve menü yetkileri. Bütün ekran/işlem/alan kodları aynı
    /// <see cref="YetkiAgaci"/> kuralıyla değerlendirilir.
    /// </summary>
    public interface IRolService
    {
        /// <summary>Tüm menü ağacını getirir (parent-child tree).</summary>
        Task<List<MenuTanimi>> GetMenuAgaciAsync(CancellationToken ct = default);

        /// <summary>Belirtilen rolün atanmış yetkilerini getirir.</summary>
        Task<List<RolYetki>> GetRolYetkileriAsync(int rolId, CancellationToken ct = default);

        /// <summary>Rolün yetkilerini toplu günceller; N olan kayıtlar saklanmaz.</summary>
        Task YetkileriGuncelleAsync(int rolId, List<RolYetki> yetkiler, CancellationToken ct = default);

        /// <summary>
        /// Kullanıcının güncel rol ve kişisel kararlarından etkin yetkisini döner. Değerlendirme
        /// istek kapsamında bir kez okunur; oturumlar arası önbellek tutulmaz.
        /// </summary>
        Task<bool> HasUserPermissionAsync(int userId, string menuKod, YetkiTipi requiredYetkiTipi, CancellationToken ct = default);
    }
}
