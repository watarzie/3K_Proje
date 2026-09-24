# Grid / 3K regresyon testleri

Bu dosya, iş kuralı kataloğunun testlerle nasıl doğrulandığını ve hangi sınırların birim testlerinin dışında kaldığını açıklar. Test sayısı, iş kuralı kapsam yüzdesi veya sıfır regresyon garantisi değildir.

Güncel depoda `20260919155111_UretimFinansV2` EF migration'ı ve
`UretimFinansMigrationPostgresTests` K39 testi bulunmaz. Aşağıdaki 19 Eylül
koşum sayıları tarihsel kayıttır; depo dışında yönetilen 02–04 SQL'lerinin
bugünkü halini veya canlıda elle uygulanmasını doğrulamaz. Bu geçiş için
ayrı staging provası ve DBA doğrulaması gerekir.

## Güncel doğrulama — Üretim planı ve çam öngörüsü, 19 Eylül 2026

Son UI/plan düzeltmesinden sonra tam backend paketi **Debug 1.259/1.259** ve **Release + coverage 1.259/1.259** geçti; atlanan/başarısız test yoktur. Katalog referansları ve 501/501 eşlenen metodun başarılı çalışması iki TRX için doğrulandı. Frontend **194/194** ve production build başarılıdır. Bu sayılar aşağıdaki önceki 1.228 vakalık koşumun yerine geçen son doğrulamadır.

Plan öncesi çam ihtiyacı, iptal kaynakları, plan kaydında durum/tarih/snapshot koruması ve form seçimi regresyonları eklendi. Çalışan kullanıcı API'si durdurulmadan ayrı build çıkışıyla koşuldu; standart runner yerine tam `dotnet test` Debug/Release + coverage, TRX sayaç kontrolü ve ayrı kural eşlemesi adımları kullanıldı. Gerçek PostgreSQL testleri yalnız izole sentetik veritabanlarında çalıştı. Uygulama DB'sine dokunulmadı.

[Güncel değişiklik, komut ve kanıt kaydı](../20260919_URETIM_FINANS_UI_DUZELTMESI.md) son TRX/coverage/frontend çıktı yollarını ve doğrulama sınırlarını içerir. Test sayıları tam iş kuralı kapsamı veya sıfır regresyon garantisi değildir.

## Önceki doğrulama — Üretim/Finans V2, 19 Eylül 2026

Üretim/finans ve ayrıntılı yetki değişikliklerinin son backend/test durumuyla tam paket iki yapılandırmada yeniden derlenip çalıştırıldı. Bu sonuç, önceki 1.218 ve 1.227 vakalık ara koşumların yerine geçer; K03/K04, K11, F7 ve son K15/K27/K34/K36 PostgreSQL kabul kanıtlarını da içerir:

| Kontrol | Sonuç |
|---|---|
| Backend Debug | 1.228/1.228 başarılı; 0 atlanan |
| Backend Release + coverage | 1.228/1.228 başarılı; 0 atlanan |
| Katalog ve test referansları | 501/501; eşlenen metotların başarılı çalışması her iki TRX ile doğrulandı |
| Değişmeyen modüllerin regresyon örnekleri | `Grid*` 207/207, `UcK*` 69/69, `Saha*` 63/63 |

Son [Debug TRX](../../artifacts/test-results/20260919-194327-950170ee/is-kurallari.trx), [Release TRX](../../artifacts/test-results/20260919-194407-106a41b0/is-kurallari.trx) ve [Cobertura raporu](../../artifacts/test-results/20260919-194407-106a41b0/2579adfb-b9a7-465f-948a-306498c1e0fe/coverage.cobertura.xml) ayrı yerel çalışma çıktılarıdır. Modül sayıları TRX'teki sınıf adı öneklerine göre alınmış alt kümelerdir; iş kuralı kapsam yüzdesi veya modülün bütün senaryolarının kanıtı değildir. Önceki ara özetteki `Saha*` 65 sayısı, son TRX sınıf öneki sayımıyla 63 olarak düzeltildi. Grid/3K/saha üretim akışları bu çalışma için değiştirilmedi.

Koşumlar `Invoke-IsKurallariTests.ps1 -Configuration Debug -NoRestore` ve ardından `-Configuration Release -Coverage -NoRestore` ile yapıldı. Önceki restore denemesi sandbox'ın kullanıcı `NuGet.Config` dosyasını okuma engeline takılmıştı; nihai koşumdan önce `dotnet restore 3K_Proje.slnx` gerekli yükseltilmiş erişimle başarıyla tamamlandı ve çözümün Release derlemesi geçti. `--no-build` kullanılmadı: SDK güncel kaynakları her iki yapılandırmada derledi. Debug derlemesinde görev dışındaki `SandikService.cs:48/51` ve `StokService.cs:29` için üç mevcut CS8602 uyarısı bulunur; derleme hatası yoktur. Her iki TRX'te toplam/çalışan/başarılı sayısı 1.228, başarısız/atlanmış/zaman aşımı sayısı sıfırdır.

