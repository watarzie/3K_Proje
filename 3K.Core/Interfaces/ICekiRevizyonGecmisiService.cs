using _3K.Core.Models;

namespace _3K.Core.Interfaces;

public interface ICekiRevizyonGecmisiService
{
    Task<CekiRevizyonGecmisiSayfa> ListeleAsync(int projeId, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<CekiRevizyonGecmisiDetayi?> DetayAsync(int projeId, string kaynak, int kayitId, CancellationToken cancellationToken);
    Task<CekiRevizyonDosyasi?> DosyaAsync(int projeId, string kaynak, int kayitId, CancellationToken cancellationToken);
}
