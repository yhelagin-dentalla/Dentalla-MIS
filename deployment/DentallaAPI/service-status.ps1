$serviceName = 'DentallaAPI'
$service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if ($null -eq $service) {
    Write-Host "Service $serviceName is not installed."
    exit 1
}

$service | Format-Table Name, DisplayName, Status, StartType -AutoSize

try {
    $response = Invoke-WebRequest -Uri 'http://127.0.0.1:5080' -Method Get -TimeoutSec 5 -UseBasicParsing -ErrorAction Stop
    Write-Host "API responded with HTTP $($response.StatusCode)."
}
catch {
    if ($_.Exception.Response) {
        Write-Host "API endpoint is reachable; HTTP status: $([int]$_.Exception.Response.StatusCode)."
    }
    else {
        Write-Warning "API endpoint did not respond: $($_.Exception.Message)"
    }
}
