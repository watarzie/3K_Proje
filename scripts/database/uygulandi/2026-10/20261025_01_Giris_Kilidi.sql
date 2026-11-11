-- =====================================================================================
-- 28 · Hesap bazlı giriş kilidi (25 Ekim 2026): aynı hesaba farklı IP'lerden sınırsız parola denemesini durdurur.
--
-- "Kullanicilar" tablosuna iki kolon eklenir:
-- 1) "BasarisizGirisSayisi" (integer, NOT NULL, varsayılan 0, >= 0): son başarılı girişten bu yana art arda hatalı
--    parola sayısı. Uygulama tek UPDATE ile artırır; başarılı girişte ve yönetici parolayı değiştirince sıfırlanır.
-- 2) "GirisKilitBitisTarihi" (timestamp, boş olabilir, Türkiye saati): 5. hatalı denemede 15 dakika sonrasına kurulur;
--    bu ana kadar doğru parola da reddedilir. Süre dolunca sonraki hatalı deneme sayacı 1'den başlatır.
-- Var olan bütün kullanıcılar sayaç 0, kilitsiz başlar. İzin değişmez.
--
-- Yıkıcı değildir, tekrar çalıştırılabilir: kolon ve kısıt varsa eklenmez, var olan sayaç/kilit değerine dokunulmaz.
-- Sabit varsayılanlı kolon eklemek PostgreSQL 11+'da tabloyu yeniden yazmaz. Yeni API'den ÖNCE uygulanır: yeni API
-- kullanıcıyı okurken bu kolonları kullanır (yoksa giriş dahil kullanıcı okuyan her uç hata verir). Eski API
-- çalışırken uygulanabilir (eski API kolonları okumaz). Finans/Ambalaj tablosuna dokunmaz.
-- =====================================================================================
BEGIN;

ALTER TABLE "Kullanicilar" ADD COLUMN IF NOT EXISTS "BasarisizGirisSayisi" integer NOT NULL DEFAULT 0;
ALTER TABLE "Kullanicilar" ADD COLUMN IF NOT EXISTS "GirisKilitBitisTarihi" timestamp without time zone;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint
                   WHERE conrelid = '"Kullanicilar"'::regclass AND conname = 'CK_Kullanicilar_BasarisizGirisSayisi') THEN
        ALTER TABLE "Kullanicilar"
            ADD CONSTRAINT "CK_Kullanicilar_BasarisizGirisSayisi" CHECK ("BasarisizGirisSayisi" >= 0);
    END IF;
END $$;

COMMIT;
