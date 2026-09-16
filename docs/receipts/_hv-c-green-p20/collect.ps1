#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p20'
$root = 'F:\GitHub\McpServer'
New-Item -ItemType Directory -Force -Path $out | Out-Null
Set-Location -LiteralPath $root

$files = @(
    'tests/McpServer.PluginIntegration.Tests/PluginUpdateServiceHarnessTests.cs',
    'tests/McpServer.PluginIntegration.Tests/PluginUpdateServiceHarnessReceipt.cs',
    'tests/McpServer.PluginIntegration.Tests/PluginPromotionGate.cs',
    'build/Build.PluginPromotion.cs',
    'build/plugin-promotion-policy.json',
    'build/Build.SyncAgentPlugins.cs',
    'docs/plans/PLAN-PLUGINHANDOFF-001.md',
    'docs/receipts/hostile-validator-20260822T132413Z.md',
    'docs/receipts/hostile-validator-20260822T132413Z.json',
    'docs/receipts/_hv-c-red-p20/p20-green-absence.json',
    'docs/receipts/_hv-c-red-p20/independent-p20-trx-summary.json',
    'docs/receipts/_c-red-p20-20260822T131402Z/p20-red.trx',
    'docs/receipts/pluginint-p20-20260822T133039Z/summary.json',
    'docs/receipts/pluginint-p20-20260822T133039Z/harness.log',
    'docs/receipts/pluginint-p20-20260822T133039Z/PluginUpdateServiceHarnessTests.trx',
    'docs/receipts/plugin-promotion-approval-missing.json'
)

$fileInfo = foreach ($rel in $files) {
    $full = Join-Path $root $rel
    $exists = Test-Path -LiteralPath $full
    $item = if ($exists) { Get-Item -LiteralPath $full } else { $null }
    [ordered]@{
        path = $rel
        exists = $exists
        lastWriteTimeUtc = if ($item) { $item.LastWriteTimeUtc.ToString('o') } else { $null }
        length = if ($item) { $item.Length } else { $null }
    }
}
$fileInfo | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $out 'file-inventory.json') -Encoding utf8

$credMd = Join-Path $root 'docs/receipts/hostile-validator-20260822T132413Z.md'
$credJson = Join-Path $root 'docs/receipts/hostile-validator-20260822T132413Z.json'
$credMdText = if (Test-Path -LiteralPath $credMd) { Get-Content -LiteralPath $credMd -Raw } else { '' }
$credJsonObj = if (Test-Path -LiteralPath $credJson) { Get-Content -LiteralPath $credJson -Raw | ConvertFrom-Json } else { $null }
[ordered]@{
    mdExists = Test-Path -LiteralPath $credMd
    jsonExists = Test-Path -LiteralPath $credJson
    mdOverallVerdict = if ($credMdText -match '(?m)^OverallVerdict:\s*(.+)\s*$') { $Matches[1].Trim() } else { $null }
    jsonOverallVerdict = if ($credJsonObj) { [string]$credJsonObj.OverallVerdict } else { $null }
    jsonFailCount = if ($credJsonObj) { [int]$credJsonObj.FailCount } else { $null }
    jsonPassCount = if ($credJsonObj) { [int]$credJsonObj.PassCount } else { $null }
    workClass = if ($credJsonObj) { [string]$credJsonObj.WorkClassLabel } else { $null }
    a2Red = if ($credJsonObj -and $credJsonObj.Claims) { [string]$credJsonObj.Claims.A2_currentlyRedIndependentRerun } else { $null }
    a3GreenAbsent = if ($credJsonObj -and $credJsonObj.Claims) { [string]$credJsonObj.Claims.A3_p20GreenAbsent } else { $null }
    namedFailed = if ($credJsonObj -and $credJsonObj.IndependentNamedTests -and $credJsonObj.IndependentNamedTests.trxCounters) { [int]$credJsonObj.IndependentNamedTests.trxCounters.failed } else { $null }
    namedPassed = if ($credJsonObj -and $credJsonObj.IndependentNamedTests -and $credJsonObj.IndependentNamedTests.trxCounters) { [int]$credJsonObj.IndependentNamedTests.trxCounters.passed } else { $null }
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'cred-p20-agree.json') -Encoding utf8

