-- =====================================================================================
-- Sahada kapatılan ürün ve eski saha durumu yüzünden Eksik Sevk Edildi'de kalan projeleri Sevk Edildi yapar
-- (veri onarımı, S-27 genişlemesi, 17.11.2026).
--
-- 34 numaralı betik (20261112_01_Saha_Iptal_Proje_Sevk_Durumu.sql, canlıda 11.11.2026'da çalıştı) yalnız sahada
-- Grid İptal edilen ürünü sayıyordu. Kullanıcı kararıyla sahada Grid Kapandı yapılan ürün de ana projede Grid Kapandı
-- ürün gibi (kalan 0) kapanmış sayılır. Canlı kopyada (17.11.2026) bu yüzden PA600-12, PA600-47 ve PA613-01 Eksik Sevk
-- Edildi'de kalıyordu. Ayrıca PA600-47-SAHA'nın tek sandığı sevkli olduğu halde eski sürümün yazdığı Eksik Sevk Edildi
-- durumu onu Saha Yönetimi listesinde tutuyordu (bugünkü kod bütün sandıkları sevkli saha projesini Sevk Edildi yapar).
--
-- Adım 1 — normal projeler: 34'ün seçimiyle aynıdır, tek fark sahada Grid İptal (12) ya da Grid Kapandı (14):
--   * Normal proje, kayıtlı durumu Eksik Sevk Edildi (6), en az bir sandığı var ve bütün sandıkları Sevk Edildi (4);
--     sevkiyat düzeltmesi açık sandığı yok.
--   * Her kök çeki satırının kalanı (Grid İptal/Kapandı → 0; hatalı ürün en az 1) saha katkısıyla kapanıyor. Saha katkısı:
--     aktif (Geri Alındı/İptal olmayan) aktarım kaleminin gerçekleşen sevki (sevk kapsamındaysa en fazla aktarılan kadar
--     konulan + trafo sevk); saha satırı Grid İptal ya da Grid Kapandı ise aktarılan miktarın tamamı. Defter öncesi
--     (legacy) saha satırlarında gerçekleşen yalnız sevk edilmiş sandıktaki konulandır.
--   * En az bir kök satır yalnız sahadaki kapatma sayesinde kapanıyor.
-- Adım 2 — saha ve yedek projeler: kayıtlı durumu Eksik Sevk Edildi (6), en az bir sandığı var, bütün sandıkları
--   Sevk Edildi ve düzeltmesi açık sandığı yok.
--
-- Yalnız Projeler.DurumId (6 → 5), UpdatedDate ve UpdatedBy (= bu betiğin adı) değişir. Miktar, sandık, saha aktarımı,
-- gerçekleşen sevk tarihi değişmez. Sevk Edildi proje uygulamada kilitlenir. Hareket geçmişine yazılmaz.
-- Değişen projeler NOTICE olarak listelenir. Tekrar çalıştırılabilir: onarılacak proje kalmadıysa hiçbir şey değişmez.
-- Seçim kontrol/Betik_Durumu.sql'deki 35 satırında da aynen bulunur; biri değişirse öteki de güncellenir.
--
-- Önce geri-alma/20261117_01_Yedek.sql, sonra bu betik çalıştırılır; geri almak için geri-alma/20261117_01_Geri_Al.sql.
-- Yeni API'den sonra uygulanır: eski API sahadaki bir Grid işleminde normal projeyi yeniden Eksik Sevk Edildi yapabilir.
-- =====================================================================================
BEGIN;

CREATE TEMP TABLE saha_kapanis_aday_proje ON COMMIT DROP AS
SELECT p."Id" AS proje_id, p."ProjeNo" AS proje_no
FROM "Projeler" p
WHERE p."ProjeTipiId" = 1
  AND p."DurumId" = 6
  AND EXISTS (SELECT 1 FROM "Sandiklar" s WHERE s."ProjeId" = p."Id")
  AND NOT EXISTS (SELECT 1 FROM "Sandiklar" s
                  WHERE s."ProjeId" = p."Id" AND (s."DurumId" <> 4 OR s."SevkiyatDuzeltmeAcikMi"));

CREATE TEMP TABLE saha_kapanis_kok_satir ON COMMIT DROP AS
SELECT a.proje_id,
       cs."Id" AS satir_id,
       CASE
           WHEN cs."GridDurumuId" IN (12, 14) THEN 0
           WHEN (cs."HataliMiktar" > 0 OR cs."DurumId" = 21) AND k.ham <= 0 THEN 1
           ELSE GREATEST(k.ham, 0)
       END AS kalan
FROM saha_kapanis_aday_proje a
JOIN "Cekiler" c ON c."ProjeId" = a.proje_id
JOIN "CekiSatirlari" cs ON cs."CekiId" = c."Id"
CROSS JOIN LATERAL (
    SELECT cs."IstenenAdet" - cs."GelenMiktar" - cs."StokKarsilanan" - cs."ProjeKarsilanan" - cs."TedarikciKarsilanan"
           + cs."ProjeGonderilen" - cs."TrafoSevkAdet" AS ham
) k
WHERE cs."KaynakCekiSatiriId" IS NULL;

-- Kök satır başına saha katkısı: gerceklesen = fiziksel saha sevki; kapanis = gerçekleşen + sahada kapatılarak kapanan.
CREATE TEMP TABLE saha_kapanis_katki ON COMMIT DROP AS
SELECT x.kaynak_id,
       x.gerceklesen,
       CASE WHEN x.saha_kapali THEN GREATEST(GREATEST(x.aktarilan, 0), x.gerceklesen) ELSE x.gerceklesen END AS kapanis
FROM (
    SELECT k."KaynakCekiSatiriId" AS kaynak_id,
           k."Miktar" AS aktarilan,
           COALESCE(ss."GridDurumuId" IN (12, 14), FALSE) AS saha_kapali,
           CASE WHEN k."SevkiyatKapsamindaMi" AND k."Miktar" > 0
                THEN LEAST(GREATEST(COALESCE(ic.konulan, 0), 0) + GREATEST(COALESCE(ss."TrafoSevkAdet", 0), 0), k."Miktar")
                ELSE 0 END AS gerceklesen
    FROM "SahaAktarimKalemleri" k
    LEFT JOIN "CekiSatirlari" ss ON ss."Id" = k."SahaCekiSatiriId"
    LEFT JOIN LATERAL (SELECT SUM(si."KonulanAdet") AS konulan
                       FROM "SandikIcerikleri" si WHERE si."CekiSatiriId" = k."SahaCekiSatiriId") ic ON TRUE
    WHERE k."DurumId" NOT IN (6, 7)
      AND k."KaynakCekiSatiriId" IN (SELECT satir_id FROM saha_kapanis_kok_satir)
    UNION ALL
    SELECT cs."KaynakCekiSatiriId",
           cs."IstenenAdet",
           cs."GridDurumuId" IN (12, 14),
           CASE WHEN COALESCE(ic.sevkli, FALSE) AND cs."IstenenAdet" > 0
                THEN LEAST(GREATEST(COALESCE(ic.konulan, 0), 0) + GREATEST(cs."TrafoSevkAdet", 0), cs."IstenenAdet")
                ELSE 0 END
    FROM "CekiSatirlari" cs
    JOIN "Cekiler" c ON c."Id" = cs."CekiId"
    JOIN "Projeler" sp ON sp."Id" = c."ProjeId" AND sp."ProjeTipiId" = 2
    LEFT JOIN LATERAL (SELECT BOOL_OR(s."DurumId" = 4) AS sevkli,
                              SUM(si."KonulanAdet") FILTER (WHERE s."DurumId" = 4) AS konulan
                       FROM "SandikIcerikleri" si JOIN "Sandiklar" s ON s."Id" = si."SandikId"
                       WHERE si."CekiSatiriId" = cs."Id") ic ON TRUE
    WHERE cs."KaynakCekiSatiriId" IN (SELECT satir_id FROM saha_kapanis_kok_satir)
      AND NOT EXISTS (SELECT 1 FROM "SahaAktarimKalemleri" d WHERE d."SahaCekiSatiriId" = cs."Id")
) x;

CREATE TEMP TABLE saha_kapanis_onarilacak ON COMMIT DROP AS
-- Adım 1: normal projeler.
SELECT a.proje_id, a.proje_no, 'normal' AS tur, COUNT(*) FILTER (WHERE o.kalan > o.gerceklesen) AS kapanan_satir
FROM saha_kapanis_aday_proje a
JOIN (
    SELECT ks.proje_id, ks.satir_id, ks.kalan,
           COALESCE(SUM(kt.gerceklesen), 0) AS gerceklesen,
           COALESCE(SUM(kt.kapanis), 0) AS kapanis
    FROM saha_kapanis_kok_satir ks
    LEFT JOIN saha_kapanis_katki kt ON kt.kaynak_id = ks.satir_id
    GROUP BY ks.proje_id, ks.satir_id, ks.kalan
) o ON o.proje_id = a.proje_id
GROUP BY a.proje_id, a.proje_no
HAVING BOOL_AND(o.kalan <= o.kapanis) AND BOOL_OR(o.kalan > o.gerceklesen)
UNION ALL
-- Adım 2: bütün sandıkları sevkli saha ve yedek projeler.
SELECT p."Id", p."ProjeNo", CASE p."ProjeTipiId" WHEN 2 THEN 'saha' ELSE 'yedek' END, 0
FROM "Projeler" p
WHERE p."ProjeTipiId" IN (2, 3)
  AND p."DurumId" = 6
  AND EXISTS (SELECT 1 FROM "Sandiklar" s WHERE s."ProjeId" = p."Id")
  AND NOT EXISTS (SELECT 1 FROM "Sandiklar" s
                  WHERE s."ProjeId" = p."Id" AND (s."DurumId" <> 4 OR s."SevkiyatDuzeltmeAcikMi"));

UPDATE "Projeler" p
SET "DurumId" = 5,
    "UpdatedDate" = now() AT TIME ZONE 'Europe/Istanbul',
    "UpdatedBy" = '20261117_01_Saha_Kapanis_Proje_Sevk_Durumu.sql'
FROM saha_kapanis_onarilacak o
WHERE p."Id" = o.proje_id AND p."DurumId" = 6;

DO $$
DECLARE
    satir record;
    adet integer := 0;
BEGIN
    FOR satir IN SELECT proje_id, proje_no, tur, kapanan_satir FROM saha_kapanis_onarilacak ORDER BY tur, proje_no LOOP
        IF satir.tur = 'normal' THEN
            RAISE NOTICE 'Sevk Edildi yapıldı: % (Id %), sahada kapatılarak kapanan satır: %',
                satir.proje_no, satir.proje_id, satir.kapanan_satir;
        ELSE
            RAISE NOTICE 'Sevk Edildi yapıldı (% projesi, bütün sandıkları sevkli): % (Id %)',
                satir.tur, satir.proje_no, satir.proje_id;
        END IF;
        adet := adet + 1;
    END LOOP;
    RAISE NOTICE 'Toplam % proje Eksik Sevk Edildi -> Sevk Edildi.', adet;
END $$;

-- Betik kaydı (README "Betik kaydı"): bu betiğin bu veritabanında çalıştığını aynı transaction içinde yazar.
CREATE TABLE IF NOT EXISTS "BetikKayitlari" (
    "BetikAdi"       text PRIMARY KEY,
    "UygulamaTarihi" timestamp without time zone NOT NULL,
    "Uygulayan"      text NOT NULL
);
INSERT INTO "BetikKayitlari" ("BetikAdi", "UygulamaTarihi", "Uygulayan")
VALUES ('20261117_01_Saha_Kapanis_Proje_Sevk_Durumu', now() AT TIME ZONE 'Europe/Istanbul', current_user)
ON CONFLICT ("BetikAdi") DO NOTHING;

COMMIT;
