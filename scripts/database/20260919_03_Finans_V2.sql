-- Finans V2 / PostgreSQL. Uygulama DB'sinde otomatik çalıştırılmaz.
-- Önce 20260919_03_Finans_Onizleme.sql; bakım penceresi + DB yedeği zorunludur.
-- Transaction hata verirse hiçbir şema/veri değişikliği commit edilmez.
BEGIN;
SELECT pg_advisory_xact_lock(319202603);
CREATE TABLE IF NOT EXISTS "FinansV2GecisYedegi" (
  "VarlikTuru" text NOT NULL, "VarlikId" integer NOT NULL, "Snapshot" jsonb NOT NULL,
  "KayitZamani" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'Europe/Istanbul'),
  PRIMARY KEY ("VarlikTuru", "VarlikId")
);
INSERT INTO "FinansV2GecisYedegi" SELECT 'FinansIsKaydi', "Id", to_jsonb(x), now() AT TIME ZONE 'Europe/Istanbul' FROM "FinansIsKayitlari" x ON CONFLICT DO NOTHING;
INSERT INTO "FinansV2GecisYedegi" SELECT 'FinansSiparisKalemi', "Id", to_jsonb(x), now() AT TIME ZONE 'Europe/Istanbul' FROM "FinansSiparisKalemleri" x ON CONFLICT DO NOTHING;
INSERT INTO "FinansV2GecisYedegi" SELECT 'FinansFaturaKalemi', "Id", to_jsonb(x), now() AT TIME ZONE 'Europe/Istanbul' FROM "FinansFaturaKalemleri" x ON CONFLICT DO NOTHING;
INSERT INTO "FinansV2GecisYedegi" SELECT 'FinansFatura', "Id", to_jsonb(x), now() AT TIME ZONE 'Europe/Istanbul' FROM "FinansFaturalari" x ON CONFLICT DO NOTHING;
INSERT INTO "FinansV2GecisYedegi" SELECT 'FinansGider', "Id", to_jsonb(x), now() AT TIME ZONE 'Europe/Istanbul' FROM "FinansGiderleri" x ON CONFLICT DO NOTHING;
ALTER TABLE "FinansGiderleri" ADD COLUMN IF NOT EXISTS "FinansTarihi" timestamp without time zone;
UPDATE "FinansGiderleri" SET "FinansTarihi" = CASE WHEN date_trunc('month', "Tarih") = "FinansDonemi" THEN "Tarih" ELSE "FinansDonemi" END WHERE "FinansTarihi" IS NULL;
ALTER TABLE "FinansGiderleri" ALTER COLUMN "FinansTarihi" SET NOT NULL;

