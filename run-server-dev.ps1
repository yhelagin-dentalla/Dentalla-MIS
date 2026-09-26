$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

Write-Host 'Starting Dentalla SERVER HOST on localhost...' -ForegroundColor Cyan
$server = Start-Process -FilePath 'dotnet' -ArgumentList @('run','--project','src\Dentalla.Api\Dentalla.Api.csproj') -PassThru
Start-Sleep -Seconds 2

try {
    Write-Host 'Starting local Dentalla Windows UI...' -ForegroundColor Cyan
    dotnet run --project 'src\Dentalla.Desktop\Dentalla.Desktop.csproj'
}
finally {
    if (-not $server.HasExited) { Stop-Process -Id $server.Id -Force }
}
