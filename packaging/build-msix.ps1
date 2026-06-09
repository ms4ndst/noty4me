# End-to-end MSIX build:
#   1. dotnet publish the app (framework-dependent, win-x64).
#   2. Stage publish output + AppxManifest.xml + Images into packaging\layout.
#   3. Run MakeAppx.exe to produce Noty4Me_1.0.0.0_x64.msix.
#   4. Sign with packaging\Noty4Me.pfx using SignTool.exe.
#
# Requires the Windows 10/11 SDK on PATH (MakeAppx + SignTool).
# Falls back to scanning common SDK install locations.

param(
    [string]$RepoRoot   = (Resolve-Path "$PSScriptRoot\..").Path,
    [string]$Config     = 'Release',
    [string]$Rid        = 'win-x64',
    [string]$PfxPath    = "$PSScriptRoot\Noty4Me.pfx",
    [SecureString]$Password,
    [switch]$SkipPublish,
    [switch]$SkipSign
)

$ErrorActionPreference = 'Stop'

if (-not $Password -and -not $SkipSign) {
    Write-Host "No -Password provided. Enter the .pfx password (will not echo):"
    $Password = Read-Host -AsSecureString
    if ($Password.Length -eq 0) { throw "Password is required for signing. Use -SkipSign to build unsigned." }
}

function Find-SdkTool([string]$tool) {
    $cmd = Get-Command $tool -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    $sdkRoot = 'C:\Program Files (x86)\Windows Kits\10\bin'
    if (-not (Test-Path $sdkRoot)) { throw "$tool not found and Windows SDK not at $sdkRoot." }
    $hit = Get-ChildItem -Recurse -Path $sdkRoot -Filter $tool -ErrorAction SilentlyContinue |
           Where-Object { $_.FullName -match '\\x64\\' } |
           Sort-Object FullName -Descending |
           Select-Object -First 1
    if (-not $hit) { throw "$tool not found under $sdkRoot. Install the Windows 10/11 SDK." }
    return $hit.FullName
}

$proj      = Join-Path $RepoRoot 'src\Noty4Me\Noty4Me.csproj'
$publishOut= Join-Path $RepoRoot "src\Noty4Me\bin\$Config\net10.0-windows10.0.19041.0\$Rid\publish"
$layoutDir = Join-Path $RepoRoot 'packaging\layout'
$imagesDir = Join-Path $RepoRoot 'packaging\Images'
$manifest  = Join-Path $RepoRoot 'packaging\AppxManifest.xml'
$outDir    = Join-Path $RepoRoot 'packaging\out'
[xml]$mx   = Get-Content $manifest
$ver       = $mx.Package.Identity.Version
$msixPath  = Join-Path $outDir "Noty4Me_${ver}_x64.msix"

New-Item -ItemType Directory -Force -Path $outDir | Out-Null

if (-not $SkipPublish) {
    Write-Host "==> dotnet publish ($Config, $Rid)"
    dotnet publish $proj -c $Config -r $Rid --self-contained false -p:PublishSingleFile=false -nologo
    if ($LASTEXITCODE -ne 0) { throw "Publish failed." }
}

Write-Host "==> Staging layout: $layoutDir"
if (Test-Path $layoutDir) { Remove-Item -Recurse -Force $layoutDir }
New-Item -ItemType Directory -Force -Path $layoutDir | Out-Null
Copy-Item -Recurse -Force "$publishOut\*" $layoutDir
Copy-Item -Force $manifest (Join-Path $layoutDir 'AppxManifest.xml')
$layoutImages = Join-Path $layoutDir 'Images'
New-Item -ItemType Directory -Force -Path $layoutImages | Out-Null
Copy-Item -Force "$imagesDir\*.png" $layoutImages

# Strip files that violate MSIX rules (.pdb is fine; appsettings.json is fine).
# AppxManifest must NOT be present inside subfolders, only at root.
Get-ChildItem -Path $layoutDir -Recurse -Filter 'AppxManifest.xml' |
    Where-Object { $_.FullName -ne (Join-Path $layoutDir 'AppxManifest.xml') } |
    Remove-Item -Force

$makeAppx = Find-SdkTool 'MakeAppx.exe'
Write-Host "==> MakeAppx.exe ($makeAppx)"
& $makeAppx pack /o /d $layoutDir /p $msixPath
if ($LASTEXITCODE -ne 0) { throw "MakeAppx failed." }

if ($SkipSign) {
    Write-Host "Skipped signing. Output: $msixPath"
    return
}

if (-not (Test-Path $PfxPath)) {
    throw "Certificate not found at $PfxPath. Run packaging\make-cert.ps1 first."
}

$signTool = Find-SdkTool 'SignTool.exe'
Write-Host "==> SignTool.exe ($signTool)"
$plainPw = [System.Net.NetworkCredential]::new('', $Password).Password
& $signTool sign /fd SHA256 /a /f $PfxPath /p $plainPw $msixPath
if ($LASTEXITCODE -ne 0) { throw "SignTool failed." }

Write-Host ""
Write-Host "OK: $msixPath"
Write-Host ""
Write-Host "Install (after trusting the .cer once):"
Write-Host "  Add-AppxPackage '$msixPath'"
