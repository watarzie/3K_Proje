-- =====================================================================================
-- FİNANS VE AMBALAJ TABLOLARINI SIFIRLAMA (yeni kurulum)
--
-- Bütün Finans* ve Ambalaj* tabloları VERİLERİYLE silinir ve güncel EF modelinden (tablolar, indeksler,
-- kısıtlar ve başlangıç verisi) yeniden oluşturulur. Eski veri taşınmaz; tablolar boş başlar.
--
-- Yeniden oluşturulan 24 tablo:
--   AmbalajIcSandikSablonlari, AmbalajKaynakSenkronizasyonKuyrugu, FinansBelgeleri, FinansDegisiklikGecmisleri
--   FinansGiderKategorileri, FinansKaynakBastirmalari, FinansParaBirimleri, FinansParametreleri
--   FinansSiparisleri, FinansUrunleri, FinansGiderKalemleri, FinansFaturalari
--   FinansFiyatTarifeleri, FinansUrunEslesmeleri, AmbalajUretimFormuSurumleri, AmbalajUretimKayitlari
--   FinansDuzenliIsleri, FinansGiderleri, AmbalajUretimFormuKayitlari, AmbalajUretimGerceklesmeleri
--   AmbalajUretimHareketleri, FinansIsKayitlari, FinansSiparisKalemleri, FinansFaturaKalemleri
-- Yalnız silinen (modelde artık olmayan) tablolar:
--   FinansIsSablonlari, FinansIsSablonSurumleri  (F13: iş şablonu sistemi kaldırıldı)
--   FinansV2GecisYedegi, AmbalajTalepEdenler     (kullanılmayan eski tablolar)
-- Başlangıç verisi: para birimleri TRY ve EUR (F1; yenisi Finans > Para Birimleri ekranından eklenir),
--   vade parametreleri: varsayılan vade 30 gün, vade uyarısı 7 gün (D3).
--
-- Dokunulmayanlar: Projeler, Sandiklar, Cekiler, kullanıcı/rol/izin (MenuTanimlari, RolYetkileri,
--   KullaniciYetkileri) ve diğer bütün tablolar. Başka tablolardan Finans/Ambalaj tablolarına yabancı
--   anahtar yoktur. Ambalaj kaynak kuyruğu fonksiyonu ambalaj_kaynak_kuyruga_ekle(integer[]) ve
--   Projeler/Sandiklar üzerindeki TR_AmbalajKaynak_* tetikleyicileri korunur; kuyruk tablosu yeniden
--   oluşturulduğu için çalışmaya devam eder. Bir projenin ambalaj kayıtları, proje Ambalaj planına
--   eklendiğinde veya sandıkları değiştiğinde kaynaktan yeniden oluşur.
--
-- GÜVENLİK: Onay, sıfırlanacak veritabanının adıdır. Betik yalnız bağlı olduğu veritabanının adı onayla aynıysa
--   çalışır; onay yoksa ya da başka bir veritabanına (ör. pgAdmin'in varsayılan "postgres" veritabanına) bağlıysa
--   hiçbir şey değiştirmeden durur.
--   psql -d 3K_Db ... -v ON_ERROR_STOP=1 -c "SET uc_k.finans_ambalaj_sifirla = '3K_Db'" -f <bu dosya>
--   (pgAdmin: sorgu aracını 3K_Db veritabanından açın; aynı pencerede önce SET satırını, sonra bu dosyayı çalıştırın.)
-- Bağlı veritabanının adı ve silinecek tabloların satır sayıları NOTICE olarak yazılır. Betik tek transaction'dır; herhangi bir
-- adımda hata olursa hiçbir değişiklik kalmaz. Çalıştırmadan önce API'yi durdurun ve yedek alın.
-- Tekrar çalıştırılabilir: her çalıştırmada tablolar boş ve güncel şemayla yeniden kurulur.
--
-- Tablo, indeks ve başlangıç verisi tanımları EF modelinin `dotnet ef dbcontext script` çıktısından alınmıştır ve
-- GÜNCEL modeli kurar: sonradan gelen 20261008_02 (ahşap/kontra m³) ve 20261013_01 (malzemeli tarife) kolonları da
-- buraya işlendi; sıfırlamadan sonra ek betik gerekmez (çalıştırılırlarsa hiçbir şey değiştirmezler). Finans/Ambalaj
-- tablolarına yeni şema betiği eklenince bu betik de güncellenir; CanliyaAlimBetikleriTests sıfırlama sonrası şemayı
-- EF modeliyle karşılaştırır.
-- SET uc_k.finans_ambalaj_sifirla = '3K_Db';
-- =====================================================================================
BEGIN;

DO $$
DECLARE
    onay text := current_setting('uc_k.finans_ambalaj_sifirla', true);
BEGIN
    IF onay IS DISTINCT FROM current_database() THEN
        RAISE EXCEPTION 'Onay yok veya bağlı veritabanıyla eşleşmiyor (bağlı veritabanı: "%", onay: "%"). Finans ve Ambalaj tabloları verileriyle silinecek; sıfırlanacak veritabanına bağlanıp önce SET uc_k.finans_ambalaj_sifirla = ''<veritabanı adı>''; çalıştırın.',
            current_database(), coalesce(onay, '');
    END IF;
END $$;

-- Silinecek verinin raporu.
DO $$
DECLARE
    tablo text;
    adet bigint;
    bulunan integer := 0;
BEGIN
    RAISE NOTICE 'Sıfırlanan veritabanı: %', current_database();
    FOREACH tablo IN ARRAY ARRAY[
        'FinansFaturaKalemleri', 'FinansSiparisKalemleri', 'FinansIsKayitlari',
        'AmbalajUretimHareketleri', 'AmbalajUretimGerceklesmeleri', 'AmbalajUretimFormuKayitlari',
        'FinansGiderleri', 'FinansDuzenliIsleri', 'AmbalajUretimKayitlari',
        'AmbalajUretimFormuSurumleri', 'FinansUrunEslesmeleri', 'FinansFiyatTarifeleri',
        'FinansFaturalari', 'FinansGiderKalemleri', 'FinansUrunleri',
        'FinansSiparisleri', 'FinansParametreleri', 'FinansParaBirimleri',
        'FinansKaynakBastirmalari', 'FinansGiderKategorileri', 'FinansDegisiklikGecmisleri',
        'FinansBelgeleri', 'AmbalajKaynakSenkronizasyonKuyrugu', 'AmbalajIcSandikSablonlari',
        'FinansIsSablonSurumleri', 'FinansIsSablonlari', 'FinansV2GecisYedegi',
        'AmbalajTalepEdenler']
    LOOP
        IF to_regclass(format('public.%I', tablo)) IS NOT NULL THEN
            bulunan := bulunan + 1;
            EXECUTE format('SELECT count(*) FROM public.%I', tablo) INTO adet;
            RAISE NOTICE '% : % satır silinecek', tablo, adet;
        END IF;
    END LOOP;
    RAISE NOTICE 'Bulunan Finans/Ambalaj tablosu: %', bulunan;
END $$;

-- Tablolar tek ifadeyle silinir: aralarındaki yabancı anahtarlar birlikte kalkar. CASCADE kullanılmaz;
-- beklenmeyen dış bağımlılık (görünüm/yabancı anahtar) varsa betik durur ve hiçbir şey silinmez.
DROP TABLE IF EXISTS
    "FinansFaturaKalemleri",
    "FinansSiparisKalemleri",
    "FinansIsKayitlari",
    "AmbalajUretimHareketleri",
    "AmbalajUretimGerceklesmeleri",
    "AmbalajUretimFormuKayitlari",
    "FinansGiderleri",
    "FinansDuzenliIsleri",
    "AmbalajUretimKayitlari",
    "AmbalajUretimFormuSurumleri",
    "FinansUrunEslesmeleri",
    "FinansFiyatTarifeleri",
    "FinansFaturalari",
    "FinansGiderKalemleri",
    "FinansUrunleri",
    "FinansSiparisleri",
    "FinansParametreleri",
    "FinansParaBirimleri",
    "FinansKaynakBastirmalari",
    "FinansGiderKategorileri",
    "FinansDegisiklikGecmisleri",
    "FinansBelgeleri",
    "AmbalajKaynakSenkronizasyonKuyrugu",
    "AmbalajIcSandikSablonlari",
    "FinansIsSablonSurumleri",
    "FinansIsSablonlari",
    "FinansV2GecisYedegi",
    "AmbalajTalepEdenler";

-- Tablolar (EF modeli)
CREATE TABLE "AmbalajIcSandikSablonlari" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY,
    "Ad" character varying(150) NOT NULL,
    "SandikCinsi" integer NOT NULL,
    "DigerSandikCinsi" character varying(100),
    "Boy" numeric(18,4) NOT NULL,
    "En" numeric(18,4) NOT NULL,
    "Yukseklik" numeric(18,4) NOT NULL,
    "CreatedDate" timestamp without time zone NOT NULL,
    "UpdatedDate" timestamp without time zone,
    "CreatedBy" text,
    "UpdatedBy" text,
    CONSTRAINT "PK_AmbalajIcSandikSablonlari" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_AmbalajIcSandikSablonlari_Olculer" CHECK ("Boy" > 0 AND "En" > 0 AND "Yukseklik" > 0)
);

