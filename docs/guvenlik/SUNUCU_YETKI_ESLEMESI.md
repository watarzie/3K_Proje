# Sunucu işlem / menü yetkisi eşlemesi

19 Eylül 2026 değişikliği: `AuthorizationBehavior`, `X-Menu-Kod` değerini yetki kararında kullanmaz. İzin kayıtları hâlâ veritabanındaki `RolYetkileri` üzerinden `IRolService` ile doğrulanır; yeni rol ID'si veya Admin bypass eklenmedi. İstemci menü başlığını değiştirmek, kaldırmak veya başka ekranda W sahibi olmak farklı bir operasyonu açmaz.

Çalıştırılabilir katalog `3K.Application/Common/RequestMenuPermissionResolver.cs` dosyasındadır. Tanımlanmamış `ISecuredRequest` 403 ile kapanır; `ISecuredRequest` eklemeyi unutmak da yeni bir operasyonu public yapmaz. `ButunKorumaliOperasyonlar_SunucudaTanimlidir` testi assembly'deki **bütün somut MediatR request tiplerini** tarar; sunucu tanımı veya açık istisna kategorisi olmayan yeni request testte başarısız olur.

## Sabit işlemler

| Operasyon | Gerekli izin |
|---|---|
| Rol oluştur / güncelle / sil | rol-yonetimi W |
| Rol detay | rol-yonetimi R |
| Rol seçim listesi | rol-yonetimi **veya** kullanicilar **veya** onay-kurallari-yonet R |
| Dashboard sorguları | dashboard R |
| Grid / 3K iş listeleri | grid-is-listesi / 3k-is-listesi R |
| Çeki ana veri düzenle / satır sil | ceki-verisi-duzenle / ceki-verisi-sil W |
| Kalite / süreç değişikliği | kalite-modulu / surec-modulu W |
| Stok listesi ve PDF / stok yazma / stok silme | stok R / stok W / stok-sil W |
| Depo raporları | depo-durumu R |
| Depo lokasyon tanımı | depo-durumu W (sandığa lokasyon atama ayrı işlemdir) |
| Hareket geçmişi | hareket-gecmisi R |
| Kullanıcı, onay, bildirim aboneliği, ambalaj, finans | Request üzerindeki mevcut sabit veya çoklu işlem izinleri; artık header fallback yok |

## Gerçek proje tipine bağlı işlemler

Normal / Saha / Yedek türü, mevcut kaydın proje FK'si üzerinden okunur. Gövdede başka projenin ID'si verilerek bir satır/sandık yetkilendirilemez. Her seçili kayıt var olmalıdır; karışık toplu seçimde her projenin gereksinimi sağlanır.

| Operasyon | Normal | Saha | Yedek |
|---|---|---|---|
| Grid okuma/yazma | grid-modulu | saha-grid-modulu | yedek-grid-modulu |
| 3K okuma/yazma | 3k-modulu | saha-3k-modulu | yedek-3k-modulu |
| Sandık yazma / tekil-toplu taşıma / sandık sevk-düzeltme | sandik-yonetimi **veya** 3k-modulu W | saha-sandiklar **veya** saha-3k-modulu W | yedek-sandiklar **veya** yedek-3k-modulu W |
| Proje sevki | proje-sevk-et W | saha-sevk-et W | yedek-sevk-et W |
| Yeni proje | aktif-projeler **veya** sandik-yonetimi W | saha-yonetimi W | yedek-yonetimi W |
| Proje silme | proje-sil W | saha-proje-sil W | yedek-proje-sil W |
| Planlanan tarih | planlanan-sevk-tarihi W | planlanan-sevk-tarihi W | yedek-planlanan-sevk-tarihi W |
| Gerçekleşen çeki raporu | gerceklesen-ceki-raporu R | saha-gerceklesen-ceki-raporu R | yedek-gerceklesen-ceki-raporu R |
| 3K sandık durum raporu | 3k-sandik-durum-raporu R | saha-3k-sandik-durum-raporu R | yedek-3k-sandik-durum-raporu R |
| Genel sandık raporu | sandik-yonetimi **veya** aktif-projeler R | saha-raporu R | yedek-raporu R |
| Tekil / toplu eksik raporu | eksik-raporu R | saha-sevk-sonrasi-eksik-raporu R | yedek-eksik-raporu R |

