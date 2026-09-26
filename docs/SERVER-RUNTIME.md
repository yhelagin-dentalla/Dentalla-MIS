# Dentalla Server Runtime — Sprint 0.1

Current executable server process: `Dentalla.Api` (product role: **Dentalla Server Host**).

## Responsibilities already implemented in code
- Windows Service hosting support;
- SQL Server DbContext boundary;
- server-owned local file storage under `C:\ProgramData\Dentalla\Storage`;
- liveness endpoint `/health/live`;
- readiness endpoint `/health/ready` checking SQL connectivity and storage writeability;
- server metadata endpoint `/api/server/info`;
- SignalR endpoint `/hubs/updates`;
- global ProblemDetails-compatible exception boundary;
- first persisted RBAC model: user account, multi-role assignment, permission catalog, role defaults, per-user Allow/Deny, finite delegation grants;
- permission catalog seed data.

## Important boundaries
The server process owns database and storage access. Future Windows/macOS/mobile clients do not receive SQL credentials or storage paths.

`Desktop -> API/SignalR -> Application -> Infrastructure -> SQL Server/files`

## Current limitations
Authentication/login is intentionally **not implemented yet**. RBAC persistence is created first so authentication can target a stable authorization model instead of hard-coded role checks.

EF migration files are also not generated in this artifact because the build environment used to prepare the source does not contain the .NET 10 SDK. Generate and review the first migration only after the project successfully compiles on the target Windows development machine.
