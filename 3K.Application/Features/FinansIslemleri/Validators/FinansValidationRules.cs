

namespace _3K.Application.Features.FinansIslemleri.Validators;

internal static class FinansValidationRules
{
    public static bool Currency(string? value) => !string.IsNullOrWhiteSpace(value) && value.Trim().Length == 3;
    public static bool CompleteDocumentTotals(string? currency, decimal? net, decimal? vat, decimal? total)
    {
        var any = !string.IsNullOrWhiteSpace(currency) || net.HasValue || vat.HasValue || total.HasValue;
        return !any || (!string.IsNullOrWhiteSpace(currency) && net.HasValue && vat.HasValue && total.HasValue);
    }
    public static bool BalancedDocumentTotals(decimal? net, decimal? vat, decimal? total)
        => !net.HasValue || !vat.HasValue || !total.HasValue || Math.Abs(net.Value + vat.Value - total.Value) <= 0.02m;
}
