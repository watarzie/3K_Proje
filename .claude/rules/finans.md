---
paths:
  - "**/*Finans*"
  - "**/FinansIslemleri/**"
  - "3K.Infrastructure/Services/Finans/**"
  - "3K.Core/Constants/FinansSekmeleri.cs"
---

# Finans modülü

Genel istek/handler/controller kuralları `.claude/rules/ozellik-yazimi.md`'dedir; burada yalnız Finans'a özgü olanlar var.

## İstek tabanları (yetki yalnız sekme düzeyindedir)

- `FinansQuery<T>` (`Features/FinansIslemleri/Queries/FinansQuery.cs`): listelenen sekmelerden herhangi birinin R'si; işlem önizlemeleri `RequiredYetkiTipi`'ni W yapar.
- `FinansCommand<T>` ve `FinansCommand` (`Commands/FinansCommand.cs`): listelenen sekmelerden herhangi birinin W'si.
- Alt sınıf yalnız `public override IReadOnlyList<string> Sekmeler => [YetkiKodlari.Finans.SekmeGiderler];` yazar. Birden çok sekmede açılan ortak işlem `3K.Core/Constants/FinansSekmeleri.cs` kümelerini kullanır (`Kayit`, `KayitVeGider`, `IsDuzenleme`, `SiparisAcma`, `GiderTanimlari`, `Varlik(tur)`).
- Finans alanları: Panel, IsKaydi, SiparisFatura, Gider, Tanim, Belge, Rapor. Dosya adları `Finans<Alan>Commands.cs` / `Finans<Alan>CommandHandlers.cs` / `Finans<Alan>Queries.cs` / `Finans<Alan>QueryHandlers.cs` / `Validators/Finans<Alan>Validators.cs` kalıbındadır.

## Handler ve servis

- Finans handler'ları servis çağrısını `FinansHandlerHelper` ile `Result`'a çevirir:
  - `ExecuteAsync<T>(() => service.XAsync(...))` → `Result<T>.Success`.
  - `ExecuteOptionalAsync<T>(..., "Gider bulunamadı.")` → servis `null` dönerse 404.
  - `ExecuteAsync(Func<Task<bool>>, notFoundMessage)` → `false` ise 404.
- Finans yazmaları servis içinde `FinansIslemYurutucu.AtomikAsync` ile Serializable transaction'da çalışır, dış transaction'a katılır; değişiklik geçmişi `FinansDenetimKaydi` ile aynı transaction'da yazılır.
- Finans servis arayüzleri `3K.Core/Interfaces/IFinans*Service.cs`; implementasyonları `internal` olarak `3K.Infrastructure/Services/Finans` altında. Servisler birbirini çağırmaz; ortak davranış `FinansIslemYurutucu`, `FinansDenetimKaydi`, `FinansFiyatBelirleyici`, `FinansSorgulari`, `FinansModelEslemeleri` üzerinden.
- Kural SQL karşılığı gerekiyorsa `FinansSorgulari` içinde tek ifade olarak tanımlanır (ör. `IsNetIfadesi`) ve eşdeğerliği testle sabitlenir.

## Controller

- Finans controller'ları `FinansControllerBase`'den türer (`[Authorize]`, `[Route("api/finans")]`, `[TypeFilter(typeof(FinansAlanErisimFiltresi))]`, `Mediator` özelliği). Örnek: `FinansGiderController`.
- Sorgu filtreleri `3K_API/Controllers/FinansIstekModelleri.cs` (`FinansFilterRequest.ToModel()`).

## Yalnız sekme izni, alan maskesi yok

- 13 Ekim 2026'dan beri Finans'ta alt işlem ve alan izni yoktur: yalnız 8 sekme düğümü (`YetkiKodlari.Finans.Sekme*`) ve İş Takibi üstündeki vade uyarısı için `finans-vade-uyarisi` (R). Sekmenin R'si sekmedeki bütün alanları (tutar, birim fiyat, gelir, gider, kârlılık, ölçü, m³) gösterir; W'si sekmedeki bütün işlemleri (kalıcı silme dahil) yapar.
- Yanıtlar maskelenmez (`FinansAlanErisimFiltresi`/`FinansAlanProjeksiyonu` kaldırıldı); rapor ve belge dosyaları ayrıca alan izni istemez. Yeni Finans alanı/DTO'su için yetki işi yoktur; yeni istek için doğru `Sekmeler` yeterlidir.
- Yeni Finans alt izin kodu ekleme; ince ayrım gerekiyorsa sekme bazında ayrı rol tanımlanır. Ön yüz aynası: `features/finans-yonetimi/ortak/finans-izinleri.ts` (sunucudaki kümelerle aynı). Eşleme tablosu: `docs/guvenlik/SUNUCU_YETKI_ESLEMESI.md`.
- Testler: `FinansSekmeYetkileriTests`, `FinansModuluTests`, `FinansAlanHttpSecurityTests`.

## Para birimi, vade ve etiketler

- Para birimleri `FinansParaBirimleri` tablosundaki `FinansParaBirimi` lookup kayıtlarıdır (TRY/EUR tohumlanır, kullanıcı ekleyebilir). **Kodda para birimi listesi sabitleme**; `FinansParaBirimiKurali.Normalize` yalnız ISO-4217 biçimini kontrol eder, geçerlilik tablodan gelir. Vade parametreleri `FinansParametre` (`FinansParametreKodlari`).
- Rapor/PDF/Excel/önizlemede enum değeri `ToString()` ile yazılmaz: finans için `FinansEtiketleri` (`3K.Core/Models/FinansEtiketleri.cs` → "Özel İş", "Sipariş Bekliyor"), kategori için `FinansKategoriKurali.Ad`.
- Finans servislerini testte üretim DI kaydıyla kur: `FinansTestServisleri`.
