# stills.ps1 'V("name", px,py,pz, tx,ty,tz, fov, focusM); ...'  - look_on.inc + q_stills.body with these views, one eval.
param([string]$views)
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$b = (Get-Content "$here\q_stills.body" -Raw).Replace('STILLS', $views)
$tmp = "$env:TEMP\tt_stills.cs"
(Get-Content "$here\look_on.inc" -Raw) + $b | Set-Content -Encoding utf8 $tmp
unity command eval_file --file $tmp 2>&1 | Select-Object -Last 1
