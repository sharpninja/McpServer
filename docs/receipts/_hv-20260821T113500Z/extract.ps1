#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-20260821T113500Z'

function Get-McpToolPayload {
    param([string]$Path)
    $raw = Get-Content -LiteralPath $Path -Raw
    $rpc = $raw | ConvertFrom-Json
    $text = $null
    if ($rpc.result.content) {
        $text = @($rpc.result.content | Where-Object { $_.type -eq 'text' } | Select-Object -First 1).text
    }
    if (-not $text -and $rpc.result.structuredContent) {
        return $rpc.result.structuredContent
    }
    if ($text) {
        try { return ($text | ConvertFrom-Json) } catch { return $text }
    }
    return $rpc
}

function Summarize-Turn {
    param($Turn, $SessionId)
    $actions = @($Turn.actions)
    $dialog = @($Turn.processingDialog)
    if (-not $dialog -or $dialog.Count -eq 0) { $dialog = @($Turn.dialog) }
    $decisions = @($Turn.designDecisions)
    $tags = @($Turn.tags)
    $context = @($Turn.contextList)
    if (-not $context -or $context.Count -eq 0) { $context = @($Turn.context) }
    [ordered]@{
        sessionId = $SessionId
        requestId = $Turn.requestId
        turnId = $Turn.id
        status = $Turn.status
        queryTitle = $Turn.queryTitle
        queryText = $Turn.queryText
        responseLen = if ($Turn.response) { [string]$Turn.response.Length } else { 0 }
        tags = $tags
        actionCount = $actions.Count
        actionTypes = @($actions | ForEach-Object { $_.type })
        actionSummaries = @($actions | ForEach-Object { [ordered]@{ order = $_.order; type = $_.type; status = $_.status; desc = [string]$_.description } })
        dialogCount = $dialog.Count
        dialogCategories = @($dialog | ForEach-Object { $_.category })
        decisionCount = $decisions.Count
        decisions = $decisions
        context = $context
        filesModified = @($Turn.filesModified)
        planFile = $Turn.planFile
        todoId = $Turn.todoId
    }
}

function Summarize-SessionFile {
    param([string]$Path, [string]$WantedSession, [string]$WantedRequest)
    $payload = Get-McpToolPayload -Path $Path
    $items = @($payload.items)
    $summary = [ordered]@{
        path = $Path
        totalCount = $payload.totalCount
        itemCount = $items.Count
        sessionIds = @($items | ForEach-Object { $_.sessionId })
        titles = @($items | ForEach-Object { $_.title })
        wantedSessionFound = $false
        wantedTurn = $null
        sessions = @()
    }
    foreach ($s in $items) {
        $turns = @($s.turns)
        $sessSum = [ordered]@{
            sessionId = $s.sessionId
            title = $s.title
            status = $s.status
            created = $s.created
            turnCount = $turns.Count
            requestIds = @($turns | ForEach-Object { $_.requestId })
            turnIds = @($turns | ForEach-Object { $_.id })
            turnStatuses = @($turns | ForEach-Object { $_.status })
        }
        $summary.sessions += $sessSum
        if ($s.sessionId -eq $WantedSession) {
            $summary.wantedSessionFound = $true
            $summary.wantedSessionTitle = $s.title
            $summary.wantedSessionStatus = $s.status
            foreach ($t in $turns) {
                if ($t.requestId -eq $WantedRequest) {
                    $summary.wantedTurn = Summarize-Turn -Turn $t -SessionId $s.sessionId
                }
            }
            if (-not $summary.wantedTurn -and $turns.Count -gt 0) {
                $summary.allTurns = @($turns | ForEach-Object { Summarize-Turn -Turn $_ -SessionId $s.sessionId })
            }
        }
    }
    return $summary
}

