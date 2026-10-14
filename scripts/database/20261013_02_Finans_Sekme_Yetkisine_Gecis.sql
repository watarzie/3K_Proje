-- =====================================================================================
-- FİNANS YETKİSİNİN YALNIZ SEKME DÜZEYİNE İNDİRİLMESİ (+ ayrı vade uyarısı izni)
--
-- Yeni model: Finans'ta yalnız 8 sekme düğümü (finans-sekme-*, Id 5238–5245) vardır. Sekmenin R izni sekmedeki bütün
-- verileri (tutar, birim fiyat, gelir, gider, kârlılık, ölçü, m³) gösterir; W izni sekmedeki bütün işlemleri (kalıcı
-- silme dahil) yapar. Alt işlem ve alan izinleri (finans-kayit-goruntule, finans-po-gir, finans-tutar-goruntule …) ve
-- alan maskelemesi kaldırılır. İş Takibi'nin üstündeki vade uyarısı ayrı izindir: finans-vade-uyarisi (Id 5247, R).
--
-- ADIMLAR (tek transaction):
--   1) finans-vade-uyarisi düğümü finans-yonetimi altına eklenir. Daha önce vade uyarısını görebilen roller
--      (finans-siparis-goruntule ve finans-kayit-goruntule ikisi de en az R) ve Admin R alır.
--   2) Hiçbir rolün bugün yapabildiği iş azalmasın diye, silinecek alt kodların seviyesi ait olduğu sekmeye taşınır:
--      sekme seviyesi = en yüksek(mevcut sekme seviyesi, o sekmeye eşlenen alt kodların seviyesi), rolün finans modülü
--      seviyesini aşmadan (Admin her sekmede W). Görüntüleme/alan izinleri (gelir, birim fiyat, tutar, kârlılık…),
--      belge, denetim ve kalıcı silme bir sekmeye taşınmaz: yeni modelde bunlar sekmenin kendi R/W iznidir.
--      NOT: sekme W'si olan rol, sekmenin bütün işlemlerini yapar (ör. yalnız "PO gir" W'si olan rol artık PO/Fatura'da
--      fatura da girer). Değişen seviyeler NOTICE olarak yazılır.
--   3) finans-yonetimi altında kalan bütün diğer kodlar (modül, sekme ve alt sekme düğümleri finans-sekme-* ve vade
--      uyarısı hariç) RolYetkileri kayıtlarıyla birlikte SİLİNİR. Silinecek kodlar ve rol × seviye kararları önce
--      NOTICE olarak listelenir.
--
-- GÜVENLİK: 3. adım yıkıcıdır. Onay, işlemin yapılacağı veritabanının adıdır; betik yalnız bağlı olduğu veritabanının
--   adı onayla aynıysa çalışır, aksi hâlde listeyi yazıp HİÇBİR ŞEY DEĞİŞTİRMEDEN durur (bütün adımlar geri alınır).
--   Önce onaysız çalıştırıp listeyi görün, sonra:
--   psql -d 3K_Db ... -v ON_ERROR_STOP=1 -c "SET uc_k.finans_sekme_yetkisi = '3K_Db'" -f <bu dosya>
--   (pgAdmin: sorgu aracını 3K_Db veritabanından açın; aynı pencerede önce SET satırını, sonra bu dosyayı çalıştırın.)
-- Tekrar çalıştırılabilir: silinecek kod kalmadıysa onay istemez ve hiçbir izni değiştirmez (yöneticinin sonradan
-- düşürdüğü sekme izni, Admin'inki dahil, geri yükseltilmez); vade düğümü varsa eklemez. 20261013_03'ün eklediği alt
-- sekme düğümleri (finans-sekme-*) hiçbir zaman silinmez.
-- Yeni API sürümüyle birlikte uygulanır: eski API silinen kodları ister, yeni API yalnız sekme kodlarını.
-- =====================================================================================
BEGIN;

CREATE TEMP TABLE finans_kalan_kodlar (kod text PRIMARY KEY) ON COMMIT DROP;
INSERT INTO finans_kalan_kodlar VALUES
    ('finans-yonetimi'),
    ('finans-sekme-panel'), ('finans-sekme-is-takibi'), ('finans-sekme-po-fatura'), ('finans-sekme-giderler'),
    ('finans-sekme-gider-kategorileri'), ('finans-sekme-tanimlar'), ('finans-sekme-raporlar'), ('finans-sekme-munferit-isler'),
    ('finans-vade-uyarisi');

-- Silinecek alt kodların seviyesinin taşınacağı sekmeler (listede olmayan kod hiçbir sekmeye taşınmaz).
CREATE TEMP TABLE finans_alt_kod_sekmeleri (kod text NOT NULL, sekme_kod text NOT NULL, PRIMARY KEY (kod, sekme_kod)) ON COMMIT DROP;
INSERT INTO finans_alt_kod_sekmeleri VALUES
    ('finans-kayit-goruntule',         'finans-sekme-is-takibi'),
    ('finans-is-iptal',                'finans-sekme-is-takibi'),
    ('finans-tarih-degistir',          'finans-sekme-is-takibi'),
    ('finans-fiyatlandirma-degistir',  'finans-sekme-is-takibi'),
    ('finans-is-ekle',                 'finans-sekme-is-takibi'),
    ('finans-is-ekle',                 'finans-sekme-munferit-isler'),
    ('finans-siparis-goruntule',       'finans-sekme-po-fatura'),
    ('finans-po-gir',                  'finans-sekme-po-fatura'),
    ('finans-po-degistir',             'finans-sekme-po-fatura'),
    ('finans-po-iptal',                'finans-sekme-po-fatura'),
    ('finans-fatura-gir',              'finans-sekme-po-fatura'),
    ('finans-fatura-degistir',         'finans-sekme-po-fatura'),
    ('finans-fatura-iptal',            'finans-sekme-po-fatura'),
    ('finans-odeme-kaydet',            'finans-sekme-po-fatura'),
    ('finans-gider-goruntule',         'finans-sekme-giderler'),
    ('finans-gider-ekle',              'finans-sekme-giderler'),
    ('finans-gider-duzenle',           'finans-sekme-giderler'),
    ('finans-gider-iptal',             'finans-sekme-giderler'),
    ('finans-gider-kutuphanesi-yonet', 'finans-sekme-gider-kategorileri'),
    ('finans-is-kutuphanesi-yonet',    'finans-sekme-tanimlar'),
    ('finans-duzenli-is-yonet',        'finans-sekme-tanimlar'),
    ('finans-birim-fiyat-degistir',    'finans-sekme-tanimlar'),
    ('finans-rapor-goruntule',         'finans-sekme-raporlar'),
    ('finans-excel-aktar',             'finans-sekme-raporlar'),
    ('finans-pdf-aktar',               'finans-sekme-raporlar');

DO $$
DECLARE
    onay text := current_setting('uc_k.finans_sekme_yetkisi', true);
    kok_id integer;
    eksik text;
    cakisan text;
    satir record;
    silinecek_adet integer;
BEGIN
    RAISE NOTICE 'Veritabanı: %', current_database();
    SELECT "Id" INTO kok_id FROM "MenuTanimlari" WHERE "Kod" = 'finans-yonetimi';
    IF kok_id IS NULL THEN
        RAISE EXCEPTION 'finans-yonetimi menü kökü bulunamadı; betik durduruldu.';
    END IF;
    SELECT string_agg(k.kod, ', ' ORDER BY k.kod) INTO eksik
    FROM finans_kalan_kodlar k
    WHERE k.kod LIKE 'finans-sekme-%' AND NOT EXISTS (SELECT 1 FROM "MenuTanimlari" m WHERE m."Kod" = k.kod);
    IF eksik IS NOT NULL THEN
        RAISE EXCEPTION 'Finans sekme düğümleri bulunamadı: %; önce 20261008_03_Finans_Sekme_Yetkileri.sql çalıştırılmalı.', eksik;
    END IF;
    IF (SELECT count(*) FROM "LookupYetkiTipleri" WHERE "Deger" IN ('R', 'W')) <> 2 THEN
        RAISE EXCEPTION 'R/W yetki tipleri bulunamadı; betik durduruldu.';
    END IF;
    SELECT format('%s (%s)', m."Id", m."Kod") INTO cakisan
    FROM "MenuTanimlari" m
    WHERE (m."Id" = 5247 AND m."Kod" <> 'finans-vade-uyarisi') OR (m."Kod" = 'finans-vade-uyarisi' AND m."Id" <> 5247);
    IF cakisan IS NOT NULL THEN
        RAISE EXCEPTION 'Id 5247 veya finans-vade-uyarisi başka bir menü tanımıyla çakışıyor: %; betik durduruldu.', cakisan;
    END IF;

    -- 1) Vade uyarısı düğümü ve izni.
    IF NOT EXISTS (SELECT 1 FROM "MenuTanimlari" WHERE "Kod" = 'finans-vade-uyarisi') THEN
        INSERT INTO "MenuTanimlari" ("Id", "Kod", "LabelKey", "Icon", "Route", "Sira", "ParentId", "CreatedDate", "CreatedBy")
        VALUES (5247, 'finans-vade-uyarisi', 'MENU.FINANS_VADE_UYARISI', 'ri-alarm-warning-line', NULL, 9, kok_id,
                now() AT TIME ZONE 'Europe/Istanbul', '20261013_02_Finans_Sekme_Yetkisine_Gecis.sql');
        PERFORM setval(pg_get_serial_sequence('"MenuTanimlari"', 'Id'), (SELECT max("Id") FROM "MenuTanimlari"));

        INSERT INTO "RolYetkileri" ("RolId", "MenuTanimiId", "YetkiTipiId", "CreatedDate", "CreatedBy")
        SELECT r."Id", 5247, (SELECT "Id" FROM "LookupYetkiTipleri" WHERE "Deger" = 'R'),
               now() AT TIME ZONE 'Europe/Istanbul', '20261013_02_Finans_Sekme_Yetkisine_Gecis.sql'
        FROM "Roller" r
        WHERE r."Id" = 1
           OR (SELECT count(*) FROM "RolYetkileri" ry
               JOIN "MenuTanimlari" m ON m."Id" = ry."MenuTanimiId"
               JOIN "LookupYetkiTipleri" tip ON tip."Id" = ry."YetkiTipiId"
               WHERE ry."RolId" = r."Id" AND tip."Deger" IN ('R', 'W')
                 AND m."Kod" IN ('finans-siparis-goruntule', 'finans-kayit-goruntule')) = 2;
        FOR satir IN
            SELECT r."Ad" AS rol FROM "RolYetkileri" ry JOIN "Roller" r ON r."Id" = ry."RolId"
            WHERE ry."MenuTanimiId" = 5247 ORDER BY r."Id"
        LOOP
            RAISE NOTICE 'Vade uyarısı izni (R) verildi: rol "%"', satir.rol;
        END LOOP;
    END IF;
