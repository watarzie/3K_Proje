/*
  Normal proje durumunu, sandik-bazli saha sevki tamamlanmis tarihsel kayitlar
  icin bir defaya mahsus onarir.

  Varsayilan calisma DRY-RUN'dir ve veri degistirmez.

  Kullanim:
    1. Dosyayi oldugu gibi calistirip "aday" sonucunu kontrol edin.
    2. Yalnizca beklenen projeler listeleniyorsa asagidaki ayarda apply=false
       degerini apply=true yapip dosyanin TAMAMINI yeniden calistirin.
    3. Tek proje ile sinirlamak icin target_proje_no alanina orn. 'PA600-36'
       yazin. NULL tum projeleri tarar.

  Degistirilmeyen veriler:
    - Sandik.DurumId (fiziksel sevk gercegi)
    - Sevkiyat / SevkiyatSandik gecmisi
    - SahaAktarimKalemi ve urun-bazli (AktarimTipiId=1) akislar
    - GerceklesenSevkTarihi

  Enum sabitleri:
    ProjeTipi.Normal=1, ProjeDurum.SevkEdildi=5,
    SandikDurum.Sevkedildi=4, SahaAktarimTipi.SandikBazli=2,
    SahaAktarimDurum.SevkiyatDuzeltmede=4 / SevkEdildi=5 /
    GeriAlindi=6 / Iptal=7, GridDurum.Iptal=12 / GridKapandi=14,
    UrunDurum.HataliUyumsuzGonderim=21.
*/

BEGIN;

SET LOCAL lock_timeout = '5s';
SET LOCAL statement_timeout = '2min';

CREATE TEMP TABLE _saha_sevk_durum_onarim_ayar
(
    apply             boolean NOT NULL,
    target_proje_no   text NULL
) ON COMMIT DROP;

-- SADECE BU SATIRDAKI false DEGERINI true YAPARAK UYGULAYIN.
INSERT INTO _saha_sevk_durum_onarim_ayar (apply, target_proje_no)
VALUES (false, NULL);

-- Apply modunda hesap ile update arasinda ilgili kayitlarin degismesini engeller.
-- Kilit 5 saniyede alinamazsa script hata verip tum transaction'i geri alir.
DO $lock$
BEGIN
    IF (SELECT a.apply FROM _saha_sevk_durum_onarim_ayar a) THEN
        PERFORM pg_advisory_xact_lock(hashtext('3k:saha-sevk-durum-onarimi'));

        LOCK TABLE
            "CekiSatirlari",
            "Cekiler",
            "SahaAktarimKalemleri",
            "SandikIcerikleri",
            "Sandiklar"
        IN SHARE MODE;

        LOCK TABLE "Projeler" IN SHARE ROW EXCLUSIVE MODE;
    END IF;
END
$lock$;

