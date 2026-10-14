-- =====================================================================================
-- "Sevkiyatlar" sayfası izni (sevkiyatlar, MENU.SEVKIYATLAR): tır seferleri ve sevkiyat istatistikleri.
--
-- Projeler menüsünün altına Route'lu düğüm olarak eklenir (Id 5246, Sira 3: "Sevk Edilen"den sonra). Okuma
-- izni (R), Projeler menüsünü görebilen ve sevk edilen projeleri görme ya da herhangi bir proje tipini sevk etme
-- izni olan rollere verilir; Admin W alır. Diğer roller için rol ekranından ayarlanır.
--
-- Tekrar çalıştırılabilir: düğüm varsa eklenmez ve izin verilmez (yöneticinin sonradan kaldırdığı izin geri
-- gelmez). Id veya kod başka bir menü tanımına aitse hiçbir şey değiştirmeden durur.
-- =====================================================================================
BEGIN;

DO $$
DECLARE
    projeler_id integer;
BEGIN
    SELECT "Id" INTO projeler_id FROM "MenuTanimlari" WHERE "Kod" = 'projeler';
    IF projeler_id IS NULL THEN
        RAISE EXCEPTION 'projeler menü düğümü bulunamadı; betik durduruldu.';
    END IF;
    IF EXISTS (SELECT 1 FROM "MenuTanimlari" WHERE ("Id" = 5246 AND "Kod" <> 'sevkiyatlar') OR ("Kod" = 'sevkiyatlar' AND "Id" <> 5246)) THEN
        RAISE EXCEPTION '5246 numaralı menü tanımı veya sevkiyatlar kodu başka bir kayda ait; betik durduruldu.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM "MenuTanimlari" WHERE "Kod" = 'sevkiyatlar') THEN
        INSERT INTO "MenuTanimlari" ("Id", "Kod", "LabelKey", "Icon", "Route", "Sira", "ParentId", "CreatedDate", "CreatedBy")
        VALUES (5246, 'sevkiyatlar', 'MENU.SEVKIYATLAR', 'ri-truck-line', '/projeler/sevkiyatlar', 3, projeler_id,
                now() AT TIME ZONE 'Europe/Istanbul', '20261008_05_Sevkiyatlar_Yetkisi.sql');

        -- Admin W; Projeler menüsünü gören ve sevk edilenleri gören ya da sevk edebilen roller R.
        INSERT INTO "RolYetkileri" ("RolId", "MenuTanimiId", "YetkiTipiId", "CreatedDate", "CreatedBy")
        SELECT r."Id", 5246,
               (SELECT "Id" FROM "LookupYetkiTipleri" WHERE "Deger" = CASE WHEN r."Id" = 1 THEN 'W' ELSE 'R' END),
               now() AT TIME ZONE 'Europe/Istanbul', '20261008_05_Sevkiyatlar_Yetkisi.sql'
        FROM "Roller" r
        WHERE EXISTS (
                SELECT 1 FROM "RolYetkileri" ry
                JOIN "MenuTanimlari" m ON m."Id" = ry."MenuTanimiId" AND m."Kod" = 'projeler'
                JOIN "LookupYetkiTipleri" tip ON tip."Id" = ry."YetkiTipiId" AND tip."Deger" IN ('R', 'W')
                WHERE ry."RolId" = r."Id")
          AND EXISTS (
                SELECT 1 FROM "RolYetkileri" ry
                JOIN "MenuTanimlari" m ON m."Id" = ry."MenuTanimiId"
                    AND m."Kod" IN ('sevk-edilen', 'proje-sevk-et', 'saha-sevk-et', 'yedek-sevk-et')
                JOIN "LookupYetkiTipleri" tip ON tip."Id" = ry."YetkiTipiId" AND tip."Deger" IN ('R', 'W')
                WHERE ry."RolId" = r."Id");
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
        JOIN "MenuTanimlari" m ON m."Id" = ry."MenuTanimiId" AND m."Kod" = 'sevkiyatlar'
        JOIN "LookupYetkiTipleri" tip ON tip."Id" = ry."YetkiTipiId"
        ORDER BY r."Id"
    LOOP
        RAISE NOTICE 'Rol "%": sevkiyatlar = %', satir.rol, satir.seviye;
    END LOOP;
END $$;

COMMIT;
