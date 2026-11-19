-- =====================================================================================
-- 36 numaralı betikten (20261119_01_Geri_Gonderim_Sevk_Partisi_Sayaci.sql) ÖNCE çalıştırılır: dokunabileceği satırın
-- ve sandık içeriklerinin sayaçlarını yedekler.
--
-- Onarım yalnız CekiSatirlari.Id 34371'in ve onun SandikIcerikleri kayıtlarının aktif parti sayaçlarını, UpdatedDate ve
-- UpdatedBy alanlarını değiştirir. Bu betik o kayıtların şimdiki değerlerini "_Yedek_20261119_01_SevkPartisi"
-- tablosuna yazar. Başka hiçbir tabloya dokunmaz.
--
-- Tekrar çalıştırılabilir: tablo varsa yeniden oluşturulmaz; zaten yedeklenmiş kaydın ilk değeri korunur
-- (onarımdan sonra tekrar çalıştırılsa da onarım öncesi değer kalır).
-- Geri alma: geri-alma/20261119_01_Geri_Al.sql. Yedek tablo uygulama tarafından kullanılmaz; doğrulama bittikten sonra
-- kullanıcı elle siler: DROP TABLE "_Yedek_20261119_01_SevkPartisi";
-- =====================================================================================
BEGIN;

CREATE TABLE IF NOT EXISTS "_Yedek_20261119_01_SevkPartisi" (
    "Tablo"                   text NOT NULL,
    "Id"                      integer NOT NULL,
    "AktifKarsilanan"         numeric NULL,
    "ErkenSonuclandirildiMi"  boolean NULL,
    "UpdatedDate"             timestamp without time zone NULL,
    "UpdatedBy"               text NULL,
    "YedekTarihi"             timestamp without time zone NOT NULL,
    PRIMARY KEY ("Tablo", "Id")
);

INSERT INTO "_Yedek_20261119_01_SevkPartisi"
    ("Tablo", "Id", "AktifKarsilanan", "ErkenSonuclandirildiMi", "UpdatedDate", "UpdatedBy", "YedekTarihi")
SELECT 'CekiSatirlari', cs."Id", cs."AktifGridSevkKarsilananMiktari", cs."AktifGridSevkPartisiErkenSonuclandirildiMi",
       cs."UpdatedDate", cs."UpdatedBy", now() AT TIME ZONE 'Europe/Istanbul'
FROM "CekiSatirlari" cs
WHERE cs."Id" = 34371
UNION ALL
SELECT 'SandikIcerikleri', si."Id", si."AktifGridSevkKarsilananMiktari", NULL,
       si."UpdatedDate", si."UpdatedBy", now() AT TIME ZONE 'Europe/Istanbul'
FROM "SandikIcerikleri" si
WHERE si."CekiSatiriId" = 34371
ON CONFLICT ("Tablo", "Id") DO NOTHING;

DO $$
BEGIN
    RAISE NOTICE 'Yedek tablosunda % kayıt var.', (SELECT COUNT(*) FROM "_Yedek_20261119_01_SevkPartisi");
END $$;

-- Betik kaydı (README "Betik kaydı"): bu betiğin bu veritabanında çalıştığını aynı transaction içinde yazar.
CREATE TABLE IF NOT EXISTS "BetikKayitlari" (
    "BetikAdi"       text PRIMARY KEY,
    "UygulamaTarihi" timestamp without time zone NOT NULL,
    "Uygulayan"      text NOT NULL
);
INSERT INTO "BetikKayitlari" ("BetikAdi", "UygulamaTarihi", "Uygulayan")
VALUES ('20261119_01_Yedek', now() AT TIME ZONE 'Europe/Istanbul', current_user)
ON CONFLICT ("BetikAdi") DO NOTHING;

COMMIT;
