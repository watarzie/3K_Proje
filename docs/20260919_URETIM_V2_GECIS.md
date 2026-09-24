# Üretim yaşam döngüsü geçişi

Gerçekleşme, ilk başarılı Tamamlandı geçişinde sunucunun Türkiye takvim zamanı ve o andaki adet/net/sarf snapshot'ıdır. İşlenen m³ net ahşap hacmidir; sarf dahil hacim ayrı alandır. Form oluşturma/indirme, kaynak eşleme ve finans düzenlemesi fiziksel üretim oluşturmaz. Yeniden açılan kayıt aynı gerçekleşme kimliğini korur. İlave fiziksel üretim yeni üretim kaydıyla girilir. Düzeltme eski snapshot'ı silmez; yeni sürüm onu geçersiz kılar ve tarih filtresi yalnız son sürüme uygulanır.

Eski nonnull m³ kolonları şema uyumluluğu için korunur. Kontra/katlanır/bilinmeyen cinste yeni hesap yapılmaz, dış sözleşmede hacim null ve M3HesaplanabilirMi=false olur. Eski hacimler yeni hesap/gerçekleşme toplamlarına alınmaz; PO/fatura snapshot'ları değiştirilmez. Finans kendi manuel hacminin sahibidir.

## Uygulama

1. `scripts/database/20260919_02_Uretim_Onizleme.sql` salt okunur raporunu arşivleyin; lookup tip 3 çakışmasını inceleyin.
2. Depo dışında yönetilen `20260919_02_Uretim_YasamDongusu.sql` şema geçişini DBA olarak bakım penceresinde elle uygulayın. Bu sürümün EF V2 migration'ını çalıştırmayın veya SQL uygulamasını EF geçmişine işaretlemeyin. Tüm 01–04 sırası ve backend yayım adımı için [veritabanı geçiş kılavuzunu](../scripts/database/README.md) izleyin.
3. Eski tamamlanan kayda bugün tarihiyle otomatik gerçekleşme üretmeyin. Rapor, snapshot'ı olmayan tamamlananların sayısını ayrıca gösterir. Eski doğrulanmış gerçekleşmeler için ayrı, kanıtı ve aktörü bulunan kontrollü veri geçişi gerekir.
4. Yeni izinleri rol/kullanıcılara açıkça atayın; önceki genel W kritik izinlerin kanıtı değildir.

## Geri dönüş

Önce uygulama yazmalarını durdurun ve yeni üç tablonun tam yedeğini alın. Önceki uygulama sürümüne dönüldüğünde tabloları silmeyin; eski uygulama onları kullanmaz. Eski üretim m³ değerleri topluca silinmediği ve finans belgeleri değiştirilmediği için tutar geri hesaplama gerekmez. Yeni kayıtlar oluştuysa eski şemaya DROP ile dönmek geçmiş kaybıdır; desteklenen geri dönüş uygulama sürümünün geri alınması ve yeni tabloların korunmasıdır. Yeni küçük ölçülü kontra/katlanır kayıtlar eski ahşap constraint'ine uymayabileceğinden eski constraint otomatik geri kurulmaz.

Bu dosyalar hazırlanan geçiş sözleşmesidir; canlı veriye uygulandığı anlamına gelmez.
