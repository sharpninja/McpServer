$ErrorActionPreference = 'Stop'
$trx = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\TestResults\p2-unit-legion-20261002T193045Z\unit\McpServer.Support.Mcp.Tests\McpServer.Support.Mcp.Tests.trx'
[xml]$x = Get-Content -LiteralPath $trx
$ns = New-Object System.Xml.XmlNamespaceManager($x.NameTable)
$ns.AddNamespace('t', 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010')
$nodes = $x.SelectNodes('//t:UnitTestResult[@testName="McpServer.Support.Mcp.Tests.Services.CliBrainSlotStrategyTests.CompleteAsync_Grok_UsesIsolatedCwdNotConfiguredRepoRoot"]', $ns)
Write-Output ('NODES=' + $nodes.Count)
foreach ($n in $nodes) {
    $msg = $n.SelectSingleNode('t:Output/t:ErrorInfo/t:Message', $ns)
    $stack = $n.SelectSingleNode('t:Output/t:ErrorInfo/t:StackTrace', $ns)
    if ($msg) { Write-Output ('MSG=' + $msg.InnerText) }
    if ($stack) {
        $s = $stack.InnerText
        if ($s.Length -gt 800) { $s = $s.Substring(0, 800) }
        Write-Output ('STACK=' + $s)
    }
}
