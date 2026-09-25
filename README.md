# Dentalla MIS — SERVER first

Основной рабочий каталог: `C:\Projects\MIS Dentalla`.

## Текущий объект разработки
Сейчас разрабатывается **DENTALLA SERVER for Windows** — полноценная установка МИС, способная работать на единственном компьютере клиники.

```text
Windows Server PC
├─ Microsoft SQL Server
├─ Dentalla.Api / Server Host (Windows Service)
├─ Dentalla.Desktop Windows (локальный UI; позже)
└─ C:\ProgramData\Dentalla\Storage
```

Даже на одном физическом ПК действует только путь:

`Desktop -> API/SignalR -> Application -> Infrastructure -> SQL Server/files`

Прямого `Desktop -> SQL Server` нет.

## Что реализовано в Server Sprint 0.1
- server host готов к Windows Service hosting;
- конфигурация `DentallaServerOptions`;
- SQL Server через EF Core;
- локальное server-owned файловое хранилище с защитой от path traversal и SHA-256 при сохранении;
- каталоги `documents/media/audio/temp/exports`;
- `/health/live`;
- `/health/ready` с проверкой SQL + writeability storage;
- `/api/server/info`;
- SignalR `/hubs/updates`;
- единый exception boundary;
- начальная серверная модель безопасности:
  - `UserAccount`;
  - несколько ролей на одном аккаунте;
  - `PermissionDefinition`;
  - `RolePermission`;
  - персональные `UserPermissionOverride` Allow/Deny;
  - временный `DelegationGrant` с `ValidFrom/ValidTo`, scope и limit;
  - базовый Permission Catalog и role defaults для 5 системных ролей.

## Что намеренно ещё НЕ сделано
- логин/authentication;
- вычисление EffectivePermission;
- audit log;
- EF migration files;
- реальные patient endpoints;
- installer/service registration;
- LAN TLS/certificate provisioning.

Это следующие серверные инкременты.

## Solution
Для текущей server-first разработки:

`Dentalla.Server.slnf`

Подробности:
- `docs\DEPLOYMENT-TOPOLOGY.md`
- `docs\SERVER-RUNTIME.md`

## Сборка
Пока код подготовлен без фактической компиляции в текущей среде. На Windows development PC первым действием будет compile-only проверка Server filter; миграции БД создаются только после успешного build.
