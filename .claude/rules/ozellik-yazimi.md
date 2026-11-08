---
paths:
  - "3K.Application/**"
  - "3K_API/**"
  - "3K.Core/Models/**"
  - "3K.Core/Interfaces/**"
---

# Bir özellik nasıl yazılır

Akış: **Controller → Command/Query → (Authorization → Validation → Approval) → Handler → Servis/Repository → Core modeli/kuralı**.
Finans'a özgü tabanlar ve yardımcılar `.claude/rules/finans.md`'dedir.

## Command / Query

- Her use case bir request sınıfıdır; yetki isteğin kendisinde bildirilir:
  - `ISecuredRequest` + `IRequiresMenuPermission` (`RequiredMenuKod`, `RequiredYetkiTipi` = `YetkiTipi.R` görüntüleme / `YetkiTipi.W` işlem).
  - Birden çok izin gerekiyorsa `IRequiresMenuPermissions.RequiredMenuPermissions` (`MenuPermissionRequirement.Read(kod)` / `.Write(kod)`; varsayılan AND, `PermissionMatch => MenuPermissionMatch.Any` açıkça belirtilir).
  - Proje/sandık bağlamlı izinler `IRequiresProjectMenuPermission` / `IRequiresSandikMenuPermission` ile ya da `RequestMenuPermissionResolver` içinde tanımlanır (proje tipi DB'den okunur, istemciden değil).
  - `ISecuredRequest` olmayan istek, açık istisna listesinde (`MenuAuthorizationExceptions`) değilse 403 alır.

## Handler

- Handler orkestre eder (yükle → kuralı kontrol et → değiştir → kaydet → döndür) ve yalnız Core soyutlamalarına bağlıdır. Constructor injection; `IServiceProvider`'dan servis çözme yok.
- Beklenen retler `Result.Failure(mesaj, kod)` ile döner. `Result` yapamayan derin servisler kullanıcıya gösterilecek reddi `IsKuraliException` (varsayılan 400, `IsKuraliException.Cakisma(...)` = 409) veya `KayitBulunamadiException` (404) ile bildirir; pipeline durum koduna çevirir. `InvalidOperationException` mesajı kullanıcıya ulaşmaz.
- Hepsi-ya-hiçbiri akışlar: `unitOfWork.ExecuteAtomicAsync(...)` (başarısız `Result` dönünce de geri alır). `IUnitOfWork.ExecuteInTransactionAsync` delegate normal dönerse commit eder; yalnız bilinçli kısmi başarı akışlarında kullan.
- Her `CancellationToken` aktarılır; `.Result`/`.Wait()` yok; aynı `DbContext` üzerinde `Task.WhenAll` yok.

## Validator

- FluentValidation, `AddValidatorsFromAssembly` ile otomatik kaydolur; dosya `Validators/` altında, sınıf adı `<Request>Validator`.
- Kurallar Core'daki aynı kural fonksiyonunu çağırır (ör. `FinansTarihKurallari.GecerliTarih`, `FinansGerekceKurali.Gecerli`); kuralı validator'da yeniden yazma.

## Controller

- Controller yalnız bind eder, MediatR'a gönderir ve `ToActionResult()` ile eşler; iş kuralı ve veri erişimi yoktur.
- `ResultExtensions.ToActionResult()` (`3K_API/Extensions`): başarıda ham DTO (200), `StatusCode == 202` ise "onaya sunuldu" gövdesi; hata gövdesi her yerde `{ message, issues? }` ve 401/403/404/409/429/500/400 eşlemesi. Dosya için `Result<DosyaIcerigi>` + `ToFileResult()`.
- **202 = onay kuyruğuna alındı, uygulanmadı.**
- Ekran istekleri Application `DTOs` altındaki record'lardır ve kendilerini servis modeline çevirir (ör. `FinansGiderKaydetDto.GiderModeli()`).

## Modeller ve DTO'lar

- Servis giriş modelleri `3K.Core/Models` altında genelde `sealed record` (ör. `FinansGiderKaydetModel`); yanıt modelleri çoğunlukla `init` özellikli `sealed class` (ör. `FinansGiderModel`).

## Pipeline davranışları: dışa aktarım sınırı ve hata loglama

- Yanıtı `Result<DosyaIcerigi>` olan her istek (PDF/Excel/etiket, dosya indirme) `DisaAktarimHizSiniriBehavior` ile kullanıcı başına 20/dk sınırlanır (429); yeni dosya ucu için attribute gerekmez. Genel API sınırı ve giriş/2FA sınırları API katmanındadır (`HizSiniriBolumleri`, `docs/guvenlik/GIRIS_VE_HIZ_SINIRI.md`).
- Başarısız her istek (400/401/403/404/409/429; 500 hariç, o zaten Error) `IsHatasiLoglamaBehavior` ile tek satır Warning loglanır: istek adı, kullanıcı, mesaj ve `IstekGirdisi` ile maskelenmiş girdi (500 için aynı girdi `UnhandledExceptionBehavior`'ın Error satırında).
  - Parola/token/anahtar/doğrulama kodu alanları `***`; e-posta alanı biçimliyse kısmen (`de***@gmail.com`), değilse tamamen gizlenir (kutuya yanlışlıkla yazılan şifre loga düşmez); dosya akışı ve bayt dizisi yalnız boyutuyla yazılır; yeni gizli alan adı eklenirse `IstekGirdisi` ve testini güncelle.
  - Satırdaki `CorrelationId`, yanıtın `X-Correlation-ID` başlığıdır; ön yüz hata bildiriminde (401 hariç) "Takip: …" olarak gösterir. Tek takip kodu budur: sunucu ayrı kod üretmez, yanıt mesajına kod yazmaz ve log şablonunda `CorrelationId`/`TakipKodu` adlı özellik kullanmaz (istek kodunu ezer). Kalıcı kayda kod gerekiyorsa `ITakipKoduSaglayici` (ör. onay yürütme hatası).
  - Service log 30 gün, trace log 7 gün saklanır.
