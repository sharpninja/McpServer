#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$pluginRoot = 'F:\GitHub\mcpserver-grok-plugin'
$workspace = 'F:\GitHub\McpServer'
$outDir = Join-Path $workspace 'docs\receipts\_hv-s0-h0-reattack'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$env:MCP_PLUGIN_ROOT = $pluginRoot
$env:GROK_PLUGIN_ROOT = $pluginRoot
$env:PLUGIN_AGENT_NAME = 'GrokCode'
$env:MCP_AGENT_NAME = 'GrokCode'
$env:MCP_WORKSPACE_PATH = $workspace
Set-Location -LiteralPath $workspace

. (Join-Path $pluginRoot 'lib\marker-resolver.ps1')
$invoke = Join-Path $pluginRoot 'lib\Invoke-McpPlugin.ps1'

function Save-Text {
    param([string]$Name, [string]$Value)
    $path = Join-Path $outDir $Name
    Set-Content -LiteralPath $path -Value $Value -Encoding utf8
    return $path
}

function Save-Json {
    param([string]$Name, [object]$Value)
    $path = Join-Path $outDir $Name
    ($Value | ConvertTo-Json -Depth 40) | Set-Content -LiteralPath $path -Encoding utf8
    return $path
}

function Invoke-PluginMethod {
    param(
        [Parameter(Mandatory)][string]$Method,
        [hashtable]$Params = @{},
        [int]$TimeoutSeconds = 90
    )
    $args = @{
        Command = 'Invoke'
        Method = $Method
        WorkspacePath = $workspace
        PluginRoot = $pluginRoot
        TimeoutSeconds = $TimeoutSeconds
    }
    if ($Params.Count -gt 0) { $args['ParamsObject'] = $Params }
    return & $invoke @args
}

function Get-PluginPayload {
    param([string]$Raw)
    if ([string]::IsNullOrWhiteSpace($Raw)) { return $null }
    $text = [string]$Raw
    $start = $text.IndexOf('{')
    if ($start -lt 0) { return $null }
    $json = $text.Substring($start)
    try { return ($json | ConvertFrom-Json -Depth 40) } catch { return $null }
}

$utc = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
Save-Text 'utc.txt' $utc | Out-Null
Write-Output "UTC=$utc"

$marker = Join-Path $workspace 'AGENTS-README-FIRST.yaml'
$sigOk = Test-MarkerSignature -MarkerFile $marker
$baseUrl = Get-MarkerField -MarkerFile $marker -FieldName 'baseUrl'
$nonce = [guid]::NewGuid().ToString('N')
$health = $null
$nonceOk = $false
try {
    $health = Invoke-RestMethod -Uri "$baseUrl/health?nonce=$nonce" -TimeoutSec 15
    $nonceOk = ($health.nonce -eq $nonce)
} catch {
    $health = [ordered]@{ error = $_.Exception.Message }
}
Save-Json 'trust.json' ([ordered]@{
    timestampUtc = $utc
    signatureOk = [bool]$sigOk
    nonce = $nonce
    nonceOk = [bool]$nonceOk
    health = $health
    baseUrl = $baseUrl
    cwd = (Get-Location).Path
}) | Out-Null
Write-Output "SIG=$sigOk NONCE_OK=$nonceOk HEALTH=$($health.status)"

try {
    $status = & $invoke -Command Status -WorkspacePath $workspace -PluginRoot $pluginRoot -TimeoutSeconds 90
    Save-Text 'plugin-status.txt' ([string]$status) | Out-Null
    Write-Output "PLUGIN_STATUS_LEN=$([string]$status.Length)"
} catch {
    Save-Text 'plugin-status.err.txt' ($_.Exception.ToString()) | Out-Null
    Write-Output "PLUGIN_STATUS_FAIL $($_.Exception.Message)"
}

