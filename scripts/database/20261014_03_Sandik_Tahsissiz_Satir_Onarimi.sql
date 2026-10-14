-- =====================================================================================
-- Sandık tahsisi olmayan çeki satırlarını, sandık numaralarının gösterdiği sandığa bağlar (veri onarımı).
--
-- "Çeki verisi düzenle" ekranı, hiç sandık tahsisi olmayan (çekide sandık numarası boş gelmiş) bir satıra
-- sandık numarası verildiğinde hedef sandıkta tahsis oluşturmuyordu. Ürün sandığa yalnız numarasıyla bağlı
-- kalıyordu: sandık kartı, 3K ekranı ve lokasyon kuralı onu okuma anında sandıkta gösteriyor (sentetik içerik),
-- Depo Durumu sayımı ise gerçek tahsisi okuduğu için sandığı saymıyordu. Kod düzeltildi; bu betik kalan
-- kayıtları onarır.
--
-- Eşleşme, okuma ekranlarının kuralıyla aynıdır: satırın etkin sandık numarası (Fiili; boşsa çekide geçen,
-- boşluk kırpılmış, büyük/küçük harf duyarsız) aynı projede TEK bir sandıkla eşleşmelidir. Tahsis değerleri
-- ekranların bugün gösterdiği ve 3K senkronunun üreteceği değerlerdir:
--   Konulan = Gelen + Stok + Proje + Tedarikçi - Başka projeye verilen (en az 0)
--   Tahsis  = İstenen (konulanın altına inmez), Eksik = Tahsis - Konulan
--   Stok / Proje / Tedarikçi kırılımı bu sırayla konulana sığacak kadar
-- Böylece kartlar ve 3K ekranı değişmez, yalnız depo sayımı düzelir.
--
-- Sevk edilmiş sandıklara dokunulmaz (sevk kaydı değişmesin). Eşleşmeyen, birden çok sandıkla eşleşen veya
-- sevk edilmiş sandığı gösteren satırlar NOTICE olarak listelenir ve olduğu gibi kalır.
-- Tekrar çalıştırılabilir: tahsisi olan satıra dokunmaz; onarılacak satır kalmadıysa hiçbir şey değişmez.
-- Hareket geçmişine yazılmaz (kullanıcı yok); oluşturulan tahsisler CreatedBy = betik adı ile ayırt edilir.
-- =====================================================================================
BEGIN;

CREATE TEMP TABLE tahsissiz_satirlar ON COMMIT DROP AS
SELECT cs."Id" AS satir_id,
       c."ProjeId" AS proje_id,
       UPPER(TRIM(COALESCE(NULLIF(TRIM(cs."FiiliSandikNo"), ''), cs."CekideGecenSandikNo", ''))) AS sandik_anahtari
FROM "CekiSatirlari" cs
JOIN "Cekiler" c ON c."Id" = cs."CekiId"
WHERE NOT EXISTS (SELECT 1 FROM "SandikIcerikleri" i WHERE i."CekiSatiriId" = cs."Id")
  AND COALESCE(NULLIF(TRIM(cs."FiiliSandikNo"), ''), NULLIF(TRIM(cs."CekideGecenSandikNo"), '')) IS NOT NULL;

CREATE TEMP TABLE sandik_eslesmeleri ON COMMIT DROP AS
SELECT t.satir_id,
       t.proje_id,
       t.sandik_anahtari,
       COUNT(s."Id") AS sandik_sayisi,
       MIN(s."Id") AS sandik_id,
       BOOL_OR(s."DurumId" = 4) AS sevk_edilmis
FROM tahsissiz_satirlar t
LEFT JOIN "Sandiklar" s ON s."ProjeId" = t.proje_id AND UPPER(TRIM(s."SandikNo")) = t.sandik_anahtari
GROUP BY t.satir_id, t.proje_id, t.sandik_anahtari;

