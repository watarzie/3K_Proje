---
paths:
  - "**/*.cs"
  - "scripts/**/*.sql"
---

# Kod stili ve kurallar

- Değiştirdiğin dosyanın namespace (dosya kapsamlı veya bloklu), girinti, süslü parantez ve adlandırma stilini koru. Yeni kodda primary constructor yaygındır (ör. `FinansGiderCommandHandlers(IFinansGiderService service)`).
- Satır sonları: `core.autocrlf=true`; çalışma kopyası CRLF'dir, yeni dosyalar da CRLF yazılır.
- Yorumlar Türkçe ve "neden"i anlatır; public tiplerde `/// <summary>` kullanılır. UTF-8 ve Türkçe karakterler bozulmaz.
- Rapor/PDF/Excel/önizlemede enum değeri `ToString()` ile yazılmaz (`OzelIs`, `SiparisBekliyor` değil): finans için `FinansEtiketleri` (`3K.Core/Models/FinansEtiketleri.cs` → "Özel İş", "Sipariş Bekliyor"), kategori için `FinansKategoriKurali.Ad`. API sözleşmesi enum sayısıyla kalır.
- Kullanıcıya dönen mesajlar Türkçedir; iç istisna detayı, dosya yolu veya gizli değer döndürülmez.
- İş kuralı controller'a yazılmaz; tekrar eden kural Core'daki politikaya/kural sınıfına çıkarılır. Kural SQL karşılığı gerekiyorsa `FinansSorgulari` içinde tek ifade olarak tanımlanır (ör. `IsNetIfadesi`) ve eşdeğerliği testle sabitlenir.
- Kullanılmayan soyutlama, reflection tabanlı dispatch, isimden davranış seçimi yok. Derleyici uyarısı bastırılmaz.

## Zaman

- Saklanan/gösterilen işlem ve rapor zamanları `TurkeyTime.Now` (`3K.Core/Helpers/TurkeyTime.cs`). `DateTime.Now` kullanma. JWT/2FA ömürleri ayrı sözleşmedir.
- `Program.cs` `Npgsql.EnableLegacyTimestampBehavior` anahtarını açar; tarih dönüşümü eklemeden önce akışın kullanımını kontrol et.
