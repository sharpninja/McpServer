$ErrorActionPreference = 'Stop'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-p0a-d0-out'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$utc = [datetime]::UtcNow
$utc.ToString('yyyy-MM-ddTHH:mm:ssZ') | Set-Content -LiteralPath (Join-Path $outDir 'utc.txt') -Encoding utf8

$nonce = [guid]::NewGuid().ToString('N')
$nonce | Set-Content -LiteralPath (Join-Path $outDir 'nonce-sent.txt') -Encoding utf8
try {
    $resp = Invoke-WebRequest -Uri ("http://PAYTON-LEGION2:7147/health?nonce=" + $nonce) -UseBasicParsing -TimeoutSec 20
    [string]$resp.StatusCode | Set-Content -LiteralPath (Join-Path $outDir 'health-status.txt') -Encoding utf8
    [string]$resp.Content | Set-Content -LiteralPath (Join-Path $outDir 'health-body.txt') -Encoding utf8
}
catch {
    $_ | Out-String | Set-Content -LiteralPath (Join-Path $outDir 'health-error.txt') -Encoding utf8
}

$planPath = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a02413-e4ee-7513-bd22-9eddd82adb73\plan.md'
$planInfo = Get-Item -LiteralPath $planPath
@{
    FullName = $planInfo.FullName
    LastWriteTimeUtc = $planInfo.LastWriteTimeUtc.ToString('o')
    Length = $planInfo.Length
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $outDir 'plan-meta.json') -Encoding utf8

$plan = Get-Content -LiteralPath $planPath -Raw
$d0Match = [regex]::Match($plan, '(?s)Complete reuse inventory \(must stay green; not new reds\):(.*?)### D1\. New red tests')
if (-not $d0Match.Success) {
    'D0_BLOCK_MISSING' | Set-Content -LiteralPath (Join-Path $outDir 'd0-block.txt') -Encoding utf8
    $d0Block = ''
}
else {
    $d0Block = $d0Match.Groups[1].Value
    $d0Block | Set-Content -LiteralPath (Join-Path $outDir 'd0-block.txt') -Encoding utf8
}

$d0Names = [regex]::Matches($d0Block, '`([A-Za-z][A-Za-z0-9_.]*)`') | ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique
@($d0Names | Sort-Object) | Set-Content -LiteralPath (Join-Path $outDir 'd0-names.txt') -Encoding utf8

$files = Get-ChildItem -Path 'F:\GitHub\McpServer\tests' -Recurse -Filter '*Handoff*.cs' -File
$methodRx = [regex]::new('(?ms)\[(?:Fact|Theory)(?:\([^\]]*\))?\](?:\s*\[[^\]]+\])*[\s\r\n]*(?:public\s+)?(?:async\s+)?(?:Task(?:<[^>]+>)?|ValueTask(?:<[^>]+>)?|void)\s+([A-Za-z_][A-Za-z0-9_]*)\s*\(')
$methods = New-Object System.Collections.Generic.List[object]
foreach ($f in $files) {
    $text = Get-Content -LiteralPath $f.FullName -Raw
    foreach ($m in $methodRx.Matches($text)) {
        $methods.Add([pscustomobject]@{
            File = $f.Name
            Method = $m.Groups[1].Value
            Relative = $f.FullName.Substring('F:\GitHub\McpServer\'.Length)
        })
    }
}

$classified = @($methods | Where-Object {
    $_.Method -ne 'Dispose' -and $_.Method -notlike 'Advance*' -and $_.Method -notlike 'SeedExisting*'
})
$classified | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $outDir 'handoff-methods.json') -Encoding utf8

function Test-MethodInD0 {
    param([string]$Method, [string[]]$Names)
    foreach ($n in $Names) {
        if ($n -eq $Method) { return $true }
        if ($n.EndsWith('.' + $Method)) { return $true }
    }
    return $false
}

$missing = New-Object System.Collections.Generic.List[string]
foreach ($row in $classified) {
    if (-not (Test-MethodInD0 -Method $row.Method -Names $d0Names)) {
        $missing.Add(($row.File + '::' + $row.Method))
    }
}
if ($missing.Count -eq 0) {
    'NONE' | Set-Content -LiteralPath (Join-Path $outDir 'missing-from-d0.txt') -Encoding utf8
}
else {
    @($missing | Sort-Object) | Set-Content -LiteralPath (Join-Path $outDir 'missing-from-d0.txt') -Encoding utf8
}

$classifiedNames = @($classified | ForEach-Object { $_.Method } | Select-Object -Unique)
$d0Extra = @($d0Names | Where-Object {
    $leaf = ($_ -split '\.')[-1]
    $classifiedNames -notcontains $leaf
} | Sort-Object)
if ($d0Extra.Count -eq 0) {
    'NONE' | Set-Content -LiteralPath (Join-Path $outDir 'd0-not-in-handoff-star-files.txt') -Encoding utf8
}
else {
    $d0Extra | Set-Content -LiteralPath (Join-Path $outDir 'd0-not-in-handoff-star-files.txt') -Encoding utf8
}

$dups = @($classified | Group-Object Method | Where-Object { $_.Count -gt 1 } | ForEach-Object { $_.Name + '=' + $_.Count })
if ($dups.Count -eq 0) { 'NONE' | Set-Content -LiteralPath (Join-Path $outDir 'duplicate-method-names.txt') -Encoding utf8 }
else { $dups | Set-Content -LiteralPath (Join-Path $outDir 'duplicate-method-names.txt') -Encoding utf8 }

$lines = Get-Content -LiteralPath $planPath
$auditLine = $lines[441]
$auditLine | Set-Content -LiteralPath (Join-Path $outDir 'd0-audit-line.txt') -Encoding utf8

