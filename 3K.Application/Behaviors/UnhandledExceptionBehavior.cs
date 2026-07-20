using MediatR;
using Microsoft.Extensions.Logging;
using _3K.Application.Common;
using _3K.Core.Exceptions;

namespace _3K.Application.Behaviors
{
    /// <summary>
    /// MediatR Pipeline Behavior: Merkezi exception → Result dönüşümünü sağlar.
    /// Bilinen iş kuralı hatalarını uygun HTTP kodu ve yapılandırılmış detaylarla döndürür;
    /// beklenmeyen hataları loglayıp kullanıcıya teknik ayrıntı sızdırmaz.
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
            catch (CekiRevizyonValidationException ex)
            {
                return CreateFailureResult(ex.Message, 400, ex.Sorunlar);
            }
            catch (CekiRevizyonConflictException ex)
            {
                return CreateFailureResult(ex.Message, 409, ex.Sorunlar);
            }
            catch (ConcurrencyConflictException ex)
            {
                return CreateFailureResult(ex.Message, 409);
            }
            catch (UniqueConstraintViolationException ex)
            {
                return CreateFailureResult(ex.Message, 409);
            }
            catch (ReferentialIntegrityConflictException ex)
            {
                return CreateFailureResult(ex.Message, 409);
            }
            catch (ProjectLockedException ex)
            {
                // İş kuralı hatası olarak yakala ve 400 ile dön
                return CreateFailureResult(ex.Message, 400);
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
        private static TResponse CreateFailureResult(string message, int code, object? issues = null)
        {
            var responseType = typeof(TResponse);

            // TResponse doğrudan Result ise
            if (responseType == typeof(Result))
            {
                return (TResponse)(object)(issues == null
                    ? Result.Failure(message, code)
                    : Result.Failure(message, code, issues));
            }

            // TResponse generic Result<T> ise
            if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
            {
                var parameterTypes = issues == null
                    ? new[] { typeof(string), typeof(int) }
                    : new[] { typeof(string), typeof(int), typeof(object) };
                var failureMethod = responseType.GetMethod(
                    "Failure",
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Static |
                    System.Reflection.BindingFlags.DeclaredOnly,
                    binder: null,
                    types: parameterTypes,
                    modifiers: null);
                if (failureMethod != null)
                {
                    var arguments = issues == null
                        ? new object[] { message, code }
                        : new[] { (object)message, code, issues };
                    return (TResponse)failureMethod.Invoke(null, arguments)!;
                }
            }

            // TResponse için IsSuccess ve Message property'leri olan sınıfları destekle (SandikKapatResult gibi)
            var isSuccessProp = responseType.GetProperty("IsSuccess");
            var messageProp = responseType.GetProperty("Message");

            if (isSuccessProp != null && messageProp != null && responseType.GetConstructor(Type.EmptyTypes) != null)
            {
                var instance = Activator.CreateInstance(responseType);
                isSuccessProp.SetValue(instance, false);
                messageProp.SetValue(instance, message);
                return (TResponse)instance!;
            }

            // Result pattern kullanmayan eski tipte komutlar — burada yeniden fırlatmak zorundayız
            throw new InvalidOperationException($"Beklenmeyen bir hata oluştu: {message}");
        }
    }
}
