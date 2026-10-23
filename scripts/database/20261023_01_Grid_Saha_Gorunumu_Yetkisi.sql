-- =====================================================================================
-- 23 · Grid Sahası ekranı izni (grid-saha-gorunumu, MENU.GRID_SAHA_GORUNUMU), 23 Ekim 2026: Grid'e gelmiş ürünler
-- (varsayılan Grid'de bekleyenler) ve Grid personelinin mal kabul kayıtları, proje tipinden bağımsız tek ekranda;
-- salt görüntüleme (R).
--
-- Sandık Yönetimi (sandik-yonetimi) altına Route'lu ekran olarak eklenir (Id 5258, Route /grid-saha, Sira Grid iş
-- listesinin bir sonrası). Admin W; Grid iş listesinde (grid-is-listesi) R veya W olan roller R alır.
-- Şema değişikliği yoktur: Grid'e geliş tarihi ayrıca saklanmaz, mal kabul tarihi ve personeli hareket geçmişinden
-- okunur.
--
-- Tekrar çalıştırılabilir: düğüm varsa eklenmez, verilmiş izin değiştirilmez. Id veya kod başka bir menü
-- tanımına aitse hiçbir şey değiştirmeden durur. Yeni API'den önce uygulanır (yoksa ekran menüde görünmez, uçlar 403).
-- =====================================================================================
BEGIN;

DO $$
DECLARE
    ust_id integer;
    sira integer;
BEGIN
    SELECT "Id" INTO ust_id FROM "MenuTanimlari" WHERE "Kod" = 'sandik-yonetimi';
    IF ust_id IS NULL THEN
        RAISE EXCEPTION 'sandik-yonetimi menü düğümü bulunamadı; betik durduruldu.';
    END IF;
    IF EXISTS (SELECT 1 FROM "MenuTanimlari" WHERE ("Id" = 5258 AND "Kod" <> 'grid-saha-gorunumu') OR ("Kod" = 'grid-saha-gorunumu' AND "Id" <> 5258)) THEN
        RAISE EXCEPTION '5258 numaralı menü tanımı veya grid-saha-gorunumu kodu başka bir kayda ait; betik durduruldu.';
    END IF;
    SELECT COALESCE((SELECT "Sira" FROM "MenuTanimlari" WHERE "Kod" = 'grid-is-listesi'), 9) + 1 INTO sira;
    IF NOT EXISTS (SELECT 1 FROM "MenuTanimlari" WHERE "Kod" = 'grid-saha-gorunumu') THEN
        INSERT INTO "MenuTanimlari" ("Id", "Kod", "LabelKey", "Icon", "Route", "Sira", "ParentId", "CreatedDate", "CreatedBy")
        VALUES (5258, 'grid-saha-gorunumu', 'MENU.GRID_SAHA_GORUNUMU', 'ri-inbox-archive-line', '/grid-saha', sira, ust_id,
                now() AT TIME ZONE 'Europe/Istanbul', '20261023_01_Grid_Saha_Gorunumu_Yetkisi.sql');
    END IF;
    PERFORM setval(pg_get_serial_sequence('"MenuTanimlari"', 'Id'), (SELECT max("Id") FROM "MenuTanimlari"));
END $$;

-- Admin W; Grid iş listesinde R veya W olan roller R. Verilmiş izin değiştirilmez.
INSERT INTO "RolYetkileri" ("RolId", "MenuTanimiId", "YetkiTipiId", "CreatedDate", "CreatedBy")
SELECT r."Id", yeni."Id",
       (SELECT "Id" FROM "LookupYetkiTipleri" WHERE "Deger" = CASE WHEN r."Id" = 1 THEN 'W' ELSE 'R' END),
       now() AT TIME ZONE 'Europe/Istanbul', '20261023_01_Grid_Saha_Gorunumu_Yetkisi.sql'
FROM "Roller" r
CROSS JOIN (SELECT "Id" FROM "MenuTanimlari" WHERE "Kod" = 'grid-saha-gorunumu') yeni
WHERE (r."Id" = 1 OR EXISTS (
        SELECT 1 FROM "RolYetkileri" ry
        JOIN "MenuTanimlari" m ON m."Id" = ry."MenuTanimiId" AND m."Kod" = 'grid-is-listesi'
        JOIN "LookupYetkiTipleri" tip ON tip."Id" = ry."YetkiTipiId" AND tip."Deger" IN ('R', 'W')
        WHERE ry."RolId" = r."Id"))
  AND NOT EXISTS (
        SELECT 1 FROM "RolYetkileri" mevcut
        WHERE mevcut."RolId" = r."Id" AND mevcut."MenuTanimiId" = yeni."Id");

DO $$
DECLARE
    satir record;
BEGIN
    FOR satir IN
        SELECT r."Ad" AS rol, tip."Deger" AS seviye
        FROM "RolYetkileri" ry
        JOIN "Roller" r ON r."Id" = ry."RolId"
        JOIN "MenuTanimlari" m ON m."Id" = ry."MenuTanimiId" AND m."Kod" = 'grid-saha-gorunumu'
        JOIN "LookupYetkiTipleri" tip ON tip."Id" = ry."YetkiTipiId"
        ORDER BY r."Id"
    LOOP
        RAISE NOTICE 'Rol "%": grid-saha-gorunumu = %', satir.rol, satir.seviye;
    END LOOP;
END $$;

COMMIT;
