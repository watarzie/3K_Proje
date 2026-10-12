-- =====================================================================================
-- "postgres-salt-okunur" MCP sunucusu için yalnız okuma yetkili rol (Claude Code teşhis sorguları).
--
-- KULLANICI ÇALIŞTIRIR (pgAdmin'de sorgu aracını 3K_Db düğümünden açın). Claude bu betiği çalıştırmaz.
-- MCP paketi (@modelcontextprotocol/server-postgres) sorguları salt okunur transaction'da çalıştırır ama paket artık
-- desteklenmiyor; asıl güvence bu roldür: yalnız SELECT yetkisi, varsayılan salt okunur transaction, zaman aşımı.
--
-- 1) Aşağıdaki '<GÜÇLÜ-PAROLA>' yerine güçlü bir parola yazıp çalıştırın.
-- 2) Windows kullanıcı ortam değişkeni (yalnız sizin makinenizde; depoya yazılmaz):
--      THREEK_READONLY_PG_URL = postgresql://claude_okuyucu:<parola>@localhost:5432/3K_Db
-- 3) Claude Code'u yeniden başlatın; ilk açılışta .mcp.json sunucusunu onaylayın.
-- =====================================================================================
BEGIN;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'claude_okuyucu') THEN
        CREATE ROLE claude_okuyucu LOGIN PASSWORD '<GÜÇLÜ-PAROLA>'
            NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION CONNECTION LIMIT 3;
    END IF;
END $$;

ALTER ROLE claude_okuyucu SET default_transaction_read_only = on;
ALTER ROLE claude_okuyucu SET statement_timeout = '30s';

GRANT CONNECT ON DATABASE "3K_Db" TO claude_okuyucu;
GRANT USAGE ON SCHEMA public TO claude_okuyucu;
GRANT SELECT ON ALL TABLES IN SCHEMA public TO claude_okuyucu;
-- Sonradan eklenen tablolar da okunabilsin (tablo sahibi rol tarafından oluşturulanlar için).
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT ON TABLES TO claude_okuyucu;

-- Kimlik tabloları ve oturum sırları okunmasın (parola özeti, belirteçler, 2FA sırları ve kurtarma kodları).
-- Not: Kullanicilar kapalı olduğu için teşhis sorgularında kullanıcı adı yerine KullaniciId görülür.
DO $$
DECLARE
    t text;
BEGIN
    FOREACH t IN ARRAY ARRAY['Kullanicilar', 'KullaniciIkiFaktorAyarlari', 'IkiFaktorGirisTalepleri', 'IkiFaktorKurtarmaKodlari']
    LOOP
        IF to_regclass(format('public.%I', t)) IS NOT NULL THEN
            EXECUTE format('REVOKE SELECT ON public.%I FROM claude_okuyucu', t);
        END IF;
    END LOOP;
END $$;

COMMIT;
