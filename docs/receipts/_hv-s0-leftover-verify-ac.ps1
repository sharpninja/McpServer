#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$plugin = 'F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1'
$outDir = Join-Path $workspace 'docs\receipts\_hv-s0-leftover-verify'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

Set-Location -LiteralPath $workspace

Write-Output '=== PLUGIN STATUS ==='
& $plugin -Command Status -WorkspacePath $workspace | Tee-Object -FilePath (Join-Path $outDir 'plugin-status.txt')

Write-Output '=== MARKER SIGNATURE ==='
. (Join-Path $workspace 'plugins\core\lib-ps\marker-resolver.ps1')
$sig = Test-MarkerSignature -MarkerFile (Join-Path $workspace 'AGENTS-README-FIRST.yaml')
"SignatureValid=$sig" | Tee-Object -FilePath (Join-Path $outDir 'signature.txt')

$nonce = [guid]::NewGuid().ToString('N')
$health = Invoke-RestMethod -Uri ("http://PAYTON-LEGION2:7147/health?nonce=$nonce") -TimeoutSec 20
$healthObj = [ordered]@{
    Status = [string]$health.status
    Version = [string]$health.version
    Storage = [string]$health.storage
    NonceSent = $nonce
    NonceEcho = [string]$health.nonce
    NonceOk = ($health.nonce -eq $nonce)
}
$healthObj | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $outDir 'health.json') -Encoding utf8
"HealthStatus=$($healthObj.Status)"
"NonceSent=$nonce"
"NonceEcho=$($healthObj.NonceEcho)"
"NonceOk=$($healthObj.NonceOk)"
"Version=$($healthObj.Version)"
"Storage=$($healthObj.Storage)"

$ids = @(
    'SESSIONATTR',
    'FAILSAFE',
    'STRICTCOUNT',
    'XAGENT',
    'SESSIONEND',
    'VERIFYWRAP',
    'TRANSCRIPT-SEARCH',
    'TEMPVOL'
)

$summary = [System.Collections.Generic.List[object]]::new()

function Invoke-GetReq {
    param(
        [string]$Method,
        [string]$Id
    )
    $paramsPath = Join-Path $outDir ("params-$Method-$Id.yaml")
    $yaml = @"
id: $Id
"@
    Set-Content -LiteralPath $paramsPath -Value $yaml -Encoding utf8
    $raw = & $plugin -Command Invoke -Method $Method -ParamsPath $paramsPath -WorkspacePath $workspace
    $raw | Set-Content -LiteralPath (Join-Path $outDir ("$Method-$Id.txt")) -Encoding utf8
    return $raw
}

function Get-AcCount {
    param($Obj)
    if ($null -eq $Obj) { return 0 }
    $ac = $null
    foreach ($name in @('acceptanceCriteria', 'AcceptanceCriteria')) {
        $p = $Obj.PSObject.Properties[$name]
        if ($null -ne $p -and $null -ne $p.Value) {
            $ac = $p.Value
            break
        }
    }
    if ($null -eq $ac) { return 0 }
    if ($ac -is [System.Array]) { return @($ac).Count }
    if ($ac -is [System.Collections.IEnumerable] -and -not ($ac -is [string])) {
        return @($ac).Count
    }
    return 0
}

foreach ($slug in $ids) {
    $frId = "FR-MCP-$slug-001"
    $trId = "TR-MCP-$slug-001"
    $testId = "TEST-MCP-$slug-001"

    $frRaw = Invoke-GetReq -Method 'workflow.requirements.getFr' -Id $frId
    $trRaw = Invoke-GetReq -Method 'workflow.requirements.getTr' -Id $trId
    $testRaw = Invoke-GetReq -Method 'workflow.requirements.getTest' -Id $testId

    $frObj = $null
    $trObj = $null
    $testObj = $null
    try { $frObj = $frRaw | ConvertFrom-Json -ErrorAction Stop } catch {}
    try { $trObj = $trRaw | ConvertFrom-Json -ErrorAction Stop } catch {}
    try { $testObj = $testRaw | ConvertFrom-Json -ErrorAction Stop } catch {}

    $row = [ordered]@{
        Slug = $slug
        FrId = $frId
        TrId = $trId
        TestId = $testId
        FrAc = (Get-AcCount $frObj)
        TrAc = (Get-AcCount $trObj)
        TestAc = (Get-AcCount $testObj)
        FrOk = [bool]($frRaw -match $frId)
        TrOk = [bool]($trRaw -match $trId)
        TestOk = [bool]($testRaw -match $testId)
    }
    $summary.Add([pscustomobject]$row)
    Write-Output ("AC {0}: FR={1} TR={2} TEST={3}" -f $slug, $row.FrAc, $row.TrAc, $row.TestAc)
}

$summary | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $outDir 'ac-summary.json') -Encoding utf8
Write-Output '=== AC SUMMARY WRITTEN ==='
