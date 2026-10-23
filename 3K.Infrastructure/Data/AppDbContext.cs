using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;

namespace _3K.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // ======= Ana Tablo DbSet'leri =======
        public DbSet<Proje> Projeler { get; set; } = null!;
        public DbSet<Ceki> Cekiler { get; set; } = null!;
        public DbSet<CekiSatiri> CekiSatirlari { get; set; } = null!;
        public DbSet<Sandik> Sandiklar { get; set; } = null!;
        public DbSet<SandikIcerik> SandikIcerikleri { get; set; } = null!;
        public DbSet<SandikUrunTransferi> SandikUrunTransferleri { get; set; } = null!;
        public DbSet<SandikTopluTasimaIslemi> SandikTopluTasimaIslemleri { get; set; } = null!;
        public DbSet<Kullanici> Kullanicilar { get; set; } = null!;
        public DbSet<IslemOnayKurali> IslemOnayKurallari { get; set; } = null!;
        public DbSet<StokKaydi> StokKayitlari { get; set; } = null!;
        public DbSet<StokHareketi> StokHareketleri { get; set; } = null!;
        public DbSet<HareketGecmisi> HareketGecmisleri { get; set; } = null!;
        public DbSet<HareketGecmisiArsiv> HareketGecmisleriArsiv { get; set; } = null!;
        public DbSet<ProjeTransfer> ProjeTransferleri { get; set; } = null!;
        public DbSet<SahaAktarim> SahaAktarimlari { get; set; } = null!;
        public DbSet<SahaAktarimKalemi> SahaAktarimKalemleri { get; set; } = null!;
        public DbSet<SahaTasimaKalemi> SahaTasimaKalemleri { get; set; } = null!;
        public DbSet<Sevkiyat> Sevkiyatlar { get; set; } = null!;
        public DbSet<SevkiyatSandik> SevkiyatSandiklari { get; set; } = null!;
        public DbSet<SandikDuzeltmeKaydi> SandikDuzeltmeKayitlari { get; set; } = null!;
        public DbSet<SevkSeferi> SevkSeferleri { get; set; } = null!;
        public DbSet<OnayBekleyenIslem> OnayBekleyenIslemler { get; set; } = null!;
        public DbSet<OnayIslemYetki> OnayIslemYetkileri { get; set; } = null!;
        public DbSet<OnayOperasyonKurali> OnayOperasyonKurallari { get; set; } = null!;
        public DbSet<CekiRevizyonTalebi> CekiRevizyonTalepleri { get; set; } = null!;
        public DbSet<Bildirim> Bildirimler { get; set; } = null!;
        public DbSet<KullaniciBildirimi> KullaniciBildirimleri { get; set; } = null!;
        public DbSet<BildirimAboneligi> BildirimAbonelikleri { get; set; } = null!;

        // ======= Ambalaj / Üretim Modülü DbSet'leri =======
        public DbSet<AmbalajUretimKaydi> AmbalajUretimKayitlari { get; set; } = null!;
        public DbSet<AmbalajUretimHareketi> AmbalajUretimHareketleri { get; set; } = null!;
        public DbSet<AmbalajUretimFormuSurumu> AmbalajUretimFormuSurumleri { get; set; } = null!;
        public DbSet<AmbalajUretimFormuKaydi> AmbalajUretimFormuKayitlari { get; set; } = null!;
        public DbSet<AmbalajUretimGerceklesmesi> AmbalajUretimGerceklesmeleri { get; set; } = null!;
        public DbSet<AmbalajIcSandikSablonu> AmbalajIcSandikSablonlari { get; set; } = null!;
        public DbSet<AmbalajKaynakSenkronizasyonKuyrukKaydi> AmbalajKaynakSenkronizasyonKuyrugu { get; set; } = null!;

        // ======= Finans Yönetimi Modülü DbSet'leri =======
        public DbSet<FinansUrun> FinansUrunleri { get; set; } = null!;
        public DbSet<FinansUrunEslesmesi> FinansUrunEslesmeleri { get; set; } = null!;
        public DbSet<FinansFiyatTarifesi> FinansFiyatTarifeleri { get; set; } = null!;
        public DbSet<FinansIsKaydi> FinansIsKayitlari { get; set; } = null!;
        public DbSet<FinansSiparis> FinansSiparisleri { get; set; } = null!;
        public DbSet<FinansSiparisKalemi> FinansSiparisKalemleri { get; set; } = null!;
        public DbSet<FinansFatura> FinansFaturalari { get; set; } = null!;
        public DbSet<FinansFaturaKalemi> FinansFaturaKalemleri { get; set; } = null!;
        public DbSet<FinansDuzenliIs> FinansDuzenliIsleri { get; set; } = null!;
        public DbSet<FinansGiderKategori> FinansGiderKategorileri { get; set; } = null!;
        public DbSet<FinansGiderKalemi> FinansGiderKalemleri { get; set; } = null!;
        public DbSet<FinansGider> FinansGiderleri { get; set; } = null!;
        public DbSet<FinansDegisiklikGecmisi> FinansDegisiklikGecmisleri { get; set; } = null!;

        // ======= RBAC (Rol Tabanlı Erişim Kontrolü) DbSet'leri =======
        public DbSet<Rol> Roller { get; set; } = null!;
        public DbSet<MenuTanimi> MenuTanimlari { get; set; } = null!;
        public DbSet<RolYetki> RolYetkileri { get; set; } = null!;
        public DbSet<YetkiDegisikligi> YetkiDegisiklikleri { get; set; } = null!;

        // ======= İki Faktörlü Doğrulama DbSet'leri =======
        public DbSet<KullaniciIkiFaktorAyari> KullaniciIkiFaktorAyarlari { get; set; } = null!;
        public DbSet<IkiFaktorGirisTalebi> IkiFaktorGirisTalepleri { get; set; } = null!;
        public DbSet<IkiFaktorKurtarmaKodu> IkiFaktorKurtarmaKodlari { get; set; } = null!;

        // ======= Lookup (Parametre) Tablo DbSet'leri =======
        public DbSet<LookupProjeDurum> LookupProjeDurumlari { get; set; } = null!;
        public DbSet<LookupSandikDurum> LookupSandikDurumlari { get; set; } = null!;
        public DbSet<LookupSandikTipi> LookupSandikTipleri { get; set; } = null!;
        public DbSet<LookupDepoLokasyon> LookupDepoLokasyonlari { get; set; } = null!;
        public DbSet<LookupUrunDurum> LookupUrunDurumlari { get; set; } = null!;
        public DbSet<LookupGridDurum> LookupGridDurumlari { get; set; } = null!;
        public DbSet<LookupGridSevkDurum> LookupGridSevkDurumlari { get; set; } = null!;
        public DbSet<LookupUcKDurum> LookupUcKDurumlari { get; set; } = null!;
        public DbSet<LookupYetkiTipi> LookupYetkiTipleri { get; set; } = null!;
        public DbSet<LookupStokDurum> LookupStokDurumlari { get; set; } = null!;
        public DbSet<LookupIslemTipi> LookupIslemTipleri { get; set; } = null!;
        public DbSet<LookupGeriGonderilmeSebebi> LookupGeriGonderilmeSebepleri { get; set; } = null!;
        public DbSet<LookupProjeTipi> LookupProjeTipleri { get; set; } = null!;
        public DbSet<LookupBirim> LookupBirimler { get; set; } = null!;
        public DbSet<LookupKaliteDurum> LookupKaliteDurumlari { get; set; } = null!;
        public DbSet<LookupSurecDurum> LookupSurecDurumlari { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Her alanın tablo, indeks ve ilişki eşlemesi kendi uzantısındadır; DbContext tek modelin sahibidir.
            modelBuilder.ConfigureLookupTables();
            modelBuilder.ConfigureAmbalajUretimModule();
            modelBuilder.ConfigureFinansModule();
            modelBuilder.ConfigureUretimYasamDongusuModule();
            modelBuilder.ConfigureYetkiKayitlari();
            modelBuilder.ConfigureKimlikModule();
            modelBuilder.ConfigureRbacModule();
            modelBuilder.ConfigureProjeCekiModule();
            modelBuilder.ConfigureSandikModule();
            modelBuilder.ConfigureStokModule();
            modelBuilder.ConfigureSahaAktarimModule();
            modelBuilder.ConfigureOnayModule();
            modelBuilder.ConfigureBildirimModule();
            modelBuilder.ConfigureHareketGecmisiModule();

            modelBuilder.SeedLookupData();
            modelBuilder.SeedApprovalRules();
            modelBuilder.SeedRbacData();
        }

        // Audit alanları (CreatedDate/By, UpdatedDate/By) artık
        // AuditInterceptor tarafından otomatik doldurulur.
    }
}
