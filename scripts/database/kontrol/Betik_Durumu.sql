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
-- Yeni betik eklenince buraya satır eklenir (CanliyaAlimBetikleriTests dosya listesiyle karşılaştırır).
-- =====================================================================================
WITH kolon(tablo, ad) AS (SELECT table_name, column_name FROM information_schema.columns WHERE table_schema = 'public')
SELECT sira, betik, CASE WHEN uygulandi IS NULL THEN 'tespit edilemez' WHEN uygulandi THEN 'uygulanmış' ELSE 'UYGULANMAMIŞ' END AS durum
FROM (VALUES
    (22, '20261021_02_Sahadan_Sahaya_Tasima',       EXISTS (SELECT 1 FROM kolon WHERE tablo = 'SahaAktarimlari' AND ad = 'IslemAnahtari')
                                                    AND EXISTS (SELECT 1 FROM kolon WHERE tablo = 'SahaTasimaKalemleri' AND ad = 'KaynakCekiSatiriId')),
    (23, '20261023_01_Grid_Saha_Gorunumu_Yetkisi',  EXISTS (SELECT 1 FROM "MenuTanimlari" WHERE "Kod" = 'grid-saha-gorunumu' AND "Route" IS NULL)),
    (24, '20261023_02_Sevk_Sonrasi_Duzeltme',       EXISTS (SELECT 1 FROM kolon WHERE tablo = 'SandikDuzeltmeKayitlari' AND ad = 'AcilisGoruntusu')
                                                    AND EXISTS (SELECT 1 FROM "MenuTanimlari" WHERE "Kod" = 'sevk-sonrasi-duzeltme'))
) AS t(sira, betik, uygulandi)
ORDER BY sira;
