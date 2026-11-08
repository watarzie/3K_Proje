---
paths:
  - "3K.Infrastructure/**"
  - "3K.Application/**"
---

# Veri erişimi

Şema değişikliği ve betik kuralları `.claude/rules/sema-betikleri.md`'dedir.

- `AppDbContext` (`3K.Infrastructure/Data/AppDbContext.cs`) yalnız `DbSet`'leri ve `OnModelCreating` içinde alan uzantılarını çağırır: `ConfigureFinansModule`, `ConfigureAmbalajUretimModule`, `ConfigureSandikModule`… Eşleme her alanın `<Alan>ModelBuilderExtensions.cs` dosyasındadır (ör. `FinansModelBuilderExtensions.cs`). Bazı entity'lerin `DbSet`'i yoktur (ör. `FinansParaBirimi`, `FinansParametre`); `context.Set<T>()` ile erişilir.
- `GenericRepository<T>` semantiği — **izleme (tracking) çakışmalarına dikkat**:
  - `FindAsync(predicate)`, `GetAllAsync()`, `GetAllWithIncludeAsync()` → `AsNoTracking` (izlenmeyen nesne döner).
  - `GetByIdAsync(id)` → `DbSet.FindAsync` (izlenir).
  - `Queryable()` → izlenen `IQueryable`.
  - `Update(entity)` → `DbSet.Update` (bütün grafiği Modified işaretler). Aynı anahtarlı bir nesne zaten izleniyorsa (ör. `Queryable()`/`GetByIdAsync` ile yüklenmiş) ve `FindAsync`'den gelen ayrı bir örnek `Update` edilirse EF "already being tracked" hatası verir. İzlenen nesneyi doğrudan değiştirip `SaveChangesAsync` çağırmak genelde doğrusudur.
- `UnitOfWork.SaveChangesAsync` eşzamanlılık/benzersizlik/FK hatalarını `ConcurrencyConflictException`, `UniqueConstraintViolationException`, `ReferentialIntegrityConflictException`'a çevirir. Commit/rollback sonrası yan etkiler için `RegisterAfterCommit` / `RegisterAfterRollback`.
- Okumalarda büyük listeler DB'de filtrelenir, sayfalanır ve projekte edilir; `IReadQueryExecutor` kullanılabilir.
- **Büyüyebilen her liste sunucuda sayfalıdır** (ekran tablosu, istatistik kırılımı, geçmiş/olay listesi, rapor önizlemesi): DB'de süzülür ve sıralanır, `Skip/Take` ile yalnız istenen sayfa okunur, toplam ayrı `Count` ile döner. Yanıt `SayfaliSonuc<T>` (`3K.Core/Models/SayfaliSonuc.cs`; `SayfaKurali`: varsayılan 25, en fazla 100, sınır dışı sayfa son sayfaya çekilir); ön yüzde `PaginatedList<T>` + `app-server-pager`. Bütün satırları belleğe alıp sayfalamak ya da ön yüzde dilimlemek yok. Sayfasız liste yalnız doğası gereği küçük kümelerde kabul edilir (lookup, sabit üst-N özet, tek kaydın alt satırları). Excel/PDF dışa aktarımı bütün satırları yazabilir ama bir üst sınır ve "ilk N satır" bilgisi taşır. Örnek: `SandikDurumIstatistikService` (özet tek istek, tablolar sekme başına sayfalı).
- `Program.cs` `Npgsql.EnableLegacyTimestampBehavior` anahtarını açar; tarih dönüşümü eklemeden önce akışın kullanımını kontrol et.
