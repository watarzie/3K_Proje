-- Yedek modülü alt yetkileri
-- Kapsam yalnızca MenuTanimlari ile Admin (RolId = 1) RolYetkileri kayıtlarıdır.
-- Geçmiş proje, çeki, sandık veya diğer operasyonel veriler değiştirilmez.

BEGIN;

DO $$
DECLARE
    menu_sequence text;
    rol_yetki_sequence text;
    max_id bigint;
BEGIN
    IF NOT EXISTS (SELECT 1 FROM "MenuTanimlari" WHERE "Kod" = 'yedek-yonetimi') THEN
        RAISE EXCEPTION 'yedek-yonetimi menü kaydı bulunamadı.';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM "Roller" WHERE "Id" = 1) THEN
        RAISE EXCEPTION 'Admin rolü (RolId = 1) bulunamadı.';
    END IF;

    menu_sequence := pg_get_serial_sequence('"MenuTanimlari"', 'Id');
    IF menu_sequence IS NOT NULL THEN
        SELECT COALESCE(MAX("Id"), 0) INTO max_id FROM "MenuTanimlari";
        IF max_id > 0 THEN
            PERFORM setval(menu_sequence::regclass, max_id, true);
        ELSE
            PERFORM setval(menu_sequence::regclass, 1, false);
        END IF;
    END IF;

    rol_yetki_sequence := pg_get_serial_sequence('"RolYetkileri"', 'Id');
    IF rol_yetki_sequence IS NOT NULL THEN
        SELECT COALESCE(MAX("Id"), 0) INTO max_id FROM "RolYetkileri";
        IF max_id > 0 THEN
            PERFORM setval(rol_yetki_sequence::regclass, max_id, true);
        ELSE
            PERFORM setval(rol_yetki_sequence::regclass, 1, false);
        END IF;
    END IF;
END $$;

WITH yedek_parent AS (
    SELECT "Id"
    FROM "MenuTanimlari"
    WHERE "Kod" = 'yedek-yonetimi'
),
menu_verisi("Kod", "LabelKey", "Sira") AS (
    VALUES
        ('yedek-ceki-yukle', 'MENU.YEDEK_CEKI_YUKLE', 1),
        ('yedek-grid-modulu', 'MENU.YEDEK_GRID_MODULU', 2),
        ('yedek-3k-modulu', 'MENU.YEDEK_3K_MODULU', 3),
        ('yedek-sandiklar', 'MENU.YEDEK_SANDIKLAR', 4),
        ('yedek-raporu', 'MENU.YEDEK_RAPORU', 5),
        ('yedek-eksik-raporu', 'MENU.YEDEK_EKSIK_RAPORU', 6),
        ('yedek-gerceklesen-ceki-raporu', 'MENU.YEDEK_GERCEKLESEN_CEKI_RAPORU', 7),
        ('yedek-3k-sandik-durum-raporu', 'MENU.YEDEK_3K_SANDIK_DURUM_RAPORU', 8),
        ('yedek-sevk-et', 'MENU.YEDEK_SEVK_ET', 9),
        ('yedek-proje-sil', 'MENU.YEDEK_PROJE_SIL', 10),
        ('yedek-planlanan-sevk-tarihi', 'MENU.YEDEK_PLANLANAN_SEVK_TARIHI', 11)
)
INSERT INTO "MenuTanimlari"
    ("Kod", "LabelKey", "Icon", "Route", "Sira", "ParentId", "CreatedDate", "CreatedBy")
SELECT
    menu_verisi."Kod",
    menu_verisi."LabelKey",
    '',
    NULL,
    menu_verisi."Sira",
    yedek_parent."Id",
    CURRENT_TIMESTAMP,
    'SQL-20260714'
FROM menu_verisi
CROSS JOIN yedek_parent
ON CONFLICT ("Kod") DO UPDATE SET
    "LabelKey" = EXCLUDED."LabelKey",
    "Icon" = EXCLUDED."Icon",
    "Route" = EXCLUDED."Route",
    "Sira" = EXCLUDED."Sira",
    "ParentId" = EXCLUDED."ParentId",
    "UpdatedDate" = CURRENT_TIMESTAMP,
    "UpdatedBy" = 'SQL-20260714'
WHERE "MenuTanimlari"."LabelKey" IS DISTINCT FROM EXCLUDED."LabelKey"
   OR "MenuTanimlari"."Icon" IS DISTINCT FROM EXCLUDED."Icon"
   OR "MenuTanimlari"."Route" IS DISTINCT FROM EXCLUDED."Route"
   OR "MenuTanimlari"."Sira" IS DISTINCT FROM EXCLUDED."Sira"
   OR "MenuTanimlari"."ParentId" IS DISTINCT FROM EXCLUDED."ParentId";

INSERT INTO "RolYetkileri"
    ("RolId", "MenuTanimiId", "YetkiTipiId", "CreatedDate", "CreatedBy")
SELECT
    1,
    menu."Id",
    3,
    CURRENT_TIMESTAMP,
    'SQL-20260714'
FROM "MenuTanimlari" menu
WHERE menu."Kod" IN (
    'yedek-ceki-yukle',
    'yedek-grid-modulu',
    'yedek-3k-modulu',
    'yedek-sandiklar',
    'yedek-raporu',
    'yedek-eksik-raporu',
    'yedek-gerceklesen-ceki-raporu',
    'yedek-3k-sandik-durum-raporu',
    'yedek-sevk-et',
    'yedek-proje-sil',
    'yedek-planlanan-sevk-tarihi'
)
ON CONFLICT ("RolId", "MenuTanimiId") DO UPDATE SET
    "YetkiTipiId" = 3,
    "UpdatedDate" = CURRENT_TIMESTAMP,
    "UpdatedBy" = 'SQL-20260714'
WHERE "RolYetkileri"."RolId" = 1
  AND "RolYetkileri"."YetkiTipiId" <> 3;

COMMIT;
