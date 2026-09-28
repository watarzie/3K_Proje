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
| `3K_API/Program.cs`, `3K_API/Extensions` | Kompozisyon kökü; `AddApi` HTTP kayıtlarını, `UseApiPipeline` middleware sırasını tutar |
| `3K.Application/DependencyInjection.cs`, `3K.Infrastructure/DependencyInjection.cs` | Katmanın kendi servis, pipeline ve arka plan işi kayıtları |
| `3K.Application/Features/<Alan>` | Command, Query, Handler, DTO ve Validator |
| `3K.Application/Behaviors` | Merkezi exception, authorization, validation ve approval adımları (bu sırayla) |
| `3K.Application/Common` | Uygulama düzeyindeki ortak sonuç ve politika yardımcıları |
| `3K.Core` | Entity, enum, interface, domain modelleri ve ortak kurallar |
| `3K.Infrastructure` | EF Core, repository/UoW, servis uygulamaları, dosya/Excel/PDF ve altyapı |
| `3K.Application.Tests` | xUnit iş kuralı ve regresyon testleri |

- Bağımlılık yönünü koru: API → Application/Infrastructure/Core; Application → Core; Infrastructure → Core. Core'a EF, HTTP veya UI bağımlılığı ekleme. Application'a doğrudan `AppDbContext`/Infrastructure referansı ekleme.
- Application sorgularında mevcut repository ve `IReadQueryExecutor` soyutlamalarını kullan. Infrastructure'daki büyük servislerde mevcut iş mantığı bulunduğunu dikkate al; görev dışı toplu katman taşıması yapma.
- Controller içine miktar, stok, tahsis veya onay iş kuralı yazma. Yeni kullanım senaryosunu ilgili feature'a yerleştir; tekrar eden kuralı ortak servis/politikaya çıkar.
- DI kayıtlarını ve gerçek çağrıları tamamla; kullanılmayan soyutlama üretme. Ayrı worker, cache ürünü veya framework eklemeyi bu dosya gerektirmez.
- Finans modülü alanlara bölünmüştür (Panel, IsKaydi, SiparisFatura, Gider, Tanim, Belge, Rapor): `FinansControllerBase` türevi controller'lar, `Finans<Alan>Commands/Queries` ve handler'ları, validator'lar ve `Finans<Alan>Modelleri` aynı adları izler. Her `IFinans*Service` arayüzünün `3K.Infrastructure/Services/Finans` altında internal bir uygulaması vardır. Servisler birbirini çağırmaz; ortak davranış `FinansIslemYurutucu`, `FinansDenetimKaydi`, `FinansFiyatBelirleyici`, `FinansSorgulari` ve `FinansModelEslemeleri` üzerinden paylaşılır.
- Finans iş kuralları Core'dadır (`FinansTutarKurallari`, `FinansMiktarKurallari`, `FinansTarihKurallari`, `FinansGerekceKurali`, `FinansParaBirimiKurali`); validator ve servis aynı kuralı çağırır. Kuralın SQL karşılığı gerekiyorsa `FinansSorgulari` içinde tek ifade olarak tanımla (ör. `IsNetIfadesi`) ve domain kuralıyla eşitliğini testle sabitle.
- İş varsayılanı gerektiren ekran istekleri Application `DTOs` altında durur ve servis modeline kendi yöntemleriyle dönüşür (ör. `FinansGiderKaydetDto.GiderModeli()`); controller yalnız bağlar ve iletir.

## Kod ve API sözleşmesi

