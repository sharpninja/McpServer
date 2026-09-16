#Requires -Version 7.0
param(
    [Parameter(Mandatory)][string]$TestsDir,
    [Parameter(Mandatory)][string]$ResultJson
)
Set-StrictMode -Version Latest
$ErrorActionPreference = "Continue"
if (Get-Variable -Name PSStyle -ErrorAction SilentlyContinue) {
    $PSStyle.OutputRendering = [System.Management.Automation.OutputRendering]::PlainText
}
$env:NO_COLOR = "1"
$env:TERM = "dumb"
Import-Module Pester -ErrorAction Stop
$files = @(Get-ChildItem -LiteralPath $TestsDir -Filter "*.Tests.ps1" -File | Sort-Object Name | ForEach-Object { $_.FullName })
Write-Output ("Discovery files=" + $files.Count)
foreach ($f in $files) { Write-Output ("FILE " + [System.IO.Path]::GetFileName($f)) }
$cfg = New-PesterConfiguration
$cfg.Run.Path = $files
$cfg.Run.PassThru = $true
$cfg.Run.Exit = $false
$cfg.Output.Verbosity = "Detailed"
try { $cfg.Output.CIFormat = "None" } catch {}
try { $cfg.Output.RenderMode = "Plaintext" } catch {}
$result = Invoke-Pester -Configuration $cfg
$passed = 0; $failed = 0; $skipped = 0; $total = 0
if ($null -ne $result) {
    $passed = [int]$result.PassedCount
    $failed = [int]$result.FailedCount
    $skipped = [int]$result.SkippedCount
    $total = [int]$result.TotalCount
}
Write-Output ("Tests Passed: {0}, Failed: {1}, Skipped: {2}" -f $passed, $failed, $skipped)
[pscustomobject]@{
    PassedCount = $passed
    FailedCount = $failed
    SkippedCount = $skipped
    TotalCount = $total
    DiscoveryFileCount = $files.Count
} | ConvertTo-Json | Set-Content -LiteralPath $ResultJson -Encoding utf8
if ($failed -ne 0 -or $skipped -ne 0) { exit 1 }
exit 0
