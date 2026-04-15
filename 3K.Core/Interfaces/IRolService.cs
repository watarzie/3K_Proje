using _3K.Core.Entities;

namespace _3K.Core.Interfaces
{
    /// <summary>
    /// Rol ve yetki yönetim servisi.
    /// Menü ağacı sorgulama ve yetki kaydetme işlemlerini sağlar.
    /// </summary>
    public interface IRolService
    {
        /// <summary>Tüm menü ağacını getirir (parent-child tree).</summary>
        Task<List<MenuTanimi>> GetMenuAgaciAsync(CancellationToken ct = default);

        /// <summary>Belirtilen rolün yetkilerini getirir.</summary>
        Task<List<RolYetki>> GetRolYetkileriAsync(int rolId, CancellationToken ct = default);

        /// <summary>Rolün yetkilerini toplu günceller (upsert).</summary>
        Task YetkileriGuncelleAsync(int rolId, List<RolYetki> yetkiler, CancellationToken ct = default);
    }
}
