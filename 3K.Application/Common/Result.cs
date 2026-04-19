namespace _3K.Application.Common
{
    /// <summary>
    /// Hata bilgisini taşıyan record. Message: açıklama, Code: HTTP status code.
    /// </summary>
    public record Error(string Message, int Code);

    /// <summary>
    /// Operasyon sonucunu taşıyan non-generic Result sınıfı.
    /// Exception fırlatmak yerine bu yapı kullanılır.
    /// </summary>
    public class Result
    {
        public bool IsSuccess { get; }
        public Error? Error { get; }
        public int StatusCode { get; }

        protected Result(bool isSuccess, Error? error, int statusCode = 200)
        {
            IsSuccess = isSuccess;
            Error = error;
            StatusCode = statusCode;
        }

        public static Result Success(int statusCode = 200) => new(true, null, statusCode);
        public static Result Failure(string message, int code = 400) => new(false, new Error(message, code), code);
    }

    /// <summary>
    /// Operasyon sonucunu ve veriyi taşıyan generic Result sınıfı.
    /// Başarılıysa Value dolu, başarısızsa Error dolu döner.
    /// </summary>
    public class Result<T> : Result
    {
        public T? Value { get; }

        private Result(bool isSuccess, T? value, Error? error, int statusCode = 200) : base(isSuccess, error, statusCode)
        {
            Value = value;
        }

        public static Result<T> Success(T value, int statusCode = 200) => new(true, value, null, statusCode);
        public new static Result<T> Failure(string message, int code = 400) => new(false, default, new Error(message, code), code);
    }
}
