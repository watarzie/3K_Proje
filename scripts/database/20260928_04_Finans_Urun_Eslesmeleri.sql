-- =====================================================================================
-- Ürün formu eşleşmeyi "aktif" alanı olmadan gönderdiği için eşleşmeler pasif kaydediliyor,
-- her güncellemede önceki eşleşme de pasifleşiyordu. Otomatik fiyatlandırma yalnız aktif
-- eşleşmeleri kullandığından hiçbir iş ürüne eşlenemedi.
--
-- Aktif bir ürünün hiç aktif eşleşmesi yoksa kullanıcının son kaydettiği (en yeni) eşleşme
-- aktifleştirilir ve değişiklik finans denetim geçmişine yazılır. Pasif ürünlere ve zaten aktif
-- eşleşmesi olan ürünlere dokunulmaz. Fiyatsız işler bir sonraki ürün kaydında veya üretim
-- aktarımında uygulamanın kendi fiyatlandırma kuralıyla fiyatlanır; betik fiyat hesaplamaz.
-- Tekrar çalıştırılabilir: düzeltilecek ürün kalmadıysa hiçbir satır etkilenmez.
-- =====================================================================================
BEGIN;

CREATE TEMP TABLE aktiflesecek_eslesmeler ON COMMIT DROP AS
SELECT DISTINCT ON (e."FinansUrunId") e."Id", e."FinansUrunId"
FROM "FinansUrunEslesmeleri" e
JOIN "FinansUrunleri" u ON u."Id" = e."FinansUrunId"
WHERE u."Aktif"
  AND NOT EXISTS (
      SELECT 1
      FROM "FinansUrunEslesmeleri" a
      WHERE a."FinansUrunId" = e."FinansUrunId" AND a."Aktif")
ORDER BY e."FinansUrunId", e."Id" DESC;

UPDATE "FinansUrunEslesmeleri" e
SET "Aktif" = true,
    "UpdatedDate" = now() AT TIME ZONE 'Europe/Istanbul',
    "UpdatedBy" = 'SYSTEM'
FROM aktiflesecek_eslesmeler s
WHERE e."Id" = s."Id";

INSERT INTO "FinansDegisiklikGecmisleri"
    ("VarlikTuru", "VarlikId", "Islem", "AlanAdi", "EskiDeger", "YeniDeger", "Aciklama",
     "IslemYapan", "IslemGrubu", "Referans", "CreatedDate", "CreatedBy")
SELECT 'FinansUrunEslesmesi', s."Id", 'Veri Düzeltmesi', 'Aktif', 'false', 'true',
       'Ürün formu eşleşmeyi pasif kaydediyordu; ürünün son eşleşmesi aktifleştirildi.',
       'SYSTEM', gen_random_uuid(), 'FinansUrunId=' || s."FinansUrunId",
       now() AT TIME ZONE 'Europe/Istanbul', 'SYSTEM'
FROM aktiflesecek_eslesmeler s;

COMMIT;