END $$;

-- Silinecek kodlar: finans-yonetimi ağacındaki kalan kodlar ve sekme/alt sekme düğümleri (finans-sekme-*) dışındaki her şey.
CREATE TEMP TABLE finans_silinecek AS
WITH RECURSIVE agac AS (
    SELECT m."Id", m."Kod", 0 AS derinlik FROM "MenuTanimlari" m WHERE m."Kod" = 'finans-yonetimi'
    UNION ALL
    SELECT c."Id", c."Kod", a.derinlik + 1 FROM "MenuTanimlari" c JOIN agac a ON c."ParentId" = a."Id"
)
SELECT a."Id" AS id, a."Kod" AS kod, a.derinlik
FROM agac a
WHERE a."Kod" NOT IN (SELECT kod FROM finans_kalan_kodlar)
  AND a."Kod" NOT LIKE 'finans-sekme-%';

-- 2) Alt kod seviyelerini sekmeye taşı (silmeden önce). Seviye: N=1 < R=2 < W=3. Silinecek kod yoksa geçiş tamamlanmıştır;
--    hiçbir seviye değişmez.
CREATE TEMP TABLE finans_sekme_hedef ON COMMIT DROP AS
WITH seviyeler AS (
    SELECT ry."RolId" AS rol_id, m."Kod" AS kod,
           CASE tip."Deger" WHEN 'W' THEN 3 WHEN 'R' THEN 2 ELSE 1 END AS seviye
    FROM "RolYetkileri" ry
    JOIN "MenuTanimlari" m ON m."Id" = ry."MenuTanimiId"
    JOIN "LookupYetkiTipleri" tip ON tip."Id" = ry."YetkiTipiId"
),
modul AS (SELECT rol_id, seviye FROM seviyeler WHERE kod = 'finans-yonetimi'),
alt AS (
    SELECT e.sekme_kod, s.rol_id, max(s.seviye) AS seviye
    FROM finans_alt_kod_sekmeleri e
    JOIN seviyeler s ON s.kod = e.kod
    WHERE e.kod IN (SELECT kod FROM finans_silinecek)
    GROUP BY e.sekme_kod, s.rol_id
),
mevcut AS (SELECT rol_id, kod AS sekme_kod, seviye FROM seviyeler WHERE kod LIKE 'finans-sekme-%')
SELECT md.rol_id, sk.kod AS sekme_kod, coalesce(mv.seviye, 1) AS eski,
       CASE WHEN md.rol_id = 1 THEN 3
            ELSE GREATEST(coalesce(mv.seviye, 1), LEAST(coalesce(a.seviye, 1), md.seviye)) END AS yeni
