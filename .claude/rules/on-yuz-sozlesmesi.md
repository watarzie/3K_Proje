# Ön yüz sözleşmesi (3k_onyuz aynası)

Bu dosya her oturumda yüklenir (yol sınırı yok). Ön yüz ayrı depodadır: `C:\Users\Watarzie\Desktop\3k_onyuz`
(Angular 20). Ön yüz kurallarının kaynağı oradaki `CLAUDE.md` ve `3k_onyuz-AGENTS.md`'dir; ön yüz dosyalarına dokunmadan
önce ikisini oku. Bu depodaki yol sınırlı kurallar ön yüz dosyalarına uygulanmaz.

- Sözleşme değişikliğinde (endpoint, DTO, enum, izin kodu, çeviri) ön yüz `3k_onyuz` tarafını da aynı işte güncelle.
- Eşleşmesi gereken yerler:
  - Endpoint → `src/app/core/constants/api-endpoints.ts` (`API.*`), servis metodu `src/app/core/services/<alan>.service.ts`
    (`BaseApiService`'ten türer).
  - DTO → `src/app/shared/models/<alan>.model.ts`: alan adları camelCase, nullable'lar ve enum **sayıları** birebir; enum
    değeri çeviri için değiştirilmez.
  - Sayfalı yanıt `SayfaliSonuc<T>` → ön yüzde `PaginatedList<T>` + `app-server-pager`.
  - 202 (onaya alındı) → ön yüzde `onayaAlindi()` (`shared/models`); 202'de başarı mesajı, liste yenileme, indirme yok.
  - Hata gövdesi `{ message, issues? }` ve `X-Correlation-ID` → ön yüzde `ApiResult<T>` (`isSuccess: false`) ve
    "Takip: …" eki.
  - İzin kodu → `src/app/core/constants/yetki-kodlari.ts` (`YETKI`, `AMBALAJ_YETKI`, `FINANS_YETKI`); düğme koşulu,
    çağrılan ucun sunucuda istediği izinle aynıdır. Finans kümeleri `features/finans-yonetimi/ortak/finans-izinleri.ts`.
  - Metin → `src/assets/i18n/tr.json` ve `en.json` birlikte (`MENU.*` izin etiketleri).
- Ön yüz testi Karma/Jasmine'dir; yalnız değişen klasörün spec'leri `--include` ile koşulur. Görünür değişiklik
  sahte veriyle ön yüz deposundaki `gorsel-kontrol` skill'iyle ya da `.claude/launch.json` → `onyuz` önizlemesiyle
  doğrulanır. Lint komutu tanımlı değildir.
- Ayrıntılı karşılaştırma için `sozlesme-denetcisi`, ön yüz değişikliği incelemesi için `on-yuz-gozden-gecirici` ajanını kullan.
