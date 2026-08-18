#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$marker = Join-Path $workspace 'AGENTS-README-FIRST.yaml'
$failsafe = Join-Path $workspace '.mcpServer\claude\failsafe\20260818T001252Z-session_submit-f830.yaml'
$baseUrl = 'http://PAYTON-LEGION2:7147'
$nonce = [guid]::NewGuid().ToString('N')

. 'F:\GitHub\mcpserver-grok-plugin\lib\marker-resolver.ps1'

$utc = [datetime]::UtcNow
Write-Output ('UTC_NOW=' + $utc.ToString('o'))
Write-Output ('UTC_COMPACT=' + $utc.ToString('yyyyMMddTHHmmssZ'))
Write-Output ('TZ_ID=' + [TimeZoneInfo]::Local.Id)
Write-Output ('TZ_DISPLAY=' + [TimeZoneInfo]::Local.DisplayName)
Write-Output ('TZ_BASE_UTC_OFFSET=' + [TimeZoneInfo]::Local.BaseUtcOffset.ToString())
Write-Output ('TZ_IS_DST=' + [TimeZoneInfo]::Local.IsDaylightSavingTime([datetime]::Now))
Write-Output ('TZ_NOW_OFFSET=' + [TimeZoneInfo]::Local.GetUtcOffset([datetime]::Now).ToString())

$svc = Get-Service -Name 'McpServer'
Write-Output ('GET_SERVICE_STATUS=' + $svc.Status)
Write-Output ('GET_SERVICE_STARTTYPE=' + $svc.StartType)

$wmi = Get-CimInstance -ClassName Win32_Service -Filter "Name='McpServer'"
Write-Output ('WMI_STATE=' + $wmi.State)
Write-Output ('WMI_PID=' + $wmi.ProcessId)
Write-Output ('WMI_STARTMODE=' + $wmi.StartMode)
Write-Output ('WMI_STARTNAME=' + $wmi.StartName)
Write-Output ('WMI_PATHNAME=' + $wmi.PathName)
Write-Output ('WMI_EXITCODE=' + $wmi.ExitCode)

$proc = Get-CimInstance -ClassName Win32_Process -Filter ("ProcessId=" + $wmi.ProcessId)
$procUtc = $proc.CreationDate.ToUniversalTime()
Write-Output ('PROC_CREATION_UTC=' + $procUtc.ToString('o'))
Write-Output ('PROC_NAME=' + $proc.Name)

$markerItem = Get-Item -LiteralPath $marker
Write-Output ('MARKER_LASTWRITE_UTC=' + $markerItem.LastWriteTimeUtc.ToString('o'))
$sigOk = Test-MarkerSignature -MarkerFile $marker
Write-Output ('TEST_MARKER_SIGNATURE=' + $sigOk)
$markerPid = Get-MarkerField -MarkerFile $marker -FieldName 'pid'
$markerStarted = Get-MarkerField -MarkerFile $marker -FieldName 'startedAt'
$markerServerStarted = Get-MarkerField -MarkerFile $marker -FieldName 'serverStartedAtUtc'
Write-Output ('MARKER_PID=' + $markerPid)
Write-Output ('MARKER_STARTEDAT=' + $markerStarted)
Write-Output ('MARKER_SERVERSTARTED=' + $markerServerStarted)
Write-Output ('PID_MATCH=' + ($markerPid -eq [string]$wmi.ProcessId))

$healthUri = $baseUrl + '/health?nonce=' + $nonce
$health = Invoke-WebRequest -Uri $healthUri -UseBasicParsing
Write-Output ('HEALTH_STATUS=' + [int]$health.StatusCode)
Write-Output ('HEALTH_BODY=' + $health.Content)
Write-Output ('HEALTH_NONCE=' + $nonce)
Write-Output ('HEALTH_NONCE_ECHOED=' + $health.Content.Contains($nonce))

$ready = Invoke-WebRequest -Uri ($baseUrl + '/ready') -UseBasicParsing
Write-Output ('READY_STATUS=' + [int]$ready.StatusCode)
Write-Output ('READY_BODY=' + $ready.Content)

