#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p16-p18-r2'

function Extract-TodoDone {
    param([string]$Path, [string]$Dest)
    if (-not (Test-Path -LiteralPath $Path)) {
        [ordered]@{ exists = $false; path = $Path } | ConvertTo-Json | Set-Content -LiteralPath $Dest -Encoding utf8
        return
    }
    $text = Get-Content -LiteralPath $Path -Raw
    $topDone = $null
    if ($text -match '(?m)^\s+done:\s+(true|false)\s*$') { $topDone = ($Matches[1] -eq 'true') }
    $taskObjs = foreach ($m in [regex]::Matches($text, '(?m)^\s+- task:\s*(.+?)\r?\n\s+done:\s+(true|false)')) {
        [pscustomobject]@{ task = $m.Groups[1].Value.Trim(); done = ($m.Groups[2].Value -eq 'true') }
    }
    [ordered]@{
        exists = $true
        path = $Path
        topDone = $topDone
        taskCount = @($taskObjs).Count
        doneTrueCount = @($taskObjs | Where-Object { $_.done }).Count
        p16 = @($taskObjs | Where-Object { $_.task -match 'P16' })
        p17 = @($taskObjs | Where-Object { $_.task -match 'P17' })
        p18 = @($taskObjs | Where-Object { $_.task -match 'P18' })
        p19 = @($taskObjs | Where-Object { $_.task -match 'P19' })
        combinedC = @($taskObjs | Where-Object { $_.task -match 'C P16|P16 red \+ hostile then P17' })
        hasError = [bool]($text -match '(?i)^ERROR ')
    } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $Dest -Encoding utf8
}

Extract-TodoDone -Path (Join-Path $out 'todo-plan-client-final.txt') -Dest (Join-Path $out 'todo-plan-final-done.json')
Extract-TodoDone -Path (Join-Path $out 'todo-pluginint-client-final.txt') -Dest (Join-Path $out 'todo-pluginint-final-done.json')

$trx = Get-ChildItem -LiteralPath $out -Recurse -Filter 'nuke-skip-reread.trx' -ErrorAction SilentlyContinue | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
if ($trx) {
    [xml]$x = Get-Content -LiteralPath $trx.FullName
    $ns = New-Object System.Xml.XmlNamespaceManager($x.NameTable)
    $ns.AddNamespace('t', 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010')
    $c = $x.SelectSingleNode('//t:ResultSummary/t:Counters', $ns)
    $u = $x.SelectSingleNode('//t:UnitTestResult', $ns)
    function Attr([object]$node, [string]$name) {
        if ($null -eq $node) { return $null }
        $a = $node.Attributes[$name]
        if ($null -eq $a) { return $null }
        return [string]$a.Value
    }
    [ordered]@{
        path = $trx.FullName
        lastWriteTimeUtc = $trx.LastWriteTimeUtc.ToString('o')
        total = (Attr $c 'total')
        executed = (Attr $c 'executed')
        passed = (Attr $c 'passed')
        failed = (Attr $c 'failed')
        skipped = (Attr $c 'skipped')
        notExecuted = (Attr $c 'notExecuted')
        testName = (Attr $u 'testName')
        outcome = (Attr $u 'outcome')
        duration = (Attr $u 'duration')
    } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'nuke-reread-trx-summary.json') -Encoding utf8
}

$paths = @(
    'F:\GitHub\McpServer\build\Build.PluginSessionLogIntegration.cs',
    'F:\GitHub\McpServer\tests\Build.Tests\PluginSessionLogIntegrationTargetTests.cs'
)
$items = foreach ($p in $paths) {
    $i = Get-Item -LiteralPath $p
    [ordered]@{ Name = $i.Name; LastWriteTimeUtc = $i.LastWriteTimeUtc.ToString('o'); Length = $i.Length }
}
$items | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'file-timestamps-after-nuke-reread.json') -Encoding utf8
Write-Output 'EXTRACT_FINAL_DONE'
