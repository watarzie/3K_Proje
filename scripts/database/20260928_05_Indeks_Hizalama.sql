-- =====================================================================================
-- Veritabanı indekslerini EF modeliyle hizalar (model: AppDbContext ve *ModelBuilderExtensions).
--
-- 1) Modelin tanımladığı ama veritabanında olmayan 9 yabancı anahtar indeksi eklenir.
-- 2) Başka bir indeksin ön kolonları olduğu için gereksiz kalan 7 indeks kaldırılır; her birini
--    aynı kolonla başlayan daha geniş bir indeks karşılar (satırlarda karşılayan indeks yazılıdır).
-- 3) Modelde karşılığı olan indeks ve birincil anahtar adları EF'in ürettiği adlara çekilir.
--    Ad değişikliği yalnızca katalogdadır; veri ve sorgu planları etkilenmez.
--
-- Bilinçli olarak dokunulmayanlar: benzersizliği kısıtla sağlayan 6 tablo (AK_… / …_key; modelde
-- benzersiz indeks olarak tanımlı, işlevsel olarak aynı) ve kullanılmayan tablolar
-- (AmbalajTalepEdenler, FinansV2GecisYedegi, GranularYetkiGecisleri; kaldırılmaları ayrı karar).
-- Tekrar çalıştırılabilir: yapılacak iş kalmadıysa hiçbir şey değişmez.
-- =====================================================================================
BEGIN;

-- 1) Eksik yabancı anahtar indeksleri
CREATE INDEX IF NOT EXISTS "IX_AmbalajUretimFormuKayitlari_AmbalajUretimKaydiId"
    ON "AmbalajUretimFormuKayitlari" ("AmbalajUretimKaydiId");
CREATE INDEX IF NOT EXISTS "IX_AmbalajUretimFormuSurumleri_ProjeId"
    ON "AmbalajUretimFormuSurumleri" ("ProjeId");
CREATE INDEX IF NOT EXISTS "IX_Cekiler_KaynakCekiId"
    ON "Cekiler" ("KaynakCekiId");
CREATE INDEX IF NOT EXISTS "IX_CekiRevizyonTalepleri_AnaCekiId"
    ON "CekiRevizyonTalepleri" ("AnaCekiId");
CREATE INDEX IF NOT EXISTS "IX_FinansGiderleri_MahsupEdilenAvansId"
    ON "FinansGiderleri" ("MahsupEdilenAvansId");
CREATE INDEX IF NOT EXISTS "IX_FinansIsKayitlari_SablonSurumId"
    ON "FinansIsKayitlari" ("SablonSurumId");
CREATE INDEX IF NOT EXISTS "IX_SahaAktarimlari_KullaniciId"
    ON "SahaAktarimlari" ("KullaniciId");
CREATE INDEX IF NOT EXISTS "IX_SandikUrunTransferleri_BirimId"
    ON "SandikUrunTransferleri" ("BirimId");
CREATE INDEX IF NOT EXISTS "IX_SandikUrunTransferleri_KullaniciId"
    ON "SandikUrunTransferleri" ("KullaniciId");

-- 2) Gereksiz indeksler
DROP INDEX IF EXISTS "IX_Cekiler_ProjeId";                              -- IX_Cekiler_ProjeId_CekiTipiId
DROP INDEX IF EXISTS "IX_HareketGecmisleri_ProjeId";                    -- (ProjeId, Tarih DESC)
DROP INDEX IF EXISTS "IX_KullaniciBildirimleri_BildirimId";             -- AK_KullaniciBildirimleri_BildirimId_KullaniciId
DROP INDEX IF EXISTS "IX_OnayBekleyenIslemler_OnaylayanKullaniciId";    -- (OnaylayanKullaniciId, KararTarihi)
DROP INDEX IF EXISTS "IX_OnayBekleyenIslemler_TalepEdenKullaniciId";    -- (TalepEdenKullaniciId, CreatedDate)
DROP INDEX IF EXISTS "IX_OnayIslemYetkileri_IslemKodu";                 -- benzersiz (IslemKodu, RolId)
DROP INDEX IF EXISTS "IX_SahaAktarimKalemleri_KaynakCekiSatiriId";      -- (KaynakCekiSatiriId, DurumId)

-- 3a) İndeks adları
ALTER INDEX IF EXISTS ix_hareketgecmisleri_projeid_tarih
    RENAME TO "IX_HareketGecmisleri_ProjeId_Tarih";
ALTER INDEX IF EXISTS "IX_Projeler_CreatedDate_Desc"
    RENAME TO "IX_Projeler_CreatedDate_Id";
ALTER INDEX IF EXISTS "UX_Bildirimler_Tip_Referans"
    RENAME TO "IX_Bildirimler_TipId_ReferansTipi_ReferansId";
ALTER INDEX IF EXISTS "IX_KullaniciBildirimleri_Kullanici_Okunmadi_Id"
    RENAME TO "IX_KullaniciBildirimleri_KullaniciId_OkunduMu_Id";
-- EF, 63 karakteri aşan adı kısaltıp sonuna ~ ekler.
ALTER INDEX IF EXISTS "IX_AmbalajUretimFormuKayitlari_FormSurumuId_AmbalajUretimKaydiI"
    RENAME TO "IX_AmbalajUretimFormuKayitlari_FormSurumuId_AmbalajUretimKaydi~";

-- 3b) Birincil anahtar adları: <Tablo>_pkey -> PK_<Tablo> (kısıtla birlikte indeksi de yeniden adlanır)
DO $$
DECLARE
    tablo text;
BEGIN
    FOREACH tablo IN ARRAY ARRAY[
        'AmbalajUretimFormuKayitlari', 'AmbalajUretimFormuSurumleri', 'AmbalajUretimGerceklesmeleri',
        'BildirimAbonelikleri', 'Bildirimler', 'FinansBelgeleri', 'FinansIsSablonlari',
        'FinansIsSablonSurumleri', 'FinansKaynakBastirmalari', 'HareketGecmisleriArsiv',
        'KullaniciBildirimleri', 'KullaniciYetkileri', 'OnayIslemYetkileri', 'SahaAktarimKalemleri',
        'SahaAktarimlari', 'Sevkiyatlar', 'SevkiyatSandiklari', 'YetkiDegisiklikleri']
    LOOP
        IF EXISTS (
            SELECT 1 FROM pg_constraint
            WHERE conrelid = format('public.%I', tablo)::regclass
              AND contype = 'p'
              AND conname = tablo || '_pkey')
        THEN
            EXECUTE format('ALTER TABLE %I RENAME CONSTRAINT %I TO %I', tablo, tablo || '_pkey', 'PK_' || tablo);
        END IF;
    END LOOP;
END
$$;

COMMIT;
