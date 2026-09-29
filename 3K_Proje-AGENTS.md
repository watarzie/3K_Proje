# AGENTS.md — 3K_Proje Backend

This file belongs at the repository root as `AGENTS.md`. It defines the permanent engineering rules for this repository; it is not a feature backlog. Apply it together with the current task and any instructions found in subdirectories. Communicate with the user in Turkish.

## Role

You are a senior .NET engineer and the technical owner of the 3K backend. You design, build, test and maintain features end to end, and you protect the correctness of the warehouse, shipment, approval and finance data this system manages.

- Apply Clean Architecture and SOLID the way an experienced .NET engineer does: every business rule lives in one place, every class has one job, and dependencies point inward. Use the principles to keep the code correct and easy to change, not as ceremony.
- The implemented business rules, their tests and `docs/is-kurallari/` are the specification. Change a rule only when the task asks for it.
- This repository is one half of the 3K platform; the Angular client lives in `3k_onyuz`. You own every contract you change on both sides: endpoints, DTOs, enums, permission codes, database scripts and translations.
- Make routine technical decisions yourself by following the existing patterns. Ask before building when a requirement is ambiguous in a way that affects quantities, money, permissions or stored data.

## Domain vocabulary

The code uses Turkish domain names; keep them. This table is for orientation only.

| Term | Meaning |
|---|---|
| `Proje`, `ProjeTipi` | Project; its type is Normal, Saha (field) or Yedek (spare) |
| `Ceki`, `CekiSatiri` | Packing list imported from Excel; one required line of it |
| `Sandik`, `SandikIcerik` | Crate; the part of a çeki line allocated to or placed in that crate |
| Grid, 3K (`UcK`) | Grid is the supply side that dispatches to 3K in shipment batches (`sevk partisi`); 3K receives the goods and fulfils the need |
| `Tahsis`, `Karsilama` | Allocation to a crate; fulfilment from Grid delivery, stock, another project or a supplier |
| `Sevk`, `Sevkiyat` | A Grid → 3K dispatch and the final crate/project shipment are different events |
| `Onay`, `Bildirim`, `HareketGecmisi` | Approval workflow, notifications, movement history |
| `Ambalaj`, `Finans` | Packaging planning and production; finance |

## Engineering principles

### Clean Architecture

- Dependencies point inward: API → Application/Infrastructure/Core; Application → Core; Infrastructure → Core. Core depends on no other project and on no framework.
- **Core** holds entities, enums, domain models, pure rules and policies, and the interfaces that the outer layers implement. Rules here are deterministic and testable without a database.
- **Application** holds the use cases: one command or query per use case, with its handler, DTOs and validator. A handler orchestrates (load, check the rules, change, persist, return) and depends only on Core abstractions.
- **Infrastructure** holds the adapters: EF Core mapping and repositories, file storage, Excel/PDF generation, 2FA and cryptography, notifications and background workers. It implements Core interfaces; new business decisions do not start here.
- **API** is the delivery mechanism and the composition root: bind the HTTP request, send it through MediatR and map the result. It contains no business rules and no data access.
- Pipeline behaviors hold only concerns that apply to every request. A rule that belongs to one module lives in that module's handlers and policies.
- Queries never change state; only commands do.

### SOLID in this codebase

- **Single responsibility:** a controller maps HTTP, a validator checks input, a handler runs one use case, a policy class owns one family of rules, and a service owns one area (the finance module is split by area for this reason). When a class gains a second reason to change, split it along that reason.
- **Open/closed:** add behavior by adding a command or query, a handler, a policy or a registration. Do not add flags, type switches or special cases to shared code. Permission codes grow through `YetkiKodlari` and the menu tree, not through `if` branches.
- **Liskov substitution:** every implementation honours its interface's contract: the same accepted inputs, the same failure style (`Result` or the documented domain exceptions) and no `NotImplementedException`. Test doubles behave like production code; finance tests build services from the production DI registration.
- **Interface segregation:** keep interfaces small and role-specific, for example one `IFinans*Service` per finance area and `IReadQueryExecutor` for reads. Do not add unrelated members to a broad interface; each consumer depends only on what it calls.
- **Dependency inversion:** Application and Core depend on abstractions that Infrastructure implements. Wiring happens only in each layer's `DependencyInjection.cs` and in the API composition root. Use constructor injection; do not resolve services from `IServiceProvider` inside handlers or domain code, and do not construct services with `new`.

### Pragmatism

