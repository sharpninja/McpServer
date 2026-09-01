$ErrorActionPreference = 'Continue'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-225300Z'
New-Item -ItemType Directory -Path $outDir -Force | Out-Null
Set-Location 'F:\GitHub\McpServer'

function Invoke-DotnetFilter {
    param([string]$Project, [string]$Filter, [string]$LogName)
    $log = Join-Path $outDir $LogName
    $args = @(
        'test', $Project, '-c', 'Debug', '--nologo', '--filter', $Filter,
        '--logger', 'console;verbosity=minimal'
    )
    & dotnet @args *>&1 | Tee-Object -FilePath $log | Out-Null
    $code = $LASTEXITCODE
    "EXIT=$code" | Add-Content -LiteralPath $log
    return $code
}

$code1 = Invoke-DotnetFilter -Project 'tests\McpServer.Support.Mcp.Tests' -Filter 'FullyQualifiedName~SessionLogTriageStoreTests.SubmitAsync_HungSaveChanges|FullyQualifiedName~SessionLogControllerErrorTests|FullyQualifiedName~McpToolErrorEnvelopeTests|FullyQualifiedName~McpToolBackendUnavailableErrorTests|FullyQualifiedName~HealthEndpointStoragePayloadTests.HealthPayload_UnreachableStorage' -LogName 'support.log'
$code2 = Invoke-DotnetFilter -Project 'tests\McpServer.Repl.Core.Tests' -Filter 'FullyQualifiedName~ReplMcpErrorClassifierTests' -LogName 'repl.log'
$code3 = Invoke-DotnetFilter -Project 'tests\Build.Tests' -Filter 'FullyQualifiedName~ReplacePluginCache' -LogName 'build.log'

$pesterLog = Join-Path $outDir 'pester.log'
$cfg = New-PesterConfiguration
$cfg.Run.Path = 'F:\GitHub\McpServer\plugins\core\test-fixtures\pester\TriagePluginIdentity.Tests.ps1'
$cfg.Run.Exit = $true
$cfg.Output.Verbosity = 'Detailed'
$r = Invoke-Pester -Configuration $cfg
$r | Out-String | Set-Content -LiteralPath $pesterLog
"Passed=$($r.PassedCount) Failed=$($r.FailedCount) Skipped=$($r.SkippedCount) Total=$($r.TotalCount) EXIT=$LASTEXITCODE" | Add-Content -LiteralPath $pesterLog

@"
supportExit=$code1
replExit=$code2
buildExit=$code3
pesterPassed=$($r.PassedCount)
pesterFailed=$($r.FailedCount)
pesterSkipped=$($r.SkippedCount)
pesterTotal=$($r.TotalCount)
"@ | Set-Content -LiteralPath (Join-Path $outDir 'test-summary.txt')

Write-Output (Get-Content -LiteralPath (Join-Path $outDir 'test-summary.txt') -Raw)
