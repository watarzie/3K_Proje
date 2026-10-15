# Veritabanı betikleri

Şema ve referans veri değişiklikleri EF migration ile değil, bu klasördeki PostgreSQL betikleriyle yapılır.

- Dosya adı `YYYYMMDD_NN_Konu.sql` biçimindedir ve betikler ad sırasıyla uygulanır.
- Her betik tek bir transaction içindedir (`BEGIN … COMMIT`) ve tekrar çalıştırılabilir (idempotent) yazılır.
- Betik uygulama açılışında çalışmaz; dağıtımın ayrı bir adımıdır. Canlıya uygulamadan önce veritabanı yedeği alınır.
- EF model/configuration değişikliği ile ilgili betik aynı değişiklikte güncellenir.

Uygulama örneği:

```bash
psql -h <sunucu> -U <kullanici> -d 3K_Db -v ON_ERROR_STOP=1 -f scripts/database/20260928_01_Yetki_Etiketleri.sql
```

## Uygulama sırası

Canlıya almadan önce `kontrol/Betik_Durumu.sql` (salt okunur) çalıştırılır: her betiğin bu veritabanında uygulanıp
uygulanmadığını bıraktığı izden (kolon, tablo, menü düğümü) gösterir. `UYGULANMAMIŞ` satırlar aşağıdaki sırayla
uygulanır. Yeni betik eklenince bu sorguya da satır eklenir (`CanliyaAlimBetikleriTests` dosya listesiyle karşılaştırır).

Betiklerin hepsi ad sırasıyla, uygulamanın veritabanına (`3K_Db`) bağlıyken çalıştırılır. pgAdmin sorgu aracını ağaçtaki
`3K_Db` düğümünden açın: sunucu düğümünden açılan pencere varsayılan `postgres` veritabanına bağlanır (sekme başlığında
veritabanı adı görünür).

Tekrar çalıştırma: 9–18 numaralı betikler tekrar çalıştırıldığında (onay ayarı oturumda açık kalsa da) hiçbir şey
değiştirmez; izinleri yalnız düğümü ekledikleri çalıştırmada verirler, yöneticinin sonradan kaldırdığı veya düşürdüğü
izni geri vermezler (`CanliyaAlimBetikleriTests`). 19 (tahsis onarımı) tekrarında yalnız henüz onarılmamış satırlara bakar; onarılmış ve atlanan satırlar değişmez
(`SandikTahsissizSatirOnarimiBetigiTests`). 6 (sıfırlama) canlıda bir kez, bilinçli olarak çalıştırılır; 4
(eşleşme onarımı) ve 7 (ödeme izni) de yalnız bir kez çalıştırılır: 4, bilerek pasifleştirilmiş eşleşmeyi yeniden
aktifleştirebilir; 7, 15 numaralı betikten sonra çalıştırılırsa silinmiş `finans-odeme-kaydet` düğümünü geri ekler.

