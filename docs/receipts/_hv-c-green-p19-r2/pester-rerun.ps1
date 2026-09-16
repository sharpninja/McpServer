#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p19-r2'
New-Item -ItemType Directory -Force -Path $out | Out-Null
$log = Join-Path $out 'independent-claude-code-pester.log'
$tests = 'F:\GitHub\mcpserver-claude-code-plugin\tests'
$sw = [System.Diagnostics.Stopwatch]::StartNew()
& pwsh.exe -NoProfile -NonInteractive -Command "Invoke-Pester -Path '$tests' -CI" *>&1 |
    Tee-Object -FilePath $log | Out-Null
$exit = $LASTEXITCODE
$sw.Stop()
$raw = Get-Content -LiteralPath $log -Raw
$plain = [regex]::Replace($raw, '\x1B\[[0-9;]*[A-Za-z]', '')
$disc = [regex]::Match($plain, 'Starting discovery in (\d+) files')
$found = [regex]::Match($plain, 'Discovery found (\d+) tests')
$passedLine = [regex]::Match($plain, 'Tests Passed:\s*(\d+),\s*Failed:\s*(\d+),\s*Skipped:\s*(\d+)')
[ordered]@{
    command = "pwsh.exe -NoProfile -NonInteractive -Command `"Invoke-Pester -Path '$tests' -CI`""
    exitCode = $exit
    durationMs = $sw.ElapsedMilliseconds
    discoveryFiles = if ($disc.Success) { [int]$disc.Groups[1].Value } else { $null }
    discoveryTests = if ($found.Success) { [int]$found.Groups[1].Value } else { $null }
    passed = if ($passedLine.Success) { [int]$passedLine.Groups[1].Value } else { $null }
    failed = if ($passedLine.Success) { [int]$passedLine.Groups[2].Value } else { $null }
    skipped = if ($passedLine.Success) { [int]$passedLine.Groups[3].Value } else { $null }
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'independent-claude-code-pester.json') -Encoding utf8
Write-Output ("PESTER_EXIT=$exit DISCOVERY=$($disc.Groups[1].Value) PASSED=$($passedLine.Groups[1].Value) FAILED=$($passedLine.Groups[2].Value) SKIPPED=$($passedLine.Groups[3].Value)")
