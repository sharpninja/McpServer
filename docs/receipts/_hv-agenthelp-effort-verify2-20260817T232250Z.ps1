#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. 'F:\GitHub\McpServer\plugins\core\lib-ps\yaml-object-mutation.ps1'

$liveYaml = 'C:\ProgramData\McpServer\appsettings.yaml'
$repoYaml = 'F:\GitHub\McpServer\appsettings.yaml'
$supportYaml = 'F:\GitHub\McpServer\src\McpServer.Support.Mcp\appsettings.yaml'
$optionsCs = 'F:\GitHub\McpServer\src\McpServer.Services\Options\AgentHelpOptions.cs'
$strategyCs = 'F:\GitHub\McpServer\src\McpServer.Services\Services\GrokCliAgentExecutionStrategy.cs'
$oneShotCs = 'F:\GitHub\McpServer\src\McpServer.Services\Services\OneShotCliAgentExecutionStrategy.cs'
$exe = 'C:\ProgramData\McpServer\McpServer.Support.Mcp.exe'

function Show-AgentHelp {
    param([string]$Label, [string]$Path)
    Write-Output ("=== {0} {1} ===" -f $Label, $Path)
    $item = Get-Item -LiteralPath $Path
    Write-Output ("LastWriteTimeUtc=" + $item.LastWriteTimeUtc.ToString('o') + " Length=" + $item.Length)
    $doc = Read-McpYamlObject -Path $Path
    $ah = $null
    if ($doc.Contains('AgentHelp')) { $ah = $doc.AgentHelp }
    elseif ($doc.Contains('Mcp') -and $doc.Mcp -and $doc.Mcp.Contains('AgentHelp')) { $ah = $doc.Mcp.AgentHelp }
    if ($null -eq $ah) {
        Write-Output 'AgentHelp=MISSING'
        return
    }
    $keys = @($ah.Keys)
    Write-Output ('Keys=' + ($keys -join ','))
    foreach ($k in $keys) {
        $v = $ah[$k]
        if ($null -eq $v) { Write-Output ("  {0}=<null>" -f $k); continue }
        if ($v -is [System.Collections.IDictionary] -or ($v -is [System.Collections.IEnumerable] -and -not ($v -is [string]))) {
            Write-Output ("  {0}=<{1}>" -f $k, $v.GetType().Name)
        } else {
            Write-Output ("  {0}={1}" -f $k, $v)
        }
    }
    $effort = @($keys | Where-Object { $_ -match 'effort' })
    Write-Output ('EffortLikeKeys=' + ($(if ($effort) { $effort -join ',' } else { '<none>' })))
}

Show-AgentHelp -Label 'LIVE' -Path $liveYaml
Show-AgentHelp -Label 'REPO' -Path $repoYaml
Show-AgentHelp -Label 'SUPPORT' -Path $supportYaml

Write-Output '=== SOURCE ==='
$optText = Get-Content -LiteralPath $optionsCs -Raw
Write-Output ("HelperEffortLiteral=" + ($optText -match 'HelperEffort'))
$propMatches = [regex]::Matches($optText, 'public\s+(?:string\??|bool|int|TimeSpan|List<string>)\s+(\w+)')
Write-Output ('TypedProperties=' + (($propMatches | ForEach-Object { $_.Groups[1].Value }) -join ','))
$st = Get-Content -LiteralPath $strategyCs -Raw
Write-Output ("HighestEffort=" + [regex]::Match($st, 'private const string HighestEffort = "([^"]+)"').Groups[1].Value)
Write-Output ("Has--effort=" + [bool]($st -match '"--effort"'))
Write-Output ("Has--reasoning-effort=" + [bool]($st -match '"--reasoning-effort"'))
$os = Get-Content -LiteralPath $oneShotCs -Raw
Write-Output ("OneShotGrokHighestEffort=" + [regex]::Match($os, 'private const string GrokHighestEffort = "([^"]+)"').Groups[1].Value)

Write-Output '=== TIMES ==='
@(
    $optionsCs, $strategyCs, $oneShotCs, $liveYaml, $exe, $repoYaml,
    'F:\GitHub\McpServer\docs\receipts\agenthelp-effort-high-20260817T231702Z.md'
) | ForEach-Object {
    $i = Get-Item -LiteralPath $_
    Write-Output ("TIME {0} lw={1} len={2}" -f $i.Name, $i.LastWriteTimeUtc.ToString('o'), $i.Length)
}

Write-Output '=== EXE META ==='
$ei = Get-Item -LiteralPath $exe
Write-Output ("ExeLength={0} LastWriteTimeUtc={1}" -f $ei.Length, $ei.LastWriteTimeUtc.ToString('o'))
$vi = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($exe)
Write-Output ("FileVersion={0} ProductVersion={1}" -f $vi.FileVersion, $vi.ProductVersion)

Write-Output '=== SERVICE/PROC ==='
$svc = Get-Service -Name 'McpServer' -ErrorAction SilentlyContinue
if ($svc) { Write-Output ("ServiceStatus=" + $svc.Status) }
Get-CimInstance Win32_Process -Filter "Name = 'McpServer.Support.Mcp.exe'" | ForEach-Object {
    Write-Output ("ProcId={0} CreationDate={1} Path={2}" -f $_.ProcessId, $_.CreationDate, $_.ExecutablePath)
}

Write-Output '=== GIT ==='
Set-Location 'F:\GitHub\McpServer'
git status --porcelain -- src/McpServer.Services/Options/AgentHelpOptions.cs src/McpServer.Services/Services/GrokCliAgentExecutionStrategy.cs src/McpServer.Services/Services/OneShotCliAgentExecutionStrategy.cs appsettings.yaml src/McpServer.Support.Mcp/appsettings.yaml
Write-Output '--- log ---'
git log -8 --format='%H %cI %s' -- src/McpServer.Services/Services/GrokCliAgentExecutionStrategy.cs
Write-Output '--- blame ---'
git blame -L 23,26 -- src/McpServer.Services/Services/GrokCliAgentExecutionStrategy.cs
Write-Output '--- porcelain all ---'
git status --porcelain

Write-Output '=== GROK HELP ==='
$grok = Get-Command grok -ErrorAction SilentlyContinue
if ($grok) {
    Write-Output ("GrokPath=" + $grok.Source)
    $help = & grok --help 2>&1 | Out-String
    $help -split "`r?`n" | Where-Object { $_ -match 'effort|reasoning' } | ForEach-Object { Write-Output ("HELP " + $_) }
} else {
    Write-Output 'GrokCommand=MISSING'
}

Write-Output '=== USER GROK CONFIG ==='
$cfg = Join-Path $env:USERPROFILE '.grok\config.toml'
if (Test-Path -LiteralPath $cfg) {
    Write-Output ("GrokConfigLwUtc=" + (Get-Item -LiteralPath $cfg).LastWriteTimeUtc.ToString('o'))
    Select-String -LiteralPath $cfg -Pattern 'effort|model' | ForEach-Object { Write-Output ("CFG " + $_.Line.Trim()) }
}

Write-Output 'DONE'
