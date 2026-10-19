<#
.SYNOPSIS
    Local write cache for MCP operations when server is unavailable.
.DESCRIPTION
    PowerShell cache manager for pending MCP operations. Stores pending
    commands as YAML files and replays them when the server becomes available.
    Records are built as objects and serialized through yaml-object-mutation.ps1;
    they are read back as objects, never parsed or assembled line by line.
#>
param(
    [Parameter(Mandatory)][ValidateSet('write','status','flush')][string]$Action,
    [string]$Method = '',
    [string]$ParamsYaml = ''
)

$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$maxRetries = 3

if (-not (Get-Command Resolve-McpCacheDir -ErrorAction SilentlyContinue)) {
    . (Join-Path $scriptDir 'resolve-cache-dir.ps1')
}
if (-not (Get-Command Read-McpYamlObject -ErrorAction SilentlyContinue)) {
    . (Join-Path $scriptDir 'yaml-object-mutation.ps1')
}
Import-McpYamlSerializer

function ConvertTo-PendingParamsObject {
    <#
    .SYNOPSIS
        Deserializes the params YAML text into an object for storage. Text that is not
        valid YAML is kept verbatim under paramsText so nothing is lost or hand-assembled.
    #>
    param([AllowEmptyString()][string]$Text)

    if ([string]::IsNullOrWhiteSpace($Text)) {
        return [ordered]@{ params = [ordered]@{}; paramsText = $null }
    }
    try {
        $parsed = ConvertFrom-Yaml -Yaml $Text -Ordered -ErrorAction Stop
        if ($null -eq $parsed) { $parsed = [ordered]@{} }
        return [ordered]@{ params = $parsed; paramsText = $null }
    } catch {
        return [ordered]@{ params = [ordered]@{}; paramsText = $Text }
    }
}

function Get-PendingParamsYaml {
    <#
    .SYNOPSIS
        Returns the params YAML text to replay for one pending record document.
    #>
    param([Parameter(Mandatory)]$Document)

    if ($Document.Contains('paramsText') -and -not [string]::IsNullOrWhiteSpace([string]$Document['paramsText'])) {
        return [string]$Document['paramsText']
    }
    if (-not $Document.Contains('params') -or $null -eq $Document['params']) { return '' }
    $params = $Document['params']
    if ($params -is [System.Collections.IDictionary] -and $params.Count -eq 0) { return '' }
    # The serializer ends its text with a line break; replay the params in the same form they were written
    # (TEST-MCP-PLUGIN-PSONLY-001 replays "title: First", not "title: First" plus a line break).
    return (ConvertTo-Yaml -Data $params -Options WithIndentedSequences).TrimEnd("`r", "`n")
}

$cacheDir = Resolve-McpCacheDir
$pendingDir = Join-Path $cacheDir 'pending'

if (-not (Test-Path $pendingDir)) { New-Item -ItemType Directory -Path $pendingDir -Force | Out-Null }

switch ($Action) {
    'write' {
        $count = @(Get-ChildItem -Path $pendingDir -Filter '*.yaml' -ErrorAction SilentlyContinue).Count
        $seq = '{0:D3}' -f ($count + 1)
        $timestamp = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
        $slug = $Method -replace '\.', '-'
        $filename = "$seq-$slug.yaml"
        $filepath = Join-Path $pendingDir $filename

        $paramsParts = ConvertTo-PendingParamsObject -Text $ParamsYaml
        $record = [ordered]@{
            id = $seq
            timestamp = $timestamp
            method = $Method
            params = $paramsParts.params
            retryCount = 0
        }
        if ($null -ne $paramsParts.paramsText) { $record['paramsText'] = $paramsParts.paramsText }
        Write-McpYamlObject -Path $filepath -Document $record
        Write-Output $filepath
    }
    'status' {
        $count = @(Get-ChildItem -Path $pendingDir -Filter '*.yaml' -ErrorAction SilentlyContinue).Count
        Write-Output $count
    }
    'flush' {
        $flushed = 0
        $failed = 0
        $items = Get-ChildItem -Path $pendingDir -Filter '*.yaml' -ErrorAction SilentlyContinue | Sort-Object Name
        foreach ($item in $items) {
            $document = Read-McpYamlObject -Path $item.FullName
            if ($document -isnot [System.Collections.IDictionary]) { $failed++; continue }
            $retryCount = if ($document.Contains('retryCount') -and $null -ne $document['retryCount']) { [int]$document['retryCount'] } else { 0 }

            if ($retryCount -ge $maxRetries) { continue }

            $method = if ($document.Contains('method')) { ([string]$document['method']).Trim() } else { '' }
            $paramsYaml = Get-PendingParamsYaml -Document $document
            $replPath = $scriptDir + '\repl-invoke.ps1'
            if ($env:MCP_CACHE_FLUSH_REPL -and (Test-Path -LiteralPath $env:MCP_CACHE_FLUSH_REPL)) {
                $replPath = $env:MCP_CACHE_FLUSH_REPL
            }
            try {
                if ($paramsYaml) {
                    & $replPath -Method $method -ParamsYaml $paramsYaml | Out-Null
                } else {
                    & $replPath -Method $method | Out-Null
                }

                Remove-Item $item.FullName -Force
                $flushed++
            } catch {
                $document['retryCount'] = $retryCount + 1
                Write-McpYamlObject -Path $item.FullName -Document $document
                $failed++
            }
        }
        $pending = @(Get-ChildItem -Path $pendingDir -Filter '*.yaml' -ErrorAction SilentlyContinue).Count
        Write-Output "flushed=$flushed failed=$failed pending=$pending"
    }
}
