$ErrorActionPreference = 'Continue'
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
$paths = @(
    "$wt\docs\receipts\hv\hostile-validator-sessionlife-p2-20261002T203600Z.md",
    "$wt\docs\receipts\hv\hostile-validator-sessionlife-p2-20261002T203600Z.json",
    "$wt\docs\receipts\hv\20261002T203600Z-sessionlife-p2-contracts-hv.response.jsonl",
    "$wt\.agent-scratch\hv-p2-astra-last-20261002T203600Z.txt",
    "$wt\.agent-scratch\hv-p2-astra-stderr-20261002T203600Z.txt"
)
foreach ($p in $paths) {
    if (Test-Path -LiteralPath $p) {
        $i = Get-Item -LiteralPath $p
        Write-Output ("FILE $($i.Length) $($i.LastWriteTimeUtc.ToString('o')) $($i.Name)")
    } else {
        Write-Output ("MISSING $p")
    }
}
$proc = Get-Process -Id 138084 -ErrorAction SilentlyContinue
if ($proc) {
    Write-Output ("CODEX_ALIVE cpu=$($proc.CPU) ws=$($proc.WorkingSet64)")
} else {
    Write-Output 'CODEX_138084_GONE'
}
Get-CimInstance Win32_Process -Filter "Name='codex.exe'" | Where-Object { $_.CreationDate -gt (Get-Date).AddHours(-3) } | ForEach-Object {
    Write-Output ("PID=$($_.ProcessId) PARENT=$($_.ParentProcessId) START=$($_.CreationDate)")
}
