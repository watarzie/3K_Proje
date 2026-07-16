namespace _3K.Core.Exceptions
{
    /// <summary>
    /// Bir işlem ilişkili kayıtların referans bütünlüğü kuralıyla çakıştığında
    /// altyapı sağlayıcısından bağımsız olarak fırlatılır.
    /// </summary>
    public sealed class ReferentialIntegrityConflictException : Exception
    {
        public ReferentialIntegrityConflictException(string message)
            : base(message)
        {
        }

        public ReferentialIntegrityConflictException(
            string message,
            Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
