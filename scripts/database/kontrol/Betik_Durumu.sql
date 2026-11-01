-- =====================================================================================
-- Betik durumu (SALT OKUNUR): bu klasördeki betiklerden hangisi bağlı veritabanında uygulanmış?
--
-- Her betik bıraktığı kalıcı izle (kolon, tablo, menü düğümü) tanınır; hiçbir şey değiştirmez. Canlıya alımdan önce
-- 3K_Db düğümünden açılan sorgu penceresinde çalıştırın; UYGULANMAMIŞ satırlar README sırasıyla uygulanır.
-- 1–20 numaralı betikler canlıya uygulandı ve depodan kaldırıldı (da54f6f); burada yalnız depodaki betikler listelenir.
-- 21 boş kaldı (otomatik sahaya aktarım şema değişikliği gerektirmedi).
-- 22 numaralı betik kolonu ve tabloyu birlikte arar: biri eksikse UYGULANMAMIŞ görünür ve betik tekrar çalıştırılır.
-- 23 numaralı betik menü düğümünü (grid-saha-gorunumu) rotasız arar; ilk sürümün rotalı düğümü uygulanmamış sayılır.
-- 24 numaralı betik tabloyu (SandikDuzeltmeKayitlari) ve menü düğümünü (sevk-sonrasi-duzeltme) birlikte arar.
-- 25 numaralı betik ProjeFotografBaglantilari.Tur kolonunu, 26 numaralı betik menü düğümünü (barkod-urun-arama) rotasız
-- arar; ilk sürümün rotalı düğümü uygulanmamış sayılır.
-- 27 numaralı betik Sandiklar.KapanmaTarihi kolonunu, SandikDurumOlaylari ve LookupSandikAcilmaSebepleri tablolarını ve
-- menü düğümünü (sandik-acilis-istatistigi) birlikte arar.
-- 28 numaralı betik Kullanicilar.BasarisizGirisSayisi ve GirisKilitBitisTarihi kolonlarını birlikte arar.
-- 29 numaralı betik menü düğümünü (barkod-etiket) arar.
-- Yeni betik eklenince buraya satır eklenir (CanliyaAlimBetikleriTests dosya listesiyle karşılaştırır).
-- =====================================================================================
WITH kolon(tablo, ad) AS (SELECT table_name, column_name FROM information_schema.columns WHERE table_schema = 'public')
SELECT sira, betik, CASE WHEN uygulandi IS NULL THEN 'tespit edilemez' WHEN uygulandi THEN 'uygulanmış' ELSE 'UYGULANMAMIŞ' END AS durum
FROM (VALUES
    (22, '20261021_02_Sahadan_Sahaya_Tasima',       EXISTS (SELECT 1 FROM kolon WHERE tablo = 'SahaAktarimlari' AND ad = 'IslemAnahtari')
                                                    AND EXISTS (SELECT 1 FROM kolon WHERE tablo = 'SahaTasimaKalemleri' AND ad = 'KaynakCekiSatiriId')),
    (23, '20261023_01_Grid_Saha_Gorunumu_Yetkisi',  EXISTS (SELECT 1 FROM "MenuTanimlari" WHERE "Kod" = 'grid-saha-gorunumu' AND "Route" IS NULL)),
    (24, '20261023_02_Sevk_Sonrasi_Duzeltme',       EXISTS (SELECT 1 FROM kolon WHERE tablo = 'SandikDuzeltmeKayitlari' AND ad = 'AcilisGoruntusu')
                                                    AND EXISTS (SELECT 1 FROM "MenuTanimlari" WHERE "Kod" = 'sevk-sonrasi-duzeltme')),
    (25, '20261024_01_Proje_Baglanti_Turu',         EXISTS (SELECT 1 FROM kolon WHERE tablo = 'ProjeFotografBaglantilari' AND ad = 'Tur')),
    (26, '20261024_02_Barkod_Urun_Arama_Yetkisi',   EXISTS (SELECT 1 FROM "MenuTanimlari" WHERE "Kod" = 'barkod-urun-arama' AND "Route" IS NULL)),
    (27, '20261024_03_Sandik_Acilis_Kapanis_Gecmisi', EXISTS (SELECT 1 FROM kolon WHERE tablo = 'Sandiklar' AND ad = 'KapanmaTarihi')
                                                    AND EXISTS (SELECT 1 FROM kolon WHERE tablo = 'SandikDurumOlaylari' AND ad = 'Kaynak')
                                                    AND EXISTS (SELECT 1 FROM kolon WHERE tablo = 'LookupSandikAcilmaSebepleri' AND ad = 'Deger')
                                                    AND EXISTS (SELECT 1 FROM "MenuTanimlari" WHERE "Kod" = 'sandik-acilis-istatistigi')),
    (28, '20261025_01_Giris_Kilidi',                EXISTS (SELECT 1 FROM kolon WHERE tablo = 'Kullanicilar' AND ad = 'BasarisizGirisSayisi')
                                                    AND EXISTS (SELECT 1 FROM kolon WHERE tablo = 'Kullanicilar' AND ad = 'GirisKilitBitisTarihi')),
    (29, '20261026_01_Barkod_Etiket_Yetkisi',       EXISTS (SELECT 1 FROM "MenuTanimlari" WHERE "Kod" = 'barkod-etiket'))
) AS t(sira, betik, uygulandi)
ORDER BY sira;