CREATE TABLE "AmbalajKaynakSenkronizasyonKuyrugu" (
    "ProjeId" integer NOT NULL,
    "Surum" bigint NOT NULL DEFAULT 1,
    "Durum" smallint NOT NULL DEFAULT 0,
    "TalepTarihiUtc" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    "UygunTarihUtc" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    "DenemeSayisi" integer NOT NULL DEFAULT 0,
    "KilitKimligi" uuid,
    "KilitBitisTarihiUtc" timestamp with time zone,
    "SonDenemeTarihiUtc" timestamp with time zone,
    "SonBasariliTarihUtc" timestamp with time zone,
    "SonHata" character varying(2000),
    "HataKuyrugunaAlindiTarihiUtc" timestamp with time zone,
    CONSTRAINT "PK_AmbalajKaynakSenkronizasyonKuyrugu" PRIMARY KEY ("ProjeId"),
    CONSTRAINT "CK_AmbalajKaynakSenkronizasyonKuyrugu_DenemeSayisi" CHECK ("DenemeSayisi" >= 0),
    CONSTRAINT "CK_AmbalajKaynakSenkronizasyonKuyrugu_Durum" CHECK ("Durum" >= 0 AND "Durum" <= 3),
    CONSTRAINT "CK_AmbalajKaynakSenkronizasyonKuyrugu_Kilit" CHECK (("Durum" = 1 AND "KilitKimligi" IS NOT NULL AND "KilitBitisTarihiUtc" IS NOT NULL) OR ("Durum" <> 1 AND "KilitKimligi" IS NULL AND "KilitBitisTarihiUtc" IS NULL)),
    CONSTRAINT "CK_AmbalajKaynakSenkronizasyonKuyrugu_Surum" CHECK ("Surum" > 0)
);

CREATE TABLE "FinansBelgeleri" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY,
    "HedefTuru" character varying(30) NOT NULL,
    "HedefId" integer NOT NULL,
    "Surum" integer NOT NULL,
    "OrijinalAd" character varying(250) NOT NULL,
    "GuvenliAd" character varying(80) NOT NULL,
    "Boyut" bigint NOT NULL,
    "IcerikTuru" character varying(80) NOT NULL,
    "Hash" character varying(64) NOT NULL,
    "Yukleyen" character varying(100) NOT NULL,
    "Icerik" bytea NOT NULL,
    "CreatedDate" timestamp without time zone NOT NULL,
    "UpdatedDate" timestamp without time zone,
    "CreatedBy" text,
    "UpdatedBy" text,
    CONSTRAINT "PK_FinansBelgeleri" PRIMARY KEY ("Id")
);

CREATE TABLE "FinansDegisiklikGecmisleri" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY,
    "VarlikTuru" character varying(100) NOT NULL,
    "VarlikId" integer NOT NULL,
    "Islem" character varying(100) NOT NULL,
    "AlanAdi" character varying(150) NOT NULL,
    "EskiDeger" text,
    "YeniDeger" text,
    "Aciklama" character varying(1000),
    "IslemYapan" character varying(100) NOT NULL DEFAULT 'SYSTEM',
    "IslemGrubu" uuid NOT NULL,
    "Referans" character varying(500),
    "CreatedDate" timestamp without time zone NOT NULL,
    "UpdatedDate" timestamp without time zone,
    "CreatedBy" text,
    "UpdatedBy" text,
    CONSTRAINT "PK_FinansDegisiklikGecmisleri" PRIMARY KEY ("Id")
);

CREATE TABLE "FinansGiderKategorileri" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY,
    "Ad" character varying(150) NOT NULL,
    "Aktif" boolean NOT NULL DEFAULT TRUE,
    "CreatedDate" timestamp without time zone NOT NULL,
    "UpdatedDate" timestamp without time zone,
    "CreatedBy" text,
    "UpdatedBy" text,
    CONSTRAINT "PK_FinansGiderKategorileri" PRIMARY KEY ("Id")
);

CREATE TABLE "FinansKaynakBastirmalari" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY,
    "KaynakTuru" character varying(50) NOT NULL,
    "KaynakKayitId" character varying(150) NOT NULL,
    "KaynakBileseni" character varying(20) NOT NULL,
    "Aciklama" character varying(1000) NOT NULL,
    "CreatedDate" timestamp without time zone NOT NULL,
    "UpdatedDate" timestamp without time zone,
    "CreatedBy" text,
    "UpdatedBy" text,
    CONSTRAINT "PK_FinansKaynakBastirmalari" PRIMARY KEY ("Id")
);

CREATE TABLE "FinansParaBirimleri" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY,
    "Kod" character varying(3) NOT NULL,
    "Ad" character varying(100) NOT NULL,
    "Sembol" character varying(10),
    "Aktif" boolean NOT NULL DEFAULT TRUE,
    "Sira" integer NOT NULL,
    "CreatedDate" timestamp without time zone NOT NULL,
    "UpdatedDate" timestamp without time zone,
    "CreatedBy" text,
    "UpdatedBy" text,
    CONSTRAINT "PK_FinansParaBirimleri" PRIMARY KEY ("Id")
);

CREATE TABLE "FinansParametreleri" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY,
    "Kod" character varying(100) NOT NULL,
    "Deger" character varying(200) NOT NULL,
    "CreatedDate" timestamp without time zone NOT NULL,
    "UpdatedDate" timestamp without time zone,
    "CreatedBy" text,
    "UpdatedBy" text,
    CONSTRAINT "PK_FinansParametreleri" PRIMARY KEY ("Id")
);

