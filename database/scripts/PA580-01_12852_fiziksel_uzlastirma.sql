/*
  PA580-01 / sıra 86 / CekiSatiriId 12852

  AMAÇ
  - Kayıtlı net miktar 8 iken fiziksel sandık sayımı 9 veya 10 çıkarsa,
    yalnız kayıt dışı kalmış fiziksel farkı kontrollü biçimde uzlaştırmak.

  ÇALIŞTIRMAYIN
  - Fiziksel sayım 8 ise: DB miktarı doğrudur; backend deployundan sonra
    kalan 2 adet normal Grid -> 3K akışıyla tamamlanmalıdır.
  - Son sevk yoldaysa veya fiziksel sayı kesin değilse.
  - Aşağıdaki salt-okunur sorgu beklenen snapshot'tan farklı sonuç verirse.

  GÜVENLİK
  - Yazma bölümü varsayılan değerlerle hata verip hiçbir şey yapmaz.
  - İlk gerçek denemede en alttaki ROLLBACK kesinlikle korunmalıdır.
  - Dry-run sonucu doğrulandıktan sonra güncel xmin değerleriyle yeniden
    çalıştırılıp yalnız en alttaki ROLLBACK, COMMIT olarak değiştirilir.
*/

-- ================================================================
-- 1) SALT-OKUNUR SNAPSHOT VE XMIN DEĞERLERİ
-- ================================================================
BEGIN TRANSACTION READ ONLY;

SELECT
    cs.xmin::text AS "SatirXmin",
    current_database() AS "Database",
    p."ProjeNo",
    p."DurumId" AS "ProjeDurumId",
    cs."Id" AS "CekiSatiriId",
    cs."SiraNo",
    cs."DurumId",
    cs."IstenenAdet",
    cs."GridGelenAdet",
    cs."GridSevkMiktari",
    cs."YenidenSevkGerekliAdet",
    cs."GelenMiktar",
    cs."ProjeGonderilen",
    cs."StokKarsilanan",
    cs."ProjeKarsilanan",
    cs."TedarikciKarsilanan",
    cs."KarsilananMiktar",
    cs."HataliMiktar",
    cs."GeriGonderilenMiktar",
    cs."TrafoSevkAdet",
    cs."KaliteDurumId",
    cs."GridDurumuId",
    cs."GridSevkDurumuId",
    cs."UcKDurumuId",
    cs."UcKKarsilamaTipiId",
    cs."GridSevkTarihi",
    cs."TeslimTarihi",
    cs."UpdatedDate",
    cs."GelenMiktar" - cs."ProjeGonderilen" AS "KayitliNetMiktar",
    GREATEST(
        cs."IstenenAdet"
        - cs."GelenMiktar"
        - cs."StokKarsilanan"
        - cs."ProjeKarsilanan"
        - cs."TedarikciKarsilanan"
        + cs."ProjeGonderilen"
        - cs."TrafoSevkAdet",
        0
    ) AS "KayitliKalan"
FROM "CekiSatirlari" cs
JOIN "Cekiler" c ON c."Id" = cs."CekiId"
JOIN "Projeler" p ON p."Id" = c."ProjeId"
WHERE cs."Id" = 12852
  AND p."ProjeNo" = 'PA580-01'
  AND cs."SiraNo" = 86;

SELECT
    si.xmin::text AS "SandikIcerikXmin",
    s.xmin::text AS "SandikXmin",
    si."Id" AS "SandikIcerikId",
    si."TahsisMiktari",
    si."KonulanAdet",
    si."EksikAdet",
    si."StokKarsilanan",
    si."ProjeKarsilanan",
    si."TedarikciKarsilanan",
    s."Id" AS "SandikId",
    s."SandikNo",
    s."DurumId" AS "SandikDurumId",
    s."SevkiyatDuzeltmeAcikMi"
FROM "SandikIcerikleri" si
JOIN "Sandiklar" s ON s."Id" = si."SandikId"
WHERE si."CekiSatiriId" = 12852
ORDER BY si."Id";

SELECT
    COALESCE(SUM(pt."Miktar"), 0) AS "AktifGidenTransfer"
FROM "ProjeTransferleri" pt
WHERE pt."KaynakCekiSatiriId" = 12852
  AND pt."DurumId" = 1;

ROLLBACK;

-- ================================================================
-- 2) KONTROLLÜ FİZİKSEL UZLAŞTIRMA
-- ================================================================
BEGIN TRANSACTION ISOLATION LEVEL SERIALIZABLE;

SET LOCAL lock_timeout = '5s';
SET LOCAL statement_timeout = '30s';

