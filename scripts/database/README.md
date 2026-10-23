# Veritabanı betikleri

Şema ve referans veri değişiklikleri EF migration ile değil, bu klasördeki PostgreSQL betikleriyle yapılır.

- Dosya adı `YYYYMMDD_NN_Konu.sql` biçimindedir ve betikler ad sırasıyla uygulanır.
- Her betik tek bir transaction içindedir (`BEGIN … COMMIT`) ve tekrar çalıştırılabilir (idempotent) yazılır.
- Betik uygulama açılışında çalışmaz; dağıtımın ayrı bir adımıdır. Canlıya uygulamadan önce veritabanı yedeği alınır.
- EF model/configuration değişikliği ile ilgili betik aynı değişiklikte güncellenir.

Uygulama örneği:

```bash
psql -h <sunucu> -U <kullanici> -d 3K_Db -v ON_ERROR_STOP=1 -f scripts/database/20261021_02_Sahadan_Sahaya_Tasima.sql
```

## Uygulama sırası

Canlıya almadan önce `kontrol/Betik_Durumu.sql` (salt okunur) çalıştırılır: her betiğin bu veritabanında uygulanıp
uygulanmadığını bıraktığı izden (kolon, tablo, menü düğümü) gösterir. `UYGULANMAMIŞ` satırlar aşağıdaki sırayla
uygulanır. Yeni betik eklenince bu sorguya da satır eklenir (`CanliyaAlimBetikleriTests` dosya listesiyle karşılaştırır).

Betiklerin hepsi ad sırasıyla, uygulamanın veritabanına (`3K_Db`) bağlıyken çalıştırılır. pgAdmin sorgu aracını ağaçtaki
`3K_Db` düğümünden açın: sunucu düğümünden açılan pencere varsayılan `postgres` veritabanına bağlanır (sekme başlığında
veritabanı adı görünür).

1–20 numaralı betikler (28 Eylül – 17 Ekim 2026) canlıya uygulandı ve `da54f6f` ile depodan kaldırıldı; geri getirilmez.
21 numara boş kaldı: otomatik sahaya aktarım (İş 1) şema değişikliği gerektirmedi. `MenuTanimlari` kimlikleri: canlıda
en büyük Id 5256; 5258 Grid Sahası ekranına verildi (23), 5257 kullanılmadı (sahadan sahaya taşıma mevcut `sahaya-aktar`
iznini kullanır); 5202, 5204, 5205, 5208, 5234 asla yeniden kullanılmaz. Grid Sahası için önceden düşünülen
`20261021_03_Grid_Saha_Gorunumu.sql` (geliş tarihi kolonu) uygulanmadı; yerine şemasız 23 numaralı betik geldi.

| Sıra | Betik | Ne yapar |
|---|---|---|
| 22 | `20261021_02_Sahadan_Sahaya_Tasima.sql` | **Sahadan sahaya taşıma:** `SahaAktarimlari`'na `KaynakSahaProjeId` (taşımada satırların geldiği saha projesi; FK `Projeler`, silinirse boşalır) ve `IslemAnahtari` (uuid; boş değilse benzersiz indeks — aynı istek ikinci kez uygulanmaz) kolonlarını, `SahaTasimaKalemleri` iz tablosunu (taşınan her satırın kaynak/hedef saha ve sandığı, miktarı, tarihi; numaralar uzunluk sınırsız kopyadır; taşıma başlığı, kaynak/hedef saha projesi, sandık ya da saha satırı silinirse bağ boşalır ve iz kalır — A→B→C zincirinde ara saha B silinse de A→B adımı okunur; yalnız kök normal satır silinirse iz de silinir) ve indekslerini ekler. Yıkıcı değildir, tekrar çalıştırılabilir. **Yeni API başlamadan önce** uygulanır: yeni API bu kolonları ve tabloyu her saha aktarımı okumasında kullandığı için betik yoksa mevcut sahaya aktarma ve eksik raporu da çalışmaz. Eski API çalışırken uygulanabilir (eski API yeni kolonları kullanmaz); yoğun saatte uygulanacaksa oturumda önce `SET lock_timeout = '5s';` verilebilir. Finans/Ambalaj tablosuna dokunmaz. |
| 23 | `20261023_01_Grid_Saha_Gorunumu_Yetkisi.sql` | **Grid Sahası ekranı izni** (`grid-saha-gorunumu`, Id 5258, `MENU.GRID_SAHA_GORUNUMU`, Route `/grid-saha`): Sandık Yönetimi altına Grid iş listesinden sonra eklenir. Admin W; Grid iş listesinde R veya W olan roller R alır (yerelde: Görüntüleyici, Görüntüleyici Yükleyici, PersonelGrid, Sevkiyat Sorumlu Mühendis, Sistem Yönetici). Şema değişikliği yoktur. **Yeni API'den önce** uygulanır: yoksa ekran menüde görünmez ve Grid Sahası uçları 403 verir. Sonunda rol × seviye listesini NOTICE olarak yazar. |

Tekrar çalıştırma: 22 numaralı betik tekrar çalıştırıldığında kolonları, tabloyu, kısıtları ve indeksleri yeniden eklemez,
var olan taşıma izlerine dokunmaz; 23 numaralı betik düğüm varsa eklemez, verilmiş izni değiştirmez
(`CanliyaAlimBetikleriTests`).
