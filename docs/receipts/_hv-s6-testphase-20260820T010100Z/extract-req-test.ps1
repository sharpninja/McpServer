$ErrorActionPreference = 'Stop'
$path = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a01cae-c549-79a0-92c2-f6c405e47824\mcp\call-3e463566-5441-47d8-a442-e7c3b6874425-98.json'
$raw = Get-Content -LiteralPath $path -Raw
$doc = $raw | ConvertFrom-Json
# MCP wrapper may nest the list
$payload = $doc
if ($doc.PSObject.Properties.Name -contains 'result') { $payload = $doc.result }
if ($payload.PSObject.Properties.Name -contains 'content') {
    $text = $payload.content | ForEach-Object { $_.text } | Where-Object { $_ } | Select-Object -First 1
    if ($text) { $payload = $text | ConvertFrom-Json }
}
$items = @()
if ($payload.PSObject.Properties.Name -contains 'items') { $items = @($payload.items) }
elseif ($payload -is [System.Array]) { $items = @($payload) }
$hits = $items | Where-Object { $_.Id -like '*TRIAGEPLUGIN*' -or $_.Id -like '*TRIAGE-002*' }
$out = Join-Path 'F:\GitHub\McpServer\docs\receipts\_hv-s6-testphase-20260820T010100Z' 'mcp-test-triageplugin.json'
$hits | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $out -Encoding utf8
Write-Output ('itemCount={0} hitCount={1}' -f $items.Count, @($hits).Count)
$hits | ForEach-Object {
    Write-Output ('--- {0} ---' -f $_.Id)
    Write-Output ('Condition={0}' -f $_.Condition)
    if ($_.AcceptanceCriteria) {
        $_.AcceptanceCriteria | ForEach-Object { Write-Output ('AC id={0} text={1}' -f $_.id, $_.text) }
    } else {
        Write-Output 'AC=none'
    }
}
