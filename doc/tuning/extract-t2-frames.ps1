# Extract T2 limit change frames from Derail Valley Player.log → JSONL for local analysis.
# Usage:
#   powershell -ExecutionPolicy Bypass -File doc/tuning/extract-t2-frames.ps1
#   powershell -ExecutionPolicy Bypass -File doc/tuning/extract-t2-frames.ps1 -Version 0.5.65
param(
    [string] $LogPath = "$env:USERPROFILE\AppData\LocalLow\Altfuture\Derail Valley\Player.log",
    [string] $Version = "",
    [string] $OutDir = ""
)

$ErrorActionPreference = "Stop"
if (-not (Test-Path $LogPath)) { throw "Player.log not found: $LogPath" }

$repoTuning = Join-Path $PSScriptRoot "raw"
if (-not $OutDir) { $OutDir = $repoTuning }
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

if (-not $Version) {
    $vLine = Select-String -Path $LogPath -Pattern "Version '([\d.]+)'" | Select-Object -Last 1
    if ($vLine) { $Version = $vLine.Matches[0].Groups[1].Value } else { $Version = "unknown" }
}

$out = Join-Path $OutDir ("v{0}_frames.jsonl" -f $Version)
$n = 0
$sw = New-Object System.IO.StreamWriter($out, $false, [Text.UTF8Encoding]::new($false))
try {
    foreach ($line in [IO.File]::ReadLines($LogPath)) {
        if ($line -notmatch 'T2 limit change:') { continue }
        $o = [ordered]@{ version = $Version }
        if ($line -match 'Speed (\d+) km/h') { $o.spd = [int]$Matches[1] }
        if ($line -match '\|\s+Limit ([^|]+?)\s+\|') { $o.limit = $Matches[1].Trim() }
        if ($line -match '\|\s+adv=([^|]+?)\s+\|') { $o.adv = $Matches[1].Trim() }
        if ($line -match 'posted=(\d+)') { $o.posted = [int]$Matches[1] }
        if ($line -match 'rec=(\d+)') { $o.rec = [int]$Matches[1] }
        if ($line -match 'agg=([\d.]+)') { $o.agg = [double]$Matches[1] }
        if ($line -match 'geoScale=([\d.]+)') { $o.geoScale = [double]$Matches[1] }
        if ($line -match 'src=([A-Za-z0-9_\-]+|—)') { $o.src = $Matches[1] }
        if ($line -match 'src=\S+ along=(\d+)') { $o.along = [int]$Matches[1] }
        elseif ($line -match 'along=(\d+)') { $o.along = [int]$Matches[1] }
        if ($line -match ' lead=(\d+)') { $o.lead = [int]$Matches[1] }
        if ($line -match 'headroom=([^ ;\r\n]+)') { $o.headroom = $Matches[1] }
        if ($line -match 'suggest=([^ ;\r\n]+)') { $o.suggest = $Matches[1] }
        if ($line -match ' min=([^ ;\r\n]+)') { $o.min = $Matches[1] }
        if ($line -match ' ahead=\d+ geo=(\d+) ') { $o.geoCount = [int]$Matches[1] }
        if ($line -match 'stress=([^ ;\r\n]+)') { $o.stress = $Matches[1] }
        if ($line -match 'build=([^ ;\r\n]+)') { $o.build = $Matches[1] }
        if ($line -match 'curveNow=([^ ;\r\n]+)') { $o.curveNow = $Matches[1] }
        if ($line -match 'curveAhead=([^ ;\r\n]+)') { $o.curveAhead = $Matches[1] }
        if ($line -match 'grade=([-\d.]+)%') { $o.grade = [double]$Matches[1] }
        if ($line -match 'planGrade=([-\d.]+)%') { $o.planGrade = [double]$Matches[1] }
        $sw.WriteLine(($o | ConvertTo-Json -Compress))
        $n++
    }
}
finally { $sw.Close() }

Write-Host "Wrote $n frames → $out"
