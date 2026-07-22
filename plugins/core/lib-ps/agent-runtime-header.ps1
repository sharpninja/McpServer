# Shared agent runtime header resolution for plugin-created session logs.

function Get-McpPluginFirstText {
    param([AllowNull()][object[]]$Values)

    foreach ($value in $Values) {
        if ($null -ne $value -and -not [string]::IsNullOrWhiteSpace([string]$value)) {
            return [string]$value
        }
    }

    return ''
}

function Get-McpPluginAgentExecutableCandidates {
    param(
        [string]$AgentName,
        [string]$HostName)

    $key = Get-McpPluginFirstText @($HostName, $AgentName)
    $key = ($key.Trim().ToLowerInvariant() -replace '[^a-z0-9]', '')

    $candidates = [System.Collections.Generic.List[string]]::new()
    foreach ($candidate in @(
        $env:MCP_AGENT_EXECUTABLE_PATH,
        $env:CODEX_EXECUTABLE_PATH,
        $env:CLAUDE_EXECUTABLE_PATH,
        $env:GROK_EXECUTABLE_PATH,
        $env:COPILOT_EXECUTABLE_PATH,
        $env:CLINE_EXECUTABLE_PATH,
        $env:OPENCODE_EXECUTABLE_PATH)) {
        if (-not [string]::IsNullOrWhiteSpace($candidate)) { $candidates.Add([string]$candidate) }
    }

    switch ($key) {
        { $_ -in @('codex') } { $candidates.Add('codex'); $candidates.Add('codex.cmd') }
        { $_ -in @('claude', 'claudecode') } { $candidates.Add('claude'); $candidates.Add('claude.cmd') }
        { $_ -in @('claudecowork') } { $candidates.Add('claude'); $candidates.Add('claude.cmd') }
        { $_ -in @('grok', 'grokcode') } { $candidates.Add('grok'); $candidates.Add('grok.cmd') }
        { $_ -in @('copilot') } { $candidates.Add('github-copilot'); $candidates.Add('copilot') }
        { $_ -in @('cline', 'clinev2') } { $candidates.Add('cline'); $candidates.Add('cline.cmd') }
        { $_ -in @('opencode') } { $candidates.Add('opencode'); $candidates.Add('opencode.cmd') }
    }

    return @($candidates | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique)
}

function Resolve-McpPluginAgentExecutablePath {
    param(
        [string]$AgentName,
        [string]$HostName,
        [string[]]$ExecutableCandidates = @())

    $candidates = @($ExecutableCandidates) + @(Get-McpPluginAgentExecutableCandidates -AgentName $AgentName -HostName $HostName)
    foreach ($candidate in $candidates) {
        if ([string]::IsNullOrWhiteSpace($candidate)) { continue }
        try {
            if (Test-Path -LiteralPath $candidate -PathType Leaf) {
                return (Resolve-Path -LiteralPath $candidate).ProviderPath
            }

            $command = Get-Command $candidate -ErrorAction SilentlyContinue | Select-Object -First 1
            if ($command -and -not [string]::IsNullOrWhiteSpace($command.Source)) {
                return [string]$command.Source
            }
        } catch {
            continue
        }
    }

    $pluginWrapper = ''
    if (-not [string]::IsNullOrWhiteSpace($env:MCP_PLUGIN_ROOT)) {
        $candidateWrapper = Join-Path $env:MCP_PLUGIN_ROOT 'Invoke-McpPlugin.ps1'
        if (Test-Path -LiteralPath $candidateWrapper -PathType Leaf) { $pluginWrapper = $candidateWrapper }
    }

    return Get-McpPluginFirstText @($env:MCP_AGENT_EXECUTABLE_PATH, $pluginWrapper)
}

function Resolve-McpPluginAgentExecutableVersion {
    param(
        [string]$ExecutablePath,
        [string]$AgentName,
        [string]$HostName)

    $explicit = Get-McpPluginFirstText @(
        $env:MCP_AGENT_EXECUTABLE_VERSION,
        $env:CODEX_EXECUTABLE_VERSION,
        $env:CLAUDE_EXECUTABLE_VERSION,
        $env:GROK_EXECUTABLE_VERSION,
        $env:COPILOT_EXECUTABLE_VERSION,
        $env:CLINE_EXECUTABLE_VERSION,
        $env:OPENCODE_EXECUTABLE_VERSION)
    if ($explicit) { return $explicit }

    if (-not [string]::IsNullOrWhiteSpace($ExecutablePath) -and (Test-Path -LiteralPath $ExecutablePath -PathType Leaf)) {
        try {
            $psi = [System.Diagnostics.ProcessStartInfo]::new()
            $psi.FileName = $ExecutablePath
            $psi.ArgumentList.Add('--version')
            $psi.UseShellExecute = $false
            $psi.RedirectStandardOutput = $true
            $psi.RedirectStandardError = $true
            $process = [System.Diagnostics.Process]::Start($psi)
            if ($process.WaitForExit(3000)) {
                $version = ($process.StandardOutput.ReadToEnd() + "`n" + $process.StandardError.ReadToEnd()).Trim()
                if ($version) { return ($version -split "\r?\n" | Where-Object { $_ } | Select-Object -First 1) }
            } else {
                $process.Kill($true)
            }
        } catch {
        }
    }

    return Get-McpPluginFirstText @($env:MCP_PLUGIN_VERSION, 'unknown')
}

function Resolve-McpPluginAgentHeaderFields {
    param(
        [Parameter(Mandatory)][string]$SessionId,
        [Parameter(Mandatory)][string]$CacheDir,
        [string]$AgentName,
        [string]$HostName,
        [string[]]$ExecutableCandidates = @())

    $agentSessionId = Get-McpPluginFirstText @(
        $env:MCP_AGENT_SESSION_ID,
        $env:CODEX_SESSION_ID,
        $env:CLAUDE_SESSION_ID,
        $env:GROK_SESSION_ID,
        $SessionId)
    $transcriptFile = Get-McpPluginFirstText @(
        $env:MCP_AGENT_SESSION_TRANSCRIPT_FILE,
        $env:CODEX_SESSION_FILE,
        $env:CODEX_ROLLOUT_FILE,
        $env:CLAUDE_TRANSCRIPT_PATH,
        $env:CLAUDE_SESSION_FILE,
        $env:GROK_TRANSCRIPT_PATH,
        $env:GROK_SESSION_FILE,
        (Join-Path $CacheDir 'session.jsonl'))
    $executablePath = Resolve-McpPluginAgentExecutablePath -AgentName $AgentName -HostName $HostName -ExecutableCandidates $ExecutableCandidates
    $executableVersion = Resolve-McpPluginAgentExecutableVersion -ExecutablePath $executablePath -AgentName $AgentName -HostName $HostName

    return [ordered]@{
        agentSessionId = $agentSessionId
        agentSessionTranscriptFile = $transcriptFile
        agentExecutablePath = $executablePath
        agentExecutableVersion = $executableVersion
    }
}