#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p20'
$root = 'F:\GitHub\McpServer'
New-Item -ItemType Directory -Force -Path $out | Out-Null
Set-Location -LiteralPath $root

$env:MCP_WORKSPACE_PATH = $root
$env:MCPSERVER_WORKSPACE_PATH = $root
$env:GROK_WORKSPACE_PATH = $root
$env:PLUGIN_AGENT_NAME = 'GrokSubagentHostile'
$env:MCP_AGENT_NAME = 'GrokSubagentHostile'
$env:GROK_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:MCP_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:MCP_PLUGIN_HOST = 'grok'

$plugin = 'F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1'
$cacheRoot = Join-Path $out 'plugin-cache'
New-Item -ItemType Directory -Force -Path $cacheRoot | Out-Null

function Save-Text {
    param([string]$Path, $Value)
    if ($null -eq $Value) { $Value = '' }
    if ($Value -isnot [string]) { $Value = ($Value | Out-String) }
    Set-Content -LiteralPath $Path -Value $Value -Encoding utf8
}

function Invoke-Plugin {
    param([string]$Method, [hashtable]$Params, [string]$Name)
    $outFile = Join-Path $out ($Name + '.txt')
    $errFile = Join-Path $out ($Name + '.err.txt')
    try {
        $result = & $plugin -Command Invoke -Method $Method -ParamsObject $Params -WorkspacePath $root -CacheRoot $cacheRoot -TimeoutSeconds 120 2> $errFile
        if ($null -eq $result) { $result = '' }
        if ($result -isnot [string]) { $result = ($result | Out-String) }
        Set-Content -LiteralPath $outFile -Value $result -Encoding utf8
        Write-Output ('OK ' + $Name)
    } catch {
        Set-Content -LiteralPath $outFile -Value ('ERROR ' + $_.Exception.ToString()) -Encoding utf8
        Write-Output ('FAIL ' + $Name + ' ' + $_.Exception.Message)
    }
}

$utcNow = [DateTime]::UtcNow
Set-Content -LiteralPath (Join-Path $out 'independent-stamp.txt') -Value $utcNow.ToString('yyyyMMddTHHmmssZ') -Encoding utf8
Write-Output ('INDEPENDENT_UTC=' + $utcNow.ToString('o'))

. 'F:\GitHub\mcpserver-grok-plugin\lib\marker-resolver.ps1'
$sigOk = Test-MarkerSignature -MarkerFile (Join-Path $root 'AGENTS-README-FIRST.yaml')
$bootOk = $false
try { $bootOk = Invoke-FullBootstrap -StartDir $root } catch { $bootOk = $false }
[ordered]@{
    TimestampUtc = $utcNow.ToString('o')
    TestMarkerSignature = [bool]$sigOk
    InvokeFullBootstrap = [bool]$bootOk
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'independent-plugin-marker-sig.json') -Encoding utf8
Write-Output ("INDEPENDENT_PLUGIN_SIG=$sigOk BOOTSTRAP=$bootOk")

$nonce = 'nonce-hv-ind-' + $utcNow.ToString('yyyyMMddHHmmss') + '-' + (Get-Random -Maximum 99999)
try {
    $health = Invoke-RestMethod -Uri ("http://PAYTON-LEGION2:7147/health?nonce=$nonce") -Method Get -TimeoutSec 15
    [ordered]@{ nonceSent = $nonce; nonceEcho = $health.nonce; status = $health.status; match = ($health.nonce -eq $nonce); storage = $health.storage; version = $health.version } |
        ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'independent-health-nonce.json') -Encoding utf8
    Write-Output ("INDEPENDENT_HEALTH_NONCE_MATCH=" + ($health.nonce -eq $nonce))
} catch {
    Save-Text (Join-Path $out 'independent-health-nonce.json') $_.Exception.ToString()
    Write-Output 'INDEPENDENT_HEALTH_NONCE_FAIL'
}

$paths = @(
    'tests/McpServer.PluginIntegration.Tests/PluginUpdateServiceHarnessTests.cs'
    'tests/McpServer.PluginIntegration.Tests/PluginUpdateServiceHarnessReceipt.cs'
    'tests/McpServer.PluginIntegration.Tests/PluginPromotionGate.cs'
    'tests/McpServer.PluginIntegration.Tests/PluginNativeSuiteReceiptTests.cs'
    'build/Build.PluginPromotion.cs'
    'build/plugin-promotion-policy.json'
    'docs/receipts/plugin-promotion-approval-missing.json'
    'docs/receipts/hostile-validator-20260822T104054Z.md'
    'docs/receipts/hostile-validator-20260822T104054Z.json'
)
$inventory = @()
foreach ($rel in $paths) {
    $full = Join-Path $root $rel
    $exists = Test-Path -LiteralPath $full
    $item = Get-Item -LiteralPath $full -ErrorAction SilentlyContinue
    $inventory += [ordered]@{
        path = $rel
        exists = $exists
        lastWriteTimeUtc = if ($item) { $item.LastWriteTimeUtc.ToString('o') } else { $null }
        length = if ($item) { [int64]$item.Length } else { $null }
    }
}
$inventory | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $out 'independent-file-inventory.json') -Encoding utf8

