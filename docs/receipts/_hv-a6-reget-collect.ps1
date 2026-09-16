#Requires -Version 7.0
[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$stamp = [datetime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
$outDir = Join-Path 'F:\GitHub\McpServer\docs\receipts' ("_hv-a6-{0}" -f $stamp)
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$stamp | Set-Content -LiteralPath (Join-Path $outDir 'stamp.txt') -Encoding utf8

$base = 'http://PAYTON-LEGION2:7147'
$key = 'tMxsITg8_OfmC8NrwlwMMCEYkr6lD0_A0zHjGU8egjg'
$ws = 'F:\GitHub\McpServer'
$headers = @{
    'X-Api-Key' = $key
    'X-Workspace-Path' = $ws
}

. (Join-Path $ws 'plugins\core\lib-ps\marker-resolver.ps1')
$marker = Join-Path $ws 'AGENTS-README-FIRST.yaml'
$sigOk = [bool](Test-MarkerSignature -MarkerFile $marker)
$nonce = [guid]::NewGuid().ToString('N')
$health = Invoke-WebRequest -Uri "$base/health?nonce=$nonce" -Headers $headers -UseBasicParsing
$health.Content | Set-Content -LiteralPath (Join-Path $outDir 'health.json') -Encoding utf8
$healthObj = $health.Content | ConvertFrom-Json
$hNames = @($healthObj.PSObject.Properties.Name)
$nonceEcho = $null
if ($hNames -contains 'nonce') { $nonceEcho = [string]$healthObj.nonce }
$healthStatusText = $null
if ($hNames -contains 'status') { $healthStatusText = [string]$healthObj.status }
[pscustomobject]@{
    stamp = $stamp
    healthStatus = [int]$health.StatusCode
    nonceSent = $nonce
    nonceEcho = $nonceEcho
    nonceMatch = ($nonceEcho -eq $nonce)
    signatureOk = $sigOk
    healthStatusText = $healthStatusText
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $outDir 'health-meta.json') -Encoding utf8

function Get-Req {
    param([string]$Kind, [string]$Id)
    $uri = "$base/mcpserver/requirements/$Kind/$([uri]::EscapeDataString($Id))"
    try {
        $resp = Invoke-WebRequest -Uri $uri -Headers $headers -UseBasicParsing
        return [pscustomobject]@{
            Kind = $Kind
            Id = $Id
            StatusCode = [int]$resp.StatusCode
            Body = $resp.Content
        }
    }
    catch {
        $code = 0
        $body = $_.Exception.Message
        if ($_.Exception.Response) {
            $code = [int]$_.Exception.Response.StatusCode
        }
        return [pscustomobject]@{
            Kind = $Kind
            Id = $Id
            StatusCode = $code
            Body = $body
        }
    }
}

$ids = [ordered]@{
    fr = @(
        'FR-MCP-HOSTILEREVIEW-001','FR-MCP-HOSTILEREVIEW-002','FR-MCP-HOSTILEREVIEW-003','FR-MCP-HOSTILEREVIEW-004','FR-MCP-HOSTILEREVIEW-005','FR-MCP-HOSTILEREVIEW-006',
        'FR-MCP-HYGIENE-001','FR-MCP-HYGIENE-002','FR-MCP-HYGIENE-003','FR-MCP-HYGIENE-004','FR-MCP-HYGIENE-005',
        'FR-MCP-WIKIEXPORT-003','FR-MCP-WIKIEXPORT-004','FR-MCP-WIKIEXPORT-005',
        'FR-MCP-WIKIEXPORT-001','FR-MCP-WIKIEXPORT-002',
        'FR-MCP-PLUGINCORE-004','FR-MCP-REPL-009','FR-MCP-PLUGININT-001','FR-MCP-172'
    )
    tr = @(
        'TR-MCP-HOSTILEREVIEW-001','TR-MCP-HOSTILEREVIEW-002','TR-MCP-HOSTILEREVIEW-003','TR-MCP-HOSTILEREVIEW-004','TR-MCP-HOSTILEREVIEW-005','TR-MCP-HOSTILEREVIEW-006',
        'TR-MCP-HYGIENE-001','TR-MCP-HYGIENE-002','TR-MCP-HYGIENE-003','TR-MCP-HYGIENE-004','TR-MCP-HYGIENE-005',
        'TR-MCP-WIKIEXPORT-003','TR-MCP-WIKIEXPORT-004','TR-MCP-WIKIEXPORT-005',
        'TR-MCP-PLUGINCORE-004','TR-MCP-PLUGINCORE-005',
        'TR-MCP-REPL-010','TR-MCP-REPL-011','TR-MCP-REPL-012','TR-MCP-REPL-013',
        'TR-MCP-PERSIST-003','TR-MCP-PLUGININT-001'
    )
    test = @(
        'TEST-MCP-HOSTILEREVIEW-001','TEST-MCP-HOSTILEREVIEW-002','TEST-MCP-HOSTILEREVIEW-003','TEST-MCP-HOSTILEREVIEW-004','TEST-MCP-HOSTILEREVIEW-005','TEST-MCP-HOSTILEREVIEW-006',
        'TEST-MCP-HYGIENE-001','TEST-MCP-HYGIENE-002','TEST-MCP-HYGIENE-003','TEST-MCP-HYGIENE-004','TEST-MCP-HYGIENE-005',
        'TEST-MCP-WIKIEXPORT-003','TEST-MCP-WIKIEXPORT-004','TEST-MCP-WIKIEXPORT-005',
        'TEST-MCP-PLUGINCORE-004','TEST-MCP-PLUGINCORE-005',
        'TEST-MCP-REPL-025','TEST-MCP-REPL-026','TEST-MCP-REPL-027','TEST-MCP-REPL-028','TEST-MCP-REPL-041',
        'TEST-MCP-195','TEST-MCP-PLUGININT-001'
    )
    mapping = @(
        'FR-MCP-HOSTILEREVIEW-001','FR-MCP-HOSTILEREVIEW-002','FR-MCP-HOSTILEREVIEW-003','FR-MCP-HOSTILEREVIEW-004','FR-MCP-HOSTILEREVIEW-005','FR-MCP-HOSTILEREVIEW-006',
        'FR-MCP-HYGIENE-001','FR-MCP-HYGIENE-002','FR-MCP-HYGIENE-003','FR-MCP-HYGIENE-004','FR-MCP-HYGIENE-005',
        'FR-MCP-WIKIEXPORT-003','FR-MCP-WIKIEXPORT-004','FR-MCP-WIKIEXPORT-005',
        'FR-MCP-PLUGINCORE-004','FR-MCP-REPL-009','FR-MCP-PLUGININT-001','FR-MCP-172'
    )
}

$summaries = New-Object System.Collections.Generic.List[object]
$frById = @{}
foreach ($kind in @('fr','tr','test','mapping')) {
    foreach ($id in $ids[$kind]) {
        $got = Get-Req -Kind $kind -Id $id
        $safe = ($id -replace '[^A-Za-z0-9\-]', '_')
        $path = Join-Path $outDir "$kind-$safe.json"
        $got.Body | Set-Content -LiteralPath $path -Encoding utf8

        $acCount = $null
        $acTexts = @()
        $title = $null
        $status = $null
        $trIds = $null
        $testIds = $null
        $parsed = $null
        $bodyText = $null
        $condition = $null
        $hasPlaceholder = $false
        $titleEqualsId = $false
        if ($got.StatusCode -eq 200) {
            try { $parsed = $got.Body | ConvertFrom-Json } catch { $parsed = $null }
        }
        if ($parsed) {
            $names = @($parsed.PSObject.Properties.Name)
            if ($names -contains 'title') { $title = $parsed.title }
            if ($names -contains 'condition') { $condition = [string]$parsed.condition }
            if (-not $title) { $title = $condition }
            if ($names -contains 'status') { $status = $parsed.status }
            if ($names -contains 'body' -and $null -ne $parsed.body) { $bodyText = [string]$parsed.body }
            elseif ($condition) { $bodyText = $condition }
            $blob = ('{0} {1} {2}' -f $title, $bodyText, $condition)
            $hasPlaceholder = ($blob -match 'Placeholder requirement backfilled')
            $titleEqualsId = ($title -eq $id)
            if ($names -contains 'acceptanceCriteria') {
                $ac = @($parsed.acceptanceCriteria)
                $acCount = $ac.Count
                $acTexts = @($ac | ForEach-Object { if ($null -eq $_) { '' } else { '{0}|sat={1}|{2}' -f $_.id, $_.isSatisfied, $_.text } })
            }
            if ($kind -eq 'mapping') {
                if ($names -contains 'trIds') { $trIds = @($parsed.trIds) }
                if ($names -contains 'testIds') { $testIds = @($parsed.testIds) }
            }
            if ($kind -eq 'fr') { $frById[$id] = $parsed }
        }

        $summaries.Add([pscustomobject]@{
            Kind = $kind
            Id = $id
            StatusCode = $got.StatusCode
            Title = $title
            Status = $status
            AcCount = $acCount
            HasPlaceholder = $hasPlaceholder
            TitleEqualsId = $titleEqualsId
            BodyPreview = $(if ($bodyText) { ($bodyText.Substring(0, [Math]::Min(240, $bodyText.Length)) -replace "`t|`r|`n", ' ') } else { $null })
            ConditionPreview = $(if ($condition) { ($condition.Substring(0, [Math]::Min(240, $condition.Length)) -replace "`t|`r|`n", ' ') } else { $null })
            AcTexts = ($acTexts -join ' || ')
            TrIds = $(if ($trIds) { $trIds -join ',' } else { $null })
            TestIds = $(if ($testIds) { $testIds -join ',' } else { $null })
        })
    }
}

$trVsFr = New-Object System.Collections.Generic.List[object]
$newFamilies = @(
    'MCP-HOSTILEREVIEW-001','MCP-HOSTILEREVIEW-002','MCP-HOSTILEREVIEW-003','MCP-HOSTILEREVIEW-004','MCP-HOSTILEREVIEW-005','MCP-HOSTILEREVIEW-006',
    'MCP-HYGIENE-001','MCP-HYGIENE-002','MCP-HYGIENE-003','MCP-HYGIENE-004','MCP-HYGIENE-005',
    'MCP-WIKIEXPORT-003','MCP-WIKIEXPORT-004','MCP-WIKIEXPORT-005'
)
foreach ($fam in $newFamilies) {
    $frId = "FR-$fam"
    $trId = "TR-$fam"
    $testId = "TEST-$fam"
    $frPath = Join-Path $outDir ("fr-{0}.json" -f ($frId -replace '[^A-Za-z0-9\-]', '_'))
    $trPath = Join-Path $outDir ("tr-{0}.json" -f ($trId -replace '[^A-Za-z0-9\-]', '_'))
    $testPath = Join-Path $outDir ("test-{0}.json" -f ($testId -replace '[^A-Za-z0-9\-]', '_'))
    $fr = $null; $tr = $null; $test = $null
    if (Test-Path $frPath) { try { $fr = Get-Content -LiteralPath $frPath -Raw | ConvertFrom-Json } catch { } }
    if (Test-Path $trPath) { try { $tr = Get-Content -LiteralPath $trPath -Raw | ConvertFrom-Json } catch { } }
    if (Test-Path $testPath) { try { $test = Get-Content -LiteralPath $testPath -Raw | ConvertFrom-Json } catch { } }
    $frAc = @()
    $trAc = @()
    $frNames = @()
    $trNames = @()
    $testNames = @()
    if ($fr) { $frNames = @($fr.PSObject.Properties.Name) }
    if ($tr) { $trNames = @($tr.PSObject.Properties.Name) }
    if ($test) { $testNames = @($test.PSObject.Properties.Name) }
    if ($frNames -contains 'acceptanceCriteria') { $frAc = @($fr.acceptanceCriteria | ForEach-Object { [string]$_.text }) }
    if ($trNames -contains 'acceptanceCriteria') { $trAc = @($tr.acceptanceCriteria | ForEach-Object { [string]$_.text }) }
    $acSame = ($frAc.Count -gt 0 -and ($frAc -join '|') -eq ($trAc -join '|'))
    $named = @()
    if ($test) {
        $cond = ''
        if ($testNames -contains 'condition' -and $null -ne $test.condition) { $cond = [string]$test.condition }
        $acJoin = ''
        if ($testNames -contains 'acceptanceCriteria') {
            $acJoin = (@($test.acceptanceCriteria | ForEach-Object { [string]$_.text }) -join ' ')
        }
        $blob = ('{0} {1}' -f $cond, $acJoin)
        $named = [regex]::Matches($blob, '[A-Za-z][A-Za-z0-9_]+_(?:RoundTrip|ValidRequest|OversizedPayload|ForeignWorkspace|MissingArtifact|StaleOrUnauthorized|AmbiguousLink|RecordsModel|OmitsFabricated|NormalizedFindings|RequestQuality|ByModelAndEffort|ByRequester|NoMatch|DoesNotMutate|SurfaceParity|ResultContract|CleanWorkspace|UnknownRuleCode|MissingAcceptanceCriteria|TrWithNoFr|FrMissingTrOrTest|TestWithNoFr|BrokenOrDuplicate|DoneTrue|DoneFalse|DoneWithout|RemainingContradicts|MissingDependency|MissingReferenced|InProgressTurn|TriageNonTerminal|StaleThreshold|WikiExport_|Dump_|AddWorkspace_|Import_|TodoYaml_)[A-Za-z0-9_]*') | ForEach-Object { $_.Value }
    }
    $frBody = ''
    $trBody = ''
    if ($frNames -contains 'body' -and $null -ne $fr.body) { $frBody = [string]$fr.body }
    if ($trNames -contains 'body' -and $null -ne $tr.body) { $trBody = [string]$tr.body }
    $testCond = ''
    if ($testNames -contains 'condition' -and $null -ne $test.condition) { $testCond = [string]$test.condition }
    $trVsFr.Add([pscustomobject]@{
        Family = $fam
        FrTitle = $(if ($frNames -contains 'title') { $fr.title } else { $null })
        TrTitle = $(if ($trNames -contains 'title') { $tr.title } else { $null })
        FrBodyHasPlaceholder = ($frBody -match 'Placeholder requirement backfilled')
        TrBodyHasPlaceholder = ($trBody -match 'Placeholder requirement backfilled')
        FrBodyPreview = $(if ($frBody) { $frBody.Substring(0, [Math]::Min(180, $frBody.Length)) -replace "`r|`n", ' ' } else { $null })
        TrBodyPreview = $(if ($trBody) { $trBody.Substring(0, [Math]::Min(180, $trBody.Length)) -replace "`r|`n", ' ' } else { $null })
        AcTextsIdentical = $acSame
        TrMentionsSchemaOrStorage = ($trBody -match '(?i)schema|storage|migration|sqlite|entity')
        TestConditionPreview = $(if ($testCond) { $testCond.Substring(0, [Math]::Min(180, $testCond.Length)) -replace "`r|`n", ' ' } else { $null })
        NamedTestsFound = ($named -join ',')
        NamedTestCount = @($named).Count
    })
}

$summaries | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $outDir 'summary.json') -Encoding utf8
$trVsFr | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $outDir 'tr-vs-fr.json') -Encoding utf8

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add("Kind`tId`tHttp`tAcCount`tPlaceholder`tTitleEqId`tStatus`tTitle`tTrIds`tTestIds`tBodyPreview")
foreach ($s in $summaries) {
    $lines.Add(("{0}`t{1}`t{2}`t{3}`t{4}`t{5}`t{6}`t{7}`t{8}`t{9}`t{10}" -f $s.Kind, $s.Id, $s.StatusCode, $s.AcCount, $s.HasPlaceholder, $s.TitleEqualsId, $s.Status, ($s.Title -replace "`t|`r|`n", ' '), $s.TrIds, $s.TestIds, $s.BodyPreview))
}
$lines -join "`n" | Set-Content -LiteralPath (Join-Path $outDir 'summary.tsv') -Encoding utf8