CREATE TABLE "FinansSiparisleri" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY,
    "KayitNo" character varying(50) NOT NULL,
    "PoNumarasi" character varying(100) NOT NULL,
    "ParaBirimi" character varying(3),
    "SiparisTarihi" timestamp without time zone NOT NULL,
    "Aciklama" text,
    "Durum" integer NOT NULL,
    "IptalEdildi" boolean NOT NULL DEFAULT FALSE,
    "IptalTarihi" timestamp without time zone,
    "IptalAciklamasi" text,
    "CreatedDate" timestamp without time zone NOT NULL,
    "UpdatedDate" timestamp without time zone,
    "CreatedBy" text,
    "UpdatedBy" text,
    CONSTRAINT "PK_FinansSiparisleri" PRIMARY KEY ("Id")
);

CREATE TABLE "FinansUrunleri" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY,
    "Kod" character varying(50) NOT NULL,
    "Ad" character varying(250) NOT NULL,
    "FiyatlandirmaBirimi" integer NOT NULL,
    "Aktif" boolean NOT NULL DEFAULT TRUE,
    "Sira" integer NOT NULL,
    "CreatedDate" timestamp without time zone NOT NULL,
    "UpdatedDate" timestamp without time zone,
    "CreatedBy" text,
    "UpdatedBy" text,
    CONSTRAINT "PK_FinansUrunleri" PRIMARY KEY ("Id")
);

CREATE TABLE "FinansGiderKalemleri" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY,
    "FinansGiderKategoriId" integer NOT NULL,
    "Kod" character varying(50) NOT NULL,
    "Ad" character varying(150) NOT NULL,
    "VarsayilanFirmaVeyaKisi" character varying(250),
    "VarsayilanMiktar" numeric(18,4),
    "VarsayilanBirim" character varying(30),
    "VarsayilanBirimFiyat" numeric(18,6),
    "VarsayilanParaBirimi" character varying(3),
    "VarsayilanKdvDahil" boolean NOT NULL DEFAULT FALSE,
    "VarsayilanKdvOrani" numeric(7,4),
    "Aktif" boolean NOT NULL DEFAULT TRUE,
    "CreatedDate" timestamp without time zone NOT NULL,
    "UpdatedDate" timestamp without time zone,
    "CreatedBy" text,
    "UpdatedBy" text,
    CONSTRAINT "PK_FinansGiderKalemleri" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_FinansGiderKalemleri_Varsayilanlar" CHECK (("VarsayilanMiktar" IS NULL OR "VarsayilanMiktar" > 0) AND ("VarsayilanBirimFiyat" IS NULL OR "VarsayilanBirimFiyat" >= 0) AND ("VarsayilanKdvOrani" IS NULL OR ("VarsayilanKdvOrani" >= 0 AND "VarsayilanKdvOrani" <= 100))),
    CONSTRAINT "FK_FinansGiderKalemleri_FinansGiderKategorileri_FinansGiderKat~" FOREIGN KEY ("FinansGiderKategoriId") REFERENCES "FinansGiderKategorileri" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "FinansFaturalari" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY,
    "FinansSiparisId" integer NOT NULL,
    "KayitNo" character varying(50) NOT NULL,
    "FaturaNumarasi" character varying(100) NOT NULL,
    "FaturaTarihi" timestamp without time zone NOT NULL,
    "Aciklama" text,
    "BelgeParaBirimiSnapshot" character varying(3),
    "BelgeNetTutarSnapshot" numeric(18,4),
    "BelgeKdvTutariSnapshot" numeric(18,4),
    "BelgeToplamTutarSnapshot" numeric(18,4),
    "MutabakatFarkiSnapshot" numeric(18,4) NOT NULL DEFAULT 0.0,
    "MutabakatAciklamasi" character varying(1000),
    "VadeTarihi" timestamp without time zone NOT NULL,
    "OdendiMi" boolean NOT NULL DEFAULT FALSE,
    "OdemeTarihi" timestamp without time zone,
    "OdemeAciklamasi" character varying(1000),
    "Durum" integer NOT NULL,
    "IptalEdildi" boolean NOT NULL DEFAULT FALSE,
    "IptalTarihi" timestamp without time zone,
    "IptalAciklamasi" text,
    "CreatedDate" timestamp without time zone NOT NULL,
    "UpdatedDate" timestamp without time zone,
    "CreatedBy" text,
    "UpdatedBy" text,
    CONSTRAINT "PK_FinansFaturalari" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_FinansFaturalari_BelgeTutarlari" CHECK (("BelgeParaBirimiSnapshot" IS NULL AND "BelgeNetTutarSnapshot" IS NULL AND "BelgeKdvTutariSnapshot" IS NULL AND "BelgeToplamTutarSnapshot" IS NULL) OR ("BelgeParaBirimiSnapshot" IS NOT NULL AND "BelgeNetTutarSnapshot" IS NOT NULL AND "BelgeKdvTutariSnapshot" IS NOT NULL AND "BelgeToplamTutarSnapshot" IS NOT NULL AND "BelgeNetTutarSnapshot" >= 0 AND "BelgeKdvTutariSnapshot" >= 0 AND "BelgeToplamTutarSnapshot" >= 0 AND ABS(("BelgeNetTutarSnapshot" + "BelgeKdvTutariSnapshot") - "BelgeToplamTutarSnapshot") <= 0.02)),
    CONSTRAINT "FK_FinansFaturalari_FinansSiparisleri_FinansSiparisId" FOREIGN KEY ("FinansSiparisId") REFERENCES "FinansSiparisleri" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "FinansFiyatTarifeleri" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY,
    "FinansUrunId" integer NOT NULL,
    "Yil" integer NOT NULL,
    "GecerlilikBaslangici" timestamp without time zone NOT NULL,
    "GecerlilikBitisi" timestamp without time zone NOT NULL,
    "BirimFiyat" numeric(18,6) NOT NULL,
    "ParaBirimi" character varying(3) NOT NULL,
    "KdvOrani" numeric(7,4) NOT NULL,
    "Aktif" boolean NOT NULL DEFAULT TRUE,
    "CreatedDate" timestamp without time zone NOT NULL,
    "UpdatedDate" timestamp without time zone,
    "CreatedBy" text,
    "UpdatedBy" text,
    CONSTRAINT "PK_FinansFiyatTarifeleri" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_FinansFiyatTarifeleri_Fiyat" CHECK ("BirimFiyat" >= 0 AND "KdvOrani" >= 0 AND "KdvOrani" <= 100),
    CONSTRAINT "CK_FinansFiyatTarifeleri_Tarih" CHECK ("GecerlilikBitisi" >= "GecerlilikBaslangici"),
    CONSTRAINT "CK_FinansFiyatTarifeleri_Yil" CHECK ("Yil" BETWEEN 2000 AND 2200),
    CONSTRAINT "FK_FinansFiyatTarifeleri_FinansUrunleri_FinansUrunId" FOREIGN KEY ("FinansUrunId") REFERENCES "FinansUrunleri" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "FinansUrunEslesmeleri" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY,
    "FinansUrunId" integer NOT NULL,
    "IsTuru" integer NOT NULL,
    "SandikAdi" character varying(250),
    "SandikTipi" character varying(100),
    "Boy" numeric(18,4),
    "En" numeric(18,4),
    "Yukseklik" numeric(18,4),
    "IcSandikSablonId" integer,
    "Malzeme" integer,
    "Aktif" boolean NOT NULL DEFAULT TRUE,
    "CreatedDate" timestamp without time zone NOT NULL,
    "UpdatedDate" timestamp without time zone,
    "CreatedBy" text,
    "UpdatedBy" text,
    CONSTRAINT "PK_FinansUrunEslesmeleri" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_FinansUrunEslesmeleri_FinansUrunleri_FinansUrunId" FOREIGN KEY ("FinansUrunId") REFERENCES "FinansUrunleri" ("Id") ON DELETE CASCADE
);

