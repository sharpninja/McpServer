$ErrorActionPreference = 'Stop'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-225300Z'
New-Item -ItemType Directory -Path $outDir -Force | Out-Null

Set-Location 'F:\GitHub\McpServer'
. 'F:\GitHub\mcpserver-grok-plugin\lib\marker-resolver.ps1'
$marker = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml'
$sig = $null
try {
    $sig = Test-MarkerSignature -MarkerFile $marker
} catch {
    $sig = "ERR:$($_.Exception.Message)"
}
"SIG=$sig" | Set-Content -LiteralPath (Join-Path $outDir 'signature.txt')

$pluginJson = Get-Content 'F:\GitHub\mcpserver-grok-plugin\.grok-plugin\plugin.json' -Raw
$pluginVer = Get-Content 'F:\GitHub\mcpserver-grok-plugin\.version' -ErrorAction SilentlyContinue
@"
pluginJson=$pluginJson
versionFile=$pluginVer
"@ | Set-Content -LiteralPath (Join-Path $outDir 'plugin-identity.txt')

$reqDump = Get-ChildItem 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a01716-0672-7030-855a-d8698be65893\mcp' -Filter 'call-53603eb8-c282-4304-9964-e735d33198f6-63.json' | Select-Object -First 1
$raw = Get-Content -LiteralPath $reqDump.FullName -Raw
$parsed = $raw | ConvertFrom-Json
$items = $null
if ($parsed.items) { $items = $parsed.items }
elseif ($parsed.result.items) { $items = $parsed.result.items }
elseif ($parsed.content) {
    $text = $parsed.content
    if ($text -is [array]) { $text = ($text | ForEach-Object { $_.text }) -join '' }
    $inner = $text | ConvertFrom-Json
    if ($inner.items) { $items = $inner.items }
    elseif ($inner.result.items) { $items = $inner.result.items }
}
$wanted = @(
    'TEST-MCP-TRIAGEERR-001',
    'TEST-MCP-TRIAGESTORE-001','TEST-MCP-TRIAGESTORE-002','TEST-MCP-TRIAGESTORE-003',
    'TEST-MCP-TRIAGESTORE-004','TEST-MCP-TRIAGESTORE-005','TEST-MCP-TRIAGESTORE-006','TEST-MCP-TRIAGESTORE-007',
    'TEST-MCP-TRIAGEPLUGIN-001','TEST-MCP-TRIAGEPLUGIN-002','TEST-MCP-TRIAGEPLUGIN-003','TEST-MCP-TRIAGEPLUGIN-004','TEST-MCP-TRIAGEPLUGIN-005',
    'TEST-MCP-TRIAGESCHEMA-001','TEST-MCP-TRIAGETODO-002','TEST-MCP-TRIAGEHELP-001','TEST-MCP-TRIAGEREQ-001'
)
$rows = @()
foreach ($id in $wanted) {
    $hit = $items | Where-Object { $_.Id -eq $id }
    if (-not $hit) {
        $rows += [pscustomobject]@{ Id = $id; Found = $false; AcCount = 0; Ac1 = '' }
        continue
    }
    $acs = @($hit.AcceptanceCriteria)
    $ac1 = ''
    if ($acs.Count -gt 0) { $ac1 = [string]$acs[0].text }
    $rows += [pscustomobject]@{
        Id = $id
        Found = $true
        Status = [string]$hit.Status
        AcCount = $acs.Count
        Ac1Len = $ac1.Length
        Ac1 = $ac1
        Condition = [string]$hit.Condition
    }
}
$rows | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $outDir 'test-ac.json')

$scratch = 'C:\Users\kingd\AppData\Local\Temp\grok-goal-01353e344a72\implementer\s2-tests.log'
if (Test-Path -LiteralPath $scratch) {
    $item = Get-Item -LiteralPath $scratch
    $tail = Get-Content -LiteralPath $scratch -Tail 80
    @"
exists=true
length=$($item.Length)
lastWriteUtc=$($item.LastWriteTimeUtc.ToString('o'))
TAIL:
$($tail -join "`n")
"@ | Set-Content -LiteralPath (Join-Path $outDir 'scratch-log.txt')
} else {
    'exists=false' | Set-Content -LiteralPath (Join-Path $outDir 'scratch-log.txt')
}

Write-Output "collect-ok acRows=$($rows.Count) sig=$sig"
