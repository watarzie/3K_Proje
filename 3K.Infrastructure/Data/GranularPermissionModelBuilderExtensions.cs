using Microsoft.EntityFrameworkCore;
using _3K.Core.Constants;
using _3K.Core.Entities;

namespace _3K.Infrastructure.Data;

public static class GranularPermissionModelBuilderExtensions
{
    public static void ConfigureGranularPermissions(this ModelBuilder builder)
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
        var seedDate = new DateTime(2026, 9, 19, 0, 0, 0, DateTimeKind.Unspecified);
        builder.Entity<MenuTanimi>().HasData(YetkiKatalogu.Tum.Select((x, i) => new MenuTanimi
        {
            Id = x.Id, Kod = x.Kod, LabelKey = x.Ad, Icon = string.Empty,
            // 5000 kodlu eski izin kaldırıldı; mevcut izinlerin canlıdaki sıra değerleri değişmemeli.
            ParentId = x.ParentId, Sira = i + 2, CreatedDate = seedDate
        }));
        // Admin ayrı bir bypass değildir; yalnız açık seed izinleri vardır. Kullanıcı reddi Admin'i de sınırlar.
        builder.Entity<RolYetki>().HasData(YetkiKatalogu.Tum.Select(x => new RolYetki
        {
            Id = 10000 + x.Id, RolId = 1, MenuTanimiId = x.Id,
            YetkiTipiId = (int)x.GerekenYetki, CreatedDate = seedDate
        }));
        builder.Entity<RolYetki>().HasData(
            new RolYetki { Id = 10046, RolId = 1, MenuTanimiId = 46, YetkiTipiId = 2, CreatedDate = seedDate },
            new RolYetki { Id = 10047, RolId = 1, MenuTanimiId = 47, YetkiTipiId = 2, CreatedDate = seedDate });
    }
}
