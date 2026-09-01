#Requires -Version 7.0
# Hostile validator evidence collector. Review only. Does not mutate product code or restart services.
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$utc = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
Write-Output "VALIDATOR_UTC=$utc"

$liveYaml = 'C:\ProgramData\McpServer\appsettings.yaml'
$exe = 'C:\ProgramData\McpServer\McpServer.Support.Mcp.exe'
$repoYaml = 'F:\GitHub\McpServer\appsettings.yaml'
$supportYaml = 'F:\GitHub\McpServer\src\McpServer.Support.Mcp\appsettings.yaml'
$optionsCs = 'F:\GitHub\McpServer\src\McpServer.Services\Options\AgentHelpOptions.cs'
$strategyCs = 'F:\GitHub\McpServer\src\McpServer.Services\Services\GrokCliAgentExecutionStrategy.cs'
$oneShotCs = 'F:\GitHub\McpServer\src\McpServer.Services\Services\OneShotCliAgentExecutionStrategy.cs'
$marker = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml'
$pluginRoot = 'F:\GitHub\mcpserver-grok-plugin'
$mutation = 'F:\GitHub\McpServer\plugins\core\lib-ps\yaml-object-mutation.ps1'
$markerResolver = 'F:\GitHub\McpServer\plugins\core\lib-ps\marker-resolver.ps1'

Write-Output '=== MARKER SIGNATURE ==='
. $markerResolver
$sig = Test-MarkerSignature -MarkerFile $marker
Write-Output "Test-MarkerSignature=$sig"

Write-Output '=== HEALTH NONCE ==='
$nonce = [guid]::NewGuid().ToString('N')
$healthUrl = "http://PAYTON-LEGION2:7147/health?nonce=$nonce"
$health = Invoke-WebRequest -Uri $healthUrl -UseBasicParsing -TimeoutSec 30
Write-Output "HealthStatus=$($health.StatusCode)"
Write-Output "NonceSent=$nonce"
Write-Output "HealthBody=$($health.Content)"
$echoOk = $health.Content -like "*$nonce*"
Write-Output "NonceEchoed=$echoOk"

Write-Output '=== LIVE YAML OBJECT ==='
. $mutation
$liveObj = Import-McpYamlFile -Path $liveYaml
$ah = $liveObj.Mcp.AgentHelp
if (-not $ah) { $ah = $liveObj.AgentHelp }
Write-Output ("LiveYamlLastWriteUtc=" + (Get-Item -LiteralPath $liveYaml).LastWriteTimeUtc.ToString('o'))
Write-Output ("LiveYamlLength=" + (Get-Item -LiteralPath $liveYaml).Length)
if ($null -eq $ah) {
    Write-Output 'AgentHelpSection=MISSING'
} else {
    Write-Output ('AgentHelpType=' + $ah.GetType().FullName)
    $keys = @($ah.PSObject.Properties.Name)
    Write-Output ('AgentHelpKeys=' + ($keys -join ','))
    foreach ($k in $keys) {
        $v = $ah.$k
        if ($v -is [System.Collections.IEnumerable] -and -not ($v -is [string])) {
            Write-Output ("AgentHelp.$k=<collection>")
        } else {
            Write-Output ("AgentHelp.$k=$v")
        }
    }
    $effortKeys = @($keys | Where-Object { $_ -match 'effort' })
    Write-Output ('EffortLikeKeys=' + ($(if ($effortKeys) { $effortKeys -join ',' } else { '<none>' })))
}

Write-Output '=== REPO YAML AGENTHELP ==='
$repoObj = Import-McpYamlFile -Path $repoYaml
$rah = $repoObj.Mcp.AgentHelp
if (-not $rah) { $rah = $repoObj.AgentHelp }
if ($rah) {
    Write-Output ("RepoDefaultExecutionStrategy=" + $rah.DefaultExecutionStrategy)
    Write-Output ("RepoHelperModel=" + $rah.HelperModel)
    $rkeys = @($rah.PSObject.Properties.Name)
    Write-Output ('RepoAgentHelpKeys=' + ($rkeys -join ','))
} else {
    Write-Output 'RepoAgentHelp=MISSING'
}

Write-Output '=== SUPPORT YAML AGENTHELP ==='
$supObj = Import-McpYamlFile -Path $supportYaml
$sah = $supObj.Mcp.AgentHelp
if (-not $sah) { $sah = $supObj.AgentHelp }
if ($sah) {
    Write-Output ("SupportDefaultExecutionStrategy=" + $sah.DefaultExecutionStrategy)
    Write-Output ("SupportHelperModel=" + $sah.HelperModel)
} else {
    Write-Output 'SupportAgentHelp=MISSING'
}