- Değiştirilen dosyanın namespace, girinti, parantez ve isimlendirme stilini koru. Mevcut Türkçe domain terimlerini, `_3K` namespace'lerini ve DTO alanlarını sebepsiz İngilizceleştirme.
- Türkçe karakterleri ve dosyanın geçerli UTF-8 içeriğini koru; bozuk encoding üretme. Açıklamalar kararın nedenini anlatsın.
- Mevcut `Result`/`Result<T>`, FluentValidation ve `ResultExtensions.ToActionResult()` sözleşmesini izle. Normal başarılı JSON çoğunlukla ham DTO'dur; tüm API'yi yeni bir wrapper'a taşıma.
- `202` onay kuyruğuna alındığını ifade eder; işlem uygulanmış değildir. `401`, `403`, `404`, `409` ve yapılandırılmış hata ayrımını koru. Kullanıcıya iç exception, secret veya fiziksel dosya yolu döndürme. Hata gövdesi her yerde `{ message, issues? }` biçimindedir.
- Handler beklenen reddi `Result.Failure` ile döner. Result dönemeyen derin servisler kullanıcıya gösterilebilir reddi `IsKuraliException`/`KayitBulunamadiException` ile bildirir; pipeline bunları HTTP koduna çevirir. `InvalidOperationException` mesajı kullanıcıya taşınmaz.
- Dosya indiren kullanım senaryosu `Result<DosyaIcerigi>` döner ve dosya adını kendisi üretir; controller yalnız `ToFileResult()` çağırır, veri erişimi yapmaz.
- Destekleyen çağrılara `CancellationToken` ilet. Async akışlarda `.Result`/`.Wait()` kullanma. Aynı scoped DbContext üzerinde sorgu/yazmaları `Task.WhenAll` ile paralelleştirme.
- Operasyon tarihleri ile JWT/2FA sürelerinin zaman dilimi sözleşmelerini ayrı koru; ilgili akışın `TurkeyTime`/UTC kullanımını incelemeden genel tarih dönüşümü yapma. Kaydedilen ve kullanıcıya gösterilen operasyon/rapor zamanları `TurkeyTime.Now` ile alınır; `DateTime.Now` sunucunun saat dilimine bağlıdır, kullanma.
- Büyük listelerde filtre/sayfalama/projection'ı mümkün olduğunca veritabanında yap; yalnız tek kayıt aramak için tüm tabloyu belleğe alma.

## Yetki ve onay

- Yetki kodları yalnız `3K.Core/Constants/YetkiKodlari` içinde tanımlıdır; kod literali yazma. Proje tipine bağlı kodları `ProjeTipiYetkiKodlari.Getir(tip)` ile çöz.
- İşlem iznini sunucuda tanımla: request ya `IRequiresMenuPermission` ile `RequiredMenuKod` ve açık `RequiredYetkiTipi` (R görüntüleme, W işlem) bildirir ya da politikası `RequestMenuPermissionResolver` içindedir. İstemcinin gönderdiği menü, route veya body değeri yetki kanıtı değildir; ekran bağlamı başlığı (`X-Menu-Kod`) kullanılmaz.
- İzin modeli tektir: ekran, işlem ve alan kodlarının hepsi `MenuTanimlari` ağacında N/R/W olarak `YetkiAgaci` ile değerlendirilir. Üst düğüm alt düğümün tavanıdır; üst W alt izni kendiliğinden vermez. Kişisel ret/izin rol seviyesinin önüne geçer ama üst tavanı aşamaz. Modüle özel değerlendirici veya ad bazlı istisna yazma; Admin rolü kimliğiyle (`SistemRolleri.AdminId`) tanınır.
- Yeni kod eklerken `YetkiKodlari`, `MenuTanimlari` kaydını ekleyen `scripts/database/` betiği (`LabelKey` = `MENU.<KOD>`), frontend `yetki-kodlari.ts` ve TR/EN `MENU.*` çevirilerini birlikte güncelle.
- Ortak endpoint'te gerçek proje tipini, sahipliği/ilişkileri ve gereken izni kayıtlar üzerinden doğrula. Okuma/yazma ayrımını ve izin kombinasyonlarını açık tut.
- Ambalaj komutlarında kayda bağlı ek izinler (durum geçişi, form sonrası seçim, dahil/haric kararı, kritik veri, iptal, tamamlanmış projeye ekleme), ilk form şartı ve kritik işlem gerekçesi `AmbalajIslemKurallari` + `AmbalajKritikIslemDenetimi` ile handler içinde, değişiklik öncesi kayıt üzerinden ve aynı `ExecuteAtomicAsync` transaction'ında denetlenir. Ölçü, m³ ve sarf alanları yanıt dönmeden `AmbalajAlanMaskesi`/`DtoyuMaskele` ile açıkça maskelenir; yeni ambalaj DTO'su eklerken maskeyi ve maske eşdeğerlik testini güncelle. Namespace'e göre davranış seçme veya reflection ile maskeleme yapma.
- Finans alan izinleri (parasal veri, birim fiyat, tutar, gelir, gider, kârlılık, ölçü, m³, sarf) yalnız HTTP sınırında `FinansAlanErisimFiltresi` + `FinansAlanProjeksiyonu` ile uygulanır; izinsiz alan `null` döner, sıfır değil. Servis ve handler ham modeli döndürür. Rapor ve belge indirme gibi dosya yanıtları bütün alan izinlerini gerektirir; frontend kapıları aynı kuralı `finans-izinleri.ts` üzerinden yansıtır.
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

