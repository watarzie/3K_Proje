-- Salt okunur etki raporu. Yeni tablolara bağımlı değildir; eski sürümde çalışır.
BEGIN READ ONLY;
SELECT "SandikCinsi",count(*) AS "KayitSayisi",sum("Adet") AS "SandikAdedi",
 sum(coalesce("M3Override","HesaplananToplamM3")) AS "EskiNetM3",sum("SarfM3") AS "EskiSarfM3",
 count(*) FILTER (WHERE "UretimDurumu"=3) AS "TamamlanmisKayit"
FROM "AmbalajUretimKayitlari" WHERE "SandikCinsi" NOT IN(1,2) GROUP BY "SandikCinsi";
SELECT "TipId", count(*) AS "KaynakSandikSayisi" FROM "Sandiklar" WHERE "TipId" NOT IN(1,2,3) GROUP BY "TipId";
SELECT count(*) AS "EskiTamamlanmis",count(*) FILTER(WHERE "TamamlanmaTarihi" IS NULL) AS "TarihiOlmayan"
FROM "AmbalajUretimKayitlari" WHERE "UretimDurumu"=3;
SELECT "KaynakTuru",count(*) AS "MaliKayitSayisi" FROM "FinansIsKayitlari" WHERE upper("KaynakTuru")='AMBALAJURETIM' GROUP BY "KaynakTuru";
COMMIT;
