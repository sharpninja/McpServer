#Requires -Version 7.0

function Import-McpYamlSerializer {
    <#
    .SYNOPSIS
        Loads the YAML serializer used for object-first YAML updates.
    .DESCRIPTION
        YAML files must be deserialized, mutated as objects, serialized, and saved.
        This function fails closed when the serializer is unavailable instead of
        letting callers fall back to line-based edits.
    #>
    [CmdletBinding()]
    param()

    if (-not (Get-Command ConvertFrom-Yaml -ErrorAction SilentlyContinue) -or
        -not (Get-Command ConvertTo-Yaml -ErrorAction SilentlyContinue)) {
        Import-Module powershell-yaml -ErrorAction Stop
    }
}

function Resolve-McpYamlFilePath {
    <#
    .SYNOPSIS
        Resolves a YAML file path without requiring the file to already exist.
    .PARAMETER Path
        Existing or future YAML file path.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Path
    )

    return $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Path)
}

function Invoke-McpYamlFileOperation {
    <#
    .SYNOPSIS
        Runs a YAML file operation with retry/backoff for transient file locks.
    .PARAMETER Operation
        File-system operation to execute.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][scriptblock]$Operation,
        [int]$Attempts = 8,
        [int]$InitialDelayMilliseconds = 25
    )

    $delay = [Math]::Max(1, $InitialDelayMilliseconds)
    for ($attempt = 1; $attempt -le $Attempts; $attempt++) {
        try {
            return (& $Operation)
        } catch [System.IO.IOException] {
            if ($attempt -ge $Attempts) { throw }
        } catch [System.UnauthorizedAccessException] {
            if ($attempt -ge $Attempts) { throw }
        }

        Start-Sleep -Milliseconds $delay
        $delay = [Math]::Min($delay * 2, 500)
    }

    throw 'YAML file operation failed after retry attempts.'
}
function Read-McpYamlObject {
    <#
    .SYNOPSIS
        Deserializes a YAML document into an ordered PowerShell object.
    .PARAMETER Path
        YAML file path to read.
    .PARAMETER Create
        Return an empty ordered map when the file does not exist or is empty.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Path,
        [switch]$Create
    )

    Import-McpYamlSerializer
    $resolvedPath = Resolve-McpYamlFilePath -Path $Path
    if (-not (Test-Path -LiteralPath $resolvedPath -PathType Leaf)) {
        if ($Create) {
            return [ordered]@{}
        }

        throw "YAML file not found: $resolvedPath"
    }

    $yamlText = Invoke-McpYamlFileOperation -Operation {
        [System.IO.File]::ReadAllText($resolvedPath)
    }
    if ([string]::IsNullOrWhiteSpace($yamlText)) {
        if ($Create) {
            return [ordered]@{}
        }

        throw "YAML file is empty: $resolvedPath"
    }

    return (ConvertFrom-Yaml -Yaml $yamlText -Ordered -ErrorAction Stop)
}

function Write-McpYamlObject {
    <#
    .SYNOPSIS
        Serializes a PowerShell object and saves it as a YAML document.
    .PARAMETER Path
        YAML file path to write.
    .PARAMETER Document
        PowerShell object to serialize.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)]$Document
    )

    Import-McpYamlSerializer
    $resolvedPath = Resolve-McpYamlFilePath -Path $Path
    $parentPath = Split-Path -Parent $resolvedPath
    if ($parentPath) {
        [System.IO.Directory]::CreateDirectory($parentPath) | Out-Null
    }

    $yamlText = ConvertTo-Yaml -Data $Document -Options WithIndentedSequences
    $tempPath = [System.IO.Path]::Combine(
        $parentPath,
        ([System.IO.Path]::GetFileName($resolvedPath) + "." + [System.Guid]::NewGuid().ToString("N") + ".tmp"))

    try {
        [System.IO.File]::WriteAllText($tempPath, ($yamlText.TrimEnd() + "`n"), [System.Text.UTF8Encoding]::new($false))
        Invoke-McpYamlFileOperation -Operation {
            [System.IO.File]::Move($tempPath, $resolvedPath, $true)
        } | Out-Null
    } catch {
        Remove-Item -LiteralPath $tempPath -Force -ErrorAction SilentlyContinue
        throw
    }
}

