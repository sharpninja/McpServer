#Requires -Version 7.0
[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$wt = 'F:\GitHub\McpServer\.worktrees\triage-plugin-core'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-s2-resume'
$out = Join-Path $outDir '07-live-diskfull.json'
$lib = Join-Path $wt 'plugins\core\lib-ps'
$scratch = Join-Path $outDir 'scratch-diskfull'
if (Test-Path -LiteralPath $scratch) { Remove-Item -LiteralPath $scratch -Recurse -Force }
[void][System.IO.Directory]::CreateDirectory($scratch)

$hookSource = Get-Content -LiteralPath (Join-Path $lib 'plugin-hook.ps1') -Raw
$diskFn = [regex]::Match($hookSource, '(?ms)^function Test-PluginDiskFullException \{.*?^\}').Value
$statusFn = [regex]::Match($hookSource, '(?ms)^function Get-PluginCodeVerifyFailureStatus \{.*?^\}').Value
$guardFn = [regex]::Match($hookSource, '(?ms)^function Invoke-PluginCodeVerifyHandleDiskFull \{.*?^\}').Value
Invoke-Expression $diskFn
Invoke-Expression $statusFn
Invoke-Expression $guardFn

$turnFile = Join-Path $scratch 'current-turn.yaml'
$original = @(
    'turnRequestId: req-20260819T000000Z-001-diskfull'
    'status: in_progress'
    'auditActions: 2'
    'auditDialog: 1'
    'lastBuildStatus: unknown'
)
Set-Content -LiteralPath $turnFile -Value $original -Encoding utf8
$beforeHash = (Get-FileHash -LiteralPath $turnFile -Algorithm SHA256).Hash
$beforeRaw = Get-Content -LiteralPath $turnFile -Raw

$disk = [System.IO.IOException]::new('There is not enough space on the disk.')
$disk.HResult = -2147024784
$guard = Invoke-PluginCodeVerifyHandleDiskFull -TurnFile $turnFile -Exception $disk

$afterExists = Test-Path -LiteralPath $turnFile
$afterRaw = if ($afterExists) { Get-Content -LiteralPath $turnFile -Raw } else { '' }
$afterHash = if ($afterExists) { (Get-FileHash -LiteralPath $turnFile -Algorithm SHA256).Hash } else { $null }
$parsed = $null
$parseOk = $false
try {
    $parsed = ConvertFrom-Yaml -Yaml $afterRaw
    $parseOk = $true
} catch {
    try {
        . (Join-Path $lib 'yaml-object-mutation.ps1')
        Import-McpYamlSerializer
        $parsed = Read-McpYamlObject -Path $turnFile
        $parseOk = $true
    } catch {
        $parseOk = $false
    }
}

$auditActions = $null
$lastBuild = $null
if ($parseOk -and $parsed) {
    if ($parsed -is [hashtable] -or $parsed -is [System.Collections.Specialized.OrderedDictionary]) {
        $auditActions = $parsed['auditActions']
        $lastBuild = $parsed['lastBuildStatus']
    } else {
        if ($parsed.PSObject.Properties.Name -contains 'auditActions') { $auditActions = $parsed.auditActions }
        if ($parsed.PSObject.Properties.Name -contains 'lastBuildStatus') { $lastBuild = $parsed.lastBuildStatus }
    }
}

$obj = [ordered]@{
    TimestampUtc = [datetime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    GuardCode = [string]$guard.code
    GuardStatus = [string]$guard.status
    AfterExists = $afterExists
    ParseOk = $parseOk
    BeforeHash = $beforeHash
    AfterHash = $afterHash
    HashUnchanged = ($beforeHash -eq $afterHash)
    AuditActions = $auditActions
    LastBuildStatus = $lastBuild
    AfterRaw = $afterRaw
    BeforeRaw = $beforeRaw
    GuardFnMutatesFile = ($guardFn -match 'Set-YamlScalar|WriteAllText|Write-McpYamlObject|Set-Content')
}
$obj | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $out -Encoding utf8
Write-Output ("WROTE {0} code={1} exists={2} parse={3} hashSame={4} audit={5} lastBuild={6}" -f $out, $obj.GuardCode, $afterExists, $parseOk, $obj.HashUnchanged, $auditActions, $lastBuild)
