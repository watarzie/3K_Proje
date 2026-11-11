-- =====================================================================================
-- <Açıklama> izni (<kod>, MENU.<KOD>): <ne işe yarar>.
--
-- <Üst düğüm> altına <Route'lu ekran / işlem kodu> olarak eklenir (Id <ID>, Sira <SIRA>). <Hangi rollere hangi seviye>.
--
-- Tekrar çalıştırılabilir: düğüm varsa eklenmez, verilmiş izin değiştirilmez. Id veya kod başka bir menü
-- tanımına aitse hiçbir şey değiştirmeden durur.
-- =====================================================================================
BEGIN;

DO $$
DECLARE
    ust_id integer;
BEGIN
    SELECT "Id" INTO ust_id FROM "MenuTanimlari" WHERE "Kod" = '<ust-kod>';
    IF ust_id IS NULL THEN
        RAISE EXCEPTION '<ust-kod> menü düğümü bulunamadı; betik durduruldu.';
    END IF;
    IF EXISTS (SELECT 1 FROM "MenuTanimlari" WHERE ("Id" = <ID> AND "Kod" <> '<kod>') OR ("Kod" = '<kod>' AND "Id" <> <ID>)) THEN
        RAISE EXCEPTION '<ID> numaralı menü tanımı veya <kod> kodu başka bir kayda ait; betik durduruldu.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM "MenuTanimlari" WHERE "Kod" = '<kod>') THEN
        INSERT INTO "MenuTanimlari" ("Id", "Kod", "LabelKey", "Icon", "Route", "Sira", "ParentId", "CreatedDate", "CreatedBy")
        VALUES (<ID>, '<kod>', 'MENU.<KOD>', '<ri-ikon veya NULL>', <'/rota' veya NULL>, <SIRA>, ust_id,
                now() AT TIME ZONE 'Europe/Istanbul', '<BETIK_ADI>.sql');
    END IF;
    PERFORM setval(pg_get_serial_sequence('"MenuTanimlari"', 'Id'), (SELECT max("Id") FROM "MenuTanimlari"));
END $$;

-- Admin W; <koşul> roller R. Verilmiş izin değiştirilmez.
INSERT INTO "RolYetkileri" ("RolId", "MenuTanimiId", "YetkiTipiId", "CreatedDate", "CreatedBy")
SELECT r."Id", yeni."Id",
       (SELECT "Id" FROM "LookupYetkiTipleri" WHERE "Deger" = CASE WHEN r."Id" = 1 THEN 'W' ELSE 'R' END),
       now() AT TIME ZONE 'Europe/Istanbul', '<BETIK_ADI>.sql'
FROM "Roller" r
CROSS JOIN (SELECT "Id" FROM "MenuTanimlari" WHERE "Kod" = '<kod>') yeni
WHERE (r."Id" = 1 OR EXISTS (
        SELECT 1 FROM "RolYetkileri" ry
        JOIN "MenuTanimlari" m ON m."Id" = ry."MenuTanimiId" AND m."Kod" = '<ust-kod>'
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
        JOIN "MenuTanimlari" m ON m."Id" = ry."MenuTanimiId" AND m."Kod" = '<kod>'
        JOIN "LookupYetkiTipleri" tip ON tip."Id" = ry."YetkiTipiId"
        ORDER BY r."Id"
    LOOP
        RAISE NOTICE 'Rol "%": <kod> = %', satir.rol, satir.seviye;
    END LOOP;
END $$;

-- Betik kaydı (scripts/database/README.md "Betik kaydı"): bu betiğin bu veritabanında çalıştığını aynı transaction içinde yazar.
CREATE TABLE IF NOT EXISTS "BetikKayitlari" (
    "BetikAdi"       text PRIMARY KEY,
    "UygulamaTarihi" timestamp without time zone NOT NULL,
    "Uygulayan"      text NOT NULL
);
INSERT INTO "BetikKayitlari" ("BetikAdi", "UygulamaTarihi", "Uygulayan")
VALUES ('<BETIK_ADI>', now() AT TIME ZONE 'Europe/Istanbul', current_user)
ON CONFLICT ("BetikAdi") DO NOTHING;

COMMIT;
