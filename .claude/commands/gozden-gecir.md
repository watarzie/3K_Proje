---
description: Arka uç ve ön yüzdeki bekleyen değişiklikleri, değişen dosyalara göre seçilen 3K denetçi ajanlarıyla paralel gözden geçirir ve tek rapor verir; dosya değiştirmez.
argument-hint: "[işin kısa amacı]"
---

# Gözden geçirme

Amaç (varsa): $ARGUMENTS

Kullanıcıyla Türkçe konuş. Bu komut dosya değiştirmez; bulguları düzeltmek ayrı bir adımdır.

1. **Kapsam:** iki depoda `git status --short` ve `git diff --stat` (arka uç
   `C:\Users\Watarzie\source\repos\3K_Proje`, ön yüz `C:\Users\Watarzie\Desktop\3k_onyuz`). Değişiklik yoksa söyle ve dur.
2. **Denetçileri seç** (değişen dosyalara göre; seçilmeyeni ve nedenini rapora yaz):
   | Koşul | Ajan |
   |---|---|
   | Arka uçta `.cs` değişikliği | `kod-gozden-gecirici` |
   | Ön yüzde değişiklik | `on-yuz-gozden-gecirici` |
   | Controller, DTO, `3K.Core/Models`, enum, `YetkiKodlari`, `api-endpoints.ts`, `shared/models`, `yetki-kodlari.ts` veya i18n | `sozlesme-denetcisi` |
   | `3K.Core/Entities`, `*ModelBuilderExtensions.cs`, `AppDbContext.cs`, `scripts/database/**`, `YetkiKodlari.cs` | `sema-betik-denetcisi` |
   | Ambalaj DTO/modeli veya Finans Command/Query | `finans-maske-denetcisi` |
   | Yetki/izin, Auth/2FA, hız sınırı, dosya yükleme/indirme, `IstekGirdisi`, onay hattı, ham SQL, `ConfirmService`/`innerHTML` | `guvenlik-denetcisi` |
   | Yeni/değişen liste, sayfalama, rapor/dışa aktarım, ağır ön yüz bağımlılığı | `performans-analisti` |
3. **Çalıştır:** seçilen ajanları **tek mesajda paralel** çağır. Her birine o ajanı ilgilendiren değişen dosya listesini,
   `git diff` özetini ve işin amacını ver.
4. **Birleştir:** tek rapor yaz:
   - Özet: commit'e engel bulgu var mı.
   - Tek tablo, önem sırasıyla (Kritik → Düşük): önem · dosya:satır · bulgu · öneri · kaynak ajan. Aynı bulguyu iki ajan
     bildirdiyse bir kez yaz.
   - "Doğrulanmadı" işaretli bulguları ayrı başlıkta ve nasıl doğrulanacağıyla ver.
   - Koşulmayan kontroller (ör. build, spec, PostgreSQL testi) — geçti diye raporlama.
5. Düzeltmeye geçmeden kullanıcıya hangi bulguların düzeltileceğini sor.
