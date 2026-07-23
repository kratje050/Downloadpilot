param(
    [string]$Configuration = "Release",
    [string]$Version = "0.2.0",
    [string]$Runtime = "win-x64",
    [string]$CertificatePath = "",
    [string]$CertificatePassword = "",
    [string]$InnoSetupCompiler = ""
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path "$PSScriptRoot\.."
Set-Location $root

$artifactRoot = Join-Path $root "artifacts\release\v$Version"
$packageName = "DownloadPilot-v$Version-$Runtime"
$packageDir = Join-Path $artifactRoot $packageName
$zipPath = Join-Path $artifactRoot "$packageName.zip"
$publishDir = Join-Path $root "publish\$Runtime"

New-Item -ItemType Directory -Force -Path $artifactRoot | Out-Null

dotnet restore .\DownloadPilot.sln
dotnet build .\DownloadPilot.sln -c $Configuration --no-restore
dotnet test .\DownloadPilot.sln -c $Configuration --no-build

powershell -ExecutionPolicy Bypass -File .\scripts\build-portable.ps1 -Configuration $Configuration

if ($CertificatePath -and (Test-Path $CertificatePath)) {
    $signtool = Get-Command signtool.exe -ErrorAction SilentlyContinue
    if ($signtool) {
        $passwordArgs = @()
        if ($CertificatePassword) {
            $passwordArgs = @("/p", $CertificatePassword)
        }

        & $signtool.Source sign /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 /f $CertificatePath @passwordArgs (Join-Path $publishDir "DownloadPilot.App.exe")
    } else {
        Write-Warning "signtool.exe niet gevonden; ondertekenen overgeslagen."
    }
}

if (Test-Path $packageDir) {
    Remove-Item $packageDir -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $packageDir | Out-Null
Copy-Item -LiteralPath (Join-Path $publishDir "DownloadPilot.App.exe") -Destination (Join-Path $packageDir "DownloadPilot.App.exe") -Force
Copy-Item -LiteralPath (Join-Path $root "README.md") -Destination (Join-Path $packageDir "README.md") -Force

if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

Compress-Archive -Path (Join-Path $packageDir "*") -DestinationPath $zipPath
$hash = Get-FileHash -Algorithm SHA256 -LiteralPath $zipPath

if ($InnoSetupCompiler -and (Test-Path $InnoSetupCompiler)) {
    & $InnoSetupCompiler ".\installer\DownloadPilot.iss"
} else {
    Write-Host "Inno Setup compiler niet opgegeven; setup-exe overgeslagen."
}

[pscustomobject]@{
    Version = $Version
    Runtime = $Runtime
    Zip = $zipPath
    SHA256 = $hash.Hash
}
