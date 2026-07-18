param (
    [switch]$NoArchive,
    [string]$OutputDirectory = $PSScriptRoot
)

Set-Location "$PSScriptRoot"
$FilesToInclude = "info.json", "build/YardMasterSuite.dll"

$modInfo = Get-Content -Raw -Path "info.json" | ConvertFrom-Json
$modId = $modInfo.Id
$modVersion = $modInfo.Version

$DistDir = "$OutputDirectory/dist"
if ($NoArchive) {
    $ZipWorkDir = "$OutputDirectory"
} else {
    $ZipWorkDir = "$DistDir/tmp"
}
$ZipOutDir = "$ZipWorkDir/$modId"

New-Item "$ZipOutDir" -ItemType Directory -Force | Out-Null
Get-ChildItem -Path $ZipOutDir -Filter "*.cache" -ErrorAction SilentlyContinue | Remove-Item -Force
# Remove stale sibling Core from older deploys (logic is now inside YardMasterSuite.dll).
Remove-Item -Force -ErrorAction SilentlyContinue "$ZipOutDir/YardMasterSuite.Core.dll"
Copy-Item -Force -Path $FilesToInclude -Destination "$ZipOutDir"

if (!$NoArchive) {
    $FILE_NAME = "$DistDir/${modId}_v$modVersion.zip"
    New-Item "$DistDir" -ItemType Directory -Force | Out-Null
    if (Test-Path $FILE_NAME) { Remove-Item $FILE_NAME -Force }
    Compress-Archive -CompressionLevel Fastest -Path "$ZipOutDir/*" -DestinationPath "$FILE_NAME"
    Remove-Item -Recurse -Force "$DistDir/tmp"
    Write-Host "Packaged: $FILE_NAME"
} else {
    Write-Host "Copied to: $ZipOutDir"
}
