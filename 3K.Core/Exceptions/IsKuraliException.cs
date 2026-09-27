namespace _3K.Core.Exceptions;

/// <summary>
/// Kullanıcıya gösterilebilir iş kuralı reddi. Mesaj teknik ayrıntı veya dosya yolu içermemelidir;
/// pipeline mesajı olduğu gibi döndürür. Varsayılan 400, durum çakışmalarında 409 kullanılır.
/// </summary>
public class IsKuraliException : Exception
{
    public IsKuraliException(string message, int statusCode = 400) : base(message)
    {
        StatusCode = statusCode;
    }

    public int StatusCode { get; }

    public static IsKuraliException Cakisma(string message) => new(message, 409);
}

/// <summary>İstenen kayıt bulunamadı (HTTP 404).</summary>
public sealed class KayitBulunamadiException(string message) : IsKuraliException(message, 404);
