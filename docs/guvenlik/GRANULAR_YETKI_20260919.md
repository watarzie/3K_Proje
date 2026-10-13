# Üretim ve finans ayrıntılı yetkileri

Bu değişiklik eski N/R/W sayılarını değiştirmez. Kök modül W izni artık alt işlemleri kapsamaz. Her ekran, eylem ve alan `3K.Core/Constants/YetkiKodlari.cs` kataloğunda ayrı kod taşır. İşlemlerde katalogdaki W, alan ve dosya görüntülemede R kullanılır. Aynı semantik işlemin eski kaynak adı (`Duzenle`, `Rapor`, `FinansTarihiDegistir`) uyumluluk için korunur; bağımsız işlemler aynı kök string'e bağlanmaz.

Menü ağacında üst izin alt iznin tavanıdır: üst N ise bütün alt erişimler N, üst R ise alt yazma izinleri etkin olarak kullanılamaz. Yalnız W anlamı taşıyan işlem kodlarının R işlemi olmadığı için üst R altında etkin karşılığı N'dir; bağımsız okuma kodları en fazla R olabilir. Üst W, alt izinleri kendiliğinden vermez. Bu sınır rol, kişisel izin, kullanıcı menüsü ve sunucu işlem kontrolünde uygulanır; eski tutarsız rol kayıtları da yalnız alt W iznine dayanarak işlem açamaz.

Üretim/finans istekleri kök modül izni **ve** bildirilen eylem/alan izinlerini birlikte gerektirir. Kök kodlar `ambalaj-uretim-listesi` ve `finans-yonetimi`dir. Bir alt W işlemi için kök de W olmalıdır; kök R salt okumayla sınırlıdır. Modülün kişisel açık reddi bütün alt endpoint'leri kapatır; bir alt kodun açık izni bu reddi aşmaz. Katalogdaki bağımsız alt izin zorunluluğu sürer; kök W tek başına bütün eylemleri vermez. Hiyerarşi sınırı Grid/3K/Saha gibi eski modüllerde de geçerlidir.

Kök şartı isteğin kendi modülüne aittir. Finans yanıtındaki ölçü/m³/sarf alanı için finans kökü ve ilgili ortak alan kodu gerekir; ayrıca üretim ekranına giriş izni aranmaz. Üretim kökünün reddi finans köküne veya bağımsız alan kararına taşınmaz. Böylece finans rolü üretim ekranına girmeden açıkça verilmiş alanı görebilir; alanın kendi kişisel reddi her iki modülde de uygulanır.

## Finans sekme düğümleri (8 Ekim 2026)

Rol ağacında Finans modülü sekme bazında kırılır: `finans-yonetimi` altında her sekmenin bir düğümü vardır (`finans-sekme-panel`, `-is-takibi`, `-po-fatura`, `-giderler`, `-gider-kategorileri`, `-tanimlar`, `-raporlar`, `-munferit-isler`; Route NULL olduğu için menüde görünmez). Sekme düğümünün R'si sekmeyi gösterir, W'si sekmedeki işlemlerin tavanıdır; üst düğüm kuralı aynen geçerlidir (sekme N ise altındaki W kodu etkin N, sekme R ise etkin R).

Yalnız o sekmenin ekranında kullanılan işlem kodları sekme düğümünün altındadır: PO ve Faturalar ← sipariş görüntüleme, PO gir/değiştir/iptal, fatura gir/değiştir/iptal, ödeme kaydet; Giderler ← gider ekle/düzenle/iptal; Gider Kategorileri ← gider kütüphanesi yönetimi; İş ve Tarife Tanımları ← düzenli iş ve iş kütüphanesi yönetimi; Finans İş Takibi ← iş iptali, tarih ve fiyatlandırma değiştirme. Birden çok sekmenin veya alan maskesinin kullandığı kodlar (kayıt/rapor/gider görüntüleme, parasal alanlar, birim fiyat, dışa aktarma, belge, kalıcı silme, denetim, tek seferlik iş ekleme) kökte kalır. Ambalaj sekmeleri bu değişikliğin kapsamı dışındadır.