ALTER TABLE "FinansIsKayitlari" ADD COLUMN IF NOT EXISTS "FinansTarihi" timestamp without time zone;
ALTER TABLE "FinansIsKayitlari" ADD COLUMN IF NOT EXISTS "FinansTarihiManuel" boolean NOT NULL DEFAULT false;
ALTER TABLE "FinansIsKayitlari" ADD COLUMN IF NOT EXISTS "FinansMiktariManuel" boolean NOT NULL DEFAULT false;
ALTER TABLE "FinansIsKayitlari" ADD COLUMN IF NOT EXISTS "ManuelNetTutar" numeric(18,2);
ALTER TABLE "FinansIsKayitlari" ADD COLUMN IF NOT EXISTS "TarifeIdSnapshot" integer;
ALTER TABLE "FinansIsKayitlari" ADD COLUMN IF NOT EXISTS "SandikCinsi" integer;
ALTER TABLE "FinansIsKayitlari" ADD COLUMN IF NOT EXISTS "SablonSurumId" integer;
ALTER TABLE "FinansIsKayitlari" ADD COLUMN IF NOT EXISTS "AlanDegerleriJson" jsonb;
ALTER TABLE "FinansIsKayitlari" ADD COLUMN IF NOT EXISTS "FiyatBilesenleriJson" jsonb;
ALTER TABLE "FinansIsKayitlari" ADD COLUMN IF NOT EXISTS "KaynakBileseni" varchar(20) NOT NULL DEFAULT 'NET';
-- V1 finans miktarları tarihsel snapshot'tır; V2 üretim sıfırlaması/ölçüsü eski bedeli değiştiremez.
-- Yalnız V1 yedeği bulunan satırlar korunur; yeni V2 satırlarına dokunulmaz.
UPDATE "FinansIsKayitlari" w SET "FinansMiktariManuel" = true
FROM "FinansV2GecisYedegi" b WHERE b."VarlikTuru"='FinansIsKaydi' AND b."VarlikId"=w."Id"
AND NOT (b."Snapshot" ? 'FinansMiktariManuel');
-- Eski dönemin bilinen gününü sakla; üretim tarihi/dönemi değişmez, bilinmeyen gün üretilmez.
UPDATE "FinansIsKayitlari" SET "FinansTarihi" = "FinansDonemi", "FinansTarihiManuel" = true WHERE "FinansTarihi" IS NULL;
ALTER TABLE "FinansIsKayitlari" ALTER COLUMN "FinansTarihi" SET NOT NULL;
DROP INDEX IF EXISTS "IX_FinansIsKayitlari_KaynakTuru_KaynakKayitId";
CREATE UNIQUE INDEX IF NOT EXISTS "IX_FinansIsKayitlari_KaynakBileseniV2" ON "FinansIsKayitlari" ("KaynakTuru", "KaynakKayitId", "KaynakBileseni") WHERE "KaynakKayitId" IS NOT NULL;
ALTER TABLE "FinansSiparisleri" ADD COLUMN IF NOT EXISTS "ParaBirimi" varchar(3);
UPDATE "FinansSiparisleri" p SET "ParaBirimi" = q.currency FROM (SELECT "FinansSiparisId" id, min("ParaBirimiSnapshot") currency FROM "FinansSiparisKalemleri" GROUP BY "FinansSiparisId" HAVING count(DISTINCT "ParaBirimiSnapshot") = 1) q WHERE p."Id" = q.id AND p."ParaBirimi" IS NULL;
ALTER TABLE "FinansSiparisKalemleri" ADD COLUMN IF NOT EXISTS "TutarBazli" boolean NOT NULL DEFAULT false;
ALTER TABLE "FinansFaturaKalemleri" ADD COLUMN IF NOT EXISTS "TutarBazli" boolean NOT NULL DEFAULT false;
ALTER TABLE "FinansSiparisKalemleri" DROP CONSTRAINT IF EXISTS "CK_FinansSiparisKalemleri_Miktar";
ALTER TABLE "FinansSiparisKalemleri" ADD CONSTRAINT "CK_FinansSiparisKalemleri_Miktar" CHECK ("Adet" >= 0 AND "M3" >= 0 AND ("Adet" > 0 OR "M3" > 0 OR "TutarBazli"));
ALTER TABLE "FinansFaturaKalemleri" DROP CONSTRAINT IF EXISTS "CK_FinansFaturaKalemleri_Miktar";
ALTER TABLE "FinansFaturaKalemleri" ADD CONSTRAINT "CK_FinansFaturaKalemleri_Miktar" CHECK ("Adet" >= 0 AND "M3" >= 0 AND ("Adet" > 0 OR "M3" > 0 OR "TutarBazli"));
ALTER TABLE "FinansGiderleri" ADD COLUMN IF NOT EXISTS "BelgeNo" varchar(100);
ALTER TABLE "FinansGiderleri" ADD COLUMN IF NOT EXISTS "AvansMi" boolean NOT NULL DEFAULT false;
ALTER TABLE "FinansGiderleri" ADD COLUMN IF NOT EXISTS "MahsupEdilenAvansId" integer;
ALTER TABLE "FinansDegisiklikGecmisleri" ADD COLUMN IF NOT EXISTS "IslemGrubu" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
ALTER TABLE "FinansDegisiklikGecmisleri" ADD COLUMN IF NOT EXISTS "Referans" varchar(500);
CREATE TABLE IF NOT EXISTS "FinansKaynakBastirmalari" (
 "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY, "CreatedDate" timestamp without time zone NOT NULL, "UpdatedDate" timestamp without time zone, "CreatedBy" text, "UpdatedBy" text,
 "KaynakTuru" varchar(50) NOT NULL, "KaynakKayitId" varchar(150) NOT NULL, "KaynakBileseni" varchar(20) NOT NULL, "Aciklama" varchar(1000) NOT NULL,
 UNIQUE ("KaynakTuru", "KaynakKayitId", "KaynakBileseni")
);
CREATE TABLE IF NOT EXISTS "FinansBelgeleri" (
 "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY, "CreatedDate" timestamp without time zone NOT NULL, "UpdatedDate" timestamp without time zone, "CreatedBy" text, "UpdatedBy" text,
 "HedefTuru" varchar(30) NOT NULL, "HedefId" integer NOT NULL, "Surum" integer NOT NULL, "OrijinalAd" varchar(250) NOT NULL, "GuvenliAd" varchar(80) NOT NULL,
 "Boyut" bigint NOT NULL, "IcerikTuru" varchar(80) NOT NULL, "Hash" varchar(64) NOT NULL, "Yukleyen" varchar(100) NOT NULL, "Icerik" bytea NOT NULL,
 UNIQUE ("HedefTuru", "HedefId", "Surum"), CHECK ("Boyut" > 0 AND "Boyut" <= 10485760)
);
CREATE TABLE IF NOT EXISTS "FinansIsSablonlari" (
 "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY, "CreatedDate" timestamp without time zone NOT NULL, "UpdatedDate" timestamp without time zone, "CreatedBy" text, "UpdatedBy" text,
 "Kod" varchar(80) NOT NULL UNIQUE, "Ad" varchar(200) NOT NULL, "Aktif" boolean NOT NULL
);
CREATE TABLE IF NOT EXISTS "FinansIsSablonSurumleri" (
 "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY, "CreatedDate" timestamp without time zone NOT NULL, "UpdatedDate" timestamp without time zone, "CreatedBy" text, "UpdatedBy" text,
 "FinansIsSablonuId" integer NOT NULL REFERENCES "FinansIsSablonlari"("Id") ON DELETE RESTRICT,
 "Surum" integer NOT NULL, "AlanlarJson" jsonb NOT NULL, UNIQUE ("FinansIsSablonuId", "Surum")
);
DO $$ BEGIN
 IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_FinansV2_SablonSurumu') THEN ALTER TABLE "FinansIsKayitlari" ADD CONSTRAINT "FK_FinansV2_SablonSurumu" FOREIGN KEY ("SablonSurumId") REFERENCES "FinansIsSablonSurumleri"("Id") ON DELETE RESTRICT; END IF;
 IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_FinansV2_GiderAvans') THEN ALTER TABLE "FinansGiderleri" ADD CONSTRAINT "FK_FinansV2_GiderAvans" FOREIGN KEY ("MahsupEdilenAvansId") REFERENCES "FinansGiderleri"("Id") ON DELETE RESTRICT; END IF;
