# CLAUDE.md — 3K_Proje (Backend)

3K platformunun .NET 10 + PostgreSQL arka ucu. Ön yüz ayrı depodadır: `C:\Users\Watarzie\Desktop\3k_onyuz` (Angular 20).
Depodaki kalıcı mühendislik kurallarının kaynağı bu dosya ve `.claude/rules/` altındaki konu dosyalarıdır (bkz. "Kural dosyaları").
İş kuralı değişikliğinden önce `docs/is-kurallari/GRID_3K_IS_KURALLARI.md` ve `docs/is-kurallari/TEST_KAPSAMI.md` okunur.

Kullanıcıyla Türkçe konuşulur. Kod içindeki Türkçe alan adları (`Proje`, `Ceki`, `Sandik`, `Tahsis`, `Sevk`, `Onay`, `Finans`, `Ambalaj`…) korunur; İngilizceye çevrilmez.

## Çalışma kuralları (önce bunlar)

- **Kullanıcı açıkça onay vermeden asla commit atma**; `git add` ile stage etme, push yapma. Sor: "Commit edebilir miyim?"
- Commit mesajları Türkçe, conventional biçimde: `refactor(finans): ...`, `fix(rapor): ...`, `feat(db): ...` (yalnız kullanıcı onay verdiğinde).
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
| `3K_API` | Controller'lar, `Extensions/` (`AddApi`, `UseApiPipeline`, `ResultExtensions`, hız sınırları `HizSiniriBolumleri`), middleware, Serilog, Ambalaj kaynak senkronizasyon worker'ı, dışa aktarım sınırlayıcısı (`Services/DisaAktarimHizSiniri`); composition root | Application + Infrastructure + Core |
| `3K.Application.Tests` | xUnit birim, regresyon ve PostgreSQL entegrasyon testleri (namespace `_3K.Application.Tests`) | Hepsine referans verir |

- Bağımlılık yönü içe doğrudur: API → Application/Infrastructure → Core. Core'a EF/HTTP eklenmez; Application `AppDbContext`'e veya Infrastructure'a referans vermez.
- DI kayıtları: `3K.Application/DependencyInjection.cs` (`AddApplication`), `3K.Infrastructure/DependencyInjection.cs` (`AddInfrastructure`, finans için `AddFinansServices`), `3K_API/Extensions/ApiServiceCollectionExtensions.cs` (`AddApi`).
- Özellik akışı: **Controller → Command/Query → (Authorization → Validation → Approval) → Handler → Servis/Repository → Core modeli/kuralı** (ayrıntı `.claude/rules/ozellik-yazimi.md`).
- MediatR pipeline sırası (dıştan içe): `IsHatasiLoglamaBehavior` → `UnhandledExceptionBehavior` → `AuthorizationBehavior` → `ValidationBehavior` → `DisaAktarimHizSiniriBehavior` → `ApprovalBehavior` → handler.
- **202 = onay kuyruğuna alındı, uygulanmadı.** Hata gövdesi her yerde `{ message, issues? }`; tek takip kodu yanıtın `X-Correlation-ID` başlığıdır.
- Yerel API: `http://localhost:50447` (`3K_API/Properties/launchSettings.json`); ön yüz bu adrese bağlanır.

## Kural dosyaları (`.claude/rules/`)

Yol sınırlı dosyalar Claude o yollardaki bir dosyayla çalışırken yüklenir; işe başlamadan önce (planlama, alt ajan) ilgili
dosyayı açıkça oku.

| Dosya | Konu | Yüklendiği yer |
|---|---|---|
| `yetki-modeli.md` | `YetkiKodlari`, `MenuTanimlari` N/R/W, rol atama, Ambalaj maskesi | Her zaman |
| `on-yuz-sozlesmesi.md` | Sözleşme değişince `3k_onyuz` aynası (endpoint, DTO, enum, izin, çeviri, 202, sayfalama) | Her zaman |
| `ozellik-yazimi.md` | Command/Query, Handler, Validator, Controller, DTO; dışa aktarım sınırı ve hata loglama | `3K.Application/**`, `3K_API/**`, `3K.Core/Models/**`, `3K.Core/Interfaces/**` |
| `finans.md` | `FinansQuery/Command` + `Sekmeler`, `FinansHandlerHelper`, `FinansIslemYurutucu`, sekme izni, para birimi | `**/*Finans*`, `FinansIslemleri/**`, `Services/Finans/**` |
| `veri-erisimi.md` | `AppDbContext`, `GenericRepository` izleme semantiği, `UnitOfWork`, sunucu sayfalaması | `3K.Infrastructure/**`, `3K.Application/**` |
| `sema-betikleri.md` | EF migration yok; idempotent betik, README, sıfırlama, yıkıcı onay kalıbı | `scripts/database/**`, `3K.Core/Entities/**`, `3K.Infrastructure/Data/**`, `YetkiKodlari.cs` |
| `test.md` | xUnit, `FinansTestServisleri`, komutlar, PostgreSQL entegrasyon testleri | `3K.Application.Tests/**`, `scripts/test/**` |
| `kod-stili.md` | Stil, CRLF, yorum, etiketler, kullanıcı mesajı, `TurkeyTime` | `**/*.cs`, `scripts/**/*.sql` |

Bir kural değişince ilgili konu dosyasını güncelle; aynı kuralı iki dosyaya yazma.

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

## Test

