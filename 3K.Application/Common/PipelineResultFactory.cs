using System.Reflection;

namespace _3K.Application.Common;

/// <summary>
/// Pipeline adımlarının handler'ın yanıt tipini bilmeden sonuç üretmesini sağlar.
/// Result, Result&lt;T&gt; ve IsSuccess/Message taşıyan eski yanıt sınıflarını destekler;
/// desteklenmeyen tipte false döner ve karar çağıran adıma kalır.
/// </summary>
internal static class PipelineResultFactory
{
    public static bool TryCreateFailure<TResponse>(string message, int code, object? issues, out TResponse response)
    {
        var responseType = typeof(TResponse);

        if (responseType == typeof(Result))
        {
            response = (TResponse)(object)(issues == null
                ? Result.Failure(message, code)
                : Result.Failure(message, code, issues));
            return true;
        }

        if (IsGenericResult(responseType))
        {
            var parameterTypes = issues == null
                ? new[] { typeof(string), typeof(int) }
                : new[] { typeof(string), typeof(int), typeof(object) };
            var failure = responseType.GetMethod(
                nameof(Result.Failure),
                BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly,
                binder: null,
                types: parameterTypes,
                modifiers: null)!;
            var arguments = issues == null ? new object[] { message, code } : new[] { message, code, issues };
            response = (TResponse)failure.Invoke(null, arguments)!;
            return true;
        }

        // SandikKapatResult gibi IsSuccess/Message taşıyan eski yanıt sınıfları.
        var isSuccess = responseType.GetProperty("IsSuccess");
        var messageProperty = responseType.GetProperty("Message");
        if (responseType.GetConstructor(Type.EmptyTypes) != null &&
            isSuccess is { CanWrite: true } && isSuccess.PropertyType == typeof(bool) &&
            messageProperty is { CanWrite: true } && messageProperty.PropertyType == typeof(string))
        {
            var instance = Activator.CreateInstance(responseType)!;
            isSuccess.SetValue(instance, false);
            messageProperty.SetValue(instance, message);
            response = (TResponse)instance;
            return true;
        }

        response = default!;
        return false;
    }

    public static bool TryCreateSuccessWithoutValue<TResponse>(int statusCode, out TResponse response)
    {
        var responseType = typeof(TResponse);

        if (responseType == typeof(Result))
        {
            response = (TResponse)(object)Result.Success(statusCode);
            return true;
        }

        if (IsGenericResult(responseType))
        {
            var valueType = responseType.GetGenericArguments()[0];
            var success = responseType.GetMethod(nameof(Result.Success), new[] { valueType, typeof(int) })!;
            response = (TResponse)success.Invoke(null, new object?[] { null, statusCode })!;
            return true;
        }

        response = default!;
        return false;
    }

    private static bool IsGenericResult(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Result<>);
}
