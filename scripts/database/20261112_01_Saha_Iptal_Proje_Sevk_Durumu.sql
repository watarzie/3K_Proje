-- =====================================================================================
-- Sahada iptal edilen ürün yüzünden Eksik Sevk Edildi'de kalan normal projeleri Sevk Edildi yapar (veri onarımı, S-27).
--
-- Vaka (11.11.2026, PA675-01 ve PA677-03): bütün normal sandıklar sevk edilmiş, projenin tek açık ürünü sahaya
-- aktarılmış ve sahada Grid İptal edilmişti (PA675-01'de ürün sahadan sahaya PA675-03-SAHA'ya taşınmış, ilk saha
-- silinmişti). Sevk durumu sahadan yalnız konulan adedi saydığı için proje Eksik Sevk Edildi kalıyor ve Sandık Yönetimi
-- listesinden çıkmıyordu. Yeni API bu ürünü ana projede iptal edilmiş ürün gibi kapanmış sayar; ama projenin kayıtlı
-- durumu yalnız bir işlem senkron tetiklediğinde yeniden hesaplanır. Bu betik bugün bu durumda olan projeleri bir kez
-- yeniden hesaplar.
--
-- Seçim, yeni API'nin kuralıyla aynıdır (SahaTamamlamaService.SenkronizeKaynakProjelerAsync + GetSahaIptalKapananMapAsync):
--   * Normal proje, kayıtlı durumu Eksik Sevk Edildi (6), en az bir sandığı var ve bütün sandıkları Sevk Edildi (4);
--     sevkiyat düzeltmesi açık sandığı yok.
--   * Her kök çeki satırının kalanı (Grid İptal/Kapandı → 0; hatalı ürün en az 1) saha katkısıyla kapanıyor.
--     Saha katkısı: aktif (Geri Alındı/İptal olmayan) aktarım kaleminin gerçekleşen sevki
--     (sevk kapsamındaysa en fazla aktarılan kadar konulan + trafo sevk); saha satırı Grid İptal ise aktarılan miktarın
--     tamamı. Defter öncesi (legacy) saha satırlarında gerçekleşen yalnız sevk edilmiş sandıktaki konulandır.
--   * En az bir kök satır yalnız saha iptali sayesinde kapanıyor (iptal olmasa açık kalırdı). Başka nedenle tutarsız
--     görünen projelere dokunulmaz.
-- Saha sandığının sevk edilmiş olması aranmaz: iptal edilen ürün hiç gönderilmeyecektir. Yalnız Grid İptal (12) sayılır;
-- sahada Grid Kapandı (14) olan satır bu kuralla kapanmaz.
-- Seçim kontrol/Betik_Durumu.sql'deki 34 satırında da aynen bulunur; biri değişirse öteki de güncellenir.
-- Bilinçli fark: defter öncesi saha satırı, herhangi bir defter kaydına bağlıysa dışlanır (API yalnız aynı köke bağlı
-- kaydı arar); bozuk defterde betik onarmaz, yanlış onarım da yapmaz.
--
-- Yalnız Projeler.DurumId (6 → 5), UpdatedDate ve UpdatedBy (= bu betiğin adı) değişir. Miktar, sandık, saha aktarımı,
-- gerçekleşen sevk tarihi değişmez. Sevk Edildi proje uygulamada kilitlenir (sevk edilmiş her proje gibi).
-- Hareket geçmişine yazılmaz (kullanıcı yok). Değişen projeler NOTICE olarak listelenir.
-- Tekrar çalıştırılabilir: onarılacak proje kalmadıysa hiçbir şey değişmez.
--
-- Geri alma: projenin "Proje kilidi aç" işlemi ya da sahadaki iptalin kaldırılması durumu yeniden hesaplar. Gerekirse
-- NOTICE'ta listelenen kimlikler için: UPDATE "Projeler" SET "DurumId" = 6 WHERE "Id" IN (...) AND "DurumId" = 5;
-- Önce test veritabanında, sonra canlıda 3K_Db düğümünden açılan sorgu penceresinde çalıştırın.
-- =====================================================================================
BEGIN;

CREATE TEMP TABLE saha_iptal_aday_proje ON COMMIT DROP AS
SELECT p."Id" AS proje_id, p."ProjeNo" AS proje_no
FROM "Projeler" p
WHERE p."ProjeTipiId" = 1
  AND p."DurumId" = 6
  AND EXISTS (SELECT 1 FROM "Sandiklar" s WHERE s."ProjeId" = p."Id")
  AND NOT EXISTS (SELECT 1 FROM "Sandiklar" s
                  WHERE s."ProjeId" = p."Id" AND (s."DurumId" <> 4 OR s."SevkiyatDuzeltmeAcikMi"));

