using _3K.Core.Models;

namespace _3K.Core.Exceptions;

/// <summary>
/// Revizyon dosyasının içeriğinden kaynaklanan ve kullanıcı tarafından
/// düzeltilebilecek doğrulama sorunlarını taşır.
/// </summary>
public sealed class CekiRevizyonValidationException : Exception
{
    public CekiRevizyonValidationException(
        string message,
        IReadOnlyCollection<CekiRevizyonSorunu> sorunlar)
        : base(message)
    {
        Sorunlar = sorunlar;
    }

    public CekiRevizyonValidationException(
        string message,
        IReadOnlyCollection<CekiRevizyonSorunu> sorunlar,
        Exception innerException)
        : base(message, innerException)
    {
        Sorunlar = sorunlar;
    }

    public IReadOnlyCollection<CekiRevizyonSorunu> Sorunlar { get; }
}

/// <summary>
/// Geçerli bir revizyonun mevcut proje durumuyla (sevk, transfer, stok vb.)
/// çakıştığını ve önce ilgili operasyonun geri alınması gerektiğini belirtir.
/// </summary>
public sealed class CekiRevizyonConflictException : Exception
{
    public CekiRevizyonConflictException(
        string message,
        IReadOnlyCollection<CekiRevizyonSorunu> sorunlar)
        : base(message)
    {
        Sorunlar = sorunlar;
    }

    public CekiRevizyonConflictException(
        string message,
        IReadOnlyCollection<CekiRevizyonSorunu> sorunlar,
        Exception innerException)
        : base(message, innerException)
    {
        Sorunlar = sorunlar;
    }

    public IReadOnlyCollection<CekiRevizyonSorunu> Sorunlar { get; }
}