$promotionCs = Join-Path $root 'build/Build.PluginPromotion.cs'
$promotionSrc = if (Test-Path -LiteralPath $promotionCs) { Get-Content -LiteralPath $promotionCs -Raw } else { '' }
$syncCs = Join-Path $root 'build/Build.SyncAgentPlugins.cs'
$syncSrc = if (Test-Path -LiteralPath $syncCs) { Get-Content -LiteralPath $syncCs -Raw } else { '' }
$policyPath = Join-Path $root 'build/plugin-promotion-policy.json'
$policy = $null
if (Test-Path -LiteralPath $policyPath) {
    $policy = Get-Content -LiteralPath $policyPath -Raw | ConvertFrom-Json
}
[ordered]@{
    promotionExists = Test-Path -LiteralPath $promotionCs
    requiresOperatorApprovalMethod = $promotionSrc.Contains('internal static bool RequiresOperatorApproval')
    allowPluginPromotionMethod = $promotionSrc.Contains('internal static bool AllowPluginPromotion')
    assertPluginPromotionAllowedMethod = $promotionSrc.Contains('internal void AssertPluginPromotionAllowed()')
    stagingBranch = $promotionSrc.Contains('string.Equals(environment, "Staging"')
    productionBranch = $promotionSrc.Contains('string.Equals(environment, "Production"')
    policyExists = Test-Path -LiteralPath $policyPath
    stagingRequire = if ($policy) { [bool]$policy.staging.requireOperatorApproval } else { $null }
    productionRequire = if ($policy) { [bool]$policy.production.requireOperatorApproval } else { $null }
    developmentRequire = if ($policy -and $policy.PSObject.Properties['development']) { [bool]$policy.development.requireOperatorApproval } else { $null }
    syncCallsAssert = $syncSrc.Contains('AssertPluginPromotionAllowed()')
    plantedApprovalExists = Test-Path -LiteralPath (Join-Path $root 'docs/receipts/plugin-promotion-approval-missing.json')
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'promotion-gate.json') -Encoding utf8

$receiptsRoot = Join-Path $root 'docs/receipts'
$p20Dirs = @()
if (Test-Path -LiteralPath $receiptsRoot) {
    $p20Dirs = @(Get-ChildItem -LiteralPath $receiptsRoot -Directory -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -like 'pluginint-p20-*' } |
        Select-Object Name, @{n='lastWriteUtc'; e={ $_.LastWriteTimeUtc.ToString('o') }} )
}
[ordered]@{
    pluginintP20DirCount = $p20Dirs.Count
    pluginintP20Dirs = $p20Dirs
    claimedDirExists = Test-Path -LiteralPath (Join-Path $receiptsRoot 'pluginint-p20-20260822T133039Z')
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'p20-dirs.json') -Encoding utf8

$summaryPath = Join-Path $receiptsRoot 'pluginint-p20-20260822T133039Z/summary.json'
$logPath = Join-Path $receiptsRoot 'pluginint-p20-20260822T133039Z/harness.log'
$summary = $null
if (Test-Path -LiteralPath $summaryPath) { $summary = Get-Content -LiteralPath $summaryPath -Raw | ConvertFrom-Json }
$log = if (Test-Path -LiteralPath $logPath) { Get-Content -LiteralPath $logPath -Raw } else { '' }
$marker = Get-Content -LiteralPath (Join-Path $root 'AGENTS-README-FIRST.yaml') -Raw
$apiKeyMatch = [regex]::Match($marker, '(?m)^apiKey:\s*(\S+)\s*$')
$apiKey = if ($apiKeyMatch.Success) { $apiKeyMatch.Groups[1].Value } else { '' }
$rawKeyInLog = if ($apiKey -and $log) { $log.Contains($apiKey) } else { $false }
$rawKeyInSummary = if ($apiKey -and (Test-Path -LiteralPath $summaryPath)) { (Get-Content -LiteralPath $summaryPath -Raw).Contains($apiKey) } else { $false }
[ordered]@{
    summaryExists = [bool]$summary
    environment = if ($summary) { [string]$summary.environment } else { $null }
    deployTarget = if ($summary) { [string]$summary.deployTarget } else { $null }
    serviceName = if ($summary) { [string]$summary.serviceName } else { $null }
    sanitizedFixtures = if ($summary) { [bool]$summary.sanitizedFixtures } else { $null }
    failed = if ($summary) { [int]$summary.failed } else { $null }
    skipped = if ($summary) { [int]$summary.skipped } else { $null }
    logFile = if ($summary) { [string]$summary.logFile } else { $null }
    logExists = Test-Path -LiteralPath $logPath
    logContainsUpdateService = $log -match 'UpdateService'
    logContainsSanitized = $log -match '(?i)sanitized'
    logContainsFailed0 = $log -match '(?im)^Failed:\s*0\s*$'
    logContainsSkipped0 = $log -match '(?im)^Skipped:\s*0\s*$'
    logContainsApiKeyRedacted = $log.Contains('apiKey=REDACTED')
    rawApiKeyInLog = $rawKeyInLog
    rawApiKeyInSummary = $rawKeyInSummary
    logContainsP20Harness = $log.Contains('agent=P20Harness')
    logContainsLiveOperatorSession = $log -match '(?i)no live operator session'
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'harness-receipt.json') -Encoding utf8

