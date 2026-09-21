-- Salt okunur. Tutar/tarih düzeltmesi yapmaz. Sonuçları migration öncesi/sonrası arşivleyin.
SELECT 'İş' tur, count(*) adet, min("FinansDonemi") ilk_donem, max("FinansDonemi") son_donem FROM "FinansIsKayitlari";
SELECT "ParaBirimiSnapshot", sum("NetTutarSnapshot") net, sum("KdvTutariSnapshot") kdv, sum("ToplamTutarSnapshot") brut FROM "FinansSiparisKalemleri" GROUP BY "ParaBirimiSnapshot";
SELECT f."Id", f."FaturaNumarasi", f."BelgeNetTutarSnapshot", sum(l."NetTutarSnapshot") satir_net, f."BelgeToplamTutarSnapshot", sum(l."ToplamTutarSnapshot") satir_brut
 FROM "FinansFaturalari" f JOIN "FinansFaturaKalemleri" l ON l."FinansFaturaId"=f."Id" GROUP BY f."Id"
 HAVING f."BelgeNetTutarSnapshot" IS NOT NULL AND (f."BelgeNetTutarSnapshot"<>sum(l."NetTutarSnapshot") OR f."BelgeToplamTutarSnapshot"<>sum(l."ToplamTutarSnapshot"));
SELECT p."Id", p."PoNumarasi", count(DISTINCT l."ParaBirimiSnapshot") para_birimi_sayisi FROM "FinansSiparisleri" p JOIN "FinansSiparisKalemleri" l ON l."FinansSiparisId"=p."Id" GROUP BY p."Id" HAVING count(DISTINCT l."ParaBirimiSnapshot")>1;
SELECT "SandikTipi", count(*) adet, sum("ToplamM3") eski_finans_m3 FROM "FinansIsKayitlari" WHERE "SandikTipi" ILIKE '%kontr%' OR "SandikTipi" ILIKE '%katlan%' GROUP BY "SandikTipi";
SELECT w."Id", w."ProjeNo", w."IsAdi", sum(l."NetTutarSnapshot") aktif_po_net,
 round(w."BirimFiyatSnapshot"*(CASE w."FiyatlandirmaBirimiSnapshot" WHEN 1 THEN w."Adet" WHEN 2 THEN w."ToplamM3" ELSE 1 END),2) mevcut_is_net
 FROM "FinansIsKayitlari" w JOIN "FinansSiparisKalemleri" l ON l."FinansIsKaydiId"=w."Id" JOIN "FinansSiparisleri" p ON p."Id"=l."FinansSiparisId"
 WHERE NOT p."IptalEdildi" GROUP BY w."Id" HAVING sum(l."NetTutarSnapshot")>round(w."BirimFiyatSnapshot"*(CASE w."FiyatlandirmaBirimiSnapshot" WHEN 1 THEN w."Adet" WHEN 2 THEN w."ToplamM3" ELSE 1 END),2);
