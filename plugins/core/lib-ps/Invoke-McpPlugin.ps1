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

    [int]$TimeoutSeconds = $(if ($env:MCP_PLUGIN_TIMEOUT_SECONDS) { [int]$env:MCP_PLUGIN_TIMEOUT_SECONDS } else { 90 })
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$shimModule = Join-Path $PSScriptRoot 'McpPluginShim.psm1'
Import-Module $shimModule -ErrorAction Stop

$script:McpPluginInvocation = New-McpPluginInvocationOptions `
    -Command $Command `
    -Method ($Method ?? '') `
    -Params ($Params ?? '') `
    -ParamsPath ($ParamsPath ?? '') `
    -Response ($Response ?? '') `
    -ResponsePath ($ResponsePath ?? '') `
    -WorkspacePath $WorkspacePath `
    -PluginRoot $PluginRoot `
    -CacheRoot ($CacheRoot ?? '') `
    -TimeoutSeconds $TimeoutSeconds

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

function Resolve-PluginPowerShellScript {
    param(
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][string]$Name
    )

    foreach ($libName in @('lib', 'lib-ps')) {
        $candidate = Join-Path (Join-Path $Root $libName) $Name
        if (Test-Path -LiteralPath $candidate) {
            return $candidate
        }
    }

    throw "Unable to find plugin PowerShell script '$Name' under '$Root'."
}

function Invoke-PluginPowerShellScript {
    param(
        [Parameter(Mandatory)][string]$ScriptPath,
        [string[]]$Arguments = @(),
        [string]$StandardInput = ''
    )

    $pluginRootFull = Resolve-FullPath $script:McpPluginInvocation.PluginRoot
    $workspaceFull = Resolve-OptionalDirectory $script:McpPluginInvocation.WorkspacePath
    $cacheRootFull = if ($script:McpPluginInvocation.CacheRoot) {
        Resolve-OptionalDirectory $script:McpPluginInvocation.CacheRoot
    } elseif ($env:PLUGIN_ROOT_OVERRIDE) {
        Resolve-OptionalDirectory $env:PLUGIN_ROOT_OVERRIDE
    } else {
        $pluginRootFull
    }

    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = (Get-Command pwsh -ErrorAction Stop).Source
    $startInfo.WorkingDirectory = $workspaceFull
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardInput = $true
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.ArgumentList.Add('-NoLogo')
    $startInfo.ArgumentList.Add('-NoProfile')
    $startInfo.ArgumentList.Add('-NonInteractive')
    $startInfo.ArgumentList.Add('-File')
    $startInfo.ArgumentList.Add((Resolve-FullPath $ScriptPath))
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
    $boundedTimeout = [Math]::Max(1, $script:McpPluginInvocation.TimeoutSeconds)
    if (-not $process.WaitForExit($boundedTimeout * 1000)) {
        try {
            $process.Kill($true)
        } catch {
        }
        throw "Plugin command timed out after ${boundedTimeout}s."
    }

    $stdout = $stdoutTask.Result
    $stderr = $stderrTask.Result

    if ($stderr.Length -gt 0) {
        [Console]::Error.Write($stderr)
    }

    if ($stdout.Length -gt 0) {
        Write-Output ($stdout.TrimEnd("`r", "`n"))
    }

    if ($process.ExitCode -ne 0) {
        $msg = "Plugin command failed with exit code $($process.ExitCode)."
        if ($stdout) { $msg += "`n" + $stdout }
        throw $msg
    }
}

$pluginRootFull = Resolve-FullPath $script:McpPluginInvocation.PluginRoot

switch ($script:McpPluginInvocation.Command) {
    'Status' {
        Invoke-PluginPowerShellScript -ScriptPath (Resolve-PluginPowerShellScript -Root $pluginRootFull -Name 'mcp-status.ps1')
    }
    'Invoke' {
        if (-not $script:McpPluginInvocation.Method) {
            throw '-Method is required when -Command Invoke is used.'
        }

        $paramsText = Read-OptionalText -Inline $script:McpPluginInvocation.Params -HasInline:$($PSBoundParameters.ContainsKey('Params')) -Path $script:McpPluginInvocation.ParamsPath -AllowRedirectedInput
        Invoke-PluginPowerShellScript -ScriptPath (Resolve-PluginPowerShellScript -Root $pluginRootFull -Name 'repl-invoke.ps1') -Arguments @('-Method', $script:McpPluginInvocation.Method, '-ParamsYaml', ($paramsText ?? ''))
    }
    'CompleteTurn' {
        $responseText = Read-OptionalText -Inline $script:McpPluginInvocation.Response -HasInline:$($PSBoundParameters.ContainsKey('Response')) -Path $script:McpPluginInvocation.ResponsePath -AllowRedirectedInput
        if (-not $responseText) {
            $responseText = 'Turn completed.'
        }

        Invoke-PluginPowerShellScript -ScriptPath (Resolve-PluginPowerShellScript -Root $pluginRootFull -Name 'final-response.ps1') -StandardInput $responseText
    }
}
