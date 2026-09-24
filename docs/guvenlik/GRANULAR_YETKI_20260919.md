# Üretim ve finans ayrıntılı yetkileri

Bu değişiklik eski N/R/W sayılarını değiştirmez. Kök modül W izni artık alt işlemleri kapsamaz. Her ekran, eylem ve alan `3K.Core/Constants/YetkiKodlari.cs` kataloğunda ayrı kod taşır. İşlemlerde katalogdaki W, alan ve dosya görüntülemede R kullanılır. Aynı semantik işlemin eski kaynak adı (`Duzenle`, `Rapor`, `FinansTarihiDegistir`) uyumluluk için korunur; bağımsız işlemler aynı kök string'e bağlanmaz.

Menü ağacında üst izin alt iznin tavanıdır: üst N ise bütün alt erişimler N, üst R ise alt yazma izinleri etkin olarak kullanılamaz. Yalnız W anlamı taşıyan işlem kodlarının R işlemi olmadığı için üst R altında etkin karşılığı N'dir; bağımsız okuma kodları en fazla R olabilir. Üst W, alt izinleri kendiliğinden vermez. Bu sınır rol, kişisel izin, kullanıcı menüsü ve sunucu işlem kontrolünde uygulanır; eski tutarsız rol kayıtları da yalnız alt W iznine dayanarak işlem açamaz.

Üretim/finans istekleri kök modül izni **ve** bildirilen eylem/alan izinlerini birlikte gerektirir. Kök kodlar `ambalaj-uretim-listesi` ve `finans-yonetimi`dir. Bir alt W işlemi için kök de W olmalıdır; kök R salt okumayla sınırlıdır. Modülün kişisel açık reddi bütün alt endpoint'leri kapatır; bir alt kodun açık izni bu reddi aşmaz. Katalogdaki bağımsız alt izin zorunluluğu sürer; kök W tek başına bütün eylemleri vermez. Hiyerarşi sınırı Grid/3K/Saha gibi eski modüllerde de geçerlidir.

Kök şartı isteğin kendi modülüne aittir. Finans yanıtındaki ölçü/m³/sarf alanı için finans kökü ve ilgili ortak alan kodu gerekir; ayrıca üretim ekranına giriş izni aranmaz. Üretim kökünün reddi finans köküne veya bağımsız alan kararına taşınmaz. Böylece finans rolü üretim ekranına girmeden açıkça verilmiş alanı görebilir; alanın kendi kişisel reddi her iki modülde de uygulanır.

## Kullanıcı kararı ve yönetim

Değerlendirme sırası: kişisel açık ret → kişisel açık izin → rol izni → ret. `KullaniciYetkileri` tablosunda satır olmaması devralmadır; `IzinVerildi=false` açık rettir. Aynı kullanıcı/kod benzersizdir. Kişisel izin yalnız o kodun katalog seviyesini verir; bir alanın R izni W işlemine dönüşmez.

Admin normal menü/alan kontrollerini atlamaz. Rol 1'e açık seed izinleri yazılır ve kullanıcıya verilen açık ret Admin için de etkilidir. Önceden var olan onay yönetimindeki Admin rolü istisnası ayrı kalır. Yeni rolün adını Admin yapma yolu reddedilir.

Rol oluşturma/değiştirme/silme `rol-yonetimi:W` ister. Bu izne sahip kişi, kendi rolü dâhil rollere kendi alt izinleriyle sınırlanmadan yetki atayabilir; bu yüzden `rol-yonetimi:W` yalnız güvenilir yöneticilere verilmelidir. Kullanıcıya rol veya kişisel izin atamak ve kişisel izin listesini görüntülemek `kullanicilar:W` ister. Ayrı `yetki-atama` izni kullanılmaz. Kullanıcı kendisine rol atayamaz veya kendi kişisel izinlerini değiştiremez. Başkasına sahip olmadığı yeni kişisel izinleri veremez ve sahip olmadığı izinleri içeren rolü kullanıcıya atayamaz. Rolden devralma yoluyla açık reddin kaldırılması da kişisel yetki artışıysa aynı kontrolden geçer. Rol güncellemesinde boş izin listesi bütün rol izinlerini kaldırır.

`GET /api/rol/sablonlar` üretimde Yönetici/Üretim Sorumlusu/Üretim Personeli/Görüntüleme ve finansta Finans Yöneticisi/Fatura Kullanıcısı/Sipariş Kullanıcısı/Rapor Görüntüleyici/Yönetici/Sadece Okuma şablonlarını döndürür. Rol oluşturma isteğindeki isteğe bağlı `sablonKodu` şablonu açıkça uygular; şablonsuz boş rol oluşturulabilir. W alt izin içeren şablonun kökü W, yalnız R içeren şablonun kökü R atanır; kök W yine yalnız seçilmiş alt izinleri açar. Salt okuma şablonları W içermez; personel şablonu kritik işlem içermez.

`GET /api/kullanici/{id}/yetkiler` rol/kişisel/etkin kararı birlikte döndürür. `PUT` aynı adrese `[{menuTanimiId,izinVerildi:true|false|null}]` gönderir; liste bütünü değiştirilir, null veya bulunmayan satır devralır. Karar değişiklikleri aktör, hedef, eski/yeni karar ve sunucu zamanı ile `YetkiDegisiklikleri` tablosuna yazılır. Hedef silinmesi audit'i cascade silmez.

## Alan güvenliği ve oturumlar