$claimedTempTrx = 'C:\Users\kingd\AppData\Local\Temp\grok-goal-678ba5b2f579\implementer\p20-green.trx'
$receiptTrx = Join-Path $receiptsRoot 'pluginint-p20-20260822T133039Z/PluginUpdateServiceHarnessTests.trx'
$receiptP20GreenName = Join-Path $receiptsRoot 'pluginint-p20-20260822T133039Z/p20-green.trx'
function Get-TrxCounters([string]$path) {
    if (-not (Test-Path -LiteralPath $path)) { return $null }
    [xml]$xml = Get-Content -LiteralPath $path -Raw
    $ns = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
    $ns.AddNamespace('t', 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010')
    $c = $xml.SelectSingleNode('//t:ResultSummary/t:Counters', $ns)
    $results = @($xml.SelectNodes('//t:UnitTestResult', $ns) | ForEach-Object {
        [ordered]@{ name = $_.testName; outcome = $_.outcome }
    })
    return [ordered]@{
        exists = $true
        total = [int]$c.total
        executed = [int]$c.executed
        passed = [int]$c.passed
        failed = [int]$c.failed
        notExecuted = [int]$c.notExecuted
        outcomes = $results
        creation = $xml.TestRun.Times.creation
    }
}
[ordered]@{
    claimedTempExists = Test-Path -LiteralPath $claimedTempTrx
    claimedTemp = Get-TrxCounters $claimedTempTrx
    receiptNamedPluginUpdateExists = Test-Path -LiteralPath $receiptTrx
    receiptNamedPluginUpdate = Get-TrxCounters $receiptTrx
    receiptNamedP20GreenExists = Test-Path -LiteralPath $receiptP20GreenName
    receiptNamedP20Green = Get-TrxCounters $receiptP20GreenName
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'implementer-trx.json') -Encoding utf8

$redTrx = Join-Path $root 'docs/receipts/_hv-c-red-p20/results-p20-red/p20-red.trx'
$redIndependent = Join-Path $root 'docs/receipts/_hv-c-red-p20/results-p20-red-independent/p20-red-independent.trx'
[ordered]@{
    redTrx = Get-TrxCounters $redTrx
    redIndependent = Get-TrxCounters $redIndependent
    redAbsence = if (Test-Path -LiteralPath (Join-Path $root 'docs/receipts/_hv-c-red-p20/p20-green-absence.json')) {
        Get-Content -LiteralPath (Join-Path $root 'docs/receipts/_hv-c-red-p20/p20-green-absence.json') -Raw | ConvertFrom-Json
    } else { $null }
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'red-before-green.json') -Encoding utf8

$dll = Join-Path $root 'tests/McpServer.PluginIntegration.Tests/bin/Debug/net10.0/McpServer.PluginIntegration.Tests.dll'
$srcFiles = @(
    'tests/McpServer.PluginIntegration.Tests/PluginUpdateServiceHarnessTests.cs',
    'tests/McpServer.PluginIntegration.Tests/PluginUpdateServiceHarnessReceipt.cs',
    'tests/McpServer.PluginIntegration.Tests/PluginPromotionGate.cs'
) | ForEach-Object { Get-Item (Join-Path $root $_) }
$newestSrc = ($srcFiles | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1)
$dllItem = if (Test-Path -LiteralPath $dll) { Get-Item -LiteralPath $dll } else { $null }
[ordered]@{
    dllExists = [bool]$dllItem
    dllUtc = if ($dllItem) { $dllItem.LastWriteTimeUtc.ToString('o') } else { $null }
    newestSrc = $newestSrc.FullName
    newestSrcUtc = $newestSrc.LastWriteTimeUtc.ToString('o')
    dllAfterSource = if ($dllItem) { $dllItem.LastWriteTimeUtc -ge $newestSrc.LastWriteTimeUtc } else { $false }
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'dll-vs-source.json') -Encoding utf8

$python = @(Get-Process -Name python, python3, py -ErrorAction SilentlyContinue)
[ordered]@{ pythonProcessCount = $python.Count; names = @($python | ForEach-Object { $_.ProcessName }) } |
    ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'python-procs.json') -Encoding utf8

