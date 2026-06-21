-- Saha eksik tamamlama senkronizasyonu
-- Amaç:
--   Normal projelerdeki kalan hesabını, sadece sevk edilmiş Saha tamamlama satırlarını düşerek
--   proje durumuna yansıtmak.
--
-- Kural:
--   Saha projesi oluşturuldu diye ana proje kapanmaz.
--   Kaynak satıra bağlı Saha satırı, sevk edilmiş bir sandığın içindeyse ana projedeki eksikten düşer.

WITH kaynak_satir AS (
    SELECT
        cs."Id",
        c."ProjeId",
        CASE
            WHEN cs."GridDurumuId" IN (12, 14) THEN 0::numeric
            WHEN (cs."HataliMiktar" > 0 OR cs."DurumId" = 21)
                 AND (
                    cs."IstenenAdet"
                    - cs."GelenMiktar"
                    - cs."StokKarsilanan"
                    - cs."ProjeKarsilanan"
                    - cs."TedarikciKarsilanan"
                    + cs."ProjeGonderilen"
                    - cs."TrafoSevkAdet"
                 ) <= 0 THEN 1::numeric
            ELSE GREATEST(
                cs."IstenenAdet"
                - cs."GelenMiktar"
                - cs."StokKarsilanan"
                - cs."ProjeKarsilanan"
                - cs."TedarikciKarsilanan"
                + cs."ProjeGonderilen"
                - cs."TrafoSevkAdet",
                0::numeric
            )
        END AS ham_kalan
    FROM public."CekiSatirlari" cs
    JOIN public."Cekiler" c ON c."Id" = cs."CekiId"
    JOIN public."Projeler" p ON p."Id" = c."ProjeId"
    WHERE p."ProjeTipiId" = 1
      AND cs."KaynakCekiSatiriId" IS NULL
),
saha_sevk_tamamlama AS (
    SELECT
        t."KaynakCekiSatiriId" AS kaynak_ceki_satiri_id,
        SUM(t."IstenenAdet") AS tamamlanan_adet
    FROM public."CekiSatirlari" t
    JOIN public."Cekiler" tc ON tc."Id" = t."CekiId"
    JOIN public."Projeler" tp ON tp."Id" = tc."ProjeId"
    JOIN public."SandikIcerikleri" si ON si."CekiSatiriId" = t."Id"
    JOIN public."Sandiklar" s ON s."Id" = si."SandikId"
    WHERE t."KaynakCekiSatiriId" IS NOT NULL
      AND tp."ProjeTipiId" = 2
      AND s."DurumId" = 4
    GROUP BY t."KaynakCekiSatiriId"
),
proje_kalan AS (
    SELECT
        ks."ProjeId",
        COUNT(*) AS toplam_satir,
        SUM(
            CASE
                WHEN GREATEST(ks.ham_kalan - COALESCE(st.tamamlanan_adet, 0), 0) > 0
                    THEN 1
                ELSE 0
            END
        ) AS eksik_satir,
        BOOL_OR(COALESCE(st.tamamlanan_adet, 0) > 0) AS saha_sevkiyle_tamamlama_var
    FROM kaynak_satir ks
    LEFT JOIN saha_sevk_tamamlama st ON st.kaynak_ceki_satiri_id = ks."Id"
    GROUP BY ks."ProjeId"
),
sandik_ozet AS (
    SELECT
        s."ProjeId",
        COUNT(*) AS toplam_sandik,
        COUNT(*) FILTER (WHERE s."DurumId" = 4) AS sevk_edilen_sandik,
        COUNT(*) FILTER (WHERE s."DurumId" IN (3, 4)) AS hazir_sandik
    FROM public."Sandiklar" s
    GROUP BY s."ProjeId"
),
yeni_durum AS (
    SELECT
        p."Id",
        CASE
            WHEN pk.eksik_satir = 0
                 AND (
                    COALESCE(so.sevk_edilen_sandik, 0) > 0
                    OR pk.saha_sevkiyle_tamamlama_var
                    OR p."DurumId" IN (5, 6)
                 ) THEN 5
            WHEN pk.eksik_satir = 0
                 AND COALESCE(so.toplam_sandik, 0) > 0
                 AND COALESCE(so.hazir_sandik, 0) = COALESCE(so.toplam_sandik, 0) THEN 3
            WHEN pk.eksik_satir > 0
                 AND p."DurumId" IN (5, 6)
                 AND COALESCE(so.sevk_edilen_sandik, 0) > 0 THEN 6
            WHEN pk.eksik_satir > 0
                 AND p."DurumId" IN (5, 6)
                 AND COALESCE(so.sevk_edilen_sandik, 0) = 0 THEN 1
            ELSE p."DurumId"
        END AS durum_id
    FROM public."Projeler" p
    JOIN proje_kalan pk ON pk."ProjeId" = p."Id"
    LEFT JOIN sandik_ozet so ON so."ProjeId" = p."Id"
    WHERE p."ProjeTipiId" = 1
)
UPDATE public."Projeler" p
SET
    "DurumId" = yd.durum_id,
    "UpdatedDate" = NOW()
FROM yeni_durum yd
WHERE yd."Id" = p."Id"
  AND p."DurumId" IS DISTINCT FROM yd.durum_id
RETURNING p."Id", p."ProjeNo", p."DurumId";
