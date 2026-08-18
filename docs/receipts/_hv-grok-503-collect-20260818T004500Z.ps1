#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
Set-Location -LiteralPath $workspace
$stamp = [datetime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
Write-Output ('UTC_NOW=' + [datetime]::UtcNow.ToString('o'))
Write-Output ('STAMP=' + $stamp)
Write-Output ('MACHINE=' + $env:COMPUTERNAME)
Write-Output ('PS_EXE=' + (Get-Process -Id $PID).Path)

# --- service / marker (read-only) ---
$svc = Get-CimInstance -ClassName Win32_Service -Filter "Name='McpServer'"
Write-Output ('SVC_STATE=' + $svc.State)
Write-Output ('SVC_PID=' + $svc.ProcessId)
Write-Output ('SVC_STARTMODE=' + $svc.StartMode)
Write-Output ('SVC_STARTNAME=' + $svc.StartName)
Write-Output ('SVC_PATH=' + $svc.PathName)

$markerPath = Join-Path $workspace 'AGENTS-README-FIRST.yaml'
$markerInfo = Get-Item -LiteralPath $markerPath
Write-Output ('MARKER_LWT_UTC=' + $markerInfo.LastWriteTimeUtc.ToString('o'))
Write-Output ('MARKER_LEN=' + $markerInfo.Length)

. (Join-Path $workspace 'plugins\core\lib-ps\marker-resolver.ps1')
$sig = Test-MarkerSignature -MarkerFile $markerPath
Write-Output ('MARKER_SIG=' + $sig)
Write-Output ('MARKER_PID=' + (Get-MarkerField -MarkerFile $markerPath -FieldName 'pid'))
Write-Output ('PID_MATCH=' + ((Get-MarkerField -MarkerFile $markerPath -FieldName 'pid') -eq [string]$svc.ProcessId))

# --- live yaml provider ---
. (Join-Path $workspace 'plugins\core\lib-ps\yaml-object-mutation.ps1')
$appsettings = 'C:\ProgramData\McpServer\appsettings.yaml'
$yamlInfo = Get-Item -LiteralPath $appsettings
Write-Output ('YAML_LWT_UTC=' + $yamlInfo.LastWriteTimeUtc.ToString('o'))
Write-Output ('YAML_LEN=' + $yamlInfo.Length)
$hasher = [System.Security.Cryptography.SHA256]::Create()
$yamlHash = [BitConverter]::ToString($hasher.ComputeHash([System.IO.File]::ReadAllBytes($appsettings))).Replace('-', '')
Write-Output ('YAML_SHA256=' + $yamlHash)

$doc = Read-McpYamlObject -Path $appsettings
$mcp = $doc['Mcp']
Write-Output ('HAS_MCP=' + ($null -ne $mcp))
if ($mcp -is [System.Collections.IDictionary]) {
    $db = $mcp['Database']
    if ($db -is [System.Collections.IDictionary]) {
        Write-Output ('Mcp.Database.Provider=' + [string]$db['Provider'])
        foreach ($k in @($db.Keys)) {
            if ($k -match 'Password|ConnectionString|ApiKey') {
                $t = [string]$db[$k]
                Write-Output ('Mcp.Database.' + $k + '=<redacted len=' + $t.Length + '>')
            } else {
                Write-Output ('Mcp.Database.' + $k + '=' + [string]$db[$k])
            }
        }
    } else {
        Write-Output 'Mcp.Database=MISSING_OR_NOT_MAP'
    }
    $todo = $mcp['TodoStorage']
    if ($todo -is [System.Collections.IDictionary]) {
        Write-Output ('Mcp.TodoStorage.Provider=' + [string]$todo['Provider'])
    } elseif ($null -ne $todo) {
        Write-Output ('Mcp.TodoStorage=' + [string]$todo)
    } else {
        Write-Output 'Mcp.TodoStorage=MISSING'
    }
    if ($mcp.Contains('Instances')) {
        Write-Output 'Mcp.Instances=PRESENT'
        $inst = $mcp['Instances']
        if ($inst -is [System.Collections.IDictionary]) {
            foreach ($iname in @($inst.Keys)) {
                $imap = $inst[$iname]
                if ($imap -is [System.Collections.IDictionary] -and $imap.Contains('Database')) {
                    $idb = $imap['Database']
                    if ($idb -is [System.Collections.IDictionary]) {
                        Write-Output ('Mcp.Instances.' + $iname + '.Database.Provider=' + [string]$idb['Provider'])
                    }
                }
            }
        }
    } else {
        Write-Output 'Mcp.Instances=ABSENT'
    }
}

Get-ChildItem -LiteralPath 'C:\ProgramData\McpServer' -Filter 'appsettings*.yaml' | ForEach-Object {
    Write-Output ('EXTRA_YAML=' + $_.Name + ' LWT=' + $_.LastWriteTimeUtc.ToString('o') + ' LEN=' + $_.Length)
}

# --- health nonce (read-only) ---
$nonce = [guid]::NewGuid().ToString('N')
$healthUri = 'http://127.0.0.1:7147/health?nonce=' + $nonce
try {
    $health = Invoke-WebRequest -Uri $healthUri -UseBasicParsing -TimeoutSec 30
    Write-Output ('HEALTH_STATUS=' + [int]$health.StatusCode)
    $body = $health.Content
    Write-Output ('HEALTH_NONCE=' + $nonce)
    Write-Output ('HEALTH_NONCE_ECHO=' + $body.Contains($nonce))
    Write-Output ('HEALTH_BODY_LEN=' + $body.Length)
    if ($body.Length -gt 800) { Write-Output ('HEALTH_BODY=' + $body.Substring(0, 800)) } else { Write-Output ('HEALTH_BODY=' + $body) }
} catch {
    Write-Output ('HEALTH_ERROR=' + $_.Exception.Message)
}

# --- failsafe as object ---
$failsafePath = 'F:\GitHub\McpServer\.mcpServer\grok\failsafe\20260818T001239Z-session_submit-a650.yaml'
$fsInfo = Get-Item -LiteralPath $failsafePath
Write-Output ('FAILSAFE_LWT_UTC=' + $fsInfo.LastWriteTimeUtc.ToString('o'))
Write-Output ('FAILSAFE_LEN=' + $fsInfo.Length)
$fsDoc = Read-McpYamlObject -Path $failsafePath
Write-Output ('FS_METHOD=' + [string]$fsDoc['method'])
Write-Output ('FS_LABEL=' + [string]$fsDoc['label'])
Write-Output ('FS_TIMESTAMP=' + [string]$fsDoc['timestamp'])
$err = [string]$fsDoc['lastDrainError']
Write-Output ('FS_ERR_LEN=' + $err.Length)
Write-Output ('FS_ERR_HAS_503=' + $err.Contains('503'))
Write-Output ('FS_ERR_HAS_BACKEND=' + $err.ToLowerInvariant().Contains('backend_unavailable'))
Write-Output ('FS_ERR_HAS_INTERNAL=' + $err.Contains('internal_server_error'))
Write-Output ('FS_ERR_HAS_SUBMIT=' + $err.Contains('SubmitAsync'))
Write-Output ('FS_ERR_HAS_METHOD_INV=' + $err.Contains('method_invocation_error'))
$sessionLog = $fsDoc['params']['sessionLog']
Write-Output ('FS_SESSIONID=' + [string]$sessionLog['sessionId'])
Write-Output ('FS_SOURCETYPE=' + [string]$sessionLog['sourceType'])
$turns = @($sessionLog['turns'])
Write-Output ('FS_TURN_COUNT=' + $turns.Count)
$t0 = $turns[0]
Write-Output ('FS_T0_STATUS=' + [string]$t0['status'])
Write-Output ('FS_T0_REQ=' + [string]$t0['requestId'])
Write-Output ('FS_T0_HAS_PLANFILE_KEY=' + ($t0.Contains('planFile')))
Write-Output ('FS_T0_HAS_TODOID_KEY=' + ($t0.Contains('todoId')))
if ($t0.Contains('planFile')) { Write-Output ('FS_T0_PLANFILE=' + [string]$t0['planFile']) }
if ($t0.Contains('todoId')) { Write-Output ('FS_T0_TODOID=' + [string]$t0['todoId']) }
$rawFs = [System.IO.File]::ReadAllText($failsafePath)
Write-Output ('FS_RAW_HAS_503=' + $rawFs.Contains('503'))
Write-Output ('FS_RAW_HAS_BACKEND=' + $rawFs.ToLowerInvariant().Contains('backend_unavailable'))
Write-Output ('FS_RAW_HAS_PLANFILE=' + $rawFs.Contains('planFile'))
Write-Output ('FS_RAW_HAS_TODOID=' + $rawFs.Contains('todoId'))

# --- server log scan ---
$logPath = 'C:\ProgramData\McpServer\logs\mcp-20260817.log'
$logInfo = Get-Item -LiteralPath $logPath
Write-Output ('LOG_LWT_UTC=' + $logInfo.LastWriteTimeUtc.ToString('o'))
Write-Output ('LOG_LEN=' + $logInfo.Length)

$counts = [ordered]@{
    lines_1838_1842 = 0
    unreach_1838_1842 = 0
    backend_1838_1842 = 0
    status503_1838_1842 = 0
    mcp_transport_1838_1842 = 0
    lines_1850_1855 = 0
    unreach_1850_1855 = 0
    backend_1850_1855 = 0
    status503_1850_1855 = 0
    sql_1850_1855 = 0
    namedpipes_1850_1855 = 0
    accessdenied_1850_1855 = 0
    ensureboot_1850_1855 = 0
    unhealthy_1850_1855 = 0
    timeout5s_1850_1855 = 0
    trace_hits = 0
    grok_1850_1855 = 0
    replace_section_1850_1855 = 0
    post_mcp_transport_1850_1855 = 0
    completed_503_1850_1855 = 0
    completed_500_1850_1855 = 0
    completed_200_mcp_1850_1855 = 0
}

$interesting1852 = [System.Collections.Generic.List[string]]::new()
$traceLines = [System.Collections.Generic.List[string]]::new()
$health1852 = [System.Collections.Generic.List[string]]::new()
$completedMcp = [System.Collections.Generic.List[string]]::new()
$firstHealth = $null
$unhandled1852 = [System.Collections.Generic.List[string]]::new()

function Clip([string]$s, [int]$n = 700) {
    if ($s.Length -le $n) { return $s }
    return $s.Substring(0, $n)
}

$fs = [System.IO.File]::Open($logPath, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
try {
    $reader = [System.IO.StreamReader]::new($fs)
    while ($null -ne ($line = $reader.ReadLine())) {
        $inEarly = $line.StartsWith('2026-08-17 18:38:') -or $line.StartsWith('2026-08-17 18:39:') -or $line.StartsWith('2026-08-17 18:40:') -or $line.StartsWith('2026-08-17 18:41:') -or $line.StartsWith('2026-08-17 18:42:')
        $in1850 = $line.StartsWith('2026-08-17 18:50:') -or $line.StartsWith('2026-08-17 18:51:') -or $line.StartsWith('2026-08-17 18:52:') -or $line.StartsWith('2026-08-17 18:53:') -or $line.StartsWith('2026-08-17 18:54:') -or $line.StartsWith('2026-08-17 18:55:')

        if ($inEarly) {
            $counts.lines_1838_1842++
            if ($line.Contains('unreachable')) { $counts.unreach_1838_1842++ }
            if ($line.Contains('backend_unavailable')) { $counts.backend_1838_1842++ }
            if ($line.Contains(' 503 ') -or $line.Contains('status":503') -or $line.Contains('StatusCode: 503') -or $line.Contains('HTTP/1.1 503') -or $line.Contains('completed with 503')) { $counts.status503_1838_1842++ }
            if ($line.Contains('/mcp-transport')) { $counts.mcp_transport_1838_1842++ }
            if ($null -eq $firstHealth -and $line.Contains('GET /health') -and $line.Contains('completed')) {
                $firstHealth = Clip $line 900
            }
        }

        if ($line.Contains('00-aab0888980690d5c55a8af5c029f0bd1')) {
            $counts.trace_hits++
            $traceLines.Add((Clip $line 900))
        }

        if ($in1850) {
            $counts.lines_1850_1855++
            if ($line.Contains('unreachable')) { $counts.unreach_1850_1855++ }
            if ($line.Contains('backend_unavailable')) { $counts.backend_1850_1855++ }
            if ($line.Contains(' 503 ') -or $line.Contains('completed with 503') -or $line.Contains('StatusCode: 503') -or $line.Contains('HTTP/1.1 503')) { $counts.status503_1850_1855++ }
            if ($line.Contains('SqlException') -or $line.Contains('Microsoft.Data.SqlClient')) { $counts.sql_1850_1855++ }
            if ($line.Contains('Named Pipes') -or $line.Contains('named pipes') -or $line.Contains('error 40')) { $counts.namedpipes_1850_1855++ }
            if ($line.Contains('Access is denied')) { $counts.accessdenied_1850_1855++ }
            if ($line.Contains('EnsureBootstrappedAsync')) { $counts.ensureboot_1850_1855++ }
            if ($line.Contains('Unhealthy')) { $counts.unhealthy_1850_1855++ }
            if ($line.Contains('timed out after 5s') -or $line.Contains('timed out after 5 s')) { $counts.timeout5s_1850_1855++ }
            if ($line.Contains('Grok') -or $line.Contains('grok')) { $counts.grok_1850_1855++ }
            if ($line.Contains('sessionlog_replace_section')) { $counts.replace_section_1850_1855++ }
            if ($line.Contains('POST /mcp-transport')) {
                $counts.post_mcp_transport_1850_1855++
                if ($line.Contains('completed')) { $completedMcp.Add((Clip $line 900)) }
            }
            if ($line.Contains('completed with 503')) { $counts.completed_503_1850_1855++ }
            if ($line.Contains('completed with 500')) { $counts.completed_500_1850_1855++ }
            if ($line.Contains('POST /mcp-transport') -and $line.Contains('completed with 200')) { $counts.completed_200_mcp_1850_1855++ }

            $keep = $false
            if ($line.Contains('Health check') -or $line.Contains('storage') -or $line.Contains('SqlException') -or $line.Contains('Named Pipes') -or $line.Contains('Access is denied') -or $line.Contains('EnsureBootstrapped') -or $line.Contains('Unhealthy') -or $line.Contains('backend_unavailable') -or $line.Contains('aab08889') -or $line.Contains('Unhandled') -or $line.Contains('timed out after 5') -or $line.Contains('POST /mcp-transport')) {
                $keep = $true
            }
            if ($keep) { $interesting1852.Add((Clip $line 800)) }
            if ($line.Contains('GET /health') -or $line.Contains('Health check')) { $health1852.Add((Clip $line 800)) }
            if ($line.Contains('Unhandled')) { $unhandled1852.Add((Clip $line 900)) }
        }
    }
} finally {
    $fs.Dispose()
}

Write-Output '--- COUNTS ---'
foreach ($k in $counts.Keys) { Write-Output ($k + '=' + $counts[$k]) }
Write-Output '--- FIRST_HEALTH_EARLY ---'
Write-Output $firstHealth
Write-Output '--- TRACE_LINES ---'
Write-Output ('TRACE_LINE_COUNT=' + $traceLines.Count)
$traceLines | ForEach-Object { Write-Output $_ }
Write-Output '--- UNHANDLED_1852 ---'
$unhandled1852 | ForEach-Object { Write-Output $_ }
Write-Output '--- COMPLETED_MCP_1850_1855 ---'
$completedMcp | ForEach-Object { Write-Output $_ }
Write-Output '--- HEALTH_1852 ---'
$health1852 | ForEach-Object { Write-Output $_ }
Write-Output '--- INTERESTING_1852_COUNT ---'
Write-Output ('interesting1852=' + $interesting1852.Count)
$interesting1852 | Select-Object -First 80 | ForEach-Object { Write-Output $_ }
if ($interesting1852.Count -gt 80) {
    Write-Output '... truncated interesting ...'
    $interesting1852 | Select-Object -Last 20 | ForEach-Object { Write-Output $_ }
}

Write-Output '--- PY_PROCS ---'
$py = Get-Process -Name python, python3, py -ErrorAction SilentlyContinue
if ($py) { $py | ForEach-Object { Write-Output ('PY_RUNNING=' + $_.ProcessName + ' pid=' + $_.Id) } } else { Write-Output 'PY_RUNNING=none' }
