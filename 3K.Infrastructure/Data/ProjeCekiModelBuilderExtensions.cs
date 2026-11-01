using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Models;

namespace _3K.Infrastructure.Data;

/// <summary>Proje, çeki, çeki satırı, çeki revizyonu ve projeler arası transfer eşlemeleri.</summary>
internal static class ProjeCekiModelBuilderExtensions
{
    public static void ConfigureProjeCekiModule(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Ceki>()
            .HasIndex(c => new { c.ProjeId, c.CekiTipiId });

        modelBuilder.Entity<CekiSatiri>()
            .HasIndex(cs => cs.KaynakCekiSatiriId);

        // --- Proje.DurumId → LookupProjeDurum.Id ---
        modelBuilder.Entity<Proje>()
            .HasOne(p => p.DurumLookup)
            .WithMany()
            .HasForeignKey(p => p.DurumId)
            .OnDelete(DeleteBehavior.Restrict);

        // --- Proje.ProjeTipiId → LookupProjeTipi.Id ---
        modelBuilder.Entity<Proje>()
            .HasOne(p => p.ProjeTipiLookup)
            .WithMany()
            .HasForeignKey(p => p.ProjeTipiId)
            .OnDelete(DeleteBehavior.Restrict);

        // Fotoğraf bağlantıları (20261018_01_Proje_Fotograf_Baglantilari.sql): proje silinince bağlantıları da silinir.
        modelBuilder.Entity<ProjeFotografBaglantisi>(entity =>
        {
            entity.ToTable("ProjeFotografBaglantilari");
            entity.Property(x => x.Adres).HasMaxLength(ProjeFotografBaglantisiKurali.EnFazlaUzunluk).IsRequired();
            entity.Property(x => x.Aciklama).HasMaxLength(ProjeFotografBaglantisiKurali.AciklamaEnFazlaUzunluk);
            entity.Property(x => x.Tur).HasConversion<short>().HasDefaultValue(ProjeBaglantiTuru.Fotograf);
            entity.HasOne(x => x.Proje)
                .WithMany(p => p.FotografBaglantilari)
                .HasForeignKey(x => x.ProjeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Proje listeleri en yeni kayıttan başlar.
        modelBuilder.Entity<Proje>()
            .HasIndex(p => new { p.CreatedDate, p.Id })
            .IsDescending();

        // --- CekiSatiri.DurumId → LookupUrunDurum.Id ---
        modelBuilder.Entity<CekiSatiri>()
            .HasOne(cs => cs.DurumLookup)
            .WithMany()
            .HasForeignKey(cs => cs.DurumId)
            .OnDelete(DeleteBehavior.Restrict);

        // --- CekiSatiri.GridDurumuId → LookupGridDurum.Id ---
        modelBuilder.Entity<CekiSatiri>()
            .HasOne(cs => cs.GridDurumLookup)
            .WithMany()
            .HasForeignKey(cs => cs.GridDurumuId)
            .OnDelete(DeleteBehavior.Restrict);

        // --- CekiSatiri.GridSevkDurumuId → LookupGridSevkDurum.Id ---
        modelBuilder.Entity<CekiSatiri>()
            .HasOne<LookupGridSevkDurum>()
            .WithMany()
            .HasForeignKey(cs => cs.GridSevkDurumuId)
            .OnDelete(DeleteBehavior.Restrict);

        // --- CekiSatiri.UcKDurumuId → LookupUcKDurum.Id ---
        modelBuilder.Entity<CekiSatiri>()
            .HasOne(cs => cs.UcKDurumLookup)
            .WithMany()
            .HasForeignKey(cs => cs.UcKDurumuId)
            .OnDelete(DeleteBehavior.Restrict);

        // --- CekiSatiri.GridPersonel → Kullanici ---
        modelBuilder.Entity<CekiSatiri>()
            .HasOne(cs => cs.GridPersonel)
            .WithMany()
            .HasForeignKey(cs => cs.GridPersonelId)
            .OnDelete(DeleteBehavior.SetNull);

        // --- Decimal hassasiyet konfigürasyonu (18,4) — kg, metre gibi kesirli birimler için ---
        modelBuilder.Entity<CekiSatiri>(e =>
        {
            e.Property(p => p.IstenenAdet).HasPrecision(18, 4);
            e.Property(p => p.OrijinalIstenenAdet).HasPrecision(18, 4);
            e.Property(p => p.GridGelenAdet).HasPrecision(18, 4);
            e.Property(p => p.TrafoSevkAdet).HasPrecision(18, 4);
            e.Property(p => p.GridSevkMiktari).HasPrecision(18, 4);
            e.Property(p => p.AktifGridSevkKarsilananMiktari).HasPrecision(18, 4);
            e.Property(p => p.YenidenSevkGerekliAdet).HasPrecision(18, 4);
            e.Property(p => p.GelenMiktar).HasPrecision(18, 4);
            e.Property(p => p.KarsilananMiktar).HasPrecision(18, 4);
            e.Property(p => p.StokKarsilanan).HasPrecision(18, 4);
            e.Property(p => p.ProjeKarsilanan).HasPrecision(18, 4);
            e.Property(p => p.ProjeGonderilen).HasPrecision(18, 4);
            e.Property(p => p.TedarikciKarsilanan).HasPrecision(18, 4);
            e.Property(p => p.HataliMiktar).HasPrecision(18, 4);
            e.Property(p => p.GeriGonderilenMiktar).HasPrecision(18, 4);
        });

        // --- Computed properties — DB'de kolon değil ---
        modelBuilder.Entity<CekiSatiri>()
            .Ignore(cs => cs.EksikMiktar);
        modelBuilder.Entity<CekiSatiri>()
            .Ignore(cs => cs.GridEksikMiktar);
        modelBuilder.Entity<CekiSatiri>()
            .Ignore(cs => cs.KumulatifToplam);
        modelBuilder.Entity<CekiSatiri>()
            .Ignore(cs => cs.KalanMiktar);

        // --- CekiSatiri.BirimId → LookupBirim.Id (Madde 7) ---
        modelBuilder.Entity<CekiSatiri>()
            .HasOne(cs => cs.BirimLookup)
            .WithMany()
            .HasForeignKey(cs => cs.BirimId)
            .OnDelete(DeleteBehavior.Restrict);

        // --- CekiSatiri.KaliteDurumId → LookupKaliteDurum.Id ---
        modelBuilder.Entity<CekiSatiri>()
            .HasOne(cs => cs.KaliteDurumLookup)
            .WithMany()
            .HasForeignKey(cs => cs.KaliteDurumId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        // --- CekiSatiri.SurecDurumId → LookupSurecDurum.Id ---
        modelBuilder.Entity<CekiSatiri>()
            .HasOne(cs => cs.SurecDurumLookup)
            .WithMany()
            .HasForeignKey(cs => cs.SurecDurumId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        // --- CekiSatiri.KaynakProjeId → Proje (FB kaynak takibi) ---
        modelBuilder.Entity<CekiSatiri>()
            .HasOne(cs => cs.KaynakProje)
            .WithMany()
            .HasForeignKey(cs => cs.KaynakProjeId)
            .OnDelete(DeleteBehavior.SetNull);

        // --- CekiSatiri.GeriGonderilmeSebebiId → LookupGeriGonderilmeSebebi ---
        modelBuilder.Entity<CekiSatiri>()
            .HasOne(cs => cs.GeriGonderilmeSebebiLookup)
            .WithMany()
            .HasForeignKey(cs => cs.GeriGonderilmeSebebiId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        modelBuilder.Entity<ProjeTransfer>()
            .Property(p => p.Miktar).HasPrecision(18, 4);

        modelBuilder.Entity<ProjeTransfer>()
            .Property(p => p.BarkodNo)
            .IsRequired();

        modelBuilder.Entity<ProjeTransfer>()
            .Property(p => p.UrunAdi)
            .IsRequired();

        modelBuilder.Entity<ProjeTransfer>()
            .HasIndex(p => p.DurumId);

        modelBuilder.Entity<ProjeTransfer>()
            .HasIndex(p => p.TransferTipiId);

        modelBuilder.Entity<ProjeTransfer>()
            .HasIndex(p => p.ParentTransferId);

        modelBuilder.Entity<ProjeTransfer>()
            .HasIndex(p => p.RootTransferId);

        // --- ProjeTransfer ilişkileri ---
        modelBuilder.Entity<ProjeTransfer>()
            .HasOne(pt => pt.KaynakProje)
            .WithMany()
            .HasForeignKey(pt => pt.KaynakProjeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProjeTransfer>()
            .HasOne(pt => pt.HedefProje)
            .WithMany()
            .HasForeignKey(pt => pt.HedefProjeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProjeTransfer>()
            .HasOne(pt => pt.KaynakCekiSatiri)
            .WithMany()
            .HasForeignKey(pt => pt.KaynakCekiSatiriId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProjeTransfer>()
            .HasOne(pt => pt.HedefCekiSatiri)
            .WithMany()
            .HasForeignKey(pt => pt.HedefCekiSatiriId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProjeTransfer>()
            .HasOne(pt => pt.ParentTransfer)
            .WithMany(pt => pt.ChildTransfers)
            .HasForeignKey(pt => pt.ParentTransferId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ProjeTransfer>()
            .HasOne(pt => pt.RootTransfer)
            .WithMany(pt => pt.RootChildTransfers)
            .HasForeignKey(pt => pt.RootTransferId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ProjeTransfer>()
            .HasOne(pt => pt.Kullanici)
            .WithMany()
            .HasForeignKey(pt => pt.KullaniciId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CekiRevizyonTalebi>(e =>
        {
            e.ToTable("CekiRevizyonTalepleri", table =>
            {
                table.HasCheckConstraint(
                    "CK_CekiRevizyonTalepleri_DosyaIcerigi_Dolu",
                    "octet_length(\"DosyaIcerigi\") > 0");
                table.HasCheckConstraint(
                    "CK_CekiRevizyonTalepleri_DosyaIcerigi_Boyut",
                    "octet_length(\"DosyaIcerigi\") <= 20971520");
                table.HasCheckConstraint(
                    "CK_CekiRevizyonTalepleri_DosyaIcerigi_Temizlenebilir",
                    "\"DosyaIcerigi\" IS NOT NULL OR " +
                    "(\"UygulananRevizyonCekiId\" IS NOT NULL AND \"UygulamaTarihi\" IS NOT NULL)");
                table.HasCheckConstraint(
                    "CK_CekiRevizyonTalepleri_DosyaSha256_Format",
                    "\"DosyaSha256\" ~ '^[0-9A-Fa-f]{64}$'");
                table.HasCheckConstraint(
                    "CK_CekiRevizyonTalepleri_OnizlemeJson_Dolu",
                    "jsonb_typeof(\"OnizlemeJson\") = 'object' AND \"OnizlemeJson\" <> '{}'::jsonb");
                table.HasCheckConstraint(
                    "CK_CekiRevizyonTalepleri_OnizlemeJson_Boyut",
                    "octet_length(\"OnizlemeJson\"::text) <= 10485760");
                table.HasCheckConstraint(
                    "CK_CekiRevizyonTalepleri_OnizlemeHash_Format",
                    "\"OnizlemeHash\" ~ '^[0-9A-Fa-f]{64}$'");
                table.HasCheckConstraint(
                    "CK_CekiRevizyonTalepleri_Sayilar_NonNegative",
                    "\"OnizlemeSurumu\" > 0 AND " +
                    "\"EklenenSatirSayisi\" >= 0 AND " +
                    "\"GuncellenenSatirSayisi\" >= 0 AND " +
                    "\"SilinenSatirSayisi\" >= 0");
            });

            e.Property(talep => talep.DosyaAdi)
                .HasMaxLength(255)
                .IsRequired();
            e.Property(talep => talep.DosyaIcerigi)
                .HasColumnType("bytea")
                .IsRequired(false);
            e.Property(talep => talep.DosyaSha256)
                .HasMaxLength(64)
                .IsFixedLength()
                .IsRequired();
            e.Property(talep => talep.OnizlemeJson)
                .HasColumnType("jsonb")
                .IsRequired();
            e.Property(talep => talep.OnizlemeHash)
                .HasMaxLength(64)
                .IsFixedLength()
                .IsRequired();
            e.Property(talep => talep.OnizlemeSurumu)
                .HasDefaultValue(1);
            e.Property(talep => talep.EklenenSatirSayisi)
                .HasDefaultValue(0);
            e.Property(talep => talep.GuncellenenSatirSayisi)
                .HasDefaultValue(0);
            e.Property(talep => talep.SilinenSatirSayisi)
                .HasDefaultValue(0);

            e.HasIndex(talep => new { talep.ProjeId, talep.CreatedDate })
                .IsDescending(false, true);
            e.HasIndex(talep => new { talep.TalepEdenKullaniciId, talep.CreatedDate })
                .IsDescending(false, true);
            e.HasIndex(talep => talep.UygulananRevizyonCekiId)
                .IsUnique()
                .HasFilter("\"UygulananRevizyonCekiId\" IS NOT NULL");

            e.HasOne(talep => talep.Proje)
                .WithMany()
                .HasForeignKey(talep => talep.ProjeId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(talep => talep.AnaCeki)
                .WithMany()
                .HasForeignKey(talep => talep.AnaCekiId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(talep => talep.TalepEdenKullanici)
                .WithMany()
                .HasForeignKey(talep => talep.TalepEdenKullaniciId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(talep => talep.UygulananRevizyonCeki)
                .WithMany()
                .HasForeignKey(talep => talep.UygulananRevizyonCekiId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Proje (1) - Ceki (1..*)
        modelBuilder.Entity<Proje>()
            .HasMany(p => p.Cekiler)
            .WithOne(c => c.Proje)
            .HasForeignKey(c => c.ProjeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ceki (1) - CekiSatiri (1..*)
        modelBuilder.Entity<Ceki>()
            .HasMany(c => c.CekiSatirlari)
            .WithOne(cs => cs.Ceki)
            .HasForeignKey(cs => cs.CekiId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ceki self-reference: Eksik tamamlama cekisi ana cekiye baglanabilir.
        modelBuilder.Entity<Ceki>()
            .HasOne(c => c.KaynakCeki)
            .WithMany(c => c.TamamlamaCekileri)
            .HasForeignKey(c => c.KaynakCekiId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        // CekiSatiri self-reference: Tamamlama satiri orijinal satiri takip eder.
        modelBuilder.Entity<CekiSatiri>()
            .HasOne(cs => cs.KaynakCekiSatiri)
            .WithMany(cs => cs.TamamlamaSatirlari)
            .HasForeignKey(cs => cs.KaynakCekiSatiriId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        // Proje (1) - Sandik (1..*)
        modelBuilder.Entity<Proje>()
            .HasMany(p => p.Sandiklar)
            .WithOne(s => s.Proje)
            .HasForeignKey(s => s.ProjeId)
            .OnDelete(DeleteBehavior.Restrict);

        // CekiSatiri (1) - SandikIcerik (0..*) — nullable FK (Saha/Yedek projelerde CekiSatiriId null olabilir)
        modelBuilder.Entity<CekiSatiri>()
            .HasMany(cs => cs.SandikIcerikleri)
            .WithOne(si => si.CekiSatiri)
            .HasForeignKey(si => si.CekiSatiriId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        // CekiSatiri - Paketleyen
        modelBuilder.Entity<CekiSatiri>()
            .HasOne(cs => cs.Paketleyen)
            .WithMany()
            .HasForeignKey(cs => cs.PaketleyenId)
            .OnDelete(DeleteBehavior.SetNull);

        // CekiSatiri - KontrolEden
        modelBuilder.Entity<CekiSatiri>()
            .HasOne(cs => cs.KontrolEden)
            .WithMany()
            .HasForeignKey(cs => cs.KontrolEdenId)
            .OnDelete(DeleteBehavior.SetNull);

        // CekiSatiri (1) - StokHareketi (0..*)
        modelBuilder.Entity<CekiSatiri>()
            .HasMany(cs => cs.StokHareketleri)
            .WithOne(sh => sh.CekiSatiri)
            .HasForeignKey(sh => sh.CekiSatiriId)
            .OnDelete(DeleteBehavior.Restrict);

        // Proje (1) - StokHareketi (0..*)
        modelBuilder.Entity<Proje>()
            .HasMany(p => p.StokHareketleri)
            .WithOne(sh => sh.Proje)
            .HasForeignKey(sh => sh.ProjeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
