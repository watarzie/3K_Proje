---
paths:
  - "3K.Application.Tests/**"
  - "scripts/test/**"
---

# Test

Özet kurallar (Release, yalnız değişen alan) `CLAUDE.md`'dedir; burada ayrıntı ve komutlar var. İzole PostgreSQL ile koşmak
için `postgres-test` skill'ini kullan.

- Tek test projesi: `3K.Application.Tests` (xUnit). Testler alanlara göre adlandırılmış sınıflardır (ör. `FinansSekmeYetkileriTests`, `FinansPoFaturaKurallariTests`, `AmbalajUretimTests`, `YetkiModeliTests`).
- Finans servislerini testte üretim DI kaydıyla kur: `FinansTestServisleri`.
- **Release konfigürasyonunda çalıştır**: Visual Studio'da açık çalışan API `bin/Debug` dosyalarını kilitler.
- **Sadece değişen alanın testlerini koş**; tam paketi yalnız kullanıcı isterse çalıştır. Raporda hangi alt kümenin koştuğunu söyle.

```powershell
dotnet build 3K_Proje.slnx -c Release
dotnet test 3K.Application.Tests/3K.Application.Tests.csproj -c Release --filter "FullyQualifiedName~FinansSekmeYetkileriTests"
dotnet test 3K.Application.Tests/3K.Application.Tests.csproj -c Release --filter "FullyQualifiedName~FinansPoFaturaKurallariTests|FullyQualifiedName~FinansGiderFiltresiTests"
dotnet test 3K.Application.Tests/3K.Application.Tests.csproj -c Release --filter "FullyQualifiedName~Ambalaj&Category!=Postgres"
```

## PostgreSQL entegrasyon testleri

- `[PostgresRaporFact]` / `[SandikPostgresFact]` + `Trait("Category", "Postgres")` ile işaretlidir; `THREEK_TEST_POSTGRES` ortam değişkeni yoksa **atlanır**.
- Değişken izole bir yerel PostgreSQL örneğinin yönetim veritabanını gösterir: host `127.0.0.1`/`localhost`, port **55439**, Database `postgres` (testler bunu doğrular). Her test sınıfı kendi geçici DB'sini `CREATE DATABASE` ile kurar ve `DROP ... WITH (FORCE)` ile siler. Asla `3K_Db`'yi göstermez.

```powershell
$env:THREEK_TEST_POSTGRES = "Host=127.0.0.1;Port=55439;Database=postgres;Username=postgres;Password=<yerel-test-parolasi>"
dotnet test 3K.Application.Tests/3K.Application.Tests.csproj -c Release --filter "FullyQualifiedName~FinansPostgresTests"
```

- In-memory testler PostgreSQL transaction/trigger/FK/eşzamanlılık davranışının kanıtı değildir; bu sınırlar değiştiyse izole entegrasyon testi koş, koşamıyorsan açıkça söyle.
- İş kuralı kapıları için runner: `./scripts/test/Invoke-IsKurallariTests.ps1 -Configuration Release`.
- Test devre dışı bırakma veya beklentiyi yanlış davranışa uydurma yok. Bilinen önceden var olan test hatası yoktur.
- Hata düzeltmesinde gerçek işlem sırası ve ret durumunda verinin değişmediği sınanır; kural, sahibi olan en alt katmanda test edilir (saf Core politikası veritabanısız, handler orkestrasyonu, veritabanı/HTTP sınırı entegrasyon testiyle).