| Sıra | Betik | Ne yapar |
|---|---|---|
| 1 | `20260928_01_Yetki_Etiketleri.sql` | Finans/Ambalaj izin etiketlerini `MENU.*` çeviri anahtarlarına çevirir. |
| 2 | `20260928_02_Ambalaj_Kullanilmayan_Yetkiler.sql` | Kullanılmayan Ambalaj izin kodlarını siler. |
| 3 | `20260928_03_Finans_Kullanilmayan_Yetkiler.sql` | Kullanılmayan Finans izin kodlarını siler. |
| 4 | `20260928_04_Finans_Urun_Eslesmeleri.sql` | Pasif kaydedilmiş ürün eşleşmelerini onarır (sıfırlamadan sonra boş tabloda iş yapmaz). |
| 5 | `20260928_05_Indeks_Hizalama.sql` | İndeksleri EF modeliyle hizalar. |
| 6 | `20260929_01_Finans_Ambalaj_Sifirlama.sql` | **Bütün Finans ve Ambalaj tablolarını verileriyle siler** ve güncel modelle yeniden oluşturur (para birimleri TRY/EUR, vade parametreleri 30/7 gün). Onay olarak veritabanının adını ister. |
| 7 | `20260929_02_Finans_Odeme_Yetkisi.sql` | `finans-odeme-kaydet` iznini ekler (fatura izni olan rollere aynı seviyede verir), `finans-sablon-yonet` iznini siler. |
| 8 | `20261003_01_Kisisel_Yetkileri_Kaldir.sql` | **Kullanıcıya özel izinler tablosunu (`KullaniciYetkileri`) verileriyle siler**; yetki yalnız rolden gelir. Silmeden önce bütün kişisel kararları listeler. Onay olarak veritabanının adını ister. |
| 9 | `20261008_01_SandikIcerik_UrunBilgisi.sql` | `SandikIcerikleri` tablosuna kalıcı `UrunBilgisi` (seri no/SKT/lot, en fazla 1000 karakter) kolonunu ekler; mevcut satırlar boş kalır. |
| 10 | `20261008_02_Finans_Ahsap_Kontra_Hacim.sql` | `FinansIsKayitlari` tablosuna elle girilen `AhsapM3`, `KontraM3` ve `HacimManuel` kolonlarını ekler, miktar kısıtını yeni kolonlarla yeniden kurar; mevcut kayıtlar boş kalır (pay türetilir). |
| 11 | `20261008_03_Finans_Sekme_Yetkileri.sql` | Rol ağacına Finans sekme düğümlerini (`finans-sekme-*`, Id 5238–5245) ekler; her role sekme seviyesini mevcut izinlerinden verir, sonra yalnız o sekmeye ait işlem kodlarını sekme düğümünün altına taşır. Etkin izinler azalmaz. **Yeni API'den önce** çalıştırılır. |
| 12 | `20261008_04_Sevk_Seferleri.sql` | Tır seferi tablosu `SevkSeferleri` ve `Sevkiyatlar."SevkSeferiId"` (boş olabilir, restrict) ekler; mevcut sevkiyatlar sefere bağlanmaz (istatistikte plaka + gün ile gruplanır). |
| 13 | `20261008_05_Sevkiyatlar_Yetkisi.sql` | Projeler altına "Sevkiyatlar" sayfası iznini (`sevkiyatlar`, Id 5246, `/projeler/sevkiyatlar`) ekler; Projeler menüsünü gören ve sevk edilenleri gören veya sevk edebilen rollere R, Admin'e W verir. |
| 14 | `20261013_01_Finans_Malzemeli_Tarife.sql` | Malzemeli tarife: `FinansUrunEslesmeleri."Malzeme"` (1 Ahşap / 2 Kontra / boş) ve `FinansIsKayitlari` tablosuna ahşap tarafının `AhsapBirimFiyatSnapshot`, `AhsapFinansUrunId` (restrict) ve `AhsapTarifeIdSnapshot` kolonlarını ekler. Kontrplak/katlanır sandıkta ahşap m³ ahşap, kontra m³ kontra tarifesiyle fiyatlanır; katlanır sandık boyutla eşleşmez. Mevcut kayıtlar boş kalır. |
| 15 | `20261013_02_Finans_Sekme_Yetkisine_Gecis.sql` | **Finans alt izin kodlarını (finans-kayit-goruntule, finans-po-gir, finans-tutar-goruntule … ) rol izinleriyle siler**; Finans yetkisi yalnız 8 sekme düğümünde kalır (R görüntüleme, W bütün işlemler, alan maskesi yok). Silmeden önce her rolün sekme seviyesini alt kodlardaki seviyesine yükseltir (modül seviyesini aşmadan; değişenleri listeler) ve İş Takibi üstündeki vade uyarısı için `finans-vade-uyarisi` (Id 5247, R) iznini ekler. Onay olarak veritabanının adını ister (`SET uc_k.finans_sekme_yetkisi = '3K_Db'`); onaysız çalıştırılınca yalnız listeyi yazar. Sekme ve alt sekme düğümleri (`finans-sekme-*`) silinmez. **Yeni API ile birlikte** uygulanır. |
| 16 | `20261013_03_Finans_Alt_Sekme_Yetkileri.sql` | Finans alt sekme düğümlerini ekler (Id 5248–5255): İş Takibi → Aylık / Tüm dönemlerde ara / Bekleyen işler; İş ve Tarife Tanımları → Düzenli İşler / Ürün ve Tarifeler / Para Birimi ve Vade; PO ve Faturalar → PO'lar / Faturalar. Her role alt sekmede sekmedeki seviyesi aynen verilir; yetkiler değişmez. Yıkıcı değildir. 15 numaralı betikten sonra, **yeni API ile birlikte** uygulanır. |
| 17 | `20261014_01_Proje_Fotograf_Baglantisi.sql` | `Projeler."FotografBaglantisi"` (varchar 1000, boş olabilir) kolonunu ve kök düzeyde rotasız `proje-fotograf-baglantisi` iznini (Id 5256) ekler: Normal/Saha/Yedek proje listelerinde R fotoğraf bağlantısını görür ve açar, W ekler/değiştirir/kaldırır. Yalnız Admin W alır; diğer roller rol ekranından ayarlanır. Yıkıcı değildir, tekrar çalıştırılabilir; yeni API ile birlikte uygulanır. |
| 18 | `20261014_02_Proje_Fotograf_Baglantisi_Projeler_Altina.sql` | `proje-fotograf-baglantisi` iznini kökten Projeler menüsünün altına taşır (Sira 9, ikonsuz). Projeler bu iznin tavanı olur: Projeler R olmayan rolde bağlantı izni etkisizdir (NOTICE ile listelenir). Rol izinleri değişmez; yıkıcı değildir, tekrar çalıştırılabilir. 17. betikten sonra. |
| 19 | `20261014_03_Sandik_Tahsissiz_Satir_Onarimi.sql` | **Veri onarımı:** sandık tahsisi olmayan çeki satırlarını sandık numaralarının gösterdiği sandığa bağlar. "Çeki verisi düzenle" tahsisi olmayan satıra numara verince tahsis oluşturmuyordu; ürün sandık kartında ve 3K ekranında görünüp Depo Durumu sayımında görünmüyordu. Yalnız projede tek ve sevk edilmemiş sandıkla eşleşen satırlar onarılır; değerler 3K senkronuyla aynıdır (konulan = gelen + stok + proje + tedarikçi − başka projeye verilen, tahsis = istenen). Sandık yok / birden çok sandık / sevk edilmiş sandık durumları NOTICE ile listelenip atlanır. Yıkıcı değildir, tekrar çalıştırılabilir; API sürümünden bağımsız, istenen anda uygulanır. Durum sorgusunda onarılacak satır kaldıkça `UYGULANMAMIŞ` görünür. |

