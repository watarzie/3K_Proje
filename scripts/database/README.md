# 19.09.2026 — Üretim/Finans V2 veritabanı geçişi

Bu klasör **mevcut 3K temel şemasının üzerine** uygulanan sürüm değişikliklerini içerir.
Boş PostgreSQL veritabanının tam kurulumu değildir. Eski `20260511223933_InitialCreate`
migration'ı tek başına güncel Ambalaj/Finans temel tablolarını kurmaz. Gerçek temel
kurulumun veya doğrulanmış kopyanın bulunması gerekir; eksik tabloyu atlayarak devam etmeyin.

## Dosyalar ve sıra

| Sıra | Dosya | Amaç |
|---|---|---|
| Önce | `20260919_02_Uretim_Onizleme.sql` | Salt okunur: eski kontra/katlanır miktarı ve belirsiz üretim tarihleri |
| Önce | `20260919_03_Finans_Onizleme.sql` | Salt okunur: mevcut belge/para birimi toplamları ve kapasite uyuşmazlıkları |
| 01 | `20260919_01_Sandik_Toplu_Tasima.sql` | Önceki geliştirmeden gelen toplu taşıma işlem anahtarı defteri; zaten kuruluysa tekrar güvenli |
| 02 | `20260919_02_Uretim_YasamDongusu.sql` | Kalıcı form sürümleri, gerçekleşme snapshot'ları ve üretim cinsi uyumu |
| 03 | `20260919_03_Finans_V2.sql` | Finans tarihi/sahipliği, tutar dağıtımı, şablon/belge/audit/bastırma ve eski snapshot yedeği |
| 04 | `20260919_04_Granular_Yetkiler.sql` | Ayrı işlem/alan izinleri, kullanıcı override ve kontrollü ilk rol eşlemesi |

02–04 geçiş ve ilgili önizleme SQL'leri DBA tarafından depo dışında yönetilir; Git'e
eklenmeleri, uygulama paketine gömülmeleri veya .NET derlemesinde bulunmaları gerekmez.
Canlı şema geçişi bu SQL'lerin yetkili kişi tarafından elle uygulanmasıdır.
`20260919155111_UretimFinansV2` EF migration'ını bu sürüm için çalıştırmayın.

## Canlıya geçiş

1. Önce canlı kopyasında staging provası yapın. Tam yedeği ve geri yükleme işlemini
   doğrulayın. Önizleme sonuçlarını kaydedin. Uyuşmayan belge, para birimi, kapasite
   veya lookup/izin kimliğini incelemeden geçiş korumalarını kaldırmayın.
2. Bakım penceresinde API yazmalarını ve worker'ları durdurun. Son tam yedeği alın.
3. DBA, 01 → 02 → 03 → 04 SQL dosyalarını sırayla elle çalıştırır. Her dosya kendi
   transaction'ını kullanır. Herhangi biri hata verirse `ROLLBACK` yapın ve
   **sonraki dosyaya geçmeyin**. Sebebi çözüp aynı idempotent sırayı tekrarlayın;
   arada uygulamayı açmayın. Önceki `InitialCreate` migration'ını mevcut şemaya
   yeniden çalıştırmayın.
4. Sonuçları önizleme/yedekle karşılaştırın. Finans eski belge net/KDV/brüt
   snapshot'larını `FinansV2GecisYedegi` içinde korur. Önceden kabul edilmiş belge
   mutabakatı satırlara deterministik dağıtılır; yeni tarife uygulanmaz. İş/PO
   kapasitesi aşılmışsa script durur, sessiz düzeltme yapmaz.
5. SQL sırası tamamlanıp sonuçlar doğrulandıktan sonra backend ve frontend'i birlikte
   yayımlayın. Rol/kullanıcı ekranından yeni kritik izinleri açıkça atayın; onay
   gerekiyorsa mevcut dinamik onay kurallarını
   yapılandırın. Kök menü W bütün yeni kritik işlemleri otomatik açmaz.
6. Ayrılmış test verisiyle ilk form, tekrar istek, tamamlanma raporu, net/sarf
   işleri, kısmi PO/fatura, alan gizliliği ve PDF indirmeyi doğrulayın. Sonra
   worker'ları açıp hata/işlem kayıtlarını izleyin.

Bu elle uygulanan SQL'ler için `__EFMigrationsHistory` tablosuna satır eklemeyin veya
V2 EF migration'ını uygulanmış gibi işaretlemeyin. Sonraki EF geçişinde önce gerçek
şema/geçmiş eşleşmesini doğrulayın. `EnsureCreated`,
`EnsureDeleted`, uyarı bastırma veya geçmiş tablolarını silme çözüm değildir.

## Yetki geçişi

İlk eşleme işaret tablosuyla yalnız bir kez yapılır. Eski R görünüm izinlerine,
eski W kritik olmayan eylemlere taşınır; kritik izinler sonradan açıkça atanır.
Mevcut Admin rolüne (ID 1) açık izin kayıtları eklenir; çalışma zamanında admin
bypass'ı yoktur. Kullanıcı açık reddi Admin dahil rol izninin önündedir.
Tekrar SQL çalıştırmak sonradan kaldırılmış bir rol iznini geri vermez.
Modül kökü ve ilgili işlem/alan izni birlikte gerekir.

## Geçmiş veri ve geri dönüş

- Bilinmeyen eski tamamlanma tarihine bugünün tarihi yazılmaz. Gerçekleşme
  snapshot'ı bulunmayan eski kayıtlar raporda ayrıca belirtilir.
- Üretim kontra/katlanır m³ hesabı yapmaz. Eski üretim kolonları uyumluluk için
  tutulur; finansın mevcut manuel değerleri ve belge snapshot'ları korunur.
- Başarısız transaction rollback olur. Başarılı geçiş sonrası yeni veri
  yazılmadıysa doğrulanmış tam yedek + uyumlu eski uygulama en güvenli dönüş yoludur.
- Yeni V2 yazmalar başladıktan sonra otomatik `Down` desteklenmez: amount-only
  mali dağıtımları fiziksel adet gibi geri çevirmeyin, audit/form/belge tablolarını
  silmeyin. Yazmaları durdurup yeni veriyi de yedekleyin; kontrollü ileri düzeltme
  veya veri koruyan geri dönüş planlayın. Snapshot yedek tablosu tam yedeğin yerine geçmez.

Üretim ayrıntıları: [Üretim geçişi](../../docs/20260919_URETIM_V2_GECIS.md).
Finans ayrıntıları: [Finans geçişi](../../docs/20260919_FINANS_V2_GECIS.md).
Yetkiler: [Granular izinler](../../docs/guvenlik/GRANULAR_YETKI_20260919.md).
Test kanıtı: [Test kapsamı](../../docs/is-kurallari/TEST_KAPSAMI.md).

Bu çalışma sırasında canlı veya uygulamanın yerel veritabanına geçiş uygulanmadı.
Gerçek SQL sırası ve rollback yalnız izole PostgreSQL test veritabanlarında doğrulandı.