CREATE TABLE "AmbalajUretimFormuSurumleri" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY,
    "FormKimligi" uuid NOT NULL,
    "Surum" integer NOT NULL,
    "IdempotencyAnahtari" uuid NOT NULL,
    "IstekHash" character varying(64) NOT NULL,
    "KapsamAnahtari" character varying(200) NOT NULL,
    "ProjeId" integer,
    "SnapshotJson" jsonb NOT NULL,
    "OlusturanKullaniciId" integer NOT NULL,
    "Aciklama" character varying(1000),
    "CreatedDate" timestamp without time zone NOT NULL,
    "UpdatedDate" timestamp without time zone,
    "CreatedBy" text,
    "UpdatedBy" text,
    CONSTRAINT "PK_AmbalajUretimFormuSurumleri" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AmbalajUretimFormuSurumleri_Projeler_ProjeId" FOREIGN KEY ("ProjeId") REFERENCES "Projeler" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "AmbalajUretimKayitlari" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY,
    "IsAkisKimligi" uuid NOT NULL,
    "ProjeId" integer,
    "ManuelProjeNo" character varying(100),
    "ManuelProjeAdi" character varying(250),
    "UstKayitId" integer,
    "IcSandikSablonId" integer,
    "BagimsizKayitMi" boolean NOT NULL DEFAULT FALSE,
    "Tur" integer NOT NULL,
    "KaynakModul" integer NOT NULL,
    "KaynakKayitId" integer,
    "KaynakSenkronizasyonuKilitliMi" boolean NOT NULL DEFAULT FALSE,
    "KaynakSonSenkronizasyonTarihi" timestamp without time zone,
    "SandikNo" character varying(100) NOT NULL,
    "Ad" character varying(250),
    "SandikCinsi" integer NOT NULL,
    "DigerSandikCinsi" character varying(100),
    "Adet" integer NOT NULL,
    "Boy" numeric(18,4) NOT NULL,
    "En" numeric(18,4) NOT NULL,
    "Yukseklik" numeric(18,4) NOT NULL,
    "AmbalajaDahil" boolean NOT NULL DEFAULT TRUE,
    "UretimeAlindi" boolean NOT NULL DEFAULT FALSE,
    "HesaplananBirimM3" numeric(18,6) NOT NULL,
    "HesaplananToplamM3" numeric(18,6) NOT NULL,
    "M3Override" numeric(18,6),
    "M3OverrideNedeni" character varying(500),
    "M3HesaplamaVersiyonu" character varying(50) NOT NULL,
    "SarfOrani" numeric(7,6) NOT NULL DEFAULT 0.11,
    "SarfM3" numeric(18,6) NOT NULL,
    "ToplamM3" numeric(18,6) NOT NULL,
    "KullanimAmaci" character varying(250),
    "TalepEdenKisi" character varying(200),
    "TalepEdenBolum" character varying(200),
    "TalimatVeren" character varying(200),
    "FirinPartiNo" character varying(100),
    "Aciklama" text,
    "UretimDurumu" integer NOT NULL,
    "UretimTarihi" timestamp without time zone,
    "TamamlanmaTarihi" timestamp without time zone,
    "FinansTarihi" timestamp without time zone,
    "IptalMi" boolean NOT NULL DEFAULT FALSE,
    "IptalOncesiUretimDurumu" integer,
    "IptalTarihi" timestamp without time zone,
    "IptalEdenKullaniciId" integer,
    "IptalNedeni" character varying(500),
    "CreatedDate" timestamp without time zone NOT NULL,
    "UpdatedDate" timestamp without time zone,
    "CreatedBy" text,
    "UpdatedBy" text,
    CONSTRAINT "PK_AmbalajUretimKayitlari" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_AmbalajUretimKayitlari_Adet" CHECK ("Adet" > 0),
    CONSTRAINT "CK_AmbalajUretimKayitlari_M3" CHECK ("HesaplananBirimM3" >= 0 AND "HesaplananToplamM3" >= 0 AND ("M3Override" IS NULL OR "M3Override" >= 0) AND "SarfM3" >= 0 AND "ToplamM3" >= 0),
    CONSTRAINT "CK_AmbalajUretimKayitlari_Olculer" CHECK ("Boy" >= 0 AND "En" >= 0 AND "Yukseklik" >= 0),
    CONSTRAINT "CK_AmbalajUretimKayitlari_Proje" CHECK ("ProjeId" IS NOT NULL OR NULLIF(BTRIM("ManuelProjeNo"), '') IS NOT NULL),
    CONSTRAINT "CK_AmbalajUretimKayitlari_SarfOrani" CHECK ("SarfOrani" >= 0 AND "SarfOrani" <= 1),
    CONSTRAINT "CK_AmbalajUretimKayitlari_UretimSecimi" CHECK (NOT "UretimeAlindi" OR ("AmbalajaDahil" AND ("M3Override" IS NOT NULL OR ("Adet" > 0 AND CASE WHEN "SandikCinsi" IN (1, 2) AND NOT "BagimsizKayitMi" AND "KaynakKayitId" IS NOT NULL AND "KaynakModul" IN (1, 2, 3) THEN "Boy" > 92 AND "En" > 92 AND "Yukseklik" > 255 ELSE "Boy" > 0 AND "En" > 0 AND "Yukseklik" > 0 END)))),
    CONSTRAINT "FK_AmbalajUretimKayitlari_AmbalajIcSandikSablonlari_IcSandikSab" FOREIGN KEY ("IcSandikSablonId") REFERENCES "AmbalajIcSandikSablonlari" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_AmbalajUretimKayitlari_AmbalajUretimKayitlari_UstKayitId" FOREIGN KEY ("UstKayitId") REFERENCES "AmbalajUretimKayitlari" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_AmbalajUretimKayitlari_Projeler_ProjeId" FOREIGN KEY ("ProjeId") REFERENCES "Projeler" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "FinansDuzenliIsleri" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY,
    "ProjeId" integer,
    "ManuelProjeNo" character varying(100),
    "ManuelProjeAdi" character varying(250),
    "IsAdi" character varying(250) NOT NULL,
    "IsTuru" integer NOT NULL,
    "OzelIsTuru" character varying(150),
    "HesaplamaYontemi" integer NOT NULL,
    "RaporGrubu" character varying(150) NOT NULL,
    "Musteri" character varying(250) NOT NULL,
    "Aciklama" text,
    "TekrarSikligi" integer NOT NULL,
    "BaslangicTarihi" timestamp without time zone NOT NULL,
    "BitisTarihi" timestamp without time zone,
    "OlusturmaGunu" integer NOT NULL,
    "Miktar" numeric(18,4) NOT NULL,
    "Birim" character varying(30) NOT NULL,
    "FinansUrunId" integer,
    "BirimFiyat" numeric(18,6) NOT NULL,
    "ParaBirimi" character varying(3) NOT NULL,
    "KdvOrani" numeric(7,4) NOT NULL,
    "Aktif" boolean NOT NULL DEFAULT TRUE,
    "CreatedDate" timestamp without time zone NOT NULL,
    "UpdatedDate" timestamp without time zone,
    "CreatedBy" text,
    "UpdatedBy" text,
    CONSTRAINT "PK_FinansDuzenliIsleri" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_FinansDuzenliIsleri_Fiyat" CHECK ("Miktar" > 0 AND "BirimFiyat" >= 0 AND "KdvOrani" >= 0 AND "KdvOrani" <= 100),
    CONSTRAINT "CK_FinansDuzenliIsleri_Gun" CHECK ("OlusturmaGunu" BETWEEN 1 AND 31),
    CONSTRAINT "CK_FinansDuzenliIsleri_Tarih" CHECK ("BitisTarihi" IS NULL OR "BitisTarihi" >= "BaslangicTarihi"),
    CONSTRAINT "FK_FinansDuzenliIsleri_FinansUrunleri_FinansUrunId" FOREIGN KEY ("FinansUrunId") REFERENCES "FinansUrunleri" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_FinansDuzenliIsleri_Projeler_ProjeId" FOREIGN KEY ("ProjeId") REFERENCES "Projeler" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "FinansGiderleri" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY,
    "Tarih" timestamp without time zone NOT NULL,
    "FinansTarihi" timestamp without time zone NOT NULL,
    "FinansDonemi" timestamp without time zone NOT NULL,
    "BelgeNo" character varying(100),
    "AvansMi" boolean NOT NULL,
    "MahsupEdilenAvansId" integer,
    "FinansGiderKategoriId" integer NOT NULL,
    "FinansGiderKalemiId" integer,
    "AltKategori" character varying(200),
    "FirmaVeyaKisi" character varying(250),
    "Aciklama" text NOT NULL,
    "Miktar" numeric(18,4) NOT NULL,
    "Birim" character varying(30) NOT NULL,
    "BirimFiyat" numeric(18,6) NOT NULL,
    "Tutar" numeric(18,4) NOT NULL,
    "ParaBirimi" character varying(3) NOT NULL,
    "KdvDahil" boolean NOT NULL,
    "KdvOrani" numeric(7,4) NOT NULL,
    "Matrah" numeric(18,4) NOT NULL,
    "KdvTutari" numeric(18,4) NOT NULL,
    "ToplamTutar" numeric(18,4) NOT NULL,
    "ProjeId" integer,
    "ManuelProjeNo" character varying(100),
    "IsTuru" integer,
    "FinansSiparisId" integer,
    "IptalEdildi" boolean NOT NULL DEFAULT FALSE,
    "IptalTarihi" timestamp without time zone,
    "IptalAciklamasi" text,
    "CreatedDate" timestamp without time zone NOT NULL,
    "UpdatedDate" timestamp without time zone,
    "CreatedBy" text,
    "UpdatedBy" text,
    CONSTRAINT "PK_FinansGiderleri" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_FinansGiderleri_Kdv" CHECK ("KdvOrani" >= 0 AND "KdvOrani" <= 100 AND "Matrah" >= 0 AND "KdvTutari" >= 0 AND "ToplamTutar" >= 0),
    CONSTRAINT "CK_FinansGiderleri_Miktar" CHECK ("Miktar" > 0 AND "BirimFiyat" >= 0 AND "Tutar" >= 0),
    CONSTRAINT "FK_FinansGiderleri_FinansGiderKalemleri_FinansGiderKalemiId" FOREIGN KEY ("FinansGiderKalemiId") REFERENCES "FinansGiderKalemleri" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_FinansGiderleri_FinansGiderKategorileri_FinansGiderKategori~" FOREIGN KEY ("FinansGiderKategoriId") REFERENCES "FinansGiderKategorileri" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_FinansGiderleri_FinansGiderleri_MahsupEdilenAvansId" FOREIGN KEY ("MahsupEdilenAvansId") REFERENCES "FinansGiderleri" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_FinansGiderleri_FinansSiparisleri_FinansSiparisId" FOREIGN KEY ("FinansSiparisId") REFERENCES "FinansSiparisleri" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_FinansGiderleri_Projeler_ProjeId" FOREIGN KEY ("ProjeId") REFERENCES "Projeler" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "AmbalajUretimFormuKayitlari" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY,
    "FormSurumuId" integer NOT NULL,
    "AmbalajUretimKaydiId" integer NOT NULL,
    "CreatedDate" timestamp without time zone NOT NULL,
    "UpdatedDate" timestamp without time zone,
    "CreatedBy" text,
    "UpdatedBy" text,
    CONSTRAINT "PK_AmbalajUretimFormuKayitlari" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AmbalajUretimFormuKayitlari_AmbalajUretimFormuSurumleri_For~" FOREIGN KEY ("FormSurumuId") REFERENCES "AmbalajUretimFormuSurumleri" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_AmbalajUretimFormuKayitlari_AmbalajUretimKayitlari_AmbalajU~" FOREIGN KEY ("AmbalajUretimKaydiId") REFERENCES "AmbalajUretimKayitlari" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "AmbalajUretimGerceklesmeleri" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY,
    "GerceklesmeKimligi" uuid NOT NULL,
    "Surum" integer NOT NULL,
    "OncekiSurumId" integer,
    "AmbalajUretimKaydiId" integer NOT NULL,
    "IsAkisKimligi" uuid NOT NULL,
    "GerceklesmeTarihi" timestamp without time zone NOT NULL,
    "ProjeId" integer,
    "ProjeNo" character varying(100) NOT NULL,
    "ProjeAdi" character varying(250),
    "SandikNo" character varying(100) NOT NULL,
    "SandikAdi" character varying(250),
    "SandikCinsi" integer NOT NULL,
    "Tur" integer NOT NULL,
    "Adet" integer NOT NULL,
    "Boy" numeric(18,4) NOT NULL,
    "En" numeric(18,4) NOT NULL,
    "Yukseklik" numeric(18,4) NOT NULL,
    "NetM3" numeric(18,6),
    "SarfM3" numeric(18,6),
    "FormulVersiyonu" character varying(50) NOT NULL,
    "KullaniciId" integer NOT NULL,
    "Gerekce" character varying(1000),
    "CreatedDate" timestamp without time zone NOT NULL,
    "UpdatedDate" timestamp without time zone,
    "CreatedBy" text,
    "UpdatedBy" text,
    CONSTRAINT "PK_AmbalajUretimGerceklesmeleri" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AmbalajUretimGerceklesmeleri_AmbalajUretimGerceklesmeleri_O~" FOREIGN KEY ("OncekiSurumId") REFERENCES "AmbalajUretimGerceklesmeleri" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_AmbalajUretimGerceklesmeleri_AmbalajUretimKayitlari_Ambalaj~" FOREIGN KEY ("AmbalajUretimKaydiId") REFERENCES "AmbalajUretimKayitlari" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "AmbalajUretimHareketleri" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY,
    "AmbalajUretimKaydiId" integer NOT NULL,
    "IslemGrubu" uuid NOT NULL,
    "KullaniciId" integer NOT NULL,
    "Tarih" timestamp without time zone NOT NULL,
    "Islem" character varying(100) NOT NULL,
    "AlanAdi" character varying(150) NOT NULL,
    "EskiDeger" text,
    "YeniDeger" text,
    "Aciklama" character varying(1000),
    "CreatedDate" timestamp without time zone NOT NULL,
    "UpdatedDate" timestamp without time zone,
    "CreatedBy" text,
    "UpdatedBy" text,
    CONSTRAINT "PK_AmbalajUretimHareketleri" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AmbalajUretimHareketleri_AmbalajUretimKayitlari_AmbalajUret~" FOREIGN KEY ("AmbalajUretimKaydiId") REFERENCES "AmbalajUretimKayitlari" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_AmbalajUretimHareketleri_Kullanicilar_KullaniciId" FOREIGN KEY ("KullaniciId") REFERENCES "Kullanicilar" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "FinansIsKayitlari" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY,
    "ProjeId" integer,
    "ProjeNo" character varying(100) NOT NULL,
    "Musteri" character varying(250) NOT NULL,
    "ManuelProjeMi" boolean NOT NULL,
    "IsTuru" integer NOT NULL,
    "IsAdi" character varying(250) NOT NULL,
    "OzelIsTuru" character varying(150),
    "HesaplamaYontemi" integer,
    "RaporGrubu" character varying(150),
    "Aciklama" character varying(2000),
    "TalepEdenKisi" character varying(200),
    "TalepEdenBolum" character varying(200),
    "SandikNo" character varying(100),
    "SandikAdi" character varying(250),
    "SandikTipi" character varying(100),
    "Boy" numeric(18,4),
    "En" numeric(18,4),
    "Yukseklik" numeric(18,4),
    "IcSandikSablonId" integer,
    "Adet" numeric(18,4) NOT NULL,
    "Birim" character varying(30) NOT NULL,
    "BirimM3" numeric(18,6) NOT NULL,
    "ToplamM3" numeric(18,6) NOT NULL,
    "AhsapM3" numeric(18,6),
    "KontraM3" numeric(18,6),
    "HacimManuel" boolean NOT NULL DEFAULT FALSE,
    "FinansUrunId" integer,
    "FiyatlandirmaBirimiSnapshot" integer NOT NULL,
    "BirimFiyatSnapshot" numeric(18,6) NOT NULL,
    "AhsapFinansUrunId" integer,
    "AhsapBirimFiyatSnapshot" numeric(18,6),
    "AhsapTarifeIdSnapshot" integer,
    "ParaBirimiSnapshot" character varying(3) NOT NULL,
    "KdvOraniSnapshot" numeric(7,4) NOT NULL,
    "TarifeYiliSnapshot" integer,
    "UretimTarihi" timestamp without time zone NOT NULL,
    "FinansDonemi" timestamp without time zone NOT NULL,
    "FinansTarihi" timestamp without time zone NOT NULL,
    "FinansTarihiManuel" boolean NOT NULL,
    "FinansMiktariManuel" boolean NOT NULL,
    "ManuelNetTutar" numeric(18,2),
    "TarifeIdSnapshot" integer,
    "SandikCinsi" integer,
    "FiyatBilesenleriJson" jsonb,
    "KayitTarihi" timestamp without time zone NOT NULL,
    "Durum" integer NOT NULL,
    "KaynakTuru" character varying(50) NOT NULL,
    "KaynakKayitId" character varying(150),
    "KaynakBileseni" character varying(20) NOT NULL DEFAULT 'NET',
    "KaynakAktif" boolean NOT NULL DEFAULT TRUE,
    "DuzenliIsId" integer,
    "IptalEdildi" boolean NOT NULL DEFAULT FALSE,
    "IptalTarihi" timestamp without time zone,
    "IptalAciklamasi" character varying(1000),
    "CreatedDate" timestamp without time zone NOT NULL,
    "UpdatedDate" timestamp without time zone,
    "CreatedBy" text,
    "UpdatedBy" text,
    CONSTRAINT "PK_FinansIsKayitlari" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_FinansIsKayitlari_Fiyat" CHECK ("BirimFiyatSnapshot" >= 0 AND "KdvOraniSnapshot" >= 0 AND "KdvOraniSnapshot" <= 100),
    CONSTRAINT "CK_FinansIsKayitlari_AhsapFiyat" CHECK ("AhsapBirimFiyatSnapshot" IS NULL OR "AhsapBirimFiyatSnapshot" >= 0),
    CONSTRAINT "CK_FinansIsKayitlari_Miktarlar" CHECK ("Adet" > 0 AND "BirimM3" >= 0 AND "ToplamM3" >= 0 AND "AhsapM3" >= 0 AND "KontraM3" >= 0),
    CONSTRAINT "CK_FinansIsKayitlari_Proje" CHECK ("ProjeId" IS NOT NULL OR NULLIF(BTRIM("ProjeNo"), '') IS NOT NULL),
    CONSTRAINT "FK_FinansIsKayitlari_FinansDuzenliIsleri_DuzenliIsId" FOREIGN KEY ("DuzenliIsId") REFERENCES "FinansDuzenliIsleri" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_FinansIsKayitlari_FinansUrunleri_AhsapFinansUrunId" FOREIGN KEY ("AhsapFinansUrunId") REFERENCES "FinansUrunleri" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_FinansIsKayitlari_FinansUrunleri_FinansUrunId" FOREIGN KEY ("FinansUrunId") REFERENCES "FinansUrunleri" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_FinansIsKayitlari_Projeler_ProjeId" FOREIGN KEY ("ProjeId") REFERENCES "Projeler" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "FinansSiparisKalemleri" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY,
    "FinansSiparisId" integer NOT NULL,
    "FinansIsKaydiId" integer NOT NULL,
    "Adet" numeric(18,4) NOT NULL,
    "M3" numeric(18,6) NOT NULL,
    "TutarBazli" boolean NOT NULL,
    "FinansUrunId" integer,
    "FiyatlandirmaBirimiSnapshot" integer NOT NULL,
    "BirimFiyatSnapshot" numeric(18,6) NOT NULL,
    "ParaBirimiSnapshot" character varying(3) NOT NULL,
    "KdvOraniSnapshot" numeric(7,4) NOT NULL,
    "NetTutarSnapshot" numeric(18,4) NOT NULL,
    "KdvTutariSnapshot" numeric(18,4) NOT NULL,
    "ToplamTutarSnapshot" numeric(18,4) NOT NULL,
    "CreatedDate" timestamp without time zone NOT NULL,
    "UpdatedDate" timestamp without time zone,
    "CreatedBy" text,
    "UpdatedBy" text,
    CONSTRAINT "PK_FinansSiparisKalemleri" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_FinansSiparisKalemleri_Miktar" CHECK ("Adet" >= 0 AND "M3" >= 0 AND ("Adet" > 0 OR "M3" > 0 OR "TutarBazli")),
    CONSTRAINT "CK_FinansSiparisKalemleri_Tutar" CHECK ("BirimFiyatSnapshot" >= 0 AND "NetTutarSnapshot" >= 0 AND "KdvTutariSnapshot" >= 0 AND "ToplamTutarSnapshot" >= 0),
    CONSTRAINT "FK_FinansSiparisKalemleri_FinansIsKayitlari_FinansIsKaydiId" FOREIGN KEY ("FinansIsKaydiId") REFERENCES "FinansIsKayitlari" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_FinansSiparisKalemleri_FinansSiparisleri_FinansSiparisId" FOREIGN KEY ("FinansSiparisId") REFERENCES "FinansSiparisleri" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_FinansSiparisKalemleri_FinansUrunleri_FinansUrunId" FOREIGN KEY ("FinansUrunId") REFERENCES "FinansUrunleri" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "FinansFaturaKalemleri" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY,
    "FinansFaturaId" integer NOT NULL,
    "FinansSiparisKalemiId" integer NOT NULL,
    "Adet" numeric(18,4) NOT NULL,
    "M3" numeric(18,6) NOT NULL,
    "TutarBazli" boolean NOT NULL,
    "NetTutarSnapshot" numeric(18,4) NOT NULL,
    "KdvTutariSnapshot" numeric(18,4) NOT NULL,
    "ToplamTutarSnapshot" numeric(18,4) NOT NULL,
    "CreatedDate" timestamp without time zone NOT NULL,
    "UpdatedDate" timestamp without time zone,
    "CreatedBy" text,
    "UpdatedBy" text,
    CONSTRAINT "PK_FinansFaturaKalemleri" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_FinansFaturaKalemleri_Miktar" CHECK ("Adet" >= 0 AND "M3" >= 0 AND ("Adet" > 0 OR "M3" > 0 OR "TutarBazli")),
    CONSTRAINT "CK_FinansFaturaKalemleri_Tutar" CHECK ("NetTutarSnapshot" >= 0 AND "KdvTutariSnapshot" >= 0 AND "ToplamTutarSnapshot" >= 0),
    CONSTRAINT "FK_FinansFaturaKalemleri_FinansFaturalari_FinansFaturaId" FOREIGN KEY ("FinansFaturaId") REFERENCES "FinansFaturalari" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_FinansFaturaKalemleri_FinansSiparisKalemleri_FinansSiparisK~" FOREIGN KEY ("FinansSiparisKalemiId") REFERENCES "FinansSiparisKalemleri" ("Id") ON DELETE RESTRICT
);

