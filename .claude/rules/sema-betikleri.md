---
paths:
  - "scripts/database/**"
  - "3K.Core/Entities/**"
  - "3K.Infrastructure/Data/**"
  - "3K.Core/Constants/YetkiKodlari.cs"
---

# Şema değişiklikleri: EF migration YOK

Canlıya alım öncesi denetim için `sema-betik-denetcisi` ajanını, yeni izin kodu için `yeni-yetki-kodu` skill'ini kullan.

- Şema/referans veri değişikliği = EF model/configuration değişikliği **+** `scripts/database/bekleyen/YYYYMMDD_NN_Konu.sql` betiği. Betik idempotent ve tek transaction'dır (`BEGIN … COMMIT`), uygulama açılışında çalışmaz.
- Klasörler: `bekleyen/` canlıya uygulanmamış, `uygulandi/YYYY-AA/` canlıda çalışmış (kullanıcı canlıya alınca taşır; silinmez), `geri-alma/` yedek ve geri alma eşleri, `kontrol/` salt okunur sorgular.
- Her yeni betik (geri alma ve yedek dahil) `COMMIT`'ten hemen önce README'deki "Betik kaydı" bloğunu içerir: `BetikKayitlari` tablosuna kendi adını (dosya adı, uzantısız) yazar. `kontrol/Betik_Durumu.sql` satırı yine eklenir (kayıt bloğundan önce elle uygulanmış veritabanı için iz). `BetikKayitlari` EF modelinde yoktur; `CanliyaAlimBetikleriTests` şema karşılaştırmasında hariç tutulur.
- Çalıştırma sırası ve açıklamaları `scripts/database/README.md` tablosundadır; yeni betik eklerken README'yi de güncelle.
- Finans/Ambalaj tablolarına şema değişikliği eklenince `20260929_01_Finans_Ambalaj_Sifirlama.sql` de güncellenir (sıfırlama güncel modeli kurmalı; `CanliyaAlimBetikleriTests` denetler).
- Yıkıcı betikler onay ayarı ister ve ayar `current_database()` ile aynı olmalıdır. Örnek: `20260929_01_Finans_Ambalaj_Sifirlama.sql` yalnız aynı oturumda `SET uc_k.finans_ambalaj_sifirla = '<veritabanı adı>'` verilmişse çalışır. Yeni yıkıcı betik aynı kalıbı izler.
- pgAdmin kullanıcısına sorgu aracını `3K_Db` düğümünden açmasını söyle (sunucu düğümü varsayılan `postgres` DB'sine bağlanır).
- Eşleme taşıma/bölme gibi davranış değiştirmemesi gereken refactor'larda `ToDebugString` ve `GenerateCreateScript` çıktısını önce/sonra karşılaştır.
