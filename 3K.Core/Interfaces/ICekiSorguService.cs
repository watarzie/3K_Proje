using _3K.Core.Entities;

namespace _3K.Core.Interfaces;

/// <summary>Çeki satırlarının okunması.</summary>
public interface ICekiSorguService
{
    Task<IEnumerable<CekiSatiri>> GetCekiSatirlariAsync(int cekiId);
}
