using _3K.Core.Models;

namespace _3K.Core.Interfaces;

/// <summary>Kullanıcıya rol atanmadan önceki yetki denetimi (kayıt ve kullanıcı güncelleme akışları).</summary>
public interface IRolAtamaService
{
    Task<RolAtamaSonucu> RolAtamayiDogrulaAsync(int? hedefKullaniciId, int rolId,
        CancellationToken cancellationToken = default);
}
