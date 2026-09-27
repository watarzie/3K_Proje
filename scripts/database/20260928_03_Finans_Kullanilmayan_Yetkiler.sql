-- =====================================================================================
-- Hiçbir sunucu işleminin denetlemediği ve hiçbir ekranın kullanmadığı finans izin
-- düğümleri rol ekranından kaldırılır:
--   finans-gider-yonet   : hiçbir işlem veya ekran bağlı değildi.
--   finans-fatura-yonet  : yalnız kaldırılan fatura operasyon/geri alma uçlarında kullanılıyordu.
--   finans-tarife-yonet  : yalnız kaldırılan fiyat tarifesi uçlarında kullanılıyordu; ürün ve
--                          tarife kayıtları sunucuda finans-is-kutuphanesi-yonet iznine bağlıdır.
--   finans-is-duzenle    : yalnız kaldırılan iş kaydı güncelleme ucunda kullanılıyordu; özel işi
--                          geri alma sunucuda finans-is-iptal iznine bağlıdır.
--
-- Silinen düğümler finans kökünün yaprak düğümleridir. Rol izinleri ve kişisel kararlar da
-- birlikte silinir (kişisel karar FK'si RESTRICT olduğundan önce silinir). Tablo yapısı değişmez.
-- Tekrar çalıştırılabilir: kod zaten yoksa hiçbir satır etkilenmez.
-- =====================================================================================
BEGIN;

CREATE TEMP TABLE silinecek_yetki_kodlari (kod text PRIMARY KEY) ON COMMIT DROP;
INSERT INTO silinecek_yetki_kodlari (kod) VALUES
    ('finans-gider-yonet'),
    ('finans-fatura-yonet'),
    ('finans-tarife-yonet'),
    ('finans-is-duzenle');

-- Güvenlik: yalnız yaprak düğümler silinir; alt düğümü olan bir kod varsa hiçbir şey değişmez.
DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM "MenuTanimlari" alt
        JOIN "MenuTanimlari" ust ON ust."Id" = alt."ParentId"
        JOIN silinecek_yetki_kodlari s ON s.kod = ust."Kod") THEN
        RAISE EXCEPTION 'Silinecek izin kodlarından birinin alt düğümü var; betik durduruldu.';
    END IF;
END $$;

DELETE FROM "KullaniciYetkileri" ky
USING "MenuTanimlari" m, silinecek_yetki_kodlari s
WHERE ky."MenuTanimiId" = m."Id" AND m."Kod" = s.kod;

DELETE FROM "RolYetkileri" ry
USING "MenuTanimlari" m, silinecek_yetki_kodlari s
WHERE ry."MenuTanimiId" = m."Id" AND m."Kod" = s.kod;

DELETE FROM "MenuTanimlari" m
USING silinecek_yetki_kodlari s
WHERE m."Kod" = s.kod;

COMMIT;
