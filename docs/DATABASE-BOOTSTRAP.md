# Dentalla Server — database bootstrap (Sprint 0.2)

## Development instance

- SQL Server: `localhost`
- SQL Server version observed in SSMS: 16.0.1200.5 (SQL Server 2022)
- New MIS database: `Dentalla`
- IDENT test/source database: `PZ_TEST` — migration source only; never the live Dentalla database.
- Development authentication: Windows Integrated Security.

Connection string:

```text
Server=localhost;Database=Dentalla;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=False
```

## Startup behavior in the current development package

`DentallaServer:ApplyDatabaseMigrationsOnStartup=true` is enabled only to simplify the early local server spike.
On first server start EF Core creates database `Dentalla` (provided the Windows account has SQL permission to create a database) and applies the committed migration.
Reference permission definitions and missing role defaults are then seeded.

This is NOT the final production update policy. Production schema migrations are a controlled server-deployment step with verified backup and rollback plan before activating the new application build.

## First checks

After starting `Dentalla.Api`:

- `http://127.0.0.1:5080/health/live`
- `http://127.0.0.1:5080/health/ready`
- `http://127.0.0.1:5080/api/server/database`

The database endpoint should report `CanConnect=true`, `Database=Dentalla` and the applied migration `20260924150000_InitialServer`.
