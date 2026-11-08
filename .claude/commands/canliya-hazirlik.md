---
description: Canlıya alım öncesi dalı denetler — şema betikleri ve çalıştırma sırası, güvenlik, sözleşme ve iş kuralı dokümanları — ve kullanıcının canlıda yapacağı adımları listeler; dosya değiştirmez, betik çalıştırmaz.
argument-hint: "[karşılaştırılacak dal, varsayılan master]"
---

# Canlıya alım hazırlığı

Karşılaştırma tabanı: $ARGUMENTS (boşsa `master`).

Kullanıcıyla Türkçe konuş. Bu komut dosya değiştirmez, veritabanına yazmaz ve betik çalıştırmaz; gerçek veritabanında
betiği kullanıcı çalıştırır.

1. **Kapsam:** iki depoda `git log --oneline <taban>..HEAD` ve `git diff --name-only <taban>...HEAD`. Commit edilmemiş
   değişiklik varsa ayrıca belirt (canlıya gitmez).
2. **Denetim (tek mesajda paralel):**
   - `sema-betik-denetcisi` — yeni betikler, README sırası, `Betik_Durumu.sql`, `CanliyaAlimBetikleriTests`, sıfırlama
     betiği, yıkıcı onay kalıbı, veri kaybı/kilit riski, `canliya-alim-*/` kopyası.
   - `guvenlik-denetcisi` — dalın bütün değişiklikleri; bağımlılık taraması dahil.
   - `sozlesme-denetcisi` — arka uç ve ön yüz aynı dalda uyumlu mu.
   - `is-kurali-dokumantasyon` — yalnız **rapor modu**: eksik eşleme/doküman satırlarını listelesin, dosya düzenlemesin.
3. **Testler:** değişen alanların test sınıflarını listele; kullanıcı isterse Release'de filtreyle koş, PostgreSQL
   davranışı değiştiyse `postgres-test` skill'ini öner. Koşulmayanı geçti diye yazma.
4. **Rapor:**
   - Engel bulgular (Kritik/Yüksek) ve sahibi.
   - Canlıda çalıştırılacak betikler **sırasıyla**, onay isteyenlerin `SET uc_k.* = '<veritabanı adı>'` adımı ve
     çalıştırma sonrası doğrulama (`scripts/database/kontrol/Betik_Durumu.sql`).
   - Yeni izin kodları ve hangi rollere verilmesi gerektiği (karar kullanıcının).
   - Ön yüz ile arka ucun birlikte yayınlanması gereken değişiklikler.