-- Başlangıç verisi
INSERT INTO "FinansParaBirimleri" ("Id", "Ad", "Aktif", "CreatedBy", "CreatedDate", "Kod", "Sembol", "Sira", "UpdatedBy", "UpdatedDate")
VALUES (1, 'Türk Lirası', TRUE, 'SYSTEM', TIMESTAMP '2026-09-29T00:00:00', 'TRY', '₺', 1, NULL, NULL);
INSERT INTO "FinansParaBirimleri" ("Id", "Ad", "Aktif", "CreatedBy", "CreatedDate", "Kod", "Sembol", "Sira", "UpdatedBy", "UpdatedDate")
VALUES (2, 'Euro', TRUE, 'SYSTEM', TIMESTAMP '2026-09-29T00:00:00', 'EUR', '€', 2, NULL, NULL);
INSERT INTO "FinansParametreleri" ("Id", "CreatedBy", "CreatedDate", "Deger", "Kod", "UpdatedBy", "UpdatedDate")
VALUES (1, 'SYSTEM', TIMESTAMP '2026-09-29T00:00:00', '30', 'VarsayilanVadeGunu', NULL, NULL);
INSERT INTO "FinansParametreleri" ("Id", "CreatedBy", "CreatedDate", "Deger", "Kod", "UpdatedBy", "UpdatedDate")
VALUES (2, 'SYSTEM', TIMESTAMP '2026-09-29T00:00:00', '7', 'VadeUyariGunu', NULL, NULL);

