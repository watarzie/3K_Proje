-- =====================================================================================
-- 34 numaralı betiğin (20261112_01_Saha_Iptal_Proje_Sevk_Durumu.sql) yaptığı durum değişikliğini geri alır.
-- Önce geri-alma/20261112_01_Yedek.sql çalıştırılmış olmalıdır; yedek tablo yoksa hiçbir şey değişmez ve hata verir.
--
-- Yalnız onarım betiğinin değiştirdiği ve sonradan başka bir işlemle değişmemiş projeler geri alınır:
-- DurumId = 5 ve UpdatedBy = betik adı. Bu projelerin DurumId, UpdatedDate ve UpdatedBy alanları yedekteki değerine döner.
-- Sonradan uygulamada değişen (UpdatedBy farklı) proje atlanır ve NOTICE olarak listelenir.
--
-- Not: Yeni API çalışıyorsa, bu projelerde sahadaki bir Grid işlemi senkronu tetiklediğinde S-27 kuralı durumu yeniden
-- Sevk Edildi yapar. Kalıcı geri dönüş için API de önceki sürüme alınmalıdır.
-- Tekrar çalıştırılabilir: geri alınacak proje kalmadıysa hiçbir şey değişmez.
-- =====================================================================================
-- Yedek yoksa transaction açılmadan durur (oturum yarım transaction'da kalmaz).
DO $$
BEGIN
    IF to_regclass('"_Yedek_20261112_01_ProjeDurum"') IS NULL THEN
        RAISE EXCEPTION 'Yedek tablosu "_Yedek_20261112_01_ProjeDurum" yok; geri alma yapılmadı.';
    END IF;
END $$;

BEGIN;

CREATE TEMP TABLE geri_alinan ON COMMIT DROP AS
SELECT y."ProjeId", y."ProjeNo", y."DurumId"
FROM "_Yedek_20261112_01_ProjeDurum" y
JOIN "Projeler" p ON p."Id" = y."ProjeId"
WHERE p."DurumId" = 5 AND p."UpdatedBy" = '20261112_01_Saha_Iptal_Proje_Sevk_Durumu.sql';

UPDATE "Projeler" p
SET "DurumId" = y."DurumId",
    "UpdatedDate" = y."UpdatedDate",
    "UpdatedBy" = y."UpdatedBy"
FROM "_Yedek_20261112_01_ProjeDurum" y
WHERE p."Id" = y."ProjeId"
  AND p."Id" IN (SELECT "ProjeId" FROM geri_alinan);

DO $$
DECLARE
    satir record;
BEGIN
    FOR satir IN SELECT "ProjeId", "ProjeNo", "DurumId" FROM geri_alinan ORDER BY "ProjeNo" LOOP
        RAISE NOTICE 'Geri alındı: % (Id %), durum -> %', satir."ProjeNo", satir."ProjeId", satir."DurumId";
    END LOOP;
    FOR satir IN
        SELECT y."ProjeId", y."ProjeNo", p."DurumId", p."UpdatedBy"
        FROM "_Yedek_20261112_01_ProjeDurum" y
        JOIN "Projeler" p ON p."Id" = y."ProjeId"
        WHERE p."DurumId" <> y."DurumId"
          AND y."ProjeId" NOT IN (SELECT "ProjeId" FROM geri_alinan)
        ORDER BY y."ProjeNo"
    LOOP
        RAISE NOTICE 'Atlandı (sonradan değişmiş): % (Id %), şimdiki durum %, son değiştiren %',
            satir."ProjeNo", satir."ProjeId", satir."DurumId", satir."UpdatedBy";
    END LOOP;
    RAISE NOTICE 'Toplam % proje geri alındı.', (SELECT COUNT(*) FROM geri_alinan);
END $$;

COMMIT;
