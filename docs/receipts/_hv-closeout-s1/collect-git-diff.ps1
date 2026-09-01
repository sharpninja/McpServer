#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-closeout-s1'
$wt = 'F:\GitHub\McpServer\.worktrees\triage-closeout'
$rel = 'src/McpServer.Storage.SqliteMigrations/Migrations/20260818205751_AddSessionLogTagsAndAgentSessionHeaders.cs'
$headText = git -C $wt show "HEAD:$rel"
$workText = Get-Content -LiteralPath (Join-Path $wt ($rel -replace '/', '\')) -Raw
$diff = git -C $wt diff -- $rel

function Test-BareAlter([string]$text) {
    return [bool]($text -match 'ALTER TABLE "SessionLogs" ADD COLUMN')
}
function Test-Pragma([string]$text) {
    return [bool]($text -match 'PRAGMA table_info')
}
function Test-CommentIfNotExists([string]$text) {
    return [bool]($text -match 'no ADD COLUMN IF NOT EXISTS')
}

$result = [ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    HeadHasAgentSessionId = [bool]($headText -match 'AgentSessionId')
    WorkHasAgentSessionId = [bool]($workText -match 'AgentSessionId')
    HeadHasBareAlter = Test-BareAlter $headText
    WorkHasBareAlter = Test-BareAlter $workText
    HeadHasPragma = Test-Pragma $headText
    WorkHasPragma = Test-Pragma $workText
    WorkCommentDeniesIfNotExists = Test-CommentIfNotExists $workText
    Diff = $diff
}

$jsonPath = Join-Path $outDir 'git-diff.json'
$result | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $jsonPath -Encoding utf8
Write-Output $jsonPath
Write-Output ($result | ConvertTo-Json -Depth 6)
exit 0