DO $repair$
DECLARE
    /*
      Fiziksel sayım sonucunu yazın: yalnız 9.0000 veya 10.0000.
      8 ise bu yazma bölümü KULLANILMAZ.
    */
    v_verified_net_in_box numeric(18,4) := 0.0000;

    -- İşlemi yapan gerçek ve aktif kullanıcı e-postası.
    v_operator_email text := 'CHANGE_ME@example.com';

    -- Birinci bölümün sonuçlarından birebir kopyalanmalıdır.
    v_expected_row_xmin text := 'CHANGE_ME';
    v_expected_box_xmin text := 'CHANGE_ME';
    v_expected_sandik_xmin text := 'CHANGE_ME';

    -- Birinci bölümde görülen mevcut DurumId (yalnız 3 veya 12 kabul edilir).
    v_expected_old_durum_id integer := NULL;

    v_row_id integer;
    v_project_id integer;
    v_project_status_id integer;
    v_box_content_id integer;
    v_box_id integer;
    v_box_status_id integer;
    v_box_correction_open boolean;
    v_operator_id integer;
    v_delta numeric(18,4);
    v_now timestamp without time zone :=
        timezone('Europe/Istanbul', statement_timestamp());
BEGIN
    IF current_database() <> 'DB_3K' THEN
        RAISE EXCEPTION
            'Yanlış veritabanı: %. Beklenen veritabanı DB_3K; hiçbir değişiklik yapılmadı.',
            current_database();
    END IF;

    IF v_verified_net_in_box NOT IN (9.0000, 10.0000) THEN
        RAISE EXCEPTION
            'Fiziksel net miktar yalnız 9 veya 10 olabilir; hiçbir değişiklik yapılmadı.';
    END IF;

    IF v_expected_old_durum_id IS NULL OR v_expected_old_durum_id NOT IN (3, 12) THEN
        RAISE EXCEPTION
            'Beklenen eski DurumId birinci bölümden kopyalanmalı ve yalnız 3 veya 12 olmalıdır.';
    END IF;

    v_delta := v_verified_net_in_box - 8.0000;

    SELECT k."Id"
    INTO STRICT v_operator_id
    FROM "Kullanicilar" k
    WHERE lower(k."Email") = lower(v_operator_email);

    /*
      Kimlik, iş değerleri ve xmin birebir uyuşmazsa INTO STRICT hata verir.
      Böylece JSON alındıktan sonra değişen bir satıra yazılmaz.
    */
    SELECT cs."Id", p."Id", p."DurumId"
    INTO STRICT v_row_id, v_project_id, v_project_status_id
    FROM "CekiSatirlari" cs
    JOIN "Cekiler" c ON c."Id" = cs."CekiId"
    JOIN "Projeler" p ON p."Id" = c."ProjeId"
    WHERE cs."Id" = 12852
      AND cs.xmin::text = v_expected_row_xmin
      AND p."ProjeNo" = 'PA580-01'
      AND cs."SiraNo" = 86
      AND cs."DurumId" = v_expected_old_durum_id
      AND cs."IstenenAdet" = 10.0000
      AND cs."GridGelenAdet" = 10.0000
      AND cs."GridSevkMiktari" = 1.0000
      AND cs."YenidenSevkGerekliAdet" = 0.0000
      AND cs."GelenMiktar" = 16.0000
      AND cs."ProjeGonderilen" = 8.0000
      AND cs."StokKarsilanan" = 0.0000
      AND cs."ProjeKarsilanan" = 0.0000
      AND cs."TedarikciKarsilanan" = 0.0000
      AND cs."KarsilananMiktar" = 0.0000
      AND cs."HataliMiktar" = 0.0000
      AND cs."GeriGonderilenMiktar" = 0.0000
      AND cs."TrafoSevkAdet" = 0.0000
      AND cs."KaliteDurumId" IS DISTINCT FROM 2 -- Tadilatta değil
      AND cs."GridDurumuId" = 8
      AND cs."GridSevkDurumuId" = 1
      AND cs."UcKDurumuId" = 2
      AND cs."UcKKarsilamaTipiId" = 2
    FOR UPDATE OF cs, p;

    IF v_project_status_id = 5 THEN
        RAISE EXCEPTION
            'Proje SevkEdildi durumunda ve uygulama tarafından kilitli; yetkili proje kilidi açılmadan SQL düzeltmesi yapılamaz.';
    END IF;

    IF COALESCE((
        SELECT SUM(pt."Miktar")
        FROM "ProjeTransferleri" pt
        WHERE pt."KaynakCekiSatiriId" = v_row_id
          AND pt."DurumId" = 1
    ), 0) <> 8.0000 THEN
        RAISE EXCEPTION
            'Aktif giden transfer toplamı artık 8 değil; işlem durduruldu.';
    END IF;

    IF (
        SELECT COUNT(*)
        FROM "SandikIcerikleri" si
        WHERE si."CekiSatiriId" = v_row_id
    ) <> 1 THEN
        RAISE EXCEPTION
            'Satır tam olarak bir SandikIcerik kaydına bağlı değil; otomatik uzlaştırma yapılmadı.';
    END IF;

    SELECT
        si."Id",
        s."Id",
        s."DurumId",
        s."SevkiyatDuzeltmeAcikMi"
    INTO STRICT
        v_box_content_id,
        v_box_id,
        v_box_status_id,
        v_box_correction_open
    FROM "SandikIcerikleri" si
    JOIN "Sandiklar" s ON s."Id" = si."SandikId"
    WHERE si."CekiSatiriId" = v_row_id
      AND si.xmin::text = v_expected_box_xmin
      AND s.xmin::text = v_expected_sandik_xmin
      AND s."ProjeId" = v_project_id
      AND si."TahsisMiktari" = 0.0000
      AND si."KonulanAdet" = 8.0000
      AND si."EksikAdet" = 2.0000
      AND si."StokKarsilanan" = 0.0000
      AND si."ProjeKarsilanan" = 0.0000
      AND si."TedarikciKarsilanan" = 0.0000
    FOR UPDATE OF si, s;

    IF v_box_status_id = 4 AND NOT COALESCE(v_box_correction_open, false) THEN
        RAISE EXCEPTION
            'Sandık sevk edilmiş ve düzeltmeye açık değil; işlem durduruldu.';
    END IF;

    IF EXISTS (
        SELECT 1
        FROM "SahaAktarimKalemleri" sak
        WHERE (
            sak."KaynakCekiSatiriId" = v_row_id
            OR sak."SahaCekiSatiriId" = v_row_id
        )
          AND sak."DurumId" IN (1, 2, 3, 4, 5)
    ) THEN
        RAISE EXCEPTION
            'Aktif saha aktarımı bulundu; işlem durduruldu.';
    END IF;

    UPDATE "CekiSatirlari"
    SET
        "GelenMiktar" = 16.0000 + v_delta,
        "DurumId" = CASE
            WHEN v_verified_net_in_box = 10.0000 THEN 3  -- Tamamlandi
            ELSE 12                                    -- KismiTamamlandi
        END,
        "UpdatedDate" = v_now,
        "UpdatedBy" = v_operator_email
    WHERE "Id" = v_row_id;

    UPDATE "SandikIcerikleri"
    SET
        "KonulanAdet" = v_verified_net_in_box,
        "EksikAdet" = 10.0000 - v_verified_net_in_box,
        "UpdatedDate" = v_now,
        "UpdatedBy" = v_operator_email
    WHERE "Id" = v_box_content_id;

    INSERT INTO "HareketGecmisleri"
    (
        "ProjeId",
        "ReferansTipi",
        "ReferansId",
        "Islem",
        "IslemTipiId",
        "KullaniciId",
        "Tarih",
        "EskiDeger",
        "YeniDeger",
        "Aciklama",
        "CreatedDate",
        "CreatedBy"
    )
    VALUES
    (
        v_project_id,
        'CekiSatiri',
        v_row_id::text,
        'Kontrollü canlı veri uzlaştırması',
        5, -- UcKDurumGuncellendi
        v_operator_id,
        v_now,
        jsonb_build_object(
            'GelenMiktar', 16,
            'NetMiktar', 8,
            'SandikKonulan', 8,
            'SandikEksik', 2
        )::text,
        jsonb_build_object(
            'GelenMiktar', 16.0000 + v_delta,
            'NetMiktar', v_verified_net_in_box,
            'SandikKonulan', v_verified_net_in_box,
            'SandikEksik', 10.0000 - v_verified_net_in_box
        )::text,
        'Fiziksel sandık sayımıyla doğrulanan kayıt dışı teslim sisteme işlendi. Transfer defteri ve Grid sevk alanları değiştirilmedi.',
        v_now,
        v_operator_email
    );
