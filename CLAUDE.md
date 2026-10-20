# CLAUDE.md — 3K_Proje (Backend)

3K platformunun .NET 10 + PostgreSQL arka ucu. Ön yüz ayrı depodadır: `C:\Users\Watarzie\Desktop\3k_onyuz` (Angular 20).
Bu dosya depodaki kalıcı mühendislik kurallarının tek kaynağıdır.
İş kuralı değişikliğinden önce `docs/is-kurallari/GRID_3K_IS_KURALLARI.md` ve `docs/is-kurallari/TEST_KAPSAMI.md` okunur.

Kullanıcıyla Türkçe konuşulur. Kod içindeki Türkçe alan adları (`Proje`, `Ceki`, `Sandik`, `Tahsis`, `Sevk`, `Onay`, `Finans`, `Ambalaj`…) korunur; İngilizceye çevrilmez.

## Çalışma kuralları (önce bunlar)

- **Kullanıcı açıkça onay vermeden asla commit atma**; `git add` ile stage etme, push yapma. Sor: "Commit edebilir miyim?"
- `appsettings*.json` içindeki JWT anahtarı, bağlantı cümlesi ve DataProtection yolu **yalnız yerel geliştirme** değerleridir; güvenlik bulgusu olarak raporlama, değiştirme.
- Gizli değerler git'e girmeyen `3K_API/appsettings.Secrets.json` dosyasındadır (`.gitignore`'da). `Program.cs` bu dosyayı en son ekler, yani ortam değişkenlerini de ezer. İçeriğini asla yazdırma, kopyalama, log'a ya da dokümana koyma.
- Gerçek uygulama veritabanı `3K_Db` üzerinde **yıkıcı SQL çalıştırma** (DROP/TRUNCATE/sıfırlama betikleri). Bunlar yalnız izole test sunucusunda denenir; gerçek DB'de kullanıcı kendisi çalıştırır.
- Sadece değişen alanın testlerini koş; her seferinde bütün test paketini çalıştırma (bkz. Test).
- İşe `git status --short` ile başla, var olan kullanıcı değişikliklerini koru, en küçük tutarlı değişikliği yap.
- Miktar, para, yetki veya kalıcı veri açısından belirsiz bir gereksinimde inşa etmeden önce sor.

## Çözüm yapısı

`3K_Proje.slnx` (src: 4 proje, tests: 1 proje). Tüm projelerde `Nullable` ve `ImplicitUsings` açık; kök namespace `_3K.*`.

| Proje | Sorumluluk | Bağımlılık |
|---|---|---|
| `3K.Core` | Entity'ler (`Entities/`), enum'lar, `Models/` (alan modelleri + saf kurallar: `FinansTutarKurallari`, `FinansTarihKurallari`, `FinansParaBirimiKurali`, `AmbalajIslemKurallari`…), `Interfaces/` (`IUnitOfWork`, `IGenericRepository<T>`, `IFinans*Service`…), `Constants/` (`YetkiKodlari`, `ProjeTipiYetkiKodlari`, `SistemRolleri`), `Exceptions/`, `Helpers/TurkeyTime` | Hiçbir projeye ve framework'e bağlı değil |
| `3K.Application` | MediatR use case'leri: `Features/<Alan>Islemleri/{Commands,Queries,Validators,DTOs}`, `Behaviors/` (pipeline), `Common/` (`Result`, `ISecuredRequest`, `RequestMenuPermissionResolver`, `AtomicTransactionExtensions`, yardımcı politikalar) | Yalnız Core (MediatR 12, FluentValidation 12) |
| `3K.Infrastructure` | EF Core + Npgsql (`Data/AppDbContext` + `<Alan>ModelBuilderExtensions`), `Repositories/` (`UnitOfWork`, `GenericRepository`), servis implementasyonları (`Services/`, `Services/Finans`, `Services/Raporlar`, `Services/Cekiler`), QuestPDF/ClosedXML raporları, 2FA, arka plan işleri | Yalnız Core |
| `3K_API` | Controller'lar, `Extensions/` (`AddApi`, `UseApiPipeline`, `ResultExtensions`), middleware, Serilog, Ambalaj kaynak senkronizasyon worker'ı; composition root | Application + Infrastructure + Core |
| `3K.Application.Tests` | xUnit birim, regresyon ve PostgreSQL entegrasyon testleri (namespace `_3K.Application.Tests`) | Hepsine referans verir |

