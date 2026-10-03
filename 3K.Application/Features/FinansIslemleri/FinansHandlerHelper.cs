using _3K.Application.Common;

namespace _3K.Application.Features.FinansIslemleri
{
    /// <summary>
    /// Finans servis sonucunu Result'a çevirir; bulunamayan kaydı kendi mesajıyla 404 yapar.
    /// Servisin iş kuralı ihlalleri IsKuraliException olarak pipeline'da durum koduna çevrilir.
    /// </summary>
    internal static class FinansHandlerHelper
    {
        /// <summary>Servis null dönerse 404; böylece başarılı sonuç her zaman değer taşır.</summary>
        public static async Task<Result<T>> ExecuteOptionalAsync<T>(
            Func<Task<T?>> action,
            string notFoundMessage = "Kayıt bulunamadı.") where T : class
            => await action() is { } value ? Result<T>.Success(value) : Result<T>.Failure(notFoundMessage, 404);

        public static async Task<Result<T>> ExecuteAsync<T>(Func<Task<T>> action)
            => Result<T>.Success(await action());

        public static async Task<Result> ExecuteAsync(Func<Task<bool>> action, string notFoundMessage)
            => await action() ? Result.Success() : Result.Failure(notFoundMessage, 404);
    }
}