$fs = Get-Item -LiteralPath $failsafe
Write-Output ('FAILSAFE_PATH=' + $fs.FullName)
Write-Output ('FAILSAFE_LENGTH=' + $fs.Length)
Write-Output ('FAILSAFE_LASTWRITE_UTC=' + $fs.LastWriteTimeUtc.ToString('o'))
Write-Output ('FAILSAFE_LASTWRITE_LOCAL=' + $fs.LastWriteTime.ToString('o'))

Import-Module powershell-yaml -ErrorAction Stop
$fsObj = Get-Content -LiteralPath $failsafe -Raw | ConvertFrom-Yaml
Write-Output ('FS_METHOD=' + $fsObj.method)
Write-Output ('FS_LABEL=' + $fsObj.label)
Write-Output ('FS_TIMESTAMP=' + $fsObj.timestamp)
Write-Output ('FS_DRAIN_ATTEMPTS=' + $fsObj.drainAttempts)
Write-Output ('FS_LAST_DRAIN_ERROR_TYPE=' + $fsObj.lastDrainError.GetType().FullName)
$errText = [string]$fsObj.lastDrainError
Write-Output ('FS_LAST_DRAIN_ERROR=<<<')
Write-Output $errText
Write-Output ('>>>')
Write-Output ('FS_ERR_CONTAINS_INTERNAL_SERVER_ERROR=' + $errText.Contains('internal_server_error'))
Write-Output ('FS_ERR_CONTAINS_BACKEND_UNAVAILABLE=' + $errText.ToLowerInvariant().Contains('backend_unavailable'))
Write-Output ('FS_ERR_CONTAINS_MESSAGE_INTERNAL=' + ($errText -match '(?m)^\s+message:\s+internal_server_error\s*$'))

$sessionLog = $fsObj.params.sessionLog
$turns = @($sessionLog.turns)
Write-Output ('FS_TURN_COUNT=' + $turns.Count)
$turn = $turns[0]
Write-Output ('FS_TURN_REQUESTID=' + $turn.requestId)
Write-Output ('FS_TURN_STATUS=' + $turn.status)
$turnKeys = @($turn.Keys)
if (-not $turnKeys) {
    $turnKeys = @($turn.PSObject.Properties.Name)
}
Write-Output ('FS_TURN_KEYS=' + ($turnKeys -join ','))
Write-Output ('FS_TURN_HAS_PLANFILE=' + ($turnKeys -contains 'planFile'))
Write-Output ('FS_TURN_HAS_TODOID=' + ($turnKeys -contains 'todoId'))
if ($turnKeys -contains 'planFile') { Write-Output ('FS_TURN_PLANFILE=' + $turn.planFile) }
if ($turnKeys -contains 'todoId') { Write-Output ('FS_TURN_TODOID=' + $turn.todoId) }

$failsafeDir = Join-Path $workspace '.mcpServer\claude\failsafe'
Write-Output '---- FAILSAFE_DIR ----'
Get-ChildItem -LiteralPath $failsafeDir -File | ForEach-Object {
    Write-Output ($_.Name + ' len=' + $_.Length + ' utc=' + $_.LastWriteTimeUtc.ToString('o'))
}
$cb97 = Get-ChildItem -LiteralPath $failsafeDir -Recurse -File -Filter '*cb97*'
Write-Output ('CB97_COUNT=' + @($cb97).Count)
$cb97 | ForEach-Object { Write-Output ('CB97=' + $_.FullName) }

