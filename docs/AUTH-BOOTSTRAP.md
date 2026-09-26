# Dentalla Server — local authentication bootstrap (Sprint 0.3)

## Purpose
Sprint 0.3 creates the first real Dentalla user account, server-side sessions, EffectivePermission calculation and AuditEvent storage.

## Security boundary
- bootstrap Director can be created only once;
- bootstrap endpoint accepts requests only from loopback;
- passwords are stored as salted PBKDF2-HMAC-SHA256 hashes;
- raw bearer session tokens are never stored in SQL; only SHA-256 token hashes are persisted;
- authorization remains server-authoritative.

## Endpoints
- `GET /api/auth/bootstrap/status`
- `POST /api/auth/bootstrap/director`
- `POST /api/auth/login`
- `GET /api/auth/me` (Bearer)
- `GET /api/auth/me/permissions` (Bearer)
- `POST /api/auth/logout` (Bearer)

## Development test
After applying the patch, build and run `Dentalla.Api`.

Check bootstrap status:

```powershell
invoke-restmethod http://127.0.0.1:5080/api/auth/bootstrap/status
```

Create the first Director using your own username/password/display name:

```powershell
$body = @{
    userName = "director"
    password = "CHANGE_THIS_TO_YOUR_OWN_PASSWORD"
    displayName = "Director"
    clientName = "Server console"
} | convertto-json

$r = invoke-restmethod `
    -method post `
    -uri http://127.0.0.1:5080/api/auth/bootstrap/director `
    -contenttype "application/json" `
    -body $body

$token = $r.accessToken
$r.user
```

Verify current user:

```powershell
invoke-restmethod `
    -uri http://127.0.0.1:5080/api/auth/me `
    -headers @{ Authorization = "Bearer $token" }
```

Verify EffectivePermission:

```powershell
invoke-restmethod `
    -uri http://127.0.0.1:5080/api/auth/me/permissions `
    -headers @{ Authorization = "Bearer $token" }
```

Logout:

```powershell
invoke-restmethod `
    -method post `
    -uri http://127.0.0.1:5080/api/auth/logout `
    -headers @{ Authorization = "Bearer $token" }
```