CREATE TEMP TABLE saha_iptal_kok_satir ON COMMIT DROP AS
SELECT a.proje_id,
       cs."Id" AS satir_id,
       CASE
           WHEN cs."GridDurumuId" IN (12, 14) THEN 0
           WHEN (cs."HataliMiktar" > 0 OR cs."DurumId" = 21) AND k.ham <= 0 THEN 1
           ELSE GREATEST(k.ham, 0)
       END AS kalan
FROM saha_iptal_aday_proje a
JOIN "Cekiler" c ON c."ProjeId" = a.proje_id
JOIN "CekiSatirlari" cs ON cs."CekiId" = c."Id"
CROSS JOIN LATERAL (
    SELECT cs."IstenenAdet" - cs."GelenMiktar" - cs."StokKarsilanan" - cs."ProjeKarsilanan" - cs."TedarikciKarsilanan"
           + cs."ProjeGonderilen" - cs."TrafoSevkAdet" AS ham
) k
WHERE cs."KaynakCekiSatiriId" IS NULL;

-- Kök satır başına saha katkısı: gerceklesen = fiziksel saha sevki; kapanis = gerçekleşen + iptalle kapanan.
CREATE TEMP TABLE saha_iptal_katki ON COMMIT DROP AS
SELECT x.kaynak_id,
       x.gerceklesen,
       CASE WHEN x.saha_iptal THEN GREATEST(GREATEST(x.aktarilan, 0), x.gerceklesen) ELSE x.gerceklesen END AS kapanis
FROM (
    SELECT k."KaynakCekiSatiriId" AS kaynak_id,
           k."Miktar" AS aktarilan,
           COALESCE(ss."GridDurumuId" = 12, FALSE) AS saha_iptal,
           CASE WHEN k."SevkiyatKapsamindaMi" AND k."Miktar" > 0
                THEN LEAST(GREATEST(COALESCE(ic.konulan, 0), 0) + GREATEST(COALESCE(ss."TrafoSevkAdet", 0), 0), k."Miktar")
                ELSE 0 END AS gerceklesen
    FROM "SahaAktarimKalemleri" k
    LEFT JOIN "CekiSatirlari" ss ON ss."Id" = k."SahaCekiSatiriId"
    LEFT JOIN LATERAL (SELECT SUM(si."KonulanAdet") AS konulan
                       FROM "SandikIcerikleri" si WHERE si."CekiSatiriId" = k."SahaCekiSatiriId") ic ON TRUE
    WHERE k."DurumId" NOT IN (6, 7)
      AND k."KaynakCekiSatiriId" IN (SELECT satir_id FROM saha_iptal_kok_satir)
    UNION ALL
    SELECT cs."KaynakCekiSatiriId",
           cs."IstenenAdet",
           cs."GridDurumuId" = 12,
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
    WHERE cs."KaynakCekiSatiriId" IN (SELECT satir_id FROM saha_iptal_kok_satir)
      AND NOT EXISTS (SELECT 1 FROM "SahaAktarimKalemleri" d WHERE d."SahaCekiSatiriId" = cs."Id")
) x;

CREATE TEMP TABLE saha_iptal_onarilacak ON COMMIT DROP AS
SELECT a.proje_id, a.proje_no, COUNT(*) FILTER (WHERE o.kalan > o.gerceklesen) AS iptalle_kapanan_satir
FROM saha_iptal_aday_proje a
JOIN (
    SELECT ks.proje_id, ks.satir_id, ks.kalan,
           COALESCE(SUM(kt.gerceklesen), 0) AS gerceklesen,
           COALESCE(SUM(kt.kapanis), 0) AS kapanis
    FROM saha_iptal_kok_satir ks
    LEFT JOIN saha_iptal_katki kt ON kt.kaynak_id = ks.satir_id
    GROUP BY ks.proje_id, ks.satir_id, ks.kalan
) o ON o.proje_id = a.proje_id
GROUP BY a.proje_id, a.proje_no
HAVING BOOL_AND(o.kalan <= o.kapanis) AND BOOL_OR(o.kalan > o.gerceklesen);

UPDATE "Projeler" p
SET "DurumId" = 5,
    "UpdatedDate" = now() AT TIME ZONE 'Europe/Istanbul',
    "UpdatedBy" = '20261112_01_Saha_Iptal_Proje_Sevk_Durumu.sql'
FROM saha_iptal_onarilacak o
WHERE p."Id" = o.proje_id AND p."DurumId" = 6;

DO $$
DECLARE
    satir record;
    adet integer := 0;
BEGIN
    FOR satir IN SELECT proje_id, proje_no, iptalle_kapanan_satir FROM saha_iptal_onarilacak ORDER BY proje_no LOOP
        RAISE NOTICE 'Sevk Edildi yapıldı: % (Id %), sahada iptalle kapanan satır: %',
            satir.proje_no, satir.proje_id, satir.iptalle_kapanan_satir;
        adet := adet + 1;
    END LOOP;
    RAISE NOTICE 'Toplam % proje Eksik Sevk Edildi -> Sevk Edildi.', adet;
END $$;

COMMIT;
