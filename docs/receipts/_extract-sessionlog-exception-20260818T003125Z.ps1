#Requires -Version 7.0
$ErrorActionPreference = 'Stop'

$path = 'C:\ProgramData\McpServer\logs\mcp-20260817.log'
$fs = [System.IO.File]::Open($path, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
try {
    $reader = [System.IO.StreamReader]::new($fs)
    $matches = [System.Collections.Generic.List[string]]::new()
    $buffer = New-Object System.Collections.Generic.Queue[string]
    $emitRemaining = 0
    $lineNumber = 0
    $backendCount = 0
    $unhandledCount = 0
    $status503Count = 0
    while ($null -ne ($line = $reader.ReadLine())) {
        $lineNumber++
        if ($buffer.Count -ge 3) { [void]$buffer.Dequeue() }
        $buffer.Enqueue(('{0}|{1}' -f $lineNumber, $line))

        if ($line.Contains('backend_unavailable')) { $backendCount++ }
        if ($line.Contains('completed with 503')) { $status503Count++ }
        if ($line.Contains('Unhandled exception in middleware pipeline: POST /mcpserver/sessionlog')) {
            $unhandledCount++
            foreach ($prior in $buffer) { $matches.Add($prior) }
            $emitRemaining = 25
        } elseif ($emitRemaining -gt 0) {
            $matches.Add(('{0}|{1}' -f $lineNumber, $line))
            $emitRemaining--
        }
    }
} finally {
    $fs.Dispose()
}

Write-Output ('backend_unavailableCount=' + $backendCount)
Write-Output ('sessionlogUnhandledCount=' + $unhandledCount)
Write-Output ('completedWith503Count=' + $status503Count)
Write-Output '--- exception windows ---'
$matches | ForEach-Object { Write-Output $_ }
