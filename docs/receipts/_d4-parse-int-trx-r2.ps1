$ErrorActionPreference = 'Stop'
$trx = 'F:\GitHub\McpServer\tests\McpServer.Support.Mcp.IntegrationTests\TestResults\d4-integrationtests-r2.trx'
[xml]$xml = Get-Content -Raw $trx
$ns = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
$ns.AddNamespace('t', 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010')
$counters = $xml.SelectSingleNode('//t:ResultSummary/t:Counters', $ns)
Write-Output ("counters total={0} executed={1} passed={2} failed={3}" -f $counters.total, $counters.executed, $counters.passed, $counters.failed)
$xml.SelectNodes('//t:UnitTestResult', $ns) | ForEach-Object {
    if ($_.outcome -ne 'Passed') {
        Write-Output ("{0} :: {1}" -f $_.outcome, $_.testName)
    }
}
$xml.SelectNodes('//t:UnitTestResult', $ns) | Where-Object { $_.testName -match 'RequirementScopeLayer|MarkerRegeneration' } | ForEach-Object {
    Write-Output ("named {0} :: {1}" -f $_.outcome, $_.testName)
}
