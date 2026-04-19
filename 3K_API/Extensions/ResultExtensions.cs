using Microsoft.AspNetCore.Mvc;
using _3K.Application.Common;

namespace _3K_API.Extensions
{
    /// <summary>
    /// Controller'larda Result → ActionResult dönüşümünü sağlayan extension metodlar.
    /// try-catch ve [Authorize] attribute yerine bu pattern kullanılır.
    /// </summary>
    public static class ResultExtensions
    {
        /// <summary>
        /// Non-generic Result için HTTP yanıtı üretir.
        /// </summary>
        public static ActionResult ToActionResult(this Result result)
        {
            if (result.IsSuccess)
            {
                if (result.StatusCode == 202) return new AcceptedResult("", new { message = "İşleminiz yetkili onayına sunulmuştur.", statusCode = 202 });
                return new OkObjectResult(new { message = "İşlem başarılı." });
            }

            return result.Error!.Code switch
            {
                401 => new UnauthorizedObjectResult(new { message = result.Error.Message }),
                403 => new ObjectResult(new { message = result.Error.Message }) { StatusCode = 403 },
                404 => new NotFoundObjectResult(new { message = result.Error.Message }),
                _ => new BadRequestObjectResult(new { message = result.Error.Message })
            };
        }

        /// <summary>
        /// Generic Result&lt;T&gt; için HTTP yanıtı üretir. Başarılıysa Value döner.
        /// </summary>
        public static ActionResult ToActionResult<T>(this Result<T> result)
        {
            if (result.IsSuccess)
            {
                if (result.StatusCode == 202) return new AcceptedResult("", new { message = "İşleminiz yetkili onayına sunulmuştur.", statusCode = 202, value = result.Value });
                return new OkObjectResult(result.Value);
            }

            return result.Error!.Code switch
            {
                401 => new UnauthorizedObjectResult(new { message = result.Error.Message }),
                403 => new ObjectResult(new { message = result.Error.Message }) { StatusCode = 403 },
                404 => new NotFoundObjectResult(new { message = result.Error.Message }),
                _ => new BadRequestObjectResult(new { message = result.Error.Message })
            };
        }
    }
}
