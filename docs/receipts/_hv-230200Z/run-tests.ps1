Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'
$logDir = 'F:\GitHub\McpServer\docs\receipts\_hv-230200Z'
New-Item -ItemType Directory -Path $logDir -Force | Out-Null

function Write-Banner([string]$Name) {
    Write-Output ('===== ' + $Name + ' START ' + (Get-Date).ToUniversalTime().ToString('o') + ' =====')
}

# REPL classifier
Write-Banner 'REPL'
$replLog = Join-Path $logDir 'repl.log'
& dotnet test 'F:\GitHub\McpServer\tests\McpServer.Repl.Core.Tests\McpServer.Repl.Core.Tests.csproj' -c Debug --filter 'FullyQualifiedName~ReplMcpErrorClassifierTests' --nologo *>&1 | Tee-Object -FilePath $replLog
Write-Output ('REPL_EXIT=' + $LASTEXITCODE)

# Support named subset
Write-Banner 'SUPPORT'
$supportLog = Join-Path $logDir 'support.log'
$filter = 'FullyQualifiedName~SubmitAsync_HungSaveChanges_FailsFastWithStorageUnavailable|FullyQualifiedName~SessionLogControllerErrorTests|FullyQualifiedName~McpToolErrorEnvelopeTests|FullyQualifiedName~McpToolBackendUnavailableErrorTests|FullyQualifiedName~HealthPayload_UnreachableStorage_ReportsStorageField_KeepsLivenessHealthy_AndEchoesNonce'
& dotnet test 'F:\GitHub\McpServer\tests\McpServer.Support.Mcp.Tests\McpServer.Support.Mcp.Tests.csproj' -c Debug --filter $filter --nologo *>&1 | Tee-Object -FilePath $supportLog
Write-Output ('SUPPORT_EXIT=' + $LASTEXITCODE)

# Build ReplacePluginCache
Write-Banner 'BUILD'
$buildLog = Join-Path $logDir 'build.log'
& dotnet test 'F:\GitHub\McpServer\tests\Build.Tests\Build.Tests.csproj' -c Debug --filter 'FullyQualifiedName~ReplacePluginCache' --nologo *>&1 | Tee-Object -FilePath $buildLog
Write-Output ('BUILD_EXIT=' + $LASTEXITCODE)

# Pester
Write-Banner 'PESTER'
$pesterLog = Join-Path $logDir 'pester.log'
$pesterFile = 'F:\GitHub\McpServer\plugins\core\test-fixtures\pester\TriagePluginIdentity.Tests.ps1'
$cfg = New-PesterConfiguration
$cfg.Run.Path = $pesterFile
$cfg.Run.PassThru = $true
$cfg.Output.Verbosity = 'Detailed'
$r = Invoke-Pester -Configuration $cfg
$summary = [ordered]@{
    TotalCount = $r.TotalCount
    PassedCount = $r.PassedCount
    FailedCount = $r.FailedCount
    SkippedCount = $r.SkippedCount
    NotRunCount = $r.NotRunCount
    Tests = @($r.Tests | ForEach-Object { [ordered]@{ Name = $_.Name; Result = $_.Result; Executed = $_.Executed } })
}
$summary | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $pesterLog -Encoding utf8
Write-Output ('PESTER_PASSED=' + $r.PassedCount + ' FAILED=' + $r.FailedCount + ' SKIPPED=' + $r.SkippedCount + ' TOTAL=' + $r.TotalCount + ' NOTRUN=' + $r.NotRunCount)
foreach ($t in $r.Tests) {
    Write-Output ('PESTER_IT=' + $t.Result + ' :: ' + $t.Name)
}