$s1 = Summarize-SessionFile -Path (Join-Path $outDir 'q-session-113141.json') -WantedSession 'GrokCode-20260821T113141Z-plugin-session' -WantedRequest 'req-20260821T113258Z-001-add-profile-new-session'
$s2 = Summarize-SessionFile -Path (Join-Path $outDir 'q-session-112802.json') -WantedSession 'GrokCode-20260821T112802Z-plugin-session' -WantedRequest 'req-20260821T112813Z-prompt-ac0b'
$s3 = Summarize-SessionFile -Path (Join-Path $outDir 'q-turn-113258.json') -WantedSession 'GrokCode-20260821T113141Z-plugin-session' -WantedRequest 'req-20260821T113258Z-001-add-profile-new-session'
$s4 = Summarize-SessionFile -Path (Join-Path $outDir 'q-turn-112813.json') -WantedSession 'GrokCode-20260821T112802Z-plugin-session' -WantedRequest 'req-20260821T112813Z-prompt-ac0b'

$todos = Get-McpToolPayload -Path (Join-Path $outDir 'todo-list-done-false.json')
$todoItems = @($todos.items)
$todoDoneFlags = @($todoItems | ForEach-Object { [bool]$_.done } | Select-Object -Unique)
$recentDoneCheck = @($todoItems | Where-Object { $_.id -match 'done' })

$mem = Get-McpToolPayload -Path (Join-Path $outDir 'memory-list-effective.json')
$memItems = @($mem.items)
if (-not $memItems -or $memItems.Count -eq 0) { $memItems = @($mem.memories) }
$memIds = @($memItems | ForEach-Object { $_.id })
$lab001 = @($memItems | Where-Object { $_.id -eq 'MEMORY-LAB-001' })
$lab002 = @($memItems | Where-Object { $_.id -eq 'MEMORY-LAB-002' })

$result = [ordered]@{
  session113141 = $s1
  session112802 = $s2
  turn113258 = $s3
  turn112813 = $s4
  todos = [ordered]@{
    totalCount = $todos.totalCount
    itemCount = $todoItems.Count
    uniqueDoneFlags = $todoDoneFlags
    ids = @($todoItems | ForEach-Object { $_.id })
  }
  memories = [ordered]@{
    rawKeys = @($mem.PSObject.Properties.Name)
    itemCount = $memItems.Count
    ids = $memIds
    lab001Present = ($lab001.Count -gt 0)
    lab002Present = ($lab002.Count -gt 0)
    lab001Text = if ($lab001.Count -gt 0) { [string]$lab001[0].text } else { $null }
    lab002Text = if ($lab002.Count -gt 0) { [string]$lab002[0].text } else { $null }
    all = @($memItems | ForEach-Object { [ordered]@{ id = $_.id; text = [string]$_.text } })
  }
}
$result | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $outDir 'extract.json') -Encoding utf8
Write-Output 'EXTRACT_OK'
Write-Output ('TODO_TOTAL=' + $todos.totalCount + ' ITEMS=' + $todoItems.Count)
Write-Output ('MEM_IDS=' + ($memIds -join ','))
Write-Output ('S113141_FOUND=' + $s1.wantedSessionFound + ' TURN=' + [bool]$s1.wantedTurn)
Write-Output ('S112802_FOUND=' + $s2.wantedSessionFound + ' TURN=' + [bool]$s2.wantedTurn)
if ($s1.wantedTurn) { Write-Output ('T113258_ID=' + $s1.wantedTurn.turnId + ' STATUS=' + $s1.wantedTurn.status + ' TITLE=' + $s1.wantedTurn.queryTitle) }
if ($s2.wantedTurn) { Write-Output ('T112813_ID=' + $s2.wantedTurn.turnId + ' STATUS=' + $s2.wantedTurn.status + ' TITLE=' + $s2.wantedTurn.queryTitle) }
if ($s4.wantedTurn) { Write-Output ('T112813B_ID=' + $s4.wantedTurn.turnId + ' STATUS=' + $s4.wantedTurn.status) }
