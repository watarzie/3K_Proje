# Finans V2 geçişi ve doğrulama

Canlı uygulama veya veritabanı bu geliştirme sırasında çalıştırılmadı/değiştirilmedi. SQL otomatik startup migration değildir; yetkili dağıtım adımıdır.

## Değişmez sözleşmeler

- İş net bedeli, PO net dağıtımı ve fatura net dağıtımı ayrı snapshot'lardır. PO/fatura `NetTutar` istekleri fiziksel adet/m³ üretmez. Net kapasite kuruş hassasiyetinde korunur; belge toplamı satır toplamına eşit olmalıdır. Fatura son satırı değil, bir PO kalemini tamamlayan son fatura kalan KDV kuruşunu alır.
- Bir PO birden çok proje/iş içerir; tek para birimi zorunludur. Farklı PO'ları tek faturada birleştirme kapsam dışıdır. Aynı iş birden çok PO'ya bölünebilir. PO/fatura numarası iptalden sonra da tekrar kullanılamaz.
- Fiyat/tarife snapshot'ı salt tarihin değişmesiyle yenilenmez. PO geçmişi bulunan iş yeniden fiyatlandırılamaz. Yeni v2 fiyatlandırma ve finans tarihi komutları gerekçeli/auditlidir; üretimi değiştirmez. Üretim senkronizasyonu manuel finans miktarını/tarihini ezmez.
- Gelir=faturalanan net; gider=avans hariç kayıtlı net gider; fark=gelir−gider; tahmini kâr=uygun aktif iş net bedeli−gider. Para birimleri ayrı tutulur, kur varsayılmaz. Panel ve finans özetlerinin tarih ekseni iş/gider finans tarihidir; belge listelerinin tarihi kendi belge tarihidir. Yaşlandırma ilk kayıt ve PO tarihinden başlar; finans tarihi taşıması yaşı sıfırlamaz.
- Kalıcı silme varsayılan olarak tüm ilişkilerde engellenir (iptal edilmiş belgeler dahil). Önizleme sürümü, ikinci onay ve gerekçe zorunludur. Ortak PO/fatura silinmez. Kaynak iş silinirse yeniden aktarımı bastıran kayıt ve bağımsız audit korunur.
- PDF içerik+metadata aynı PostgreSQL transaction'ında saklanır. En fazla 10 MB; uzantı, PDF imzası ve EOF doğrulanır. Bu doğrulama antivirüs değildir. Kullanıcı dosya yolu kullanılmaz; hash/sunucu adı/versiyon/yükleyen tutulur. PDF sürümleri üstüne yazılmaz.
- Şablon alanları typed/versioned; serbest kod/formül çalıştırılmaz. Fiyat bileşenleri aynı para biriminde typed yöntemlerle hesaplanır. Düzenli işler worker/explicit komutla idempotent eksik ayları tamamlar; GET okumaları kayıt üretmez. 29/30/31 günleri ay sonuna kırpılır.

## Dağıtım sırası

1. Uygulama yazmalarını ve worker'ı durdurun; doğrulanmış tam PostgreSQL yedeği alın, geri yüklemeyi ayrı DB'de prova edin.
2. `scripts/database/20260919_03_Finans_Onizleme.sql` salt okunur raporunu alın. Çok para birimli PO, iş netini aşan PO ve PO'yu aşan fatura varsa yetkili uzlaştırma kararı verin. Fiyatı güncel tarifeden yeniden hesaplayarak geçmişi değiştirmeyin.
3. Üretim/yetki geçişlerini kendi belgelerine göre uygulayın; `20260919_03_Finans_V2.sql` betiğini bakım penceresinde tek transaction olarak çalıştırın. Hata halinde tüm değişiklikler rollback olur. SQL advisory lock tekrarlı geçişi seri tutar.
4. V1 iş/PO kalemi/fatura kalemi/fatura/gider snapshot'ları `FinansV2GecisYedegi` tablosunda ilk halleriyle saklanır; tekrar çalıştırma bunları güncellemez. Eski finans dönemi işin finans tarihi olarak korunur; bilinmeyen gün tahmin edilmez. Gider aynı ayda ise gerçek gider günü, taşınmış farklı dönemdeyse eski finans dönemi korunur. V1 finans miktarları manuel sahiplik flag'iyle kaynak yeniden hesaplamasından korunur.
5. Eski kabul edilmiş fatura belge net/KDV tutarı mevcut satır snapshot oranıyla dağıtılır; kuruş farkı son satıra verilir. Fiziksel miktar ve PO fiyatı değişmez. Aşılan iş/PO kapasitesi migration'ı durdurur; sessiz tutar düzeltmesi yapılmaz.
6. Yeni kodu başlatmadan önce satır sayıları, para birimi toplamları, tarih aralıkları, duplicate source key ve belge FK'lerini kontrol edin. UI ile bir sentetik/ayrılmış test projesinde gerekçeli fiyat/tarih/PO/fatura akışını doğrulayın. Sonra worker'ı açın.

## Rollback

Migration transaction'ı hata verirse PostgreSQL kendisi rollback eder. Başarılı geçişten sonra henüz yeni yazma yoksa en güvenli geri dönüş, eski uygulama sürümüyle birlikte doğrulanmış tam DB yedeğini geri yüklemektir. Yeni v2 yazmalar başladıktan sonra otomatik down-script çalıştırmayın: amount-only dağıtımlar V1 fiziksel miktar sözleşmesine dönüştürülemez. Önce yazmaları durdurup yeni veriyi ayrı yedekleyin; v2 kayıtlarını ve audit'lerini koruyarak ileri düzeltme veya kontrollü geri yükleme planlayın. `FinansV2GecisYedegi` karşılaştırma ve kontrollü satır restorasyonu için yardımcıdır; tam DB yedeğinin yerine geçmez. Yeni tabloları/belgeleri silen otomatik geri alma yoktur.

## Sınırlar ve testler

Panel/rapor graph'ı en fazla 20.000 iş ve 20.000 giderle sınırlıdır; sessiz truncation yerine hata verir. Panel yılbaşından seçili bitişe YTD topladığı için yalnız ayı daraltmak yüksek yıllık hacimde yeterli olmayabilir. Yaşlandırma en fazla 20.000 filtreli iş; birleşik hareketler en fazla 20.000'inci sayfa önekine gider. PDF/Excel tüm izinleri gerektirir; gerekli para/ölçü/m³/sarf izni eksikse 403. JSON alanları yetkisizse sahte sıfır değil null; audit içeriği ve dinamik alan kapları da maskelenir.

`FinansV2PostgresTests` yalnız `THREEK_TEST_POSTGRES` test profilinde localhost:55439/postgres bağlantısından GUID isimli geçici DB oluşturup sonunda siler; appsettings okumaz. Tutar bölüşümü, concurrency rollback, tarih, NET/SARF, manuel sahiplik, bastırma, şablon sürümü, 31 catch-up, belge metadata ve SQL iki kez uygulama senaryolarını kapsar. `FinansAlanHttpSecurityTests` gerçek controller/JWT/authorization/alan filtresi zincirini izole servis verisiyle doğrular.
