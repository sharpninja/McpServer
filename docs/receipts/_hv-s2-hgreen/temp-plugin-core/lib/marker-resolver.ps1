$ErrorActionPreference = 'Stop'

# Functions for marker file discovery and verification (PowerShell equivalent).

$script:MARKER_FILENAME = 'AGENTS-README-FIRST.yaml'

function Find-MarkerFile {
    param(
        [string]$StartDir = (Get-Location).Path,
        [int]$MaxDepth = 20
    )

    $dir = $StartDir
    $depth = 0
    while ($dir -and $depth -lt $MaxDepth) {
        $candidate = Join-Path $dir $script:MARKER_FILENAME
        if (Test-Path $candidate) {
            return $candidate
        }
        $parent = Split-Path $dir -Parent
        if ($parent -eq $dir) { break }
        $dir = $parent
        $depth++
    }

    throw "No marker file ($script:MARKER_FILENAME) found walking up from $StartDir"
}

function Get-MarkerField {
    param(
        [Parameter(Mandatory)][string]$MarkerFile,
        [Parameter(Mandatory)][string]$FieldName
    )

    $content = Get-Content $MarkerFile -Raw
    foreach ($line in ($content -split "`n")) {
        if ($line -match "^${FieldName}:\s*(.+)$") {
            $value = $Matches[1].Trim()
            # Remove surrounding quotes if present
            if ($value.StartsWith('"') -and $value.EndsWith('"')) {
                $value = $value.Substring(1, $value.Length - 2)
            }
            return $value
        }
    }
    return $null
}

function Get-MarkerFileSnapshot {
    param(
        [string]$StartDir = (Get-Location).Path,
        [string]$MarkerFile
    )

    $path = $MarkerFile
    if (-not $path) {
        $path = Find-MarkerFile -StartDir $StartDir
    }

    $resolved = (Resolve-Path -LiteralPath $path).ProviderPath
    $item = Get-Item -LiteralPath $resolved
    return [ordered]@{
        markerFilePath = $resolved
        markerLastWriteUtc = $item.LastWriteTimeUtc.ToString('O')
    }
}

function Get-MarkerEndpoint {
    param(
        [Parameter(Mandatory)][string]$MarkerFile,
        [Parameter(Mandatory)][string]$EndpointName
    )

    $inEndpoints = $false
    foreach ($line in (Get-Content $MarkerFile)) {
        if ($line -match '^endpoints:') {
            $inEndpoints = $true
            continue
        }
        if ($inEndpoints -and $line -match '^\S') {
            break
        }
        if ($inEndpoints -and $line -match "^\s+${EndpointName}:\s*(.+)$") {
            return $Matches[1].Trim()
        }
    }
    return $null
}

function Get-MarkerAgentPluginField {
    param(
        [Parameter(Mandatory)][string]$MarkerFile,
        [Parameter(Mandatory)][string]$FieldName
    )

    $inAgentPlugins = $false
    foreach ($line in (Get-Content $MarkerFile)) {
        if ($line -match '^agent_plugins:') {
            $inAgentPlugins = $true
            continue
        }
        if ($inAgentPlugins -and $line -match '^\S') {
            break
        }
        if ($inAgentPlugins -and $line -match "^\s+${FieldName}:\s*(.+)$") {
            return $Matches[1].Trim()
        }
    }
    return $null
}

