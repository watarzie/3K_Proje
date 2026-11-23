-- =====================================================================================
-- 38 · İç sandık tipi yönetimi ve "İç Sandık Katlanır" (23 Kasım 2026)
--
-- 1) AmbalajIcSandikSablonlari."Aktif": boolean NOT NULL, varsayılan true. Var olan bütün iç sandık tipleri aktif
--    başlar. Kullanılan tip (ambalaj kaydı, finans işi veya tarife eşleşmesi bağlı) artık silinmez, pasife alınır;
--    pasif tip yeni kayda atanamaz, eski kayıtlarda adıyla görünür.
-- 2) "İç Sandık Katlanır" sandık tipi veritabanında lookup değildir: AmbalajSandikCinsi enum'unda 5 (IcKatlanir)
--    değeridir ve AmbalajUretimKayitlari / AmbalajIcSandikSablonlari / FinansIsKayitlari "SandikCinsi" kolonlarına sayı
--    olarak yazılır. Bu kolonlarda değer kısıtı olmadığı için tohum veya kısıt değişikliği gerekmez. Var olan
--    "Katlanır Sandık" (4) kayıtları dönüştürülmez; elle fiyat kuralları korunur.
--
-- Yıkıcı değildir, tekrar çalıştırılabilir: kolon varsa eklenmez. Sabit varsayılanlı NOT NULL kolon eklemek tabloyu
-- yeniden yazmaz. Yeni API'den önce uygulanır (yeni API bu kolonu okur ve yazar); eski API çalışırken uygulanabilir
-- (eski API kolonu yazmaz, varsayılan true dolar). Finans tablosuna dokunmaz.
-- =====================================================================================
BEGIN;

ALTER TABLE "AmbalajIcSandikSablonlari" ADD COLUMN IF NOT EXISTS "Aktif" boolean NOT NULL DEFAULT true;

-- Betik kaydı (README "Betik kaydı"): bu betiğin bu veritabanında çalıştığını aynı transaction içinde yazar.
CREATE TABLE IF NOT EXISTS "BetikKayitlari" (
    "BetikAdi"       text PRIMARY KEY,
    "UygulamaTarihi" timestamp without time zone NOT NULL,
    "Uygulayan"      text NOT NULL
);
INSERT INTO "BetikKayitlari" ("BetikAdi", "UygulamaTarihi", "Uygulayan")
VALUES ('20261123_01_Ic_Sandik_Tipi_Aktif', now() AT TIME ZONE 'Europe/Istanbul', current_user)
ON CONFLICT ("BetikAdi") DO NOTHING;

COMMIT;
