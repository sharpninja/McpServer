#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p20'
$root = 'F:\GitHub\McpServer'
New-Item -ItemType Directory -Force -Path $out | Out-Null
Set-Location -LiteralPath $root

$files = @(
    'tests/McpServer.PluginIntegration.Tests/PluginUpdateServiceHarnessTests.cs',
    'tests/McpServer.PluginIntegration.Tests/PluginUpdateServiceHarnessReceipt.cs',
    'tests/McpServer.PluginIntegration.Tests/PluginPromotionGate.cs',
    'build/Build.PluginPromotion.cs',
    'build/plugin-promotion-policy.json',
    'docs/plans/PLAN-PLUGINHANDOFF-001.md',
    'docs/receipts/hostile-validator-20260822T104054Z.md',
    'docs/receipts/_c-red-p20-20260822T131402Z/p20-red.trx'
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

$testsPath = Join-Path $root 'tests/McpServer.PluginIntegration.Tests/PluginUpdateServiceHarnessTests.cs'
$src = if (Test-Path -LiteralPath $testsPath) { Get-Content -LiteralPath $testsPath -Raw } else { '' }
$methodHits = [ordered]@{
    harnessMethod = [regex]::IsMatch($src, 'void\s+PluginSessionLogHarness_AgainstUpdateService_SanitizedFixtures_FailedZeroSkippedZero\s*\(')
    promotionMethod = [regex]::IsMatch($src, 'void\s+PluginPromotion_StagingOrProduction_RequiresOperatorApprovalFlag\s*\(')
    factCount = ([regex]::Matches($src, '\[Fact')).Count
    skipFactCount = ([regex]::Matches($src, '\[Fact\s*\(\s*Skip')).Count
    theoryCount = ([regex]::Matches($src, '\[Theory')).Count
    ignoreCount = ([regex]::Matches($src, '\[Ignore')).Count
    compileOnlyAssertTrue = [regex]::IsMatch($src, 'Assert\.True\(\s*true\s*\)')
    developmentAssert = $src.Contains('Assert.Equal("Development"')
    updateServiceAssert = $src.Contains('Assert.Equal("UpdateService"')
    sanitizedAssert = $src.Contains('receipt.SanitizedFixtures')
    stagingApproval = $src.Contains('RequiresOperatorApproval("Staging")')
    productionApproval = $src.Contains('RequiresOperatorApproval("Production")')
    developmentNoApproval = $src.Contains('RequiresOperatorApproval("Development")')
    allowStagingNull = $src.Contains('gate.Allow("Staging", operatorApprovalArtifactPath: null)')
    allowProductionNull = $src.Contains('gate.Allow("Production", operatorApprovalArtifactPath: null)')
}
$methodHits | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'test-method-hits.json') -Encoding utf8

$receiptCs = Join-Path $root 'tests/McpServer.PluginIntegration.Tests/PluginUpdateServiceHarnessReceipt.cs'
$receiptSrc = if (Test-Path -LiteralPath $receiptCs) { Get-Content -LiteralPath $receiptCs -Raw } else { '' }
$gateCs = Join-Path $root 'tests/McpServer.PluginIntegration.Tests/PluginPromotionGate.cs'
$gateSrc = if (Test-Path -LiteralPath $gateCs) { Get-Content -LiteralPath $gateCs -Raw } else { '' }
[ordered]@{
    receiptMissingDirMessage = $receiptSrc.Contains('No docs/receipts/pluginint-p20-<utc> receipt directory exists.')
    promotionMissingSourceMessage = $gateSrc.Contains('Build.PluginPromotion.cs is missing.')
    promotionMissingPolicyMessage = $gateSrc.Contains('plugin-promotion-policy.json is missing.')
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'exception-message-hits.json') -Encoding utf8

$receiptsRoot = Join-Path $root 'docs/receipts'
$p20Dirs = @()
if (Test-Path -LiteralPath $receiptsRoot) {
    $p20Dirs = @(Get-ChildItem -LiteralPath $receiptsRoot -Directory -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -like 'pluginint-p20-*' } |
        Select-Object -ExpandProperty Name)
}
[ordered]@{
    pluginintP20DirCount = $p20Dirs.Count
    pluginintP20Dirs = $p20Dirs
    buildPluginPromotionExists = Test-Path -LiteralPath (Join-Path $root 'build/Build.PluginPromotion.cs')
    pluginPromotionPolicyExists = Test-Path -LiteralPath (Join-Path $root 'build/plugin-promotion-policy.json')
    plantedApprovalExists = Test-Path -LiteralPath (Join-Path $root 'docs/receipts/plugin-promotion-approval-missing.json')
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'p20-green-absence.json') -Encoding utf8

