$ErrorActionPreference = 'Stop'
$dump = Get-ChildItem -LiteralPath 'C:\Users\kingd\.grok\sessions' -Recurse -Filter 'call-696111cb-56e6-45d5-a960-cfcd1a008c24-113.json' -ErrorAction SilentlyContinue | Select-Object -First 1
if ($null -eq $dump) { throw 'mapping dump not found' }
$raw = Get-Content -LiteralPath $dump.FullName -Raw -Encoding UTF8
$doc = $raw | ConvertFrom-Json
$items = @()
if ($doc.items) { $items = @($doc.items) }
elseif ($doc.result.items) { $items = @($doc.result.items) }
if ($items.Count -eq 0) {
    foreach ($p in $doc.PSObject.Properties) {
        if ($p.Value -is [string] -and $p.Value.Length -gt 100) {
            try {
                $inner = $p.Value | ConvertFrom-Json
                if ($inner.items) { $items = @($inner.items); break }
            } catch {}
        }
    }
}
$ids = @('FR-MCP-TRIAGESTORE-001', 'FR-MCP-SESSIONLOGCTX-001', 'FR-MCP-TRIAGE-002', 'FR-MCP-TRIAGEPLUGIN-001')
$wanted = $items | Where-Object { $ids -contains $_.FrId }
$out = [ordered]@{
    dump = $dump.FullName
    total = $items.Count
    wanted = @($wanted)
}
$dest = 'F:\GitHub\McpServer\docs\receipts\_hv-g1-map-extract.json'
($out | ConvertTo-Json -Depth 8) | Set-Content -LiteralPath $dest -Encoding UTF8
Write-Output ('total=' + $items.Count + ' wanted=' + @($wanted).Count)
$wanted | ForEach-Object {
    Write-Output ('FR=' + $_.FrId + ' TR=' + ($_.TrIds -join ',') + ' TEST=' + ($_.TestIds -join ','))
}
