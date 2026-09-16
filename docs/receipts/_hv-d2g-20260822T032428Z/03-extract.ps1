#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-d2g-20260822T032428Z'

function Get-McpText {
    param([string]$Path)
    $raw = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
    $text = $raw.result.content[0].text
    return ($text | ConvertFrom-Json)
}

function Save-Json {
    param([string]$Name, $Object)
    ($Object | ConvertTo-Json -Depth 14) | Set-Content -LiteralPath (Join-Path $outDir $Name) -Encoding utf8
}

$plan = Get-McpText (Join-Path $outDir 'todo-PLAN-PLUGINHANDOFF-001.json')
$handoff = Get-McpText (Join-Path $outDir 'todo-MCP-HANDOFF-001.json')
$handoffPlan = Get-McpText (Join-Path $outDir 'todo-MCP-HANDOFFPLAN-001.json')
$handoffReview = Get-McpText (Join-Path $outDir 'todo-MCP-HANDOFFREVIEW-001.json')

$taskExtract = foreach ($t in @($plan.ImplementationTasks)) {
    [ordered]@{ Task = [string]$t.Task; Done = [bool]$t.Done }
}
Save-Json -Name 'todo-plan-tasks.json' -Object $taskExtract

$todoSummary = [ordered]@{
    planId = $plan.Id
    planDone = [bool]$plan.Done
    planCompletedDate = $plan.CompletedDate
    planRemaining = [string]$plan.Remaining
    planDoneSummary = $plan.DoneSummary
    handoffDone = [bool]$handoff.Done
    handoffCompletedDate = $handoff.CompletedDate
    handoffRemaining = [string]$handoff.Remaining
    handoffPlanDone = [bool]$handoffPlan.Done
    handoffPlanRemaining = [string]$handoffPlan.Remaining
    handoffReviewDone = [bool]$handoffReview.Done
    handoffReviewRemaining = [string]$handoffReview.Remaining
    d15Task = @($plan.ImplementationTasks | Where-Object { [string]$_.Task -match 'D1\.5' } | ForEach-Object { [ordered]@{ Task = $_.Task; Done = $_.Done } })
    d2Task = @($plan.ImplementationTasks | Where-Object { [string]$_.Task -match '^D2 ' -or [string]$_.Task -match 'D2 implement' } | ForEach-Object { [ordered]@{ Task = $_.Task; Done = $_.Done } })
    d3Task = @($plan.ImplementationTasks | Where-Object { [string]$_.Task -match '^D3 ' } | ForEach-Object { [ordered]@{ Task = $_.Task; Done = $_.Done } })
    d4Task = @($plan.ImplementationTasks | Where-Object { [string]$_.Task -match '^D4 ' } | ForEach-Object { [ordered]@{ Task = $_.Task; Done = $_.Done } })
    d5Task = @($plan.ImplementationTasks | Where-Object { [string]$_.Task -match '^D5 ' } | ForEach-Object { [ordered]@{ Task = $_.Task; Done = $_.Done } })
    d1Task = @($plan.ImplementationTasks | Where-Object { [string]$_.Task -match '^D1 remaining' } | ForEach-Object { [ordered]@{ Task = $_.Task; Done = $_.Done } })
}
Save-Json -Name 'todo-summary.json' -Object $todoSummary

$ids = @(
    'TEST-HANDOFF-001','TEST-HANDOFF-002','TEST-HANDOFF-003','TEST-HANDOFF-004','TEST-HANDOFF-005','TEST-HANDOFF-006','TEST-HANDOFF-007',
    'FR-HANDOFF-002','FR-HANDOFF-006','FR-HANDOFF-007',
    'TR-HANDOFF-AGENT-001','TR-HANDOFF-AUDIT-001','TR-HANDOFF-SURFACE-001'
)

$tests = Get-McpText (Join-Path $outDir 'req-test.json')
$frs = Get-McpText (Join-Path $outDir 'req-fr.json')
$trs = Get-McpText (Join-Path $outDir 'req-tr.json')
$maps = Get-McpText (Join-Path $outDir 'req-mapping.json')

