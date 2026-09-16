#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-d2g-20260822T032428Z'
$raw = Get-Content -LiteralPath (Join-Path $outDir 'session-query-agent.json') -Raw | ConvertFrom-Json
$inner = $raw.result.content[0].text | ConvertFrom-Json
$item = $inner.items | Where-Object { $_.sessionId -eq 'GrokSubagentHostile-20260822T032428Z-pluginhandoff-d2g' } | Select-Object -First 1
$turn = $item.turns | Select-Object -First 1
$proof = [ordered]@{
    totalCount = $inner.totalCount
    sessionId = $item.sessionId
    sessionStatus = $item.status
    turnCount = $item.turnCount
    requestId = $turn.requestId
    turnStatus = $turn.status
    queryTitle = $turn.queryTitle
    todoId = $turn.todoId
    planFile = $turn.planFile
    actionCount = @($turn.actions).Count
    dialogCount = @($turn.processingDialog).Count
    decisionCount = @($turn.designDecisions).Count
    filesModified = @($turn.filesModified)
    responsePreview = [string]$turn.response
}
$proof | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $outDir 'session-query-proof.json') -Encoding utf8
$proof | ConvertTo-Json -Depth 6
