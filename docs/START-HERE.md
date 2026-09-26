# Dentalla MIS — START HERE

Актуально на 26.09.2026.

## Что уже работает в коде
- server-first архитектура `Desktop -> API/SignalR -> Application -> Infrastructure -> SQL Server/files`;
- SQL Server + EF Core migrations;
- server-owned file storage;
- health/server endpoints и SignalR;
- локальная server-authoritative authentication;
- bootstrap Director, login/logout, `AuthSession`;
- `EffectivePermission`, role defaults, user overrides и delegations;
- immutable audit log;
- нормализованный login directory из `StaffProfile/UserAccount/UserRoleAssignment`;
- development role-context switch Director;
- базовый Desktop shell;
- миграция IDENT/GREIS core data;
- GREIS appointments, Encounters, PerformedServices, treatment courses и historical receipt totals.

GREIS Phase 4–6 закрыты. Контрольные итоги:
- `PerformedService`: **30 799**;
- historical delivered work: **92 524 139,00 ₽**;
- `TreatmentCourse`: **27**;
- GREIS historical actual receipts: **93 880 212,50 ₽**.

## Patient Workspace — целевой первый production UI
Единый `Patient Workspace` остаётся первой полной пользовательской вертикалью. Информация не должна сваливаться на один экран: используется постоянный header и специализированные вкладки.

Целевые вкладки:
1. Обзор
2. План лечения
3. История болезни
4. Изображения
5. Коммуникации
6. Приёмы
7. Финансы
8. Документы

ФИО, телефоны, e-mail, адреса, паспорт, место работы, профессия, источник прихода и другие мастер-данные редактируются через управляемый Patient/Person workflow. Клинические факты изменяются только внутри соответствующего Encounter и по EffectivePermission. Финансовые факты не редактируются как обычные поля — только через операции, корректировки и аудит.

## Важный переходный долг перед новым clinical workflow
Текущие `Patient` и `Encounter` были достаточны для нормализации legacy-истории, но не являются окончательной runtime-моделью:
- `Patient` ещё упрощён и должен перейти к утверждённой модели `Person -> Patient` с отдельными контактами, адресами, документами и связями;
- `Encounter` должен поддерживать открытый lifecycle и возможность существовать без обязательного `Appointment`;
- исторические GREIS financial/service snapshots нельзя автоматически превращать в модель новых текущих финансовых операций.

## Ближайший порядок работ
1. Smoke-test текущего Server + Desktop на уже мигрированной БД.
2. Зафиксировать фактически работающие UI/API сценарии и найденные дефекты.
3. Нормализовать runtime-модель `Person/Patient` и lifecycle `Encounter` без разрушения импортированной истории.
4. Реализовать Patient Workspace read model/API.
5. Продолжить D01 «Мой день врача» и D02 clinical/fast-visit workflow.

Production Foundation отдельно требует LAN TLS/certificates, lockout/rate limiting, MFA для удалённого доступа, CI/test gate, controlled deployment/staging/rollback и recovery drill.

Основной статус проекта и архитектурные решения ведутся в Notion; фактически реализованный код и EF schema — в GitHub `main`.
