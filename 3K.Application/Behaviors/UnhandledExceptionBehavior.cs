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
            catch (ResultRollbackException ex) when (ex.Result.Error is { } error)
            {
                // İç içe atomik bir adım başarısız oldu; bütün transaction geri alındı.
                return Failure(error.Message, error.Code, error.Issues);
            }
            catch (CekiRevizyonValidationException ex)
            {
                return Failure(ex.Message, 400, ex.Sorunlar);
            }
            catch (CekiRevizyonConflictException ex)
            {
                return Failure(ex.Message, 409, ex.Sorunlar);
            }
            catch (Exception ex) when (ex is ConcurrencyConflictException
                                           or UniqueConstraintViolationException
                                           or ReferentialIntegrityConflictException)
            {
                return Failure(ex.Message, 409);
            }
            catch (ProjectLockedException ex)
            {
                return Failure(ex.Message, 400);
            }
            catch (IsKuraliException ex)
            {
                return Failure(ex.Message, ex.StatusCode);
            }
            catch (Exception ex)
            {
                var correlationId = Guid.NewGuid().ToString("N")[..8];

                _logger.LogError(ex,
                    "[{CorrelationId}] Beklenmeyen hata oluştu. İstek: {RequestName}, Tip: {ExceptionType}, Detay: {Message}",
                    correlationId, typeof(TRequest).Name, ex.GetType().Name, ex.Message);

                return Failure($"Beklenmeyen bir hata oluştu. Takip kodu: {correlationId}", 500);
            }
        }

        private static TResponse Failure(string message, int code, object? issues = null) =>
            PipelineResultFactory.TryCreateFailure<TResponse>(message, code, issues, out var failure)
                ? failure
                : throw new InvalidOperationException($"Beklenmeyen bir hata oluştu: {message}");
    }
}
