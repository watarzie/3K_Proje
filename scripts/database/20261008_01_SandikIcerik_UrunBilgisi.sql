-- =====================================================================================
-- Sandık içeriğine kalıcı "Ürün bilgisi" alanı (seri no, son kullanma tarihi, lot).
--
-- Kullanıcı bu bilgiyi bugün 3K işlem panelinin Açıklama kutusuna (CekiSatirlari."UcKAciklama") yazıyor;
-- o alan Geri Gönder ve Geri Al akışlarında kural gereği temizlenir (U-175, U-178). Yeni kolon işlemlerden
-- bağımsızdır: yalnız sandık ekranından düzenlenir, sandık taşıması korur, Grid/3K akışları yazmaz.
--
-- Yalnız kolon eklenir; mevcut satırlar boş (NULL) kalır, veri taşınmaz. Tekrar çalıştırılabilir.
-- =====================================================================================
BEGIN;

ALTER TABLE "SandikIcerikleri" ADD COLUMN IF NOT EXISTS "UrunBilgisi" character varying(1000) NULL;

COMMIT;