$planPath = Join-Path $workspace 'docs\plans\triage-cluster-002.md'
$planExists = Test-Path -LiteralPath $planPath
$planText = if ($planExists) { Get-Content -LiteralPath $planPath -Raw } else { '' }
$expectedIds = @(106,107,108,113,116,117,118,120,121,122,125,130,134,140,142,144,147,150,151,152,153,154,155,156,157,158,159)
$planIdHits = @()
foreach ($n in $expectedIds) {
    $planIdHits += [ordered]@{
        id = $n
        present = ($planText -match [regex]::Escape([string]$n))
    }
}
$protocolHits = [ordered]@{
    worktreeHeading = ($planText -match 'Worktree and subagent protocol')
    worktreesPath = ($planText -match '\.worktrees/')
    gitWorktreeAdd = ($planText -match 'git worktree add')
    mergeAfterAgree = ($planText -match 'Merge only after hostile AGREE')
    planTodo = ($planText -match 'PLAN-TRIAGELEFTOVER-001')
    repoRootWorktrees = ($planText -match 'F:\\GitHub\\McpServer\\.worktrees')
}
Save-Json 'plan-exists.json' ([ordered]@{
    exists = $planExists
    length = $planText.Length
    idHits = $planIdHits
    missingIds = @($planIdHits | Where-Object { -not $_.present } | ForEach-Object { $_.id })
    presentCount = @($planIdHits | Where-Object { $_.present }).Count
    protocolHits = $protocolHits
}) | Out-Null
Write-Output "PLAN_EXISTS=$planExists PLAN_IDS=$($protocolHits.planTodo) PRESENT=$($planIdHits | Where-Object { $_.present } | Measure-Object | Select-Object -ExpandProperty Count)"

$gi = Get-Content -LiteralPath (Join-Path $workspace '.gitignore')
$giHits = @($gi | Select-String -Pattern '\.worktrees' -SimpleMatch)
Save-Text 'gitignore-worktrees.txt' (($giHits | ForEach-Object { '{0}:{1}' -f $_.LineNumber, $_.Line }) -join "`n") | Out-Null

$gitStatus = & git -C $workspace status --short
$gitDiffNames = & git -C $workspace diff --name-only HEAD
$gitDiffStat = & git -C $workspace diff --stat HEAD
$gitWorktrees = & git -C $workspace worktree list
$gitLog = & git -C $workspace log -8 --oneline
Save-Text 'git-status.txt' (($gitStatus | Out-String).TrimEnd()) | Out-Null
Save-Text 'git-diff-names.txt' (($gitDiffNames | Out-String).TrimEnd()) | Out-Null
Save-Text 'git-diff-stat.txt' (($gitDiffStat | Out-String).TrimEnd()) | Out-Null
Save-Text 'git-worktree-list.txt' (($gitWorktrees | Out-String).TrimEnd()) | Out-Null
Save-Text 'git-log.txt' (($gitLog | Out-String).TrimEnd()) | Out-Null

$worktreesDir = Join-Path $workspace '.worktrees'
$wtExists = Test-Path -LiteralPath $worktreesDir
$wtChildren = @()
if ($wtExists) {
    $wtChildren = @(Get-ChildItem -LiteralPath $worktreesDir -Force | Select-Object Name, FullName, Mode, LastWriteTimeUtc)
}
Save-Json 'worktrees-dir.json' ([ordered]@{
    exists = $wtExists
    childCount = $wtChildren.Count
    children = $wtChildren
}) | Out-Null
Write-Output "WORKTREES_DIR=$wtExists CHILDREN=$($wtChildren.Count)"

$areas = @(
    'SESSIONATTR'
    'FAILSAFE'
    'STRICTCOUNT'
    'XAGENT'
    'SESSIONEND'
    'VERIFYWRAP'
    'TRANSCRIPT-SEARCH'
    'TEMPVOL'
)

