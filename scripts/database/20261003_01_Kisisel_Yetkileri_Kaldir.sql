-- =====================================================================================
-- KULLANICIYA ÖZEL İZİNLERİN KALDIRILMASI
--
-- Yetki artık yalnız rolden gelir. Kullanıcıya özel açık izin / açık ret kararlarını tutan
-- "KullaniciYetkileri" tablosu VERİLERİYLE silinir. Rol izinleri (RolYetkileri), menü tanımları ve
-- yetki değişikliği denetim kayıtları (YetkiDegisiklikleri) korunur.
--
-- ETKİ: Tablodaki kararlar etkisini kaybeder.
--   - Kişisel RET kaydı olan kullanıcı, o izni rolünden alıyorsa yeniden kullanabilir hâle gelir.
--   - Kişisel İZİN kaydı olan kullanıcı, izin rolünde yoksa onu kaybeder.
--   Bütün kararlar silinmeden önce NOTICE olarak listelenir (kullanıcı, izin kodu, kişisel karar, rol seviyesi).
--   Önce onaysız çalıştırıp listeyi görün: betik onay yoksa hiçbir şey değiştirmeden durur ve listeyi yine yazar.
--   RET kaydı olup rolü o izni veren kullanıcıyı, betiği onayla çalıştırmadan önce uygun bir role taşıyın.
--
-- GÜVENLİK: Onay, işlemin yapılacağı veritabanının adıdır. Betik yalnız bağlı olduğu veritabanının adı onayla
--   aynıysa tabloyu siler; onay yoksa ya da başka bir veritabanına bağlıysa hiçbir şey değiştirmeden durur.
--   psql -d 3K_Db ... -v ON_ERROR_STOP=1 -c "SET uc_k.kisisel_yetki_kaldir = '3K_Db'" -f <bu dosya>
--   (pgAdmin: sorgu aracını 3K_Db veritabanından açın; aynı pencerede önce SET satırını, sonra bu dosyayı çalıştırın.)
-- Betik tek transaction'dır. Tekrar çalıştırılabilir: tablo yoksa onay istemeden hiçbir şey yapmaz.
-- Bu betik yeni API sürümüyle birlikte uygulanır (eski API sürümü bu tabloyu okur).
-- =====================================================================================
BEGIN;

DO $$
DECLARE
    onay text := current_setting('uc_k.kisisel_yetki_kaldir', true);
    karar record;
    adet integer := 0;
BEGIN
    IF to_regclass('public."KullaniciYetkileri"') IS NULL THEN
        RAISE NOTICE 'KullaniciYetkileri tablosu yok (veritabanı: %); yapılacak iş yok.', current_database();
        RETURN;
    END IF;

    RAISE NOTICE 'Veritabanı: %', current_database();
    FOR karar IN
        SELECT k."AdSoyad" AS ad, k."Email" AS eposta, m."Kod" AS kod, ky."IzinVerildi" AS izin,
               CASE coalesce(ry."YetkiTipiId", 1) WHEN 3 THEN 'W' WHEN 2 THEN 'R' ELSE 'N' END AS rol
        FROM "KullaniciYetkileri" ky
        JOIN "Kullanicilar" k ON k."Id" = ky."KullaniciId"
        JOIN "MenuTanimlari" m ON m."Id" = ky."MenuTanimiId"
        LEFT JOIN "RolYetkileri" ry ON ry."RolId" = k."RolId" AND ry."MenuTanimiId" = ky."MenuTanimiId"
        ORDER BY k."AdSoyad", m."Kod"
    LOOP
        adet := adet + 1;
        RAISE NOTICE '% <%> · % · kişisel karar: % · rol seviyesi: %',
            karar.ad, karar.eposta, karar.kod, CASE WHEN karar.izin THEN 'İZİN' ELSE 'RET' END,
            karar.rol || CASE WHEN NOT karar.izin AND karar.rol <> 'N'
                THEN '  <-- silinince rolünden erişim açılır' ELSE '' END;
    END LOOP;
    RAISE NOTICE 'Kişisel karar sayısı: %', adet;

    IF onay IS DISTINCT FROM current_database() THEN
        RAISE EXCEPTION 'Onay yok veya bağlı veritabanıyla eşleşmiyor (bağlı veritabanı: "%", onay: "%"). KullaniciYetkileri tablosu verileriyle silinecek; listeyi kontrol edip önce SET uc_k.kisisel_yetki_kaldir = ''<veritabanı adı>''; çalıştırın.',
            current_database(), coalesce(onay, '');
    END IF;

    DROP TABLE "KullaniciYetkileri";
    RAISE NOTICE 'KullaniciYetkileri tablosu silindi.';
END $$;

COMMIT;