Write-Output '=== SOURCE PROPERTY NAMES ==='
$optText = Get-Content -LiteralPath $optionsCs -Raw
Write-Output ("AgentHelpOptionsHasHelperEffort=" + ($optText -match 'HelperEffort'))
Write-Output ("AgentHelpOptionsHasEffortProperty=" + ($optText -match 'public\s+\S+\s+\w*[Ee]ffort\w*\s*\{'))
$props = [regex]::Matches($optText, 'public\s+[^\n{]+\{') | ForEach-Object { $_.Value.Trim() }
Write-Output 'AgentHelpOptionsPublicMembers:'
$props | ForEach-Object { Write-Output ("  " + $_) }

Write-Output '=== STRATEGY CONST ==='
$st = Get-Content -LiteralPath $strategyCs -Raw
$m = [regex]::Match($st, 'private const string HighestEffort = "([^"]+)"')
Write-Output ("HighestEffort=" + $m.Groups[1].Value)
Write-Output ("EmitsEffortFlag=" + ($st -match '"--effort"'))
Write-Output ("EmitsReasoningEffortFlag=" + ($st -match '"--reasoning-effort"'))
$os = Get-Content -LiteralPath $oneShotCs -Raw
$gm = [regex]::Match($os, 'private const string GrokHighestEffort = "([^"]+)"')
Write-Output ("OneShotGrokHighestEffort=" + $gm.Groups[1].Value)

Write-Output '=== FILE TIMES UTC ==='
@(
    $optionsCs, $strategyCs, $oneShotCs, $liveYaml, $exe, $repoYaml,
    'F:\GitHub\McpServer\docs\receipts\agenthelp-effort-high-20260817T231702Z.md'
) | ForEach-Object {
    $i = Get-Item -LiteralPath $_
    Write-Output ("TIME " + $i.FullName + " lw=" + $i.LastWriteTimeUtc.ToString('o') + " len=" + $i.Length)
}

Write-Output '=== EXE METADATA ==='
if (Test-Path -LiteralPath $exe) {
    $ei = Get-Item -LiteralPath $exe
    Write-Output ("ExeExists=True Length=" + $ei.Length + " LastWriteTimeUtc=" + $ei.LastWriteTimeUtc.ToString('o'))
    $vi = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($exe)
    Write-Output ("FileVersion=" + $vi.FileVersion + " ProductVersion=" + $vi.ProductVersion)
} else {
    Write-Output 'ExeExists=False'
}

Write-Output '=== RUNNING PROCESS ==='
$svc = Get-Service -Name 'McpServer' -ErrorAction SilentlyContinue
if ($svc) {
    Write-Output ("ServiceStatus=" + $svc.Status)
}
Get-CimInstance Win32_Process -Filter "Name='McpServer.Support.Mcp.exe'" | ForEach-Object {
    Write-Output ("ProcId=" + $_.ProcessId + " CreationDate=" + $_.CreationDate + " ExecutablePath=" + $_.ExecutablePath)
}

Write-Output '=== GIT STATUS PRODUCT ==='
Push-Location 'F:\GitHub\McpServer'
try {
    git status --porcelain -- src/McpServer.Services/Options/AgentHelpOptions.cs src/McpServer.Services/Services/GrokCliAgentExecutionStrategy.cs src/McpServer.Services/Services/OneShotCliAgentExecutionStrategy.cs appsettings.yaml src/McpServer.Support.Mcp/appsettings.yaml
    Write-Output '--- git log strategy ---'
    git log -5 --format='%H %cI %s' -- src/McpServer.Services/Services/GrokCliAgentExecutionStrategy.cs
    Write-Output '--- git blame HighestEffort ---'
    git blame -L 23,26 -- src/McpServer.Services/Services/GrokCliAgentExecutionStrategy.cs
    Write-Output '--- git diff --stat since receipt ---'
    git status --porcelain
} finally {
    Pop-Location
}

Write-Output '=== GROK CLI HELP ==='
$grok = Get-Command grok -ErrorAction SilentlyContinue
if ($grok) {
    Write-Output ("GrokPath=" + $grok.Source)
    $help = & grok --help 2>&1 | Out-String
    $helpLines = $help -split "`r?`n" | Where-Object { $_ -match 'effort|reasoning' }
    if ($helpLines) { $helpLines | ForEach-Object { Write-Output ("HELP " + $_) } } else { Write-Output 'HELP no effort/reasoning lines' }
} else {
    Write-Output 'GrokCommand=MISSING'
}

Write-Output '=== USER GROK CONFIG ==='
$cfg = Join-Path $env:USERPROFILE '.grok\config.toml'
if (Test-Path -LiteralPath $cfg) {
    $cfgItem = Get-Item -LiteralPath $cfg
    Write-Output ("GrokConfigLwUtc=" + $cfgItem.LastWriteTimeUtc.ToString('o'))
    Select-String -LiteralPath $cfg -Pattern 'effort|model' | ForEach-Object { Write-Output ("CFG " + $_.Line.Trim()) }
} else {
    Write-Output 'GrokConfig=MISSING'
}

Write-Output 'DONE'
