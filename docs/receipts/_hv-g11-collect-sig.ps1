# Marker HMAC-SHA256 verify using marker-v1 fields from AGENTS-README-FIRST.yaml.
$ErrorActionPreference = 'Stop'
$path = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml'
$raw = Get-Content -LiteralPath $path -Raw
# Extract scalar fields with simple regex (YAML scalars, no nested).
function Get-Scalar([string]$text, [string]$key) {
    if ($text -match "(?m)^${key}:\s*(.+)$") { return $Matches[1].Trim() }
    throw "missing $key"
}
function Get-Endpoint([string]$text, [string]$key) {
    if ($text -match "(?m)^\s+${key}:\s*(.+)$") { return $Matches[1].Trim() }
    throw "missing endpoint $key"
}
$apiKey = Get-Scalar $raw 'apiKey'
$port = Get-Scalar $raw 'port'
$baseUrl = Get-Scalar $raw 'baseUrl'
$workspace = Get-Scalar $raw 'workspace'
$workspacePath = Get-Scalar $raw 'workspacePath'
$markerPid = Get-Scalar $raw 'pid'
$startedAt = Get-Scalar $raw 'startedAt'
$markerWrittenAtUtc = Get-Scalar $raw 'markerWrittenAtUtc'
$serverStartedAtUtc = Get-Scalar $raw 'serverStartedAtUtc'
$sigValue = if ($raw -match '(?m)^\s+value:\s*([0-9A-Fa-f]+)') { $Matches[1] } else { throw 'missing signature value' }
$policy = if ($raw -match '(?m)^\s+policy:\s*(.+)$') { $Matches[1].Trim() } else { throw 'missing policy' }
$digest = if ($raw -match '(?m)^\s+contract_digest:\s*(.+)$') { $Matches[1].Trim() } else { throw 'missing digest' }

$payload = @"
canonicalization=marker-v1
port=$port
baseUrl=$baseUrl
apiKey=$apiKey
workspace=$workspace
workspacePath=$workspacePath
pid=$markerPid
startedAt=$startedAt
markerWrittenAtUtc=$markerWrittenAtUtc
serverStartedAtUtc=$serverStartedAtUtc
endpoints.health=$(Get-Endpoint $raw 'health')
endpoints.swagger=$(Get-Endpoint $raw 'swagger')
endpoints.swaggerUi=$(Get-Endpoint $raw 'swaggerUi')
endpoints.mcpTransport=$(Get-Endpoint $raw 'mcpTransport')
endpoints.sessionLog=$(Get-Endpoint $raw 'sessionLog')
endpoints.sessionLogDialog=$(Get-Endpoint $raw 'sessionLogDialog')
endpoints.contextSearch=$(Get-Endpoint $raw 'contextSearch')
endpoints.contextPack=$(Get-Endpoint $raw 'contextPack')
endpoints.contextSources=$(Get-Endpoint $raw 'contextSources')
endpoints.todo=$(Get-Endpoint $raw 'todo')
endpoints.repo=$(Get-Endpoint $raw 'repo')
endpoints.desktop=$(Get-Endpoint $raw 'desktop')
endpoints.gitHub=$(Get-Endpoint $raw 'gitHub')
endpoints.tools=$(Get-Endpoint $raw 'tools')
endpoints.workspace=$(Get-Endpoint $raw 'workspace')
endpoints.serverStartupUtc=$(Get-Endpoint $raw 'serverStartupUtc')
endpoints.markerFileTimestamp=$(Get-Endpoint $raw 'markerFileTimestamp')
agentPlugins.policy=$policy
agentPlugins.contractDigest=$digest
"@
# payload must have trailing LF on final line. Here-string already has final newline.
$hmac = [System.Security.Cryptography.HMACSHA256]::new([System.Text.Encoding]::UTF8.GetBytes($apiKey))
$hash = $hmac.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($payload))
$hex = ([BitConverter]::ToString($hash) -replace '-', '').ToUpperInvariant()
$match = [string]::Equals($hex, $sigValue.ToUpperInvariant())
$out = [ordered]@{
    TimestampUtc = [datetime]::UtcNow.ToString('o')
    Computed = $hex
    MarkerValue = $sigValue.ToUpperInvariant()
    Match = $match
    PayloadLength = $payload.Length
}
($out | ConvertTo-Json) | Set-Content -LiteralPath 'F:\GitHub\McpServer\docs\receipts\_hv-g11-out\marker-sig.json' -Encoding utf8
Write-Output ($out | ConvertTo-Json)
Write-Output ('UTCNOW=' + [datetime]::UtcNow.ToString('yyyyMMddTHHmmssZ'))
