<#
.SYNOPSIS
  upload.ps1 in a console window the person types into: login, then steamcmd's own password and Steam Guard prompts.
  For an agent, whose shell has no console input (Docs/STEAM.md 7.2). Nothing is set live.

.DESCRIPTION
  Start it in a new window:
    Start-Process powershell.exe -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-File','Tools\Steam\upload_window.ps1','-Desc','Refluff 0.9.2: ...'
  On "Invalid Password" (steamcmd exit code 5) it offers another try in the same window. When it is done it writes
  Tools/Steam/output/upload_status.txt ("exit=<code>" or "error=<message>"); the build id is in
  Tools/Steam/output/app_build_5304930.log ("Successfully finished AppID 5304930 build (BuildID N)").
#>
param([string]$Desc = "")

$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$status = Join-Path $here 'output\upload_status.txt'
New-Item -ItemType Directory -Force (Split-Path $status) | Out-Null
Remove-Item $status -ErrorAction SilentlyContinue

$Host.UI.RawUI.WindowTitle = 'Refluff -> Steam upload (nothing is set live)'
Write-Host ''
Write-Host 'Refluff -> Steam, app 5304930 / depot 5304931, from Builds\Steam\content\windows' -ForegroundColor Cyan
Write-Host 'Upload only: the build lands in SteamPipe > Builds WITHOUT a branch. Nothing goes live, nothing is published.' -ForegroundColor Cyan
Write-Host ''
Write-Host '1. Type the Steam login below and press Enter.' -ForegroundColor Yellow
Write-Host '2. WAIT until steamcmd prints "password:", then type the password (it stays invisible) and press Enter.' -ForegroundColor Yellow
Write-Host '   A login steamcmd has cached skips this step.' -ForegroundColor Yellow
Write-Host '3. Then the Steam Guard code, or confirm the login in the Steam Mobile app.' -ForegroundColor Yellow
Write-Host ''
$acc = ''
$code = -1
while ($true) {
    $prev = $acc
    $acc = (Read-Host $(if ($prev) { 'Steam login (Enter = same as before)' } else { 'Steam login of the Steamworks account' })).Trim()
    if ($acc -eq '') { $acc = $prev }
    if ($acc -eq '') { continue }
    try {
        & (Join-Path $here 'upload.ps1') -Account $acc -Desc $Desc
        $code = $LASTEXITCODE
    }
    catch {
        [IO.File]::WriteAllText($status, "error=$($_.Exception.Message)")
        Write-Host $_.Exception.Message -ForegroundColor Red
        break
    }
    if ($code -eq 0) { break }
    Write-Host ''
    Write-Host "steamcmd exit code $code$(if ($code -eq 5) { ' = Invalid Password (wrong login or password)' })" -ForegroundColor Red
    if ((Read-Host 'Try again? y / n') -notmatch '^[yY]') { break }
}
if ($code -ne -1 -and -not (Test-Path $status)) { [IO.File]::WriteAllText($status, "exit=$code") }
Write-Host ''
Write-Host 'Done. The result is in Tools\Steam\output; this window can be closed.' -ForegroundColor Green
Read-Host 'Press Enter to close'
