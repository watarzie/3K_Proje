using _3K.Core.Entities;
using System.Threading.Tasks;

namespace _3K.Core.Interfaces
{
    public interface IProjectLockService
    {
        Task CheckLockAsync(int projeId);
        Task CheckLockByCekiSatiriAsync(int cekiSatiriId);
        Task CheckLockBySandikAsync(int sandikId);
        Task CheckLockByCekiAsync(int cekiId);
    }
}
