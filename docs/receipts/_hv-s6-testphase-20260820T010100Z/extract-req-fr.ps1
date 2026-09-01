$ErrorActionPreference = 'Stop'
$path = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a01cae-c549-79a0-92c2-f6c405e47824\mcp\call-7c95c1b3-42d9-43f0-986b-5d653d4a6960-103.json'
$raw = Get-Content -LiteralPath $path -Raw
$doc = $raw | ConvertFrom-Json
$payload = $doc
if ($doc.PSObject.Properties.Name -contains 'result') { $payload = $doc.result }
if ($payload.PSObject.Properties.Name -contains 'content') {
    $text = $payload.content | ForEach-Object { $_.text } | Where-Object { $_ } | Select-Object -First 1
    if ($text) { $payload = $text | ConvertFrom-Json }
}
$items = @()
if ($payload.PSObject.Properties.Name -contains 'items') { $items = @($payload.items) }
$hits = $items | Where-Object { $_.Id -eq 'FR-MCP-TRIAGEPLUGIN-001' -or $_.Id -eq 'FR-MCP-TRIAGE-002' }
$out = Join-Path 'F:\GitHub\McpServer\docs\receipts\_hv-s6-testphase-20260820T010100Z' 'mcp-fr-triageplugin.json'
$hits | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $out -Encoding utf8
Write-Output ('itemCount={0} hitCount={1}' -f $items.Count, @($hits).Count)
$hits | ForEach-Object {
    Write-Output ('--- {0} ---' -f $_.Id)
    Write-Output ('Title={0}' -f $_.Title)
    Write-Output ('Body={0}' -f $_.Body)
    if ($_.AcceptanceCriteria) {
        $_.AcceptanceCriteria | ForEach-Object { Write-Output ('AC id={0} text={1}' -f $_.id, $_.text) }
    } else {
        Write-Output 'AC=none'
    }
}