- Bir işteki miktar/stok/tahsis/transfer değişiklikleri birlikte başarılı olmalıdır. "Ya hepsi ya hiçbiri" akışında `ExecuteAtomicAsync` kullan: delegate başarısız `Result` dönerse bütün değişiklikler geri alınır ve aynı sonuç döner; iç içe çağrıda dış transaction da geri alınır. Birkaç `SaveChanges` çağrısını tek transaction sanma.
- Finans yazmaları `FinansIslemYurutucu.AtomikAsync` ile Serializable transaction'da çalışır ve dışarıda açılmış transaction'a katılır. Değişiklik geçmişi aynı transaction'da `FinansDenetimKaydi` ile yazılır; aynı kapsamdaki kayıtlar tek işlem grubunu paylaşır. Benzersizlik ve serileştirme çakışmaları 409 iş kuralı hatasına çevrilir.
- `IUnitOfWork.ExecuteInTransactionAsync` delegate normal döndüğünde commit eder; yalnız bilinçli kısmi başarı akışlarında (ör. atlanan satırları raporlayan toplu 3K komutları) kullan. Kendi "exception fırlatıp rollback yaptır" sınıfını yazma.
- `xmin` concurrency, idempotency anahtarı, unique constraint ve hareket geçmişini koru. Aynı anahtar + aynı işlem tekrarı çift kayıt üretmemeli; farklı payload çakışma olarak ele alınmalı.
- Dosya ve bildirim yan etkilerini transaction sınırıyla uyumlu yürüt; mevcut commit/rollback callback'lerini değerlendir. Kullanıcı dosya adını güvenilmez kabul et; güvenli sunucu adı ve hedef dizin kontrolü kullan.
- EF migration kullanılmaz. Şema/veri değişikliğinde EF model/configuration ile `scripts/database/` altındaki idempotent, transaction içindeki PostgreSQL betiğini birlikte güncelle; trigger/seed bağımlılıklarını da kontrol et. Betik ayrı dağıtım adımıdır, uygulama açılışında çalışmaz.
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
- Finans servislerini testte `FinansTestServisleri` ile üretim DI kaydından kur. EF eşlemesini değiştiren yeniden düzenlemede veritabanı oluşturma betiğinin değişmediğini karşılaştır.
- Bellek içi testleri PostgreSQL transaction/trigger/FK/concurrency veya HTTP authorization kanıtı sayma. Bu sınırlar değiştiğinde izole entegrasyon doğrulaması yap; yapılamazsa eksikliği açıkça belirt.
- Testleri geçsin diye devre dışı bırakma veya beklentiyi hatalı davranışa uydurma. Eski rapordaki başarı sayılarını yeni koşum sonucu olarak sunma.

## Teslim ve bakım

Sonuçta değişen davranışı, ilgili dosyaları, çalıştırılan kontrolleri ve varsa şema/diğer depo bağımlılığını kısa anlat. Test/SDK/erişim engelini açıkça belirt. Kod oluşturulmasını deployment veya canlı doğrulama olarak sunma. Mimari/komutlar değişirse bu dosyanın ilgili bölümünü güncelle; buraya geçici görev listesi veya oturum dökümü ekleme.