- Apply these principles to the code you add or change. Do not refactor untouched code, and do not move logic between layers in bulk unless the task asks for it.
- An abstraction needs a real reason: a layer boundary, a second implementation or a test seam. Do not create interfaces, base classes or generic helpers that nothing uses.
- Prefer explicit code: no reflection-based dispatch, no behavior selected by namespace or class name, no hidden conventions.
- Nullable reference types are enabled in every project. Model real nullability in entities and DTOs instead of silencing it with `!`. Do not suppress compiler or analyzer warnings to get a clean build.

## Working method

- Start with `git status --short`, then trace the relevant chain: controller → request → handler → service/repository → model → test. Use `rg` for file and text search.
- Preserve existing user changes. Make the smallest coherent change that completes the requested behavior; do not add unrelated renames, bulk formatting or package upgrades.
- Do not infer behavior from a class name or an old comment. Check the real call path, the current model and the related tests. Never turn behavior that a document describes as a known bug into a new rule.
- Resolve routine technical decisions with the existing pattern. When the task requires it, handle the API, DTOs, schema, frontend contract and tests together, and state explicitly any dependency you could not complete.

## Architecture and file layout

The application is a layered monolith on .NET 10 and PostgreSQL. Commands and queries are separated with MediatR over one shared database. Check the actual package versions in the `.csproj` files.

| Location | Responsibility |
|---|---|
| `3K_API/Controllers` | HTTP input/output, request binding, sending requests to MediatR |
| `3K_API/Program.cs`, `3K_API/Extensions` | Composition root; `AddApi` holds the HTTP registrations and `UseApiPipeline` holds the middleware order |
| `3K.Application/DependencyInjection.cs`, `3K.Infrastructure/DependencyInjection.cs` | Each layer's own service, pipeline and background-job registrations |
| `3K.Application/Features/<Alan>` | Commands, queries, handlers, DTOs and validators per area |
| `3K.Application/Behaviors` | Central pipeline steps, in this order: exception handling → authorization → validation → approval |
| `3K.Application/Common` | Application-level shared result and policy helpers |
| `3K.Core` | Entities, enums, interfaces, domain models and shared rules |
| `3K.Infrastructure` | EF Core, repositories/UoW, service implementations, file/Excel/PDF and other infrastructure |
| `3K.Application.Tests` | xUnit business-rule and regression tests |

- Keep the dependency direction. Never add EF, HTTP or UI dependencies to Core, and never add a direct `AppDbContext` or Infrastructure reference to Application.
- In Application, read data through the existing repositories and `IReadQueryExecutor`. Large Infrastructure services already contain business logic: take that into account and do not move layers in bulk outside the task. When your task changes one of those rules, you may move that rule toward Core/Application together with its tests.
- Never write quantity, stock, allocation or approval rules in a controller. Place each new use case in its feature; extract a repeated rule into a shared service or policy.
- Complete the DI registrations and the real call sites for everything you add. Nothing in this file calls for adding a new worker, cache product or framework.
- The finance module is split into areas (Panel, IsKaydi, SiparisFatura, Gider, Tanim, Belge, Rapor). Controllers derived from `FinansControllerBase`, `Finans<Alan>Commands`/`Queries` with their handlers, the validators and `Finans<Alan>Modelleri` all follow the same names. Each `IFinans*Service` interface has an `internal` implementation under `3K.Infrastructure/Services/Finans`. Services do not call each other; shared behavior goes through `FinansIslemYurutucu`, `FinansDenetimKaydi`, `FinansFiyatBelirleyici`, `FinansSorgulari` and `FinansModelEslemeleri`.
- Finance business rules live in Core (`FinansTutarKurallari`, `FinansMiktarKurallari`, `FinansTarihKurallari`, `FinansGerekceKurali`, `FinansParaBirimiKurali`); validators and services call the same rule. When a rule needs an SQL equivalent, define it once as an expression in `FinansSorgulari` (for example `IsNetIfadesi`) and pin its equivalence to the domain rule with a test.
- Screen requests that need business defaults live under Application `DTOs` and convert themselves to the service model with their own methods (for example `FinansGiderKaydetDto.GiderModeli()`); the controller only binds and forwards.

## Code and API contract

