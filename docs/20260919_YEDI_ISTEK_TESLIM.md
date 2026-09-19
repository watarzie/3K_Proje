# Yedi istek — uygulama ve yayın notu

19 Eylül 2026. Backend ve `C:\Users\Watarzie\Desktop\3k_onyuz` frontend birlikte güncellendi. Mevcut MediatR/katman, repository/UnitOfWork, standalone Angular ve servis/model ayrımı korundu. Paket yükseltmesi, canlıya yayın veya uygulama veritabanında veri düzeltmesi yapılmadı.

## Yapılanlar

| İstek | Sonuç ve ana dosyalar | Doğrulama |
|---|---|---|
| 1. Sunucu yetkisi | `AuthorizationBehavior`, `RequestMenuPermissionResolver`, `MenuAuthorizationExceptions`: `X-Menu-Kod` izin seçmez. Rol CRUD sabit rol-yönetimi W; gerçek proje/sandık ilişkileri, R/W ve açık AND/OR grupları kullanılır. Tanımsız istek reddedilir. Sidebar kendi oturum sahibinin menüsünü ayrı sorgudan okur. | `ServerAuthorizationRegressionTests`, `OrtakYetkiOnayIsKurallariTests`: değiştirilmiş/eksik header, başka menüde W, salt okuma, gerçek Normal/Saha/Yedek ilişkisi, public/kişisel/onay yolları. Assembly'deki bütün MediatR request'lerin tanımı taranır. |
| 2. Toplu taşıma | `SandikUrunleriTopluTasiCommandHandler`, ortak `SandikUrunTasimaIslemi`; frontend `sandik-detay` ve `sandik-toplu-tasi`: içerik kimliğiyle seçim, satır miktarı, tek hedef, tahsis/fiziksel önizleme ve tek HTTP isteği. Tek transaction, hata halinde bütün satırlar geri alınır. Anahtar defteri tekrar isteği ayırt eder. | Gerçek PostgreSQL rollback, decimal, aynı barkodlu ayrı kayıtlar, miktar/kaynak payları/aktif sayaç, kilit/saha koruması, eşzamanlılık ve tekrar; frontend tek istek/loading/anahtar testleri. |
| 3. Yedek rapor stili | `PdfService`: Yedek'e özel mor vurgu yerine rapor ailesinin mevcut mavi rengi; rapor dışı ekran rengi değiştirilmedi. | Sentetik PDF ve Excel üretildi; PDF sayfaları ve gerçek Excel baskı çıktıları görsel kontrol edildi. |
| 4. Saha/Yedek toplu eksik | `GetTopluEksikUrunlerRaporuQuery/Handler/Validator`, `PdfController`, frontend `PdfService` ve `proje-listesi`: her proje için mevcut tekil PDF/Excel, tek ZIP; gerçek tip ve proje bazlı yetki, 25 proje/100 MiB sınırı, cancellation ve güvenli adlar. | Üç türde tekil/toplu içerik eşitliği, Saha tamamlanan satırları, boş rapor davranışı, yanlış tip/eksik yetki/limit ve iptal testleri. |
| 5. Doğal sandık sırası | `Core/Common/SandikNumarasiComparer`, `PdfService`, Ambalaj rapor sıralaması: rakam parçaları sayısal tipe çevrilmeden karşılaştırılır; büyük sayılar, boşluk, sıfır öneki ve eşit anahtar bağlayıcısı güvenlidir. Özgün etiket korunur. | SND-2/SND-10, 1/2/10, baştaki sıfırlar, karma ve çok büyük numaralar; ürün miktarı ve sırası regresyonları. Yüklenmiş özgün Excel şablonunu yerinde dolduran yolun satır düzeni bilinçli olarak değiştirilmedi. |
| 6. Sandık adları | `PdfService`: Saha/Yedek tekil/toplu içerik ve eksik raporlarında mevcut `Ad/AdIngilizce`, boşsa `-`. Bölünmüş çeki satırında gerçek tahsislerin numara/adları aynı hücrede listelenir, miktar çoğaltılmaz. | Uzun/boş TR/EN adlar, çoklu tahsis, toplam miktar ve sayfa düzeni; gerçek PDF/Excel örnekleri. |
| 7. Revizyon geçmişi | `CekiRevizyonGecmisiQueries`, Core interface/modeller, `CekiRevizyonGecmisiService`, `RevizyonDosyaDeposu`, `CekiController`; frontend ayrı servis/model ve `ceki-revizyon-gecmisi` bileşeni. Proje listesinde Sevkiyatlar yanında yetkili aksiyon, sunucu sayfalama, kimlik/dosya/kullanıcı/tarih/durum/sayılar ve eski-yeni snapshot detayları. | PostgreSQL liste/proje sınırı/sayfalama/hash/dosya kontrolleri; gerçek Excel yükleme → doğrudan/onaylı A/U/D uygulama → temizlik → yeniden indirme zinciri; tekrar talep/onay, aynı adlı dosyalar ve eski eksik kayıtlar. |