Son ek kabul kanıtları:

- [AmbalajUretimV2Tests](../../3K.Application.Tests/AmbalajUretimV2Tests.cs) ve [AmbalajUretimV2PostgresTests](../../3K.Application.Tests/AmbalajUretimV2PostgresTests.cs): toplam 23 vaka. `K03K04_KaynakVarsayilanHaric_YetkiliKararResyncteKorunur_FormaDahilOlur` kaynakların varsayılan hariçliğini ve yetkili kararın yeniden senkronizasyonda korunmasını; `K11_UcCins_185Ahsap26Kontra14Katlanir_FinansManuelM3UretimeKarismaz` üç cins toplamını ve finans ölçüsünün üretime karışmamasını sınar. PostgreSQL form eşzamanlılığı, snapshot/FK ve finans hatasında atomik rollback ayrıca çalışır.
- [FinansV2PostgresTests](../../3K.Application.Tests/FinansV2PostgresTests.cs): 9 gerçek PostgreSQL vakası ve 6 yaşlandırma sınır vakası. `CokProjePo_7000_3000Fatura_SarfAcikTamamlanmaz_PoZorunlu_TarihYaslandirmayiDegistirmez` K12/K19/K21/K22/K29 için aktif PO zorunluluğunu, 10.000 PO'nun 7.000 faturası sonrası 3.000 bakiyesini, çok projeli PO'ya iki faturayı, açık sarf nedeniyle tamamlanmayan projeyi ve finans tarihi değişince yaşın korunmasını doğrular. `IkiWorkerEszamanliCatchup_TarifeTarihSnapshotlariVeDortFiyatYontemi` K26/F4/K17 için iki ayrı açık bağlantı/DbContext'in birlikte başlatılmasını, serializable yarışında güvenli yeniden denemeyi, tekil dönem üretimini, 31. gün telafisini, dönem tarihine göre tarife seçimini, eski fiyatların korunmasını ve dört yöntemde fiziksel miktar uydurmayan tutar bazlı kısmi PO'yu doğrular.
- Aynı sınıftaki `K15_GenelArama_SeciliAyDisindakiProjePoVeFaturayiBulur_DigerFiltreleriKorur`, seçili ay dışında kalan proje/PO/faturanın genel aramada bulunmasını, diğer filtrelerin korunmasını ve iş/gider filtrelerinde `%`, `_`, `\` karakterlerinin gerçek metin olarak aranmasını doğrular. Bu test önce `tr-TR` uygulama kültürüyle PostgreSQL küçültmesi arasındaki I/ı farkında başarısız olmuş; parametreli, jokerleri kaçırılmış `ILIKE` düzeltmesinden sonra geçmiştir. [FinansModuluTests](../../3K.Application.Tests/FinansModuluTests.cs) içindeki `Aylik_arama_bir_alt_satira_eslesince_ayni_proje_biriminin_tum_satirlarini_toplama_dahil_eder` de aynı sonuç beklentisini koruyarak bellek içinden gerçek izole PostgreSQL'e taşındı. Böylece bu iki sınıftaki finans kabul akışı 10 gerçek PostgreSQL vakası içerir; 6 yaşlandırma sınır testi bu sayıya dahil değildir.
- `SablonSurumu_Bilesenler_BelgeSurumu_ve_MigrationIdempotency` K27 için farklı m³/fiyatlı iki bileşeni (`1,2×500`, `0,5×800`) ve sabit 200 ile toplam 1.200'ü; zorunlu dinamik alan eksikken kayıt/audit değişmemesini; eski şablon sürümü, alan/bileşen ve manuel net snapshot'ının yeniden kaydedilirken korunmasını doğrular. `TutarRevizyonu_GerceklestirilenTutarinAltinaInemez_FaturaAsimiRollback` K34 için faturayı iptal edip 7.000 bekleyen fatura bakiyesinin geri gelmesini, belgenin geçmişte korunmasını ve gerçek aktör/tarih/gerekçe/eski-yeni audit alanlarını sınar. `CokProjePo_MixedCurrencyRollback_PdfAuditFailOrphanYok` K36 için ortak PO'ya bağlı işin önizlemede engellenmesini, doğru sürüm/ikinci onay/gerekçe ile zorlanan silmenin de reddini ve iki projenin PO satırları, iş kayıtları ile audit'in değişmemesini doğrular.
- [FinansOzelIsContractTests](../../3K.Application.Tests/FinansOzelIsContractTests.cs): 3 vaka. Ham JSON → gerçek controller → MediatR komutu adaptöründe serbest iş türü ve opsiyonel talep eden kişi/bölümün korunması (F7) ile manuel net tutarın yetkisiz JSON'da null olması doğrulanır. Bu adaptör testi tek başına HTTP model binding/authorization veya veritabanı kalıcılığı kanıtı değildir.

Yeni güvenlik kanıtları `GranularYetkiTests`/`GranularYetkiPostgresTests` (19), `FinansAlanHttpSecurityTests`/`FinansAlanProjeksiyonuTests` (31) ve `AmbalajAlanMaskelemeTests` (8) vakasında yer alır. Gerçek JWT/Kestrel/MediatR/MVC zinciri; sahte `X-Menu-Kod`, kök modül reddi + açık alt izin, aynı token ile izin kaldırılması, parasal/ölçü/m³/sarf alanlarında null, iç içe audit/string alanları ve yetkisiz binary indirme 403 davranışını doğrular. Finans veri servisi/izin deposu HTTP testinde sentetiktir; kalıcı override/rol/audit davranışı ayrıca gerçek PostgreSQL'de sınanır. Canlı kullanıcıyla tarayıcı kabulünün yerine geçtiği iddia edilmez.

19 Eylül'deki eski koşumda `UretimFinansMigrationPostgresTests`, K39 için o tarihteki `20260919155111_UretimFinansV2.Up` SQL operasyonlarını tek transaction içinde çalıştırmıştı (3/3). V1-benzeri sentetik şemada 01–04 sırası, eski belge snapshot yedeği ve kontrollü mutabakat, üretim tarihini koruma, legacy miktar bayrağı, tekrar çalıştırmada audit/izin iptalini koruma, kapasite hatasında önceki DDL dahil geri alma ve eksik temel şemada durma sınanmıştı. İlk koşum SQL02 lookup INSERT'indeki eksik `CreatedDate` nedeniyle başarısız olmuş, düzeltmeden sonra yeni lookup ve tekrar yolu geçmişti. Bu test ve EF V2 migration'ı artık depoda yoktur; sonuçlar depo dışındaki güncel SQL dosyalarının veya DBA'nın ayrı transaction'larla yaptığı uygulamanın kanıtı değildir.

`TransactionConcurrencyMappingTests` doğrudan ve sarmalanmış PostgreSQL `40001`/`40P01` hatalarını ve geçici olmayan hataların ayrımını 11 vaka ile sınar. Gerçek PostgreSQL taşıma ve finans eşzamanlılık/rollback testleri de tam koşumda çalışmıştır. `THREEK_TEST_POSTGRES` yalnız `127.0.0.1:55439` üzerindeki sentetik sunucudur; her sınıf kendi benzersiz DB'sini kurup kaldırır. Uygulama veritabanına SQL uygulanmadı.

Bu son koşumun assembly satır/dal kapsamı Application %57,81/%54,24, Core %73,96/%82,40, Infrastructure %19,99/%41,66 ve API %3,94/%10,10'dur. Bunlar bütün assembly'lere aittir; V2 kabul maddelerinin veya 501 katalog maddesinin tamamının doğrulandığı anlamına gelmez. V2 kabul eşlemesi [uygulama kaydında](../20260919_URETIM_FINANS_V2.md), alan/rol politikası [güvenlik notunda](../guvenlik/GRANULAR_YETKI_20260919.md) tutulur. Frontend güncel doğrulaması ayrı teslim kaydında raporlanır; aşağıdaki 82/82 sonucu önceki değişikliğe aittir.

## Önceki ek doğrulama — 19 Eylül 2026

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

Her çalıştırma ayrı bir `artifacts/test-results/<tarih-kimlik>/` klasöründe TRX raporu ve `-Coverage` seçildiyse Cobertura raporu bırakır. Önceki raporlar silinmez. Üretim Program'ı veya kuyruk worker'ı başlatılmaz; uygulamanın gerçek bağlantı ayarları kullanılmaz. HTTP güvenlik testleri localhost'ta sentetik bağımlılıklarla izole test host'u açar. Birim testler gerçek handler/service metotlarını bellek içi bağımlılıklarla çalıştırır; PostgreSQL entegrasyon sınıfları ayrıca gerçek sentetik DB kullanır. EF model/SQL çeviri testleri tek başına gerçek PostgreSQL davranışını kanıtlamaz.

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
