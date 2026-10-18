---
name: postgres-test
description: İzole yerel PostgreSQL test sunucusunu (127.0.0.1:55439) başlatıp değişen alanın Release testlerini PostgreSQL entegrasyon vakalarıyla birlikte koşar ve sunucuyu kapatır. PostgreSQL'e dokunan (transaction, trigger, FK, eşzamanlılık, SQL ifadesi, şema betiği) bir değişikliği doğrularken kullan.
argument-hint: "<test filtresi, ör. FullyQualifiedName~Finans|FullyQualifiedName~Ambalaj>"
---

# İzole PostgreSQL ile test koşumu

Bu depo testlerinde `[PostgresRaporFact]` / `[SandikPostgresFact]` vakaları `THREEK_TEST_POSTGRES` yoksa **atlanır**.
Bu skill izole yerel sunucuyu kurar/başlatır, ortam değişkenini ayarlar, filtreli testleri koşar ve kapatır.

## Kurallar
- Asla gerçek `3K_Db`'ye bağlanma; betik yalnız `127.0.0.1` üzerinde, `Database=postgres` yönetim veritabanıyla çalışır
  (testler bunu doğrular, her sınıf kendi geçici DB'sini açıp `DROP ... WITH (FORCE)` ile siler).
- Yalnız değişen alanın filtresiyle koş; tam paketi yalnız kullanıcı isterse koş.
- Release yapılandırması kullanılır (Visual Studio'daki API `bin/Debug`'ı kilitler).

## Çalıştırma
Argüman verilmezse değişen dosyalardan uygun filtreyi seç (ör. `FullyQualifiedName~Finans`), sonra:

```powershell
& "$env:CLAUDE_PROJECT_DIR/.claude/skills/postgres-test/postgres-test.ps1" -Filter "$ARGUMENTS"
```

- İlk çalıştırmada küme `%LOCALAPPDATA%\3K\pg-test` altında `initdb` ile kurulur. Parola `THREEK_TEST_PG_PASSWORD`
  ortam değişkeninden okunur, yoksa yerel test parolası `yerel-test` kullanılır.
- Port 55439 bağlanamazsa betik sunucuyu 55440'ta açıp 55439'u yerel yönlendiriciyle bağlar (Windows'ta görülen
  "could not bind ... Permission denied" durumu).
- Yerel porta bağlanma sandbox'ta engellenebilir; komutu sandbox dışında çalıştırman gerekebilir.
- Derleme zaten güncelse `-NoBuild`; sonraki koşumlar için sunucuyu açık bırakmak istersen `-KeepRunning`.

## Rapor
Sonuçta şunu söyle: koşulan filtre, başarılı/başarısız/atlanan sayıları, PostgreSQL vakalarının gerçekten koştuğu
(Atlanan: 0). Bilinen önceden var olan hata yoktur (2026-10-18'de giderildi); her başarısızlığı çıktısıyla birlikte bildir.