Yetki eşlemesinin ayrıntısı: [SUNUCU_YETKI_ESLEMESI.md](guvenlik/SUNUCU_YETKI_ESLEMESI.md). Taşımanın korunmuş iş kuralları ve sınırları: [TOPLU_SANDIK_TASIMA.md](is-kurallari/TOPLU_SANDIK_TASIMA.md).

## Revizyon geçmişinin doğruluk sınırı

- Yeni ve mevcut `CekiRevizyonTalebi` kayıtlarının `OnizlemeJson/OnizlemeHash` snapshot'ı kullanılır; bugünkü veriden eski değer tahmin edilmez. Kullanılmayan `Revizyon` entity'sine paralel geçmiş sistemi kurulmadı.
- Onay bekleme, reddedilme, uygulama hatası, yürütülme ve uygulanma farklıdır. Yalnız onay/önizleme, uygulanmış sayılmaz. Talep ile ilişkili `Ceki` tekrar satır olarak listelenmez.
- Temizlenen DB blob'u yerine mevcut `Uploads/{projeId}/Revizyonlar` arşivi okunur. İstemci yol gönderemez; proje dizini, symlink/junction ve SHA256 kontrolleri vardır. DTO'larda fiziksel yol yoktur. Yeni gereksiz dosya/DB kopyası oluşturulmaz.
- Talebi/snapshot'ı olmayan eski revizyon açıkça eski kayıt gösterilir; eksik dosya/detay üretilmez. Sayısal eski `CreatedBy` mevcut kullanıcı adıyla çözülür; bulunamayan isim uydurulmaz.
- Yayında mevcut `Uploads` arşivi korunmalıdır. Eski sunucuya ait mutlak yollar başka sunucuya taşınmışsa dosya sınırı dışındaki kaydı indirmez; önce normal dosya taşıma/yol uyumluluğu ayrıca doğrulanmalıdır.

## Gerekli kurulum

1. Test ortamında DB yedeğini ve dosya arşivini koruyun. Backend/frontend değişikliklerini aynı sürüm olarak hazırlayın.
2. **Yeni API sürümünden önce** mevcut temel şema üzerinde [20260919_01_Sandik_Toplu_Tasima.sql](../scripts/database/20260919_01_Sandik_Toplu_Tasima.sql) çalıştırın. Bu çalışma için tek yeni şema scripti budur; yalnız `SandikTopluTasimaIslemleri` işlem anahtarı defteri ve indexlerini ekler. Mevcut miktar, tahsis, teslim veya sevkiyat verisi değiştirmez. Tekrar çalıştırılabilir.
3. Sıfır kurulumda önce sistemin güncel temel kurulumunu, ardından aynı scripti uygulayın. Eski `InitialCreate` bütün güncel modüllerin şeması değildir. Ayrıntı [database/README.md](../scripts/database/README.md) içinde. Canlı güncellemede `EnsureCreated` kullanılmaz.
4. Backend ve frontend'i yayımlayın; `Uploads` klasörünü ezmeyin. Revizyon geçmişi/rapor/yetki için yeni kolon veya izin seed'i gerekmez. DB'deki mevcut rol R/W kayıtlarını [sunucu eşlemesine](guvenlik/SUNUCU_YETKI_ESLEMESI.md) göre kontrol edin; eski header davranışı eksik izni artık telafi etmez.
5. Gerçek kullanıcılarla test ortamında Normal/Saha/Yedek okuma-yazma, salt-okuma red, toplu taşıma, ZIP ve tarihçe/dosya indirme kabul kontrollerini tamamlayın. Bu çalışma deploy veya canlı kabul testi değildir.

İşlem anahtarı defterini rutin temizlemeyin: eski ağ tekrarını tanımak için kalmalıdır. Önceki API sürümüne dönüşte ek tabloyu bırakmak güvenlidir; veri silen geri alma scripti verilmedi.

