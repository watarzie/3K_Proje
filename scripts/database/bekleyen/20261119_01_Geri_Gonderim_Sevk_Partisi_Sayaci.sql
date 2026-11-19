-- =====================================================================================
-- Sayaçları boş kalmış eski geri gönderimi bugünkü biçime getirir; Grid yeniden sevki açılır
-- (veri onarımı, 19.11.2026).
--
-- Canlı PA558-01, sandık 14, satır 58 "Basınç ventili borusu" (CekiSatirlari.Id 34371): 15.09.2026'da 1 adet Tadilat
-- için geri gönderildi. Geri gönderim, aktif Grid sevk partisi sayaçları canlıya girmeden (16.09.2026) bir gün önce
-- yapıldığı için AktifGridSevkKarsilananMiktari ve AktifGridSevkPartisiErkenSonuclandirildiMi boş kaldı. Uygulama bu
-- kaydı "belirsiz eski parti" sayar (G-037, U-244) ve Grid'in yeniden sevkini "3K tarafında işlem yapılmış" diye
-- kilitler. Bugünkü kod geri gönderimde sayaca 0, bayrağa true yazar (aktif parti iade edildi, sonuçlandı).
--
-- Seçim yalnız Id 34371'dir ve kayıt hâlâ aynı durumdaysa değişir: 3K durumu Geri Gönderildi (12), Grid sevk durumu
-- Yeniden Sevk Gerekli (4), yeniden sevk borcu ve aktif sevk partisi var, iki satır alanı ve bütün sandık içeriği
-- sayaçları boş, ürünün hiçbir sandığı sevk edilmemiş. Canlı kopyada (19.11.2026) 3K'da bu partiden adet kalmadığı
-- (Gelen 0, sandık Grid payı 0) doğrulandı; sayaç bu yüzden 0'dır. Aynı desendeki PA599-01 satır 109 (Id 7626) bilerek dışarıda bırakıldı: sandığı sevk edilmiş, ayrı karar.
--
-- Değişen: CekiSatirlari.AktifGridSevkKarsilananMiktari = 0, AktifGridSevkPartisiErkenSonuclandirildiMi = true;
-- satırın SandikIcerikleri.AktifGridSevkKarsilananMiktari = 0; ikisinde UpdatedDate ve UpdatedBy (= betik adı).
-- Miktarlar (Gelen, Konulan, Geri Gönderilen, Yeniden Sevk Gerekli, Grid Sevk), durumlar ve sandık değişmez.
-- Hareket geçmişine yazılmaz. Değişen kayıt NOTICE olarak yazılır.
-- Tekrar çalıştırılabilir: kayıt onarıldıysa ya da durumu değiştiyse hiçbir şey değişmez.
-- Seçim kontrol/Betik_Durumu.sql'deki 36 satırında da aynen bulunur; biri değişirse öteki de güncellenir.
--
-- Önce geri-alma/20261119_01_Yedek.sql, sonra bu betik çalıştırılır; geri almak için geri-alma/20261119_01_Geri_Al.sql.
-- API sürümünden bağımsızdır (şema değişikliği yok).
-- =====================================================================================
BEGIN;

CREATE TEMP TABLE geri_gonderim_onarilacak ON COMMIT DROP AS
SELECT cs."Id" AS satir_id, cs."SiraNo" AS sira_no, p."ProjeNo" AS proje_no
FROM "CekiSatirlari" cs
JOIN "Cekiler" c ON c."Id" = cs."CekiId"
JOIN "Projeler" p ON p."Id" = c."ProjeId"
WHERE cs."Id" = 34371
  AND cs."UcKDurumuId" = 12
  AND cs."GridSevkDurumuId" = 4
  AND cs."YenidenSevkGerekliAdet" > 0
  AND COALESCE(cs."GridSevkMiktari", 0) > 0
  AND cs."AktifGridSevkKarsilananMiktari" IS NULL
  AND cs."AktifGridSevkPartisiErkenSonuclandirildiMi" IS NULL
  AND NOT EXISTS (SELECT 1 FROM "SandikIcerikleri" si
                  WHERE si."CekiSatiriId" = cs."Id" AND si."AktifGridSevkKarsilananMiktari" IS NOT NULL)
  AND NOT EXISTS (SELECT 1 FROM "SandikIcerikleri" si JOIN "Sandiklar" s ON s."Id" = si."SandikId"
                  WHERE si."CekiSatiriId" = cs."Id" AND s."DurumId" = 4);

UPDATE "SandikIcerikleri" si
SET "AktifGridSevkKarsilananMiktari" = 0,
    "UpdatedDate" = now() AT TIME ZONE 'Europe/Istanbul',
    "UpdatedBy" = '20261119_01_Geri_Gonderim_Sevk_Partisi_Sayaci.sql'
FROM geri_gonderim_onarilacak o
WHERE si."CekiSatiriId" = o.satir_id;

UPDATE "CekiSatirlari" cs
SET "AktifGridSevkKarsilananMiktari" = 0,
    "AktifGridSevkPartisiErkenSonuclandirildiMi" = TRUE,
    "UpdatedDate" = now() AT TIME ZONE 'Europe/Istanbul',
    "UpdatedBy" = '20261119_01_Geri_Gonderim_Sevk_Partisi_Sayaci.sql'
FROM geri_gonderim_onarilacak o
WHERE cs."Id" = o.satir_id;

DO $$
DECLARE
    satir record;
BEGIN
    FOR satir IN SELECT satir_id, sira_no, proje_no FROM geri_gonderim_onarilacak LOOP
        RAISE NOTICE 'Sayaçlar onarıldı, Grid yeniden sevki açıldı: % satır % (Id %)',
            satir.proje_no, satir.sira_no, satir.satir_id;
    END LOOP;
    RAISE NOTICE 'Toplam % satır onarıldı.', (SELECT COUNT(*) FROM geri_gonderim_onarilacak);
END $$;

-- Betik kaydı (README "Betik kaydı"): bu betiğin bu veritabanında çalıştığını aynı transaction içinde yazar.
CREATE TABLE IF NOT EXISTS "BetikKayitlari" (
    "BetikAdi"       text PRIMARY KEY,
    "UygulamaTarihi" timestamp without time zone NOT NULL,
    "Uygulayan"      text NOT NULL
);
INSERT INTO "BetikKayitlari" ("BetikAdi", "UygulamaTarihi", "Uygulayan")
VALUES ('20261119_01_Geri_Gonderim_Sevk_Partisi_Sayaci', now() AT TIME ZONE 'Europe/Istanbul', current_user)
ON CONFLICT ("BetikAdi") DO NOTHING;

COMMIT;