Ortak proje/sandık okumaları ilgili türün yönetim, Grid, 3K ekranlarından kullanılabilir. Depo görünümü, genel sevk listesi ve kaynak/hedef seçen sahaya aktar ekranı kendi okuma izinleriyle ortak okumaları kullanır. Proje dropdown'u yalnız seçim bilgisi veren ortak lookup'tır; erişimi çağıran kullanım senaryolarının açık OR listesine bağlıdır, yazma izni kazandırmaz. Normal eksik kaynak seçimi, saha/yedek sandık ve sahaya aktar ekranlarının ortak okumasıdır.

Revizyon geçmişi/liste/detay/dosya için gerçek türün yönetim izni (`sandik-yonetimi`, `saha-yonetimi`, `yedek-yonetimi`) **veya** `ceki-revizyon-yukle` **veya** `sevk-edilen` R gerekir. Proje kilit açma talebinde gerçek türün yönetim izni **veya** `sevk-edilen` W gerekir; onay süreci ayrıca korunur.

`IRequiresMenuPermissions` varsayılan **All**; açık `PermissionMatch.Any` alternatif izin grubudur. Gruplar kendi aralarında **AND**'dir. Bir boş/geçersiz grup izin kazandıramaz. Sabit menü sözleşmesinin mevcut Query=R / Command=W kuralı korunur; merkezi katalog rapor oluşturan salt-okuma komutlarını açık R olarak sınıflar.

## Korunan istisnalar ve ilişkiler

- Login ve 2FA challenge akışları menü yetkisine tabi değildir; kendi kimlik doğrulama kontrolleri korunur.
- `MenuAuthorizationExceptions` public (login/2FA ve yalnız `LookupBase` türlerini kabul eden lookup), kimliği zorunlu kişisel okuma/yazma ve yalnız sunucu içi komutları adlarıyla listeler. Kategoriye girmeyen işaretsiz bir request otomatik geçmez.
- Kullanıcının sidebar menüsü ayrı `GetKullaniciMenuQuery` ile yalnız oturum sahibinin **güncel DB rolünden** okunur. İstemci rol/kullanıcı ID'si seçemez; rol yönetimi detay endpoint'i bunun üzerinden açılmaz.
- Bildirimlerin kişisel okumaları ve kişisel onay geçmişi mevcut sahiplik/erişim kontrolleriyle çalışır. Dahili finans aktarım ve background akışları dış HTTP operasyonu olarak eklenmedi.
- Onay handler'ının doğrulanmış karar/payload sonrası açtığı sunucu `IApprovalExecutionContext` bağlamında yalnız `IApprovalOperation` çalıştırma izni devralır. Aynı bağlam rol silme gibi ilgisiz request'leri bypass etmez. Handler iş kuralları tekrar çalışır.
- Saha eksik tamamlamasında kaynak normal satırın hedef saha projesiyle aynı projede olması istenmez; hedefin gerçek yetkisi ve mevcut handler'ın kaynak uygunluğu/kapasite kontrolleri birlikte korunur.
- Tam tekil taşımada kaynak içerik silinse bile aynı kullanıcı + işlem anahtarı + aynı payload, değişmez transfer defterinden kanıtlanırsa tekrar isteği gerçek proje yetkisiyle değerlendirilebilir. Defter yoksa veya kullanıcı/içerik farklıysa bu istisna açılmaz.

## Kurulum ve doğrulama sınırı

Yeni tablo, kolon veya izin seed'i gerekmiyor. Mevcut menü/işlem izinleri artık sunucu tarafından gerçekten uygulanır; canlıya geçerken kullanılan rollerin gerekli R/W kayıtlarını yönetim ekranından kontrol edin. UI gizleme veya header gönderme eksik DB izninin yerine geçmez.

`ServerAuthorizationRegressionTests` ve `OrtakYetkiOnayIsKurallariTests` header spoof, readonly, normal/saha/yedek, ilişkiler, açık AND/OR, onay/public giriş, self-menu ve idempotent tekrar senaryolarını doğrular. PostgreSQL SQL çeviri testi bağlantı açmadan LINQ çevirisini kontrol eder. Bunlar gerçek HTTP/JWT middleware, canlı rol verisi veya PostgreSQL bağlantılı entegrasyon testinin yerine geçmez.