$secretInPlan = $plan.Contains('IngestAsync_SecretInDraft_IsRedactedOnPersist')
$secretInD0 = $d0Block.Contains('IngestAsync_SecretInDraft_IsRedactedOnPersist')
$secretOnAuditLine = $auditLine.Contains('IngestAsync_SecretInDraft_IsRedactedOnPersist')
@{
    secretInPlan = $secretInPlan
    secretInD0Block = $secretInD0
    secretOnAuditLine = $secretOnAuditLine
    classifiedCount = $classified.Count
    factTheoryRawCount = $methods.Count
    d0NameCount = @($d0Names).Count
    missingCount = $missing.Count
    filesCount = @($files).Count
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $outDir 'summary.json') -Encoding utf8

Select-String -Path 'F:\GitHub\McpServer\plugins\core\lib-ps\repl-invoke.ps1' -Pattern 'return 2' |
    ForEach-Object { [string]$_.LineNumber + ':' + $_.Line.Trim() } |
    Set-Content -LiteralPath (Join-Path $outDir 'drain-return-2.txt') -Encoding utf8

$dbSets = Select-String -Path 'F:\GitHub\McpServer\src\McpServer.Storage\McpDbContext.cs' -Pattern 'public DbSet<[^>]+>\s+(\w+)' |
    ForEach-Object { $_.Matches[0].Groups[1].Value }
$dbSets | Set-Content -LiteralPath (Join-Path $outDir 'dbsets.txt') -Encoding utf8

$gMatch = [regex]::Match($plan, '(?s)### Locked tables\[\] \(McpDbContext DbSet CLR property names, inspected 2026-08-21\)(.*?)Import remaps rows')
$gBlock = if ($gMatch.Success) { $gMatch.Groups[1].Value } else { '' }
$planTables = @()
if ($gBlock) {
    $planTables = @(($gBlock -split '[,\r\n]+' | ForEach-Object { $_.Trim() } | Where-Object { $_ -match '^[A-Z][A-Za-z0-9]+$' }))
}
$planTables | Set-Content -LiteralPath (Join-Path $outDir 'plan-tables.txt') -Encoding utf8
$onlyDb = @($dbSets | Where-Object { $planTables -notcontains $_ })
$onlyPlan = @($planTables | Where-Object { $dbSets -notcontains $_ })
@{
    dbSetCount = @($dbSets).Count
    planTableCount = @($planTables).Count
    onlyInDb = $onlyDb
    onlyInPlan = $onlyPlan
} | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $outDir 'tables-diff.json') -Encoding utf8

$d1Names = @(
    'PluginHandoffSkill_InvokeIngestGetApprove_UsesDocumentedWorkflowMethods',
    'ProcessingLease_RenewsAndFencesTerminalUpdates',
    'Provenance_IncludesEffectiveCustomPromptIdentityAndVersion'
)
$d1Hits = @()
foreach ($n in $d1Names) {
    $hit = Select-String -Path 'F:\GitHub\McpServer\tests' -Pattern $n -SimpleMatch -ErrorAction SilentlyContinue
    $d1Hits += ($n + '=' + @($hit).Count)
}
$d1Hits | Set-Content -LiteralPath (Join-Path $outDir 'd1-newred-tests-hits.txt') -Encoding utf8

$agentPoolNewRed = $plan.Contains('AgentPool_DoesNotRetainRawHandoffSourceInPromptState')
$fr003 = $plan.Contains('FR-HANDOFF-003 field diagnostics')
$listedPrev = $plan.Contains('listed previously')
@{
    agentPoolNewRedNameInPlan = $agentPoolNewRed
    frHandoff003FieldDiagnostics = $fr003
    listedPreviously = $listedPrev
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $outDir 'plan-needles.json') -Encoding utf8

$noneHits = Select-String -Path 'F:\GitHub\McpServer\src' -Pattern 'CancellationToken\.None' -Include '*Handoff*' |
    ForEach-Object { [string]$_.Path + ':' + $_.LineNumber + ':' + $_.Line.Trim() }
if (-not $noneHits) { 'NONE' | Set-Content -LiteralPath (Join-Path $outDir 'src-handoff-cancellationtoken-none.txt') -Encoding utf8 }
else { $noneHits | Set-Content -LiteralPath (Join-Path $outDir 'src-handoff-cancellationtoken-none.txt') -Encoding utf8 }

$outside = Select-String -Path 'F:\GitHub\McpServer\tests' -Pattern 'public (async )?(Task|void) \w*Handoff\w*\(' -ErrorAction SilentlyContinue |
    Where-Object { $_.Path -notmatch 'Handoff[^\\/]*\.cs$' } |
    ForEach-Object { $_.Path.Substring('F:\GitHub\McpServer\'.Length) + ':' + $_.LineNumber + ':' + $_.Line.Trim() }
if (-not $outside) { 'NONE' | Set-Content -LiteralPath (Join-Path $outDir 'handoff-named-methods-outside-handoff-files.txt') -Encoding utf8 }
else { $outside | Set-Content -LiteralPath (Join-Path $outDir 'handoff-named-methods-outside-handoff-files.txt') -Encoding utf8 }

Write-Output ('UTC=' + $utc.ToString('yyyy-MM-ddTHH:mm:ssZ'))
Write-Output ('classified=' + $classified.Count)
Write-Output ('d0Names=' + @($d0Names).Count)
Write-Output ('missing=' + $missing.Count)
Write-Output ('missingList=' + ($(if ($missing.Count -eq 0) { 'NONE' } else { ($missing -join ';') })))
Write-Output ('secretOnAuditLine=' + $secretOnAuditLine)
Write-Output ('files=' + @($files).Count)
