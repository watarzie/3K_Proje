namespace _3K.Core.Exceptions;

/// <summary>
/// Çeki dosyasının içeriği veya formatı geçersiz olduğunda fırlatılır.
/// </summary>
public sealed class CekiImportValidationException : Exception
{
    public CekiImportValidationException(string message)
        : base(message)
    {
    }

    public CekiImportValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Çeki yüklemesi mevcut proje/çeki durumu ile çakıştığında fırlatılır.
/// </summary>
public sealed class CekiImportConflictException : Exception
{
    public CekiImportConflictException(string message)
        : base(message)
    {
    }
}