- Bağımlılık yönü içe doğrudur: API → Application/Infrastructure → Core. Core'a EF/HTTP eklenmez; Application `AppDbContext`'e veya Infrastructure'a referans vermez.
- DI kayıtları: `3K.Application/DependencyInjection.cs` (`AddApplication`), `3K.Infrastructure/DependencyInjection.cs` (`AddInfrastructure`, finans için `AddFinansServices`), `3K_API/Extensions/ApiServiceCollectionExtensions.cs` (`AddApi`).
- MediatR pipeline sırası (dıştan içe): `IsHatasiLoglamaBehavior` → `UnhandledExceptionBehavior` → `AuthorizationBehavior` → `ValidationBehavior` → `ApprovalBehavior` → handler.
- Başarısız her istek (400/401/403/404/409/429; 500 hariç, o zaten Error) `IsHatasiLoglamaBehavior` ile tek satır Warning loglanır: istek adı, kullanıcı, mesaj ve `IstekGirdisi` ile maskelenmiş girdi (500 için aynı girdi `UnhandledExceptionBehavior`'ın Error satırında). Parola/token/anahtar/doğrulama kodu alanları `***`; e-posta alanı biçimliyse kısmen (`de***@gmail.com`), değilse tamamen gizlenir (kutuya yanlışlıkla yazılan şifre loga düşmez); dosya akışı ve bayt dizisi yalnız boyutuyla yazılır; yeni gizli alan adı eklenirse `IstekGirdisi` ve testini güncelle. Satırdaki `CorrelationId`, yanıtın `X-Correlation-ID` başlığıdır; ön yüz hata bildiriminde (401 hariç) "Takip: …" olarak gösterir. Tek takip kodu budur: sunucu ayrı kod üretmez, yanıt mesajına kod yazmaz ve log şablonunda `CorrelationId`/`TakipKodu` adlı özellik kullanmaz (istek kodunu ezer). Kalıcı kayda kod gerekiyorsa `ITakipKoduSaglayici` (ör. onay yürütme hatası). Service log 30 gün, trace log 7 gün saklanır.
- Yerel API: `http://localhost:50447` (`3K_API/Properties/launchSettings.json`); ön yüz bu adrese bağlanır.

## Bir özellik nasıl yazılır

Akış: **Controller → Command/Query → (Authorization → Validation → Approval) → Handler → Servis/Repository → Core modeli/kuralı**.

### Command / Query

- Her use case bir request sınıfıdır; yetki isteğin kendisinde bildirilir:
  - `ISecuredRequest` + `IRequiresMenuPermission` (`RequiredMenuKod`, `RequiredYetkiTipi` = `YetkiTipi.R` görüntüleme / `YetkiTipi.W` işlem).
  - Birden çok izin gerekiyorsa `IRequiresMenuPermissions.RequiredMenuPermissions` (`MenuPermissionRequirement.Read(kod)` / `.Write(kod)`; varsayılan AND, `PermissionMatch => MenuPermissionMatch.Any` açıkça belirtilir).
  - Proje/sandık bağlamlı izinler `IRequiresProjectMenuPermission` / `IRequiresSandikMenuPermission` ile ya da `RequestMenuPermissionResolver` içinde tanımlanır (proje tipi DB'den okunur, istemciden değil).
  - `ISecuredRequest` olmayan istek, açık istisna listesinde (`MenuAuthorizationExceptions`) değilse 403 alır.
- Finans tabanları (yetki yalnız sekme düzeyindedir):
  - `FinansQuery<T>` (`Features/FinansIslemleri/Queries/FinansQuery.cs`): listelenen sekmelerden herhangi birinin R'si; işlem önizlemeleri `RequiredYetkiTipi`'ni W yapar.
  - `FinansCommand<T>` ve `FinansCommand` (`Commands/FinansCommand.cs`): listelenen sekmelerden herhangi birinin W'si.
  - Alt sınıf yalnız `public override IReadOnlyList<string> Sekmeler => [YetkiKodlari.Finans.SekmeGiderler];` yazar. Birden çok sekmede açılan ortak işlem `3K.Core/Constants/FinansSekmeleri.cs` kümelerini kullanır (`Kayit`, `KayitVeGider`, `IsDuzenleme`, `SiparisAcma`, `GiderTanimlari`, `Varlik(tur)`).
- Finans alanları: Panel, IsKaydi, SiparisFatura, Gider, Tanim, Belge, Rapor. Dosya adları `Finans<Alan>Commands.cs` / `Finans<Alan>CommandHandlers.cs` / `Finans<Alan>Queries.cs` / `Finans<Alan>QueryHandlers.cs` / `Validators/Finans<Alan>Validators.cs` kalıbındadır.

### Handler

- Handler orkestre eder (yükle → kuralı kontrol et → değiştir → kaydet → döndür) ve yalnız Core soyutlamalarına bağlıdır. Constructor injection; `IServiceProvider`'dan servis çözme yok.
- Finans handler'ları servis çağrısını `FinansHandlerHelper` ile `Result`'a çevirir:
  - `ExecuteAsync<T>(() => service.XAsync(...))` → `Result<T>.Success`.
  - `ExecuteOptionalAsync<T>(..., "Gider bulunamadı.")` → servis `null` dönerse 404.
  - `ExecuteAsync(Func<Task<bool>>, notFoundMessage)` → `false` ise 404.
- Beklenen retler `Result.Failure(mesaj, kod)` ile döner. `Result` yapamayan derin servisler kullanıcıya gösterilecek reddi `IsKuraliException` (varsayılan 400, `IsKuraliException.Cakisma(...)` = 409) veya `KayitBulunamadiException` (404) ile bildirir; pipeline durum koduna çevirir. `InvalidOperationException` mesajı kullanıcıya ulaşmaz.
- Hepsi-ya-hiçbiri akışlar: `unitOfWork.ExecuteAtomicAsync(...)` (başarısız `Result` dönünce de geri alır). `IUnitOfWork.ExecuteInTransactionAsync` delegate normal dönerse commit eder; yalnız bilinçli kısmi başarı akışlarında kullan.
- Finans yazmaları servis içinde `FinansIslemYurutucu.AtomikAsync` ile Serializable transaction'da çalışır, dış transaction'a katılır; değişiklik geçmişi `FinansDenetimKaydi` ile aynı transaction'da yazılır.
- Her `CancellationToken` aktarılır; `.Result`/`.Wait()` yok; aynı `DbContext` üzerinde `Task.WhenAll` yok.

### Validator

- FluentValidation, `AddValidatorsFromAssembly` ile otomatik kaydolur; dosya `Validators/` altında, sınıf adı `<Request>Validator`.
- Kurallar Core'daki aynı kural fonksiyonunu çağırır (ör. `FinansTarihKurallari.GecerliTarih`, `FinansGerekceKurali.Gecerli`); kuralı validator'da yeniden yazma.

### Controller

- Controller yalnız bind eder, MediatR'a gönderir ve `ToActionResult()` ile eşler; iş kuralı ve veri erişimi yoktur.
- Finans controller'ları `FinansControllerBase`'den türer (`[Authorize]`, `[Route("api/finans")]`, `[TypeFilter(typeof(FinansAlanErisimFiltresi))]`, `Mediator` özelliği). Örnek: `FinansGiderController`.
- `ResultExtensions.ToActionResult()` (`3K_API/Extensions`): başarıda ham DTO (200), `StatusCode == 202` ise "onaya sunuldu" gövdesi; hata gövdesi her yerde `{ message, issues? }` ve 401/403/404/409/429/500/400 eşlemesi. Dosya için `Result<DosyaIcerigi>` + `ToFileResult()`.
- **202 = onay kuyruğuna alındı, uygulanmadı.**
- Ekran istekleri Application `DTOs` altındaki record'lardır ve kendilerini servis modeline çevirir (ör. `FinansGiderKaydetDto.GiderModeli()`); sorgu filtreleri `3K_API/Controllers/FinansIstekModelleri.cs` (`FinansFilterRequest.ToModel()`).

### Modeller ve DTO'lar

- Servis giriş modelleri `3K.Core/Models` altında genelde `sealed record` (ör. `FinansGiderKaydetModel`); yanıt modelleri çoğunlukla `init` özellikli `sealed class` (ör. `FinansGiderModel`).
- Finans servis arayüzleri `3K.Core/Interfaces/IFinans*Service.cs`; implementasyonları `internal` olarak `3K.Infrastructure/Services/Finans` altında. Servisler birbirini çağırmaz; ortak davranış `FinansIslemYurutucu`, `FinansDenetimKaydi`, `FinansFiyatBelirleyici`, `FinansSorgulari`, `FinansModelEslemeleri` üzerinden.

## Yetki modeli

- Bütün kodlar tek kaynaktan gelir: `3K.Core/Constants/YetkiKodlari.cs` (`YetkiKodlari.Finans.*`, `YetkiKodlari.Ambalaj.*`, `YetkiKodlari.Proje.*`…). Kod literali yazma. Proje tipine bağlı kodlar `ProjeTipiYetkiKodlari.Getir(projeTipiId)`.
- Ekran, işlem ve alan kodlarının hepsi `MenuTanimlari` ağacında N/R/W olarak `YetkiAgaci` ile değerlendirilir: R görüntüleme, W işlem. Üst düğüm alt düğümün tavanıdır; üst W alt izni kendiliğinden vermez. Yetki yalnız rolden gelir: kullanıcıya özel izin/ret yoktur (`KullaniciYetkileri` tablosu `20261003_01_Kisisel_Yetkileri_Kaldir.sql` ile kaldırıldı). İstisna gerekiyorsa ayrı, dar bir rol tanımlanır.
- Admin rolü yetki bypass'ı değildir (`SistemRolleri.AdminId` = 1, kendi `RolYetkileri` ile değerlendirilir). `X-Menu-Kod` başlığı, route veya gövde yetki kaynağı değildir.
- Kullanıcıya rol atama denetimi `IRolAtamaService` (`RolAtamaService`): `kullanicilar:W` ister; kullanıcı kendi rolünü değiştiremez, sahip olmadığı izinleri içeren rolü başkasına atayamaz, Admin rolünü yalnız mevcut Admin atar.
- Yeni kod eklerken birlikte güncelle: `YetkiKodlari` + `scripts/database/` altında `MenuTanimlari` satırı (`LabelKey` = `MENU.<KOD>`) + ön yüz `src/app/core/constants/yetki-kodlari.ts` + TR/EN `MENU.*` çevirileri.

### Finans: yalnız sekme izni, alan maskesi yok

- 13 Ekim 2026'dan beri Finans'ta alt işlem ve alan izni yoktur: yalnız 8 sekme düğümü (`YetkiKodlari.Finans.Sekme*`) ve İş Takibi üstündeki vade uyarısı için `finans-vade-uyarisi` (R). Sekmenin R'si sekmedeki bütün alanları (tutar, birim fiyat, gelir, gider, kârlılık, ölçü, m³) gösterir; W'si sekmedeki bütün işlemleri (kalıcı silme dahil) yapar.
- Yanıtlar maskelenmez (`FinansAlanErisimFiltresi`/`FinansAlanProjeksiyonu` kaldırıldı); rapor ve belge dosyaları ayrıca alan izni istemez. Yeni Finans alanı/DTO'su için yetki işi yoktur; yeni istek için doğru `Sekmeler` yeterlidir.
- Yeni Finans alt izin kodu ekleme; ince ayrım gerekiyorsa sekme bazında ayrı rol tanımlanır. Ön yüz aynası: `features/finans-yonetimi/ortak/finans-izinleri.ts` (sunucudaki kümelerle aynı). Eşleme tablosu: `docs/guvenlik/SUNUCU_YETKI_ESLEMESI.md`.
- Testler: `FinansSekmeYetkileriTests`, `FinansModuluTests`, `FinansAlanHttpSecurityTests`.
- Ambalaj tarafında maskeleme açıktır: `AmbalajAlanMaskesi` ve `AmbalajYetkilendirmeYardimcisi.DtoyuMaskele` (`3K.Application/Features/AmbalajIslemleri`); yeni Ambalaj DTO'sunda maskeyi ve `AmbalajAlanMaskelemeTests`'i güncelle. Kayda bağlı ek izinler handler içinde `AmbalajIslemKurallari` + `AmbalajKritikIslemDenetimi` ile kontrol edilir.

## Veri erişimi

- `AppDbContext` (`3K.Infrastructure/Data/AppDbContext.cs`) yalnız `DbSet`'leri ve `OnModelCreating` içinde alan uzantılarını çağırır: `ConfigureFinansModule`, `ConfigureAmbalajUretimModule`, `ConfigureSandikModule`… Eşleme her alanın `<Alan>ModelBuilderExtensions.cs` dosyasındadır (ör. `FinansModelBuilderExtensions.cs`). Bazı entity'lerin `DbSet`'i yoktur (ör. `FinansParaBirimi`, `FinansParametre`); `context.Set<T>()` ile erişilir.
- `GenericRepository<T>` semantiği — **izleme (tracking) çakışmalarına dikkat**:
  - `FindAsync(predicate)`, `GetAllAsync()`, `GetAllWithIncludeAsync()` → `AsNoTracking` (izlenmeyen nesne döner).
  - `GetByIdAsync(id)` → `DbSet.FindAsync` (izlenir).
  - `Queryable()` → izlenen `IQueryable`.
  - `Update(entity)` → `DbSet.Update` (bütün grafiği Modified işaretler). Aynı anahtarlı bir nesne zaten izleniyorsa (ör. `Queryable()`/`GetByIdAsync` ile yüklenmiş) ve `FindAsync`'den gelen ayrı bir örnek `Update` edilirse EF "already being tracked" hatası verir. İzlenen nesneyi doğrudan değiştirip `SaveChangesAsync` çağırmak genelde doğrusudur.
- `UnitOfWork.SaveChangesAsync` eşzamanlılık/benzersizlik/FK hatalarını `ConcurrencyConflictException`, `UniqueConstraintViolationException`, `ReferentialIntegrityConflictException`'a çevirir. Commit/rollback sonrası yan etkiler için `RegisterAfterCommit` / `RegisterAfterRollback`.
- Okumalarda büyük listeler DB'de filtrelenir, sayfalanır ve projekte edilir; `IReadQueryExecutor` kullanılabilir.

### Şema değişiklikleri: EF migration YOK

- Şema/referans veri değişikliği = EF model/configuration değişikliği **+** `scripts/database/YYYYMMDD_NN_Konu.sql` betiği. Betik idempotent ve tek transaction'dır (`BEGIN … COMMIT`), uygulama açılışında çalışmaz.
- Çalıştırma sırası ve açıklamaları `scripts/database/README.md` tablosundadır; yeni betik eklerken README'yi de güncelle.
- Finans/Ambalaj tablolarına şema değişikliği eklenince `20260929_01_Finans_Ambalaj_Sifirlama.sql` de güncellenir (sıfırlama güncel modeli kurmalı; `CanliyaAlimBetikleriTests` denetler).
- Yıkıcı betikler onay ayarı ister ve ayar `current_database()` ile aynı olmalıdır. Örnek: `20260929_01_Finans_Ambalaj_Sifirlama.sql` yalnız aynı oturumda `SET uc_k.finans_ambalaj_sifirla = '<veritabanı adı>'` verilmişse çalışır. Yeni yıkıcı betik aynı kalıbı izler.
- pgAdmin kullanıcısına sorgu aracını `3K_Db` düğümünden açmasını söyle (sunucu düğümü varsayılan `postgres` DB'sine bağlanır).
- Eşleme taşıma/bölme gibi davranış değiştirmemesi gereken refactor'larda `ToDebugString` ve `GenerateCreateScript` çıktısını önce/sonra karşılaştır.

## Korunacak alan kuralları

- Zincir `Proje → Ceki → CekiSatiri` ve `SandikIcerik` tahsis ilişkileri korunur. Barkod satır kimliği değildir; bölünmüş satırda ilgili `SandikIcerikId` ile çalışılır.
- `IstenenAdet`, `TahsisMiktari` ve `KonulanAdet` ayrı miktarlardır; planlama değişikliği fiziksel teslim üretmez. `EksikMiktar`, `KalanMiktar` ve `GridEksikMiktar` birbirinin yerine kullanılmaz; eski `null` sayaçlar sebepsiz sıfıra çevrilmez. Miktarlar `decimal` kalır.
- Aktif Grid sevk partisi sayaçları ile kümülatif 3K teslimi ayrıdır; yeni parti önceki fiziksel teslimi veya başka sandığın karşılamasını sıfırlamaz.
- Sandık kapatma, Grid sevki ve son sevkiyat ayrı olaylardır; kalite durumları, Grid iptal/kapalı durumları, sevk kilidi ve saha kaynak korumaları korunur.
- Aynı işi yapan tekil, toplu ve eski uçlar birlikte kontrol edilir; birinde düzeltilen kural ötekinde farklı kalmaz.
- Çeki revizyonu dosya özetini, değişmez önizlemeyi, kaynak/saha bağlarını ve geçmişi korur; geçmiş güncel veriden yeniden kurulmaz.
- Üretim tamamlandı varsayımıyla finans kaydı oluşturulmaz veya silinmez; uygunluk ve fiyat geçmişi servis politikalarıyla doğrulanır.
- Sorgular durum değiştirmez; yalnız komutlar değiştirir.

### Onay, tekrar ve eşzamanlılık

- Onay hattı, saklanan komut bütünlüğü, onaylayan izni ve yürütme durumu kontrolleri atlanmaz; istek, karar ve yürütme ayrı olaylardır. Giriş/2FA adımı tam oturumdan ayrıdır; yeni yetkilendirme herkese açık giriş akışını kilitlemez.
- `xmin` eşzamanlılığı, tekrar anahtarları, benzersiz kısıtlar ve hareket geçmişi korunur: aynı anahtarla aynı istek ikinci kez uygulanmaz, farklı gövdeyle gelirse çakışmadır (409).
- Dosya ve bildirim yan etkileri transaction sınırına göre (`RegisterAfterCommit`/`RegisterAfterRollback`) çalışır. Kullanıcının verdiği dosya adı güvenilmezdir; dosya sunucunun ürettiği güvenli adla, hedef klasör denetlenerek saklanır.
- Koda veya log'a gizli değer, bağlantı cümlesi, JWT anahtarı, TOTP sırrı/kurtarma kodu ya da gerçek kullanıcı verisi konmaz. API veya worker'lar test için canlı ayarlarla başlatılmaz; canlı veri değiştirilmez.
- Rapor değişikliğinde ortak stil, doğru sandık kimliği/adı, doğal numara sırası ve tekil/toplu PDF-Excel çıktılarında aynı içerik korunur; örnek çıktı kontrol edilir.

## Zaman ve para birimi

- Saklanan/gösterilen işlem ve rapor zamanları `TurkeyTime.Now` (`3K.Core/Helpers/TurkeyTime.cs`). `DateTime.Now` kullanma. JWT/2FA ömürleri ayrı sözleşmedir.
- `Program.cs` `Npgsql.EnableLegacyTimestampBehavior` anahtarını açar; tarih dönüşümü eklemeden önce akışın kullanımını kontrol et.
- Para birimleri `FinansParaBirimleri` tablosundaki `FinansParaBirimi` lookup kayıtlarıdır (TRY/EUR tohumlanır, kullanıcı ekleyebilir). **Kodda para birimi listesi sabitleme**; `FinansParaBirimiKurali.Normalize` yalnız ISO-4217 biçimini kontrol eder, geçerlilik tablodan gelir. Vade parametreleri `FinansParametre` (`FinansParametreKodlari`).

## Test

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

### PostgreSQL entegrasyon testleri

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

## Kod stili ve kurallar

- Değiştirdiğin dosyanın namespace (dosya kapsamlı veya bloklu), girinti, süslü parantez ve adlandırma stilini koru. Yeni kodda primary constructor yaygındır (ör. `FinansGiderCommandHandlers(IFinansGiderService service)`).
- Satır sonları: `core.autocrlf=true`; çalışma kopyası CRLF'dir, yeni dosyalar da CRLF yazılır.
- Yorumlar Türkçe ve "neden"i anlatır; public tiplerde `/// <summary>` kullanılır. UTF-8 ve Türkçe karakterler bozulmaz.
- Rapor/PDF/Excel/önizlemede enum değeri `ToString()` ile yazılmaz (`OzelIs`, `SiparisBekliyor` değil): finans için `FinansEtiketleri` (`3K.Core/Models/FinansEtiketleri.cs` → "Özel İş", "Sipariş Bekliyor"), kategori için `FinansKategoriKurali.Ad`. API sözleşmesi enum sayısıyla kalır.
- Kullanıcıya dönen mesajlar Türkçedir; iç istisna detayı, dosya yolu veya gizli değer döndürülmez.
- İş kuralı controller'a yazılmaz; tekrar eden kural Core'daki politikaya/kural sınıfına çıkarılır. Kural SQL karşılığı gerekiyorsa `FinansSorgulari` içinde tek ifade olarak tanımlanır (ör. `IsNetIfadesi`) ve eşdeğerliği testle sabitlenir.
- Kullanılmayan soyutlama, reflection tabanlı dispatch, isimden davranış seçimi yok. Derleyici uyarısı bastırılmaz.
- Sözleşme değişikliğinde (endpoint, DTO, enum, izin kodu, çeviri) ön yüz `3k_onyuz` tarafını da aynı işte güncelle.
- Commit mesajları Türkçe, conventional biçimde: `refactor(finans): ...`, `fix(rapor): ...`, `feat(db): ...` (yalnız kullanıcı onay verdiğinde).

## Çalıştırma

- API: `dotnet run --project 3K_API` (profil `3K_API`: `http://localhost:50447`, `https://localhost:50446`). Test için canlı ayarlarla başlatılmaz.
- Ön yüz: `C:\Users\Watarzie\Desktop\3k_onyuz` içinde `npm start` (`ng serve`, port 4200); Claude Code'da `.claude/launch.json` → `onyuz`.

## Dokunulmayacak yerler

- Üretilmiş veya yerel klasörler: `publish/`, `artifacts/`, `.codex-build/`, `.tmp/`, `.vs/`, `bin/`, `obj/`, `.claude/worktrees/` ve git dışı canlıya alım kopyaları (`canliya-alim-*/`; betiğin aslı `scripts/database` altındadır, kopyayı değil aslını düzenle).

## Claude Code araçları

- Ajanlar (`.claude/agents`): `kod-gozden-gecirici` (commit önermeden önce, salt okunur), `sema-betik-denetcisi` (EF modeli/betik/izin kodu değişince ve canlıya alımdan önce), `finans-maske-denetcisi` (Ambalaj DTO'su veya Finans isteği değişince), `is-kurali-dokumantasyon` (iş kuralı değişince doküman hizalama), `canli-hata-teshisci` (canlı hata bildiriminde, düzeltmeden önce teşhis).
- Beceriler: `postgres-test` (izole PostgreSQL ile test), `yeni-yetki-kodu` (uçtan uca izin kodu).
- MCP (`.mcp.json`): `context7` (kütüphane dokümanı), `postgres-salt-okunur` (`THREEK_READONLY_PG_URL`, yerel `3K_Db` salt okunur).
- Yedek `3K_Db`'ye geri yüklenince salt-okunur rolün tablo izinleri silinir: `.claude/mcp-salt-okunur-rol.sql`'i `3K_Db` düğümünden tekrar çalıştır (DBeaver'da otomatik commit açık olmalı).
- Hook'lar: `.claude/hooks/koruma.js` gizli dosyayı ve gerçek veritabanına yazan SQL'i engeller; `crlf.js` yazılan `.cs/.sql/.md` dosyalarını CRLF'e çevirir.

## Teslim

Sonunda kısaca: değişen davranış, ilgili dosyalar, koşulan testler (hangi filtreyle), şema betiği ve ön yüz bağımlılığı. Koşmadığın kontrolü geçti diye raporlama.
