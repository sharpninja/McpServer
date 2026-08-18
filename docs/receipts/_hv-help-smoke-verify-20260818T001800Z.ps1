#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. 'F:\GitHub\mcpserver-grok-plugin\lib\marker-resolver.ps1'

$workspace = 'F:\GitHub\McpServer'
$marker = Join-Path $workspace 'AGENTS-README-FIRST.yaml'
$baseUrl = 'http://PAYTON-LEGION2:7147'
$helpSessionId = 'help-20260818001213-0aa9f6de59d2403296130363aa94bb75'
$stamp = [datetime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
Write-Output ('UTC_STAMP=' + $stamp)
Write-Output ('HOST=' + [System.Net.Dns]::GetHostName())

Write-Output '=== SERVICE ==='
$svc = Get-Service -Name McpServer
Write-Output ('GetServiceStatus=' + $svc.Status)
Write-Output ('GetServiceStartType=' + $svc.StartType)
$cim = Get-CimInstance -ClassName Win32_Service -Filter "Name='McpServer'"
Write-Output ('Win32State=' + $cim.State)
Write-Output ('Win32ProcessId=' + $cim.ProcessId)
Write-Output ('Win32StartMode=' + $cim.StartMode)
Write-Output ('Win32StartName=' + $cim.StartName)
Write-Output ('Win32PathName=' + $cim.PathName)
Write-Output ('Win32ExitCode=' + $cim.ExitCode)
$proc = Get-CimInstance -ClassName Win32_Process -Filter ("ProcessId=" + $cim.ProcessId)
if ($proc) {
    $created = $proc.CreationDate
    if ($created) {
        Write-Output ('ProcessCreationDateUtc=' + ([datetime]$created).ToUniversalTime().ToString('o'))
    }
}

Write-Output '=== MARKER ==='
$markerItem = Get-Item -LiteralPath $marker
Write-Output ('MarkerLastWriteTimeUtc=' + $markerItem.LastWriteTimeUtc.ToString('o'))
$sigOk = Test-MarkerSignature -MarkerFile $marker
Write-Output ('Test-MarkerSignature=' + $sigOk)
$markerYaml = Get-Content -LiteralPath $marker -Raw
if ($markerYaml -match '(?m)^pid:\s*(\d+)') { Write-Output ('MarkerPid=' + $Matches[1]) }
if ($markerYaml -match '(?m)^apiKey:\s*(\S+)') {
    $key = $Matches[1]
    $sha = [System.BitConverter]::ToString([System.Security.Cryptography.SHA256]::HashData([System.Text.Encoding]::UTF8.GetBytes($key))).Replace('-', '')
    Write-Output ('ApiKeyPrefix4=' + $key.Substring(0, 4))
    Write-Output ('ApiKeySuffix4=' + $key.Substring($key.Length - 4))
    Write-Output ('ApiKeySha256=' + $sha)
}

Write-Output '=== HEALTH ==='
$nonce = [guid]::NewGuid().ToString('N')
$health = Invoke-WebRequest -Uri ($baseUrl + '/health?nonce=' + $nonce) -UseBasicParsing -TimeoutSec 15
Write-Output ('HealthStatus=' + [int]$health.StatusCode)
$healthJson = $health.Content | ConvertFrom-Json
Write-Output ('HealthNonceSent=' + $nonce)
Write-Output ('HealthNonceEcho=' + $healthJson.nonce)
Write-Output ('HealthNonceMatch=' + ($healthJson.nonce -eq $nonce))
Write-Output ('HealthStorage=' + $healthJson.storage)
Write-Output ('HealthBody=' + $health.Content)

Write-Output '=== READY ==='
try {
    $ready = Invoke-WebRequest -Uri ($baseUrl + '/ready') -UseBasicParsing -TimeoutSec 15
    Write-Output ('ReadyStatus=' + [int]$ready.StatusCode)
    Write-Output ('ReadyBody=' + $ready.Content)
} catch {
    Write-Output ('ReadyError=' + $_.Exception.Message)
}

Write-Output '=== GIT_PRODUCT ==='
Push-Location $workspace
try {
    Write-Output '--- porcelain ---'
    git status --porcelain -- src tests plugins
    Write-Output '--- src/tests dirty count ---'
    $dirty = @(git status --porcelain -- src tests)
    Write-Output ('DirtySrcTestsCount=' + $dirty.Count)
    Write-Output '--- recent src writes after 2026-08-18 00:11Z ---'
    $cutoff = [datetime]::Parse('2026-08-18T00:11:00Z').ToUniversalTime()
    $recent = Get-ChildItem -Path (Join-Path $workspace 'src'), (Join-Path $workspace 'tests') -Recurse -File -ErrorAction SilentlyContinue |
        Where-Object { $_.LastWriteTimeUtc -ge $cutoff }
    Write-Output ('RecentSrcTestsCount=' + @($recent).Count)
    $recent | Select-Object -First 40 | ForEach-Object {
        Write-Output ($_.LastWriteTimeUtc.ToString('o') + ' ' + $_.FullName)
    }
} finally {
    Pop-Location
}

Write-Output '=== IMPLEMENTER_RECEIPT ==='
$rec = Get-Item -LiteralPath (Join-Path $workspace 'docs\receipts\agenthelp-live-smoke-20260818T001316Z.md')
Write-Output ('ReceiptExists=' + $rec.Exists)
Write-Output ('ReceiptUtc=' + $rec.LastWriteTimeUtc.ToString('o'))
Write-Output ('ReceiptLen=' + $rec.Length)

Write-Output '=== LOG_SCAN ==='
$logCandidates = @(
    'C:\ProgramData\McpServer\logs\mcp-20260818.log',
    'C:\ProgramData\McpServer\logs\mcp-20260817.log'
)
foreach ($path in $logCandidates) {
    if (-not (Test-Path -LiteralPath $path)) {
        Write-Output ('LogMissing=' + $path)
        continue
    }
    $item = Get-Item -LiteralPath $path
    Write-Output ('LogPath=' + $path)
    Write-Output ('LogLength=' + $item.Length)
    Write-Output ('LogUtc=' + $item.LastWriteTimeUtc.ToString('o'))
    $fs = [System.IO.File]::Open($path, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
    try {
        $take = [Math]::Min(400000, $fs.Length)
        if ($fs.Length -gt $take) { $fs.Seek(-$take, [System.IO.SeekOrigin]::End) | Out-Null }
        $reader = [System.IO.StreamReader]::new($fs, $true)
        $text = $reader.ReadToEnd()
    } finally {
        $fs.Dispose()
    }
    $needles = @(
        $helpSessionId,
        'help-20260818001213',
        'agent_help_submit_turn',
        'agent_help_create_session',
        'agent_help_get_status',
        'agent_help_get_transcript',
        'latencyMs',
        '55827',
        '--effort'
    )
    $lines = $text -split "`r?`n"
    $hits = $lines | Where-Object {
        foreach ($needle in $needles) {
            if ($_.Contains($needle)) { return $true }
        }
        return $false
    } | Select-Object -Last 40
    Write-Output ('HitCount=' + @($hits).Count)
    $hits | ForEach-Object { Write-Output $_ }
}

$script:McpSessionHeader = $null
$script:McpId = 0

function Invoke-McpRpc {
    param(
        [Parameter(Mandatory)][string]$Method,
        $Params = $null,
        [string]$Label = $Method
    )
    $script:McpId++
    $payload = [ordered]@{
        jsonrpc = '2.0'
        id = $script:McpId
        method = $Method
    }
    if ($null -ne $Params) { $payload['params'] = $Params }
    $json = $payload | ConvertTo-Json -Depth 30 -Compress
    Write-Output ('---- MCP {0} id={1} ----' -f $Label, $script:McpId)
    $req = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::Post, ($baseUrl + '/mcp-transport'))
    $req.Headers.Accept.Clear()
    [void]$req.Headers.Accept.Add([System.Net.Http.Headers.MediaTypeWithQualityHeaderValue]::new('application/json'))
    [void]$req.Headers.Accept.Add([System.Net.Http.Headers.MediaTypeWithQualityHeaderValue]::new('text/event-stream'))
    [void]$req.Headers.TryAddWithoutValidation('X-Workspace-Path', $workspace)
    if ($script:McpSessionHeader) {
        [void]$req.Headers.TryAddWithoutValidation('Mcp-Session-Id', $script:McpSessionHeader)
    }
    $req.Content = [System.Net.Http.StringContent]::new($json, [System.Text.Encoding]::UTF8, 'application/json')
    $client = [System.Net.Http.HttpClient]::new()
    $client.Timeout = [TimeSpan]::FromSeconds(180)
    try {
        $resp = $client.Send($req)
        $sid = $null
        if ($resp.Headers.TryGetValues('Mcp-Session-Id', [ref]$sid)) {
            $script:McpSessionHeader = @($sid)[0]
        }
        $body = $resp.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        Write-Output ('HTTP=' + [int]$resp.StatusCode)
        Write-Output ('Mcp-Session-Id=' + $script:McpSessionHeader)
        if ($body.Contains("`ndata:") -or $body.StartsWith('event:')) {
            $dataLines = @()
            foreach ($line in ($body -split "`n")) {
                $trim = $line.TrimEnd("`r")
                if ($trim.StartsWith('data:')) { $dataLines += $trim.Substring(5).Trim() }
            }
            $body = ($dataLines -join "`n")
        }
        Write-Output $body
        return $body
    } finally {
        $client.Dispose()
        $req.Dispose()
    }
}

function Invoke-McpTool {
    param([Parameter(Mandatory)][string]$Name, [Parameter(Mandatory)][hashtable]$Arguments)
    Invoke-McpRpc -Method 'tools/call' -Label $Name -Params @{ name = $Name; arguments = $Arguments }
}

[void](Invoke-McpRpc -Method 'initialize' -Params @{
    protocolVersion = '2025-03-26'
    capabilities = @{}
    clientInfo = @{ name = 'hostile-validator-help-smoke'; version = '1.0.0' }
})
[void](Invoke-McpRpc -Method 'notifications/initialized' -Params @{})

Write-Output '=== AGENT_HELP_GET_STATUS ==='
Invoke-McpTool -Name 'agent_help_get_status' -Arguments @{
    workspacePath = $workspace
    sessionId = $helpSessionId
}

Write-Output '=== AGENT_HELP_GET_TRANSCRIPT ==='
Invoke-McpTool -Name 'agent_help_get_transcript' -Arguments @{
    workspacePath = $workspace
    sessionId = $helpSessionId
}

Write-Output '=== AGENT_HELP_CREATE_SESSION independent no overrides ==='
Invoke-McpTool -Name 'agent_help_create_session' -Arguments @{
    workspacePath = $workspace
    topic = 'hostile-validate-help-smoke'
    callerAgent = 'GrokCode'
    callerSessionId = ('GrokCode-' + $stamp + '-hostile-help-smoke')
    callerRequestId = ('req-' + $stamp + '-001-hostile-validate-help-smoke')
    issueSummary = 'Hostile validator independent create-session with no executionStrategy or agentModel override. Observation: verifying live AgentHelp defaults after claimed live smoke. Inference: none.'
}

Write-Output ('FULL_BOOTSTRAP=' + ($sigOk -and ($healthJson.nonce -eq $nonce) -and ([int]$health.StatusCode -eq 200)))
Write-Output 'VERIFY_DONE'
