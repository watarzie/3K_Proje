-- =====================================================================================
-- 34 numaralı betikten (20261112_01_Saha_Iptal_Proje_Sevk_Durumu.sql) ÖNCE çalıştırılır: dokunabileceği projelerin
-- durumunu yedekler.
--
-- Onarım betiği yalnız Eksik Sevk Edildi (6) durumundaki normal projelerin DurumId, UpdatedDate ve UpdatedBy alanlarını
-- değiştirir. Bu betik o kümenin tamamını (seçimden bağımsız, daha geniş küme) "_Yedek_20261112_01_ProjeDurum"
-- tablosuna yazar. Başka hiçbir tabloya dokunmaz.
--
-- Tekrar çalıştırılabilir: tablo varsa yeniden oluşturulmaz; zaten yedeklenmiş projenin ilk kaydı korunur
-- (onarımdan sonra tekrar çalıştırılsa da ilk, onarım öncesi değer kalır).
-- Geri alma: geri-alma/20261112_01_Geri_Al.sql. Yedek tablo uygulama tarafından kullanılmaz; doğrulama bittikten sonra
-- kullanıcı elle siler: DROP TABLE "_Yedek_20261112_01_ProjeDurum";
-- =====================================================================================
BEGIN;

CREATE TABLE IF NOT EXISTS "_Yedek_20261112_01_ProjeDurum" (
    "ProjeId"     integer PRIMARY KEY,
    "ProjeNo"     text NOT NULL,
    "DurumId"     integer NOT NULL,
    "UpdatedDate" timestamp without time zone NULL,
    "UpdatedBy"   text NULL,
    "YedekTarihi" timestamp without time zone NOT NULL
);

INSERT INTO "_Yedek_20261112_01_ProjeDurum" ("ProjeId", "ProjeNo", "DurumId", "UpdatedDate", "UpdatedBy", "YedekTarihi")
SELECT p."Id", p."ProjeNo", p."DurumId", p."UpdatedDate", p."UpdatedBy", now() AT TIME ZONE 'Europe/Istanbul'
FROM "Projeler" p
WHERE p."ProjeTipiId" = 1 AND p."DurumId" = 6
ON CONFLICT ("ProjeId") DO NOTHING;

DO $$
BEGIN
    RAISE NOTICE 'Yedek tablosunda % proje var.', (SELECT COUNT(*) FROM "_Yedek_20261112_01_ProjeDurum");
END $$;

-- Betik kaydı (README "Betik kaydı"): bu betiğin bu veritabanında çalıştığını aynı transaction içinde yazar.
CREATE TABLE IF NOT EXISTS "BetikKayitlari" (
    "BetikAdi"       text PRIMARY KEY,
    "UygulamaTarihi" timestamp without time zone NOT NULL,
    "Uygulayan"      text NOT NULL
);
INSERT INTO "BetikKayitlari" ("BetikAdi", "UygulamaTarihi", "Uygulayan")
VALUES ('20261112_01_Yedek', now() AT TIME ZONE 'Europe/Istanbul', current_user)
ON CONFLICT ("BetikAdi") DO NOTHING;

COMMIT;
