#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-234800Z'
$rows = @()
Get-ChildItem -LiteralPath $outDir -Filter '*.log' | ForEach-Object {
    $text = Get-Content -LiteralPath $_.FullName -Raw
    $failed = $null; $passed = $null; $skipped = $null; $total = $null
    $m = [regex]::Match($text, 'Failed:\s+(\d+),\s+Passed:\s+(\d+),\s+Skipped:\s+(\d+),\s+Total:\s+(\d+)')
    if ($m.Success) {
        $failed = [int]$m.Groups[1].Value
        $passed = [int]$m.Groups[2].Value
        $skipped = [int]$m.Groups[3].Value
        $total = [int]$m.Groups[4].Value
    }
    $exit = $null
    $em = [regex]::Match($text, 'EXIT_[^=]+=(\d+)')
    if (-not $em.Success) {
        $em = [regex]::Match($text, '(?m)^EXIT_.+=(\d+)')
    }
    $exitLine = (Select-String -Path $_.FullName -Pattern 'EXIT_|Passed!|Failed!' | Select-Object -Last 3)
    $rows += [pscustomobject]@{
        log = $_.Name
        failed = $failed
        passed = $passed
        skipped = $skipped
        total = $total
        lastLines = ($exitLine | ForEach-Object { $_.Line }) -join ' | '
        length = $_.Length
    }
    Write-Output ($_.Name + ' Failed=' + $failed + ' Passed=' + $passed + ' Skipped=' + $skipped + ' Total=' + $total)
}
$rows | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $outDir 'test-summary.json') -Encoding utf8

$drive = Get-PSDrive -Name F
Write-Output ('FREE_GB=' + [math]::Round($drive.Free / 1GB, 2))
Write-Output 'SUMMARIZE_DONE'