$exe = Get-Item -LiteralPath 'C:\ProgramData\McpServer\McpServer.Support.Mcp.exe'
$yaml = Get-Item -LiteralPath 'C:\ProgramData\McpServer\appsettings.yaml'
$exeHash = (Get-FileHash -LiteralPath $exe.FullName -Algorithm SHA256).Hash
$yamlHash = (Get-FileHash -LiteralPath $yaml.FullName -Algorithm SHA256).Hash
Write-Output ('EXE_LASTWRITE_UTC=' + $exe.LastWriteTimeUtc.ToString('o'))
Write-Output ('EXE_SHA256=' + $exeHash)
Write-Output ('EXE_FILEVER=' + $exe.VersionInfo.FileVersion)
Write-Output ('EXE_PRODVER=' + $exe.VersionInfo.ProductVersion)
Write-Output ('YAML_LASTWRITE_UTC=' + $yaml.LastWriteTimeUtc.ToString('o'))
Write-Output ('YAML_SHA256=' + $yamlHash)
Write-Output ('YAML_LENGTH=' + $yaml.Length)

$cutoff = [datetime]::Parse('2026-08-18T00:31:00Z').ToUniversalTime()
Write-Output ('RECENT_SRC_CUTOFF_UTC=' + $cutoff.ToString('o'))
$recentSrc = Get-ChildItem -LiteralPath (Join-Path $workspace 'src') -Recurse -File |
    Where-Object { $_.LastWriteTimeUtc -ge $cutoff }
$recentTests = Get-ChildItem -LiteralPath (Join-Path $workspace 'tests') -Recurse -File |
    Where-Object { $_.LastWriteTimeUtc -ge $cutoff }
Write-Output ('RECENT_SRC_COUNT=' + @($recentSrc).Count)
$recentSrc | Select-Object -First 30 | ForEach-Object {
    Write-Output ('RECENT_SRC=' + $_.FullName.Substring($workspace.Length + 1) + ' utc=' + $_.LastWriteTimeUtc.ToString('o'))
}
Write-Output ('RECENT_TESTS_COUNT=' + @($recentTests).Count)
$recentTests | Select-Object -First 30 | ForEach-Object {
    Write-Output ('RECENT_TEST=' + $_.FullName.Substring($workspace.Length + 1) + ' utc=' + $_.LastWriteTimeUtc.ToString('o'))
}

$sessionLogService = Get-Item -LiteralPath (Join-Path $workspace 'src\McpServer.Services\Session\SessionLogService.cs') -ErrorAction SilentlyContinue
if (-not $sessionLogService) {
    $hits = Get-ChildItem -LiteralPath (Join-Path $workspace 'src') -Recurse -Filter 'SessionLogService.cs'
    $hits | ForEach-Object {
        Write-Output ('SESSIONLOGSERVICE=' + $_.FullName + ' utc=' + $_.LastWriteTimeUtc.ToString('o'))
    }
} else {
    Write-Output ('SESSIONLOGSERVICE=' + $sessionLogService.FullName + ' utc=' + $sessionLogService.LastWriteTimeUtc.ToString('o'))
}

Push-Location $workspace
try {
    Write-Output '---- GIT_STATUS_SRC_TESTS ----'
    $st = git status --porcelain -- src tests
    $stLines = @($st)
    Write-Output ('GIT_DIRTY_SRC_TESTS_COUNT=' + $stLines.Count)
    Write-Output '---- GIT_LOG_SINCE_0031 ----'
    git --no-pager log --since='2026-08-18 00:31:00 +0000' --pretty=format:'%H %cI %s' -- src tests
    Write-Output ''
    Write-Output '---- GIT_LOG_HEAD ----'
    git --no-pager log -3 --pretty=format:'%H %cI %s'
    Write-Output ''
    Write-Output '---- GIT_STATUS_RECEIPTS ----'
    git status --porcelain -- docs/receipts/sessionlog-backend-unavailable-20260818T003530Z.md
} finally {
    Pop-Location
}

Write-Output '---- LOG_DIR ----'
Get-ChildItem -LiteralPath 'C:\ProgramData\McpServer\logs' -File | Sort-Object LastWriteTimeUtc | ForEach-Object {
    Write-Output ($_.Name + ' len=' + $_.Length + ' utc=' + $_.LastWriteTimeUtc.ToString('o') + ' local=' + $_.LastWriteTime.ToString('o'))
}