FROM modul md
CROSS JOIN (SELECT kod FROM finans_kalan_kodlar WHERE kod LIKE 'finans-sekme-%') sk
LEFT JOIN alt a ON a.rol_id = md.rol_id AND a.sekme_kod = sk.kod
LEFT JOIN mevcut mv ON mv.rol_id = md.rol_id AND mv.sekme_kod = sk.kod
WHERE EXISTS (SELECT 1 FROM finans_silinecek);

DO $$
DECLARE
    satir record;
BEGIN
    FOR satir IN
        SELECT r."Ad" AS rol, h.sekme_kod, h.eski, h.yeni
        FROM finans_sekme_hedef h JOIN "Roller" r ON r."Id" = h.rol_id
        WHERE h.yeni > h.eski
        ORDER BY r."Id", h.sekme_kod
    LOOP
        RAISE NOTICE 'Rol "%": % % -> %', satir.rol, satir.sekme_kod,
            CASE satir.eski WHEN 3 THEN 'W' WHEN 2 THEN 'R' ELSE 'N' END,
            CASE satir.yeni WHEN 3 THEN 'W' WHEN 2 THEN 'R' ELSE 'N' END;
    END LOOP;
END $$;

UPDATE "RolYetkileri" ry
SET "YetkiTipiId" = (SELECT "Id" FROM "LookupYetkiTipleri" WHERE "Deger" = CASE h.yeni WHEN 3 THEN 'W' ELSE 'R' END),
    "UpdatedDate" = now() AT TIME ZONE 'Europe/Istanbul',
    "UpdatedBy" = '20261013_02_Finans_Sekme_Yetkisine_Gecis.sql'
