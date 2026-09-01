#Requires -Version 7.0
[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$ws = 'F:\GitHub\McpServer'
$plugin = 'F:\GitHub\mcpserver-grok-plugin'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-g8-120-04.json'
$pluginExe = Join-Path $plugin 'lib\Invoke-McpPlugin.ps1'
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$psi = [System.Diagnostics.ProcessStartInfo]::new()
$psi.FileName = 'pwsh.exe'
foreach ($a in @(
    '-NoProfile', '-NonInteractive', '-File', $pluginExe,
    '-Command', 'Status',
    '-WorkspacePath', $ws,
    '-PluginRoot', $plugin,
    '-TimeoutSeconds', '35'
)) { [void]$psi.ArgumentList.Add($a) }
$psi.WorkingDirectory = $ws
$psi.UseShellExecute = $false
$psi.RedirectStandardOutput = $true
$psi.RedirectStandardError = $true
$psi.CreateNoWindow = $true
$p = [System.Diagnostics.Process]::Start($psi)
$exited = $p.WaitForExit(40000)
if (-not $exited) {
    try { $p.Kill($true) } catch { }
}
$stdout = ''
$stderr = ''
try { $stdout = $p.StandardOutput.ReadToEnd() } catch { }
try { $stderr = $p.StandardError.ReadToEnd() } catch { }
$sw.Stop()
$obj = [ordered]@{
    TimestampUtc = [datetime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    TimedOut = (-not $exited)
    ExitCode = if ($exited) { $p.ExitCode } else { $null }
    ElapsedSec = [math]::Round($sw.Elapsed.TotalSeconds, 3)
    StdOut = $stdout
    StdErr = $stderr
}
$obj | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $out -Encoding utf8
Write-Output ("WROTE {0} timedOut={1} exit={2} elapsed={3}" -f $out, (-not $exited), $obj.ExitCode, $obj.ElapsedSec)
