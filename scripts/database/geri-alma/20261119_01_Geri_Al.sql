-- =====================================================================================
-- 36 numaralı betiğin (20261119_01_Geri_Gonderim_Sevk_Partisi_Sayaci.sql) yaptığı sayaç onarımını geri alır.
-- Önce geri-alma/20261119_01_Yedek.sql çalıştırılmış olmalıdır; yedek tablo yoksa hiçbir şey değişmez ve hata verir.
--
-- Yalnız onarımın değiştirdiği ve sonradan başka bir işlemle değişmemiş kayıtlar geri alınır: UpdatedBy = betik adı ve
-- sayaçlar hâlâ onarımın yazdığı değerde (satırda 0 + true, içerikte 0). Bu kayıtların sayaçları, UpdatedDate ve
-- UpdatedBy alanları yedekteki değerine döner. Satır sonradan uygulamada değiştiyse (ör. Grid yeniden sevk etti) hiçbir
-- kayıt geri alınmaz ve satır NOTICE olarak yazılır: yarım geri alma satır/içerik sayaç eşliğini bozardı.
-- Tekrar çalıştırılabilir: geri alınacak kayıt kalmadıysa hiçbir şey değişmez. Onarımın "BetikKayitlari" satırını
-- siler, kendi satırını yazar.
-- =====================================================================================
-- Yedek yoksa transaction açılmadan durur (oturum yarım transaction'da kalmaz).
DO $$
BEGIN
    IF to_regclass('"_Yedek_20261119_01_SevkPartisi"') IS NULL THEN
        RAISE EXCEPTION 'Yedek tablosu "_Yedek_20261119_01_SevkPartisi" yok; geri alma yapılmadı.';
    END IF;
END $$;

BEGIN;

-- Satır onarımın bıraktığı durumdaysa satır ve bütün içerikleri birlikte geri alınır.
CREATE TEMP TABLE geri_alinan_satir ON COMMIT DROP AS
SELECT cs."Id"
FROM "CekiSatirlari" cs
JOIN "_Yedek_20261119_01_SevkPartisi" y ON y."Tablo" = 'CekiSatirlari' AND y."Id" = cs."Id"
WHERE cs."UpdatedBy" = '20261119_01_Geri_Gonderim_Sevk_Partisi_Sayaci.sql'
  AND cs."AktifGridSevkKarsilananMiktari" = 0
  AND cs."AktifGridSevkPartisiErkenSonuclandirildiMi" = TRUE
  AND NOT EXISTS (SELECT 1 FROM "SandikIcerikleri" si
                  WHERE si."CekiSatiriId" = cs."Id"
                    AND (si."UpdatedBy" IS DISTINCT FROM '20261119_01_Geri_Gonderim_Sevk_Partisi_Sayaci.sql'
                         OR si."AktifGridSevkKarsilananMiktari" IS DISTINCT FROM 0));

UPDATE "SandikIcerikleri" si
SET "AktifGridSevkKarsilananMiktari" = y."AktifKarsilanan",
    "UpdatedDate" = y."UpdatedDate",
    "UpdatedBy" = y."UpdatedBy"
FROM "_Yedek_20261119_01_SevkPartisi" y
WHERE y."Tablo" = 'SandikIcerikleri' AND y."Id" = si."Id"
  AND si."CekiSatiriId" IN (SELECT "Id" FROM geri_alinan_satir);

UPDATE "CekiSatirlari" cs
SET "AktifGridSevkKarsilananMiktari" = y."AktifKarsilanan",
    "AktifGridSevkPartisiErkenSonuclandirildiMi" = y."ErkenSonuclandirildiMi",
    "UpdatedDate" = y."UpdatedDate",
    "UpdatedBy" = y."UpdatedBy"
FROM "_Yedek_20261119_01_SevkPartisi" y
WHERE y."Tablo" = 'CekiSatirlari' AND y."Id" = cs."Id"
  AND cs."Id" IN (SELECT "Id" FROM geri_alinan_satir);

DO $$
DECLARE
    satir record;
BEGIN
    FOR satir IN SELECT "Id" FROM geri_alinan_satir LOOP
        RAISE NOTICE 'Geri alındı: satır Id %', satir."Id";
    END LOOP;
    FOR satir IN
        SELECT cs."Id", cs."UpdatedBy"
        FROM "CekiSatirlari" cs
        JOIN "_Yedek_20261119_01_SevkPartisi" y ON y."Tablo" = 'CekiSatirlari' AND y."Id" = cs."Id"
        WHERE cs."Id" NOT IN (SELECT "Id" FROM geri_alinan_satir)
          AND (cs."AktifGridSevkKarsilananMiktari" IS DISTINCT FROM y."AktifKarsilanan"
               OR cs."AktifGridSevkPartisiErkenSonuclandirildiMi" IS DISTINCT FROM y."ErkenSonuclandirildiMi")
    LOOP
        RAISE NOTICE 'Atlandı (sonradan değişmiş): satır Id %, son değiştiren %', satir."Id", satir."UpdatedBy";
    END LOOP;
    RAISE NOTICE 'Toplam % satır geri alındı.', (SELECT COUNT(*) FROM geri_alinan_satir);
END $$;

-- Betik kaydı (README "Betik kaydı"): bu betiğin bu veritabanında çalıştığını aynı transaction içinde yazar.
CREATE TABLE IF NOT EXISTS "BetikKayitlari" (
    "BetikAdi"       text PRIMARY KEY,
    "UygulamaTarihi" timestamp without time zone NOT NULL,
    "Uygulayan"      text NOT NULL
);
INSERT INTO "BetikKayitlari" ("BetikAdi", "UygulamaTarihi", "Uygulayan")
VALUES ('20261119_01_Geri_Al', now() AT TIME ZONE 'Europe/Istanbul', current_user)
ON CONFLICT ("BetikAdi") DO NOTHING;
-- Onarım geri alındı: kaydı silinir, durum sorgusu 36'yı yeniden izden (onarılacak satır var mı) değerlendirir.
DELETE FROM "BetikKayitlari" WHERE "BetikAdi" = '20261119_01_Geri_Gonderim_Sevk_Partisi_Sayaci';

COMMIT;
