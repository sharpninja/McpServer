#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

. 'F:\GitHub\McpServer\plugins\core\lib-ps\yaml-object-mutation.ps1'
. 'F:\GitHub\mcpserver-grok-plugin\lib\marker-resolver.ps1'

$utc = [datetime]::UtcNow
Write-Output ('UTC=' + $utc.ToString('yyyy-MM-ddTHH:mm:ssZ'))
Write-Output ('PSVersion=' + $PSVersionTable.PSVersion.ToString())
Write-Output ('ProcessName=' + (Get-Process -Id $PID).ProcessName)

Write-Output '=== PYTHON_PROBE ==='
foreach ($cmd in @('python', 'python3', 'py')) {
    $found = Get-Command $cmd -ErrorAction SilentlyContinue
    if ($found) {
        Write-Output ('CMD_PRESENT {0}={1}' -f $cmd, $found.Source)
    } else {
        Write-Output ('CMD_ABSENT {0}' -f $cmd)
    }
}

Write-Output '=== SERVICE_CIM ==='
$svc = Get-CimInstance -ClassName Win32_Service -Filter "Name = 'McpServer'"
if ($null -eq $svc) {
    Write-Output 'SERVICE=MISSING'
} else {
    Write-Output ('Name=' + $svc.Name)
    Write-Output ('State=' + $svc.State)
    Write-Output ('Status=' + $svc.Status)
    Write-Output ('StartMode=' + $svc.StartMode)
    Write-Output ('StartName=' + $svc.StartName)
    Write-Output ('PathName=' + $svc.PathName)
    Write-Output ('ProcessId=' + $svc.ProcessId)
    Write-Output ('ExitCode=' + $svc.ExitCode)
}

Write-Output '=== GET_SERVICE ==='
$gs = Get-Service -Name McpServer
Write-Output ('GetService.Status=' + $gs.Status)
Write-Output ('GetService.StartType=' + $gs.StartType)
Write-Output ('GetService.ServiceType=' + $gs.ServiceType)

Write-Output '=== PROCESS ==='
Get-CimInstance Win32_Process -Filter "Name = 'McpServer.Support.Mcp.exe'" | ForEach-Object {
    $dt = $null
    $cd = $_.CreationDate
    if ($cd -is [datetime]) {
        $dt = $cd.ToUniversalTime().ToString('o')
    } elseif ($cd) {
        try { $dt = ([datetime]$cd).ToUniversalTime().ToString('o') } catch { $dt = [string]$cd }
    }
    Write-Output ('ProcId={0} CreationDateUtc={1}' -f $_.ProcessId, $dt)
    if ($_.ExecutablePath) { Write-Output ('ExecutablePath=' + $_.ExecutablePath) }
    if ($_.CommandLine) { Write-Output ('CommandLine=' + $_.CommandLine) }
}
$gp = Get-Process -Name 'McpServer.Support.Mcp' -ErrorAction SilentlyContinue
if ($gp) {
    foreach ($item in @($gp)) {
        Write-Output ('GetProcess.Id=' + $item.Id)
        $st = $null
        try { $st = $item.StartTime } catch { $st = $null }
        if ($st) {
            Write-Output ('GetProcess.StartTimeUtc=' + $st.ToUniversalTime().ToString('o'))
        } else {
            Write-Output 'GetProcess.StartTimeUtc=UNAVAILABLE'
        }
    }
} else {
    Write-Output 'GetProcess=NONE'
}

$exe = 'C:\ProgramData\McpServer\McpServer.Support.Mcp.exe'
if (Test-Path -LiteralPath $exe) {
    $ei = Get-Item -LiteralPath $exe
    $vi = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($exe)
    $hash = Get-FileHash -LiteralPath $exe -Algorithm SHA256
    Write-Output ('ExeExists=True Length={0} LastWriteTimeUtc={1} CreationTimeUtc={2}' -f $ei.Length, $ei.LastWriteTimeUtc.ToString('o'), $ei.CreationTimeUtc.ToString('o'))
    Write-Output ('ExeFileVersion={0} ProductVersion={1} SHA256={2}' -f $vi.FileVersion, $vi.ProductVersion, $hash.Hash)
} else {
    Write-Output 'ExeExists=False'
}

Write-Output '=== PROGRAMDATA_BINARIES ==='
Get-ChildItem -LiteralPath 'C:\ProgramData\McpServer' -File | Sort-Object LastWriteTimeUtc | ForEach-Object {
    Write-Output ('BIN {0} lw={1} len={2}' -f $_.Name, $_.LastWriteTimeUtc.ToString('o'), $_.Length)
}

