-- =====================================================================================
-- Betik durumu (SALT OKUNUR): bu klasördeki betiklerden hangisi bağlı veritabanında uygulanmış?
--
-- Hiçbir şey değiştirmez. Canlıya alımdan önce 3K_Db düğümünden açılan sorgu penceresinde çalıştırın; UYGULANMAMIŞ
-- satırlar README sırasıyla uygulanır. Sütunlar: durum; kaynak ("kayıt" = betik "BetikKayitlari"na kendi satırını
-- yazmış, "iz" = kayıt yok ama betiğin bıraktığı kalıcı iz (kolon, tablo, menü düğümü) bulundu); kayit_tarihi.
-- Kayıt tablosu 34 numaralı betikle başladı; 22–29 kayıt yazmaz, izden tanınır. Tablo yoksa sorgu yine çalışır.
-- Satırlar bekleyen/ ve uygulandi/ klasörlerindeki bütün betikleri kapsar (geri-alma/ ve kontrol/ hariç).
-- 1–20 numaralı betikler canlıya uygulandı ve depodan kaldırıldı (da54f6f); burada yalnız depodaki betikler listelenir.
-- 21 boş kaldı (otomatik sahaya aktarım şema değişikliği gerektirmedi).
-- 22 numaralı betik kolonu ve tabloyu birlikte arar: biri eksikse UYGULANMAMIŞ görünür ve betik tekrar çalıştırılır.
-- 23 numaralı betik menü düğümünü (grid-saha-gorunumu) rotasız arar; ilk sürümün rotalı düğümü uygulanmamış sayılır.
-- 24 numaralı betik tabloyu (SandikDuzeltmeKayitlari) ve menü düğümünü (sevk-sonrasi-duzeltme) birlikte arar.
-- 25 numaralı betik ProjeFotografBaglantilari.Tur kolonunu, 26 numaralı betik menü düğümünü (barkod-urun-arama) rotasız
-- arar; ilk sürümün rotalı düğümü uygulanmamış sayılır.
-- 27 numaralı betik Sandiklar.KapanmaTarihi kolonunu, SandikDurumOlaylari ve LookupSandikAcilmaSebepleri tablolarını ve
-- menü düğümünü (sandik-acilis-istatistigi) birlikte arar.
-- 28 numaralı betik Kullanicilar.BasarisizGirisSayisi ve GirisKilitBitisTarihi kolonlarını birlikte arar.
-- 29 numaralı betik menü düğümünü (barkod-etiket) arar.
-- 30–33 feature/yukleme-simulasyonu dalındadır.
-- 34 numaralı betik veri onarımıdır (S-27): sahada iptalle kapanacak Eksik Sevk Edildi normal proje kalmadıysa
-- uygulanmış görünür; UYGULANMAMIŞ ise onarılacak proje vardır. Seçim betiktekiyle aynıdır; biri değişirse öteki de
-- güncellenir (saha_iptal_* CTE'leri).
-- Yeni betik eklenince buraya satır eklenir (CanliyaAlimBetikleriTests dosya listesiyle karşılaştırır); betiğin sonunda
-- README'deki "Betik kaydı" bloğu bulunur. İz satırı yine yazılır: kayıt bloğundan önce elle uygulanmış veritabanı için.
-- =====================================================================================
WITH kolon(tablo, ad) AS (SELECT table_name, column_name FROM information_schema.columns WHERE table_schema = 'public'),
-- Kayıt tablosu henüz yoksa boş okunur (to_regclass); varsa dinamik okunur, böylece sorgu her veritabanında çalışır.
kayit AS (
    SELECT x.ad, x.tarih
    FROM XMLTABLE('/table/row'
             PASSING CASE WHEN to_regclass('"BetikKayitlari"') IS NULL THEN '<table/>'::xml
                          ELSE query_to_xml('SELECT "BetikAdi" AS ad, "UygulamaTarihi" AS tarih FROM "BetikKayitlari"',
                                            false, false, '') END
             COLUMNS ad text PATH 'ad', tarih timestamp PATH 'tarih') x),
saha_iptal_aday AS (
    SELECT p."Id" AS proje_id FROM "Projeler" p
    WHERE p."ProjeTipiId" = 1 AND p."DurumId" = 6
      AND EXISTS (SELECT 1 FROM "Sandiklar" s WHERE s."ProjeId" = p."Id")
      AND NOT EXISTS (SELECT 1 FROM "Sandiklar" s WHERE s."ProjeId" = p."Id" AND (s."DurumId" <> 4 OR s."SevkiyatDuzeltmeAcikMi"))),
saha_iptal_kok AS (
    SELECT a.proje_id, cs."Id" AS satir_id,
           CASE WHEN cs."GridDurumuId" IN (12, 14) THEN 0
                WHEN (cs."HataliMiktar" > 0 OR cs."DurumId" = 21) AND k.ham <= 0 THEN 1
                ELSE GREATEST(k.ham, 0) END AS kalan
    FROM saha_iptal_aday a
    JOIN "Cekiler" c ON c."ProjeId" = a.proje_id
    JOIN "CekiSatirlari" cs ON cs."CekiId" = c."Id"
    CROSS JOIN LATERAL (SELECT cs."IstenenAdet" - cs."GelenMiktar" - cs."StokKarsilanan" - cs."ProjeKarsilanan"
                               - cs."TedarikciKarsilanan" + cs."ProjeGonderilen" - cs."TrafoSevkAdet" AS ham) k
    WHERE cs."KaynakCekiSatiriId" IS NULL),
saha_iptal_katki AS (
    SELECT x.kaynak_id, x.gerceklesen,
           CASE WHEN x.saha_iptal THEN GREATEST(GREATEST(x.aktarilan, 0), x.gerceklesen) ELSE x.gerceklesen END AS kapanis
    FROM (
        SELECT k."KaynakCekiSatiriId" AS kaynak_id, k."Miktar" AS aktarilan, COALESCE(ss."GridDurumuId" = 12, FALSE) AS saha_iptal,
               CASE WHEN k."SevkiyatKapsamindaMi" AND k."Miktar" > 0
                    THEN LEAST(GREATEST(COALESCE(ic.konulan, 0), 0) + GREATEST(COALESCE(ss."TrafoSevkAdet", 0), 0), k."Miktar")
                    ELSE 0 END AS gerceklesen
        FROM "SahaAktarimKalemleri" k
        LEFT JOIN "CekiSatirlari" ss ON ss."Id" = k."SahaCekiSatiriId"
        LEFT JOIN LATERAL (SELECT SUM(si."KonulanAdet") AS konulan
                           FROM "SandikIcerikleri" si WHERE si."CekiSatiriId" = k."SahaCekiSatiriId") ic ON TRUE
        WHERE k."DurumId" NOT IN (6, 7) AND k."KaynakCekiSatiriId" IN (SELECT satir_id FROM saha_iptal_kok)
        UNION ALL
        SELECT cs."KaynakCekiSatiriId", cs."IstenenAdet", cs."GridDurumuId" = 12,
               CASE WHEN COALESCE(ic.sevkli, FALSE) AND cs."IstenenAdet" > 0
                    THEN LEAST(GREATEST(COALESCE(ic.konulan, 0), 0) + GREATEST(cs."TrafoSevkAdet", 0), cs."IstenenAdet")
                    ELSE 0 END
        FROM "CekiSatirlari" cs
        JOIN "Cekiler" c ON c."Id" = cs."CekiId"
        JOIN "Projeler" sp ON sp."Id" = c."ProjeId" AND sp."ProjeTipiId" = 2
        LEFT JOIN LATERAL (SELECT BOOL_OR(s."DurumId" = 4) AS sevkli,
                                  SUM(si."KonulanAdet") FILTER (WHERE s."DurumId" = 4) AS konulan
                           FROM "SandikIcerikleri" si JOIN "Sandiklar" s ON s."Id" = si."SandikId"
                           WHERE si."CekiSatiriId" = cs."Id") ic ON TRUE
        WHERE cs."KaynakCekiSatiriId" IN (SELECT satir_id FROM saha_iptal_kok)
          AND NOT EXISTS (SELECT 1 FROM "SahaAktarimKalemleri" d WHERE d."SahaCekiSatiriId" = cs."Id")) x),
saha_iptal_onarilacak AS (
    SELECT o.proje_id
    FROM (SELECT ks.proje_id, ks.satir_id, ks.kalan,
                 COALESCE(SUM(kt.gerceklesen), 0) AS gerceklesen, COALESCE(SUM(kt.kapanis), 0) AS kapanis
          FROM saha_iptal_kok ks LEFT JOIN saha_iptal_katki kt ON kt.kaynak_id = ks.satir_id
          GROUP BY ks.proje_id, ks.satir_id, ks.kalan) o
    GROUP BY o.proje_id
    HAVING BOOL_AND(o.kalan <= o.kapanis) AND BOOL_OR(o.kalan > o.gerceklesen))
SELECT t.sira, t.betik,
       CASE WHEN k.ad IS NOT NULL THEN 'uygulanmış'
            WHEN t.uygulandi IS NULL THEN 'tespit edilemez'
            WHEN t.uygulandi THEN 'uygulanmış'
            ELSE 'UYGULANMAMIŞ' END AS durum,
       CASE WHEN k.ad IS NOT NULL THEN 'kayıt' WHEN t.uygulandi THEN 'iz' ELSE '-' END AS kaynak,
       k.tarih AS kayit_tarihi
FROM (VALUES
    (22, '20261021_02_Sahadan_Sahaya_Tasima',       EXISTS (SELECT 1 FROM kolon WHERE tablo = 'SahaAktarimlari' AND ad = 'IslemAnahtari')
                                                    AND EXISTS (SELECT 1 FROM kolon WHERE tablo = 'SahaTasimaKalemleri' AND ad = 'KaynakCekiSatiriId')),
    (23, '20261023_01_Grid_Saha_Gorunumu_Yetkisi',  EXISTS (SELECT 1 FROM "MenuTanimlari" WHERE "Kod" = 'grid-saha-gorunumu' AND "Route" IS NULL)),
    (24, '20261023_02_Sevk_Sonrasi_Duzeltme',       EXISTS (SELECT 1 FROM kolon WHERE tablo = 'SandikDuzeltmeKayitlari' AND ad = 'AcilisGoruntusu')
                                                    AND EXISTS (SELECT 1 FROM "MenuTanimlari" WHERE "Kod" = 'sevk-sonrasi-duzeltme')),
    (25, '20261024_01_Proje_Baglanti_Turu',         EXISTS (SELECT 1 FROM kolon WHERE tablo = 'ProjeFotografBaglantilari' AND ad = 'Tur')),
    (26, '20261024_02_Barkod_Urun_Arama_Yetkisi',   EXISTS (SELECT 1 FROM "MenuTanimlari" WHERE "Kod" = 'barkod-urun-arama' AND "Route" IS NULL)),
    (27, '20261024_03_Sandik_Acilis_Kapanis_Gecmisi', EXISTS (SELECT 1 FROM kolon WHERE tablo = 'Sandiklar' AND ad = 'KapanmaTarihi')
                                                    AND EXISTS (SELECT 1 FROM kolon WHERE tablo = 'SandikDurumOlaylari' AND ad = 'Kaynak')
                                                    AND EXISTS (SELECT 1 FROM kolon WHERE tablo = 'LookupSandikAcilmaSebepleri' AND ad = 'Deger')
                                                    AND EXISTS (SELECT 1 FROM "MenuTanimlari" WHERE "Kod" = 'sandik-acilis-istatistigi')),
    (28, '20261025_01_Giris_Kilidi',                EXISTS (SELECT 1 FROM kolon WHERE tablo = 'Kullanicilar' AND ad = 'BasarisizGirisSayisi')
                                                    AND EXISTS (SELECT 1 FROM kolon WHERE tablo = 'Kullanicilar' AND ad = 'GirisKilitBitisTarihi')),
    (29, '20261026_01_Barkod_Etiket_Yetkisi',       EXISTS (SELECT 1 FROM "MenuTanimlari" WHERE "Kod" = 'barkod-etiket')),
    (34, '20261112_01_Saha_Iptal_Proje_Sevk_Durumu', NOT EXISTS (SELECT 1 FROM saha_iptal_onarilacak))
) AS t(sira, betik, uygulandi)
LEFT JOIN kayit k ON k.ad = t.betik
ORDER BY t.sira;
