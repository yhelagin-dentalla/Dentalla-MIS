MIS Dentalla — полный raw snapshot PZ_TEST
==========================================

Назначение
----------
Этот патч НЕ превращает IDENT schema в рабочую схему новой МИС.
Он переносит все пользовательские таблицы PZ_TEST в Dentalla.ident_raw как read-only legacy snapshot,
а затем мы будем поэтапно нормализовывать данные в доменные таблицы Dentalla.

Что НЕ переносится как security/config новой МИС
-------------------------------------------------
- SQL logins/users/roles;
- серверные разрешения;
- security model IDENT;
- исполняемая бизнес-логика IDENT.

SQL definitions VIEW/PROC/FUNCTION/TRIGGER/SYNONYM сохраняются только как inventory
в migration.IdentObjectDefinitions.

1. Собрать
----------
dotnet build .\Dentalla.Server.slnf

2. Сначала посмотреть инвентаризацию
-----------------------------------
dotnet run --project .\src\Dentalla.Migration.Ident\Dentalla.Migration.Ident.csproj -- inventory

По умолчанию:
source = localhost / PZ_TEST
target = localhost / Dentalla
Windows Authentication.

3. Выполнить первый полный snapshot
-----------------------------------
dotnet run --project .\src\Dentalla.Migration.Ident\Dentalla.Migration.Ident.csproj -- snapshot

Если ident_raw уже существует и нужно обновить snapshot:

dotnet run --project .\src\Dentalla.Migration.Ident\Dentalla.Migration.Ident.csproj -- snapshot --replace

--replace удаляет/создаёт заново только соответствующие таблицы schema ident_raw.
Рабочие Patients/security/audit и другие таблицы Dentalla не затрагиваются.

4. Проверить после копирования
-----------------------------
dotnet run --project .\src\Dentalla.Migration.Ident\Dentalla.Migration.Ident.csproj -- verify

5. Где смотреть результат в SSMS
--------------------------------
Dentalla
  Tables
    ident_raw.*
    migration.IdentSnapshotRuns
    migration.IdentSnapshotTables
    migration.IdentObjectDefinitions

Контрольный SQL:

SELECT TOP (20) *
FROM Dentalla.migration.IdentSnapshotRuns
ORDER BY StartedAtUtc DESC;

SELECT Status, COUNT(*) AS Tables
FROM Dentalla.migration.IdentSnapshotTables
WHERE RunId = (SELECT TOP 1 RunId FROM Dentalla.migration.IdentSnapshotRuns ORDER BY StartedAtUtc DESC)
GROUP BY Status;

SELECT SUM(SourceRowCount) AS SourceRows,
       SUM(TargetRowCount) AS TargetRows
FROM Dentalla.migration.IdentSnapshotTables
WHERE RunId = (SELECT TOP 1 RunId FROM Dentalla.migration.IdentSnapshotRuns ORDER BY StartedAtUtc DESC);

Важно
-----
Source PZ_TEST migration code не модифицирует.
Copy выполняется target-side командой SELECT INTO из PZ_TEST в Dentalla.ident_raw.
