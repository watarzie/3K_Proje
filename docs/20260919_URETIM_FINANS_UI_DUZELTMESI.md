# Üretim planı, çam öngörüsü ve finans görünümü — 19.09.2026

Bu not, Üretim/Finans V2 geliştirmesinden sonra istenen ekran ve plan öncesi m³ düzeltmesinin son doğrulamasıdır. Önceki V2 geliştirmesinin tamamını yeniden tanımlamaz.

## Değişen davranış

- Üretim planındaki ikinci, düz tablo kaldırıldı. Eski kart/tablo düzeni korunarak form oluşturma, sürüm geçmişi ve satır bazlı yaşam döngüsü işlemleri aynı panele bağlandı. Masaüstünde işlem sütununa alan açıldı; dar ekranda tablo kendi içinde kayar.
- Kaynak ve manuel sandıkların form seçimi tek durumdan yönetilir. Kaynak kaydı ilk kez kalıcı kimlik aldığında seçim kaybolmaz. Yapılmaz kayıtlar forma seçilemez; onaya alınan `202` sonucu uygulanmış gibi gösterilmez.
- **Planı kaydet**, kaynak sandık seçimini ve fırın parti bilgisini kaydeder. Üretim durumunu, üretim/tamamlanma tarihlerini veya gerçekleşme snapshot'ını değiştirmez. Bekleyen sandığın fiilî üretim başlangıcı ilk başarılı üretim formudur. Mevcut form/finans/tamamlanma sonrasında parti değişikliği kritik yetki ve gerekçe denetiminden geçer.
- Manuel sandıkların bu paneldeki seçimi oluşturulacak forma aittir; mevcut plan-kaydet sözleşmesine manuel seçim kalıcılığı eklenmedi. Ekranda bu ayrım açıklanır. Açık panelde kaynak planı kaydedildiğinde manuel form seçimi korunur.
- Finans İş Şablonları, Raporlar, Gider Kategorileri ve PO/Faturalar ekranları mevcut mor/Trezo temasındaki başlık, kart, boş/yükleniyor/hata, arama ve işlem düzenine getirildi. Rapor filtreleri temel kapsam ve açılabilir gelişmiş filtreler olarak ayrıldı. PO/fatura sunucu sayfalaması, arama, yetki, onay ve sürüm sözleşmeleri korunur.

## Çam ihtiyacının anlamı

| Gösterge | Anlam |
|---|---|
| Üstteki **Öngörülen çam ihtiyacı** ve proje/grup m³ | Üretime seçim veya plan kaydı beklemeden, ambalaja dahil kaynak ve manuel sandıkların net çam ihtiyacı |
| Paneldeki **Seçili hacim** / üretim hacmi | Gerçek seçime bağlı miktar; öngörüyle aynı alan değildir |
| Gerçekleşen üretim raporu | Tamamlanma snapshot'ları; öngörü hesabından üretilmez |

Hesap sunucudadır. Üst özet yalnız açık sayfayı değil, izin ve filtre kapsamındaki projeleri esas alır. Kaydedilmiş m³ düzeltmesi korunur; kaynak henüz ambalaj kaydı oluşturmamışsa mevcut ahşap hesaplayıcısı salt-okuma amacıyla kullanılır. Kaynak ölçü birimi dönüşümü ve net ahşap formülü değiştirilmedi. Bu öngörü sarf dahil stok satın alma toplamı veya dış geometrik hacim değildir.

Mevcut trafo ve AH/YG bushing varsayılan hariç tutma politikası ve yetkili kullanıcının açık dahil/hariç kararı korunur. Kontra/katlanır/diğer cinslerde üretim m³ uygulanmaz. Tamamlanmış projeye sonradan gelen, henüz karar verilmemiş kaynak otomatik dahil edilmez. İptal edilen kaynağın eski kaydı yokmuş gibi yeniden varsayılan hesaba girmesi engellendi; aynı kaynak için geçerli aktif kayıt varsa o esas alınır. Ölçüsü eksik dahil sandıklar plan öncesinde de uyarıya girer.

## Doğrulama

| Kontrol | Sonuç |
|---|---|
| Backend Debug | 1.259/1.259 başarılı; 0 atlanan |
| Backend Release + coverage | 1.259/1.259 başarılı; 0 atlanan |
| İş kuralı kataloğu | 501/501 metot referansı ve her iki TRX'te başarılı çalışması doğrulandı |
| Frontend ChromeHeadless | 194/194 başarılı |
| Frontend production build | Başarılı |
| Görsel kontrol | Gerçek bileşenlerle sentetik veri, masaüstü ve 390 px dar ekran |

