-- =====================================================================================
-- 37 — Yükleme simülasyonu: araç aileleri ve tip sadelik sırası (saha geri bildirimi 19.11.2026, madde 1–4)
-- -------------------------------------------------------------------------------------
-- 1) EkipmanTipleri.OtomatikSecimSirasi (integer, boş olabilir; doluysa > 0): otomatik yerleşimde her yük kendisini alan
--    en sade tipe gider (standart konteyner 1, High Cube 2, Hardtop 3, Open Top 4, Flat Rack 5, Platform 6…). Boş tip
--    (Reefer, Pallet Wide, frigo, modüler, diğer) yalnız kullanıcı planda açıkça seçince kullanılır. Sıra yalnız ilk
--    çalıştırmada yazılır: sonradan elle değiştirilen sıra tekrar çalıştırmada korunur.
-- 2) Lowbed / ağır yük ailesi (Id 8, kod lowbed-agir-yuk): lowbed, low-loader, ağır yük dorseleri ve modüler taşıyıcı
--    "Açık kasa / açık platform / lowbed" ailesinden (2) bu aileye taşınır; aile 2'nin adı (değiştirilmediyse) "Standart
--    açık kasa / platform" olur. Böylece standart açık kasa seçilince lowbed gelmez. Ailelerin sırası: Konteyner, Standart
--    açık kasa / platform, Lowbed / ağır yük, Tenteli, Kapalı kasa / frigo, diğerleri.
-- Var olan planların ailesi değişmez (aile 2'yi seçmiş plan artık lowbed'i normal yüklere kullanmaz; aileye sığmayan yük
-- gerekçesiyle lowbed'e gider; aile 2'de lowbed dışında verili konfigürasyon yoksa bu planın normal yükleri "ekipman verisi
-- yok" nedeniyle yerleşemez, plan başka aileyle yeniden hesaplanmalıdır). Konfigürasyonlar, teknik veri ve plan kayıtları
-- değişmez. Tekrar çalıştırma: tip sırası ve
-- aile sırası yalnız tohum değerindeyken yazılır (elle değiştirilen korunur); lowbed tipleri aile 2'deyse yine 8'e taşınır.
-- Önce etkilenecekler NOTICE ile sayılır (taşınacak tip, ailesi 2 olan plan).
-- Ön koşul: 31 (20261105_02_Yukleme_Planlama.sql) uygulanmış olmalı; değilse ya da 8 / lowbed-agir-yuk başka kayda aitse
-- hiçbir şey değiştirmeden durur. Yıkıcı değildir, tekrar çalıştırılabilir. Yeni API'den önce uygulanır (yeni API tip
-- listesinde bu kolonu okur). Finans/Ambalaj tablosuna dokunmaz.
-- =====================================================================================
BEGIN;

DO $$
BEGIN
    IF to_regclass('"EkipmanTipleri"') IS NULL OR to_regclass('"EkipmanAileleri"') IS NULL THEN
        RAISE EXCEPTION '31 numaralı betik (20261105_02_Yukleme_Planlama.sql) uygulanmamış; önce o betiği çalıştırın.';
    END IF;
    IF EXISTS (SELECT 1 FROM "EkipmanAileleri"
               WHERE ("Id" = 8 AND "Kod" <> 'lowbed-agir-yuk') OR ("Kod" = 'lowbed-agir-yuk' AND "Id" <> 8)) THEN
        RAISE EXCEPTION '8 numaralı ekipman ailesi veya lowbed-agir-yuk kodu başka bir kayda ait; betik durduruldu.';
    END IF;
END $$;

DO $$
DECLARE
    tasinacak integer;
    plan2 integer;
BEGIN
    SELECT count(*) INTO tasinacak FROM "EkipmanTipleri"
    WHERE "Kod" IN ('lowbed', 'low-loader', 'uzamali-agir-yuk', 'havuzlu', 'hidrolik-dumenli', 'moduler') AND "AileId" = 2;
    plan2 := CASE WHEN to_regclass('"YuklemePlanlari"') IS NULL THEN 0
                  ELSE (SELECT count(*) FROM "YuklemePlanlari" WHERE "TasimaAilesiId" = 2) END;
    RAISE NOTICE 'Lowbed / ağır yük ailesine taşınacak tip: %; ailesi "açık kasa" (2) olan plan: % (bu planlarda lowbed yalnız aileye sığmayan yük için kullanılır).', tasinacak, plan2;
END $$;

ALTER TABLE "EkipmanTipleri" ADD COLUMN IF NOT EXISTS "OtomatikSecimSirasi" integer;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'CK_EkipmanTipleri_OtomatikSecimSirasi') THEN
        ALTER TABLE "EkipmanTipleri" ADD CONSTRAINT "CK_EkipmanTipleri_OtomatikSecimSirasi"
            CHECK ("OtomatikSecimSirasi" IS NULL OR "OtomatikSecimSirasi" > 0);
    END IF;
