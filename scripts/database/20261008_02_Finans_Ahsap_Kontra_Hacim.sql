-- =====================================================================================
-- Finans iş kaydında ahşap ve kontra m³ ayrı giriş (kontra/katlanır sandıkta Ambalaj m³ hesaplamaz).
--
-- "AhsapM3"/"KontraM3": elle girilen hacim (boşsa pay eski kayıtlardaki gibi sandık cinsi ve fiyat satırlarından
-- türetilir; veri taşınmaz). "HacimManuel": hacim elle girildi; üretim aktarımı m³ alanlarını ezmez. Elle girilen
-- kayıtta "ToplamM3" = ahşap + kontra (bileşik toplam) olarak saklanır ve m³ tarifesi bu toplamla fiyatlanır.
--
-- "CK_FinansIsKayitlari_Miktarlar" kısıtı yeni kolonların negatif olmamasını da kapsayacak şekilde yeniden kurulur
-- (NULL değerler kısıttan geçer). Tekrar çalıştırılabilir.
-- =====================================================================================
BEGIN;

ALTER TABLE "FinansIsKayitlari" ADD COLUMN IF NOT EXISTS "AhsapM3" numeric(18,6) NULL;
ALTER TABLE "FinansIsKayitlari" ADD COLUMN IF NOT EXISTS "KontraM3" numeric(18,6) NULL;
ALTER TABLE "FinansIsKayitlari" ADD COLUMN IF NOT EXISTS "HacimManuel" boolean NOT NULL DEFAULT FALSE;

ALTER TABLE "FinansIsKayitlari" DROP CONSTRAINT IF EXISTS "CK_FinansIsKayitlari_Miktarlar";
ALTER TABLE "FinansIsKayitlari" ADD CONSTRAINT "CK_FinansIsKayitlari_Miktarlar"
    CHECK ("Adet" > 0 AND "BirimM3" >= 0 AND "ToplamM3" >= 0 AND "AhsapM3" >= 0 AND "KontraM3" >= 0);

COMMIT;
