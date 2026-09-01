#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$outDir = Join-Path $workspace 'docs\receipts\_hv-234800Z'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
Set-Location -LiteralPath $workspace

$trust = [ordered]@{}
$trust['utcNow'] = [DateTime]::UtcNow.ToString('o')
$trust['cwd'] = (Get-Location).ProviderPath

$pluginJson = Get-Content -LiteralPath 'F:\GitHub\mcpserver-grok-plugin\.grok-plugin\plugin.json' -Raw | ConvertFrom-Json
$trust['pluginJsonVersion'] = [string]$pluginJson.version
$trust['pluginDotVersion'] = (Get-Content -LiteralPath 'F:\GitHub\mcpserver-grok-plugin\.version' -Raw).Trim()

. '.\plugins\core\lib-ps\marker-resolver.ps1'
$sig = Test-MarkerSignature -MarkerFile (Join-Path $workspace 'AGENTS-README-FIRST.yaml')
$trust['markerSignature'] = [bool]$sig

$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$bytes = New-Object byte[] 16
$rng.GetBytes($bytes)
$nonce = [System.BitConverter]::ToString($bytes).Replace('-', '').ToLowerInvariant()
$trust['nonce'] = $nonce

$marker = Get-Content -LiteralPath (Join-Path $workspace 'AGENTS-README-FIRST.yaml')
$apiKey = ($marker | Select-String '^apiKey:\s*(.+)$').Matches[0].Groups[1].Value
$uri = 'http://PAYTON-LEGION2:7147/health?nonce=' + $nonce
$http = Invoke-WebRequest -Uri $uri -Headers @{ 'X-Api-Key' = $apiKey } -TimeoutSec 30
$trust['healthStatusCode'] = [int]$http.StatusCode
$trust['healthBody'] = [string]$http.Content
$resp = $http.Content | ConvertFrom-Json
$trust['nonceMatch'] = ($resp.nonce -eq $nonce)
$trust['healthStatus'] = [string]$resp.status
$trust['healthVersion'] = [string]$resp.version
if ($resp.PSObject.Properties.Name -contains 'storage') {
    $trust['storage'] = ($resp.storage | ConvertTo-Json -Compress)
}

$drive = Get-PSDrive -Name F
$trust['fFreeGB'] = [math]::Round(($drive.Free / 1GB), 2)
$trust['fUsedGB'] = [math]::Round((($drive.Used) / 1GB), 2)

$hredMd = Join-Path $workspace 'docs\receipts\hostile-validator-20260818T233800Z.md'
$hredJson = Join-Path $workspace 'docs\receipts\hostile-validator-20260818T233800Z.json'
$trust['hredMdExists'] = [bool](Test-Path -LiteralPath $hredMd)
$trust['hredJsonExists'] = [bool](Test-Path -LiteralPath $hredJson)
if ($trust['hredMdExists']) {
    $hredItem = Get-Item -LiteralPath $hredMd
    $trust['hredMdLastWriteUtc'] = $hredItem.LastWriteTimeUtc.ToString('o')
    $trust['hredMdLength'] = $hredItem.Length
    $hredText = Get-Content -LiteralPath $hredMd -Raw
    $trust['hredMdHasOverallAgree'] = ($hredText -match 'OverallVerdict\s*:\s*AGREE') -or ($hredText -match '## OverallVerdict\s+AGREE')
}
if ($trust['hredJsonExists']) {
    $hredObj = Get-Content -LiteralPath $hredJson -Raw | ConvertFrom-Json
    $trust['hredJsonOverall'] = [string]$hredObj.OverallVerdict
    $trust['hredJsonPass'] = $hredObj.Counts.PASS
    $trust['hredJsonFail'] = $hredObj.Counts.FAIL
}

$scratch = 'C:\Users\kingd\AppData\Local\Temp\grok-goal-01353e344a72\implementer\s2-tests.log'
$trust['scratchExists'] = [bool](Test-Path -LiteralPath $scratch)
if ($trust['scratchExists']) {
    $s = Get-Item -LiteralPath $scratch
    $trust['scratchLength'] = $s.Length
    $trust['scratchLastWriteUtc'] = $s.LastWriteTimeUtc.ToString('o')
}

$trust | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $outDir 'trust.json') -Encoding utf8
Write-Output ('TRUST_WROTE ' + (Join-Path $outDir 'trust.json'))
Write-Output ('SIG=' + $trust['markerSignature'])
Write-Output ('NONCE_MATCH=' + $trust['nonceMatch'])
Write-Output ('HEALTH=' + $trust['healthStatus'])
Write-Output ('VERSION=' + $trust['healthVersion'])
Write-Output ('FREE_GB=' + $trust['fFreeGB'])
Write-Output ('HRED_AGREE=' + $trust['hredMdHasOverallAgree'])
Write-Output ('HRED_JSON=' + $trust['hredJsonOverall'])
Write-Output ('SCRATCH=' + $trust['scratchExists'] + ' LEN=' + $trust['scratchLength'])
