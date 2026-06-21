#Requires -Version 7.0
[CmdletBinding()]
param(
    [ValidateSet('Status', 'Invoke', 'CompleteTurn')]
    [string]$Command = 'Status',

    [string]$Method,

    [string]$Params,

    [string]$ParamsPath,

    [string]$Response,

    [string]$ResponsePath,

    [string]$WorkspacePath = $(if ($env:MCP_WORKSPACE_PATH) { $env:MCP_WORKSPACE_PATH } elseif ($env:MCPSERVER_WORKSPACE_PATH) { $env:MCPSERVER_WORKSPACE_PATH } elseif ($env:CLAUDE_PROJECT_DIR) { $env:CLAUDE_PROJECT_DIR } else { (Get-Location).ProviderPath }),

    [string]$PluginRoot = $(if ($env:MCP_PLUGIN_ROOT) { $env:MCP_PLUGIN_ROOT } elseif ($env:CLAUDE_PLUGIN_ROOT) { $env:CLAUDE_PLUGIN_ROOT } else { Split-Path -Parent $PSScriptRoot }),

    [string]$CacheRoot,

    [string]$BashPath,

    [int]$TimeoutSeconds = $(if ($env:MCP_PLUGIN_TIMEOUT_SECONDS) { [int]$env:MCP_PLUGIN_TIMEOUT_SECONDS } else { 90 })
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Resolve-FullPath {
    param([Parameter(Mandatory)][string]$Path)

    $resolved = Resolve-Path -LiteralPath $Path -ErrorAction Stop
    return $resolved.ProviderPath
}

function Resolve-OptionalDirectory {
    param([Parameter(Mandatory)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        [void][System.IO.Directory]::CreateDirectory($Path)
    }

    return (Resolve-FullPath $Path)
}

function Resolve-BashExecutable {
    param([string]$Candidate)

    if ($Candidate) {
        return (Resolve-FullPath $Candidate)
    }

    if ($env:BASH -and (Test-Path -LiteralPath $env:BASH)) {
        return (Resolve-FullPath $env:BASH)
    }

    foreach ($root in @(${env:ProgramFiles}, ${env:ProgramFiles(x86)})) {
        if (-not $root) {
            continue
        }

        $gitBash = Join-Path $root 'Git\bin\bash.exe'
        if (Test-Path -LiteralPath $gitBash) {
            return $gitBash
        }
    }

    foreach ($name in @('bash.exe', 'bash')) {
        $command = Get-Command $name -ErrorAction SilentlyContinue
        if ($command) {
            return $command.Source
        }
    }

    throw 'Unable to find bash. Install Git for Windows or pass -BashPath.'
}

function ConvertTo-BashPath {
    param([Parameter(Mandatory)][string]$Path)

    $full = Resolve-FullPath $Path
    if ($full -match '^([A-Za-z]):\\(.*)$') {
        $drive = $Matches[1].ToLowerInvariant()
        $tail = $Matches[2] -replace '\\', '/'
        return "/$drive/$tail"
    }

    return ($full -replace '\\', '/')
}

function Read-RedirectedInput {
    if ([Console]::IsInputRedirected) {
        return [Console]::In.ReadToEnd()
    }

    return ''
}

function Read-OptionalText {
    param(
        [string]$Inline,
        [bool]$HasInline,
        [string]$Path,
        [switch]$AllowRedirectedInput
    )

    if ($Path) {
        return [System.IO.File]::ReadAllText((Resolve-FullPath $Path))
    }

    if ($HasInline) {
        return $Inline
    }

    if ($AllowRedirectedInput) {
        return (Read-RedirectedInput)
    }

    return ''
}

function Invoke-BashPluginScript {
    param(
        [Parameter(Mandatory)][string]$ScriptPath,
        [string[]]$Arguments = @(),
        [string]$StandardInput = ''
    )

    $bash = Resolve-BashExecutable $BashPath
    $pluginRootFull = Resolve-FullPath $PluginRoot
    $workspaceFull = Resolve-OptionalDirectory $WorkspacePath
    $cacheRootFull = if ($CacheRoot) {
        Resolve-OptionalDirectory $CacheRoot
    } elseif ($env:PLUGIN_ROOT_OVERRIDE) {
        Resolve-OptionalDirectory $env:PLUGIN_ROOT_OVERRIDE
    } else {
        $pluginRootFull
    }

    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $bash
    $startInfo.WorkingDirectory = $workspaceFull
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardInput = $true
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.ArgumentList.Add((ConvertTo-BashPath $ScriptPath))
    foreach ($argument in $Arguments) {
        $startInfo.ArgumentList.Add($argument)
    }
    $startInfo.Environment['MCP_PLUGIN_ROOT'] = $pluginRootFull
    $startInfo.Environment['CLAUDE_PLUGIN_ROOT'] = $pluginRootFull
    $startInfo.Environment['PLUGIN_ROOT_OVERRIDE'] = $cacheRootFull
    $startInfo.Environment['MCP_WORKSPACE_PATH'] = $workspaceFull
    $startInfo.Environment['MCPSERVER_WORKSPACE_PATH'] = $workspaceFull
    $startInfo.Environment['MCP_WORKSPACE_START_DIR'] = $workspaceFull
    $startInfo.Environment['CLAUDE_PROJECT_DIR'] = $workspaceFull

    $process = [System.Diagnostics.Process]::new()
    $process.StartInfo = $startInfo
    [void]$process.Start()

    if ($StandardInput.Length -gt 0) {
        $process.StandardInput.Write($StandardInput)
    }
    $process.StandardInput.Close()

    $stdoutTask = $process.StandardOutput.ReadToEndAsync()
    $stderrTask = $process.StandardError.ReadToEndAsync()
    $boundedTimeout = [Math]::Max(1, $TimeoutSeconds)
    if (-not $process.WaitForExit($boundedTimeout * 1000)) {
        try {
            if ([System.Environment]::OSVersion.Platform -eq [System.PlatformID]::Win32NT) {
                & "$env:WINDIR\System32\taskkill.exe" /PID $process.Id /T /F > $null 2> $null
            } else {
                $process.Kill($true)
            }
        } catch {
        }
        throw "Plugin command timed out after ${boundedTimeout}s."
    }
    $stdout = $stdoutTask.Result
    $stderr = $stderrTask.Result

    if ($stderr.Length -gt 0) {
        [Console]::Error.Write($stderr)
    }

    if ($process.ExitCode -ne 0) {
        throw "Plugin command failed with exit code $($process.ExitCode)."
    }

    if ($stdout.Length -gt 0) {
        Write-Output ($stdout.TrimEnd("`r", "`n"))
    }
}

$pluginRootFull = Resolve-FullPath $PluginRoot

function Resolve-StatusScript {
    param([Parameter(Mandatory)][string]$Root)

    if ($env:MCP_STATUS_SCRIPT -and (Test-Path -LiteralPath $env:MCP_STATUS_SCRIPT)) {
        return $env:MCP_STATUS_SCRIPT
    }

    foreach ($libName in @('lib-sh', 'lib')) {
        $libDir = Join-Path $Root $libName
        $candidate = Get-ChildItem -LiteralPath $libDir -Filter 'mcp.*.status.sh' -ErrorAction SilentlyContinue |
            Select-Object -First 1
        if ($candidate) {
            return $candidate.FullName
        }
    }

    return (Resolve-PluginShellScript -Root $Root -Name 'mcp-status.sh')
}

function Resolve-PluginShellScript {
    param(
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][string]$Name
    )

    foreach ($libName in @('lib-sh', 'lib')) {
        $candidate = Join-Path (Join-Path $Root $libName) $Name
        if (Test-Path -LiteralPath $candidate) {
            return $candidate
        }
    }

    throw "Unable to find plugin shell script '$Name' under '$Root\\lib-sh' or '$Root\\lib'."
}

switch ($Command) {
    'Status' {
        Invoke-BashPluginScript -ScriptPath (Resolve-StatusScript -Root $pluginRootFull)
    }
    'Invoke' {
        if (-not $Method) {
            throw '-Method is required when -Command Invoke is used.'
        }

        $paramsText = Read-OptionalText -Inline $Params -HasInline:$($PSBoundParameters.ContainsKey('Params')) -Path $ParamsPath -AllowRedirectedInput
        Invoke-BashPluginScript -ScriptPath (Resolve-PluginShellScript -Root $pluginRootFull -Name 'repl-invoke.sh') -Arguments @($Method) -StandardInput ($paramsText ?? '')
    }
    'CompleteTurn' {
        $responseText = Read-OptionalText -Inline $Response -HasInline:$($PSBoundParameters.ContainsKey('Response')) -Path $ResponsePath -AllowRedirectedInput
        if (-not $responseText) {
            $responseText = 'Turn completed.'
        }

        Invoke-BashPluginScript -ScriptPath (Resolve-PluginShellScript -Root $pluginRootFull -Name 'final-response.sh') -StandardInput $responseText
    }
}