Write-Output '=== MARKER ==='
$markerPath = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml'
$markerItem = Get-Item -LiteralPath $markerPath
Write-Output ('MarkerLastWriteTimeUtc=' + $markerItem.LastWriteTimeUtc.ToString('o'))
Write-Output ('MarkerLength=' + $markerItem.Length)
$markerText = Get-Content -LiteralPath $markerPath -Raw
$pidMatch = [regex]::Match($markerText, '(?m)^pid:\s*(\d+)\s*$')
$keyMatch = [regex]::Match($markerText, '(?m)^apiKey:\s*(\S+)\s*$')
$startedMatch = [regex]::Match($markerText, '(?m)^startedAt:\s*(\S+)\s*$')
$serverStartedMatch = [regex]::Match($markerText, '(?m)^serverStartedAtUtc:\s*(\S+)\s*$')
$writtenMatch = [regex]::Match($markerText, '(?m)^markerWrittenAtUtc:\s*(\S+)\s*$')
$sigMatch = [regex]::Match($markerText, '(?m)^\s+value:\s*([0-9A-Fa-f]+)\s*$')
$versionMatch = [regex]::Match($markerText, 'MCP Server version:\s*(\S+)')
Write-Output ('MarkerPid=' + $pidMatch.Groups[1].Value)
Write-Output ('MarkerStartedAt=' + $startedMatch.Groups[1].Value)
Write-Output ('MarkerServerStartedAtUtc=' + $serverStartedMatch.Groups[1].Value)
Write-Output ('MarkerWrittenAtUtc=' + $writtenMatch.Groups[1].Value)
Write-Output ('MarkerSignatureValue=' + $sigMatch.Groups[1].Value)
Write-Output ('MarkerVersion=' + $versionMatch.Groups[1].Value)
$apiKey = $keyMatch.Groups[1].Value
$keyBytes = [System.Text.Encoding]::UTF8.GetBytes($apiKey)
$keyHash = [System.BitConverter]::ToString([System.Security.Cryptography.SHA256]::HashData($keyBytes)).Replace('-', '')
Write-Output ('ApiKeyLength=' + $apiKey.Length)
Write-Output ('ApiKeySha256=' + $keyHash)
Write-Output ('ApiKeyPrefix4=' + $apiKey.Substring(0, [Math]::Min(4, $apiKey.Length)))
Write-Output ('ApiKeySuffix4=' + $apiKey.Substring([Math]::Max(0, $apiKey.Length - 4)))
$sigOk = Test-MarkerSignature -MarkerFile $markerPath
Write-Output ('Test-MarkerSignature=' + $sigOk)
Write-Output ('PidMatchService=' + ($pidMatch.Groups[1].Value -eq [string]$svc.ProcessId))

Write-Output '=== PRIOR_INDEPENDENT_BASELINE ==='
Write-Output 'PriorHostileReceipt=docs/receipts/hostile-validator-20260817T233618Z.md'
Write-Output 'PriorProcessId=5572'
Write-Output 'PriorStartMode=Auto'
Write-Output 'PriorStartName=LocalSystem'
Write-Output 'PriorLiveYamlSha256=B42E2462D67EADE136EC3BF64A1224BF1253ADB73EA6596CFED1BC7C7A4E3D46'
Write-Output 'PriorLiveYamlLastWrite=2026-08-17T23:30:09.0404870Z'
Write-Output 'PriorLiveYamlLength=58975'

Write-Output '=== HEALTH_NONCE ==='
$nonce = [guid]::NewGuid().ToString('N')
try {
    $health = Invoke-WebRequest -Uri ('http://127.0.0.1:7147/health?nonce=' + $nonce) -UseBasicParsing -TimeoutSec 20
    Write-Output ('HealthStatus=' + [int]$health.StatusCode)
    Write-Output ('HealthNonceSent=' + $nonce)
    Write-Output ('HealthBody=' + $health.Content)
    $healthObj = $health.Content | ConvertFrom-Json
    Write-Output ('HealthNonceEcho=' + $healthObj.nonce)
    Write-Output ('HealthNonceMatch=' + ($healthObj.nonce -eq $nonce))
    $healthObj.PSObject.Properties | ForEach-Object {
        Write-Output ('HealthField.{0}={1}' -f $_.Name, $_.Value)
    }
} catch {
    Write-Output ('HealthError=' + $_.Exception.Message)
}

Write-Output '=== READY ==='
try {
    $ready = Invoke-WebRequest -Uri 'http://127.0.0.1:7147/ready' -UseBasicParsing -TimeoutSec 20
    Write-Output ('ReadyStatus=' + [int]$ready.StatusCode)
    Write-Output ('ReadyBody=' + $ready.Content)
} catch {
    Write-Output ('ReadyError=' + $_.Exception.Message)
}

