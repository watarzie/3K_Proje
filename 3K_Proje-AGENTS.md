# AGENTS.md — 3K_Proje Backend

Bu dosya depo kökünde `AGENTS.md` olarak bulunmalıdır. Kalıcı geliştirme kurallarını tanımlar; özellik backlog'u değildir. Güncel görev ve varsa ilgili alt dizin yönergeleriyle birlikte kullan. Kullanıcıyla Türkçe iletişim kur.

## Çalışma biçimi

- Önce `git status --short` ile çalışma durumunu, sonra ilgili controller → request → handler → servis/repository → model → test zincirini incele. Dosya ve metin aramada `rg` kullan.
- Mevcut kullanıcı değişikliklerini koru. İstenen davranışı tamamlayan en küçük, tutarlı değişikliği yap; ilgisiz yeniden adlandırma, toplu formatlama veya paket yükseltmesi ekleme.
- Sınıf adından veya eski yorumdan davranış tahmin etme. Gerçek çağrı yolunu, güncel modeli ve ilgili testleri kontrol et. Belgede bilinen hata olarak anlatılan davranışı yeni doğru kural haline getirme.
- Rutin teknik kararları mevcut örüntüyle çöz. Görev gerektirdiğinde API, DTO, şema, frontend sözleşmesi ve testleri birlikte ele al; eksik kalan bağımlılığı açıkça bildir.

## Mimari ve dosya yerleşimi

Uygulama .NET 10 hedefleyen, PostgreSQL kullanan katmanlı bir monolittir. Komut/sorgu ayrımı MediatR ile sağlanır; ortak veritabanı kullanılır. Gerçek paket sürümlerini `.csproj` dosyalarından doğrula.

| Yer | Sorumluluk |
|---|---|
| `3K_API/Controllers` | HTTP giriş/çıkışı, request bağlama, MediatR çağrısı |
| `3K_API/Program.cs` | DI, middleware, authentication ve worker kayıtları |
| `3K.Application/Features/<Alan>` | Command, Query, Handler, DTO ve Validator |
| `3K.Application/Behaviors` | Merkezi exception, authorization, approval, validation ve cache adımları |
| `3K.Application/Common` | Uygulama düzeyindeki ortak sonuç ve politika yardımcıları |
| `3K.Core` | Entity, enum, interface, domain modelleri ve ortak kurallar |
| `3K.Infrastructure` | EF Core, repository/UoW, servis uygulamaları, dosya/Excel/PDF ve altyapı |
| `3K.Application.Tests` | xUnit iş kuralı ve regresyon testleri |

- Bağımlılık yönünü koru: API → Application/Infrastructure/Core; Application → Core; Infrastructure → Core. Core'a EF, HTTP veya UI bağımlılığı ekleme. Application'a doğrudan `AppDbContext`/Infrastructure referansı ekleme.
- Application sorgularında mevcut repository ve `IReadQueryExecutor` soyutlamalarını kullan. Infrastructure'daki büyük servislerde mevcut iş mantığı bulunduğunu dikkate al; görev dışı toplu katman taşıması yapma.
- Controller içine miktar, stok, tahsis veya onay iş kuralı yazma. Yeni kullanım senaryosunu ilgili feature'a yerleştir; tekrar eden kuralı ortak servis/politikaya çıkar.
- DI kayıtlarını ve gerçek çağrıları tamamla; kullanılmayan soyutlama üretme. Ayrı worker, cache ürünü veya framework eklemeyi bu dosya gerektirmez.

## Kod ve API sözleşmesi

