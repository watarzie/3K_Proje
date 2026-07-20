using _3K.Core.Entities;
using _3K.Core.Models;

namespace _3K.Core.Interfaces
{
    public interface IFinansBelgeService
    {
        Task<FinansBelgeDto> YukleAsync(FinansBelgeTuru tur, int kayitId, string dosyaAdi, string icerikTuru,
            long boyut, Stream icerik, string kullanici, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<FinansBelgeDto>> ListeleAsync(FinansBelgeTuru tur, int kayitId, CancellationToken cancellationToken = default);
        Task<FinansDosyaIcerigi?> AcAsync(int belgeId, CancellationToken cancellationToken = default);
        Task<bool> SilAsync(int belgeId, CancellationToken cancellationToken = default);
    }
}