$receiptsRoot = Join-Path $root 'docs\receipts'
$p20Dirs = @()
if (Test-Path -LiteralPath $receiptsRoot) {
    $p20Dirs = @(Get-ChildItem -LiteralPath $receiptsRoot -Directory -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -match '^pluginint-p20-\d{8}T\d{6}Z$' } |
        ForEach-Object { $_.Name })
}
[ordered]@{
    pluginintP20DirCount = $p20Dirs.Count
    pluginintP20Dirs = $p20Dirs
    buildPluginPromotionExists = Test-Path -LiteralPath (Join-Path $root 'build\Build.PluginPromotion.cs')
    pluginPromotionPolicyExists = Test-Path -LiteralPath (Join-Path $root 'build\plugin-promotion-policy.json')
    plantedApprovalExists = Test-Path -LiteralPath (Join-Path $root 'docs\receipts\plugin-promotion-approval-missing.json')
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'independent-p20-green-absence.json') -Encoding utf8

$dll = Join-Path $root 'tests\McpServer.PluginIntegration.Tests\bin\Debug\net10.0\McpServer.PluginIntegration.Tests.dll'
$srcFiles = @(
    (Join-Path $root 'tests\McpServer.PluginIntegration.Tests\PluginUpdateServiceHarnessTests.cs')
    (Join-Path $root 'tests\McpServer.PluginIntegration.Tests\PluginUpdateServiceHarnessReceipt.cs')
    (Join-Path $root 'tests\McpServer.PluginIntegration.Tests\PluginPromotionGate.cs')
)
$srcNewest = ($srcFiles | ForEach-Object { (Get-Item -LiteralPath $_).LastWriteTimeUtc } | Measure-Object -Maximum).Maximum
$dllItem = Get-Item -LiteralPath $dll -ErrorAction SilentlyContinue
[ordered]@{
    dllPath = $dll
    dllExists = [bool]$dllItem
    dllLastWriteTimeUtc = if ($dllItem) { $dllItem.LastWriteTimeUtc.ToString('o') } else { $null }
    sourceNewestUtc = $srcNewest.ToString('o')
    dllNewerOrEqual = if ($dllItem) { $dllItem.LastWriteTimeUtc -ge $srcNewest } else { $false }
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'independent-dll-vs-source.json') -Encoding utf8

$p19Hits = @(Select-String -Path (Join-Path $root 'tests\McpServer.PluginIntegration.Tests\PluginNativeSuiteReceiptTests.cs') -Pattern 'public void PluginNativeSuite_|public void PluginInt_P19_')
$p20Hits = @(Select-String -Path (Join-Path $root 'tests\McpServer.PluginIntegration.Tests\*.cs') -Pattern 'PluginSessionLogHarness_AgainstUpdateService_SanitizedFixtures_FailedZeroSkippedZero|PluginPromotion_StagingOrProduction_RequiresOperatorApprovalFlag')
$skipHits = @(Select-String -Path (Join-Path $root 'tests\McpServer.PluginIntegration.Tests\PluginUpdateServiceHarnessTests.cs') -Pattern 'Skip\s*=')
[ordered]@{
    p19Methods = @($p19Hits | ForEach-Object { $_.Line.Trim() })
    p20Hits = @($p20Hits | ForEach-Object { $_.Path + ':' + $_.LineNumber + ':' + $_.Line.Trim() })
    p20HitCount = $p20Hits.Count
    skipAttributeHits = @($skipHits | ForEach-Object { $_.LineNumber.ToString() + ':' + $_.Line.Trim() })
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'independent-named-test-grep.json') -Encoding utf8

$a1Md = Get-Content -LiteralPath (Join-Path $root 'docs\receipts\hostile-validator-20260822T104054Z.md') -Raw
$a1Json = Get-Content -LiteralPath (Join-Path $root 'docs\receipts\hostile-validator-20260822T104054Z.json') -Raw | ConvertFrom-Json
[ordered]@{
    mdExists = $true
    mdOverallAgree = [bool]($a1Md -match '(?m)^OverallVerdict:\s*AGREE\s*$')
    mdFailListNone = [bool]($a1Md -match '(?m)^## Explicit FAIL list\s*\r?\n\s*\r?\nNone\.')
    jsonOverallVerdict = [string]$a1Json.OverallVerdict
    jsonFailCount = [int]$a1Json.FailCount
    jsonPassCount = [int]$a1Json.PassCount
    jsonUnknownCount = [int]$a1Json.UnknownCount
    jsonP20NamesInPluginIntegrationTests = [int]$a1Json.P20NamesInPluginIntegrationTests
    jsonValidatorIdentity = [string]$a1Json.ValidatorIdentity
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'independent-a1-104054Z.json') -Encoding utf8