- Değiştirilen dosyanın namespace, girinti, parantez ve isimlendirme stilini koru. Mevcut Türkçe domain terimlerini, `_3K` namespace'lerini ve DTO alanlarını sebepsiz İngilizceleştirme.
- Türkçe karakterleri ve dosyanın geçerli UTF-8 içeriğini koru; bozuk encoding üretme. Açıklamalar kararın nedenini anlatsın.
- Mevcut `Result`/`Result<T>`, FluentValidation ve `ResultExtensions.ToActionResult()` sözleşmesini izle. Normal başarılı JSON çoğunlukla ham DTO'dur; tüm API'yi yeni bir wrapper'a taşıma.
- `202` onay kuyruğuna alındığını ifade eder; işlem uygulanmış değildir. `401`, `403`, `404`, `409` ve yapılandırılmış hata ayrımını koru. Kullanıcıya iç exception, secret veya fiziksel dosya yolu döndürme.
- Destekleyen çağrılara `CancellationToken` ilet. Async akışlarda `.Result`/`.Wait()` kullanma. Aynı scoped DbContext üzerinde sorgu/yazmaları `Task.WhenAll` ile paralelleştirme.
- Operasyon tarihleri ile JWT/2FA sürelerinin zaman dilimi sözleşmelerini ayrı koru; ilgili akışın `TurkeyTime`/UTC kullanımını incelemeden genel tarih dönüşümü yapma.
- Büyük listelerde filtre/sayfalama/projection'ı mümkün olduğunca veritabanında yap; yalnız tek kayıt aramak için tüm tabloyu belleğe alma.

## Yetki ve onay

- İşlem iznini sunucuda tanımla; `X-Menu-Kod`, route veya body değeri tek başına yetki kanıtı değildir. Mevcut `ISecuredRequest` ve sabit/çoklu menü gereksinimlerini uygun şekilde kullan.
- Ortak endpoint'te gerçek proje tipini, sahipliği/ilişkileri ve gereken izni kayıtlar üzerinden doğrula. Okuma/yazma ayrımını ve izin kombinasyonlarını açık tut. Eski header fallback davranışını yeni güvenlik örüntüsü olarak kopyalama.
- Login/2FA challenge ile tam oturumu ayır. Yetki eklemek için public giriş akışını yanlışlıkla kilitleme; mevcut servis kullanıcısı/onay yürütme bağlamlarını incele.
- Approval pipeline, kayıtlı komut bütünlüğü, onay yetkisi ve uygulama durumu kontrollerini atlama. Talep, karar ve fiili uygulama ayrı olaylardır; tekrar istek ikinci kez uygulamamalıdır.

## Korunacak domain kuralları

- `Proje → Ceki → CekiSatiri` ve `SandikIcerik` tahsis ilişkilerini koru. Barkod tekil satır kimliği değildir; bölünmüş satırlarda ilgili `SandikIcerikId` ile çalış.
- `IstenenAdet`, `TahsisMiktari` ve `KonulanAdet` farklıdır. Plan değişikliği fiziksel teslim üretmemeli. Miktarlarda `decimal` ve mevcut hassasiyet/constraint kurallarını koru.
- Aktif Grid sevk partisi sayaçlarını kümülatif 3K tesliminden ayır. Yeni partide eski fiziksel teslimi veya başka sandığın karşılamasını sıfırlama.
- Stok/proje/tedarikçi karşılamaları, geri gönderim ve başka projeye çıkan miktarları mevcut politikalarla hesapla. `EksikMiktar`, `KalanMiktar` ve `GridEksikMiktar` birbirinin yerine geçmez. Legacy `null` sayaçları sebepsiz sıfıra dönüştürme.
- Kalite, Grid iptal/kapalı durumları, sevkiyat kilidi/düzeltme kapsamı ve aktif saha kaynak korumalarını koru. Sandık kapatma, Grid sevki ve nihai sevkiyat aynı olay değildir.
- Tekil/toplu/legacy endpoint'lerin aynı işi yapan yollarını birlikte kontrol et; bir yolu düzeltirken diğerinde farklı kural bırakma.
- Revizyonda dosya hash'i, değişmez önizleme, uygulama kimliği, kaynak/saha bağlantıları ve tarihsel iz korunmalı. Geçmişi güncel veriden yeniden tahmin etme.
- Ambalaj ile finansın uygunluk, miktar ve fiyat geçmişi politikalarını ilgili servislerden doğrula; üretim tamamlandı varsayımıyla finansal kayıt üretme veya silme.

## Transaction, veri ve dosyalar

