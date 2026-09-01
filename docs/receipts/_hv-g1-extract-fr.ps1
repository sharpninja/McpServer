$ErrorActionPreference = 'Stop'
$dump = Get-ChildItem -LiteralPath 'C:\Users\kingd\.grok\sessions' -Recurse -Filter 'call-19a3d216-91a6-4d05-9f9c-fb03d037defa-87.json' -ErrorAction SilentlyContinue | Select-Object -First 1
if ($null -eq $dump) { throw 'FR dump not found' }
$raw = Get-Content -LiteralPath $dump.FullName -Raw -Encoding UTF8
Write-Output ('DUMP=' + $dump.FullName)
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
$ids = @(
    'FR-MCP-TRIAGESTORE-001',
    'FR-MCP-SESSIONLOGCTX-001',
    'FR-MCP-TRIAGEPLUGIN-001',
    'FR-MCP-TRIAGE-002'
)
$wanted = $items | Where-Object { $ids -contains $_.Id }
$dest = 'F:\GitHub\McpServer\docs\receipts\_hv-g1-fr-extract.json'
$out = [ordered]@{
    totalItems = $items.Count
    wantedCount = @($wanted).Count
    wanted = @($wanted | ForEach-Object {
        [ordered]@{
            Id = $_.Id
            Title = $_.Title
            Status = $_.Status
            Body = $_.Body
            AcceptanceCriteria = @($_.AcceptanceCriteria | ForEach-Object {
                [ordered]@{ id = $_.id; text = $_.text; isSatisfied = $_.isSatisfied }
            })
        }
    })
}
($out | ConvertTo-Json -Depth 12) | Set-Content -LiteralPath $dest -Encoding UTF8
Write-Output "total=$($items.Count) wanted=$($out.wantedCount)"
foreach ($w in $out.wanted) {
    Write-Output ("ID={0} STATUS={1} AC={2} TITLE={3}" -f $w.Id, $w.Status, @($w.AcceptanceCriteria).Count, $w.Title)
    foreach ($ac in $w.AcceptanceCriteria) {
        Write-Output ("  AC {0} sat={1} {2}" -f $ac.id, $ac.isSatisfied, $ac.text)
    }
}
