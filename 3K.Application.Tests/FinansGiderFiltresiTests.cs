using _3K.Core.Entities;
using _3K.Core.Models;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Services.Finans;
using Microsoft.EntityFrameworkCore;

namespace _3K.Application.Tests;

/// <summary>Gider listesi ve rapor verisi tek gider filtresinden geçer; filtre ikinci kez uygulanmaz.</summary>
public sealed class FinansGiderFiltresiTests
{
    [Fact]
    public void Arama_belge_numarasini_da_tarar_para_birimi_ve_proje_normallesir()
    {
        using var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=model_validation;Username=test;Password=test").Options);

        var sql = FinansSorgulari.ExpenseFilter(
                context.Set<FinansGider>(),
                new FinansListeFiltre(Arama: "BLG-7", ParaBirimi: " eur ", ProjeNo: " P-1 "))
            .ToQueryString();

        Assert.Contains("\"BelgeNo\" ILIKE", sql);
        Assert.DoesNotContain("lower(", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("'EUR'", sql);
        Assert.Contains("'P-1'", sql);
    }
}
