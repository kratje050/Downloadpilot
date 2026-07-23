param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

Set-Location "$PSScriptRoot\.."

$publishDir = Join-Path (Get-Location) "publish\win-x64"
if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
}

& "C:\Program Files\dotnet\dotnet.exe" publish .\DownloadPilot.App\DownloadPilot.App.csproj `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    -o $publishDir

Write-Host "Portable build klaar in: $publishDir"
