#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p1-reverify'
Set-Location -LiteralPath 'F:\GitHub\McpServer'

git log -n 5 --format='%H %cI %s' -- tests/Build.Tests/PluginSessionLogIntegrationTargetTests.cs |
    Set-Content -LiteralPath (Join-Path $out 'git-log-testfile.txt') -Encoding utf8
git status --porcelain -- tests/Build.Tests/PluginSessionLogIntegrationTargetTests.cs tests/McpServer.PluginIntegration.Tests build McpServer.sln docs/todo.yaml docs/Project/TODO.yaml |
    Set-Content -LiteralPath (Join-Path $out 'git-status-scope2.txt') -Encoding utf8
git log -S 'BuildTests_PluginSessionLogIntegrationTarget_ExistsAndTreatsSkipAsFail' --all --format='%H %cI %s' -- '*.cs' |
    Set-Content -LiteralPath (Join-Path $out 'git-pickaxe-t1.txt') -Encoding utf8
git log -S 'Solution_ContainsMcpServerPluginIntegrationTestsProject' --all --format='%H %cI %s' -- '*.cs' |
    Set-Content -LiteralPath (Join-Path $out 'git-pickaxe-t2.txt') -Encoding utf8
git log -S 'Catalog_HasExactlyEightEnabledScenarios' --all --format='%H %cI %s' -- '*.cs' |
    Set-Content -LiteralPath (Join-Path $out 'git-pickaxe-t3.txt') -Encoding utf8
git log -n 15 --diff-filter=R --summary -- tests/Build.Tests |
    Set-Content -LiteralPath (Join-Path $out 'git-renames-buildtests.txt') -Encoding utf8
git log -n 3 --format='%H %cI %s' -- tests/Build.Tests/BuildTargetTests.cs |
    Set-Content -LiteralPath (Join-Path $out 'git-log-buildtargettests.txt') -Encoding utf8
git check-ignore -v tests/Build.Tests/PluginSessionLogIntegrationTargetTests.cs |
    Set-Content -LiteralPath (Join-Path $out 'git-check-ignore-testfile.txt') -Encoding utf8
Write-Output 'GIT_DONE'
Get-Item (Join-Path $out 'git-*.txt') | ForEach-Object { $_.Name + ' len=' + $_.Length }
