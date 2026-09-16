#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p5-p6-20260822T012151Z'

function Parse-Trx {
    param([string]$Path, [string]$Name)
    [xml]$xml = Get-Content -LiteralPath $Path
    $ns = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
    $ns.AddNamespace('t', 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010')
    $counters = $xml.SelectSingleNode('//t:ResultSummary/t:Counters', $ns)
    $units = @($xml.SelectNodes('//t:UnitTestResult', $ns) | ForEach-Object {
        [ordered]@{
            testName = $_.testName
            outcome = $_.outcome
            duration = $_.duration
            startTime = $_.startTime
            endTime = $_.endTime
        }
    })
    $obj = [ordered]@{
        File = $Path
        Label = $Name
        total = $counters.total
        executed = $counters.executed
        passed = $counters.passed
        failed = $counters.failed
        notExecuted = $counters.notExecuted
        timeout = $counters.timeout
        aborted = $counters.aborted
        inconclusive = $counters.inconclusive
        skippedAttr = $counters.skipped
        results = $units
        failedNames = @($units | Where-Object { $_.outcome -eq 'Failed' } | ForEach-Object { $_.testName })
        skippedOrNotExecutedNames = @($units | Where-Object { $_.outcome -in @('NotExecuted', 'Skipped', 'Timeout') } | ForEach-Object { $_.testName })
    }
    $obj | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out ($Name + '-trx.json')) -Encoding utf8
    Write-Output ("TRX_$Name total=$($obj.total) executed=$($obj.executed) passed=$($obj.passed) failed=$($obj.failed) notExecuted=$($obj.notExecuted) skippedAttr=$($obj.skippedAttr) resultCount=$($units.Count)")
    foreach ($u in $units) {
        Write-Output ("  " + $u.outcome + " " + $u.duration + " " + $u.testName)
    }
}

Parse-Trx -Path (Join-Path $out 'trx-filter\dotnet-fixture-filter.trx') -Name 'filter'
Parse-Trx -Path (Join-Path $out 'trx-all\dotnet-pluginintegration-all.trx') -Name 'all'

$before = Get-Content -LiteralPath (Join-Path $out 'isolation-before.json') -Raw | ConvertFrom-Json
$after = Get-Content -LiteralPath (Join-Path $out 'isolation-after.json') -Raw | ConvertFrom-Json
$cmp = @()
for ($i = 0; $i -lt $before.Dbs.Count; $i++) {
    $b = $before.Dbs[$i]
    $a = @($after.Dbs | Where-Object { $_.Path -eq $b.Path }) | Select-Object -First 1
    $cmp += [ordered]@{
        Path = $b.Path
        BeforeExists = $b.Exists
        AfterExists = if ($a) { $a.Exists } else { $false }
        BeforeLength = $b.Length
        AfterLength = $a.Length
        BeforeWrite = $b.LastWriteTimeUtc
        AfterWrite = $a.LastWriteTimeUtc
        WriteChanged = [string]$b.LastWriteTimeUtc -ne [string]$a.LastWriteTimeUtc
        LengthChanged = $b.Length -ne $a.Length
    }
}
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    BeforeTempCount = @($before.TempPluginIntDirs).Count
    AfterTempCount = @($after.TempPluginIntDirs).Count
    Before7147Pid = @($before.Listeners7147 | ForEach-Object { $_.OwningProcess })
    After7147Pid = @($after.Listeners7147 | ForEach-Object { $_.OwningProcess })
    Dbs = $cmp
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'isolation-compare.json') -Encoding utf8
Write-Output 'ISOLATION_COMPARE_WRITTEN'
foreach ($row in $cmp) {
    Write-Output ("DB " + $row.Path + " writeChanged=" + $row.WriteChanged + " lengthChanged=" + $row.LengthChanged + " afterExists=" + $row.AfterExists)
}

$logPath = 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\bin\Debug\net10.0\logs\mcp-20260821.log'
$listening = Select-String -LiteralPath $logPath -Pattern 'Now listening on|listening on http|mcp-pluginint-|Data Source=|7147' | Select-Object -Last 40
$listening | ForEach-Object { $_.Line } | Set-Content -LiteralPath (Join-Path $out 'support-log-listen.txt') -Encoding utf8
Write-Output ('LOG_HITS=' + @($listening).Count)
