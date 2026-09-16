#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p5-p6-20260822T012151Z'
$pidDev = 16936
$proc = Get-CimInstance Win32_Process -Filter ("ProcessId=" + $pidDev) -ErrorAction SilentlyContinue
$info = [ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    Pid = $pidDev
    Found = [bool]$proc
    Name = if ($proc) { $proc.Name } else { $null }
    ExecutablePath = if ($proc) { $proc.ExecutablePath } else { $null }
    CommandLine = if ($proc) { $proc.CommandLine } else { $null }
}
$info | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'developer-process.json') -Encoding utf8
Write-Output ('DEV_PROC_FOUND=' + $info.Found)
Write-Output ('DEV_EXE=' + $info.ExecutablePath)

$raw = Get-Content -LiteralPath 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml' -Raw
if ($raw -match '(?m)^apiKey:\s*(.+)$') { $key = $Matches[1].Trim() } else { throw 'no key' }
$headers = @{ 'X-Api-Key' = $key }
try {
    $exec = Invoke-RestMethod -Uri 'http://127.0.0.1:7147/mcpserver/diagnostic/execution-path' -Headers $headers -TimeoutSec 15
    $exec | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'diagnostic-execution-path.json') -Encoding utf8
    Write-Output ('DIAG_EXEC=' + $exec.baseDirectory)
} catch {
    Set-Content -LiteralPath (Join-Path $out 'diagnostic-execution-path.json') -Value $_.Exception.ToString() -Encoding utf8
    Write-Output 'DIAG_EXEC_FAIL'
}
try {
    $apps = Invoke-RestMethod -Uri 'http://127.0.0.1:7147/mcpserver/diagnostic/appsettings-path' -Headers $headers -TimeoutSec 15
    $apps | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'diagnostic-appsettings-path.json') -Encoding utf8
    Write-Output ('DIAG_CONTENT_ROOT=' + $apps.contentRootPath)
} catch {
    Set-Content -LiteralPath (Join-Path $out 'diagnostic-appsettings-path.json') -Value $_.Exception.ToString() -Encoding utf8
    Write-Output 'DIAG_APPSETTINGS_FAIL'
}

$candidateRoots = @(
    'C:\ProgramData\McpServer',
    'F:\GitHub\McpServer',
    'F:\GitHub\McpServer\mcp-data'
)
if ($proc -and $proc.ExecutablePath) {
    $candidateRoots += Split-Path -Parent $proc.ExecutablePath
}
$dbHits = @()
foreach ($root in ($candidateRoots | Select-Object -Unique)) {
    if (Test-Path -LiteralPath $root) {
        $files = Get-ChildItem -LiteralPath $root -Filter '*.db' -Recurse -File -ErrorAction SilentlyContinue |
            Select-Object -First 30 FullName, Length, LastWriteTimeUtc
        foreach ($f in $files) {
            $dbHits += [ordered]@{
                Path = $f.FullName
                Length = $f.Length
                LastWriteTimeUtc = $f.LastWriteTimeUtc.ToString('o')
            }
        }
    }
}
$dbHits | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'developer-db-search.json') -Encoding utf8
Write-Output ('DB_HITS=' + $dbHits.Count)
foreach ($h in $dbHits) {
    Write-Output ($h.Path + ' ' + $h.Length + ' ' + $h.LastWriteTimeUtc)
}
