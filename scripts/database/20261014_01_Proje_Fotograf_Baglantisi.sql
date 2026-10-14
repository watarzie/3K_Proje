-- =====================================================================================
-- Proje fotoğraf sayfası bağlantısı.
--
-- "Projeler"."FotografBaglantisi" (varchar 1000, boş olabilir) kolonunu ekler: projenin fotoğraflarının bulunduğu web
-- sayfası (yalnız http/https; doğrulama uygulamadadır, ProjeFotografBaglantisiKurali).
--
-- Yetki düğümü proje-fotograf-baglantisi (Id 5256, MENU.PROJE_FOTOGRAF_BAGLANTISI): kök düzeyde, rotasızdır; Normal,
-- Saha ve Yedek proje listelerinin hepsinde aynı koddur. R bağlantıyı görür ve açar, W ekler/değiştirir/kaldırır.
-- Yalnız Admin W alır; diğer roller rol ekranından ayarlanır (varsayılan N: bağlantı görünmez).
--
-- Yıkıcı değildir. Tekrar çalıştırılabilir: kolon/düğüm varsa eklenmez ve izin verilmez (yöneticinin sonradan kaldırdığı
-- izin geri gelmez). Id veya kod başka
-- bir menü tanımına aitse hiçbir şey değiştirmeden durur. Yeni API ile birlikte uygulanır (yeni API kolonu okur).
-- =====================================================================================
BEGIN;

ALTER TABLE "Projeler" ADD COLUMN IF NOT EXISTS "FotografBaglantisi" character varying(1000) NULL;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "MenuTanimlari"
               WHERE ("Id" = 5256 AND "Kod" <> 'proje-fotograf-baglantisi')
                  OR ("Kod" = 'proje-fotograf-baglantisi' AND "Id" <> 5256)) THEN
        RAISE EXCEPTION '5256 numaralı menü tanımı veya proje-fotograf-baglantisi kodu başka bir kayda ait; betik durduruldu.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM "MenuTanimlari" WHERE "Kod" = 'proje-fotograf-baglantisi') THEN
        INSERT INTO "MenuTanimlari" ("Id", "Kod", "LabelKey", "Icon", "Route", "Sira", "ParentId", "CreatedDate", "CreatedBy")
        VALUES (5256, 'proje-fotograf-baglantisi', 'MENU.PROJE_FOTOGRAF_BAGLANTISI', 'ri-camera-line', NULL, 2, NULL,
                now() AT TIME ZONE 'Europe/Istanbul', '20261014_01_Proje_Fotograf_Baglantisi.sql');

        -- Yalnız Admin W.
        INSERT INTO "RolYetkileri" ("RolId", "MenuTanimiId", "YetkiTipiId", "CreatedDate", "CreatedBy")
        SELECT r."Id", 5256, (SELECT "Id" FROM "LookupYetkiTipleri" WHERE "Deger" = 'W'),
               now() AT TIME ZONE 'Europe/Istanbul', '20261014_01_Proje_Fotograf_Baglantisi.sql'
        FROM "Roller" r
        WHERE r."Id" = 1;
    END IF;
    PERFORM setval(pg_get_serial_sequence('"MenuTanimlari"', 'Id'), (SELECT max("Id") FROM "MenuTanimlari"));
END $$;

DO $$
DECLARE
    satir record;
BEGIN
    FOR satir IN
        SELECT r."Ad" AS rol, tip."Deger" AS seviye
        FROM "RolYetkileri" ry
        JOIN "Roller" r ON r."Id" = ry."RolId"
        JOIN "MenuTanimlari" m ON m."Id" = ry."MenuTanimiId" AND m."Kod" = 'proje-fotograf-baglantisi'
        JOIN "LookupYetkiTipleri" tip ON tip."Id" = ry."YetkiTipiId"
        ORDER BY r."Id"
    LOOP
        RAISE NOTICE 'Rol "%": proje-fotograf-baglantisi = %', satir.rol, satir.seviye;
    END LOOP;
END $$;

COMMIT;
