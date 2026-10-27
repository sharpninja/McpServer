<#
.SYNOPSIS
    Local write cache for MCP operations when server is unavailable.
.DESCRIPTION
    PowerShell cache manager for pending MCP operations. Stores pending
    commands as YAML files and replays them when the server becomes available.
    Records are built as objects and serialized through yaml-object-mutation.ps1;
    they are read back as objects, never parsed or assembled line by line.
#>
[CmdletBinding(SupportsShouldProcess)]
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

    if ($Document.Contains('paramsText') -and $null -ne $Document['paramsText']) {
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

# Status is read-only; WhatIf returns before any directory, record or replay effect.
if ($Action -ne 'status' -and -not $PSCmdlet.ShouldProcess($pendingDir, ('Cache '+$Action+' using existing pending lifecycle'))) { return }
if ($Action -ceq 'write' -and -not (Test-Path -LiteralPath $pendingDir)) { New-Item -ItemType Directory -Path $pendingDir -Force | Out-Null }

switch ($Action) {
    'write' {
        # Use the highest durable ordinal, not count: retired records leave holes.
        $highestOrdinal = [bigint]::Zero
        foreach ($pendingFile in @(Get-ChildItem -LiteralPath $pendingDir -Filter '*.yaml' -File -ErrorAction SilentlyContinue)) {
            $prefix = [regex]::Match($pendingFile.Name, '^([0-9]+)-')
            if ($prefix.Success) {
                $ordinal = [bigint]::Parse($prefix.Groups[1].Value)
                if ($ordinal -gt $highestOrdinal) { $highestOrdinal = $ordinal }
            }
        }
        $seq = ($highestOrdinal + [bigint]::One).ToString('D3')
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
        # Keep original invocation bytes as data. Parsing is retained only for legacy inspection.
        $record['paramsText'] = $ParamsYaml
        Write-McpYamlObject -Path $filepath -Document $record -ExactScalars -NoClobber
        Write-Output $filepath
    }
    'status' {
        $count = @(Get-ChildItem -Path $pendingDir -Filter '*.yaml' -ErrorAction SilentlyContinue).Count
        Write-Output $count
    }
    'flush' {
        $flushed = 0
        $failed = 0
        $items = Get-ChildItem -LiteralPath $pendingDir -Filter '*.yaml' -File -ErrorAction SilentlyContinue | Sort-Object -Property @{ Expression = {
            $prefix = [regex]::Match($_.Name, '^([0-9]+)-')
            if ($prefix.Success) { [bigint]::Parse($prefix.Groups[1].Value) } else { [bigint]::Zero }
        } }, Name
        foreach ($item in $items) {
            $document = Read-McpYamlObject -Path $item.FullName
            if ($document -isnot [System.Collections.IDictionary]) { $failed++; continue }
            $retryCount = if ($document.Contains('retryCount') -and $null -ne $document['retryCount']) { [int]$document['retryCount'] } else { 0 }

            if ($retryCount -ge $maxRetries) { continue }

            $method = if ($document.Contains('method')) { ([string]$document['method']).Trim() } else { '' }
            $paramsYaml = Get-PendingParamsYaml -Document $document
            $replPath = Join-Path $scriptDir 'repl-invoke.ps1'
            if ($env:MCP_CACHE_FLUSH_REPL -and (Test-Path -LiteralPath $env:MCP_CACHE_FLUSH_REPL)) {
                $replPath = $env:MCP_CACHE_FLUSH_REPL
            }
            try {
                if ($paramsYaml) {
                    & $replPath -Method $method -ParamsYaml $paramsYaml | Out-Null
                } else {
                    & $replPath -Method $method | Out-Null
                }

                Remove-Item -LiteralPath $item.FullName -Force
                $flushed++
            } catch {
                $document['retryCount'] = $retryCount + 1
                Write-McpYamlObject -Path $item.FullName -Document $document -ExactScalars
                $failed++
            }
        }
        $pending = @(Get-ChildItem -Path $pendingDir -Filter '*.yaml' -ErrorAction SilentlyContinue).Count
        Write-Output "flushed=$flushed failed=$failed pending=$pending"
    }
}