CREATE TEMP TABLE _saha_sevk_durum_onarim_adaylari
ON COMMIT DROP
AS
WITH aktif_sandik_bazli_kalemler AS
(
    SELECT
        k."Id"                           AS kalem_id,
        k."KaynakSandikId"               AS kaynak_sandik_id,
        k."KaynakCekiSatiriId"           AS kaynak_ceki_satiri_id,
        k."Miktar"                       AS miktar,
        k."DurumId"                      AS durum_id,
        (
            ks."Id" IS NOT NULL
            AND kp."Id" IS NOT NULL
            AND ks."ProjeId" = k."KaynakProjeId"
            AND kp."ProjeTipiId" = 1
            AND ss."Id" IS NOT NULL
            AND sp."Id" IS NOT NULL
            AND ss."ProjeId" = k."SahaProjeId"
            AND sp."ProjeTipiId" = 2
            AND ss."DurumId" = 4
        )                                 AS referanslar_tutarli
    FROM "SahaAktarimKalemleri" k
    LEFT JOIN "Sandiklar" ks
        ON ks."Id" = k."KaynakSandikId"
    LEFT JOIN "Projeler" kp
        ON kp."Id" = k."KaynakProjeId"
    LEFT JOIN "Sandiklar" ss
        ON ss."Id" = k."SahaSandikId"
    LEFT JOIN "Projeler" sp
        ON sp."Id" = k."SahaProjeId"
    WHERE k."KaynakSandikId" IS NOT NULL
      AND k."AktarimTipiId" = 2
      AND k."DurumId" NOT IN (6, 7)
),
aktif_sandik_ozeti AS
(
    SELECT
        a.kaynak_sandik_id,
        count(*)                                                AS aktif_kalem_sayisi,
        bool_and(a.referanslar_tutarli)                          AS tum_referanslar_tutarli,
        bool_and(a.durum_id IN (4, 5))                           AS tum_kalemler_saha_uzerinden_sevk_edilmis
    FROM aktif_sandik_bazli_kalemler a
    GROUP BY a.kaynak_sandik_id
),
aktif_aktarim_miktarlari AS
(
    SELECT
        a.kaynak_sandik_id,
        a.kaynak_ceki_satiri_id,
        sum(a.miktar) AS aktif_aktarim_miktari
    FROM aktif_sandik_bazli_kalemler a
    GROUP BY a.kaynak_sandik_id, a.kaynak_ceki_satiri_id
),
kaynak_satir_ham_kalanlari AS
(
    SELECT
        si."SandikId" AS kaynak_sandik_id,
        cs."Id"       AS kaynak_ceki_satiri_id,
        CASE
            WHEN cs."GridDurumuId" IN (12, 14) THEN 0::numeric
            WHEN
                (cs."HataliMiktar" > 0 OR cs."DurumId" = 21)
                AND
                (
                    cs."IstenenAdet"
                    - cs."GelenMiktar"
                    - cs."StokKarsilanan"
                    - cs."ProjeKarsilanan"
                    - cs."TedarikciKarsilanan"
                    + cs."ProjeGonderilen"
                    - cs."TrafoSevkAdet"
                ) <= 0
                THEN 1::numeric
            ELSE greatest(
                cs."IstenenAdet"
                - cs."GelenMiktar"
                - cs."StokKarsilanan"
                - cs."ProjeKarsilanan"
                - cs."TedarikciKarsilanan"
                + cs."ProjeGonderilen"
                - cs."TrafoSevkAdet",
                0::numeric)
        END AS ham_kalan
    FROM "SandikIcerikleri" si
    INNER JOIN "CekiSatirlari" cs
        ON cs."Id" = si."CekiSatiriId"
    INNER JOIN
    (
        SELECT DISTINCT a.kaynak_sandik_id
        FROM aktif_sandik_bazli_kalemler a
    ) aktif_sandik
        ON aktif_sandik.kaynak_sandik_id = si."SandikId"
    WHERE si."CekiSatiriId" IS NOT NULL
      AND cs."KaynakCekiSatiriId" IS NULL
),
aktarilmasi_gereken_miktarlar AS
(
    SELECT
        h.kaynak_sandik_id,
        h.kaynak_ceki_satiri_id,
        max(h.ham_kalan) AS gereken_miktar
    FROM kaynak_satir_ham_kalanlari h
    GROUP BY h.kaynak_sandik_id, h.kaynak_ceki_satiri_id
    HAVING max(h.ham_kalan) > 0
),
saha_uzerinden_sevk_edilen_kaynak_sandiklar AS
(
    SELECT o.kaynak_sandik_id
    FROM aktif_sandik_ozeti o
    WHERE o.aktif_kalem_sayisi > 0
      AND o.tum_referanslar_tutarli
      AND o.tum_kalemler_saha_uzerinden_sevk_edilmis
      -- Bos/anlamsiz bir defter grubu sandigi tamamlanmis saymasin.
      AND EXISTS
      (
          SELECT 1
          FROM aktarilmasi_gereken_miktarlar g
          WHERE g.kaynak_sandik_id = o.kaynak_sandik_id
      )
      -- Kaynak sandiktaki her acik satir, ayni kaynak sandik kimligiyle
      -- aktif sandik-bazli defterde yeterli miktarda temsil edilmelidir.
      AND NOT EXISTS
      (
          SELECT 1
          FROM aktarilmasi_gereken_miktarlar g
          LEFT JOIN aktif_aktarim_miktarlari m
            ON m.kaynak_sandik_id = g.kaynak_sandik_id
           AND m.kaynak_ceki_satiri_id = g.kaynak_ceki_satiri_id
          WHERE g.kaynak_sandik_id = o.kaynak_sandik_id
            AND coalesce(m.aktif_aktarim_miktari, 0) < g.gereken_miktar
      )
),
proje_sandik_ozeti AS
(
    SELECT
        p."Id"                                                AS proje_id,
        p."ProjeNo"                                           AS proje_no,
        p."DurumId"                                           AS eski_durum_id,
        count(s."Id")::integer                                AS toplam_sandik,
        count(*) FILTER (WHERE s."DurumId" = 4)::integer      AS fiziksel_sevk_edilen,
        count(*) FILTER (WHERE saha.kaynak_sandik_id IS NOT NULL)::integer
                                                                AS saha_uzerinden_sevk_edilen,
        count(*) FILTER
        (
            WHERE s."DurumId" <> 4
              AND saha.kaynak_sandik_id IS NOT NULL
        )::integer                                             AS saha_ile_tamamlanan_fiziksel_olmayan,
        count(*) FILTER
        (
            WHERE s."DurumId" = 4
               OR saha.kaynak_sandik_id IS NOT NULL
        )::integer                                             AS etkin_sevk_edilen
    FROM "Projeler" p
    INNER JOIN "Sandiklar" s
        ON s."ProjeId" = p."Id"
    LEFT JOIN saha_uzerinden_sevk_edilen_kaynak_sandiklar saha
        ON saha.kaynak_sandik_id = s."Id"
    CROSS JOIN _saha_sevk_durum_onarim_ayar ayar
    WHERE p."ProjeTipiId" = 1
      AND (ayar.target_proje_no IS NULL OR p."ProjeNo" = ayar.target_proje_no)
    GROUP BY p."Id", p."ProjeNo", p."DurumId"
)
SELECT
    o.proje_id,
    o.proje_no,
    o.eski_durum_id,
    5::integer AS yeni_durum_id,
    o.toplam_sandik,
    o.fiziksel_sevk_edilen,
    o.saha_uzerinden_sevk_edilen,
    o.saha_ile_tamamlanan_fiziksel_olmayan,
    o.etkin_sevk_edilen
