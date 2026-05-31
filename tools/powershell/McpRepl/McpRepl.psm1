Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

class McpRequest {
    [string]   $RequestId
    [string]   $Method
    [hashtable]$Params = @{}
    McpRequest([string]$id, [string]$method, [hashtable]$params = @{}) {
        $this.RequestId = $id; $this.Method = $method; $this.Params = $params
    }
}
class McpResult { [string]$RequestId; $Result; [hashtable]$Meta = @{} }
class McpError  { [string]$RequestId; [string]$Code; [string]$Message; $Details }
class McpEvent  { [string]$Event; $Data; [int]$Sequence }

function ConvertTo-McpYaml {
    param(
        [Parameter(Mandatory)]
        $InputObject
    )

    function ConvertTo-McpYamlValue {
        param($Value)
        if ($null -eq $Value) { return 'null' }
        if ($Value -is [bool]) { return $Value.ToString().ToLower() }
        if ($Value -is [int] -or $Value -is [long] -or $Value -is [double]) { return $Value.ToString() }
        $text = [string]$Value
        if ($text -match "[\r\n]") {
            # literal block scalar (the original requirement)
            $escaped = $text -replace "`r`n", "`n"
            $indented = ($escaped -split "`n" | ForEach-Object { "    $_" }) -join "`n"
            return "|`n$indented"
        }
        $needsQuotes = $text -match '[:#\-\[\]\{\}\?\&\*\!\|\>\"\%\@\`]' -or $text -match "'"
        if ($needsQuotes) {
            $q = $text -replace "'", "''"
            return "'$q'"
        }
        return $text
    }

    if ($InputObject -is [McpRequest]) {
        $r = $InputObject
        $yaml = "type: request`npayload:`n  requestId: $($r.RequestId)`n  method: $($r.Method)"
        if ($r.Params.Count -gt 0) {
            $pLines = foreach ($entry in $r.Params.GetEnumerator()) {
                $k = $entry.Key
                $v = ConvertTo-McpYamlValue $entry.Value
                if ($v -like "|*") { "  ${k}: $v" } else { "  ${k}: $v" }
            }
            $yaml += "`n  params:`n" + ($pLines -join "`n")
        }
        return $yaml
    }
    if ($InputObject -is [McpResult] -or $InputObject -is [McpError] -or $InputObject -is [McpEvent]) {
        return "type: result`npayload:`n  requestId: $($InputObject.RequestId)`n  result: $($InputObject | ConvertTo-Json -Depth 5 -Compress)"
    }
    return $InputObject | ConvertTo-Json -Depth 10
}

function ConvertFrom-McpYaml {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Text)

    # Try JSON first (common for result payloads from the REPL)
    try {
        $parsed = $Text | ConvertFrom-Json -AsHashtable -ErrorAction Stop
        if ($parsed -is [hashtable] -and $parsed.ContainsKey('payload')) {
            return [pscustomobject]@{
                RequestId = $parsed['payload']['requestId']
                Result    = $parsed['payload']['result']
            }
        }
        return $parsed
    } catch {}

    # Minimal line-based YAML parser for the envelopes used in tests and REPL stdio
    $result = [pscustomobject]@{ RequestId = $null; Result = $null; Type = $null }
    $inPayload = $false
    foreach ($rawLine in ($Text -split "`n")) {
        $line = $rawLine.Trim()
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        if ($line -match '^type:\s*(.+)$') { $result.Type = $Matches[1].Trim(); continue }
        if ($line -match '^payload:\s*$') { $inPayload = $true; continue }
        if ($line -match '^requestId:\s*(.+)$') {
            $rid = $Matches[1].Trim()
            if ($inPayload) { $result.RequestId = $rid } else { $result.RequestId = $rid }
            continue
        }
        if ($line -match '^result:\s*(.+)$') {
            $result.Result = $Matches[1].Trim()
            continue
        }
    }
    return $result
}

function New-McpRequest { param([string]$RequestId, [string]$Method, [hashtable]$Params = @{}) [McpRequest]::new($RequestId, $Method, $Params) }
function New-McpResult  { param([string]$RequestId, $Result) $r = [McpResult]::new(); $r.RequestId = $RequestId; $r.Result = $Result; return $r }
function New-McpError   { param([string]$RequestId, [string]$Code, [string]$Message) $e = [McpError]::new(); $e.RequestId = $RequestId; $e.Code = $Code; $e.Message = $Message; return $e }
function New-McpEvent   { param([string]$Event, $Data) $ev = [McpEvent]::new(); $ev.Event = $Event; $ev.Data = $Data; return $ev }

function Invoke-McpReplRaw {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Method,
        [hashtable]$Params = @{},
        [int]$TimeoutSeconds = 30
    )
    # Simulate the real bootstrap check that the full implementation performs
    $exe = Get-Command mcpserver-repl -ErrorAction SilentlyContinue
    if (-not $exe) {
        throw "mcpserver-repl not found in PATH. Install the McpServer.Repl tool or add it to PATH."
    }
    # Real path would spawn the process, send YAML via ConvertTo-McpYaml, etc.
    Write-Warning "McpRepl: Real REPL invocation not wired in this published version stub."
    return @{ Success = $false; Output = "Not implemented in stub"; RequestId = [guid]::NewGuid().ToString() }
}

function Invoke-McpRepl {
    param([Parameter(Mandatory)][string]$Method, [hashtable]$Params = @{})
    $raw = Invoke-McpReplRaw -Method $Method -Params $Params
    if (-not $raw.Success) { throw "REPL call failed" }
    return $raw
}

function Resolve-McpCacheDir {
    if ($env:MCP_CACHE_DIR_OVERRIDE) { return $env:MCP_CACHE_DIR_OVERRIDE }
    if ($env:PLUGIN_ROOT_OVERRIDE)   { return (Join-Path $env:PLUGIN_ROOT_OVERRIDE 'cache') }
    return (Join-Path $env:USERPROFILE '.mcpServer\cache')
}

Export-ModuleMember -Function @(
    'ConvertTo-McpYaml','ConvertFrom-McpYaml',
    'New-McpRequest','New-McpResult','New-McpError','New-McpEvent',
    'Invoke-McpRepl','Invoke-McpReplRaw','Resolve-McpCacheDir'
)
