using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;

namespace _3K.Infrastructure.Data;

/// <summary>Kullanıcı hesabı ve iki faktörlü doğrulama tablolarının eşlemesi.</summary>
internal static class KimlikModelBuilderExtensions
{
    public static void ConfigureKimlikModule(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Kullanici>()
            .HasIndex(k => k.Email)
            .IsUnique();

        modelBuilder.Entity<Kullanici>()
            .Property(k => k.IkiFaktorZorunluMu)
            .HasDefaultValue(false);

        // Hesap bazlı giriş kilidi (GirisKilidiKurali); sayaç veritabanında atomik artırılır.
        modelBuilder.Entity<Kullanici>(e =>
        {
            e.ToTable(table => table.HasCheckConstraint(
                "CK_Kullanicilar_BasarisizGirisSayisi",
                "\"BasarisizGirisSayisi\" >= 0"));
            e.Property(k => k.BasarisizGirisSayisi)
                .HasDefaultValue(0);
        });

        // İki faktörlü doğrulama tabloları BaseEntity'den türemez. Güvenlik
        // kayıtlarının yalnız ihtiyaç duyduğu alanlar ve UTC zamanları tutulur.
        modelBuilder.Entity<KullaniciIkiFaktorAyari>(e =>
        {
            e.ToTable("KullaniciIkiFaktorAyarlari");
            e.HasKey(x => x.KullaniciId);

            e.Property(x => x.SifreliGizliAnahtar)
                .HasMaxLength(2048)
                .IsRequired();
            e.Property(x => x.EtkinMi)
                .HasDefaultValue(false);
            e.Property(x => x.DogrulandiTarihiUtc)
                .HasColumnType("timestamp with time zone");

            e.HasOne(x => x.Kullanici)
                .WithOne()
                .HasForeignKey<KullaniciIkiFaktorAyari>(x => x.KullaniciId)
                .HasConstraintName("FK_Kullanici2FA_Kullanicilar")
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<IkiFaktorGirisTalebi>(e =>
        {
            e.ToTable("IkiFaktorGirisTalepleri", table =>
            {
                table.HasCheckConstraint(
                    "CK_IkiFaktorGirisTalepleri_Amac",
                    "\"Amac\" IN (1, 2)");
                table.HasCheckConstraint(
                    "CK_IkiFaktorGirisTalepleri_BasarisizDenemeSayisi",
                    "\"BasarisizDenemeSayisi\" >= 0");
            });

            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.TokenHash)
                .HasMaxLength(128)
                .IsRequired();
            e.Property(x => x.Amac)
                .HasConversion<short>()
                .HasColumnType("smallint");
            e.Property(x => x.SonKullanmaTarihiUtc)
                .HasColumnType("timestamp with time zone");
            e.Property(x => x.TuketildiTarihiUtc)
                .HasColumnType("timestamp with time zone");
            e.Property(x => x.BasarisizDenemeSayisi)
                .HasDefaultValue(0);
            e.Property(x => x.BeniHatirla)
                .HasDefaultValue(false);

            e.HasIndex(x => x.TokenHash).IsUnique();
            e.HasIndex(x => new { x.KullaniciId, x.SonKullanmaTarihiUtc });

            e.HasOne(x => x.Kullanici)
                .WithMany()
                .HasForeignKey(x => x.KullaniciId)
                .HasConstraintName("FK_IkiFaktorTalep_Kullanicilar")
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<IkiFaktorKurtarmaKodu>(e =>
        {
            e.ToTable("IkiFaktorKurtarmaKodlari");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.KodHash)
                .HasMaxLength(255)
                .IsRequired();
            e.Property(x => x.KullanildiTarihiUtc)
                .HasColumnType("timestamp with time zone");

            e.HasIndex(x => new { x.KullaniciId, x.KodHash }).IsUnique();
            e.HasIndex(x => new { x.KullaniciId, x.KullanildiTarihiUtc });

            e.HasOne(x => x.Kullanici)
                .WithMany()
                .HasForeignKey(x => x.KullaniciId)
                .HasConstraintName("FK_IkiFaktorKurtarma_Kullanicilar")
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- Kullanici.RolId → Rol ---
        modelBuilder.Entity<Kullanici>()
            .HasOne(k => k.Rol)
            .WithMany(r => r.Kullanicilar)
            .HasForeignKey(k => k.RolId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
