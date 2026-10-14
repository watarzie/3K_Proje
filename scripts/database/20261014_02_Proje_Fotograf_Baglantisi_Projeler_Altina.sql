-- =====================================================================================
-- Proje fotoğraf bağlantısı iznini (proje-fotograf-baglantisi, Id 5256) Projeler menüsünün altına taşır.
--
-- 20261014_01 düğümü kök düzeyde ekledi; rol ekranında Projeler'in altında (Planlanan Sevk Tarihi'nden sonra, Sira 9)
-- ve ikonsuz görünür. Projeler düğümü artık bu iznin tavanıdır: Projeler R olmayan rolde bağlantı izni etkisizdir
-- (betik sonunda böyle roller NOTICE olarak listelenir). Rol izin satırları değişmez.
--
-- Yıkıcı değildir. Tekrar çalıştırılabilir: düğüm zaten Projeler altındaysa hiçbir şey değişmez. 20261014_01'den sonra
-- çalıştırılır; düğüm veya Projeler yoksa hiçbir şey değiştirmeden durur.
-- =====================================================================================
BEGIN;

DO $$
DECLARE
    projeler_id integer;
    dugum_id integer;
BEGIN
    SELECT "Id" INTO projeler_id FROM "MenuTanimlari" WHERE "Kod" = 'projeler';
    IF projeler_id IS NULL THEN
        RAISE EXCEPTION 'projeler menü düğümü bulunamadı; betik durduruldu.';
    END IF;
    SELECT "Id" INTO dugum_id FROM "MenuTanimlari" WHERE "Kod" = 'proje-fotograf-baglantisi';
    IF dugum_id IS NULL THEN
        RAISE EXCEPTION 'proje-fotograf-baglantisi düğümü bulunamadı; önce 20261014_01_Proje_Fotograf_Baglantisi.sql çalıştırılmalı.';
    END IF;

    UPDATE "MenuTanimlari"
    SET "ParentId" = projeler_id,
        "Sira" = 9,
        "Icon" = '',
        "UpdatedDate" = now() AT TIME ZONE 'Europe/Istanbul',
        "UpdatedBy" = '20261014_02_Proje_Fotograf_Baglantisi_Projeler_Altina.sql'
    WHERE "Id" = dugum_id
      AND ("ParentId" IS DISTINCT FROM projeler_id OR "Sira" <> 9 OR COALESCE("Icon", '') <> '');
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
        JOIN "LookupYetkiTipleri" tip ON tip."Id" = ry."YetkiTipiId" AND tip."Deger" IN ('R', 'W')
        WHERE NOT EXISTS (
            SELECT 1 FROM "RolYetkileri" ust
            JOIN "MenuTanimlari" pm ON pm."Id" = ust."MenuTanimiId" AND pm."Kod" = 'projeler'
            JOIN "LookupYetkiTipleri" ut ON ut."Id" = ust."YetkiTipiId" AND ut."Deger" IN ('R', 'W')
            WHERE ust."RolId" = ry."RolId")
        ORDER BY r."Id"
    LOOP
        RAISE NOTICE 'Rol "%": proje-fotograf-baglantisi = % ama Projeler izni yok; bağlantı izni etkisiz.', satir.rol, satir.seviye;
    END LOOP;
END $$;

COMMIT;
