-- =====================================================================================
-- API'nin veritabanına bağlandığı, süper kullanıcı OLMAYAN uygulama rolü: uck_uygulama
--
-- Neden: API "postgres" (süper kullanıcı) ile bağlanırsa uygulamadaki herhangi bir açık yalnız 3K verisine değil,
-- veritabanı sunucusunun tamamına (işletim sistemi komutu, sunucu dosyaları, diğer veritabanları) uzanır.
-- Bu rol yalnız 3K tablolarını okur/yazar; tablo/rol oluşturamaz, şemayı değiştiremez, TRUNCATE edemez,
-- sunucu dosyası okuyamaz, program çalıştıramaz.
--
-- KULLANICI ÇALIŞTIRIR; süper kullanıcıyla (postgres; rol oluşturmak için gerekir), uygulama veritabanına
-- bağlıyken. Claude bu betiği gerçek veritabanında çalıştırmaz. Yıkıcı değildir, tekrar çalıştırılabilir, veri
-- değiştirmez.
--
--   sudo -u postgres psql -d <veritabanı> -v ON_ERROR_STOP=1 -f Uygulama_Rolu.sql
--
-- Betik parola içermez (parola betiğe, kabuk geçmişine ve sunucu loguna düşmesin). Rol parolası ayrıca,
-- etkileşimli olarak verilir; parola verilene kadar rol giriş yapamaz:
--
--   sudo -u postgres psql -c '\password uck_uygulama'
--
-- Sonra sunucudaki appsettings.Secrets.json bağlantı cümlesinde yalnız Username ve Password değişir ve servis
-- yeniden başlatılır. Geri dönüş: bağlantı cümlesi eski haline getirilip servis yeniden başlatılır (rol durabilir).
--
-- Şema betikleri (scripts/database/bekleyen) eskisi gibi, bugün tabloların sahibi olan rolle çalıştırılır (canlıda
-- "Watarzie"). Aşağıdaki varsayılan izinler sayesinde o rolün sonradan oluşturduğu tablolar uygulama rolüne
-- kendiliğinden açılır; betiği yeniden çalıştırmak gerekmez. Şema betikleri BAŞKA bir rolle çalıştırılmaya başlanırsa
-- bu betik bir kez daha çalıştırılır. Uygulamanın DOĞRUDAN çağıracağı yeni bir fonksiyon eklenirse o betik EXECUTE
-- iznini kendisi verir (tetikleyici fonksiyonları için izin gerekmez).
--
-- Yedek başka bir sunucuya geri yüklenirse izinler yedekle gelir ama rol gelmez: önce bu betik çalıştırılır.
--
-- Rolü tamamen kaldırmak için (bağlantı cümlesi eski haline döndükten sonra):
--   DROP OWNED BY uck_uygulama;  DROP ROLE uck_uygulama;
-- =====================================================================================
BEGIN;

DO $$
BEGIN
    IF NOT (SELECT rolsuper FROM pg_roles WHERE rolname = current_user) THEN
        RAISE EXCEPTION 'Bu betik süper kullanıcıyla (postgres) çalıştırılmalıdır; şu an: %', current_user;
    END IF;
    IF current_database() IN ('postgres', 'template0', 'template1') THEN
        RAISE EXCEPTION 'Betik uygulama veritabanına bağlıyken çalıştırılmalıdır; şu an: %', current_database();
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'uck_uygulama') THEN
        CREATE ROLE uck_uygulama LOGIN;
    END IF;
END $$;

-- Rol önceden başka niteliklerle açılmış olsa da sonuç aynıdır.
ALTER ROLE uck_uygulama LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;

DO $$
BEGIN
    EXECUTE format('GRANT CONNECT ON DATABASE %I TO uck_uygulama', current_database());
END $$;

-- Şemada nesne oluşturma PUBLIC'e kapatılır (PostgreSQL 14 ve öncesinde açıktır); uygulama rolü tablo açamaz.
REVOKE CREATE ON SCHEMA public FROM PUBLIC;
GRANT USAGE ON SCHEMA public TO uck_uygulama;

-- Uygulamanın yaptığı her şey: okuma, yazma ve satır kilidi (SELECT ... FOR UPDATE, UPDATE izni ister).
-- TRUNCATE, REFERENCES ve TRIGGER bilerek verilmez.
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO uck_uygulama;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO uck_uygulama;

-- Bundan sonra oluşturulacak tablo ve sıralar. Varsayılan izin, nesneyi OLUŞTURAN role bağlıdır: şema betiklerini
-- çalıştıran rol(ler) bugünkü tablo sahiplerinden bilinir, bu betiği çalıştıran rol de eklenir. Aynı roller
-- PUBLIC'e kapatılan şemada tablo oluşturmaya devam edebilsin diye CREATE izinleri açıkça verilir.
DO $$
DECLARE
    sahip text;
BEGIN
    FOR sahip IN
        SELECT DISTINCT pg_get_userbyid(c.relowner)::text
          FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
         WHERE n.nspname = 'public' AND c.relkind IN ('r', 'p')
        UNION
        SELECT current_user::text
    LOOP
        EXECUTE format('GRANT USAGE, CREATE ON SCHEMA public TO %I', sahip);
        EXECUTE format(
            'ALTER DEFAULT PRIVILEGES FOR ROLE %I IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO uck_uygulama',
            sahip);
        EXECUTE format(
            'ALTER DEFAULT PRIVILEGES FOR ROLE %I IN SCHEMA public GRANT USAGE, SELECT ON SEQUENCES TO uck_uygulama',
            sahip);
        RAISE NOTICE 'uck_uygulama: "%" rolünün bundan sonra oluşturacağı tablolar kendiliğinden açılacak.', sahip;
    END LOOP;
END $$;

-- Betik kaydı (README "Betik kaydı"). Kayıt tablosu burada KURULMAZ: süper kullanıcı kurarsa sahibi o olur ve şema
-- betiklerini çalıştıran rol kendi kaydını yazamaz. Tablo varsa (34 numaralı betikten beri vardır) kayıt yazılır.
-- Betik kayıtlarını uygulama okumaz; uygulama rolü hangi betiğin çalıştığı bilgisini göremesin, değiştiremesin.
DO $$
BEGIN
    IF to_regclass('public."BetikKayitlari"') IS NOT NULL THEN
        INSERT INTO public."BetikKayitlari" ("BetikAdi", "UygulamaTarihi", "Uygulayan")
        VALUES ('Uygulama_Rolu', now() AT TIME ZONE 'Europe/Istanbul', current_user)
        ON CONFLICT ("BetikAdi") DO NOTHING;
        REVOKE ALL ON public."BetikKayitlari" FROM uck_uygulama;
    END IF;
END $$;

DO $$
DECLARE
    tablo_sayisi integer;
    acik_tablo_sayisi integer;
BEGIN
    SELECT count(*), count(*) FILTER (WHERE has_table_privilege('uck_uygulama', c.oid, 'SELECT, INSERT, UPDATE, DELETE'))
      INTO tablo_sayisi, acik_tablo_sayisi
      FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
     WHERE n.nspname = 'public' AND c.relkind IN ('r', 'p');
    RAISE NOTICE 'uck_uygulama: % tablonun %''ine okuma/yazma izni verildi (BetikKayitlari bilerek kapalı).',
        tablo_sayisi, acik_tablo_sayisi;
END $$;

COMMIT;
