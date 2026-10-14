-- =====================================================================================
-- Finans alt sekme yetki düğümleri.
--
-- Üç Finans sekmesinin altındaki alt sekmeler ayrıca yetkilendirilir (rol editöründe sekme düğümünün altında):
--   İş Takibi (finans-sekme-is-takibi)      → Aylık görünüm (5248), Tüm dönemlerde ara (5249), Bekleyen işler (5250)
--   İş ve Tarife Tanımları (finans-sekme-tanimlar) → Düzenli İşler (5251), Ürün ve Tarifeler (5252), Para Birimi ve Vade (5253)
--   PO ve Faturalar (finans-sekme-po-fatura) → PO'lar (5254), Faturalar (5255)
-- Sekme alt sekmenin tavanıdır: alt sekme R görünür, W alt sekmenin işlemlerini yapar.
--
-- Hiçbir rolün bugünkü yetkisi değişmez: her role, alt sekmede bağlı olduğu sekmedeki seviyesi aynen verilir.
-- Yıkıcı değildir. Tekrar çalıştırılabilir: düğüm varsa eklenmez; alt sekme izni yalnız bu çalıştırmada eklenen düğüme
-- verilir (yöneticinin sonradan kaldırdığı veya düşürdüğü alt sekme izni geri gelmez).
-- Beklenen sekme düğümü yoksa veya Id başka bir koda aitse hiçbir şey değiştirmeden durur.
-- 20261013_02_Finans_Sekme_Yetkisine_Gecis.sql'den sonra, yeni API ile birlikte uygulanır.
-- =====================================================================================
BEGIN;

CREATE TEMP TABLE finans_alt_sekmeleri (
    id integer PRIMARY KEY,
    kod text NOT NULL,
    label_key text NOT NULL,
    icon text NOT NULL,
    sira integer NOT NULL,
    sekme_kod text NOT NULL
) ON COMMIT DROP;

INSERT INTO finans_alt_sekmeleri VALUES
    (5248, 'finans-sekme-is-takibi-aylik',      'MENU.FINANS_SEKME_IS_TAKIBI_AYLIK',      'ri-calendar-line',        1, 'finans-sekme-is-takibi'),
    (5249, 'finans-sekme-is-takibi-arama',      'MENU.FINANS_SEKME_IS_TAKIBI_ARAMA',      'ri-search-2-line',        2, 'finans-sekme-is-takibi'),
    (5250, 'finans-sekme-is-takibi-bekleyen',   'MENU.FINANS_SEKME_IS_TAKIBI_BEKLEYEN',   'ri-time-line',            3, 'finans-sekme-is-takibi'),
    (5251, 'finans-sekme-tanimlar-duzenli',     'MENU.FINANS_SEKME_TANIMLAR_DUZENLI',     'ri-repeat-line',          1, 'finans-sekme-tanimlar'),
    (5252, 'finans-sekme-tanimlar-tarifeler',   'MENU.FINANS_SEKME_TANIMLAR_TARIFELER',   'ri-price-tag-3-line',     2, 'finans-sekme-tanimlar'),
    (5253, 'finans-sekme-tanimlar-para-birimi', 'MENU.FINANS_SEKME_TANIMLAR_PARA_BIRIMI', 'ri-money-euro-circle-line', 3, 'finans-sekme-tanimlar'),
    (5254, 'finans-sekme-po-fatura-po',         'MENU.FINANS_SEKME_PO_FATURA_PO',         'ri-shopping-bag-3-line',  1, 'finans-sekme-po-fatura'),
    (5255, 'finans-sekme-po-fatura-fatura',     'MENU.FINANS_SEKME_PO_FATURA_FATURA',     'ri-file-list-3-line',     2, 'finans-sekme-po-fatura');

-- Bu çalıştırmada eklenen alt sekme düğümleri: izin yalnız bunlara verilir.
CREATE TEMP TABLE finans_yeni_alt_sekmeler (kod text PRIMARY KEY) ON COMMIT DROP;

DO $$
DECLARE
    eksik text;
    cakisan text;
