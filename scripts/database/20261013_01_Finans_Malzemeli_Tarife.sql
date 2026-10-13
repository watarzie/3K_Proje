-- =====================================================================================
-- Malzemeli tarife: kontrplak ve katlanır sandıkta ahşap m³ ahşap tarifesiyle, kontra m³ kontra tarifesiyle fiyatlanır.
--
-- "FinansUrunEslesmeleri"."Malzeme": eşleşmenin malzemesi (1 = Ahşap, 2 = Kontra, boş = malzemeden bağımsız, eski tek
-- m³ tarifesi). Katlanır sandık artık boy/en/yükseklik ile eşleşmez; eski "Boy"/"En"/"Yukseklik" değerleri silinmez,
-- yalnız eşleşmede kullanılmaz (ürün kaydedilince temizlenir). Aşağıdaki NOTICE bu tür eşleşmeleri sayar: aynı sandık
-- tipine boyuta göre ayrılmış birden çok ürün varsa artık sıra (Sira) önceliklisi seçilir; Tanımlar ekranından birleştirin.
--
-- "FinansIsKayitlari": "AhsapBirimFiyatSnapshot" doluysa iş malzemeli fiyatlanmıştır; "FinansUrunId"/"BirimFiyatSnapshot"
-- kontra tarafıdır ve net = kontra m³ × kontra fiyatı + ahşap m³ × ahşap fiyatı. "AhsapFinansUrunId" ve
-- "AhsapTarifeIdSnapshot" ahşap tarafının ürünü ve tarife sürümüdür. Mevcut kayıtlar boş kalır (tek tarifeli hesap aynen
-- sürer); hacim girilince veya ürün yeniden fiyatlanınca yeni kural uygulanır.
--
-- Tekrar çalıştırılabilir.
-- =====================================================================================
BEGIN;

ALTER TABLE "FinansUrunEslesmeleri" ADD COLUMN IF NOT EXISTS "Malzeme" integer NULL;

ALTER TABLE "FinansIsKayitlari" ADD COLUMN IF NOT EXISTS "AhsapBirimFiyatSnapshot" numeric(18,6) NULL;
ALTER TABLE "FinansIsKayitlari" ADD COLUMN IF NOT EXISTS "AhsapFinansUrunId" integer NULL;
ALTER TABLE "FinansIsKayitlari" ADD COLUMN IF NOT EXISTS "AhsapTarifeIdSnapshot" integer NULL;

ALTER TABLE "FinansIsKayitlari" DROP CONSTRAINT IF EXISTS "CK_FinansIsKayitlari_AhsapFiyat";
ALTER TABLE "FinansIsKayitlari" ADD CONSTRAINT "CK_FinansIsKayitlari_AhsapFiyat"
    CHECK ("AhsapBirimFiyatSnapshot" IS NULL OR "AhsapBirimFiyatSnapshot" >= 0);

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_FinansIsKayitlari_FinansUrunleri_AhsapFinansUrunId') THEN
        ALTER TABLE "FinansIsKayitlari"
            ADD CONSTRAINT "FK_FinansIsKayitlari_FinansUrunleri_AhsapFinansUrunId"
            FOREIGN KEY ("AhsapFinansUrunId") REFERENCES "FinansUrunleri" ("Id") ON DELETE RESTRICT;
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS "IX_FinansIsKayitlari_AhsapFinansUrunId" ON "FinansIsKayitlari" ("AhsapFinansUrunId");
CREATE INDEX IF NOT EXISTS "IX_FinansIsKayitlari_AhsapTarifeIdSnapshot" ON "FinansIsKayitlari" ("AhsapTarifeIdSnapshot");

DO $$
DECLARE
    boyutlu integer;
BEGIN
    SELECT count(*) INTO boyutlu
    FROM "FinansUrunEslesmeleri"
    WHERE "Aktif" AND ("Boy" IS NOT NULL OR "En" IS NOT NULL OR "Yukseklik" IS NOT NULL);
    IF boyutlu > 0 THEN
        RAISE NOTICE '% aktif ürün eşleşmesinde eski boy/en/yükseklik değeri var; bu değerler artık eşleşmede kullanılmaz.', boyutlu;
    END IF;
END $$;

COMMIT;