Ek regresyonlar: plan öncesi kaynak/manual m³, varsayılan ve açık hariç kararları, kaydedilmiş override, eksik ölçü, tamamlanmış proje ve iptal kaynağı; plan kaydının durum/tarih/snapshot koruması; ilk form ve kritik parti değişikliği; kaynak kimliği yenilenirken form seçimi; manuel form seçiminin plan kaydından ayrılması; finans şablon sürümü, kategori, izin değişimi, `202` ve eski arama yanıtı.

`AmbalajUretimV2PostgresTests.PlanlamaOngorusu_SecimOncesi_SqlSayfalamaVeDurumErisimiyleTutarlidir` gerçek PostgreSQL'de sayfalama, tüm sayfaların özeti, durum erişimi ve salt-okuma davranışını doğrular. Testler yalnız `127.0.0.1:55439` üzerindeki ayrı sentetik PostgreSQL sunucusunda kendilerine ait veritabanlarını oluşturup kaldırdı. Görev sonunda bu test sunucusu kapatıldı; uygulama veritabanına SQL uygulanmadı.

Çalışan kullanıcı API'si Debug DLL'lerini kilitlediği için durdurulmadı. Bu son koşumda standart runner yerine aynı tam test projesi `--artifacts-path artifacts/ui-regression-build` ile ayrı çıkış dizininde Debug ve Release olarak derlenip çalıştırıldı; Release'te `--collect 'XPlat Code Coverage'` kullanıldı. `--no-build` kullanılmadı. Ardından `Test-IsKuraliEslemesi.ps1 -TrxPath ...` her iki TRX için çalıştırıldı ve toplam/başarılı/atlanmış sayaçları kontrol edildi. Çıktı dizini için ayrı restore başarılıdır.

Yerel kanıt dosyaları:

- [Debug TRX](../artifacts/test-results/ui-final-20260919/debug/ui-debug.trx)
- [Release TRX](../artifacts/test-results/ui-final-20260919/release/ui-release.trx)
- [Coverage XML](../artifacts/test-results/ui-final-20260919/release/88933d71-d46c-41e2-8179-244e3e83cd09/coverage.cobertura.xml)
- [Frontend test çıktısı](../artifacts/frontend-ui-final-test-20260919.log)
- [Frontend build çıktısı](../artifacts/frontend-ui-final-build-20260919.log)

Assembly satır/dal kapsamı: Application %58,82/%56,02; Core %73,96/%82,40; Infrastructure %20,00/%41,66; API %3,94/%10,10. Bunlar iş kuralı kapsam yüzdesi değildir. Mevcut backend nullable uyarıları ve frontend Bootstrap Sass/vendor `axisIndex` uyarıları sürmektedir; derleme hatası yoktur.

Görsel önizleme yalnız geçici test düzeneğinde sentetik servislerle çalıştırıldı; gerçek kullanıcı oturumu/veritabanı üzerinden uçtan uca kabul yerine geçmez. Geçici önizleme dosyaları ve test sunucusu kaldırıldı; uygulamanın geliştirme sunucusu çalışır bırakıldı. Computer-use becerisi masaüstü/dar ekran kontrolünde kullanıldı; bu kontrol rapor başlığının mobil hizasının düzeltilmesini sağladı.

## Dağıtım ve sınır

**Bu düzeltme için yeni kolon, migration veya SQL eklenmedi.** Önceki Üretim/Finans V2 geçişi henüz uygulanmadıysa onun [dağıtım kılavuzu](../scripts/database/README.md) hâlâ geçerlidir; bu not önceki şema gereksinimini kaldırmaz. Backend ve frontend birlikte yayımlanmalıdır. Canlıya dağıtım yapılmadı.

Grid/3K teslim, tahsis, sevk ve normal–saha senkronizasyon kodları bu ek düzeltme için değiştirilmedi; mevcut testleri tam paket içinde yeniden çalıştı. Başarılı testler bütün olası üretim verileri için sıfır regresyon garantisi değildir. Gerçek veri ve kullanıcı rolleriyle staging kabulü yayımdan önce yapılmalıdır.