`IAlanErisimService.GetAsync` sunucuda ölçü, üretim m³, sarf, parasal ana izin ve birim fiyat/tutar/gelir/gider/kârlılık kararlarını verir. Parasal alt alanların her biri ana izinle AND edilir. Sarf m³ için üretim m³ ve sarf görüntüleme birlikte gerekir. Gizli değeri 0 yapmak geçerli değildir; HTTP yanıtında null veya alanın bulunmaması gerekir. Finans HTTP filtresi ve üretim DTO projeksiyonları bu sözleşmeyi kullanır; dosyada gizli alanları ayıramayan rapor uçları gerekli alan izinleri yoksa 403 döndürür.

Sunucu oturumlar arası izin cache'i tutmaz; her istek güncel rol/override kaydını okur. JWT içindeki eski rol yeni izin vermez. Frontend kayıt sonrası aynı tarayıcıdaki açık sekmelere BroadcastChannel bildirir; sunucunun yetki değişikliği olayı menüyü yeniler. Kaçan olaylar için pencere yeniden odaklandığında seyrek kontrol yapılır; sürekli zamanlayıcıyla menü istenmez. Signal kullanan directive izin kaldırılınca mevcut düğmeyi kaldırır; boş bağlam fail-closed'dur. Backend reddi yenilemeyi beklemez.

Onaylı üretim/finans komutları kayıtlı başlatanın güncel izinleriyle yeniden doğrulanır. Başlatan kimliği olmayan yürütme reddedilir. Gerçek oturum `UserId`, işlemin başlatanı `IslemKullaniciId` olarak ayrıdır. Eski modüllerin onay davranışı bu değişiklikle değiştirilmez.

## Dağıtım ve veri geçişi

`scripts/database/20260919_04_Granular_Yetkiler.sql` bütün şema/katalog/eşleme değişikliğini transaction içinde yapar. Önce uygulama veritabanı yedeği alınır; SQL ayrı dağıtım adımıdır ve uygulama başlatılırken otomatik çalıştırılmaz. Katalog ID/kod çakışmasında işlem durur.

İlk geçişte eski modül R izni yeni görünüm/alan R izinlerine; eski W izni bunlara ve yalnız kritik olmayan eylemlere taşınır. İptal/silme, geçmiş/fiyat/dönem düzeltmesi, yeniden açma, form sonrası karar gibi kritik W izinleri eski W rollerine otomatik verilmez. Bunlar yalnız rol 1'in açık seed'inde veya sonradan yetkili yöneticinin açık atamasında bulunur. Önceki kurulumlarda Admin'e ayrı verilen `yetki-atama` izni artık seed edilmez.

Canlıda eski `yetki-atama` kaydı varsa önce yeni yetki denetimlerini içeren API sürümü yayımlanır. Ardından `MenuTanimlari` tablosunda `Id=5000` ve `Kod='yetki-atama'` eşleşmesi ile bu kayda bağlı `RolYetkileri` ve `KullaniciYetkileri` satırları doğrulanır. Beklenmedik alt menü (`ParentId=5000`) yoksa tek transaction içinde önce bağlı rol/kişisel izinler, sonra menü kaydı temizlenir; `YetkiDegisiklikleri` denetim geçmişi silinmez. Eski geçiş SQL'i bu temizlikten sonra yeniden çalıştırılmaz. Yeni koşul nedeniyle `rol-yonetimi:W` ve `kullanicilar:W` sahibi rollerin kapsamı canlıya çıkmadan gözden geçirilir.

`GranularYetkiGecisleri` işaretçisi eşlemeyi bir kez uygular. SQL'in yeniden çalışması sonradan kaldırılmış bir Admin/rol iznini yeniden vermez ve kullanıcı retlerini silmez. SQL sonunda rol/kod dökümü alınır. Geri dönüş yeni tabloları silerek yapılmaz: uygulama ve veritabanı yedeği birlikte geri yüklenir, aradaki yetki değişiklikleri audit'ten karşılaştırılır.

## Doğrulama sınırları

`GranularYetkiTests` gerçek evaluator/handler akışında ret önceliği, tek hedef izin, oturumda iptal, alan ana/alt izinler, self escalation, boş rol iptali ve onay başlatanını sınar. `GranularYetkiPostgresTests` yalnız localhost:55439 sentetik veritabanında migration tekrarını ve gerçek kayıtlardan ret/rol davranışını doğrular; uygulama DB'sini kullanmaz. Frontend `can-write.directive.spec.ts` yüklenmeyen/boş bağlamı, canlı iptali, izin geri gelmesini ve abonelik temizliğini sınar. Bellek testleri HTTP veya PostgreSQL transaction kanıtı değildir.

`FinansAlanHttpSecurityTests` ayrı yerel Kestrel sunucusunda gerçek JWT doğrulaması, FinansController, MediatR authorization, AlanErisimService ve MVC JSON filtresini birlikte çalıştırır. 401, sahte menüyle 403, aynı token ile sonraki istekte izin iptali, alt alanların null kalması, audit JSON/string gizlemesi, eksik alan izinleriyle binary dosyanın 403 olması ve tam izinle indirme doğrulanır. Uygulama Program'ı, appsettings ve worker'lar çalıştırılmaz; finans veri servisi ve izin deposu sentetiktir. Dolayısıyla bu test tek başına gerçek DB + HTTP uçtan uca kullanıcı kabulü sayılmaz; kalıcılık ayrıca PostgreSQL testiyle sınanır.

`FinansAlanProjeksiyonuTests` m³ fiyatlandırma miktarı, aylık sipariş/fatura m³ miktarları, sarf satırı, dinamik şablon stringleri ve enum biçimlerini; `AmbalajAlanMaskelemeTests` legacy planlama/şablon/özet, sarf bağımsızlığı, geçmiş erişimi, snapshot audit, form parçaları ve null toplamları doğrular. Yeni alan eklendiğinde yalnız UI kolonu gizlemek yeterli değildir: JSON alan sınıflandırması ve rapor çıktısı için negatif test eklenmelidir.