Push-Location $root
$gitHead = git rev-parse HEAD 2>$null
$gitBranch = git rev-parse --abbrev-ref HEAD 2>$null
$porcelain = git status --porcelain -- docs/Project/TODO.yaml docs/todo.yaml docs/plans/PLAN-PLUGINHANDOFF-001.md build/Build.PluginPromotion.cs build/plugin-promotion-policy.json build/Build.SyncAgentPlugins.cs tests/McpServer.PluginIntegration.Tests/PluginUpdateServiceHarnessTests.cs tests/McpServer.PluginIntegration.Tests/PluginUpdateServiceHarnessReceipt.cs tests/McpServer.PluginIntegration.Tests/PluginPromotionGate.cs 2>$null
$phaseDHits = git status --porcelain -- docs/plans src tests 2>$null
Pop-Location
[ordered]@{
    head = [string]$gitHead
    branch = [string]$gitBranch
    scopedPorcelain = @($porcelain)
    todoYamlPorcelain = @($porcelain | Where-Object { $_ -match 'TODO.yaml|todo.yaml' })
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'git-live.json') -Encoding utf8
Set-Content -LiteralPath (Join-Path $out 'git-status-src.txt') -Value ($phaseDHits | Out-String) -Encoding utf8

$plan = Get-Content -LiteralPath (Join-Path $root 'docs/plans/PLAN-PLUGINHANDOFF-001.md') -Raw
[ordered]@{
    hasCredP20ThenGreen = $plan.Contains('C-red-P20 AGREE, then P20 execute, then C-green-P20 AGREE')
    hasP20GreenText = $plan.Contains('P20 green: harness against development UpdateService with sanitized fixtures')
    hasPhaseDHeading = $plan.Contains('## 8. Phase D: Handoff remediations and closeout')
    namedHarness = $plan.Contains('PluginSessionLogHarness_AgainstUpdateService_SanitizedFixtures_FailedZeroSkippedZero')
    namedPromotion = $plan.Contains('PluginPromotion_StagingOrProduction_RequiresOperatorApprovalFlag')
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'plan-hits.json') -Encoding utf8

$testsPath = Join-Path $root 'tests/McpServer.PluginIntegration.Tests/PluginUpdateServiceHarnessTests.cs'
$src = if (Test-Path -LiteralPath $testsPath) { Get-Content -LiteralPath $testsPath -Raw } else { '' }
[ordered]@{
    harnessMethod = [regex]::IsMatch($src, 'void\s+PluginSessionLogHarness_AgainstUpdateService_SanitizedFixtures_FailedZeroSkippedZero\s*\(')
    promotionMethod = [regex]::IsMatch($src, 'void\s+PluginPromotion_StagingOrProduction_RequiresOperatorApprovalFlag\s*\(')
    factCount = ([regex]::Matches($src, '\[Fact')).Count
    skipFactCount = ([regex]::Matches($src, '\[Fact\s*\(\s*Skip')).Count
    theoryCount = ([regex]::Matches($src, '\[Theory')).Count
    ignoreCount = ([regex]::Matches($src, '\[Ignore')).Count
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'test-method-hits.json') -Encoding utf8

Write-Output 'COLLECT_DONE'
