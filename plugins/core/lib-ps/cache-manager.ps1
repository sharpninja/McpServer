<#
.SYNOPSIS
    Local write cache for MCP operations when server is unavailable.
.DESCRIPTION
    PowerShell fallback for cache-manager.sh. Stores pending REPL commands
    as YAML files and replays them when the server becomes available.
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

$cacheDir = Resolve-McpCacheDir
$pendingDir = Join-Path $cacheDir 'pending'

if (-not (Test-Path $pendingDir)) { New-Item -ItemType Directory -Path $pendingDir -Force | Out-Null }

switch ($Action) {
    'write' {
        $count = (Get-ChildItem -Path $pendingDir -Filter '*.yaml' -ErrorAction SilentlyContinue).Count
        $seq = '{0:D3}' -f ($count + 1)
        $timestamp = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
        $slug = $Method -replace '\.', '-'
        $filename = "$seq-$slug.yaml"
        $filepath = Join-Path $pendingDir $filename

        $content = @"
id: "$seq"
timestamp: "$timestamp"
method: $Method
$(if ($ParamsYaml) { "params:`n$($ParamsYaml -split "`n" | ForEach-Object { "  $_" } | Out-String)" } else { "params: {}" })
retryCount: 0
"@
        Set-Content -Path $filepath -Value $content.TrimEnd() -NoNewline
        Write-Output $filepath
    }
    'status' {
        $count = (Get-ChildItem -Path $pendingDir -Filter '*.yaml' -ErrorAction SilentlyContinue).Count
        Write-Output $count
    }
    'flush' {
        $flushed = 0
        $failed = 0
        $items = Get-ChildItem -Path $pendingDir -Filter '*.yaml' -ErrorAction SilentlyContinue | Sort-Object Name
        foreach ($item in $items) {
            $content = Get-Content $item.FullName -Raw
            $methodMatch = [regex]::Match($content, '^method:\s*(.+)$', 'Multiline')
            $retryMatch = [regex]::Match($content, '^retryCount:\s*(\d+)', 'Multiline')
            $retryCount = if ($retryMatch.Success) { [int]$retryMatch.Groups[1].Value } else { 0 }

            if ($retryCount -ge $maxRetries) { continue }

            $method = $methodMatch.Groups[1].Value.Trim()
            try {
                & "$scriptDir\repl-invoke.ps1" -Method $method | Out-Null
                Remove-Item $item.FullName -Force
                $flushed++
            } catch {
                $newCount = $retryCount + 1
                $content = $content -replace "retryCount:\s*\d+", "retryCount: $newCount"
                Set-Content -Path $item.FullName -Value $content -NoNewline
                $failed++
            }
        }
        $pending = (Get-ChildItem -Path $pendingDir -Filter '*.yaml' -ErrorAction SilentlyContinue).Count
        Write-Output "flushed=$flushed failed=$failed pending=$pending"
    }
}
