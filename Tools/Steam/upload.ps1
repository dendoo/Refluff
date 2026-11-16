<#
.SYNOPSIS
  Uploads Builds/Steam/content (the release build) to Steam app 5304930 with steamcmd.
  Full walkthrough: Assets/CarpetCleaning/Docs/STEAM.md section 7.

.DESCRIPTION
  1. Build first: Unity menu CarpetCleaning > Steam > Build Windows (release, for upload).
  2. Dry run:  upload.bat -Account <builder_login> -Preview
  3. Upload:   upload.bat -Account <builder_login> -Desc "v0.1.0 first test"
  steamcmd asks for the password and the Steam Guard code itself; nothing is stored by this script.
  steamcmd is downloaded into Tools/Steam/steamcmd on the first run. Logs land in Tools/Steam/output.
  A finished upload is NOT live: pick it in Steamworks > SteamPipe > Builds and set it on a branch.
#>
param(
    [Parameter(Mandatory = $true)][string]$Account,
    [string]$Desc = "",
    [switch]$Preview
)

$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = (Resolve-Path (Join-Path $here '..\..')).Path
$content = Join-Path $root 'Builds\Steam\content\windows'

if (-not (Test-Path (Join-Path $content 'Refluff.exe'))) {
    throw "No release build in $content. Run CarpetCleaning > Steam > Build Windows (release, for upload) in Unity first."
}
if (Test-Path (Join-Path $content 'steam_appid.txt')) {
    throw "steam_appid.txt is in the release content. That is a local test build: rebuild with the release menu item."
}

# steamcmd, fetched once from Valve
$cmdDir = Join-Path $here 'steamcmd'
$steamcmd = Join-Path $cmdDir 'steamcmd.exe'
if (-not (Test-Path $steamcmd)) {
    Write-Host "Downloading steamcmd into $cmdDir ..."
    New-Item -ItemType Directory -Force $cmdDir | Out-Null
    $zip = Join-Path $cmdDir 'steamcmd.zip'
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    Invoke-WebRequest 'https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip' -OutFile $zip -UseBasicParsing
    Expand-Archive $zip -DestinationPath $cmdDir -Force
    Remove-Item $zip
}

if ($Desc -eq '') {
    $ver = (Select-String -Path (Join-Path $root 'ProjectSettings\ProjectSettings.asset') -Pattern '^\s*bundleVersion:\s*(.+)$').Matches[0].Groups[1].Value.Trim()
    $Desc = "Refluff $ver " + (Get-Date -Format 'yyyy-MM-dd HH:mm')
}

# A copy of the app script with Desc/Preview filled in, next to the original so its relative paths still hold.
$template = Join-Path $here 'app_build_5304930.vdf'
$generated = Join-Path $here 'app_build_5304930.generated.vdf'
$safeDesc = ($Desc -replace '"', "'") -replace '\$', '$$$$'
$previewValue = if ($Preview) { '1' } else { '0' }
$vdf = Get-Content $template -Raw
$vdf = $vdf -replace '"Desc"\s+"[^"]*"', ('"Desc" "' + $safeDesc + '"')
$vdf = $vdf -replace '"Preview"\s+"[^"]*"', ('"Preview" "' + $previewValue + '"')
[IO.File]::WriteAllText($generated, $vdf, (New-Object Text.UTF8Encoding $false))

Write-Host ("Uploading {0} as '{1}'{2}" -f $content, $Desc, $(if ($Preview) { ' (PREVIEW, nothing is uploaded)' } else { '' }))
try {
    & $steamcmd +login $Account +run_app_build $generated +quit
    $code = $LASTEXITCODE
}
finally {
    Remove-Item $generated -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "steamcmd exit code $code. Build logs: $(Join-Path $here 'output')"
if ($code -ne 0) { exit $code }
if (-not $Preview) {
    Write-Host "Next: Steamworks > SteamPipe > Builds -> set the new build live on a branch (default or a beta)."
}
