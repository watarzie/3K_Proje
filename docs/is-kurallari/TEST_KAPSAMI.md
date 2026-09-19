# Grid / 3K regresyon testleri

Bu dosya, iş kuralı kataloğunun testlerle nasıl doğrulandığını ve hangi sınırların birim testlerinin dışında kaldığını açıklar. Test sayısı, iş kuralı kapsam yüzdesi veya sıfır regresyon garantisi değildir.

## Güncel ek doğrulama — 19 Eylül 2026

Sunucu yetkilendirmesi, toplu sandık taşıma, Saha/Yedek raporları ve revizyon geçmişi sonrasında tam paket yeniden çalıştırıldı:

| Kontrol | Sonuç |
|---|---|
| Backend Debug | 1.113/1.113 başarılı; 0 atlanan |
| Backend Release + coverage | 1.113/1.113 başarılı; 0 atlanan |
| Çözüm Release build | Başarılı |
| Frontend ChromeHeadless / production build | 82/82 başarılı / build başarılı |
| Katalog ve test referansları | 501/501; eşlenen metotların başarılı çalışması TRX ile doğrulandı |

`ServerAuthorizationRegressionTests`, `SandikTopluTasimaTests`, `RaporTutarliligiTests`, `RaporPostgresEntegrasyonTests`, `CekiRevizyonGecmisiTests` ve `CekiRevizyonYasamDongusuEntegrasyonTests` eklendi. İzole PostgreSQL'de gerçek taşıma transaction/rollback/concurrency, rapor üretimi ve revizyon yükleme–doğrudan/onaylı uygulama–temizlik–indirme doğrulandı. Yeni revizyon testi gerçek A/U/D parser/handler/onay servisiyle aynı adlı dosyaların, snapshot'ın ve tekrar yürütme korumasının bütünlüğünü kontrol eder; SSE/bildirim taşıması no-op'tur.

Bu koşumlarda `THREEK_TEST_POSTGRES` yalnız 127.0.0.1:55439 üzerinde ayrı sentetik test sunucusuna verildi. Uygulama DB'sine veri düzeltmesi uygulanmadı. Değişken olmadan yeni PG testleri atlanır; tam runner sıfır-atlama kontrolünden geçmez. Yeniden çalıştırma, gerekli SQL, dosya listesi, TRX/coverage yolları, PDF/Excel görsel kontrolü ve sınırlar [teslim notundadır](../20260919_YEDI_ISTEK_TESLIM.md).

Gerçek HTTP/JWT middleware, canlı roller ve kullanıcılarla ekran kabulü bu testlerin yerine geçtiği iddia edilmez. Aşağıdaki 12 Eylül sayıları önceki değişikliğin tarihsel sonucudur; yeni koşum olarak okunmamalıdır.

## Doğrulanan sonuç — 12 Eylül 2026

Başlangıçtaki 635 backend vakasına **392 yeni vaka** eklendi. Yeni testler mevcut xUnit projesinde ve mevcut handler/service arayüzleriyle çalışır; test uğruna üretim mimarisi değiştirilmedi.

| Kontrol | Sonuç |
|---|---|
| Backend Debug | 1.027 / 1.027 başarılı, 0 başarısız, 0 atlanan |
| Backend Release + coverage | 1.027 / 1.027 başarılı, 0 başarısız, 0 atlanan |
| Tüm çözüm Release derlemesi | Başarılı; API çalıştırılmadı |
| Mevcut frontend testi | 62 / 62 başarılı; frontend kaynak kodu değiştirilmedi |
| Katalog ve test referansları | 501 / 501 madde eşlendi; isimlendirilmiş testlerin başarılı çalışması TRX'ten doğrulandı |

Son [Debug TRX](../../artifacts/test-results/20260912-221904-56bf6192/is-kurallari.trx), [Release TRX](../../artifacts/test-results/20260912-221844-3048f211/is-kurallari.trx) ve [Cobertura raporu](../../artifacts/test-results/20260912-221844-3048f211/0c25abc5-76b0-4cec-abe1-adf429455107/coverage.cobertura.xml) yerel çalışma çıktılarıdır. Yeni çalıştırma yeni bir rapor klasörü üretir.

Yeni Grid etiket normalizasyon metodunun satır ve dal kapsamı %100'dür. Bu **yalnız yeni metoda** aittir: tüm Application assembly'sinin satır/dal kapsamı %53,61 / %50,18; Core %42,12 / %55,62; Infrastructure %17,07 / %17,42'dir. Bu assembly'ler Grid/3K dışındaki kodları da içerir; bu yüzdeler iş kuralı kapsamı olarak yorumlanmamalıdır.

