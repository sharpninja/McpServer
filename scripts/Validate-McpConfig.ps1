<#
.SYNOPSIS
    Validates MCP appsettings instance configuration.
#>
[CmdletBinding()]
param(
    [string]$ConfigPath = "src/McpServer.Support.Mcp/appsettings.json"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $ConfigPath)) {
    throw "Config file not found: $ConfigPath"
}

$json = Get-Content -Raw -Path $ConfigPath | ConvertFrom-Json
if (-not $json.Mcp) {
    throw "Missing 'Mcp' section."
}

$instances = $json.Mcp.Instances
if (-not $instances) {
    Write-Host "No Mcp:Instances configured. Validation passed."
    exit 0
}

$ports = @{}
$instances.PSObject.Properties | ForEach-Object {
    $name = $_.Name
    $instance = $_.Value

    if (-not $instance.RepoRoot) {
        throw "Instance '$name' missing RepoRoot."
    }
    $resolvedRoot = [System.IO.Path]::GetFullPath([string]$instance.RepoRoot)
    if (-not (Test-Path -Path $resolvedRoot -PathType Container)) {
        throw "Instance '$name' RepoRoot does not exist: '$($instance.RepoRoot)' (resolved '$resolvedRoot')."
    }

    $port = [int]$instance.Port
    if ($port -le 0) {
        throw "Instance '$name' has invalid port '$($instance.Port)'."
    }

    if ($ports.ContainsKey($port)) {
        throw "Duplicate port '$port' in instances '$($ports[$port])' and '$name'."
    }
    $ports[$port] = $name

    $provider = "yaml"
    if ($instance.TodoStorage -and $instance.TodoStorage.Provider) {
        $provider = ([string]$instance.TodoStorage.Provider).Trim().ToLowerInvariant()
    }

    if (@("yaml", "sqlite") -notcontains $provider) {
        throw "Instance '$name' has unsupported TodoStorage provider '$provider'. Allowed: yaml, sqlite."
    }

    if ($provider -eq "sqlite") {
        $sqliteDataSource = ""
        if ($instance.TodoStorage -and $instance.TodoStorage.SqliteDataSource) {
            $sqliteDataSource = [string]$instance.TodoStorage.SqliteDataSource
        }
        if ([string]::IsNullOrWhiteSpace($sqliteDataSource)) {
            throw "Instance '$name' provider sqlite requires TodoStorage.SqliteDataSource."
        }
    }
}

$instanceCount = @($instances.PSObject.Properties).Count
Write-Host "MCP config validation passed for $instanceCount instances."
