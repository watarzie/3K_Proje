using _3K.Application.Common;

namespace _3K.Application.Features.FinansIslemleri
{
    internal static class FinansHandlerHelper
    {
        public static async Task<Result<T>> ExecuteAsync<T>(Func<Task<T>> action)
        {
            try
            {
                return Result<T>.Success(await action());
            }
            catch (UnauthorizedAccessException exception)
            {
                return Result<T>.Failure(exception.Message, 403);
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
            catch (UnauthorizedAccessException exception)
            {
                return Result.Failure(exception.Message, 403);
            }
            catch (InvalidOperationException exception)
            {
                return Result.Failure(exception.Message, 409);
            }
        }

        public static Result<T> NotFound<T>(string message) => Result<T>.Failure(message, 404);
    }
}
