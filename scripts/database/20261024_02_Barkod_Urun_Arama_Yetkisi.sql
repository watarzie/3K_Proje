-- =====================================================================================
-- 26 · Barkodla ürün arama ekranı izni (barkod-urun-arama, MENU.BARKOD_URUN_ARAMA), 24 Ekim 2026: bir barkodun
-- Normal/Saha/Yedek bütün projelerdeki satırları (varsayılan yalnız eksikler) ve PDF/Excel raporu; salt görüntüleme (R).
--
-- Sandık Yönetimi (sandik-yonetimi) altına kenar menü düğümü olarak eklenir (Id 5260, Route /barkod-arama, Sira
-- kardeşlerin en büyüğünün bir sonrası). Admin W; Sandık Yönetimi'nde R veya W olan roller R alır.
-- Şema değişikliği yoktur.
--
-- Tekrar çalıştırılabilir: düğüm varsa eklenmez, verilmiş izin değiştirilmez. Id veya kod başka bir menü tanımına
-- aitse hiçbir şey değiştirmeden durur. Yeni API'den önce uygulanır (yoksa ekran kenar menüde görünmez, uçlar 403).
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
    IF EXISTS (SELECT 1 FROM "MenuTanimlari" WHERE ("Id" = 5260 AND "Kod" <> 'barkod-urun-arama') OR ("Kod" = 'barkod-urun-arama' AND "Id" <> 5260)) THEN
        RAISE EXCEPTION '5260 numaralı menü tanımı veya barkod-urun-arama kodu başka bir kayda ait; betik durduruldu.';
    END IF;
    SELECT COALESCE(max("Sira"), 0) + 1 INTO sira FROM "MenuTanimlari" WHERE "ParentId" = ust_id AND "Kod" <> 'barkod-urun-arama';
    IF NOT EXISTS (SELECT 1 FROM "MenuTanimlari" WHERE "Kod" = 'barkod-urun-arama') THEN
        INSERT INTO "MenuTanimlari" ("Id", "Kod", "LabelKey", "Icon", "Route", "Sira", "ParentId", "CreatedDate", "CreatedBy")
        VALUES (5260, 'barkod-urun-arama', 'MENU.BARKOD_URUN_ARAMA', 'ri-barcode-line', '/barkod-arama', sira, ust_id,
                now() AT TIME ZONE 'Europe/Istanbul', '20261024_02_Barkod_Urun_Arama_Yetkisi.sql');
    END IF;
    PERFORM setval(pg_get_serial_sequence('"MenuTanimlari"', 'Id'), (SELECT max("Id") FROM "MenuTanimlari"));
END $$;

-- Admin W; Sandık Yönetimi'nde R veya W olan roller R. Verilmiş izin değiştirilmez.
INSERT INTO "RolYetkileri" ("RolId", "MenuTanimiId", "YetkiTipiId", "CreatedDate", "CreatedBy")
SELECT r."Id", yeni."Id",
       (SELECT "Id" FROM "LookupYetkiTipleri" WHERE "Deger" = CASE WHEN r."Id" = 1 THEN 'W' ELSE 'R' END),
       now() AT TIME ZONE 'Europe/Istanbul', '20261024_02_Barkod_Urun_Arama_Yetkisi.sql'
FROM "Roller" r
CROSS JOIN (SELECT "Id" FROM "MenuTanimlari" WHERE "Kod" = 'barkod-urun-arama') yeni
WHERE (r."Id" = 1 OR EXISTS (
        SELECT 1 FROM "RolYetkileri" ry
        JOIN "MenuTanimlari" m ON m."Id" = ry."MenuTanimiId" AND m."Kod" = 'sandik-yonetimi'
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
        JOIN "MenuTanimlari" m ON m."Id" = ry."MenuTanimiId" AND m."Kod" = 'barkod-urun-arama'
        JOIN "LookupYetkiTipleri" tip ON tip."Id" = ry."YetkiTipiId"
        ORDER BY r."Id"
    LOOP
        RAISE NOTICE 'Rol "%": barkod-urun-arama = %', satir.rol, satir.seviye;
    END LOOP;
END $$;

COMMIT;
