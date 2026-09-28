#Requires -RunAsAdministrator
$ErrorActionPreference = 'Stop'

$serviceName = 'DentallaAPI'
$displayName = 'Dentalla API'
$deploymentDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$publishScript = Join-Path $deploymentDir 'publish-api.ps1'
$publishDir = Join-Path $deploymentDir 'publish'
$exe = Join-Path $publishDir 'Dentalla.Api.exe'

Write-Host 'Building DentallaAPI service payload...'
& $publishScript

$existing = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if ($null -ne $existing) {
    Write-Host "Stopping existing service $serviceName..."
    if ($existing.Status -ne 'Stopped') {
        Stop-Service -Name $serviceName -Force
        (Get-Service -Name $serviceName).WaitForStatus('Stopped', [TimeSpan]::FromSeconds(30))
    }
    Write-Host "Removing existing service $serviceName..."
    sc.exe delete $serviceName | Out-Host
    $deadline = (Get-Date).AddSeconds(30)
    while ((Get-Service -Name $serviceName -ErrorAction SilentlyContinue) -and (Get-Date) -lt $deadline) {
        Start-Sleep -Milliseconds 500
    }
    if (Get-Service -Name $serviceName -ErrorAction SilentlyContinue) {
        throw "Service $serviceName is still pending deletion. Restart Windows and run this installer again."
    }
}

Write-Host "Creating Windows service $serviceName..."
New-Service `
    -Name $serviceName `
    -BinaryPathName ('"' + $exe + '"') `
    -DisplayName $displayName `
    -Description 'Dentalla MIS local Application/API Server' `
    -StartupType Automatic | Out-Null

# Restart automatically after unexpected failures.
sc.exe failure $serviceName reset= 86400 actions= restart/5000/restart/15000/restart/60000 | Out-Host
sc.exe failureflag $serviceName 1 | Out-Host

Write-Host "Starting $serviceName..."
Start-Service -Name $serviceName
(Get-Service -Name $serviceName).WaitForStatus('Running', [TimeSpan]::FromSeconds(30))

Write-Host ''
Get-Service -Name $serviceName | Format-Table Name, Status, StartType -AutoSize
Write-Host 'DentallaAPI is installed and will start automatically with Windows.'
Write-Host 'API endpoint: http://127.0.0.1:5080'
