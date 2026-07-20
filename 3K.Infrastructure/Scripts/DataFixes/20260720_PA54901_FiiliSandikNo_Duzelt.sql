/*
    PA549-01 / Sandık 9-18 veri düzeltmesi

    Amaç:
      - SandikIcerikleri kayıtları tekil olarak Sandık 9'u gösteren,
      - son sandık ürün transferi 18 -> 9 olan,
      - buna rağmen CekiSatirlari.FiiliSandikNo = '18' kalmış
        FCT01171726 ve FCT00685169 satırlarının FiiliSandikNo değerini '9' yapmak.

    Bilinçli olarak değiştirilmez:
      - CekideGecenSandikNo (planlanan/çekide yazan sandık),
      - Grid ve 3K işlem alanları,
      - SandikIcerikleri,
      - SandikUrunTransferleri hareket geçmişi,
      - eksik FCT01171726-01 satırı.

    Script yalnız PA549-01'in doğrulanan veri şekli birebir mevcutsa güncelleme yapar.
    Herhangi bir ön koşul farklıysa EXCEPTION üretir ve transaction rollback olur.
*/

BEGIN;

SET LOCAL lock_timeout = '10s';
SET LOCAL statement_timeout = '60s';

-- Ön kontroller ile UPDATE arasına yeni sandık/içerik/transfer işlemi girmesini
-- engeller. Kilit 10 saniyede alınamazsa script değişiklik yapmadan durur.
LOCK TABLE
    public."Projeler",
    public."Cekiler",
    public."CekiSatirlari",
    public."Sandiklar",
    public."SandikIcerikleri",
    public."SandikUrunTransferleri"
IN SHARE ROW EXCLUSIVE MODE;

DO $data_fix$
DECLARE
    v_proje_id integer;
    v_ana_ceki_id integer;
    v_sandik_9_id integer;
    v_satir_ids integer[];
    v_sayac integer;
    v_gecerli_sayac integer;
    v_barkod_1_sayisi integer;
    v_barkod_2_sayisi integer;
    v_guncellenen_sayisi integer;
