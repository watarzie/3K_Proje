-- =====================================================================================
-- 25 · Proje bağlantı türü (24 Ekim 2026): proje fotoğraf bağlantıları penceresine "Gerçekleşen Çeki" sekmesi.
--
-- "ProjeFotografBaglantilari" tablosuna "Tur" (smallint, NOT NULL, varsayılan 1) kolonu eklenir:
-- 1 = Fotoğraf, 2 = Gerçekleşen çeki (ProjeBaglantiTuru). Var olan bütün bağlantılar varsayılanla fotoğraf kalır.
-- İzin değişmez (proje-fotograf-baglantisi: R görür/açar, W ekler/değiştirir/siler); her türden en fazla 20 bağlantı.
--
-- Yıkıcı değildir, tekrar çalıştırılabilir: kolon varsa eklenmez. Sabit varsayılanlı kolon eklemek PostgreSQL 11+'da
-- tabloyu yeniden yazmaz. Yeni API'den ÖNCE uygulanır: yeni API bağlantıları okurken bu kolonu kullanır (yoksa proje
-- listeleri hata verir). Eski API çalışırken uygulanabilir (eski API kolonu okumaz, eklediği kayıt fotoğraf olur).
-- =====================================================================================
BEGIN;

ALTER TABLE "ProjeFotografBaglantilari" ADD COLUMN IF NOT EXISTS "Tur" smallint NOT NULL DEFAULT 1;

COMMIT;
