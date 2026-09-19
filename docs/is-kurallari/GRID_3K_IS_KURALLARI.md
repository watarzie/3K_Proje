# Grid ve 3K İş Kuralları Kataloğu

**Sürüm:** 1.0 — 12 Eylül 2026  
**Kapsam:** Grid ve 3K; bu modülleri doğrudan etkileyen çeki, tahsis, stok/proje/tedarikçi, saha/yedek, sandık, sevkiyat, onay ve yetki bağlantıları.

## İçindekiler

- [Okuma kılavuzu ve kapsam sınırı](#kapsam)
- [1. Temel kavramlar ve değişmez ayrımlar](#kavramlar)
- [2. Miktar ve genel durum hesapları](#hesaplar)
- [3. Yetki ve dinamik onay](#yetki-onay)
- [4. Ortak kilit, kalite ve süreç kuralları](#kilit-kalite)
- [5. Normal proje–saha–yedek ilişkisi ve tamamlanma](#saha)
- [6. Sandık kapatma, nihai sevkiyat, silme ve raporlar](#sandik)
- [7. Grid kuralları](#grid)
- [8. 3K karşılama kuralları](#uck)
- [9. Kaynak, tahsis, taşıma ve çeki güncelleme kuralları](#kaynak-tahsis)
- [10. Kullanıcı senaryoları: sonuç nasıl okunmalı?](#ornekler)
- [11. Farklı işlem yolları ve dikkat edilmesi gereken sınırlar](#farklar)
- [12. Kaynak ve doğrulama envanteri](#kaynaklar)

<a id="kapsam"></a>

## Okuma kılavuzu ve kapsam sınırı

Bu doküman, mevcut backend ve önyüzün **uyguladığı davranışların** yazılı dökümüdür; yeni bir iş kuralı önerisi değildir. Ana inceleme backend çalışma ağacı ve `C:/Users/Watarzie/Desktop/3k_onyuz` önyüzü üzerinden yapılmıştır. Henüz commit edilmemiş son Grid/3K düzeltmeleri de kapsamdadır. Canlıda aynı sürümün ve aynı veritabanı ayarlarının bulunduğu varsayılmamıştır.

- **Ortak kurallar:** miktarların anlamı, durum hesabı, yetki/onay, kilit ve saha ilişkisi.
- **G-…:** Grid kuralları.
- **U-…:** 3K kuralları.
- **K-…:** kaynak, tahsis ve revizyon kuralları.
- **“Mevcut uygulama farkı”** olarak belirtilen yerler, farklı işlem yollarının aynı kontrolü uygulamadığını gösterir. Bunlar tek bir ideal kurala dönüştürülerek gizlenmemiştir.
- Kod referansları belge hazırlanırken incelenen sürüme aittir; ileride satır numaraları değişebilir.
- Yerel veritabanı yapılandırmaları ayrıca tarihli bir anlık görüntü olarak verilmiştir. Kalıcı ürün politikası değildir.
- Bu çalışma kod/veri değiştirmeden hazırlanmış bir kural envanteridir; her canlı senaryonun çalıştırıldığı bir kabul testi veya hatasızlık garantisi değildir.
- Ambalaj üretim hesabı, finans tarifeleri ve sistemin Grid/3K dışındaki bağımsız modülleri bu dokümanın kapsamı dışındadır.

<a id="kavramlar"></a>

## 1. Temel kavramlar ve değişmez ayrımlar

| Kavram | Sistemdeki karşılığı | Anlamı |
|---|---|---|
| Güncel çeki ihtiyacı | `CekiSatiri.IstenenAdet` | Operasyonun güncel hedef miktarıdır. |
| İlk miktar referansı | `OrijinalIstenenAdet` | İlk gerçek miktar değişikliğinde önceki miktar saklanır; güncel hesabın girdisi değildir. |
| Sandığa tahsis | `SandikIcerik.TahsisMiktari` | Güncel ihtiyacın ilgili sandığa ayrılan kısmıdır. Fiziksel teslim değildir. |
| Sandığa konulan | `SandikIcerik.KonulanAdet` | İlgili sandıkta fiziksel karşılanan/konulan miktardır. |
| Grid gelen | `GridGelenAdet` | Grid tarafında bulunan/gelen miktardır; 3K teslimi değildir. |
| Aktif Grid sevki | `GridSevkMiktari` | Aktif sevk kaydının miktarıdır; geçmiş sevklerin toplamı değildir. |
| Aktif sevkten karşılanan | `AktifGridSevkKarsilananMiktari` | Aktif sevkin 3K tarafından işlenen miktarını geçmiş teslimlerden ayırır. Ana satırda ve sandık içeriğinde tutulur. |
| Kümülatif Grid teslimi | `GelenMiktar` | 3K tarafına fiziksel ulaşan Grid miktarıdır; kaynak karşılamaları ayrıca tutulur. |
| Diğer karşılamalar | `StokKarsilanan / ProjeKarsilanan / TedarikciKarsilanan` | Kaynak bazlı miktarlardır; birbiri yerine kullanılmaz. |
| Başka projeye verilen | `ProjeGonderilen` | Kaynak satırın net fiziksel karşılamasını azaltır; kalanını artırabilir. |
| Trafo sevk | `TrafoSevkAdet` | İhtiyacı normal 3K tesliminden ayrı kapatan miktardır. |
| Yeniden sevk borcu | `YenidenSevkGerekliAdet` | Sonuçlandırılmış eksik/gelmedi/iade gibi akışlardan kalan yeniden sevk ihtiyacıdır. |
| Grid durumu | `GridDurumuId` | Grid'deki tedarik/iş durumu. |
| Grid sevk durumu | `GridSevkDurumuId` | Grid → 3K sevk süreci. |
| 3K durumu | `UcKDurumuId` | 3K karşılama/teslim sonucu. |
| Genel ürün durumu | `DurumId` | Durumlar ve kalan hesabından türetilen operasyon sonucu. |
| Sandık/proje sevki | `Sandik.DurumId / Proje.DurumId` | Fiziksel son sevkiyat aşaması; Grid → 3K sevkiyle aynı değildir. |

**O-01.** Kimliklendirme yalnız barkodla yapılmaz. Aynı barkod aynı projede birden fazla çeki satırında bulunabilir. İşlem anahtarı ilgili `CekiSatiriId`, gerekiyorsa `SandikIcerikId`, proje ve sandık bağlamıdır.

**O-02.** Miktarlar ondalıklıdır. Temel miktar alanları `decimal`, veritabanı hassasiyeti ilgili alanlarda `numeric(18,4)` olarak tanımlanmıştır. Ana güncel miktar modeli ondalıklıdır; ekranda tam sayı görünmesi verinin tamsayı olduğu anlamına gelmez. Eski stok ucundaki fiili integer dönüşümü K-057'de ayrıca belirtilmiştir.

**O-03.** “Grid'e tam geldi”, “sevk adeti tam geldi”, “ihtiyaç tamamlandı”, “sandık kapandı” ve “proje sevk edildi” farklı sonuçlardır. Birinin gerçekleşmesi diğerlerini kendiliğinden garanti etmez.

**O-04.** Örnek: ihtiyaç 35, Grid gelen 35, 3K'ya sevk 32, 3K teslim 32 ise **sevk tam teslim alınmış olabilir; ürünün 3 adet ihtiyacı sürer**. “Tam geldi” etiketi tek başına 35 adedin tamamlandığını göstermez.

**O-05.** İptal/Grid kapandı ile kalan sıfırlanabilir; bu sıfırlama fiziksel teslim, stok girişi veya sevkiyat kaydı üretildiği anlamına gelmez.

**O-06.** Orijinal miktar ilk değişiklik referansıdır; tam revizyon geçmişi değildir. Örneğin 1→3→4 değişikliğinde güncel 4, referans 1 kalır. Ayrıntılı değişiklikler hareket/revizyon kayıtlarından izlenir.

Kaynak: [CekiSatiri.cs](C:/Users/Watarzie/source/repos/3K_Proje/3K.Core/Entities/CekiSatiri.cs), [SandikIcerik.cs](C:/Users/Watarzie/source/repos/3K_Proje/3K.Core/Entities/SandikIcerik.cs), [AppDbContext.cs:310](C:/Users/Watarzie/source/repos/3K_Proje/3K.Infrastructure/Data/AppDbContext.cs:310).

<a id="hesaplar"></a>

## 2. Miktar ve genel durum hesapları

### 2.1 Hesap sözlüğü

Aşağıdaki formüllerde **I** güncel istenen, **G** 3K Grid teslimi, **S** stok, **P** projeden, **T** tedarikçi, **V** başka projeye verilen, **TR** trafo sevk miktarıdır.

| Hesap | Mevcut formül / davranış |
|---|---|
| Net kümülatif karşılanan | `G + S + P + T - V` |
| Aritmetik ihtiyaç | `I - G - S - P - T + V - TR` |
| Operasyonel kalan | Grid İptal veya Grid Kapandı ise 0; aksi halde aritmetik ihtiyacın en az 0 olan değeri. Hatalı miktar/uyumsuzluk varken sonuç ≤0 ise açık iş işareti olarak 1. |
| Grid eksik | İptal/Grid Kapandı ise 0; aksi halde `I - GridGelenAdet - TR`. Entity özelliğinde ayrıca sıfıra kırpma yoktur. |
| Entity `EksikMiktar` | Grid Kapandı ise 0; aksi halde `max(I - net kümülatif, 0)`. Bu ham özellik trafoyu düşmez ve İptal için ayrı sıfır kuralı taşımaz. |
| Saha etkili kalan | Normal kaynak satırda `max(operasyonel kalan - ilgili saha haritasındaki karşılık, 0)`; saha kopyasında kendi kalanıdır. Haritanın “aktarım”, “iş tamamlama” veya “sevk edilmiş gerçekleşen” olması çağrılan ekrana göre önemlidir. |

**H-01.** Grid sevk miktarı kalan formülünden doğrudan düşülmez; malın 3K'ya gerçekten ulaşması/karşılanması gerekir.

**H-02.** `KarsilananMiktar` alternatif kaynakların toplam yardımcı alanıdır ve bazı güncel üst sınır doğrulamalarında da kullanılır. Kaynak bazlı alanlarla birlikte yeniden toplanırsa çift sayım oluşur. Güncel kalan hesabı ayrıntılı kaynak alanlarını kullanır.

**H-03.** Hatalı üründe kalan 1 gösterimi her zaman “tam 1 fiziksel adet eksik” anlamına gelmez; miktar dolsa da işin tamamlanmadığını koruyan işaret olabilir. Kapasite ve fiziksel ihtiyaç kontrolleri bu gösterimle karıştırılmamalıdır.

**H-04.** Sandık satırının tahsisi ile çeki ana toplamı farklıysa, dağıtılmış ürünlerde bu fark geçerli olabilir. Miktar revizyonunda tek tahsisli satırın güncellenmesi ve çok tahsisli satırın korunması K bölümünde açıklanmıştır.

**H-05.** Ekrandaki “Eksik”, “Kalan”, rapordaki eksik ve entity `EksikMiktar` aynı isim altında tek formül değildir. Gerçek ekran/rapor eşlemesi aşağıdaki modül bölümlerindedir.

Kaynak: [CekiSatiri.cs:164](C:/Users/Watarzie/source/repos/3K_Proje/3K.Core/Entities/CekiSatiri.cs:164), [CekiSatiriKalanHelper.cs:8](C:/Users/Watarzie/source/repos/3K_Proje/3K.Core/Helpers/CekiSatiriKalanHelper.cs:8), [GridUcKSevkPartisiKurali.cs](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs).

### 2.2 Genel durum belirleme sırası

`DurumHesaplaService.HesaplaGenelDurum` şu önceliklerle çalışır:

| Öncelik / Grid durumu | 3K durumu | İlk genel sonuç |
|---|---|---|
| En önce | Paketlendi | Tamamlandı |
| En önce | İade Edildi | Geri Gönderildi |
| İptal | Diğer | İptal/Pasif |
| Siparişte | Diğer | Siparişte |
| Gelmedi | Diğer | Gelmedi |
| Trafo Sevk | Tam / Kontrol | Tamamlandı |
| Trafo Sevk | Eksik | Eksik |
| Trafo Sevk | Diğer | Trafo Sevk |
| Tam Geldi | Tam / Kontrol | Tamamlandı |
| Tam Geldi | Eksik | Eksik |
| Tam Geldi | Gelmedi | Kayıp |
| Tam Geldi | Diğer | Grid'de Hazır |
| Eksik Geldi | Tam / Kontrol | Kısmi Tamamlandı |
| Eksik Geldi | Eksik | Eksik |
| Eksik Geldi | Gelmedi | Kayıp |
| Eksik Geldi | Diğer | Grid'de Eksik |
| Yukarıdakilere girmeyen | Tam / Kontrol / Projeden / Stoktan / Tedarikçiden | Tamamlandı |
| Yukarıdakilere girmeyen | Eksik / Geri / Hatalı | Sırasıyla Eksik / Geri Gönderildi / Hatalı |
| Yukarıdakilere girmeyen | Eski Başka Projeye Verildi | Başka Projeye Verildi |
| Diğer | Diğer | Bekliyor |

**H-06.** Bu tablo yalnız ilk aşamadır. Ardından `HesaplaKalanVeDurum` çağrılan yollarda kalan ≤0 ise genel durum Tamamlandı yapılır. Kalan >0 ve ilk sonuç Tamamlandı ise net karşılanan+trafo >0 olduğunda Kısmi Tamamlandı, yoksa Eksik yapılır. Diğer ilk sonuçlar aynen kalabilir.

**H-07.** Dolayısıyla genel durum tek başına Grid veya 3K durumunun kopyası değildir. Her endpoint aynı ikinci aşamayı çağırıyor varsayılmamalıdır.

Kaynak: [DurumHesaplaService.cs:13](C:/Users/Watarzie/source/repos/3K_Proje/3K.Infrastructure/Services/DurumHesaplaService.cs:13), [DurumHesaplaService.cs:113](C:/Users/Watarzie/source/repos/3K_Proje/3K.Infrastructure/Services/DurumHesaplaService.cs:113).

<a id="yetki-onay"></a>

## 3. Yetki ve dinamik onay

### 3.1 Erişim kuralları

**Y-01.** Güvenli isteklerde oturum ve kullanıcı kimliği zorunludur. Menü yetkisi bulunmadan API'ye doğrudan istek atmak yetki kontrolünü kaldırmaz.

**Y-02.** Korunan işlemin menü/işlem izni sunucudaki açık sözleşme veya `RequestMenuPermissionResolver` kataloğundan gelir. `X-Menu-Kod` yetki sağlamaz. Tanımlanmamış korumalı işlem reddedilir. Proje, sandık ve satır bağlamı gerektiren işlemlerde gerçek proje tipi/ilişkisi veritabanından doğrulanır.

**Y-03.** Okuma için R, değiştiren işlem için W gerekir; rapor indirme komutları okuma olarak açık tanımlıdır. W, R gereksinimini de karşılar. Çoklu izinlerde varsayılan `All` (AND); aynı endpoint'i kullanan izinli ekranlar `Any` (OR) grubuyla belirtilir. Ayrı gruplar ve toplu seçimde ayrı gerçek projelerin gereksinimleri birlikte sağlanır. [Sunucu işlem/yetki kataloğu](../guvenlik/SUNUCU_YETKI_ESLEMESI.md).

**Y-04.** Rol, kullanıcı/rol ilişkisi ve `RolYetkileri` kayıtlarıyla doğrulanır. Normal menü yetki kontrolünde genel bir “Admin her menüyü atlar” istisnası yoktur.

**Y-05.** Menü görüntüleme/yazma izni ile **başkasının işlemini onaylama yetkisi** farklıdır. Onay merkezine giriş izni, listedeki her işlemi onaylayabilme anlamına gelmez.

**Y-06.** Mevcut kodda onay yetkisi için özel bir Admin istisnası vardır: rol ID=1 **veya** rol adı Admin olan kullanıcı tüm onay işlemlerine erişebilir ve kendi talebini onaylayabilir. Diğer roller için `OnayIslemYetkileri` kullanılır; kendi taleplerini onaylayamazlar. Bu gerçek uygulama istisnasıdır; “yetkilerin tamamı yalnız DB kural satırıdır” denemez.

Kaynak: [AuthorizationBehavior.cs:25](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Behaviors/AuthorizationBehavior.cs:25), [RolService.cs:71](C:/Users/Watarzie/source/repos/3K_Proje/3K.Infrastructure/Services/RolService.cs:71), [OnayYetkiService.cs:18](C:/Users/Watarzie/source/repos/3K_Proje/3K.Infrastructure/Services/OnayYetkiService.cs:18).

### 3.2 Hangi durumda onaya düşer?

**Y-07.** Proje/sandık sevkiyat kilidini açma komutları her zaman onay ister. Kilit açma modu, hedef ve açıklama talebin parçasıdır.

**Y-08.** Çeki revizyonu uygulama ve sandık lokasyon atama gibi yapılandırılabilir işlemler `OnayOperasyonKurallari` tablosunu okur. Bu tür bir işlem için kayıt yoksa varsayılan **onay gerekli**dir.

**Y-09.** Tekil `UcKDurumGuncelleCommand`, seçilen 3K durumuna ait `IslemOnayKurallari` kaydını kullanır. Bu türde kayıt yoksa varsayılan **onay gerekmiyor**dur. Kuralların bu iki varsayılanı birbirinden farklıdır.

**Y-10.** 3K durumuna bağlı kurallar 24 saatlik uygulama belleği önbelleği kullanır. Yönetim komutu ilgili önbelleği temizler; DB'ye elle müdahalenin tüm çalışan uygulamalarda anında etkili olduğu varsayılmamalıdır.

**Y-11.** Onay gereken komut hemen işlenmez: işlem kodu, komut verisi, talep eden, proje/referans ve bekleyen durum kaydedilir. Kullanıcıya kuyruğa alındığı bildirilir. Talebin açılması fiziksel sevk/teslim değildir.

**Y-12.** Onaylayan kişinin işlem kodu yetkisi kontrol edilir; bekleyen talep bir kez karar alınabilecek şekilde sahiplenilir. Onaylı komut tekrar aynı iş kurallarından geçirilir; arada değişen stok, tahsis, kilit veya miktar nedeniyle çalıştırma başarısız olabilir.

**Y-13.** “Onaylandı fakat çalıştırılamadı” mümkündür: onay kararı ile işin başarıyla uygulanması farklı alanlardır. Başarısızlıkta veriyi elle değiştirmek veya aynı talebi başarı varsayarak sürdürmek doğru değildir; yürütme sonucu kontrol edilir.

**Y-14.** Eski “Başka Projeye Verildi” onay tanımı/verisi bulunması, yeni ekranda bağımsız bir işlem olduğu anlamına gelmez. Güncel projeden karşılama hedef işlem üzerinden yürür.

**Y-15 — Mevcut uygulama farkı.** Tekil 3K komutunun onay arayüzünü taşımayan toplu/alternatif komutlara bu kontrol kendiliğinden yayılmaz. Tekil tedarikçi veya projeden onay ayarından hareketle her API yolunun aynı onaya tabi olduğu söylenemez; U/K bölümlerindeki yol ayrımlarına bakılmalıdır.

**Y-16.** Sandık kapatmada `ForceClose` kullanıcının eksik/hatalı ürün uyarısını kabul etmesidir; ayrı dinamik onay merkezi talebiyle aynı mekanizma değildir.

Kaynak: [ApprovalBehavior.cs:39](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Behaviors/ApprovalBehavior.cs:39), [IslemOnaylaCommandHandler.cs](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/OnayIslemleri/Commands/IslemOnaylaCommandHandler.cs), [UpdateOnayKuraliCommand.cs](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/OnayIslemleri/Commands/UpdateOnayKuraliCommand.cs), [UcKDurumGuncelleCommand.cs](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommand.cs).

### 3.3 Yerel veritabanı ayarları — 12.09.2026 anlık görüntü

**Kaynak:** localhost / 3K_Db, yalnız okunarak incelendi. Canlı ayarlarla eşitliği doğrulanmadı. Bu tabloda bir lookup tanımının bulunması, Hatalı Ürün/Başka Projeye Verildi gibi eski tiplerin güncel 3K işlem ekranında kullanılabildiği anlamına gelmez.

| 3K işlemi | Yerelde onay |
|---|---|
| Sevk Adeti Tam Geldi | Hayır |
| Sevk Adeti Eksik Geldi | Hayır |
| Gelmedi | Hayır |
| Projeden Karşılandı | Evet |
| Stoktan Karşılandı | Hayır |
| Tedarikçiden Geldi | Hayır |
| Başka Projeye Verildi (eski tanım) | Evet |
| Geri Gönderildi | Hayır |
| Hatalı Ürün | Hayır |
| Fazla Geldi | Hayır |
| Çeki Revizyonu Uygula (operasyon kuralı) | Evet |
| Sandık Lokasyon Güncelle (operasyon kuralı) | Hayır |
| Proje/Sandık Kilidi Aç | Komut gereği her zaman |

Yerelde ilgili Grid/3K işlem kodlarına atanmış onay rolleri **Admin** ve **Sevkiyat Sorumlu Mühendis**tir. Sandık lokasyon güncelleme kodunda atanmış rol Admin'dir. “Sistem Yönetici” rolünün onay merkezine W erişimi bulunması, bu tabloda otomatik olarak aynı işlem onaylarını aldığı anlamına gelmez.

| Menü kodu | W rolleri (okuma+yazma) | Yalnız R rolleri |
|---|---|---|
| grid-is-listesi | Admin, PersonelGrid, Sevkiyat Sorumlu Mühendis, Sistem Yönetici | Görüntüleyici Yükleyici |
| grid-modulu | Admin, PersonelGrid, Sevkiyat Sorumlu Mühendis, Görüntüleyici Yükleyici, Sistem Yönetici | 3K Ustabaşı, Görüntüleyici, Açık Kalem Yetkilisi, Salt Okunur Kullanıcı |
| 3k-is-listesi | Admin, Personel3K, Sistem Yönetici | Görüntüleyici Yükleyici, Salt Okunur Kullanıcı |
| 3k-modulu | Admin, Personel3K, Sevkiyat Sorumlu Mühendis, Sistem Yönetici | 3K Ustabaşı, Görüntüleyici, Görüntüleyici Yükleyici, Salt Okunur Kullanıcı |
| saha-grid-modulu | Admin, PersonelGrid, Sevkiyat Sorumlu Mühendis, Görüntüleyici Yükleyici, Sistem Yönetici | 3K Ustabaşı, Görüntüleyici, GeVernova, Açık Kalem Yetkilisi, Sipariş Operasyon Yetkilisi |
| saha-3k-modulu | Admin, Personel3K, Sistem Yönetici, Sipariş Operasyon Yetkilisi | Sevkiyat Sorumlu Mühendis, 3K Ustabaşı, Görüntüleyici, GeVernova, Görüntüleyici Yükleyici |
| yedek-grid-modulu | Admin, GeVernova, Sistem Yönetici | Görüntüleyici, Görüntüleyici Yükleyici, Sipariş Operasyon Yetkilisi |
| yedek-3k-modulu | Admin, Personel3K, Sistem Yönetici, Sipariş Operasyon Yetkilisi | Görüntüleyici, GeVernova, Görüntüleyici Yükleyici |
| islem-onay-merkezi | Admin, Sevkiyat Sorumlu Mühendis, Sistem Yönetici | — |
| onay-kurallari-yonet | Admin, Sistem Yönetici | — |

<a id="kilit-kalite"></a>

## 4. Ortak kilit, kalite ve süreç kuralları

**L-01.** Fiziksel olarak sevk edilmiş sandık, `SevkiyatDuzeltmeAcikMi=false` ise kilitlidir. Sadece Grid'den 3K'ya sevk etmek fiziksel sandık sevki değildir.

**L-02.** Ürünün kilidi ilgili sandık içerik bağlarından bulunur. Tekil kontrolde gerektiğinde fiili/orijinal sandık numarası da değerlendirilir. Çok sandıklı satırda yalnız ekrandaki numaraya bakarak diğer sandığın kilidi yok sayılamaz.

**L-03.** Tam sevk edilmiş projeye bağlı kayıtlar veri kaydetme aşamasında da korunur. Onaylı sandık düzeltmesi yalnız ilgili kapsamı açar; bütün projeyi sınırsız düzenlemeye açmaz. Hareket geçmişi ve onay kayıtlarının yazılması bu kilitten ayrı tutulur.

**L-04.** Kilit açmada iki mod farklıdır:

| Mod | Korunan/değişen veri |
|---|---|
| Sevkiyat kaydı korunsun | Sandık sevk edilmiş kalır; düzeltme bayrağı açılır. Sevkiyat ilişkileri/tarihleri otomatik silinmez. |
| Sevkiyat geri alınsın | İlgili sevkiyat-sandık ilişkileri kaldırılır; sandık sevk öncesi durumuna, bilgi yoksa Kapandı durumuna döner. Düzeltme bayrağı ve sevk öncesi durum bilgisi temizlenir. |

**L-05.** Son etkin sevkiyat da geri alınmışsa projenin gerçekleşen sevk tarihi temizlenebilir. Başka normal/saha sevki sürüyorsa tarih ve proje durumu buna göre korunur/yeniden hesaplanır.

**L-06.** Kaydı korunarak açılan sandığa yeniden “Sevk Et” yapılmaz; “Düzeltmeyi Tamamla” kullanılır. Bu işlem düzeltme bayrağını kapatır ve sandığı yeniden kilitler, ikinci sevkiyat üretmez.

**L-07.** Normal proje yalnız saha üzerinden sevk edilmişse ve açılacak fiziksel normal sandık sevki yoksa, normal proje kilit açma işlemiyle saha sevki iptal edilmez. İlgili saha sevki/aktarımı kendi akışından ele alınır.

**L-08.** Onaylı sevkiyat düzeltmesi kalite, aktif saha, stok, transfer veya miktar kurallarını iptal etmez.

**L-09.** Kalite ve süreç iki ayrı alan ve işlemdir. Kalite/süreç güncellemede geçerli lookup, seçili ürün ve proje ilişkisi aranır. Aktif saha kaynağı veya sevk kilitli sandık varsa işlem engellenir; eski/yeni değer loglanır.

**L-10.** Kalite “Tadilatta”, ilgili Grid/3K operasyonlarında engel olabilir. Tamamlanmış süreç de ilgili işlem/güncelleme yollarında engeldir. Tamamlanmış süreci tekrar süreç seçimiyle değiştirme kontrolü vardır. Tekil/toplu ve devam sevki istisnaları G/U bölümlerinde açıklanır.

**L-11.** Yerel kalite seçenekleri Onaylandı/Tadilatta; süreç seçenekleri Ambar, İmalat, Tedarik, Tedarik 3K Teslim, Siparişte, Tamamlandı'dır. Görünen metinler lookup kaynaklıdır; süreç alanı yalnız serbest açıklama değildir.

Kaynak: [SandikSevkKilidiHelper.cs](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/SandikSevkKilidiHelper.cs), [ProjectLockInterceptor.cs:34](C:/Users/Watarzie/source/repos/3K_Proje/3K.Infrastructure/Data/Interceptors/ProjectLockInterceptor.cs:34), [ProjeKilidiAcCommand.cs](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/ProjeIslemleri/Commands/ProjeKilidiAcCommand.cs), [SandikKilidiAcCommand.cs:87](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/SandikIslemleri/Commands/SandikKilidiAcCommand.cs:87), [SandikSevkiyatDuzeltmeTamamlaCommand.cs](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/SandikIslemleri/Commands/SandikSevkiyatDuzeltmeTamamlaCommand.cs), Grid kalite/süreç komutları.

<a id="saha"></a>

## 5. Normal proje–saha–yedek ilişkisi ve tamamlanma

**S-01.** Aktif saha aktarımına bağlanmış normal kaynak satırında Grid/3K ve ilgili sandık işlemleri kaynak proje üzerinden yürütülmez; iş saha projesinde sürdürülür. Böylece aynı ihtiyacın hem normalde hem sahada iki kez karşılanması önlenir.

**S-02.** Saha bağlantısı sadece proje adına eklenen bir metin değildir. Kaynak satır, hedef satır, aktarım kalemi ve sandık ilişkileri birlikte kullanılır; eski verilerdeki kaynak satır bağlantıları da bazı sorgularda desteklenir.

**S-03.** Aktarım planı, gerçekten karşılanan miktar, işin tamamlanması ve fiziksel sevk edilmiş gerçekleşen miktar farklı haritalardır. “Sahaya ayırdım” ile “sahada teslim aldım” veya “sahadan sevk ettim” aynı toplamı ifade etmez.

**S-04.** Normal projede ürün tamamlanması, saha iş tamamlama etkisi dikkate alınan etkin kalana göre hesaplanır. Saha/yedek projede bağlı ürünler kendi satırının iş tamamlanma kuralını kullanır.

**S-05.** Saha/yedekte bağlı ürün için Grid İptal veya Grid Kapandı işi tamamlanmış saydırır. Aksi halde hatalı/uyumsuzluk olmamalı ve aritmetik kalan ≤0 olmalıdır. Bağsız manuel içerikte miktar >0 ve konulan ≥miktar aranır.

**S-06.** Bu nedenle bütün diğer işleri bitmiş bir saha projesindeki son açık ürün iptal edilince iş tamamlanma yüzdesine yansır; teslim miktarını sahte şekilde artırmaya gerek yoktur. Normal kaynak projeye etkisi saha iş tamamlama bağlantısı üzerinden hesaplanır.

**S-07.** Yüzde ürün/satır tamamlanma oranıdır; “toplam gelen adet / toplam adet” oranıyla aynı değildir. Normalde çeki satırları, saha/yedekte sandık içerikleri esas alınabilir. Çok sandığa bölünme satır sayısını etkileyebilir.

**S-08.** İş tamamlanma yüzdesi ile Sevk Edildi/Kısmi Sevk durumu ayrı ölçüttür. İptal ile iş tamamlanması, sevkiyat kaydının tamamlanması değildir. Eksikle kapatılmış/sevk edilmiş sandık %100 ürün tamamlanması gerektirmez.

**S-09.** Normal proje sevk durumu fiziksel normal sandıklar ve doğrulanmış saha sevkleriyle hesaplanır. Bazı normal sandıklar sevk edilmiş ama hepsi değilse Kısmi Sevk; bütün sandıklar saha üzerinden etkin sevk edilmiş özel durumda tam sevk; diğer durumlarda sevk kapsamındaki ürün tamamlanması belirleyicidir.

**S-10.** Normalde henüz sevk yoksa sandıkların hazır/kapalı olması tamamlanma durumunu etkiler. Saha/yedekte tüm sandıklar sevk edilmişse veya korunmuş mevcut tam-sevk durumu varsa sevk durumu gösterilebilir; mevcut kısmi durum da kendi kuralıyla korunur.

**S-11 — Liste görünürlüğü.** Sevk edilenler sorgusu kayıtlı Sevk Edildi/Kısmi Sevk durumlarını veya fiziksel sevkli sandığı olan projeleri getirir. Aktif proje sorgusu yalnız kayıtlı tam Sevk Edildi durumunu dışlar; Kısmi Sevk aktif listede de bulunabilir. Bunun Grid/3K iş listesindeki ürün filtresiyle aynı olduğu varsayılmamalıdır.

**S-12 — Eski veri farkı.** Veritabanındaki kayıtlı proje durumu önce filtrelenir, bazı görüntü durumları sonra yeniden hesaplanır. Eski ve uyumsuz kayıtlı durum, yüzdeden bağımsız görünürlük farkına yol açabilir. Bir projenin aktarımını silip yeniden kurmak genel iş kuralı değildir; veri tamiri ayrıca doğrulanan kayıtlarla yapılmalıdır.

**S-13.** Aktif saha bağlantısı bulunan kaynak/hedef satırların veya sandıkların bağımsız silinmesi korumalıdır. Resmi aktarımı geri alma akışı, teslim/sevk/transfer bağımlılıkları incelenerek kullanılmalıdır.

**S-14.** Depoda görünme ile tamamlanma farklıdır. Sevk edilmiş sandık depo görünümünden çıkar; Grid Kapandı veya gerçek gelen/kaynak karşılama izi depo uygunluğunda kullanılır. Sadece iptal veya %100 görünüm fiziksel stok oluşturmaz.

**S-15.** Belirsiz olmayan açıkça atanmış depo lokasyonu otomatik hesapla gelişigüzel ezilmez. Sevkli sandığa normal lokasyon atama ayrıca engellenir.

Kaynak: [SahaAktarimBlokajHelper.cs](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/SahaAktarimBlokajHelper.cs), [SahaTamamlamaService.cs](C:/Users/Watarzie/source/repos/3K_Proje/3K.Infrastructure/Services/SahaTamamlamaService.cs), [SahaYedekUrunTamamlanmaHelper.cs](C:/Users/Watarzie/source/repos/3K_Proje/3K.Core/Helpers/SahaYedekUrunTamamlanmaHelper.cs), [NormalProjeSevkDurumHelper.cs](C:/Users/Watarzie/source/repos/3K_Proje/3K.Core/Helpers/NormalProjeSevkDurumHelper.cs), [ProjeListeleQueryHandler.cs:27](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/ProjeIslemleri/Queries/ProjeListeleQueryHandler.cs:27), [ProjeRepository.cs:56](C:/Users/Watarzie/source/repos/3K_Proje/3K.Infrastructure/Repositories/ProjeRepository.cs:56), [SandikDepoKurali.cs](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/SandikDepoKurali.cs).

<a id="sandik"></a>

## 6. Sandık kapatma, nihai sevkiyat, silme ve raporlar

### 6.1 Sandık kapatma ve son sevkiyat

**C-01.** Sandık kapatmada sandık var olmalı; sevk edilmiş sandık yeniden kapatılamaz, zaten kapalı sandık tekil işlemde tekrar kapatılamaz.

**C-02.** Sandıkta aktif sahaya aktarılmış normal ürün varsa normal sandığı kapatma reddedilir. `ForceClose` bu saha engelini kaldırmaz.

**C-03.** Bağlı çeki ürünlerinin kalan >0 olması eksik/hatalı uyarısı üretir. Kullanıcı `ForceClose` ile uyarıyı kabul ederse sandık Kapandı olabilir. Bu, ürünlerin kalanını sıfırlamaz ve eksiklerin teslim alındığı anlamına gelmez.

**C-04.** Toplu kapatma, sevkli veya sahaya aktarılmış sandık varsa reddedilir. Eksik/hatalı uyarısında ForceClose yoksa toplu işlem uygulanmaz. Zaten kapalı olanlar normal akışta atlanır. Kapatma kontrolü çeki bağlantılı içerikleri incelemektedir; bağsız manuel içerik için aynı kontrolün otomatik uygulandığı varsayılmaz.

**C-05.** Tekil sandık sevki için sandık istenen projeye ait olmalı ve Kapandı durumunda bulunmalıdır. Aktif saha aktarımındaki kaynak sandık normal projeden yeniden sevk edilemez.

**C-06.** Sevkte eski sandık durumu korunur, yeni durum Sevk Edildi olur, düzeltme kapanır; mevcut ilişki yoksa sevkiyat kaydı oluşturulur. Projenin gerçekleşen ilk sevk tarihi ve sevk durumu güncellenir.

**C-07.** Saha sandığı sevk/düzeltme/geri alma işlemi, kaynak normal proje durumunu bağlantılar üzerinden yeniden eşitler. Grid aktif sevk sayaçlarını sıfırlamak nihai sandık sevkiyatını geri almak yerine geçmez.

Kaynak: [SandikKapatCommandHandler.cs:30](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/SandikIslemleri/Commands/SandikKapatCommandHandler.cs:30), [TopluSandikKapatCommandHandler.cs](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/SandikIslemleri/Commands/TopluSandikKapatCommandHandler.cs), [SandikSevkEtCommand.cs:48](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/SandikIslemleri/Commands/SandikSevkEtCommand.cs:48).

### 6.2 Silme türlerinin ayrımı

**D-01.** Manuel ürün silme ile “Çeki satırlarını sil” ayrı işlemlerdir. Birindeki korumalar diğerinin aynısı değildir.

**D-02.** Manuel normal ürün silme yalnız manuel eklenmiş, doğru projeye bağlı ve kaynak bağlantısı olmayan satıra uygulanır. Grid/3K miktarı, sevk, karşılamalar, iade/hata, kalite veya süreç işlemi varsa önce bunların uygun geri alma akışı gerekir.

**D-03.** Saha/yedek manuel silme de çeki/import kaydını veya saha aktarımıyla üretilen bağlı ürünü manuel kabul etmez. Aktif saha bağlantısı ve sevkli sandık silmeye engeldir. Bağlı içerikler birlikte değerlendirilir.

**D-04.** Çeki satırlarını silmede bütün seçili satırlar bulunmalı; aktif saha aktarımı ve sevkli sandık engelleri aşılmamalıdır. Başka seçilmemiş satıra aktif proje transferi veren kaynak doğrudan silinemez; önce hedef karşılaması geri alınmalı veya ilgili hedefler de seçim kapsamında olmalıdır.

**D-05.** Silinen hedefe dışarıdan yapılmış transfer geri alınır, kaynak `ProjeGonderilen` miktarı düşürülür ve kaynak durum/kalanı yeniden hesaplanır. İlgili stok hareketleri stok geri alma yordamıyla çözülür; sadece miktar kolonları silinmez.

**D-06.** İlgili içerikler ve boş kalan sandık/çeki kayıtları temizlenebilir; kalan sandık durumları ve hareket geçmişi güncellenir. Bu nedenle kullanıcı satır iptal etmek istiyorsa silme aynı işlem değildir.

Kaynak: [ManuelUrunSilmeKurali.cs](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/ManuelUrunSilmeKurali.cs), [ManuelUrunSilCommandHandler.cs](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/SandikIslemleri/Commands/ManuelUrunSilCommandHandler.cs), [CekiSatirlariSilCommand.cs:46](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/CekiIslemleri/Commands/CekiSatirlariSilCommand.cs:46).

### 6.3 Raporlar

**R-01.** Eksik raporunda istenen proje tipi gerçek projeyle eşleşmelidir. Normal: `eksik-raporu`; saha: `saha-sevk-sonrasi-eksik-raporu`; yedek: `yedek-eksik-raporu` menü yetkisi aranır. Yanlış proje tipiyle farklı rapor yetkisi kullanılamaz.

**R-02.** Normal/yedek eksik raporunda etkin rapor kalanı >0 olanlar ve aktif saha kaynağı olan satırlar listelenir. Saha sevk sonrası eksik raporu tamamlanan ürünleri de listeler; başlığında “eksik” yazması yalnız eksik satır içerdiği anlamına gelmez.

**R-03.** Bu raporun saha haritası bağlı saha satırlarının istenen miktarlarını toplar ve eksik raporda yalnız sevkli sandıklarla sınırlamaz. İş tamamlanma veya sevk edilmiş gerçekleşen haritalarıyla aynı tanım değildir.

**R-04.** Üretilen PDF/Excel raporlarında sandık numarası ortak doğal karşılaştırıcıyla sıralanır (1, 2, 10; SND-1, SND-2, SND-10); rakamlar sayısal tipe çevrilmediğinden büyük değerler taşmaz. Eşit doğal değerler özgün metinle, aynı sandıkta ürünler mevcut sıra numarası ve kayıt kimliğiyle kararlı sıralanır. Boş numaralar sona gelir, özgün numara gösterimi korunur. Miktar gösterimi tam sayıysa ondalık göstermeyebilir, değilse dört ondalığa kadar biçimlenir. Orijinal çeki Excel şablonunu dolduran yol, şablon satır/formül düzenini korur.

**R-05.** Gerçekleşen çeki çıktısı sevk durumundaki veya sevkiyat kaydı bulunan projelerde açılır. Fiili sandık ve gerçekleşen miktar, orijinal çeki planından ayrı değerlendirilir. Saha/yedekte çeki bağlantısı olmayan içeriklerin rapora yansıtılması için içerik bazlı satır üretimi vardır.

**R-06.** Rapor toplamları ile ekrandaki sayaçlar farklı kapsamda olabilir: tamamlanan ürün sayısı, eksik ürün sayısı, toplam miktar ve fiziksel gerçekleşen ayrı değerlerdir. Eşit görünmeleri genel bir kural değildir.

Kaynak: [GetEksikUrunlerPdfQuery.cs](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/PdfIslemleri/Queries/GetEksikUrunlerPdfQuery.cs), [GetEksikUrunlerPdfQueryHandler.cs](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/PdfIslemleri/Queries/GetEksikUrunlerPdfQueryHandler.cs), [PdfService.cs:60](C:/Users/Watarzie/source/repos/3K_Proje/3K.Infrastructure/Services/PdfService.cs:60), [PdfService.cs:1311](C:/Users/Watarzie/source/repos/3K_Proje/3K.Infrastructure/Services/PdfService.cs:1311), [PdfService.cs:2119](C:/Users/Watarzie/source/repos/3K_Proje/3K.Infrastructure/Services/PdfService.cs:2119).

---

<a id="grid"></a>

## 7. Grid kuralları

### 7.1. Kapsam ve okuma anahtarı

Bu doküman 12.09.2026 tarihindeki **çalışma ağacında bulunan** backend, frontend ve regresyon testlerinin davranışını tarif eder.
Gelecek tasarım önerisi değildir; mevcut uygulamanın denetlenebilir bir fotoğrafıdır.

- `[KOD]`: Çalışan backend/frontend kodundan doğrudan gözlenen davranış.
- `[SÖZLEŞME]`: Testle sabitlenmiş veya kod yorumunda açıkça tanımlanmış beklenen davranış.
- `[KABUL]`: Kullanıcı tarafından mevcut haliyle kabul edilmiş iş kararı.
- `[FARK]`: Katmanlar, yorumlar veya tekil/toplu yollar arasında gözlenen farklılık; çözüm önerisi içermez.
- Backend yolları depo köküne göredir.
- `_onyuz/` öneki `C:/Users/Watarzie/Desktop/3k_onyuz/` referans frontend kökünü ifade eder.
- Saha senkronizasyonunun iç hesapları, genel yetkilendirme, çeki revizyonu ve 3K'nın kendi karşılama ayrıntıları bu belgenin ilgili bölümlerinin kapsamındadır; burada yalnız Grid davranışını değiştiren sınırlar yazılmıştır.

### 7.2. Temel kavramlar ve iki ayrı durum ekseni

**G-001 `[KOD]` — Grid durumu ayrı bir iş eksenidir.** `GridDurumuId`, ürünün Grid tarafındaki kabul/işleme sonucunu taşır; sevkiyatın durumunu tek başına ifade etmez. Kaynak: [CekiSatiri.cs:31](C:/Users/Watarzie/source/repos/3K_Proje/3K.Core/Entities/CekiSatiri.cs:31).

**G-002 `[KOD]` — Grid durum kümesi 14 değerdir.** Bekliyor(1), Üretimde(2), Stok Hazır(3), Sevk Edildi(4), Kısmi Sevk Edildi(5), Bekletiliyor(6), İptal Edildi(7), Tam Geldi(8), Eksik Geldi(9), Gelmedi(10), Trafo Sevk(11), İptal(12), Siparişte(13), Grid Kapandı(14). Kaynak: [GridDurum.cs:6](C:/Users/Watarzie/source/repos/3K_Proje/3K.Core/Enums/GridDurum.cs:6).

**G-003 `[FARK]` — Enum yorumu sayısal olarak güncel değildir.** Yorum “13 kayıt” derken enumda 14 değer vardır. Kaynak: [GridDurum.cs:3](C:/Users/Watarzie/source/repos/3K_Proje/3K.Core/Enums/GridDurum.cs:3).

**G-004 `[KOD]` — Grid sevk durumu ikinci ve bağımsız eksendir.** Sevk Edildi(1), Bekliyor(2), Sevk Edilmedi(3), Yeniden Sevk Gerekli(4) değerlerini taşır. Kaynak: [GridSevkDurum.cs:3](C:/Users/Watarzie/source/repos/3K_Proje/3K.Core/Enums/GridSevkDurum.cs:3).

**G-005 `[KOD]` — Aktif/son sevk partisi ile kümülatif teslim aynı şey değildir.** `GridSevkMiktari` aktif/son partinin miktarını ve teslim tamamlandıktan sonra da bu parti değerini; `GelenMiktar` bütün partilerden 3K'nın teslim aldığı kümülatif miktarı temsil eder. Kaynak: [GridUcKSevkPartisiKurali.cs:7](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:7), [CekiSatiri.cs:49](C:/Users/Watarzie/source/repos/3K_Proje/3K.Core/Entities/CekiSatiri.cs:49).

**G-006 `[KOD]` — Aktif parti ayrıca yaşam döngüsü alanlarıyla izlenir.** `AktifGridSevkKarsilananMiktari` yalnız aktif partide karşılanan miktardır; erken sonuçlandırma bayrağında `null=legacy`, `false=takipli ve erken sonuçlandırılmamış`, `true=erken sonuçlandırılmış` anlamı vardır. `false` tek başına teslim edilebilir miktar kaldığını göstermez. Kaynak: [CekiSatiri.cs:53](C:/Users/Watarzie/source/repos/3K_Proje/3K.Core/Entities/CekiSatiri.cs:53), [GridUcKSevkPartisiKurali.cs:148](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:148).

**G-007 `[KOD]` — “3K tarafında işlem var” merkezi ölçütü üçlüdür.** 3K durumu Bekliyor değilse veya `GelenMiktar>0` ise veya `KarsilananMiktar>0` ise gerçekleşmiş 3K işlemi vardır. Kaynak: [GridUcKSevkPartisiKurali.cs:80](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:80).

### 7.3. Grid API yüzeyi

**G-008 `[KOD]` — Okuma uçları.** `GET api/Grid/is-listesi` aksiyon işlerini; `GET api/Grid/{projeId}/urunler` proje ürünlerini, isteğe bağlı sandık kimliği/numarası filtresiyle döndürür. Kaynak: [GridController.cs:20](C:/Users/Watarzie/source/repos/3K_Proje/3K_API/Controllers/GridController.cs:20).

**G-009 `[KOD]` — Yazma uçları.** Tekil durum güncelleme/sıfırlama; toplu sevk; manuel ürün ekleme; kalite/süreç güncelleme; toplu terminal durum ve toplu sıfırlama ayrı komutlardır. Kaynak: [GridController.cs:48](C:/Users/Watarzie/source/repos/3K_Proje/3K_API/Controllers/GridController.cs:48).

### 7.4. Tekil Grid durum güncelleme

**G-010 `[KOD]` — Komut alanları.** Ürün ve proje kimliği, yeni Grid durumu, opsiyonel Grid gelen/Trafo/sevk miktarları, opsiyonel sevk durumu ve açıklama alınır. Kaynak: [GridDurumGuncelleCommand.cs:10](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridDurumGuncelleCommand.cs:10).

**G-011 `[KOD]` — Sayısal doğrulama hassasiyeti.** Kimlikler pozitif olmalı; üç miktar alanı en çok 14 tam + 4 ondalık basamak kabul eder; pozitif sevk miktarı verilirse sevk durumu “Sevk Edildi” olmalıdır. Kaynak: [GridValidators.cs:7](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Validators/GridValidators.cs:7).

**G-012 `[KOD]` — Tekil komutun kabul ettiği durumlar sınırlıdır.** Tam Geldi, Eksik Geldi, Gelmedi, Trafo Sevk, İptal, Siparişte, Bekliyor ve Grid Kapandı kabul edilir; enumdaki diğer altı durum bu komutla atanamaz. Kaynak: [GridDurumGuncelleCommandHandler.cs:19](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridDurumGuncelleCommandHandler.cs:19).

**G-013 `[KOD]` — Satır ve bağlam kilitleri işlemden önce uygulanır.** Aktif saha tamamlaması olan kaynak normal satır ve sevk edilmiş sandıktaki satır güncellenemez. Kaynak: [GridDurumGuncelleCommandHandler.cs:59](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridDurumGuncelleCommandHandler.cs:59).

**G-014 `[KOD]` — Tadilatta blokajı tekil Grid güncellemesinde geneldir.** Kalite metni tam olarak “Tadilatta” ise seçilen yeni Grid durumu ne olursa olsun güncelleme durur. Kaynak: [GridDurumGuncelleCommandHandler.cs:86](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridDurumGuncelleCommandHandler.cs:86).

**G-015 `[KOD]` — Gerçek 3K işlemi sonrası temel kilit vardır.** Merkezi devam-sevk kararı oluşmadıkça Grid durumu değiştirilemez; kullanıcı önce 3K durumunu sıfırlamaya yönlendirilir. Kaynak: [GridDurumGuncelleCommandHandler.cs:66](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridDurumGuncelleCommandHandler.cs:66).

**G-016 `[KOD]` — Tekil yolda 3K kilidinin tek istisnası devam sevkidir.** Karar yeni parti üretmeli, istenen Grid durumu Tam/Eksik/Trafo olmalı ve sevk durumu “Sevk Edildi” gönderilmelidir. Kaynak: [GridDurumGuncelleCommandHandler.cs:69](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridDurumGuncelleCommandHandler.cs:69).

**G-017 `[KOD]` — Kullanıcı izi her başarılı tekil denemede parent satıra yazılır.** Devam sevki dahil `GridPersonelId` oturum kullanıcısı, `GridAciklama` istek açıklaması olur. Kaynak: [GridDurumGuncelleCommandHandler.cs:101](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridDurumGuncelleCommandHandler.cs:101).

#### 7.4.1. Duruma göre alan etkileri

| Kural | Yeni durum | Doğrudan alan etkisi |
|---|---|---|
| G-018 `[KOD]` | Tam Geldi | `GridGelenAdet=IstenenAdet`, `TrafoSevkAdet=0`. Kaynak: [GridDurumGuncelleCommandHandler.cs:118](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridDurumGuncelleCommandHandler.cs:118). |
| G-019 `[KOD]` | Eksik Geldi | Gelen zorunlu, `0<gelen<IstenenAdet`; Trafo sıfırlanır. Kaynak: [GridDurumGuncelleCommandHandler.cs:123](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridDurumGuncelleCommandHandler.cs:123). |
| G-020 `[KOD]` | Gelmedi | Grid/Trafo sıfır; sevk durumu Sevk Edilmedi; aktif sevk miktarı ve parti takibi temizlenir. Kaynak: [GridDurumGuncelleCommandHandler.cs:132](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridDurumGuncelleCommandHandler.cs:132). |
| G-021 `[KOD]` | Trafo Sevk | Trafo `>0` ve `<=IstenenAdet`; Grid gelen verilmezse 0; ikisinin toplamı isteneni aşamaz. Kaynak: [GridDurumGuncelleCommandHandler.cs:140](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridDurumGuncelleCommandHandler.cs:140). |
| G-022 `[KOD]` | İptal | Grid/Trafo sıfır; sevk durumu Sevk Edilmedi; sevk miktarı ve aktif parti takibi temizlenir. Kaynak: [GridDurumGuncelleCommandHandler.cs:152](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridDurumGuncelleCommandHandler.cs:152). |
| G-023 `[KOD]` | Siparişte | İptal ile aynı sayaç/sevk temizliği uygulanır. Kaynak: [GridDurumGuncelleCommandHandler.cs:160](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridDurumGuncelleCommandHandler.cs:160). |
| G-024 `[KOD]` | Grid Kapandı | Mevcut miktarlar korunur; ilgili sandık Grid lokasyonuna alınır. Kaynak: [GridDurumGuncelleCommandHandler.cs:168](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridDurumGuncelleCommandHandler.cs:168). |
| G-025 `[KOD]` | Bekliyor | Yalnız Grid durumu ile personel/açıklama değişir; switch içinde miktar veya sevk alanı temizliği yoktur. Kaynak: [GridDurumGuncelleCommandHandler.cs:108](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridDurumGuncelleCommandHandler.cs:108). |

**G-026 `[KOD]` — Sevk talebi yalnız belirli Grid sonuçlarıyla uyumludur.** “Sevk Edildi” seçimi, devam sevki veya Tam/Eksik ya da Grid geleni pozitif Trafo Sevk satırında mümkündür. Kaynak: [GridDurumGuncelleCommandHandler.cs:179](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridDurumGuncelleCommandHandler.cs:179).

**G-027 `[KOD]` — İlk sevkin üst sınırı Grid gelenidir.** Devam sevkinde merkezi kararın üst sınırı kullanılır; her iki durumda da sevk miktarı pozitif olmalıdır. Kaynak: [GridDurumGuncelleCommandHandler.cs:192](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridDurumGuncelleCommandHandler.cs:192).

**G-028 `[KOD]` — Yeni sevk sandık tahsis kapasitesine sığmalıdır.** Fiziksel mevcut + yeni partiden sandığa girecek ihtiyaç toplam tahsis kapasitesini aşarsa 409 döner. Fazla-geldi olabilecek, proje ihtiyacını aşan parça kapasiteye eklenmez. Kaynak: [GridUcKSevkPartisiKurali.cs:241](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:241).

**G-029 `[KOD]` — Başarılı güncelleme çapraz durumları da senkronlar.** Genel ürün durumu yeniden hesaplanır; süreç otomatik tamamlanma kuralı çalışır; kaynak saha satırıysa kaynak proje senkronu tetiklenir. Kaynak: [GridDurumGuncelleCommandHandler.cs:228](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridDurumGuncelleCommandHandler.cs:228).

**G-030 `[KOD]` — Trafo Sevk sandığı otomatik kapatabilir.** Devam sevki olmayan Trafo işleminden sonra aynı sandıktaki her ürün 3K tamamlanmış tiplerden birinde veya Trafo Sevk ise sandık Kapandı yapılır. Kaynak: [GridDurumGuncelleCommandHandler.cs:238](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridDurumGuncelleCommandHandler.cs:238).

### 7.5. Sevk miktarını 3K işlemi başlamadan değiştirme

**G-031 `[SÖZLEŞME]` — Bekleyen aktif sevk miktarı mutlak toplamdır.** 3K'da henüz hiçbir gerçek işlem yokken mevcut `2` sevk tekrar `3` girilirse sonuç `3` olur; `2+3=5` yapılmaz. Tekil ve toplu yollar aynı beklentidedir. Kaynak: [GridUcKParcaliSevkPartisiRegresyonTests.cs:838](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/GridUcKParcaliSevkPartisiRegresyonTests.cs:838).

**G-032 `[SÖZLEŞME]` — Legacy bekleyen sevk de yeni takip sözleşmesine alınır.** Sayaç/bayrak alanları null olan, işlemsiz aktif parti üzerine yazıldığında yeni miktar kaydedilir; parent ve child sayaç 0, erken sonuç bayrağı false olur. Kaynak: [GridUcKParcaliSevkPartisiRegresyonTests.cs:866](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/GridUcKParcaliSevkPartisiRegresyonTests.cs:866).

**G-033 `[SÖZLEŞME]` — Azaltma ve tekrar kaydetme idempotent mutlak davranır.** `3→2→2` çağrıları sonunda aktif sevk 2 kalır; her çağrıda miktar eklenmez. Kaynak: [GridUcKParcaliSevkPartisiRegresyonTests.cs:893](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/GridUcKParcaliSevkPartisiRegresyonTests.cs:893).

**G-034 `[SÖZLEŞME]` — Ondalıklı miktar korunur.** Dört basamağa kadar ondalıklı yeni mutlak sevk miktarı tekil ve toplu yolda kaybolmadan taşınır. Kaynak: [GridUcKParcaliSevkPartisiRegresyonTests.cs:956](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/GridUcKParcaliSevkPartisiRegresyonTests.cs:956), [GridUcKParcaliSevkPartisiRegresyonTests.cs:1559](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/GridUcKParcaliSevkPartisiRegresyonTests.cs:1559).

**G-035 `[SÖZLEŞME]` — Çoklu sandıkta overwrite child takibini yeniden başlatır.** Yeni mutlak toplam parent aktif miktarı olur; sandık bazlı aktif sayaçlar sıfırlanır ve sonraki 3K teslimi yeni toplamı tahsis sınırları içinde dağıtır. Kaynak: [GridUcKParcaliSevkPartisiRegresyonTests.cs:981](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/GridUcKParcaliSevkPartisiRegresyonTests.cs:981), [GridUcKSevkPartisiKurali.cs:397](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:397).

**G-036 `[SÖZLEŞME]` — Gerçek teslim başladıktan sonra overwrite kapanır.** 3K durumu/sayaçları gerçek işlem gösterdiğinde aktif sevk miktarı doğrudan ezilemez; yalnız geçerli devam-sevk kararı yeni parti açabilir. Kaynak: [GridUcKParcaliSevkPartisiRegresyonTests.cs:929](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/GridUcKParcaliSevkPartisiRegresyonTests.cs:929), [GridDurumGuncelleCommandHandler.cs:69](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridDurumGuncelleCommandHandler.cs:69).

### 7.6. Aktif parti ve devam sevki

**G-037 `[KOD]` — Legacy belirsiz kayıt otomatik tahmin edilmez.** Aktif sevk varken sayaç/bayrak nullable eşleşmiyorsa; Eksik 3K durumu sayaçsızsa; veya kümülatif geçmiş aktif partiyle tek anlamlı değilse kayıt belirsizdir ve devam kararı üretmez. Kaynak: [GridUcKSevkPartisiKurali.cs:114](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:114).

**G-038 `[KOD]` — Erken kapatılmamış parti ile teslim edilebilir parti ayrı kararlardır.** Pozitif aktif miktar gerekir; erken sonuçlandırılmış parti kapalıdır. Sevk Edildi veya güvenli açık Yeniden Sevk parti erken kapatılmamış sayılabilir; fiili teslim edilebilirlik ayrıca legacy belirsizliği olmamasını ve aktif partide kalan `>0` olmasını ister. Kaynak: [GridUcKSevkPartisiKurali.cs:160](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:160).

**G-039 `[KOD]` — Yeniden sevk borcu birinci öncelikli devam türüdür.** Borç ve kalan pozitifse, önceki parti yok/erken sonuçlu/tamam/yorumlanabilir legacy Gelmedi ise üst sınır `min(YenidenSevkGerekliAdet,KalanMiktar)` olur. Kaynak: [GridUcKSevkPartisiKurali.cs:21](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:21).

**G-040 `[KOD]` — Proje transfer telafisi ikinci devam türüdür.** Sevk Edildi durumunda pozitif aktif parti tamamlandıysa, `ProjeGonderilen>0` ve kalan varsa üst sınır `min(ProjeGonderilen,KalanMiktar)` olur. Kaynak: [GridUcKSevkPartisiKurali.cs:37](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:37).

**G-041 `[KOD]` — Eksik Grid tamamlama üçüncü devam türüdür.** Grid Eksik Geldi, sevk Sevk Edildi, önceki aktif parti tamam ve kalan pozitifse yeni partinin üst sınırı kalan miktardır. Kaynak: [GridUcKSevkPartisiKurali.cs:48](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:48).

**G-042 `[KOD]` — Tam Geldi parçalı devam dördüncü türdür.** Önceki aktif parti tamamlandıktan sonra üst sınır `min(KalanMiktar, max(GridGelenAdet-GelenMiktar,0))` olarak hesaplanır. Kaynak: [GridUcKSevkPartisiKurali.cs:63](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:63).

**G-043 `[SÖZLEŞME]` — Borç hiçbir zaman mevcut kalanı aşan yeni parti üretmez.** Ondalıklı kalan da korunur; kalan 0 ise borç pozitif görünse bile devam partisi yoktur. Kaynak: [GridUcKParcaliSevkPartisiRegresyonTests.cs:1118](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/GridUcKParcaliSevkPartisiRegresyonTests.cs:1118).

**G-044 `[KOD]` — Yeni parti takibi atomik başlar.** Aktif miktar yeni mutlak değere, parent sayaç 0'a, erken sonuç bayrağı false'a; mevcut child aktif sayaçları 0'a çekilir. Kaynak: [GridUcKSevkPartisiKurali.cs:397](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:397).

**G-045 `[KOD]` — Gerçek devam partisi 3K kararını yeniden açar.** 3K durum/tip Bekliyor, teslim tarihi null olur; Eksik Grid tamamlama Grid'i Tam Geldi'ye yükseltir; yeniden-sevk borcu sevk kadar azalır. Kaynak: [GridUcKSevkPartisiKurali.cs:423](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:423).

**G-046 `[KABUL]` — Grid Tam Geldi satırda kalan bulunması tek başına alternatif kaynak kapısını açmaz.** Örneğin istenen 5, tamamlanmış Grid aktif partisi 2 ve kalan 3 ise stok/proje/tedarikçi yolu sırf kalan var diye açılmaz; başka açık istisna yoksa kalan Grid'in parçalı devam sevkiyle ilerler. Bu dokümanda değişiklik önerilmemektedir. Kaynak: [UcKDurumGuncelleCommandHandler.cs:145](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:145), [GridUcKSevkPartisiKurali.cs:63](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:63).

### 7.7. Toplu Grid sevki

**G-047 `[KOD]` — Toplu sevk miktarı satır tipine göre seçilir.** Normal ilk akışta istenen toplam; Trafo satırında yalnız Grid gelen; devam akışında merkezi kararın üst sınırı sevk edilir. Kaynak: [GridTopluSevkCommandHandler.cs:101](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridTopluSevkCommandHandler.cs:101).

**G-048 `[KOD]` — Normal toplu sevk satırı Tam Geldi'ye taşır.** Devam olmayan ve Trafo olmayan satırda Grid Tam Geldi, Grid gelen istenen, Trafo 0 yapılır; sonra aktif parti başlatılır. Kaynak: [GridTopluSevkCommandHandler.cs:111](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridTopluSevkCommandHandler.cs:111).

**G-049 `[KOD]` — Toplu sevkin bütün seçimi durduran kilitleri vardır.** Seçimde tek bir sevk edilmiş sandık, sahaya aktarılmış kaynak satır veya Tadilatta ürün varsa hiçbir satır sevk edilmez. Kaynak: [GridTopluSevkCommandHandler.cs:62](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridTopluSevkCommandHandler.cs:62).

**G-050 `[KOD]` — Toplu sevkte bazı engeller satır bazlı atlanır.** Devam kararı olmayan gerçek 3K işlemi ve yetersiz sandık tahsis kapasitesi ilgili satırı atlar; diğer uygun satırlar kaydedilebilir. Kaynak: [GridTopluSevkCommandHandler.cs:101](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridTopluSevkCommandHandler.cs:101).

**G-051 `[KOD]` — Hiç başarı yoksa hata türü nedene bağlıdır.** Kapasite engeli varsa 409 ve örnek detaylar; aksi halde sevke uygun satır bulunamadığına ilişkin 400 döner. Kaynak: [GridTopluSevkCommandHandler.cs:173](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridTopluSevkCommandHandler.cs:173).

**G-052 `[FARK]` — Kısmi başarı ayrıntısı API başarı sonucunda taşınmaz.** Atlanan satırlar hareket açıklamasına yazılır; frontend yalnız genel “başarıyla sevk edildi” bildirimi gösterir. Kaynak: [GridTopluSevkCommandHandler.cs:185](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridTopluSevkCommandHandler.cs:185), [grid-urunler.component.ts:942](C:/Users/Watarzie/Desktop/3k_onyuz/src/app/features/grid/grid-urunler/grid-urunler.component.ts:942).

### 7.8. Toplu terminal durum güncelleme

**G-053 `[KOD]` — Yalnız üç hedef desteklenir.** Toplu komut Tam Geldi, Grid Kapandı veya İptal dışında durum kabul etmez. Kaynak: [GridTopluDurumGuncelleCommandHandler.cs:34](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridTopluDurumGuncelleCommandHandler.cs:34).

**G-054 `[KOD]` — Sevk/saha kilidi tüm toplu terminal işlemi durdurur.** Seçimde bir kilitli sandık ya da sahaya aktarılmış kaynak satır olması yeterlidir. Kaynak: [GridTopluDurumGuncelleCommandHandler.cs:75](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridTopluDurumGuncelleCommandHandler.cs:75).

**G-055 `[KOD]` — 3K blokajı toplu terminalde yalnız Tam Geldi'ye uygulanır.** 3K işlemi olan Tam Geldi satırları atlanır; toplu İptal ve Grid Kapandı bu kontrolden bilinçli olarak muaftır. Kaynak: [GridTopluDurumGuncelleCommandHandler.cs:96](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridTopluDurumGuncelleCommandHandler.cs:96).

**G-056 `[SÖZLEŞME]` — Toplu İptal geçmiş fiziksel kaynakları silmez.** Grid/Trafo ve aktif sevk takibi temizlenir; mevcut 3K gelen, stok/proje/tedarikçi gibi karşılanmış kaynak miktarları korunur. Kaynak: [GridTopluDurumGuncelleCommandHandler.cs:116](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridTopluDurumGuncelleCommandHandler.cs:116), [GridUcKParcaliSevkPartisiRegresyonTests.cs:1014](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/GridUcKParcaliSevkPartisiRegresyonTests.cs:1014).

**G-057 `[SÖZLEŞME]` — Toplu Grid Kapandı aktif sevk geçmişini korur.** Miktarlar ve aktif sayaçlar değişmez; sandık Grid lokasyonuna alınır; terminal kalan görünümü 0 olur. Kaynak: [GridTopluDurumGuncelleCommandHandler.cs:128](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridTopluDurumGuncelleCommandHandler.cs:128), [GridUcKParcaliSevkPartisiRegresyonTests.cs:1055](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/GridUcKParcaliSevkPartisiRegresyonTests.cs:1055).

**G-058 `[FARK]` — Tekil ve toplu terminal 3K davranışı eşit değildir.** Tekil İptal/Grid Kapandı gerçek 3K işlemi sonrası G-015 nedeniyle engellenir; toplu karşılıkları G-055 muafiyetini kullanır. Kaynak: [GridDurumGuncelleCommandHandler.cs:69](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridDurumGuncelleCommandHandler.cs:69), [GridTopluDurumGuncelleCommandHandler.cs:96](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridTopluDurumGuncelleCommandHandler.cs:96).

**G-059 `[FARK]` — Tekil ve toplu Tam Geldi alan etkisi eşit değildir.** Tekil Tam Geldi Trafo'yu 0 yapar; toplu Tam Geldi yalnız Grid geleni istenene çeker ve mevcut Trafo miktarını açıkça sıfırlamaz. Kaynak: [GridDurumGuncelleCommandHandler.cs:118](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridDurumGuncelleCommandHandler.cs:118), [GridTopluDurumGuncelleCommandHandler.cs:109](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridTopluDurumGuncelleCommandHandler.cs:109).

**G-060 `[FARK]` — Tadilatta kontrolü toplu terminal handlerında yoktur.** Tekil Grid güncelleme ve toplu sevk Tadilatta satırı engellerken toplu Tam/Grid Kapandı/İptal handlerı aynı kontrolü yapmaz. Kaynak: [GridDurumGuncelleCommandHandler.cs:86](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridDurumGuncelleCommandHandler.cs:86), [GridTopluSevkCommandHandler.cs:77](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridTopluSevkCommandHandler.cs:77), [GridTopluDurumGuncelleCommandHandler.cs:75](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridTopluDurumGuncelleCommandHandler.cs:75).

**G-061 `[FARK]` — Toplu terminal kısmi başarıyı genel başarı olarak döndürür.** En az bir satır güncellendiyse Tam Geldi nedeniyle atlanan satırlar olsa bile sonuç başarıdır ve atlama listesi istemciye verilmez. Kaynak: [GridTopluDurumGuncelleCommandHandler.cs:91](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridTopluDurumGuncelleCommandHandler.cs:91).

### 7.9. Grid durumunu sıfırlama

**G-062 `[KOD]` — Tekil sıfırlama güçlü geri almadır.** Grid durumu fiilen Gelmedi; Grid/Trafo 0; sevk Sevk Edilmedi; sevk miktarı, aktif takip, yeniden-sevk borcu, tarih, personel, Grid açıklaması, kalite ve süreç null/0 yapılır. Kaynak: [GridDurumSifirlaCommandHandler.cs:82](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridDurumSifirlaCommandHandler.cs:82).

**G-063 `[KOD]` — Sıfırlama gerçek 3K işlemini ezmez.** Tekilde işlem reddedilir; topluda o satır atlanır. Her iki yol da sevk edilmiş sandık ve saha aktarım kilitlerine uyar. Kaynak: [GridDurumSifirlaCommandHandler.cs:53](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridDurumSifirlaCommandHandler.cs:53), [GridTopluSifirlaCommandHandler.cs:60](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridTopluSifirlaCommandHandler.cs:60).

**G-064 `[KOD]` — Geri alma sandığı yeniden açabilir.** Alanlar temizlendikten sonra ilgili kapanmış sandıkların durumu ortak senkronizasyon yardımcısıyla yeniden değerlendirilir. Kaynak: [GridDurumSifirlaCommandHandler.cs:100](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridDurumSifirlaCommandHandler.cs:100), [GridTopluSifirlaCommandHandler.cs:148](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridTopluSifirlaCommandHandler.cs:148).

**G-065 `[KOD]` — Toplu sıfırlama satır bazlı 3K atlaması yapar.** Zaten sıfır kabul edilen satırlar sessiz, 3K işlemli satırlar hata listesine alınarak atlanır; en az bir başarı varsa genel sonuç başarıdır. Kaynak: [GridTopluSifirlaCommandHandler.cs:81](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridTopluSifirlaCommandHandler.cs:81).

**G-066 `[FARK]` — Sıfırlama etiketi ile kaydedilen değer farklıdır.** Kod satırı `GridDurumuId=Gelmedi` yapar; hareket metni ve “YeniDeğer” bunu “Bekliyor (Sıfırlandı)” diye kaydeder. Kaynak: [GridDurumSifirlaCommandHandler.cs:82](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridDurumSifirlaCommandHandler.cs:82).

**G-067 `[FARK]` — “Zaten sıfır” kontrolü bütün temizlenen alanları kapsamaz.** Kontrol Grid sevk miktarı, aktif takip, tarih, personel ve açıklamayı sınamaz; diğer temel alanlar sıfırsa komut bu kalıntıları temizlemeden “zaten sıfır” diyebilir. Kaynak: [GridDurumSifirlaCommandHandler.cs:63](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridDurumSifirlaCommandHandler.cs:63), [GridTopluSifirlaCommandHandler.cs:90](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridTopluSifirlaCommandHandler.cs:90).

**G-068 `[FARK]` — Frontend geri-al görünürlüğü backend kontrolünden daha dardır.** UI yalnız Grid metni Bekliyor/Gelmedi dışında veya kalite/süreç doluysa reset durumu görür; sevk miktarı/durumu/borç gibi alanları hesaba katmaz. Kaynak: [grid-urunler.component.ts:871](C:/Users/Watarzie/Desktop/3k_onyuz/src/app/features/grid/grid-urunler/grid-urunler.component.ts:871).

### 7.10. Manuel Grid ürünü

**G-069 `[KOD]` — Manuel ürün son yüklenen çekiye eklenir.** Projenin `YuklemeTarihi` en yeni çekisi seçilir; çeki yoksa 404, sandık numarası boşsa 400 döner. Kaynak: [GridManuelUrunEkleCommandHandler.cs:22](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridManuelUrunEkleCommandHandler.cs:22).

**G-070 `[KOD]` — Sandık bulunamazsa oluşturulur.** Yeni sandık TipId=1, Hazırlanıyor ve Belirsiz lokasyonla açılır; mevcut sevk edilmiş sandığa ürün eklenemez. Kaynak: [GridManuelUrunEkleCommandHandler.cs:38](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridManuelUrunEkleCommandHandler.cs:38).

**G-071 `[KOD]` — Manuel satır başlangıç durumu hazır kabul edilir.** Sıra 9999, manuel bayrağı true, Grid Tam Geldi/Grid gelen istenen, sevk Sevk Edilmedi, 3K Bekliyor ve süreç Tamamlandı kaydedilir. Kaynak: [GridManuelUrunEkleCommandHandler.cs:58](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridManuelUrunEkleCommandHandler.cs:58).

**G-072 `[KOD]` — Manuel satırın sandık içeriği tam konulmuş oluşturulur.** Tahsis ve konulan istenen miktara, eksik 0'a eşitlenir. Kaynak: [GridManuelUrunEkleCommandHandler.cs:79](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridManuelUrunEkleCommandHandler.cs:79).

**G-073 `[FARK]` — Miktar doğrulaması katmanlar arasında farklıdır.** Frontend açıklama/sandık ve `miktar>0` ister, boş barkodu “MANUEL” gönderir; backend handlerında yalnız sandık numarası doğrudan doğrulanır ve bu komut için FluentValidator görünmez. Kaynak: [grid-urunler.component.ts:989](C:/Users/Watarzie/Desktop/3k_onyuz/src/app/features/grid/grid-urunler/grid-urunler.component.ts:989), [GridManuelUrunEkleCommandHandler.cs:22](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridManuelUrunEkleCommandHandler.cs:22).

### 7.11. Kalite ve süreç komutları

**G-074 `[KOD]` — Kalite değeri lookup üzerinden doğrulanır.** Seçim boşsa/geçersizse durur; satırlar hem kimlik hem proje ile filtrelenir; saha aktarımı veya sevk edilmiş sandık bütün seçimi engeller. Kaynak: [KaliteDurumGuncelleCommandHandler.cs:31](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/KaliteDurumGuncelleCommandHandler.cs:31).

**G-075 `[KOD]` — Süreç değeri hem validator hem lookup ile doğrulanır.** Proje ve her satır kimliği pozitif, liste dolu ve enum değeri geçerli olmalı; handler ayrıca lookup metnini kontrol eder. Kaynak: [GridValidators.cs:43](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Validators/GridValidators.cs:43), [SurecDurumGuncelleCommandHandler.cs:31](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/SurecDurumGuncelleCommandHandler.cs:31).

**G-076 `[KOD]` — Tamamlandı süreç durumu nihaidir.** Seçimde bir tamamlanmış satır varsa süreç toplu güncellemesi 409 ile bütünü durdurur. Kaynak: [SurecDurumGuncelleCommandHandler.cs:49](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/SurecDurumGuncelleCommandHandler.cs:49), [GridSurecDurumHelper.cs:8](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridSurecDurumHelper.cs:8).

**G-077 `[KOD]` — Süreç otomatik tamamlanabilir.** Persist edilmiş süreç henüz Tamamlandı değilse, Grid durumu İptal dışında ve Grid eksik `<=0` olduğunda Tamamlandı atanır; helper tamamlanmış süreci geri açmaz. Kaynak: [GridSurecDurumHelper.cs:13](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridSurecDurumHelper.cs:13).

**G-078 `[KOD]` — UI aynı otomatik sonucu hesaplanmış olarak da gösterir.** İptal olmayan ve Grid eksiği 0 satır, persisted süreç metni boş olsa bile “Tamamlandı” görünür ve kilitli kabul edilir. Kaynak: [grid-urunler.component.ts:1365](C:/Users/Watarzie/Desktop/3k_onyuz/src/app/features/grid/grid-urunler/grid-urunler.component.ts:1365).

### 7.12. Ürün listesi, sandık dağılımı ve miktar görünümü

**G-079 `[KOD]` — Ürün listesi proje bazında tüm satırları çeker.** Proje yoksa 404; satırlar sıra numarasına göre gelir; bu uç DTO listesi döndürür ve backend sayfalama parametresi yoktur. Kaynak: [GetGridUrunlerQueryHandler.cs:28](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Queries/GetGridUrunlerQueryHandler.cs:28), [GridController.cs:33](C:/Users/Watarzie/source/repos/3K_Proje/3K_API/Controllers/GridController.cs:33).

**G-080 `[KOD]` — Bir parent satır çoklu sandıkta çoklu görünür.** Her `SandikIcerik` için ayrı DTO üretilir; tahsis yoksa fiili/çekideki sandık numarasıyla tek fallback görünüm oluşturulur. Kaynak: [GetGridUrunlerQueryHandler.cs:97](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Queries/GetGridUrunlerQueryHandler.cs:97).

**G-081 `[KOD]` — Ana ve sandık miktarı ayrıdır.** Tek tahsiste ana güncel istenen; çoklu tahsiste ilgili sandık payı birincil miktardır. `AnaIstenenAdet` daima güncel parent toplamını, `SandikMiktari` fiziksel tahsisi taşır. Kaynak: [GetGridUrunlerQueryHandler.cs:189](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Queries/GetGridUrunlerQueryHandler.cs:189), [GridUrunDto.cs:21](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/DTOs/GridUrunDto.cs:21).

**G-082 `[KOD]` — Orijinal miktar yalnız geçmiş göstergesidir.** `OrijinalIstenenAdet` null değilse ilk gerçek miktar değişikliği görünür; hesaplamalara katılmaz. Kaynak: [CekiSatiri.cs:13](C:/Users/Watarzie/source/repos/3K_Proje/3K.Core/Entities/CekiSatiri.cs:13), [GridUrunDto.cs:29](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/DTOs/GridUrunDto.cs:29).

**G-083 `[KOD]` — UI miktar hiyerarşisi.** Birincil satırda güncel/dağıtılmış miktar; çoklu tahsiste farklıysa “Ana toplam”; geçmiş varsa “Çekide/Çeki toplamında … · Miktar güncellendi” gösterilir. Kaynak: [grid-urunler.component.html:218](C:/Users/Watarzie/Desktop/3k_onyuz/src/app/features/grid/grid-urunler/grid-urunler.component.html:218), [grid-urunler.component.ts:1374](C:/Users/Watarzie/Desktop/3k_onyuz/src/app/features/grid/grid-urunler/grid-urunler.component.ts:1374).

**G-084 `[KOD]` — Grid/Trafo toplamları sandıklara paylaştırılır.** Grid gelen, Trafo, aktif sevk, yeniden-sevk borcu ve proje gönderilen değerleri tahsis oranına göre sandık DTO'larına dağıtılır; child kaynak sayaçları çoklu tahsiste doğrudan okunur. Kaynak: [GetGridUrunlerQueryHandler.cs:120](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Queries/GetGridUrunlerQueryHandler.cs:120).

**G-085 `[KOD]` — Grid eksik formülü.** Normalde `max(Istenen-GridGelen-Trafo,0)`; İptal ve Grid Kapandı için 0'dır. Entity getter aynı terminal kuralı uygular fakat normal sonucu `max(0)` ile sınırlamaz. Kaynak: [GetGridUrunlerQueryHandler.cs:180](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Queries/GetGridUrunlerQueryHandler.cs:180), [CekiSatiri.cs:160](C:/Users/Watarzie/source/repos/3K_Proje/3K.Core/Entities/CekiSatiri.cs:160).

**G-086 `[KOD]` — Kalanın Grid görünümündeki terminal davranışı.** İptal/Grid Kapandı kalan 0; diğerlerinde 3K gelen ve alternatif kaynaklar düşülür, proje gönderilen eklenir, Trafo düşülür; hatalı ürün tamam görünse bile en az 1 kalır. Kaynak: [CekiSatiri.cs:201](C:/Users/Watarzie/source/repos/3K_Proje/3K.Core/Entities/CekiSatiri.cs:201).

**G-087 `[KOD]` — Tablodaki “Grid Sevk” ham aktif parti değildir.** Aktif miktar yoksa 0; 3K Bekliyor + sevk Sevk Edildi iken `kümülatif gelen + aktif sevk`, diğer durumda `max(kümülatif gelen,aktif sevk)` gösterilir. Kaynak: [grid-urunler.component.ts:1136](C:/Users/Watarzie/Desktop/3k_onyuz/src/app/features/grid/grid-urunler/grid-urunler.component.ts:1136), [grid-urunler.component.html:263](C:/Users/Watarzie/Desktop/3k_onyuz/src/app/features/grid/grid-urunler/grid-urunler.component.html:263).

**G-088 `[KOD]` — Saha/Yedek bağımsız sandık içeriği sentetik tamamlanmış satırdır.** Bağlı çeki satırı olmayan içerik negatif sentetik kimlikle; Grid/3K Tam Geldi, Sevk Edildi ve kalan 0 görünür; normal Grid mutasyonuna açılmaz. Kaynak: [GetGridUrunlerQueryHandler.cs:266](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Queries/GetGridUrunlerQueryHandler.cs:266), [grid-urunler.component.ts:1033](C:/Users/Watarzie/Desktop/3k_onyuz/src/app/features/grid/grid-urunler/grid-urunler.component.ts:1033).

### 7.13. Grid iş listesi

**G-089 `[KOD]` — İş listesi iki sınıf üretir.** Merkezi devam kararı olan satır `yeniden`, “Yeniden sevk gerekli”, öncelik 1 olur; aksi halde Grid Eksik Geldi + Grid eksiği ve kalan pozitifse `eksik`, öncelik 2 olur. Kaynak: [GridIsListesiSiniflandirma.cs:21](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Queries/GridIsListesiSiniflandirma.cs:21).

**G-090 `[KOD]` — Teslimatı süren parti iş listesinden geçici düşer.** Yeni devam partisi yokken aktif parti teslim edilebilir ve gerçek 3K işlemi varsa Grid işi üretilmez. 3K hiç başlamadıysa Eksik Grid satırı listede kalabilir ve mevcut sevk toplamı değiştirilebilir. Kaynak: [GridIsListesiSiniflandirma.cs:16](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Queries/GridIsListesiSiniflandirma.cs:16).

**G-091 `[KOD]` — Sevk edilmiş proje/sandık varsayılan olarak elenir.** Yalnız açık sevkiyat düzeltme penceresiyle eşleşen satır yeniden aday olabilir. Kaynak: [GetGridIsListesiQueryHandler.cs:35](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Queries/GetGridIsListesiQueryHandler.cs:35).

**G-092 `[KOD]` — Özet sayaçları filtre öncesidir.** Toplam, Eksik, Yeniden ve Bugün sayaçları sınıflanmış bütün adaylardan hesaplanır; ardından Bugün ve iş tipi filtresi uygulanır. Kaynak: [GetGridIsListesiQueryHandler.cs:101](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Queries/GetGridIsListesiQueryHandler.cs:101).

**G-093 `[KOD]` — Sayfalama satır değil proje bazlıdır.** Projeler son işlem tarihi, öncelik ve proje numarasına göre sıralanır; sayfadaki projelerin bütün iş satırları birlikte döner. Sayfa en az 1, boyut 1–100 aralığına sıkıştırılır. Kaynak: [GetGridIsListesiQueryHandler.cs:31](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Queries/GetGridIsListesiQueryHandler.cs:31).

**G-094 `[FARK]` — Backend sayfalama SQL seviyesinde değildir.** Önce bütün aday satırlar materialize edilip sınıflanır, sonra proje gruplarına `Skip/Take` uygulanır. Kaynak: [GetGridIsListesiQueryHandler.cs:35](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Queries/GetGridIsListesiQueryHandler.cs:35).

**G-095 `[FARK]` — İş listesi araması yalnız açık sayfada istemci tarafındadır.** Frontend arama metnini API'ye göndermez; dönen sayfanın proje/müşteri/sandık/barkod/ürün alanlarını filtreler. Kaynak: [grid-is-listesi.component.ts:59](C:/Users/Watarzie/Desktop/3k_onyuz/src/app/features/grid/grid-is-listesi/grid-is-listesi.component.ts:59).

**G-096 `[KOD]` — Frontend sayfa boyutu proje adedidir.** Varsayılan 25; seçenekler 15/25/50/100'dür. Önceki/sonraki her tıklama API'yi yeniden çağırır. Kaynak: [grid-is-listesi.component.ts:42](C:/Users/Watarzie/Desktop/3k_onyuz/src/app/features/grid/grid-is-listesi/grid-is-listesi.component.ts:42), [grid-is-listesi.component.html:213](C:/Users/Watarzie/Desktop/3k_onyuz/src/app/features/grid/grid-is-listesi/grid-is-listesi.component.html:213).

**G-097 `[KOD]` — Aksiyon miktarı iş tipine göre gösterilir.** Yeniden işte yeniden-sevk borcu; eksik işte Grid eksiği; bunlar yoksa kalan miktar kullanılır. Kaynak: [grid-is-listesi.component.ts:217](C:/Users/Watarzie/Desktop/3k_onyuz/src/app/features/grid/grid-is-listesi/grid-is-listesi.component.ts:217).

**G-098 `[KOD]` — İşten doğru modül rotasına gidilir.** Normal proje `/grid`, Saha `/saha-yonetimi/grid`, Yedek `/yedek-yonetimi/grid` rotasına ve odak çeki satırı parametresine yönlenir. Kaynak: [grid-is-listesi.component.ts:203](C:/Users/Watarzie/Desktop/3k_onyuz/src/app/features/grid/grid-is-listesi/grid-is-listesi.component.ts:203).

### 7.14. Doğrulama ve tekil/toplu sınır farkları

**G-099 `[FARK]` — Bazı toplu Grid handlerları proje üyeliğini sorguda doğrulamaz.** Toplu sevk, toplu terminal ve toplu sıfırlama satırları yalnız gönderilen kimliklerle yükler; `request.ProjeId` ayrıca satırın proje kimliğiyle eşleştirilmez. Kalite/süreç handlerları ise iki alanı birlikte filtreler. Kaynak: [GridTopluSevkCommandHandler.cs:49](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridTopluSevkCommandHandler.cs:49), [GridTopluDurumGuncelleCommandHandler.cs:58](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridTopluDurumGuncelleCommandHandler.cs:58), [GridTopluSifirlaCommandHandler.cs:48](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridTopluSifirlaCommandHandler.cs:48), [KaliteDurumGuncelleCommandHandler.cs:41](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/KaliteDurumGuncelleCommandHandler.cs:41).

**G-100 `[FARK]` — Tekil Grid handlerı da komuttaki proje ile satırın projesini doğrudan karşılaştırmaz.** Satır kimlikle yüklenir; `ProjeId` lokasyon değişimi ve hareket kaydı gibi yan işlemlerde kullanılır. Kaynak: [GridDurumGuncelleCommandHandler.cs:53](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridDurumGuncelleCommandHandler.cs:53).

**G-101 `[FARK]` — Sevk durumu için backend enum doğrulaması görünmez.** Pozitif sevk miktarında yalnız “Sevk Edildi” eşleşmesi zorlanır; frontend seçimleri dört enum değeriyle sınırlar, ancak handler genel olarak gelen başka bir sayıyı atamadan önce `Enum.IsDefined` kontrolü yapmaz. Kaynak: [GridValidators.cs:22](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Validators/GridValidators.cs:22), [GridDurumGuncelleCommandHandler.cs:179](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridDurumGuncelleCommandHandler.cs:179), [grid-urunler.component.ts:44](C:/Users/Watarzie/Desktop/3k_onyuz/src/app/features/grid/grid-urunler/grid-urunler.component.ts:44).

**G-102 `[FARK]` — Frontend Grid durum seçenekleri backend kabul kümesinden dardır.** UI yalnız Tam, Eksik, Gelmedi, Trafo, İptal ve Grid Kapandı sunar; backend ayrıca Siparişte ve Bekliyor kabul eder. Kaynak: [grid-urunler.component.ts:35](C:/Users/Watarzie/Desktop/3k_onyuz/src/app/features/grid/grid-urunler/grid-urunler.component.ts:35), [GridDurumGuncelleCommandHandler.cs:19](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridDurumGuncelleCommandHandler.cs:19).

**G-103 `[FARK]` — UI ve backend 3K işlemi algısı birebir aynı değildir.** UI fallback kilidi 3K durumu ve `GelenMiktar` üzerinden kurar; backend merkezi ölçütü bunlara ek olarak `KarsilananMiktar>0` kabul eder. Backend kararı son otoritedir. Kaynak: [grid-urunler.component.ts:1062](C:/Users/Watarzie/Desktop/3k_onyuz/src/app/features/grid/grid-urunler/grid-urunler.component.ts:1062), [GridUcKSevkPartisiKurali.cs:80](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:80).

**G-104 `[FARK]` — Checkbox seçilebilirliği bütün operasyon kilitlerini önceden uygulamaz.** UI seçim kutusunu saha manuel/sahaya aktarılmış/sevk edilmiş sandık için kapatır; Tadilatta ve 3K işlemli satırlar seçilebilir kalabilir, nihai sonucu ilgili backend toplu handlerı belirler. Kaynak: [grid-urunler.component.ts:1033](C:/Users/Watarzie/Desktop/3k_onyuz/src/app/features/grid/grid-urunler/grid-urunler.component.ts:1033), [GridTopluSevkCommandHandler.cs:77](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Commands/GridTopluSevkCommandHandler.cs:77).

### 7.15. Kısa tekil/toplu davranış matrisi

| İşlem | Sevk edilmiş sandık | Aktif saha kaynak satırı | Tadilatta | Gerçek 3K işlemi | Kısmi başarı |
|---|---:|---:|---:|---:|---:|
| Tekil Grid güncelle | Engeller | Engeller | Engeller | Yalnız geçerli devam sevki geçer | Yok |
| Toplu sevk | Seçimin tümünü engeller | Seçimin tümünü engeller | Seçimin tümünü engeller | Devam değilse satırı atlar | Var |
| Toplu Tam Geldi | Seçimin tümünü engeller | Seçimin tümünü engeller | Handler kontrol etmez | Satırı atlar | Var |
| Toplu İptal/Grid Kapandı | Seçimin tümünü engeller | Seçimin tümünü engeller | Handler kontrol etmez | Muaf | Normalde tüm seçili uygun satırlar |
| Tekil sıfırlama | Engeller | Engeller | Özel blok yok | Engeller | Yok |
| Toplu sıfırlama | Seçimin tümünü engeller | Seçimin tümünü engeller | Özel blok yok | Satırı atlar | Var |

Matristeki davranışların kaynakları G-013–G-016, G-049–G-061 ve G-063–G-065'te verilmiştir.

### 7.16. Talep Formu çıktı akışı

**G-105 `[KOD]` — Talep Formu yalnız seçili ve süreç açısından açık satırlardan başlar.** Buton süreç yazma alanında gösterilir; seçim yoksa veya seçilenlerden biri Tamamlandı süreçteyse kapalıdır. Açılış ayrıca sevk edilmiş sandık kilidini ve tamamlanmış süreç seçimini denetler. Kaynak: [grid-urunler.component.html:78](C:/Users/Watarzie/Desktop/3k_onyuz/src/app/features/grid/grid-urunler/grid-urunler.component.html:78), [grid-urunler.component.ts:1497](C:/Users/Watarzie/Desktop/3k_onyuz/src/app/features/grid/grid-urunler/grid-urunler.component.ts:1497).

**G-106 `[KOD]` — PDF kalemleri o anki Grid görünümünden hazırlanır.** Her seçili görünümün çeki satırı kimliği, barkodu, açıklaması, `istenenAdet` değeri ve birimi forma alınır; kullanıcı çıktı miktarını değiştirebilir veya kalemi listeden çıkarabilir. Kaynak: [grid-urunler.component.ts:1502](C:/Users/Watarzie/Desktop/3k_onyuz/src/app/features/grid/grid-urunler/grid-urunler.component.ts:1502), [grid-urunler.component.html:922](C:/Users/Watarzie/Desktop/3k_onyuz/src/app/features/grid/grid-urunler/grid-urunler.component.html:922).

**G-107 `[KOD]` — Talep kaynağı üç sabit seçenektir.** Varsayılan 1=Ambar; 2 UI'da Üretim ve süreç enumunda İmalat; 3 UI'da Tedarikçi ve süreç enumunda Tedarik anlamına gelir. Kaynak: [grid-urunler.component.ts:1514](C:/Users/Watarzie/Desktop/3k_onyuz/src/app/features/grid/grid-urunler/grid-urunler.component.ts:1514), [grid-urunler.component.html:913](C:/Users/Watarzie/Desktop/3k_onyuz/src/app/features/grid/grid-urunler/grid-urunler.component.html:913), [SurecDurum.cs:3](C:/Users/Watarzie/source/repos/3K_Proje/3K.Core/Enums/SurecDurum.cs:3).

**G-108 `[KOD]` — Talep PDF'si tamamen istemcide üretilip indirilir.** A4 PDF proje, kaynak, tarih/saat ve düzenlenmiş kalemleri içerir; bu aşamada ayrı bir backend “talep kaydı oluştur” komutu çağrılmaz. Kaynak: [grid-urunler.component.ts:1536](C:/Users/Watarzie/Desktop/3k_onyuz/src/app/features/grid/grid-urunler/grid-urunler.component.ts:1536).

**G-109 `[FARK]` — PDF indirme ile süreç güncellemesi atomik değildir.** Dosya önce indirilir, sonra benzersiz çeki satırı kimlikleriyle süreç durumu asenkron güncellenir; backend güncellemesi başarısız olsa bile indirilmiş PDF geri alınmaz. Kaynak: [grid-urunler.component.ts:1677](C:/Users/Watarzie/Desktop/3k_onyuz/src/app/features/grid/grid-urunler/grid-urunler.component.ts:1677).

### 7.17. Son not

Bu bölümde `[FARK]` olarak işaretlenen maddeler otomatik olarak hata veya değişiklik talebi sayılmaz.
Özellikle G-046 mevcut ve kabul edilmiş iş kuralıdır; alternatif kaynak açılması önerilmemiştir.
Davranış değişikliği yapılacaksa önce ilgili G-numarası hedeflenmeli, tekil/toplu/UI/API etkisi birlikte kararlaştırılmalı ve sözleşme testiyle sabitlenmelidir.

---

<a id="uck"></a>

## 8. 3K karşılama kuralları

Belge tarihi: 12.09.2026

### 8.1. Amaç, kapsam ve okuma anahtarı

Bu belge, 3K ürün karşılama modülünün bugün kodda çalışan iş kurallarını tarif eder.

Backend referansları 3K_Proje depo köküne, önyüz referansları 3k_onyuz depo köküne göredir.

Belge üretim kodunu değiştirmez; veri düzeltme veya veritabanı mutabakat betiği değildir.

Global kalan, proje tamamlama, yetki ve saha kurallarının ayrıntıları bu belgenin ortak bölümlerine bırakılmıştır.

Buradaki formüller yalnız 3K akışında karar vermek için doğrudan kullanılan kısımları gösterir.

Etiketler:

- [MEVCUT]: Kodun şu an gözlenen ve uygulanmakta olan davranışı.
- [KABUL EDİLEN HEDEF]: Kullanıcı tarafından korunması özellikle kabul edilen davranış.
- [UYUMSUZLUK]: İki akışın veya istemci-sunucu sözleşmesinin bugün farklı davrandığı nokta.
- [LEGACY]: Yeni aktif-parti takip alanlarından önce oluşmuş kayıtların özel davranışı.
- [TEST]: Davranışın mevcut otomatik test karşılığı.

### 8.2. Temel kavramlar ve veri anlamları

- U-001 [MEVCUT] IstenenAdet, çeki satırının güncel ana ihtiyacıdır; sandık tahsisi değildir. Kaynak: [UcKUrunDto.cs:26](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/DTOs/UcKUrunDto.cs:26)
- U-002 [MEVCUT] SandikMiktari, ilgili SandikIcerik kaydının güncel tahsis payıdır. Kaynak: [UcKUrunDto.cs:30](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/DTOs/UcKUrunDto.cs:30)
- U-003 [MEVCUT] Bir satır birden fazla sandığa dağıtılmışsa API her sandık tahsisi için ayrı UcKUrunDto döndürür. Kaynak: [GetUcKUrunlerQueryHandler.cs:142](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Queries/GetUcKUrunlerQueryHandler.cs:142)
- U-004 [MEVCUT] Sandık tahsisi yoksa sorgu, çeki/fiili sandık numarası üzerinden tek bir fallback satırı üretir. Kaynak: [GetUcKUrunlerQueryHandler.cs:135](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Queries/GetUcKUrunlerQueryHandler.cs:135)
- U-005 [MEVCUT] GelenMiktar, tüm Grid sevk partilerinden 3K'nın fiziksel olarak teslim aldığı kümülatif miktardır. Kaynak: [GridUcKSevkPartisiKurali.cs:7](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:7)
- U-006 [MEVCUT] GridSevkMiktari kümülatif toplam değil, son takip edilen Grid sevk partisinin miktarıdır; bu alan tek başına partinin hâlâ açık veya teslim edilebilir olduğunu göstermez. Kaynak: [GridUcKSevkPartisiKurali.cs:7](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:7)
- U-007 [MEVCUT] AktifGridSevkKarsilananMiktari yalnız mevcut Grid sevk partisinde 3K tarafından karşılanan miktarı tutar. Kaynak: [GridUcKSevkPartisiKurali.cs:89](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:89)
- U-008 [MEVCUT] Aktif parti kalanı max(GridSevkMiktari - AktifPartiKarsilanan, 0) formülüdür. Kaynak: [GridUcKSevkPartisiKurali.cs:148](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:148)
- U-009 [MEVCUT] KarsilananMiktar, alternatif kaynak toplamıdır; stok, proje ve tedarikçi kırılımları ayrıca tutulur. Kaynak: [UcKDurumGuncelleCommandHandler.cs:445](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:445)
- U-010 [MEVCUT] ProjeGonderilen, bu satırdan başka projelere fiziksel olarak verilen miktardır ve net tamamlanandan düşülür. Kaynak: [UcKDurumGuncelleCommandHandler.cs:727](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:727)
- U-011 [MEVCUT] YenidenSevkGerekliAdet, eksik, gelmedi veya geri gönderim sonrasında Grid'den yeniden beklenen açık borçtur. Kaynak: [UcKDurumGuncelleCommandHandler.cs:426](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:426)
- U-012 [MEVCUT] AktifGridSevkPartisiErkenSonuclandirildiMi false ise parti erken sonuçlandırılmamıştır; teslimi tamamlanmış bir partide de false kalabilir. true Eksik/Gelmedi/Geri ile erken sonuçlandırmayı, null ise legacy veya aktif-parti bilgisinin yokluğunu taşıyabilir. Açık/teslim-edilebilir kararı ayrıca hesaplanır. Kaynak: [GridUcKSevkPartisiKurali.cs:160](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:160)
- U-013 [MEVCUT] Parent ve SandikIcerik üzerindeki aktif-parti sayaçları birlikte tutulur; parent toplamı child toplamlarıyla uyumlu olmalıdır. Kaynak: [GridUcKSevkPartisiKurali.cs:344](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:344)
- U-014 [MEVCUT] Tüm adet alanları decimal akıştadır; komut doğrulaması en çok 18 basamak ve 4 ondalık kabul eder. Kaynak: [UcKDurumGuncelleCommandValidator.cs:10](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Validators/UcKDurumGuncelleCommandValidator.cs:10)
- U-015 [MEVCUT] UcKDurumuId genel 3K durumunu, UcKKarsilamaTipiId son seçilen karşılama tipini temsil eder; Fazla Geldi işleminde bu ikisi farklı olabilir. Kaynak: [UcKDurumGuncelleCommandHandler.cs:494](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:494)
- U-016 [MEVCUT] Genel ürün durumu ve merkezi kalan, işlem sonlarında IDurumHesaplaService üzerinden yeniden hesaplanır. Kaynak: [UcKDurumGuncelleCommandHandler.cs:549](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:549)
- U-017 [MEVCUT] 3K liste DTO'su ana toplam, sandık miktarı ve sandık/ana kalanlarını ayrı alanlarla taşır. Kaynak: [UcKUrunDto.cs:26](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/DTOs/UcKUrunDto.cs:26)
- U-018 [MEVCUT] OrijinalIstenenAdet yalnız miktar revizyonu geçmişini göstermek içindir; 3K hesap üst sınırı olarak kullanılmaz. Kaynak: [UcKUrunDto.cs:26](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/DTOs/UcKUrunDto.cs:26)

### 8.3. Endpoint ve komut sözleşmesi

- U-019 [MEVCUT] Güncel tekil işlem PUT api/UcK/durum-guncelle endpointidir. Kaynak: [UcKController.cs:45](C:/Users/Watarzie/source/repos/3K_Proje/3K_API/Controllers/UcKController.cs:45)
- U-020 [MEVCUT] Güncel tekil geri alma PUT api/UcK/durum-sifirla endpointidir. Kaynak: [UcKController.cs:55](C:/Users/Watarzie/source/repos/3K_Proje/3K_API/Controllers/UcKController.cs:55)
- U-021 [MEVCUT] Güncel toplu Tam Geldi POST api/UcK/toplu-tam-geldi endpointidir. Kaynak: [UcKController.cs:65](C:/Users/Watarzie/source/repos/3K_Proje/3K_API/Controllers/UcKController.cs:65)
- U-022 [MEVCUT] Güncel toplu tedarikçi POST api/UcK/toplu-tedarikci endpointidir. Kaynak: [UcKController.cs:75](C:/Users/Watarzie/source/repos/3K_Proje/3K_API/Controllers/UcKController.cs:75)
- U-023 [MEVCUT] Güncel toplu geri alma PUT api/UcK/toplu-sifirla endpointidir. Kaynak: [UcKController.cs:85](C:/Users/Watarzie/source/repos/3K_Proje/3K_API/Controllers/UcKController.cs:85)
- U-024 [MEVCUT] Tekil güncelleme komutu CekiSatiriId, opsiyonel SandikIcerikId, ProjeId ve KarsilamaTipiId taşır. Kaynak: [UcKDurumGuncelleCommand.cs:62](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommand.cs:62)
- U-025 [MEVCUT] Miktar alanı GelenAdet adını taşır; Tam/Gelmedi dışında işlemdeki artış, fazla veya iade adedidir. Kaynak: [UcKDurumGuncelleCommand.cs:65](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommand.cs:65)
- U-026 [MEVCUT] Komut ISecuredRequest ve dinamik onay sözleşmelerini uygular; onay operasyon kodu karşılama tipinden türetilir. Kaynak: [UcKDurumGuncelleCommand.cs:13](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommand.cs:13)
- U-026A [MEVCUT] Dinamik onay işaretleyicisi yalnız tekil UcKDurumGuncelleCommand üzerindedir; durum sıfırlama ile toplu Tam Geldi, tedarikçi ve sıfırlama komutları ISecuredRequest uygular fakat IRequireApproval/IApprovalOperation/IApprovalReference uygulamaz. Bu nedenle toplu endpointler tekil işlemin dinamik onay akışına kendiliğinden girmez. Kaynak: [UcKDurumGuncelleCommand.cs:13](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommand.cs:13); [UcKDurumSifirlaCommand.cs:10](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumSifirlaCommand.cs:10); [UcKTopluTamGeldiCommand.cs:9](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKTopluTamGeldiCommand.cs:9); [UcKTopluTedarikciCommand.cs:10](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKTopluTedarikciCommand.cs:10); [UcKTopluSifirlaCommand.cs:9](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKTopluSifirlaCommand.cs:9)
- U-027 [MEVCUT] Onay açıklaması proje, stok ve tedarikçi kaynaklarında kaynak/miktar bilgisini içerir. Kaynak: [UcKDurumGuncelleCommand.cs:35](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommand.cs:35)
- U-028 [MEVCUT] Toplu secimler gönderilmişse onlar kullanılır; gönderilmemişse CekiSatiriIdler sandıksız seçimlere çevrilir. Kaynak: [UcKSandikSecimDto.cs:9](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKSandikSecimDto.cs:9)
- U-029 [MEVCUT] Toplu seçimler CekiSatiriId + SandikIcerikId ikilisine göre tekilleştirilir. Kaynak: [UcKSandikSecimDto.cs:27](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKSandikSecimDto.cs:27)

### 8.4. Tüm tekil işlemlerde ortak blokajlar

- U-030 [MEVCUT] Geçerli işlem tipleri Tam, Eksik, Gelmedi, Projeden, Stoktan, Tedarikçiden, Geri Gönderildi ve Fazla Geldi'dir. Kaynak: [UcKDurumGuncelleCommandHandler.cs:20](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:20)
- U-031 [MEVCUT] Bu küme dışındaki tip handler tarafından reddedilir. Kaynak: [UcKDurumGuncelleCommandHandler.cs:51](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:51)
- U-032 [MEVCUT] CekiSatiri bulunamazsa 404 döner. Kaynak: [UcKDurumGuncelleCommandHandler.cs:54](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:54)
- U-033 [MEVCUT] Gönderilen SandikIcerikId satıra ait değilse işlem reddedilir. Kaynak: [UcKDurumGuncelleCommandHandler.cs:59](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:59)
- U-034 [MEVCUT] Birden çok sandığa dağıtılmış satırda geri gönderim için sandık seçimi zorunludur. Kaynak: [UcKDurumGuncelleCommandHandler.cs:67](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:67)
- U-035 [MEVCUT] Normal proje satırı sahaya aktarılmışsa 3K işlemi normal projede yapılamaz; saha projesine yönlendirilir. Kaynak: [UcKDurumGuncelleCommandHandler.cs:91](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:91)
- U-036 [MEVCUT] Ürünün bulunduğu sandık sevk kilidindeyse 3K işlemi yapılamaz. Kaynak: [UcKDurumGuncelleCommandHandler.cs:95](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:95)
- U-037 [MEVCUT] Grid İptal satırında hiçbir 3K işlemi yapılamaz. Kaynak: [UcKDurumGuncelleCommandHandler.cs:100](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:100)
- U-038 [MEVCUT] Grid Kapandı satırında hiçbir 3K işlemi yapılamaz. Kaynak: [UcKDurumGuncelleCommandHandler.cs:103](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:103)
- U-039 [MEVCUT] Kalite durumu metni Tadilatta olan satırda tüm 3K güncellemeleri reddedilir. Kaynak: [UcKDurumGuncelleCommandHandler.cs:129](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:129)
- U-040 [MEVCUT] Trafo Sevk satırında fiziksel işlem, yalnız 3K'ya sevk edilmiş aktif Grid partisi varsa açıktır. Kaynak: [UcKDurumGuncelleCommandHandler.cs:106](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:106)
- U-041 [MEVCUT] Trafo Sevk satırında Geri Gönderildi için genel fiziksel gate yerine özel geri-gönderim kararı kullanılır. Kaynak: [UcKDurumGuncelleCommandHandler.cs:120](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:120)
- U-042 [MEVCUT] Grid Gelmedi satırında yalnız Projeden, Stoktan veya Tedarikçiden karşılama yapılabilir. Kaynak: [UcKDurumGuncelleCommandHandler.cs:137](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:137)
- U-043 [MEVCUT] Fiziksel tipler legacy aktif-parti geçmişi belirsizse 409 ile durur; sistem miktar tahmin etmez. Kaynak: [UcKDurumGuncelleCommandHandler.cs:177](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:177)
- U-044 [MEVCUT] Kaynak karşılamaları satır ve toplam tahsis kapasitesini aşamaz. Kaynak: [UcKDurumGuncelleCommandHandler.cs:290](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:290)
- U-045 [MEVCUT] İşlem sonunda toplam fiziksel + alternatif - proje çıkışı + trafo miktarı ana ihtiyacı aşamaz. Kaynak: [UcKDurumGuncelleCommandHandler.cs:549](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:549)
- U-046 [MEVCUT] Başarılı tekil işlem satırı, sandık içeriği ve yan kaynak hareketleri aynı UnitOfWork işlemi içinde kaydedilir. Kaynak: [UcKDurumGuncelleCommandHandler.cs:37](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:37)

### 8.5. Aktif Grid sevk partisi yaşam döngüsü

- U-047 [MEVCUT] Aktif parti yoksa, yani GridSevkMiktari sıfır veya negatifse, teslim kapalıdır. Kaynak: [GridUcKSevkPartisiKurali.cs:160](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:160)
- U-048 [MEVCUT] Erken sonuçlandırılmış parti, yalnız açık borcun alternatif kaynaktan kapanmasıyla yeniden teslimata açılmaz; resmi 3K reseti veya Grid'in yeni bir sevk partisi başlatması ayrı yaşam-döngüsü işlemleridir. Kaynak: [GridUcKSevkPartisiKurali.cs:167](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:167); [UcKDurumSifirlaCommandHandler.cs:121](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumSifirlaCommandHandler.cs:121); [GridUcKSevkPartisiKurali.cs:397](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:397)
- U-049 [MEVCUT] GridSevkDurumu SevkEdildi ise ve parti sonuçlanmamışsa parti teslimata açıktır. Kaynak: [GridUcKSevkPartisiKurali.cs:173](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:173)
- U-050 [MEVCUT] GridSevkDurumu YenidenSevkGerekli ise açık takip bayrağı false olan kısmi parti teslimata açık kalabilir. Kaynak: [GridUcKSevkPartisiKurali.cs:176](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:176)
- U-051 [MEVCUT] Liste/UI için teslim edilebilir kararı: legacy belirsiz değil, parti açık ve parti kalanı pozitif olmalıdır. Kaynak: [GridUcKSevkPartisiKurali.cs:216](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:216)
- U-052 [MEVCUT] Fazla teslim kararı normal kalan sıfır olsa dahi, güvenli ve açık aktif parti üzerinden açık kalabilir. Kaynak: [GridUcKSevkPartisiKurali.cs:227](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:227)
- U-053 [MEVCUT] Yeni parti başlatıldığında GridSevkMiktari yeni partinin mutlak miktarına yazılır; önceki partiyle toplanmaz. Kaynak: [GridUcKSevkPartisiKurali.cs:397](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:397)
- U-054 [MEVCUT] Yeni parti başlatıldığında parent aktif sayaç sıfır, sonuçlandırma bayrağı false olur. Kaynak: [GridUcKSevkPartisiKurali.cs:407](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:407)
- U-055 [MEVCUT] Yeni parti başlatıldığında child aktif sayaçları da sıfırlanır. Kaynak: [GridUcKSevkPartisiKurali.cs:411](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:411)
- U-056 [MEVCUT] Devam partisi ise UcKDurumu, UcKKarsilamaTipi ve TeslimTarihi tekrar bekleyen başlangıca alınır. Kaynak: [GridUcKSevkPartisiKurali.cs:423](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:423)
- U-057 [MEVCUT] Grid Eksik tamamlama partisi Grid durumunu Tam Geldi'ye çekebilir ve GridGelenAdet'i ihtiyaca yükseltir. Kaynak: [GridUcKSevkPartisiKurali.cs:430](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:430)
- U-058 [MEVCUT] Yeniden sevk partisi açılırken açık yeniden-sevk borcu sevk miktarı kadar azaltılır. Kaynak: [GridUcKSevkPartisiKurali.cs:438](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:438)
- U-059 [MEVCUT] Yeni yeniden-sevk borcu ve kalan varsa parti üst sınırı min(borç, kalan) olur. Kaynak: [GridUcKSevkPartisiKurali.cs:21](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:21)
- U-060 [MEVCUT] Proje çıkışı telafisinde parti üst sınırı min(ProjeGonderilen, kalan) olur. Kaynak: [GridUcKSevkPartisiKurali.cs:37](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:37)
- U-061 [MEVCUT] Grid Eksik satırın önceki partisi tamamlandıysa kalan ihtiyacın tamamı için EksikGridTamamlama kararı üretilebilir. Kaynak: [GridUcKSevkPartisiKurali.cs:48](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:48)
- U-062 [MEVCUT] Grid Tam satırda devam üst sınırı min(kalan, max(GridGelenAdet - GelenMiktar, 0)) formülüdür. Kaynak: [GridUcKSevkPartisiKurali.cs:63](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:63)
- U-063 [MEVCUT] Belirsiz legacy aktif parti için hiçbir otomatik devam sevki kararı üretilmez. Kaynak: [GridUcKSevkPartisiKurali.cs:14](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:14)
- U-064 [MEVCUT] Aktif parti teslim üst sınırı aktif parti kalanı, satır kalanı ve sandık kalanın en küçüğüdür. Kaynak: [GridUcKSevkPartisiKurali.cs:449](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:449)
- U-065 [MEVCUT] Sandık seçilmişse ayrıca o sandığın aktif parti payı üst sınırı uygulanır. Kaynak: [GridUcKSevkPartisiKurali.cs:495](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:495)
- U-066 [MEVCUT] Sandık seçilmemişse fiziksel miktar child tahsislerine sıra ile, her child hedefini aşmadan dağıtılır. Kaynak: [GridUcKSevkPartisiKurali.cs:570](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:570)
- U-067 [MEVCUT] Parent aktif sayaç hiçbir koşulda aktif Grid sevk miktarını aşamaz. Kaynak: [GridUcKSevkPartisiKurali.cs:521](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:521)
- U-068 [MEVCUT] Child sayaçlarının null/0 sözleşmesi ve toplamı tutarsızsa işlem 409 ile durur. Kaynak: [GridUcKSevkPartisiKurali.cs:526](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:526)
- U-069 [MEVCUT] Aktif parti child hedefi, parti öncesi sandık açıkları üzerinden hesaplanır; dolu sandığa yeni pay ayrılmaz. Kaynak: [GridUcKSevkPartisiKurali.cs:364](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:364)

### 8.6. Legacy NULL aktif-parti kayıtları

- U-070 [LEGACY] Aktif sevk yoksa nullable takip alanları karşılama kararını etkilemez. Kaynak: [GridUcKSevkPartisiKurali.cs:667](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:667)
- U-071 [LEGACY] Parent sayaç ve sonuç bayrağından yalnız birinin dolu olması belirsizliktir. Kaynak: [GridUcKSevkPartisiKurali.cs:121](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:121)
- U-072 [LEGACY] Yeni sayaçları olmayan UcK Eksik Geldi kaydı daima belirsiz kabul edilir. Kaynak: [GridUcKSevkPartisiKurali.cs:129](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:129)
- U-073 [LEGACY] UcK Tam/Fazla kaydı yalnız GelenMiktar aktif parti miktarına tam eşitse tamamlanmış parti olarak türetilebilir. Kaynak: [GridUcKSevkPartisiKurali.cs:100](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:100)
- U-074 [LEGACY] Gelen miktarı aktif partiden farklı Tam/Fazla kaydı otomatik yorumlanmaz. Kaynak: [GridUcKSevkPartisiKurali.cs:132](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:132)
- U-075 [LEGACY] Bekliyor/Gelmedi ve fiziksel geleni sıfır olan eski kayıt güvenli yorumlanabilir. Kaynak: [GridUcKSevkPartisiKurali.cs:141](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:141)
- U-076 [LEGACY] Güvenli Bekliyor kayıt materialize edilirken aktif sayaç 0, sonuç bayrağı false yapılır. Kaynak: [GridUcKSevkPartisiKurali.cs:707](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:707)
- U-077 [LEGACY] Güvenli Gelmedi kayıt materialize edilirken aktif sayaç 0, sonuç bayrağı true yapılır. Kaynak: [GridUcKSevkPartisiKurali.cs:707](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:707)
- U-078 [LEGACY] Kaynak karşılaması, UcK durumunu değiştirmeden önce güvenli legacy materializasyonunu çalıştırır. Kaynak: [UcKDurumGuncelleCommandHandler.cs:333](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:333)
- U-079 [LEGACY] Belirsiz geçmişte kaynak karşılama veya fiziksel teslim tahminle ilerlemez, veri mutabakatı ister. Kaynak: [GridUcKSevkPartisiKurali.cs:678](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:678)
- U-080 [LEGACY] Aktif sevki olmayan tam sıfırlamada parent ve child takip alanları 0 değil null yapılır. Kaynak: [GridUcKSevkPartisiKurali.cs:612](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:612)

### 8.7. Sevk Adeti Tam Geldi

- U-081 [MEVCUT] Tam Geldi yalnız teslimata açık aktif Grid partisi varken seçilebilir. Kaynak: [UcKDurumGuncelleCommandHandler.cs:170](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:170)
- U-082 [MEVCUT] Tam Geldi ana çeki miktarını değil, aktif partinin güvenli kalanını teslim alır. Kaynak: [UcKDurumGuncelleCommandHandler.cs:365](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:365)
- U-083 [MEVCUT] Tam miktarı kullanıcı girişinden alınmaz; merkezi aktif-parti helper'ı hesaplar. Kaynak: [UcKDurumGuncelleCommandHandler.cs:368](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:368)
- U-084 [MEVCUT] Hesaplanan miktar aktif parti sayaçlarına ve uygun child'a yazılır. Kaynak: [UcKDurumGuncelleCommandHandler.cs:388](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:388)
- U-085 [MEVCUT] Aynı miktar kümülatif GelenMiktar'a eklenir; önceki teslimler korunur. Kaynak: [UcKDurumGuncelleCommandHandler.cs:397](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:397)
- U-086 [MEVCUT] Başarılı işlem UcKDurumuId=TamGeldi ve TeslimTarihi=şimdi yapar. Kaynak: [UcKDurumGuncelleCommandHandler.cs:397](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:397)
- U-087 [MEVCUT] Tek sandıklı proje-transfer telafi paketinde sandık dolu görünse bile yeni Grid paketi ayrıca teslim alınabilir. Kaynak: [UcKProjeTransferTelafiTeslimKural.cs:7](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKProjeTransferTelafiTeslimKural.cs:7)
- U-088 [MEVCUT] Telafi paketinde alınacak miktar aktif Grid sevk miktarıdır; ham kalanı aşarsa 409 döner. Kaynak: [UcKProjeTransferTelafiTeslimKural.cs:29](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKProjeTransferTelafiTeslimKural.cs:29)

### 8.8. Sevk Adeti Eksik Geldi

- U-089 [MEVCUT] Eksik Geldi için işlem adedi pozitif olmalıdır. Kaynak: [UcKDurumGuncelleCommandHandler.cs:218](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:218)
- U-090 [MEVCUT] Girilen adet seçili sandığın tahsis miktarından küçük olmalıdır. Kaynak: [UcKDurumGuncelleCommandHandler.cs:221](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:221)
- U-091 [MEVCUT] Girilen adet aktif parti/satır/sandık üst sınırını aşamaz. Kaynak: [UcKDurumGuncelleCommandHandler.cs:402](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:402)
- U-092 [MEVCUT] Girilen adet aktif parti sayacına ve kümülatif GelenMiktar'a eklenir. Kaynak: [UcKDurumGuncelleCommandHandler.cs:415](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:415)
- U-093 [MEVCUT] İşlemden sonra aktif partide kalan miktar yeniden-sevk borcuna eklenir. Kaynak: [UcKDurumGuncelleCommandHandler.cs:426](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:426)
- U-094 [MEVCUT] Eksik işlem GridSevkDurumu'nu YenidenSevkGerekli yapar ve aktif partiyi erken sonuçlandırır. Kaynak: [UcKDurumGuncelleCommandHandler.cs:429](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:429)
- U-095 [MEVCUT] Erken sonuçlandırma nedeniyle aynı eski partinin kalanı sonradan tekrar Tam Geldi ile alınamaz. Kaynak: [GridUcKSevkPartisiKurali.cs:167](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:167)

### 8.9. Gelmedi

- U-096 [MEVCUT] Gelmedi için açık ve sevk edilmiş bir aktif Grid partisi bulunmalıdır. Kaynak: [UcKDurumGuncelleCommandHandler.cs:188](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:188)
- U-097 [MEVCUT] Aktif partide daha önce karşılanan miktar varsa Gelmedi reddedilir; önce o parti işlemi geri alınmalıdır. Kaynak: [UcKDurumGuncelleCommandHandler.cs:188](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:188)
- U-098 [MEVCUT] Gelmedi aktif-parti sayaçlarını sıfırlar. Kaynak: [UcKDurumGuncelleCommandHandler.cs:435](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:435)
- U-099 [MEVCUT] Aktif GridSevkMiktari'nin tamamı yeniden-sevk borcuna eklenir. Kaynak: [UcKDurumGuncelleCommandHandler.cs:439](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:439)
- U-100 [MEVCUT] UcK durumu Gelmedi, Grid sevk durumu YenidenSevkGerekli ve parti erken sonuçlandırılmış olur. Kaynak: [UcKDurumGuncelleCommandHandler.cs:437](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:437)
- U-101 [MEVCUT] Gelmedi işlemi sandığı varsayılan 3K depo lokasyonuna taşıyan tipler arasında değildir. Kaynak: [UcKDurumGuncelleCommandHandler.cs:647](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:647)

### 8.10. Alternatif kaynak uygunluğu

- U-102 [MEVCUT] Alternatif kaynak tipleri Projeden, Stoktan ve Tedarikçiden karşılama seçenekleridir. Kaynak: [UcKDurumGuncelleCommandHandler.cs:145](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:145)
- U-103 [MEVCUT] Bu üç tip Grid durumu Eksik Geldi, Gelmedi veya Trafo Sevk ise açıktır. Kaynak: [UcKDurumGuncelleCommandHandler.cs:146](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:146)
- U-104 [MEVCUT] Kalan pozitifken son işlem Geri Gönderildi ise alternatif kaynaklar açıktır. Kaynak: [UcKDurumGuncelleCommandHandler.cs:151](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:151)
- U-105 [MEVCUT] Kalan pozitifken GeriGonderilenMiktar veya YenidenSevkGerekliAdet pozitifse alternatif kaynaklar açıktır. Kaynak: [UcKDurumGuncelleCommandHandler.cs:151](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:151)
- U-106 [MEVCUT] Projeden karşılama için ayrıca kalan ve ProjeGonderilen pozitifse proje-transfer telafi istisnası vardır. Kaynak: [UcKDurumGuncelleCommandHandler.cs:157](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:157)
- U-107 [KABUL EDİLEN HEDEF] Grid durumu Tam Geldi, ihtiyaç 35, aktif Grid sevki/3K teslimi 32 ve kalan 3 olan satır; yukarıdaki istisnalardan biri yoksa proje, stok veya tedarikçiden karşılamaya kapalı kalır.
- U-108 [KABUL EDİLEN HEDEF] U-107 davranışı aktif-parti değişikliğinin açtığı bir regresyon değildir; aynı gate hem mevcut backend hem önyüz kodunda açıkça vardır. Kaynak: [UcKDurumGuncelleCommandHandler.cs:145](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:145); Önyüz: [uck-urunler.component.ts:1001](C:/Users/Watarzie/Desktop/3k_onyuz/src/app/features/uck/uck-urunler/uck-urunler.component.ts:1001)
- U-109 [MEVCUT] Alternatif kaynak miktarı pozitif olmalı ve satırın merkezi kalanını aşmamalıdır. Kaynak: [UcKDurumGuncelleCommandHandler.cs:225](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:225)
- U-110 [MEVCUT] Alternatif kaynak kullanıldığında açık yeniden-sevk borcu kullanılan miktar kadar azaltılır. Kaynak: [UcKDurumGuncelleCommandHandler.cs:679](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:679)
- U-111 [MEVCUT] Borç sıfırlanınca GridSevkDurumu YenidenSevkGerekli'den SevkEdildi'ye döner. Kaynak: [UcKDurumGuncelleCommandHandler.cs:687](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:687)
- U-112 [MEVCUT] Kaynak işlemi aktif eski partiyi yeniden açmaz; güvenli legacy takip önce materialize edilir. Kaynak: [UcKDurumGuncelleCommandHandler.cs:333](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:333)

### 8.11. Projeden Karşılandı

- U-113 [MEVCUT] Pozitif miktar, kaynak proje numarası ve kaynak CekiSatiriId zorunludur. Kaynak: [UcKDurumGuncelleCommandHandler.cs:225](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:225)
- U-114 [MEVCUT] Kendi projesinden karşılama blokajı, request.KaynakHedefProjeNo metnini hedef satırın gerçek ProjeNo değeriyle karşılaştırır; seçilen KaynakCekiSatiriId kaydının gerçek proje kimliği bu kontrolde kullanılmaz. Kaynak: [UcKDurumGuncelleCommandHandler.cs:232](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:232)
- U-115 [MEVCUT] Kaynak satır sahaya aktarılmışsa normal projeden kaynak olarak kullanılamaz. Kaynak: [UcKDurumGuncelleCommandHandler.cs:704](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:704)
- U-116 [MEVCUT] Backend kaynak kullanılabilirini max(GelenMiktar + ProjeKarsilanan - ProjeGonderilen, 0) hesaplar. Kaynak: [UcKDurumGuncelleCommandHandler.cs:714](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:714)
- U-117 [MEVCUT] Kaynak stok/tedarikçi kırılımları backend proje-kaynak kullanılabilir formülüne dahil değildir. Kaynak: [UcKDurumGuncelleCommandHandler.cs:780](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:780)
- U-118 [MEVCUT] Hedefte KarsilananMiktar ve ProjeKarsilanan artırılır. Kaynak: [UcKDurumGuncelleCommandHandler.cs:445](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:445)
- U-119 [MEVCUT] Kaynakta ProjeGonderilen artırılır, kalan ve durum yeniden hesaplanır. Kaynak: [UcKDurumGuncelleCommandHandler.cs:719](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:719)
- U-120 [MEVCUT] Kaynak kalan açılmışsa, kaynak daha önce sevk edilmiş aktif bir satırsa yeniden-sevk ihtiyacı açılabilir. Kaynak: [UcKDurumGuncelleCommandHandler.cs:787](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:787)
- U-121 [MEVCUT] Aktif ProjeTransfer kaydı kaynak/hedef satır, miktar ve zincir alanlarıyla oluşturulur. Kaynak: [UcKDurumGuncelleCommandHandler.cs:735](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:735)
- U-122 [MEVCUT] Önyüz kaynak proje listesinden mevcut projeyi çıkarır. Kaynak: Önyüz [uck-urunler.component.ts:161](C:/Users/Watarzie/Desktop/3k_onyuz/src/app/features/uck/uck-urunler/uck-urunler.component.ts:161)
- U-123 [MEVCUT] Önyüz kaynak ürünü ad benzerliği ve pozitif kullanılabilir miktarla filtreler. Kaynak: Önyüz [uck-urunler.component.ts:175](C:/Users/Watarzie/Desktop/3k_onyuz/src/app/features/uck/uck-urunler/uck-urunler.component.ts:175)
- U-124 [MEVCUT] Ad benzerliği tam eşleşme değildir; hedefteki en az üç karakterlik anlamlı kelimelerden birinin kaynak açıklamada geçmesi yeterlidir. Kaynak: Önyüz [uck-urunler.component.ts:862](C:/Users/Watarzie/Desktop/3k_onyuz/src/app/features/uck/uck-urunler/uck-urunler.component.ts:862)
- U-125 [UYUMSUZLUK] Backend projeden karşılama handler'ı ürün adı/barkod eşitliğini ve seçilen kaynak satırın gerçek projesinin request.KaynakHedefProjeNo ile eşleşmesini yeniden doğrulamaz; burada doğrulananlar kaynak satırın varlığı, saha aktarım blokajı ve net kullanılabilir miktardır. Kaynak: [UcKDurumGuncelleCommandHandler.cs:699](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:699)

### 8.12. Stoktan Karşılandı

- U-126 [MEVCUT] Pozitif miktar ve geçerli StokKaydiId zorunludur. Kaynak: [UcKDurumGuncelleCommandHandler.cs:244](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:244)
- U-127 [MEVCUT] Stok malzeme adı ile satır açıklaması noktalama/çoklu boşluk temizlenip Türkçe küçük harfe çevrildikten sonra tam eşit olmalıdır. Kaynak: [UcKDurumGuncelleCommandHandler.cs:255](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:255)
- U-128 [MEVCUT] Stok bakiyesi işlem miktarından azsa işlem reddedilir. Kaynak: [UcKDurumGuncelleCommandHandler.cs:266](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:266)
- U-129 [MEVCUT] Hedefte KarsilananMiktar ve StokKarsilanan artırılır. Kaynak: [UcKDurumGuncelleCommandHandler.cs:457](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:457)
- U-130 [MEVCUT] Stok bakiyesi azaltılır; sıfır olursa stok durumu Tükendi yapılır. Kaynak: [UcKDurumGuncelleCommandHandler.cs:463](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:463)
- U-131 [MEVCUT] Aynı transaction içinde StoktanKarsilandi hareketi oluşturulur. Kaynak: [UcKDurumGuncelleCommandHandler.cs:472](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:472)
- U-132 [MEVCUT] Önyüz stok seçiminde yalnız ilk 500 kayıt içinden miktarı pozitif ve durumu Aktif olanları gösterir. Kaynak: Önyüz [uck-urunler.component.ts:307](C:/Users/Watarzie/Desktop/3k_onyuz/src/app/features/uck/uck-urunler/uck-urunler.component.ts:307)
- U-133 [UYUMSUZLUK] Önyüz stok listesi tüm aktif stoğu garanti etmez; 500'den sonraki uygun kayıt seçim ekranına gelmez, backend ise gönderilen geçerli ID'yi işleyebilir. Kaynak: Önyüz [uck-urunler.component.ts:307](C:/Users/Watarzie/Desktop/3k_onyuz/src/app/features/uck/uck-urunler/uck-urunler.component.ts:307)

### 8.13. Tedarikçiden Geldi

- U-134 [MEVCUT] Tekil tedarikçi işleminde miktar pozitif olmalıdır. Kaynak: [UcKDurumGuncelleCommandHandler.cs:270](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:270)
- U-135 [MEVCUT] Hedefte KarsilananMiktar ve TedarikciKarsilanan artırılır. Kaynak: [UcKDurumGuncelleCommandHandler.cs:486](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:486)
- U-136 [MEVCUT] Toplu tedarikçi işleminde her seçim için seçili sandığın kalanının tamamı karşılanır; kullanıcı adet göndermez. Kaynak: [UcKTopluTedarikciCommandHandler.cs:68](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKTopluTedarikciCommandHandler.cs:68)
- U-137 [MEVCUT] Toplu tedarikçi gate'i Eksik/Gelmedi/Trafo veya geri gönderim/yeniden-sevk borcu istisnasını kullanır. Kaynak: [UcKTopluTedarikciCommandHandler.cs:167](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKTopluTedarikciCommandHandler.cs:167)
- U-138 [MEVCUT] Proje-transfer telafi istisnası toplu tedarikçi için geçerli değildir. Kaynak: [UcKTopluTedarikciCommandHandler.cs:167](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKTopluTedarikciCommandHandler.cs:167)
- U-139 [MEVCUT] Toplu tedarikçi uygun satırları kaydeder; bazı satırlar hatalıysa kayıt sonrası başarısız Result döndürür. Kaynak: [UcKTopluTedarikciCommandHandler.cs:125](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKTopluTedarikciCommandHandler.cs:125)
- U-140 [UYUMSUZLUK] Toplu tedarikçi handler'ı açık bir ExecuteInTransactionAsync sarmalayıcısı kullanmaz. Kaynak: [UcKTopluTedarikciCommandHandler.cs:33](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKTopluTedarikciCommandHandler.cs:33)

### 8.14. Fazla Geldi

- U-141 [MEVCUT] Fazla miktar pozitif olmalı ve StogaAktar true gönderilmelidir. Kaynak: [UcKDurumGuncelleCommandHandler.cs:274](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:274)
- U-142 [MEVCUT] Fazla işlemi yalnız güvenli ve açık aktif Grid partisi varken yapılabilir. Kaynak: [UcKDurumGuncelleCommandHandler.cs:206](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:206)
- U-143 [MEVCUT] Sistem önce normal ihtiyacın açık kısmını aktif partiden fiziksel GelenMiktar'a alır. Kaynak: [UcKDurumGuncelleCommandHandler.cs:494](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:494)
- U-144 [MEVCUT] Kullanıcının girdiği Fazla Gelen Adet normal teslim miktarına eklenmez; ayrı stok girişi olarak ele alınır. Kaynak: [UcKDurumGuncelleCommandHandler.cs:521](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:521)
- U-145 [MEVCUT] Satır UcKDurumuId TamGeldi olur, UcKKarsilamaTipiId ise FazlaGeldi olarak kalır. Kaynak: [UcKDurumGuncelleCommandHandler.cs:521](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:521)
- U-146 [MEVCUT] Fazla miktar için yeni stok kaydı ve FazlaTeslimStogaAktarildi hareketi oluşturulur. Kaynak: [UcKDurumGuncelleCommandHandler.cs:574](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:574)
- U-147 [MEVCUT] Önyüz Fazla Geldi adet alanını manuel açar ve stoka aktar checkbox'ını zorunlu tutar. Kaynak: Önyüz [uck-urunler.component.html:581](C:/Users/Watarzie/Desktop/3k_onyuz/src/app/features/uck/uck-urunler/uck-urunler.component.html:581)

### 8.15. Geri Gönderildi ve Hatalı Ürün

- U-148 [MEVCUT] Geri gönderim yeni inbound teslim değildir; daha önce fiziksel alınan Grid payını geri çevirir. Kaynak: [GridUcKSevkPartisiKurali.cs:321](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:321)
- U-149 [MEVCUT] Geri gönderilebilir parent miktar max(GelenMiktar - ProjeGonderilen, 0) ile sınırlıdır. Kaynak: [GridUcKSevkPartisiKurali.cs:302](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:302)
- U-150 [MEVCUT] Sandık seçilmişse üst sınır ayrıca KonulanAdet - stok - proje - tedarikçi fiziksel Grid payıyla sınırlanır. Kaynak: [GridUcKSevkPartisiKurali.cs:285](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:285)
- U-151 [MEVCUT] Devam eden, tamamlanmamış aktif parti varken geri gönderim kapalıdır. Kaynak: [GridUcKSevkPartisiKurali.cs:327](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:327)
- U-152 [MEVCUT] Geri gönderim için pozitif adet ve GeriGonderilmeSebebiId zorunludur. Kaynak: [UcKDurumGuncelleCommandHandler.cs:280](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:280)
- U-153 [MEVCUT] Geri gönderilen miktar aktif-parti sayacından ve kümülatif GelenMiktar'dan düşülür. Kaynak: [UcKDurumGuncelleCommandHandler.cs:526](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:526)
- U-154 [MEVCUT] Aynı miktar GeriGonderilenMiktar ve YenidenSevkGerekliAdet alanlarına eklenir. Kaynak: [UcKDurumGuncelleCommandHandler.cs:537](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:537)
- U-155 [MEVCUT] GridSevkDurumu YenidenSevkGerekli, UcK durumu GeriGönderildi olur ve aktif parti sonuçlandırılır. Kaynak: [UcKDurumGuncelleCommandHandler.cs:539](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:539)
- U-156 [MEVCUT] Bu handler GridGelenAdet'i azaltmaz; 527. satırdaki yorum mevcut uygulamayla birebir uyuşmaz. Kaynak: [UcKDurumGuncelleCommandHandler.cs:526](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:526)
- U-157 [MEVCUT] Hatalı Ürün, çalıştırılabilir bir güncel 3K karşılama tipi değildir. Kaynak: [UcKDurumGuncelleCommandHandler.cs:20](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:20)
- U-158 [MEVCUT] Hatalı Ürün enumda legacy UcKDurumu=13 olarak yaşamaya devam eder. Kaynak: [UcKDurum.cs:6](C:/Users/Watarzie/source/repos/3K_Proje/3K.Core/Enums/UcKDurum.cs:6)
- U-159 [MEVCUT] Hatalı Ürün, Geri Gönderildi işlemindeki sebep seçeneklerinden biridir ve sebep enum değeri 4'tür. Kaynak: [GeriGonderilmeSebebi.cs:6](C:/Users/Watarzie/source/repos/3K_Proje/3K.Core/Enums/GeriGonderilmeSebebi.cs:6); Önyüz [uck-urunler.component.html:605](C:/Users/Watarzie/Desktop/3k_onyuz/src/app/features/uck/uck-urunler/uck-urunler.component.html:605)
- U-160 [UYUMSUZLUK] Önyüzde Hatalı Ürün için kalmış bir validasyon dalı vardır; seçenek listesinde bulunmadığından güncel panelden erişilemez. Kaynak: Önyüz [uck-urunler.component.ts:26](C:/Users/Watarzie/Desktop/3k_onyuz/src/app/features/uck/uck-urunler/uck-urunler.component.ts:26)

### 8.16. Sandık içeriği ve lokasyon senkronizasyonu

- U-161 [MEVCUT] Başarılı 3K işleminden sonra SandikIcerik kayıtları merkezi helper ile senkronize edilir. Kaynak: [UcKDurumGuncelleCommandHandler.cs:561](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:561)
- U-162 [MEVCUT] Hedef KonulanAdet, Gelen + StokKarsilanan + ProjeKarsilanan + TedarikciKarsilanan - ProjeGonderilen toplamından türetilir. Kaynak: [UcKSandikIcerikSenkronizasyonHelper.cs:35](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKSandikIcerikSenkronizasyonHelper.cs:35)
- U-163 [MEVCUT] Hedef fiziksel miktar toplam tahsis kapasitesini aşarsa senkronizasyon başarısız olur. Kaynak: [UcKSandikIcerikSenkronizasyonHelper.cs:35](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKSandikIcerikSenkronizasyonHelper.cs:35)
- U-164 [MEVCUT] Dağıtımda seçili sandık varsa öncelik ona verilir; kalan fark diğer child kayıtlara deterministik sırayla dağıtılır. Kaynak: [UcKSandikIcerikSenkronizasyonHelper.cs:45](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKSandikIcerikSenkronizasyonHelper.cs:45)
- U-165 [MEVCUT] Her child EksikAdet değeri max(etkin tahsis miktarı - KonulanAdet, 0) olarak yenilenir; etkin tahsis, helper'ın TahsisMiktari hesabından gelir ve ham child alanıyla her durumda özdeş olmak zorunda değildir. Kaynak: [UcKSandikIcerikSenkronizasyonHelper.cs:77](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKSandikIcerikSenkronizasyonHelper.cs:77)
- U-166 [MEVCUT] Tam, Eksik, Proje, Stok, Tedarikçi ve Fazla işlemleri belirsiz sandık lokasyonunu varsayılan 3K deposuna taşır. Kaynak: [UcKDurumGuncelleCommandHandler.cs:647](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:647)
- U-167 [MEVCUT] Gelmedi ve Geri Gönderildi varsayılan 3K depo lokasyonu atamaz. Kaynak: [UcKDurumGuncelleCommandHandler.cs:669](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:669)

### 8.17. Tekil geri alma

- U-168 [MEVCUT] Tekil geri alma transaction içinde çalışır. Kaynak: [UcKDurumSifirlaCommandHandler.cs:37](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumSifirlaCommandHandler.cs:37)
- U-169 [MEVCUT] Seçili SandikIcerik satıra ait olmalıdır. Kaynak: [UcKDurumSifirlaCommandHandler.cs:48](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumSifirlaCommandHandler.cs:48)
- U-170 [MEVCUT] Tek sandık parçasında stok veya proje kaynağı varsa yalnız o child'ın geri alınması reddedilir; tüm satır geri alınmalıdır. Kaynak: [UcKDurumSifirlaCommandHandler.cs:62](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumSifirlaCommandHandler.cs:62)
- U-171 [MEVCUT] Sahaya aktarılmış, sevk kilitli veya Grid İptal satırı geri alınamaz. Kaynak: [UcKDurumSifirlaCommandHandler.cs:74](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumSifirlaCommandHandler.cs:74)
- U-172 [MEVCUT] Başka projeye aktif kaynak veren satır, hedef transfer geri alınmadan sıfırlanamaz. Kaynak: [UcKDurumSifirlaCommandHandler.cs:89](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumSifirlaCommandHandler.cs:89)
- U-173 [MEVCUT] Tüm satır geri almada stok kullanımı iade edilir; fazla teslimden doğan stok güvenle silinemiyorsa işlem durur. Kaynak: [UcKDurumSifirlaCommandHandler.cs:113](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumSifirlaCommandHandler.cs:113); [UcKStokHareketGeriAlHelper.cs:15](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKStokHareketGeriAlHelper.cs:15)
- U-174 [MEVCUT] Tüm satır geri almada aktif sayaçlar sıfırlanır/reopen edilir ve tüm 3K kaynak kırılımları sıfıra çekilir. Kaynak: [UcKDurumSifirlaCommandHandler.cs:121](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumSifirlaCommandHandler.cs:121)
- U-175 [MEVCUT] Tüm satır geri almada TeslimTarihi, açıklama, kaynak proje, geri gönderim sebebi ve yeniden-sevk borcu temizlenir. Kaynak: [UcKDurumSifirlaCommandHandler.cs:121](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumSifirlaCommandHandler.cs:121)
- U-176 [MEVCUT] Sandık bazlı geri almada o child'ın fiziksel Grid miktarı aktif sayaçtan ve GelenMiktar'dan düşülür. Kaynak: [UcKDurumSifirlaCommandHandler.cs:143](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumSifirlaCommandHandler.cs:143)
- U-177 [MEVCUT] Sandık bazlı geri almada yalnız o child'ın TedarikciKarsilanan payı ayrıca düşülür. Kaynak: [UcKDurumSifirlaCommandHandler.cs:164](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumSifirlaCommandHandler.cs:164)
- U-178 [MEVCUT] Sandık bazlı geri alma önce kalan 3K tamamlanan miktar varsa Eksik, yoksa Bekliyor durumunu hesaplar. **Geri alınan miktar mevcut Grid sevk partisinden pozitif bir pay içeriyorsa**, sonraki parti helper'ı mevcut partiyi tekrar açar: 3K durum/karşılama tipi Bekliyor olur, teslim tarihi/açıklaması temizlenir. Diğer sandıklardaki kümülatif fiziksel teslim korunur; Bekliyor etiketi bütün eski teslimlerin sıfırlandığı anlamına gelmez. Kaynak: [UcKDurumSifirlaCommandHandler.cs:168](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumSifirlaCommandHandler.cs:168), [GridUcKSevkPartisiKurali.cs:831](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:831). Aktif payın tekil/toplu geri alınması `GridUcKParcaliSevkPartisiRegresyonTests.EksikPartiSandikBazliSifirlaninca_YalnizPartiEksigiBorctanDusulurVeKalanBorcKorunur` ile doğrulanır.
- U-179 [MEVCUT] Eksik parti finalizasyonundan child geri alınırsa yalnız ilgili parti eksik borcu geri düzeltilir; önceki borç korunur. Kaynak: [GridUcKSevkPartisiKurali.cs:831](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:831)
- U-180 [MEVCUT] Tüm satır geri almada hedefe gelen aktif proje transferleri pasife çekilir ve kaynak ProjeGonderilen azaltılır. Kaynak: [UcKDurumSifirlaCommandHandler.cs:196](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumSifirlaCommandHandler.cs:196)
- U-181 [MEVCUT] Kalite ve Süreç yalnız satır tüm 3K hareketleriyle gerçekten başlangıç seviyesine döndüyse temizlenir. Kaynak: [UcKDurumSifirlaCommandHandler.cs:187](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumSifirlaCommandHandler.cs:187)

### 8.18. Toplu geri alma

- U-182 [MEVCUT] Toplu geri alma, başarısız Result için özel exception üretip transaction'ı rollback eder. Kaynak: [UcKTopluSifirlaCommandHandler.cs:35](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKTopluSifirlaCommandHandler.cs:35)
- U-183 [MEVCUT] Seçimlerden herhangi biri sahaya aktarılmışsa tüm toplu istek işlem başlamadan reddedilir. Kaynak: [UcKTopluSifirlaCommandHandler.cs:83](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKTopluSifirlaCommandHandler.cs:83)
- U-184 [MEVCUT] Sevk kilitli, Grid İptal, zaten başlangıçta veya geçersiz child olan seçimler hata listesine alınır/atlanır. Kaynak: [UcKTopluSifirlaCommandHandler.cs:91](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKTopluSifirlaCommandHandler.cs:91)
- U-185 [MEVCUT] Child stok/proje kaynağı içeriyorsa sandık bazlı toplu geri alma reddedilir. Kaynak: [UcKTopluSifirlaCommandHandler.cs:125](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKTopluSifirlaCommandHandler.cs:125)
- U-186 [MEVCUT] Aktif giden proje transferi bulunan satır atlanır. Kaynak: [UcKTopluSifirlaCommandHandler.cs:139](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKTopluSifirlaCommandHandler.cs:139)
- U-187 [MEVCUT] Hiçbir satır geri alınamadıysa bütün transaction başarısızdır. Kaynak: [UcKTopluSifirlaCommandHandler.cs:307](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKTopluSifirlaCommandHandler.cs:307)
- U-188 [MEVCUT] En az bir başarı varsa diğer seçim hataları istemciye başarısızlık olarak dönmez; endpoint Success döner. Kaynak: [UcKTopluSifirlaCommandHandler.cs:322](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKTopluSifirlaCommandHandler.cs:322)
- U-189 [MEVCUT] Geri alınan satır/sandıklar için sandık durumu yeniden açma senkronizasyonu çalışır. Kaynak: [UcKTopluSifirlaCommandHandler.cs:310](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKTopluSifirlaCommandHandler.cs:310)

### 8.19. Toplu Tam Geldi

- U-190 [MEVCUT] Toplu Tam Geldi transaction içinde çalışır ve seçimleri parent+child bazında işler. Kaynak: [UcKTopluTamGeldiCommandHandler.cs:35](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKTopluTamGeldiCommandHandler.cs:35)
- U-191 [MEVCUT] Sevk kilitli, sahaya aktarılmış, Grid İptal/Kapandı veya Kalite Tadilatta satırlar atlanır. Kaynak: [UcKTopluTamGeldiCommandHandler.cs:69](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKTopluTamGeldiCommandHandler.cs:69)
- U-192 [MEVCUT] Açık aktif Grid partisi olmayan seçim atlanır. Kaynak: [UcKTopluTamGeldiCommandHandler.cs:87](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKTopluTamGeldiCommandHandler.cs:87)
- U-193 [MEVCUT] Tekil Tam Geldi ile aynı aktif-parti üst sınırı ve child sayaçları kullanılır. Kaynak: [UcKTopluTamGeldiCommandHandler.cs:95](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKTopluTamGeldiCommandHandler.cs:95)
- U-194 [MEVCUT] Dolu child, proje-transfer telafi paketi değilse atlanır. Kaynak: [UcKTopluTamGeldiCommandHandler.cs:109](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKTopluTamGeldiCommandHandler.cs:109)
- U-195 [MEVCUT] Başarılı seçimler kaydedildikten sonra hata varsa endpoint başarısız Result döndürür; Result dönmesi transaction rollback'i tetiklemez. Kaynak: [UcKTopluTamGeldiCommandHandler.cs:208](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKTopluTamGeldiCommandHandler.cs:208); [UnitOfWork.cs:86](C:/Users/Watarzie/source/repos/3K_Proje/3K.Infrastructure/Repositories/UnitOfWork.cs:86)
- U-196 [UYUMSUZLUK] Bu nedenle toplu Tam Geldi istemcide hata gösterirken uygun satırlar kalıcı olarak güncellenmiş olabilir. Kaynak: [UcKTopluTamGeldiCommandHandler.cs:191](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKTopluTamGeldiCommandHandler.cs:191)

### 8.20. İş listesi

- U-197 [MEVCUT] İş listesi sayfa numarasını en az 1, sayfa boyutunu 1-100 aralığında sınırlar. Kaynak: [GetUcKIsListesiQueryHandler.cs:25](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Queries/GetUcKIsListesiQueryHandler.cs:25)
- U-198 [MEVCUT] Sevk edilmiş proje/sandıklar yalnız sevkiyat düzeltmesi açıksa iş listesinde kalabilir. Kaynak: [GetUcKIsListesiQueryHandler.cs:33](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Queries/GetUcKIsListesiQueryHandler.cs:33)
- U-199 [MEVCUT] İstatistik sayaçları SadeceBugun ve IsTipi filtreleri uygulanmadan önce hesaplanır. Kaynak: [GetUcKIsListesiQueryHandler.cs:85](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Queries/GetUcKIsListesiQueryHandler.cs:85)
- U-200 [MEVCUT] Öncelik 1 Yeniden Sevk Gerekli, öncelik 2 3K Teslim Bekliyor, öncelik 3 Grid Eksik Geldi'dir. Kaynak: [GetUcKIsListesiQueryHandler.cs:227](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Queries/GetUcKIsListesiQueryHandler.cs:227)
- U-201 [MEVCUT] Teslim bekleyen işi yalnız aktif parti güvenle teslim edilebilir ve parti kalanı pozitifse üretilir. Kaynak: [GetUcKIsListesiQueryHandler.cs:178](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Queries/GetUcKIsListesiQueryHandler.cs:178)
- U-202 [MEVCUT] İş listesi kalanı max(istenen - gelen - stok - proje - tedarikçi + proje çıkışı - trafo, 0) formülüdür; İptal/Kapandı için sıfırdır. Kaynak: [GetUcKIsListesiQueryHandler.cs:259](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Queries/GetUcKIsListesiQueryHandler.cs:259)
- U-203 [MEVCUT] Pagination satır sayısına değil proje grubu sayısına uygulanır; bir sayfa seçilen projelerin tüm iş satırlarını döndürür. Kaynak: [GetUcKIsListesiQueryHandler.cs:112](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Queries/GetUcKIsListesiQueryHandler.cs:112)
- U-204 [MEVCUT] Sorgu aday satırları önce veritabanından listeye alır, tip eşleme/filtre/gruplama ve pagination'ı bellekte yapar. Kaynak: [GetUcKIsListesiQueryHandler.cs:47](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Queries/GetUcKIsListesiQueryHandler.cs:47)

### 8.21. Ürün listesi DTO ve önyüz davranışı

- U-205 [MEVCUT] Backend aktif parti teslim, aktif parti kalan, fazla teslim ve geri gönderim kararlarını DTO'da ayrı alanlarla gönderir. Kaynak: [UcKUrunDto.cs:42](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/DTOs/UcKUrunDto.cs:42)
- U-206 [MEVCUT] Her sandık satırının aktif parti kalanı parent parti kalanı, merkezi kalan ve child aktif payının en küçüğüdür. Kaynak: [GetUcKUrunlerQueryHandler.cs:210](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Queries/GetUcKUrunlerQueryHandler.cs:210)
- U-207 [MEVCUT] Proje transfer telafi paketinde child boşluğu yerine satırın merkezi kalanı kullanılabilir. Kaynak: [GetUcKUrunlerQueryHandler.cs:210](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Queries/GetUcKUrunlerQueryHandler.cs:210)
- U-208 [MEVCUT] Önyüz yeni backend alanı varsa fiziksel işlem uygunluğunda bu alanı tek otorite kabul eder. Kaynak: Önyüz [uck-urunler.component.ts:927](C:/Users/Watarzie/Desktop/3k_onyuz/src/app/features/uck/uck-urunler/uck-urunler.component.ts:927)
- U-209 [MEVCUT] Yeni alanlar hiç yoksa önyüz enum tabanlı legacy fallback kullanır. Kaynak: Önyüz [uck-urunler.component.ts:942](C:/Users/Watarzie/Desktop/3k_onyuz/src/app/features/uck/uck-urunler/uck-urunler.component.ts:942)
- U-210 [MEVCUT] Geri gönderim sözleşmesinin yalnız yarısı gelirse önyüz güvenli biçimde aksiyonu kapatır; tam legacy payload'da GelenMiktar fallback'i vardır. Kaynak: Önyüz [uck-urunler.component.ts:975](C:/Users/Watarzie/Desktop/3k_onyuz/src/app/features/uck/uck-urunler/uck-urunler.component.ts:975)
- U-211 [MEVCUT] Panel Tam Geldi miktarını aktif Grid partisinin backend'den gelen kalanından otomatik doldurur ve input'u kapatır. Kaynak: Önyüz [uck-urunler.component.ts:607](C:/Users/Watarzie/Desktop/3k_onyuz/src/app/features/uck/uck-urunler/uck-urunler.component.ts:607)
- U-212 [MEVCUT] Eksik/Fazla/Geri/Kaynak işlemlerinde panel açılış miktarı kümülatif değer değildir; yeni hareket için sıfırdan başlar, Geri iade üst sınırıyla açılır. Kaynak: Önyüz [uck-urunler.component.ts:607](C:/Users/Watarzie/Desktop/3k_onyuz/src/app/features/uck/uck-urunler/uck-urunler.component.ts:607)
- U-213 [MEVCUT] Panel Güncel Kalan değerini frontend'de yeniden hesaplamaz; DTO'daki kalan alanını gösterir. Kaynak: Önyüz [uck-urunler.component.ts:1081](C:/Users/Watarzie/Desktop/3k_onyuz/src/app/features/uck/uck-urunler/uck-urunler.component.ts:1081)
- U-214 [MEVCUT] Kaydetme sonrası önyüz UCK_UPDATED bildirimi yayıp listeyi API'den tekrar yükler. Kaynak: Önyüz [uck-urunler.component.ts:1276](C:/Users/Watarzie/Desktop/3k_onyuz/src/app/features/uck/uck-urunler/uck-urunler.component.ts:1276); [uck.service.ts:30](C:/Users/Watarzie/Desktop/3k_onyuz/src/app/core/services/uck.service.ts:30)
- U-215 [MEVCUT] Satır anahtarı child varsa SandikIcerikId, yoksa CekiSatiriId ile kurulur; çoklu sandık seçimleri ayrışır. Kaynak: Önyüz [uck-urunler.component.ts:338](C:/Users/Watarzie/Desktop/3k_onyuz/src/app/features/uck/uck-urunler/uck-urunler.component.ts:338)
- U-216 [MEVCUT] Toplu istek hem parent ID listesini hem child seçim listesini gönderir. Kaynak: Önyüz [uck-urunler.component.ts:1393](C:/Users/Watarzie/Desktop/3k_onyuz/src/app/features/uck/uck-urunler/uck-urunler.component.ts:1393)
- U-217 [MEVCUT] Saha/yedek manuel SandikIcerik satırları sorguda tamamlanmış gösterilir ve 3K işlemine kapalıdır. Kaynak: [GetUcKUrunlerQueryHandler.cs:325](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Queries/GetUcKUrunlerQueryHandler.cs:325); Önyüz [uck-urunler.component.ts:226](C:/Users/Watarzie/Desktop/3k_onyuz/src/app/features/uck/uck-urunler/uck-urunler.component.ts:226)
- U-218 [MEVCUT] Ürün ekranı proje/sandık listesini tek çağrıda alır ve görünen filtreyi istemcide uygular; ürün pagination sözleşmesi yoktur. Kaynak: Önyüz [uck.service.ts:35](C:/Users/Watarzie/Desktop/3k_onyuz/src/app/core/services/uck.service.ts:35); [uck-urunler.component.ts:318](C:/Users/Watarzie/Desktop/3k_onyuz/src/app/features/uck/uck-urunler/uck-urunler.component.ts:318)
- U-219 [MEVCUT] Güncel 3K ürün ekranının aktif işlem çağrıları api/UcK servislerini kullanır; bu ekran eski Sandik teslim endpointlerini çağırmaz. Sandik servisinde legacy teslimAl/topluTeslimAl metotlarının hâlâ tanımlı olması bu ekranın sözleşmesini değiştirmez. Kaynak: Önyüz [uck-urunler.component.ts:1276](C:/Users/Watarzie/Desktop/3k_onyuz/src/app/features/uck/uck-urunler/uck-urunler.component.ts:1276); [uck.service.ts:66](C:/Users/Watarzie/Desktop/3k_onyuz/src/app/core/services/uck.service.ts:66); [sandik.service.ts:50](C:/Users/Watarzie/Desktop/3k_onyuz/src/app/core/services/sandik.service.ts:50)

### 8.22. Eski UcKTeslimAl endpointleri

- U-220 [MEVCUT] Eski tekil PUT api/Sandik/teslim-al ve toplu POST api/Sandik/toplu-teslim-al endpointleri hâlâ controller'da açıktır. Kaynak: [SandikController.cs:108](C:/Users/Watarzie/source/repos/3K_Proje/3K_API/Controllers/SandikController.cs:108)
- U-221 [MEVCUT] Eski tekil endpoint de saha, sevk kilidi, İptal/Kapandı, Tadilatta ve aktif-parti üst sınırını uygular. Kaynak: [UcKTeslimAlCommandHandler.cs:48](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/SandikIslemleri/Commands/UcKTeslimAlCommandHandler.cs:48)
- U-222 [MEVCUT] Eski tekil endpoint istenen miktar üst sınırı aşarsa reddeder; sessiz kırpma yapmaz. Kaynak: [UcKTeslimAlCommandHandler.cs:75](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/SandikIslemleri/Commands/UcKTeslimAlCommandHandler.cs:75)
- U-223 [UYUMSUZLUK] Eski tekil endpoint UcK Tam/Eksik etiketini yalnız kümülatif GelenMiktar >= IstenenAdet karşılaştırmasıyla seçer; alternatif kaynakları hesaba katmaz. Kaynak: [UcKTeslimAlCommandHandler.cs:98](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/SandikIslemleri/Commands/UcKTeslimAlCommandHandler.cs:98)
- U-224 [MEVCUT] Eski toplu endpoint istenen miktarı aktif-parti üst sınırına Math.Min ile kırpar. Kaynak: [UcKTopluTeslimAlCommandHandler.cs:105](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/SandikIslemleri/Commands/UcKTopluTeslimAlCommandHandler.cs:105)
- U-225 [UYUMSUZLUK] Böylece eski toplu endpoint aşan miktarı reddetmek yerine izin verilen kadarını işler. Kaynak: [UcKTopluTeslimAlCommandHandler.cs:108](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/SandikIslemleri/Commands/UcKTopluTeslimAlCommandHandler.cs:108)
- U-226 [UYUMSUZLUK] Eski toplu endpoint de Tam/Eksik etiketini yalnız GelenMiktar ile ana istek karşılaştırmasından üretir. Kaynak: [UcKTopluTeslimAlCommandHandler.cs:128](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/SandikIslemleri/Commands/UcKTopluTeslimAlCommandHandler.cs:128)
- U-227 [MEVCUT] Her iki eski endpoint aktif-parti sayacını ve SandikIcerik senkronizasyonunu kullanır; çift saymayı merkezi helper engeller. Kaynak: [UcKTeslimAlCommandHandler.cs:75](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/SandikIslemleri/Commands/UcKTeslimAlCommandHandler.cs:75); [UcKTopluTeslimAlCommandHandler.cs:108](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/SandikIslemleri/Commands/UcKTopluTeslimAlCommandHandler.cs:108)

### 8.23. Test kapsamı ve görünür boşluklar

- U-228 [TEST] İkinci Grid partisi ile tekil/toplu 3K kombinasyonları ve çift saymama regresyon testleri vardır. Kaynak: [GridUcKParcaliSevkPartisiRegresyonTests.cs:25](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/GridUcKParcaliSevkPartisiRegresyonTests.cs:25)
- U-229 [TEST] Legacy açık parti, Gelmedi materializasyonu ve kaynak sonrası davranış testleri vardır. Kaynak: [GridUcKParcaliSevkPartisiRegresyonTests.cs:180](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/GridUcKParcaliSevkPartisiRegresyonTests.cs:180)
- U-230 [TEST] Eksik/Gelmedi borcunun bir kez eklenmesi ve child geri alma testleri vardır. Kaynak: [GridUcKParcaliSevkPartisiRegresyonTests.cs:533](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/GridUcKParcaliSevkPartisiRegresyonTests.cs:533)
- U-231 [TEST] Geri gönderim; proje çıkışı, karışık kaynak, açık parti blokajı ve tekrarlı parçalı iade ile test edilir. Kaynak: [GridUcKParcaliSevkPartisiRegresyonTests.cs:668](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/GridUcKParcaliSevkPartisiRegresyonTests.cs:668)
- U-232 [TEST] Çoklu tahsis parent-child sayaç, seçili sandık ve seçimsiz dağıtım senaryoları test edilir. Kaynak: [GridUcKParcaliSevkPartisiRegresyonTests.cs:1388](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/GridUcKParcaliSevkPartisiRegresyonTests.cs:1388)
- U-233 [TEST] Eski tekil/toplu teslim endpointlerinin aktif-parti üst sınırı test edilir. Kaynak: [GridUcKParcaliSevkPartisiRegresyonTests.cs:1314](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/GridUcKParcaliSevkPartisiRegresyonTests.cs:1314)
- U-234 [TEST] Proje transfer telafi adaylığı, tek sandık şartı, miktar üst sınırı ve tekrar çalıştırmama testleri vardır. Kaynak: [UcKProjeTransferTelafiTeslimKuralTests.cs:11](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/UcKProjeTransferTelafiTeslimKuralTests.cs:11)
- U-235 [TEST] Kaynak sonrası tam/sandık bazlı reset ve sahte yeniden-sevk borcu bırakmama testleri vardır. Kaynak: [UcKSevkKaynakSonrasiSifirlamaTests.cs:18](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/UcKSevkKaynakSonrasiSifirlamaTests.cs:18)
- U-236 [TEST] Önyüz aktif parti gate'i, otomatik Tam miktarı, Eksik input'u, Fazla ve Geri sözleşmesi test edilir. Kaynak: Önyüz [uck-urunler-sevk-partisi.spec.ts:70](C:/Users/Watarzie/Desktop/3k_onyuz/src/app/features/uck/uck-urunler/uck-urunler-sevk-partisi.spec.ts:70)
- U-237 [TEST] Güncel ana miktar, orijinal miktar ve çoklu tahsis gösterim ayrımı önyüz testlerine sahiptir. Kaynak: Önyüz [uck-urunler-miktar.spec.ts:15](C:/Users/Watarzie/Desktop/3k_onyuz/src/app/features/uck/uck-urunler/uck-urunler-miktar.spec.ts:15)
- U-238 [DOĞRULANDI] Grid Tam + kısmi 3K teslim + pozitif kalan satırında alternatif kaynakların kapalı kalması `UcKKaynakTransferStokIadeKatalogTests.GridTam_OtuzBesHedeftenOtuzIkiTeslimde_AlternatifKaynakKapaliKalirVeYazmaYapilmaz` testiyle doğrudan doğrulanır. Grid'e gelen 35 adedin yalnız 32'sinin teslim edilmesi, kalan 3'ün farklı kaynaktan tekrar karşılanmasına izin vermez; miktar sonradan 32'den 35'e artırılıp Grid kabulü gerçekten eksik kalan senaryo farklıdır.
- U-239 [UYUMSUZLUK] Projeden karşılama endpointine UI filtrelerini aşan farklı ürün kaynağı gönderilmesini reddeden bir backend testi/kuralı bulunmamaktadır.
- U-240 [UYUMSUZLUK] Toplu Tam Geldi'nin kısmi başarı kaydedip hata Result döndürmesi, kullanıcı arayüzünde işlem başarısız sanılmasına yol açabilecek mevcut sözleşmedir.

### 8.24. Kabul edilen hedefler ve değişiklik sınırı

- U-241 [KABUL EDİLEN HEDEF] Aktif Grid partisi miktarı ile kümülatif 3K gelen miktarı ayrı tutulmaya devam etmelidir.
- U-242 [KABUL EDİLEN HEDEF] Yeni parti teslimleri yalnız o partinin karşılanan sayacını artırmalı ve kalanını azaltmalı; önceki partinin teslimi tekrar sayılmamalıdır.
- U-243 [KABUL EDİLEN HEDEF] Çoklu sandıkta parent ve child aktif sayaçları birlikte, tahsis paylarını aşmadan ilerlemelidir.
- U-244 [KABUL EDİLEN HEDEF] Legacy geçmiş kesin değilse otomatik tahmin yerine 409/veri mutabakatı davranışı korunmalıdır.
- U-245 [KABUL EDİLEN HEDEF] Grid Tam + 35 ihtiyaç + 32 sevk/teslim + 3 kalan örneği, tanımlı geri gönderim/borç/proje-transfer istisnası yoksa alternatif kaynağa açılmamalıdır.
- U-246 [KABUL EDİLEN HEDEF] Önyüz fiziksel teslim miktarını kendisi türetmemeli; backend aktif-parti kararını kullanmalıdır.
- U-247 [KABUL EDİLEN HEDEF] Orijinal çeki miktarı yalnız geçmiş/gösterim alanı olmalı, güncel tahsis ve teslim hesaplarına geri sokulmamalıdır.
- U-248 [KABUL EDİLEN HEDEF] Bu belgede [UYUMSUZLUK] olarak işaretlenen maddeler otomatik değişiklik talebi değildir; ayrı iş kararı ve regresyon testi olmadan üretim davranışı değiştirilmemelidir.

---

<a id="kaynak-tahsis"></a>

## 9. Kaynak, tahsis, taşıma ve çeki güncelleme kuralları

Bu bölüm mevcut çalışma ağacındaki backend kodunun ve belirtilen testlerin envanteridir; yeni bir iş kuralı önerisi değildir. Kaynak referansları depo köküne göre `dosya: satır` yerine doğrudan `dosya:satır` biçimindedir. Veritabanında işlem yapılmamıştır.
“Ana 3K yolu”, `api/UcK/durum-guncelle` komutunu ifade eder. Ayrı Sandık/Stok uçları aynı işlem adı kullanılsa da aynı kurallara sahip kabul edilmemelidir. Ekranın hangi eski ucu çağırdığı bu bölümün kapsamı dışında kalır.
Tamamlanma, liste görünürlüğü, genel yetki/onay ve sandık sevkiyat durumları ana dokümanın diğer bölümlerinde ele alınır. Burada bu konuların kaynak/tahsis işlemlerine sınır koyan tarafı belirtilmiştir.

### 9.A. Miktarların anlamı ve kaynak karşılama ön koşulları

- **K-001 — Ana talep, sandık tahsisi ve fiili teslim farklı verilerdir.** `IstenenAdet` güncel çeki toplamı; `TahsisMiktari` belirli sandığın ayrılmış payı; `KonulanAdet` sandıkta karşılanan fiziksel miktardır. Birini artırmak diğerinin gerçekleştiği anlamına gelmez.
  Ana kümülatif fiziksel toplam `GelenMiktar + StokKarsilanan + ProjeKarsilanan + TedarikciKarsilanan - ProjeGonderilen` olarak hesaplanır; kaynak kalemleri ayrıca tekrar toplanmamalıdır.
  Kaynak: [CekiSatiri.cs:193](C:/Users/Watarzie/source/repos/3K_Proje/3K.Core/Entities/CekiSatiri.cs:193); [UcKSandikIcerikSenkronizasyonHelper.cs:35](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKSandikIcerikSenkronizasyonHelper.cs:35).

- **K-002 — Ana 3K yolunda terminal Grid durumları kaynak karşılamasını da kapatır.** Grid `Iptal` veya `GridKapandi` iken tekli karşılama kabul edilmez; sevk kilitli sandık ve aktif saha kaynağı kontrolleri ayrıca uygulanır.
  `KaliteDurumu` lookup kaydının adı `Tadilatta` ise tekli 3K işlemi engellenir. Bu kontrolün toplu tedarikçi ucunda da bulunduğu varsayılmamalıdır; fark K-058'de belirtilmiştir.
  Kaynak: [UcKDurumGuncelleCommandHandler.cs:91](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:91); [UcKDurumGuncelleCommandHandler.cs:129](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:129).

- **K-003 — Kabul edilmiş alternatif kaynak kapısı korunur.** Proje/stok/tedarikçi karşılaması Grid `EksikGeldi`, `Gelmedi` veya `TrafoSevk` durumunda açılır; yahut kalan pozitifken iade tipi, iade miktarı veya yeniden sevk ihtiyacı bulunmalıdır.
  Yalnız `Grid=TamGeldi` ve sıradan pozitif kalan bulunması kaynak karşılamasını açmaz. Başka projeye verilen miktarın telafisi için, kalan pozitif ve `ProjeGonderilen>0` olduğunda yalnız `ProjedenKarsilandi` özel dalı vardır.
  Kaynak: [UcKDurumGuncelleCommandHandler.cs:145](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:145); [UcKTopluTedarikciCommandHandler.cs:166](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKTopluTedarikciCommandHandler.cs:166).

- **K-004 — Kaynak karşılaması hedef ana talebi ve fiziksel tahsis kapasitesini aşamaz.** Ana 3K yolunda yeni miktarla oluşacak `Gelen + Karsilanan - ProjeGonderilen + Trafo` toplamı `IstenenAdet` sınırını geçemez.
  Sandık kaydı varsa `Gelen + Stok + Proje + Tedarikci - ProjeGonderilen + yeniKaynak` toplamı ayrıca etkin tahsis toplamına karşı doğrulanır. Tahsis yetersizliğinde teslimi sessizce kırpmak veya tahsisi büyütmek yerine işlem reddedilir.
  Kaynak: [UcKDurumGuncelleCommandHandler.cs:290](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:290); test: [GridUcKParcaliSevkPartisiRegresyonTests.cs:63](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/GridUcKParcaliSevkPartisiRegresyonTests.cs:63).

- **K-005 — Kaynak karşılaması eski aktif sevkin izini kaybettirmemelidir.** Ana tekli kaynak işlemi ve toplu tedarikçi, son 3K tipini değiştirmeden önce güvenli legacy aktif sevk bilgisini somutlaştırır; belirsiz eski dağılımda reddeder.
  Yeni takip sayacı ile eski `null` değerleri aynı anlamda değildir. Kaynak karşılaması Grid teslimi olarak sayılmaz; kendi kaynak kırılımına eklenir ve mevcut yeniden sevk ihtiyacı karşılanan miktar kadar azaltılır.
  Kaynak: [UcKDurumGuncelleCommandHandler.cs:333](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:333); [UcKDurumGuncelleCommandHandler.cs:682](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:682).

### 9.B. Başka projeden karşılama ve transfer zinciri

- **K-006 — Ana 3K proje karşılamasında kaynak satır açıkça seçilir.** Miktar pozitif, kaynak proje metni dolu ve `KaynakCekiSatiriId` pozitif olmalıdır; satır ID üzerinden bulunur, bulunamazsa 404 döner.
  Aynı proje kontrolü hedef projenin gerçek `ProjeNo` değeri ile istek içindeki `KaynakHedefProjeNo` metnini karşılaştırır. Bu kontrolü kaynak satırın gerçek proje ID'siyle yapılan bir eşitlik kontrolü gibi yorumlamamak gerekir.
  Kaynak: [UcKDurumGuncelleCommandHandler.cs:225](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:225); [UcKDurumGuncelleCommandHandler.cs:699](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:699).

- **K-007 — Donör projenin kullanılabilir miktarı özel bir formüldür.** Ana 3K ve FB karşılamasında `max(GelenMiktar + ProjeKarsilanan - ProjeGonderilen, 0)` kullanılır; istek bundan büyük olamaz.
  Stoktan/tedarikçiden karşılananlar ve `TrafoSevkAdet` bu kaynak havuzuna dahil değildir. Liste DTO'sundaki `NetKullanilabilir=Konulan` alanıyla aynı kavrammış gibi kullanılmamalıdır.
  Kaynak: [UcKDurumGuncelleCommandHandler.cs:780](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:780); [FBDenKarsilaCommandHandler.cs:78](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/SandikIslemleri/Commands/FBDenKarsilaCommandHandler.cs:78); [GetUcKUrunlerQueryHandler.cs:301](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Queries/GetUcKUrunlerQueryHandler.cs:301).

- **K-008 — Aktif saha kaynağı normal projeden yeniden tüketilemez.** Ana 3K yolunda hem hedef normal kaynak satır hem donör kaynak satır için saha ilişkisi kontrol edilir; işlem ilişkili saha projesinde yürütülmelidir.
  `KaynakCekiSatiriId` taşıyan türetilmiş saha satırı bu “normal kaynak satır” blokajına girmez. Aktif ilişki yeni aktarım defterinden, eski kayıtlarda saha proje türü + kaynak satır bağlantısından bulunabilir.
  Kaynak: [SahaAktarimBlokajHelper.cs:12](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/SahaAktarimBlokajHelper.cs:12); [SahaTamamlamaService.cs:153](C:/Users/Watarzie/source/repos/3K_Proje/3K.Infrastructure/Services/SahaTamamlamaService.cs:153); [UcKDurumGuncelleCommandHandler.cs:710](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:710).

- **K-009 — Kaynak ve hedef farklı sayaçlarla izlenir.** Hedefte `KarsilananMiktar` ve `ProjeKarsilanan` artar; donörde `ProjeGonderilen` artar, durum ve kalan yeniden hesaplanır.
  Donörün eski `GelenMiktar` değeri silinmez: başka projeye gönderim ayrı düşüm olarak taşınır. Donörün `SandikIcerik` dağılımını bu helper doğrudan yeniden dağıtmaz; bunu eşzamanlı fiziksel sandık taşıma işlemi olarak yorumlamamak gerekir.
  Kaynak: [UcKDurumGuncelleCommandHandler.cs:445](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:445); [UcKDurumGuncelleCommandHandler.cs:719](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:719).

- **K-010 — Donörde telafi sevki ihtiyacı koşullu açılır.** Kaynak verdikten sonra donörde kalan pozitif, Grid sevk durumu `SevkEdildi` ve sevk miktarı pozitifse borç `max(eskiBorç, KalanMiktar)` olur ve sevk durumu `YenidenSevkGerekli` yapılır.
  Bu dal yeni bir fiziksel teslim yaratmaz; önceki sevk miktarını da sıfırlamaz. Sevk edilmemiş donörün durumu bu helper tarafından zorla yeniden sevke çevrilmez.
  Kaynak: [UcKDurumGuncelleCommandHandler.cs:789](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:789); [FBDenKarsilaCommandHandler.cs:161](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/SandikIslemleri/Commands/FBDenKarsilaCommandHandler.cs:161).

- **K-011 — Ana 3K yolu transfer defteri oluşturur.** Kaynak/hedef projeler satırların çekileri üzerinden çözülür; `ProjeTransfer` kaydı iki proje, iki satır, miktar, kullanıcı, tarih ve aktif durum içerir.
  Hedefin önceki aktif dışarı verme kaydı varsa zincir seviyesi/tarihe göre ilk kayıt ebeveyn seçilir; yeni kayıt `Telafi`, aksi halde `Karsilama` olur. `ParentTransferId`, `RootTransferId`, `ZincirSeviyesi` ilişkiyi taşır.
  Kaynak: [UcKDurumGuncelleCommandHandler.cs:735](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:735); [UcKDurumGuncelleCommandHandler.cs:748](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:748).

- **K-012 — Tek sandıklı proje-transfer telafisinde özel teslim hesabı vardır.** Daha önce başka projeye vermiş, yeniden sevki yapılmış, iki 3K alanı Bekliyor ve teslim tarihi boş satırda ham kalan pozitifse telafi adayı oluşur; uygulama yalnız bir tahsiste etkindir.
  Yeni sevk miktarı ham kalanı aşmıyorsa yeni sevkin tamamı karşılanır; eski fiziksel sandık tutarı bir kez daha sevkten düşülmez. Çoklu tahsis için bu tek-sandık kestirmesi uygulanmaz.
  Kaynak: [UcKProjeTransferTelafiTeslimKural.cs:13](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKProjeTransferTelafiTeslimKural.cs:13); test: [UcKProjeTransferTelafiTeslimKuralTests.cs:11](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/UcKProjeTransferTelafiTeslimKuralTests.cs:11).

### 9.C. Stok, tedarikçi ve fazla teslim

- **K-013 — Ana 3K stok karşılaması ürün adına göre eşleşir.** Pozitif miktar ve stok ID gerekir; stok bulunmalı, `MalzemeAdi` ile satır `Aciklama` normalize edilerek eşit olmalıdır.
  Normalizasyon harf/rakam/boşluk dışını temizler, boşlukları birleştirir ve Türkçe küçük harfe çevirir. Bu handler'da ayrıca barkod veya birim eşitliği aranmadığından “barkodu eşleşen stok şartı” şeklinde yazılmamalıdır.
  Kaynak: [UcKDurumGuncelleCommandHandler.cs:244](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:244).

- **K-014 — Stok miktarı tüketilir ve tüketim deftere yazılır.** `StokKaydi.Miktar` istek kadar azalır; sıfırsa durum `Tukendi` olur. Hedefte `StokKarsilanan` ile `KarsilananMiktar` artar.
  `StokHareketi` stok, çeki satırı, proje, kullanıcı, miktar, işlem tipi ve tarihi tutar. Ana 3K yolunda tüketim miktarı pozitif yazılır; ayrı Sandık yolunda negatif olması nedeniyle ters işlem mutlak değeri kullanır.
  Kaynak: [UcKDurumGuncelleCommandHandler.cs:457](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:457); [StoktanKarsilaCommandHandler.cs:78](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/SandikIslemleri/Commands/StoktanKarsilaCommandHandler.cs:78).

- **K-015 — Uygun stok listesi ve işlem doğrulaması aynı katman değildir.** `GetUygunStoklarAsync` yalnız Aktif ve miktarı pozitif stokları, kod/ad aramasıyla listeler.
  Ana 3K kaydetme yolu stok ID üzerinden varlık, ad eşleşmesi ve yeterli miktarı yeniden kontrol eder; bu handler'da stok durumu için ayrı Aktif kontrolü yoktur. Liste filtresi tek başına kayıt işlemi güvencesi sayılmaz.
  Kaynak: [StokService.cs:39](C:/Users/Watarzie/source/repos/3K_Proje/3K.Infrastructure/Services/StokService.cs:39); [UcKDurumGuncelleCommandHandler.cs:249](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:249).

- **K-016 — Tedarikçi karşılaması manuel miktar kaydıdır.** Tekli ana 3K yolunda miktar pozitif olmalıdır; kaynak kapısı ve hedef toplam/tahsis sınırları geçerli kalır.
  `TedarikciKarsilanan` ve `KarsilananMiktar` artar. Bu komutta tedarikçi kartı/ID/adı eşleştirilmez ve stok bakiyesi düşülmez; işlem “hangi tedarikçi kartından tüketildi” defteri olarak değerlendirilmemelidir.
  Kaynak: [UcKDurumGuncelleCommandHandler.cs:270](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:270); [UcKDurumGuncelleCommandHandler.cs:486](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:486).

- **K-017 — Toplu tedarikçi ayrı ayrı kalanları kapatır.** Seçili içerik yoksa ana kalan, içerik varsa `max(etkinTahsis - KonulanAdet, 0)` kullanılır; kalan sıfırsa satır atlanır.
  Çocuk ID'si satıra ait olmalıdır. Seçimler `(CekiSatiriId, SandikIcerikId)` çiftine göre tekilleştirilir; içerikli seçim listesi doluysa yalın satır ID listesi yerine o liste kullanılır.
  Kaynak: [UcKTopluTedarikciCommandHandler.cs:60](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKTopluTedarikciCommandHandler.cs:60); [UcKSandikSecimDto.cs:11](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKSandikSecimDto.cs:11).

- **K-018 — Toplu tedarikçide uygunsuz satırlar atlanabilir.** Bulunamayan, kilitli, normal-saha kaynağı, terminal Grid durumlu veya kaynak kapısı kapalı satırlar hata listesine eklenir; uygun satırlar işlenir.
  Sonunda Save yapılır ve hata varsa “N ürün güncellendi, M hata” sonucu dönebilir. Bunu “bir hata varsa bütün toplu tedarikçi geri alınır” garantisi olarak belgelememek gerekir; toplu resetin davranışı farklıdır.
  Kaynak: [UcKTopluTedarikciCommandHandler.cs:46](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKTopluTedarikciCommandHandler.cs:46); [UcKTopluTedarikciCommandHandler.cs:143](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKTopluTedarikciCommandHandler.cs:143).

- **K-019 — Fazla teslim normal talep ve stok fazlasını ayırır.** Pozitif fazla adet ve `StogaAktar=true` gerekir; güvenli, açık fiziksel Grid sevki bulunmalıdır.
  Önce normal ihtiyaç/aktif sevk sınırında teslim karşılanır, istek miktarındaki fazla ayrı stok kaydı olur. Fazla miktar talep satırının normal `GelenMiktar` değerine sınırsız eklenmez.
  Kaynak: [UcKDurumGuncelleCommandHandler.cs:210](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:210); [UcKDurumGuncelleCommandHandler.cs:274](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:274); [UcKDurumGuncelleCommandHandler.cs:495](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:495).

- **K-020 — Fazla teslim stoğunun menşei korunur.** Yeni stok kaydına barkod, ürün adı, birim, kaynak proje, `Fazla teslim` giriş nedeni ve aktif durum yazılır.
  `FazlaTeslimStogaAktarildi` hareketi stokla kaynak çeki satırını ilişkilendirir. Aynı isimli mevcut stoğa sessizce birleştirmek bu helper'ın davranışı değildir.
  Kaynak: [UcKDurumGuncelleCommandHandler.cs:607](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:607).

- **K-021 — Kaynaktan fiziksel karşılama depo konumunu yalnız belirsizse doldurur.** Tam/eksik teslim, proje/stok/tedarikçi ve fazla teslim sonucunda ilgili sandıkların Belirsiz lokasyonu 3K olur.
  Kullanıcı tarafından belirlenmiş başka bir lokasyon burada ezilmez. Gelmedi/iade işlemleri bu pozitif fiziksel konum tetikleyicileri arasında sayılmaz.
  Kaynak: [UcKDurumGuncelleCommandHandler.cs:647](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:647); [UcKDurumGuncelleCommandHandler.cs:669](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:669).

### 9.D. İade ve kaynak işlemlerini geri alma

- **K-022 — Grid'e iade edilebilir miktar tüm kaynakların toplamı değildir.** Ana düzeyde sınır `max(GelenMiktar - ProjeGonderilen, 0)`; seçili içerikte ek sınır `max(Konulan - Stok - Proje - Tedarikci, 0)` olur.
  Stok/proje/tedarikçi karşılamaları Grid'e iade edilecek fiziksel Grid payına dahil edilmez. Çoklu tahsiste hangi sandıktan iade edildiği belirtilmeden iade kabul edilmez.
  Kaynak: [GridUcKSevkPartisiKurali.cs:292](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:292); [GridUcKSevkPartisiKurali.cs:305](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:305); [UcKDurumGuncelleCommandHandler.cs:64](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:64).

- **K-023 — İade için neden, miktar ve güvenli sevk durumu gerekir.** Geçerli iade nedeni, pozitif miktar ve hesaplanan iade üst sınırı kontrol edilir; tamamlanmamış açık aktif sevk varken iade dalı açılmaz.
  İadede gerçek Grid gelen ve ilgili aktif karşılanan sayaç düşer, iade ve yeniden sevk miktarı artar. Stok/proje/tedarikçi sayaçları iade adı altında azaltılmaz.
  Kaynak: [UcKDurumGuncelleCommandHandler.cs:203](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:203); [UcKDurumGuncelleCommandHandler.cs:280](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:280); [UcKDurumGuncelleCommandHandler.cs:524](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:524).

- **K-024 — Başka projeye verilmiş malzeme donörde önce sıfırlanamaz.** Aktif dışarı giden `ProjeTransfer` kaydı bulunan satırda 3K reset, hedefteki karşılamanın önce geri alınmasını ister.
  Bu sıra donörde kullanılabilir stoğu geri yaratıp aynı anda hedefteki teslimi geçerli bırakmayı önler. Tekli ve toplu sıfırlama aynı dışarı-giden transfer koşulunu kontrol eder.
  Kaynak: [UcKDurumSifirlaCommandHandler.cs:90](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumSifirlaCommandHandler.cs:90); [UcKTopluSifirlaCommandHandler.cs:139](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKTopluSifirlaCommandHandler.cs:139).

- **K-025 — Hedefte tam 3K reset gelen proje transferlerini tersler.** Donör `ProjeGonderilen` değeri transfer miktarı kadar, sıfır altına inmeyecek şekilde azaltılır; donör durumu/kalanı yeniden hesaplanır.
  Defter silinmez; transfer `GeriAlindi` olur, iptal tarihi ve açıklaması yazılır. Seçili tek sandık reseti bu transfer tersleme dalını çalıştırmaz; proje kaynağı içeren seçili parçalar zaten engellenir.
  Kaynak: [UcKDurumSifirlaCommandHandler.cs:198](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumSifirlaCommandHandler.cs:198); [UcKTopluSifirlaCommandHandler.cs:247](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKTopluSifirlaCommandHandler.cs:247).

- **K-026 — Tam reset stok defterini de geri alır.** Stoktan karşılamaların `abs(Miktar)` toplamı stoğa geri eklenir; fazla teslimden yaratılan miktar ilgili stoktan düşülür ve stok durumu pozitif/sıfır bakiyeye göre güncellenir.
  Geri alınan satıra ait hareket kayıtları kaldırılır. Helper önce bütün mevcut stok gruplarını doğrular, sonra bakiyeleri değiştirir; bir gruptaki engel yüzünden önceki grup yarım değişmez.
  Kaynak: [UcKStokHareketGeriAlHelper.cs:35](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKStokHareketGeriAlHelper.cs:35); [UcKStokHareketGeriAlHelper.cs:80](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKStokHareketGeriAlHelper.cs:80).

- **K-027 — Fazla teslimden oluşan stok tüketilmişse kök işlem hemen geri alınamaz.** Aynı stokta geri alınan hareket kümesi dışında başka hareket bulunması veya stok miktarının oluşturulan miktardan az olması geri almayı engeller.
  Önce tüketen işlemlerin terslenmesi gerekir. Veri bozukluğu notu: ilgili stok kartı hiç bulunamazsa helper o grubu atlar; “eksik stok kartı daima hata verir” garantisi kodda yoktur.
  Kaynak: [UcKStokHareketGeriAlHelper.cs:43](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKStokHareketGeriAlHelper.cs:43).

- **K-028 — Seçili sandık reseti ana satırın tamamını sıfırlamaz.** Seçili içerikte stok/proje karşılaması varsa reddedilir; uygunsa seçili Grid fiziksel payı ile tedarikçi payı ana toplamdan çıkarılır ve diğer sandıkların payları korunur.
  Aktif sevk erken sonuçlandırma bayrağı son kaynak tipi değişmiş olsa da esas alınır; geri açılan mevcut sevkin hesaplanan açığı borçtan sıfır altına inmeyecek şekilde düşülür. `false` bayrağında sırf türetilmiş `EksikGeldi` etiketiyle eski borç silinmez; toplam borç kökenlere ayrılmış bir defter olmadığından karmaşık eski-borç/kaynak mahsup senaryoları ayrıca doğrulanmalıdır.
  Kaynak: [UcKDurumSifirlaCommandHandler.cs:62](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumSifirlaCommandHandler.cs:62); [UcKDurumSifirlaCommandHandler.cs:143](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumSifirlaCommandHandler.cs:143); test: [UcKSevkKaynakSonrasiSifirlamaTests.cs:18](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/UcKSevkKaynakSonrasiSifirlamaTests.cs:18).

- **K-029 — Resetin kapsamı ve toplu sonucu önemlidir.** Tam reset 3K gelen/kaynak/iade/yeniden-sevk değerlerini temizler, aktif Grid karşılamasını sıfırlayıp Grid sevk durumunu borç/sevk miktarından senkronize eder; çeki talebi/tahsis bu işlemle yeniden yazılmaz.
  Toplu resette bazı geçersiz satırlar atlanabilir; en az bir başarı varsa sonuç Success olabilir. Senkronizasyon gibi işlem düzeyi failure, özel exception ile transaction rollback'e çevrilir; bu davranış toplu tedarikçiyle aynı değildir.
  Kaynak: [UcKDurumSifirlaCommandHandler.cs:123](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumSifirlaCommandHandler.cs:123); [UcKTopluSifirlaCommandHandler.cs:33](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKTopluSifirlaCommandHandler.cs:33); [UcKTopluSifirlaCommandHandler.cs:312](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKTopluSifirlaCommandHandler.cs:312).

### 9.E. Sandık tahsisi ve fiziksel taşıma

- **K-030 — Legacy tahsis fallback'i tek ve çok sandıkta farklıdır.** Pozitif `TahsisMiktari` korunur. Tahsis yoksa tek içerikte ana istenen miktar, çok içerikte yalnız mevcut fiziksel konulan miktar güvenli fallback olarak kullanılır.
  Her sandığa ana toplamın tamamını kopyalamak yoktur. Okuma helper'ı veritabanında tahsis düzeltmesi yapmaz; sandık taşıma komutunun manuel satır fallback'i `max(Miktar,Konulan)`dır.
  Kaynak: [SandikTahsisHelper.cs:14](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/SandikTahsisHelper.cs:14); [SandikUrunTasiCommandHandler.cs:508](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/SandikIslemleri/Commands/SandikUrunTasiCommandHandler.cs:508).

- **K-031 — 3K sandık senkronizasyonu fark kadar dağıtır.** Hedef fiziksel toplam ana satırın kaynaklı kümülatif netidir; tüm sandıkların etkin tahsisini aşarsa reddedilir.
  Önce seçili içerik, sonra ID sırası kullanılarak artış boş kapasiteye, azalış mevcut miktara uygulanır. Bütün çocuklara aynı ana toplamı atamak yerine mevcut dağılım korunarak gereken fark işlenir.
  Kaynak: [UcKSandikIcerikSenkronizasyonHelper.cs:13](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKSandikIcerikSenkronizasyonHelper.cs:13); [UcKSandikIcerikSenkronizasyonHelper.cs:139](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKSandikIcerikSenkronizasyonHelper.cs:139).

- **K-032 — Sandık kaynak kırılımları fiziksel toplamı aşacak şekilde dağıtılmaz.** Stok, proje, tedarikçi alanları ayrı eşitlenir; her kaynak için kapasite konulan miktardan diğer kaynak payları çıkarılarak bulunur.
  Sonrasında her içeriğin eksik miktarı `max(etkinTahsis-Konulan,0)` olur. Bu fiziksel eksik alanı, terminal durumları veya hatalı ürün uyarısını içeren ana iş kalanı ile özdeş değildir.
  Kaynak: [UcKSandikIcerikSenkronizasyonHelper.cs:55](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKSandikIcerikSenkronizasyonHelper.cs:55); [CekiSatiri.cs:205](C:/Users/Watarzie/source/repos/3K_Proje/3K.Core/Entities/CekiSatiri.cs:205).

- **K-033 — Miktarlı taşıma aynı proje içindeki farklı sandıklar içindir.** Kaynak/hedef sandıklar ve istekteki proje eşleşmelidir; bağlı çeki de aynı projeye ait olmalıdır.
  Pozitif, en çok 14 tam/4 ondalık basamaklı miktar ve boş olmayan işlem anahtarı gerekir; istenen taşıma kaynak etkin tahsisinden büyük olamaz. Bu işlem başka projeden karşılama yerine kullanılamaz.
  Kaynak: [SandikUrunTasiCommandHandler.cs:30](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/SandikIslemleri/Commands/SandikUrunTasiCommandHandler.cs:30); [SandikUrunTasiCommandHandler.cs:122](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/SandikIslemleri/Commands/SandikUrunTasiCommandHandler.cs:122); [SandikUrunTasiCommandHandler.cs:426](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/SandikIslemleri/Commands/SandikUrunTasiCommandHandler.cs:426).

- **K-034 — Miktarlı taşıma sevkiyat kaydı olan sandığı düzeltme kilidiyle aşmaz.** Kaynak veya hedef `Sevkedildi` ise `SevkiyatDuzeltmeAcikMi` açık olsa bile resmi sevkiyat geri alma istenir.
  Aktif saha aktarımı olan normal kaynak satır, negatif içerik miktarları ve tutarsız aktif sevk sayaçları ayrıca engeldir. Salt sayısal taşıma isteği tarihsel sevkiyat içeriğini değiştirme yetkisi vermez.
  Kaynak: [SandikUrunTasiCommandHandler.cs:87](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/SandikIslemleri/Commands/SandikUrunTasiCommandHandler.cs:87); [SandikUrunTasiCommandHandler.cs:133](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/SandikIslemleri/Commands/SandikUrunTasiCommandHandler.cs:133); [SandikUrunTasiCommandHandler.cs:181](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/SandikIslemleri/Commands/SandikUrunTasiCommandHandler.cs:181).

- **K-035 — Tahsis taşıma ile fiziksel taşıma aynı miktar olmak zorunda değildir.** Tahsis istek kadar kayar; fiziksel taşınan `min(istek, kaynakKonulan)`dır. Hiç gelmemiş tahsis de taşınabilir ama teslim yaratılmaz.
  Eksik pay tahsis oranıyla, kaynak kırılımları fiziksel taşıma oranıyla bölünür; dört ondalık aşağı yuvarlama ve tam taşıma bakiyesi korunur. Ana `GelenMiktar` veya diğer ana kaynak toplamları taşıma diye artırılmaz.
  Kaynak: [SandikUrunTasiCommandHandler.cs:193](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/SandikIslemleri/Commands/SandikUrunTasiCommandHandler.cs:193); [SandikUrunTasiCommandHandler.cs:461](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/SandikIslemleri/Commands/SandikUrunTasiCommandHandler.cs:461); [SandikUrunTasiCommandHandler.cs:591](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/SandikIslemleri/Commands/SandikUrunTasiCommandHandler.cs:591).

- **K-036 — Aktif Grid teslim payı fiziksel kaynakla birlikte taşınır.** Grid fiziksel payı konulandan kaynak karşılamalarını çıkararak hesaplanır; aktif karşılanan sayacın taşınan kısmı bu Grid payından türetilir.
  Ana/çocuk nullable sayaç sözleşmesi ve toplam eşitliği doğrulanır. Yeni çocuk takipli ana satıra bağlanıyorsa sayacı sıfırdan başlar; legacy ana satırda null korunur; taşımada ana aktif toplam değişmez.
  Kaynak: [SandikUrunTasiCommandHandler.cs:181](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/SandikIslemleri/Commands/SandikUrunTasiCommandHandler.cs:181); [SandikUrunTasiCommandHandler.cs:626](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/SandikIslemleri/Commands/SandikUrunTasiCommandHandler.cs:626); test: [GridUcKParcaliSevkPartisiRegresyonTests.cs:1468](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/GridUcKParcaliSevkPartisiRegresyonTests.cs:1468).

- **K-037 — Hedef içerik kimliği barkoddan ibaret değildir.** Aynı çeki satırının hedef sandıkta bir kaydı varsa miktarlar o kayda eklenir; birden fazla eşleşme veri belirsizliği olarak reddedilir.
  Çeki bağlantısı olmayan manuel içerik, barkod/ad aynı diye başka manuel kayıtla birleştirilmez; ayrı hedef kayıt açılır. Kaynak tahsis ve konulan sıfırsa içerik kaldırılır; rapor gölgesi `Miktar` iki tarafta tahsise eşitlenir.
  Kaynak: [SandikUrunTasiCommandHandler.cs:532](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/SandikIslemleri/Commands/SandikUrunTasiCommandHandler.cs:532); [SandikUrunTasiCommandHandler.cs:239](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/SandikIslemleri/Commands/SandikUrunTasiCommandHandler.cs:239).

- **K-038 — Taşıma tekrarında çift hareket oluşmamalıdır.** `IslemAnahtari` daha önce aynı proje/kaynak içerik/hedef sandık/miktarla kullanılmışsa işlem tekrar uygulanmadan başarılı döner; farklı yükle kullanılmışsa 409 olur.
  Tahsis/fiziksel paylar, transfer defteri ve hareket kaydı transaction kapsamındadır; eşzamanlılık/benzersiz anahtar çakışmaları ayrıca ele alınır. Bu idempotency sözleşmesi bütün 3K komutları için genellenmez.
  Kaynak: [SandikUrunTasiCommandHandler.cs:47](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/SandikIslemleri/Commands/SandikUrunTasiCommandHandler.cs:47); [SandikUrunTasiCommandHandler.cs:330](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/SandikIslemleri/Commands/SandikUrunTasiCommandHandler.cs:330); [SandikUrunTasiCommandHandler.cs:402](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/SandikIslemleri/Commands/SandikUrunTasiCommandHandler.cs:402).

- **K-039 — Taşıma sonrası konum ve saha izi tekilleştirilir.** Tek aktif sandık kalırsa `FiiliSandikNo` ona güncellenir; birden çok sandık varsa tek bir metinle dağılım uydurulmaz.
  Aktif saha aktarımının hedefi tek sandığa düşerse `SahaSandikId` güncellenir; parçalıysa null yapılarak geri alma gerçek içeriklerden çözülür. Kapanmış sandık değişince Hazırlanıyor'a, boş kaynak Bos'a alınır ve ilgili saha kaynakları senkronize edilir.
  Kaynak: [SandikUrunTasiCommandHandler.cs:253](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/SandikIslemleri/Commands/SandikUrunTasiCommandHandler.cs:253); [SandikUrunTasiCommandHandler.cs:670](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/SandikIslemleri/Commands/SandikUrunTasiCommandHandler.cs:670); [SandikUrunTasiCommandHandler.cs:727](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/SandikIslemleri/Commands/SandikUrunTasiCommandHandler.cs:727).

- **K-040 — Fiili sandığı bütünüyle değiştirme parçalı taşıma değildir.** Birden fazla tahsis varsa `FiiliSandikDegistir` reddeder ve miktarlı taşımaya yönlendirir; tek içerikte tahsis, konulan, eksik, kaynak kırılımları ve aktif sayaç korunur.
  Hedef sandık veri kontrollerinden sonra oluşturulur. Fark: bu eski komutta kaynak içerik hiç yoksa Konulan fallback'i ana istenendir; `SandikService.SandikDegistirAsync` aynı durumda sıfır kullanır. Bu iki yolu eşdeğer saymamak gerekir.
  Kaynak: [FiiliSandikDegistirCommandHandler.cs:58](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/SandikIslemleri/Commands/FiiliSandikDegistirCommandHandler.cs:58); [FiiliSandikDegistirCommandHandler.cs:103](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/SandikIslemleri/Commands/FiiliSandikDegistirCommandHandler.cs:103); [SandikService.cs:112](C:/Users/Watarzie/source/repos/3K_Proje/3K.Infrastructure/Services/SandikService.cs:112).

- **K-041 — Sandık Yönetimi aktif normal Grid/3K akışını doğrudan ezemez.** Sevk miktarı, takip sayacı veya 3K işlemi bulunan normal satırda yeni tahsis ekleme ve fiziksel miktar/Grid-3K durumunu doğrudan değiştirme reddedilir.
  Personel/açıklama gibi miktarı değiştirmeyen güncellemeler ayrı kalır. Saha/Yedek içerik güncellemesi farklı daldır: seçili tahsise uyar, diğer içerikleri koruyup toplam konulandan bağlı satırı senkronize eder; normal dalın yasağı ona genellenmez.
  Kaynak: [UrunGuncelleCommandHandler.cs:121](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/SandikIslemleri/Commands/UrunGuncelleCommandHandler.cs:121); [UrunGuncelleCommandHandler.cs:290](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/SandikIslemleri/Commands/UrunGuncelleCommandHandler.cs:290); test: [GridUcKParcaliSevkPartisiRegresyonTests.cs:817](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/GridUcKParcaliSevkPartisiRegresyonTests.cs:817).

### 9.F. Manuel çeki ana verisi, revizyon ve orijinal miktar

- **K-042 — İlk miktar değişikliğinde orijinal miktar tek kez tutulur.** Gerçek miktar farkında `OrijinalIstenenAdet ??= eskiIstenenAdet` uygulanır; ilk değişiklikteki başlangıç değeri sonraki düzenlemelerde ezilmez.
  Aynı miktarla kayıt bir snapshot yaratmaz. Miktar orijinale geri getirilse de “sonradan düzenlendi” izi silinmez. Reddedilen değişiklikte orijinal snapshot da kaydedilmez.
  Kaynak: [CekiSatiriAnaVeriGuncelleCommand.cs:169](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/CekiIslemleri/Commands/CekiSatiriAnaVeriGuncelleCommand.cs:169); test: [CekiSatiriAnaVeriTahsisTests.cs:17](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/CekiSatiriAnaVeriTahsisTests.cs:17).

- **K-043 — Orijinal miktar hesaplama kaynağı değil tarihsel tabandır.** Güncel operasyonun hedefi `IstenenAdet`tir; `OrijinalIstenenAdet` yalnız ilk değişiklik öncesini gösterir.
  Tahsis eski olduğu için eski tahsis değerini “orijinal çeki” diye üretmek yoktur. Normal yeni yükleme ve A revizyonuyla eklenen satırda snapshot null kalır; değişiklik oluşana kadar tarihsel miktar iddiası kurulmaz.
  Kaynak: [CekiSatiri.cs:13](C:/Users/Watarzie/source/repos/3K_Proje/3K.Core/Entities/CekiSatiri.cs:13); test: [GridUcKSatirMiktarSenkronizasyonTests.cs:17](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/GridUcKSatirMiktarSenkronizasyonTests.cs:17); [CekiOrijinalMiktarTests.cs:91](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/CekiOrijinalMiktarTests.cs:91).

- **K-044 — Manuel miktar değişikliği yalnız tek tam tahsisi otomatik takip ettirir.** Tek içerik ve tahsis eski ana miktara eşitse veya legacy sıfır/negatifse, gerçek miktar değişikliğinde tahsis yeni ana miktara alınır.
  Eksik `max(yeniTahsis-Konulan,0)` olur; kesirli miktarlar integer'a çevrilmez. Eski ana miktardan farklı pozitif tek tahsis bilinçli kısmi tahsis kabul edilir ve otomatik büyütülmez.
  Kaynak: [CekiSatiriAnaVeriGuncelleCommand.cs:107](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/CekiIslemleri/Commands/CekiSatiriAnaVeriGuncelleCommand.cs:107); [CekiSatiriAnaVeriGuncelleCommand.cs:181](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/CekiIslemleri/Commands/CekiSatiriAnaVeriGuncelleCommand.cs:181); test: [CekiSatiriAnaVeriTahsisTests.cs:248](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/CekiSatiriAnaVeriTahsisTests.cs:248).

- **K-044A — Manuel miktar değişikliği normal Grid kabulünün Tam/Eksik durumunu güncel tutar.** Gerçek miktar farkında, mevcut durum Tam Geldi veya Eksik Geldi, Grid gelen miktarı pozitif ve trafo miktarı sıfırsa durum gerçek Grid gelen ile yeni talep karşılaştırılarak belirlenir. Miktar artışı fiziksel Grid gelişini, 3K teslimini veya aktif parti sayaçlarını artırmaz/sıfırlamaz.
  Örnek: 2 adet sevk edilip 3K'da tamamen teslim alındıktan sonra çeki 3'e çıkarılırsa tahsis 3, teslim 2, kalan 1 ve Grid Eksik Geldi olur. Önceki parti tamamlandığından kalan 1 mevcut eksik tamamlama sevkiyle gönderilebilir; tekil/toplu 3K tesliminin sonunda toplam 3 olur. Eski parti henüz tamamlanmadıysa bu miktar düzenlemesi devam sevki kilidini kaldırmaz. Miktar gerçek gelen seviyesine düşürülürse Tam Geldi etiketi geri gelir.
  İptal/Grid Kapandı, trafo ve diğer durumlar; kalite/süreç, saha ve nihai sandık sevk kilitleri bu düzenlemeyle yeniden açılmaz. Çoklu/kısmi tahsis kapasiteleri ayrıca korunur. Aynı miktarla kaydetmek eski tutarsız kayıtlar için toplu veri onarma işlemi değildir.
  Kaynak: [CekiSatiriAnaVeriGuncelleCommand.cs](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/CekiIslemleri/Commands/CekiSatiriAnaVeriGuncelleCommand.cs); test: `CekiSatiriAnaVeriTahsisTests.TamamlanmisTeslimSonrasiArtis_GridKabulDurumuYeniIhtiyaciIzler`, `GridUcKParcaliSevkPartisiRegresyonTests.TamTeslimdenSonraCekiIkiUcDuzenlenir_KalanBirTekliVeTopluAkistaTamamlanir`.

- **K-045 — Çoklu/kısmi tahsis artışında sandık dağılımı tahmin edilmez.** Ana çeki miktarı artırılabilir ama mevcut çoklu tahsisler ve eksik paylar otomatik dağıtılmaz.
  Daha sonra yeni talebi karşılamak için fiziksel tahsis kapasitesi yetmiyorsa karşılama/sevk sınırı işlemi durdurur. Manuel kayıt aynı miktarla tekrar gönderilerek eski tahsisleri genel onarma davranışı yoktur.
  Kaynak: [CekiSatiriAnaVeriGuncelleCommand.cs:181](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/CekiIslemleri/Commands/CekiSatiriAnaVeriGuncelleCommand.cs:181); test: [CekiSatiriAnaVeriTahsisTests.cs:263](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/CekiSatiriAnaVeriTahsisTests.cs:263); [CekiSatiriAnaVeriTahsisTests.cs:312](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/CekiSatiriAnaVeriTahsisTests.cs:312).

- **K-046 — Manuel miktar azaltmanın fiziksel alt sınırları vardır.** Yeni miktar `max(GridGelen+Trafo, max(Gelen+Stok+Proje+Tedarikci-ProjeGonderilen,0))` değerinden küçük olamaz; tüm içeriklerin konulan toplamı ayrıca kontrol edilir.
  Tam tek-tahsis dışındaki azaltma etkin tahsis toplamının altına da inemez. “Ana miktarı azaltıp fazlayı görünmez yapmak” yerine reddedilir; uygun tek tahsis konulan miktara kadar azaltılabilir.
  Kaynak: [CekiSatiriAnaVeriGuncelleCommand.cs:99](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/CekiIslemleri/Commands/CekiSatiriAnaVeriGuncelleCommand.cs:99); [CekiSatiriAnaVeriGuncelleCommand.cs:114](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/CekiIslemleri/Commands/CekiSatiriAnaVeriGuncelleCommand.cs:114); [CekiSatiriAnaVeriGuncelleCommand.cs:203](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/CekiIslemleri/Commands/CekiSatiriAnaVeriGuncelleCommand.cs:203).

- **K-047 — Manuel ana veri düzeltmesi teslim geçmişini sıfırlamaz.** Talep/tahsis güncellenirken gerçekleşmiş Grid sevk, 3K teslim, kaynak karşılama ve konulan değerleri korunur; durum/kalan güncel talep üzerinden yeniden hesaplanır.
  Miktar, birim ve planlanan sandık birlikte değişiyorsa aynı transaction kapsamındadır. Aktif saha kaynağı veya sevk kilitli kaynak/hedef sandık bütün bu değişikliği engeller; yalnız miktar artırılıyor olması istisna değildir.
  Kaynak: [CekiSatiriAnaVeriGuncelleCommand.cs:58](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/CekiIslemleri/Commands/CekiSatiriAnaVeriGuncelleCommand.cs:58); test: [CekiSatiriAnaVeriTahsisTests.cs:166](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/CekiSatiriAnaVeriTahsisTests.cs:166); [CekiSatiriAnaVeriTahsisTests.cs:362](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/CekiSatiriAnaVeriTahsisTests.cs:362).

- **K-048 — Manuel planlanan sandık değişikliği özel fiili konumu ezmez.** Fiili sandık boşsa veya eski planlanan sandıkla aynıysa yeni planı takip eder; daha önce kullanıcıca başka yere taşınmışsa korunur.
  Çoklu tahsis varken bu yoldan sandık no değiştirerek bütün parçaları tek sandığa toplamak reddedilir. Tek içerik taşınırken miktar/birim güncellemesiyle uyumlu kalır.
  Kaynak: [CekiSatiriAnaVeriGuncelleCommand.cs:128](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/CekiIslemleri/Commands/CekiSatiriAnaVeriGuncelleCommand.cs:128); test: [CekiSatiriAnaVeriTahsisTests.cs:341](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/CekiSatiriAnaVeriTahsisTests.cs:341); [CekiSatiriAnaVeriTahsisTests.cs:394](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/CekiSatiriAnaVeriTahsisTests.cs:394).

- **K-049 — Excel U revizyonu manuel ana veri düzeltmesiyle aynı yan etkiye sahip değildir.** İşlem görmüş U satırında mevcut akış önce Grid/3K/stok/proje işlemlerini otomatik geri alır, ardından yeni ana veriyi uygular.
  Önizlemede geri alma etkisi/uyarısı gösterilir. Dolayısıyla “revizyon miktarı değişti ama eski teslim her koşulda korunur” iddiası doğru değildir; koruma K-047'deki manuel güncelleme yoluna aittir.
  Kaynak: [CekiService.cs:704](C:/Users/Watarzie/source/repos/3K_Proje/3K.Infrastructure/Services/CekiService.cs:704); [CekiService.cs:1318](C:/Users/Watarzie/source/repos/3K_Proje/3K.Infrastructure/Services/CekiService.cs:1318); [CekiService.cs:1929](C:/Users/Watarzie/source/repos/3K_Proje/3K.Infrastructure/Services/CekiService.cs:1929).

- **K-050 — Revizyonun otomatik geri alması kaynak bağımlılıklarını gözetir.** Sevk kilitli sandıkta veya aktif dışarı giden proje transferinde otomatik geri alma yapılamaz; önce ilgili resmi karşı işlem gerekir.
  Gelen transferler donörde terslenip pasife alınır; stok hareketleri terslenir; Grid/3K miktarları, aktif sayaçlar, kaynaklar, kalite/süreç/personel izleri başlangıca çekilir ve ayrı hareket kaydı yazılır.
  Kaynak: [CekiService.cs:1977](C:/Users/Watarzie/source/repos/3K_Proje/3K.Infrastructure/Services/CekiService.cs:1977); [CekiService.cs:1988](C:/Users/Watarzie/source/repos/3K_Proje/3K.Infrastructure/Services/CekiService.cs:1988); [CekiService.cs:2042](C:/Users/Watarzie/source/repos/3K_Proje/3K.Infrastructure/Services/CekiService.cs:2042).

- **K-051 — U revizyonu tek tam tahsisi günceller, çoklu dağılımı uydurmaz.** Tek içerikte eski ana miktarı izleyen/legacy tahsis `max(yeniIstenen, Konulan)` yapılır; böylece helper tek başına çağrılsa da fiziksel alt sınır korunur.
  Kasıtlı farklı tahsis ve çoklu tahsisler değişmeden kalır. İçerik hiç yoksa yeni planlanan sandığa talep kadar tahsis oluşturulur. U revizyonundaki gerçek miktar farkı ilk orijinal snapshot'ı koruyarak yazar.
  Kaynak: [CekiService.cs:2154](C:/Users/Watarzie/source/repos/3K_Proje/3K.Infrastructure/Services/CekiService.cs:2154); [CekiService.cs:2193](C:/Users/Watarzie/source/repos/3K_Proje/3K.Infrastructure/Services/CekiService.cs:2193); test: [CekiOrijinalMiktarTests.cs:107](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/CekiOrijinalMiktarTests.cs:107).

- **K-052 — Aktif saha kaynağının U revizyonu önizleme ve uygulamada denetlenir.** Normal projenin kök kaynak satırı aktif saha ilişkisi taşıyorsa değişim engellenir; onay sonrası yeni ilişki oluşmasına karşı uygulama anında yeniden sorgulanır.
  Bütün U kaynakları herhangi bir geri alma/hedef sandık oluşturma yapılmadan kontrol edilir. Normal olmayan proje türleri veya kendisi türetilmiş satır U kontrolü açısından ayrı ele alınır; D silme korumasının daha geniş kapsamı daraltılmaz.
  Kaynak: [CekiService.cs:675](C:/Users/Watarzie/source/repos/3K_Proje/3K.Infrastructure/Services/CekiService.cs:675); [CekiService.cs:1560](C:/Users/Watarzie/source/repos/3K_Proje/3K.Infrastructure/Services/CekiService.cs:1560); test: [CekiRevizyonAktifSahaKaynakKorumaTests.cs:97](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/CekiRevizyonAktifSahaKaynakKorumaTests.cs:97).

- **K-053 — Revizyonda plan/fiili sandık ayrımı korunur.** Fiili sandık eski planı izliyorsa yeni plana güncellenir; özel fiili konum ezilmez. İçerik taşıma için plan değişikliği, fiili-plan eşliği ve taşınabilir içerik koşulları birlikte aranır.
  Taşınabilirlik sevk kilidinin olmaması ve konulan/eksik/kaynak paylarının sıfır olmasıdır. U otomatik geri alması bu alanları önce temizleyebildiği için önizleme uyarısı ile son hareket sırası birlikte okunmalıdır.
  Kaynak: [CekiService.cs:2165](C:/Users/Watarzie/source/repos/3K_Proje/3K.Infrastructure/Services/CekiService.cs:2165); [CekiService.cs:2218](C:/Users/Watarzie/source/repos/3K_Proje/3K.Infrastructure/Services/CekiService.cs:2218); [CekiService.cs:2671](C:/Users/Watarzie/source/repos/3K_Proje/3K.Infrastructure/Services/CekiService.cs:2671).

- **K-054 — Ekran toplamı ile kapasite düzeltmesi birbirine karıştırılmamalıdır.** Grid tek-tahsis görünümünde güncel ana talep ve operasyon toplamlarını eski tahsisle kırpmaz; çoklu sandıkta paylar korunur ve filtre gerçek tahsis sayısını değiştirmez.
  Okuma helper'ı ana kalanı dört ondalık hassasiyetle sandıklara dağıtır; fiziksel açıklık yokken hatalı ürün gibi iş uyarısı ilk pozitif tahsiste gösterilebilir. Görsel toplam düzeldi diye veritabanındaki tahsisin kendiliğinden onarıldığı varsayılmaz.
  Kaynak: [GetGridUrunlerQueryHandler.cs:204](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/GridIslemleri/Queries/GetGridUrunlerQueryHandler.cs:204); [SandikTahsisHelper.cs:51](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/SandikTahsisHelper.cs:51); [SandikTahsisHelper.cs:69](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/SandikTahsisHelper.cs:69).

### 9.G. Ayrı uçların mevcut farklılıkları ve sınırlar

- **K-055 — Eski FB karşılaması ana 3K komutuyla aynı değildir.** `api/Sandik/fbden-karsila`, kaynak projeyi `AlinanFB` ile bulur ve aynı barkodlu ilk satırı seçer; donör sevk kilidini ve normal-saha ilişkisini kontrol eder.
  Ana 3K `ProjedenKarsilandi` açık kaynak satır ID'si kullanır; kendi helper'ında donör sevk kilidi ve barkod/ad/birim eşitliği ayrı doğrulanmaz. Bu gerçek uç farkıdır; üst yetki/onay katmanlarından bağımsız “hepsinde aynı kontrol” yazılmamalıdır.
  Kaynak: [SandikController.cs:91](C:/Users/Watarzie/source/repos/3K_Proje/3K_API/Controllers/SandikController.cs:91); [FBDenKarsilaCommandHandler.cs:51](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/SandikIslemleri/Commands/FBDenKarsilaCommandHandler.cs:51); [UcKDurumGuncelleCommandHandler.cs:699](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:699).

- **K-056 — Eski FB ucunda hedef sayaç davranışı da farklıdır.** Hedef `KarsilananMiktar` artar ve ilk sandık içeriğinin konulan/eksik alanları değiştirilir; ana 3K yolundaki `ProjeKarsilanan` ve ortak çoklu-sandık senkronizasyonu burada yoktur.
  Kaynak proje bulunamazsa kaynak dalı atlanıp hedef güncellemesine ilerleyen bir kod yolu vardır. Bu durum önerilen iş kuralı değil, mevcut endpoint farklılığıdır; güncel UI kullanımının ve ek korumaların ayrıca doğrulanması gerekir.
  Kaynak: [FBDenKarsilaCommandHandler.cs:54](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/SandikIslemleri/Commands/FBDenKarsilaCommandHandler.cs:54); [FBDenKarsilaCommandHandler.cs:118](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/SandikIslemleri/Commands/FBDenKarsilaCommandHandler.cs:118).

- **K-057 — Eski stok uçlarının kapsamı farklıdır.** `api/Sandik/stoktan-karsila` stok yeterliliği/saha/sevk kilidi kontrolüyle ilk içeriği günceller; ana 3K'daki normalize ad eşleşmesi ve ortak tahsis senkronizasyonunu kullanmaz.
  `api/Stok/karsila` ayrıca ayrı bir handler'dır: önce stok servisini çağırır, seçilen ilk içeriğe `(int)Miktar` uygular; ana kaynak sayaçlarını aynı biçimde güncellemez. Pozitif miktar validator'ı bulunması bu iki yolu ana 3K ile eşdeğer yapmaz.
  Kaynak: [StoktanKarsilaCommandHandler.cs:28](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/SandikIslemleri/Commands/StoktanKarsilaCommandHandler.cs:28); [StokKarsilaCommandHandler.cs:23](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/StokIslemleri/Commands/StokKarsilaCommandHandler.cs:23); [StokValidators.cs:6](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/StokIslemleri/Validators/StokValidators.cs:6).

- **K-058 — Tekli ve toplu tedarikçi tamamen aynı doğrulama listesine sahip değildir.** Tekli yol Tadilatta kontrolü ve yeni toplam+Trafo ana talep sınırını açıkça yapar; toplu yol kendi kalan hesabı ve ortak sandık kapasite senkronizasyonunu kullanır.
  Toplu seçili sandık kalanı formülünde ana kalanla ayrıca `min` yoktur. Bu fark kayıt bazlı iş kuralı analizi gerektirir; doküman bunu eşdeğer güvence ya da bu çalışma kapsamında düzeltilmiş davranış olarak sunmaz.
  Kaynak: [UcKTopluTedarikciCommandHandler.cs:67](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKTopluTedarikciCommandHandler.cs:67); [UcKDurumGuncelleCommandHandler.cs:129](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:129); [UcKDurumGuncelleCommandHandler.cs:290](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKDurumGuncelleCommandHandler.cs:290).

- **K-059 — Transaction kapsamı, bütün Result.Failure sonuçlarının otomatik rollback'i demek değildir.** Genel UnitOfWork exception'da rollback yapar; normal dönen sonuçtan sonra commit eder ve `Result.IsSuccess` incelemez.
  Bu nedenle tekli ana 3K yolundaki kaynak/tahsis ön doğrulamaları ve stok tüketiminden önce yapılan kontroller önemlidir. Toplu reset başarısız sonucu özel exception'a çevirir; diğer uçlar için aynı davranış varsayılmamalıdır.
  Kaynak: [UnitOfWork.cs:86](C:/Users/Watarzie/source/repos/3K_Proje/3K.Infrastructure/Repositories/UnitOfWork.cs:86); [UcKTopluSifirlaCommandHandler.cs:33](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Features/UcKIslemleri/Commands/UcKTopluSifirlaCommandHandler.cs:33); test: [GridUcKParcaliSevkPartisiRegresyonTests.cs:63](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/GridUcKParcaliSevkPartisiRegresyonTests.cs:63).

- **K-060 — Legacy veriyi okumak ve düzeltmek ayrı işlemlerdir.** Fiziksel içerik yoksa `SandikService`, benzersiz proje+sandık numarası eşleşmesinden yalnız okuma için negatif ID'li sentetik içerik üretir; belirsiz eşleşmeyi üretmez.
  Stale tahsis, kayıp stok kartı, sayacı belirsiz eski sevk veya eski uçlardan gelen farklı kırılımlar için “görüntü mevcut, veri tutarlı” sonucu çıkarılamaz. Bu bölüm bunları DB üzerinde topluca onaran bir script veya veri doğrulaması değildir.
  Kaynak: [SandikService.cs:183](C:/Users/Watarzie/source/repos/3K_Proje/3K.Infrastructure/Services/SandikService.cs:183); [SandikService.cs:269](C:/Users/Watarzie/source/repos/3K_Proje/3K.Infrastructure/Services/SandikService.cs:269); [GridUcKSevkPartisiKurali.cs:650](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application/Common/GridUcKSevkPartisiKurali.cs:650).

### 9.H. Test dayanakları ve doğrulama sınırı

- Manuel miktar/orijinal snapshot, tek-tam/kısmi/çoklu tahsis, kesir, miktar azaltma ve saha/sevk blokajı: [CekiSatiriAnaVeriTahsisTests.cs:17](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/CekiSatiriAnaVeriTahsisTests.cs:17).
- Revizyon snapshot, yeni A satırı, tek tahsis 2→4 örneği, çoklu dağılım ve otomatik geri almanın sayaçları temizlemesi: [CekiOrijinalMiktarTests.cs:66](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/CekiOrijinalMiktarTests.cs:66).
- U revizyonunun aktif saha kaynağını önizlemede ve uygulama anında yeniden engellemesi: [CekiRevizyonAktifSahaKaynakKorumaTests.cs:16](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/CekiRevizyonAktifSahaKaynakKorumaTests.cs:16).
- Kaynak karşılaması tahsisi aşınca stok/ana satırın değişmemesi; legacy kaynak işlemleri; çoklu tahsis, miktarlı/fiili taşıma: [GridUcKParcaliSevkPartisiRegresyonTests.cs:63](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/GridUcKParcaliSevkPartisiRegresyonTests.cs:63), [GridUcKParcaliSevkPartisiRegresyonTests.cs:1468](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/GridUcKParcaliSevkPartisiRegresyonTests.cs:1468).
- Eksik teslim→tedarikçi→seçili reset→yeniden teslimde sahte borç kalmaması; önceki bağımsız borcun korunması: [UcKSevkKaynakSonrasiSifirlamaTests.cs:18](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/UcKSevkKaynakSonrasiSifirlamaTests.cs:18).
- Donörün telafi teslimi ile sıradan ilk/kümülatif teslimin ayrılması: [UcKProjeTransferTelafiTeslimKuralTests.cs:11](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/UcKProjeTransferTelafiTeslimKuralTests.cs:11).
- Eski tahsisten sahte orijinal miktar üretilmemesi ve filtre altında çoklu tahsis davranışı: [GridUcKSatirMiktarSenkronizasyonTests.cs:17](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/GridUcKSatirMiktarSenkronizasyonTests.cs:17).

Bu bölüm testlerin mevcut niyetini dayanak gösterir; burada yeni bir tüm-test çalıştırması veya canlı veri doğrulaması yapıldığı anlamına gelmez. K-055–K-059 farklılıkları açıkça işaretlenmiş mevcut uygulama sınırlarıdır; ana GridTam alternatif-kaynak kapısını değiştirme önerisi değildir.

---

<a id="ornekler"></a>

## 10. Kullanıcı senaryoları: sonuç nasıl okunmalı?

Bu örnekler diğer engellerin olmadığı varsayımıyla okunmalıdır. Onay, kalite, saha, sevk kilidi, yeterli kaynak ve doğru tahsis kontrolleri ayrıca geçerlidir.

| Senaryo | Beklenen yorum |
|---|---|
| İhtiyaç 35; Grid gelen 35; Grid sevk 32; 3K teslim 32 | Aktif sevk tamamdır, genel ihtiyaç 3 açıktır. Sevk tam ile ihtiyaç tam aynı değildir. |
| Aynı senaryoda kalan 3'ü projeden/tedarikçiden karşılamak | Yalnız kalan >0 olması yetmez. Mevcut kaynak uygunluk kuralı ayrıca geçmelidir. Grid Tam Geldi/3K Tam Geldi durumunda özel bir istisna yoksa kaynak karşılaması otomatik açılmaz. |
| İhtiyaç 3; henüz 3K işlemi yok; Grid sevki 2 girilmiş, 3 yapılacak | Güncel sevk miktarı 3 olarak üzerine yazılabilir; 2+3=5 diye eklenmez. Kullanıcı mutlaka 3K'nın ilk 2'yi almasını beklemek zorunda değildir. |
| 3K önceki sevki gerçekten teslim almış; sonra kalan sevk edilecek | Önceki teslim korunur; uygun devam sevki aktif sevk olarak açılır. İlk sevki körlemesine ezme ile devam sevki ayrı akıştır. |
| Güncel ihtiyaç 1'den 3'e çıkarılmış, tek uygun tahsis var | Tahsis 3'e eşitlenir; ilk miktar referansı 1 kalır. Sevk/teslim geçmişi sırf miktar düzenlendi diye 3'e çıkarılmaz. |
| Aynı ürün iki sandığa dağıtılmış, toplam ihtiyaç değişiyor | Sandık dağılımı keyfî biçimde yeniden paylaştırılmaz. Dağılım/taşıma kuralları ve güvenli kontrol sonucu belirleyicidir. |
| Ürün “iptal”, kalan 0 | İş talebi kapatılmıştır; fiziksel 3K teslimi/stok kendiliğinden artmış değildir. |
| Hatalı ürün var, sayısal ihtiyaç karşılanmış | Açık hata nedeniyle kalan 1 işareti görülebilir. Bu tek başına 1 yeni adet sevk kapasitesi anlamına gelmez. |
| Saha son eksik ürünü iptal ediyor, diğerleri tamam | Saha iş tamamlanmasına ve bağlantılı normal iş ilerlemesine yansıyabilir; yeni fiziksel sevkiyat sayılmaz. |
| Sevk kaydı korunsun ile kilit açılıyor | Sevkiyat geçmişi ve sevk durumu korunarak kapsamlı olmayan düzeltme açılır; iş bittiğinde Düzeltmeyi Tamamla ile yeniden kilitlenir. |
| Onay verildi, o sırada kaynak tüketildi veya kilit değişti | Talep onaylı olsa da çalıştırma mevcut iş kurallarında reddedilebilir. |
| Tamamlanma %99, proje Kısmi Sevk | Tek başına tutarsızlık kanıtı değildir; ürün işi, sandık sevki ve kayıtlı proje durumu birlikte incelenir. |
| Aynı barkod farklı miktarlarla birden fazla satırda | Bir satırın teslimi diğer satırı otomatik kapatmaz; satır ve sandık kimliği kontrol edilmelidir. |
| Sandik “Ürün İptal” alternatifi çağrılıyor | Eski/alternatif komut yalnız genel durumu İptal/Pasif yapıp açıklama yazar; GridDurumu'nu İptal yapmaz. Grid ekranındaki İptal ile aynı sıfırlama hesabı varsayılmaz. |

<a id="farklar"></a>

## 11. Farklı işlem yolları ve dikkat edilmesi gereken sınırlar

**F-01. Tekil ve toplu işlem aynı sözleşme değildir.** Satır seçme/atlama, onay, kalite, süreç ve hata davranışları komuta göre değişir. “Toplu” kelimesinden bütün endpointlerin ya hep ya hiç veya her zaman kısmi başarı döndürdüğü çıkarılamaz.

**F-02. Güncel 3K ve eski Sandık/Stok karşılama yolları aynı değildir.** U/K bölümlerinde ayrı yazılan teslim-al, FB'den karşıla, stoktan karşıla gibi komutların ekranda çağrılıp çağrılmadığı ile backend'de varlığı ayrıdır. Aynı kullanıcı ifadesiyle anılsalar bile veri dağıtımı ve korumaları farklı olabilir.

**F-03. Genel iptal ile Grid iptal farklıdır.** `SandikIslemleri/UrunIptalCommandHandler` genel `DurumId` ve `Remarks` alanlarını değiştirir. Grid iptal kalan formülünün kontrol ettiği `GridDurumuId` alanıdır. Bu fark belgede korunmuştur.

**F-04. Süreç tamamlandı ve ürünün ihtiyacı tamamlandı eş anlamlı değildir.** Özellikle Grid gelme miktarı tamamlanınca süreç tamamlanabilir; bu, bütün miktarın 3K'ya sevk/teslim edildiğini göstermez. Devam sevki istisnaları bu ayrımı gözetir.

**F-05. Eksik seçim API'leri bile farklı kapsam taşır.** `GetEksikUrunlerQuery` saha **iş tamamlama** etkisinden sonraki kalan >0 satırları döndürür. `GetEksikUrunlerByProjeQuery` yalnız normal kaynak projeden, bağlı tamamlama satırlarının **planlanan istenen miktarını** düşerek planlanabilir kalan >0 satırları döndürür. Eksik PDF ise ayrı rapor kapsamını kullanır.

**F-06. SQL'le durum düzeltme iş kuralı değildir.** Geçmiş anomalileri düzeltmek için hazırlanmış kayıt bazlı scriptler, normal operasyonun veya bu kataloğun otomatik adımları değildir. Doğrudan SQL değişikliği uygulama doğrulamaları, kaynak hareketleri ve saha eşitleme adımlarını atlayabilir.

**F-07. Yeni sayaçlar geçmiş tablosu değildir.** Aktif sevk karşılanan ana/alt sayaçları ve erken sonuçlandırma bayrağı mevcut hesaplamada kullanılır; ekranda parti tablosu gösterilmemesi bu alanları gereksiz yapmaz. Null değer eski kayıt uyumluluğunu temsil eder.

**F-08. Mevcut davranış ile onaylanmış iş politikası ayrıdır.** Özellikle Admin onay istisnası, farklı API'lerde onay kapsamı, eski iptal yolu ve farklı rapor/eksik haritaları bu incelemede tespit edilen uygulama gerçekleridir. Bu dokümanda bunlar değiştirilmemiş veya yeni politika olarak onaylanmış sayılmamıştır.

<a id="kaynaklar"></a>

## 12. Kaynak ve doğrulama envanteri

### 12.1 İncelenen akışlar

| Alan | İncelenen uygulama kaynakları |
|---|---|
| Grid | GridController; iş listesi/ürün sorguları; tekil durum/sevk; toplu sevk/durum; tekil/toplu sıfırlama; manuel ekleme; kalite ve süreç; validator ve önyüz |
| 3K | UcKController; iş listesi/ürün sorguları; bütün aktif karşılama tipleri; tekil/toplu tam geldi/tedarikçi; tekil/toplu sıfırlama; validator, önyüz ve alternatif teslim komutları |
| Miktar | CekiSatiri, SandikIcerik; kalan/durum hesaplayıcıları; GridUcKSevkPartisiKurali; miktar hassasiyeti |
| Kaynak | Projeden/FB'den, stoktan, tedarikçiden; stok hareket geri alma, proje transferi, geri gönderme ve kapasite |
| Tahsis/çeki | Ana veri güncelleme; orijinal miktar; Excel revizyonu; sandık değiştirme/taşıma; doğrudan içerik düzenleme |
| Ortak koruma | Menü yetkisi; dinamik onay; Admin istisnası; kalite/süreç; proje ve sandık sevk kilidi; onay yürütme |
| Saha bağlantısı | Aktif aktarım blokajı; iş tamamlama ve sevk haritaları; normal/saha/yedek proje sayaç/durumları; liste filtreleri |
| Son işlemler | Sandık kapatma/sevk/düzeltme; manuel ve çeki satırı silme; eksik ve gerçekleşen raporların ilgili hesapları |

### 12.2 Mevcut test kaynakları

Aşağıdaki test dosyaları kuralların bakımında kullanılabilecek mevcut regresyon kaynaklarıdır. **Bu belgeleme çalışmasında test paketi yeniden koşturulmadı; dosyaların varlığı bütün olası senaryoların kanıtı değildir.**

| Kural alanı | Test kaynakları |
|---|---|
| Miktar/tahsis/orijinal referans | [CekiSatiriAnaVeriTahsisTests.cs](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/CekiSatiriAnaVeriTahsisTests.cs), [CekiOrijinalMiktarTests.cs](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/CekiOrijinalMiktarTests.cs), [GridUcKSatirMiktarSenkronizasyonTests.cs](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/GridUcKSatirMiktarSenkronizasyonTests.cs) |
| Aktif sevk/devam/kapasite | [GridUcKParcaliSevkPartisiRegresyonTests.cs](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/GridUcKParcaliSevkPartisiRegresyonTests.cs), [GridUcKSevkPartisiYasamDongusuTests.cs](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/GridUcKSevkPartisiYasamDongusuTests.cs), [GridSevkTahsisKapasitesiTests.cs](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/GridSevkTahsisKapasitesiTests.cs) |
| Kaynak sonrası sıfırlama/telafi | [UcKSevkKaynakSonrasiSifirlamaTests.cs](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/UcKSevkKaynakSonrasiSifirlamaTests.cs), [UcKProjeTransferTelafiTeslimKuralTests.cs](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/UcKProjeTransferTelafiTeslimKuralTests.cs) |
| Grid iş listesi | [GridIsListesiTests.cs](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/GridIsListesiTests.cs) |
| Saha ilerleme ve güvenlik | [SahaIsTamamlamaServiceTests.cs](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/SahaIsTamamlamaServiceTests.cs), [SahaProjeTamamlanmaRegresyonTests.cs](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/SahaProjeTamamlanmaRegresyonTests.cs), [GridSahaIsTamamlamaSenkronizasyonTests.cs](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/GridSahaIsTamamlamaSenkronizasyonTests.cs), [SahaAkisiGuvenlikKurallariTests.cs](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/SahaAkisiGuvenlikKurallariTests.cs) |
| Saha kaynak revizyonu | [CekiRevizyonAktifSahaKaynakKorumaTests.cs](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/CekiRevizyonAktifSahaKaynakKorumaTests.cs), [SahaKaynakAnaVeriKorumaTests.cs](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/SahaKaynakAnaVeriKorumaTests.cs) |
| Fiziksel sevk ve düzeltme | [NormalProjeSevkDurumHelperTests.cs](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/NormalProjeSevkDurumHelperTests.cs), [NormalProjeSevkDurumHesaplayiciTests.cs](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/NormalProjeSevkDurumHesaplayiciTests.cs), [NormalProjeSevkKomutlariTests.cs](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/NormalProjeSevkKomutlariTests.cs), [SevkiyatKilidiAcRegresyonTests.cs](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/SevkiyatKilidiAcRegresyonTests.cs), [ProjectLockCorrectionScopeTests.cs](C:/Users/Watarzie/source/repos/3K_Proje/3K.Application.Tests/ProjectLockCorrectionScopeTests.cs) |

### 12.3 Belgenin güncel tutulması

Yeni bir değişiklikte yalnız ekran yazısı değil; ilgili G/U/K kuralı, ortak kalan formülü, tekil/toplu yol, saha etkisi, ters işlem, yetki/onay ve regresyon senaryosu birlikte kontrol edilmelidir. Böylece bir akış düzeltilirken diğer akışın sessizce değişmesi daha kolay fark edilir.

**İnceleme özeti:** Bu katalog mevcut uygulamayı tarif eder. Kod satırları ve tarihli DB ayarlarıyla desteklenmiştir; canlı veri üzerinde düzeltme yapılmamıştır.