Sekme yetkileri betiği (11) yeni API sürümü yayımlanmadan **önce** çalıştırılır: yeni API Kontrol Paneli, İş Takibi,
Giderler ve Raporlar uçlarında sekme düğümü izni ister; düğümler yokken bu uçlar herkese (Admin dahil) 403 döner. Eski
API sekme düğümlerini kullanmaz ve taşınan kodların etkin seviyesi değişmediği için betikten etkilenmez. Betik sonunda
rol × sekme seviyeleri ve sekmelerin altındaki kodlar NOTICE olarak listelenir.

Sıfırlama betiği (6) Finans/Ambalaj tablolarını güncel modelle kurar (2026-10-15'ten beri 10 ve 14 numaralı betiklerin
kolonları dahil); sıfırlamadan sonra ek betik gerekmez, 10 ve 14 çalıştırılsa da bir şey değiştirmez. Finans/Ambalaj
tablolarına yeni şema betiği eklenince sıfırlama betiği de aynı işte güncellenir; `CanliyaAlimBetikleriTests` sıfırlama
sonrası şemayı EF modeliyle karşılaştırır.

Sıfırlama betiği (6) onay olarak sıfırlanacak veritabanının adını ister ve yalnız bağlı olduğu veritabanının adı
onayla aynıysa çalışır. Onay yoksa veya başka bir veritabanına bağlıysa hiçbir şey değiştirmeden hata verir. Çıktıda
önce bağlı veritabanının adı, sonra silinecek tabloların satır sayıları NOTICE olarak yazılır:

```bash
psql -h <sunucu> -U <kullanici> -d 3K_Db -v ON_ERROR_STOP=1 \
  -c "SET uc_k.finans_ambalaj_sifirla = '3K_Db'" \
  -f scripts/database/20260929_01_Finans_Ambalaj_Sifirlama.sql
```

pgAdmin kullanılıyorsa `3K_Db` düğümünden açılan sorgu penceresinde önce `SET uc_k.finans_ambalaj_sifirla = '3K_Db';`
satırı, ardından aynı pencerede betiğin içeriği çalıştırılır. Sıfırlamadan önce API durdurulur ve yedek alınır;
sonra API yeniden başlatılır. Sıfırlama Projeler, Sandiklar, Cekiler ve izin tablolarına dokunmaz. Ambalaj kaynak kuyruğu fonksiyonu ve
tetikleyicileri korunur; bir projenin ambalaj kayıtları, proje Ambalaj planına eklendiğinde veya sandıkları
değiştiğinde kaynaktan yeniden oluşur.

Kişisel izin kaldırma betiği (8) da onay olarak veritabanının adını ister. Önce onaysız çalıştırın: hiçbir şey
değiştirmeden durur ve `KullaniciYetkileri` tablosundaki bütün kişisel kararları (kullanıcı, izin kodu, İZİN/RET,
rol seviyesi) NOTICE olarak listeler. Kişisel RET kaydı olup rolü o izni veren kullanıcı, tablo silinince rolünden
erişim kazanır; böyle bir kullanıcıyı önce uygun bir role taşıyın. Ardından onayla çalıştırın:

```bash
psql -h <sunucu> -U <kullanici> -d 3K_Db -v ON_ERROR_STOP=1 \
  -c "SET uc_k.kisisel_yetki_kaldir = '3K_Db'" \
  -f scripts/database/20261003_01_Kisisel_Yetkileri_Kaldir.sql
```

Betik yeni API sürümüyle birlikte uygulanır; eski API sürümü bu tabloyu okuduğu için betikten önce durdurulur.
Tablo kaldırıldıktan sonra 2, 3 ve 7 numaralı betikler tekrar çalıştırılsa da kişisel izin adımlarını atlar.
