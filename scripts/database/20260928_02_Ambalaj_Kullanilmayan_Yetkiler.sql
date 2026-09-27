-- =====================================================================================
-- Kaldırılan eski ambalaj kayıt uçlarıyla (kayitlar/*, m3-override, sarf-orani, secim,
-- iptal/aktiflestir, manuel proje, rapor ve kayıt bazlı form indirmeleri) birlikte hiçbir
-- kod yolunun denetlemediği izin düğümleri rol ekranından kaldırılır.
--
-- Silinen düğümler ambalaj kökünün yaprak düğümleridir; hiçbir işlem veya alan görünürlüğü
-- bunlara bağlı değildir. Rol izinleri ve kişisel kararlar da birlikte silinir (kişisel karar
-- FK'si RESTRICT olduğundan önce silinir). Tablo yapısı değişmez.
-- Tekrar çalıştırılabilir: kod zaten yoksa hiçbir satır etkilenmez.
-- =====================================================================================
BEGIN;

CREATE TEMP TABLE silinecek_yetki_kodlari (kod text PRIMARY KEY) ON COMMIT DROP;
INSERT INTO silinecek_yetki_kodlari (kod) VALUES
    ('ambalaj-m3-duzenle'),
    ('ambalaj-sarf-duzenle'),
    ('ambalaj-kaynak-senkronize'),
    ('ambalaj-uretime-al'),
    ('ambalaj-uretimden-cikar'),
    ('ambalaj-ilave-olustur'),
    ('ambalaj-saha-olustur'),
    ('ambalaj-yedek-olustur'),
    ('ambalaj-ic-olustur'),
    ('ambalaj-diger-olustur'),
    ('ambalaj-manuel-proje'),
    ('ambalaj-tur-duzenle'),
    ('ambalaj-cins-duzenle'),
    ('ambalaj-proje-duzenle'),
    ('ambalaj-olcu-duzenle'),
    ('ambalaj-talep-duzenle'),
    ('ambalaj-geri-yukle'),
    ('ambalaj-gecmis-goruntule');

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
