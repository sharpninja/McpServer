#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p15\run2'

function Extract-TodoDone {
    param([string]$Path, [string]$Dest)
    if (-not (Test-Path -LiteralPath $Path)) {
        [ordered]@{ exists = $false; path = $Path } | ConvertTo-Json | Set-Content -LiteralPath $Dest -Encoding utf8
        return
    }
    $text = Get-Content -LiteralPath $Path -Raw
    $topDone = $null
    if ($text -match '(?m)^\s+done:\s+(true|false)\s*$') { $topDone = ($Matches[1] -eq 'true') }
    $tasks = [regex]::Matches($text, '(?ms)^\s+- task:\s*(.+?)\r?\n\s+done:\s+(true|false)')
    $taskObjs = foreach ($m in $tasks) {
        [pscustomobject]@{ task = $m.Groups[1].Value.Trim(); done = ($m.Groups[2].Value -eq 'true') }
    }
    [ordered]@{
        exists = $true
        path = $Path
        topDone = $topDone
        taskCount = @($taskObjs).Count
        doneTrueCount = @($taskObjs | Where-Object { $_.done }).Count
        p14 = @($taskObjs | Where-Object { $_.task -match '(?i)^P14' } | ForEach-Object { $_.task + ' done=' + $_.done })
        p15 = @($taskObjs | Where-Object { $_.task -match '(?i)^P15' } | ForEach-Object { $_.task + ' done=' + $_.done })
        p16 = @($taskObjs | Where-Object { $_.task -match '(?i)^P16' } | ForEach-Object { $_.task + ' done=' + $_.done })
        combinedC = @($taskObjs | Where-Object { $_.task -match 'P14 red|P15 red' } | ForEach-Object { $_.task + ' done=' + $_.done })
        tasks = $taskObjs
    } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $Dest -Encoding utf8
}

Extract-TodoDone -Path (Join-Path $out 'todo-plan.txt') -Dest (Join-Path $out 'todo-plan-done-extract.json')
Extract-TodoDone -Path (Join-Path $out 'todo-pluginint.txt') -Dest (Join-Path $out 'todo-pluginint-done-extract.json')
Extract-TodoDone -Path (Join-Path $out 'todo-plan-client.txt') -Dest (Join-Path $out 'todo-plan-client-done-extract.json')
Extract-TodoDone -Path (Join-Path $out 'todo-pluginint-client.txt') -Dest (Join-Path $out 'todo-pluginint-client-done-extract.json')

function Extract-Req {
    param([string]$Path, [string]$Dest)
    if (-not (Test-Path -LiteralPath $Path)) {
        [ordered]@{ exists = $false } | ConvertTo-Json | Set-Content -LiteralPath $Dest -Encoding utf8
        return
    }
    $text = Get-Content -LiteralPath $Path -Raw
    $id = $null
    if ($text -match '(?m)^\s+id:\s*(.+)$') { $id = $Matches[1].Trim() }
    $status = $null
    if ($text -match '(?m)^\s+status:\s*(.+)$') { $status = $Matches[1].Trim() }
    $isSatisfied = $null
    if ($text -match '(?m)^\s+isSatisfied:\s*(true|false)') { $isSatisfied = ($Matches[1] -eq 'true') }
    $acs = [regex]::Matches($text, '(?ms)^\s+- id:\s*(ac-\d+)\r?\n\s+text:\s*(.+?)\r?\n\s+isSatisfied:\s*(true|false)')
    $acObjs = foreach ($m in $acs) {
        [pscustomobject]@{
            id = $m.Groups[1].Value
            text = $m.Groups[2].Value.Trim()
            isSatisfied = ($m.Groups[3].Value -eq 'true')
        }
    }
    [ordered]@{
        exists = $true
        id = $id
        status = $status
        isSatisfied = $isSatisfied
        acCount = @($acObjs).Count
        acSatisfiedTrue = @($acObjs | Where-Object { $_.isSatisfied }).Count
        acs = $acObjs
    } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $Dest -Encoding utf8
}

Extract-Req -Path (Join-Path $out 'req-fr.txt') -Dest (Join-Path $out 'req-fr-extract.json')
Extract-Req -Path (Join-Path $out 'req-tr.txt') -Dest (Join-Path $out 'req-tr-extract.json')
Extract-Req -Path (Join-Path $out 'req-test.txt') -Dest (Join-Path $out 'req-test-extract.json')

$git = git -C 'F:\GitHub\McpServer' status --porcelain -- 'docs/Project/TODO.yaml' 'docs/todo.yaml' 'tests/McpServer.PluginIntegration.Tests'
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    Porcelain = @($git)
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'todo-yaml-git-status.json') -Encoding utf8

$files = @(
    'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T053539Z.md'
    'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T053539Z.json'
    'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T060633Z.md'
    'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T060633Z.json'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginSessionLogWorkflowAdapter.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginSessionLogWorkflowAdapterTests.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginSessionLogWorkflowResult.cs'
    'F:\GitHub\McpServer\docs\plans\PLAN-PLUGINHANDOFF-001.md'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginIntegrationServerFixture.cs'
)
$rows = foreach ($p in $files) {
    $item = Get-Item -LiteralPath $p -ErrorAction SilentlyContinue
    if ($null -eq $item) {
        [ordered]@{ Path = $p; Exists = $false }
    } else {
        [ordered]@{
            Path = $p
            Exists = $true
            LastWriteTimeUtc = $item.LastWriteTimeUtc.ToString('o')
            Length = $item.Length
        }
    }
}
$rows | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $out 'timestamps.json') -Encoding utf8

Write-Output 'EXTRACT_DONE'
