-- =====================================================================================
-- Finans sekme yetki düğümleri (rol yönetiminde Finans modülünün sekme bazında kırılımı).
--
-- 1) finans-yonetimi kökünün altına her Finans sekmesi için bir düğüm eklenir (Id 5238–5245,
--    Route NULL: menüde görünmez; Sira 1–8: rol editöründe yapraklardan önce gelir).
-- 2) ÖNCE her role sekme düğümünün seviyesi verilir: sekmeye taşınacak alt kodlardaki ve sekmeyi bugün
--    açan koddaki seviyelerin yükseği, rolün finans modülü seviyesiyle sınırlı (Admin her sekmede W).
--    Böylece hiçbir rolün bugünkü etkin izni azalmaz. Sıra tersine olsaydı taşınan alt kodlar sekme
--    düğümü N olduğu için fiilen N olur, rol editöründeki ilk kayıtta eski W kalıcı silinirdi.
-- 3) SONRA yalnız o sekmeye ait işlem kodları sekme düğümünün altına taşınır (ParentId). Birden çok
--    sekmenin ya da alan maskesinin kullandığı kodlar (kayıt/rapor/gider görüntüleme, parasal alanlar,
--    birim fiyat, dışa aktarma, belge, kalıcı silme, denetim, tek seferlik iş ekleme) kökte kalır.
--
-- Beklenen bir kod eksikse veya Id başka bir koda aitse hiçbir şey değiştirmeden durur. Tekrar
-- çalıştırılabilir: düğüm varsa eklenmez, verilmiş sekme izni değiştirilmez, doğru üstteki kod
-- yeniden taşınmaz. Uygulama açılışında çalışmaz; API ile birlikte elle uygulanır.
-- =====================================================================================
BEGIN;

CREATE TEMP TABLE finans_sekmeleri (
    id integer PRIMARY KEY,
    kod text NOT NULL,
    label_key text NOT NULL,
    icon text NOT NULL,
    sira integer NOT NULL,
    acan_kodlar text[] NOT NULL
) ON COMMIT DROP;

-- Sekmeyi bugün açan kodlar: panel modülle, diğerleri ekranın okuma izniyle açılır.
INSERT INTO finans_sekmeleri VALUES
    (5238, 'finans-sekme-panel',              'MENU.FINANS_SEKME_PANEL',              'ri-dashboard-line',       1, ARRAY['finans-yonetimi']),
    (5239, 'finans-sekme-is-takibi',          'MENU.FINANS_SEKME_IS_TAKIBI',          'ri-calendar-check-line',  2, ARRAY['finans-kayit-goruntule']),
    (5240, 'finans-sekme-po-fatura',          'MENU.FINANS_SEKME_PO_FATURA',          'ri-file-list-3-line',     5, ARRAY['finans-siparis-goruntule']),
    (5241, 'finans-sekme-giderler',           'MENU.FINANS_SEKME_GIDERLER',           'ri-wallet-3-line',        3, ARRAY['finans-gider-goruntule']),
    (5242, 'finans-sekme-gider-kategorileri', 'MENU.FINANS_SEKME_GIDER_KATEGORILERI', 'ri-folder-settings-line', 7, ARRAY['finans-gider-kutuphanesi-yonet']),
    (5243, 'finans-sekme-tanimlar',           'MENU.FINANS_SEKME_TANIMLAR',           'ri-settings-3-line',      4, ARRAY['finans-duzenli-is-yonet', 'finans-is-kutuphanesi-yonet']),
    (5244, 'finans-sekme-raporlar',           'MENU.FINANS_SEKME_RAPORLAR',           'ri-file-chart-line',      6, ARRAY['finans-rapor-goruntule']),
    (5245, 'finans-sekme-munferit-isler',     'MENU.FINANS_SEKME_MUNFERIT_ISLER',     'ri-briefcase-4-line',     8, ARRAY['finans-is-ekle']);

CREATE TEMP TABLE finans_sekme_altlari (
    kod text PRIMARY KEY,
    sekme_kod text NOT NULL
) ON COMMIT DROP;

