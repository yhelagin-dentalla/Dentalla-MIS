MIS Dentalla — Server Sprint 0.3 patch
======================================

Purpose
-------
First real Identity/Auth server increment:
- StaffProfile separated from UserAccount;
- one-time local Director bootstrap;
- salted PBKDF2 password credentials;
- opaque bearer AuthSession with hashed token storage;
- EffectivePermission resolver;
- persistent immutable AuditEvent;
- migration 20260924180000_ServerAuthentication.

Install
-------
1. Stop Dentalla.Api if it is running.
2. Extract this archive over C:\Projects\MIS Dentalla with file replacement.
3. Build:
   dotnet build .\Dentalla.Server.slnf
4. Run:
   dotnet run --project .\src\Dentalla.Api\Dentalla.Api.csproj

The server will automatically apply the new migration in the current development configuration.

Verification
------------
GET http://127.0.0.1:5080/api/server/database
should show both migrations applied and no pending migrations.

GET http://127.0.0.1:5080/api/auth/bootstrap/status
should initially return: {"required":true}

See docs\AUTH-BOOTSTRAP.md for the first Director creation and auth tests.

Important
---------
Do not send your chosen Director password to ChatGPT. Enter it only on the local server.
The public/LAN listener remains disabled; API is still bound to 127.0.0.1.
