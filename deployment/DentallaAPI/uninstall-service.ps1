#Requires -RunAsAdministrator
$ErrorActionPreference = 'Stop'

$serviceName = 'DentallaAPI'
$service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if ($null -eq $service) {
    Write-Host "Service $serviceName is not installed."
    exit 0
}

if ($service.Status -ne 'Stopped') {
    Write-Host "Stopping $serviceName..."
    Stop-Service -Name $serviceName -Force
    (Get-Service -Name $serviceName).WaitForStatus('Stopped', [TimeSpan]::FromSeconds(30))
}

Write-Host "Removing $serviceName..."
sc.exe delete $serviceName | Out-Host
Write-Host 'Service removal requested. Published files are preserved.'