Katalogdaki kapsam durumu:

| Durum | Madde |
|---|---:|
| Doğrudan birim testli | 349 |
| Kısmen doğrulanan | 69 |
| Gerçek entegrasyon doğrulaması gereken | 19 |
| Arayüz doğrulaması gereken | 33 |
| Açık fark / karar / test ihtiyacı | 29 |
| Çalıştırılabilir uygulama kuralı olmayan | 2 |
| Toplam | 501 |

Dolayısıyla **501 maddenin tamamı eksiksiz test edildi** denmiyor. Her maddenin tam sınırı aşağıdaki eşleme tablolarında görülebilir; bileşik bir kuralın tek alt senaryosunu test etmek, tamamını kapsamak sayılmadı.

## Bu değişiklikte giderilen hata

Tamamlanmış 2 adet Grid sevki ve 2 adet 3K tesliminden sonra **Çeki verisi düzenle** ile ihtiyacın 3'e çıkarılması, tek tam tahsisi büyütüyor fakat eski Grid **Tam Geldi** durumunu bırakıyordu. Devam sevki `Grid gelen 2 - 3K teslim 2 = 0` gördüğü için kalan 1 kilitli kalıyordu.

Miktar gerçekten değiştiğinde normal Grid kabulünün Tam/Eksik etiketi gerçek Grid gelen miktarıyla yeni talebe göre güncellenir. Miktar düzenlemesi sırasında gerçek gelen, sevk, teslim, kaynak karşılamaları, aktif parti sayaçları, kalite ve süreç kayıtları değiştirilmez. İptal, Grid Kapandı, trafo, saha ve nihai sevk kilitleri açılmaz.

Miktar artışının kalan sevki kilitlemesini yeniden üreten regresyon testleri önce mevcut kodda başarısız çalıştırıldı, ardından düzeltmeyle başarılı oldu. Tekil/toplu Grid ve 3K kombinasyonları, legacy tamamlanmış teslim, bitmemiş partinin blokajı, kaynak karşılama, kalite/sevk kilidi, miktarı geri azaltma ve ondalık miktarlar ayrı doğrulanır. Bu değişiklik **yeni veritabanı kolonu veya SQL scripti gerektirmez**; geçmişte kalmış tutarsız kayıtları kendiliğinden topluca onarmaz.

Buradaki “yeni SQL yok” ifadesi, daha önceki geliştirmelerde eklenmiş kolonların mevcut kurulum/geçiş gerekliliğini kaldırmaz. Bu çalışmada yerel veya canlı veritabanına veri düzeltmesi uygulanmadı.

## Tek komutla çalıştırma

Depo kökünde:

```powershell
./scripts/test/Invoke-IsKurallariTests.ps1 -Configuration Release -Coverage
```

Paketler daha önce indirilmişse ağdan restore gerektirmemek için `-NoRestore` eklenebilir. `-Configuration Debug` ile ikinci yapılandırma da çalıştırılabilir. Kural eşlemesindeki eksik/eski referans, başarısız test veya derleme, scriptin başarısız çıkmasına neden olur; deploy/merge öncesi aynı komut kullanılmalıdır. Çalıştırma sonunda TRX raporunda sıfır test/atlanmış test olmadığı ve eşlemedeki metotların gerçekten başarılı çalıştığı da kontrol edilir. `Birim` satırına test referansı koymamak denetimi geçmez.

Her çalıştırma ayrı bir `artifacts/test-results/<tarih-kimlik>/` klasöründe TRX raporu ve `-Coverage` seçildiyse Cobertura raporu bırakır. Önceki raporlar silinmez. API veya kuyruk worker'ı başlatılmaz; uygulamanın gerçek bağlantı ayarları kullanılmaz. Birim testler gerçek handler/service metotlarını bellek içi bağımlılıklarla çalıştırır. EF model/SQL çeviri testleri gerçek PostgreSQL davranışını kanıtlamaz.

## Kural–test eşlemesi

