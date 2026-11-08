---
description: Yeni bir özelliği planla → onay al → arka uç ve ön yüzü geliştir → test yaz → gözden geçir. 3K ajanlarını sırayla çağırır; commit atmaz.
argument-hint: "<özellik isteği>"
---

# Özellik iş akışı

İstek: $ARGUMENTS

Bu akışı sırayla yürüt. Kullanıcıyla Türkçe konuş. Commit atma, stage etme, push yapma; dal değiştirme.

1. **Durum:** `git status --short` (arka uç ve `C:\Users\Watarzie\Desktop\3k_onyuz`). Var olan değişiklikleri koru.
2. **Plan:** `ozellik-planlayici` ajanını isteğin tam metniyle çağır. Dönen şartnameyi, açık soruları ve uygulama planını
   kullanıcıya özetle.
   - Miktar, para, yetki veya kalıcı veriyle ilgili açık soru varsa **burada dur** ve yanıtı bekle.
   - Kullanıcı planı onaylamadan 3. adıma geçme.
3. **Arka uç:** plan arka uç değişikliği içeriyorsa `arka-uc-gelistirici`'yi onaylanan planla çağır (şema betiği ve
   izin kodu dahil). Sonuçtaki build/test çıktısını kontrol et.
4. **Ön yüz:** plan ön yüz değişikliği içeriyorsa `on-yuz-gelistirici`'yi onaylanan plan ve arka uçtaki gerçek
   controller/DTO değişiklikleriyle çağır. Arka uç ve ön yüz aynı dosyalara dokunmadığı için sözleşme kesinse 3 ve 4
   paralel çalışabilir; sözleşme 3. adımda belirleniyorsa sırayla çalıştır.
5. **Test:** `test-yazici`'yi değişen dosyalar ve kabul kriterleriyle çağır.
6. **Gözden geçirme:** `/gozden-gecir` komutundaki adımları uygula (denetçileri paralel çalıştır).
   Kritik/Yüksek bulguları ilgili geliştirici ajana düzelttir ve yalnız o denetçiyi yeniden çalıştır.
7. **Doküman:** davranış veya iş kuralı değiştiyse `is-kurali-dokumantasyon`'u çağır.
8. **Teslim:** CLAUDE.md "Teslim" biçiminde özetle: değişen davranış, dosyalar (iki depo), koşulan testler ve filtreleri,
   şema betiği ve canlıda çalıştırma sırası, açık kalanlar. Sonra "Commit edebilir miyim?" diye sor.