END $$;

-- Sadelik sırası: hiçbir tipte sıra yoksa (ilk çalıştırma) tohum tiplerine yazılır.
UPDATE "EkipmanTipleri" t SET "OtomatikSecimSirasi" = v.sira, "UpdatedDate" = now() AT TIME ZONE 'Europe/Istanbul'
FROM (VALUES
    ('k20-dv', 1),
    ('k40-dv', 1),
    ('k40-hc', 2),
    ('k45-hc', 2),
    ('k20-ht', 3),
    ('k40-ht', 3),
    ('k20-ot', 4),
    ('k40-ot', 4),
    ('k20-fr', 5),
    ('k40-fr', 5),
    ('k20-pl', 6),
    ('k40-pl', 6),
    ('acik-kasa', 1),
    ('kapakli-acik-kasa', 1),
    ('sal-platform', 2),
    ('uzamali-platform', 3),
    ('lowbed', 1),
    ('low-loader', 1),
    ('uzamali-agir-yuk', 2),
    ('havuzlu', 2),
    ('hidrolik-dumenli', 3),
    ('tenteli-standart', 1),
    ('tenteli-mega', 2),
    ('tenteli-jumbo', 2),
    ('kapali-kasa', 1)
) AS v(kod, sira)
WHERE t."Kod" = v.kod
  AND NOT EXISTS (SELECT 1 FROM "EkipmanTipleri" x WHERE x."OtomatikSecimSirasi" IS NOT NULL);

INSERT INTO "EkipmanAileleri" ("Id", "Kod", "Ad", "Sira", "CreatedDate")
VALUES (8, 'lowbed-agir-yuk', 'Lowbed / ağır yük', 3, '2026-11-05')
ON CONFLICT DO NOTHING;
SELECT setval(pg_get_serial_sequence('"EkipmanAileleri"', 'Id'), (SELECT max("Id") FROM "EkipmanAileleri"));

UPDATE "EkipmanAileleri" SET "Ad" = 'Standart açık kasa / platform', "UpdatedDate" = now() AT TIME ZONE 'Europe/Istanbul'
WHERE "Id" = 2 AND "Kod" = 'acik-platform' AND "Ad" = 'Açık kasa / açık platform / lowbed';

-- Aile sırası: yalnız 31'in tohum değerinde duran aile yeni sıraya geçer (elle değiştirilen sıra korunur).
UPDATE "EkipmanAileleri" a SET "Sira" = v.yeni, "UpdatedDate" = now() AT TIME ZONE 'Europe/Istanbul'
FROM (VALUES (3, 3, 4), (4, 4, 5), (5, 5, 6), (6, 6, 7), (7, 7, 8)) AS v(id, eski, yeni)
WHERE a."Id" = v.id AND a."Sira" = v.eski;

UPDATE "EkipmanTipleri" SET "AileId" = 8, "UpdatedDate" = now() AT TIME ZONE 'Europe/Istanbul'
WHERE "Kod" IN ('lowbed', 'low-loader', 'uzamali-agir-yuk', 'havuzlu', 'hidrolik-dumenli', 'moduler') AND "AileId" = 2;

DO $$
DECLARE
    satir record;
BEGIN
    FOR satir IN
        SELECT a."Ad" AS aile, string_agg(t."Ad" || COALESCE(' (' || t."OtomatikSecimSirasi" || ')', ' (yalnız seçilince)'), ', '
                   ORDER BY t."OtomatikSecimSirasi" NULLS LAST, t."Sira") AS tipler
        FROM "EkipmanAileleri" a JOIN "EkipmanTipleri" t ON t."AileId" = a."Id"
        GROUP BY a."Id", a."Ad", a."Sira" ORDER BY a."Sira"
    LOOP
        RAISE NOTICE '%: %', satir.aile, satir.tipler;
    END LOOP;
END $$;

-- Betik kaydı (README "Betik kaydı"): bu betiğin bu veritabanında çalıştığını aynı transaction içinde yazar.
CREATE TABLE IF NOT EXISTS "BetikKayitlari" (
    "BetikAdi"       text PRIMARY KEY,
    "UygulamaTarihi" timestamp without time zone NOT NULL,
    "Uygulayan"      text NOT NULL
);
INSERT INTO "BetikKayitlari" ("BetikAdi", "UygulamaTarihi", "Uygulayan")
VALUES ('20261119_02_Yukleme_Arac_Aileleri', now() AT TIME ZONE 'Europe/Istanbul', current_user)
ON CONFLICT ("BetikAdi") DO NOTHING;

COMMIT;