BEGIN
    SELECT count(*)::integer, min(p."Id")
    INTO v_sayac, v_proje_id
    FROM public."Projeler" AS p
    WHERE upper(trim(p."ProjeNo")) = 'PA549-01';

    IF v_sayac <> 1 THEN
        RAISE EXCEPTION
            'Veri düzeltmesi durduruldu: PA549-01 proje kaydı tekil değil. Bulunan: %.',
            v_sayac;
    END IF;

    SELECT count(*)::integer, min(c."Id")
    INTO v_sayac, v_ana_ceki_id
    FROM public."Cekiler" AS c
    WHERE c."ProjeId" = v_proje_id
      AND c."CekiTipiId" = 1;

    IF v_sayac <> 1 THEN
        RAISE EXCEPTION
            'Veri düzeltmesi durduruldu: PA549-01 için tek bir normal ana çeki bekleniyordu. Bulunan: %.',
            v_sayac;
    END IF;

    SELECT count(*)::integer, min(s."Id")
    INTO v_sayac, v_sandik_9_id
    FROM public."Sandiklar" AS s
    WHERE s."ProjeId" = v_proje_id
      AND trim(s."SandikNo") = '9';

    IF v_sayac <> 1 THEN
        RAISE EXCEPTION
            'Veri düzeltmesi durduruldu: PA549-01 / Sandık 9 tekil değil. Bulunan: %.',
            v_sayac;
    END IF;

    SELECT count(*)::integer
    INTO v_sayac
    FROM public."Sandiklar" AS s
    WHERE s."ProjeId" = v_proje_id
      AND trim(s."SandikNo") = '18';

    IF v_sayac <> 0 THEN
        RAISE EXCEPTION
            'Veri düzeltmesi durduruldu: PA549-01 içinde Sandık 18 yeniden oluşturulmuş. Bulunan: %.',
            v_sayac;
    END IF;

    SELECT
        count(*)::integer,
        count(*) FILTER (
            WHERE upper(trim(cs."BarkodNo")) = 'FCT01171726'
        )::integer,
        count(*) FILTER (
            WHERE upper(trim(cs."BarkodNo")) = 'FCT00685169'
        )::integer,
        array_agg(cs."Id" ORDER BY cs."Id")
    INTO v_sayac, v_barkod_1_sayisi, v_barkod_2_sayisi, v_satir_ids
    FROM public."CekiSatirlari" AS cs
    WHERE cs."CekiId" = v_ana_ceki_id
      AND upper(trim(cs."BarkodNo")) IN ('FCT01171726', 'FCT00685169')
      AND trim(coalesce(cs."FiiliSandikNo", '')) = '18';

    IF v_sayac <> 2 OR v_barkod_1_sayisi <> 1 OR v_barkod_2_sayisi <> 1 THEN
        RAISE EXCEPTION
            'Veri düzeltmesi durduruldu: Her hedef barkoddan birer satır bekleniyordu. Toplam: %, FCT01171726: %, FCT00685169: %.',
            v_sayac,
            v_barkod_1_sayisi,
            v_barkod_2_sayisi;
    END IF;

    SELECT
        count(*)::integer,
        count(DISTINCT si."CekiSatiriId")::integer
    INTO v_sayac, v_gecerli_sayac
    FROM public."SandikIcerikleri" AS si
    WHERE si."CekiSatiriId" = ANY(v_satir_ids)
      AND si."SandikId" = v_sandik_9_id
      AND (si."TahsisMiktari" > 0 OR si."KonulanAdet" > 0);

    IF v_sayac <> 2 OR v_gecerli_sayac <> 2 THEN
        RAISE EXCEPTION
            'Veri düzeltmesi durduruldu: Her hedef satır için Sandık 9 üzerinde bir aktif içerik bekleniyordu. İçerik: %, hedef satır: %.',
            v_sayac,
            v_gecerli_sayac;
    END IF;

    SELECT count(*)::integer
    INTO v_sayac
    FROM public."SandikIcerikleri" AS si
    WHERE si."CekiSatiriId" = ANY(v_satir_ids);

    IF v_sayac <> 2 THEN
        RAISE EXCEPTION
            'Veri düzeltmesi durduruldu: Hedef satırlara bağlı toplam iki içerik bekleniyordu. Bulunan: %.',
            v_sayac;
    END IF;

    SELECT
        count(*)::integer,
        count(*) FILTER (
            WHERE trim(son_transfer."KaynakSandikNo") = '18'
              AND trim(son_transfer."HedefSandikNo") = '9'
        )::integer
    INTO v_sayac, v_gecerli_sayac
    FROM (
        SELECT DISTINCT ON (transfer."CekiSatiriId")
            transfer."CekiSatiriId",
            transfer."KaynakSandikNo",
            transfer."HedefSandikNo"
        FROM public."SandikUrunTransferleri" AS transfer
        WHERE transfer."ProjeId" = v_proje_id
          AND transfer."CekiSatiriId" = ANY(v_satir_ids)
        ORDER BY transfer."CekiSatiriId", transfer."Tarih" DESC, transfer."Id" DESC
    ) AS son_transfer;

    IF v_sayac <> 2 OR v_gecerli_sayac <> 2 THEN
        RAISE EXCEPTION
            'Veri düzeltmesi durduruldu: İki hedef satırın son transferi 18 -> 9 olmalıydı. Toplam: %, geçerli: %.',
            v_sayac,
            v_gecerli_sayac;
    END IF;

    UPDATE public."CekiSatirlari" AS cs
    SET "FiiliSandikNo" = '9',
        "UpdatedDate" = timezone('Europe/Istanbul', now()),
        "UpdatedBy" = 'PA549-01 FiiliSandikNo veri düzeltmesi'
    WHERE cs."Id" = ANY(v_satir_ids)
      AND trim(coalesce(cs."FiiliSandikNo", '')) = '18';

    GET DIAGNOSTICS v_guncellenen_sayisi = ROW_COUNT;

    IF v_guncellenen_sayisi <> 2 THEN
        RAISE EXCEPTION
            'Veri düzeltmesi durduruldu: İki satır güncellenmeliydi. Güncellenen: %.',
            v_guncellenen_sayisi;
    END IF;

    SELECT count(*)::integer
    INTO v_sayac
    FROM public."CekiSatirlari" AS cs
    WHERE cs."Id" = ANY(v_satir_ids)
      AND trim(coalesce(cs."FiiliSandikNo", '')) = '9';

    IF v_sayac <> 2 THEN
        RAISE EXCEPTION
            'Veri düzeltmesi son kontrolden geçemedi. Fiili sandığı 9 olan hedef satır: %.',
            v_sayac;
    END IF;

    RAISE NOTICE
        'PA549-01 veri düzeltmesi başarılı. Güncellenen CekiSatiri Id değerleri: %.',
        v_satir_ids;
END;
$data_fix$;

SELECT
    cs."Id" AS "CekiSatiriId",
    cs."SiraNo",
    cs."BarkodNo",
    cs."CekideGecenSandikNo",
    cs."FiiliSandikNo",
    s."SandikNo" AS "IcerikSandikNo",
    si."TahsisMiktari",
    si."KonulanAdet"
FROM public."CekiSatirlari" AS cs
JOIN public."Cekiler" AS c
  ON c."Id" = cs."CekiId"
JOIN public."Projeler" AS p
  ON p."Id" = c."ProjeId"
LEFT JOIN public."SandikIcerikleri" AS si
  ON si."CekiSatiriId" = cs."Id"
LEFT JOIN public."Sandiklar" AS s
  ON s."Id" = si."SandikId"
WHERE upper(trim(p."ProjeNo")) = 'PA549-01'
  AND upper(trim(cs."BarkodNo")) IN ('FCT01171726', 'FCT00685169')
ORDER BY cs."Id", si."Id";

COMMIT;