-- Yalnız o sekmenin ekranında kullanılan işlem kodları.
INSERT INTO finans_sekme_altlari VALUES
    ('finans-siparis-goruntule',       'finans-sekme-po-fatura'),
    ('finans-po-gir',                  'finans-sekme-po-fatura'),
    ('finans-po-degistir',             'finans-sekme-po-fatura'),
    ('finans-po-iptal',                'finans-sekme-po-fatura'),
    ('finans-fatura-gir',              'finans-sekme-po-fatura'),
    ('finans-fatura-degistir',         'finans-sekme-po-fatura'),
    ('finans-fatura-iptal',            'finans-sekme-po-fatura'),
    ('finans-odeme-kaydet',            'finans-sekme-po-fatura'),
    ('finans-gider-ekle',              'finans-sekme-giderler'),
    ('finans-gider-duzenle',           'finans-sekme-giderler'),
    ('finans-gider-iptal',             'finans-sekme-giderler'),
    ('finans-gider-kutuphanesi-yonet', 'finans-sekme-gider-kategorileri'),
    ('finans-duzenli-is-yonet',        'finans-sekme-tanimlar'),
    ('finans-is-kutuphanesi-yonet',    'finans-sekme-tanimlar'),
    ('finans-is-iptal',                'finans-sekme-is-takibi'),
    ('finans-tarih-degistir',          'finans-sekme-is-takibi'),
    ('finans-fiyatlandirma-degistir',  'finans-sekme-is-takibi');

-- Ön koşullar: kök, beklenen kodlar, yetki tipleri ve Id/kod çakışması.
DO $$
DECLARE
    kok_id integer;
    eksik text;
    cakisan text;
BEGIN
    SELECT "Id" INTO kok_id FROM "MenuTanimlari" WHERE "Kod" = 'finans-yonetimi';
    IF kok_id IS NULL THEN
        RAISE EXCEPTION 'finans-yonetimi menü kökü bulunamadı; betik durduruldu.';
    END IF;

    SELECT string_agg(k.kod, ', ' ORDER BY k.kod) INTO eksik
    FROM (SELECT kod FROM finans_sekme_altlari
          UNION SELECT unnest(acan_kodlar) FROM finans_sekmeleri) k
    WHERE NOT EXISTS (SELECT 1 FROM "MenuTanimlari" m WHERE m."Kod" = k.kod);
    IF eksik IS NOT NULL THEN
        RAISE EXCEPTION 'Beklenen finans izin kodları bulunamadı: %; betik durduruldu.', eksik;
    END IF;

    IF (SELECT count(*) FROM "LookupYetkiTipleri" WHERE "Deger" IN ('R', 'W')) <> 2 THEN
        RAISE EXCEPTION 'R/W yetki tipleri bulunamadı; betik durduruldu.';
    END IF;

    SELECT string_agg(format('%s (%s)', m."Id", m."Kod"), ', ') INTO cakisan
    FROM "MenuTanimlari" m
    JOIN finans_sekmeleri s ON (s.id = m."Id" AND s.kod <> m."Kod") OR (s.kod = m."Kod" AND s.id <> m."Id");
    IF cakisan IS NOT NULL THEN
        RAISE EXCEPTION 'Sekme Id''leri veya kodları başka bir menü tanımıyla çakışıyor: %; betik durduruldu.', cakisan;
    END IF;

    -- 1) Sekme düğümleri.
    INSERT INTO "MenuTanimlari" ("Id", "Kod", "LabelKey", "Icon", "Route", "Sira", "ParentId", "CreatedDate", "CreatedBy")
    SELECT s.id, s.kod, s.label_key, s.icon, NULL, s.sira, kok_id,
           now() AT TIME ZONE 'Europe/Istanbul', '20261008_03_Finans_Sekme_Yetkileri.sql'
    FROM finans_sekmeleri s
    WHERE NOT EXISTS (SELECT 1 FROM "MenuTanimlari" m WHERE m."Kod" = s.kod);

    PERFORM setval(pg_get_serial_sequence('"MenuTanimlari"', 'Id'), (SELECT max("Id") FROM "MenuTanimlari"));
END $$;