-- İndeksler
CREATE UNIQUE INDEX "IX_AmbalajIcSandikSablonlari_Ad" ON "AmbalajIcSandikSablonlari" ("Ad");
CREATE INDEX "IX_AmbalajKaynakSenkronizasyonKuyrugu_Islenebilir" ON "AmbalajKaynakSenkronizasyonKuyrugu" ("Durum", "UygunTarihUtc", "TalepTarihiUtc") WHERE "Durum" IN (0, 1);
CREATE INDEX "IX_AmbalajUretimFormuKayitlari_AmbalajUretimKaydiId" ON "AmbalajUretimFormuKayitlari" ("AmbalajUretimKaydiId");
CREATE UNIQUE INDEX "IX_AmbalajUretimFormuKayitlari_FormSurumuId_AmbalajUretimKaydi~" ON "AmbalajUretimFormuKayitlari" ("FormSurumuId", "AmbalajUretimKaydiId");
CREATE UNIQUE INDEX "IX_AmbalajUretimFormuSurumleri_FormKimligi_Surum" ON "AmbalajUretimFormuSurumleri" ("FormKimligi", "Surum");
CREATE UNIQUE INDEX "IX_AmbalajUretimFormuSurumleri_IdempotencyAnahtari" ON "AmbalajUretimFormuSurumleri" ("IdempotencyAnahtari");
CREATE UNIQUE INDEX "IX_AmbalajUretimFormuSurumleri_KapsamAnahtari_Surum" ON "AmbalajUretimFormuSurumleri" ("KapsamAnahtari", "Surum");
CREATE INDEX "IX_AmbalajUretimFormuSurumleri_ProjeId" ON "AmbalajUretimFormuSurumleri" ("ProjeId");
CREATE UNIQUE INDEX "IX_AmbalajUretimGerceklesmeleri_AmbalajUretimKaydiId_Surum" ON "AmbalajUretimGerceklesmeleri" ("AmbalajUretimKaydiId", "Surum");
CREATE UNIQUE INDEX "IX_AmbalajUretimGerceklesmeleri_GerceklesmeKimligi_Surum" ON "AmbalajUretimGerceklesmeleri" ("GerceklesmeKimligi", "Surum");
CREATE INDEX "IX_AmbalajUretimGerceklesmeleri_GerceklesmeTarihi" ON "AmbalajUretimGerceklesmeleri" ("GerceklesmeTarihi");
CREATE UNIQUE INDEX "IX_AmbalajUretimGerceklesmeleri_OncekiSurumId" ON "AmbalajUretimGerceklesmeleri" ("OncekiSurumId") WHERE "OncekiSurumId" IS NOT NULL;
CREATE INDEX "IX_AmbalajUretimHareketleri_AmbalajUretimKaydiId_Tarih" ON "AmbalajUretimHareketleri" ("AmbalajUretimKaydiId", "Tarih");
CREATE INDEX "IX_AmbalajUretimHareketleri_IslemGrubu" ON "AmbalajUretimHareketleri" ("IslemGrubu");
CREATE INDEX "IX_AmbalajUretimHareketleri_KullaniciId" ON "AmbalajUretimHareketleri" ("KullaniciId");
CREATE INDEX "IX_AmbalajUretimKayitlari_BagimsizKayitMi_Tur_ProjeId" ON "AmbalajUretimKayitlari" ("BagimsizKayitMi", "Tur", "ProjeId");
CREATE INDEX "IX_AmbalajUretimKayitlari_IcSandikSablonId" ON "AmbalajUretimKayitlari" ("IcSandikSablonId");
CREATE INDEX "IX_AmbalajUretimKayitlari_IptalMi_UretimeAlindi_UretimDurumu" ON "AmbalajUretimKayitlari" ("IptalMi", "UretimeAlindi", "UretimDurumu");
CREATE UNIQUE INDEX "IX_AmbalajUretimKayitlari_IsAkisKimligi" ON "AmbalajUretimKayitlari" ("IsAkisKimligi");
CREATE UNIQUE INDEX "IX_AmbalajUretimKayitlari_KaynakKayitId" ON "AmbalajUretimKayitlari" ("KaynakKayitId") WHERE "KaynakKayitId" IS NOT NULL;
CREATE UNIQUE INDEX "IX_AmbalajUretimKayitlari_OzelSandikTekil" ON "AmbalajUretimKayitlari" ("ProjeId", "Tur", "SandikNo") WHERE "BagimsizKayitMi" AND NOT "IptalMi";
CREATE INDEX "IX_AmbalajUretimKayitlari_ProjeId" ON "AmbalajUretimKayitlari" ("ProjeId");
CREATE INDEX "IX_AmbalajUretimKayitlari_UretimTarihi" ON "AmbalajUretimKayitlari" ("UretimTarihi");
CREATE INDEX "IX_AmbalajUretimKayitlari_UstKayitId" ON "AmbalajUretimKayitlari" ("UstKayitId");
CREATE UNIQUE INDEX "IX_FinansBelgeleri_HedefTuru_HedefId_Surum" ON "FinansBelgeleri" ("HedefTuru", "HedefId", "Surum");
CREATE INDEX "IX_FinansDegisiklikGecmisleri_VarlikTuru_VarlikId_CreatedDate" ON "FinansDegisiklikGecmisleri" ("VarlikTuru", "VarlikId", "CreatedDate");
CREATE INDEX "IX_FinansDuzenliIsleri_Aktif_BaslangicTarihi_BitisTarihi" ON "FinansDuzenliIsleri" ("Aktif", "BaslangicTarihi", "BitisTarihi");
CREATE INDEX "IX_FinansDuzenliIsleri_FinansUrunId" ON "FinansDuzenliIsleri" ("FinansUrunId");
CREATE INDEX "IX_FinansDuzenliIsleri_ProjeId" ON "FinansDuzenliIsleri" ("ProjeId");
CREATE UNIQUE INDEX "IX_FinansFaturaKalemleri_FinansFaturaId_FinansSiparisKalemiId" ON "FinansFaturaKalemleri" ("FinansFaturaId", "FinansSiparisKalemiId");
CREATE INDEX "IX_FinansFaturaKalemleri_FinansSiparisKalemiId" ON "FinansFaturaKalemleri" ("FinansSiparisKalemiId");
CREATE INDEX "IX_FinansFaturalari_Durum_FaturaTarihi" ON "FinansFaturalari" ("Durum", "FaturaTarihi");
CREATE UNIQUE INDEX "IX_FinansFaturalari_FaturaNumarasi" ON "FinansFaturalari" ("FaturaNumarasi");
CREATE INDEX "IX_FinansFaturalari_FinansSiparisId" ON "FinansFaturalari" ("FinansSiparisId");
CREATE UNIQUE INDEX "IX_FinansFaturalari_KayitNo" ON "FinansFaturalari" ("KayitNo");
CREATE INDEX "IX_FinansFaturalari_OdendiMi_VadeTarihi" ON "FinansFaturalari" ("OdendiMi", "VadeTarihi");
CREATE UNIQUE INDEX "IX_FinansFiyatTarifeleri_FinansUrunId_Yil_GecerlilikBaslangici~" ON "FinansFiyatTarifeleri" ("FinansUrunId", "Yil", "GecerlilikBaslangici", "GecerlilikBitisi");
CREATE UNIQUE INDEX "IX_FinansGiderKalemleri_FinansGiderKategoriId_Ad" ON "FinansGiderKalemleri" ("FinansGiderKategoriId", "Ad");
CREATE UNIQUE INDEX "IX_FinansGiderKalemleri_Kod" ON "FinansGiderKalemleri" ("Kod");
CREATE UNIQUE INDEX "IX_FinansGiderKategorileri_Ad" ON "FinansGiderKategorileri" ("Ad");
CREATE INDEX "IX_FinansGiderleri_FinansDonemi_IptalEdildi" ON "FinansGiderleri" ("FinansDonemi", "IptalEdildi");
CREATE INDEX "IX_FinansGiderleri_FinansGiderKalemiId" ON "FinansGiderleri" ("FinansGiderKalemiId");
CREATE INDEX "IX_FinansGiderleri_FinansGiderKategoriId_FinansGiderKalemiId" ON "FinansGiderleri" ("FinansGiderKategoriId", "FinansGiderKalemiId");
CREATE INDEX "IX_FinansGiderleri_FinansSiparisId" ON "FinansGiderleri" ("FinansSiparisId");
CREATE INDEX "IX_FinansGiderleri_MahsupEdilenAvansId" ON "FinansGiderleri" ("MahsupEdilenAvansId");
CREATE INDEX "IX_FinansGiderleri_ProjeId" ON "FinansGiderleri" ("ProjeId");
CREATE INDEX "IX_FinansIsKayitlari_AhsapFinansUrunId" ON "FinansIsKayitlari" ("AhsapFinansUrunId");
CREATE INDEX "IX_FinansIsKayitlari_AhsapTarifeIdSnapshot" ON "FinansIsKayitlari" ("AhsapTarifeIdSnapshot");
CREATE INDEX "IX_FinansIsKayitlari_Durum_IptalEdildi_KaynakAktif" ON "FinansIsKayitlari" ("Durum", "IptalEdildi", "KaynakAktif");
CREATE INDEX "IX_FinansIsKayitlari_DuzenliIsId" ON "FinansIsKayitlari" ("DuzenliIsId");
CREATE INDEX "IX_FinansIsKayitlari_FinansUrunId" ON "FinansIsKayitlari" ("FinansUrunId");
CREATE UNIQUE INDEX "IX_FinansIsKayitlari_KaynakBileseniV2" ON "FinansIsKayitlari" ("KaynakTuru", "KaynakKayitId", "KaynakBileseni") WHERE "KaynakKayitId" IS NOT NULL;
CREATE INDEX "IX_FinansIsKayitlari_ProjeId_FinansDonemi" ON "FinansIsKayitlari" ("ProjeId", "FinansDonemi");
CREATE INDEX "IX_FinansIsKayitlari_UretimTarihi" ON "FinansIsKayitlari" ("UretimTarihi");
CREATE UNIQUE INDEX "IX_FinansKaynakBastirmalari_KaynakTuru_KaynakKayitId_KaynakBil~" ON "FinansKaynakBastirmalari" ("KaynakTuru", "KaynakKayitId", "KaynakBileseni");
CREATE UNIQUE INDEX "IX_FinansParaBirimleri_Kod" ON "FinansParaBirimleri" ("Kod");
CREATE UNIQUE INDEX "IX_FinansParametreleri_Kod" ON "FinansParametreleri" ("Kod");
CREATE INDEX "IX_FinansSiparisKalemleri_FinansIsKaydiId" ON "FinansSiparisKalemleri" ("FinansIsKaydiId");
CREATE UNIQUE INDEX "IX_FinansSiparisKalemleri_FinansSiparisId_FinansIsKaydiId" ON "FinansSiparisKalemleri" ("FinansSiparisId", "FinansIsKaydiId");
CREATE INDEX "IX_FinansSiparisKalemleri_FinansUrunId" ON "FinansSiparisKalemleri" ("FinansUrunId");
CREATE INDEX "IX_FinansSiparisleri_Durum_SiparisTarihi" ON "FinansSiparisleri" ("Durum", "SiparisTarihi");
CREATE UNIQUE INDEX "IX_FinansSiparisleri_KayitNo" ON "FinansSiparisleri" ("KayitNo");
CREATE UNIQUE INDEX "IX_FinansSiparisleri_PoNumarasi" ON "FinansSiparisleri" ("PoNumarasi") WHERE "IptalEdildi" = false;
CREATE INDEX "IX_FinansUrunEslesmeleri_FinansUrunId_IsTuru" ON "FinansUrunEslesmeleri" ("FinansUrunId", "IsTuru");
CREATE UNIQUE INDEX "IX_FinansUrunleri_Kod" ON "FinansUrunleri" ("Kod");

-- Kimlik dizileri başlangıç verisinin ardından devam eder
SELECT setval(
    pg_get_serial_sequence('"FinansParaBirimleri"', 'Id'),
    GREATEST(
        (SELECT MAX("Id") FROM "FinansParaBirimleri") + 1,
        nextval(pg_get_serial_sequence('"FinansParaBirimleri"', 'Id'))),
    false);
SELECT setval(
    pg_get_serial_sequence('"FinansParametreleri"', 'Id'),
    GREATEST(
        (SELECT MAX("Id") FROM "FinansParametreleri") + 1,
        nextval(pg_get_serial_sequence('"FinansParametreleri"', 'Id'))),
    false);

DO $$
BEGIN
    RAISE NOTICE '"%" veritabanında Finans ve Ambalaj tabloları yeniden oluşturuldu.', current_database();
END $$;

COMMIT;
