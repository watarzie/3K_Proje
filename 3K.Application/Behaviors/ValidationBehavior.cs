using FluentValidation;
using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Behaviors
{
    /// <summary>
    /// MediatR Pipeline Behavior: FluentValidation ile gelen verileri doğrular.
    /// Handler'a düşmeden önce boş alan, negatif değer vb. kontrolleri yapar.
    /// Validation hatası varsa Exception fırlatmaz; Result.Failure döner.
    /// </summary>
    public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (!_validators.Any())
                return await next();

            var context = new ValidationContext<TRequest>(request);

            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            var failures = validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .ToList();

            if (failures.Count == 0)
                return await next();

            // Hata mesajlarını birleştir
            var errorMessages = string.Join(" | ", failures.Select(f => f.ErrorMessage));

            return CreateFailureResult(errorMessages, 400);
        }

        private static TResponse CreateFailureResult(string message, int code)
        {
            var responseType = typeof(TResponse);

            if (responseType == typeof(Result))
                return (TResponse)(object)Result.Failure(message, code);

            if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
            {
                var failureMethod = responseType.GetMethod("Failure", new[] { typeof(string), typeof(int) });
                if (failureMethod != null)
                    return (TResponse)failureMethod.Invoke(null, new object[] { message, code })!;
            }

            // Result pattern kullanmayan tipler için fallback
            throw new ValidationException(message);
        }
    }
}
