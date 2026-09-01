$ErrorActionPreference = 'Stop'
$path = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a01716-0672-7030-855a-d8698be65893\mcp\call-7d010fa2-ea16-42e8-9efc-b47b1271aacc-92.json'
$raw = Get-Content -LiteralPath $path -Raw
$parsed = $raw | ConvertFrom-Json
$items = $null
if ($parsed.items) { $items = $parsed.items }
elseif ($parsed.result.items) { $items = $parsed.result.items }
else {
    $text = $parsed.content
    if ($text -is [array]) { $text = ($text | ForEach-Object { $_.text }) -join '' }
    $inner = $text | ConvertFrom-Json
    if ($inner.items) { $items = $inner.items } else { $items = $inner.result.items }
}
$hits = $items | Where-Object { $_.FrId -like 'FR-MCP-TRIAGE*' }
$hits | Select-Object FrId, @{n='TrIds';e={ $_.TrIds -join ',' }}, @{n='TestIds';e={ $_.TestIds -join ',' }} |
    ConvertTo-Json -Depth 5 | Set-Content 'F:\GitHub\McpServer\docs\receipts\_hv-225300Z\mappings.json'
Write-Output ("triageMappings=" + @($hits).Count)
