$ErrorActionPreference = 'Stop'

$deploymentDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $deploymentDir '..\..')).Path
$project = Join-Path $repoRoot 'src\Dentalla.Api\Dentalla.Api.csproj'
$publishDir = Join-Path $deploymentDir 'publish'

if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
}
New-Item -ItemType Directory -Path $publishDir -Force | Out-Null

Write-Host 'Publishing DentallaAPI (win-x64, self-contained)...'
dotnet publish $project -c Release -r win-x64 --self-contained true -o $publishDir
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

$exe = Join-Path $publishDir 'Dentalla.Api.exe'
if (-not (Test-Path $exe)) {
    throw "Published executable was not found: $exe"
}

Write-Host ''
Write-Host "DentallaAPI published to: $publishDir"
Write-Host "Executable: $exe"
