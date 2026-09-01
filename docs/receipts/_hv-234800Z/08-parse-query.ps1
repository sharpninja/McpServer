#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-234800Z'
$outer = Get-Content -LiteralPath (Join-Path $outDir 'mcp-query.json') -Raw | ConvertFrom-Json
$text = [string]$outer.result.content[0].text
$obj = $text | ConvertFrom-Json
$session = $obj.items[0]
$turn = $session.turns[0]
$proof = [ordered]@{
    totalCount = $obj.totalCount
    sessionId = [string]$session.sessionId
    sourceType = [string]$session.sourceType
    sessionStatus = [string]$session.status
    turnCount = $session.turnCount
    requestId = [string]$turn.requestId
    turnStatus = [string]$turn.status
    queryTitle = [string]$turn.queryTitle
    responseNull = ($null -eq $turn.response)
    responseLen = if ($turn.response) { [string]$turn.response.Length } else { 0 }
    responseHasAgree = if ($turn.response) { [string]$turn.response.Contains('OverallVerdict AGREE') } else { $false }
    actionCount = @($turn.actions).Count
    dialogCount = if ($turn.processingDialog) { @($turn.processingDialog).Count } else { 0 }
    decisionDialogCount = 0
    designDecisionCount = if ($turn.designDecisions) { @($turn.designDecisions).Count } else { 0 }
    filesModifiedCount = if ($turn.filesModified) { @($turn.filesModified).Count } else { 0 }
    tagCount = if ($turn.tags) { @($turn.tags).Count } else { 0 }
    planFile = [string]$turn.planFile
    todoId = [string]$turn.todoId
}
if ($turn.processingDialog) {
    $proof.decisionDialogCount = @($turn.processingDialog | Where-Object { $_.category -eq 'decision' }).Count
}
$proof | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $outDir 'query-proof.json') -Encoding utf8
$proof.GetEnumerator() | ForEach-Object { Write-Output ($_.Key + '=' + $_.Value) }

# Also parse complete body
$cOuter = Get-Content -LiteralPath (Join-Path $outDir 'mcp-complete.json') -Raw | ConvertFrom-Json
$cText = [string]$cOuter.result.content[0].text
Write-Output ('COMPLETE_TEXT=' + $cText)