## Çalıştırılan kontroller

| Kontrol | Sonuç |
|---|---|
| Backend Debug | 1.113/1.113 başarılı; 0 atlanan |
| Backend Release + XPlat coverage | 1.113/1.113 başarılı; 0 atlanan |
| Çözüm Release build | Başarılı |
| Katalog–test eşlemesi | 501/501 referans doğrulandı; eşlenen metotların başarılı çalışması TRX ile kontrol edildi |
| Frontend ChromeHeadless | 82/82 başarılı |
| Frontend production build | Başarılı |
| Rapor örnekleri | 3 türde eksik PDF/Excel, Saha/Yedek proje sandıkları ve tek sandık PDF; doğal sıra, mavi vurgu, uzun/boş ad, çoklu tahsis görsel kontrolü |

Son yerel [Debug TRX](../artifacts/test-results/20260919-143917-6367e2c7/is-kurallari.trx), [Release TRX](../artifacts/test-results/20260919-143931-b0c3f93c/is-kurallari.trx) ve [Cobertura](../artifacts/test-results/20260919-143931-b0c3f93c/c69f6309-e8f5-4885-893c-db9be137a7be/coverage.cobertura.xml) `artifacts` altındadır; kaynak kodla commit edilmesi gerekmez. Örnek raporlar `artifacts/report-qa-20260919` altındadır, gerçek iş verisi içermez.

### Yeniden doğrulama

Gerçek PG testleri için uygulama veritabanından ayrı, yalnız localhost'ta 55439 portundaki test PostgreSQL'i gerekir. `THREEK_TEST_POSTGRES` bu sunucunun `postgres` yönetim DB'sini göstermelidir; test rolünün geçici DB oluşturma yetkisi olmalıdır. Testler kendi GUID adlı DB'lerini oluşturup kaldırır; uygulama appsettings'ini okumaz. Değişken yoksa PG vakaları açıkça atlanır ve sıfır-atlama kuralını uygulayan tam runner başarısız olur. Üretim bağlantısını bu değişkene vermeyin.

```powershell
# Yalnız ayrılmış test sunucusu ve test hesabıyla:
$env:THREEK_TEST_POSTGRES = 'Host=127.0.0.1;Port=55439;Username=TEST_KULLANICISI;Database=postgres'
./scripts/test/Invoke-IsKurallariTests.ps1 -Configuration Debug -NoRestore
./scripts/test/Invoke-IsKurallariTests.ps1 -Configuration Release -Coverage -NoRestore
dotnet build 3K_Proje.slnx --configuration Release --no-restore
```

Frontend deposunda `npm test -- --watch=false --browsers=ChromeHeadless` ve `npm run build` çalıştırılır. Mevcut Bootstrap/Sass deprecation ve üçüncü taraf global script `axisIndex` uyarıları kaldı; paketler yükseltilmedi. Backend'in değişmeyen Grid/hareket/stok/sandık kodlarındaki nullable uyarıları bu görevde bastırılmadı.

## Açıkça doğrulanmayan sınırlar

- Yetki testleri gerçek `HttpContext` başlıklarıyla MediatR davranışını çalıştırır; yayımlanmış HTTP/JWT middleware ve canlı rol kayıtlarıyla uçtan uca oturum testi değildir.
- PG entegrasyonu sentetik, güncel EF şemasıyla ayrılmış sunucuda yapıldı. Canlıya özgü trigger/veri anomalisinin tamamını veya yük altında çok kullanıcılı işletimi kanıtlamaz. Toplu taşımanın saha servisi bağımlılığı kontrollü test bağımlılığıdır; mevcut saha regresyonları ayrıca tam pakette çalıştı.
- Revizyon yaşam döngüsü gerçek servis/handler/parser/onay/transaction yolunu kullanır; SSE ve bildirim dış taşıması testte no-op'tur.
- Excel'in 18 kolonlu raporu mevcut ayarlarıyla yatay baskı sayfalarına bölünür; önceki kodda da FitToPages yoktu. Yeni bir baskı tasarımı eklenmedi. Uzun sandık adları mevcut hücre sınırları içinde satıra sarılır.
- Testlerin geçmesi ve 501 madde eşlemesi, bütün iş kurallarının %100 davranış kapsamı veya sıfır regresyon garantisi değildir. Gerçek kullanıcılarla ekran kabulü ayrıca yapılmalıdır.
