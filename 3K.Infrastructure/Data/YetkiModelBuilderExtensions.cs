using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;

namespace _3K.Infrastructure.Data;

/// <summary>Kişisel yetki kararları ve yetki değişikliği denetim kayıtlarının eşlemesi.</summary>
public static class YetkiModelBuilderExtensions
{
    public static void ConfigureYetkiKayitlari(this ModelBuilder builder)
    {
        builder.Entity<KullaniciYetki>(entity =>
        {
            entity.ToTable("KullaniciYetkileri");
            entity.HasIndex(x => new { x.KullaniciId, x.MenuTanimiId }).IsUnique();
            entity.HasOne(x => x.Kullanici).WithMany().HasForeignKey(x => x.KullaniciId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.MenuTanimi).WithMany().HasForeignKey(x => x.MenuTanimiId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<YetkiDegisikligi>(entity =>
        {
            entity.ToTable("YetkiDegisiklikleri");
            entity.Property(x => x.HedefTuru).HasMaxLength(30);
            entity.HasIndex(x => new { x.HedefTuru, x.HedefId, x.CreatedDate });
        });
    }
}
