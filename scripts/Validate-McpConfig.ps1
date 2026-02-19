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

    $port = [int]$instance.Port
    if ($port -le 0) {
        throw "Instance '$name' has invalid port '$($instance.Port)'."
    }

    if ($ports.ContainsKey($port)) {
        throw "Duplicate port '$port' in instances '$($ports[$port])' and '$name'."
    }
    $ports[$port] = $name
}

$instanceCount = @($instances.PSObject.Properties).Count
Write-Host "MCP config validation passed for $instanceCount instances."