- Tek test projesi `3K.Application.Tests` (xUnit). **Release konfigürasyonunda** ve **yalnız değişen alanın** filtresiyle koş (`dotnet test 3K.Application.Tests/3K.Application.Tests.csproj -c Release --filter "FullyQualifiedName~<Sinif>"`); tam paketi yalnız kullanıcı isterse. Raporda hangi alt kümenin koştuğunu söyle.
- PostgreSQL entegrasyon testleri `THREEK_TEST_POSTGRES` yoksa atlanır ve yalnız izole sunucuyu (port 55439) gösterir; `postgres-test` skill'i ile koş. Ayrıntı `.claude/rules/test.md`.
- Test devre dışı bırakma veya beklentiyi yanlış davranışa uydurma yok. Koşmadığın kontrolü geçti diye raporlama.

## Çalıştırma

- API: `dotnet run --project 3K_API` (profil `3K_API`: `http://localhost:50447`, `https://localhost:50446`). Test için canlı ayarlarla başlatılmaz.
- Ön yüz: `C:\Users\Watarzie\Desktop\3k_onyuz` içinde `npm start` (`ng serve`, port 4200); Claude Code'da bu deponun `.claude/launch.json` dosyasındaki `onyuz` yapılandırması.

## Dokunulmayacak yerler

- Üretilmiş veya yerel klasörler: `publish/`, `artifacts/`, `.codex-build/`, `.tmp/`, `.vs/`, `bin/`, `obj/`, `.claude/worktrees/` ve git dışı canlıya alım kopyaları (`canliya-alim-*/`; betiğin aslı `scripts/database` altındadır, kopyayı değil aslını düzenle).

## Claude Code araçları

Yapı: `CLAUDE.md` (bu dosya) · `.mcp.json` · `.claude/settings.json` (izinler, hook'lar; kişisel `settings.local.json` git dışı) ·
`.claude/rules/` (konu kuralları) · `.claude/commands/` (iş akışı komutları) · `.claude/skills/` · `.claude/agents/` · `.claude/hooks/`.

- **Komutlar** (`.claude/commands`, `/ad` ile çağrılır): `/ozellik <istek>` (planla → onay → geliştir → test → gözden geçir), `/gozden-gecir` (değişen dosyalara göre denetçileri paralel çalıştırıp tek rapor), `/hata-duzelt <belirti>` (teşhis → onay → önce kırmızı test → en küçük düzeltme), `/canliya-hazirlik` (şema, güvenlik ve doküman denetimi).
- **Ajanlar** (`.claude/agents`). Ön yüz ajanları kuralları `3k_onyuz/CLAUDE.md` ve `3k_onyuz-AGENTS.md`'den okur.
  - Planlama: `ozellik-planlayici` (yeni özellik/belirsiz istek; şartname + iki depo planı, salt okunur).
  - Geliştirme: `arka-uc-gelistirici` (.NET kodu, şema betiği, hata düzeltme), `on-yuz-gelistirici` (Angular kodu), `test-yazici` (yalnız xUnit testleri ve spec'ler), `refactor-uzmani` (davranış değiştirmeyen yeniden düzenleme).
  - Denetim (salt okunur): `kod-gozden-gecirici` (arka uç, commit önermeden önce), `on-yuz-gozden-gecirici` (ön yüz), `sozlesme-denetcisi` (arka uç ↔ ön yüz sözleşmesi), `guvenlik-denetcisi` (yetki, giriş/2FA, girdi, dosya, gizli değer, bağımlılık), `performans-analisti` (sorgu, sayfalama, paket boyutu), `sema-betik-denetcisi` (EF modeli/betik/izin kodu değişince ve canlıya alımdan önce), `finans-maske-denetcisi` (Ambalaj DTO'su veya Finans isteği değişince).
  - Destek: `canli-hata-teshisci` (canlı/yerel hata ve kırık test teşhisi, düzeltmeden önce, salt okunur), `is-kurali-dokumantasyon` (davranış değişince iş kuralı ve güvenlik dokümanlarını hizalama).
- Beceriler: `postgres-test` (izole PostgreSQL ile test), `yeni-yetki-kodu` (uçtan uca izin kodu).
- MCP (`.mcp.json`): `context7` (kütüphane dokümanı), `postgres-salt-okunur` (`THREEK_READONLY_PG_URL`, yerel geliştirme `3K_Db` salt okunur), `postgres-canli-kopya` (`THREEK_CANLI_KOPYA_PG_URL`, canlı yedeğinin kullanıcı tarafından geri yüklendiği yerel `3K_Db_Canli`, salt okunur). Canlı hata bildiriminde veri `3K_Db_Canli`'den okunur; `3K_Db` geliştirme veritabanıdır, canlı yedeğiyle ezilmez. Canlı kopyadaki kişisel veri rapora/çıktıya yazılmaz.
- Yedek bir veritabanına geri yüklenince o veritabanında salt-okunur rolün tablo izinleri silinir: `.claude/mcp-salt-okunur-rol.sql`'i o veritabanının düğümünden (`3K_Db` ya da `3K_Db_Canli`) tekrar çalıştır (DBeaver'da otomatik commit açık olmalı).
- Hook'lar: `.claude/hooks/koruma.js` gizli dosyayı ve gerçek veritabanına yazan SQL'i engeller; `crlf.js` yazılan `.cs/.sql/.md` dosyalarını CRLF'e çevirir.

## Teslim

Sonunda kısaca: değişen davranış, ilgili dosyalar, koşulan testler (hangi filtreyle), şema betiği ve ön yüz bağımlılığı. Koşmadığın kontrolü geçti diye raporlama.
