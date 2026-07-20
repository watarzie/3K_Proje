using _3K.Core.Models;

namespace _3K.Core.Interfaces
{
    public interface IFinansDonemService
    {
        Task<FinansDonemOlusturSonuc> OlusturAsync(DateTime referansTarihi, CancellationToken cancellationToken = default);
    }
}