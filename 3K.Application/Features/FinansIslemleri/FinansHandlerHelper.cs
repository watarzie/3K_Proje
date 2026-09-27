using _3K.Application.Common;

namespace _3K.Application.Features.FinansIslemleri
{
    /// <summary>
    /// Finans servisinin iş kuralı ihlallerini (InvalidOperationException) 409 sonucuna,
    /// bulunamayan kaydı 404 sonucuna çevirir; bütün finans handler'ları bu yolu kullanır.
    /// </summary>
    internal static class FinansHandlerHelper
    {
        /// <summary>Servis null dönerse 404; böylece başarılı sonuç her zaman değer taşır.</summary>
        public static async Task<Result<T>> ExecuteOptionalAsync<T>(
            Func<Task<T?>> action,
            string notFoundMessage = "Kayıt bulunamadı.") where T : class
        {
            var result = await ExecuteAsync(action);
            if (!result.IsSuccess) return Result<T>.Failure(result.Error!.Message, result.StatusCode);
            return result.Value is { } value ? Result<T>.Success(value) : Result<T>.Failure(notFoundMessage, 404);
        }

        public static async Task<Result<T>> ExecuteAsync<T>(Func<Task<T>> action)
        {
            try
            {
                return Result<T>.Success(await action());
            }
            catch (InvalidOperationException exception)
            {
                return Result<T>.Failure(exception.Message, 409);
            }
        }

        public static async Task<Result> ExecuteAsync(Func<Task<bool>> action, string notFoundMessage)
        {
            try
            {
                return await action()
                    ? Result.Success()
                    : Result.Failure(notFoundMessage, 404);
            }
            catch (InvalidOperationException exception)
            {
                return Result.Failure(exception.Message, 409);
            }
        }
    }
}
