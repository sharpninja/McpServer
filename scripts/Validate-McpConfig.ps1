<#
.SYNOPSIS
    Validates MCP appsettings instance configuration.
#>
[CmdletBinding()]
param(
    [string]$ConfigPath = ""
)

$ErrorActionPreference = "Stop"

function ConvertFrom-YamlScalar {
    param(
        [string]$Value
    )

    $trimmed = $Value.Trim()
    if (($trimmed.StartsWith("'") -and $trimmed.EndsWith("'")) -or ($trimmed.StartsWith('"') -and $trimmed.EndsWith('"'))) {
        return $trimmed.Substring(1, $trimmed.Length - 2)
    }

    return $trimmed
}

function Get-McpInstancesFromYaml {
    param(
        [string]$Path
    )

    $lines = Get-Content -Path $Path
    $hasMcp = $false
    $instances = [ordered]@{}
    $inInstances = $false
    $currentInstance = $null
    $inTodoStorage = $false

    foreach ($rawLine in $lines) {
        $line = $rawLine.TrimEnd()
        if ([string]::IsNullOrWhiteSpace($line) -or $line.TrimStart().StartsWith('#')) {
            continue
        }

        if ($line -match '^Mcp:\s*$') {
            $hasMcp = $true
            continue
        }

        if (-not $hasMcp) {
            continue
        }

        if ($line -match '^  Instances:\s*$') {
            $inInstances = $true
            $currentInstance = $null
            $inTodoStorage = $false
            continue
        }

        if (-not $inInstances) {
            continue
        }

        if ($line -match '^  [A-Za-z0-9_-]+:\s*$') {
            break
        }

        if ($line -match '^    ([^:\s][^:]*):\s*$') {
            $currentInstance = $Matches[1]
            $instances[$currentInstance] = [ordered]@{
                RepoRoot = $null
                Port = $null
                TodoStorage = [ordered]@{
                    Provider = $null
                    SqliteDataSource = $null
                }
            }
            $inTodoStorage = $false
            continue
        }

        if ($null -eq $currentInstance) {
            continue
        }

        if ($line -match '^      TodoStorage:\s*$') {
            $inTodoStorage = $true
            continue
        }

        if ($line -match '^      [A-Za-z0-9_-]+:\s*') {
            $inTodoStorage = $false
        }

        if ($line -match '^      RepoRoot:\s*(.+)$') {
            $instances[$currentInstance].RepoRoot = ConvertFrom-YamlScalar $Matches[1]
            continue
        }

        if ($line -match '^      Port:\s*(.+)$') {
            $instances[$currentInstance].Port = ConvertFrom-YamlScalar $Matches[1]
            continue
        }

        if ($inTodoStorage -and $line -match '^        Provider:\s*(.+)$') {
            $instances[$currentInstance].TodoStorage.Provider = ConvertFrom-YamlScalar $Matches[1]
            continue
        }

        if ($inTodoStorage -and $line -match '^        SqliteDataSource:\s*(.+)$') {
            $instances[$currentInstance].TodoStorage.SqliteDataSource = ConvertFrom-YamlScalar $Matches[1]
            continue
        }
    }

    return @{
        HasMcp = $hasMcp
        Instances = $instances
    }
}

if ([string]::IsNullOrWhiteSpace($ConfigPath)) {
    $candidatePaths = @(
        "src/McpServer.Support.Mcp/appsettings.yaml",
        "src/McpServer.Support.Mcp/appsettings.yml",
        "src/McpServer.Support.Mcp/appsettings.json"
    )
    $ConfigPath = $candidatePaths | Where-Object { Test-Path $_ } | Select-Object -First 1
}

if (-not (Test-Path $ConfigPath)) {
    throw "Config file not found: $ConfigPath"
}

$extension = [System.IO.Path]::GetExtension($ConfigPath)
$config = switch ($extension.ToLowerInvariant()) {
    ".yaml" { Get-McpInstancesFromYaml -Path $ConfigPath }
    ".yml" { Get-McpInstancesFromYaml -Path $ConfigPath }
    ".json" {
        $json = Get-Content -Raw -Path $ConfigPath | ConvertFrom-Json
        @{
            HasMcp = $null -ne $json.Mcp
            Instances = $json.Mcp.Instances
        }
    }
    default { throw "Unsupported config format '$extension' for '$ConfigPath'." }
}

if (-not $config.HasMcp) {
    throw "Missing 'Mcp' section."
}

$instances = $config.Instances
if (-not $instances) {
    Write-Host "No Mcp:Instances configured. Validation passed."
    exit 0
}

$instanceEntries = if ($instances -is [System.Collections.IDictionary]) {
    $instances.GetEnumerator() | Sort-Object Name
}
else {
    $instances.PSObject.Properties | ForEach-Object {
        [pscustomobject]@{
            Name = $_.Name
            Value = $_.Value
        }
    }
}

$ports = @{}
$instanceEntries | ForEach-Object {
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

$instanceCount = @($instanceEntries).Count
Write-Host "MCP config validation passed for $instanceCount instances."