function Find-Req {
    param($Bucket, [string]$Id)
    $items = @()
    if ($Bucket.items) { $items = @($Bucket.items) }
    elseif ($Bucket.testing) { $items = @($Bucket.testing) }
    elseif ($Bucket.functional) { $items = @($Bucket.functional) }
    elseif ($Bucket.technical) { $items = @($Bucket.technical) }
    elseif ($Bucket.mapping) { $items = @($Bucket.mapping) }
    foreach ($item in $items) {
        $itemId = [string]($item.id)
        if (-not $itemId) { $itemId = [string]($item.Id) }
        if ($itemId -eq $Id) { return $item }
    }
    return $null
}

$reqExtract = foreach ($id in $ids) {
    $item = $null
    if ($id.StartsWith('TEST-')) { $item = Find-Req $tests $id }
    elseif ($id.StartsWith('FR-')) { $item = Find-Req $frs $id }
    elseif ($id.StartsWith('TR-')) { $item = Find-Req $trs $id }
    if ($null -eq $item) {
        [ordered]@{ id = $id; found = $false }
        continue
    }
    $acs = @()
    $acSrc = $item.acceptanceCriteria
    if (-not $acSrc) { $acSrc = $item.AcceptanceCriteria }
    foreach ($ac in @($acSrc)) {
        $acs += [ordered]@{
            id = [string]($ac.id)
            isSatisfied = [bool]$ac.isSatisfied
            text = [string]$ac.text
        }
    }
    [ordered]@{
        id = $id
        found = $true
        status = [string]$item.status
        acCount = $acs.Count
        ac = $acs
    }
}
Save-Json -Name 'req-handoff-extract.json' -Object $reqExtract

$mapExtract = @()
$mapItems = @()
if ($maps.items) { $mapItems = @($maps.items) } elseif ($maps.mapping) { $mapItems = @($maps.mapping) }
foreach ($m in $mapItems) {
    $fr = [string]$m.frId
    if (-not $fr) { $fr = [string]$m.FrId }
    if ($fr -match 'HANDOFF') {
        $mapExtract += [ordered]@{
            frId = $fr
            trIds = $m.trIds
            testIds = $m.testIds
        }
    }
}
Save-Json -Name 'req-handoff-maps.json' -Object $mapExtract

$pluginRoots = @(
    'F:\GitHub\mcpserver-grok-plugin',
    'F:\GitHub\mcpserver-claude-code-plugin',
    'F:\GitHub\mcpserver-codex-plugin',
    'F:\GitHub\mcpserver-copilot-plugin',
    'F:\GitHub\mcpserver-cline-plugin'
)
$coreInvoke = 'F:\GitHub\McpServer\plugins\core\skills\handoff\invoke.ps1'
$coreHash = (Get-FileHash -LiteralPath $coreInvoke -Algorithm SHA256).Hash
$pluginCopies = foreach ($root in $pluginRoots) {
    $p = Join-Path $root 'skills\handoff\invoke.ps1'
    $exists = Test-Path -LiteralPath $p -PathType Leaf
    $hash = if ($exists) { (Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash } else { $null }
    $verPath = Join-Path $root '.version'
    $pj = Join-Path $root 'plugin.json'
    $ver = $null
    if (Test-Path -LiteralPath $verPath) { $ver = (Get-Content -LiteralPath $verPath -Raw).Trim() }
    elseif (Test-Path -LiteralPath $pj) {
        $pjObj = Get-Content -LiteralPath $pj -Raw | ConvertFrom-Json
        $ver = [string]$pjObj.version
    }
    [ordered]@{
        root = $root
        invokeExists = $exists
        sha256 = $hash
        matchesCore = ($exists -and $hash -eq $coreHash)
        version = $ver
    }
}
Save-Json -Name 'official-plugin-invoke.json' -Object $pluginCopies

Write-Output 'EXTRACT_DONE'
Write-Output ('PLAN_DONE=' + $todoSummary.planDone)
Write-Output ('HANDOFF_DONE=' + $todoSummary.handoffDone)
Write-Output ('HANDOFFPLAN_DONE=' + $todoSummary.handoffPlanDone)
Write-Output ('HANDOFFREVIEW_DONE=' + $todoSummary.handoffReviewDone)
