#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-g3-113-post-deploy'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

. 'F:\GitHub\mcpserver-grok-plugin\lib\marker-resolver.ps1'

$markerFile = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml'
$sigOk = Test-MarkerSignature -MarkerFile $markerFile
$apiKey = Get-MarkerField -MarkerFile $markerFile -FieldName 'apiKey'
$baseUrl = Get-MarkerField -MarkerFile $markerFile -FieldName 'baseUrl'
$nonce = [guid]::NewGuid().ToString('N')
$health = Invoke-RestMethod -Uri "$baseUrl/health?nonce=$nonce" -TimeoutSec 15
$nonceOk = $health.nonce -eq $nonce

$manifestPath = 'C:\ProgramData\McpServer\.mcpservice-deployment.json'
$manifest = $null
$manifestRaw = $null
if (Test-Path -LiteralPath $manifestPath) {
    $manifestRaw = Get-Content -LiteralPath $manifestPath -Raw
    $manifest = $manifestRaw | ConvertFrom-Json
}

$svc = Get-Service -Name 'McpServer' -ErrorAction SilentlyContinue
$imagePath = (Get-ItemProperty -LiteralPath 'HKLM:\SYSTEM\CurrentControlSet\Services\McpServer' -Name ImagePath -ErrorAction SilentlyContinue).ImagePath

$tempScript = 'C:\Users\kingd\AppData\Local\Temp\hv-113-update-service.ps1'
$tempExists = Test-Path -LiteralPath $tempScript
$tempText = if ($tempExists) { Get-Content -LiteralPath $tempScript -Raw } else { $null }

$gitVersion = Get-Content -LiteralPath 'F:\GitHub\McpServer\GitVersion.yml' -Raw
$gitVersionMatch = [regex]::Match($gitVersion, '(?m)^next-version:\s*(.+)$')
$nextVersion = if ($gitVersionMatch.Success) { $gitVersionMatch.Groups[1].Value.Trim() } else { $null }

$backupDir = Join-Path $env:USERPROFILE 'McpServer-Backups'
$latestBackup = $null
if (Test-Path -LiteralPath $backupDir) {
    $latestBackup = Get-ChildItem -LiteralPath $backupDir -Filter 'McpServer-backup-*.zip' |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1
}

$copyItemHits = @()
$nukeHits = @()
if ($tempText) {
    $copyItemHits = @([regex]::Matches($tempText, 'Copy-Item') | ForEach-Object { $_.Value })
    $nukeHits = @([regex]::Matches($tempText, 'build\.ps1[''\s]+UpdateService|UpdateService') | ForEach-Object { $_.Value })
}

$result = [ordered]@{
    utc = [DateTime]::UtcNow.ToString('o')
    signatureOk = [bool]$sigOk
    healthStatus = [string]$health.status
    healthVersion = [string]$health.version
    healthStorage = [string]$health.storage
    nonceSent = $nonce
    nonceEchoed = [string]$health.nonce
    nonceOk = [bool]$nonceOk
    serviceName = if ($svc) { $svc.Name } else { $null }
    serviceStatus = if ($svc) { [string]$svc.Status } else { $null }
    imagePath = $imagePath
    manifestPath = $manifestPath
    manifestExists = [bool]$manifest
    generatedBy = if ($manifest) { [string]$manifest.generatedBy } else { $null }
    generatedUtc = if ($manifest) { [string]$manifest.generatedUtc } else { $null }
    operation = if ($manifest) { [string]$manifest.operation } else { $null }
    gitVersionNext = $nextVersion
    tempScriptPath = $tempScript
    tempScriptExists = $tempExists
    tempScriptCopyItemCount = $copyItemHits.Count
    tempScriptUpdateServiceHitCount = $nukeHits.Count
    latestBackup = if ($latestBackup) { $latestBackup.FullName } else { $null }
    latestBackupUtc = if ($latestBackup) { $latestBackup.LastWriteTimeUtc.ToString('o') } else { $null }
    pluginStatus = 'available'
    pluginRoot = 'F:\GitHub\mcpserver-grok-plugin'
}

$jsonPath = Join-Path $outDir 'summary.json'
($result | ConvertTo-Json -Depth 6) | Set-Content -LiteralPath $jsonPath -Encoding utf8
if ($manifestRaw) {
    $manifestRaw | Set-Content -LiteralPath (Join-Path $outDir 'mcpservice-deployment.json') -Encoding utf8
}
if ($tempText) {
    $tempText | Set-Content -LiteralPath (Join-Path $outDir 'hv-113-update-service.ps1.txt') -Encoding utf8
}

Write-Output ("SIG_OK={0}" -f $result.signatureOk)
Write-Output ("NONCE_OK={0}" -f $result.nonceOk)
Write-Output ("HEALTH_STATUS={0}" -f $result.healthStatus)
Write-Output ("HEALTH_VERSION={0}" -f $result.healthVersion)
Write-Output ("SERVICE_STATUS={0}" -f $result.serviceStatus)
Write-Output ("GENERATED_BY={0}" -f $result.generatedBy)
Write-Output ("GENERATED_UTC={0}" -f $result.generatedUtc)
Write-Output ("GITVERSION_NEXT={0}" -f $result.gitVersionNext)
Write-Output ("TEMP_SCRIPT_EXISTS={0}" -f $result.tempScriptExists)
Write-Output ("TEMP_COPYITEM_COUNT={0}" -f $result.tempScriptCopyItemCount)
Write-Output ("TEMP_UPDATESERVICE_HITS={0}" -f $result.tempScriptUpdateServiceHitCount)
Write-Output ("LATEST_BACKUP={0}" -f $result.latestBackup)
Write-Output ("SUMMARY={0}" -f $jsonPath)
