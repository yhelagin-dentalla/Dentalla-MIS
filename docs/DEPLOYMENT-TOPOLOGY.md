# Dentalla MIS — deployment topology

## Current development target: SERVER for Windows

The first deliverable is a complete **Dentalla Server installation** for one Windows computer.
It must be fully usable even when the clinic has no other computers.

```text
Windows Server PC
├─ Microsoft SQL Server                authoritative database
├─ Dentalla.Api                        Windows Service / Server Host
│  ├─ HTTPS API
│  ├─ SignalR
│  ├─ authentication + RBAC
│  ├─ background jobs
│  ├─ integrations
│  └─ database/file access
├─ Dentalla.Desktop                    local Windows UI
│  └─ connects to 127.0.0.1 through API/SignalR
└─ C:\ProgramData\Dentalla\Storage    documents/media/audio/etc.
```

### Critical rule
Even when UI, API and SQL Server are on the same physical PC:

`Dentalla.Desktop -> API/SignalR -> Application -> Infrastructure -> SQL Server`

There is **no** shortcut `Desktop -> SQL Server`.

The server host is an independent process/service. Closing the desktop window must not stop API,
background work, integrations or database access for other clients.

## Phase 1 — single-computer clinic
- Windows server PC contains SQL Server, server host, storage and local desktop UI.
- local UI talks to `127.0.0.1`.
- no external clients are required.

## Phase 2 — LAN clients
### Windows client
Contains only application binaries/resources/configuration/local transient cache.
No authoritative database and no SQL Server credentials.
Connects to Dentalla Server through LAN API/SignalR.

### macOS client
Same application contract and server API.
No direct SQL Server access.

## Phase 3 — remote desktop clients
Windows/macOS remote access is added only after LAN operation is stable.
The server database is never published to the Internet.
Remote connectivity uses an approved secure contour (VPN and/or gateway + MFA, according to the later security ADR).

## Phase 4 — reduced mobile clients
Future Android/iOS clients use the same API with a reduced permission/function surface.
They contain no authoritative clinical database.
Mobile implementation is not part of the current Server-first scope.

## Packaging consequence
The **Server installer/package** will eventually install/configure:
1. Dentalla Server Host;
2. local Windows Dentalla Desktop;
3. server storage directories;
4. service configuration and certificates;
5. database connection/migration tooling;
6. health/recovery/update tooling.

SQL Server edition/install strategy is a separate deployment decision; the application boundary does not depend on a specific edition.
