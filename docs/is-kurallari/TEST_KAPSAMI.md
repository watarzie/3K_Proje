# Grid / 3K regresyon testleri

Bu dosya, iş kuralı kataloğunun testlerle nasıl doğrulandığını ve hangi sınırların birim testlerinin dışında kaldığını açıklar. Test sayısı, iş kuralı kapsam yüzdesi veya sıfır regresyon garantisi değildir.

Güncel depoda `20260919155111_UretimFinansV2` EF migration'ı ve
`UretimFinansMigrationPostgresTests` K39 testi bulunmaz. Aşağıdaki 19 Eylül
koşum sayıları tarihsel kayıttır; depo dışında yönetilen 02–04 SQL'lerinin
bugünkü halini veya canlıda elle uygulanmasını doğrulamaz. Bu geçiş için
ayrı staging provası ve DBA doğrulaması gerekir.

## Barkod etiketi (C-16) — 26 Ekim 2026

Değişiklik henüz commit edilmedi.

**Davranış.**
- Yeni ekran `/barkod-etiket`, `barkod-etiket` R ister (Id 5262, rotasız, Sandık Yönetimi altında). Ekran Projeler ve 3K sandık listesinin "Diğer" menüsünden açılır. Bir projenin seçilen sandıkları (100×75 mm) ve ürünleri (60×40 mm) için Grid etiketi PDF ya da Godex EZPL (`.prn`) olarak üretilir; hiçbir kayıt değişmez.
- Barkodlar: sandık barkodu `Sandik.Id`'nin 12 haneli sıfır dolgusudur. Ürün barkodu `BarkodNo`'nun kırpılmış, büyük harfli, 16 haneye soldan sıfır dolgusudur. Basılamayan ürün barkodu (boş, 16'dan uzun, ASCII dışı, `^`/`~`) listede seçilemez, istekte 400 alır.
- Alanlar: FB NO (boşsa proje no), CASE NO, CLIENT, DESTINATION, AA/YY tarih ve çeki açıklamasının TANIM/DESCRIPTION ayrımı. REMARKS ekranda düzenlenir, boşsa "-". Etiket metni ASCII'ye çevrilir.
- Listeler sunucuda sayfalıdır. Sınırlar: 200 seçim, adet 1–100, toplam 1000 etiket, REMARKS 60. Projeye ait olmayan seçim 404 alır.
- Ürünler çeki satırları ile çeki satırı olmayan sandık içerikleridir (elle eklenen malzemeler). Seçim (kaynak, kimlik) ile yapılır.

**Kapsam sınırı.** İzin `scripts/database/20261026_01_Barkod_Etiket_Yetkisi.sql` ile kurulur (README sıra 29; şema değişikliği yok). Eşleme satırı `coverage/ORTAK_SAHA_YETKI_TEST_ESLESMESI.md` dosyasındadır: C-16 Birim. Ambalaj ve Finans'ta yeni kural yoktur.

- [BarkodEtiketTests](../../3K.Application.Tests/BarkodEtiketTests.cs) (10 metot; 9'u bellek içi, 1'i izole PostgreSQL):
  - Code 128 kodlaması (alt küme seçimi, kontrol sembolü, desen tablosu).
  - Ürün barkodu kuralı (Theory, 9 vaka) ve sandık barkodu, ASCII çevirisi, REMARKS, açıklama ayrımı, FB NO, tarih, adet sınırları.
  - EZPL sandık etiketinin şablonla satır satır aynı olması; ürün etiketinin şablon öğeleri ve yeni başlığı.
  - PDF'in adet kadar sayfa üretmesi.
  - Doğrulayıcılar ve altı isteğin `barkod-etiket` R yetkisi (gerçek `AuthorizationBehavior` ile 403).
  - `Postgres_ListelerSayfaliSecimProjeyeAitDosyaUretilir` (`THREEK_TEST_POSTGRES` ister): sayfalı listeler, sıra ve süzgeçler, dosya içeriği, başka projeye ait seçimde 404, basılamayan barkodda 400.
- [CanliyaAlimBetikleriTests](../../3K.Application.Tests/CanliyaAlimBetikleriTests.cs) (izole PostgreSQL, 1 test): betik 29'u da çalıştırır. İzin düğümünü (5262, rotasız, son sıra) ve rol izinlerini doğrular; tekrarda değişmezliği sınar.

**Koşum (Release, 26 Ekim 2026, dokümantasyon oturumu, PostgreSQL'siz):**
- Filtre `FullyQualifiedName~BarkodEtiketTests|FullyQualifiedName~CanliyaAlimBetikleriTests` (`dotnet test ... -c Release`, derleme dahil) ile 20 test seçildi: 18 başarılı, 0 başarısız, 2 atlanan.
  - Başarılı: `BarkodEtiketTests`'in bellek içi 9 metodu (18 vaka; Theory 9 vaka).
  - Atlananlar: `BarkodEtiketTests.Postgres_ListelerSayfaliSecimProjeyeAitDosyaUretilir` ve `CanliyaAlimBetikleriTests.Postgres_Betik_SemayiModeleGetirir_TekrarindaTasimaIziniKorur`. Oturumda `THREEK_TEST_POSTGRES` tanımlı değildi.
- Kural–test eşleme doğrulayıcısı (`scripts/test/Test-IsKuraliEslemesi.ps1`): 548 kural doğrulandı (önceki 547 + C-16). `-TrxPath` kipi kullanılmadı.

**Koşulmayanlar:**
- İki PostgreSQL testi bu oturumda koşulmadı (atlandı). Liste, sayfa, 404/400 ve betik davranışı bu oturumda gerçek veritabanında doğrulanmadı.
- Tam paketler (.NET ve ön yüz) koşulmadı. Ön yüz spec'i (`barkod-etiket.component.spec.ts`) bu oturumda koşulmadı.
- HTTP ve tarayıcı akışı, gerçek Godex yazıcıda baskı ve canlı veri (`3K_Db`) denenmedi.

**Açık kalanlar (C-16):**
- Salt okunurluk test edilmiyor: servis yazma yapmıyor ama hiçbir testte kaydın değişmediği ölçülmüyor.
- 1000 etiketlik toplam sınırının tam sınırda kabulü testsiz. Proje listesinin sırası (en yeni önce) testsiz. Aramanın Türkçe İ/ı ile büyük/küçük harf duyarsızlığı (SQL `upper`) testsiz.

## Finans: katlanır sandık elle fiyatlanır — 26 Ekim 2026

Katlanır sandığın kontrplak mantığıyla (ahşap/kontra m³ × tarife) fiyatlanması kaldırıldı; sistem katlanırda tarife, m³ veya ölçüden hesap yapmaz (`FinansKatlanirKurali`). Diğer sandık tipleri değişmedi. Aşağıdaki maddeler 13 Ekim "Malzemeli tarife" ve 10 Ekim "Kontrplak hacmi" bölümlerindeki katlanır ifadelerinin yerine geçer.

- Ambalaj aktarımında katlanır iş fiyatsız açılır (adet yöntemi, birim fiyat 0, ürünsüz); katlanıra özel eski eşleşme dahil hiçbir tarife kullanılmaz (`FinansFiyatBelirleyici.OtomatikFiyatUygulaAsync`). Bedel 0 olduğu için fiyat girilmeden PO açılamaz.
- Fiyat işin Fiyatlandırma sekmesinden girilir: adet × birim fiyat veya net toplam (bileşenler m³ satırı içeremez); m³ ve sabit tutar yöntemi 409'dur. Para birimi tablodan seçilir, KDV elle girilir. Girilen tutar `FinansTutarKurallari.IsNet` ile olduğu gibi kullanılır; PO, fatura, panel ve raporlar bu tutarla çalışır. Kayıtta m³ tutulmaz (birim/toplam m³ ve elle ahşap/kontra payı temizlenir).
- Gerekçe katlanırda isteğe bağlıdır (yazılırsa değişiklik geçmişine "Gerekçe" satırı eklenir); diğer işlerde zorunluluk sürer ve boş gerekçe artık doğrulayıcı yerine serviste 400 ile reddedilir. Alan değişiklikleri her durumda denetim kaydına yazılır.
- Katlanırda elle hacim girişi 409'dur (eski kayıtta elle hacim girilmiş olsa da); malzemesi "bilinmiyor"dur ve yeni katlanır işin m³ payı (0, 0)'dır. Kontrplak gibi m³ ile fiyatlanmış eski katlanır kaydın m³'ü gösterimde (iş detayı, aylık/proje kırılımı, PO) eskisi gibi kontra payı olarak kalır (`FinansHacimKurallari.Hacim`).
- Tarifeyle fiyatlanmış eski katlanır kayıtların fiyatı korunur (veri taşınmadı, betik yok); yeniden fiyatlama, geriye dönük doldurma (sorguda `SandikCinsi` ile, cinsi boş eski kayıtta tip metniyle elenir) ve tarife düzeltme ("Katlanır sandık" koruma nedeni) onları değiştirmez. Saha/yedek katlanır sandık da bu kurala dahildir (kural iş türünden bağımsız).
- Yeni aktif "Katlanır Sandık" tarife eşleşmesi tanımlanamaz (doğrulayıcı 400). Önceden kaydedilmiş eşleşme ürünün diğer güncellemelerini kilitlemesin diye reddedilmez; fiyatlamada yok sayılır. Ön yüz tarife formunda katlanır seçeneği yoktur.
- Ön yüz: `FinansIsKaydi.elleFiyatlanir`; iş detayında fiyatsız katlanır için "Fiyatı gir" uyarısı, Fiyatlandırma'da yalnız Adet/Manuel net toplam, m³ bileşeni ve hacim kartı yok, gerekçe isteğe bağlı; Ambalaj yaşam döngüsünde katlanır için "fiyat Finans'ta elle girilir" notu.

- [FinansKatlanirFiyatTests](../../3K.Application.Tests/FinansKatlanirFiyatTests.cs): katlanır tanıma (cins/tip metni), yöntem ve bileşen kuralı, malzeme/m³ payı, hacim temizliği ve net, tarife düzeltme koruması, eşleşme doğrulayıcısı, fiyatlandırma gerekçesinin doğrulayıcıda isteğe bağlı olması.
- [FinansPostgresTests](../../3K.Application.Tests/FinansPostgresTests.cs) `KatlanirSandik_TarifeVeM3Hesaplanmaz_ElleFiyatPoVeFaturadaAynenKullanilir` (PostgreSQL): eski katlanır eşleşmesi ve genel tarifeler varken fiyatsız aktarım, aktarımdan sonra tanımlanan tarifenin (geriye dönük doldurma) fiyatsız katlanırı fiyatlamaması, fiyatsız PO reddi, hacim/m³/m³ bileşeni reddi ve kaydın değişmemesi, gerekçesiz adet × birim fiyat (m³ 0), net toplam düzeltmesi ve gerekçe geçmişi, eski tarifeli kaydın yeniden fiyatlamada korunması, PO + fatura ve SQL tutar ifadesinin aynı bedeli vermesi, kontrplakta boş gerekçenin 400 olması.
- Güncellenen: `FinansPoFaturaKurallariTests.Elle_hacim_yalniz_kontrplak_sandikta_girilir_katlanirda_girilmez`, `FinansUrunTarifeTests` (malzemeli eşleşme örneği kontrplak), `FinansPostgresTests.MalzemeliTarife_AhsapVeKontraM3KendiTarifesiyle_KismiPoOranli` (katlanırsız; kısmi PO kontrplakla).
- Ön yüz spec'leri: `finans-kayit-detay` ("Katlanır sandık (elle fiyat)"), `finans-urun-kaydet`.

Koşum (Release, izole 127.0.0.1:55439 sunucusunda): `FinansKatlanirFiyatTests|FinansPoFaturaKurallariTests|FinansUrunTarifeTests|FinansTarifeDuzeltmeTests|FinansTutarKurallariTests|FinansMunferitIsTests|FinansModuluTests|FinansPostgresTests|FinansPoOnayPostgresTests` → 183 başarılı, 0 atlanan, 1 başarısız: `FinansPoOnayPostgresTests.GecisBetigi_…` bu dalda bulunmayan `scripts/database/20261017_01_Finans_Po_Is_Onayi.sql` betiğini aradığı için (klasör 18 Ekim'de bilerek silindi; bu değişiklikten bağımsız). Ön yüz: finans + ambalaj yaşam döngüsü + finans servis spec'leri 241/241, development derlemesi temiz. Ekran canlı veriyle görsel olarak denenmedi. Tam paketler koşulmadı.

## Sandık açılış/kapanış geçmişi (C-11–C-15) — 24 Ekim 2026

Değişiklik henüz commit edilmedi.

**Davranış.**
- **Kapanma tarihi (C-11):** `Sandik.KapanmaTarihi` sandık Kapandı'ya girince `TurkeyTime.Now` ile yazılır. Kapandı → Sevk Edildi geçişinde korunur. Sevkli sandığın kilidi açılıp sevk öncesi Kapandı'ya dönmesi yeni kapanma sayılmaz, tarih değişmez. Hazırlanıyor'a ya da Boş'a dönünce tarih boşalır. Tarih 3K sandık kartında, Sandık Yönetimi tablosunda ve sandık detayında gösterilir.
- **Eksik uyarısı (C-12):** Sandık listesi `EksikUrunSayisi` ve `EksikAdet` döner. Hesap, sandık detayındaki satır eksiğiyle aynı fonksiyondur (`SandikTahsisHelper.HesaplaIcerikMiktari`): Grid Kapandı satır eksik sayılmaz; eksik = max(sandık miktarı − konulan, 0).
- **Geri açma sebebi (C-13):** Kapalı sandığı elle Hazırlanıyor'a almak (`ProjeIslemleri.SandikKapatCommand`, `Kapali=false`) sebep ister. Sebepler `LookupSandikAcilmaSebepleri` kayıtlarıdır: "Trafo kurulum", "Tadilat". Not isteğe bağlıdır, en fazla 500 karakterdir. Sebep yoksa ya da bulunamazsa istek 400 alır ve sandık değişmez.
- **Açılış/kapanış olayları (C-14):** Her kapanma ve geri açılma `SandikDurumOlaylari`'na yazılır. Tek yazım noktası `SandikDurumOlayInterceptor`'dır. Elle ve toplu handler'lar olayı kaynak ve sebeple kendisi ekler. Diğer yollar "Otomatik" kaynaklıdır. Kapandı → Sevk Edildi geri açılma sayılmaz. Sandık silinince iz kalır. Betik 27 eski hareket geçmişini "Geçmişten aktarılan" kaynağıyla bir kez aktarır.
- **İstatistik (C-15):** Ekran `sandik-acilis-istatistigi` R ister. Tarih aralığı, proje ve müşteri süzgeci vardır. Ekranda özet, sebep dağılımı ve en çok açılan 20 sandık; proje, müşteri, kişi ve olay tabloları sunucuda sayfalıdır (25/50/100); Excel çıktısı alınır. Sandığın zaman çizelgesi `GET api/sandik/{sandikId}/durum-gecmisi` ucundadır.

**Kapsam sınırı.** Şema, tohum, geçmiş aktarımı ve izin `scripts/database/20261024_03_Sandik_Acilis_Kapanis_Gecmisi.sql` ile kurulur (README sıra 27, izin düğümü Id 5261). Eşleme satırları `coverage/ORTAK_SAHA_YETKI_TEST_ESLESMESI.md` dosyasındadır: C-11 ve C-13 Birim, C-12 Kısmi, C-14 ve C-15 Entegrasyon. Ambalaj ve Finans'ta yeni kural yoktur.

- [SandikAcilisKapanisTests](../../3K.Application.Tests/SandikAcilisKapanisTests.cs) (bellek içi, 14 metot / 31 vaka):
  - Olay türü matrisi ve yeni Kapandı sandık.
  - Kapanma tarihi kuralı.
  - Geri açma hatası (sebep, not sınırı) ve Türkçe harf duyarsız arama.
  - Komut ve istatistik doğrulayıcıları.
  - Sebepsiz ya da bulunamayan sebeple geri almada sandık, olay ve hareketin değişmemesi.
  - Sebepli geri açmanın olay ve hareketi; kapatmada sebebin yok sayılması; durum değişmezse olay yazılmaması.
  - Tekil kapatmanın Elle, toplu kapatmanın Toplu kaynağı.
  - Liste ve detayın ortak eksik hesabı.
- [SandikAcilisKapanisPostgresTests](../../3K.Application.Tests/SandikAcilisKapanisPostgresTests.cs) (izole PostgreSQL, 2 test, `THREEK_TEST_POSTGRES` ister; üretimdeki interceptor zinciriyle: olay → denetim → proje kilidi):
  - Interceptor'ın izlenmeyen `Update`'te Otomatik olay ve kapanma tarihi yazması.
  - Sevkte ve kilit açmada olay yazılmaması, tarihin korunması.
  - Sebepli elle geri açmada tek olay.
  - Zaman çizelgesi, istatistik kırılımları ve süzgeçler, Excel'in 7 sayfası.
  - Sandık silinince izin kalması.
  - Zorla toplu kapatmanın izlenen sandıklarla çalışması (Toplu kaynaklı olay ve kapanma tarihi) ve yeni projeyle kapalı eklenen sandığın tek Otomatik olayı.
- [CanliyaAlimBetikleriTests](../../3K.Application.Tests/CanliyaAlimBetikleriTests.cs) (izole PostgreSQL, 1 test): betik 27'yi de çalıştırır.
  - Şemanın EF modeliyle aynı olması.
  - Tohum sebepler, izin düğümü (5261) ve rol izinleri.
  - Eski hareketlerin (arşiv dahil, kullanıcısı 0 olan dahil) bir kez aktarılması; silinmiş sandık ve sayısal olmayan referansın atlanması; kapalı ve sevkli sandığın kapanma tarihinin doldurulması.
  - Tekrar çalıştırmada değişmezlik: dolu kapanma tarihi korunur, Hazırlanıyor sandıktaki bayat tarih boşaltılır, olay tablosu doluyken aktarım yapılmaz.

**Koşum (Release, 24 Ekim 2026, dokümantasyon oturumu, PostgreSQL'siz):**
- Komut `dotnet test 3K.Application.Tests/3K.Application.Tests.csproj -c Release` ile derleme dahil koşuldu (test DLL'i 22:49:42; değişen kaynakların en yenisi 22:47:09).
- Filtre `FullyQualifiedName~SandikAcilisKapanis|FullyQualifiedName~CanliyaAlimBetikleriTests|FullyQualifiedName~OrtakSandikRaporIsKurallariTests` ile 54 test seçildi: 52 başarılı, 0 başarısız, 2 atlanan.
  - Başarılı: `SandikAcilisKapanisTests` 31 ve `OrtakSandikRaporIsKurallariTests` 21. İkincisi C-01–C-04 kapatma ve eksik raporu testleridir; kapatma handler'ları değiştiği için koşuldu.
  - Atlananlar: `SandikAcilisKapanisPostgresTests`'in 1 testi ve `CanliyaAlimBetikleriTests`'in 1 testi. Oturumda `THREEK_TEST_POSTGRES` tanımlı değildi.
- Kural–test eşleme doğrulayıcısı (`scripts/test/Test-IsKuraliEslemesi.ps1`): 547 kural doğrulandı (önceki 542 + C-11–C-15). `-TrxPath` kipi kullanılmadı; bu kip bütün eşlenen testlerin raporda bulunmasını ister, alt küme TRX'iyle geçmez.

**Koşum (Release, 24 Ekim 2026, inceleme sonrası, izole PostgreSQL 55439):**
- `postgres-test` ile filtre `FullyQualifiedName~SandikAcilisKapanis|FullyQualifiedName~CanliyaAlimBetikleriTests|FullyQualifiedName~OrtakSandikRaporIsKurallariTests|FullyQualifiedName~LookupOnbellekPostgresTests|FullyQualifiedName~SevkSonrasiDuzeltmePostgresTests|FullyQualifiedName~SahadanSahayaTasimaPostgresTests|FullyQualifiedName~ServerAuthorizationRegressionTests|FullyQualifiedName~ApiYanitSozlesmesiTests`: 133 başarılı, 0 başarısız, 0 atlanan.
- Ön yüz: `ng test --include` (sandık durum modeli, geri açma penceresi, istatistik ekranı, sandık detayı, proje listesi) 38/38; `ng build` başarılı.

**Koşulmayanlar:**
- Tam paketler (.NET ve ön yüz) koşulmadı; 3K sandık kartının (`uck-sandiklar`) spec'i yoktur, kart yalnız derlemeyle doğrulandı.
- HTTP ve tarayıcı akışı denenmedi. Canlı veride (`3K_Db`) denenmedi; betiğin canlı veritabanı kopyasında provası yok.

**Açık kalanlar:**
- `GetProjeSandiklariQueryHandler`'ın `EksikUrunSayisi`/`EksikAdet` toplaması handler düzeyinde testsiz (C-12).
- Otomatik olay üreten yollar (ürün güncelleme, taşıma, saha aktarımı, Grid'de otomatik kapanma G-030) tek tek sınanmadı. Proje silinince olayların silinmesi ve kullanılan sebebin silinememesi de testsiz (C-14).
- `sandik-acilis-istatistigi` R'nin pipeline'da 403 üretmesi, `durum-gecmisi` ucunun sandık okuma izni ve 20/50.000 sınırları testsiz (C-15). Sayfalama (sayfa/boyut sınırı, son sayfaya çekme, sayfaların sırayla bütün olayları vermesi) `SandikAcilisKapanisPostgresTests` ve `SandikAcilisKapanisTests.SayfaKurali_*` ile sınanır.

## Sevk sonrası düzeltme (L-12–L-17, K-034) — 23 Ekim 2026

Değişiklik henüz commit edilmedi.

**Davranış.** Sevkli sandık "Sevkiyat kaydı korunsun" ile düzeltmeye açılınca gerçekte gitmiş ama kaydedilmemiş ürün eklenir, yanlış ürün silinir ya da düzeltmedeki sandıklar arasında taşınır. Sevkiyat kaydı korunur.
- Korunarak açma (sandık ve proje) gerekçe ister. Proje tipinin izniyle birlikte ayrı `sevk-sonrasi-duzeltme` W iznini ister (L-12). Geri alınarak açma ve sandık ekleme bu izni istemez.
- Açılıştaki içerik `SandikDuzeltmeKayitlari`'na yazılır; tamamlanınca "önce → sonra" farkı kayda ve hareket geçmişine eklenir. `GET api/sandik/{sandikId}/sevk-sonrasi-duzeltme` durumu ve geçmişi döner (L-13).
- Proje kilidi düzeltmedeki sandıklar için unutulan ürünü, ürün silmeyi ve düzeltmedeki iki sandık arası taşımayı açar. Ana veri, kilitli sandık, başka projenin sandığı/çekisi/satırı ve aktif saha kaynağı kapalı kalır (L-14).
- Düzeltmedeki sevkli sandık taşıma, ürün güncelleme, çeki satırı silme ve Grid Trafo Sevk ile "Sevk Edildi"den çıkmaz. Lokasyonu ürün ve 3K/Grid işlemleriyle otomatik değişmez. Grid manuel ürün sevkli sandığa eklenmez (L-15). Taşıma kuralı K-034 bu yüzden değişti: düzeltme açıksa sevkli sandık taşımaya katılır.
- Düzeltme sandık eklemeyi ve silmeyi açmaz (L-16, kullanıcı kararı). Bu yetenek bir ara sürümde vardı, aynı gün kaldırıldı. Yeni ve silinen sandık kilitli projede düzeltme açıkken de reddedilir; sonradan sandık gerekiyorsa projeye normal yolla eklenir.
- Sahaya aktarılmış ürün düzeltmede de değişmez; sandık içeriği hedef saha projelerini döner (L-17).

**Kapsam sınırı.** Şema ve izin `scripts/database/20261023_02_Sevk_Sonrasi_Duzeltme.sql` ile kurulur (README sıra 24). Betik tabloyu kurar ve izin düğümünü ekler (Id 5259); izni yalnız Admin W alır. Ambalaj ve Finans'ta yeni kural yoktur. Eşleme satırları `coverage/ORTAK_SAHA_YETKI_TEST_ESLESMESI.md` (L-12, L-14, L-16 Birim; L-13, L-15 Kısmi; L-17 Entegrasyon) ve `coverage/GRID_TAHSIS_TEST_ESLESMESI.md` (K-034) dosyalarındadır. Ön yüz: `sandik-detay` (düzeltme paneli, saha bağlantısı; `sandik-detay-duzeltme.spec.ts`), `uck-sandiklar` ve `proje-listesi` kilit açma pencereleri.

- [SevkSonrasiDuzeltmeTests](../../3K.Application.Tests/SevkSonrasiDuzeltmeTests.cs) (bellek içi, 4 metot / 7 vaka): saf fark kuralı (değişiklik yok, eklendi/çıkarıldı/değişti ve sandık bilgisi, bölünmüş tahsis tek ürün). Gerekçe doğrulayıcısı iki komutta korunarak açmada boş/boşluk gerekçeyi reddeder, geri alınarak açmada istemez.
- [SevkSonrasiDuzeltmePostgresTests](../../3K.Application.Tests/SevkSonrasiDuzeltmePostgresTests.cs) (izole PostgreSQL, 4 test, `THREEK_TEST_POSTGRES` ister):
  - Kilit aç, ürün ekle, taşı ve tamamla akışında sevkiyat korunur, iki sandığın kaydına fark yazılır (2 kayıt).
  - Düzeltmede ürün güncelleme, çeki satırı silme ve Belirsiz lokasyonlu sandık 1'e unutulan ürün sandıkları Sevk Edildi bırakır. Lokasyon ve sevkiyat bağı değişmez. Grid manuel ürün 409 alır.
  - Sahaya aktarılmış ürün düzeltmede değişmez ve saha projesi gösterilir.
  - Düzeltme açık değilken doğrudan yazım proje kilidine takılır.
- [ProjectLockCorrectionScopeTests](../../3K.Application.Tests/ProjectLockCorrectionScopeTests.cs): 8 yeni `Duzeltme*` metodu interceptor kapsamını sınar: unutulan ürün, başka projenin satırına tahsis, ürün silme, taşıma, başka proje ve aktif saha kaynağı. `Duzeltme_SandikEklemeVeSilmeyiAcmaz` (ekle / boş sandık sil) düzeltmede de `ProjectLockedException` bekler.
- [ServerAuthorizationRegressionTests](../../3K.Application.Tests/ServerAuthorizationRegressionTests.cs): `SevkSonrasiDuzeltme_AyriDuzeltmeIzniIster` (sandık ve proje, 4 vaka) ve `SevkiyatGeriAlinarakAcma_DuzeltmeIzniIstemez`.
- [SandikTasimaKatalogKurallariTests](../../3K.Application.Tests/SandikTasimaKatalogKurallariTests.cs) ve [SandikTopluTasimaTests](../../3K.Application.Tests/SandikTopluTasimaTests.cs): taşıma testleri yeni davranışa göre yeniden adlandırıldı. Düzeltme kapalıyken 409, açıkken sevkli sandık taşımaya katılır ve sevk durumu korunur (K-034).
- [SevkiyatKilidiAcRegresyonTests](../../3K.Application.Tests/SevkiyatKilidiAcRegresyonTests.cs): `ZatenAcikDuzeltme_TekrarTalepteGereksizGuncellemeOlusturmaz` düzeltme kaydı sorgusuyla birlikte geçer.
- [CanliyaAlimBetikleriTests](../../3K.Application.Tests/CanliyaAlimBetikleriTests.cs) (izole PostgreSQL, 1 test): betik 24'ü de çalıştırır. Düğüm (Id 5259, üst 5, `MENU.SEVK_SONRASI_DUZELTME`) ve yalnız Admin W izni kurulur; tekrarında değişmez.

**Koşum (Release, 23 Ekim 2026, dokümantasyon oturumu, PostgreSQL'siz, sandık ekleme/silme kaldırıldıktan sonra):**
- Komut `--no-build` ile koşuldu. Kullanılan Release çıktısı (14:01:41) değişen kaynak dosyaların en yenisinden (14:01:30) sonra derlenmişti; bu oturumda yeniden derleme yapılmadı.
- Filtre `FullyQualifiedName~SevkSonrasiDuzeltme|FullyQualifiedName~ProjectLockCorrectionScopeTests|FullyQualifiedName~SevkiyatKilidiAcRegresyonTests|FullyQualifiedName~ServerAuthorizationRegressionTests|FullyQualifiedName~SandikTasimaKatalogKurallariTests|FullyQualifiedName~SandikTopluTasimaTests|FullyQualifiedName~CanliyaAlimBetikleriTests`: toplam 166; 161 başarılı, 0 başarısız, 5 atlanan.
  - Sınıf bazında başarılı: `ProjectLockCorrectionScopeTests` 41, `ServerAuthorizationRegressionTests` 63, `SevkiyatKilidiAcRegresyonTests` 28, `SandikTasimaKatalogKurallariTests` 14, `SandikTopluTasimaTests` 8, `SevkSonrasiDuzeltmeTests` 7.
  - Atlananlar `SevkSonrasiDuzeltmePostgresTests`'in 4 testi ve `CanliyaAlimBetikleriTests`'in 1 testidir; oturumda `THREEK_TEST_POSTGRES` tanımlı değildi.
- Kural–test eşleme doğrulayıcısı (`scripts/test/Test-IsKuraliEslemesi.ps1`): 542 kural doğrulandı (önceki 536 + L-12–L-17). `-TrxPath` kipi bu alt küme TRX'iyle kullanılamaz: kip, bütün eşlenen testlerin raporda bulunmasını ister ve ilk eksik testte (U-098) durur.
- Önceki koşum aynı filtreyle, sandık ekleme/silme yeteneği varken yapıldı (Release 13:06:49): 170 test; 166 başarılı, 4 atlanan. O yetenek kaldırıldığı için yalnız tarihsel kayıttır.

**Koşulmayanlar:**
- İzole PostgreSQL testleri (`SevkSonrasiDuzeltmePostgresTests`, `CanliyaAlimBetikleriTests`) bu oturumda atlandı. L-13, L-15'in ürün güncelleme/satır silme/lokasyon/Grid manuel kısmı ve L-17'nin veritabanı kanıtı bunlara bağlıdır.
- Tam paketler (.NET ve ön yüz) koşulmadı. Ön yüz testleri (`sandik-detay-duzeltme.spec.ts` dahil) bu oturumda koşulmadı.
- HTTP ve tarayıcı akışı denenmedi. Canlı veride (`3K_Db`) denenmedi; betiğin canlı veritabanı kopyasında provası yok.

**Açık kalanlar:**
- Testsiz kod yolları (L-15): Grid Trafo Sevk sevkli sandığı Kapandı'ya çekmez (G-030 istisnası); `UcKDurumGuncelleCommandHandler` ve `GridTopluDurumGuncelleCommandHandler` sevkli sandığın lokasyonunu değiştirmez.
- `SandikEkleCommandHandler`'ın sevk edilmiş projede 409'u ve `SandikSilCommandHandler`'ın sevkli sandık reddi (L-16) handler düzeyinde sınanmadı; yalnız proje kilidi testi var.
- Aynı sandıkta eşzamanlı ikinci düzeltme açılışının kısmi benzersiz indekse takılması sınanmadı. HTTP 403 akışı ve gerekçe 1000 karakter sınırı ayrıca denenmedi.

## Grid Sahası: Grid'e gelen ürünler ve mal kabul kayıtları (G-111–G-115) — 23 Ekim 2026

Değişiklik henüz commit edilmedi.

**Davranış.** Yeni Grid Sahası ekranı proje tipinden bağımsız ve salt okunurdur; `grid-saha-gorunumu` R ister (G-111). Menü düğümü ve rol izinleri `scripts/database/20261023_01_Grid_Saha_Gorunumu_Yetkisi.sql` ile kurulur (README sıra 23).
- Ürünler (`GET api/grid/saha/urunler`, G-112, G-113): Grid'e gelmiş (Tam/Eksik Geldi) satırları listeler. Varsayılan kapsam "Grid'de bekleyen"dir, "Tümü" kapsamı da vardır.
  - Bekleyen miktar `max(GridGelenAdet − GelenMiktar − yolda, 0)` ile hesaplanır. Saf kural `GridSahaBekleyenKurali`'dır, SQL ikizi `GridSorgulari`'dır.
  - Sevk kilidi Grid iş listesiyle ortaktır (`GridSorgulari.SevkKilidiDisinda`).
- Mal kabul (`GET api/grid/saha/mal-kabul`, G-114): canlı ve arşiv hareket geçmişindeki Grid teslim alma kayıtlarını `GridHareketOkuyucu` ile okur. Varsayılan aralık son 30 gün, en uzun aralık 366 gündür.
- Gelen eki (G-115): tekil Tam/Eksik Geldi, toplu Tam Geldi ve Grid manuel ürün hareketinin açıklamasına "Gelen: N" eki yazılır (`GridMalKabulAciklamaKurali`). Şema değişmez.

**Kapsam sınırı.** Tablo/kolon değişikliği yoktur; tek betik menü ve izin betiğidir. Ambalaj ve Finans'ta yeni kural yoktur. Eşleme satırları `coverage/GRID_TAHSIS_TEST_ESLESMESI.md` dosyasındadır: G-112 ve G-114 Birim; G-111, G-113 ve G-115 Kısmi.

- [GridSahaTests](../../3K.Application.Tests/GridSahaTests.cs) (bellek içi, 7 metot / 8 vaka; SQL kanıtı değildir):
  - İki sorgu `grid-saha-gorunumu` R bildirir, proje bağlamlı izin taşımaz (G-111).
  - Saf kural: kısmi partide yolda ve bekleyen, erken sonuçlandırılmış parti, sayaçsız eski parti, Grid'e gelmiş sayılmayan durumlar.
  - SQL ikizleri 3000 rastgele satırda saf kurala eşittir. Npgsql çevirisi bağlantısız üretilir.
  - "Gelen" eki yazılır ve yalnız açıklamanın sonundan okunur; kullanıcı notundaki aynı kalıp sayılmaz; eki olmayan eski kayıtta gelen adet null döner.
  - Ürün ve mal kabul doğrulayıcıları geçersiz değeri reddeder: kapsam, durum, sıralama, sayfa boyutu, tür ve tarih aralığı (366 geçer / 367 reddedilir; yalnız başlangıç 2000-01-01 reddedilir; taşan tarih 500 değil doğrulama hatasıdır).
  - Mal kabul handler'ı varsayılan olarak son 30 günü ve yalnız mal kabul türünü okur; kayıtları ekrana eşler.
- [GridSahaPostgresTests](../../3K.Application.Tests/GridSahaPostgresTests.cs) (izole PostgreSQL, 2 test, `THREEK_TEST_POSTGRES` ister):
  - `GridHareketOkuyucu` canlı ve arşiv kayıtlarını birleştirir; yalnız gerçek mal kabulleri seçer.
  - Tür, personel ve arama süzgeçleri çalışır. Personel özeti personel süzgecinden bağımsızdır.
  - Okuyucu satırın son mal kabulünü ve günlük mal kabul sayısını verir.
  - Ürünler handler'ı: bekleyen/tümü kapsamı, sevk kilidi, arama, durum süzgeci, sıralama, sayfalama ve son mal kabul.
- [GridKomutKatalogKapsamTests](../../3K.Application.Tests/GridKomutKatalogKapsamTests.cs): `TekilTamEksikTrafo_DurumMiktarSozlesmesiniUygular` (3 vaka) hareket açıklamasındaki "Gelen" ekini de doğrular. Tam/Eksik Geldi'de ek Grid'e gelen adettir; Trafo Sevk'te ek yoktur. `TopluTamGeldi_HerSatirinHareketineGelenAdetEkiYazilir` ve `TopluGridKapandi_HareketeGelenEkiYazilmaz` toplu yolu, `ManuelGridUrunu_EnYeniCekiyeYeniSandikVeTamIcerikleOndalikEklenir` manuel ürün hareketinin işlem metnini ve ekini sınar.
- [CanliyaAlimBetikleriTests](../../3K.Application.Tests/CanliyaAlimBetikleriTests.cs) (izole PostgreSQL, 1 test): `Postgres_Betik_SemayiModeleGetirir_TekrarindaTasimaIziniKorur` 20261023_01 betiğini de çalıştırır.
  - Betik `grid-saha-gorunumu` düğümünü rotasız kurar: Id 5258, Route NULL, üst 5, Sira 10 (24.10.2026). İlk sürümün `/grid-saha` rotası tekrar çalıştırmada boşaltılır ve durum sorgusu o hâli "UYGULANMAMIŞ" gösterir.
  - İzin yalnız Admin'e (W) ve `grid-is-listesi` R'si olan role (R) verilir.
  - Betik tekrar çalışınca düğüm çoğalmaz, izinler değişmez.
- [GridIsListesiTests](../../3K.Application.Tests/GridIsListesiTests.cs): `FiiliSandikNoBosluksa_CekidekiSandikNoIleKilitKontroluYapilir`, ortak sevk kilidinin iş listesi tarafını sınar (G-113).
- Ön yüz `grid-saha.component.spec.ts` (5 test): varsayılan kapsam, KPI, rozetler, salt okunur rozeti, tip iznine bağlı "Grid'e git"/"Projeye git" bağlantıları, süzgeç/sıralama/sayfa parametreleri ve aramada bekleme. Ayrıca mal kabul sekmesi (gün grupları, arşiv/toplu rozeti, eski kayıtta "6*", personel özeti) ve hata durumu.
- Ön yüz `grid.service.spec.ts` (4 test): ürün ve mal kabul uçları yalnız dolu süzgeçleri kırpılmış olarak gönderir; varsayılan kapsam ve tür yazılmaz. Sunucu hatası `ApiResult` olarak döner.

**Koşum (Release, 23 Ekim 2026, inceleme düzeltmelerinden sonra, PostgreSQL vakaları izole 127.0.0.1:55439 sunucusunda):**
- `FullyQualifiedName~GridSaha|FullyQualifiedName~GridKomutKatalogKapsamTests|FullyQualifiedName~GridIsListesi|FullyQualifiedName~GridTopluSevk|FullyQualifiedName~ServerAuthorizationRegressionTests|FullyQualifiedName~CanliyaAlimBetikleriTests`: 158/158 başarılı, 0 atlanan. Son okuyucu düzeltmesinden sonra `FullyQualifiedName~GridSaha` 30/30.
- Ön yüz: `grid-saha.component.spec.ts`, `grid.service.spec.ts`, `proje-tipi.spec.ts`, `sidebar.component.spec.ts` 26/26; `ng build` geçti.

**Önceki koşum (dokümantasyon oturumu, PostgreSQL'siz):**
- `FullyQualifiedName~GridSahaTests|FullyQualifiedName~GridSahaPostgresTests|FullyQualifiedName~GridKomutKatalogKapsamTests|FullyQualifiedName~CanliyaAlimBetikleriTests`: toplam 52; 49 başarılı, 0 başarısız, 3 atlanan.
  - Sınıf bazında `GridSahaTests` 7/7, `GridKomutKatalogKapsamTests` 42/42 başarılı.
  - Atlananlar `GridSahaPostgresTests`'in 2 testi ve `CanliyaAlimBetikleriTests`'in 1 testidir; oturumda `THREEK_TEST_POSTGRES` tanımlı değildi.
- `FullyQualifiedName~GridIsListesiTests`: 25/25 başarılı.
- Ön yüz `ng test --watch=false --browsers=ChromeHeadless`, yalnız `grid-saha.component.spec.ts` ve `grid.service.spec.ts` dahil: 9/9 başarılı.
- Kural–test eşleme doğrulayıcısı (`scripts/test/Test-IsKuraliEslemesi.ps1`): ilk çalıştırmada S-16 satırındaki yeniden adlandırılmış `OtomatikSahaAktarimPlanlayiciTests` metotları yüzünden durdu; S-16 eşlemesi güncellendikten sonra 536 kural doğrulandı (önceki 531 + G-111–G-115).

**Koşulmayanlar:**
- İzole PostgreSQL testleri dokümantasyon oturumunda atlandı; yukarıdaki inceleme sonrası koşumda koşuldu (atlanan 0).
- Tam paketler (.NET ve ön yüz) koşulmadı.
- HTTP ve tarayıcı akışı denenmedi. Canlı veride (`3K_Db`) denenmedi; betiğin canlı veritabanı kopyasında provası yok.

**Açık kalanlar:**
- İki sorgunun yetki bildirimi bellek içinde sınanır; HTTP 403 akışı ayrıca denenmedi.
- Karar bekliyor: toplu sevkin örtük Tam Geldi'sinin mal kabul sayılması; gelen adedin kümülatif yerine artış olarak yazılması ve durumu değişmeyen artışın (Eksik → Eksik) mal kabul sayılması (G-114, G-115).
- Ürün listesinde proje tipi süzgeci; proje no, sandık no, barkod ve poz no araması.
- Özetteki "Bugünkü mal kabul" alanı ve "Sahaya aktarıldı" bayrağının sunucuda hesaplanması (testte saha servisi boştur).
- Sayaçsız partide 3K Fazla Geldi dalı yalnız rastgele eşdeğerlik testinde geçer.
- `grid-is-listesi` W'si olan rolün betikten R alması.

## Sahadan sahaya taşıma, Kapandı sandığın yeniden açılması ve saha izi raporu (S-21–S-26, R-08) — 22 Ekim 2026

Değişiklik henüz commit edilmedi. Aynı günün otomatik aktarım bölümünün (aşağıda) devamıdır.

**Sahadan sahaya taşıma (S-21–S-23).** Saha projesine aktarılmış ürünler başka bir saha projesine taşınır. Önizleme `POST api/proje/sahadan-sahaya-tasima/onizle` (sahaya-aktar R, veri değiştirmez), uygulama `POST api/proje/sahadan-sahaya-tasima` (sahaya-aktar W) ile yapılır; yeni izin kodu yoktur.
- Satır kopyalanmaz: aynı saha satırı sayaçları ve kalitesiyle hedef sahanın Eksik Tamamlama çekisine, içerik hedef sandığa, aktif defter kaydı hedef sahaya geçer. `KaynakCekiSatiriId` kök normal satırı göstermeye devam eder; kök satırın aktif aktarım toplamı değişmez. Satır bütün olarak taşınır.
- Her taşınan satır için `SahaTasimaKalemi` izi yazılır. Proje, sandık, başlık ya da saha satırı silinirse bağ boşalır, iz kalır; yalnız kök satır silinirse iz de silinir.
- Engellerde (sevk edilmiş kaynak/hedef saha ya da sandık, sevkiyat düzeltmesi, bölünmüş satır, sevkiyat kapsamı, açık projeden karşılama, eksik/tutarsız defter) 409 ve engel maddeleri döner, hiçbir kayıt değişmez. Kök bağı olmayan saha satırı 400 alır.
- `IslemAnahtari` başlıkta benzersizdir: aynı anahtarla aynı istek ilk sonucu döner, farklı istek 409 alır.

**Geri alma (S-20 istisnası).** Taşıma başlığı "aktarımın tamamını geri al" ile geri alınmaz (409); geri dönüş ters yönde yeni taşımadır. Taşınmış satır, satır ya da sandık bazlı geri almayla doğrudan ana projeye dönebilir. `GET api/proje/{id}/saha-aktarimlari` satırları `SahaAktarimTasimaMi` alanını da taşır.

**Sandık durumu, kutu bilgisi ve numara (S-24–S-26).**
- İçeriği değişen Kapandı sandık (taşımada hedef ya da içinde ürün kalan kaynak; otomatik ve elle aktarımda yeniden kullanılan hedef) Hazırlanıyor'a döner ve "Sandık Geri Açıldı" hareketi yazılır; boşalan kaynak sandık silinmez, Bos olur.
- Taşımada yeni hedef sandığa ad, tip, ölçü, ağırlık ve lokasyon yalnız tek kaynak sandık taşımayla boşalıyorsa kopyalanır.
- Saha sandık numarası çakışması bütün yollarda `KoliNoKurali.Normalize` ile aranır ("05" = "5"); çakışmada "-2", "-3"… eklenir. Elle yollar önceden yalnız tam eşitliğe bakıyordu.

**Otomatik aktarım düzeltmeleri (S-18, S-19).** Önizleme özeti plan parçasına ek olarak hedef saha projesini ve hedef sandık eşleşmelerini içerir; önizlemeden sonra hedefteki eşleşen sandık değişirse uygulama 409 (`PLAN_DEGISTI`) alır. Ortak motor, sandık bazlı kalemdeki ürünü bildirilen kaynak sandıkta bulamazsa 409 döner.

**Rapor (R-08).** Saha projesinin sevk sonrası eksik raporunda ayrı "Saha izi" bölümü vardır: PDF'te ürün sayfalarından sonra ayrı yatay sayfa, Excel'de "Saha Izi" sayfası (aynı satır kaynağı). Bölüm, kök projelerin sahalardaki bütün aktif ürünlerini, gittiği saha ve sandığı, "A (1) → B (3)" izini ve sevk durumunu listeler.

**Şema ve kapsam sınırı.** Şema betiği `scripts/database/20261021_02_Sahadan_Sahaya_Tasima.sql`'dir (README sıra 22): `SahaAktarimlari.KaynakSahaProjeId`, `SahaAktarimlari.IslemAnahtari` (boş olmayan değer için benzersiz indeks) ve `SahaTasimaKalemleri`. README'ye göre yeni API'den önce uygulanır. Ambalaj ve Finans'ta yeni kural yoktur. Ön yüz akışı bu bölümde doğrulanmadı.

- [SahadanSahayaTasimaTests](../../3K.Application.Tests/SahadanSahayaTasimaTests.cs) (bellek içi, 16 metot / 18 vaka; SQL/transaction kanıtı değildir):
  - Satır taşımada sayaçlar, kök bağ, defter kaydı ve iz hedefe geçer; kök toplamı değişmez.
  - Sandık taşımada manuel içerik dahil bütün içerik geçer ve kaynak Bos olur.
  - Kutu bilgisi yalnız kaynak boşalınca kopyalanır; Kapandı hedef ve kaynak yeniden açılır.
  - Kaynak/hedef saha sevkli (2'şer vaka), sevkli kaynak sandık, bölünmüş satır, sevkiyat kapsamı ve açık karşılama engeli 409 alır; hiçbir kayıt değişmez.
  - Hedefte dolu numara "2-2" olur. Aynı anahtarla tekrar ilk sonucu döner, farklı taşıma 409 alır.
  - Taşıma başlığı toplu geri alınmaz ve ters yönde taşıma çalışır; önizleme veri değiştirmez.
- [SahadanSahayaTasimaPostgresTests](../../3K.Application.Tests/SahadanSahayaTasimaPostgresTests.cs) (izole PostgreSQL, 4 test, `THREEK_TEST_POSTGRES` ister):
  - Gerçek EF ile satır hedefe geçer ve saha izi raporu satırı, PDF ve Excel "Saha Izi" sayfası üretilir.
  - Boşalan kaynak sandık ya da kaynak saha projesi silinince iz kalır.
  - A→B→C zincirinde yeni çeki ve kutu bilgili sandık açılır, Tamamlandı hedef Hazırlanıyor olur, ara saha silinince ilk adım izde kalır.
  - Aynı anahtarla eşzamanlı iki taşıma tek başlık ve tek iz yazar.
- [CanliyaAlimBetikleriTests](../../3K.Application.Tests/CanliyaAlimBetikleriTests.cs) (izole PostgreSQL, 1 test): betik önceki şemada çalışır ve EF modeliyle aynı kolon, kısıt (ON DELETE dahil) ve indeksi kurar. Tekrar çalışma şemayı ve var olan izi değiştirmez; `kontrol/Betik_Durumu.sql` öncesini ve sonrasını doğru gösterir.
- [OtomatikSahayaAktarCommandTests](../../3K.Application.Tests/OtomatikSahayaAktarCommandTests.cs) (bellek içi; bu değişiklikle 3 yeni test, toplam 11):
  - `MevcutKapanmisBirNumaraliSandik_EksikleriAlincaYenidenAcilir`: Kapandı "1" sandığı eksikleri alınca açılır.
  - `AktarimMotoru_SandikBazliUrunBildirilenSandiktaDegilse_409`: bildirilen kaynak sandıkta olmayan ürün reddedilir.
  - `OnizlemedenSonraHedeftekiBirNumaraliSandikSevkEdilirse_409`: önizlemeden sonra hedef sandık sevk edilirse uygulama reddedilir.
- [OtomatikSahaAktarimPostgresTests](../../3K.Application.Tests/OtomatikSahaAktarimPostgresTests.cs) (izole PostgreSQL; 1 yeni test, toplam 3): `EsZamanliIkiUygulama_YalnizBirAktarimYazar` aynı önizlemeyle eşzamanlı iki uygulamadan yalnız birinin yazdığını doğrular. Aşağıdaki bölümün "Açık kalanlar" listesindeki eşzamanlı iki uygulama maddesi bununla kapandı.

**Koşum (Release, 22 Ekim 2026, kod incelemesi düzeltmelerinden sonra):**
- Bellek içi, filtre `(FullyQualifiedName~Finans|FullyQualifiedName~OtomatikSaha|FullyQualifiedName~SahadanSahayaTasima|FullyQualifiedName~SahaAkisiGuvenlikKurallariTests|FullyQualifiedName~EksiklerdenSaha|FullyQualifiedName~SandiklardanSaha|FullyQualifiedName~EksikUrun|FullyQualifiedName~Rapor|FullyQualifiedName~ServerAuthorizationRegressionTests|FullyQualifiedName~YetkiModeliTests)&Category!=Postgres`: 583/585. İki başarısız test (`FinansSekmeYetkileriTests.TestFiksturu_GocBetigindekiSekmeAltlariylaAyni`, `TestFiksturu_AltSekmeBetigiVeYetkiKodlariylaAyni`) depodan 18 Ekim'de kaldırılan 13 Ekim betiklerini arar; HEAD'de de başarısızdır, bu işten gelmez.
- İzole PostgreSQL, `OtomatikSahaAktarimPostgresTests|SahadanSahayaTasimaPostgresTests|CanliyaAlimBetikleriTests|SandikTopluTasimaSahaPostgresTests|FinansPostgresTests|FinansPoAkisPostgresTests` sınıfları: 41/41 başarılı, 0 atlanan.
- Kural–test eşleme doğrulayıcısı (`scripts/test/Test-IsKuraliEslemesi.ps1`, bu oturumda çalıştırıldı): 531 kural doğrulandı (önceki 524 + S-21–S-26 ve R-08).

**Koşulmayanlar:**
- Tam paketler koşulmadı.
- HTTP/ön yüz akışı tarayıcıda denenmedi; canlı veride (`3K_Db`) denenmedi.
- Betiğin canlı veritabanı kopyasında provası bu bölümde kayıtlı değil. `CanliyaAlimBetikleriTests` önceki şemayı izole veritabanında kurar.

**Açık kalanlar:**
- Taşımanın seçim retleri (400/404) ve kök bağı olmayan satırın 400 alması.
- Sevkiyat düzeltmesindeki kaynak sandık; sevk edilmiş ya da düzeltmedeki hedef sandık.
- Sevk Edildi / Sevkiyat Düzeltmede durumlu kalem, transferin kaynak ucu, içeriksiz satır ve eksik/tutarsız defter engeli.
- "05" = "5" numara eşleşmesi, elle yollarda normal biçimli çakışma ve "-3" sonrası.
- Elle aktarımda Bos/Kapandı hedef sandığın açılması ve `xmin` yarışı.
- Taşınmış satırın satır ya da sandık bazlı geri almayla ana projeye dönmesi.
- Kök satır silinince izin silinmesi (davranış testi; şema kuralı betik testinde).
- Saha izi PDF içeriği/yerleşimi (görsel kontrol), "Sevk edildi"/"Sevkiyat düzeltmede" durum metinleri, normal/yedek raporunda bölümün olmaması ve toplu eksik raporu.
- Önizlemenin R, uygulamanın W istemesi için ayrı yetki testi.

## Otomatik sahaya aktarım, 1 numaralı sandık kuralı ve aktarımın tamamını geri alma (S-16–S-20) — 22 Ekim 2026

Değişiklik henüz commit edilmedi.

**Otomatik aktarım (S-16–S-18).** Sevk görmüş normal projenin kalanları tek işlemle saha projesine aktarılır. Önizleme `GET api/proje/{id}/otomatik-saha-aktarim/onizle` ile alınır (sahaya-aktar R, veri değiştirmez); uygulama `POST api/proje/{id}/otomatik-saha-aktarim` ile yapılır (sahaya-aktar W).
- Sevk edilmemiş sandık, elle sandık aktarımının güvenlik kuralını geçiyorsa aynı numarayla sandık bazlı aktarılır; geçmiyorsa gerekçesiyle ana projede kalır.
- Sevk edilmiş sandıklardaki ürünlerin planlanabilir kalanı ("Eksiklerden oluştur" ile aynı hesap) saha projesinin "1" numaralı eksikler sandığına gider. Satır bölünmüşse yalnız sevkli payın eksiği aktarılır.
- Hatalı/uyumsuz ürün otomatik aktarılmaz; Grid İptal ve Grid Kapandı satırları plana girmez.
- Hedef saha projesi defterle ya da `<ProjeNo>-SAHA` adıyla bulunur. Birden çok aday varsa kullanıcı seçer; aday yoksa yeni proje açılır. Çakışan numaralara -2 eklenir; geri alınmış aktarımın bıraktığı boş sandık yeniden kullanılır.
- Uygulama planı Serializable transaction içinde yeniden kurar. Plan özeti (SHA-256) önizlemeyle farklıysa ya da plan boşsa 409 (`PLAN_DEGISTI`) döner ve hiçbir kayıt yazılmaz; ikinci basış kayıt üretmez.
- `EksiklerdenSahaProjesiOlusturCommandHandler` gövdesi davranış değiştirmeden ortak motora (`SahaAktarimIslemi`) taşındı; elle ürün bazlı, elle sandık bazlı ve otomatik yol bu motoru kullanır.

**1 numaralı sandık (S-19).** Ana projenin 1 numaralı sandığı ("01" dahil) hiçbir yoldan sandık olarak sahaya aktarılmaz: otomatik plan onu bırakır, elle sandık aktarımı ve motor 409 döner.

**Aktarımın tamamını geri alma (S-20).** `POST api/proje/saha-aktarimi-geri-al` (saha-aktarim-geri-al W) bir aktarım başlığını hepsi-ya-hiçbiri geri alır. İşlem görmüş satır, sevk edilmiş ya da düzeltmedeki sandık veya sevkiyat kapsamındaki kalem varsa 409 döner ve engeller listelenir. Başarılı geri almada saha satırları silinir, boşalan saha sandığı Bos olur. `GET api/proje/{id}/saha-aktarimlari` satırları artık aktarım başlığını taşır.

**Kapsam sınırı.** Yeni izin kodu ve şema betiği yoktur (mevcut `YetkiKodlari.Saha` kodları kullanılır); Ambalaj ve Finans'ta yeni kural yoktur. Ön yüz 3K Sandıklar ekranında 1 numaralı sandığı seçimden çıkarır; bu davranış ve ön yüz testleri bu bölümde doğrulanmadı.

- [OtomatikSahaAktarimPlanlayiciTests](../../3K.Application.Tests/OtomatikSahaAktarimPlanlayiciTests.cs) (veritabanısız, 13 metot / 16 vaka): sevkli sandığın kalanı eksiklere gider; temiz sevksiz sandık sandık bazlı aktarılır; işlenmiş sevksiz sandık atlanır ve ürünü eksiklere girmez; "1", "01" ve " 1 " numaralı sevksiz sandık ana projede kalır; sevkli 1 numaralı sandığın eksikleri aktarılır. Ayrıca: zaten sahadaki sandık atlanır, hatalı ürün gerekçeyle listelenir, planlanmış miktar düşülür, bölünmüş satırda yalnız sevkli pay aktarılır, Grid İptal/Grid Kapandı plana girmez. Normal olmayan proje ve hiç sevk görmemiş proje (409) reddedilir; plan özeti sıradan bağımsızdır.
- [OtomatikSahayaAktarCommandTests](../../3K.Application.Tests/OtomatikSahayaAktarCommandTests.cs) (bellek içi, 8 test; SQL/transaction kanıtı değildir): önizle → uygula akışında yeni `-SAHA` projesi, "1" eksikler sandığı ve sandık 3 oluşur. İkinci basış kayıt üretmez; farklı plan özeti 409 alır ve hiçbir sayı değişmez. Mevcut saha projesinin "1" sandığına ekleme yapılır. Geri alma sonrası yeniden önizleme aynı özeti üretir ve Bos sandıklar Hazırlanıyor olarak yeniden kullanılır. İşlem görmüş satırda geri alma 409 alır ve hiçbir kayıt değişmez. Elle sandık aktarımında ve motorda 1 numaralı sandık reddedilir.
- [OtomatikSahaAktarimPostgresTests](../../3K.Application.Tests/OtomatikSahaAktarimPostgresTests.cs) (izole PostgreSQL, 2 test, `THREEK_TEST_POSTGRES` ister): uygula → geri al → yeniden uygula zinciri gerçek EF, saha servisi ve FK sırasıyla koşar; plan değişince transaction hiçbir kayıt (hareket dahil) bırakmaz.

**Koşum (Release, bu dokümantasyon oturumunda):**
- `(FullyQualifiedName~OtomatikSaha|FullyQualifiedName~SahaAkisiGuvenlikKurallariTests|FullyQualifiedName~ServerAuthorizationRegressionTests)&Category!=Postgres`: 106/106 başarılı, 0 atlanan.
- `FullyQualifiedName~OtomatikSaha`: 24 başarılı, 2 atlanan. Atlanan iki vaka `OtomatikSahaAktarimPostgresTests`'tir; oturumda `THREEK_TEST_POSTGRES` tanımlı değildi.
- Kural–test eşleme doğrulayıcısı (`scripts/test/Test-IsKuraliEslemesi.ps1`): 524 kural doğrulandı (önceki 519 + S-16–S-20).

**Koşulmayanlar:**
- İzole PostgreSQL testlerinin geliştirme oturumunda geçtiği bildirildi, ancak sayısı aktarılmadı; bu oturumda koşulmadı.
- Tam paketler koşulmadı.
- HTTP/ön yüz akışı tarayıcıda denenmedi; canlı veride (`3K_Db`) denenmedi.

**Açık kalanlar:**
- Seçimsiz çoklu hedefte 400.
- Proje ve sandık numarası çakışmasında -2 eki.
- Sevk edilmiş ya da düzeltmedeki sandık ve `SevkiyatKapsamindaMi` engeliyle geri alma.
- Eşzamanlı iki uygulama.
- `GetSahaAktarimlariQuery`'nin yeni alanları.

## Ambalaj: çeki dışından eklenen sandık İlave ve "Yapılmaz" başlar, plan çekmecesinde görünmez — 21 Ekim 2026

Hata (canlı, PA727-03): çekiden 4 sandık oluşmuş projeye sonradan elle eklenen sandığın ambalaj kaydı Normal ve ambalaja dahil oluştu; Özel Sandıklar > İlave formunun kaynak listesi yalnız ambalaj dışı kaynakları gösterdiği için sandık oradan yönetilemiyordu. İki neden vardı: (1) ilk ambalaj kararı yalnız ada bakıyordu (transformatör/bushing dışında hep dahil); (2) İlave grubu "plan başlangıcı = projenin ilk ambalaj kaydı" zamanına göre seçiliyordu ve proje hiç senkronlanmamışsa (yerel yedekte 197 sandıklı normal projenin 182'si) her sandık Normal sayılıyordu. Artık grup sandığın kaynağından gelir (`AmbalajUretimPolitikasi.CekiDisiEklenenSandikMi`): numarası projenin çeki satırlarında (ilk yükleme ve revizyon; elle eklenen ürün ve eksik tamamlama satırları `IsManuelEklenen` olduğu için hariç; `CekideGecenSandikNo`, çeki yüklemesiyle aynı `KoliNoKurali` biçiminde) geçmeyen sandık İlave'dir. Sandık ekle ve Grid elle ürün ekle ile açılan sandık bu yüzden İlave'dir; çeki revizyonuyla gelen sandık normal planda kalır; projenin hiç çeki satırı yoksa sandık ilave sayılmaz. İlave kaydın ilk ambalaj kararı "Yapılmaz"dır (`YeniKaynakKarari`): ana plana ve tamamlama sayacına girmez, finansa aktarılmaz, plan çekmecesinde hiç listelenmez (`PlandaGosterilirMi`; kullanıcı daha önce dahil ettiyse İlave rozetiyle görünür) ve yalnız Özel Sandıklar'da İlave sandığa kaynak olarak seçilip yönetilir. Özel sandığa dönüştürülen kaynak planda kaydı yokmuş gibi yeniden görünmez, proje öngörüsüne girmez ve plandan ambalaj kararı değiştirilemez (409; önceden dönüştürülen çeki sandığı planda "dahil" hayalet satır olarak kalıyordu). Kayıtlı grup ve karar sonraki senkronlarda korunur (mevcut kayıtlar için betik yok, kullanıcı kararı). Kaydı henüz oluşmamış sandık plan, proje listesi/öngörü ve İlave aday listesinde aynı kuralla okunur; dönüştürme komutu kaynağı senkronladıktan sonra ambalaja dahil kaynağı 400 ile reddeder ve senkronun eklediği satırlar geri alınır. Koli no biçimi `CekiExcelOkuyucu`'dan Core'a (`KoliNoKurali`) taşındı, çeki yüklemesi aynı fonksiyonu kullanır; int'e sığmayan uzun sayısal numara artık taşma hatası vermez (elle girilen numara liste/plan uçlarını 500'e düşürmesin diye). Şema, DTO, izin kodu ve ön yüz değişikliği yoktur; Ambalaj katalog kapsamı dışında olduğundan kural kodu yoktur. 14 Ekim bölümündeki "plan başlangıcına göre gruplama" bununla değişti.

- [AmbalajSandikTipiVeTamamlamaTests](../../3K.Application.Tests/AmbalajSandikTipiVeTamamlamaTests.cs) (bellek içi): `RRQ2_Grup_kayitta_bir_kez_belirlenir_ve_korunur`, `Ceki_disi_eklenen_sandik_ceki_satirlarindaki_numaraya_gore_belirlenir` (koli no biçimi dahil), `Koli_no_ceki_yuklemesindeki_bicime_gelir`, `Ilave_sandigin_ilk_ambalaj_karari_yapilmazdir` (Core kuralları); `Ceki_sonrasi_elle_eklenen_sandik_ilk_senkronda_ilave_ve_ambalaj_disi_olusur` (bildirim senaryosu: hiç kaydı olmayan projede ilk senkron; revizyon sandığı Normal, elle ürün satırı kararı değiştirmez, finans modeli yok, hareket "False"); `RRQ2_Ceki_disindan_eklenen_sandik_senkronda_ilave_olur_ve_oyle_kalir`; `Senkronize_edilmemis_elle_eklenen_sandik_planda_gorunmez_ve_ozel_sandik_adayidir`; `Senkronlanmis_ilave_kaynak_ambalaj_disindayken_planda_gorunmez_dahilse_gorunur`; `Elle_eklenen_sandik_ozel_ilave_sandiga_donusur_cekiden_gelen_donusmez` (dönüşen sandık planda yok, plandan karar 409); `A7_Kaynak_sandiktan_ozel_sandik_kaynak_tipini_korur` (dönüşen çeki sandığı plana dönmez); `Ilave_aday_listesi_kaydi_olmayan_sandigi_senkronun_ilk_karariyla_okur` (kayıtsız transformatör, tamamlanmış proje); `Saha_projesinde_ceki_disindan_eklenen_sandik_saha_grubunda_ve_dahil_olusur`; `A2_Ilave_siniflanan_formsuz_kaynak_kayit_…` İlave kaydı dahil edilince tamamlamayı durdurur.
- [AmbalajUretimV2PostgresTests](../../3K.Application.Tests/AmbalajUretimV2PostgresTests.cs) (izole PostgreSQL): `Senkron_CekiDisindanEklenenSandigiIlaveVeAmbalajDisiOlusturur_OkumaTarafiAyniKuraliUygular` çeki satırı sorgusunun EF/SQL çevirisini, proje listesi öngörüsünü ve aday listesini kayıt oluşturmadan, ardından senkronu doğrular (`AmbalajaDahil = false` veritabanına yazılır; EF'deki `HasDefaultValue(true)` bunu ezmez). `OzelIlave_KayitsizKaynaktan_CekiSandigiReddedilirGeriAlinir_ElleEkleneDonusur`: çeki sandığı dönüşümü 400 ve projede hiç ambalaj kaydı kalmaz; elle eklenen sandık özel İlave kaydına dönüşür, plan çekmecesinde yalnız çeki sandığı kalır.

Koşum (Release): `(FullyQualifiedName~Ambalaj|FullyQualifiedName~Ceki)&Category!=Postgres` 465 başarılı, 1 atlanan (`SandikTopluTasimaPostgresTests.CekiCokluTahsis_…`, kategori etiketi olmayan PostgreSQL vakası, ilgisiz); izole PostgreSQL'de `AmbalajUretimV2PostgresTests|ProjeSilmeAmbalajTests|(Ceki&Category=Postgres)` 20/20, 0 atlanan. Tam paketler koşulmadı; HTTP/ön yüz akışı tarayıcıda denenmedi. Açık: dönüştürülen özel İlave sandık silinirse (iptal) kaynak sandık senkronla geri gelmez ve İlave aday listesine dönmez (kullanıcı iptali geri alınmaz kuralı); çeki sandığında bu durum önceden de vardı.

## Hata bildiriminde tek takip kodu — 18 Ekim 2026

500 yanıtında ekranda iki kod çıkıyordu: `UnhandledExceptionBehavior` kendi 8 haneli kodunu mesaja yazıyor ("Takip kodu: …"), ön yüz de isteğin `X-Correlation-ID`'sini ekliyordu ("Takip: …"). Behavior'ın log şablonundaki `{CorrelationId}` özelliği LogContext'teki istek kodunu da eziyordu; Error satırı istek koduyla bulunamıyordu. Artık sunucu kod üretmez: 500 mesajı "Beklenmeyen bir hata oluştu.", log satırı istek kodunu taşır. Onay yürütme hatalarında (`IslemOnaylaCommandHandler`) yanıt mesajı kodsuzdur; onay kaydında kalan hata metni istek kodunu `ITakipKoduSaglayici` ile taşır (yalnız harf/rakam/tire, en fazla 64 karakter; HTTP dışında kodsuz).

- [IsHatasiLoglamaBehaviorTests](../../3K.Application.Tests/IsHatasiLoglamaBehaviorTests.cs) (`BeklenmeyenHata_KendiTakipKodunuUretmez_IstekKodunuEzmez`): mesaj kodsuz, Error satırında `CorrelationId` özelliği yok.
- [OrtakOnayYurutmeIsKurallariTests](../../3K.Application.Tests/OrtakOnayYurutmeIsKurallariTests.cs) (`BeklenmeyenHata_YanittaKendiKoduYok_OnayKaydiIstekTakipKodunuTasir`): yanıt kodsuz, kayıt istek kodunu taşır, kod yoksa kodsuz, istisna metni kayda girmez.

Koşum (Release): `IsHatasiLoglamaBehaviorTests|OrtakOnayYurutmeIsKurallariTests|AtomicTransactionTests|FinansHataEslemeTests|AmbalajKritikIslemTests|ApiYanitSozlesmesiTests|OnayIslem` 74/74; izole PostgreSQL ile `CekiRevizyonYasamDongusuEntegrasyonTests|FinansPoOnayPostgresTests|OrtakOnayYurutmeIsKurallariTests|IsHatasiLoglamaBehaviorTests` (GecisBetigi hariç) 32/32, 0 atlanan. `FinansPoOnayPostgresTests.GecisBetigi_…` `scripts/database/` çalışma kopyasından kaldırıldığı için betik dosyasını bulamadan düşer (bu değişiklikle ilgisiz). Ön yüz değişmedi.

## Projede birden çok fotoğraf bağlantısı — 18 Ekim 2026

Tekil `Projeler."FotografBaglantisi"` kolonu `ProjeFotografBaglantilari` tablosuna taşındı (`20261018_01_Proje_Fotograf_Baglantilari.sql`: dolu tekil değer açıklamasız bağlantı olarak kopyalanır, kolon aynı transaction'da kaldırılır; tablo varken `20261014_01` kolonu geri eklemez). Bir projede (Normal/Saha/Yedek fark etmez) birden çok http/https bağlantı ve isteğe bağlı açıklama (en fazla 100 karakter) tutulur; en fazla 20 bağlantı, aynı adres iki kez eklenmez (409), başka projenin bağlantısı düzenlenemez/silinemez (404). İzin değişmedi: `proje-fotograf-baglantisi` R görür/açar, W ekler/düzenler/siler. Uçlar: `POST api/proje/{id}/fotograf-baglantilari`, `PUT`/`DELETE api/proje/{id}/fotograf-baglantilari/{baglantiId}`; yanıt projenin güncel listesidir. Bağlantı ve hareket geçmişi `ExecuteAtomicAsync` ile birlikte yazılır. Bağlantı tablosu proje kilidi zincirinde değildir: sevk edilmiş projede de yazılır (eski kolon muafiyeti `ProjectLockInterceptor`'dan kaldırıldı). Proje silinince bağlantılar FK `ON DELETE CASCADE` ile silinir.

- [ProjeFotografBaglantisiTests](../../3K.Application.Tests/ProjeFotografBaglantisiTests.cs): adres/açıklama kuralı (boş adres artık geçersiz), iki doğrulayıcı, iki komutun yalnız W ile geçmesi (gerçek `AuthorizationBehavior`), ekleme/düzenleme/silme ve hareket geçmişi (aynı değerle kayıtta hareket yok), aynı adres/başka proje/olmayan kayıt/üst sınır retlerinde verinin değişmemesi, listede yalnız R ile ve eklenme sırasıyla görünmesi; `Postgres_Betikler_TekilBaglantiyiTasiyipKolonuKaldirir_TekrarCalisir_ProjeSilinceBaglantilarSilinir` (01→02→18_01: dolu değer kırpılıp taşınır, boşluk atlanır, kolon kalkar; üç betik ikinci kez değiştirmez; EF eşlemesi ve cascade silme).
- [ProjectLockCorrectionScopeTests](../../3K.Application.Tests/ProjectLockCorrectionScopeTests.cs): sevk edilmiş projede bağlantı eklenir/değişir/silinir; aynı kayıtta proje alanı değişirse kilit yine uygulanır.
- [CanliyaAlimBetikleriTests](../../3K.Application.Tests/CanliyaAlimBetikleriTests.cs): betik sıra 21 olarak canlı provaya eklendi (önceki şema, durum sorgusu, şema = EF modeli, tekrar).
- [ApiYanitSozlesmesiTests](../../3K.Application.Tests/ApiYanitSozlesmesiTests.cs): bağlantı listesi JSON dizisi olarak yazılır.
- Ön yüz (`proje-listesi.component.spec.ts`): kural aynası; R ile tek bağlantı çipten, çok bağlantı çipin açtığı listeden yeni sekmede (`rel="noopener noreferrer"`) açılır, belge tıklaması listeyi kapatır, izin yoksa çip yok; W ile açıklamalı ekleme (şemasız adrese https), düzenleme, onaylı silme, satır ve pencerenin yerinde güncellenmesi; 20 bağlantıda ekleme kapalı, düzenleme açık; sunucu reddi bildirilir; R düzenlemeyi açmaz.

Koşum (Release): `ProjeFotografBaglantisiTests|ApiYanitSozlesmesiTests|ProjectLockCorrectionScopeTests|CanliyaAlimBetikleriTests` 49/49 + 3 atlanan (bellek içi); izole PostgreSQL ile `ProjeFotografBaglantisiTests|CanliyaAlimBetikleriTests|ProjectLockCorrectionScopeTests` 50/50 (0 atlanan), `ProjeSilme` 2/2 (0 atlanan); `ServerAuthorization|SahaProjeTamamlanmaRegresyonTests|YetkiModeliTests|ProjeSilme` 101/101 + 2 atlanan. Ön yüz: `features/projeler/**` + `proje.service` spec'leri 26/26, `ng build` başarılı. Tam paketler koşulmadı. Betik `3K_Db`'de veya canlı kopyada çalıştırılmadı; tarayıcıda uçtan uca denenmedi (yerel DB'de tablo yok).

## Raporlarda ürün sevk durumu: ayrı SEVK sütunu, normal proje kendi sevkiyatının anlık görüntüsü (R-07) — 18 Ekim 2026

Son düzeltme (18.10.2026, kullanıcı geri bildirimi): PA600-36 sandık 1'in sevkiyatı geri alındıktan sonra Grid Kapandı satırı (FCT01120721) hâlâ "Sevk Edildi (Grid Kapandı)" görünüyordu; kural Grid Kapandı'yı sandıktan bağımsız sayıyordu. Artık Grid Kapandı ve trafodaki ürün satırın sandığıyla gider: sandığı sevk edildiyse gitmiş, sevk edilmediyse ya da sevkiyatı geri alındıysa Sevk Edilmedi. Kullanıcı isteğiyle sandığıyla giden Grid Kapandı ürün yalnız "Sevk Edildi" yazar; "Sevk Edildi (Grid Kapandı)" etiketi ve özetteki ayrı sayacı kaldırıldı. Parçalı satır (SMC45152: 1 trafoda, 1 3K'dan sandıkta) sandığı gittiyse Sevk Edildi; Eksik Sevk paydası artık istenen adettir (trafo dahil). Kullanıcı isteğiyle sandık sayfasındaki SEVK TARİHİ ve ürün sayfasındaki SAHAYA AKT. sütunları ile başlıktaki "sahaya aktarılan" toplamı kaldırıldı; sandık sayfasındaki SAHAYA AKT. sütunu kaldı. Rapor açılış koşulu (R-05) sorgusu master'daki haline döndü. Canlı veri bu son düzeltmede okunamadı (salt okunur kullanıcının tablo yetkileri kalkmış); aşağıdaki canlı sayılar Grid Kapandı/trafo değişikliğinden öncedir ve sandığı sevk edilmemiş Grid Kapandı/trafo satırları kadar değişir.

Sorun: saha ve yedek gerçekleşen çeki listesinde elde kalan sandıktaki tamamlanmış ürün sevk edilmiş gibi görünüyordu; sevk sonrası eksik raporunda hangi ürünün gittiği anlaşılmıyordu. Gerçekleşen çeki ile eksik / sevk sonrası eksik raporlarına (normal, saha, yedek; PDF ve Excel) ayrı SEVK sütunu eklendi; Durum yalnız iş tamamlanmasıdır ("Eksik Sevk" → "Eksik"). Kurallar (`UrunSevkDurumuKurali`): Grid İptal → Sevk Edilmedi; Grid Kapandı ve trafodaki adet satırın sandığıyla gider; diğerlerinde projenin kendi sevkli sandığına fiilen konan, 3K'nın teslim aldığı adet tam ise Sevk Edildi, kısmen ise Eksik Sevk (giden/beklenen), hiç değilse Sevk Edilmedi; tamamı trafoyla kapanan Trafoyla. Kullanıcı kararıyla (18.10.2026) normal projenin raporu yalnız kendi sevkiyatının anlık görüntüsüdür: sahaya aktarılan kısmın saha sevki, saha Grid kararı ve saha trafosu normal SEVK'e karışmaz; aktarılan adet normal gerçekleşen çekide ayrı "SAHAYA AKT." sütunundadır (eksik raporunda mevcut Hedef Saha sütunu); sandık bazlı sahaya aktarılan sandık sandık sayfasında "Sevk Edilmedi" + "Aktarıldı"/"Kısmen (a/b ürün)" görünür; Grid'in beyan ettiği ama 3K'nın teslim almadığı adet sayılmaz; normal gerçekleşen çekide KALAN projenin kendi kalanıdır. Hesap yalnız raporu okur; 3K/Grid işlemleri, sevk kilidi, saha aktarımı, durumlar ve veri değişmez; projenin rapor için sevk edilmiş sayılması (R-05), proje durumu (S-09) ve eksik raporu kalanı (R-03) aynı kaldı. Gelişim: ilk sürüm (9b6242f, b21cbab ile geri alındı) iptal ve Grid Kapandı satırlarına "-" yazıyordu; ikinci sürüm saha sandığıyla gideni ve saha Grid Kapandı'yı normal SEVK'e sayıyordu ve Grid manuel beyanını 3K teslimi gibi saydığı için PA675-01 #9999 FCT00685015'i (3K gelen 0, Grid 28.08'de "Toplu Sevk", 07.09'da sahaya aktarılıp sahada Grid İptal) "Sevk Edildi" gösterdi; kullanıcı normal raporun anlık görüntü olmasını istedi. Çok ajanlı analizde (canlı veri, salt okunur) doğrulananlar: aktarımlı 915 normal satırın hepsinde kendi giden + aktarılan = istenen − trafo (çift sayım yok); sevkli sandıkta konan > 3K teslimi olan yalnız 4 manuel satır (PA600-16, PA600-47, PA675-01, PA675-02 #9999); normal projelerde 17 içeriksiz eski satır var; sandık numarası olan yalnız PA600-50 sıra 73/74 (sandık 36/37 sevkli; ekranlar gibi sandık numarasından okunur), diğerleri numarasız (Sevk Edilmedi, Trafoyla ya da "-"). Canlıda beklenen (satır): PA675-01 (sandık 17 şu an sevkiyat düzeltmesine açık, 79 içerik; sayılar düzeltme kapanınca yeniden okunmalı) 213 Sevk Edildi, 10 Sevk Edildi (Grid Kapandı), 3 Sevk Edilmedi (1'i sahaya aktarılan FCT00685015), 5 Trafoyla; PA600-36 201 / 20 / 19 Sevk Edilmedi (15'i sahaya aktarılan, sıra 262/263/268/269 dahil) / 3 Trafoyla; PA600-16 218 / 6 / 13 (11 aktarılan; sıra 164 Sevk Edilmedi + 100 sahaya aktarılan) / 6; PA704-06 116 / 10 / 40 (hepsi aktarılan) / 3, sandık 2/4–7 "Sevk Edilmedi / Aktarıldı"; PA549-01 172 / 7 / 2 Eksik Sevk / 73 Sevk Edilmedi (70 aktarılan) / 3. Normal gerçekleşen çekide KALAN değişikliği 23 projede 319 satırı etkiler (toplam +6.321 adet; ör. PA600-16 sıra 164 KALAN 0 → 100, SAHAYA AKT. 100); aktarımlı 915 satırda KALAN = aktarılan adet, eksik raporunda (R-03) aynı satırların KALAN'ı 0 kalır. Normal projelerde 1.563 Grid Kapandı satırının 3K kaydı yoktur; bunlar sandıkları sevk edildiyse "Sevk Edildi", edilmediyse "Sevk Edilmedi" görünür. Saha ve yedek raporlarında KALAN, listelenen satırlar ve iş durumu hesabı değişmedi; bu işle SEVK / SEVK TARİHİ sütunları, "Sevk (satır)" özeti, sandık sayacı ve açıklama notu eklendi, Durum metni "Eksik Sevk" → "Eksik" oldu; eksik Excel'inde 3K Durum/Süreç/Açıklama 16–18 → 17–19. Grid İptal olduğu halde 3K'nın teslim alıp sevkli sandığa koyduğu 3 satır (PA677-01 #123, PA686-03/04 #22) kural gereği Sevk Edilmedi görünür. Bilinen sınırlar: rapor güncel veriden hesaplanır (sevk anının kopyası saklanmaz; düzeltmesi açık sandıkta güncel içerik görünür); kendi sevkiyatıyla boş gitmiş ve sandık bazlı aktarılmış 6 sandık (PA600-12 sandık 21–25, PA600-18 sandık 5) "Sevk Edildi" + "Aktarıldı" birlikte görünür; "Kısmen (a/b ürün)" ürün sayısı oranıdır (canlıda yalnız PA600-18 sandık 4: 1/7); Sandık Yönetimi ekranı saha sandığıyla gitmiş sandığı "Sevk Edildi (Saha)" gösterir, rapor normal sevkiyatı gösterir (rapor notunda yazılı); sahada Grid İptal olan 103 aktarım normal raporda "Sevk Edilmedi" + aktarılan adet olarak görünür, saha kararı saha raporundadır. Şema, API, izin ve ön yüz değişikliği yoktur.

- [UrunSevkDurumuKuraliTests](../../3K.Application.Tests/UrunSevkDurumuKuraliTests.cs) (veritabanısız, 9 test): tam/kısmi sevk ve bölünmüş satır; atanmış ama konmamış, elde kalan, sandıksız, başka proje sandığı; yalnız 3K teslimi (Grid manuel beyanı sayılmaz; stok/proje/tedarikçi sayılır, projeye gönderilen düşülür); Grid İptal/İptal Edildi; Grid Kapandı satırın sandığıyla (sevkli / elde ya da sevkiyatı geri alınmış / sandıksız / bölünmüş, istenen 0 dahil); trafo satırın sandığıyla (tam trafo, SMC45152 parçalı, kısmi gelen); içeriksiz eski satırın sandık numarasıyla eşleşen sandığı; etiket, özet, sandık durumu ve sandık aktarım metni.
- [RaporSevkDurumuPostgresTests](../../3K.Application.Tests/RaporSevkDurumuPostgresTests.cs) (izole PostgreSQL, gerçek rapor servisleri): dört sandıklı normal/saha/yedek projede gerçekleşen çeki ve eksik raporunun PDF/Excel çıktısı (sandık sevk/tarih, Durum–SEVK çiftleri, Eksik Sevk (1/2) ve (2/4), sevkli sandıktaki İptal, özet, renk, "Sahaya Akt." sütununun yalnız normalde bulunması); `NormalRapor_KendiSevkiyatininAnlikGoruntusu_SahayaAktarilanAyriGosterilir` normal projede saha sandığıyla giden, sahada Grid İptal/Grid Kapandı olan, kısmi, sandık bazlı, geri alınmış ve iptal edilmiş aktarımı; normal kısmı gitmiş satırda sahada Grid İptal ve saha trafosunun normal SEVK'e sızmadığını (Eksik Sevk (1/2)); KALAN'ın kendi kalanı olmasını; sandık sayfasında "1 / 5", sandık 5 "Aktarıldı", sandık 6 "Kısmen (1/2 ürün)" (Grid Kapandı, Grid İptal ve tam trafolu ürün sayılmaz), geri alınmış sandık bazlı aktarımlı sandık 7 "-"; iki sevkiyattaki sandığın son tarihi; FCT00685015 senaryosu (Grid manuel + Grid beyanı + 3K teslimi yok + sahada İptal); içeriksiz eski satırı gerçekleşen çekide ve eksik raporunda doğrular; saha/yedekte çeki bağı olmayan içerik. [RaporPostgresEntegrasyonTests](../../3K.Application.Tests/RaporPostgresEntegrasyonTests.cs) toplu/tekil eksik Excel karşılaştırması 19 sütun.
- Otomatik doğrulama Excel içeriğini ve PDF'in üretildiğini kapsar; PDF içeriği örnek çıktıların sayfa görüntüsüyle elle kontrol edildi (normal sandık sayfası notu ve "SAHAYA AKT." sütunu, ürün sayfası notu ve sütunu; saha raporunda sütun yok). Excel'de açıklama notu birleşik satıra yazılır; A sütunu genişliği değişmedi (örnek çıktılarda önceki sürümle aynı).

Koşum (Release, izole PostgreSQL açık): `UrunSevkDurumuKuraliTests|RaporSevkDurumuPostgresTests|RaporPostgresEntegrasyonTests|OrtakSandikRaporIsKurallariTests|RaporTutarliligiTests|ServerAuthorizationRegressionTests|SevkiyatRaporTests|SahaIsTamamlamaServiceTests|TalepFormuRaporTests` 142/142, 0 atlanan. Değişiklik ayrıca dört mercekli (kod, canlı veri, test gücü, iş kuralı) bağımsız ajan incelemesinden geçti; bulunan Excel genişlik gerilemesi, not metinleri ve test açıkları kapatıldı. R-05 rapor kapısı bu işte de testsiz kaldı (önceden açık). Kural–test eşleme doğrulaması yapıldı. Tam paketler koşulmadı; düzeltmesi açık sevkli sandık ayrıca test edilmedi.

## Ürün bilgisi Grid ekranında da görünür ve düzenlenir; ortak alanda eşzamanlılık (U-249) — 18 Ekim 2026

Ürün bilgisi (seri no, SKT, lot) 3K'da olduğu gibi sandık satırında (`SandikIcerik.UrunBilgisi`) kalır; şema değişmedi. Grid listesi artık `GridUrunDto.SandikId` ve `UrunBilgisi` döner (3K listesiyle aynı sandık satırından). İki ekranda da bilgi ürün adının altında görünür ve satırdaki etiket düğmesi ortak "Ürün bilgisi" penceresini (`shared/components/urun-bilgisi-duzenle`) açar; pencere Grid/3K işlem kilitlerinden bağımsızdır (Grid durum paneli 3K işlemi yapılmış satırda açılmıyordu). 3K karşılama panelindeki alan kaldı ve aynı kuralı kullanır. Sunucu ucu aynıdır (`PUT api/sandik/icerik-bilgisi-guncelle`); yeni izin kodu yoktur: yetki `ProjectMenuOperation.ProductInfoWrite` ile gerçek proje tipinin Sandıklar, 3K veya Grid W iznine bağlandı, Grid W sandık yazmanın geri kalanını açmaz. İstek yeni `OncekiUrunBilgisi` alanını taşır; kayıtlı değer ekranda görülenden farklıysa 409 döner ve hiçbir şey yazılmaz, ekran listeyi tazeler. Eski ön yüz (alanı göndermeyen) yalnız boş bilgiyi doldurabilir, dolu bilgiyi ezemez.

- [SandikIcerikUrunBilgisiTests](../../3K.Application.Tests/SandikIcerikUrunBilgisiTests.cs): `EkrandakiDegerGuncel` boşluk farkını yok sayar, içerik/harf farkını yakalar; ekrandaki eski değer güncel değilse 409, kayıt/kaydetme/geçmiş değişmez, güncel değeri gören kullanıcı aynı değişikliği yapabilir; kaydı uygulanmış isteğin tekrarı çakışma sayılmaz ve yazmaz. Mevcut vakalar (geçmiş, boşaltma, sevk kilidi, başka proje 404) ekranda güncel değerle koşar.
- [ServerAuthorizationRegressionTests](../../3K.Application.Tests/ServerAuthorizationRegressionTests.cs): Normal/Saha/Yedek için Grid, 3K ve (Normal) sandık yönetimi W ile kayıt açılır; Grid R, başka tipin Grid W'si ve ilgisiz izin 403; Grid W ile sandık özelliği ve ürün güncelleme 403; gövdede başka proje kimliği 403.
- [GridUcKSatirMiktarSenkronizasyonTests](../../3K.Application.Tests/GridUcKSatirMiktarSenkronizasyonTests.cs): `UrunBilgisi_IkiListedeAyniSandikSatirindanAyniKimlikleDoner` — iki sandığa bölünmüş üründe Grid ve 3K listeleri aynı (sandık, içerik, bilgi) üçlüsünü döner; tahsissiz satırda içerik kimliği ve bilgi yoktur, iki liste aynı sandığı gösterir.
- Ön yüz: `urun-bilgisi-duzenle.component.spec.ts` (7 test: ortak kural ve kilit nedeni, istekte önceki değer, kayıt, 409'da çakışma bildirimi, diğer retler, sevk kilitli sandıkta kapalı kutu, satır tazelenince güncel değer), `grid-urunler-urun-bilgisi.spec.ts` (4 test: 3K işlemi yapılmış/Tadilatta satırda düzenleme, kilit/tahsissiz/izinsiz satırda açılmaz, yalnız o sandık satırı güncellenir, çakışmada tazeleme), `uck-urunler-urun-bilgisi.spec.ts` (8 test; önceki değer, 409 sonrası panelde güncel değer, satır düğmesi 3K paneli kilitli satırda açılır).

Koşum (Release): `SandikIcerikUrunBilgisiTests|ServerAuthorizationRegressionTests|GridUcKSatirMiktarSenkronizasyonTests` 103/103 (yeni testler dahil). Ön yüz `ng test` (`shared/components/urun-bilgisi-duzenle`, `features/grid/grid-urunler`, `features/uck/uck-urunler`, `features/sandik-yonetimi/sandik-detay`) 68/68; `ng build` (development) hatasız. Tam paketler ve PostgreSQL entegrasyon testleri koşulmadı (şema, transaction ve SQL ifadesi değişmedi; eşzamanlı iki kaydın `xmin` ile 409'a çevrilmesi mevcut davranıştır, burada ayrıca sınanmadı). Ekranlar çalışan API ile tarayıcıda denenmedi.

## Ambalaj: sandık ölçüsü güncellenince üretim formunun güncel ölçüyle (yeniden) oluşması — 17 Ekim 2026

Hata: ilk üretim formu kaydı "Üretimde" yapıp otomatik kaynak senkronuna kilitler (`KaynakSenkronizasyonuKilitliMi`); sandık ölçüsü/tipi sonradan değişince kuyruk senkronu kaydı bilinçli olarak atlar ve kaynak kaydın ölçüsünü Ambalaj'da düzeltecek başka yol yoktur. "Yeniden oluştur" bu yüzden çam m³'ünü, parçaları (teknik resim) ve formu eski ölçüyle üretiyordu (canlı veride sandık 3: sandıkta En 1200, kayıtta ve form sürüm 2'de 1000). Artık üretim formu oluşturma/yeniden oluşturma, seçimdeki tamamlanmamış kaynak kayıtları (`AmbalajUretimKaydi.FormdaKaynaktanTazelenirMi`) güncel sandıktan tazeler: ad, cins, numara/adet, ölçü, m³/sarf; değişiklik "Kaynak sandık değişiklikleri üretim formuna alındı" hareketiyle yazılır ve finansa aktarılır. Tamamlanan kayıt, iptal/özel sandık ve otomatik kuyruk senkronu değişmedi (`KaynakSenkronizasyonu_KilitliVeyaIlerlemişKaydiEzmez` geçerli). Kaynakta ölçüsü eksik sandıkta veya PO geçmişi olan (iptal edilmiş PO dahil) finans kaleminde ad/cins/ölçü değişiyorsa (`AmbalajUretimPolitikasi.KaynakFiyatEslesmesiniDegistirirMi`) 409 döner ve hiçbir kayıt değişmez. Kaynak eşlemesi (senkron ve tazeleme ortak) değeri aynı ölçüyü yeniden atamaz: ölçeksiz sandık değeri ile `numeric(18,4)` kayıt değeri (1400 ile 1400.0000) aynı sayılır, hareket geçmişine sahte ölçü değişikliği yazılmaz (canlıda senkron daha önce "2150.0000 → 2150" satırları yazmıştı). Şema, DTO, izin kodu ve ön yüz değişikliği yoktur; Ambalaj katalog kapsamı dışında olduğundan kural kodu yoktur.

- [AmbalajKaynakTazelemeTests](../../3K.Application.Tests/AmbalajKaynakTazelemeTests.cs) (bellek içi, gerçek `SandikOzellikGuncelleCommandHandler`, kaynak senkronu ve form komutu): sandık → senkron → ilk form → ölçü güncelleme → kuyruk senkronu kilitli kaydı atlar → yeniden oluşturma yeni dış/iç ölçü, parça boyu/hacmi, net/sarf/toplam m³, brüt kg, hareket ve finans modelini yeni ölçüyle üretir; kuyruk işlemeden ilk form da güncel ölçüyü kullanır; tamamlanan kayıt eski ölçüsünü korur; PO geçmişli kalemde değişiklik yokken yeniden oluşturma serbest, ölçü değişince 409 ve kayıt/hareket/form/finans/kaydetme sayısı değişmez; kaynak ölçüsü silinince 409 ve hiçbir şey değişmez; tazeleme olayı kayıt geçmişinde ölçü/m³ izni olmayan kullanıcıya ölçü ve m³ değeri göstermez; giriş kuralı ve eşleme veritabanısız.
- [AmbalajUretimV2PostgresTests](../../3K.Application.Tests/AmbalajUretimV2PostgresTests.cs) (izole PostgreSQL): `Form_SandikOlcusuGuncellenince_YenidenOlusturmaGuncelOlcuyleHesaplar` aynı sırayı gerçek EF ile koşar (form hesabı kaydı izlenmeden okuduğu için tazeleme aynı transaction'da önce yazılır; ara kaydetme kaldırılınca test kırılır) ve geçmişte yalnız gerçekten değişen ölçünün (En) yazıldığını doğrular; `Form_YenidenOlusturmaFinansHatasi_KaynakTazelemesiniGeriAlir` sonraki adım başarısız olunca tazelenen ölçü, m³, son senkron tarihi, hareket ve form sürümünün geri alındığını doğrular.
- Tazeleme kapatılınca bellek içi senaryo testlerinden 4'ü kırılır (koşuldu, sonra geri alındı).

Koşum (Release): `FullyQualifiedName~Ambalaj&Category!=Postgres` 262/262; izole PostgreSQL'de `AmbalajUretimV2PostgresTests|ProjeSilmeAmbalaj` 11/11, 0 atlanan. Tam paketler koşulmadı; PO'lu kalemde finans servisinin gerçek reddi ve HTTP/ön yüz akışı ayrıca denenmedi; canlı veride sandık 3'ün düzelmesi için formun yeniden oluşturulması gerekir.

## Finans PO öncesi iş onayı (Y-17) — 17 Ekim 2026

Proje kalemleri PO'ya bağlanmadan önce İşlem Onay Merkezi'nde onaylanır (`FINANS_PO_IS_ONAYI`, Sistem Kuralları'nda "Finans: PO Öncesi İş Onayı" kartı). Kalem `FinansIsKayitlari.PoOnay*` alanlarını, onaya sunulan grup `FinansPoOnayTalepleri` görüntüsünü taşır; karar `OnayBekleyenIslemler`'dedir. Şema betiği `20261017_01_Finans_Po_Is_Onayi.sql` (README sıra 20); sıfırlama betiği güncel tabloyu kurar. Yeni uç `POST api/finans/po-onay-talepleri` (PO açma sekmeleri W); yeni izin kodu yok.

- [FinansPoOnayKuraliTests](../../3K.Application.Tests/FinansPoOnayKuraliTests.cs) (veritabanısız): durum türetme (sunulmadı/bekliyor/ret/onaylı/uygulanamadı → düştü), tutar/para birimi değişince düşme ve tarih değişikliğinin düşürmemesi, kapsam (yalnız üretim kategorileri), kural kapalı ve kayıtsız (açık) varsayılanı, birleşik durum, PO seçicisinde onay durumuna göre ayrı grup, görüntü özetinin jsonb gidiş-dönüşü, ret mesajı, doğrulayıcı, iç komut sözleşmesi.
- [FinansPoOnayPostgresTests](../../3K.Application.Tests/FinansPoOnayPostgresTests.cs) (izole PostgreSQL, gerçek MediatR onay hattı): onaysız grup yeni PO'ya, mevcut PO'ya (kalem ve grup yolu) bağlanamaz; gönderme grup başına talep açar, özel iş atlanır, tekrar gönderme 409; onaycı detayı görür, kuralın onaycısı/talep edeni/karar vereni olmayan kişi göremez (404), detay yalnız onay kaydındaki talep eden ve projeyle eşleşen talepten okunur; talep eden kendi talebini onaylayamaz; onay kalemlere yansır ve PO açılır; ret nedeni görünür; talepten sonra değişen kalemde onay uygulanmaz; onaydan sonra tutar değişince kalan bakiye ve PO tutar artışı reddedilir, azaltma serbest. Kural kapalıyken PO serbest ve gönderme hemen onaylar; kural kaydı silinince onay aranır. Geçiş betiği PO'ya bağlı proje kayıtlarını uygulamanın net kuralıyla aynı tutarla onaylar (tarifeli, malzemeli, manuel, adetli), sipariş bekleyen/iptal PO'lu/özel işi onaylamaz; tekrarında geçiş, kural ve roller değişmez.
- [CanliyaAlimBetikleriTests](../../3K.Application.Tests/CanliyaAlimBetikleriTests.cs): betik canlı sırasıyla şemayı modele getirir, kuralı açık ve Admin onaycıyla ekler; yönetici kuralı kapatıp rolü kaldırınca tekrar çalıştırma bunları geri vermez; sıfırlama sonrası betik şemayı değiştirmez.
- [FinansAlanHttpSecurityTests](../../3K.Application.Tests/FinansAlanHttpSecurityTests.cs): `POST api/finans/po-onay-talepleri` PO açma sekmelerinin yazması olmadan (yalnız okumayla da) 403. `FinansPoOnayKuraliTests` ayrıca hiçbir API ucunun iç komutu almadığını ve görüntü özetinin yanıta yazılmadığını doğrular.
- PO mekaniğini sınayan mevcut PostgreSQL testleri (`FinansPoAkisPostgresTests`, `FinansPostgresTests`, `FinansSandikKirilimiTests`, `FinansTarifeDuzeltmePostgresTests`, `FinansVadeParaBirimiPostgresTests`) onayı konu almadığı için kurulumda kuralı kapatır (`FinansPostgresTests.PoOnayiniKapatAsync`); beklentileri değişmedi. `FinansSekmeYetkileriTests` tablosuna yeni istek eklendi.
- Ön yüz: `finans-dagitim.component.spec.ts` (onaysız grup seçilemez, neden yazılır, "Onaya gönder" ve bildirimler), `finans-po-onayi.spec.ts`, `finans-po-onay-detayi.component.spec.ts`, `onay-listesi.component.spec.ts` (onay penceresinde Finans detayı).

Koşum (Release): `(Finans|Onay|Ambalaj|ServerAuthorization|YetkiModeli|MenuAuthorization)&Category!=Postgres` 722/722; izole PostgreSQL'de `(Finans|Ambalaj|Onay|CanliyaAlim)&Category=Postgres` 49/49, 0 atlanan. Denetim sonrası düzeltmelerden (yanıtta özet yok, detayın talep eden/proje eşleşmesi, negatif testler) sonra `(Finans|Onay|ServerAuthorization)&Category!=Postgres` 459/459 ve PostgreSQL'de `FinansPoOnayPostgresTests|CanliyaAlimBetikleriTests|FinansPoAkisPostgresTests` 13/13, 0 atlanan. Ön yüz: `ng test` (finans-yonetimi, bildirim-merkezi, onay-yonetimi, finans-po-onay-detayi) 198/198; `ng build` (development) hatasız. Tam paketler koşulmadı. Ekranlar çalışan API ile tarayıcıda denenmedi (betik yerel veritabanına uygulanmadı).

## Ürün bilgisi 3K karşılama panelinde, birim/proje tipi metninde "?" düzeltmesi (U-249) — 14 Ekim 2026

Ürün bilgisi (seri no, SKT, lot) artık 3K karşılama panelinde, Açıklama alanının altında kendi "Ürün bilgisini kaydet" düğmesiyle girilir; karşılama Kaydet'inden bağımsızdır (işlenmiş satıra bilgi girmek yeni hareket üretmez). Sandık İçerik Yönetimi bilgiyi yalnız gösterir. Kayıt için 3K satırı `SandikId` taşır (`UcKUrunDto.SandikId`, yeni alan); sunucu ucu ve kuralı değişmedi. Sandık İçerik Yönetimi'nde miktarların altındaki "?": lookup önbelleği açılışta `LookupBirim` ve `LookupProjeTipi` tablolarını yüklemiyordu, önbellekte olmayan metin "?" döner. İkisi yüklenir; çeki yüklemesinden gelen tahsiste birim tutulmadığı için birim metni çeki satırının biriminden tamamlanır. Aynı düzeltme sevkiyat raporundaki birim ve sunucunun döndürdüğü proje tipi metinlerini de etkiler.

- [LookupOnbellekPostgresTests](../../3K.Application.Tests/LookupOnbellekPostgresTests.cs): `Postgres_Acilista_UygulamaninOkuduguButunLookuplarCozulur` (uygulamanın `GetDeger` ile okuduğu bütün lookup türleri "?" dönmez), `Postgres_SandikIcerigi_BirimiTahsisteYoksaCekiSatirindanGosterir`.
- Ön yüz: `uck-urunler-urun-bilgisi.spec.ts` (5 test: sandık/içerik ile kayıt, boşaltma null, ret, kilit/tahsissiz/izinsiz satırda kayıt yok, kilit nedeni), `sandik-detay.component.spec.ts` (sandık ekranı yalnız gösterir).

Koşum (Release): `(UcK|Sandik|CekiSatiriAnaVeri|ProjedenKarsilama|Lookup)&Category!=Postgres` 478 başarılı, 9 atlanan, 6 başarısız — başarısızların hepsi `CekiOrijinalMiktarTests.RevizyonU_SandikDegisikligi_TasimaUygunlugunuYeniEksikHesabindanOnceBelirler` (temiz HEAD'de de aynı 6 vaka başarısız, `DataTableReader` uint→string dönüşüm hatası; bu değişiklikle ilgisiz). İzole PostgreSQL'de `LookupOnbellekPostgresTests|SandikIcerikUrunBilgisiTests|SandikTahsissizSatirOnarimiBetigiTests` 18/18, 0 atlanan. Ön yüz `uck-urunler` + `sandik-detay` spec'leri 36/36; `ng build` (development) hatasız. Tam paketler koşulmadı.

## Çeki verisi düzenlemesinde tahsissiz satıra tahsis, hareket geçmişi ve tahsissiz satır onarımı (K-062–K-064) — 14 Ekim 2026

Depo Durumu'nda PA600-51 Öz Nakliyat 22, Sandık Yönetimi'nde 24 sandık görünüyordu. Çekide sandık numarası boş gelen (hiç `SandikIcerik` tahsisi olmayan) iki satıra "Çeki verisi düzenle" ekranından 36/37 verilmiş; ekran sandığı oluşturmuş ama tahsis oluşturmamıştı. Ürün sandığa yalnız numarasıyla bağlı kaldı: sandık listesi/kartı ve lokasyon kuralı sentetik içeriği (`SandikService`), 3K ekranı satırın sandık numarasını okuyor; Depo Durumu proje satırı sayımı ise yalnız fiziksel tahsisi okuyor (K-064). Excel U revizyonu aynı durumda tahsis açıyordu (K-051); ana veri ekranı ise tahsis açmıyor, hareket geçmişi de yazmıyordu.

- **Değişen davranış (K-062):** `CekiSatiriAnaVeriGuncelleCommand`, etkin sandık numarası değişen satırın hiç tahsisi yoksa hedef sandıkta (bulunan veya oluşturulan) tahsis açar. Değerler yazmadan önce `UcKSandikIcerikSenkronizasyonHelper.Yansit` ile hesaplanır: Tahsis = yeni istenen; Konulan = max(Gelen + Stok + Proje + Tedarikçi − ProjeGonderilen, 0); Stok/Proje/Tedarikçi kırılımı bu sırayla konulana sığacak kadar; Eksik = Tahsis − Konulan; birim istekteki birim. `Yansit`, `SenkronizeAsync`'in veritabanına dokunmayan hesap kısmıdır; `SenkronizeAsync` davranışı değişmedi. Sandık numarası değişmeden yapılan düzenleme tahsis oluşturmaz.
- **Değişen davranış (K-063):** Alanlar değiştiyse veya tahsis oluştuysa aynı transaction'da `HareketGecmisi` yazılır ("Çeki Verisi Düzenlendi", `UrunGuncellendi`, "CekiSatiri"; eski/yeni değer sıra, barkod, poz, açıklama, miktar+birim, sandık özeti; tahsis oluştuysa açıklama bunu söyler). Ret durumunda kayıt ve hareket yazılmaz. Handler artık `IHareketService` ve `ICurrentUserService` alır.
- **Veri onarımı (K-064):** `scripts/database/20261014_03_Sandik_Tahsissiz_Satir_Onarimi.sql` (README sıra 19) tahsisi olmayan, etkin sandık numarası projede tek ve sevk edilmemiş sandıkla eşleşen satırlara aynı değerlerle tahsis ekler (`CreatedBy` = betik adı, hareket yazılmaz); sandık yok / birden çok sandık / sevk edilmiş sandık NOTICE ile atlanır; tekrar çalıştırılabilir. `kontrol/Betik_Durumu.sql` 19. satırı onarılabilir satır kalmadıysa "uygulanmış" gösterir. Ana oturumun bildirdiği yerel kopyadaki etki: PA600-51 sıra 73→36, 74→37 ve PA651-01 sıra 200→17 onarılır; PA600-50 sıra 73/74 (sandıkları sevk edilmiş) atlanır. Canlı veritabanında uygulama kullanıcıya aittir; bu kayıt canlı uygulamayı doğrulamaz.

Testler:

- [CekiSatiriAnaVeriTahsisTests](../../3K.Application.Tests/CekiSatiriAnaVeriTahsisTests.cs): yeni `TahsissizSatiraSandikNumarasiVerilince_HedefSandiktaTahsisOlusur` (hedef sandık var/yok; K-062, K-063), `TahsissizKismenGelmisSatir_YeniTahsisteKalanEksikOlarakKalir` (K-062), `AnaVeriDuzenlemesi_EskiVeYeniDegerleHareketGecmisineYazilir` (K-063). Mevcut ret testlerinin `TumKayitlar` karşılaştırması artık hareketleri de içerir; hareket yazılmadığını da doğrular.
- [SandikTahsissizSatirOnarimiBetigiTests](../../3K.Application.Tests/SandikTahsissizSatirOnarimiBetigiTests.cs) (izole PostgreSQL): `Postgres_TahsissizSatirlar_TekSevkEdilmemisSandigaBaglanir_TekrarindaDegismez` (K-064).
- [GridUcKParcaliSevkPartisiRegresyonTests](../../3K.Application.Tests/GridUcKParcaliSevkPartisiRegresyonTests.cs) ve [SahaKaynakAnaVeriKorumaTests](../../3K.Application.Tests/SahaKaynakAnaVeriKorumaTests.cs): yalnız handler kurucusunun yeni bağımlılıkları eklendi; beklentiler değişmedi.

Koşum (Release, ana oturumda; bu doküman güncellemesinde testler yeniden koşulmadı): `(CekiSatiriAnaVeri|GridUcK|SahaKaynakAnaVeri|UcK|ProjedenKarsilama)&Category!=Postgres` 270/270; izole PostgreSQL sunucuda `SandikTahsissizSatirOnarimiBetigiTests|CanliyaAlimBetikleriTests|CekiSatiriAnaVeri` 68/68, 0 atlanan. Tam paketler koşulmadı. Ekranlar arası okuma farkı (sandık listesi/kartı sentetik, depo sayımı fiziksel) testle sabitlenmedi. API sözleşmesi (istek/yanıt) değişmedi; şema değişikliği yok.

## 3K "Projeden Karşılandı" kaynak listesi 3K izniyle (U-250–U-255) — 14 Ekim 2026

3K panelinde kaynak projenin ürün listesi artık Grid ekranının ucundan (`GET api/Grid/{kaynakProjeId}/urunler`, kaynak projede `grid-modulu` R) okunmaz. Grid izni olmayan 3K personeli 403 alıyor, ön yüz hatayı yutup "Bu projede eşleşen isimli veya 3K'ya gelmiş ürün bulunamadı." gösteriyordu (canlıda tablette görülen vaka). Yeni uç `GET api/UcK/{hedefProjeId}/projeden-karsilama-kaynaklari?kaynakProjeId=` (`GetProjedenKarsilamaKaynaklariQuery`), karşılamayı kaydeden `UcKDurumGuncelleCommand` ile aynı izni ister: hedef projenin gerçek türündeki 3K menüsünün W'si. Kaynak projenin Grid izni sorulmaz. Kimlikler pozitif olmalı, kaynak hedeften farklı olmalıdır ("Bir proje kendi ürünlerinden karşılama yapamaz."). Kaynak proje yoksa 404 döner; ürünsüz kaynak proje 200 ve boş liste alır (Grid ucu 404 verir). Manuel saha/yedek içerikleri listelenmez. Kalan satırların yedi alanı Grid listesiyle aynıdır, çünkü hesap ortak `GridUrunListesiOkuyucu` içindedir (`GetGridUrunlerQueryHandler` davranışı değişmedi; katalogda G-079–G-088 ve K-054 kaynak satırları okuyucuya taşındı). Ön yüz istek sürerken "yükleniyor" gösterir, reddi takip koduyla kırmızı hata olarak gösterir, proje değişince eski liste/seçim ve geç gelen yanıtı bırakır. Ad eşleşmesi ve pozitif kullanılabilir süzgeci ön yüzde aynen kaldı. Şema betiği ve yeni izin kodu yok.

- [ProjedenKarsilamaKaynaklariTests](../../3K.Application.Tests/ProjedenKarsilamaKaynaklariTests.cs): `KaynakSatirlari_GridListesiyleAyniSatirVeMiktarlariDondurur` (U-253), `ManuelSandikIcerigi_KaynakSecenegiOlarakSunulmaz` (Saha/Yedek, U-254), `UrunsuzKaynakProje_BosListeDoner_GridGibi404Vermez` ve `KaynakProjeYoksa_404Doner` (U-252), `Validator_KendiProjesindenVeGecersizKimliktenKaynakListesiAcmaz` (U-251).
- [ServerAuthorizationRegressionTests](../../3K.Application.Tests/ServerAuthorizationRegressionTests.cs): `ProjedenKarsilamaKaynaklari_HedefProjenin3KYazmaIzniyleAcilir_GridIzniSorulmaz` (Normal/Saha/Yedek → `3k-modulu`/`saha-3k-modulu`/`yedek-3k-modulu` W), `ProjedenKarsilamaKaynaklari_3KYazmaIzniOlmadanAcilmaz` (`3k-modulu` R, `grid-modulu` W → 403), `ProjedenKarsilamaKaynaklari_OlmayanHedefProjeIleAcilmaz` (U-250).
- Ön yüz: `uck-urunler-kaynak.spec.ts` (5 test, U-255 ve U-123'ün sıfıra kırpma kısmı).

Koşum (Release, bu doküman güncellemesi sırasında): `ProjedenKarsilamaKaynaklariTests|ServerAuthorizationRegressionTests` 54/54, 0 atlanan. Bunun içinde yeni testler (`ProjedenKarsilamaKaynaklariTests` 9 + `ServerAuthorizationRegressionTests.ProjedenKarsilamaKaynaklari*` 6) 15/15. Grid listesi ortak okuyucuya taşındığı için `GridSozlesmeVeSorguIsKurallariTests|GridUcKCokSandikGeriGonderimRegresyonTests|GridUcKSatirMiktarSenkronizasyonTests` 27/27, 0 atlanan. Ön yüz: `ng test --include=src/app/features/uck/uck-urunler/**/*.spec.ts` (ChromeHeadless) 29/29 — yeni `uck-urunler-kaynak.spec.ts` 5 test ve kurulum sinyalleri eklenen `uck-urunler-sevk-partisi.spec.ts` dahil; `ng build` (development) hatasız. Tam paketler koşulmadı. Controller ucu HTTP/rota testiyle ayrıca denenmedi.

## Canlıya alım betik provası (README sıra 9–18) — 14 Ekim 2026

Bu sürümün betikleri önceki sürümün (master) şemasında ve eski Finans yetki ağacında ad sırasıyla çalıştırıldığında şema EF modeliyle aynı olur; betiklerin hepsi tekrar çalıştırıldığında hiçbir şey değişmez. Tekrar çalıştırma sorunları giderildi: `20261008_03` eski alt kodları `20261013_02` sildikten sonra hata veriyordu (eski kod kontrolü artık yalnız eklenecek sekme varken yapılır); `20261013_02` onay ayarı oturumda açıkken tekrar çalıştırılınca `20261013_03`'ün alt sekme düğümlerini silebiliyordu (`finans-sekme-*` düğümleri silinmez) ve Admin'in düşürülmüş sekme iznini W'ye geri yükseltiyordu (silinecek kod yoksa seviye değişmez); `20261008_03`, `20261008_05`, `20261013_03` ve `20261014_01` tekrarında yöneticinin sonradan kaldırdığı izni (rol ekranı N'yi satır silerek kaydeder) geri veriyordu (izin artık yalnız düğüm o çalıştırmada eklenince verilir).

- [CanliyaAlimBetikleriTests](../../3K.Application.Tests/CanliyaAlimBetikleriTests.cs): `Postgres_Betikler_CanliSirasiylaSemayiModeleGetirir_TekrarindaYoneticiKararlariniKorur` — master şeması (bu sürümün tablo/kolon/kısıt/indeksleri kaldırılmış) + canlı Finans ağacı; `20261013_02` önce onaysız (hata, hiçbir şey değişmez), sonra oturumda onayla; sonuç şeması EF modeliyle kolon/kısıt/indeks düzeyinde aynı (varsayılan değer hariç); Finans altında yalnız 8 sekme + vade, sekmeler altında yalnız 8 alt sekme; Sevkiyatlar ve fotoğraf düğümleri Projeler altında. Salt okunur durum sorgusu `scripts/database/kontrol/Betik_Durumu.sql` klasördeki her betiği listeler; öncesinde 9–18 `UYGULANMAMIŞ`, 1–8 uygulanmış (4 tespit edilemez), sonrasında hepsi uygulanmış. Ardından yönetici kararları (Sevkiyatlar, fotoğraf, alt sekme, Admin sekme/alt sekme/vade izni kaldırma, Admin Panel W → R) ve onay açıkken bütün betiklerin tekrarı: menü ve rol izinleri aynı kalır.
- `Postgres_Sifirlama_TekBasinaSemayiModeleGetirir_SonrakiBetiklerDegistirmez`: güncel şemada sıfırlama (`20260929_01`) durmadan çalışır (dışarıdan Finans/Ambalaj'a yabancı anahtar yok) ve tek başına EF modeliyle aynı şemayı kurar (2026-10-15'ten beri ahşap/kontra ve malzemeli tarife kolonları dahil); ardından `20261008_02` ve `20261013_01` çalıştırılsa da şema değişmez.
- [FinansSekmeYetkileriTests](../../3K.Application.Tests/FinansSekmeYetkileriTests.cs): `Postgres_GocBetigi_…` — sekme düğümleri varken eksik eski kod durdurmaz; eklenecek sekme varken durdurur.

Koşum (Release, PostgreSQL izole sunucuda): `CanliyaAlimBetikleriTests|FinansSekmeYetkileriTests|TopluSevkEtTests|ProjeFotografBaglantisiTests` 143/143, 0 atlanan. Betikler canlı veritabanının kopyasında denenmedi; sıra 1–8'in canlıda uygulanmış olduğu varsayıldı.

## Sevkiyat raporları (dönem PDF/Excel, sefer PDF) — 14 Ekim 2026

Sevkiyatlar sayfasından rapor alınır (`sevkiyatlar` R; yeni izin ve şema yok). Dönem raporu sefer listesiyle aynı seferleri seçer (`SevkSeferiOkuyucu.Suz`, liste de aynı yöntemi kullanır): tarih ve proje tipi sorguda; plaka normalize içerir, proje no seferdeki projelerden birinde geçer ve sefer bütün projeleriyle gelir. PDF: özet kutuları, araç (plaka) özeti, sefer × proje tablosu (sandık numaraları; sefer sayfa sonunda bölünmez). Excel: Seferler, Araçlar, Projeler, Sandıklar (ölçü, net/brüt kg, tip, depo, düzeltme açık) ve Ürünler (poz, malzeme kodu, sandık miktarı, konulan, eksik, birim) sayfaları. Sefer PDF'i: araç/dorse/şoför/gönderen, proje proje sandık tablosu, imza alanı; eski kayıt plaka + sevk günüyle bulunur. Sandık miktarı sandık içeriği ekranının kuralıdır (`SandikTahsisHelper.HesaplaSandikMiktari`, Grid kapanmışsa tamamı konulmuş). En fazla 1000 sefer; uymayan süzgeç 404. Ölçü/ağırlık/içerik raporun alındığı andaki kayıttır (sevk anı görüntüsü saklanmaz); "geri alınarak açılan" sandık sevkiyattan çıktığı için rapora girmez.

- [SevkiyatRaporTests](../../3K.Application.Tests/SevkiyatRaporTests.cs): `Model_AracOzetiPlakayaGoreToplar_AgirligiOlmayanSandikToplamaKatilmaz`, `Excel_BesSayfaliDokum_SeferProjeSandikVeUrunSatirlariDogruDoner`, `Pdf_DonemVeSeferRaporuUretilir_BosAlanlarHataVermez`, `Bicim_PdfVeExcelKabulEdilir`, `Dogrulayici_GecersizBicimTersTarihVeSefersizIstegiReddeder`, `Yetki_RaporlarSevkiyatlarOkumaIzniIster`, `Postgres_DonemVeSeferRaporu_SuzgecSandikVeIcerikleriKurar` (gerçek SQL: plaka/proje no süzgeci, eski kayıt, doğal sandık sırası, tahsis ve Grid kapalı miktarı, saha elle eklenen malzeme, gönderen = seferi kaydeden, tek sefer ve eski kayıt PDF'i, 404/400).
- [TopluSevkEtTests](../../3K.Application.Tests/TopluSevkEtTests.cs): sefer listesi ortak süzgeçle aynı sonucu verir.
- Ön yüz: `sevkiyat.service.spec.ts` (rapor istekleri blob, süzgeç parametreleri), `sevkiyatlar.component.spec.ts` (seferler görünümünde bütün süzgeçler, istatistik görünümünde yalnız tarih; dosya adları; hata mesajı; sefer PDF düğmesi).

Koşum (Release, PostgreSQL izole sunucuda): `SevkiyatRaporTests|TopluSevkEtTests|ServerAuthorization|ButunKorumali` 62/62, 0 atlanan. Ön yüz: projeler + core spec'leri 104/104. Tam paketler koşulmadı.

## Proje fotoğraf bağlantısı ve hacim gösteriminde bileşik m³'ün kaldırılması — 14 Ekim 2026

Projelere fotoğraf sayfası bağlantısı eklendi (`Projeler."FotografBaglantisi"`, `20261014_01_Proje_Fotograf_Baglantisi.sql`). Tek izin kodu `proje-fotograf-baglantisi` (kök düzeyde, rotasız; Normal/Saha/Yedek listelerinde aynı): R bağlantıyı görür ve açar, W ekler/değiştirir/kaldırır; yalnız Admin W alır. Adres kuralı `ProjeFotografBaglantisiKurali`: yalnız http/https, en fazla 1000 karakter, boşluk yok; şemasız adrese https eklenir; boş değer kaldırır. Liste (`ProjeListeleQuery`) bağlantıyı yalnız R izniyle döndürür.

- [ProjeFotografBaglantisiTests](../../3K.Application.Tests/ProjeFotografBaglantisiTests.cs): `Kural_HttpVeHttpsAdresiniKabulEder_SemasizAdreseHttpsEkler`, `Kural_TehlikeliVeyaBozukAdresiReddeder` (javascript:, data:, ftp, file, boşluklu, şemasız boş), `Kural_BosDegerBaglantiyiKaldirir_UzunlukSiniriUygulanir`, `Dogrulayici_GecersizAdresiVeProjesizIstegiReddeder_BosuKaldirmaSayar`, `Komut_YalnizFotografBaglantisiYazmaIzniyleCalisir_ProjeTipiKoduYetmez` (gerçek `AuthorizationBehavior`, 403), `Komut_BaglantiyiKaydeder_DegisiklikGecmisineYazar_BosGonderilinceKaldirir` (eklendi/güncellendi/kaldırıldı hareketleri, aynı adreste hareket yok, 404), `Liste_BaglantiyiYalnizOkumaIzniOlanaGosterir` (izin yoksa ve oturum kimliği yoksa boş), `Postgres_Betikler_KolonuVeDugumuProjelerAltindaKurar_YalnizAdmineYazmaVerir_TekrarCalisir` (kolonu betik ekler: varchar(1000) boş olabilir; düğüm 5256 rotasız, `20261014_02` ile Projeler altında Sira 9 ve ikonsuz; yalnız Admin W; ikinci çalıştırma değiştirmez; EF eşlemesiyle okuma/yazma).
- [SahaProjeTamamlanmaRegresyonTests](../../3K.Application.Tests/SahaProjeTamamlanmaRegresyonTests.cs): liste handler'ının yeni bağımlılıklarıyla (rol servisi, kullanıcı) mevcut beklentiler değişmeden geçer.
- Ön yüz (`proje-listesi.component.spec.ts`): adres kuralı sunucuyla aynı; R ile yeni sekmede açılan "Fotoğraflar" çipi (`rel=noopener`), izin yoksa çip/düğme yok; W ile şemasız adres https ile gönderilir ve satır yerinde güncellenir, geçersiz adreste Kaydet kapalı; kaldırma onay ister ve boş gönderir; R düzenlemeyi açmaz.
- Hacim gösterimi (yalnız ön yüz, hesap değişmedi): ahşap ve kontra m³ toplanıp "bileşik m³" olarak gösterilmez. Ahşap kapalı sandıkta yalnız ahşap m³, kontrplak/katlanır sandıkta (veya kontra girildiyse) ahşap ve kontra ayrı görünür; fiyat önizlemesi "tarife × (ahşap + kontra)" bileşenleriyle yazılır. Spec: `finans-kayit-detay`, `finans-munferit-isler`, `finans-urun-kaydet`.

Koşum (Release): `ProjeFotografBaglantisiTests|SahaProjeTamamlanmaRegresyonTests|ServerAuthorization|ButunKorumali|YetkiModeliTests` 97/97; `ProjeFotografBaglantisiTests` izole PostgreSQL ile 16/16 (0 atlanan). Ön yüz: projeler + finans + core + rol yönetimi spec'leri 293/293. Tam paketler koşulmadı. Betik canlı ağacın kopyasında denenmedi.

## Finans alt sekme yetkileri, Ambalaj "gerekli sandık" sayacı ve özel sandık finans durumu/geçmişi — 14 Ekim 2026

**Finans alt sekme yetkileri (Y-03).** Üç Finans sekmesinin uçları alt sekme düğümüne bağlandı (`YetkiKodlari.Finans` altında 8 yeni `Sekme*` kodu, kümeler `FinansSekmeleri`): İş Takibi → Aylık görünüm (aylık iş listesi, proje–sandık kırılımı, aylık değer, tek seferlik iş; aylık ve sipariş durum rapor dosyaları Raporlar ile `Any`), Tüm dönemlerde ara (yalnız ön yüz kapısı; genel arama ucu kayıt sekmeleri kümesiyle açılır), Bekleyen işler (yaşlandırma; Panel ve Raporlar ile `Any`); Tanımlar → Düzenli İşler, Ürün ve Tarifeler (ürün/tarife, yeniden fiyatlama, tarife düzeltme ve önizlemesi), Para Birimi ve Vade (para birimi ve vade parametresi kaydı; parametre okuması Faturalar ile `Any`); PO/Fatura → PO'lar (PO düzeltme/iptal, PO açma kümesi) ve Faturalar (fatura listesi, giriş, düzeltme, iptal, ödeme). PO listesi PO'lar veya Faturalar ile açılır; belge yükleme ve kalıcı silmede PO → PO'lar, fatura → Faturalar alt sekmesi. Sekme düğümü alt sekmenin tavanıdır; sekmenin W'si alt sekme iznini kendiliğinden vermez. İş kaydı düzenleme ile iş kaydı/PO/fatura detayı sekme düzeyinde kaldı. Betik `20261013_03_Finans_Alt_Sekme_Yetkileri.sql` (yıkıcı değil, tekrar çalıştırılabilir) 8 `MenuTanimlari` düğümünü (Id 5248–5255) ekler ve her rolün sekmedeki R/W seviyesini alt sekmelere kopyalar; bugünkü etkin yetkiler değişmez. 13 Ekim bölümündeki "W'si sekmenin bütün işlemlerini açar" ifadesi bu üç sekmede artık alt sekme W'siyle birlikte okunur.

- [FinansSekmeYetkileriTests](../../3K.Application.Tests/FinansSekmeYetkileriTests.cs): istek → sekme/alt sekme tablosu (`FinansIstegi_SekmeKumesindenHerhangiBirininIzniniIster`, `TumFinansIstekleri_SekmeTablosundadir`); sekmenin alt sekme tavanı olması ve alt sekme kaydı yokken N (`SekmeDugumu_AltSekmeninTavanidir`); sekme W'sinin alt sekmeye bağlı sorgu ve işlemi açmaması, sekme R altında alt sekme W'nin R'ye inmesi, kardeş alt sekme işleminin ve sekmesiz alt sekme izninin açmaması (`AltSekme_SekmeYazmasiAltSekmeIzniVermez_SekmeOkumasiAltSekmeYazmasininTavanidir`); PO'lar/Faturalar alt sekmelerinin birbirinin işlemini (belge yükleme, kalıcı silme dahil) açmaması (`PoVeFaturaAltSekmeleri_BirbirininIsleminiAcmaz`); hedef kayıt türüne göre (alt) sekme (`KayitBagliIslem_HedefKaydinSekmesininYazmaIzniniIster`); fikstürün betik ve `YetkiKodlari` ile aynı olması (`TestFiksturu_AltSekmeBetigiVeYetkiKodlariylaAyni`); gerçek PostgreSQL'de `Postgres_AltSekmeBetigi_DugumleriSekmeAltinaEkler_RolSeviyesiniKopyalar_TekrarCalisir` (düğümlerin doğru sekme altında olması, rol seviyesinin kopyalanması, sekme N/kayıtsızken satır eklenmemesi, etkin seviyelerin değişmemesi, kimlik dizisi, ikinci çalıştırmanın hiçbir şeyi değiştirmemesi ve sonradan indirilmiş alt sekme iznini ezmemesi, sekme düğümü eksikse hiçbir şey yapmadan durması).
- [FinansAlanHttpSecurityTests](../../3K.Application.Tests/FinansAlanHttpSecurityTests.cs) (`Http_OlusturmaYanitiSahteSifirUretmez_SekmeYazmasiylaFiyatGorunur`): gerçek Kestrel + JWT sınırında PO/Fatura sekmesi W + Faturalar alt sekmesi W ile PO açma 403 ve servis çağrılmaz; PO'lar alt sekmesi verilince açılır ve birim fiyat görünür.
- Tekil istek → alt sekme kodu: yukarıdaki sekme tablosu (`FinansIstegi_SekmeKumesindenHerhangiBirininIzniniIster`) her isteği sınar. Aynı eşlemeyi tek tek tekrarlayan testler (`FinansModuluTests`, `FinansSandikKirilimiTests`, `FinansTarifeDuzeltmeTests`, `FinansVadeKurallariTests` içinde) 18.10.2026'da kaldırıldı.
- "Tüm dönemlerde ara" alt sekmesinin sunucuda karşılığı yoktur; bu alt sekmesi kapalı ama İş Takibi R'si olan kullanıcı genel arama ucunu doğrudan çağırabilir. `20261013_03` betiği yalnız test fikstüründeki ağaçta otomatik sınandı; canlı ağacın kopyasında provası bu kayıtta yoktur.

**Ambalaj planı "gerekli sandık" sayacı.** Plan ekranındaki "X / Y gerekli sandık" sayacı (`AmbalajPlanlamaYardimcisi.PlanDtoOlustur`) ambalaj kaydı henüz oluşmamış projede 0/0 gösteriyordu. Artık kayıt tabanlı gereksinimlere ek olarak hiçbir kaydın `KaynakKayitId`'siyle bağlanmamış (senkronize edilmemiş) kaynak sandıkları da sayar: varsayılan "Yapılmaz" sandık (`AmbalajUretimPolitikasi.VarsayilanYapilmazMi`) sayılmaz, kayda bağlanmış kaynak kaydıyla bir kez sayılır, planın genel durumu Tamamlandı ise kayıtsız kaynak eklenmez. Grup kırılımında kayıtsız kaynak proje tipine ve plan başlangıcına göre (`AmbalajUretimPolitikasi.KaynakGrubu`) gruplanır; özel sandıklar "Özel sandık" grubundadır. Ambalaj üretim hesabı katalog kapsamı dışında olduğundan kural kodu yoktur.

- [AmbalajPlanlamaSayfalamaTests](../../3K.Application.Tests/AmbalajPlanlamaSayfalamaTests.cs) (`GerekliSandik_KayitsizKaynaklariSayar_VarsayilanYapilmazVeKayitliKaynagiTekrarSaymaz`): kayıtsız planda 2/0; tamamlanmış kayıt + özel sandıkla 4/1, kırılım Normal 2/1 + Özel sandık 2/0. Genel durum Tamamlandı iken kayıtsız kaynağın sayılmaması ve Saha/Yedek/İlave grup ayrımı ayrıca test edilmedi.

**Özel sandık listesi: finans durumu, denetim bilgisi ve kayıt geçmişi.** `GetAmbalajBagimsizSandiklarQuery.FinansDurumu` ("bekleyen" = finansa aktarılmamış, "aktarilan", boş = hepsi; başka değer 400) listeyi süzer; özet, arama ve tür filtresine göre iki sekmenin sayısını (`FinansaAktarilmayanKayitSayisi`, `FinansaAktarilanKayitSayisi`) finans durumu süzgecinden önce verir. Satır oluşturan ve oluşturma tarihini, finansa aktaran ve aktarım tarihini (son "Özel sandık finansa aktarıldı" hareketi; eski yolla üretime alınmış kayıtta aktaran bilinmez, tarih üretim tarihidir) taşır. Yeni uç `GET api/ambalaj/kayitlar/{id}/gecmis` (`GetAmbalajKayitGecmisiQuery`, `YetkiKodlari.Ambalaj.Listele` R) kaydın `AmbalajUretimHareketi` satırlarını işlem grubu başına tek olayda (en yeni üstte) Türkçe alan etiketi ve okunur değerle verir; iç teknik alanlar gösterilmez, ölçü/m³/sarf/kaynak alanları görünüm izni yoksa listeye girmez; kullanıcının görebildiği üretim durumunda olmayan kayıt 404'tür. Şema değişikliği yoktur; Ambalaj katalog kapsamı dışında olduğundan kural kodu yoktur.

- [AmbalajSandikTipiVeTamamlamaTests](../../3K.Application.Tests/AmbalajSandikTipiVeTamamlamaTests.cs) (`Ozel_sandik_listesi_finans_durumuna_gore_suzulur_denetim_ve_gecmis_gosterilir`): bekleyen/aktarılan süzgeci ve (1, 1) sayıları, oluşturan/aktaran adı ve tarihleri, bekleyen satırda aktarım tarihinin boş olması, geçersiz süzgecin reddi; geçmişte en yeni olayın aktarım işlemi ve kullanıcısı, "Üretime alındı / finansa aktarıldı" Hayır → Evet ve "Üretim durumu" Planlandı → Üretimde, oluşturma olayında okunur yapı adı ve teknik alanın gizlenmesi, olmayan kayıtta 404. `Ozel_sandik_gecmisi_olcu_ve_m3_iznine_gore_alan_gizler_gorulemeyen_durumda_404_doner`: ölçü ve m³ izni olmayan kullanıcıda geçmişte Boy/En/Yükseklik ve m³ alanları yoktur (sandık no görünür); bekleyen kayıtları göremeyen kullanıcı Planlandı kaydın geçmişinde 404 alır. Testler bellek içi çalışır: sarf/kaynak alanlarının izne göre gizlenmesi, eski kayıtta aktarım tarihinin üretim tarihine düşmesi, HTTP rotası ve sorguların PostgreSQL çevirisi test edilmedi. Liste DTO'suna eklenen denetim alanları (tarih, ad) ölçü/m³ değildir; `AmbalajAlanMaskesi`/`AmbalajAlanMaskelemeTests` değişmedi. Koşum: `AmbalajSandikTipiVeTamamlamaTests|AmbalajPlanlamaSayfalamaTests|AmbalajAlanMaskelemeTests` 62/62 (Release).

**Ön yüz.** Rol editöründe sekme açılınca/yükseltilince aynı seviyedeki alt sekmeler onu izler, kapatılmış alt sekme yükseltmede kapalı kalır (`menu-tree.component.spec.ts`); `finans-izinleri.spec.ts` sunucudaki sekme/alt sekme kümelerinin aynasıdır (alt sekme tavanı, PO'lar/Faturalar ayrımı, vade uyarısının Faturalar alt sekmesinden bağımsızlığı). Yalnız ön yüzde: İş Takibi aylık listesi yalnız proje satırlarını gösterir ve sağda proje paneli açar (özet + Kategoriler | Sandıklar (arama ve durum süzgeci) | PO / Fatura sekmeleri; `finans-aylik-po-akisi.spec.ts`); vade uyarısı metni gün penceresini göstermez ("Vadesi yaklaşan {0} fatura var"); ambalaj yaşam döngüsü ekranında değişiklik geçmişi istenince yüklenir (`uretim-yasam-dongusu.component.spec.ts`). Ön yüz finans + ambalaj + rol yönetimi + çekirdek spec'leri 313/313.

Koşum (Release, PostgreSQL vakaları izole sunucuda, port 55439): `FullyQualifiedName~Finans|FullyQualifiedName~Yetki|FullyQualifiedName~Ambalaj|FullyQualifiedName~Authorization` → 745/745 başarılı, 0 atlanan (`FinansSekmeYetkileriTests.Postgres_AltSekmeBetigi_…` dahil). Kural–test eşlemesi denetimi: 508 kural doğrulandı. Tam paketler koşulmadı.

## Finans yalnız sekme yetkisi, malzemeli tarife, özel sandığın finansa aktarılması ve münferit iş modeli — 13 Ekim 2026

**Finans yetkisi yalnız sekme (Y-03).** Finans'ta 33 alt işlem/alan kodu ve yanıt alan maskesi (`FinansAlanErisimFiltresi`, `FinansAlanProjeksiyonu`, `IAlanErisimService`) kaldırıldı. Her finans isteği sekme kümesinden herhangi birinin iznini ister (sorgu R, işlem ve işlem önizlemesi W); sekmenin R'si bütün alanları (tutar, fiyat, gelir, gider, kârlılık, ölçü, m³) gösterir, W'si sekmenin bütün işlemlerini (kalıcı silme dahil) açar. Rapor/belge dosyaları ayrıca alan izni istemez. İş Takibi üstündeki vade uyarısı ayrı `finans-vade-uyarisi` R iznidir. Göç `20261013_02_Finans_Sekme_Yetkisine_Gecis.sql` (onaylı, yıkıcı: önce sekme seviyelerini yükseltir, sonra alt kodları rol izinleriyle siler). Bu bölümün altındaki 4–10 Ekim kayıtlarında geçen alt kod, "alan maskesi", "fiyat değiştirme izni", yetkisiz kullanıcıya `null` dönen alan ve `FinansAlanProjeksiyonuTests` ifadeleri artık geçerli değildir; tarihsel kayıt olarak bırakılmıştır. *(14 Ekim 2026'da genişletildi: İş Takibi, Tanımlar ve PO/Fatura uçları alt sekme iznine bağlandı, sekmenin W'si alt sekme W'si vermez; bkz. 14 Ekim bölümü.)*

- [FinansSekmeYetkileriTests](../../3K.Application.Tests/FinansSekmeYetkileriTests.cs): modül kökünün sekme tavanı olması, her finans isteğinin sekme kümesi (`Any`) ve bütün isteklerin tabloda bulunması, kayda bağlı işlemin (belge yükleme, kalıcı silme ve önizlemesi) hedef kaydın sekmesinin W'sini istemesi, sekme R/W'nin gerçek yetki değerlendiricisiyle sorgu/işlem/kalıcı silme sonucu, ortak uçların küme içi ve küme dışı sekmelerle davranışı, vade uyarısının yalnız kendi R izniyle açılması; eski `20261008_03` göçünün PostgreSQL testi.
- [FinansAlanHttpSecurityTests](../../3K.Application.Tests/FinansAlanHttpSecurityTests.cs): gerçek Kestrel + JWT sınırında sekme okumasıyla fiyat/tutar/ölçü/m³ ve panel gelir/gider/kâr alanlarının maskesiz dönmesi, dosyaların yalnız sekme okumasıyla üretilmesi, sekme kapalıyken işlem ve sekmeye özel okumanın 403 olması, vade uyarısı izni, münferit iş için sekme W'sinin yetmesi.
- [FinansModuluTests](../../3K.Application.Tests/FinansModuluTests.cs) ve [YetkiModeliTests](../../3K.Application.Tests/YetkiModeliTests.cs): istek → sekme kodu eşlemesi; PO açmanın fiyat girilse de yalnız sipariş açılan sekmeleri istemesi; ağaç tavanı testleri `PoGir` yerine `SekmePoFatura` ile.
- `20261013_02` göç betiğinin otomatik testi **yoktur**; canlı ağacın kopyasında ayrıca prova edilmelidir.

**Hacim gerekçesi isteğe bağlı.** Elle ahşap/kontra m³ girişinde gerekçe boş bırakılabilir (`FinansGerekceKurali.IstegeBagliGecerli`, yazılırsa en fazla 1000 karakter); `HacimGirAsync` denetim kaydına "Gerekçe" satırını yalnız gerekçe verildiyse yazar. Kanıt: `FinansPostgresTests.KontraHacim_ElleGirilir_BilesikToplamTarifeyleFiyatlanir_AktarimEzmez` (PostgreSQL).

**Malzemeli tarife.** Tarife eşleşmesine malzeme eklendi (`FinansUrunEslesmesi.Malzeme`: Ahşap/Kontra/boş). Kontrplak/katlanır sandıkta net = kontra m³ × kontra tarifesi + ahşap m³ × ahşap tarifesi (`FinansTutarKurallari.IsNet`/`MalzemeliNet`, SQL eşi `FinansSorgulari.IsNetIfadesi`); kontra tarafına tarife yoksa genel m³ tarifesine düşer; ahşap kapalı sandık ahşap tarifesini tek tarife olarak kullanır; katlanır sandık boyutla eşleşmez; malzemeli eşleşme yalnız m³ tarifede tanımlanır. Hacim girilince tarifeden fiyatlanan iş yeniden fiyatlanır; ahşap tarifesi sonradan tanımlanınca geriye dönük doldurma ahşap tarafını tamamlar; kısmi PO m³ oranıyla pay alır; tarife düzeltme/yeniden fiyatlama yalnız düzeltilen sürümün tarafını değiştirir. Şema `20261013_01_Finans_Malzemeli_Tarife.sql` (betiği çalıştıran otomatik test yoktur).

- [FinansTutarKurallariTests](../../3K.Application.Tests/FinansTutarKurallariTests.cs) (`Malzemeli_net_ahsap_ve_kontra_m3_u_kendi_tarifesiyle_fiyatlar_sql_ile_ayni`), [FinansTarifeDuzeltmeTests](../../3K.Application.Tests/FinansTarifeDuzeltmeTests.cs) (`Malzemeli_kalemde_duzeltme_yalniz_surumun_tarafini_degistirir`), [FinansUrunTarifeTests](../../3K.Application.Tests/FinansUrunTarifeTests.cs) (`Malzemeli_eslesme_m3_tarifede_ahsap_veya_kontra_olmalidir`), [FinansPostgresTests](../../3K.Application.Tests/FinansPostgresTests.cs) (`MalzemeliTarife_KontraTarifesiYoksa_GenelM3TarifesiKullanilir_AdetTarifesiKullanilmaz`, `MalzemeliTarife_AhsapVeKontraM3KendiTarifesiyle_KatlanirBoyutsuz_KismiPoOranli`).

**Özel sandığın finansa aktarılması.** Özel (bağımsız) sandığın üretim formu yoktur; "Finansa aktar" (`AmbalajOzelSandikFinansCommand`) sandığı üretime alınmış (Planlandı ise Üretimde) sayar, isteğe bağlı finans tarihini yazar ve Finans iş kaydını açar. "Finanstan geri al" yalnız iş kaydı PO'ya bağlı değilse ve sandık proje tamamlamasıyla tamamlanmamışsa yapılır. Özel sandık için form oluşturma 409'dur; ölçüsü eksik veya "yapılmaz" işaretli özel sandık aktarılamaz. Ambalaj proje tamamlamasında özel sandığın tek engeli "finansa aktarılmamış"tır (`FinansaAktarilmadi`). 10 Ekim kaydındaki "üretim formu oluşunca üretime alınır ve finansa aktarılır" davranışı bununla değişti.

- [AmbalajSandikTipiVeTamamlamaTests](../../3K.Application.Tests/AmbalajSandikTipiVeTamamlamaTests.cs): `Ozel_sandik_kaydedilince_uretime_alinmaz_finans_kaydi_finansa_aktarilinca_acilir`, `Ozel_sandik_finansa_aktarilir_ve_po_yoksa_finanstan_geri_alinir`, `Ozel_sandik_olculeri_eksikse_veya_yapilmayacaksa_finansa_aktarilamaz_form_olusturulamaz`, `A2_Finansa_aktarilmamis_ozel_sandik_tamamlamayi_ozel_sandik_olarak_engeller`.

**Özel sandığın dış ölçüsü (2026-10-24).** İlave/Saha/Yedek özel sandığın boy/en/yüksekliği sandık modülündeki gibi dış ölçüdür (`AmbalajUretimPolitikasi.DisOlcuKullanirMi`): ahşap/kafes sandıkta m³ normal ambalajdaki gibi iç ölçüye düşülerek (boy/en −92, yükseklik −255 mm) ve ayak kuralı dış boydan hesaplanır; aynı ölçülü sandık özel sandıkta, normal ambalajda ve Finans aktarımında aynı m³'ü verir. Kaynak sandıktan dönüştürülen İlave kaynağın m³'ünü korur. İç ölçüye düşülemeyen ölçü kayıtta 400 ile reddedilir. İç sandık ve plandaki manuel kalem girilen ölçüyü iç ölçü olarak kullanır. Ön yüz özel sandık formunda İç sandık dışındaki türlerde ölçüleri "Dış" diye etiketler.

- [AmbalajSandikTipiVeTamamlamaTests](../../3K.Application.Tests/AmbalajSandikTipiVeTamamlamaTests.cs): `Ozel_sandik_dis_olcuyle_normal_ambalajla_ayni_m3u_bulur_ve_finansa_ayni_aktarilir`, `Kaynaktan_ilaveye_donusen_sandik_ayni_dis_olcuyle_ayni_m3u_korur`, `Ozel_sandikta_ic_olcuye_dusulemeyen_dis_olcu_reddedilir`, `Dis_olcu_kurali_kaynak_ve_ilave_saha_yedek_ozel_sandikta_uygulanir`.

**Münferit iş modeli.** Münferit iş = tarife + proje (sistem projesi ya da serbest proje adı; biri zorunlu) + tarih + adet/m³. İş adı tarifenin adıdır; serbest proje adı proje no ve müşteri olarak yazılır; tarife seçenekleri iş tarihindeki fiyatı taşır; ana panel dönemin münferit işlerini (en yeni 20) ve sayısını gösterir. 8 Ekim iskeletindeki "fiyat değiştirme izni istenmez / liste maskesi" ifadeleri sekme yetkisine geçişle anlamını yitirdi.

- [FinansMunferitIsTests](../../3K.Application.Tests/FinansMunferitIsTests.cs): DTO (tarih gün başı, proje adı kırpma, iş adı/fiyat/müşteri taşınmaz), doğrulayıcı (tarife ve tek proje kaynağı), izin (yalnız Münferit İşler sekmesi) ve PostgreSQL'de tarifeden fiyatlama, sistem/serbest proje, m³ zorunluluğu, liste ve panel.

**Ön yüz.** Panel özel tarih aralığı doğrulaması ve vade uyarısında kalem kalem kalan gün gösterimi yalnız ön yüzdedir (`3k_onyuz`: finans-panel, finans-aylik spec'leri); ön yüz derlemesi (development) temiz; finans + ambalaj + rol yönetimi + çekirdek servis spec'leri 291/291 ('npx ng test --include' ile). Panel + vade uyarısı, münferit iş formu (1440 ve 390 px) ve özel sandığın "Finansa aktar" penceresi sahte veriyle görsel olarak kontrol edildi; canlı veriyle ayrıca denenmeli.

Koşum (Release, PostgreSQL vakaları izole 127.0.0.1:55439 sunucusunda): `FullyQualifiedName~Finans|FullyQualifiedName~Yetki|FullyQualifiedName~Ambalaj|FullyQualifiedName~Authorization` → 726/726 başarılı, 0 atlanan (`KontraHacim_…`, iki `MalzemeliTarife_…`, `FinansMunferitIsTests.Postgres_…` ve `FinansSekmeYetkileriTests.Postgres_GocBetigi_…` dahil). Şema betikleri izole sunucuda elle prova edildi: `20261013_01` asgari şemada iki kez hatasız (kısıt/FK/indeks adları EF ile aynı); `20261013_02` örnek rol ağacında onaysız (liste yazıp hiçbir şey değiştirmeden durur), onaylı (sekme seviyeleri yükseltilir, alt kodlar silinir) ve ikinci kez (iş yapmaz) çalıştırıldı — bu provalar otomatik test değildir, canlı ağacın kopyasında tekrarlanmalıdır. Kural–test eşlemesi denetimi: 508 kural doğrulandı. Tam paketler koşulmadı.

## Kontrplak hacmi, özel sandık–finans ilişkisi ve panel dökümü — 10 Ekim 2026

- Ahşap/kontra m³ yalnız Ambalaj'ın m³ hesaplamadığı kontrplak/katlanır sandıkta (veya daha önce elle girilmiş hacimde) girilir (`FinansHacimKurallari.ElleGirilebilir`); ekranda işin Fiyatlandırma sekmesindedir ve tarife × bileşik m³ önizlenir. Ahşap sandıkta elle hacim 409'dur.
- Özel (bağımsız) sandık kaydedilince üretime alınmaz ve finansa aktif iş kaydı açmaz; normal plan sandıkları gibi üretim formu oluşunca üretime alınır ve finansa aktarılır. Eski davranışla üretime alınmış kayıtlar düzenlenince durumu korunur (veri taşınmadı). *(13 Ekim 2026'da değişti: özel sandığın üretim formu yoktur, "Finansa aktar" ile aktarılır; bkz. 13 Ekim bölümü.)*
- Finans panelinin detay dökümü Normal → Saha → Yedek → Bağımsız gruplarında, grup içinde iş bedeline göre sıralıdır; projesiz işler müşteriye göre ayrı satırdır.

Testler: [FinansPoFaturaKurallariTests](../../3K.Application.Tests/FinansPoFaturaKurallariTests.cs) (`Elle_hacim_yalniz_kontrplak_katlanir_sandikta_girilir`), [FinansPostgresTests](../../3K.Application.Tests/FinansPostgresTests.cs) (`KontraHacim_…` ahşap reddi, `PanelDokumu_ProjeTipineGoreGruplanir_…`), [AmbalajSandikTipiVeTamamlamaTests](../../3K.Application.Tests/AmbalajSandikTipiVeTamamlamaTests.cs) (`Ozel_sandik_kaydedilince_uretime_alinmaz_finans_kaydi_form_olusunca_acilir`; 13 Ekim'de `…_finansa_aktarilinca_acilir` olarak yeniden adlandırıldı); ön yüz `finans-kayit-detay`, `finans-panel-detay`, `uretim-yasam-dongusu`, `finans-urun-kaydet` spec'leri.

Koşum: `FullyQualifiedName~Finans|FullyQualifiedName~Ambalaj` 554/554 (PostgreSQL vakaları izole sunucuda); ön yüz finans + ambalaj spec'leri 207/207. Tam paketler koşulmadı.

## Çok projeli toplu sevk (tır seferi) ve Sevkiyatlar — 8 Ekim 2026

Birden çok projenin sevke hazır sandıkları tek tır seferinde, hepsi ya da hiçbiri sevk edilir (`POST api/sevkiyat/toplu-sevk`, C-08); sefer kaydı (`SevkSeferleri`, yıl + sıra numarası, normalize plaka, şoför, dorse) proje başına sevkiyatları gruplar ve tekil sevk de tek projelik sefer açar (C-09). Sevkiyatlar sayfası (`sevkiyatlar` R) sefer listesi, sefer detayı ve plaka/proje/ay istatistiklerini sefer ve sevkiyat kayıtlarından okur; sefere bağlı olmayan eski sevkiyatlar plaka + sevk günüyle "eski kayıt" olarak gruplanır (C-10). Şema `20261008_04_Sevk_Seferleri.sql`, izin `20261008_05_Sevkiyatlar_Yetkisi.sql`.

- [TopluSevkEtTests](../../3K.Application.Tests/TopluSevkEtTests.cs): plaka normalizasyonu ve Türk plakası biçimi, sefer numarası sırası, doğrulayıcı sınırları (25 proje, 250 sandık, tekrar, tarih aralığı), istek özetinin sıradan bağımsızlığı, farklı tipte projelerin gerçek tipinin sevk izninin birlikte aranması; gerçek PostgreSQL'de normal + yedek proje tek seferde (proje başına sevkiyat ve proje içi numara, sandık/proje durumu, ilk sevk tarihi, hareket açıklaması), aynı anahtar tekrarı, aynı anahtarla farklı istek 409, kapanmamış sandıklı projede hiçbir kaydın yazılmaması ve proje/sandık bazında engel, tekil sevkin tek projelik seferi, istatistik ve eski kayıt sanal seferi, eşzamanlı iki toplu sevkten yalnız birinin uygulanması, aynı normal kaynak satırı paylaşan iki saha projesinin gerçek saha servisi ve üretim interceptor'larıyla tek seferde sevki (engelde hiçbir şey yazılmaz, tekil sevk yalnız kendi projesini sevk eder), şema ve izin betiklerinin EF modeliyle aynı nesneleri kurup tekrar çalışması.
- [NormalProjeSevkKomutlariTests](../../3K.Application.Tests/NormalProjeSevkKomutlariTests.cs): tekil sevkin ortak sevk motoruna taşındıktan sonra da tek kayıtla ve aynı durum kurallarıyla çalışması.
- Ön yüz: `toplu-sevk.component.spec.ts` (yetkisiz tipin ve sevke hazır sandığı olmayan projenin dışarıda kalması, varsayılan seçim, sıralı istek, aynı istekte aynı işlem anahtarı, 409 engellerinin proje/sandık bazında gösterimi, ağ hatası), `sevkiyatlar.component.spec.ts` (liste, eski kayıt rozeti, detayın bir kez yüklenmesi, süzgeçler, plaka/proje istatistiğinden seferlere geçiş, aylık ölçek), `sevkiyat.service.spec.ts` (parametreler, eski kayıt detayı, issues, plaka biçimi, sevke hazır sandık kuralı), `proje-listesi.component.spec.ts` (Aktif Projeler'de satırın gerçek tipinin sevk izni, toplu sevk düğmesinin açıldığı listeler, modül listesinde kilit açmanın sunucu kuralına uyması).

Koşum: `TopluSevkEtTests|NormalProjeSevkKomutlariTests` 17/17 (PostgreSQL vakaları izole 127.0.0.1:55439 sunucusunda); sevk/sandık regresyon alt kümesi 181/181; geniş filtrede 488/490 (2 başarısızlık önceden var olan `CekiOrijinalMiktarTests.RevizyonU_AyniPlanliTahsisDuzeltmesi_*`; temiz HEAD'de de başarısız). Ön yüz projeler + sevkiyat servisi + base-api sorunları 24/24, çekirdek ve paylaşılan spec'ler 111/111. Tam paketler koşulmadı. Ekran kabulü sahte veriyle yerel önizlemede yapıldı; canlı veriyle ayrıca yapılmalı.

## Finans Münferit İşler sekmesi (iskelet) — 8 Ekim 2026

Tek seferlik işler için ayrı sekme: `GET/POST api/finans/munferit-isler`, `GET api/finans/urun-secenekleri`. Fiyat elle yazılmaz; kayıt seçilen iş/ürün tanımının iş tarihindeki tarifesiyle fiyatlanır (para birimi ve KDV de tarifeden), rapor grubu "Münferit İş". Ürün m³ ile fiyatlanıyorsa ahşap/kontra m³ zorunludur ve bileşik toplam fiyatlandırma miktarıdır. Tarifesi olmayan tarih 409 ("… tarihinde geçerli tarife yok"). Liste elle açılmış, düzenli işe bağlı olmayan özel işleri gösterir (eski "Tek Seferlik İş" kayıtları dahil; eski düğme yerinde). Veri modeline kolon eklenmedi; sekmenin alan listesi kullanıcıdan beklenmektedir.

- [FinansMunferitIsTests](../../3K.Application.Tests/FinansMunferitIsTests.cs): DTO eşlemesi (fiyat alanı yok), doğrulayıcı, izinler (fiyat değiştirme istenmez) ve gerçek PostgreSQL'de adet/m³ tarifeli ürünle fiyatlama, hacimsiz m³ isteğinin ve tarifesiz tarihin reddi, pasif ürün, listenin üretim/düzenli iş kayıtlarını dışlaması, arama ve ürün seçenekleri.
- [FinansAlanHttpSecurityTests](../../3K.Application.Tests/FinansAlanHttpSecurityTests.cs): fiyat değiştirme izni olmadan oluşturma, sekme R iken oluşturma 403, liste maskesi ve sekme izni.
- Ön yüz: `finans-munferit-isler.component.spec.ts` (liste ve iptaller, yazma kapısı, m³ zorunluluğu, isteğin fiyat taşımaması, tarife önizlemesi yalnız izinle, gerekçeli iptal, PO aç izni), `finans-izinleri.spec.ts` (sekme kuralı).

Koşum: `FinansMunferitIsTests|FinansModuluTests|FinansIstekDtoTests|FinansOzelIsContractTests|FinansAlanHttpSecurityTests|FinansSekmeYetkileriTests` 132/132 (PostgreSQL izole sunucuda); ön yüz finans spec'leri 164/164. Tam paketler koşulmadı.

## Finans sekme yetki düğümleri — 8 Ekim 2026

Rol ağacına Finans sekme düğümleri eklendi ve yalnız o sekmeye ait işlem kodları sekme düğümünün altına taşındı (`20261008_03_Finans_Sekme_Yetkileri.sql`). Sekme düğümü alt kodların tavanıdır; sekmeye özel uçlar sekme kodunu mevcut kodlarla birlikte ister (Y-03).

- [FinansSekmeYetkileriTests](../../3K.Application.Tests/FinansSekmeYetkileriTests.cs): test fikstürünün betikle aynı olması, sekme N/R/W → alt W kodunun etkin seviyesi (modül R tavanı dahil), sekmeye özel 10 uç ve sekmeye bağlanmayan 5 paylaşılan uç; gerçek PostgreSQL'de canlı ağacın (kök 47 + 33 yaprak, 4 rol + tavanlı salt okuma rolü) kopyasında göç: düğümler, taşınan kodlar, **bütün mevcut kodların etkin seviyesinin değişmemesi**, rol × sekme seviyeleri, kimlik dizisi, tekrar çalıştırmanın sonradan değiştirilmiş sekme iznini ezmemesi ve eksik kodda hiçbir şey yapmadan durması.
- [FinansAlanHttpSecurityTests](../../3K.Application.Tests/FinansAlanHttpSecurityTests.cs): izin deposu hiyerarşiyi fikstürden okur; sekme kapalıyken PO yazması ve panel/aylık okuması 403, paylaşılan uçlar açık.
- Ön yüz: `finans-izinleri.spec.ts` (sekme kuralları, tanım alt görünümleri, açılış sekmesi), `finans-yetki-render.component.spec.ts` (sekme düğmesi/gövdesi, açılış sekmesi, oturumda izin kaldırılınca sekme değişimi, kategori sekmesinin R ile açılması), `menu-tree.component.spec.ts` (alt düzeyler kapalı, tümünü aç/kapat, sekme açıklaması ve N onayı).

Koşum: `FullyQualifiedName~Finans|FullyQualifiedName~Yetki|FullyQualifiedName~ServerAuthorizationRegressionTests` 409/409 (PostgreSQL vakaları izole 127.0.0.1:55439 sunucusunda); ön yüz rol yönetimi + çekirdek servisler + finans spec'leri 235/235. Tam paketler koşulmadı. Uygulama DB'sine yalnız canlı ağacı okumak için SELECT çalıştırıldı.

## Finans İş Takibi: proje → sandık kırılımı ve ahşap/kontra m³ — 8 Ekim 2026

**Sandık kırılımı.** `GET api/finans/proje-sandik-kirilimi` (`finans-kayit-goruntule` R) projenin (isteğe bağlı finans dönemindeki) bütün üretim kayıtlarını sayfalamasız döndürür; 250 kayıt sınırı yoktur, 20.000 kayıt üstü 409'dur. Tutarlar SQL'de hesaplanır; sandık başına gruplama (`FinansSandikKirilimiKurali`) sandık no + kaynak üretim kaydı + para birimiyle yapılır, çünkü ahşap/kontra payı fiyat satırlarına (JSON) bağlı kayıt kuralıdır. Aynı sandığın NET ve SARF kayıtları tek satırdadır: tutar ikisinin toplamı, adet ve m³ yalnız sandığın kendisi, durum aktif kayıtların en düşük iş durumudur (hepsi iptalse iptal). Genel aramaya `sandikNo` (ILIKE) filtresi eklendi; aylık sonuç sayfadaki projelere bağlı özel/düzenli işleri `projeOzelIsleri` ile ayrıca döndürür (sayfalama birimi değişmedi).

- [FinansSandikKirilimiTests](../../3K.Application.Tests/FinansSandikKirilimiTests.cs): 11 vaka — iki sandık × NET + SARF, iptal ve para birimi ayrımı, doğal sıra, doğrulayıcı, okuma izni, yanıtın alan maskesi (tutar/m³ null, sarf kaydının m³'ü sarf iznine bağlı) ve gerçek PostgreSQL'de 264 kayıtlık kırılım + sandık no filtresi + projeye bağlı özel iş.
- [FinansModuluTests](../../3K.Application.Tests/FinansModuluTests.cs): istek → yetki kodu eşlemesine kırılım sorgusu eklendi.
- Ön yüz: `ortak/finans-kirilim.spec.ts` (4), `finans-aylik-po-akisi.spec.ts` (kırılımdan kategori dökümü, proje "Sandıklar" paneli ilk 8 + tümü, NET/SARF rozetleri, yenilemede önbellek, "Özel / Düzenli İşler" alt grubu), `finans-kayit-arama.component.spec.ts` (proje seçici + sandık no sunucu filtresi, proje → sandık başlıkları ve alt toplamlar).

**Ahşap/kontra m³.** Kontra/katlanır sandıkta ahşap ve kontra m³ `POST is-kayitlari/{id}/hacim` ile elle girilir; `ToplamM3` bileşik toplamdır ve tarife fiyatlaması bununla çalışır; üretim aktarımı elle girilen hacmi ezmez, PO'ya bağlı kayıtta 409. Şema betiği `20261008_02_Finans_Ahsap_Kontra_Hacim.sql`. Kanıt: `FinansPoFaturaKurallariTests`, `FinansIstekDtoTests`, `FinansAlanProjeksiyonuTests`/`FinansAlanHttpSecurityTests` (ahsapM3/kontraM3 maskesi; 13 Ekim'de alan maskesi ve `FinansAlanProjeksiyonuTests` kaldırıldı) ve `FinansPostgresTests.KontraHacim_ElleGirilir_BilesikToplamTarifeyleFiyatlanir_AktarimEzmez`.

Koşum: kırılım için değişen alan alt kümesi (`FinansSandikKirilimiTests|FinansModuluTests|FinansAlanProjeksiyonuTests|FinansHataEslemeTests|FinansPostgresTests|FinansPoAkisPostgresTests|FinansVadeParaBirimiPostgresTests|FinansAlanHttpSecurityTests|FinansPoFaturaKurallariTests|FinansIstekDtoTests`) 187/187, atlanan yok; hacim için finans alt kümesi 190 başarılı/1 atlanan ve PostgreSQL alt kümesi 48/48. PostgreSQL vakaları izole sunucuda (127.0.0.1:55439) çalıştı. Ön yüz finans spec'leri 155/155. Tam backend ve frontend paketleri bu değişikliklerde koşulmadı. Uygulama DB'sine dokunulmadı.

## Hatalı tarife düzeltme — 4 Ekim 2026

Kayıtlar oluştuktan sonra yanlış girilmiş bir tarife sürümünün fiyatı/KDV'si gerekçeyle düzeltilir; yalnız o sürümün tarife değeriyle fiyatlanmış açık kalemler yeni değere çekilir (`FinansTarifeDuzeltmeKurali`). PO'ya bağlı, elle fiyatlı, manuel netli, bileşenli ve iptal kalemler korunur; diğer sürümler ve tarih kuralı değişmez.

- [FinansTarifeDuzeltmeTests](../../3K.Application.Tests/FinansTarifeDuzeltmeTests.cs): 14 vaka — koruma nedenleri, iptal PO'nun engellememesi, yeni net yuvarlaması, doğrulama, yazma yetkisi ve önizlemede fiyat/KDV/tutarın yetkisiz kullanıcıya null dönmesi.
- [FinansTarifeDuzeltmePostgresTests](../../3K.Application.Tests/FinansTarifeDuzeltmePostgresTests.cs): canlıdaki 565→500 vakası; ekranda aynı gün düzeltmenin reddi, önizlemenin veriyi değiştirmemesi, yalnız tarife fiyatlı açık kalemin 565'e çekilmesi, sürüm geçmişi ve denetim kaydı.
- Ön yüz `finans-tarife-duzeltme.component.spec.ts`: 4 vaka — yetkisiz açılmama, değer değişince önizlemenin düşmesi, gerekçe zorunluluğu, onaya düşen işlemin başarı sayılmaması.

Koşum: değişen alan alt kümesi (`FinansTarifeDuzeltme|FinansUrunTarifeTests|Yeniden_fiyatlama_yalniz_posuz`, 22/22, atlanan yok) geçici dizinde açılan izole PostgreSQL 18 kümesinde (127.0.0.1:55439) çalıştı; yetki/güvenlik regresyon alt kümesi 133/133, finans aylık ön yüz testleri 21/21. Tam backend ve frontend paketleri bu değişiklikte koşulmadı. Uygulama DB'sine dokunulmadı.

## Yetki hiyerarşisi doğrulaması — 25 Eylül 2026

Üst menü N/R sınırlandırması, rol/kullanıcı menüsü, rol kaydı, kişisel izin ve rol atama regresyonları eklendikten sonra backend paketinin Debug ve Release koşumlarının her birinde **1.289 başarılı, 0 başarısız, 28 atlanan** test vardır (toplam 1.317). Atlanan testler izole PostgreSQL bağlantısı gerektirir; bu koşum gerçek DB entegrasyonu kanıtı değildir. Kural–test eşlemesi denetiminde 501 kural referansı geçerlidir. Frontend üretim derlemesi ve yeni yetki odak testleri (16/16) başarılıdır. Tam frontend koşumunda 211 testin 209'u geçti; görev dışı finans panelinin mevcut iki testi başarısız kaldı. Canlı rol kayıtları ve tarayıcı kabulü ayrıca doğrulanmalıdır.

Sonraki üç durumlu kutu görsel senkronizasyonu düzeltmesinde rol ekranının gerçek DOM tıklaması, kaydı ve yeniden yüklemesini içeren 7 odak frontend testi ve üretim derlemesi başarılıdır. Bu dar düzeltmeden sonra tam frontend paketi yeniden çalıştırılmadı; yukarıdaki 211 testlik sonuç önceki koşuma aittir.

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

Yeni güvenlik kanıtları `GranularYetkiTests`/`GranularYetkiPostgresTests` (19), `FinansAlanHttpSecurityTests`/`FinansAlanProjeksiyonuTests` (31) ve `AmbalajAlanMaskelemeTests` (8) vakasında yer alır (tarihsel kayıt: `GranularYetki*` ve `FinansAlanProjeksiyonuTests` sınıfları artık yok; Finans alan maskesi 13.10.2026'da kaldırıldı). Gerçek JWT/Kestrel/MediatR/MVC zinciri; sahte `X-Menu-Kod`, kök modül reddi + açık alt izin, aynı token ile izin kaldırılması, parasal/ölçü/m³/sarf alanlarında null, iç içe audit/string alanları ve yetkisiz binary indirme 403 davranışını doğrular. Finans veri servisi/izin deposu HTTP testinde sentetiktir; kalıcı override/rol/audit davranışı ayrıca gerçek PostgreSQL'de sınanır. Canlı kullanıcıyla tarayıcı kabulünün yerine geçtiği iddia edilmez.

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
| Doğrudan birim testli | 337 |
| Kısmen doğrulanan | 77 |
| Gerçek entegrasyon doğrulaması gereken | 18 |
| Arayüz doğrulaması gereken | 32 |
| Açık fark / karar / test ihtiyacı | 30 |
| Çalıştırılabilir uygulama kuralı olmayan veya kodu kaldırılmış | 8 |
| Toplam | 502 |

Sayılar `coverage/*_TEST_ESLESMESI.md` tablolarının kapsam sütunundan çıkarılır (05.10.2026). Kaldırılan eski uçlara (c41a0a0) ait K-040, U-221, U-222, U-224, U-227 ve U-233 "Kapsam dışı" sayılır; testleri uçlarla birlikte silindi.

Dolayısıyla **502 maddenin tamamı eksiksiz test edildi** denmiyor. Her maddenin tam sınırı aşağıdaki eşleme tablolarında görülebilir; bileşik bir kuralın tek alt senaryosunu test etmek, tamamını kapsamak sayılmadı.

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