$todoPorcelain = @(git -C $root status --porcelain -- 'docs/Project/TODO.yaml' 'docs/todo.yaml')
[ordered]@{ todoYamlPorcelain = $todoPorcelain } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'independent-todo-yaml-porcelain.json') -Encoding utf8

$gitLive = [ordered]@{
    branch = (git -C $root rev-parse --abbrev-ref HEAD).Trim()
    sha = (git -C $root rev-parse HEAD).Trim()
    porcelainP20 = @(git -C $root status --porcelain -- 'tests/McpServer.PluginIntegration.Tests/PluginUpdateServiceHarnessTests.cs' 'tests/McpServer.PluginIntegration.Tests/PluginUpdateServiceHarnessReceipt.cs' 'tests/McpServer.PluginIntegration.Tests/PluginPromotionGate.cs')
    logP20Tests = @(git -C $root log --format='%H %cI %s' -- 'tests/McpServer.PluginIntegration.Tests/PluginUpdateServiceHarnessTests.cs' 'tests/McpServer.PluginIntegration.Tests/PluginUpdateServiceHarnessReceipt.cs' 'tests/McpServer.PluginIntegration.Tests/PluginPromotionGate.cs')
}
$gitLive | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $out 'independent-git-live.json') -Encoding utf8

$py = @(Get-CimInstance Win32_Process -ErrorAction SilentlyContinue | Where-Object { $_.Name -match '^(python|python3|py)\.exe$' } | Select-Object ProcessId, Name, CommandLine)
[ordered]@{ pythonProcessCount = $py.Count; processes = $py } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $out 'independent-python-procs.json') -Encoding utf8

Invoke-Plugin -Method 'client.Todo.GetAsync' -Params @{ id = 'PLAN-PLUGINHANDOFF-001' } -Name 'independent-todo-plan-client'
Invoke-Plugin -Method 'client.Todo.GetAsync' -Params @{ id = 'MCP-PLUGININT-001' } -Name 'independent-todo-pluginint-client'
Invoke-Plugin -Method 'client.Todo.GetAsync' -Params @{ id = 'MCP-WORKSPACEHYGIENE-002' } -Name 'independent-todo-hygiene-client'
Invoke-Plugin -Method 'workflow.requirements.getTest' -Params @{ id = 'TEST-MCP-PLUGININT-001' } -Name 'independent-req-test'

$sessionId = (Get-Content -LiteralPath (Join-Path $out 'session-id.txt') -Raw).Trim()
$requestId = (Get-Content -LiteralPath (Join-Path $out 'request-id.txt') -Raw).Trim()
$agent = 'GrokSubagentHostile'
$now = [DateTime]::UtcNow.ToString('o')
Invoke-Plugin -Method 'client.SessionLog.AppendDialogAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    items = @(
        @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'Independent verify pass started. Continuing session GrokSubagentHostile-20260822T131910Z-c-red-p20. Re-query live todo/requirements, rebuild+rerun PluginUpdateServiceHarnessTests, recheck pluginint-p20 absence and 104054Z AGREE.' }
        @{ timestamp = $now; role = 'model'; category = 'decision'; content = 'Decision: continue the existing in-progress hostile session rather than open a duplicate. Consequence: persistence proof stays on requestId req-20260822T131910Z-001-hostile-c-red-p20. Alternatives rejected: new session that orphans turnId 42975; trusting prior --no-build collector as the independent rerun.' }
    )
} -Name 'independent-sl-dialog-mid'

$resultsDir = Join-Path $out 'results-p20-red-independent'
New-Item -ItemType Directory -Force -Path $resultsDir | Out-Null
$filter = 'FullyQualifiedName~PluginUpdateServiceHarnessTests'
$listLog = Join-Path $out 'independent-dotnet-list-p20.log'
$consoleLog = Join-Path $out 'independent-dotnet-p20-filter.log'

