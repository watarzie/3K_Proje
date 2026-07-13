namespace _3K.Core.Exceptions
{
    /// <summary>
    /// Veritabanındaki benzersizlik kuralı ihlalini sağlayıcıdan bağımsız biçimde uygulamaya taşır.
    /// </summary>
    public sealed class UniqueConstraintViolationException : Exception
    {
        public UniqueConstraintViolationException(
            string? constraintName,
            string message,
            Exception innerException)
            : base(message, innerException)
        {
            ConstraintName = constraintName;
        }

        public string? ConstraintName { get; }
    }
}
