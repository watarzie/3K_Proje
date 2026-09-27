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
            var context = new ValidationContext<TRequest>(request);
            var failures = new List<FluentValidation.Results.ValidationFailure>();

            // Validator'lar aynı scoped servisleri paylaşabilir; sıralı çalıştırılır.
            foreach (var validator in _validators)
            {
                var result = await validator.ValidateAsync(context, cancellationToken);
                failures.AddRange(result.Errors.Where(f => f != null));
            }

            if (failures.Count == 0)
                return await next();

            var errorMessages = string.Join(" | ", failures.Select(f => f.ErrorMessage));
            return PipelineResultFactory.TryCreateFailure<TResponse>(errorMessages, 400, null, out var failure)
                ? failure
                : throw new ValidationException(errorMessages);
        }
    }
}