FROM finans_sekme_hedef h
JOIN "MenuTanimlari" m ON m."Kod" = h.sekme_kod
WHERE ry."RolId" = h.rol_id AND ry."MenuTanimiId" = m."Id" AND h.yeni > h.eski AND h.yeni >= 2;

INSERT INTO "RolYetkileri" ("RolId", "MenuTanimiId", "YetkiTipiId", "CreatedDate", "CreatedBy")
SELECT h.rol_id, m."Id", (SELECT "Id" FROM "LookupYetkiTipleri" WHERE "Deger" = CASE h.yeni WHEN 3 THEN 'W' ELSE 'R' END),
       now() AT TIME ZONE 'Europe/Istanbul', '20261013_02_Finans_Sekme_Yetkisine_Gecis.sql'
FROM finans_sekme_hedef h
JOIN "MenuTanimlari" m ON m."Kod" = h.sekme_kod
WHERE h.yeni >= 2
  AND NOT EXISTS (SELECT 1 FROM "RolYetkileri" ry WHERE ry."RolId" = h.rol_id AND ry."MenuTanimiId" = m."Id");

-- 3) Alt kodları sil (onaylı).
DO $$
DECLARE
    onay text := current_setting('uc_k.finans_sekme_yetkisi', true);
    satir record;
    adet integer;