Sunucu, yalnız o sekmeye özel uçlarda sekme kodunu mevcut kodlarla birlikte (AND) ister: panel ← `finans-sekme-panel`; aylık iş listesi, proje sandık kırılımı, aylık ve sipariş durumu raporu ← `finans-sekme-is-takibi`; gider listesi ← `finans-sekme-giderler`; iş/özet/alacak raporu ve rapor önizleme ← `finans-sekme-raporlar`. Taşınan kodlar zaten sekme düğümüyle tavanlandığından PO/fatura, tanım ve kategori uçlarına ayrıca sekme kodu eklenmez. Birden çok sekmenin kullandığı uçlar (genel arama, kayıt detayı, bekleyen işler, gider raporu) sekmeye bağlı değildir. Vade parametrelerini okumak artık ön yüzle aynı olarak iş kütüphanesi R ister.

Ön yüz sekme düğmesini ve gövdesini aynı kuralla gösterir (`ortak/finans-izinleri.ts` `finansSekmeAcik`); açılış sekmesi erişilebilen ilk sekmedir, yalnız açık sekmenin verisi yüklenir, oturumda izin kaldırılırsa erişilebilen ilk sekmeye geçilir. Gider Kategorileri sekmesi okuma izniyle açılır; ekleme/düzenleme W ister. Rol editöründe alt düzeydeki dallar kapalı başlar ("Tümünü aç/kapat"), sekme düğümü kısa R/W açıklaması taşır ve sekme N yapılırken altındaki seçimlerin kapanacağı onaylatılır.

Geçiş `scripts/database/20261008_03_Finans_Sekme_Yetkileri.sql` ile yapılır: önce düğümler eklenir, sonra her role sekme seviyesi (taşınacak alt kodlardaki ve sekmeyi bugün açan koddaki seviyelerin yükseği, modül seviyesiyle sınırlı; Admin W) verilir, en son kodlar taşınır. Sıra tersine olsaydı taşınan kodlar fiilen N olur ve rol editöründeki ilk kayıtta eski W kalıcı silinirdi. Betik yeni API'den önce uygulanır.

## Finans yalnız sekme izni (13 Ekim 2026)

Finans'ta alt işlem ve alan izinleri kaldırıldı: yalnız 8 sekme düğümü ve İş Takibi üstündeki vade uyarısı için `finans-vade-uyarisi` (R) kalır. Sekmenin R'si sekmedeki bütün verileri (tutar, birim fiyat, gelir, gider, kârlılık, ölçü, m³) gösterir; W'si sekmedeki bütün işlemleri (kalıcı silme dahil) yapar. `FinansAlanErisimFiltresi`/`FinansAlanProjeksiyonu` ve `IAlanErisimService` kaldırıldı; rapor ve belge dosyaları ayrıca alan izni istemez. Yukarıdaki "Finans sekme düğümleri" bölümünde anlatılan alt kodlar ve alan maskesi artık geçerli değildir. Birden çok sekmede açılan ortak işlemler (kayıt detayı, belgeler, PO açma…) `3K.Core/Constants/FinansSekmeleri.cs` kümelerinden herhangi bir sekmenin izniyle yapılır; ön yüz aynası `ortak/finans-izinleri.ts`. Eşleme tablosu: `SUNUCU_YETKI_ESLEMESI.md`.

Geçiş `scripts/database/20261013_02_Finans_Sekme_Yetkisine_Gecis.sql` ile yapılır (onaylı, yıkıcı): önce her rolün sekme seviyesi alt kodlardaki seviyesine yükseltilir (modül seviyesiyle sınırlı, Admin W), vade uyarısı izni eski kapıyı geçen rollere (sipariş ve kayıt görüntüleme R) verilir, sonra alt kodlar rol izinleriyle silinir. Sekme W'si olan rol sekmedeki bütün işlemleri yapar; ince ayrım gerekiyorsa sekme bazında ayrı rol tanımlanır. Ambalaj alan maskelemesi (`AmbalajAlanMaskesi`) bu değişikliğin dışındadır.

## Rol ve yönetim

