#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$logPath = 'C:\ProgramData\McpServer\logs\mcp-20260817.log'
$fs = [System.IO.File]::Open($logPath, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
$hits = New-Object System.Collections.Generic.List[string]
try {
    $reader = [System.IO.StreamReader]::new($fs)
    while ($null -ne ($line = $reader.ReadLine())) {
        if ($line -match 'postrestart3') {
            $outIdx = $line.IndexOf(', Output:')
            if ($outIdx -ge 0) {
                $tail = $line.Substring($outIdx)
                if ($tail.Length -gt 450) { $tail = $tail.Substring(0, 450) }
                [void]$hits.Add($line.Substring(0, [Math]::Min(180, $line.Length)) + ' || ' + $tail)
            } else {
                [void]$hits.Add($line.Substring(0, [Math]::Min(400, $line.Length)))
            }
        }
    }
} finally {
    $fs.Dispose()
}
Write-Output ('Postrestart3Hits=' + $hits.Count)
foreach ($h in $hits) { Write-Output $h }

$todo = Get-Item -LiteralPath 'F:\GitHub\McpServer\docs\Project\TODO.yaml'
Write-Output ('TodoYamlLw=' + $todo.LastWriteTimeUtc.ToString('o'))
$plugin = Get-Content -LiteralPath 'F:\GitHub\mcpserver-grok-plugin\.grok-plugin\plugin.json' -Raw
Write-Output ('PluginJsonSnippet=' + $plugin.Substring(0, [Math]::Min(300, $plugin.Length)))
Write-Output 'POST_DONE'
