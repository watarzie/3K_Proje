-- =====================================================================================
-- 32 · Yükleme planı otomatik yerleşimi (5 Kasım 2026, Simülasyon Faz 2): plan revizyonu hesaplanan yerleşimi saklar
-- (isterler 27: ekipman konfigürasyonları, aile dışı gerekçeler, yerleşim koordinatları, uyarılar ve teyitler).
--
-- YuklemePlanRevizyonlari."YerlesimJson": jsonb, boş olabilir; doluysa JSON nesnesi olmalı
-- (CK_YuklemePlanRevizyonlari_YerlesimJson_Nesne). Yalnız "Hesapla" yazar; kaydetme ve kaynaktan güncelleme yeni
-- revizyonu yerleşimsiz açar. Var olan revizyonlar yerleşimsiz kalır.
--
-- Yıkıcı değildir, tekrar çalıştırılabilir: kolon ve kısıt varsa eklenmez. Boş kolon eklemek tabloyu yeniden yazmaz
-- (kısa kilit); yoğun saatte oturumda önce SET lock_timeout = '5s'; verilebilir. 31 numaralı betik
-- (20261105_02_Yukleme_Planlama.sql) önce uygulanmış olmalıdır; değilse hiçbir şey değiştirmeden durur. Yeni API'den
-- önce uygulanır (yeni API plan revizyonunu okurken bu kolonu kullanır). Finans/Ambalaj tablosuna dokunmaz.
-- =====================================================================================
BEGIN;

DO $$
BEGIN
    IF to_regclass('"YuklemePlanRevizyonlari"') IS NULL THEN
        RAISE EXCEPTION '31 numaralı betik (20261105_02_Yukleme_Planlama.sql) uygulanmamış; önce o betiği çalıştırın.';
    END IF;
END $$;

ALTER TABLE "YuklemePlanRevizyonlari" ADD COLUMN IF NOT EXISTS "YerlesimJson" jsonb;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'CK_YuklemePlanRevizyonlari_YerlesimJson_Nesne') THEN
        ALTER TABLE "YuklemePlanRevizyonlari" ADD CONSTRAINT "CK_YuklemePlanRevizyonlari_YerlesimJson_Nesne"
            CHECK ("YerlesimJson" IS NULL OR jsonb_typeof("YerlesimJson") = 'object');
    END IF;
END $$;

DO $$
DECLARE
    hesapli integer;
BEGIN
    SELECT count(*) INTO hesapli FROM "YuklemePlanRevizyonlari" WHERE "YerlesimJson" IS NOT NULL;
    RAISE NOTICE 'YuklemePlanRevizyonlari.YerlesimJson hazır; yerleşimi olan revizyon: %', hesapli;
END $$;

-- Betik kaydı (README "Betik kaydı"): bu betiğin bu veritabanında çalıştığını aynı transaction içinde yazar.
CREATE TABLE IF NOT EXISTS "BetikKayitlari" (
    "BetikAdi"       text PRIMARY KEY,
    "UygulamaTarihi" timestamp without time zone NOT NULL,
    "Uygulayan"      text NOT NULL
);
INSERT INTO "BetikKayitlari" ("BetikAdi", "UygulamaTarihi", "Uygulayan")
VALUES ('20261105_03_Yukleme_Yerlesim', now() AT TIME ZONE 'Europe/Istanbul', current_user)
ON CONFLICT ("BetikAdi") DO NOTHING;

COMMIT;
