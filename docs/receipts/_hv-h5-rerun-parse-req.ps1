#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$outDir = 'F:\GitHub\McpServer\docs\receipts'

function Get-Inner {
    param([string]$Name)
    $outer = Get-Content -LiteralPath (Join-Path $outDir $Name) -Raw | ConvertFrom-Json
    return ($outer.result.content[0].text | ConvertFrom-Json)
}

$tr = Get-Inner '_hv-h5-rerun-req-tr.json'
$trItems = @($tr.items)
Write-Output ('TR_TOTAL=' + $trItems.Count)
foreach ($item in $trItems) {
    $id = [string]$item.Id
    if ($id -like 'TR-MCP-PRODUCT-*') {
        Write-Output ('PRODUCT_TR ID=' + $id + ' TITLE=' + $item.Title + ' STATUS=' + $item.Status)
    }
}

# Copy independent full test transcript if present
$src = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a015c3-bb99-7833-9dd0-9f6518e2b97b\terminal\call-a500a221-5db9-4035-a5c0-f30d110a62ef-25.log'
$dest = Join-Path $outDir '_hv-h5-rerun-full-test.txt'
if (Test-Path $src) {
    Copy-Item -LiteralPath $src -Destination $dest -Force
    Write-Output ('COPIED_FULL_TEST=' + (Get-Item $dest).Length)
} else {
    Write-Output 'FULL_TEST_SRC_MISSING'
}

# Client/REPL product tests
$clientTests = Get-ChildItem -Path 'F:\GitHub\McpServer\tests' -Recurse -Filter *Product*.cs |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }
Write-Output 'PRODUCT_TEST_FILES:'
$clientTests | ForEach-Object { Write-Output ('  ' + $_.FullName.Substring('F:\GitHub\McpServer\'.Length) + ' LW=' + $_.LastWriteTimeUtc.ToString('o')) }

# Dispatcher in stdio products
$stdio = 'F:\GitHub\McpServer\src\McpServer.Support.Mcp\McpStdio\FwhMcpTools.Products.cs'
$disp = @(Select-String -Path $stdio -Pattern 'SendAsync|QueryAsync|IDispatcher')
Write-Output ('STDIO_DISPATCH_HITS=' + $disp.Count)
$disp | ForEach-Object { Write-Output ('STDIO_DISP=' + $_.LineNumber + ':' + $_.Line.Trim()) }

# Handoff lock snippet hash
$handoff = 'F:\GitHub\McpServer\tests\McpServer.Support.Mcp.Tests\Services\HandoffDurabilityTests.cs'
Write-Output ('HANDOFF_SHA256=' + (Get-FileHash -LiteralPath $handoff -Algorithm SHA256).Hash)
Write-Output ('HANDOFF_LW=' + (Get-Item $handoff).LastWriteTimeUtc.ToString('o'))

Write-Output 'PARSE_DONE'