BEGIN
    SELECT count(*) INTO adet FROM finans_silinecek;
    IF adet = 0 THEN
        RAISE NOTICE 'Silinecek finans alt kodu yok; sekme yetkisine geçiş zaten tamamlanmış.';
        RETURN;
    END IF;
    FOR satir IN
        SELECT s.kod, coalesce(string_agg(format('%s=%s', r."Ad", tip."Deger"), ', ' ORDER BY r."Id"), '(rol kaydı yok)') AS roller
        FROM finans_silinecek s
        LEFT JOIN "RolYetkileri" ry ON ry."MenuTanimiId" = s.id
        LEFT JOIN "Roller" r ON r."Id" = ry."RolId"
        LEFT JOIN "LookupYetkiTipleri" tip ON tip."Id" = ry."YetkiTipiId"
        GROUP BY s.kod
        ORDER BY s.kod
    LOOP
        RAISE NOTICE 'Silinecek: % · %', satir.kod, satir.roller;
    END LOOP;
    RAISE NOTICE 'Silinecek finans alt kodu: %', adet;

    IF onay IS DISTINCT FROM current_database() THEN
        RAISE EXCEPTION 'Onay yok veya bağlı veritabanıyla eşleşmiyor (bağlı veritabanı: "%", onay: "%"). Yukarıdaki % finans alt kodu rol izinleriyle silinecek; listeyi kontrol edip önce SET uc_k.finans_sekme_yetkisi = ''<veritabanı adı>''; çalıştırın.',
            current_database(), coalesce(onay, ''), adet;
    END IF;

    DELETE FROM "RolYetkileri" WHERE "MenuTanimiId" IN (SELECT id FROM finans_silinecek);
    -- Çocuklar önce: derinliği büyük olandan küçüğe.
    FOR satir IN SELECT id FROM finans_silinecek ORDER BY derinlik DESC, id LOOP
        DELETE FROM "MenuTanimlari" WHERE "Id" = satir.id;
    END LOOP;
    RAISE NOTICE '% finans alt kodu ve rol izinleri silindi.', adet;
END $$;

DROP TABLE finans_silinecek;

-- Döküm: rol × finans sekme ve vade uyarısı seviyeleri.
DO $$
DECLARE
    satir record;
BEGIN
    FOR satir IN
        SELECT r."Ad" AS rol, string_agg(format('%s=%s', m."Kod", tip."Deger"), ', ' ORDER BY m."Sira") AS izinler
        FROM "RolYetkileri" ry
        JOIN "Roller" r ON r."Id" = ry."RolId"
        JOIN "MenuTanimlari" m ON m."Id" = ry."MenuTanimiId"
        JOIN "LookupYetkiTipleri" tip ON tip."Id" = ry."YetkiTipiId"
        WHERE m."Kod" LIKE 'finans-sekme-%' OR m."Kod" = 'finans-vade-uyarisi'
        GROUP BY r."Id", r."Ad"
        ORDER BY r."Id"
    LOOP
        RAISE NOTICE 'Rol "%": %', satir.rol, satir.izinler;
    END LOOP;
END $$;

COMMIT;