Write-Output '=== LIVE_AGENTHELP ==='
$liveYaml = 'C:\ProgramData\McpServer\appsettings.yaml'
$item = Get-Item -LiteralPath $liveYaml
$hash = Get-FileHash -LiteralPath $liveYaml -Algorithm SHA256
Write-Output ('LiveYamlLastWriteTimeUtc=' + $item.LastWriteTimeUtc.ToString('o'))
Write-Output ('LiveYamlLength=' + $item.Length)
Write-Output ('LiveYamlSha256=' + $hash.Hash)
$doc = Read-McpYamlObject -Path $liveYaml
if ($doc.Contains('AgentHelp') -and $doc['AgentHelp'] -is [System.Collections.IDictionary]) {
    $ah = $doc['AgentHelp']
    Write-Output ('AgentHelpKeys=' + (@($ah.Keys) -join ','))
    foreach ($k in @($ah.Keys)) {
        $v = $ah[$k]
        if ($v -is [bool]) {
            Write-Output ('AgentHelp.{0}={1} TYPE=bool' -f $k, $v)
        } else {
            Write-Output ('AgentHelp.{0}={1} TYPE={2}' -f $k, $v, $v.GetType().Name)
        }
    }
} else {
    Write-Output 'AgentHelp=MISSING'
}

Write-Output '=== EVENTLOG_SERVICE ==='
try {
    $events = Get-WinEvent -FilterHashtable @{
        LogName = 'System'
        ProviderName = 'Service Control Manager'
        StartTime = (Get-Date).AddHours(-6)
    } -ErrorAction Stop | Where-Object {
        $_.Message -match 'McpServer'
    } | Select-Object -First 20
    if (-not $events) {
        Write-Output 'EVENTLOG_HITS=0'
    } else {
        foreach ($ev in $events) {
            Write-Output ('EV Id={0} TimeUtc={1} Level={2}' -f $ev.Id, $ev.TimeCreated.ToUniversalTime().ToString('o'), $ev.LevelDisplayName)
            $msg = ($ev.Message -replace '\s+', ' ').Trim()
            if ($msg.Length -gt 400) { $msg = $msg.Substring(0, 400) }
            Write-Output ('EV Msg=' + $msg)
        }
    }
} catch {
    Write-Output ('EVENTLOG_ERROR=' + $_.Exception.Message)
}

Write-Output '=== SERVER_LOG_STARTUP ==='
$logPath = 'C:\ProgramData\McpServer\logs\mcp-20260817.log'
if (Test-Path -LiteralPath $logPath) {
    $li = Get-Item -LiteralPath $logPath
    Write-Output ('LogLength=' + $li.Length)
    Write-Output ('LogLastWriteUtc=' + $li.LastWriteTimeUtc.ToString('o'))
    $fs = [System.IO.File]::Open($logPath, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
    try {
        $take = [Math]::Min([int64]400000, $fs.Length)
        [void]$fs.Seek(-$take, [System.IO.SeekOrigin]::End)
        $reader = [System.IO.StreamReader]::new($fs)
        $text = $reader.ReadToEnd()
    } finally {
        $fs.Dispose()
    }
    $lines = $text -split "`r?`n"
    Write-Output ('TailChars=' + $text.Length)
    Write-Output ('TailLines=' + $lines.Count)
    $needles = @('unreachable', 'storage', 'backend_unavailable', 'Ready', 'listening', 'Kestrel', 'started', 'Application started', 'now listening', 'SQLite')
    $hits = $lines | Where-Object {
        if ($_ -notmatch '23:3[6-9]|23:4[0-5]') { return $false }
        foreach ($n in $needles) { if ($_ -match $n) { return $true } }
        return $false
    } | Select-Object -Last 80
    Write-Output '--- log hits 23:36-23:45 ---'
    foreach ($h in $hits) { Write-Output $h }
} else {
    Write-Output 'LOG_MISSING'
}

Write-Output '=== RESTART_SCRIPT ==='
$rs = 'F:\GitHub\McpServer\docs\receipts\_restart-mcpserver-20260817T233717Z.ps1'
$rst = Get-Content -LiteralPath $rs -Raw
Write-Output ('RestartScriptLw=' + (Get-Item -LiteralPath $rs).LastWriteTimeUtc.ToString('o'))
Write-Output ('RestartServiceCount=' + ([regex]::Matches($rst, 'Restart-Service')).Count)
Write-Output ('StopServiceCount=' + ([regex]::Matches($rst, 'Stop-Service')).Count)
Write-Output ('StartServiceCount=' + ([regex]::Matches($rst, 'Start-Service')).Count)
Write-Output ('CopyItemCount=' + ([regex]::Matches($rst, 'Copy-Item')).Count)

Write-Output '=== GIT_PRODUCT ==='
Push-Location 'F:\GitHub\McpServer'
try {
    Write-Output '--- porcelain targeted ---'
    git status --porcelain -- appsettings.yaml src/McpServer.Support.Mcp/appsettings.yaml src/McpServer.Services/Options/AgentHelpOptions.cs src/McpServer.Services/Services/GrokCliAgentExecutionStrategy.cs docs/Project/TODO.yaml
    Write-Output '--- porcelain src tests ---'
    git status --porcelain -- src tests appsettings.yaml Directory.Packages.props Directory.Build.props
} finally {
    Pop-Location
}

Write-Output 'VERIFY_DONE'