$acSummary = @()
foreach ($area in $areas) {
    $frId = "FR-MCP-$area-001"
    $trId = "TR-MCP-$area-001"
    $testId = "TEST-MCP-$area-001"
    $frRaw = $null
    $trRaw = $null
    $testRaw = $null
    $mapRaw = $null
    try { $frRaw = Invoke-PluginMethod -Method 'workflow.requirements.getFr' -Params @{ id = $frId } } catch { $frRaw = $_.Exception.ToString() }
    try { $trRaw = Invoke-PluginMethod -Method 'workflow.requirements.getTr' -Params @{ id = $trId } } catch { $trRaw = $_.Exception.ToString() }
    try { $testRaw = Invoke-PluginMethod -Method 'workflow.requirements.getTest' -Params @{ id = $testId } } catch { $testRaw = $_.Exception.ToString() }
    try { $mapRaw = Invoke-PluginMethod -Method 'workflow.requirements.listMappings' -Params @{ frId = $frId } } catch { $mapRaw = $_.Exception.ToString() }
    Save-Text "plugin-getFr-$frId.txt" ([string]$frRaw) | Out-Null
    Save-Text "plugin-getTr-$trId.txt" ([string]$trRaw) | Out-Null
    Save-Text "plugin-getTest-$testId.txt" ([string]$testRaw) | Out-Null
    Save-Text "plugin-map-$frId.txt" ([string]$mapRaw) | Out-Null

    $frObj = Get-PluginPayload ([string]$frRaw)
    $trObj = Get-PluginPayload ([string]$trRaw)
    $testObj = Get-PluginPayload ([string]$testRaw)
    $mapObj = Get-PluginPayload ([string]$mapRaw)

    $frItem = $null
    if ($frObj -and $frObj.payload -and $frObj.payload.result) { $frItem = $frObj.payload.result.item }
    if (-not $frItem -and $frObj -and $frObj.item) { $frItem = $frObj.item }
    if (-not $frItem -and $frObj -and $frObj.Id) { $frItem = $frObj }

    $trItem = $null
    if ($trObj -and $trObj.payload -and $trObj.payload.result) { $trItem = $trObj.payload.result.item }
    if (-not $trItem -and $trObj -and $trObj.item) { $trItem = $trObj.item }
    if (-not $trItem -and $trObj -and $trObj.Id) { $trItem = $trObj }

    $testItem = $null
    if ($testObj -and $testObj.payload -and $testObj.payload.result) { $testItem = $testObj.payload.result.item }
    if (-not $testItem -and $testObj -and $testObj.item) { $testItem = $testObj.item }
    if (-not $testItem -and $testObj -and $testObj.Id) { $testItem = $testObj }

    $frAc = @()
    if ($frItem -and $frItem.acceptanceCriteria) { $frAc = @($frItem.acceptanceCriteria) }
    elseif ($frItem -and $frItem.AcceptanceCriteria) { $frAc = @($frItem.AcceptanceCriteria) }
    $trAc = @()
    if ($trItem -and $trItem.acceptanceCriteria) { $trAc = @($trItem.acceptanceCriteria) }
    elseif ($trItem -and $trItem.AcceptanceCriteria) { $trAc = @($trItem.AcceptanceCriteria) }
    $testAc = @()
    if ($testItem -and $testItem.acceptanceCriteria) { $testAc = @($testItem.acceptanceCriteria) }
    elseif ($testItem -and $testItem.AcceptanceCriteria) { $testAc = @($testItem.AcceptanceCriteria) }

    $frTexts = @($frAc | ForEach-Object { if ($_.text) { [string]$_.text } elseif ($_.Text) { [string]$_.Text } else { '' } })
    $trTexts = @($trAc | ForEach-Object { if ($_.text) { [string]$_.text } elseif ($_.Text) { [string]$_.Text } else { '' } })
    $testTexts = @($testAc | ForEach-Object { if ($_.text) { [string]$_.text } elseif ($_.Text) { [string]$_.Text } else { '' } })

    $mapItems = @()
    if ($mapObj -and $mapObj.payload -and $mapObj.payload.result -and $mapObj.payload.result.items) { $mapItems = @($mapObj.payload.result.items) }
    elseif ($mapObj -and $mapObj.items) { $mapItems = @($mapObj.items) }

    $row = [ordered]@{
        area = $area
        frId = $frId
        trId = $trId
        testId = $testId
        frExists = [bool]$frItem
        trExists = [bool]$trItem
        testExists = [bool]$testItem
        frAcCount = $frAc.Count
        trAcCount = $trAc.Count
        testAcCount = $testAc.Count
        frAcNonEmpty = @($frTexts | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }).Count
        trAcNonEmpty = @($trTexts | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }).Count
        testAcNonEmpty = @($testTexts | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }).Count
        frAcTexts = $frTexts
        trAcTexts = $trTexts
        testAcTexts = $testTexts
        mappingCount = $mapItems.Count
        mappingTr = @($mapItems | ForEach-Object { if ($_.trId) { $_.trId } elseif ($_.TrId) { $_.TrId } elseif ($_.trIds) { $_.trIds } else { $null } })
        mappingTest = @($mapItems | ForEach-Object { if ($_.testId) { $_.testId } elseif ($_.TestId) { $_.TestId } elseif ($_.testIds) { $_.testIds } else { $null } })
    }
    $acSummary += $row
    Write-Output ("AREA={0} FR={1}/{2} TR={3}/{4} TEST={5}/{6} MAP={7}" -f $area, [bool]$frItem, $frAc.Count, [bool]$trItem, $trAc.Count, [bool]$testItem, $testAc.Count, $mapItems.Count)
}

