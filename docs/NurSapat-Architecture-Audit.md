# NurSapat Architecture Audit

Дата аудита: 2026-07-27  
Solution: `NurSapat.sln`  
Состояние: текущее рабочее дерево, включая незакоммиченные изменения  
FigJam: [NurSapat Current Change Map](https://www.figma.com/board/YauCcjX15N6Cbh5WDhGZBW/NurSapat-Current-Change-Map?node-id=0-1)

## 1. Executive Summary

NurSapat — desktop-приложение Avalonia 12 на .NET 8, объединяющее CAD-конструктор оконных изделий, ERP/заказы, PostgreSQL persistence, автономный SQLite-кэш, Supabase Auth/Sync и собственный updater.

Проверено 1409 файлов вне `bin`, `obj`, `packages`, `publish`, `updater_build` и zip-архивов. Обнаружено 13 `.csproj`; 12 входят в solution, а `NurSapat.Cad.Core.Tests` существует, собирается и проходит тесты, но отсутствует в `NurSapat.sln`.

Повторная сборка `dotnet build NurSapat.sln --no-restore /p:UseSharedCompilation=false /m:1` успешна: 0 ошибок, 0 предупреждений. Первый параллельный запуск получил CS2012 из-за занятого `NurSapat.Cad.UI.dll`; это была коллизия процесса компиляции, а не ошибка исходников.

Обнаружено 2 Critical и 6 High проблем. Главные:

- пароль PostgreSQL зафиксирован в tracked `appsettings.json` и продублирован fallback-строкой в `Program.cs`;
- любой успешный Supabase-пользователь получает локальные права `UserPermissions.CreateAdmin()`;
- Remember Me сохраняет пароль в локальный JSON без защиты;
- access/refresh tokens сериализуются в `%APPDATA%/NurSapat/supabase-session.json` без OS-level encryption;
- offline hash использует один salted SHA-256 вместо password KDF;
- updater проверяет хеши, но не цифровую подпись издателя;
- `CadEditorViewModel.cs` содержит 9167 строк, а invalidation/recalculation распределены по нескольким слоям.

Файлы в рамках аудита не удалялись.

## 2. Solution Structure

| Проект | Тип / TFM | Назначение | Прямые ProjectReference | Статус |
| --- | --- | --- | --- | --- |
| `NurSapat.App` | WinExe / `net8.0-windows` | Avalonia composition root, ERP UI, документы | Core, Cad.UI, Data, Persistence, Sync, Updates, Updater build-only | активен |
| `NurSapat.Core` | library / `net8.0` | домен, costing, contracts, serialization | нет | активен |
| `NurSapat.Cad.Foundation` | library / `net8.0` | чистое CAD-ядро: scene, commands, events, selection, viewport | нет | активен |
| `NurSapat.Cad.Core` | library / `net8.0` | CAD-модели, geometry, document | Foundation, Core | активен |
| `NurSapat.Cad.Tools` | library / `net8.0` | ICadTool и реализации инструментов | Cad.Core | активен |
| `NurSapat.Cad.UI` | library / `net8.0` | Avalonia canvas, rendering, dialogs, CAD VM | Core, Cad.Core, Cad.Tools | активен |
| `NurSapat.Data` | library / `net8.0` | legacy Dapper/Npgsql repositories и SQL migration | Core | legacy, но используется |
| `NurSapat.Infrastructure.Persistence` | library / `net8.0` | EF Core PostgreSQL | Core | активен |
| `NurSapat.Infrastructure.Sync` | library / `net8.0` | SQLite offline cache, Supabase auth/sync | нет | активен |
| `NurSapat.Infrastructure.Updates` | library / `net8.0` | version/release/download/validation/install | Core | активен |
| `NurSapat.Updater` | Exe / `net8.0-windows` | замена файлов и restart | Updates | активен |
| `NurSapat.Infrastructure.Persistence.Tests` | xUnit / `net8.0` | persistence/integrity/snapshot tests | Persistence | в solution |
| `NurSapat.Cad.Core.Tests` | xUnit / `net8.0` | CAD geometry regression tests | Cad.Core | **не в solution** |

Не обнаружены `Directory.Build.props`, `Directory.Build.targets`, `global.json`, `NuGet.config`, launch settings, publish profiles или CI/CD YAML.

## 3. Architecture

Фактические слои:

`Avalonia Views → ViewModels/Services → Core/CAD → Persistence/Sync/Updates → PostgreSQL/SQLite/Supabase/GitHub`

Положительные свойства:

- `Cad.Foundation` не зависит от Avalonia и других проектов;
- PostgreSQL implementation находится в отдельном infrastructure-проекте;
- DB access в новом слое идёт через `IDbContextFactory`;
- CAD tools вынесены в отдельный проект.

Нарушения и смешение:

- `NurSapat.App` напрямую ссылается на все infrastructure-проекты и legacy `NurSapat.Data`;
- `App.ServiceProvider` является глобальным service locator;
- `WorkspaceWindowService` и ряд window services разрешают зависимости через `IServiceProvider`;
- `Program.cs` создаёт `DatabaseService` через `new`;
- App содержит persistence adapters и одновременно бизнес-оркестрацию;
- доменные, App-, persistence- и legacy-модели заказов/материалов/пользователей сосуществуют.

## 4. Application Startup

Подтверждённая последовательность:

1. `NurSapat.App/Program.cs:Main()` создаёт `ServiceCollection`.
2. `ConfigureServices()` читает config и регистрирует сервисы.
3. `BuildServiceProvider()` создаёт root provider.
4. `BuildAvaloniaApp(provider)` создаёт `App(provider)`.
5. `App.OnFrameworkInitializationCompleted()` разрешает `MainWindow`.
6. Fire-and-forget запускается `InitializeOfflineSyncAsync()`.
7. Инициализируются SQLite schemas, восстанавливается Supabase session, стартует background synchronizer.
8. `UpdateStartupCoordinator.Initialize()` обрабатывает update startup.
9. `MainWindowViewModel` показывает `LoginViewModel`.
10. После успешного входа `ApplicationPersistenceInitializer.EnsureReadyAsync()` готовит PostgreSQL.
11. `WorkspaceWindowService.OpenWorkspace()` открывает `MainWorkspaceWindow`, запускает autosave и recovery.
12. На exit `IBackgroundSynchronizer.StopAsync().GetAwaiter().GetResult()` выполняет sync-over-async.

## 5. Authentication

UI: `LoginView.axaml`, `LoginViewModel`. Поля: email, password, Remember Me.

Online flow:

`LoginViewModel.LoginAsync → SupabaseAuthService.SignInAsync → Supabase GoTrue → CachedUserRepository.UpsertSuccessfulLoginAsync → EnsureReadyAsync PostgreSQL → IUserSessionService → MainWorkspaceWindow`.

Offline flow:

при сетевой ошибке `TryAuthenticateOfflineAsync` проверяет SQLite-кэш пользователя через `OfflinePasswordHasher`. Offline login доступен только после успешного online login на этом устройстве.

Сессии:

- Supabase access/refresh token сохраняются `FileSessionPersistence` в `%APPDATA%/NurSapat/supabase-session.json`;
- активный App user хранится в singleton `UserSessionService`;
- logout очищает local session и best-effort вызывает Supabase sign-out.

Права:

роль не загружается из Supabase claim или PostgreSQL. `LoginViewModel.cs:251` создаёт `UserPermissions.CreateAdmin()` для любого успешного входа. Это не полноценная безопасная авторизация.

## 6. Database

Основной provider по текущему config: PostgreSQL через EF Core 8/Npgsql. Дополнительно используются:

- SQLite `offline-cache.db` для offline materials/orders/users/release cache;
- Supabase для Auth и remote sync;
- legacy Dapper/Npgsql layer в `NurSapat.Data`;
- JSON-файлы для settings, documents, templates, recent files и autosave.

`NurSapatDbContext` содержит 29 `DbSet`: customers, suppliers, orders, order constructions, geometry, cost lines, materials/prices, construction elements/series/rules, stock, currency, users, templates, settings, calculation snapshots, price lists, technology/production entities.

EF migrations:

1. `20260711104048_InitialCreate`
2. `20260713043715_AddCurrencyRates`
3. `20260714083509_PersistenceCutover`
4. `20260726161217_AddMaterialSuppliersText`
5. `20260727030844_AddProductionDomainModel`

Legacy SQL: `NurSapat.Data/Migrations/InitialSchema.sql`; Supabase SQL: `NurSapat.Infrastructure.Sync/Scripts/supabase-schema.sql`.

Недостатки: нет retry policy, нет явного application Unit of Work, EF multi-step workflows не объединены общей явной транзакцией, legacy и EF schemas существуют параллельно.

## 7. UI Navigation

- `MainWindow.axaml` + `MainWindowViewModel`: login/registration shell.
- `ViewLocator` применяет reflection-конвенцию `ViewModel → View`.
- `MainWorkspaceWindow.axaml` + `MainWorkspaceViewModel`: рабочая оболочка.
- `OrderWorkspaceViewModel`: координирует CAD, order sheet, lifecycle и calculation.
- `WorkspaceNavigationService` переключает Drawing, Order sheet, Additional и сервисные разделы.
- `DictionaryWindowService`, edit/window services открывают словари и dialogs.

Обнаружено 118 product View/Dialog XAML и 142 ViewModel source files. Большинство root views используют `x:DataType`; несовпадение имён не является достаточным доказательством dead code из-за nested controls, dialog services и reflection.

## 8. CAD Architecture

Основной runtime:

`CadEditorView.axaml → CadEditorViewModel → ICadTool/CadToolFactory → FrameNode/GeometryElements → render helpers → CadCanvasControl`.

Ключевые файлы:

- `CadEditorViewModel.cs` и partial-файлы — состояние editor, selection, tools, geometry mutation, history;
- `FrameNode.cs` — CAD tree, geometry и snapshots;
- `CadCanvasControl.cs` — pointer/keyboard input, hit-test, render invalidation;
- `GeometryElementRenderer.cs`, `FrameNodeRenderer.cs`, `FrameVolumeRenderer.cs` — отрисовка;
- `ConstructionMemento`/snapshot helpers — undo/save.

`CadEditorViewModel.cs` — GOD VIEWMODEL: 9167 строк только в основном partial.

## 9. CAD Tools

`CadToolKind` содержит 28 зарегистрированных рабочих значений без `None`. В каталоге найден 31 concrete `*Tool.cs`, включая auxiliary/legacy placement tools.

Категории: selection, profile/vertex selection, alignments, dimensions, impost offsets, split profile/arc, radius/arc editing, delete/clear/undo, recalculate, properties/details, copy/paste, standard templates.

Flow:

`XAML Command Binding → ActivateCadTool/SelectToolModeAsync → CadToolFactory → ICadTool Begin/Update/Complete → FrameRoot mutation → render invalidation`.

`CadToolbarLayout` и `CadToolbarRegistry` оба активны. Это кандидаты на консолидацию, но не на автоматическое удаление.

## 10. Geometry and Rendering

Крупнейшие файлы:

- `CadEditorViewModel.cs` — 9167 строк;
- `GeometryElementRenderer.cs` — 3982;
- `SashGeometry.cs` — 3266;
- `FrameNode.cs` — 2808;
- `FrameNodeRenderer.cs` — 1487;
- `CadCanvasControl.cs` — 1435.

В App/Cad.UI найдено 63 прямых вызова `InvalidateVisual()`. Частичное coalescing есть в `CadCanvasControl.ScheduleInvalidateVisual`, но другие ветви по-прежнему инвалидируют напрямую. Единого transaction-like pipeline `mutate → recalculate → validate → invalidate once` нет.

## 11. Document Lifecycle

- `DocumentManager`: new/open/save/save-as, dirty state, autosave lifecycle.
- `ProjectSerializer`: JSON load/save `ProjectDocumentData`.
- `CadProjectSnapshotHelper`: capture/restore CAD state.
- `AutoSaveService`: timer-based autosave.
- `AutoSaveRecoveryWindow`: пользовательское восстановление.
- `ConstructionDrawingStore` и `OrderProjectMapper`: drawing JSON ↔ order/project.

Serialization и reflection были проверены перед формированием deletion candidates. DTO, Avalonia resources и `ViewLocator`-создаваемые View нельзя считать unused по одним compile-time references.

## 12. ERP Workflow

Order aggregate:

`OrderWorkspaceViewModel → OrderSheetViewModel/CadEditorViewModel → OrderLifecycleService/OrderCalculationService → Snapshot services → OrderRepository`.

PostgreSQL relationships:

`Customer → Order → OrderConstruction → GeometryElement/CostSpecificationLine`; `Order → CalculationSnapshot → PriceListVersion`; production: batches, reservations, postings, cutting jobs.

Новый production domain присутствует в текущем рабочем дереве и migration, но UI и end-to-end operational workflow ещё не замкнуты.

## 13. Domain Models

Существуют четыре семейства моделей:

1. `NurSapat.Core.Models` — целевые domain/contracts;
2. `NurSapat.Cad.Core.Models` — CAD runtime;
3. `NurSapat.App.Models` — UI/store DTO;
4. `Infrastructure.Persistence.Entities/Models` и `Data.Legacy` — storage/legacy.

Ручные mapping layers: `ErpMappers`, `AppPersistenceMappers`, `OrderProjectMapper`. Риск — drift полей и семантики между моделями.

## 14. Dependency Injection

В `Program.ConfigureServices()` статически найдено 112 `AddSingleton`, 70 `AddTransient`, 0 `AddScoped`. Дополнительные регистрации находятся в extension methods Persistence/Sync/Updates.

Риски:

- слишком много mutable singleton ViewModel/state;
- static `App.ServiceProvider`;
- `IServiceProvider` внутри window services;
- `DatabaseService` создаётся вручную;
- один root provider не освобождается явно.

## 15. Configuration

Config sources:

- `NurSapat.App/appsettings.json`;
- environment variables;
- AppData settings/profile JSON;
- update settings JSON;
- offline SQLite path;
- autosave/recent-files paths.

Strongly typed options: `PersistenceOptions`, `SyncOptions`, `UpdateOptions`, `EnvironmentSettings`.

Environment-specific `appsettings.*.json` и feature flags не обнаружены. Секреты нельзя хранить в FigJam или отчёте; найденный пароль намеренно не повторяется здесь.

## 16. Update System

Pipeline:

`Source → Build/Publish → ZIP + version manifest → GitHub Release → UpdateService → staging → UpdateValidator hashes → Updater.exe → backup/replace → restart/rollback`.

Имеются SHA-256/SHA-512 checks, staging, protected user data, backup/rollback. Не обнаружены проверка цифровой подписи package/updater/manifest, publisher trust chain и CI/CD release pipeline.

## 17. Unused Code

| Кандидат | Доказательство | Риск | Рекомендация |
| --- | --- | --- | --- |
| `NurSapat.Cad.Core/Templates/CadTemplateSerializer.cs` | весь type закомментирован; 0 runtime/XAML/DI refs | low | удалить и проверить build/tests |
| `build_output.txt` | tracked stale build log, не используется runtime | low | удалить |
| `ccalc_extract.txt` | tracked, 0 bytes, 0 refs | low | удалить |
| `_encode_verify.txt` | tracked diagnostic artifact, 0 refs | low | удалить |
| `Ruler` | 0 bytes, нет в projects | low | удалить |
| `NurSapat-win-x64.zip` | release archive 85.9 MB в root | low | держать в releases, не source tree |
| `publish/` | ignored publish output | low | очистить локально при необходимости |
| `updater_build/` | updater build output | low | очистить локально при необходимости |
| `Versions/` | release/version artifacts | medium | перенести в release storage после проверки |
| `ArcContourTest/` | только `bin/obj`, нет csproj/source и `.sln` entry | medium | ручная проверка, затем удалить |
| `Распакованные формы/` | 0 ProjectReference, Delphi/DBF reference corpus | high | **ТРЕБУЕТ РУЧНОЙ ПРОВЕРКИ**, не удалять автоматически |

Итоговый счёт удаления: 10 candidates; legacy corpus вынесен отдельно как manual-review evidence set.

## 18. Technical Debt

| Severity | Проблема | Evidence | Исправление |
| --- | --- | --- | --- |
| Critical | DB credential в repo и fallback code | `appsettings.json:4`, `Program.cs:346` | rotate; удалить из git history; env/credential store |
| Critical | любой login получает admin | `LoginViewModel.cs:251` | server-derived claims/role table; deny-by-default |
| High | remembered password plaintext | `LoginViewModel.cs:107,237` | не сохранять пароль; OS credential vault |
| High | token session plaintext | `FileSessionPersistence.cs:34` | DPAPI/Keychain/libsecret |
| High | offline hash = salted SHA-256 | `OfflinePasswordHasher.cs:55` | Argon2id/PBKDF2 с параметрами и migration |
| High | unsigned updater trust chain | `UpdateValidator.cs:61-150` | подпись manifest/package/updater + pinned publisher key |
| High | GOD CAD ViewModel | `CadEditorViewModel.cs` | application services/state reducers/use-case handlers |
| High | fragmented CAD invalidation/async | 63 `InvalidateVisual`; fire-and-forget tool calls | atomic mutation/recalc/invalidate pipeline |
| Medium | sync-over-async | `App.axaml.cs`, `PersistenceSync.cs` | async shutdown and async-first APIs |
| Medium | no retry/UoW | Persistence DI/repositories | transient retry, explicit workflow transactions |
| Medium | test project missing solution | `NurSapat.Cad.Core.Tests.csproj` | add to `.sln` and CI |
| Medium | no CI/CD/crash reporting | repository inventory | reproducible pipeline and telemetry |

## 19. Testing Gaps

Фактический запуск:

- `NurSapat.Infrastructure.Persistence.Tests`: 3 passed;
- `NurSapat.Cad.Core.Tests`: 20 passed;
- всего: 23 passed, 0 failed, 0 skipped.

Solution-level `dotnet test NurSapat.sln --no-build` запускает только 3 persistence tests, потому что CAD tests отсутствуют в solution.

Без достаточного покрытия: OpeningType, sash/opening geometry, glazing/beads, full undo/redo, document roundtrip/recovery, complete costing/discount/payment, auth permissions/offline migration, updater trust/rollback, ViewModel/E2E, PostgreSQL integration и renderer golden tests.

## 20. Recommended Roadmap

| Этап | Priority | Задачи | Критерий готовности |
| --- | --- | --- | --- |
| A — security/cleanup | P0 | rotate/remove DB secret; server roles; protect passwords/tokens; password KDF; add CAD tests to solution; delete only proven artifacts | clean clone builds; 23+ tests run from solution; no secret in repo/history |
| B — architecture | P1 | убрать global service locator; уменьшить singleton state; разделить App/domain/infrastructure; унифицировать models/mappers | dependency rules enforced; composition root only |
| C — CAD stability | P1 | split GOD VM; single mutation/recalc/invalidation pipeline; stable undo/redo; geometry tests | deterministic geometry snapshots and one invalidation per transaction |
| D — ERP completion | P1/P2 | price lists, snapshots, reservations, postings, batches, cutting, payments/tax/reporting; explicit transactions | PostgreSQL integration scenarios pass |
| E — production readiness | P0/P1 | signed updater; CI/CD; migration/backup restore drill; structured logs/crash reporting; cancellation/retry | reproducible signed release and rollback drill |

## Verification Record

- All 13 `.csproj` inventoried; 12 are in `NurSapat.sln`.
- Solution build succeeded with 0 errors / 0 warnings after disabling shared compilation and serializing the build.
- 23 tests passed across both test projects.
- View/ViewModel links checked through `x:DataType`, code-behind, window services, reflection `ViewLocator` and DI.
- DI registrations checked in Program and all three extension roots.
- Reflection and JSON serialization usages checked before unused classification.
- EF migrations, model snapshot, legacy SQL and Supabase SQL checked.
- Git history checked for suspicious tracked audit/legacy candidates.
- FigJam updated with 21 sections, 84 evidence cards, local labeled connectors, ProjectReference diagram, startup/auth sequence, ERD and update pipeline.
- No files were deleted.

## Unconfirmed

- Runtime behavior against a real PostgreSQL/Supabase environment was not executed because external credentials and services were not part of the local test fixture.
- A production publish/install/rollback and cryptographic trust test was not available.
- Coverage percentage cannot be calculated because no coverage report was produced.
- `Распакованные формы/` may be intentional reverse-engineering/reference material and requires owner confirmation.
