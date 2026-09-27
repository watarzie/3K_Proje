-- =====================================================================================
-- Finans ve Ambalaj işlem/alan izinlerinin etiketleri diğer menülerle aynı biçime alınır.
--
-- Önce:  LabelKey = 'Kayıt düzenleme' (düz Türkçe metin)
-- Sonra: LabelKey = 'MENU.AMBALAJ_KAYIT_DUZENLE' (frontend i18n anahtarı; TR/EN çevirisi
--        src/assets/i18n dosyalarındadır)
--
-- Tablo yapısı değişmez; yalnız LabelKey verisi güncellenir. Tekrar çalıştırılabilir:
-- zaten MENU.* olan kayıtlar atlanır. Rol atamaları (RolYetkileri) etkilenmez.
-- =====================================================================================
BEGIN;

UPDATE "MenuTanimlari"
SET "LabelKey"    = 'MENU.' || upper(replace("Kod", '-', '_')),
    "UpdatedDate" = now(),
    "UpdatedBy"   = '20260928_01_Yetki_Etiketleri.sql'
WHERE ("Kod" LIKE 'ambalaj-%' OR "Kod" LIKE 'finans-%')
  AND "LabelKey" NOT LIKE 'MENU.%';

-- Doğrulama: çeviri anahtarı olmayan menü kalmamalı.
DO $$
DECLARE
    kalan integer;
BEGIN
    SELECT count(*) INTO kalan FROM "MenuTanimlari" WHERE "LabelKey" NOT LIKE 'MENU.%';
    IF kalan > 0 THEN
        RAISE EXCEPTION 'Çeviri anahtarı olmayan % menü tanımı kaldı.', kalan;
    END IF;
END $$;

COMMIT;