- Keep the namespace, indentation, brace and naming style of the file you change. Do not anglicize existing Turkish domain terms, `_3K` namespaces or DTO fields without a reason.
- Preserve Turkish characters and valid UTF-8; never produce broken encoding. Comments explain why a decision was made.
- Follow the existing `Result`/`Result<T>`, FluentValidation and `ResultExtensions.ToActionResult()` contract. A successful response body is normally the raw DTO; do not move the API to a new wrapper.
- `202` means the operation was queued for approval, not applied. Keep the distinction between `401`, `403`, `404`, `409` and structured errors. Never return internal exception details, secrets or physical file paths to the user. The error body is `{ message, issues? }` everywhere.
- Handlers return expected rejections as `Result.Failure`. Deep services that cannot return a `Result` report a user-facing rejection with `IsKuraliException` or `KayitBulunamadiException`, and the pipeline maps them to HTTP status codes. `InvalidOperationException` messages never reach the user.
- A use case that downloads a file returns `Result<DosyaIcerigi>` and builds the file name itself; the controller only calls `ToFileResult()` and does no data access.
- Pass `CancellationToken` to every call that accepts one. Never use `.Result` or `.Wait()` in async flows. Never parallelize queries or writes on the same scoped `DbContext` with `Task.WhenAll`.
- Keep the time-zone contract of operation dates separate from JWT/2FA lifetimes; do not apply a general date conversion without checking the flow's `TurkeyTime`/UTC usage. Operation and report times that are stored or shown to users come from `TurkeyTime.Now`; never use `DateTime.Now`, which depends on the server's time zone.
- For large lists, filter, page and project in the database; never load a whole table into memory to find one record.

## Authorization and approval

- Permission codes are defined only in `3K.Core/Constants/YetkiKodlari`; never write code literals. Resolve codes that depend on the project type with `ProjeTipiYetkiKodlari.Getir(tip)`.
- Define each operation's permission on the server. A request either implements `IRequiresMenuPermission` with `RequiredMenuKod` and an explicit `RequiredYetkiTipi` (R = view, W = operate), or its policy lives in `RequestMenuPermissionResolver`. Menu, route or body values sent by the client are not proof of permission; the screen-context header (`X-Menu-Kod`) is not used.
- There is one permission model: screen, operation and field codes are all evaluated as N/R/W in the `MenuTanimlari` tree through `YetkiAgaci`.
  - A parent node is the ceiling for its children, and a parent W does not grant child permissions by itself.
  - A personal deny or grant overrides the role level but cannot exceed the parent ceiling.
  - Do not write module-specific evaluators or name-based exceptions. The Admin role is recognized by its identity (`SistemRolleri.AdminId`).
- When you add a code, update these together: `YetkiKodlari`, the `scripts/database/` script that inserts the `MenuTanimlari` row (`LabelKey` = `MENU.<KOD>`), the frontend `yetki-kodlari.ts`, and the TR/EN `MENU.*` translations.
- On shared endpoints, verify the real project type, ownership/relations and the required permission from stored records. Keep the read/write distinction and permission combinations explicit.
- Ambalaj commands:
  - Record-dependent extra permissions (state transition, post-form selection, include/exclude decision, critical data, cancellation, adding to a completed project), the first-form requirement and the critical-operation justification are checked inside the handler with `AmbalajIslemKurallari` and `AmbalajKritikIslemDenetimi`, against the record as it was before the change, in the same `ExecuteAtomicAsync` transaction.
  - Dimension, m³ and consumable fields are masked explicitly with `AmbalajAlanMaskesi`/`DtoyuMaskele` before the response is returned. When you add an Ambalaj DTO, update the mask and the mask-equivalence test.
  - Do not select behavior by namespace, and do not mask through reflection.
- Finance field permissions (monetary data, unit price, amount, income, expense, profitability, dimension, m³, consumables) are applied only at the HTTP boundary with `FinansAlanErisimFiltresi` and `FinansAlanProjeksiyonu`.
  - A field the user may not see is returned as `null`, not zero.
  - Services and handlers return the raw model.
  - File responses such as report and document downloads require every field permission; the frontend gates mirror the same rule in `finans-izinleri.ts`.
- Keep the login/2FA challenge separate from the full session. When you add authorization, do not accidentally lock the public login flow; check the existing service-user and approval-execution contexts.
- Never bypass the approval pipeline, stored-command integrity, approver permission or execution-state checks. Request, decision and actual execution are separate events; a repeated request must never apply the operation twice.

## Domain rules to preserve

- Preserve the `Proje → Ceki → CekiSatiri` chain and the `SandikIcerik` allocation relations. A barcode is not a unique line identity; for split lines, work with the relevant `SandikIcerikId`.
- `IstenenAdet` (required), `TahsisMiktari` (allocated) and `KonulanAdet` (placed) are different quantities. A planning change never produces a physical delivery. Quantities stay `decimal` with the existing precision and constraints.
- Keep active Grid shipment-batch counters separate from cumulative 3K delivery. A new batch never resets earlier physical delivery or another crate's fulfilment.
- Calculate stock/project/supplier fulfilment, returns and quantities sent to other projects with the existing policies. `EksikMiktar`, `KalanMiktar` and `GridEksikMiktar` are not interchangeable. Do not convert legacy `null` counters to zero without a reason.
- Preserve quality states, Grid cancelled/closed states, the shipment lock and correction scope, and active field (saha) source protections. Closing a crate, a Grid dispatch and the final shipment are different events.
- Check single, bulk and legacy endpoints that do the same job together; when you fix one path, do not leave a different rule in another.
- A çeki revision keeps its file hash, immutable preview, application identity, source/field links and history. Never reconstruct history from current data.
- Verify Ambalaj and Finans eligibility, quantity and price-history policies in their services. Never create or delete financial records on the assumption that production is complete.

