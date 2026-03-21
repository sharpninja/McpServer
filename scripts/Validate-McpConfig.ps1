<#
.SYNOPSIS
    Validates MCP appsettings instance configuration.
#>
[CmdletBinding()]
param(
    [string]$ConfigPath = ""
)

$ErrorActionPreference = "Stop"
$yamlKeyPattern = '[A-Za-z0-9_][A-Za-z0-9_\-]*'

function ConvertFrom-YamlScalar {
    <#
    .SYNOPSIS
        Normalizes a simple YAML scalar value for validation.

    .DESCRIPTION
        Trims surrounding whitespace and removes matching single- or double-quote delimiters
        when both ends use the same quote character. Mismatched quote pairs are left unchanged
        so malformed values are not silently rewritten during validation.
    #>
    param(
        [string]$Value
    )

    $trimmed = $Value.Trim()
    if ($trimmed.Length -ge 2 -and (
            ($trimmed[0] -eq "'" -and $trimmed[$trimmed.Length - 1] -eq "'") -or
            ($trimmed[0] -eq '"' -and $trimmed[$trimmed.Length - 1] -eq '"'))) {
        return $trimmed.Substring(1, $trimmed.Length - 2)
    }

    return $trimmed
}

function Get-McpInstancesFromYaml {
    <#
    .SYNOPSIS
        Extracts the Mcp:Instances block from the repository YAML settings file.

    .DESCRIPTION
        Parses the checked-in appsettings YAML using the repository's current indentation pattern
        so the validation script can run in CI without depending on an external YAML module.
        The return value includes a HasMcp flag and an ordered dictionary of instance settings
        containing RepoRoot, Port, and TodoStorage fields needed by this validator.
    #>
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

        if ($line -match "^  ${yamlKeyPattern}:\s*$") {
            # A sibling key under Mcp means the Instances block has ended.
            break
        }

        if ($line -match "^    (${yamlKeyPattern}):\s*$") {
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

        if ($line -match '^      (RepoRoot|Port):\s*') {
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

function ConvertTo-McpInstanceMap {
    <#
    .SYNOPSIS
        Normalizes parsed instance settings into a consistent ordered dictionary.

    .DESCRIPTION
        Converts either JSON-derived PSCustomObject instances or the YAML parser output into
        the same RepoRoot/Port/TodoStorage shape so the validation logic can iterate a single
        data structure regardless of the source file format.
    #>
    param(
        [object]$Instances
    )

    $instanceMap = [ordered]@{}
    if ($null -eq $Instances) {
        return $instanceMap
    }

    $entries = if ($Instances -is [System.Collections.IDictionary]) {
        $Instances.GetEnumerator() | Sort-Object Name
    }
    else {
        $Instances.PSObject.Properties | ForEach-Object {
            [pscustomobject]@{
                Name = $_.Name
                Value = $_.Value
            }
        } | Sort-Object Name
    }

    foreach ($entry in $entries) {
        $instanceMap[$entry.Name] = [ordered]@{
            RepoRoot = $entry.Value.RepoRoot
            Port = $entry.Value.Port
            TodoStorage = [ordered]@{
                Provider = $entry.Value.TodoStorage.Provider
                SqliteDataSource = $entry.Value.TodoStorage.SqliteDataSource
            }
        }
    }

    return $instanceMap
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
            Instances = ConvertTo-McpInstanceMap -Instances $json.Mcp.Instances
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

$instanceEntries = $instances.GetEnumerator() | Sort-Object Name

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