INSERT INTO "SandikIcerikleri"
    ("SandikId", "CekiSatiriId", "TahsisMiktari", "KonulanAdet", "EksikAdet",
     "StokKarsilanan", "ProjeKarsilanan", "TedarikciKarsilanan", "Miktar", "BirimId", "CreatedDate", "CreatedBy")
SELECT e.sandik_id, cs."Id", d.tahsis, d.konulan, GREATEST(d.tahsis - d.konulan, 0),
       d.stok, d.proje, LEAST(GREATEST(cs."TedarikciKarsilanan", 0), GREATEST(d.konulan - d.stok - d.proje, 0)),
       0, cs."BirimId", now() AT TIME ZONE 'Europe/Istanbul', '20261014_03_Sandik_Tahsissiz_Satir_Onarimi.sql'
FROM sandik_eslesmeleri e
JOIN "CekiSatirlari" cs ON cs."Id" = e.satir_id
CROSS JOIN LATERAL (
    SELECT GREATEST(cs."GelenMiktar" + cs."StokKarsilanan" + cs."ProjeKarsilanan" + cs."TedarikciKarsilanan"
                    - cs."ProjeGonderilen", 0) AS konulan
) k
CROSS JOIN LATERAL (
    SELECT LEAST(GREATEST(cs."StokKarsilanan", 0), k.konulan) AS stok
) st
CROSS JOIN LATERAL (
    SELECT k.konulan,
           GREATEST(cs."IstenenAdet", k.konulan) AS tahsis,
           st.stok,
           LEAST(GREATEST(cs."ProjeKarsilanan", 0), k.konulan - st.stok) AS proje
) d
WHERE e.sandik_sayisi = 1 AND NOT e.sevk_edilmis;

DO $$
DECLARE
    satir record;
BEGIN
    FOR satir IN
        SELECT p."ProjeNo", cs."SiraNo", cs."BarkodNo", s."SandikNo", i."KonulanAdet", i."TahsisMiktari"
        FROM "SandikIcerikleri" i
        JOIN "CekiSatirlari" cs ON cs."Id" = i."CekiSatiriId"
        JOIN "Sandiklar" s ON s."Id" = i."SandikId"
        JOIN "Projeler" p ON p."Id" = s."ProjeId"
        JOIN sandik_eslesmeleri e ON e.satir_id = cs."Id"
        WHERE i."CreatedBy" = '20261014_03_Sandik_Tahsissiz_Satir_Onarimi.sql'
        ORDER BY p."ProjeNo", cs."SiraNo"
    LOOP
        RAISE NOTICE 'Tahsis oluşturuldu: proje %, sıra %, barkod %, sandık % (konulan %, tahsis %)',
            satir."ProjeNo", satir."SiraNo", satir."BarkodNo", satir."SandikNo", satir."KonulanAdet", satir."TahsisMiktari";
    END LOOP;

    FOR satir IN
        SELECT p."ProjeNo", cs."SiraNo", cs."BarkodNo", e.sandik_anahtari,
               CASE WHEN e.sandik_sayisi = 0 THEN 'bu numarada sandık yok'
                    WHEN e.sandik_sayisi > 1 THEN 'aynı numarada birden çok sandık var'
                    ELSE 'sandık sevk edilmiş' END AS neden
        FROM sandik_eslesmeleri e
        JOIN "CekiSatirlari" cs ON cs."Id" = e.satir_id
        JOIN "Projeler" p ON p."Id" = e.proje_id
        WHERE e.sandik_sayisi <> 1 OR e.sevk_edilmis
        ORDER BY p."ProjeNo", cs."SiraNo"
    LOOP
        RAISE NOTICE 'Atlandı (%): proje %, sıra %, barkod %, sandık numarası %',
            satir.neden, satir."ProjeNo", satir."SiraNo", satir."BarkodNo", satir.sandik_anahtari;
    END LOOP;
END $$;

COMMIT;
