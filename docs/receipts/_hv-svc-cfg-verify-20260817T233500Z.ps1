#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. 'F:\GitHub\McpServer\plugins\core\lib-ps\yaml-object-mutation.ps1'

$utc = [datetime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
Write-Output ("UTC=" + $utc)
Write-Output ("PSVersion=" + $PSVersionTable.PSVersion.ToString())
Write-Output ("ProcessName=" + (Get-Process -Id $PID).ProcessName)

$liveYaml = 'C:\ProgramData\McpServer\appsettings.yaml'
$repoYaml = 'F:\GitHub\McpServer\appsettings.yaml'
$supportYaml = 'F:\GitHub\McpServer\src\McpServer.Support.Mcp\appsettings.yaml'
$optionsCs = 'F:\GitHub\McpServer\src\McpServer.Services\Options\AgentHelpOptions.cs'
$strategyCs = 'F:\GitHub\McpServer\src\McpServer.Services\Services\GrokCliAgentExecutionStrategy.cs'
$oneShotCs = 'F:\GitHub\McpServer\src\McpServer.Services\Services\OneShotCliAgentExecutionStrategy.cs'
$exe = 'C:\ProgramData\McpServer\McpServer.Support.Mcp.exe'
$implReceipt = 'F:\GitHub\McpServer\docs\receipts\windows-service-agenthelp-config-20260817T233017Z.md'

Write-Output '=== PYTHON_PROBE ==='
foreach ($cmd in @('python', 'python3', 'py')) {
    $found = Get-Command $cmd -ErrorAction SilentlyContinue
    if ($found) {
        Write-Output ("CMD_PRESENT {0}={1}" -f $cmd, $found.Source)
    } else {
        Write-Output ("CMD_ABSENT {0}" -f $cmd)
    }
}

Write-Output '=== SERVICE_CIM ==='
$svc = Get-CimInstance -ClassName Win32_Service -Filter "Name = 'McpServer'"
if ($null -eq $svc) {
    Write-Output 'SERVICE=MISSING'
} else {
    Write-Output ("Name=" + $svc.Name)
    Write-Output ("State=" + $svc.State)
    Write-Output ("StartMode=" + $svc.StartMode)
    Write-Output ("StartName=" + $svc.StartName)
    Write-Output ("PathName=" + $svc.PathName)
    Write-Output ("ProcessId=" + $svc.ProcessId)
    Write-Output ("ExitCode=" + $svc.ExitCode)
}

Write-Output '=== PROCESS ==='
Get-CimInstance Win32_Process -Filter "Name = 'McpServer.Support.Mcp.exe'" | ForEach-Object {
    Write-Output ("ProcId={0} CreationDate={1} ExecutablePath={2} CommandLine={3}" -f $_.ProcessId, $_.CreationDate, $_.ExecutablePath, $_.CommandLine)
}

if (Test-Path -LiteralPath $exe) {
    $ei = Get-Item -LiteralPath $exe
    $vi = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($exe)
    Write-Output ("ExeExists=True Length={0} LastWriteTimeUtc={1} FileVersion={2} ProductVersion={3}" -f $ei.Length, $ei.LastWriteTimeUtc.ToString('o'), $vi.FileVersion, $vi.ProductVersion)
} else {
    Write-Output 'ExeExists=False'
}

function Show-AgentHelp {
    param([string]$Label, [string]$Path)
    Write-Output ("=== YAML {0} ===" -f $Label)
    Write-Output ("Path=" + $Path)
    if (-not (Test-Path -LiteralPath $Path)) {
        Write-Output 'EXISTS=False'
        return
    }
    $item = Get-Item -LiteralPath $Path
    Write-Output ("EXISTS=True LastWriteTimeUtc={0} Length={1}" -f $item.LastWriteTimeUtc.ToString('o'), $item.Length)
    $hash = Get-FileHash -LiteralPath $Path -Algorithm SHA256
    Write-Output ("SHA256=" + $hash.Hash)
    $doc = Read-McpYamlObject -Path $Path
    $ah = $null
    if ($doc.Contains('AgentHelp')) { $ah = $doc['AgentHelp'] }
    if ($null -eq $ah) {
        Write-Output 'AgentHelp=MISSING'
        return
    }
    $keys = @($ah.Keys)
    Write-Output ('AgentHelpKeys=' + ($keys -join ','))
    foreach ($k in $keys) {
        $v = $ah[$k]
        if ($null -eq $v) { Write-Output ("AgentHelp.{0}=<null>" -f $k); continue }
        if ($v -is [bool]) { Write-Output ("AgentHelp.{0}={1} TYPE=bool" -f $k, $v); continue }
        if ($v -is [System.Collections.IDictionary] -or ($v -is [System.Collections.IEnumerable] -and -not ($v -is [string]))) {
            Write-Output ("AgentHelp.{0}=<{1}>" -f $k, $v.GetType().Name)
        } else {
            Write-Output ("AgentHelp.{0}={1} TYPE={2}" -f $k, $v, $v.GetType().Name)
        }
    }
    $effort = @($keys | Where-Object { $_ -match 'effort' })
    Write-Output ('EffortLikeKeys=' + ($(if ($effort.Count -gt 0) { $effort -join ',' } else { '<none>' })))
    if ($doc.Contains('VoiceConversation') -and $doc['VoiceConversation'] -is [System.Collections.IDictionary]) {
        $vc = $doc['VoiceConversation']
        if ($vc.Contains('DefaultExecutionStrategy')) {
            Write-Output ("VoiceConversation.DefaultExecutionStrategy=" + $vc['DefaultExecutionStrategy'])
        }
    }
}

Show-AgentHelp -Label 'LIVE' -Path $liveYaml
Show-AgentHelp -Label 'REPO' -Path $repoYaml
Show-AgentHelp -Label 'SUPPORT' -Path $supportYaml

Write-Output '=== SOURCE ==='
$optText = Get-Content -LiteralPath $optionsCs -Raw
Write-Output ("HelperEffortLiteral=" + [bool]($optText -match 'HelperEffort'))
$propMatches = [regex]::Matches($optText, 'public\s+(?:string\??|bool|int|TimeSpan|List<string>)\s+(\w+)')
Write-Output ('TypedProperties=' + (($propMatches | ForEach-Object { $_.Groups[1].Value }) -join ','))
$enabledDefault = [regex]::Match($optText, 'public bool Enabled \{ get; set; \} = (true|false);')
Write-Output ("EnabledDefault=" + $enabledDefault.Groups[1].Value)
$st = Get-Content -LiteralPath $strategyCs -Raw
Write-Output ("HighestEffort=" + [regex]::Match($st, 'private const string HighestEffort = "([^"]+)"').Groups[1].Value)
Write-Output ("Has--effort=" + [bool]($st -match '"--effort"'))
Write-Output ("Has--reasoning-effort=" + [bool]($st -match '"--reasoning-effort"'))
$os = Get-Content -LiteralPath $oneShotCs -Raw
Write-Output ("OneShotGrokHighestEffort=" + [regex]::Match($os, 'private const string GrokHighestEffort = "([^"]+)"').Groups[1].Value)

Write-Output '=== FILE_TIMES ==='
@(
    $optionsCs, $strategyCs, $oneShotCs, $liveYaml, $repoYaml, $supportYaml, $exe, $implReceipt,
    'F:\GitHub\McpServer\src\McpServer.Support.Mcp\McpStdio\FwhMcpTools.AgentHelp.cs',
    'F:\GitHub\McpServer\docs\Project\TODO.yaml'
) | ForEach-Object {
    if (Test-Path -LiteralPath $_) {
        $i = Get-Item -LiteralPath $_
        Write-Output ("TIME {0} lw={1} len={2}" -f $i.FullName, $i.LastWriteTimeUtc.ToString('o'), $i.Length)
    } else {
        Write-Output ("TIME_MISSING {0}" -f $_)
    }
}

Write-Output '=== GIT ==='
Push-Location 'F:\GitHub\McpServer'
try {
    Write-Output '--- porcelain targeted ---'
    git status --porcelain -- appsettings.yaml src/McpServer.Support.Mcp/appsettings.yaml src/McpServer.Services/Options/AgentHelpOptions.cs src/McpServer.Services/Services/GrokCliAgentExecutionStrategy.cs src/McpServer.Services/Services/OneShotCliAgentExecutionStrategy.cs docs/Project/TODO.yaml
    Write-Output '--- repo appsettings hash vs HEAD ---'
    git hash-object -- appsettings.yaml
    git rev-parse HEAD:appsettings.yaml
    Write-Output '--- log -1 appsettings.yaml ---'
    git log -1 --format='%H %cI %s' -- appsettings.yaml
    Write-Output '--- porcelain docs/receipts relevant ---'
    git status --porcelain -- docs/receipts/windows-service-agenthelp-config-20260817T233017Z.md docs/receipts/_update-windows-service-agenthelp-20260817T232801Z.ps1
} finally {
    Pop-Location
}

Write-Output '=== MARKER_META ==='
$marker = Get-Item -LiteralPath 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml'
Write-Output ("MarkerLastWriteTimeUtc=" + $marker.LastWriteTimeUtc.ToString('o'))
Write-Output ("MarkerLength=" + $marker.Length)