FROM proje_sandik_ozeti o
WHERE o.toplam_sandik > 0
  -- Bu script fiziksel-sevk akisinin genel bir onarimi degildir.
  -- En az bir sandik saha uzerinden sevk edilmis olmalidir.
  -- En az bir fiziksel olarak sevk edilmemis kaynak sandik, saha sevki ile
  -- kapanmis olmali. Boylece tamamen fiziksel sevkli ama baska nedenle kismi
  -- kalan projeler bu dar tarihsel onarimin kapsamina girmez.
  AND o.saha_ile_tamamlanan_fiziksel_olmayan > 0
  AND o.etkin_sevk_edilen = o.toplam_sandik
  AND o.eski_durum_id <> 5;

-- DRY-RUN ONIZLEME
SELECT
    ayar.apply,
    ayar.target_proje_no,
    count(aday.proje_id)::integer AS aday_proje_sayisi
FROM _saha_sevk_durum_onarim_ayar ayar
LEFT JOIN _saha_sevk_durum_onarim_adaylari aday ON true
GROUP BY ayar.apply, ayar.target_proje_no;

SELECT
    proje_id,
    proje_no,
    eski_durum_id,
    yeni_durum_id,
    toplam_sandik,
    fiziksel_sevk_edilen,
    saha_uzerinden_sevk_edilen,
    saha_ile_tamamlanan_fiziksel_olmayan,
    etkin_sevk_edilen
FROM _saha_sevk_durum_onarim_adaylari
ORDER BY proje_no, proje_id;

-- apply=false iken UPDATE kosulu saglanmaz; sifir satir degisir.
CREATE TEMP TABLE _saha_sevk_durum_onarim_guncellenen
ON COMMIT DROP
AS
WITH guncellenen AS
(
    UPDATE "Projeler" p
       SET "DurumId" = 5,
           "UpdatedDate" = timezone('Europe/Istanbul', clock_timestamp()),
           "UpdatedBy" = 'one-time:saha-sevk-durum-onarimi-20260807'
      FROM _saha_sevk_durum_onarim_adaylari aday
      CROSS JOIN _saha_sevk_durum_onarim_ayar ayar
     WHERE ayar.apply
       AND p."Id" = aday.proje_id
       AND p."ProjeTipiId" = 1
       AND p."DurumId" = aday.eski_durum_id
       AND p."DurumId" <> 5
    RETURNING
        p."Id"          AS proje_id,
        p."ProjeNo"     AS proje_no,
        p."DurumId"     AS yeni_durum_id,
        p."UpdatedDate" AS updated_date,
        p."UpdatedBy"   AS updated_by
)
SELECT
    g.proje_id,
    g.proje_no,
    a.eski_durum_id,
    g.yeni_durum_id,
    a.toplam_sandik,
    a.fiziksel_sevk_edilen,
    a.saha_uzerinden_sevk_edilen,
    a.saha_ile_tamamlanan_fiziksel_olmayan,
    a.etkin_sevk_edilen,
    g.updated_date,
    g.updated_by
FROM guncellenen g
INNER JOIN _saha_sevk_durum_onarim_adaylari a
    ON a.proje_id = g.proje_id;

-- RETURNING detayi ve kesin update sayisi
SELECT *
FROM _saha_sevk_durum_onarim_guncellenen
ORDER BY proje_no, proje_id;

SELECT
    ayar.apply,
    count(g.proje_id)::integer AS guncellenen_proje_sayisi
FROM _saha_sevk_durum_onarim_ayar ayar
LEFT JOIN _saha_sevk_durum_onarim_guncellenen g ON true
GROUP BY ayar.apply;

COMMIT;
