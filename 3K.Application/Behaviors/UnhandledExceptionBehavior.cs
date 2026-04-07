using MediatR;
using Microsoft.Extensions.Logging;
using _3K.Application.Common;

namespace _3K.Application.Behaviors
{
    /// <summary>
    /// MediatR Pipeline Behavior: Beklenmedik hataları merkezi olarak yakalar.
    /// İş kuralları zaten Result dönecek; bu behavior sadece sistem seviyesi Exception'ları yakalar,
    /// loglar ve kullanıcıya detay sızdırmadan güvenli bir Result.Failure döner.
    /// </summary>
    public class UnhandledExceptionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly ILogger<UnhandledExceptionBehavior<TRequest, TResponse>> _logger;

        public UnhandledExceptionBehavior(ILogger<UnhandledExceptionBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            try
            {
                return await next();
            }
            catch (Exception ex)
            {
                var requestName = typeof(TRequest).Name;
                var correlationId = Guid.NewGuid().ToString("N")[..8]; // 8 karakterlik kısa takip ID

                _logger.LogError(ex,
                    "[{CorrelationId}] Beklenmeyen hata oluştu. İstek: {RequestName}, Tip: {ExceptionType}, Detay: {Message}",
                    correlationId, requestName, ex.GetType().Name, ex.Message);

                return CreateFailureResult($"Beklenmeyen bir hata oluştu. Takip kodu: {correlationId}", 500);
            }
        }

        /// <summary>
        /// TResponse tipinden uygun Failure Result'ı üretir.
        /// </summary>
        private static TResponse CreateFailureResult(string message, int code)
        {
            var responseType = typeof(TResponse);

            // TResponse doğrudan Result ise
            if (responseType == typeof(Result))
            {
                return (TResponse)(object)Result.Failure(message, code);
            }

            // TResponse generic Result<T> ise
            if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
            {
                var failureMethod = responseType.GetMethod("Failure", new[] { typeof(string), typeof(int) });
                if (failureMethod != null)
                {
                    return (TResponse)failureMethod.Invoke(null, new object[] { message, code })!;
                }
            }

            // Result pattern kullanmayan eski tipte komutlar — burada yeniden fırlatmak zorundayız
            throw new InvalidOperationException($"Beklenmeyen bir hata oluştu: {message}");
        }
    }
}
