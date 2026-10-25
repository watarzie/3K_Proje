-- =====================================================================================
-- 29 · Barkod etiketi ekranı izni (barkod-etiket, MENU.BARKOD_ETIKET), 26 Ekim 2026: bir projenin seçilen sandıkları
-- ve ürünleri için Grid etiketleri (sandık 100×75 mm, ürün 60×40 mm; PDF veya Godex EZPL). Veri değiştirmez (R).
--
-- Sandık Yönetimi (sandik-yonetimi) altına rotasız izin düğümü olarak eklenir (Id 5262, Route NULL, Sira kardeşlerin
-- en büyüğünün bir sonrası): kenar menüde görünmez; ekran (/barkod-etiket) Projeler listesi ve 3K sandık listesinin
-- "Diğer" menüsünden açılır. Admin W; Sandık Yönetimi'nde R veya W olan roller R alır. Şema değişikliği yoktur.
--
-- 28 numara feature/giris-guvenligi dalındaki 20261025_01_Giris_Kilidi.sql betiğindedir; bu betik ondan bağımsızdır
-- (menü kimliği kullanmaz), sıra yalnız dosya adındaki tarihtir.
--
-- Tekrar çalıştırılabilir: düğüm varsa eklenmez, verilmiş izin değiştirilmez. Id veya kod başka bir menü tanımına
-- aitse hiçbir şey değiştirmeden durur. Yeni API'den önce uygulanır (yoksa "Barkod Etiketi" bağlantısı görünmez ve
-- etiket uçları 403 verir).
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
    IF EXISTS (SELECT 1 FROM "MenuTanimlari" WHERE ("Id" = 5262 AND "Kod" <> 'barkod-etiket') OR ("Kod" = 'barkod-etiket' AND "Id" <> 5262)) THEN
        RAISE EXCEPTION '5262 numaralı menü tanımı veya barkod-etiket kodu başka bir kayda ait; betik durduruldu.';
    END IF;
    SELECT COALESCE(max("Sira"), 0) + 1 INTO sira FROM "MenuTanimlari" WHERE "ParentId" = ust_id AND "Kod" <> 'barkod-etiket';
    IF NOT EXISTS (SELECT 1 FROM "MenuTanimlari" WHERE "Kod" = 'barkod-etiket') THEN
        INSERT INTO "MenuTanimlari" ("Id", "Kod", "LabelKey", "Icon", "Route", "Sira", "ParentId", "CreatedDate", "CreatedBy")
        VALUES (5262, 'barkod-etiket', 'MENU.BARKOD_ETIKET', 'ri-printer-line', NULL, sira, ust_id,
                now() AT TIME ZONE 'Europe/Istanbul', '20261026_01_Barkod_Etiket_Yetkisi.sql');
    END IF;
    PERFORM setval(pg_get_serial_sequence('"MenuTanimlari"', 'Id'), (SELECT max("Id") FROM "MenuTanimlari"));
END $$;

-- Admin W; Sandık Yönetimi'nde R veya W olan roller R. Verilmiş izin değiştirilmez.
INSERT INTO "RolYetkileri" ("RolId", "MenuTanimiId", "YetkiTipiId", "CreatedDate", "CreatedBy")
SELECT r."Id", yeni."Id",
       (SELECT "Id" FROM "LookupYetkiTipleri" WHERE "Deger" = CASE WHEN r."Id" = 1 THEN 'W' ELSE 'R' END),
       now() AT TIME ZONE 'Europe/Istanbul', '20261026_01_Barkod_Etiket_Yetkisi.sql'
FROM "Roller" r
CROSS JOIN (SELECT "Id" FROM "MenuTanimlari" WHERE "Kod" = 'barkod-etiket') yeni
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
        JOIN "MenuTanimlari" m ON m."Id" = ry."MenuTanimiId" AND m."Kod" = 'barkod-etiket'
        JOIN "LookupYetkiTipleri" tip ON tip."Id" = ry."YetkiTipiId"
        ORDER BY r."Id"
    LOOP
        RAISE NOTICE 'Rol "%": barkod-etiket = %', satir.rol, satir.seviye;
    END LOOP;
END $$;

COMMIT;