$dll = Join-Path $root 'tests/McpServer.PluginIntegration.Tests/bin/Debug/net10.0/McpServer.PluginIntegration.Tests.dll'
$dll9 = Join-Path $root 'tests/McpServer.PluginIntegration.Tests/bin/Debug/net9.0/McpServer.PluginIntegration.Tests.dll'
$csFiles = @(
    (Join-Path $root 'tests/McpServer.PluginIntegration.Tests/PluginUpdateServiceHarnessTests.cs'),
    (Join-Path $root 'tests/McpServer.PluginIntegration.Tests/PluginUpdateServiceHarnessReceipt.cs'),
    (Join-Path $root 'tests/McpServer.PluginIntegration.Tests/PluginPromotionGate.cs')
)
$csNewest = ($csFiles | Where-Object { Test-Path -LiteralPath $_ } | ForEach-Object { (Get-Item -LiteralPath $_).LastWriteTimeUtc } | Measure-Object -Maximum).Maximum
$dllItem = $null
if (Test-Path -LiteralPath $dll) { $dllItem = Get-Item -LiteralPath $dll }
elseif (Test-Path -LiteralPath $dll9) { $dllItem = Get-Item -LiteralPath $dll9 }
[ordered]@{
    dllPath = if ($dllItem) { $dllItem.FullName } else { $null }
    dllLastWriteTimeUtc = if ($dllItem) { $dllItem.LastWriteTimeUtc.ToString('o') } else { $null }
    sourceNewestUtc = if ($csNewest) { $csNewest.ToString('o') } else { $null }
    dllNewerOrEqual = if ($dllItem -and $csNewest) { $dllItem.LastWriteTimeUtc -ge $csNewest } else { $false }
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'dll-vs-source.json') -Encoding utf8

$named = @(
    'PluginSessionLogHarness_AgainstUpdateService_SanitizedFixtures_FailedZeroSkippedZero',
    'PluginPromotion_StagingOrProduction_RequiresOperatorApprovalFlag'
)
$grepHits = foreach ($name in $named) {
    $hits = @(Select-String -Path (Join-Path $root 'tests/McpServer.PluginIntegration.Tests/*.cs') -Pattern $name -SimpleMatch -ErrorAction SilentlyContinue)
    [ordered]@{
        name = $name
        count = $hits.Count
        files = @($hits | ForEach-Object { $_.Path.Substring($root.Length + 1) + ':' + $_.LineNumber })
    }
}
$grepHits | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $out 'named-test-grep.json') -Encoding utf8

$python = @(Get-CimInstance Win32_Process -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -match '^(python|python3|py)\.exe$' } |
    Select-Object ProcessId, Name, CommandLine)
[ordered]@{
    pythonProcessCount = @($python).Count
    processes = @($python | ForEach-Object {
        [ordered]@{ pid = $_.ProcessId; name = $_.Name; commandLine = $_.CommandLine }
    })
} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $out 'python-procs.json') -Encoding utf8

$todoYaml = git status --porcelain -- 'docs/Project/TODO.yaml' 'docs/todo.yaml' 2>$null
[ordered]@{
    porcelain = @($todoYaml)
    empty = [string]::IsNullOrWhiteSpace(($todoYaml | Out-String).Trim())
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'todo-yaml-porcelain.json') -Encoding utf8

$gitLive = [ordered]@{
    branch = (git rev-parse --abbrev-ref HEAD).Trim()
    sha = (git rev-parse HEAD).Trim()
}
$gitLive | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'git-live.json') -Encoding utf8

$updateServiceHits = @()
$updateServiceHits += @(Get-ChildItem -LiteralPath $receiptsRoot -Directory -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -match 'update.?service|pluginint-p20' } |
    Select-Object -ExpandProperty Name)
$recentLogs = @()
if (Test-Path -LiteralPath $receiptsRoot) {
    $recentLogs = @(Get-ChildItem -LiteralPath $receiptsRoot -File -Recurse -ErrorAction SilentlyContinue |
        Where-Object { $_.LastWriteTimeUtc -gt [DateTime]::UtcNow.AddHours(-6) -and $_.Name -match 'UpdateService|update-service' } |
        Select-Object -First 20 FullName, LastWriteTimeUtc)
}
[ordered]@{
    matchingReceiptDirs = $updateServiceHits
    recentUpdateServiceFiles = @($recentLogs | ForEach-Object {
        [ordered]@{ path = $_.FullName; lastWriteTimeUtc = $_.LastWriteTimeUtc.ToString('o') }
    })
} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $out 'updateservice-recent.json') -Encoding utf8

$cGreenP19 = Join-Path $root 'docs/receipts/hostile-validator-20260822T104054Z.md'
$p19Text = if (Test-Path -LiteralPath $cGreenP19) { Get-Content -LiteralPath $cGreenP19 -Raw } else { '' }
[ordered]@{
    exists = Test-Path -LiteralPath $cGreenP19
    overallAgree = $p19Text.Contains('OverallVerdict: AGREE')
    mentionsCGreenP19 = $p19Text.Contains('C-green-P19')
    mentionsCRedP20Complete = $p19Text.Contains('C-red-P20') -and $p19Text.Contains('OverallVerdict: AGREE') -and $p19Text.Contains('C-red-P20')
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'c-green-p19-receipt.json') -Encoding utf8

$implTrx = Join-Path $root 'docs/receipts/_c-red-p20-20260822T131402Z/p20-red.trx'
$implTrxAlt = 'C:\Users\kingd\AppData\Local\Temp\grok-goal-678ba5b2f579\implementer\p20-red.trx'
[ordered]@{
    workspaceTrxExists = Test-Path -LiteralPath $implTrx
    tempTrxExists = Test-Path -LiteralPath $implTrxAlt
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'implementer-trx-exists.json') -Encoding utf8

Write-Output 'COLLECT_DONE'