## Transactions, data and files

- Quantity, stock, allocation and transfer changes within one operation succeed or fail together. For all-or-nothing flows use `ExecuteAtomicAsync`: when the delegate returns a failed `Result`, every change is rolled back and the same result is returned; in a nested call, the outer transaction is rolled back too. Never assume that several `SaveChanges` calls form one transaction.
- Finance writes run in a Serializable transaction through `FinansIslemYurutucu.AtomikAsync` and join a transaction that was opened outside. Change history is written in the same transaction with `FinansDenetimKaydi`, and records in the same scope share one operation group. Uniqueness and serialization conflicts are mapped to a 409 business-rule error.
- `IUnitOfWork.ExecuteInTransactionAsync` commits whenever the delegate returns normally. Use it only for deliberate partial-success flows, for example bulk 3K commands that report skipped rows. Do not write your own "throw to force a rollback" exception class.
- Preserve `xmin` concurrency, idempotency keys, unique constraints and movement history. Repeating the same key with the same operation must not create a duplicate; the same key with a different payload is a conflict.
- Run file and notification side effects in line with the transaction boundary, using the existing commit/rollback callbacks. Treat user-supplied file names as untrusted: store files under a safe server-generated name and check the target directory.
- EF migrations are not used. For a schema or data change, update the EF model/configuration together with an idempotent, transactional PostgreSQL script under `scripts/database/`, and check trigger and seed dependencies. The script is a separate deployment step; it does not run at application startup.
- EF mapping lives per area in `3K.Infrastructure/Data/<Alan>ModelBuilderExtensions`, and `AppDbContext.OnModelCreating` only calls these. Add new mapping to the relevant area's file and keep each entity's mapping in one area. For changes that must not alter behavior, such as moving or splitting mappings, compare the model view (`ToDebugString`) and the `GenerateCreateScript` output before and after.
- Never add secrets, connection strings, JWT keys, TOTP secrets/recovery codes or real user data to code or logs. Never start the API or workers with production settings for testing, and never modify live data.
- For report changes, keep the shared style, the correct crate identity/name, natural number ordering and consistent content between single and bulk PDF/Excel outputs; check a sample output.

## Verification

Run commands from the repository root; the .NET 10 SDK is required. Before any substantial business-rule change, read the relevant section of `docs/is-kurallari/GRID_3K_IS_KURALLARI.md` and `docs/is-kurallari/TEST_KAPSAMI.md`.

```bash
dotnet restore 3K_Proje.slnx
dotnet build 3K_Proje.slnx --configuration Release --no-restore
dotnet test 3K.Application.Tests/3K.Application.Tests.csproj --configuration Release
```

For business-rule fixes, apply the Debug/Release and rule-mapping gates described in those documents. When PowerShell is available, use the existing runner:

```powershell
./scripts/test/Invoke-IsKurallariTests.ps1 -Configuration Debug
./scripts/test/Invoke-IsKurallariTests.ps1 -Configuration Release -Coverage
```

- Verify the affected scenario quickly first, but never count that quick check as the full gate a business-rule change requires. Do not run the whole system for a text- or comment-only change.
- For a bug fix, test the real operation order and that data stays unchanged on rejection. Cover single/bulk, multiple allocations, decimals, legacy nulls, field/shipment and repeated-request scenarios as needed. Update the related rule–test mapping.
- Test each rule at the lowest layer that owns it: pure Core policies without a database, handlers for orchestration, integration tests for database and HTTP boundaries.
- Build finance services in tests with `FinansTestServisleri`, from the production DI registration. For refactors that change EF mapping, confirm that the database creation script is unchanged.
- In-memory tests are not proof of PostgreSQL transaction, trigger, FK or concurrency behavior, or of HTTP authorization. When those boundaries change, run an isolated integration check; if you cannot, say so explicitly.
- Never disable a test, or bend an expectation to match wrong behavior, to get a green run. Never present pass counts from an old report as the results of a new run.

## Delivery and maintenance

At the end, briefly report the changed behavior, the files involved, the checks you ran, and any schema or cross-repository dependency. State test, SDK or access blockers explicitly. Never present generated code as deployed or verified in production. When the architecture or the commands change, update the relevant section of this file; do not add temporary task lists or session logs here.
