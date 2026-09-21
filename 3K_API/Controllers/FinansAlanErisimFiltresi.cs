using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K_API.Controllers;

/// <summary>HTTP sınırında bütün eski/yeni finans DTO'larına aynı açık alan projeksiyonu uygulanır.</summary>
public sealed class FinansAlanErisimFiltresi(IAlanErisimService erisim) : IAsyncActionFilter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var permissions = await erisim.GetAsync(context.HttpContext.RequestAborted);
        var path = context.HttpContext.Request.Path.Value ?? "";
        var file = path.Contains("/raporlar/", StringComparison.OrdinalIgnoreCase) && !path.EndsWith("/veri", StringComparison.OrdinalIgnoreCase) ||
                   path.Contains("/belgeler/", StringComparison.OrdinalIgnoreCase) && path.EndsWith("/indir", StringComparison.OrdinalIgnoreCase);
        if (file && !(permissions.ParasalVeri && permissions.BirimFiyat && permissions.Tutar && permissions.Gelir && permissions.Gider && permissions.Karlilik && permissions.Olcu && permissions.UretimM3 && permissions.Sarf))
        {
            context.Result = new ObjectResult(new { message = "Bu belge bütün mali ve ölçü alanlarını içerir; gerekli alan görüntüleme izinleri bulunmuyor." }) { StatusCode = 403 };
            return;
        }
        var executed = await next();
        if (executed.Result is ObjectResult { Value: not null } result && result.StatusCode is null or >= 200 and < 300)
        {
            var node = JsonSerializer.SerializeToNode(result.Value, result.Value.GetType(), JsonOptions);
            FinansAlanProjeksiyonu.Uygula(node, permissions, path.Contains("/denetim", StringComparison.OrdinalIgnoreCase));
            result.Value = node;
            result.DeclaredType = typeof(JsonNode);
        }
    }
}
