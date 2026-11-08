---
description: Bir hata bildirimini teşhis et → onay al → önce hatayı tekrar üreten testi yaz → en küçük düzeltmeyi yap → gözden geçir. Commit atmaz.
argument-hint: "<belirti: ekran, beklenen/görünen değer, proje/sandık no, Takip kodu veya hata çıktısı>"
---

# Hata düzeltme iş akışı

Belirti: $ARGUMENTS

Kullanıcıyla Türkçe konuş. Commit atma, stage etme, push yapma. Gerçek veritabanında veri değiştirme; veri onarımı
gerekiyorsa betiği hazırla, kullanıcı çalıştırır.

1. **Teşhis:** `canli-hata-teshisci`'yi belirti, ekrandaki sayılar, kayıt numaraları ve Takip kodu (ya da test/derleme
   çıktısı) ile çağır. Canlı bildirimde veri `3K_Db_Canli`'den okunur; `postgres-canli-kopya` bağlantısı yoksa bunu
   kullanıcıya söyle.
2. **Onay:** kök nedeni, kapsamı, düzeltme önerisini ve tekrar üreten test önerisini kullanıcıya özetle.
   - Neden **veri** ise (kod doğruysa) kod değiştirme; onarım betiği önerisini sun ve dur.
   - Para, miktar veya kalıcı veri kararı gerekiyorsa soruyu sor ve dur.
   - Kullanıcı düzeltmeyi onaylamadan 3. adıma geçme.
3. **Düzeltme:** sorunun bulunduğu tarafa göre `arka-uc-gelistirici` ve/veya `on-yuz-gelistirici`'yi teşhis raporuyla
   çağır. İstenen sıra: önce tekrar üreten test **kırmızı**, sonra en küçük düzeltme, sonra test **yeşil**; tekil,
   toplu ve eski uçlarda aynı desen. Testin gerçekten kırmızı görüldüğünü sonuçtan doğrula.
4. **Gözden geçirme:** değişen dosyalarla `/gozden-gecir` adımlarını uygula.
5. **Doküman:** iş kuralı davranışı değiştiyse `is-kurali-dokumantasyon`'u çağır.
6. **Teslim:** kök neden, değişen dosyalar, kırmızı→yeşil test (filtreyle), etkilenen diğer kayıtlar ve gerekiyorsa
   kullanıcının çalıştıracağı veri onarımı. Sonra "Commit edebilir miyim?" diye sor.
