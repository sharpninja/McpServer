$ErrorActionPreference = 'Stop'
$path = 'C:\Users\kingd\.grok\sessions\F%3A\GitHub\McpServer\01a01b4f-1f61-7d33-8313-e19bdd66b6b8\mcp\call-50d1dcd4-d845-4bcf-8b73-391de0e57227-59.json'
if (-not (Test-Path -LiteralPath $path)) {
    $alt = Get-ChildItem -LiteralPath 'C:\Users\kingd\.grok\sessions' -Recurse -Filter 'call-50d1dcd4-d845-4bcf-8b73-391de0e57227-59.json' -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($null -eq $alt) { throw "MCP dump not found: $path" }
    $path = $alt.FullName
}
$raw = Get-Content -LiteralPath $path -Raw -Encoding UTF8
$doc = $raw | ConvertFrom-Json
$items = @()
if ($doc.items) { $items = @($doc.items) }
elseif ($doc.result.items) { $items = @($doc.result.items) }
elseif ($doc.content) {
    $text = if ($doc.content -is [string]) { $doc.content } else { ($doc.content | ConvertTo-Json -Depth 100 -Compress) }
    try { $inner = $text | ConvertFrom-Json; if ($inner.items) { $items = @($inner.items) } } catch {}
}
# Tool wrappers often nest the MCP payload as a string field.
if ($items.Count -eq 0) {
    $textProps = @($doc.PSObject.Properties | Where-Object { $_.Value -is [string] -and $_.Value.Length -gt 100 })
    foreach ($p in $textProps) {
        try {
            $inner = $p.Value | ConvertFrom-Json
            if ($inner.items) { $items = @($inner.items); break }
            if ($inner.result.items) { $items = @($inner.result.items); break }
        } catch {}
    }
}
$wanted = $items | Where-Object { $_.Id -like 'TEST-MCP-TRIAGESTORE*' -or $_.Id -like 'TEST-MCP-SESSIONLOGCTX*' }
$out = [ordered]@{
    dumpPath = $path
    totalItems = $items.Count
    wantedCount = @($wanted).Count
    wanted = @($wanted | ForEach-Object {
        [ordered]@{
            Id = $_.Id
            Title = $_.Title
            Condition = $_.Condition
            Status = $_.Status
            AcceptanceCriteria = @($_.AcceptanceCriteria | ForEach-Object {
                [ordered]@{ id = $_.id; text = $_.text; isSatisfied = $_.isSatisfied }
            })
        }
    })
}
$dest = 'F:\GitHub\McpServer\docs\receipts\_hv-g1-test-extract.json'
($out | ConvertTo-Json -Depth 12) | Set-Content -LiteralPath $dest -Encoding UTF8
Write-Output "WROTE $dest total=$($items.Count) wanted=$($out.wantedCount)"
foreach ($w in $out.wanted) {
    Write-Output ("ID={0} STATUS={1} AC={2}" -f $w.Id, $w.Status, @($w.AcceptanceCriteria).Count)
}