function Test-MarkerSignature {
    param(
        [Parameter(Mandatory)][string]$MarkerFile
    )

    $apiKey = Get-MarkerField -MarkerFile $MarkerFile -FieldName 'apiKey'
    $port = Get-MarkerField -MarkerFile $MarkerFile -FieldName 'port'
    $baseUrl = Get-MarkerField -MarkerFile $MarkerFile -FieldName 'baseUrl'
    $workspace = Get-MarkerField -MarkerFile $MarkerFile -FieldName 'workspace'
    $workspacePath = Get-MarkerField -MarkerFile $MarkerFile -FieldName 'workspacePath'
    $markerPid = Get-MarkerField -MarkerFile $MarkerFile -FieldName 'pid'
    $startedAt = Get-MarkerField -MarkerFile $MarkerFile -FieldName 'startedAt'
    $markerWrittenAtUtc = Get-MarkerField -MarkerFile $MarkerFile -FieldName 'markerWrittenAtUtc'
    $serverStartedAtUtc = Get-MarkerField -MarkerFile $MarkerFile -FieldName 'serverStartedAtUtc'

    # Build canonical payload (marker-v1 format)
    $payload = "canonicalization=marker-v1`n"
    $payload += "port=$port`n"
    $payload += "baseUrl=$baseUrl`n"
    $payload += "apiKey=$apiKey`n"
    $payload += "workspace=$workspace`n"
    $payload += "workspacePath=$workspacePath`n"
    $payload += "pid=$markerPid`n"
    $payload += "startedAt=$startedAt`n"
    $payload += "markerWrittenAtUtc=$markerWrittenAtUtc`n"
    $payload += "serverStartedAtUtc=$serverStartedAtUtc`n"

    # Extract endpoints
    $inEndpoints = $false
    foreach ($line in (Get-Content $MarkerFile)) {
        if ($line -match '^endpoints:') {
            $inEndpoints = $true
            continue
        }
        if ($inEndpoints -and $line -match '^\S') {
            break
        }
        if ($inEndpoints -and $line -match '^\s+(\S+):\s*(.+)$') {
            $key = $Matches[1].Trim()
            $val = $Matches[2].Trim()
            $payload += "endpoints.$key=$val`n"
        }
    }

    $agentPluginsPolicy = Get-MarkerAgentPluginField -MarkerFile $MarkerFile -FieldName 'policy'
    $agentPluginsDigest = Get-MarkerAgentPluginField -MarkerFile $MarkerFile -FieldName 'contract_digest'
    if ($agentPluginsPolicy -or $agentPluginsDigest) {
        $payload += "agentPlugins.policy=$agentPluginsPolicy`n"
        $payload += "agentPlugins.contractDigest=$agentPluginsDigest`n"
    }

    # Extract stored signature
    $storedSignature = $null
    $inSignature = $false
    foreach ($line in (Get-Content $MarkerFile)) {
        if ($line -match '^signature:') {
            $inSignature = $true
            continue
        }
        if ($inSignature -and $line -match '^\S') {
            break
        }
        if ($inSignature -and $line -match '^\s+value:\s*(.+)$') {
            $storedSignature = $Matches[1].Trim()
        }
    }

    if (-not $storedSignature) {
        Write-Error "No signature value found in marker file"
        return $false
    }

    # Compute HMAC-SHA256
    $keyBytes = [System.Text.Encoding]::UTF8.GetBytes($apiKey)
    $payloadBytes = [System.Text.Encoding]::UTF8.GetBytes($payload)
    $hmac = New-Object System.Security.Cryptography.HMACSHA256
    $hmac.Key = $keyBytes
    $hashBytes = $hmac.ComputeHash($payloadBytes)
    $computed = ($hashBytes | ForEach-Object { $_.ToString('X2') }) -join ''

    return ($computed -eq $storedSignature)
}

function Invoke-FullBootstrap {
    param(
        [string]$StartDir = (Get-Location).Path
    )

    # Find marker
    try {
        $markerFile = Find-MarkerFile -StartDir $StartDir
    } catch {
        Write-Error "MCP_UNTRUSTED: No marker file found"
        return $false
    }

    # Verify signature
    if (-not (Test-MarkerSignature -MarkerFile $markerFile)) {
        Write-Error "MCP_UNTRUSTED: Signature verification failed"
        return $false
    }

    # Health nonce check
    $baseUrl = Get-MarkerField -MarkerFile $markerFile -FieldName 'baseUrl'
    $nonce = "nonce-$(Get-Date -Format 'yyyyMMddHHmmss')-$PID"
    try {
        $response = Invoke-RestMethod -Uri "$baseUrl/health?nonce=$nonce" -TimeoutSec 5
        if ($response.nonce -ne $nonce) {
            Write-Error "MCP_UNTRUSTED: Nonce verification failed"
            return $false
        }
    } catch {
        Write-Error "MCP_UNTRUSTED: Health check failed - $_"
        return $false
    }

    return $true
}
