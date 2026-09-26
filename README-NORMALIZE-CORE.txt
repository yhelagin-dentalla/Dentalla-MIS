Dentalla MIS — Core normalization patch
======================================

Purpose
-------
Normalize the full IDENT raw snapshot into Dentalla-owned core tables:
  ident_raw -> staff.StaffProfiles
            -> security.UserAccounts
            -> security.UserRoleAssignments
            -> dbo.Patients
            -> scheduling.Appointments
            -> integration.ExternalIdentifiers

After normalize-core the login directory reads ONLY normalized Dentalla tables.
Doctor and Administrator workspaces read scheduling.Appointments + Patients + StaffProfiles through Dentalla.Api.

Install
-------
Extract this patch into:
  C:\Projects\MIS Dentalla
with file replacement.

1. Build:
  cd "C:\Projects\MIS Dentalla"
  dotnet build .\Dentalla.Server.slnf

2. Start server once so migration 20260924203000_CoreLegacyNormalization is applied:
  dotnet run --project .\src\Dentalla.Api\Dentalla.Api.csproj

3. In a second PowerShell run normalization:
  cd "C:\Projects\MIS Dentalla"
  dotnet run --project .\src\Dentalla.Migration.Ident\Dentalla.Migration.Ident.csproj -- normalize-core

Optional Chief Medical Officer legacy staff id override:
  dotnet run --project .\src\Dentalla.Migration.Ident\Dentalla.Migration.Ident.csproj -- normalize-core --chief-medical-officer-id 123

4. Check normalized login directory:
  http://127.0.0.1:5080/api/auth/dev-login-directory
Expected source:
  Dentalla.staff/security

5. Check today's normalized schedule:
  http://127.0.0.1:5080/api/workspaces/day-schedule

6. Start Desktop:
  dotnet run --project .\src\Dentalla.Desktop\Dentalla.Desktop.csproj

Notes
-----
- normalize-core is idempotent: deterministic Dentalla IDs + MERGE are used.
- IDENT IDs never become Dentalla PKs. Mapping is stored in integration.ExternalIdentifiers.
- Login no longer queries ident_raw.
- Appointment normalization currently uses IDENT Receptions + CurrentTimeTable + Times. It does not infer NoShow/Fulfilled from dates alone. Cancelled is mapped only when ID_ReceptionCancelReasons is present; other imported entries remain Scheduled until richer status semantics are normalized.
- Encounter, TreatmentPlan and finance are NOT normalized by this patch.