function Parse-ConsoleCounts {
    param([string]$Console)
    $failed = $null; $passed = $null; $skipped = $null; $total = $null
    if ($Console -match '(?m)Failed!\s+Failed:\s+(\d+),\s+Passed:\s+(\d+),\s+Skipped:\s+(\d+),\s+Total:\s+(\d+)') {
        $failed = [int]$Matches[1]; $passed = [int]$Matches[2]; $skipped = [int]$Matches[3]; $total = [int]$Matches[4]
    } elseif ($Console -match '(?m)Passed!\s+Failed:\s+(\d+),\s+Passed:\s+(\d+),\s+Skipped:\s+(\d+),\s+Total:\s+(\d+)') {
        $failed = [int]$Matches[1]; $passed = [int]$Matches[2]; $skipped = [int]$Matches[3]; $total = [int]$Matches[4]
    }
    return [ordered]@{ failed = $failed; passed = $passed; skipped = $skipped; total = $total }
}

function Parse-Trx {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) { return $null }
    [xml]$trxXml = Get-Content -LiteralPath $Path -Raw
    $ns = New-Object System.Xml.XmlNamespaceManager($trxXml.NameTable)
    $ns.AddNamespace('t', 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010')
    $counters = $trxXml.SelectSingleNode('//t:ResultSummary/t:Counters', $ns)
    $results = @($trxXml.SelectNodes('//t:UnitTestResult', $ns) | ForEach-Object {
        $messageNode = $_.SelectSingleNode('t:Output/t:ErrorInfo/t:Message', $ns)
        [ordered]@{
            name = $_.testName
            outcome = $_.outcome
            duration = $_.duration
            message = if ($messageNode) { $messageNode.InnerText } else { $null }
        }
    })
    return [ordered]@{
        total = $counters.total
        executed = $counters.executed
        passed = $counters.passed
        failed = $counters.failed
        notExecuted = $counters.notExecuted
        skippedAttr = $counters.skipped
        outcomes = $results
    }
}

$listArgs = @('test', 'tests/McpServer.PluginIntegration.Tests', '-c', 'Debug', '--list-tests', '--filter', $filter, '-m:1')
$swList = [System.Diagnostics.Stopwatch]::StartNew()
& dotnet @listArgs *>&1 | Tee-Object -FilePath $listLog | Out-Null
$listExit = $LASTEXITCODE
$swList.Stop()
$listText = Get-Content -LiteralPath $listLog -Raw
$listed = @([regex]::Matches($listText, 'PluginUpdateServiceHarnessTests\.\S+') | ForEach-Object { $_.Value } | Select-Object -Unique)
[ordered]@{
    command = ('dotnet ' + ($listArgs -join ' '))
    exitCode = $listExit
    durationMs = $swList.ElapsedMilliseconds
    listedTests = $listed
    listedCount = @($listed).Count
    usedNoBuild = $false
} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $out 'independent-dotnet-list-p20-exit.json') -Encoding utf8

$runArgs = @('test', 'tests/McpServer.PluginIntegration.Tests', '-c', 'Debug', '--filter', $filter, '--logger', 'trx;LogFileName=p20-red-independent.trx', '--results-directory', $resultsDir, '-m:1')
$sw = [System.Diagnostics.Stopwatch]::StartNew()
& dotnet @runArgs *>&1 | Tee-Object -FilePath $consoleLog | Out-Null
$exit = $LASTEXITCODE
$sw.Stop()
$console = Get-Content -LiteralPath $consoleLog -Raw
$counts = Parse-ConsoleCounts -Console $console
$trx = Join-Path $resultsDir 'p20-red-independent.trx'
$trxSummary = Parse-Trx -Path $trx
if ($trxSummary) {
    $trxSummary | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'independent-p20-trx-summary.json') -Encoding utf8
}
$plainConsole = if ($console) { [regex]::Replace($console, '\x1B\[[0-9;]*[A-Za-z]', '') } else { '' }
$harnessFail = $plainConsole -match 'FileNotFoundException\s*:\s*No docs/receipts/pluginint-p20-<utc> receipt directory exists\.'
$promoFail = $plainConsole -match 'FileNotFoundException\s*:\s*Build\.PluginPromotion\.cs is missing\.'
[ordered]@{
    command = ('dotnet ' + ($runArgs -join ' '))
    exitCode = $exit
    durationMs = $sw.ElapsedMilliseconds
    usedNoBuild = $false
    consoleFailed = $counts.failed
    consolePassed = $counts.passed
    consoleSkipped = $counts.skipped
    consoleTotal = $counts.total
    trxPath = $trx
    trxExists = (Test-Path -LiteralPath $trx)
    harnessFileNotFound = $harnessFail
    promotionFileNotFound = $promoFail
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'independent-dotnet-p20-filter-exit.json') -Encoding utf8

Write-Output ("INDEPENDENT_TEST_EXIT=$exit FAILED=$($counts.failed) PASSED=$($counts.passed) SKIPPED=$($counts.skipped) TOTAL=$($counts.total)")
Write-Output 'INDEPENDENT_VERIFY_DONE'
