#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p19'
New-Item -ItemType Directory -Force -Path $out | Out-Null
$pluginRoot = 'F:\GitHub\mcpserver-claude-code-plugin'
$files = @(
    'ReplFailsafe.Tests.ps1'
    'HookTurnDedupe.Tests.ps1'
    'CurrentTurnSessionRebind.Tests.ps1'
)

$results = @()
foreach ($file in $files) {
    $path = Join-Path (Join-Path $pluginRoot 'tests') $file
    $log = Join-Path $out ("omitted-$file.log")
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $cmd = @"
Import-Module Pester -Force
`$cfg = New-PesterConfiguration
`$cfg.Run.Path = '$path'
`$cfg.Run.Exit = `$true
`$cfg.Output.Verbosity = 'Detailed'
Invoke-Pester -Configuration `$cfg
"@
    & pwsh.exe -NoProfile -NonInteractive -Command $cmd *>&1 | Tee-Object -FilePath $log | Out-Null
    $exit = $LASTEXITCODE
    $sw.Stop()
    $text = if (Test-Path -LiteralPath $log) { Get-Content -LiteralPath $log -Raw } else { '' }
    $failed = $null; $skipped = $null; $passed = $null
    if ($text -match 'Tests Passed:\s*(\d+),\s*Failed:\s*(\d+),\s*Skipped:\s*(\d+)') {
        $passed = [int]$Matches[1]
        $failed = [int]$Matches[2]
        $skipped = [int]$Matches[3]
    }
    $results += [ordered]@{
        file = $file
        exists = (Test-Path -LiteralPath $path)
        exitCode = $exit
        durationMs = $sw.ElapsedMilliseconds
        passed = $passed
        failed = $failed
        skipped = $skipped
        log = $log
    }
}

$results | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'omitted-pester.json') -Encoding utf8
Write-Output 'OMITTED_PESTER_DONE'