END;
$repair$;

-- Transaction içindeki sonuç doğrulaması.
SELECT
    p."ProjeNo",
    cs."Id" AS "CekiSatiriId",
    cs."GelenMiktar",
    cs."ProjeGonderilen",
    cs."GelenMiktar" - cs."ProjeGonderilen" AS "NetMiktar",
    GREATEST(
        cs."IstenenAdet"
        - cs."GelenMiktar"
        - cs."StokKarsilanan"
        - cs."ProjeKarsilanan"
        - cs."TedarikciKarsilanan"
        + cs."ProjeGonderilen"
        - cs."TrafoSevkAdet",
        0
    ) AS "Kalan",
    cs."DurumId",
    cs."UcKDurumuId",
    cs."GridSevkDurumuId",
    cs."GridSevkMiktari",
    si."KonulanAdet" AS "SandikKonulan",
    si."EksikAdet" AS "SandikEksik"
FROM "CekiSatirlari" cs
JOIN "Cekiler" c ON c."Id" = cs."CekiId"
JOIN "Projeler" p ON p."Id" = c."ProjeId"
JOIN "SandikIcerikleri" si ON si."CekiSatiriId" = cs."Id"
WHERE cs."Id" = 12852;

-- GÜVENLİ VARSAYILAN: ilk denemede mutlaka ROLLBACK kalmalıdır.
ROLLBACK;

-- Yalnız dry-run sonucu fiziksel sayımla birebir uyumluysa, aynı güncel
-- xmin değerleriyle tekrar çalıştırıp yukarıdaki ROLLBACK yerine COMMIT yazın.
