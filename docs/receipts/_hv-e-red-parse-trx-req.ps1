#Requires -Version 7.0
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-e-red-20260822T151657Z'
$trxPath = Join-Path $outDir 'hostile-review-hv-e-red.trx'
$reqPath = Join-Path $outDir 'requirements-all.json'
$idsPath = Join-Path $outDir 'ids.json'

[xml]$trx = Get-Content -LiteralPath $trxPath -Raw
$ns = New-Object System.Xml.XmlNamespaceManager($trx.NameTable)
$ns.AddNamespace('t', 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010')
$counters = $trx.SelectSingleNode('//t:ResultSummary/t:Counters', $ns)
$results = @($trx.SelectNodes('//t:UnitTestResult', $ns))
$outcomeGroups = $results | Group-Object outcome | ForEach-Object {
    [ordered]@{ outcome = $_.Name; count = $_.Count; names = @($_.Group | ForEach-Object { $_.testName }) }
}

$planNames = @(
    'HostileReviewEntity_RoundTrip_SqlitePgSqlServer'
    'HostileReviewSubmit_ValidRequest_CreatesQueueItem'
    'HostileReviewSubmit_OversizedPayload_Rejected'
    'HostileReviewSubmit_ForeignWorkspace_403'
    'HostileReviewSubmit_MissingArtifact_ReturnsDiagnosticNotSilentOmit'
    'HostileReviewSubmit_StaleOrUnauthorizedLink_Diagnostic'
    'HostileReviewSubmit_AmbiguousLink_Diagnostic'
    'HostileReviewExecution_RecordsModelEffortAgentTemplateRunId'
    'HostileReviewExecution_OmitsFabricatedTokenCounts'
    'HostileReviewGet_NormalizedFindings_TaxonomyComplete'
    'HostileReview_RequestQuality_ScoresDisclosureScopeObjectiveConfidenceContext'
    'HostileReviewQuery_ByModelAndEffort_ReturnsOnlyMatchingRuns'
    'HostileReviewQuery_ByRequesterAndTargetType_AndFilters'
    'HostileReviewQuery_NoMatch_EmptyList'
    'HostileReview_Default_DoesNotMutateProductFiles'
    'HostileReview_SurfaceParity_RestReplDirectorPlugin'
)

$trxNames = @($results | ForEach-Object { $_.testName })
$missingInTrx = @($planNames | Where-Object { -not ($trxNames | Where-Object { $_ -like ('*' + $planNamesItem) }) })
$missingInTrx = @()
foreach ($name in $planNames) {
    if (-not ($trxNames | Where-Object { $_ -like ('*' + $name) })) {
        $missingInTrx += $name
    }
}
$extraInTrx = @($trxNames | Where-Object {
    $tn = $_
    -not ($planNames | Where-Object { $tn -like ('*' + $_) })
})

$reqRaw = Get-Content -LiteralPath $reqPath -Raw
$reqObj = $reqRaw | ConvertFrom-Json
$items = @()
if ($reqObj.items) { $items = @($reqObj.items) }
elseif ($reqObj.result.items) { $items = @($reqObj.result.items) }
elseif ($reqObj.functional -or $reqObj.technical -or $reqObj.tests) {
    $items = @()
}

function Get-HostProp {
    param($obj, [string[]]$names)
    foreach ($n in $names) {
        if ($null -eq $obj) { continue }
        $p = $obj.PSObject.Properties[$n]
        if ($p) { return $p.Value }
    }
    return $null
}

$allNodes = New-Object System.Collections.Generic.List[object]
function Walk($node) {
    if ($null -eq $node) { return }
    $allNodes.Add($node) | Out-Null
    if ($node -is [System.Collections.IEnumerable] -and $node -isnot [string]) {
        foreach ($child in $node) { Walk $child }
        return
    }
    if ($node.PSObject) {
        foreach ($p in $node.PSObject.Properties) {
            if ($p.Name -in @('Count','Length','SyncRoot','IsReadOnly','IsFixedSize','IsSynchronized','Keys','Values')) { continue }
            $val = $p.Value
            if ($null -eq $val) { continue }
            if ($val -is [string] -or $val -is [ValueType]) { continue }
            Walk $val
        }
    }
}
Walk $reqObj

$hostNodes = @()
foreach ($n in $allNodes) {
    try {
        $id = [string](Get-HostProp $n @('id','Id','requirementId','RequirementId'))
        if ($id -match 'HOSTILEREVIEW') {
            $hostNodes += [ordered]@{
                id = $id
                type = [string](Get-HostProp $n @('type','Type','kind','Kind','requirementType'))
                status = [string](Get-HostProp $n @('status','Status'))
                title = [string](Get-HostProp $n @('title','Title','name','Name'))
                acCount = @(Get-HostProp $n @('acceptanceCriteria','AcceptanceCriteria')).Count
                ac = @(Get-HostProp $n @('acceptanceCriteria','AcceptanceCriteria') | ForEach-Object { if ($_ -is [string]) { $_ } else { Get-HostProp $_ @('text','Text','criterion','Criterion','description','Description') } })
            }
        }
    } catch {
    }
}

$ids = Get-Content -LiteralPath $idsPath -Raw | ConvertFrom-Json

$summary = [ordered]@{
    trxPath = $trxPath
    counters = [ordered]@{
        total = $counters.total
        executed = $counters.executed
        passed = $counters.passed
        failed = $counters.failed
        error = $counters.error
        timeout = $counters.timeout
        aborted = $counters.aborted
        inconclusive = $counters.inconclusive
        notRunnable = $counters.notRunnable
        notExecuted = $counters.notExecuted
        disconnected = $counters.disconnected
        warning = $counters.warning
        completed = $counters.completed
    }
    outcomeGroups = $outcomeGroups
    trxNames = $trxNames
    missingInTrx = $missingInTrx
    extraInTrx = $extraInTrx
    todoPlanDone = $ids.todos.'PLAN-PLUGINHANDOFF-001'.done
    todoHrDone = $ids.todos.'MCP-HOSTILEREVIEW-001'.done
    hostRequirementCount = $hostNodes.Count
    hostRequirements = $hostNodes
}

$summary | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $outDir 'trx-req-parse.json') -Encoding utf8
Write-Output ('TRX_TOTAL=' + $counters.total)
Write-Output ('TRX_EXECUTED=' + $counters.executed)
Write-Output ('TRX_PASSED=' + $counters.passed)
Write-Output ('TRX_FAILED=' + $counters.failed)
Write-Output ('TRX_NOTEXECUTED=' + $counters.notExecuted)
Write-Output ('TRX_INCONCLUSIVE=' + $counters.inconclusive)
Write-Output ('MISSING_IN_TRX=' + ($missingInTrx -join ','))
Write-Output ('EXTRA_IN_TRX=' + ($extraInTrx -join ','))
Write-Output ('HOST_REQ_COUNT=' + $hostNodes.Count)
foreach ($h in $hostNodes) {
    Write-Output ('REQ=' + $h.id + '|type=' + $h.type + '|status=' + $h.status + '|ac=' + $h.acCount + '|title=' + $h.title)
}
Write-Output ('TODO_PLAN_DONE=' + $ids.todos.'PLAN-PLUGINHANDOFF-001'.done)
Write-Output ('TODO_HR_DONE=' + $ids.todos.'MCP-HOSTILEREVIEW-001'.done)
