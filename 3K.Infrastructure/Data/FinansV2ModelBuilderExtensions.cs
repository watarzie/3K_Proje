using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;

namespace _3K.Infrastructure.Data;

public static class FinansV2ModelBuilderExtensions
{
    public static void ConfigureFinansV2Module(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FinansIsKaydi>(entity =>
        {
            entity.Property(x => x.FinansTarihi).HasColumnType("timestamp without time zone");
            entity.Property(x => x.KaynakBileseni).HasMaxLength(20).HasDefaultValue("NET");
            entity.Property(x => x.ManuelNetTutar).HasPrecision(18, 2);
            entity.Property(x => x.SandikCinsi).HasConversion<int?>();
            entity.Property(x => x.AlanDegerleriJson).HasColumnType("jsonb");
            entity.Property(x => x.FiyatBilesenleriJson).HasColumnType("jsonb");
            entity.HasIndex(x => new { x.KaynakTuru, x.KaynakKayitId, x.KaynakBileseni }).IsUnique()
                .HasDatabaseName("IX_FinansIsKayitlari_KaynakBileseniV2")
                .HasFilter("\"KaynakKayitId\" IS NOT NULL");
            entity.HasOne<FinansIsSablonSurumu>().WithMany().HasForeignKey(x => x.SablonSurumId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<FinansSiparis>().Property(x => x.ParaBirimi).HasMaxLength(3);
        modelBuilder.Entity<FinansGider>(entity =>
        {
            entity.Property(x => x.FinansTarihi).HasColumnType("timestamp without time zone");
            entity.Property(x => x.BelgeNo).HasMaxLength(100);
            entity.HasOne<FinansGider>().WithMany().HasForeignKey(x => x.MahsupEdilenAvansId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<FinansDegisiklikGecmisi>().Property(x => x.Referans).HasMaxLength(500);
        modelBuilder.Entity<FinansKaynakBastirma>(entity =>
        {
            entity.ToTable("FinansKaynakBastirmalari");
            entity.Property(x => x.KaynakTuru).HasMaxLength(50);
            entity.Property(x => x.KaynakKayitId).HasMaxLength(150);
            entity.Property(x => x.KaynakBileseni).HasMaxLength(20);
            entity.Property(x => x.Aciklama).HasMaxLength(1000);
            entity.HasIndex(x => new { x.KaynakTuru, x.KaynakKayitId, x.KaynakBileseni }).IsUnique();
        });
        modelBuilder.Entity<FinansBelge>(entity =>
        {
            entity.ToTable("FinansBelgeleri");
            entity.HasIndex(x => new { x.HedefTuru, x.HedefId, x.Surum }).IsUnique();
            entity.Property(x => x.HedefTuru).HasMaxLength(30);
            entity.Property(x => x.OrijinalAd).HasMaxLength(250);
            entity.Property(x => x.GuvenliAd).HasMaxLength(80);
            entity.Property(x => x.IcerikTuru).HasMaxLength(80);
            entity.Property(x => x.Hash).HasMaxLength(64);
            entity.Property(x => x.Yukleyen).HasMaxLength(100);
        });
        modelBuilder.Entity<FinansIsSablonu>(entity =>
        {
            entity.ToTable("FinansIsSablonlari");
            entity.HasIndex(x => x.Kod).IsUnique();
            entity.Property(x => x.Kod).HasMaxLength(80);
            entity.Property(x => x.Ad).HasMaxLength(200);
        });
        modelBuilder.Entity<FinansIsSablonSurumu>(entity =>
        {
            entity.ToTable("FinansIsSablonSurumleri");
            entity.HasIndex(x => new { x.FinansIsSablonuId, x.Surum }).IsUnique();
            entity.Property(x => x.AlanlarJson).HasColumnType("jsonb");
            entity.HasOne(x => x.Sablon).WithMany(x => x.Surumler).HasForeignKey(x => x.FinansIsSablonuId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