Save-Json 'ac-summary.json' $acSummary | Out-Null

$bugIds = @(106,107,108,113,116,117,118,120,121,122,125,130,134,140,142,144,147,150,151,152,153,154,155,156,157,158,159)
$bugRows = @()
foreach ($n in $bugIds) {
    $id = "BUG-TRIAGE-$n"
    $raw = $null
    try { $raw = Invoke-PluginMethod -Method 'workflow.todo.get' -Params @{ id = $id } } catch { $raw = $_.Exception.ToString() }
    Save-Text "todo-$id.txt" ([string]$raw) | Out-Null
    $obj = Get-PluginPayload ([string]$raw)
    $item = $null
    if ($obj -and $obj.payload -and $obj.payload.result) { $item = $obj.payload.result.item }
    if (-not $item -and $obj -and $obj.item) { $item = $obj.item }
    if (-not $item -and $obj -and $obj.Id) { $item = $obj }
    $done = $null
    $completed = $null
    $summary = $null
    if ($item) {
        if ($null -ne $item.Done) { $done = [bool]$item.Done }
        elseif ($null -ne $item.done) { $done = [bool]$item.done }
        $completed = $item.CompletedDate
        if (-not $completed) { $completed = $item.completedDate }
        $summary = $item.DoneSummary
        if (-not $summary) { $summary = $item.doneSummary }
    }
    $bugRows += [ordered]@{
        id = $id
        exists = [bool]$item
        done = $done
        completedDate = $completed
        doneSummary = $summary
    }
    Write-Output ("TODO {0} exists={1} done={2}" -f $id, [bool]$item, $done)
}
Save-Json 'bug-triage-27.json' $bugRows | Out-Null

$planTodoRaw = $null
try { $planTodoRaw = Invoke-PluginMethod -Method 'workflow.todo.get' -Params @{ id = 'PLAN-TRIAGELEFTOVER-001' } } catch { $planTodoRaw = $_.Exception.ToString() }
Save-Text 'todo-PLAN-TRIAGELEFTOVER-001.txt' ([string]$planTodoRaw) | Out-Null

$csHits = @()
foreach ($area in $areas) {
    $ids = @("FR-MCP-$area-001", "TR-MCP-$area-001", "TEST-MCP-$area-001")
    foreach ($id in $ids) {
        $hits = @(git -C $workspace grep -n -- "$id" -- '*.cs' '*.ps1' ':!docs/*' ':!*.md' 2>$null)
        if ($hits.Count -gt 0) {
            $csHits += [ordered]@{ id = $id; hits = $hits }
        }
    }
}
Save-Json 'product-id-hits.json' $csHits | Out-Null
Write-Output "PRODUCT_ID_HIT_GROUPS=$($csHits.Count)"

$srcStatus = @($gitStatus | Where-Object { $_ -match '^(..)\s+(src/|plugins/|tests/)' })
Save-Text 'git-status-src-plugins-tests.txt' (($srcStatus | Out-String).TrimEnd()) | Out-Null
Write-Output "SRC_PLUGIN_TEST_STATUS_LINES=$($srcStatus.Count)"
Write-Output 'COLLECT_DONE'
