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

Betiklerin hepsi ad sırasıyla, uygulamanın veritabanına (`3K_Db`) bağlıyken çalıştırılır. Daha önce uygulanmış betik
tekrar çalıştırıldığında değişiklik yapmaz. pgAdmin sorgu aracını ağaçtaki `3K_Db` düğümünden açın: sunucu düğümünden
açılan pencere varsayılan `postgres` veritabanına bağlanır (sekme başlığında veritabanı adı görünür).

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

Sıfırlama betiği (6) Finans/Ambalaj tablolarını yazıldığı andaki modelle kurar; 10 numaralı betikten sonra çalıştırılırsa
10 numaralı betik (ahşap/kontra m³ kolonları) yeniden uygulanmalıdır.

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
