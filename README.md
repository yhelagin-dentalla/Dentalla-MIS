# Dentalla MIS — server-first

Основной рабочий Git-каталог: `C:\Projects\MIS Dentalla git`.
Старый `C:\Projects\MIS Dentalla` сохранён как дополнительная резервная копия и не является основным checkout.

## Архитектура
Dentalla MIS строится как server-first on-premise система для локальной сети клиники:

```text
Windows Server / clinic PC
├─ Microsoft SQL Server
├─ Dentalla.Api / Server Host
├─ Dentalla.Desktop
└─ C:\ProgramData\Dentalla\Storage
```

Основной путь данных:

`Desktop -> API/SignalR -> Application -> Infrastructure -> SQL Server/files`

Прямого `Desktop -> SQL Server` нет.

Технологический стек текущей ветки: .NET 10, ASP.NET Core 10, EF Core 10, Avalonia 12.1.3, Microsoft SQL Server.

## Текущее состояние на 26.09.2026

### Server / Foundation
Реализованы:
- Windows Service-compatible server host;
- `DentallaServerOptions` и server-owned файловое хранилище;
- SQL Server через EF Core и versioned migrations;
- `/health/live`, `/health/ready`, `/api/server/info`;
- SignalR `/hubs/updates`;
- единый exception boundary;
- локальная server-authoritative модель безопасности;
- `UserAccount`, `StaffProfile`, несколько системных ролей на одном аккаунте;
- `PermissionDefinition`, `RolePermission`, `UserPermissionOverride`, `DelegationGrant`;
- bootstrap первого Director;
- login/logout и server-side `AuthSession`;
- opaque bearer token, в БД хранится только hash токена;
- расчёт `EffectivePermission` на сервере;
- immutable `AuditEvent` для чувствительных действий;
- нормализованный login directory из `UserAccounts + StaffProfiles + UserRoleAssignments`;
- development role-context switch для Director на loopback.

Системные роли первой версии:
- `Doctor`;
- `Administrator`;
- `Marketer`;
- `ChiefMedicalOfficer`;
- `Director`.

### Legacy migration — IDENT + GREIS
Архитектурное правило: legacy ID никогда не становится PK новой MIS. Связь источников хранится через source-aware `ExternalIdentifier`.

GREIS Phase 4–6 завершены и синхронизированы с Domain/EF:
- финальных GREIS appointments: **18 823**;
- `Encounter`: **15 836**;
- `PerformedService`: **30 799**;
- исторических `ServiceCatalogItem`: **293**;
- исторически оказанных работ: **92 524 139,00 ₽**;
- `TreatmentCourse`: **27**;
- связей `TreatmentCourseEncounter`: **35**;
- `TreatmentCoursePlannedService`: **5**;
- historical actual receipts: **2 387** canonical Patients;
- контрольная сумма `GREIS_ACTUAL_RECEIPTS_V1`: **93 880 212,50 ₽**.

GREIS financial history не реконструируется как старые долги, авансовые остатки или кассовый ledger. В нормализованной модели хранится source-aware historical receipt aggregate на Patient.

Документация миграции:
- `docs/GREIS-PHASE4A-AUDIT.md`;
- `docs/GREIS-PHASE4B-DESIGN.md`;
- `docs/GREIS-PHASE5A-TREATMENT-COURSE-AUDIT.md`;
- `docs/GREIS-PHASE5B-RECONCILIATION.md`;
- `docs/GREIS-PHASE6-ACTUAL-RECEIPTS.md`.

## Что пока остаётся development-only / не завершено
- API по умолчанию привязан к `127.0.0.1:5080`; LAN endpoints отключены;
- Desktop development-login пока работает без пароля и ещё не является production auth workflow;
- LAN TLS/certificate provisioning, rate limiting/lockout и MFA для удалённого доступа не завершены;
- installer/service registration и production deployment pipeline не завершены;
- GitHub CI/unit/integration test gate пока не создан;
- полноценный Patient Workspace API ещё не реализован;
- текущий `Patient` остаётся упрощённой migration/MVP-моделью; целевая модель `Person -> Patient` ещё впереди;
- текущий `Encounter` оптимизирован под историческую миграцию и перед новым clinical workflow должен получить корректный lifecycle открытого приёма и поддержку Encounter без обязательного Appointment;
- `ApplyDatabaseMigrationsOnStartup=true` допустим для текущего dev-контура, но не является целевой production deployment policy.

## Сборка и локальный запуск

```powershell
cd "c:\projects\mis dentalla git"
dotnet tool restore
dotnet build
.\run-server-dev.ps1
```

`run-server-dev.ps1` запускает `Dentalla.Api`, затем локальный `Dentalla.Desktop`.

Явное применение EF migrations при необходимости:

```powershell
dotnet ef database update `
  --project src\Dentalla.Infrastructure `
  --startup-project src\Dentalla.Api
```

## Solution
Для server-first разработки:

`Dentalla.Server.slnf`

Полное solution:

`Dentalla.sln`

Основные технические документы:
- `docs/START-HERE.md`;
- `docs/DEPLOYMENT-TOPOLOGY.md`;
- `docs/SERVER-RUNTIME.md`;
- `docs/AUTH-BOOTSTRAP.md`;
- `docs/DATABASE-BOOTSTRAP.md`.

## Source of truth
- **Notion** — архитектура, бизнес-правила, roadmap и критерии готовности.
- **GitHub `main`** — фактически реализованный код, EF model/migrations и executable migration scripts.
- При расхождении старого README/рабочего файла с кодом и актуальной Notion source-of-truth нужно сначала сверять `main` и соответствующую Notion-страницу, а не продолжать старое промежуточное решение автоматически.
