# DentallaAPI Windows service

`DentallaAPI` is the local Application/API Server for Dentalla MIS. The Desktop client continues to use the architecture `Desktop -> API -> Application -> Infrastructure -> SQL Server`; the Desktop client never connects directly to SQL Server.

## Install or update

Open **PowerShell as Administrator** in the repository root and run:

```powershell
powershell -executionpolicy bypass -file .\deployment\DentallaAPI\install-service.ps1
```

The installer:

1. stops the existing service when updating;
2. applies committed EF Core migrations as the installing administrator;
3. publishes `Dentalla.Api` as a self-contained `win-x64` deployment into `deployment\DentallaAPI\publish`;
4. provisions the Windows virtual service identity `NT SERVICE\DentallaAPI` in SQL Server with access only to the existing `Dentalla` database;
5. replaces/creates the `DentallaAPI` Windows service with startup type `Automatic`;
6. configures automatic restart after unexpected failures;
7. starts the service and waits for `http://127.0.0.1:5080/health/ready` to report READY before declaring installation successful.

The service listens on `http://127.0.0.1:5080` according to `src/Dentalla.Api/appsettings.json`.

## Check status

```powershell
powershell -executionpolicy bypass -file .\deployment\DentallaAPI\service-status.ps1
```

Or use Windows `services.msc`; the service name is `DentallaAPI`, display name `Dentalla API`.

## Remove service

Open PowerShell as Administrator:

```powershell
powershell -executionpolicy bypass -file .\deployment\DentallaAPI\uninstall-service.ps1
```

Published files and the SQL login/user are intentionally preserved when the service is removed. Removing SQL access is a separate administrative operation so uninstalling the Windows service cannot accidentally damage database ownership or data.

## SQL Server identity

Runtime uses the Windows virtual service account:

```text
NT SERVICE\DentallaAPI
```

`configure-runtime-sql.ps1` creates the SQL login/database user when missing and grants only `db_datareader` and `db_datawriter` in the existing `Dentalla` database. It does **not** grant `CREATE DATABASE`, `db_owner`, `securityadmin`, or `sysadmin`.

The Desktop client never receives SQL credentials or direct SQL access.

## Migrations and runtime boundary

`DentallaAPI` does not apply EF Core migrations at normal service startup (`ApplyDatabaseMigrationsOnStartup=false`). Schema changes are a controlled deployment operation performed by `install-service.ps1` under the installing administrator before the runtime service starts.

Reference-data seeding remains enabled at startup because it is application-owned DML and the runtime identity has the required read/write permissions. It does not own or alter the database schema.
