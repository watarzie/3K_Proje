# Toplu sandık taşıma — 19 Eylül 2026

`POST /api/Sandik/urunleri-toplu-tasi` aynı projedeki tek kaynak sandığın 1–250 farklı `SandikIcerikId` kaydını ortak hedefe taşır. İstek `projeId`, `kaynakSandikId`, `hedefSandikId`, `islemAnahtari` ve `satirlar[{kaynakSandikIcerikId,tasinanAdet}]` içerir. Her satır ayrı kimliğiyle işlenir; barkod/ürün adı üzerinden gruplama yapılmaz. Aynı kaydı iki kez seçmek reddedilir. Legacy kaynakta aynı çeki satırının birden çok içeriği seçilmişse belirsiz kayıtlar birleştirilmek yerine işlem reddedilir.

Mevcut K-033–K-039 tekil taşıma kuralları `SandikUrunTasimaIslemi` içinde ortak yürütülür. Tekil handler da aynı motoru çağırır; miktar dağılımı ve iş kuralları değiştirilmedi. Özellikle:

- Pozitif, 14 tam/4 ondalık basamağı aşmayan miktar; gerçek proje/çeki ilişkisi ve kaynak tahsis sınırı zorunludur.
- Kaynak veya hedef sevk edilmişse düzeltme kilidi açık olsa da taşıma yapılamaz; resmi sevkiyat geri alma gerekir. Aktif normal→saha kaynak blokajı korunur.
- Tahsis istenen miktar kadar, fiziksel miktar yalnız `min(taşınan tahsis, mevcut konulan)` kadar taşınır. Planlanan fakat gelmemiş miktar fiziksel teslim üretmez.
- Stok/proje/tedarikçi kırılımları, eksik payı ve aktif Grid teslim sayacı mevcut oran/4 basamak kurallarıyla bölünür. Ana kümülatif miktarlar ve kalite durumu değiştirilmez. Mevcut miktarlı taşımada olmayan yeni bir kalite red kuralı eklenmedi.
- Hedefte aynı gerçek çeki satırına ait mevcut tahsis tekil akıştaki gibi kullanılır. Çeki bağı olmayan aynı barkodlu manuel kayıtlar ayrı kalır.
- Konum/sandık durumları ve saha aktarım izi mevcut tekil akışla güncellenir. Hareket geçmişi ve transfer defteri her satır için korunur.

## Atomiklik ve tekrar

Toplu işlem mevcut `UnitOfWork` üzerinden **tek Serializable transaction** kullanır. Bir satırın başarısız `Result` sonucu özel rollback exception'ına çevrilir; daha önceki satırın SaveChanges'i dahil tüm iş, defter ve hareketler geri alınır. İlgili içerik kimliği ve hata nedeni yanıtlanır. UnitOfWork'ün normal `Result.Failure` dönüşünde commit edebileceği göz önüne alınmıştır; hata sonucu commit edilmez.

Yeni `SandikTopluTasimaIslemleri` tablosu miktar defteri değildir: işlem anahtarı + kanonik istek SHA256 + kullanıcı ve nesne kimliklerini saklar. Aynı kullanıcı/anahtar/istek sırası değişse de tekrar uygulanmaz; farklı miktar/hedef/seçim veya kullanıcı 409 alır. Benzersiz işlem anahtarı, mevcut xmin ve Serializable korumaları eşzamanlı işlemleri sınırlar. Defter ve bütün taşıma aynı transaction'da commit edilir.

Sunucu yetkisi gerçek kaynak/hedef sandıklarının proje türünden yazma izni olarak çözülür; `X-Menu-Kod` yetki kaynağı değildir. UI seçimi/yazma görünürlüğü yalnız kullanıcı deneyimidir.

## Testler ve kurulum

- `SandikTopluTasimaTests`: miktar/limit/mükerrer seçim/anahtar doğrulaması; legacy belirsiz kaydı yazmadan reddetme.
- `SandikTopluTasimaPostgresTests`: gerçek rollback, eşzamanlı aynı/farklı anahtarlar, tekrar payload/kullanıcı çakışması, miktar/aktif sayaç/kaynak payları, sevkiyat kilidi, proje ilişkisi, saha blokajı, aynı barkodlu kayıtların ayrılığı ve SQL'in iki kez uygulanması.
- `SandikTasimaKatalogKurallariTests`: mevcut tekil akışın regresyonları korunur.
- Frontend `sandik-toplu-tasi.component.spec.ts` ve `sandik-toplu-tasima.service.spec.ts`: tek istek, tahsis/fiziksel önizleme, izin/loading, dört basamak ve ağ tekrar anahtarı.

PostgreSQL testleri, yalnız açıkça verilen `THREEK_TEST_POSTGRES` yönetim bağlantısındaki **izole localhost test sunucusunda** geçici `sandik_toplu_tests_<guid>` veritabanları oluşturup kaldırır. Uygulama appsettings bağlantısı kullanılmaz. Değişken yoksa entegrasyon testleri açıkça atlanır; sıfır-atlama kontrolü olan tam regresyon kapısı bu değişkenle çalıştırılmalıdır. Bu testler gerçek üretim trigger'ları veya bütün saha servislerinin entegrasyon kanıtı değildir; saha servisi bağımlılığı testte kontrollüdür.

Yayın öncesi `scripts/database/20260919_01_Sandik_Toplu_Tasima.sql` uygulanır. Sıfır kurulum/güncelleme açıklaması aynı dizindeki README'dedir. Script mevcut miktar/tahsis/iş verilerini değiştirmez.