END $$;
-- Belge mutabakatı mevcut satır snapshot oranıyla dağıtılır; son satır kuruş kalıntısını alır.
-- Kaynak miktarlar, belge tarihi/tutarı ve PO fiyatları değişmez. Orijinal satırlar yukarıdaki arşivdedir.
WITH allocation AS (
 SELECT l."Id", l."FinansFaturaId", f."BelgeNetTutarSnapshot" net, f."BelgeKdvTutariSnapshot" vat,
 row_number() OVER (PARTITION BY l."FinansFaturaId" ORDER BY l."Id") rn,
 count(*) OVER (PARTITION BY l."FinansFaturaId") n,
 CASE WHEN sum(l."NetTutarSnapshot") OVER (PARTITION BY l."FinansFaturaId") > 0 THEN
 round(f."BelgeNetTutarSnapshot" * l."NetTutarSnapshot" / sum(l."NetTutarSnapshot") OVER (PARTITION BY l."FinansFaturaId"), 2) ELSE 0 END share_net,
 CASE WHEN sum(l."NetTutarSnapshot") OVER (PARTITION BY l."FinansFaturaId") > 0 THEN
 round(f."BelgeKdvTutariSnapshot" * l."NetTutarSnapshot" / sum(l."NetTutarSnapshot") OVER (PARTITION BY l."FinansFaturaId"), 2) ELSE 0 END share_vat
 FROM "FinansFaturaKalemleri" l JOIN "FinansFaturalari" f ON f."Id"=l."FinansFaturaId"
 WHERE f."BelgeNetTutarSnapshot" IS NOT NULL AND f."BelgeKdvTutariSnapshot" IS NOT NULL
 AND NOT EXISTS (SELECT 1 FROM "FinansDegisiklikGecmisleri" a WHERE a."VarlikTuru"='FinansFatura' AND a."VarlikId"=f."Id" AND a."Islem"='V2 Mutabakat Geçişi')
), reconciled AS (
 SELECT "Id", CASE WHEN rn=n THEN net - coalesce(sum(share_net) FILTER (WHERE rn<n) OVER (PARTITION BY "FinansFaturaId"),0) ELSE share_net END new_net,
 CASE WHEN rn=n THEN vat - coalesce(sum(share_vat) FILTER (WHERE rn<n) OVER (PARTITION BY "FinansFaturaId"),0) ELSE share_vat END new_vat FROM allocation
)
UPDATE "FinansFaturaKalemleri" l SET "NetTutarSnapshot"=r.new_net, "KdvTutariSnapshot"=r.new_vat, "ToplamTutarSnapshot"=r.new_net+r.new_vat FROM reconciled r WHERE l."Id"=r."Id";
INSERT INTO "FinansDegisiklikGecmisleri" ("VarlikTuru","VarlikId","Islem","AlanAdi","EskiDeger","YeniDeger","Aciklama","IslemYapan","CreatedDate","IslemGrubu")
 SELECT 'FinansFatura', f."Id", 'V2 Mutabakat Geçişi', 'KalemTutarları', NULL, f."BelgeToplamTutarSnapshot"::text,
 'Kabul edilmiş belge toplamı mevcut satır snapshot oranıyla dağıtıldı; orijinal satırlar FinansV2GecisYedegi tablosunda korundu.', 'MIGRATION', now() AT TIME ZONE 'Europe/Istanbul', '00000000-0000-0000-0000-000000000000'
 FROM "FinansFaturalari" f WHERE f."BelgeNetTutarSnapshot" IS NOT NULL AND NOT EXISTS
 (SELECT 1 FROM "FinansDegisiklikGecmisleri" a WHERE a."VarlikTuru"='FinansFatura' AND a."VarlikId"=f."Id" AND a."Islem"='V2 Mutabakat Geçişi');