- [Ortak hesaplar, saha, yetki ve sevkiyat](coverage/ORTAK_SAHA_YETKI_TEST_ESLESMESI.md)
- [Grid, tahsis ve çeki düzenleme/revizyon](coverage/GRID_TAHSIS_TEST_ESLESMESI.md)
- [3K, kaynak, transfer, stok ve iade](coverage/UCK_KAYNAK_TEST_ESLESMESI.md)
- [Eski/yeni işlem yollarının ayrımları](coverage/AKIS_AYRIMLARI_TEST_ESLESMESI.md)

Tablolarda her kural ayrı gösterilir. **Birim** ilgili davranışın doğrudan test edildiğini, **Kısmi** maddenin yalnız belirtilen kısmının doğrulandığını, **Entegrasyon/UI/Açık** ise ek doğrulama ihtiyacını belirtir. **Kapsam dışı**, çalıştırılabilir kural olmayan SQL müdahalesi/politika açıklamasıdır; testli davranış sayılmaz. Dokümanda mevcut davranış olarak tarif edilen şüpheli eski endpoint farkları, sırf test yeşil olsun diye doğru iş kuralına dönüştürülmez.

Eşlemenin eksik/eski referanslarını denetlemek için:

```powershell
./scripts/test/Test-IsKuraliEslemesi.ps1
```

Bu yardımcı kontrol kural ID'lerini ve metot adlarını doğrular; **bir iş kuralının davranış testinin yerini tutmaz**.

## Gelecek düzeltmeler için çalışma kuralı

1. Sorunu gerçek işlem sırasını koruyarak testte yeniden üret; yalnız son DTO değerini taklit etmekle yetinme.
2. Düzeltme öncesinde testin doğru nedenle başarısız olduğunu gör.
3. İlgili tekil/toplu akışları, izin verilen ve reddedilen sınırları birlikte doğrula. Red halinde miktar/stock/transfer kaydı yazılmadığını kontrol et.
4. Miktar artışı/azalışı, sıfır, dört ondalık, çoklu tahsis, legacy NULL takibi, saha ve nihai sevk kilitlerini değerlendir.
5. Kural ve test eşlemesini güncelle. Tüm backend paketini Debug ve Release'te; etkileniyorsa frontend testlerini de çalıştır.
6. SQL/transaction, eşzamanlılık, yetki middleware'i veya PDF/ekran değişiyorsa yalnız birim test sonucu ile canlıya çıkma: ilgili entegrasyon/arayüz doğrulamasını tamamla.

## Birim testinin tek başına güvence veremediği sınırlar

- PostgreSQL transaction geri alma, eşzamanlı işlemler, trigger ve FK davranışı.
- Gerçek JWT/HTTP middleware ve onay işleminin tüm request pipeline'ında çalışması.
- Gerçek kullanıcı/rol yapılandırması, eski veritabanı kayıtlarının doğruluğu ve saha aktarım geçmişi.
- Tarayıcıdaki görünürlük/yenileme, PDF içeriği ve baskı yerleşimi.

Bu sınırlar gizlenmez; eşleme tablolarında ayrı işaretlenir. Birim test kapsamının artması regresyon riskini azaltır, her koşulda hata olmayacağını ispatlamaz.

## Ayrı ele alınması gereken bulgular

- **L-09:** Geçersiz kalite/süreç ID'sinde lookup cache `?` döndürüyor; handler yalnız boş metni reddettiğinden uygulama seviyesinde geçerli-ID kontrolü garanti değil. FK varsa hata veritabanı kaydında çıkabilir. Bu mevcut kabulü doğru davranış sayan bir test eklenmedi ve bu çalışmada ilgili üretim kodu değiştirilmedi.
- **U-125/U-239, G-073/G-101 ve eski endpoint farkları:** Kimlik/durum doğrulaması ile tekil/toplu/legacy yolların farklılıkları eşlemede açık tutuldu. Kabul edilen iş politikasını netleştirip ayrı negatif test ve düzeltme gerektirir.
- **K-050/K-053 ve kısmi işaretlenen diğer bileşik kurallar:** Temel senaryolar testli olsa da revizyon/geri alma/dağıtımın bütün kombinasyonları tamamlanmış değildir. Gerçek DB geri alma, trigger/FK ve eşzamanlılık testleri ayrıca kurulmalıdır.
- **U-178 belge düzeltmesi:** Sandık bazlı geri almada aktif Grid payı geri alınıyorsa mevcut parti Bekliyor açılır; başka sandıktaki fiziksel teslim silinmez. Katalog bu mevcut istisnayı artık açık anlatıyor. Bu, bu çalışmada eklenmiş yeni bir üretim davranışı değildir.