BEGIN
    RAISE NOTICE 'Veritabanı: %', current_database();
    SELECT string_agg(DISTINCT a.sekme_kod, ', ') INTO eksik
    FROM finans_alt_sekmeleri a
    WHERE NOT EXISTS (SELECT 1 FROM "MenuTanimlari" m WHERE m."Kod" = a.sekme_kod);
    IF eksik IS NOT NULL THEN
        RAISE EXCEPTION 'Finans sekme düğümleri bulunamadı: %; önce 20261008_03_Finans_Sekme_Yetkileri.sql çalıştırılmalı.', eksik;
    END IF;
    IF (SELECT count(*) FROM "LookupYetkiTipleri" WHERE "Deger" IN ('R', 'W')) <> 2 THEN
        RAISE EXCEPTION 'R/W yetki tipleri bulunamadı; betik durduruldu.';
    END IF;
    SELECT string_agg(format('%s (%s)', m."Id", m."Kod"), ', ') INTO cakisan
    FROM "MenuTanimlari" m
    JOIN finans_alt_sekmeleri a ON (a.id = m."Id" AND a.kod <> m."Kod") OR (a.kod = m."Kod" AND a.id <> m."Id");
    IF cakisan IS NOT NULL THEN
        RAISE EXCEPTION 'Alt sekme Id''leri veya kodları başka bir menü tanımıyla çakışıyor: %; betik durduruldu.', cakisan;
    END IF;

    INSERT INTO finans_yeni_alt_sekmeler
    SELECT a.kod FROM finans_alt_sekmeleri a WHERE NOT EXISTS (SELECT 1 FROM "MenuTanimlari" m WHERE m."Kod" = a.kod);

    INSERT INTO "MenuTanimlari" ("Id", "Kod", "LabelKey", "Icon", "Route", "Sira", "ParentId", "CreatedDate", "CreatedBy")
    SELECT a.id, a.kod, a.label_key, a.icon, NULL, a.sira, sekme."Id",
           now() AT TIME ZONE 'Europe/Istanbul', '20261013_03_Finans_Alt_Sekme_Yetkileri.sql'
    FROM finans_alt_sekmeleri a
    JOIN "MenuTanimlari" sekme ON sekme."Kod" = a.sekme_kod
    WHERE a.kod IN (SELECT kod FROM finans_yeni_alt_sekmeler);

    PERFORM setval(pg_get_serial_sequence('"MenuTanimlari"', 'Id'), (SELECT max("Id") FROM "MenuTanimlari"));
END $$;

-- Her role yeni alt sekmede, bağlı olduğu sekmedeki seviye (R/W); kaydı olmayan alt sekme izni verilmez.
INSERT INTO "RolYetkileri" ("RolId", "MenuTanimiId", "YetkiTipiId", "CreatedDate", "CreatedBy")
SELECT ry."RolId", alt."Id", ry."YetkiTipiId", now() AT TIME ZONE 'Europe/Istanbul', '20261013_03_Finans_Alt_Sekme_Yetkileri.sql'
FROM finans_alt_sekmeleri a
JOIN finans_yeni_alt_sekmeler yeni ON yeni.kod = a.kod
JOIN "MenuTanimlari" alt ON alt."Kod" = a.kod
JOIN "MenuTanimlari" sekme ON sekme."Kod" = a.sekme_kod
JOIN "RolYetkileri" ry ON ry."MenuTanimiId" = sekme."Id"
JOIN "LookupYetkiTipleri" tip ON tip."Id" = ry."YetkiTipiId" AND tip."Deger" IN ('R', 'W')
WHERE NOT EXISTS (
    SELECT 1 FROM "RolYetkileri" mevcut WHERE mevcut."RolId" = ry."RolId" AND mevcut."MenuTanimiId" = alt."Id");

-- Döküm: rol × alt sekme seviyeleri.
DO $$
DECLARE
    satir record;
BEGIN
    FOR satir IN
        SELECT r."Ad" AS rol, string_agg(format('%s=%s', m."Kod", tip."Deger"), ', ' ORDER BY m."Id") AS izinler
        FROM "RolYetkileri" ry
        JOIN "Roller" r ON r."Id" = ry."RolId"
        JOIN "MenuTanimlari" m ON m."Id" = ry."MenuTanimiId"
        JOIN "LookupYetkiTipleri" tip ON tip."Id" = ry."YetkiTipiId"
        WHERE m."Id" BETWEEN 5248 AND 5255
        GROUP BY r."Id", r."Ad"
        ORDER BY r."Id"
    LOOP
        RAISE NOTICE 'Rol "%": %', satir.rol, satir.izinler;
    END LOOP;
END $$;

COMMIT;
