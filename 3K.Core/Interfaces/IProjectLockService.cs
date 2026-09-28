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
