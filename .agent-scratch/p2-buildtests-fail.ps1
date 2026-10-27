$ErrorActionPreference = 'Stop'
$run = 'p2-unit-legion-20261002T200805Z'
$root = "F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\TestResults\$run"
$bt = Join-Path $root 'build-tests\Build.Tests.trx'
[xml]$x = Get-Content -LiteralPath $bt
$ns = New-Object System.Xml.XmlNamespaceManager($x.NameTable)
$ns.AddNamespace('t', 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010')
$failed = $x.SelectNodes('//t:UnitTestResult[@outcome="Failed"]', $ns)
Write-Output ('FAILED_COUNT=' + $failed.Count)
foreach ($node in $failed) {
    Write-Output ('NAME=' + $node.testName)
    $msg = $node.SelectSingleNode('t:Output/t:ErrorInfo/t:Message', $ns)
    $stack = $node.SelectSingleNode('t:Output/t:ErrorInfo/t:StackTrace', $ns)
    if ($msg) { Write-Output $msg.InnerText }
    if ($stack) {
        $s = $stack.InnerText
        if ($s.Length -gt 800) { $s = $s.Substring(0, 800) }
        Write-Output $s
    }
    Write-Output '---'
}
$native = Get-Content -LiteralPath (Join-Path $root 'pester\native-run.json') -Raw
Write-Output ('NATIVE_HEAD=' + $native.Substring(0, [Math]::Min(400, $native.Length)))
$xml = Join-Path $root 'pester\results.xml'
if (Test-Path -LiteralPath $xml) {
    [xml]$p = Get-Content -LiteralPath $xml
    $rootNode = $p.DocumentElement
    Write-Output ('PESTER_XML=' + $rootNode.OuterXml.Substring(0, [Math]::Min(300, $rootNode.OuterXml.Length)))
}