Push-Location $ws
try {
    git status --short | Set-Content (Join-Path $outDir 'git-status.txt') -Encoding utf8
    git diff --stat | Set-Content (Join-Path $outDir 'git-diff-stat.txt') -Encoding utf8
    git log -8 --oneline | Set-Content (Join-Path $outDir 'git-log.txt') -Encoding utf8
}
finally { Pop-Location }

$repl = Join-Path $ws 'plugins\core\lib-ps\repl-invoke.ps1'
Select-String -Path $repl -Pattern 'return 2' -Context 5,5 | ForEach-Object { $_.ToString() } | Set-Content (Join-Path $outDir 'repl-return-2.txt') -Encoding utf8
Select-String -Path $repl -Pattern 'REPL_FAILSAFE_DRAIN_TIMEOUT|ReplFailsafeDraining|Get-ReplMethodTimeoutSeconds' | ForEach-Object { $_.LineNumber.ToString() + ':' + $_.Line } | Set-Content (Join-Path $outDir 'repl-drain-symbols.txt') -Encoding utf8

Get-ChildItem -Path (Join-Path $ws 'src'), (Join-Path $ws 'tests'), (Join-Path $ws 'plugins') -Recurse -File -ErrorAction SilentlyContinue |
    Where-Object {
        $_.Name -match 'HostileReview|WorkspaceValidation|WikiDump' -and
        $_.FullName -notmatch '\\docs\\receipts\\' -and
        $_.FullName -notmatch '\\docs\\plans\\'
    } |
    Select-Object -ExpandProperty FullName |
    Set-Content (Join-Path $outDir 'product-name-hits.txt') -Encoding utf8

Write-Output "STAMP=$stamp"
Write-Output "OUT=$outDir"
Write-Output "COUNT=$($summaries.Count)"
Write-Output "SIG=$sigOk"
Write-Output "NONCE=$($nonceEcho -eq $nonce)"
