$serviceName = 'DentallaAPI'
$service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if ($null -eq $service) {
    Write-Host "Service $serviceName is not installed."
    exit 1
}

$service | Format-Table Name, DisplayName, Status, StartType -AutoSize

$serviceConfig = Get-CimInstance Win32_Service -Filter "Name='$serviceName'" -ErrorAction SilentlyContinue
if ($null -ne $serviceConfig) {
    Write-Host "Service account: $($serviceConfig.StartName)"
}

try {
    $response = Invoke-RestMethod -Uri 'http://127.0.0.1:5080/health/ready' -Method Get -TimeoutSec 5 -ErrorAction Stop
    if ($response.ready -eq $true) {
        Write-Host 'API readiness: READY'
        exit 0
    }

    Write-Warning 'API responded but did not report READY.'
    exit 2
}
catch {
    Write-Warning "API readiness check failed: $($_.Exception.Message)"
    exit 2
}