DO $$ BEGIN
 IF EXISTS (SELECT 1 FROM "FinansIsKayitlari" w JOIN "FinansSiparisKalemleri" l ON l."FinansIsKaydiId"=w."Id" JOIN "FinansSiparisleri" p ON p."Id"=l."FinansSiparisId" WHERE NOT p."IptalEdildi" GROUP BY w."Id" HAVING sum(l."NetTutarSnapshot") > coalesce(w."ManuelNetTutar", round(w."BirimFiyatSnapshot" * CASE w."FiyatlandirmaBirimiSnapshot" WHEN 1 THEN w."Adet" WHEN 2 THEN w."ToplamM3" ELSE 1 END,2))) THEN
 RAISE EXCEPTION 'Mevcut PO toplamı işin tarihsel net bedelini aşıyor. Yetkili belge uzlaştırması olmadan geçiş yapılmadı.';
 END IF;
 IF EXISTS (SELECT 1 FROM "FinansSiparisKalemleri" p JOIN "FinansFaturaKalemleri" l ON l."FinansSiparisKalemiId"=p."Id" JOIN "FinansFaturalari" f ON f."Id"=l."FinansFaturaId" WHERE NOT f."IptalEdildi" GROUP BY p."Id" HAVING sum(l."NetTutarSnapshot")>p."NetTutarSnapshot" OR sum(l."ToplamTutarSnapshot")>p."ToplamTutarSnapshot") THEN
 RAISE EXCEPTION 'Mevcut belge mutabakatı PO kapasitesini aşıyor. Önizleme raporunu inceleyin; yetkili belge düzeltmesi olmadan geçiş yapılmadı.';
 END IF;
END $$;
COMMIT;
