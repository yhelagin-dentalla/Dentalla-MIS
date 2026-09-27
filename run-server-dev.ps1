$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

Write-Host 'Starting Dentalla SERVER HOST on localhost...' -ForegroundColor Cyan
$server = Start-Process -FilePath 'dotnet' -ArgumentList @('run','--project','src\Dentalla.Api\Dentalla.Api.csproj') -PassThru

try {
    Write-Host 'Waiting for Dentalla Server readiness...' -ForegroundColor DarkCyan

    $ready = $false
    $deadline = [DateTime]::UtcNow.AddSeconds(60)

    while (-not $ready -and [DateTime]::UtcNow -lt $deadline) {
        if ($server.HasExited) {
            throw 'Dentalla.Api stopped before becoming ready.'
        }

        try {
            $response = Invoke-WebRequest `
                -Uri 'http://127.0.0.1:5080/health/ready' `
                -UseBasicParsing `
                -TimeoutSec 2

            if ($response.StatusCode -eq 200) {
                $ready = $true
                break
            }
        }
        catch {
            # API can be unavailable or return 503 while migrations/startup checks are still running.
        }

        Start-Sleep -Milliseconds 500
    }

    if (-not $ready) {
        throw 'Dentalla Server did not become ready within 60 seconds.'
    }

    Write-Host 'Dentalla Server is ready.' -ForegroundColor Green
    Write-Host 'Starting local Dentalla Windows UI...' -ForegroundColor Cyan
    dotnet run --project 'src\Dentalla.Desktop\Dentalla.Desktop.csproj'
}
finally {
    if (-not $server.HasExited) { Stop-Process -Id $server.Id -Force }
}
