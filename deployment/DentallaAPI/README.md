# DentallaAPI Windows service

`DentallaAPI` is the local Application/API Server for Dentalla MIS. The Desktop client continues to use the architecture `Desktop -> API -> Application -> Infrastructure -> SQL Server`; the Desktop client never connects directly to SQL Server.

## Install or update

Open **PowerShell as Administrator** in the repository root and run:

```powershell
powershell -executionpolicy bypass -file .\deployment\DentallaAPI\install-service.ps1
```

The installer:

1. publishes `Dentalla.Api` as a self-contained `win-x64` deployment into `deployment\DentallaAPI\publish`;
2. replaces an existing `DentallaAPI` service when updating;
3. installs the Windows service with startup type `Automatic`;
4. configures automatic restart after unexpected service failures;
5. starts the service immediately.

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

Published files are intentionally preserved when the service is removed.

## SQL Server identity

The initial local installation runs under the default Windows service account used by `New-Service` (LocalSystem). The current development connection string uses Windows Integrated Security. Therefore that Windows identity must have the required rights to the local `Dentalla` SQL Server database. Before production deployment, service identity and SQL permissions must be explicitly hardened as part of server deployment; SQL credentials must never be moved into the Desktop client.

## Migrations

Current development configuration may apply committed migrations on API startup. Production database migrations remain a controlled deployment step after a verified backup and rollback plan.
