#Requires -RunAsAdministrator
$ErrorActionPreference = 'Stop'

$serviceName = 'DentallaAPI'
$displayName = 'Dentalla API'
$serviceAccount = "NT SERVICE\$serviceName"
$deploymentDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $deploymentDir '..\..')).Path
$publishScript = Join-Path $deploymentDir 'publish-api.ps1'
$sqlProvisionScript = Join-Path $deploymentDir 'configure-runtime-sql.ps1'
$publishDir = Join-Path $deploymentDir 'publish'
$exe = Join-Path $publishDir 'Dentalla.Api.exe'
$healthUri = 'http://127.0.0.1:5080/health/ready'

$existing = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if ($null -ne $existing -and $existing.Status -ne 'Stopped') {
    Write-Host "Stopping existing service $serviceName..."
    Stop-Service -Name $serviceName -Force
    (Get-Service -Name $serviceName).WaitForStatus('Stopped', [TimeSpan]::FromSeconds(30))
}

Write-Host 'Applying EF Core migrations as the installing administrator...'
$infrastructureProject = Join-Path $repoRoot 'src\Dentalla.Infrastructure'
$startupProject = Join-Path $repoRoot 'src\Dentalla.Api'
& dotnet ef database update --project $infrastructureProject --startup-project $startupProject
if ($LASTEXITCODE -ne 0) {
    throw "EF database update failed with exit code $LASTEXITCODE. DentallaAPI was not installed."
}

Write-Host 'Building DentallaAPI service payload...'
& $publishScript

Write-Host 'Provisioning least-privilege SQL access for the service identity...'
& $sqlProvisionScript -serviceName $serviceName

$existing = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if ($null -ne $existing) {
    Write-Host "Removing existing service $serviceName..."
    sc.exe delete $serviceName | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw "Could not delete existing service $serviceName."
    }

    $deadline = (Get-Date).AddSeconds(30)
    while ((Get-Service -Name $serviceName -ErrorAction SilentlyContinue) -and (Get-Date) -lt $deadline) {
        Start-Sleep -Milliseconds 500
    }
    if (Get-Service -Name $serviceName -ErrorAction SilentlyContinue) {
        throw "Service $serviceName is still pending deletion. Restart Windows and run this installer again."
    }
}

Write-Host "Creating Windows service $serviceName as $serviceAccount..."
New-Service `
    -Name $serviceName `
    -BinaryPathName ('"' + $exe + '"') `
    -DisplayName $displayName `
    -Description 'Dentalla MIS local Application/API Server' `
    -StartupType Automatic | Out-Null

# Use the Windows virtual service account instead of LocalSystem. This identity
# is local to DentallaAPI and is granted access only to the Dentalla database.
sc.exe config $serviceName obj= $serviceAccount password= "" | Out-Host
if ($LASTEXITCODE -ne 0) {
    throw "Could not configure $serviceName to run as $serviceAccount."
}

# Restart automatically after unexpected failures.
sc.exe failure $serviceName reset= 86400 actions= restart/5000/restart/15000/restart/60000 | Out-Host
sc.exe failureflag $serviceName 1 | Out-Host

Write-Host "Starting $serviceName..."
Start-Service -Name $serviceName

$deadline = (Get-Date).AddSeconds(45)
$ready = $false
$lastHealthError = $null
while ((Get-Date) -lt $deadline) {
    $service = Get-Service -Name $serviceName -ErrorAction Stop
    if ($service.Status -eq 'Stopped') {
        throw "$serviceName stopped during startup. Check the Windows Application event log for Dentalla.Api/.NET Runtime errors."
    }

    try {
        $response = Invoke-RestMethod -Uri $healthUri -Method Get -TimeoutSec 3
        if ($response.ready -eq $true) {
            $ready = $true
            break
        }
    }
    catch {
        $lastHealthError = $_.Exception.Message
    }

    Start-Sleep -Seconds 1
}

if (-not $ready) {
    Stop-Service -Name $serviceName -Force -ErrorAction SilentlyContinue
    throw "$serviceName did not become ready within 45 seconds. Last health error: $lastHealthError"
}

Write-Host ''
Get-Service -Name $serviceName | Format-Table Name, Status, StartType -AutoSize
Write-Host "Service account: $serviceAccount"
Write-Host "Health check: $healthUri = READY"
Write-Host 'DentallaAPI is installed and will start automatically with Windows.'
Write-Host 'API endpoint: http://127.0.0.1:5080'