- Bir işteki miktar/stok/tahsis/transfer değişiklikleri birlikte başarılı olmalıdır. Uygun yerde mevcut `IUnitOfWork.ExecuteInTransactionAsync` kullan; birkaç `SaveChanges` çağrısını tek transaction sanma.
- Bu yardımcı delegate normal dönerse commit eder: yazmadan sonra `Result.Failure` döndürmek otomatik rollback değildir. Red kontrollerini yazmadan önce tamamla veya mevcut exception/rollback yolunu doğru uygula.
- `xmin` concurrency, idempotency anahtarı, unique constraint ve hareket geçmişini koru. Aynı anahtar + aynı işlem tekrarı çift kayıt üretmemeli; farklı payload çakışma olarak ele alınmalı.
- Dosya ve bildirim yan etkilerini transaction sınırıyla uyumlu yürüt; mevcut commit/rollback callback'lerini değerlendir. Kullanıcı dosya adını güvenilmez kabul et; güvenli sunucu adı ve hedef dizin kontrolü kullan.
- Şema değişikliğinde model/configuration ile sürümlü migration/SQL ve veri geçişini birlikte ele al. Mevcut migration'ın canlı şemayı tamamen temsil ettiğini varsayma; trigger/seed bağımlılıklarını da kontrol et. Uyarı bastırmak şema düzeltmesi değildir.
- Secret, connection string, JWT anahtarı, TOTP secret/kurtarma kodu veya gerçek kullanıcı verisini koda/loga ekleme. Test için üretim ayarlarıyla API/worker başlatma veya canlı veriyi değiştirme.
- Rapor değişikliğinde mevcut ortak stil, doğru sandık kimliği/adı, doğal numara sırası ve tekil/toplu PDF/Excel içerik tutarlılığını koru; örnek çıktıyı kontrol et.

## Doğrulama

Komutları depo kökünde çalıştır. .NET 10 SDK gerekir. Kapsamlı iş kuralı değişikliklerinden önce `docs/is-kurallari/GRID_3K_IS_KURALLARI.md` içindeki ilgili bölümü ve `docs/is-kurallari/TEST_KAPSAMI.md` dosyasını oku.

```bash
dotnet restore 3K_Proje.slnx
dotnet build 3K_Proje.slnx --configuration Release --no-restore
dotnet test 3K.Application.Tests/3K.Application.Tests.csproj --configuration Release
```

İş kuralı düzeltmelerinde belgedeki Debug/Release ve kural eşlemesi kapılarını uygula; PowerShell varsa mevcut runner'ı kullan:

```powershell
./scripts/test/Invoke-IsKurallariTests.ps1 -Configuration Debug
./scripts/test/Invoke-IsKurallariTests.ps1 -Configuration Release -Coverage
```

- Önce etkilenen senaryoyla hızlı doğrulama yap; iş kuralı değişikliğinde gereken tam kapıyı bunun yerine sayma. Sadece metin/yorum değişikliği için tüm sistemi çalıştırma.
- Hata düzeltmesinde gerçek işlem sırasını ve red halinde değişmeyen veriyi test et. Gerektiğinde tekil/toplu, çoklu tahsis, ondalık, legacy null, saha/sevk ve tekrar istek senaryolarını kapsa. İlgili kural–test eşlemesini güncelle.
- Bellek içi testleri PostgreSQL transaction/trigger/FK/concurrency veya HTTP authorization kanıtı sayma. Bu sınırlar değiştiğinde izole entegrasyon doğrulaması yap; yapılamazsa eksikliği açıkça belirt.
- Testleri geçsin diye devre dışı bırakma veya beklentiyi hatalı davranışa uydurma. Eski rapordaki başarı sayılarını yeni koşum sonucu olarak sunma.

## Teslim ve bakım

Sonuçta değişen davranışı, ilgili dosyaları, çalıştırılan kontrolleri ve varsa şema/diğer depo bağımlılığını kısa anlat. Test/SDK/erişim engelini açıkça belirt. Kod oluşturulmasını deployment veya canlı doğrulama olarak sunma. Mimari/komutlar değişirse bu dosyanın ilgili bölümünü güncelle; buraya geçici görev listesi veya oturum dökümü ekleme.
