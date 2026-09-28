param(
    [string]$configuration = "Release",
    [string]$runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$project = Join-Path $repoRoot "src\Dentalla.Desktop\Dentalla.Desktop.csproj"
$output = Join-Path $PSScriptRoot "publish"

if (Test-Path $output) {
    Remove-Item $output -Recurse -Force
}

New-Item -ItemType Directory -Path $output -Force | Out-Null

Write-Host "Publishing Dentalla MIS desktop client..."
Write-Host "Avalonia desktop is self-contained; runtime/native files stay beside DentallaMIS.exe."

dotnet publish $project `
    -c $configuration `
    -r $runtime `
    --self-contained true `
    -o $output `
    -p:PublishSingleFile=false `
    -p:DebugType=None `
    -p:DebugSymbols=false

if ($LASTEXITCODE -ne 0) {
    throw "DentallaMIS publish failed with exit code $LASTEXITCODE."
}

$exe = Join-Path $output "DentallaMIS.exe"
if (-not (Test-Path $exe)) {
    throw "DentallaMIS.exe was not produced."
}

Write-Host ""
Write-Host "DentallaMIS.exe is ready:"
Write-Host $exe
Write-Host ""
Write-Host "Keep the entire publish directory together when running or copying the application."
Write-Host "DentallaAPI must be installed and running on this workstation/server."
