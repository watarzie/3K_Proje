-- =====================================================================================
-- Betik durumu (SALT OKUNUR): bu klasördeki betiklerden hangisi bağlı veritabanında uygulanmış?
--
-- Her betik bıraktığı kalıcı izle (kolon, tablo, menü düğümü) tanınır; hiçbir şey değiştirmez. Canlıya alımdan önce
-- 3K_Db düğümünden açılan sorgu penceresinde çalıştırın; UYGULANMAMIŞ satırlar README sırasıyla uygulanır.
-- 4 numaralı betik (ürün eşleşmesi onarımı) iz bırakmaz; sıfırlama (6) sonrası zaten iş yapmaz.
-- Yeni betik eklenince buraya satır eklenir (CanliyaAlimBetikleriTests dosya listesiyle karşılaştırır).
-- =====================================================================================
WITH kolon(tablo, ad) AS (SELECT table_name, column_name FROM information_schema.columns WHERE table_schema = 'public'),
kod AS (SELECT "Id", "Kod", "ParentId", "LabelKey" FROM "MenuTanimlari"),
finans_agaci AS (
    WITH RECURSIVE a AS (SELECT "Id", "Kod" FROM kod WHERE "Kod" = 'finans-yonetimi'
                         UNION ALL SELECT k."Id", k."Kod" FROM kod k JOIN a ON k."ParentId" = a."Id")
    SELECT "Kod" FROM a)
SELECT sira, betik, CASE WHEN uygulandi IS NULL THEN 'tespit edilemez' WHEN uygulandi THEN 'uygulanmış' ELSE 'UYGULANMAMIŞ' END AS durum
FROM (VALUES
    (1,  '20260928_01_Yetki_Etiketleri',               NOT EXISTS (SELECT 1 FROM kod WHERE "LabelKey" NOT LIKE 'MENU.%')),
    (2,  '20260928_02_Ambalaj_Kullanilmayan_Yetkiler', NOT EXISTS (SELECT 1 FROM kod WHERE "Kod" IN ('ambalaj-m3-duzenle', 'ambalaj-uretime-al'))),
    (3,  '20260928_03_Finans_Kullanilmayan_Yetkiler',  NOT EXISTS (SELECT 1 FROM kod WHERE "Kod" IN ('finans-gider-yonet', 'finans-is-duzenle'))),
    (4,  '20260928_04_Finans_Urun_Eslesmeleri',        NULL::boolean),
    (5,  '20260928_05_Indeks_Hizalama',                to_regclass('public."IX_Cekiler_KaynakCekiId"') IS NOT NULL),
    (6,  '20260929_01_Finans_Ambalaj_Sifirlama',       to_regclass('public."FinansParaBirimleri"') IS NOT NULL),
    (7,  '20260929_02_Finans_Odeme_Yetkisi',           NOT EXISTS (SELECT 1 FROM kod WHERE "Kod" = 'finans-sablon-yonet')
                                                       AND EXISTS (SELECT 1 FROM kod WHERE "Kod" IN ('finans-odeme-kaydet', 'finans-vade-uyarisi'))),
    (8,  '20261003_01_Kisisel_Yetkileri_Kaldir',       to_regclass('public."KullaniciYetkileri"') IS NULL),
    (9,  '20261008_01_SandikIcerik_UrunBilgisi',       EXISTS (SELECT 1 FROM kolon WHERE tablo = 'SandikIcerikleri' AND ad = 'UrunBilgisi')),
    (10, '20261008_02_Finans_Ahsap_Kontra_Hacim',      EXISTS (SELECT 1 FROM kolon WHERE tablo = 'FinansIsKayitlari' AND ad = 'HacimManuel')),
    (11, '20261008_03_Finans_Sekme_Yetkileri',         EXISTS (SELECT 1 FROM kod WHERE "Kod" = 'finans-sekme-panel')),
    (12, '20261008_04_Sevk_Seferleri',                 to_regclass('public."SevkSeferleri"') IS NOT NULL),
    (13, '20261008_05_Sevkiyatlar_Yetkisi',            EXISTS (SELECT 1 FROM kod WHERE "Kod" = 'sevkiyatlar')),
    (14, '20261013_01_Finans_Malzemeli_Tarife',        EXISTS (SELECT 1 FROM kolon WHERE tablo = 'FinansUrunEslesmeleri' AND ad = 'Malzeme')),
    (15, '20261013_02_Finans_Sekme_Yetkisine_Gecis',   EXISTS (SELECT 1 FROM kod WHERE "Kod" = 'finans-vade-uyarisi')
                                                       AND NOT EXISTS (SELECT 1 FROM finans_agaci WHERE "Kod" NOT LIKE 'finans-sekme-%'
                                                                       AND "Kod" NOT IN ('finans-yonetimi', 'finans-vade-uyarisi'))),
    (16, '20261013_03_Finans_Alt_Sekme_Yetkileri',     EXISTS (SELECT 1 FROM kod WHERE "Kod" = 'finans-sekme-po-fatura-fatura')),
    (17, '20261014_01_Proje_Fotograf_Baglantisi',      EXISTS (SELECT 1 FROM kolon WHERE tablo = 'Projeler' AND ad = 'FotografBaglantisi')),
    (18, '20261014_02_Proje_Fotograf_Baglantisi_Projeler_Altina',
         EXISTS (SELECT 1 FROM kod f JOIN kod p ON p."Id" = f."ParentId" AND p."Kod" = 'projeler' WHERE f."Kod" = 'proje-fotograf-baglantisi'))
) AS t(sira, betik, uygulandi)
ORDER BY sira;
