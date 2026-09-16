#Requires -Version 7.0
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-e-red-20260822T151657Z'
$trxPath = Join-Path $outDir 'hostile-review-hv-e-red.trx'
$reqPath = Join-Path $outDir 'requirements-all.json'

[xml]$trx = Get-Content -LiteralPath $trxPath -Raw
$ns = New-Object System.Xml.XmlNamespaceManager($trx.NameTable)
$ns.AddNamespace('t', 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010')
$counters = $trx.SelectSingleNode('//t:ResultSummary/t:Counters', $ns)
$results = @($trx.SelectNodes('//t:UnitTestResult', $ns))
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

Write-Output ('TRX_TOTAL=' + $counters.total)
Write-Output ('TRX_EXECUTED=' + $counters.executed)
Write-Output ('TRX_PASSED=' + $counters.passed)
Write-Output ('TRX_FAILED=' + $counters.failed)
Write-Output ('TRX_NOTEXECUTED=' + $counters.notExecuted)
Write-Output ('TRX_INCONCLUSIVE=' + $counters.inconclusive)
Write-Output ('TRX_TIMEOUT=' + $counters.timeout)
Write-Output ('RESULT_COUNT=' + $results.Count)
foreach ($r in $results) {
    Write-Output ('OUTCOME=' + $r.outcome + '|NAME=' + $r.testName)
}
foreach ($name in $planNames) {
    $hit = @($results | Where-Object { $_.testName -like ('*' + $name) })
    $oc = if ($hit.Count -eq 0) { 'MISSING' } else { ($hit | ForEach-Object { $_.outcome }) -join ',' }
    Write-Output ('PLANNAME=' + $name + '|COUNT=' + $hit.Count + '|OUTCOME=' + $oc)
}

$reqRaw = Get-Content -LiteralPath $reqPath -Raw
$reqObj = $reqRaw | ConvertFrom-Json
# unwrap MCP tool content if needed
if ($reqObj.PSObject.Properties.Name -contains 'result' -and $reqObj.result) { $reqObj = $reqObj.result }
$idsWanted = @(
    'FR-MCP-HOSTILEREVIEW-001','FR-MCP-HOSTILEREVIEW-002','FR-MCP-HOSTILEREVIEW-003','FR-MCP-HOSTILEREVIEW-004','FR-MCP-HOSTILEREVIEW-005','FR-MCP-HOSTILEREVIEW-006'
    'TR-MCP-HOSTILEREVIEW-001','TR-MCP-HOSTILEREVIEW-002','TR-MCP-HOSTILEREVIEW-003','TR-MCP-HOSTILEREVIEW-004','TR-MCP-HOSTILEREVIEW-005','TR-MCP-HOSTILEREVIEW-006'
    'TEST-MCP-HOSTILEREVIEW-001','TEST-MCP-HOSTILEREVIEW-002','TEST-MCP-HOSTILEREVIEW-003','TEST-MCP-HOSTILEREVIEW-004','TEST-MCP-HOSTILEREVIEW-005','TEST-MCP-HOSTILEREVIEW-006'
)

function Get-Id($n) {
    foreach ($k in @('id','Id','requirementId','RequirementId')) {
        if ($n.PSObject.Properties[$k]) { return [string]$n.PSObject.Properties[$k].Value }
    }
    return ''
}
function Get-AC($n) {
    $ac = $null
    foreach ($k in @('acceptanceCriteria','AcceptanceCriteria')) {
        if ($n.PSObject.Properties[$k]) { $ac = $n.PSObject.Properties[$k].Value }
    }
    if ($null -eq $ac) { return @() }
    $out = @()
    foreach ($item in @($ac)) {
        if ($item -is [string]) { $out += $item; continue }
        $t = $null
        foreach ($k in @('text','Text','criterion','Criterion','description','Description')) {
            if ($item.PSObject.Properties[$k]) { $t = [string]$item.PSObject.Properties[$k].Value; break }
        }
        if ($t) { $out += $t } else { $out += ($item | ConvertTo-Json -Compress -Depth 6) }
    }
    return $out
}

$found = [ordered]@{}
$stack = New-Object System.Collections.Stack
$stack.Push($reqObj)
$visited = 0
while ($stack.Count -gt 0 -and $visited -lt 200000) {
    $visited++
    $n = $stack.Pop()
    if ($null -eq $n) { continue }
    if ($n -is [string] -or $n -is [ValueType]) { continue }
    if ($n -is [System.Collections.IEnumerable] -and $n -isnot [string] -and $n -isnot [System.Management.Automation.PSCustomObject]) {
        foreach ($c in $n) { $stack.Push($c) }
        continue
    }
    if ($n.PSObject) {
        $id = Get-Id $n
        if ($id -and ($idsWanted -contains $id) -and -not $found.Contains($id)) {
            $found[$id] = [ordered]@{
                id = $id
                status = [string]$(if ($n.PSObject.Properties['status']) { $n.status } elseif ($n.PSObject.Properties['Status']) { $n.Status } else { '' })
                title = [string]$(if ($n.PSObject.Properties['title']) { $n.title } elseif ($n.PSObject.Properties['Title']) { $n.Title } else { '' })
                ac = @(Get-AC $n)
            }
        }
        foreach ($p in $n.PSObject.Properties) {
            $v = $p.Value
            if ($null -eq $v) { continue }
            if ($v -is [string] -or $v -is [ValueType]) { continue }
            $stack.Push($v)
        }
    }
}

Write-Output ('WALK_VISITED=' + $visited)
Write-Output ('FOUND_COUNT=' + $found.Count)
foreach ($id in $idsWanted) {
    if ($found.Contains($id)) {
        $row = $found[$id]
        Write-Output ('REQ=' + $id + '|status=' + $row.status + '|acCount=' + @($row.ac).Count + '|title=' + $row.title)
        $i = 0
        foreach ($a in @($row.ac)) {
            $i++
            $snip = if ($a.Length -gt 180) { $a.Substring(0,180) } else { $a }
            Write-Output ('AC=' + $id + '|' + $i + '|' + $snip)
        }
    } else {
        Write-Output ('REQ_MISSING=' + $id)
    }
}

$hash = Get-FileHash -LiteralPath $trxPath -Algorithm SHA256
$runLog = Join-Path $outDir 'hostile-review-run.log'
$runHash = Get-FileHash -LiteralPath $runLog -Algorithm SHA256
Write-Output ('TRX_SHA256=' + $hash.Hash)
Write-Output ('RUNLOG_SHA256=' + $runHash.Hash)
Write-Output ('TRX_BYTES=' + (Get-Item -LiteralPath $trxPath).Length)
Write-Output ('RUNLOG_BYTES=' + (Get-Item -LiteralPath $runLog).Length)

$found | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $outDir 'hostilereview-requirements.json') -Encoding utf8
