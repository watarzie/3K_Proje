-- =====================================================================================
-- Finans izin ağacı güncellemesi (D3, F13).
--
-- 1) finans-odeme-kaydet (MENU.FINANS_ODEME_KAYDET): faturayı ödendi/ödenmedi olarak işaretleme izni,
--    finans kökünün (finans-yonetimi) yaprağı olarak eklenir. Mevcut kullanıcılar ödeme işaretini
--    kaybetmesin diye fatura girme veya değiştirme izni olan her role aynı seviyede (R/W; ikisinden
--    yüksek olanı) verilir. Kişisel kararlarda fatura izinlerinden biri açıkça verilmişse ödeme izni
--    de verilir; yalnız ret kararları varsa ret kopyalanır. Diğer roller için rol ekranından ayarlanır.
-- 2) finans-sablon-yonet: iş şablonu sistemi kaldırıldığı için (F13) izin düğümü, rol izinleri ve
--    kişisel kararlarıyla birlikte silinir (kişisel karar FK'si RESTRICT olduğundan önce silinir).
--
-- Tablo yapısı değişmez. Tekrar çalıştırılabilir: izin zaten varsa eklenmez, verilmiş izin
-- değiştirilmez; silinecek kod yoksa hiçbir satır etkilenmez.
-- =====================================================================================
BEGIN;

DO $$
DECLARE
    kok_id integer;
BEGIN
    SELECT "Id" INTO kok_id FROM "MenuTanimlari" WHERE "Kod" = 'finans-yonetimi';
    IF kok_id IS NULL THEN
        RAISE EXCEPTION 'finans-yonetimi menü kökü bulunamadı; betik durduruldu.';
    END IF;
    IF EXISTS (SELECT 1 FROM "MenuTanimlari" WHERE "Id" = 5237 AND "Kod" <> 'finans-odeme-kaydet') THEN
        RAISE EXCEPTION '5237 numaralı menü tanımı başka bir koda ait; betik durduruldu.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM "MenuTanimlari" WHERE "Kod" = 'finans-odeme-kaydet') THEN
        INSERT INTO "MenuTanimlari" ("Id", "Kod", "LabelKey", "Icon", "Route", "Sira", "ParentId", "CreatedDate", "CreatedBy")
        VALUES (5237, 'finans-odeme-kaydet', 'MENU.FINANS_ODEME_KAYDET', '', NULL, 5237, kok_id,
                now() AT TIME ZONE 'Europe/Istanbul', '20260929_02_Finans_Odeme_Yetkisi.sql');
    END IF;
END $$;

-- Rol izinleri: fatura girme/değiştirme seviyesinin yükseği (N verilmez; N zaten izin yok demektir).
INSERT INTO "RolYetkileri" ("RolId", "MenuTanimiId", "YetkiTipiId", "CreatedDate", "CreatedBy")
SELECT ry."RolId", odeme."Id", max(ry."YetkiTipiId"), now() AT TIME ZONE 'Europe/Istanbul', '20260929_02_Finans_Odeme_Yetkisi.sql'
FROM "RolYetkileri" ry
JOIN "MenuTanimlari" fatura ON fatura."Id" = ry."MenuTanimiId" AND fatura."Kod" IN ('finans-fatura-gir', 'finans-fatura-degistir')
JOIN "LookupYetkiTipleri" tip ON tip."Id" = ry."YetkiTipiId" AND tip."Deger" IN ('R', 'W')
CROSS JOIN (SELECT "Id" FROM "MenuTanimlari" WHERE "Kod" = 'finans-odeme-kaydet') odeme
WHERE NOT EXISTS (
    SELECT 1 FROM "RolYetkileri" mevcut
    WHERE mevcut."RolId" = ry."RolId" AND mevcut."MenuTanimiId" = odeme."Id")
GROUP BY ry."RolId", odeme."Id";

-- Kişisel kararlar: fatura izinlerinden biri açıkça verilmişse izin, yalnız ret varsa ret.
-- Kişisel izin tablosu 20261003_01 ile kaldırıldı; bu adım yalnız tablo hâlâ varsa çalışır.
DO $$
BEGIN
    IF to_regclass('public."KullaniciYetkileri"') IS NOT NULL THEN
        INSERT INTO "KullaniciYetkileri" ("KullaniciId", "MenuTanimiId", "IzinVerildi", "CreatedDate", "CreatedBy")
        SELECT ky."KullaniciId", odeme."Id", bool_or(ky."IzinVerildi"), now() AT TIME ZONE 'Europe/Istanbul', '20260929_02_Finans_Odeme_Yetkisi.sql'
        FROM "KullaniciYetkileri" ky
        JOIN "MenuTanimlari" fatura ON fatura."Id" = ky."MenuTanimiId" AND fatura."Kod" IN ('finans-fatura-gir', 'finans-fatura-degistir')
        CROSS JOIN (SELECT "Id" FROM "MenuTanimlari" WHERE "Kod" = 'finans-odeme-kaydet') odeme
        WHERE NOT EXISTS (
            SELECT 1 FROM "KullaniciYetkileri" mevcut
            WHERE mevcut."KullaniciId" = ky."KullaniciId" AND mevcut."MenuTanimiId" = odeme."Id")
        GROUP BY ky."KullaniciId", odeme."Id";
    END IF;
END $$;

-- Şablon izni (yaprak düğüm olduğu doğrulanır).
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM "MenuTanimlari" alt
        JOIN "MenuTanimlari" ust ON ust."Id" = alt."ParentId"
        WHERE ust."Kod" = 'finans-sablon-yonet') THEN
        RAISE EXCEPTION 'finans-sablon-yonet izninin alt düğümü var; betik durduruldu.';
    END IF;
END $$;

DO $$
BEGIN
    IF to_regclass('public."KullaniciYetkileri"') IS NOT NULL THEN
        DELETE FROM "KullaniciYetkileri" ky
        USING "MenuTanimlari" m
        WHERE ky."MenuTanimiId" = m."Id" AND m."Kod" = 'finans-sablon-yonet';
    END IF;
END $$;

DELETE FROM "RolYetkileri" ry
USING "MenuTanimlari" m
WHERE ry."MenuTanimiId" = m."Id" AND m."Kod" = 'finans-sablon-yonet';

DELETE FROM "MenuTanimlari" WHERE "Kod" = 'finans-sablon-yonet';

COMMIT;