function Update-McpYamlObject {
    <#
    .SYNOPSIS
        Deserializes, mutates, serializes, and saves a YAML file.
    .PARAMETER Path
        YAML file path to update.
    .PARAMETER Mutation
        Script block that receives the deserialized document and mutates it.
    .PARAMETER Create
        Create an empty ordered map before mutation when the file is absent or empty.
    .EXAMPLE
        Update-McpYamlObject -Path .\appsettings.yaml -Create -Mutation {
            param($document)
            $document['Triage'] = [ordered]@{ AgentPath = 'codex' }
        }
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][scriptblock]$Mutation,
        [switch]$Create
    )

    $document = Read-McpYamlObject -Path $Path -Create:$Create
    & $Mutation $document
    Write-McpYamlObject -Path $Path -Document $document
    return $document
}

function Set-McpYamlObjectValue {
    <#
    .SYNOPSIS
        Sets one YAML value by deserializing, mutating the object, serializing, and saving.
    .PARAMETER Path
        YAML file path to update.
    .PARAMETER KeyPath
        One or more keys. Multiple keys create or update a nested mapping.
    .PARAMETER Value
        Value to assign at the key path. Prefer ordered maps or arrays for complex values.
    .PARAMETER Create
        Create an empty ordered map before mutation when the file is absent or empty.
    .EXAMPLE
        Set-McpYamlObjectValue -Path .\appsettings.yaml -KeyPath Triage,AgentPath -Value 'codex' -Create
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][ValidateNotNullOrEmpty()][string[]]$KeyPath,
        [Parameter(Mandatory)][AllowNull()]$Value,
        [switch]$Create
    )

    Update-McpYamlObject -Path $Path -Create:$Create -Mutation {
        param($document)

        if ($document -isnot [System.Collections.IDictionary]) {
            throw 'Root YAML document must be a mapping.'
        }

        $target = $document
        for ($index = 0; $index -lt ($KeyPath.Count - 1); $index++) {
            $key = $KeyPath[$index]
            if (-not $target.Contains($key) -or $target[$key] -isnot [System.Collections.IDictionary]) {
                $target[$key] = [ordered]@{}
            }

            $target = $target[$key]
        }

        $target[$KeyPath[-1]] = $Value
    }
}

function ConvertTo-McpYamlText {
    <#
    .SYNOPSIS
        Serializes a native object to YAML text through the shared serializer.
    .DESCRIPTION
        The only sanctioned way for plugin scripts to produce YAML text. Callers pass
        ordered maps, hashtables, arrays or PSCustomObjects; they never assemble YAML
        from string literals, joins or here-strings.
    .PARAMETER Document
        Object to serialize.
    .EXAMPLE
        ConvertTo-McpYamlText -Document ([ordered]@{ method = 'workflow.todo.list'; params = [ordered]@{} })
    #>
    [CmdletBinding()]
    [OutputType([string])]
    param(
        [Parameter(Mandatory)][AllowNull()]$Document
    )

    Import-McpYamlSerializer
    $data = if ($null -eq $Document) { [ordered]@{} } else { $Document }
    return (ConvertTo-Yaml -Data $data -Options WithIndentedSequences)
}

function Write-McpReplLogEntry {
    <#
    .SYNOPSIS
        Appends one REPL diagnostic log entry (MCP_PLUGIN_REPL_LOG) as a serialized YAML document.
    .DESCRIPTION
        Each entry is a mapping with method and params (the params YAML text as a scalar),
        serialized from an object and terminated by a document separator so the log reads
        back with ConvertFrom-Yaml -AllDocuments.
    .PARAMETER Path
        Log file path (normally $env:MCP_PLUGIN_REPL_LOG).
    .PARAMETER Method
        Workflow or client method name.
    .PARAMETER ParamsYaml
        Params YAML text as sent to the REPL; may be empty.
    .EXAMPLE
        Write-McpReplLogEntry -Path $env:MCP_PLUGIN_REPL_LOG -Method workflow.todo.list -ParamsYaml (ConvertTo-McpYamlText -Document ([ordered]@{ done = $false }))
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Method,
        [AllowEmptyString()][string]$ParamsYaml = ''
    )

    $normalized = ($ParamsYaml -replace "`r`n", "`n") -replace "`r", "`n"
    $entry = [ordered]@{
        method = $Method
        params = $normalized
    }
    $text = (ConvertTo-McpYamlText -Document $entry).TrimEnd("`r", "`n")
    Add-Content -LiteralPath $Path -Value ($text + [Environment]::NewLine + '---')
}