Yetki yalnız rolden gelir: rol izni üst düğümün etkin seviyesiyle sınırlanır, kaydı olmayan kod N'dir. Kullanıcıya özel açık izin/ret özelliği (`KullaniciYetkileri` tablosu, `/api/kullanici/{id}/yetkiler` uçları ve ön yüzdeki "Kullanıcıya özel izinler" penceresi) 3 Ekim 2026'da kaldırıldı; tablo `scripts/database/20261003_01_Kisisel_Yetkileri_Kaldir.sql` ile silinir. Tek bir kişiye istisna gerekiyorsa ayrı, dar bir rol tanımlanır.

Admin normal menü/alan kontrollerini atlamaz. Rol 1'e açık seed izinleri yazılır. Önceden var olan onay yönetimindeki Admin rolü istisnası ayrı kalır. Yeni rolün adını Admin yapma yolu reddedilir.

Rol oluşturma/değiştirme/silme `rol-yonetimi:W` ister. Bu izne sahip kişi, kendi rolü dâhil rollere kendi alt izinleriyle sınırlanmadan yetki atayabilir; bu yüzden `rol-yonetimi:W` yalnız güvenilir yöneticilere verilmelidir. Kullanıcıya rol atamak `kullanicilar:W` ister (`RolAtamaService`). Ayrı `yetki-atama` izni kullanılmaz. Kullanıcı kendisine rol atayamaz ve sahip olmadığı izinleri içeren rolü kullanıcıya atayamaz. Rol güncellemesinde boş izin listesi bütün rol izinlerini kaldırır.

`GET /api/rol/sablonlar` üretimde Yönetici/Üretim Sorumlusu/Üretim Personeli/Görüntüleme ve finansta Finans Yöneticisi/Fatura Kullanıcısı/Sipariş Kullanıcısı/Rapor Görüntüleyici/Yönetici/Sadece Okuma şablonlarını döndürür. Rol oluşturma isteğindeki isteğe bağlı `sablonKodu` şablonu açıkça uygular; şablonsuz boş rol oluşturulabilir. W alt izin içeren şablonun kökü W, yalnız R içeren şablonun kökü R atanır; kök W yine yalnız seçilmiş alt izinleri açar. Salt okuma şablonları W içermez; personel şablonu kritik işlem içermez.

Rol izni değişiklikleri aktör, hedef, eski/yeni değer ve sunucu zamanı ile `YetkiDegisiklikleri` tablosuna yazılır; kaldırılan kişisel izin özelliğinin eski kayıtları (`HedefTuru = 'Kullanici'`) denetim geçmişi olarak korunur. Hedef silinmesi audit'i cascade silmez.

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

`FinansAlanHttpSecurityTests` ayrı yerel Kestrel sunucusunda gerçek JWT doğrulaması, finans controller'ları (ortak `FinansControllerBase`), MediatR authorization, AlanErisimService ve MVC JSON filtresini birlikte çalıştırır. 401, sahte menüyle 403, aynı token ile sonraki istekte izin iptali, alt alanların null kalması, audit JSON/string gizlemesi, eksik alan izinleriyle binary dosyanın 403 olması ve tam izinle indirme doğrulanır. Uygulama Program'ı, appsettings ve worker'lar çalıştırılmaz; finans veri servisi ve izin deposu sentetiktir. Dolayısıyla bu test tek başına gerçek DB + HTTP uçtan uca kullanıcı kabulü sayılmaz; kalıcılık ayrıca PostgreSQL testiyle sınanır.

`FinansAlanProjeksiyonuTests` m³ fiyatlandırma miktarı, aylık sipariş/fatura m³ miktarları, sarf satırı, dinamik şablon stringleri ve enum biçimlerini; `AmbalajAlanMaskelemeTests` legacy planlama/şablon/özet, sarf bağımsızlığı, geçmiş erişimi, snapshot audit, form parçaları ve null toplamları doğrular. Yeni alan eklendiğinde yalnız UI kolonu gizlemek yeterli değildir: JSON alan sınıflandırması ve rapor çıktısı için negatif test eklenmelidir.
