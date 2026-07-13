namespace _3K.Core.Exceptions
{
    /// <summary>
    /// Kalıcı verinin istemci tarafından okunduktan sonra başka bir işlemce değiştirildiğini belirtir.
    /// </summary>
    public sealed class ConcurrencyConflictException : Exception
    {
        public ConcurrencyConflictException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
