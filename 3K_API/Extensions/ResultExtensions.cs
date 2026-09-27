using Microsoft.AspNetCore.Mvc;
using _3K.Application.Common;
using _3K.Core.Models;

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

            var errorBody = CreateErrorBody(result.Error!);
            return result.Error!.Code switch
            {
                401 => new UnauthorizedObjectResult(errorBody),
                403 => new ObjectResult(errorBody) { StatusCode = 403 },
                404 => new NotFoundObjectResult(errorBody),
                409 => new ConflictObjectResult(errorBody),
                429 => new ObjectResult(errorBody) { StatusCode = 429 },
                500 => new ObjectResult(errorBody) { StatusCode = 500 },
                _ => new BadRequestObjectResult(errorBody)
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

            var errorBody = CreateErrorBody(result.Error!);
            return result.Error!.Code switch
            {
                401 => new UnauthorizedObjectResult(errorBody),
                403 => new ObjectResult(errorBody) { StatusCode = 403 },
                404 => new NotFoundObjectResult(errorBody),
                409 => new ConflictObjectResult(errorBody),
                429 => new ObjectResult(errorBody) { StatusCode = 429 },
                500 => new ObjectResult(errorBody) { StatusCode = 500 },
                _ => new BadRequestObjectResult(errorBody)
            };
        }

        /// <summary>
        /// Başarılı dosya sonucunu indirme yanıtına, hatayı standart hata gövdesine çevirir.
        /// Başarılı ama boş sonuç (ör. silinmiş belge) 404 döner.
        /// </summary>
        public static ActionResult ToFileResult(this Result<DosyaIcerigi> result)
        {
            if (!result.IsSuccess)
                return result.ToActionResult();

            return result.Value is { } dosya
                ? new FileContentResult(dosya.Icerik, dosya.IcerikTuru) { FileDownloadName = dosya.DosyaAdi }
                : new NotFoundObjectResult(new { message = "Dosya bulunamadı." });
        }

        private static object CreateErrorBody(Error error)
        {
            return error.Issues == null
                ? new { message = error.Message }
                : new { message = error.Message, issues = error.Issues };
        }
    }
}
