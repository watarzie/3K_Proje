-- Bu script backend deploy edilmeden önce çalıştırılmalıdır.
-- Mevcut kayıtları temizlemez; yalnız günlük job'ın güvenli biçimde
-- DosyaIcerigi alanını NULL yapabilmesi için şemayı hazırlar.

BEGIN;

ALTER TABLE public."CekiRevizyonTalepleri"
    ADD COLUMN IF NOT EXISTS "DosyaIcerigiTemizlenmeTarihi" timestamp without time zone NULL;

ALTER TABLE public."CekiRevizyonTalepleri"
    ALTER COLUMN "DosyaIcerigi" DROP NOT NULL;

ALTER TABLE public."CekiRevizyonTalepleri"
    DROP CONSTRAINT IF EXISTS "CK_CekiRevizyonTalepleri_DosyaIcerigi_Dolu";

ALTER TABLE public."CekiRevizyonTalepleri"
    ADD CONSTRAINT "CK_CekiRevizyonTalepleri_DosyaIcerigi_Dolu"
    CHECK (
        "DosyaIcerigi" IS NULL
        OR octet_length("DosyaIcerigi") > 0
    );

ALTER TABLE public."CekiRevizyonTalepleri"
    DROP CONSTRAINT IF EXISTS "CK_CekiRevizyonTalepleri_DosyaIcerigi_Boyut";

ALTER TABLE public."CekiRevizyonTalepleri"
    ADD CONSTRAINT "CK_CekiRevizyonTalepleri_DosyaIcerigi_Boyut"
    CHECK (
        "DosyaIcerigi" IS NULL
        OR octet_length("DosyaIcerigi") <= 20971520
    );

ALTER TABLE public."CekiRevizyonTalepleri"
    DROP CONSTRAINT IF EXISTS "CK_CekiRevizyonTalepleri_DosyaIcerigi_Temizleme";

ALTER TABLE public."CekiRevizyonTalepleri"
    ADD CONSTRAINT "CK_CekiRevizyonTalepleri_DosyaIcerigi_Temizleme"
    CHECK (
        (
            "DosyaIcerigi" IS NOT NULL
            AND "DosyaIcerigiTemizlenmeTarihi" IS NULL
        )
        OR (
            "DosyaIcerigi" IS NULL
            AND "UygulananRevizyonCekiId" IS NOT NULL
            AND "UygulamaTarihi" IS NOT NULL
            AND "DosyaIcerigiTemizlenmeTarihi" IS NOT NULL
        )
    );

CREATE INDEX IF NOT EXISTS "IX_CekiRevizyonTalepleri_DosyaIcerigiTemizleme"
    ON public."CekiRevizyonTalepleri" ("CreatedDate", "Id")
    WHERE "DosyaIcerigi" IS NOT NULL
      AND "UygulananRevizyonCekiId" IS NOT NULL;

COMMENT ON COLUMN public."CekiRevizyonTalepleri"."DosyaIcerigiTemizlenmeTarihi"
    IS 'Başarıyla uygulanmış revizyonun DB içindeki ağır dosya içeriğinin günlük job ile temizlendiği Türkiye zamanı.';

COMMIT;
