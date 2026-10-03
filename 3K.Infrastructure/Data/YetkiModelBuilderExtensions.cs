using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;

namespace _3K.Infrastructure.Data;

/// <summary>Yetki değişikliği denetim kayıtlarının eşlemesi.</summary>
public static class YetkiModelBuilderExtensions
{
    public static void ConfigureYetkiKayitlari(this ModelBuilder builder)
    {
        builder.Entity<YetkiDegisikligi>(entity =>
        {
            entity.ToTable("YetkiDegisiklikleri");
            entity.Property(x => x.HedefTuru).HasMaxLength(30);
            entity.HasIndex(x => new { x.HedefTuru, x.HedefId, x.CreatedDate });
        });
    }
}