-- 2) Rollere sekme seviyesi (taşımadan önce). Seviye sırası N < R < W; N verilmez (kayıt yoksa N'dir).
WITH seviyeler AS (
    SELECT ry."RolId" AS rol_id, m."Kod" AS kod,
           CASE tip."Deger" WHEN 'W' THEN 3 WHEN 'R' THEN 2 ELSE 1 END AS seviye
    FROM "RolYetkileri" ry
    JOIN "MenuTanimlari" m ON m."Id" = ry."MenuTanimiId"
    JOIN "LookupYetkiTipleri" tip ON tip."Id" = ry."YetkiTipiId"
),
modul AS (
    SELECT rol_id, seviye FROM seviyeler WHERE kod = 'finans-yonetimi'
),
aday AS (
    SELECT s.kod AS sekme_kod, sv.rol_id, max(sv.seviye) AS seviye
    FROM finans_sekmeleri s
    JOIN seviyeler sv
      ON sv.kod = ANY (s.acan_kodlar)
      OR sv.kod IN (SELECT a.kod FROM finans_sekme_altlari a WHERE a.sekme_kod = s.kod)
    GROUP BY s.kod, sv.rol_id
),
hedef AS (
    -- Admin (rol 1) her sekmede W; diğer roller modül seviyesini aşmaz.
    SELECT s.kod AS sekme_kod, md.rol_id,
           CASE WHEN md.rol_id = 1 THEN 3 ELSE LEAST(coalesce(a.seviye, 1), md.seviye) END AS seviye
    FROM modul md
    CROSS JOIN finans_sekmeleri s
    LEFT JOIN aday a ON a.rol_id = md.rol_id AND a.sekme_kod = s.kod
)
INSERT INTO "RolYetkileri" ("RolId", "MenuTanimiId", "YetkiTipiId", "CreatedDate", "CreatedBy")
SELECT h.rol_id, m."Id", tip."Id", now() AT TIME ZONE 'Europe/Istanbul', '20261008_03_Finans_Sekme_Yetkileri.sql'
FROM hedef h
JOIN "MenuTanimlari" m ON m."Kod" = h.sekme_kod
JOIN "LookupYetkiTipleri" tip ON tip."Deger" = CASE h.seviye WHEN 3 THEN 'W' ELSE 'R' END
WHERE h.seviye >= 2
  AND NOT EXISTS (
      SELECT 1 FROM "RolYetkileri" mevcut
      WHERE mevcut."RolId" = h.rol_id AND mevcut."MenuTanimiId" = m."Id");

-- 3) Sekmeye ait işlem kodlarını sekme düğümünün altına taşı (izinler önceki adımda korunmuştur).
UPDATE "MenuTanimlari" alt
SET "ParentId" = sekme."Id",
    "UpdatedDate" = now() AT TIME ZONE 'Europe/Istanbul',
    "UpdatedBy" = '20261008_03_Finans_Sekme_Yetkileri.sql'
FROM finans_sekme_altlari a
JOIN "MenuTanimlari" sekme ON sekme."Kod" = a.sekme_kod
WHERE alt."Kod" = a.kod
  AND alt."ParentId" IS DISTINCT FROM sekme."Id";

-- Döküm: rol × sekme seviyeleri ve sekmelerin altındaki kodlar.
DO $$
DECLARE
    satir record;
BEGIN
    FOR satir IN
        SELECT r."Ad" AS rol, m."Kod" AS sekme, tip."Deger" AS seviye
        FROM "RolYetkileri" ry
        JOIN "Roller" r ON r."Id" = ry."RolId"
        JOIN "MenuTanimlari" m ON m."Id" = ry."MenuTanimiId"
        JOIN "LookupYetkiTipleri" tip ON tip."Id" = ry."YetkiTipiId"
        WHERE m."Kod" LIKE 'finans-sekme-%'
        ORDER BY r."Id", m."Sira"
    LOOP
        RAISE NOTICE 'Rol "%": % = %', satir.rol, satir.sekme, satir.seviye;
    END LOOP;
    FOR satir IN
        SELECT sekme."Kod" AS sekme, string_agg(alt."Kod", ', ' ORDER BY alt."Sira") AS kodlar
        FROM "MenuTanimlari" sekme
        JOIN "MenuTanimlari" alt ON alt."ParentId" = sekme."Id"
        WHERE sekme."Kod" LIKE 'finans-sekme-%'
        GROUP BY sekme."Kod", sekme."Sira"
        ORDER BY sekme."Sira"
    LOOP
        RAISE NOTICE '% altında: %', satir.sekme, satir.kodlar;
    END LOOP;
END $$;

COMMIT;
